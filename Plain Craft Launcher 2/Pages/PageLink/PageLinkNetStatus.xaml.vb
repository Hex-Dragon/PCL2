Imports System.Net
Imports STUN
Imports STUN.Attributes
Public Class PageLinkNetStatus
    Public NATType As String = Nothing
    Public UPnPStatus As String = Nothing
    Public IPv4Status As String = Nothing
    Public IPv6Status As String = Nothing
    Public Sub NetStatusTest()
        Hint("正在检测，请稍等...")

        'IPv4 NAT 类型检测
        RunInNewThread(Sub()
                           Dim STUNServerDomain As String = "stun.miwifi.com" '指定 STUN 服务器
                           Log("[STUN] 指定的 STUN 服务器: " + STUNServerDomain)
                           Try
                               Dim STUNServerIP As String = Dns.GetHostAddresses(STUNServerDomain)(0).ToString() '解析 STUN 服务器 IP
                               Log("[STUN] 解析目标 STUN 服务器 IP: " + STUNServerIP)
                               Dim STUNServerEndPoint As IPEndPoint = New IPEndPoint(IPAddress.Parse(STUNServerIP), 3478) '设置 IPEndPoint

                               STUNClient.ReceiveTimeout = 500 '设置超时
                               Log("[STUN] 开始进行 NAT 测试")
                               Dim STUNTestResult = STUNClient.Query(STUNServerEndPoint, STUNQueryType.ExactNAT, True) '进行 STUN 测试

                               If Not STUNTestResult.QueryError = STUNQueryError.Success Then
                                   Log("[STUN] NAT 测试失败")
                                   NATType = "TestFailed"
                                   Throw New Exception()
                                   Exit Sub
                               End If

                               NATType = STUNTestResult.NATType.ToString()
                               Log("[STUN] NAT 检测完成，本地 NAT 类型为: " + NATType)

                               If NATType = "OpenInternet" Then IPv4Status = "Public"
                           Catch ex As Exception
                               Log("[STUN] 进行 NAT 测试失败: " + ex.ToString())
                           End Try

                           Hint(NATType)
                       End Sub)

        'IPv6 检测
        RunInNewThread(Sub()
                           Dim LocalIPAddresses = Dns.GetHostAddresses(Dns.GetHostName())
                           For Each IP In LocalIPAddresses
                               If System.Net.Sockets.AddressFamily.InterNetwork.Equals(IP.AddressFamily) Then
                                   NetRequestRetry("4.ipw.cn", "GET", "", "application/x-www-form-urlencoded")
                                   IPv4Status = "Supported"
                               End If
                           Next


                       End Sub)
    End Sub
    Public Sub ChangeNATText()
        LabNetStatusNATTitle.Text = "NAT 类型：中等"
    End Sub

    Private Sub BtnNATTest_Click(sender As Object, e As EventArgs) Handles BtnNATTest.Click
        NetStatusTest()
    End Sub
End Class
