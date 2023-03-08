Imports System.Net
Imports System.Net.Sockets

Public Class PageLinkIoi
    Public Const RequestVersion As Integer = 4
    Public Const IoiVersion As Integer = 10 '由于已关闭更新渠道，在提升 IoiVersion 时必须提升 RequestVersion
    Public Shared PathIoi As String = PathAppdata & "联机模块\IOI 联机模块.exe"

#Region "初始化"

    '页面初始化
    Private IsLoad As Boolean = False
    Private Sub MeLoaded() Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        RefreshUi()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

        '更新线程
        RunInNewThread(Sub()
                           Do While True
                               Thread.Sleep(200)
                               If FrmMain.PageCurrent.Page = FormMain.PageType.Link AndAlso FrmMain.PageCurrentSub = FormMain.PageSubType.LinkIoi Then RunInUiWait(Sub() FrmLinkIoi.RefreshUi())
                               RefreshWorker()
                           Loop
                       End Sub, "Link Timer")

    End Sub

    '加载器初始化与左边栏处理
    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanBack, PanAlways, InitLoader, Sub()
                                                                      End Sub)
    End Sub

    '初始化加载器与步骤
    Public Shared WithEvents InitLoader As New LoaderCombo(Of Integer)("联机模块初始化", {
        New LoaderTask(Of Integer, Integer)("端口检查", AddressOf InitPortCheck) With {.ProgressWeight = 1},
        New LoaderTask(Of Integer, Integer)("启动请求核心", AddressOf StartSocketListener) With {.ProgressWeight = 1},
        New LoaderTask(Of Integer, List(Of NetFile))("初次启动尝试", AddressOf InitFirst) With {.ProgressWeight = 4},
        New LoaderDownload("下载更新文件", New List(Of NetFile)) With {.ProgressWeight = 6},
        New LoaderTask(Of List(Of NetFile), Boolean)("二次启动尝试", AddressOf InitSecond) With {.ProgressWeight = 3},
        New LoaderTask(Of Boolean, Boolean)("创建请求核心房间", AddressOf InitRequest) With {.ProgressWeight = 2}
    })
    '判断端口是否被占用
    Private Shared Sub InitPortCheck()
        '检查协议
        If Not Setup.Get("LinkEula") Then
Reopen:
            Select Case MyMsgBox("PCL 的联机服务由速聚授权提供。" & vbCrLf & "在使用前，你需要同意速聚的用户服务协议和隐私政策。", "协议授权", "同意", "拒绝", "查看用户服务协议和隐私政策")
                Case 1
                    Setup.Set("LinkEula", True)
                Case 2
                    Throw New Exception("你拒绝了用户服务协议……")
                Case 3
                    OpenWebsite("https://mp.weixin.qq.com/mp/appmsgalbum?__biz=MzkxMTMyODk3Mg==&action=getalbum&album_id=2585385685407514625&scene=173&from_msgid=2247483720&from_itemidx=1&count=3&nolastread=1#wechat_redirect")
                    GoTo Reopen
            End Select
        End If
        '先掐死
        IoiStop(True)
        '检查端口
        Dim HasConflict As Boolean = False
        For Each Port In Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            If Port.Port <> 55555 AndAlso Port.Port <> 55557 Then Continue For
            Log("[IOI] 发现端口 " & Port.Port & " 被占用")
            HasConflict = True
        Next
        If Not HasConflict Then Exit Sub
        '对应的报错
        For Each Line As String In ShellAndGetOutput("netstat", "-ano", 30000).Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
            If Not (Line.Contains("127.0.0.1:55555") OrElse Line.Contains("127.0.0.1:55557")) Then Continue For
            Dim ProcessName As String
            Try
                ProcessName = Process.GetProcessById(Line.Split(" ").Last).ProcessName
            Catch ex As Exception
                Log(ex, "获取占用端口的进程信息失败，假定进程已结束")
                Continue For
            End Try
            If ProcessName = "联机模块" Then
                Throw New Exception("由于一个已知问题，请在重启电脑后再尝试使用联机功能")
            ElseIf ProcessName = "Idle" Then
                '不知道为啥反正又没占用了
                Log("[IOI] 未发现占用此端口的程序，继续执行")
            Else
                Throw New Exception("端口被程序 " & ProcessName & " 占用，无法启动联机模块，请在任务管理器关闭此程序后再试")
            End If
        Next
    End Sub
    '下载更新前尝试启动
    Private Shared Sub InitFirst(Task As LoaderTask(Of Integer, List(Of NetFile)))
        '初次启动尝试
        If IoiVersion <> Setup.Get("LinkIoiVersion") Then
            Log("[IOI] 设置版本强制要求联机模块更新")
            Setup.Set("LinkIoiVersion", IoiVersion)
        ElseIf File.Exists(PathIoi) Then
            Task.Progress = 0.2
            CheckFirewall()
            If IoiStart() Then
                '已完成启动与初始化
                Task.Output = New List(Of NetFile)
                Exit Sub
            Else
                '需要更新
                IoiStop(True)
                File.Delete(PathIoi)
                GoTo StartDownload
            End If
            '如果抛出异常则直接使加载器失败
        End If
        '开始下载
StartDownload:
        Task.Progress = 0.8
        Log("[IOI] 需要下载联机模块")
        If File.Exists(PathTemp & "联机模块.zip") Then File.Delete(PathTemp & "联机模块.zip")
        Task.Output = New List(Of NetFile) From {New NetFile(
            {"https://gitcode.net/to/cato_bin/-/raw/master/ioi_v2_x" & If(Is32BitSystem, 32, 64) & ".zip",
             "http://mirror.hiper.cn.s2.the.bb/ioi_v2_x" & If(Is32BitSystem, 32, 64) & ".zip",
             "http://mirror.hiper.cn.s3.the.bb:175/ioi_v2_x" & If(Is32BitSystem, 32, 64) & ".zip",
             "https://pcl2-server-1253424809.file.myqcloud.com/link/ioi_v2_x" & If(Is32BitSystem, 32, 64) & ".zip{CDN}"},
            PathTemp & "联机模块.zip")}
    End Sub
    '下载更新后尝试启动
    Private Shared Sub InitSecond(Task As LoaderTask(Of List(Of NetFile), Boolean))
        '若首次尝试已经成功，则直接跳过
        If IoiState = LoadState.Finished Then Exit Sub
        '解压更新包
        Log("[IOI] 解压联机模块以完成下载")
        If File.Exists(PathTemp & "ioi.exe") Then File.Delete(PathTemp & "ioi.exe")
        If File.Exists(PathIoi) Then File.Delete(PathIoi)
        Compression.ZipFile.ExtractToDirectory(PathTemp & "联机模块.zip", PathTemp)
        File.Delete(PathTemp & "联机模块.zip")
        CopyFile(PathTemp & "ioi.exe", PathIoi)
        File.Delete(PathTemp & "ioi.exe")
        Task.Progress = 0.4
        '再次尝试启动
        CheckFirewall()
        If IoiStart() Then Exit Sub
        IoiStop(True)
        Throw New Exception("联机模块初始化失败")
    End Sub
    '在 IOI 启动后建立 55557 房间
    Private Shared Sub InitRequest()
        Try
            Dim Result As JObject = GetJson(NetRequestOnce("http://127.0.0.1:55555/api/port?proto=tcp&port=55557" & "&password=" & IoiPassword, "PUT", "", "", 100000))
            If Result("msg") IsNot Nothing Then Throw New InvalidOperationException(Result("msg").ToString)
        Catch ex As InvalidOperationException
            'API 返回的错误
            Log("创建请求核心房间失败：" & ex.Message, LogLevel.Msgbox)
        Catch ex As Exception
            '常规错误
            Log(ex, "创建请求核心房间失败", LogLevel.Msgbox)
        End Try
    End Sub

    '检查防火墙权限，并添加对应的权限
    Private Shared Sub CheckFirewall()
        Try
            Log("[IOI] Windows 防火墙：检测开始")
            If Not PageLinkLeft.FirewallIsBlock(PathIoi) Then Exit Try
            If PageLinkLeft.FirewallPolicy.CurrentProfile.ExceptionsNotAllowed Then
                '禁止白名单
                MyMsgBox("由于 Windows 防火墙阻止了所有传入连接，PCL 无法获取防火墙通行权限。" & vbCrLf &
                         "联机会有很大概率失败，就算连上了，延迟也会变高……" & vbCrLf & vbCrLf &
                         "请先关闭 Windows 防火墙中的 " & vbLQ & "阻止所有传入连接" & vbRQ & " 选项，然后重启 PCL。", "遭到防火墙拦截")
            ElseIf IsAdmin() Then
                '有管理员权限
                Log("[IOI] Windows 防火墙：尝试添加防火墙通行权限")
                Try
                    PageLinkLeft.FirewallAddAuthorized("Plain Craft Launcher 启动器（IOI 联机模块）", PathIoi)
                Catch ex As Exception
                    Log(ex, "无法将联机模块添加至防火墙白名单，可能导致联机失败", LogLevel.Msgbox)
                End Try
            Else
                '无管理员权限
                If MyMsgBox("由于你开启了 Windows 防火墙，PCL 需要获取防火墙通行权限。" & vbCrLf & vbCrLf &
                            "若继续，PCL 将尝试以管理员权限重新启动。" & vbCrLf &
                            "若拒绝，联机模块可能会被防火墙拦截，联机会有很大概率失败。", "需要管理员权限", "继续", "拒绝") = 1 Then
                    Log("[IOI] Windows 防火墙：尝试提升权限")
                    If RerunAsAdmin("--link ioi") Then
                        FrmMain.EndProgram(False) '已重新运行
                    Else
                        Hint("获取管理员权限失败，请尝试右键 PCL，选择 " & vbLQ & "以管理员身份运行" & vbRQ & "，然后再进入联机页面！", HintType.Critical)
                    End If
                Else
                    Hint("在没有防火墙权限的情况下尝试联机，很可能会导致联机失败！", HintType.Critical)
                End If
            End If
            Log("[IOI] Windows 防火墙：无防火墙通行权限")
        Catch ex As Exception
            Log(ex, "无法检测防火墙状态，可能导致联机失败", LogLevel.Msgbox)
        End Try
    End Sub

#End Region

#Region "进程管理"

    Private Shared IoiId As String, IoiPassword As String
    Private Shared IoiProcess As Process = Nothing
    Private Shared IoiState As LoadState = LoadState.Waiting

    ''' <summary>
    ''' 若 Ioi 正在运行，则结束 Ioi 进程，同时初始化状态数据。返回是否关闭了对应进程。
    ''' </summary>
    Public Shared Function IoiStop(SleepWhenKilled As Boolean) As Boolean
        IoiStop = False
        '发送断开信息
        Try
            If IoiProcess IsNot Nothing AndAlso Not IoiProcess.HasExited Then
                For i = 0 To UserList.Count - 1
                    If i > UserList.Count - 1 Then Exit For
                    Dim User = UserList.Values(i)
                    SendDisconnectRequest(User)
                Next
            End If
        Catch ex As Exception
            Log(ex, "结束 IOI 进程时发送 Disconnect 信息失败")
        End Try
        '关闭所有 Ioi 进程
        For Each ProcessObject In Process.GetProcesses
            If ProcessObject.ProcessName <> "IOI 联机模块" Then Continue For
            IoiStop = True
            Try
                ProcessObject.Kill()
                Log("[IOI] 已关闭联机模块：" & ProcessObject.Id)
                If SleepWhenKilled Then Thread.Sleep(3000) '等待 3 秒确认进程已退出
            Catch ex As Exception
                Log(ex, "关闭联机模块失败（" & ProcessObject.Id & "）")
            End Try
        Next
        '初始化
        IoiProcess = Nothing
        IoiId = Nothing
        IoiPassword = Nothing
        IoiState = LoadState.Waiting
        UserList = New Dictionary(Of String, LinkUserIoi)
        RoomListForMe = New List(Of RoomEntry)
    End Function
    ''' <summary>
    ''' 启动 Ioi，并等待初始化完成后退出运行，同时更新 IoiId 与 IoiPassword。
    ''' 正常初始化返回 True，需要更新返回 False，其余情况抛出异常。
    ''' 若 Ioi 正在运行，则会先停止其运行。
    ''' </summary>
    Public Shared Function IoiStart() As Boolean
        IoiStop(True)
        Log("[IOI] 启动联机模块进程")
        Dim Info = New ProcessStartInfo With {
            .FileName = PathIoi,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardError = True,
            .RedirectStandardOutput = True,
            .WorkingDirectory = PathTemp
        }
        IoiProcess = New Process() With {.StartInfo = Info}
        Using outputWaitHandle As New AutoResetEvent(False)
            Using errorWaitHandle As New AutoResetEvent(False)
                AddHandler IoiProcess.OutputDataReceived, Function(sender, e)
                                                              Try
                                                                  If e.Data Is Nothing Then
                                                                      outputWaitHandle.[Set]()
                                                                  Else
                                                                      IoiLogLine(e.Data)
                                                                  End If
                                                              Catch ex As ObjectDisposedException
                                                              Catch ex As Exception
                                                                  Log(ex, "读取联机模块信息失败")
                                                                  IoiState = LoadState.Failed
                                                              End Try
                                                              Return Nothing
                                                          End Function
                AddHandler IoiProcess.ErrorDataReceived, Function(sender, e)
                                                             Try
                                                                 If e.Data Is Nothing Then
                                                                     errorWaitHandle.[Set]()
                                                                 Else
                                                                     IoiLogLine(e.Data)
                                                                 End If
                                                             Catch ex As ObjectDisposedException
                                                             Catch ex As Exception
                                                                 Log(ex, "读取联机模块错误信息失败")
                                                                 IoiState = LoadState.Failed
                                                             End Try
                                                             Return Nothing
                                                         End Function
                IoiProcess.Start()
                IoiState = LoadState.Loading
                IoiProcess.BeginOutputReadLine()
                IoiProcess.BeginErrorReadLine()
                '等待
                Do Until IoiProcess.HasExited OrElse IoiState <> LoadState.Loading
                    Thread.Sleep(10)
                Loop
                '输出
                If IoiState = LoadState.Finished Then
                    Log("[IOI] 联机模块启动成功")
                    Return True
                Else
                    Throw New Exception("联机模块启动失败")
                End If
                'Select Case IoiState
                '    Case LoadState.Finished
                '        Log("[IOI] 联机模块启动成功")
                '        Return True
                '    Case LoadState.Aborted
                '        Log("[IOI] 联机模块要求更新")
                '        Return False
                '    Case LoadState.Failed
                '        Log("[IOI] 联机模块启动出现异常")
                '        Return False
                '    Case Else 'LoadState.Loading
                '        Throw New Exception("联机模块启动失败，请检查你的网络连接")
                'End Select
            End Using
        End Using
    End Function

    'Ioi 日志
    Private Shared Sub IoiLogLine(Content As String)

        '初始化
        If Content.Contains(" > http://127.0.0.1:55555/") Then Exit Sub
        If Content.Contains("All done") Then
            If IoiId IsNot Nothing AndAlso IoiPassword IsNot Nothing Then
                IoiState = LoadState.Finished
            Else
                Log("[IOI] 联机模块汇报初始化完成，但未提供账户信息")
                IoiState = LoadState.Failed
            End If
            Exit Sub
        End If
        If Content.Contains("Password :: ") Then
            IoiPassword = Content.Split({"Password :: "}, StringSplitOptions.None)(1)
            Exit Sub '为保证安全不记录密码到 Log
        End If

        '初始化
        If Content.Contains("ID :: ") Then IoiId = Content.Split({"ID :: "}, StringSplitOptions.None)(1)
        If Content.Contains("Initialization failed") OrElse Content.Contains("The version is ") Then IoiState = LoadState.Aborted

        '系统回应
        If Content.Contains("'portssub' from ") Then LastPortsId = Content.Split(" ").Last
        If Content.Contains("Listening tcp ") Then
            If UserList.ContainsKey(LastPortsId) Then
                UserList(LastPortsId).Ports(Content.Split(" ").Last) = RegexSeek(Content, "(?<=Listening tcp )[^:]+")
            Else
                Log("[IOI] 未在列表中的用户出现意料外的连接信息")
                NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & LastPortsId & "&password=" & IoiPassword, "DELETE", "", "", 500)
            End If
        End If

        '断开连接
        If Content.Contains("Closed tcp connection from ") AndAlso Content.Contains(":55557") Then
            For i = 0 To UserList.Count - 1
                Dim User = UserList.Values(i)
                If Not Content.Contains(User.Ports(55557)) OrElse User.IsDisposed Then Continue For
                Log("[IOI] 检测到 55557 端口断开（" & User.DisplayName & "）")
                UserRemove(User, True)
                Exit For
            Next
        End If

        '写入日志
        If Not Content.Contains("read /dev/stdin: The handle is invalid.") AndAlso LogLinesCount < If(ModeDebug, 10000, 1000) Then
            LogLinesCount += 1
            Log("[IOI] " & Content)
        End If

    End Sub
    Private Shared LogLinesCount As Integer = 0
    Private Shared LastPortsId As String = "" '上一个收到 portssub 的 ID，用于记录端口

#End Region

#Region "时钟"

    'UI 线程刷新
    Private Shared UserListIdentifyCache As String = ""
    Private Shared RoomListIdentifyCache As String = ""
    Public Sub RefreshUi()
        Try
            '确认用户列表缓存
            Dim UserListIdentify As String = Join(UserList, vbCrLf)
            If UserListIdentify <> UserListIdentifyCache Then
                UserListIdentifyCache = UserListIdentify
                '刷新列表项
                PanUserList.Children.Clear()
                For i = 0 To UserList.Count - 1
                    If i > UserList.Count - 1 Then Exit For
                    PanUserList.Children.Add(UserList.Values(i).ToListItem)
                Next
                '刷新列表标题
                CardUser.Title = "已连接的玩家 (" & PanUserList.Children.Count & ")"
            End If
            '刷新用户列表项
            For Each UserItem As MyListItem In PanUserList.Children
                CType(UserItem.Tag, LinkUserIoi).RefreshUi(UserItem)
            Next
            '确认房间列表缓存
            Dim RoomListIdentify As String = Join(GetRoomList(), vbCrLf)
            If RoomListIdentify <> RoomListIdentifyCache Then
                RoomListIdentifyCache = RoomListIdentify
                '刷新列表项
                PanRoom.Children.Clear()
                For Each Room In GetRoomList()
                    PanRoom.Children.Add(Room.ToListItem)
                Next
            End If
            '刷新房间列表项
            For Each RoomItem As MyListItem In PanRoom.Children
                CType(RoomItem.Tag, RoomEntry).RefreshUi(RoomItem)
            Next
            '刷新操作提示
            If UserList.Count = 0 Then
                If RoomListForMe.Count = 0 Then
                    LabHint.Text = "若想创建房间，请点击创建房间按钮，并按说明进行操作。" & vbCrLf & "若想加入他人的房间，请点击建立连接，然后输入对方的联机码。"
                Else
                    LabHint.Text = "若想让其他人加入你的房间，请点击复制联机码，然后让你的朋友输入你的联机码以建立连接。"
                End If
            Else
                If GetRoomList.Count = 0 Then
                    LabHint.Text = "若想创建房间，请点击创建房间按钮，然后输入对局域网开放后 MC 显示的端口号，或本地服务端的端口号。"
                ElseIf RoomListForMe.Count = 0 Then
                    '已有连接，本人没有房间，对方有房间
                    LabHint.Text = "若想加入某个房间，直接点击该房间即可获取说明。"
                Else
                    '已有连接，且本人有房间
                    LabHint.Text = "指向你所创建的房间，能在右侧找到修改房间名称、关闭房间等选项。" &
                                   If(RoomListForMe.Count <> GetRoomList.Count, vbCrLf & "若想加入其他人的房间，直接点击该房间即可获取说明。", "")
                End If
            End If
        Catch ex As Exception
            Log(ex, "联机模块 UI 时钟运行失败", LogLevel.Feedback)
        End Try
    End Sub

    '工作线程刷新
    Public Sub RefreshWorker()
        Try
            '检测退出
            If InitLoader.State = LoadState.Finished AndAlso IoiProcess.HasExited Then
                Log("[IOI] 联机模块出现异常！", LogLevel.Hint)
                ModuleStopManually()
            End If
            '检测用户
            For i = 0 To UserList.Values.Count - 1
                If i > UserList.Values.Count - 1 Then Exit For
                Dim User = UserList.Values(i)
                If User.Progress < 1 Then Continue For '跳过连接中的用户
                '发送心跳包
                If Date.Now - User.LastSend > New TimeSpan(0, 0, RandomInteger(50, 30)) Then
                    RunInNewThread(Sub()
                                       Try
                                           SendUpdateRequest(User, 1)
                                       Catch ex As Exception
                                           Log(ex, "心跳包发送失败（" & User.DisplayName & "）", LogLevel.Normal)
                                       End Try
                                   End Sub, "Link Heartbeat " & User.Id)
                End If
                '检测被动离线
                If Date.Now - User.LastReceive > New TimeSpan(0, 2, 0) Then
                    SendDisconnectRequest(User)
                    Hint("与 " & User.DisplayName & " 的连接已中断！", HintType.Critical)
                End If
            Next
        Catch ex As Exception
            Log(ex, "联机模块工作时钟运行失败", LogLevel.Feedback)
        End Try
    End Sub

#End Region

#Region "发送请求"

    ''' <summary>
    ''' 发送 Portsub 请求并等待获取控制台端口。进度将从 0 变化至 80%。
    ''' </summary>
    Private Shared Sub SendPortsubRequest(User As LinkUserIoi)
        Log("[IOI] 尝试建立连接：" & User.Id)
        '向对方发送 portsub 请求
        Dim Retry As Integer = 0
RetryLink:
        Dim Result As JObject = GetJson(NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "PUT", "", "", 100000))
        '失败与重试
        If Result("msg") IsNot Nothing Then
            Dim ErrorMessage As String = Result("msg").ToString
            Select Case ErrorMessage
                Case "failed to find any peer in table"
                    Retry += 1
                    ErrorMessage = "我方网络环境不佳，连接失败。" '初始化对等机列表超时
                    Log("[IOI] 尝试建立连接结果：未找到对等机，第 " & Retry & " 级重试")
                Case "routing: not found"
                    Retry += 4
                    ErrorMessage = "我方或对方网络环境不佳，或对方已关闭联机模块，未找到路由。" '未知情况
                    Log("[IOI] 尝试建立连接结果：无法连接到路由，第 " & Retry & " 级重试")
                Case "you are already connected to specified host"
                    If User.IsDisposed Then Throw New ThreadInterruptedException("用户对象已被释放")
                    Log("[IOI] 尝试建立连接结果：已与对方连接")
                    GoTo Done
                    'Retry += 1
                    'NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "DELETE", "", "", 100000)
                    'ErrorMessage = "已与对方连接。" '双向连接偶尔导致
                    'Log("[IOI] 已与对方连接，尝试中断现有连接，第 " & Retry & " 级重试")
                Case "dial backoff"
                    Retry += 20
                    ErrorMessage = "对方网络环境不佳，或对方已关闭联机模块。请尝试让对方主动连接你，而不是你去连接对方。" 'NAT2 连 NAT4 导致
                    Log("[IOI] 尝试建立连接结果：NAT 异常，第 " & Retry & " 级重试")
                Case Else
                    If ErrorMessage.Contains("all dials failed") Then
                        Retry += 8
                        ErrorMessage = "我方或对方网络环境不佳，或对方已关闭联机模块，连接失败。" '网络不佳，或对方已关闭模块
                        Log("[IOI] 尝试建立连接结果：连接失败，第 " & Retry & " 级重试")
                    Else
                        Retry += 8
                        Log("[IOI] 尝试建立连接结果：未知错误（" & ErrorMessage & "），第 " & Retry & " 级重试")
                    End If
            End Select
            If User.IsDisposed Then Throw New ThreadInterruptedException("用户对象已被释放")
            If Retry <= 64 Then
                User.Progress = Retry * 0.01 + 0.05
                Thread.Sleep(3000)
                GoTo RetryLink
            Else
                Throw New InvalidOperationException(ErrorMessage)
            End If
        End If
        Log("[IOI] 尝试建立连接结果：成功")
        '等待获取对方端口
Done:
        Dim WaitCount As Integer = 0
        Do Until User.Ports.ContainsKey(55555) AndAlso User.Ports.ContainsKey(55557)
            WaitCount += 1
            User.Progress = 0.7 + WaitCount / 200 * 0.1 '70% ~ 80%
            If WaitCount = 100 Then Throw New Exception("连接超时，请尝试重新连接（未收到端口回报）！")
            Thread.Sleep(150)
        Loop
        User.Progress = 0.8
    End Sub

    ''' <summary>
    ''' 向控制台发送 Connect 请求。
    ''' </summary>
    Private Shared Sub SendConnectRequest(User As LinkUserIoi)
        Dim RawJson As New JObject()
        RawJson("version") = RequestVersion
        RawJson("name") = GetPlayerName()
        RawJson("id") = IoiId
        RawJson("type") = "connect"
        User.Send(RawJson)
    End Sub

    ''' <summary>
    ''' 向控制台发送 Update 请求。
    ''' </summary>
    Private Shared Sub SendUpdateRequest(User As LinkUserIoi, Stage As Integer, Optional Unique As Long = -1)
        If Unique = -1 Then Unique = GetTimeTick()
        Dim RawJson As New JObject
        RawJson("name") = GetPlayerName()
        RawJson("id") = IoiId
        RawJson("type") = "update"
        RawJson("stage") = Stage
        RawJson("unique") = Unique
        If Stage < 3 Then
            Dim Rooms As New JArray
            For Each Room In RoomListForMe
                Dim RoomObject As New JObject
                RoomObject("name") = Room.DisplayName
                RoomObject("port") = Room.Port
                Rooms.Add(RoomObject)
            Next
            RawJson("rooms") = Rooms
            User.PingPending(Unique) = Date.Now
        End If
        User.Send(RawJson)
    End Sub

    ''' <summary>
    ''' 尝试发送断开请求，并将其从用户列表中移除。
    ''' </summary>
    Private Shared Sub SendDisconnectRequest(User As LinkUserIoi, Optional Message As String = Nothing, Optional IsError As Boolean = False)
        Dim RawJson As New JObject()
        RawJson("id") = IoiId
        RawJson("type") = "disconnect"
        If Message IsNot Nothing Then
            RawJson("message") = Message
            RawJson("isError") = IsError
        End If
        Try
            User.Send(RawJson)
            Thread.Sleep(50)
        Catch
        End Try
        NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
        User.Dispose()
    End Sub

#End Region

#Region "左边栏操作"

    '刷新连接
    Private Shared Sub BtnListRefresh_Click(sender As MyIconButton, e As EventArgs)
        Dim User As LinkUserIoi = sender.Tag
        User.PingRecord.Clear()
        FrmLinkIoi.RefreshUi()
        RunInThread(Sub()
                        Try
                            SendUpdateRequest(User, 1)
                        Catch ex As Exception
                            If InitLoader.State = LoadState.Finished Then Log(ex, "刷新与 " & User.DisplayName & " 的连接失败", LogLevel.Hint)
                        End Try
                    End Sub)
    End Sub
    '断开连接
    Private Shared Sub BtnListDisconnect_Click(sender As MyIconButton, e As EventArgs)
        Dim User As LinkUserIoi = sender.Tag
        sender.IsEnabled = False
        RunInThread(Sub()
                        If User.Progress < 1 AndAlso User.RelativeThread IsNot Nothing AndAlso User.RelativeThread.IsAlive Then
                            NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
                            User.Dispose()
                        Else
                            SendDisconnectRequest(User, GetPlayerName() & " 主动断开了连接！")
                        End If
                    End Sub)
    End Sub
    '复制联机码
    Public Shared Sub BtnLeftCopy_Click() Handles BtnLeftCopy.Click
        ClipboardSet(IoiId.Substring(4) & SecretEncrypt(GetPlayerName), False)
        Hint("已复制联机码！", HintType.Finish)
    End Sub

#End Region

#Region "玩家名"

    ''' <summary>
    ''' 获取当前的玩家名。
    ''' </summary>
    Public Shared Function GetPlayerName() As String
        '自动生成玩家名
        If AutogenPlayerName Is Nothing Then
            If IsPlayerNameValid(McLoginName) Then
                AutogenPlayerName = McLoginName()
            Else
                AutogenPlayerName = "玩家 " & CType(GetHash(If(UniqueAddress, "")) Mod 1048576, Integer).ToString("x5").ToUpper
            End If
        End If
        '获取玩家自定义的名称
        Dim CustomName As String = Setup.Get("LinkName").ToString.Trim()
        If CustomName <> "" Then
            If IsPlayerNameValid(CustomName) Then
                Return CustomName.Trim
            Else
                Hint("你所设置的玩家名存在异常，已被重置！", HintType.Critical)
                Setup.Set("LinkName", "")
            End If
        End If
        '使用自动生成的玩家名
        Return AutogenPlayerName
    End Function
    Private Shared AutogenPlayerName As String = Nothing '并非由玩家自定义，而是自动生成的玩家名
    ''' <summary>
    ''' 检查某个玩家名是否合法。
    ''' </summary>
    Private Shared Function IsPlayerNameValid(Name As String) As Boolean
        For Each ValidateRule As Validate In {New ValidateNullOrWhiteSpace, New ValidateLength(0, 20), New ValidateFilter}
            If Not String.IsNullOrEmpty(ValidateRule.Validate(Name)) Then Return False
        Next
        Return True
    End Function

#End Region

#Region "请求核心"

    Private Shared Listener As Socket = Nothing
    ''' <summary>
    ''' 启动 Socket 监听核心。
    ''' </summary>
    Public Shared Sub StartSocketListener()
        If Listener IsNot Nothing Then Exit Sub '已经启动过了
        Listener = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) With {.ReceiveTimeout = 1000000000, .SendTimeout = 1000000000}
        Listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
        Try
            Listener.Bind(New IPEndPoint(IPAddress.Any, 55557))
        Catch ex As Exception
            Log(ex, "初次启动 Socket 监听核心失败，开始重试")
            Thread.Sleep(1000)
            Listener.Bind(New IPEndPoint(IPAddress.Any, 55557))
        End Try
        Listener.Listen(100)
        Log("[IOI] 已启动 Socket 监听核心")
        RunInNewThread(Sub()
                           While True
                               Try
                                   '获取新的 Socket
                                   Dim bytes(1024) As Byte
                                   Dim ClientSocket As Socket = Listener.Accept()
                                   '获取初始输入信息
                                   Dim RequestInput As String = Encoding.UTF8.GetString(bytes, 0, ClientSocket.Receive(bytes))
                                   If Not RequestInput.StartsWith("PCL - ") Then Exit Sub
                                   Dim RawData As String
                                   Try
                                       RawData = SecretDecrypt(WebUtility.UrlDecode(RequestInput.Substring(6)))
                                       Log("[IOI] 接收到新的 Socket：" & RawData)
                                       PageLinkIoi.ReceiveJson(GetJson(RawData), ClientSocket)
                                   Catch ex As Exception
                                       Log(ex, "新的 Socket 处理出错（内容：" & RequestInput & "）")
                                   End Try
                               Catch ex As Exception
                                   Log(ex, "Socket 监听出错")
                                   Thread.Sleep(3000) '防止死循环出错
                               End Try
                           End While
                       End Sub, "Socket Listener")
    End Sub

#End Region

#Region "用户核心"

    '用户基类
    Public MustInherit Class LinkUserBase
        Implements IDisposable

        '基础数据
        Public Uuid As Integer = GetUuid()
        Public Id As String
        Public DisplayName As String

        '请求管理
        Public Socket As Socket = Nothing
        Public Sub Send(Request As JObject)
            If Socket Is Nothing Then Throw New Exception("该用户尚未绑定 Socket")
            Dim Data As String = Request.ToString(Newtonsoft.Json.Formatting.None)
            If ModeDebug Then Log("[IOI] 发送联机数据包（" & DisplayName & "）：" & Data)
            Data = "PCL - " & WebUtility.UrlEncode(SecretEncrypt(Data))
            Me.LastSend = Date.Now
            Socket.Send(Encoding.UTF8.GetBytes(Data))
        End Sub
        Public ListenerThread As Thread = Nothing
        Public Sub StartListener()
            If ListenerThread IsNot Nothing Then Exit Sub
            '启动监听
            ListenerThread = RunInNewThread(Sub()
                                                Try
                                                    Dim bytes(1024) As Byte
                                                    While PageLinkIoi.UserList.ContainsValue(Me)
                                                        Dim RequestInput As String = Encoding.UTF8.GetString(bytes, 0, Socket.Receive(bytes))
                                                        If Not RequestInput.StartsWith("PCL - ") Then Exit Sub
                                                        Dim RawData As String = SecretDecrypt(WebUtility.UrlDecode(RequestInput.Substring(6)))
                                                        If ModeDebug Then Log("[IOI] 接收联机数据包（" & DisplayName & "）：" & RawData)
                                                        PageLinkIoi.ReceiveJson(GetJson(RawData)) '调用去实际的联机模块
                                                    End While
                                                Catch ex As ThreadInterruptedException
                                                    Log("[IOI] 用户监听已中断（" & DisplayName & "）")
                                                Catch ex As Exception
                                                    If IsDisposed Then Exit Sub
                                                    If ex.GetType.Equals(GetType(SocketException)) AndAlso CType(ex, SocketException).SocketErrorCode = 10053 Then
                                                        Log("[IOI] 客户端已关闭（" & DisplayName & "）")
                                                        PageLinkIoi.UserRemove(CType(Me, PageLinkIoi.LinkUserIoi), True)
                                                        Exit Sub
                                                    End If
                                                    Log(ex, "用户监听出错（" & DisplayName & "）")
                                                    PageLinkIoi.UserRemove(CType(Me, PageLinkIoi.LinkUserIoi), True)
                                                End Try
                                            End Sub, "Link Listener " & DisplayName)
        End Sub
        Public Sub BindSocket(Socket As Socket)
            If Me.Socket IsNot Nothing Then Throw New Exception("该用户已经绑定了 Socket")
            Me.Socket = Socket
            StartListener()
        End Sub

        'Ping
        '0：与 Ping 计算无关，不回应
        '1：A to B，2：B to A，3：A to B
        Public PingPending As New Dictionary(Of Long, Date)
        Public PingRecord As New Queue(Of Integer)

        '心跳包
        Public LastSend As Date = Date.Now
        Public LastReceive As Date = Date.Now

        '类型转换
        Public Sub New(Id As String, DisplayName As String)
            Me.Id = Id
            Me.DisplayName = DisplayName
            Log("[IOI] 无通信包的新用户对象：" & ToString())
        End Sub
        Public Sub New(Id As String, DisplayName As String, Socket As Socket)
            Me.Id = Id
            Me.DisplayName = DisplayName
            Me.Socket = Socket
            Log("[IOI] 新用户对象：" & ToString())
            StartListener()
        End Sub
        Public Overrides Function ToString() As String
            Return DisplayName & " @ " & Id & " #" & Uuid
        End Function
        Public Shared Widening Operator CType(User As LinkUserBase) As String
            Return User.ToString
        End Operator

        '释放资源
        Public IsDisposed As Boolean = False
        Protected Overridable Sub Dispose(IsDisposing As Boolean)
            If Socket IsNot Nothing Then Socket.Dispose()
            If ListenerThread IsNot Nothing AndAlso ListenerThread.IsAlive Then ListenerThread.Interrupt()
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            If Not IsDisposed Then
                IsDisposed = True
                Dispose(True)
            End If
            GC.SuppressFinalize(Me)
        End Sub
    End Class

    '用户对象
    Public Shared UserList As New Dictionary(Of String, LinkUserIoi)
    Public Class LinkUserIoi
        Inherits LinkUserBase
        Public Sub New(Id As String, DisplayName As String, Socket As Socket)
            MyBase.New(Id, DisplayName, Socket)
        End Sub
        Public Sub New(Id As String, DisplayName As String)
            MyBase.New(Id, DisplayName)
        End Sub

        '基础数据
        Public Ports As New Dictionary(Of Integer, String)
        Public Rooms As New List(Of RoomEntry)

        '进度与 UI
        Public Progress As Double = 0
        Public RelativeThread As Thread = Nothing

        Public Function GetDescription() As String
            Return If(Progress < 1,
                "正在连接，" & Math.Round(Progress * 100) & "%",
                "已连接，" & If(PingRecord.Count = 0, "检查延迟中", Math.Round(PingRecord.Average) & "ms"))
        End Function
        Public Function ToListItem() As MyListItem
            Dim Item As New MyListItem With {
                .Title = DisplayName, .Height = 42, .Tag = Me, .Type = MyListItem.CheckType.None,
                .PaddingRight = 60,
                .Logo = "pack://application:,,,/images/Blocks/Grass.png"}
            '绑定图标按钮
            Dim BtnRefresh As New MyIconButton With {.Logo = Logo.IconButtonRefresh, .LogoScale = 0.85, .ToolTip = "刷新", .Tag = Me}
            AddHandler BtnRefresh.Click, AddressOf BtnListRefresh_Click
            ToolTipService.SetPlacement(BtnRefresh, Primitives.PlacementMode.Bottom)
            ToolTipService.SetHorizontalOffset(BtnRefresh, -10)
            ToolTipService.SetVerticalOffset(BtnRefresh, 5)
            ToolTipService.SetShowDuration(BtnRefresh, 2333333)
            ToolTipService.SetInitialShowDelay(BtnRefresh, 200)
            Dim BtnClose As New MyIconButton With {.Logo = Logo.IconButtonCross, .LogoScale = 0.85, .ToolTip = "断开", .Tag = Me}
            AddHandler BtnClose.Click, AddressOf BtnListDisconnect_Click
            ToolTipService.SetPlacement(BtnClose, Primitives.PlacementMode.Bottom)
            ToolTipService.SetHorizontalOffset(BtnClose, -10)
            ToolTipService.SetVerticalOffset(BtnClose, 5)
            ToolTipService.SetShowDuration(BtnClose, 2333333)
            ToolTipService.SetInitialShowDelay(BtnClose, 200)
            Item.Buttons = {BtnRefresh, BtnClose}
            '刷新并返回
            RefreshUi(Item)
            Return Item
        End Function
        Public Sub RefreshUi(RelatedListItem As MyListItem)
            RelatedListItem.Title = DisplayName
            RelatedListItem.Info = GetDescription()
            RelatedListItem.Buttons(0).Visibility = If(Progress = 1, Visibility.Visible, Visibility.Collapsed)
        End Sub

        '释放
        Protected Overrides Sub Dispose(IsDisposing As Boolean)
            Log("[IOI] 用户资源释放（IOI, " & DisplayName & "）")
            If RelativeThread IsNot Nothing AndAlso RelativeThread.IsAlive Then RelativeThread.Interrupt()
            UserList.Remove(Id)
            MyBase.Dispose(IsDisposing)
        End Sub
    End Class

    '房间对象
    Private Shared RoomListForMe As New List(Of RoomEntry)

    Private Function GetRoomList() As List(Of RoomEntry)
        Dim RoomList As New List(Of RoomEntry)(RoomListForMe)
        For i = 0 To UserList.Count - 1
            If i > UserList.Count - 1 Then Exit For
            RoomList.AddRange(UserList.Values(i).Rooms)
        Next
        Return RoomList
    End Function
    Public Class RoomEntry

        '基础数据
        Public Port As Integer
        Public DisplayName As String
        Public User As LinkUserIoi = Nothing '若 IsOwner = True，则此项为 Nothing
        Public IsOwner As Boolean
        Public ReadOnly Property Ip As String
            Get
                If IsOwner Then
                    Return "localhost:" & Port
                Else
                    Return User.Ports(Port) & ":" & Port
                End If
            End Get
        End Property

        '类型转换
        Public Sub New(Port As Integer, DisplayName As String, Optional User As LinkUserIoi = Nothing)
            Me.IsOwner = User Is Nothing
            Me.User = User
            Me.DisplayName = DisplayName
            Me.Port = Port
        End Sub
        Public Overrides Function ToString() As String
            Return DisplayName & " - " & Port & " - " & IsOwner
        End Function
        Public Shared Widening Operator CType(Room As RoomEntry) As String
            Return Room.ToString
        End Operator
        Public Shared Function SelectPort(Room As RoomEntry) As Integer
            Return Room.Port
        End Function

        'UI
        Public Function GetDescription() As String
            If IsOwner Then
                Return "由我创建，端口 " & Port
            Else
                Return "由 " & User.DisplayName & " 创建，端口 " & Port
            End If
        End Function
        Public Function ToListItem() As MyListItem
            Dim Item As New MyListItem With {
                .Title = DisplayName, .Height = 42, .Info = GetDescription(), .Tag = Me, .PaddingRight = If(IsOwner, 60, 0),
                .Type = If(IsOwner, MyListItem.CheckType.None, MyListItem.CheckType.Clickable),
                .Logo = "pack://application:,,,/images/Blocks/" & If(IsOwner, "GrassPath", "Grass") & ".png"}
            If IsOwner Then
                '绑定图标按钮
                Dim BtnEdit As New MyIconButton With {.Logo = Logo.IconButtonEdit, .LogoScale = 1, .ToolTip = "修改名称", .Tag = Me}
                AddHandler BtnEdit.Click, AddressOf BtnRoomEdit_Click
                ToolTipService.SetPlacement(BtnEdit, Primitives.PlacementMode.Bottom)
                ToolTipService.SetHorizontalOffset(BtnEdit, -22)
                ToolTipService.SetVerticalOffset(BtnEdit, 5)
                ToolTipService.SetShowDuration(BtnEdit, 2333333)
                ToolTipService.SetInitialShowDelay(BtnEdit, 200)
                Dim BtnClose As New MyIconButton With {.Logo = Logo.IconButtonCross, .LogoScale = 0.85, .ToolTip = "关闭", .Tag = Me}
                AddHandler BtnClose.Click, AddressOf BtnRoomClose_Click
                ToolTipService.SetPlacement(BtnClose, Primitives.PlacementMode.Bottom)
                ToolTipService.SetHorizontalOffset(BtnClose, -10)
                ToolTipService.SetVerticalOffset(BtnClose, 5)
                ToolTipService.SetShowDuration(BtnClose, 2333333)
                ToolTipService.SetInitialShowDelay(BtnClose, 200)
                Item.Buttons = {BtnEdit, BtnClose}
            Else
                '绑定点击事件
                AddHandler Item.Click, AddressOf BtnRoom_Click
            End If
            Return Item
        End Function
        Public Sub RefreshUi(RelatedListItem As MyListItem)
            RelatedListItem.Title = DisplayName
            RelatedListItem.Info = GetDescription()
        End Sub

    End Class

#End Region

    '正向与反向连接
    Public Shared Sub BtnLeftCreate_Click() Handles BtnLeftCreate.Click
        ''获取信息
        'Dim Code As String = MyMsgBoxInput("", New ObjectModel.Collection(Of Validate) From {New ValidateLength(9, 99999)}, "", "输入对方的联机码", "确定", "取消")
        'If Code Is Nothing Then Exit Sub
        ''检查
        'If Code.StartsWith("P") AndAlso Code.Length < 48 Then
        '    Hint("你输入的可能是 HiPer 的联机码，请在左侧的联机方式中选择 HiPer！", HintType.Critical) : Exit Sub
        'End If
        'Dim Id As String, DisplayName As String
        'Try '解密失败检查
        '    Id = "12D3" & Code.Substring(0, 48)
        '    DisplayName = SecretDecrypt(Code.Substring(48))
        'Catch
        '    Hint("你输入的联机码有误！", HintType.Critical)
        '    Exit Sub
        'End Try
        'If Id = IoiId Then '自我连接检查
        '    Hint("我连我自己？搁这卡 Bug 呢？", HintType.Critical)
        '    Exit Sub
        'End If
        ''开始
        'Dim User As New LinkUserIoi(Id, DisplayName)
        'User.RelativeThread = RunInNewThread(Sub()
        '                                         Dim WaitCount As Integer = 0
        '                                         Try
        '                                             '加入列表
        '                                             If UserList.ContainsKey(Id) Then
        '                                                 Hint(UserList(Id).DisplayName & " 已在列表中，无需再次连接！")
        '                                                 Exit Sub
        '                                             Else
        '                                                 UserList.Add(Id, User)
        '                                             End If
        '                                             '发送 portsub 请求（0% -> 80%）
        '                                             SendPortsubRequest(User)
        '                                             '构建 Socket（81% -> 82%）
        '                                             Dim ClientSocket As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) With {.ReceiveTimeout = 1000000000, .SendTimeout = 1000000000}
        '                                             ClientSocket.Connect(New IPEndPoint(IPAddress.Parse(User.Ports(55557)), 55557))
        '                                             User.BindSocket(ClientSocket)
        '                                             User.Progress = 0.82
        '                                             '发送 connect 请求（83% -> 85%）
        '                                             SendConnectRequest(User)
        '                                             User.Progress = 0.85
        '                                             '等待对方向自己请求
        '                                             Log("[IOI] 加入成功，等待反向请求")
        '                                             Do While User.Progress < 0.9999
        '                                                 User.Progress += 0.0002
        '                                                 If User.Progress > 0.98 AndAlso User.Progress < 0.9999 Then Throw New Exception("对方未回应连接请求！")
        '                                                 Thread.Sleep(100)
        '                                             Loop
        '                                             Hint("已连接到 " & User.DisplayName & "！", HintType.Finish)
        '                                         Catch ex As ThreadInterruptedException
        '                                             Log("[IOI] 已中断主动发起的连接（" & User.DisplayName & "）")
        '                                         Catch ex As InvalidOperationException
        '                                             'API 返回的错误
        '                                             NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
        '                                             User.Dispose()
        '                                             If InitLoader.State = LoadState.Finished Then Log("与 " & DisplayName & " 建立连接失败：" & ex.Message, LogLevel.Msgbox)
        '                                         Catch ex As Exception
        '                                             '常规错误
        '                                             NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
        '                                             User.Dispose()
        '                                             If ex.InnerException IsNot Nothing AndAlso TypeOf ex.InnerException Is ThreadInterruptedException Then Exit Sub
        '                                             If InitLoader.State = LoadState.Finished Then Log(ex, "与 " & DisplayName & " 建立连接失败", LogLevel.Msgbox)
        '                                         End Try
        '                                     End Sub, "Link Create " & DisplayName)
    End Sub
    Private Shared Sub SendPortsubBack(User As LinkUserIoi, TargetVersion As Integer)
        Try
            SendPortsubRequest(User)
            User.Progress = 0.9
            If TargetVersion > RequestVersion Then
                SendDisconnectRequest(User, "无法连接到 " & GetPlayerName() & "：对方的 PCL 版本过低！", True)
                Throw New InvalidOperationException("你的 PCL 版本过低！")
            ElseIf TargetVersion < RequestVersion Then
                SendDisconnectRequest(User, "无法连接到 " & GetPlayerName() & "：你的 PCL 版本过低！", True)
                Throw New InvalidOperationException("对方的 PCL 版本过低！")
            Else
                SendUpdateRequest(User, 1)
                User.Progress = 1
                Hint(User.DisplayName & " 已与你建立连接！")
            End If
        Catch ex As ThreadInterruptedException
            Log("[IOI] 已中断被动建立的连接（" & User.DisplayName & "）")
        Catch ex As InvalidOperationException
            'API 返回的错误
            NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
            User.Dispose()
            If InitLoader.State = LoadState.Finished Then Log("与 " & User.DisplayName & " 建立连接失败：" & ex.Message, LogLevel.Hint)
        Catch ex As Exception
            '常规错误
            NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
            User.Dispose()
            If InitLoader.State = LoadState.Finished Then Log(ex, "与 " & User.DisplayName & " 建立连接失败", LogLevel.Hint)
        End Try
    End Sub

    '创建房间
    Private Sub LinkCreate() Handles BtnCreate.Click
        'If MyMsgBox("请先进入 MC 并暂停游戏，在暂停页面选择对局域网开放，然后在下一个窗口输入 MC 显示的端口号。" & vbCrLf & "若使用服务端开服，则直接在下一个窗口输入服务器配置中的端口号即可。", "提示", "继续", "取消") = 2 Then Exit Sub
        ''获取端口号
        'Dim Port As String = MyMsgBoxInput("", New ObjectModel.Collection(Of Validate) From {
        '                                           New ValidateInteger(0, 65535),
        '                                           New ValidateExceptSame({"55555", "55557"}, "端口不能为 %！"),
        '                                           New ValidateExceptSame(RoomListForMe.Select(AddressOf RoomEntry.SelectPort), "端口 % 已创建过房间，请在删除该房间后继续！")
        '                                       }, "", "输入端口号", "确定", "取消")
        'If Port Is Nothing Then Exit Sub
        ''获取显示名称
        'Dim DisplayName As String = MyMsgBoxInput(GetPlayerName() & " 的房间 - " & Port, New ObjectModel.Collection(Of Validate) From {
        '                                           New ValidateNullOrWhiteSpace(), New ValidateLength(1, 40), New ValidateFilter()
        '                                       }, "", "输入房间名（建议包含游戏版本等信息）", "确定", "取消")
        'If DisplayName Is Nothing Then Exit Sub
        'DisplayName = DisplayName.Trim
        ''开始
        'RunInThread(Sub()
        '                Try
        '                    '请求
        '                    Dim Result As JObject = GetJson(NetRequestOnce("http://127.0.0.1:55555/api/port?proto=tcp&port=" & Port & "&password=" & IoiPassword, "PUT", "", "", 100000))
        '                    If Result("msg") IsNot Nothing Then Throw New InvalidOperationException(Result("msg").ToString)
        '                    '成功
        '                    RoomListForMe.Add(New RoomEntry(Port, DisplayName))
        '                    Hint("房间 " & DisplayName & " 已创建！", HintType.Finish)
        '                    SendUpdateRequestToAllUsers()
        '                Catch ex As InvalidOperationException
        '                    'API 返回的错误
        '                    Log("创建房间失败：" & ex.Message, LogLevel.Msgbox)
        '                Catch ex As Exception
        '                    '常规错误
        '                    Log(ex, "创建房间失败", LogLevel.Msgbox)
        '                End Try
        '            End Sub)
    End Sub
    Private Shared Sub SendUpdateRequestToAllUsers()
        For i = 0 To UserList.Count - 1
            If i > UserList.Count - 1 Then Exit For
            Dim User = UserList.Values(i)
            If User.Progress < 1 Then Continue For
            Try
                SendUpdateRequest(User, 1) '不需要使用多线程，发送实际会瞬间完成
            Catch ex As Exception
                Log(ex, "发送全局刷新请求失败（" & User.DisplayName & "）")
            End Try
        Next
    End Sub
    '修改房间名称
    Private Shared Sub BtnRoomEdit_Click(sender As MyIconButton, e As EventArgs)
        'Dim Room As RoomEntry = sender.Tag
        ''获取房间名
        'Dim DisplayName As String = MyMsgBoxInput(Room.DisplayName, New ObjectModel.Collection(Of Validate) From {
        '                                           New ValidateNullOrWhiteSpace(),
        '                                           New ValidateLength(1, 40)
        '                                       }, "", "输入房间名（建议包含游戏版本等信息）", "确定", "取消")
        'If DisplayName Is Nothing Then Exit Sub
        'DisplayName = DisplayName.Trim
        ''修改
        'Room.DisplayName = DisplayName
        'FrmLinkIoi.RefreshUi()
        'SendUpdateRequestToAllUsers()
    End Sub
    '加入房间
    Private Shared Sub BtnRoom_Click(sender As MyListItem, e As EventArgs)
        Dim Room As RoomEntry = sender.Tag
        If MyMsgBox("请在多人游戏页面点击直接连接，输入 " & Room.Ip & " 以进入服务器！", "加入房间", "复制地址", "确定") = 1 Then
            ClipboardSet(Room.Ip)
        End If
    End Sub
    '关闭房间
    Private Shared Sub BtnRoomClose_Click(sender As MyIconButton, e As EventArgs)
        Dim Room As RoomEntry = sender.Tag
        RunInThread(Sub()
                        Try
                            '远程移除
                            Dim Result As JObject = GetJson(NetRequestOnce("http://127.0.0.1:55555/api/port?proto=tcp&port=" & Room.Port & "&password=" & IoiPassword, "DELETE", "", "", 100000))
                            If Result("msg") IsNot Nothing Then Throw New InvalidOperationException(Result("msg").ToString)
                            '本地移除
                            RoomListForMe.Remove(Room)
                            '成功
                            RunInUi(Sub() FrmLinkIoi.RefreshUi())
                            SendUpdateRequestToAllUsers()
                        Catch ex As InvalidOperationException
                            'API 返回的错误
                            If InitLoader.State = LoadState.Finished Then Log("移除房间失败：" & ex.Message, LogLevel.Msgbox)
                        Catch ex As Exception
                            '常规错误
                            If InitLoader.State = LoadState.Finished Then Log(ex, "移除房间失败", LogLevel.Msgbox)
                        End Try
                    End Sub)
    End Sub

    '获取数据包
    Public Shared Sub ReceiveJson(JsonData As JObject, Optional NewSocket As Socket = Nothing)
        '获取数据
        Dim Id As String = JsonData("id"), Type As String = JsonData("type")
        Select Case Type
            Case "connect"
                Dim DisplayName As String = JsonData("name")
                Dim User As New LinkUserIoi(Id, DisplayName, NewSocket)
                '如果发生了双向连接
                If UserList.ContainsKey(Id) Then
                    If Id > IoiId Then
                        Log("[IOI] 双向连接，应当抛弃当前用户（" & DisplayName & "）")
                        For Each Pair In UserList(User.Id).Ports.ToList
                            User.Ports(Pair.Key) = Pair.Value
                        Next
                        UserList(Id).Dispose()
                    Else
                        '应当保留当前用户
                        Log("[IOI] 双向连接，应当保留当前用户（" & DisplayName & "）")
                        NewSocket.Dispose()
                        Exit Sub
                    End If
                End If
                '加入列表
                UserList.Add(Id, User)
                '返回请求
                User.RelativeThread = RunInNewThread(Sub()
                                                         SendPortsubBack(User, JsonData("version").ToObject(Of Integer))
                                                     End Sub, "Link Connect " & DisplayName)
                '更新时间
                User.LastReceive = Date.Now
            Case "update"
                If Not UserList.ContainsKey(Id) Then Throw New Exception("未在列表中的用户发送了更新请求：" & Id)
                Dim User = UserList(Id)
                '拉满进度（该请求也作为反向连接回应出现，用于向正向连接方传达连接已完成信号）
                User.Progress = 1
                '更新名称
                Dim DisplayName As String = JsonData("name")
                User.DisplayName = DisplayName
                '更新房间列表
                If JsonData("rooms") IsNot Nothing Then
                    User.Rooms = New List(Of RoomEntry)
                    For Each RoomObject In JsonData("rooms")
                        User.Rooms.Add(New RoomEntry(RoomObject("port"), RoomObject("name"), User))
                    Next
                End If
                '更新 Ping
                Dim Stage As Integer = JsonData("stage"), Unique As Long = JsonData("unique")
                If Stage > 1 Then
                    User.PingRecord.Enqueue((Date.Now - User.PingPending(Unique)).TotalMilliseconds / 2)
                    If User.PingRecord.Count > 5 Then User.PingRecord.Dequeue()
                    User.PingPending.Remove(Unique)
                End If
                '返回请求
                If Stage > 0 AndAlso Stage < 3 Then RunInNewThread(Sub()
                                                                       Try
                                                                           SendUpdateRequest(User, Stage + 1, Unique)
                                                                       Catch ex As Exception
                                                                           Log(ex, "发送回程请求失败")
                                                                       End Try
                                                                   End Sub, "Link Update " & DisplayName)
                '更新时间
                User.LastReceive = Date.Now
            Case "disconnect"
                '断开连接
                If Not UserList.ContainsKey(Id) Then Exit Sub
                UserRemove(UserList(Id), ShowLeaveMessage:=JsonData("message") Is Nothing)
                If JsonData("message") IsNot Nothing Then Hint(JsonData("message").ToString, If(JsonData("isError").ToObject(Of Boolean), HintType.Critical, HintType.Info))
            Case Else
                Throw New Exception("未知的操作种类：" & Type)
        End Select
    End Sub
    ''' <summary>
    ''' 从用户列表中移除一位用户。提示信息视作该用户主动离开。
    ''' </summary>
    Public Shared Sub UserRemove(User As LinkUserIoi, ShowLeaveMessage As Boolean)
        If Not UserList.ContainsKey(User.Id) Then Exit Sub
        If ShowLeaveMessage Then Hint(User.DisplayName & " 已离开！")
        NetRequestOnce("http://127.0.0.1:55555/api/link?id=" & User.Id & "&password=" & IoiPassword, "DELETE", "", "", 500)
        User.Dispose()
    End Sub

    '关闭联机模块按钮
    Private Shared Sub ModuleStateChanged(Loader As LoaderBase, NewState As LoadState, OldState As LoadState) Handles InitLoader.OnStateChangedUi
        If FrmLinkLeft IsNot Nothing Then CType(FrmLinkLeft.ItemIoi.Buttons(0), MyIconButton).Visibility = If(NewState = LoadState.Finished, Visibility.Visible, Visibility.Collapsed)
    End Sub
    Public Shared Sub ModuleStopManually()
        IoiStop(False)
        InitLoader.Error = New Exception("联机模块已关闭，点击以重新启动")
        InitLoader.State = LoadState.Failed
        IoiState = LoadState.Failed
    End Sub

End Class
