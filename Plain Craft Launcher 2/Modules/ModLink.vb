Imports System.Text.RegularExpressions
Imports Open.Nat

Public Module ModLink

#Region "UPnP 映射"
    ''' <summary>
    ''' UPnP 状态，可能值："Disabled", "Enabled", "Unsupported", "Failed"
    ''' </summary>
    Public UPnPStatus As String = "Disabled"
    Public UPnPMappingName As String = "PCL2 CE Link Lobby"
    Public UPnPDevice = Nothing
    Public CurrentUPnPMapping As Mapping = Nothing

    ''' <summary>
    ''' 寻找 UPnP 设备并尝试创建一个 UPnP 映射
    ''' </summary>
    Public Async Sub CreateUPnPMapping(Optional LocalPort As Integer = 25565, Optional PublicPort As Integer = 10240)
        Log($"[UPnP] 尝试创建 UPnP 映射，本地端口：{LocalPort}，远程端口：{PublicPort}，映射名称：{UPnPMappingName}")

        Dim UPnPDiscoverer = New NatDiscoverer()
        Dim cts = New CancellationTokenSource(10000)
        Try
            UPnPDevice = Await UPnPDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts)

            CurrentUPnPMapping = New Mapping(Protocol.Tcp, LocalPort, PublicPort, UPnPMappingName)
            Await UPnPDevice.CreatePortMapAsync(CurrentUPnPMapping)

            UPnPStatus = "Enabled"
            Log($"[UPnP] UPnP 映射创建成功")
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
    Public Async Sub RemoveUPnPMapping()
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
    Public Sub MCInstanceFinding()
        'Java 进程 PID 查询
        'Dim PIDLookupResult As String() = Nothing
        'Dim JavaNames As String() = Nothing
        'JavaNames.Append("java.exe")
        'JavaNames.Append("javaw.exe")

        'For Each java In JavaNames
        '    Dim JavaProcesses As Process() = Process.GetProcessesByName(java)
        '    If JavaProcesses Is Nothing OrElse JavaProcesses.Length <= 0 Then
        '        Continue For
        '    Else
        '        For Each p In JavaProcesses
        '            Log("[MCDetect] 检测到 Java 进程，PID: " + p.Id.ToString())
        '            PIDLookupResult.Append(p.Id.ToString())
        '        Next
        '    End If
        'Next

        '通过 PID 查询对应进程监听的端口号
        'Dim Pro As Process = New Process

        'Try
        '    Dim NetStatOutput As String = ShellAndGetOutput("netstat", "-ano")
        '    'Log(NetStatOutput)
        '    Dim reg As Regex = New Regex("\\s+", RegexOptions.Compiled)
        '    Dim line As String = Nothing
        '    Dim line1 As String = Nothing
        '    Dim reader As StringReader = New StringReader(NetStatOutput)
        '    While Not line = reader.ReadLine()
        '        line1 = reader.ReadLine().Trim()

        '        If Not line.StartsWithF("TCP") AndAlso Not line.StartsWithF("UDP") Then
        '            Continue While
        '        End If

        '        line = reg.Replace(line, ",")
        '        Log(line)
        '        Dim soc As String = line.Split(",")(1)

        '    End While
        'Catch ex As Exception

        'End Try

    End Sub

#End Region

End Module
