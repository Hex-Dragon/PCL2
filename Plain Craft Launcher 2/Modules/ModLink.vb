Imports System.Text.RegularExpressions
Imports Open.Nat

Public Class ModLink

#Region "UPnP 映射"
    Public Shared Async Sub StartUPnPRequest(Optional LocalPort As Integer = 25565, Optional PublicPort As Integer = 10240)
        Dim UPnPDiscoverer = New NatDiscoverer()
        Dim cts = New CancellationTokenSource(10000)
        Dim UPnPDevice = Await UPnPDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts)

        Await UPnPDevice.CreatePortMapAsync(New Mapping(Protocol.Tcp, 10140, 10240, "PCL2 Link Lobby"))
        Hint("UPnP 映射已创建")
    End Sub
#End Region

#Region "Minecraft 实例探测"
    Public Shared Sub MCInstanceFinding()
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

End Class
