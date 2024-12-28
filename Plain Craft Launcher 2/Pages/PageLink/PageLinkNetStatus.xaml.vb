Imports System.Net
Imports STUN
Imports STUN.Attributes
Public Class PageLinkNetStatus
    Public NATType As String = Nothing
    Public NATTypeFriendly As String = Nothing
    Public UPnPStatus As String = Nothing
    Public IPv4Status As String = Nothing
    Public IPv4StatusFriendly As String = Nothing
    Public IPv6Status As String = Nothing
    Public IPv6StatusFriendly As String = Nothing
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
                               NATType = "TestFailed"
                           End Try

                           RunInUi(Sub()
                                       ChangeNATText()
                                   End Sub)
                       End Sub)

        'IP 检测
        RunInNewThread(Sub()
                           Log("[IP] 开始进行 IP 检测")
                           '获取本地 IP 地址
                           Dim LocalIPAddresses = Dns.GetHostAddresses(Dns.GetHostName())

                           Try
                               For Each IP In LocalIPAddresses
                                   If Sockets.AddressFamily.InterNetwork.Equals(IP.AddressFamily) Then 'IPv4
                                       Dim PublicIPv4Address As String = NetRequestRetry("http://4.ipw.cn", "GET", "", "application/x-www-form-urlencoded")

                                       If IP.ToString() = PublicIPv4Address Then '判断是否是公网地址
                                           IPv4Status = "Public"
                                           Log("[IP] 检测到 IPv4 公网地址")
                                           Exit For
                                       ElseIf IP.ToString().StartsWithF("169.254.") Then '判断是否是本地回环地址
                                           Continue For
                                       End If

                                       IPv4Status = "Supported"
                                       Log("[IP] 检测到 IPv4 支持")
                                       Exit For
                                   End If
                               Next
                           Catch ex As Exception
                               Log("[IP] IPv4 检测失败: " + ex.ToString())
                               IPv4Status = "Unsupported"
                           End Try

                           Try
                               For Each IP In LocalIPAddresses
                                   If Sockets.AddressFamily.InterNetworkV6.Equals(IP.AddressFamily) Then 'IPv6
                                       Dim PublicIPv6Address As String = NetRequestRetry("http://6.ipw.cn", "GET", "", "application/x-www-form-urlencoded")

                                       If IP.ToString() = PublicIPv6Address Then '判断是否是公网地址
                                           IPv6Status = "Public"
                                           Log("[IP] 检测到 IPv6 公网地址")
                                           Exit For
                                       ElseIf IP.ToString().StartsWithF("fe80") Then '判断是否是本地回环地址
                                           Continue For
                                       End If

                                       IPv6Status = "Supported"
                                       Log("[IP] 检测到 IPv6 地址")
                                   End If
                               Next
                           Catch ex As Exception
                               Log("[IP] IPv6 检测失败: " + ex.ToString())
                               IPv6Status = "Unsupported"
                           End Try

                           If IPv4Status Is Nothing Then IPv4Status = "Unsupported" '致敬每一位勇士
                           If IPv6Status Is Nothing Then IPv6Status = "Unsupported" '如果轮了一圈出来还是没 IPv6 地址，那就是没有

                           Log($"[IP] IP 检测完成，IPv4 支持情况: {IPv4Status}，IPv6 支持情况: {IPv6Status}")

                           RunInUi(Sub()
                                       ChangeIPText()
                                   End Sub)
                       End Sub)
    End Sub
    Public Sub ChangeNATText()
        Select Case NATType
            Case = "OpenInternet"
                NATTypeFriendly = "开放"
            Case = "FullCone"
                NATTypeFriendly = "中等（完全圆锥）"
            Case = "Restricted"
                NATTypeFriendly = "中等（受限圆锥）"
            Case = "PortRestricted"
                NATTypeFriendly = "中等（端口受限圆锥）"
            Case = "Symmetric"
                NATTypeFriendly = "严格（对称）"
            Case = "SymmetricUDPFirewall"
                NATTypeFriendly = "严格（对称 + 防火墙）"
            Case = "Unspecified"
                NATTypeFriendly = "未知"
            Case = "TestFailed"
                NATTypeFriendly = "测试失败"
        End Select

        If Not NATType = "TestFailed" Then
            LabNetStatusNATTitle.Text = "NAT 类型：" + NATTypeFriendly.Substring(0, 2)
            LabNetStatusNATDesc.Text = $"当前 NAT 类型为 {NATTypeFriendly}，UPnP 已启用。{vbCrLf}NAT 类型决定了你是否能与对方建立直接连接。你可以尝试调整光猫和路由器设置以改善 NAT 环境。"
        Else
            LabNetStatusNATTitle.Text = "NAT 类型：测试失败"
            LabNetStatusNATDesc.Text = $"NAT 测试失败，请检查你的防火墙和互联网连接。{vbCrLf}NAT 类型决定了你是否能与对方建立直接连接。你可以尝试调整光猫和路由器设置以改善 NAT 环境。"
        End If
    End Sub
    Public Sub ChangeIPText()
        Select Case IPv4Status
            Case = "Public"
                IPv4StatusFriendly = "公网"
            Case = "Supported"
                IPv4StatusFriendly = "支持"
            Case = "Unsupported"
                IPv4StatusFriendly = "不支持"
        End Select

        Select Case IPv6Status
            Case = "Public"
                IPv6StatusFriendly = "公网"
            Case = "Supported"
                IPv6StatusFriendly = "支持"
            Case = "Unsupported"
                IPv6StatusFriendly = "不支持"
        End Select

        Dim IPStatusTitle As String = Nothing
        Dim IPStatusDesc As String = Nothing
        If Not IPv6Status = "Unsupported" AndAlso Not IPv4Status = "Unsupported" Then
            IPStatusTitle = "IPv6 优先"
            IPStatusDesc = "你的网络环境支持 IPv6，这会让连接更加顺利。"
        ElseIf IPv6Status = "Unsupported" AndAlso Not IPv4Status = "Unsupported" Then
            IPStatusTitle = "仅 IPv4"
            IPStatusDesc = "支持 IPv6 很可能会让连接更加顺利。你可以尝试调整光猫和路由器设置以获取 IPv6 地址。"
        ElseIf IPv6Status = "Unsupported" AndAlso IPv4Status = "Unsupported" Then
            IPStatusTitle = "仅 IPv6"
            IPStatusDesc = "你的网络环境仅支持 IPv6，你可真勇敢..."
        Else
            IPStatusTitle = "你真的连上网了么..."
            IPStatusDesc = "你的网络既不支持 IPv4 也不支持 IPv6，请检查你的防火墙和互联网连接。"
        End If

        LabNetStatusIPv6Title.Text = "IP 版本：" + IPStatusTitle
        LabNetStatusIPv6Desc.Text = $"本地 IPv4 状态: {IPv4StatusFriendly}，本地 IPv6 状态: {IPv6StatusFriendly}。{vbCrLf}{IPStatusDesc}"
    End Sub

    Private Sub BtnNATTest_Click(sender As Object, e As EventArgs) Handles BtnNATTest.Click
        NetStatusTest()
    End Sub
End Class
