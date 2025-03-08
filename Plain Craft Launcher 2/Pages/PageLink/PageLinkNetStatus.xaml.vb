Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports STUN
Imports STUN.Attributes
Imports Makaretu.Nat
Imports PCL.ModLink
Public Class PageLinkNetStatus
    Public NetQualityCounter As Integer = 0

    Public NATType As String = Nothing
    Public NATTypeFriendly As String = Nothing
    Public UPnPStatusFriendly As String = Nothing

    Public Enum IPSupportStatus
        Open
        Supported
        Unsupported
    End Enum
    Public IPv4Status As IPSupportStatus = Nothing
    Public IPv4StatusFriendly As String = Nothing
    Public IPv6Status As IPSupportStatus = Nothing
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
        CreateUPnPMapping()
        Thread.Sleep(500) '因为异步不会处理直接硬等 0.5s
        If UPnPStatus = UPnPStatusType.Enabled Then
            UPnPStatusFriendly = "已启用"
            RemoveUPnPMapping()
            Thread.Sleep(500)
            If UPnPStatus = UPnPStatusType.Failed Then
                UPnPStatusFriendly = "异常"
            End If
        ElseIf UPnPStatus = UPnPStatusType.Unsupported Then
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
        Dim TaskCompleted As Boolean = False

        RunInNewThread(Sub()
                           Dim V4PubDetected As Boolean = Nothing
                           Dim V6PubDetected As Boolean = Nothing

                           Try
                               For Each ip In NatDiscovery.GetIPAddresses()
                                   If Not V4PubDetected AndAlso ip.AddressFamily() = AddressFamily.InterNetwork Then 'IPv4
                                       If ip.IsPublic() Then
                                           Hint("Public v4: " + ip.ToString())
                                           IPv4Status = IPSupportStatus.Open
                                           Log("[IP] 检测到 IPv4 公网地址")
                                           V4PubDetected = True
                                           Continue For
                                       ElseIf ip.IsPrivate() Then
                                           IPv4Status = IPSupportStatus.Supported
                                           Log("[IP] 检测到 IPv4 支持")
                                           Continue For
                                       Else
                                           Continue For
                                       End If
                                   End If

                                   If Not V6PubDetected AndAlso ip.AddressFamily() = AddressFamily.InterNetworkV6 Then 'IPv6
                                       If ip.IsPublic() Then
                                           IPv6Status = IPSupportStatus.Open
                                           Log("[IP] 检测到 IPv6 公网地址")
                                           V6PubDetected = True
                                           Continue For
                                       ElseIf ip.IsPrivate() Then
                                           IPv6Status = IPSupportStatus.Supported
                                           Log("[IP] 检测到 IPv6 支持")
                                           Continue For
                                       ElseIf ip.IsIPv6LinkLocal() OrElse ip.IsIPv6SiteLocal() OrElse ip.IsIPv6Teredo() OrElse ip.IsIPv4MappedToIPv6() Then
                                           Continue For
                                       End If
                                   End If
                               Next

                               If IPv4Status = Nothing Then IPv4Status = IPSupportStatus.Unsupported '致敬每一位勇士
                               If IPv6Status = Nothing Then IPv6Status = IPSupportStatus.Unsupported '如果轮了一圈出来还是没 IPv6 地址，那就是没有

                               Log($"[IP] IP 检测完成，IPv4 支持情况: {IPv4Status}，IPv6 支持情况: {IPv6Status}")
                           Catch ex As Exception
                               Log("[IP] 检测 IP 版本支持失败: " + ex.ToString())
                           Finally
                               TaskCompleted = True
                           End Try
                       End Sub, "IPStatus")

        While Not TaskCompleted
            Thread.Sleep(200)
        End While

        Log($"[IP] IP 检测完成，IPv4 支持情况: {IPv4Status.ToString()}，IPv6 支持情况: {IPv6Status.ToString()}")

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
            Case = IPSupportStatus.Open
                IPv4StatusFriendly = "公网"
                NetQualityCounter += 2
            Case = IPSupportStatus.Supported
                IPv4StatusFriendly = "支持"
            Case = IPSupportStatus.Unsupported
                IPv4StatusFriendly = "不支持"
        End Select

        Select Case IPv6Status
            Case = IPSupportStatus.Open
                IPv6StatusFriendly = "公网"
                NetQualityCounter += 2
            Case = IPSupportStatus.Supported
                IPv6StatusFriendly = "支持"
                NetQualityCounter += 1
            Case = IPSupportStatus.Unsupported
                IPv6StatusFriendly = "不支持"
        End Select

        Dim IPStatusTitle As String = Nothing
        Dim IPStatusDesc As String = Nothing
        If Not IPv6Status = IPSupportStatus.Unsupported AndAlso Not IPv4Status = IPSupportStatus.Unsupported Then
            IPStatusTitle = "IPv6 优先"
            IPStatusDesc = "你的网络环境支持 IPv6，这会让连接更加顺利。"
        ElseIf IPv6Status = IPSupportStatus.Unsupported AndAlso Not IPv4Status = IPSupportStatus.Unsupported Then
            IPStatusTitle = "仅 IPv4"
            IPStatusDesc = "支持 IPv6 很可能会让连接更加顺利。你可以尝试调整光猫和路由器设置以获取 IPv6 地址。"
        ElseIf Not IPv6Status = IPSupportStatus.Unsupported AndAlso IPv4Status = IPSupportStatus.Unsupported Then
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
