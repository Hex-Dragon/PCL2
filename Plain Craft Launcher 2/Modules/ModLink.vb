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
    Public UPnPPublicPort As String = Nothing

    ''' <summary>
    ''' 寻找 UPnP 设备并尝试创建一个 UPnP 映射
    ''' </summary>
    Public Async Sub CreateUPnPMapping(Optional LocalPort As Integer = 25565, Optional PublicPort As Integer = 10240)
        Log($"[UPnP] 尝试创建 UPnP 映射，本地端口：{LocalPort}，远程端口：{PublicPort}，映射名称：{UPnPMappingName}")

        UPnPPublicPort = PublicPort
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
        Log("[MCDetect] 开始检测 Java 进程及其监听端口")
        Dim PIDLookupResultArrayList As ArrayList = New ArrayList

        Dim JavaNames As ArrayList = New ArrayList
        JavaNames.Add("javaw")
        JavaNames.Add("java")

        For Each java In JavaNames
            Dim JavaProcesses As Process() = Process.GetProcessesByName(java)
            If JavaProcesses Is Nothing OrElse JavaProcesses.Length <= 0 Then
                Log("[MCDetect] 未检测到 Java 进程")
                Exit Sub
            Else
                For Each p In JavaProcesses
                    Log("[MCDetect] 检测到 Java 进程，PID: " + p.Id.ToString())
                    PIDLookupResultArrayList.Add(p.Id.ToString())
                Next
            End If
        Next

        '通过 PID 查询对应进程监听的端口号
        'Dim Pro As Process = New Process

        'Try
        '    Dim NetStatOutput As String = ShellAndGetOutput("netstat", "-ano")
        '    Log("[MCDetect] 查询 NetStat 信息成功")
        '    'Log(NetStatOutput)
        '    Dim reg As Regex = New Regex("\\s+", RegexOptions.Compiled)
        '    'Dim line As String = Nothing
        '    Dim lines As String() = NetStatOutput.Split("\r")
        '    Log("line length: " + lines.Length.ToString())

        'For Each line In lines
        'Log("当前行：" + line)
        'line = line.Trim()

        'If Not line.StartsWithF("TCP") AndAlso Not line.StartsWithF("UDP") Then
        '    Continue For
        'End If

        'line = reg.Replace(line, ",")
        'Log(line)
        'Dim pid As String = line.Split(",")(4)
        'Dim fullip As String = line.Split(",")(1)
        'Dim port As String = fullip.Split(":")(1)
        'If fullip.StartsWithF("0.0.0.0") AndAlso PIDLookupResultArrayList.Contains(pid) Then
        '    Log($"Java 进程 PID {pid} 识别到监听端口 {port}")
        'End If
        'Next

        '    Dim line As String = Nothing
        '    Dim sr As StringReader = New StringReader(NetStatOutput)
        '    While True
        '        line = line.Trim()
        '        If line.Contains("[") Then
        '            Exit While
        '        End If
        '        Log("当前行：" + line)

        '        If Not line.StartsWithF("TCP") AndAlso Not line.StartsWithF("UDP") Then
        '            Continue While
        '        End If

        '        line = reg.Replace(line, ",")
        '        Log(line)
        '        Dim pid As String = line.Split(",")(4)
        '        Dim fullip As String = line.Split(",")(1)
        '        Dim port As String = fullip.Split(":")(1)
        '        If fullip.StartsWithF("0.0.0.0") AndAlso PIDLookupResultArrayList.Contains(pid) Then
        '            Log($"Java 进程 PID {pid} 识别到监听端口 {port}")
        '        End If

        '    End While
        'Catch ex As Exception
        '    Log("[MCDetect] 通过 PID 查询端口号异常: " + ex.ToString())
        'End Try

    End Sub

#End Region

End Module
