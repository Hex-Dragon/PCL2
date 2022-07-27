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
            Log(ex, "开发者模式测试出错", LogLevel.Feedback)
        End Try
    End Sub

#End If

    '开始
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ApplicationStartTick = GetTimeTick()
        SecretOnApplicationStart()
        Try
            '检查参数调用
            If e.Args.Length > 0 Then
                If e.Args(0) = "--update" Then
                    '自动更新
                    UpdateReplace(e.Args(1), e.Args(2).Trim(""""), e.Args(3).Trim(""""), e.Args(4))
                    Environment.[Exit](Result.Cancel)
                    Exit Sub
                ElseIf e.Args(0) = "--link" Then
                    '稍作等待后切换到联机页面
                    Thread.Sleep(1000)
                    FormMain.IsLinkRestart = True
#If DEBUG Then
                ElseIf e.Args(0) = "--make" Then
                    '制作更新包
                    UpdateMake(e.Args(1))
                    Environment.[Exit](Result.Cancel)
                    Exit Sub
#End If
                End If
            End If
            '初始化文件结构
            Directory.CreateDirectory(Path & "PCL\Pictures")
            Directory.CreateDirectory(Path & "PCL\Musics")
            Try
                Directory.CreateDirectory(PathTemp)
                If Not CheckPermission(PathTemp) Then Throw New Exception("PCL2 没有对 " & PathTemp & " 的访问权限")
            Catch ex As Exception
                MyMsgBox("手动设置的缓存文件夹不可用，PCL2 将使用默认缓存文件夹。" & vbCrLf & "错误原因：" & GetString(ex, False), "缓存文件夹不可用")
                Setup.Set("SystemSystemCache", "")
                PathTemp = IO.Path.GetTempPath() & "PCL\"
            End Try
            Directory.CreateDirectory(PathTemp & "Cache")
            Directory.CreateDirectory(PathTemp & "Download")
            '检测单例
#If Not DEBUG Then
            Dim WindowHwnd As IntPtr = FindWindow(Nothing, "Plain Craft Launcher 2　")
            If WindowHwnd <> IntPtr.Zero Then
                '将已有的 PCL2 窗口拖出来
                ShowWindowToTop(WindowHwnd)
                '播放提示音并退出
                Beep()
                Environment.[Exit](Result.Cancel)
                Exit Sub
            End If
#End If
            '设置初始窗口
            If Setup.Get("UiLauncherLogo") AndAlso Not FormMain.IsLinkRestart Then
                FrmStart = New SplashScreen("Images\icon.ico")
                FrmStart.Show(False, True)
            End If
            '动态 DLL 调用
            AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf AssemblyResolve
            '日志初始化
            LogStart()
            '添加日志
            Log("[Start] 程序版本：" & VersionDisplayName & "（" & VersionCode & "）")
            Log("[Start] 识别码：" & UniqueAddress & If(ThemeCheckOne(9), "，已解锁反馈主题", ""))
            Log("[Start] 程序路径：" & PathWithName)
            '检测压缩包运行
            If Path.Contains(IO.Path.GetTempPath()) OrElse Path.Contains("AppData\Local\Temp\") Then
                MyMsgBox("PCL2 正在临时文件夹运行，设置、游戏存档等很可能无法保存，且部分功能会无法使用或出错。" & vbCrLf & "请将 PCL2 从压缩文件中解压，或是更换文件夹后再继续使用！", "环境警告", "我知道了", IsWarn:=True)
            End If
            '设置初始化
            Setup.Load("SystemDebugMode")
            Setup.Load("SystemDebugAnim")
            Setup.Load("ToolDownloadThread")
            '网络配置初始化
            ServicePointManager.Expect100Continue = True
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 Or SecurityProtocolType.Tls Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12
            ServicePointManager.DefaultConnectionLimit = 1024
            ServicePointManager.ServerCertificateValidationCallback = New Security.RemoteCertificateValidationCallback(Function() As Boolean
                                                                                                                           Return True
                                                                                                                       End Function)
            '计时
            Log("[Start] 第一阶段加载用时：" & GetTimeTick() - ApplicationStartTick & " ms")
            ApplicationStartTick = GetTimeTick()
            '执行测试
#If DEBUG Then
            Test()
#End If
            AniControlEnabled += 1
        Catch ex As Exception
            MsgBox(GetString(ex, False, True), MsgBoxStyle.Critical, "PCL2 初始化错误")
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
        Dim ExceptionString As String = GetString(e.Exception, False, True)
        If ExceptionString.Contains("System.Windows.Threading.Dispatcher.Invoke") OrElse
           ExceptionString.Contains("MS.Internal.AppModel.ITaskbarList.HrInit") OrElse
           ExceptionString.Contains(".Net Framework") OrElse ' “自动错误判断” 的结果分析
           ExceptionString.Contains("未能加载文件或程序集") Then
            OpenWebsite("https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/thank-you/net462-offline-installer")
            MsgBox("你的 .Net Framework 版本过低或损坏，请在打开的网页中重新下载并安装 .NET Framework 4.6.2 后重试！", MsgBoxStyle.Information, "运行环境错误")
            FormMain.EndProgramForce(Result.Cancel)
        Else
            FeedbackInfo()
            Log(e.Exception, "程序出现未知错误", LogLevel.Assert, "锟斤拷烫烫烫")
        End If
    End Sub

    '动态 DLL 调用
    Private Shared AssemblyStun As Assembly
    Private Shared AssemblyNAudio As Assembly
    Private Shared AssemblyJson As Assembly
    Private Shared AssemblyDialog As Assembly
    Private Shared ReadOnly AssemblyStunLock As New Object
    Private Shared ReadOnly AssemblyNAudioLock As New Object
    Private Shared ReadOnly AssemblyJsonLock As New Object
    Private Shared ReadOnly AssemblyDialogLock As New Object
    Public Shared Function AssemblyResolve(sender As Object, args As ResolveEventArgs) As Assembly
        If args.Name.StartsWith("NAudio") Then
            SyncLock AssemblyNAudioLock
                If AssemblyNAudio Is Nothing Then
                    Log("[Start] 加载 DLL：NAudio")
                    AssemblyNAudio = Assembly.Load(GetResources("NAudio"))
                End If
                Return AssemblyNAudio
            End SyncLock
        ElseIf args.Name.StartsWith("Newtonsoft.Json") Then
            SyncLock AssemblyJsonLock
                If AssemblyJson Is Nothing Then
                    Log("[Start] 加载 DLL：Json")
                    AssemblyJson = Assembly.Load(GetResources("Json"))
                End If
                Return AssemblyJson
            End SyncLock
        ElseIf args.Name.StartsWith("Ookii.Dialogs.Wpf") Then
            SyncLock AssemblyDialogLock
                If AssemblyDialog Is Nothing Then
                    Log("[Start] 加载 DLL：Dialogs")
                    AssemblyDialog = Assembly.Load(GetResources("Dialogs"))
                End If
                Return AssemblyDialog
            End SyncLock
        ElseIf args.Name.StartsWith("STUN") Then
            SyncLock AssemblyStunLock
                If AssemblyStun Is Nothing Then
                    Log("[Start] 加载 DLL：STUN")
                    AssemblyStun = Assembly.Load(GetResources("STUN"))
                End If
                Return AssemblyStun
            End SyncLock
        Else
            Return Nothing
        End If
    End Function

    '切换窗口


    '控件模板事件
    Private Sub MyIconButton_Click(sender As Object, e As EventArgs)
        If Setup.Get("LoginType") = McLoginType.Legacy Then
            '离线
            Dim Names As New List(Of String)
            Names.AddRange(Setup.Get("LoginLegacyName").ToString.Split("¨"))
            Names.Remove(sender.Tag)
            Setup.Set("LoginLegacyName", Join(Names, "¨"))
            FrmLoginLegacy.ComboName.ItemsSource = Names
            FrmLoginLegacy.ComboName.Text = If(Names.Count > 0, Names(0), "")
        Else
            '非离线
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
            Setup.Set("Login" & Token & "Email", Join(Dict.Keys.ToArray, "¨"))
            Setup.Set("Login" & Token & "Pass", Join(Dict.Values.ToArray, "¨"))
            Select Case Token
                Case "Mojang"
                    FrmLoginMojang.ComboName.ItemsSource = Dict.Keys
                    FrmLoginMojang.ComboName.Text = If(Dict.Keys.Count > 0, Dict.Keys(0), "")
                    FrmLoginMojang.TextPass.Password = If(Dict.Values.Count > 0, Dict.Values(0), "")
                Case "Nide"
                    FrmLoginNide.ComboName.ItemsSource = Dict.Keys
                    FrmLoginNide.ComboName.Text = If(Dict.Keys.Count > 0, Dict.Keys(0), "")
                    FrmLoginNide.TextPass.Password = If(Dict.Values.Count > 0, Dict.Values(0), "")
                Case "Auth"
                    FrmLoginAuth.ComboName.ItemsSource = Dict.Keys
                    FrmLoginAuth.ComboName.Text = If(Dict.Keys.Count > 0, Dict.Keys(0), "")
                    FrmLoginAuth.TextPass.Password = If(Dict.Values.Count > 0, Dict.Values(0), "")
                Case Else
                    DebugAssert(True)
                    Exit Sub
            End Select
        End If
    End Sub

End Class
