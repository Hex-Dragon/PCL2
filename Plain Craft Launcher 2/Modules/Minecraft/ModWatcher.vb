Public Module ModWatcher

    '对全体的监视
    Public McWatcherList As New List(Of Watcher)
    Private IsWatcherRunning As Boolean = False
    Public HasRunningMinecraft As Boolean = False
    Private Sub WatcherStateChanged()
        Dim IsRunning As Boolean = False
        Dim IsCrashed As Boolean = False
        For Each Watcher In McWatcherList
            If Watcher.State = Watcher.MinecraftState.Loading OrElse Watcher.State = Watcher.MinecraftState.Running Then
                IsRunning = True
                Exit For
            ElseIf Watcher.State = Watcher.MinecraftState.Crashed Then
                IsCrashed = True
            End If
        Next
        If IsWatcherRunning = IsRunning Then Exit Sub
        IsWatcherRunning = IsRunning
        If IsWatcherRunning Then
            MinecraftStart()
        Else
            MinecraftStop(IsCrashed)
        End If
    End Sub
    Private Sub MinecraftStart()
        McLaunchLog("[全局] 出现运行中的 Minecraft")
        HasRunningMinecraft = True
        FrmMain.BtnExtraShutdown.ShowRefresh()
    End Sub
    Private Sub MinecraftStop(HasMinecraftCrashed As Boolean)
        McLaunchLog("[全局] 已无运行中的 Minecraft")
        HasRunningMinecraft = False
        FrmMain.BtnExtraShutdown.ShowRefresh()
        '音乐播放
        If Setup.Get("UiMusicStop") Then
            RunInUi(Sub() If MusicResume() Then Log("[Music] 已根据设置，在结束后开始音乐播放"))
        ElseIf Setup.Get("UiMusicStart") Then
            RunInUi(Sub() If MusicPause() Then Log("[Music] 已根据设置，在结束后暂停音乐播放"))
        End If
        '启动器可见性
        Select Case Setup.Get("LaunchArgumentVisible")
            Case 2
                '直接关闭
                If HasMinecraftCrashed Then
                    RunInUi(Sub() FrmMain.Hidden = False)
                Else
                    RunInUi(Sub() FrmMain.EndProgram(False))
                End If
            Case 3
                '恢复
                RunInUi(Sub() FrmMain.Hidden = False)
        End Select
    End Sub

    '对单个进程的监视
    Public Class Watcher

        '初始化
        Public GameProcess As Process
        Public Version As McVersion
        Private WindowTitle As String = ""
        Private PID As Integer
        Public Loader As LoaderTask(Of Process, Integer)
        Public Sub New(Loader As LoaderTask(Of Process, Integer), Version As McVersion, WindowTitle As String)
            Me.Loader = Loader
            Me.Version = Version
            Me.WindowTitle = WindowTitle
            Me.PID = Loader.Input.Id
            WatcherLog("开始 Minecraft 日志监控")
            If Me.WindowTitle <> "" Then WatcherLog("要求窗口标题：" & WindowTitle)

            '更改列表
            Dim NewWatcherList As New List(Of Watcher)
            For Each Watch In McWatcherList
                If Watch.State = MinecraftState.Crashed OrElse Watch.State = MinecraftState.Ended Then Continue For
                NewWatcherList.Add(Watch)
            Next
            NewWatcherList.Add(Me)
            McWatcherList = NewWatcherList
            WatcherStateChanged()

            '初始化进程与日志读取
            Me.GameProcess = Loader.Input
            GameProcess.BeginOutputReadLine()
            GameProcess.BeginErrorReadLine()
            AddHandler GameProcess.OutputDataReceived, AddressOf LogReceived
            AddHandler GameProcess.ErrorDataReceived, AddressOf LogReceived

            '初始化时钟
            RunInNewThread(Sub()
                               Try
                                   MinecraftBeforeLaunch = GetAllMinecraftWindowHandle(False).Keys.ToList
                                   Do Until State = MinecraftState.Ended OrElse State = MinecraftState.Crashed OrElse Loader.State = LoadState.Aborted
                                       TimerWindow()
                                       TimerLog()
                                       If State = MinecraftState.Loading Then ProgressUpdate()
                                       '设置窗口标题
                                       For i = 1 To 3
                                           If IsWindowFinished AndAlso WindowTitle <> "" AndAlso State = MinecraftState.Running AndAlso Not GameProcess.HasExited Then
                                               Dim RealTitle As String = WindowTitle.Replace("{time}", Date.Now.ToShortTimeString)
                                               SetWindowText(WindowHandle, RealTitle.ToCharArray)
                                           End If
                                           Thread.Sleep(64)
                                       Next
                                   Loop
                                   WatcherLog("Minecraft 日志监控已退出")
                               Catch ex As Exception
                                   Log(ex, "Minecraft 日志监控主循环出错", LogLevel.Feedback)
                                   State = MinecraftState.Ended
                               End Try
                           End Sub, "Minecraft Watcher PID " & PID)
        End Sub

        '状态
        Private _State As MinecraftState = MinecraftState.Loading
        Public Property State As MinecraftState
            Get
                Return _State
            End Get
            Set(ByVal value As MinecraftState)
                If _State = value Then Exit Property
                _State = value
                WatcherStateChanged()
            End Set
        End Property
        Public Enum MinecraftState
            Loading
            Running
            Crashed
            Ended
        End Enum

        '日志
        Public WaitingLog As New List(Of String)(1000)
        Private ReadOnly WaitingLogLock As New Object
        Private Sub LogReceived(sender As Object, e As DataReceivedEventArgs)
            SyncLock WaitingLogLock
                WaitingLog.Add(e.Data)
            End SyncLock
        End Sub
        Private Sub TimerLog()
            Try
                '输出文本
                Dim Copyed As New List(Of String)
                SyncLock WaitingLogLock
                    If WaitingLog.Count = 0 Then Exit Sub
                    Copyed = WaitingLog
                    WaitingLog = New List(Of String)(1000)
                End SyncLock
                For Each Str As String In Copyed
                    GameLog(Str)
                Next
                '游戏退出检查
                If GameProcess.HasExited Then
                    WatcherLog("Minecraft 已退出，返回值：" & GameProcess.ExitCode)
                    'If Process.ExitCode = 1 Then
                    '    '返回值为 1，考虑是任务管理器结束
                    '    WatcherLog("Minecraft 返回值为 1，考虑为任务管理器结束") '并不，崩了照样是 1
                    '    State = MinecraftState.Ended
                    'Else
                    If State = MinecraftState.Loading Then
                        '窗口未出现
                        WatcherLog("Minecraft 尚未加载完成，可能已崩溃")
                        Crashed()
                    ElseIf GameProcess.ExitCode <> 0 AndAlso State = MinecraftState.Running AndAlso Version.ReleaseTime.Year >= 2012 Then
                        '返回值不为 0 且未结束
                        WatcherLog("Minecraft 返回值异常，可能已崩溃")
                        Crashed()
                    Else
                        '正常关闭
                        State = MinecraftState.Ended
                    End If
                End If
            Catch ex As Exception
                Log(ex, "输出 Minecraft 日志失败", LogLevel.Feedback)
            End Try
        End Sub
        Public LatestLog As New Queue(Of String)
        Private Sub GameLog(Text As String)
            '预处理
            If Text Is Nothing Then Exit Sub
            Text = Text.Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr, vbCrLf)
            '加入预存储
            LatestLog.Enqueue(Text)
            If LatestLog.Count >= 501 Then LatestLog.Dequeue()
            '进度处理
            If LogProgress < 1 Then
                WatcherLog("日志 1/5：已出现日志输出")
                LogProgress = 1
            End If '可能第一句就是后面需要判断的 Log（重现：启动 1.15.2 原版）
            If LogProgress < 2 AndAlso Text.Contains("Setting user:") Then
                WatcherLog("日志 2/5：游戏用户已设置") '仅确保支持 Minecraft 1.7+
                LogProgress = 2
            ElseIf LogProgress < 3 AndAlso Text.ToLower.Contains("lwjgl version") Then
                WatcherLog("日志 3/5：LWJGL 版本已确认")
                LogProgress = 3
            ElseIf LogProgress < 4 AndAlso (Text.Contains("OpenAL initialized") OrElse Text.Contains("Starting up SoundSystem")) Then
                WatcherLog("日志 4/5：OpenAL 已加载") '仅确保支持 Minecraft 1.7+
                LogProgress = 4
            ElseIf LogProgress < 5 AndAlso ((Text.Contains("Created") AndAlso Text.Contains("textures") AndAlso Text.Contains("-atlas")) OrElse Text.Contains("Found animation info")) Then
                WatcherLog("日志 5/5：材质已加载") '仅确保支持 Minecraft 1.7+
                LogProgress = 5
            End If
            '输出日志
            'Log(Text)
            '关闭与崩溃检测
            If Not Text.Contains("[CHAT]") Then
                If Text.Contains("Someone is closing me!") Then
                    WatcherLog("识别为关闭的 Log：" & Text)
                    State = MinecraftState.Ended
                ElseIf Text.Contains("Crash report saved to") OrElse Text.Contains("This crash report has been saved to:") Then
                    ' Text.Contains("Minecraft ran into a problem! Report saved to:") Then
                    'Minecraft 崩溃，忽略 VanillaFix
                    WatcherLog("识别为崩溃的 Log：" & Text)
                    Crashed()
                ElseIf Text.Contains("Could not save crash report to") Then
                    'Minecraft 崩溃，无法保存崩溃日志
                    WatcherLog("识别为崩溃的 Log：" & Text)
                    Crashed()
                ElseIf Text.Contains("/ERROR]: Unable to launch") OrElse Text.Contains("An exception was thrown, the game will display an error screen and halt.") Then
                    'Forge 崩溃
                    WatcherLog("识别为崩溃的 Log：" & Text)
                    Crashed()
                    'ElseIf Text.Contains("Shutdown failure!") Then
                    '    'Minecraft 强行崩溃，由于点 X 强行关闭也会触发这句话，所以不可用
                    '    Crashed(Nothing)
                End If
            End If
        End Sub
        Private Sub WatcherLog(Text As String)
            McLaunchLog("[" & PID & "] " & Text)
        End Sub

        '进度更新
        Private LogProgress As Integer = 0
        Private Sub ProgressUpdate()
            Dim CurrentProgress As Double
            If IsWindowAppeared OrElse LogProgress = 5 Then
                CurrentProgress = 0.95
                WatcherLog("Minecraft 加载已完成")
                State = MinecraftState.Running
            Else
                CurrentProgress = Math.Min(LogProgress, 3) / 3 * 0.9
            End If
            Loader.Progress = CurrentProgress
        End Sub

        '窗口检查
        Private IsWindowAppeared As Boolean = False
        Private IsWindowFinished As Boolean = False
        Private WindowHandle As IntPtr
        Private MinecraftBeforeLaunch As List(Of IntPtr)
        Private Sub TimerWindow()
            Try
                If GameProcess.HasExited Then Exit Sub
                If Not IsWindowFinished Then
                    '获取全部窗口，检查是否有新增的
                    Dim MinecraftWindows = GetAllMinecraftWindowHandle(True)
                    For Each Window In MinecraftBeforeLaunch
                        If MinecraftWindows.ContainsKey(Window) Then MinecraftWindows.Remove(Window)
                    Next
                    If MinecraftWindows.Count = 0 Then Exit Sub
                    '已找到窗口
                    If MinecraftWindows.Values(0) Then
                        '已找到 Minecraft 窗口
                        WindowHandle = MinecraftWindows.Keys(0)
                        WatcherLog("Minecraft 窗口已加载：" & WindowHandle.ToInt64)
                        IsWindowFinished = True
                        '最大化
                        Thread.Sleep(3000) '1.7.10 LiteLoader 不延迟会显示错误
                        If Setup.Get("LaunchArgumentWindowType") = 4 Then ShowWindow(WindowHandle, 3)
                    ElseIf Not IsWindowAppeared Then
                        '已找到 FML 窗口
                        WatcherLog("FML 窗口已加载：" & MinecraftWindows.Keys(0).ToInt64)
                    End If
                    IsWindowAppeared = True
                End If
            Catch ex As Exception
                Log(ex, "检查 Minecraft 窗口失败", LogLevel.Feedback)
            End Try
        End Sub
        ''' <summary>
        ''' 获取所有 Minecraft 窗口句柄。
        ''' Value: 是否为 Minecraft 窗口，而不是 FML 窗口。
        ''' </summary>
        ''' <param name="CanUseFml">是否包含 FML 窗口</param>
        Private Function GetAllMinecraftWindowHandle(CanUseFml As Boolean) As Dictionary(Of IntPtr, Boolean)
            Dim AllList As New Dictionary(Of IntPtr, Boolean)
            EnumWindows(Sub(hwnd As IntPtr, lParam As Integer)
                            '检查类名
                            Dim str As New StringBuilder(512)
                            GetClassName(hwnd, str, str.Capacity)
                            Dim ClassName As String = str.ToString
                            If Not (ClassName = "GLFW30" OrElse ClassName = "LWJGL" OrElse ClassName = "SunAwtFrame") Then Exit Sub
                            '检查窗口标题名
                            str = New StringBuilder(512)
                            GetWindowText(hwnd, str, str.Capacity)
                            Dim WindowText As String = str.ToString
                            If CanUseFml AndAlso WindowText.StartsWith("FML") Then
                                AllList.Add(hwnd, False)
                            ElseIf Not (WindowText.StartsWith("GLFW") OrElse WindowText = "PopupMessageWindow") Then
                                '有的 Mod 可以修改窗口标题，不能检测是否为 Minecraft 打头
                                '部分版本会搞个 GLFW message window 出来所以得反选
                                AllList.Add(hwnd, True)
                            End If
                            'If WindowText.StartsWith("Minecraft") Then
                            '    AllList.Add(hwnd, True)
                            'ElseIf CanUseFml AndAlso WindowText.StartsWith("FML") Then
                            '    AllList.Add(hwnd, False)
                            'End If
                        End Sub, 0)
            Return AllList
        End Function
        Private Delegate Sub EnumWindowsSub(hwnd As IntPtr, lParam As Integer)
        Private Declare Function EnumWindows Lib "user32" (hWnd As EnumWindowsSub, lParam As Integer) As Boolean
        Private Declare Function GetClassName Lib "user32" Alias "GetClassNameA" (hWnd As Integer, str As StringBuilder, maxCount As Integer) As Integer
        Private Declare Function GetWindowText Lib "user32" Alias "GetWindowTextA" (hWnd As Integer, str As StringBuilder, maxCount As Integer) As Integer
        Private Declare Function SetWindowText Lib "user32" Alias "SetWindowTextA" (hWnd As Integer, str As String) As Boolean
        Private Declare Function ShowWindow Lib "user32" (hWnd As IntPtr, cmdWindow As UInteger) As Boolean

        '崩溃处理
        Private Sub Crashed()
            If State = MinecraftState.Crashed OrElse State = MinecraftState.Ended Then Exit Sub
            State = MinecraftState.Crashed
            '崩溃分析
            WatcherLog("Minecraft 已崩溃，将在 2 秒后开始崩溃分析")
            Hint("检测到 Minecraft 出现错误，错误分析已开始……")
            RunInNewThread(Sub()
                               Try
                                   Thread.Sleep(2000)
                                   WatcherLog("崩溃分析开始")
                                   Dim Analyzer As New CrashAnalyzer(PID)
                                   Analyzer.Collect(Version.PathIndie, LatestLog.ToList)
                                   Analyzer.Prepare()
                                   Analyzer.Analyze(Version)
                                   Analyzer.Output(False, New List(Of String) From
                                                           {Version.Path & Version.Name & ".json",
                                                            Path & "PCL\Log1.txt",
                                                            Path & "PCL\LatestLaunch.bat"})
                               Catch ex As Exception
                                   Log(ex, "崩溃分析失败", LogLevel.Feedback)
                               End Try
                           End Sub, "Crash Analyzer")
        End Sub

        '强制关闭
        Public Sub Kill()
            State = MinecraftState.Ended
            WatcherLog("尝试强制结束 Minecraft 进程")
            Try
                If Not GameProcess.HasExited Then GameProcess.Kill()
                WatcherLog("已强制结束 Minecraft 进程")
            Catch ex As Exception
                Log(ex, "强制结束 Minecraft 进程失败", LogLevel.Hint)
            End Try
        End Sub

    End Class

End Module
