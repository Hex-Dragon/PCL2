Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports STUN
Imports STUN.Attributes
Public Class PageLinkNetStatus
    Public NetQualityCounter As Integer = 0

    Public NATType As String = Nothing
    Public NATTypeFriendly As String = Nothing
    Public UPnPStatusFriendly As String = Nothing

    Public IPv4Status As String = Nothing
    Public IPv4StatusFriendly As String = Nothing
    Public IPv6Status As String = Nothing
    Public IPv6StatusFriendly As String = Nothing

    Public Shared PublicIPv4Address As String = Nothing

    Public Sub NetStatusTest()
        If Convert.ToBoolean(ReadReg("LinkFirstTimeNetTest", "True")) Then
            MyMsgBox($"你似乎是第一次打开 PCL 的联机模块。为了正常运行联机模块，PCL 接下来会申请 Windows 防火墙权限。{vbCrLf}{vbCrLf}请在接下来出现的弹窗中点击 ""允许""。", "首次联机提示", "我知道了", ForceWait:=True)
            Dim TestTcpListener = TcpListener.Create("5600")
            TestTcpListener.Start()
            Thread.Sleep(200)
            TestTcpListener.Stop()
            WriteReg("LinkFirstTimeNetTest", "False")
        End If

        RunInUi(Sub()
                    FrmLinkLeft.NetStatusUpdate("正在检测...")

                    LabNetStatusNATTitle.Text = "NAT 类型：正在检测"
                    LabNetStatusNATDesc.Text = "正在检测 NAT 类型，这可能需要几秒钟"

                    LabNetStatusPingTitle.Text = "Ping 值：正在检测"
                    LabNetStatusPingDesc.Text = "正在检测 Ping 值，这可能需要几秒钟"

                    LabNetStatusIPv6Title.Text = "IP 版本：正在检测"
                    LabNetStatusIPv6Desc.Text = "正在检测 IP 版本，这可能需要几秒钟"
                End Sub)

        RunInNewThread(Sub()
                           NATTest()
                           PingTest()
                           IPTest()
                           ChangeNetQualityText()
                       End Sub)
    End Sub
    Public Sub NATTest()
        'IPv4 NAT 类型检测
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

        'UPnP 映射测试
        ModLink.CreateUPnPMapping()
        Thread.Sleep(500) '因为异步不会处理直接硬等 0.5s
        If ModLink.UPnPStatus = "Enabled" Then
            UPnPStatusFriendly = "已启用"
            ModLink.RemoveUPnPMapping()
            Thread.Sleep(500)
            If ModLink.UPnPStatus = "Failed" Then
                UPnPStatusFriendly = "异常"
            End If
        ElseIf ModLink.UPnPStatus = "Unsupported" Then
            UPnPStatusFriendly = "不兼容"
        Else
            UPnPStatusFriendly = "异常"
        End If

        RunInUi(Sub()
                    ChangeNATText()
                End Sub)
    End Sub
    Public Sub PingTest()
        Dim PingSender = New Ping()
        Dim PingReplied As PingReply = Nothing
        Dim PingRtt As String = Nothing
        Dim PingServerDomain As String = "www.baidu.com" '指定 Ping 服务器
        Log("[Ping] Ping 目标服务器: " + PingServerDomain)
        Try
            Dim PingServerIP As String = Dns.GetHostAddresses(PingServerDomain)(0).ToString() '解析 Ping 服务器 IP
            Log("[Ping] 解析 Ping 目标服务器 IP: " + PingServerIP)

            Log("[Ping] 开始进行 Ping 测试")
            PingReplied = PingSender.Send(PingServerIP)
            If PingReplied.Status = IPStatus.Success Then
                PingRtt = PingReplied.RoundtripTime
            End If

            Log($"[Ping] Ping 测试完成，Ping 值: {PingRtt} ms")

            If PingRtt >= 100 Then
                NetQualityCounter -= 1
            End If
        Catch ex As Exception
            Log("[Ping] 进行 Ping 测试失败: " + ex.ToString())
        End Try

        RunInUi(Sub()
                    LabNetStatusPingTitle.Text = $"Ping 值：{PingRtt} ms"
                    LabNetStatusPingDesc.Text = $"{If(PingRtt >= 100, "当前网络延迟较高，可能会影响联机体验", "当前网络延迟较低")}{vbCrLf}Ping 值可以反映你的网络延迟水平，一般来说越低越好。"
                End Sub)
    End Sub
    Public Sub IPTest()
        'IP 检测
        Log("[IP] 开始进行 IP 检测")
        '获取本地 IP 地址
        Dim LocalIPAddresses = Dns.GetHostAddresses(Dns.GetHostName())

        Dim TaskTotal = 0
        Dim TaskCompleted = 0

        TaskTotal += 1
        RunInNewThread(Sub()
                           Try
                               For Each IP In LocalIPAddresses
                                   If Sockets.AddressFamily.InterNetwork.Equals(IP.AddressFamily) Then 'IPv4
                                       Dim PublicIPv4Address As String = NetRequestOnce("http://4.ipw.cn", "GET", "", "application/x-www-form-urlencoded", 4000)

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
                           Finally
                               TaskCompleted += 1
                           End Try
                       End Sub, "NetStatus V4")

        TaskTotal += 1
        RunInNewThread(Sub()
                           Try
                               For Each IP In LocalIPAddresses
                                   If Sockets.AddressFamily.InterNetworkV6.Equals(IP.AddressFamily) Then 'IPv6
                                       Dim PublicIPv6Address As String = NetRequestOnce("http://6.ipw.cn", "GET", "", "application/x-www-form-urlencoded", 4000)

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
                           Finally
                               TaskCompleted += 1
                           End Try
                       End Sub, "NetStatus V6")

        While TaskCompleted <> TaskTotal
            Thread.Sleep(200)
        End While

        If IPv4Status Is Nothing Then IPv4Status = "Unsupported" '致敬每一位勇士
        If IPv6Status Is Nothing Then IPv6Status = "Unsupported" '如果轮了一圈出来还是没 IPv6 地址，那就是没有

        Log($"[IP] IP 检测完成，IPv4 支持情况: {IPv4Status}，IPv6 支持情况: {IPv6Status}")

        RunInUi(Sub()
                    ChangeIPText()
                End Sub)
    End Sub
    Public Sub ChangeNetQualityText()
        Thread.Sleep(200)
        Dim NetQualityText As String = Nothing
        If NetQualityCounter >= 4 Then
            NetQualityText = "网络优秀"
        ElseIf NetQualityCounter >= 2 Then
            NetQualityText = "网络良好"
        Else
            NetQualityText = "网络较差"
        End If

        Log($"[Link] 最终网络质量指数: {NetQualityCounter}，判定网络质量: {NetQualityText}")

        RunInUi(Sub()
                    FrmLinkLeft.NetStatusUpdate(NetQualityText)
                End Sub)
    End Sub
    Public Sub ChangeNATText()
        Dim NATTypeDesc As String = Nothing

        Select Case NATType
            Case = "OpenInternet"
                NATTypeFriendly = "开放"
                NATTypeDesc = "当前网络环境不会影响联机体验，适合作为大厅创建者"
                NetQualityCounter += 3
            Case = "FullCone"
                NATTypeFriendly = "中等（完全圆锥）"
                NATTypeDesc = "当前网络环境不会影响联机体验，适合作为大厅创建者"
                NetQualityCounter += 3
            Case = "Restricted"
                NATTypeFriendly = "中等（受限圆锥）"
                NATTypeDesc = "这可能会影响您的联机体验"
                NetQualityCounter += 2
            Case = "PortRestricted"
                NATTypeFriendly = "中等（端口受限圆锥）"
                NATTypeDesc = "部分路由器和防火墙设置可能会影响您的联机体验"
                NetQualityCounter += 1
            Case = "Symmetric"
                NATTypeFriendly = "严格（对称）"
                NATTypeDesc = "这将严重影响您的联机体验"
            Case = "SymmetricUDPFirewall"
                NATTypeFriendly = "严格（对称 + 防火墙）"
                NATTypeDesc = "这将严重影响您的联机体验"
            Case = "Unspecified"
                NATTypeFriendly = "未知"
                NATTypeDesc = "这将严重影响您的联机体验"
            Case = "TestFailed"
                NATTypeFriendly = "测试失败"
                NATTypeDesc = "这将严重影响您的联机体验，请检查你的防火墙和互联网连接"
        End Select

        If Not NATType = "TestFailed" Then
            LabNetStatusNATTitle.Text = "NAT 类型：" + NATTypeFriendly.Substring(0, 2) + If(UPnPStatusFriendly = "已启用", " + UPnP", "")
            LabNetStatusNATDesc.Text = $"当前 NAT 类型为 {NATTypeFriendly}，UPnP {UPnPStatusFriendly}{vbCrLf}{NATTypeDesc}"
        Else
            LabNetStatusNATTitle.Text = "NAT 类型：测试失败"
            LabNetStatusNATDesc.Text = $"NAT 测试失败，UPnP {UPnPStatusFriendly}{vbCrLf}{NATTypeDesc}"
        End If
    End Sub
    Public Sub ChangeIPText()
        Select Case IPv4Status
            Case = "Public"
                IPv4StatusFriendly = "公网"
                NetQualityCounter += 2
            Case = "Supported"
                IPv4StatusFriendly = "支持"
            Case = "Unsupported"
                IPv4StatusFriendly = "不支持"
        End Select

        Select Case IPv6Status
            Case = "Public"
                IPv6StatusFriendly = "公网"
                NetQualityCounter += 2
            Case = "Supported"
                IPv6StatusFriendly = "支持"
                NetQualityCounter += 1
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
End Class
