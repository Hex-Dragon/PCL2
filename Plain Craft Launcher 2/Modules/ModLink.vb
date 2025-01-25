Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Open.Nat
Imports System.Net
Imports System.Net.Sockets

Public Class ModLink

#Region "MCPing"
    Public Class WorldInfo
        Public Property Port As Integer
        Public Property VersionName As String
        Public Property PlayerMax As Integer
        Public Property PlayerOnline As Integer
        Public Property Description As String
        Public Property Favicon As String

        Public Overrides Function ToString() As String
            Return $"[MCPing] Version: {VersionName}, Players: {PlayerOnline}/{PlayerMax}, Description: {Description}"
        End Function
    End Class

    Public Class MCPing


        Sub New(IP As String, Port As Integer)
            _IP = IP
            _Port = Port
        End Sub

        Private _IP As String
        Private _Port As Integer

        ''' <summary>
        ''' 对疑似 MC 端口进行 MCPing，并返回相关信息
        ''' </summary>
        Public Async Function GetInfo() As Tasks.Task(Of WorldInfo)
            Try
                ' 创建 TCP 客户端并连接到服务器
                Using client As New TcpClient(_IP, _Port)
                    ' 向服务器发送握手数据包
                    Using stream = client.GetStream()
                        If Not stream.CanWrite OrElse Not stream.CanRead Then Return New WorldInfo

                        Dim handshake As Byte() = BuildHandshake(_IP, _Port)
                        Await stream.WriteAsync(handshake, 0, handshake.Length)
                        Log($"[MCPing] Send {String.Join(" ", handshake)}", LogLevel.Debug)

                        ' 向服务器发送查询状态信息的数据包
                        Dim statusRequest As Byte() = BuildStatusRequest()
                        Await stream.WriteAsync(statusRequest, 0, statusRequest.Length)
                        Log($"[MCPing] Send {String.Join(" ", statusRequest)}")

                        ' 读取服务器响应的数据
                        Dim result As New List(Of Byte)
                        While True
                            Dim responseBuffer(1024) As Byte
                            Dim bytesRead As Integer = Await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length)
                            If bytesRead = 0 Then Exit While
                            result.AddRange(responseBuffer.Take(bytesRead))
                        End While

                        Log($"[MCPing] Received ({result.Count}) = {String.Join(" ", result)}")
                        ' 将响应数据转换为字符串
                        Dim response As String = Encoding.UTF8.GetString(result.ToArray(), 0, result.Count)
                        Dim i = 0
                        While i < response.Length AndAlso response.Chars(i) <> "{" AndAlso response.Chars(i + 1) <> """"
                            i += 1
                        End While
                        If i = response.Length Then Return New WorldInfo
                        response = response.Substring(i)
                        Log("[MCPing] Server Response: " & response)

                        Dim j = JObject.Parse(response)

                        Return New WorldInfo With {
                        .VersionName = j("version")("name"),
                        .PlayerMax = j("players")("max"),
                        .PlayerOnline = j("players")("online"),
                        .Description = j("description")("text"),
                        .Favicon = j("favicon"),
                        .Port = _Port
                        }
                    End Using
                End Using
            Catch ex As Exception
                Log(ex, "[MCPing] Error: " & ex.Message)
            End Try
            Return New WorldInfo
        End Function


        Function BuildHandshake(serverIp As String, serverPort As Integer) As Byte()
            ' 构建握手数据包
            Dim handshake As New List(Of Byte)
            handshake.AddRange(GetVarInt(0)) ' 数据包 ID 握手包
            handshake.AddRange(GetVarInt(578)) ' 协议
            Dim encodedIP = Encoding.UTF8.GetBytes(serverIp)
            handshake.AddRange(GetVarInt(encodedIP.Length)) ' 服务器地址长度
            handshake.AddRange(encodedIP) ' 服务器地址
            handshake.AddRange(BitConverter.GetBytes(CUShort(serverPort)).Reverse()) ' 服务器端口
            handshake.AddRange(GetVarInt(1)) ' 下一个状态 获取服务器状态

            handshake.InsertRange(0, GetVarInt(handshake.Count))

            Return handshake.ToArray()
        End Function

        Function BuildStatusRequest() As Byte()
            ' 构建状态请求数据包
            Dim packet As New List(Of Byte)
            packet.AddRange(GetVarInt(1))
            packet.AddRange(GetVarInt(0))
            Return packet.ToArray() ' 状态请求数据包
        End Function

        Private Function GetVarInt(value As Integer) As Byte()
            If value < 0 Then Return {}
            Dim result As New List(Of Byte)
            Do
                Dim temp As Byte = CByte(value And &H7F)
                value >>= 7
                If value <> 0 Then
                    temp = temp Or &H80
                End If
                result.Add(temp)
            Loop While value <> 0
            Return result.ToArray()
        End Function
    End Class
#End Region

#Region "端口查找"
    Public Class PortFinder
        ' 定义需要的结构和常量
        <StructLayout(LayoutKind.Sequential)>
        Public Structure MIB_TCPROW_OWNER_PID
            Public dwState As Integer
            Public dwLocalAddr As Integer
            Public dwLocalPort As Integer
            Public dwRemoteAddr As Integer
            Public dwRemotePort As Integer
            Public dwOwningPid As Integer
        End Structure

        <DllImport("iphlpapi.dll", SetLastError:=True)>
        Public Shared Function GetExtendedTcpTable(
        ByVal pTcpTable As IntPtr,
        ByRef dwOutBufLen As Integer,
        ByVal bOrder As Boolean,
        ByVal ulAf As Integer,
        ByVal TableClass As Integer,
        ByVal reserved As Integer) As Integer
        End Function

        Public Shared Function GetProcessPort(ByVal dwProcessId As Integer) As List(Of Integer)
            Dim ports As New List(Of Integer)
            Dim tcpTable As IntPtr = IntPtr.Zero
            Dim dwSize As Integer = 0
            Dim dwRetVal As Integer

            If dwProcessId = 0 Then
                Return ports
            End If

            dwRetVal = GetExtendedTcpTable(IntPtr.Zero, dwSize, True, 2, 5, 0)
            If dwRetVal <> 0 AndAlso dwRetVal <> 122 Then ' 122 表示缓冲区不足
                Return ports
            End If

            tcpTable = Marshal.AllocHGlobal(dwSize)
            Try
                If GetExtendedTcpTable(tcpTable, dwSize, True, 2, 5, 0) <> 0 Then
                    Return ports
                End If

                Dim tablePtr As IntPtr = tcpTable
                Dim dwNumEntries As Integer = Marshal.ReadInt32(tablePtr)
                tablePtr = IntPtr.Add(tablePtr, 4)

                For i As Integer = 0 To dwNumEntries - 1
                    Dim row As MIB_TCPROW_OWNER_PID = Marshal.PtrToStructure(Of MIB_TCPROW_OWNER_PID)(tablePtr)
                    If row.dwOwningPid = dwProcessId Then
                        ports.Add(row.dwLocalPort >> 8 Or (row.dwLocalPort And &HFF) << 8) ' 转换端口号
                    End If
                    tablePtr = IntPtr.Add(tablePtr, Marshal.SizeOf(Of MIB_TCPROW_OWNER_PID)())
                Next
            Finally
                Marshal.FreeHGlobal(tcpTable)
            End Try

            Return ports
        End Function
    End Class
#End Region

#Region "UPnP 映射"
    ''' <summary>
    ''' UPnP 状态，可能值："Disabled", "Enabled", "Unsupported", "Failed"
    ''' </summary>
    Public Shared UPnPStatus As String = "Disabled"
    Public Shared UPnPMappingName As String = "PCL2 CE Link Lobby"
    Public Shared UPnPDevice = Nothing
    Public Shared CurrentUPnPMapping As Mapping = Nothing
    Public Shared UPnPPublicPort As String = Nothing

    ''' <summary>
    ''' 寻找 UPnP 设备并尝试创建一个 UPnP 映射
    ''' </summary>
    Public Shared Async Sub CreateUPnPMapping(Optional LocalPort As Integer = 25565, Optional PublicPort As Integer = 10240)
        Log($"[UPnP] 尝试创建 UPnP 映射，本地端口：{LocalPort}，远程端口：{PublicPort}，映射名称：{UPnPMappingName}")

        UPnPPublicPort = PublicPort
        Dim UPnPDiscoverer = New NatDiscoverer()
        Dim cts = New CancellationTokenSource(10000)
        Try
            UPnPDevice = Await UPnPDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts)

            CurrentUPnPMapping = New Mapping(Protocol.Tcp, LocalPort, PublicPort, UPnPMappingName)
            Await UPnPDevice.CreatePortMapAsync(CurrentUPnPMapping)

            Await UPnPDevice.CreatePortMapAsync(New Mapping(Protocol.Tcp, LocalPort, PublicPort, "PCL2 Link Lobby"))
            Hint("UPnP 映射已创建")
        Catch NotFoundEx As NatDeviceNotFoundException
            UPnPStatus = "Unsupported"
            CurrentUPnPMapping = Nothing
            Log("[UPnP] 找不到可用的 UPnP 设备")
        Catch ex As Exception
            UPnPStatus = "Failed"
            CurrentUPnPMapping = Nothing
            Log("[UPnP] UPnP 映射创建失败: " + ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' 尝试移除现有 UPnP 映射记录
    ''' </summary>
    Public Shared Async Sub RemoveUPnPMapping()
        Log($"[UPnP] 尝试移除 UPnP 映射，本地端口：{CurrentUPnPMapping.PrivatePort}，远程端口：{CurrentUPnPMapping.PublicPort}，映射名称：{UPnPMappingName}")

        Try
            Await UPnPDevice.DeletePortMapAsync(CurrentUPnPMapping)

            UPnPStatus = "Disabled"
            CurrentUPnPMapping = Nothing
            Log("[UPnP] UPnP 映射移除成功")
        Catch ex As Exception
            UPnPStatus = "Failed"
            CurrentUPnPMapping = Nothing
            Log("[UPnP] UPnP 映射移除失败: " + ex.ToString())
        End Try
    End Sub

#End Region

#Region "Minecraft 实例探测"
    Public Shared Async Function MCInstanceFinding() As Tasks.Task(Of List(Of WorldInfo))
        'Java 进程 PID 查询
        Dim PIDLookupResult As New List(Of String)
        Dim JavaNames As New List(Of String)
        JavaNames.Add("java")
        JavaNames.Add("javaw")

        For Each java In JavaNames
            Dim JavaProcesses As Process() = Process.GetProcessesByName(java)
            Log($"[MCDetect] 找到 {java} 进程 {JavaProcesses.Length} 个")

            If JavaProcesses Is Nothing OrElse JavaProcesses.Length = 0 Then
                Continue For
            Else
                For Each p In JavaProcesses
                    Log("[MCDetect] 检测到 Java 进程，PID: " + p.Id.ToString())
                    PIDLookupResult.Add(p.Id.ToString())
                Next
            End If
        Next

        Dim res As New List(Of WorldInfo)
        Try
            If Not PIDLookupResult.Any Then Return res
            Dim ports = PortFinder.GetProcessPort(Integer.Parse(PIDLookupResult.First))
            Log($"[MCDetect] 获取到端口数量 {ports.Count}")
            For Each port In ports
                Log($"[MCDetect] 找到疑似端口，开始验证：{port}")
                Dim test As New MCPing("127.0.0.1", port)
                Dim info = Await test.GetInfo()
                If Not String.IsNullOrWhiteSpace(info.VersionName) Then
                    Log($"[MCDetect] 端口 {port} 为有效 Minecraft 世界")
                    res.Add(info)
                End If
            Next
        Catch ex As Exception
            Log(ex, "[MCDetect] 获取端口信息错误", LogLevel.Debug)
        End Try
        Return res
    End Function
#End Region

End Class
