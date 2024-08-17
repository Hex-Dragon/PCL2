Imports System.Net
Imports System.Reflection
Imports System.Windows.Threading

Public Class Application

#If DEBUG Then
    ''' <summary>
    ''' 用于开始程序时的一些测试。
    ''' </summary>
    Private Sub Test()
        Try
            ModDevelop.Start()
        Catch ex As Exception
            Log(ex, "开发者模式测试出错", LogLevel.Msgbox)
        End Try
    End Sub
#End If

    '开始
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Try
            SecretOnApplicationStart()
            '检查参数调用
            If e.Args.Length > 0 Then
                If e.Args(0) = "--update" Then
                    '自动更新
                    UpdateReplace(e.Args(1), e.Args(2).Trim(""""), e.Args(3).Trim(""""), e.Args(4))
                    Environment.Exit(Result.Cancel)
                ElseIf e.Args(0).StartsWithF("--memory") Then
                    '内存优化
                    Dim Ram = My.Computer.Info.AvailablePhysicalMemory
                    Try
                        PageOtherTest.MemoryOptimizeInternal()
                    Catch ex As Exception
                        MsgBox(ex.Message, MsgBoxStyle.Critical, "内存优化失败")
                        Environment.Exit(-1)
                    End Try
                    If My.Computer.Info.AvailablePhysicalMemory < Ram Then '避免 ULong 相减出现负数
                        Environment.Exit(0)
                    Else
                        Environment.Exit((My.Computer.Info.AvailablePhysicalMemory - Ram) / 1024) '返回清理的内存量（K）
                    End If
#If DEBUG Then
                    '制作更新包
                ElseIf e.Args(0) = "--edit1" Then
                    ExeEdit(e.Args(1), True)
                    Environment.Exit(Result.Cancel)
                ElseIf e.Args(0) = "--edit2" Then
                    ExeEdit(e.Args(1), False)
                    Environment.Exit(Result.Cancel)
#End If
                End If
            End If
            '初始化文件结构
            Directory.CreateDirectory(Path & "PCL\Pictures")
            Directory.CreateDirectory(Path & "PCL\Musics")
            Try
                Directory.CreateDirectory(PathTemp)
                If Not CheckPermission(PathTemp) Then Throw New Exception("PCL 没有对 " & PathTemp & " 的访问权限")
            Catch ex As Exception
                If PathTemp = IO.Path.GetTempPath() & "PCL\" Then
                    MyMsgBox("PCL 无法访问缓存文件夹，可能导致程序出错或无法正常使用！" & vbCrLf & "错误原因：" & GetExceptionDetail(ex), "缓存文件夹不可用")
                Else
                    MyMsgBox("手动设置的缓存文件夹不可用，PCL 将使用默认缓存文件夹。" & vbCrLf & "错误原因：" & GetExceptionDetail(ex), "缓存文件夹不可用")
                    Setup.Set("SystemSystemCache", "")
                    PathTemp = IO.Path.GetTempPath() & "PCL\"
                End If
            End Try
            Directory.CreateDirectory(PathTemp & "Cache")
            Directory.CreateDirectory(PathTemp & "Download")
            Directory.CreateDirectory(PathAppdata)
            '检测单例
#If Not DEBUG Then
            Dim WindowHwnd As IntPtr = FindWindow(Nothing, "Plain Craft Launcher　")
            If WindowHwnd = IntPtr.Zero Then FindWindow(Nothing, "Plain Craft Launcher 2　")
            If WindowHwnd <> IntPtr.Zero Then
                '将已有的 PCL 窗口拖出来
                ShowWindowToTop(WindowHwnd)
                '播放提示音并退出
                Beep()
                Environment.[Exit](Result.Cancel)
            End If
#End If
            '设置 ToolTipService 默认值
            ToolTipService.InitialShowDelayProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(300))
            ToolTipService.BetweenShowDelayProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(400))
            ToolTipService.ShowDurationProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(9999999))
            ToolTipService.PlacementProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(Primitives.PlacementMode.Bottom))
            ToolTipService.HorizontalOffsetProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(8.0))
            ToolTipService.VerticalOffsetProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(4.0))
            '设置初始窗口
            If Setup.Get("UiLauncherLogo") Then
                FrmStart = New SplashScreen("Images\icon.ico")
                FrmStart.Show(False, True)
            End If
            '动态 DLL 调用
            AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf AssemblyResolve
            '日志初始化
            LogStart()
            '添加日志
            Log($"[Start] 程序版本：{VersionDisplayName} ({VersionCode})")
            Log($"[Start] 识别码：{UniqueAddress}{If(ThemeCheckOne(9), "，已解锁反馈主题", "")}")
            Log($"[Start] 程序路径：{PathWithName}")
            Log($"[Start] 系统编码：{Encoding.Default} ({Encoding.Default.CodePage}, GBK={IsGBKEncoding})")
            Log($"[Start] 管理员权限：{IsAdmin()}")
            '检测压缩包运行
            If Path.Contains(IO.Path.GetTempPath()) OrElse Path.Contains("AppData\Local\Temp\") Then
                MyMsgBox("PCL 正在临时文件夹运行，设置、游戏存档等很可能无法保存，且部分功能会无法使用或出错。" & vbCrLf & "请将 PCL 从压缩文件中解压，或是更换文件夹后再继续使用！", "环境警告", "我知道了", IsWarn:=True)
            End If
            '设置初始化
            Setup.Load("SystemDebugMode")
            Setup.Load("SystemDebugAnim")
            Setup.Load("ToolDownloadThread")
            Setup.Load("ToolDownloadCert")
            '网络配置初始化
            ServicePointManager.Expect100Continue = True
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 Or SecurityProtocolType.Tls Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12
            ServicePointManager.DefaultConnectionLimit = 1024
            '计时
            Log("[Start] 第一阶段加载用时：" & GetTimeTick() - ApplicationStartTick & " ms")
            ApplicationStartTick = GetTimeTick()
            '执行测试
#If DEBUG Then
            Test()
#End If
            AniControlEnabled += 1
        Catch ex As Exception
            Dim FilePath As String = Nothing
            Try
                FilePath = PathWithName
            Catch
            End Try
            MsgBox(GetExceptionDetail(ex, True) & vbCrLf & "PCL 所在路径：" & If(String.IsNullOrEmpty(FilePath), "获取失败", FilePath), MsgBoxStyle.Critical, "PCL 初始化错误")
            FormMain.EndProgramForce(Result.Exception)
        End Try
    End Sub

    '结束
    Private Sub Application_SessionEnding(sender As Object, e As SessionEndingCancelEventArgs) Handles Me.SessionEnding
        FrmMain.EndProgram(False)
    End Sub

    '异常
    Private IsCritErrored As Boolean = False
    Private Sub Application_DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        On Error Resume Next
        e.Handled = True
        If IsProgramEnded Then Exit Sub
        If IsCritErrored Then
            '在汇报错误后继续引发错误，知道这次压不住了
            FormMain.EndProgramForce(Result.Exception)
            Exit Sub
        End If
        IsCritErrored = True
        Dim ExceptionString As String = GetExceptionDetail(e.Exception, True)
        If ExceptionString.Contains("System.Windows.Threading.Dispatcher.Invoke") OrElse
           ExceptionString.Contains("MS.Internal.AppModel.ITaskbarList.HrInit") OrElse
           ExceptionString.Contains(".NET Framework") OrElse ' “自动错误判断” 的结果分析
           ExceptionString.Contains("未能加载文件或程序集") Then
            OpenWebsite("https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/thank-you/net462-offline-installer")
            MsgBox("你的 .NET Framework 版本过低或损坏，请在打开的网页中重新下载并安装 .NET Framework 4.6.2 后重试！", MsgBoxStyle.Information, "运行环境错误")
            FormMain.EndProgramForce(Result.Cancel)
        Else
            FeedbackInfo()
            Log(e.Exception, "程序出现未知错误", LogLevel.Assert, "锟斤拷烫烫烫")
        End If
    End Sub

    '动态 DLL 调用
    Private Shared AssemblyNAudio As Assembly
    Private Shared AssemblyJson As Assembly
    Private Shared AssemblyDialog As Assembly
    Private Shared ReadOnly AssemblyNAudioLock As New Object
    Private Shared ReadOnly AssemblyJsonLock As New Object
    Private Shared ReadOnly AssemblyDialogLock As New Object
    Public Shared Function AssemblyResolve(sender As Object, args As ResolveEventArgs) As Assembly
        If args.Name.StartsWithF("NAudio") Then
            SyncLock AssemblyNAudioLock
                If AssemblyNAudio Is Nothing Then
                    Log("[Start] 加载 DLL：NAudio")
                    AssemblyNAudio = Assembly.Load(GetResources("NAudio"))
                End If
                Return AssemblyNAudio
            End SyncLock
        ElseIf args.Name.StartsWithF("Newtonsoft.Json") Then
            SyncLock AssemblyJsonLock
                If AssemblyJson Is Nothing Then
                    Log("[Start] 加载 DLL：Json")
                    AssemblyJson = Assembly.Load(GetResources("Json"))
                End If
                Return AssemblyJson
            End SyncLock
        ElseIf args.Name.StartsWithF("Ookii.Dialogs.Wpf") Then
            SyncLock AssemblyDialogLock
                If AssemblyDialog Is Nothing Then
                    Log("[Start] 加载 DLL：Dialogs")
                    AssemblyDialog = Assembly.Load(GetResources("Dialogs"))
                End If
                Return AssemblyDialog
            End SyncLock
        Else
            Return Nothing
        End If
    End Function

    '切换窗口

    '控件模板事件
    Private Sub MyIconButton_Click(sender As Object, e As EventArgs)
        Select Case Setup.Get("LoginType")
            Case McLoginType.Ms
                '微软
                Dim MsJson As JObject = GetJson(Setup.Get("LoginMsJson"))
                MsJson.Remove(sender.Tag)
                Setup.Set("LoginMsJson", MsJson.ToString(Newtonsoft.Json.Formatting.None))
                If FrmLoginMs.ComboAccounts.SelectedItem Is sender.Parent Then FrmLoginMs.ComboAccounts.SelectedIndex = 0
                FrmLoginMs.ComboAccounts.Items.Remove(sender.Parent)
            Case McLoginType.Legacy
                '离线
                Dim Names As New List(Of String)
                Names.AddRange(Setup.Get("LoginLegacyName").ToString.Split("¨"))
                Names.Remove(sender.Tag)
                Setup.Set("LoginLegacyName", Join(Names, "¨"))
                FrmLoginLegacy.ComboName.ItemsSource = Names
                FrmLoginLegacy.ComboName.Text = If(Names.Any, Names(0), "")
            Case Else
                '第三方
                Dim Token As String = GetStringFromEnum(Setup.Get("LoginType"))
                Dim Dict As New Dictionary(Of String, String)
                Dim Names As New List(Of String)
                Dim Passs As New List(Of String)
                If Not Setup.Get("Login" & Token & "Email") = "" Then Names.AddRange(Setup.Get("Login" & Token & "Email").ToString.Split("¨"))
                If Not Setup.Get("Login" & Token & "Pass") = "" Then Passs.AddRange(Setup.Get("Login" & Token & "Pass").ToString.Split("¨"))
                For i = 0 To Names.Count - 1
                    Dict.Add(Names(i), Passs(i))
                Next
                Dict.Remove(sender.Tag)
                Setup.Set("Login" & Token & "Email", Join(Dict.Keys, "¨"))
                Setup.Set("Login" & Token & "Pass", Join(Dict.Values, "¨"))
                Select Case Token
                    Case "Nide"
                        FrmLoginNide.ComboName.ItemsSource = Dict.Keys
                        FrmLoginNide.ComboName.Text = If(Dict.Keys.Any, Dict.Keys(0), "")
                        FrmLoginNide.TextPass.Password = If(Dict.Values.Any, Dict.Values(0), "")
                    Case "Auth"
                        FrmLoginAuth.ComboName.ItemsSource = Dict.Keys
                        FrmLoginAuth.ComboName.Text = If(Dict.Keys.Any, Dict.Keys(0), "")
                        FrmLoginAuth.TextPass.Password = If(Dict.Values.Any, Dict.Values(0), "")
                End Select
        End Select
    End Sub

    Public Shared ShowingTooltips As New List(Of Border)
    Private Sub TooltipLoaded(sender As Border, e As EventArgs)
        ShowingTooltips.Add(sender)
    End Sub
    Private Sub TooltipUnloaded(sender As Border, e As RoutedEventArgs)
        ShowingTooltips.Remove(sender)
    End Sub

End Class
