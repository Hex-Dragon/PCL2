Imports System.IO.Compression

Public Module ModLaunch

#Region "开始"

    ''' <summary>
    ''' 记录启动日志。
    ''' </summary>
    Public Sub McLaunchLog(Text As String)
        RunInUi(Sub()
                    FrmLaunchRight.LabLog.Text += vbCrLf & "[" & GetTimeNow() & "] " & Text
                End Sub)
        Log("[Launch] " & Text)
    End Sub

    '启动状态切换
    Public McLaunchLoader As New LoaderTask(Of String, Object)("Loader Launch", AddressOf McLaunchStart) With {.OnStateChanged = AddressOf McLaunchState, .ReloadTimeout = 1}
    Public McLaunchLoaderReal As LoaderCombo(Of Object)
    Public McLaunchProcess As Process
    Public McLaunchWatcher As Watcher
    Private Sub McLaunchState(Loader As LoaderTask(Of String, Object))
        Select Case McLaunchLoader.State
            Case LoadState.Finished, LoadState.Failed, LoadState.Waiting, LoadState.Aborted
                FrmLaunchLeft.PageChangeToLogin()
            Case LoadState.Loading
                '在预检测结束后再触发动画
                FrmLaunchRight.LabLog.Text = ""
        End Select
    End Sub
    Private Sub McLaunchStart(Loader As LoaderTask(Of String, Object))
        '开始动画
        RunInUiWait(AddressOf FrmLaunchLeft.PageChangeToLaunching)
        '预检测（预检测的错误将直接抛出）
        Try
            McLaunchPrecheck()
            McLaunchLog("预检测已通过")
        Catch ex As Exception
            Hint(ex.Message, HintType.Critical)
            Throw
        End Try
        '正式加载
        Try
            '构造主加载器
            Dim LaunchLoader As New LoaderCombo(Of Object)("Minecraft 启动", {
                New LoaderCombo(Of Integer)("Java 处理", {
                    New LoaderTask(Of Integer, List(Of NetFile))("Java 验证", AddressOf McLaunchJavaValidate) With {.ProgressWeight = 2}
                }) With {.ProgressWeight = 2, .Show = False, .Block = False},
                McLoginLoader,
                New LoaderCombo(Of String)("补全文件", DlClientFix(McVersionCurrent, False, AssetsIndexExistsBehaviour.DownloadInBackground, True)) With {.ProgressWeight = 15, .Show = False, .Block = True},
                New LoaderTask(Of Integer, String)("提供参数中的服务器 IP", Sub(InnerLoader As LoaderTask(Of Integer, String)) InnerLoader.Output = Loader.Input) With {.ProgressWeight = 0.01, .Show = False},
                New LoaderTask(Of String, List(Of McLibToken))("获取启动参数", AddressOf McLaunchArgumentMain) With {.ProgressWeight = 2},
                New LoaderTask(Of List(Of McLibToken), Integer)("解压文件", AddressOf McLaunchNatives) With {.ProgressWeight = 2},
                New LoaderTask(Of Integer, Integer)("预启动处理", AddressOf McLaunchPrerun) With {.ProgressWeight = 1},
                New LoaderTask(Of Integer, Process)("启动进程", AddressOf McLaunchRun) With {.ProgressWeight = 2},
                New LoaderTask(Of Process, Integer)("等待游戏窗口出现", AddressOf McLaunchWait) With {.ProgressWeight = 1},
                New LoaderTask(Of Integer, Integer)("结束处理", AddressOf McLaunchEnd) With {.ProgressWeight = 1}
            }) With {.Show = False}
            '等待加载器执行并更新 UI
            McLaunchLoaderReal = LaunchLoader
            LaunchLoader.Start()
            '任务栏进度条
            LoaderTaskbarAdd(LaunchLoader)
            Do While LaunchLoader.State = LoadState.Loading
                FrmLaunchLeft.Dispatcher.Invoke(AddressOf FrmLaunchLeft.LaunchingRefresh)
                Thread.Sleep(200)
            Loop
            FrmLaunchLeft.Dispatcher.Invoke(AddressOf FrmLaunchLeft.LaunchingRefresh)
            '成功与失败处理
            Select Case LaunchLoader.State
                Case LoadState.Finished
                    Hint(McVersionCurrent.Name & " 启动成功！", HintType.Finish)
                Case LoadState.Aborted
                    Hint("已取消启动！", HintType.Info)
                Case LoadState.Failed
                    Throw LaunchLoader.Error
                Case Else
                    Throw New Exception("错误的状态改变：" & GetStringFromEnum(CType(LaunchLoader.State, [Enum])))
            End Select
        Catch ex As Exception
            Dim CurrentEx = ex
NextInner:
            If CurrentEx.Message.StartsWith("$") Then
                '若有以 $ 开头的错误信息，则以此为准显示提示
                '若错误信息为 $$，则不提示
                If Not CurrentEx.Message = "$$" Then MyMsgBox(CurrentEx.Message.TrimStart("$"), "启动失败")
                Throw
            ElseIf CurrentEx.InnerException IsNot Nothing Then
                '检查下一级错误
                CurrentEx = CurrentEx.InnerException
                GoTo NextInner
            Else
                '没有特殊处理过的错误信息
                McLaunchLog("错误：" & GetString(ex, False))
                Log(ex, "Minecraft 启动失败", LogLevel.Msgbox, "启动失败")
                Throw
            End If
        End Try
    End Sub

#End Region

#Region "预检测"

    Private Sub McLaunchPrecheck()
        '检查路径
        If McVersionCurrent.PathIndie.Contains("!") OrElse McVersionCurrent.PathIndie.Contains(";") Then Throw New Exception("游戏路径中不可包含 ! 或 ;（" & McVersionCurrent.PathIndie & "）")
        If McVersionCurrent.Path.Contains("!") OrElse McVersionCurrent.Path.Contains(";") Then Throw New Exception("游戏路径中不可包含 ! 或 ;（" & McVersionCurrent.Path & "）")
        '检查输入信息
        Dim CheckResult As String = Nothing
        RunInUiWait(Sub() CheckResult = McLoginAble(McLoginInput))
        If CheckResult <> "" Then Throw New ArgumentException(CheckResult)
        '检查版本
        If McVersionCurrent Is Nothing Then Throw New Exception("未选择 Minecraft 版本！")
        McVersionCurrent.Load()
        If McVersionCurrent.State = McVersionState.Error Then Throw New Exception("Minecraft 存在问题：" & McVersionCurrent.Info)
        '求赞助
        If ThemeCheckGold() Then Exit Sub
        RunInNewThread(Sub()
                           Select Case Setup.Get("SystemLaunchCount")
                               Case 20, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000
                                   If MyMsgBox("PCL2 已经为你启动了 " & Setup.Get("SystemLaunchCount") & " 次游戏啦！" & vbCrLf &
                                               "如果觉得 PCL2 还算好用的话，也可以考虑小小地赞助一下作者呢 qwq……" & vbCrLf &
                                               "毕竟一个人开发也不容易（小声）……",
                                               "求赞助啦……", "这就赞助！", "但是我拒绝") = 1 Then
                                       OpenWebsite("https://afdian.net/@LTCat/plan")
                                   End If
                           End Select
                       End Sub, "Donate")
    End Sub

#End Region

#Region "主登录模块"

    '登录方式
    Public Enum McLoginType
        Legacy = 0
        Mojang = 1
        Nide = 2
        Auth = 3
        Ms = 5
    End Enum

    '各个登录方式的对应数据
    Public MustInherit Class McLoginData
        ''' <summary>
        ''' 登录方式。
        ''' </summary>
        Public Type As McLoginType
        Public Overrides Function Equals(obj As Object) As Boolean
            Return obj IsNot Nothing AndAlso obj.GetHashCode() = GetHashCode()
        End Function
    End Class
    Public Class McLoginServer
        Inherits McLoginData

        ''' <summary>
        ''' 登录用户名。
        ''' </summary>
        Public UserName As String
        ''' <summary>
        ''' 登录密码。
        ''' </summary>
        Public Password As String
        ''' <summary>
        ''' 登录服务器基础地址。
        ''' </summary>
        Public BaseUrl As String
        ''' <summary>
        ''' 登录所使用的标识符，如 “Mojang”、“Nide”，用于存储缓存等。
        ''' </summary>
        Public Token As String
        ''' <summary>
        ''' 登录方式的描述字符串，如 “正版”、“统一通行证”。
        ''' </summary>
        Public Description As String
        ''' <summary>
        ''' 是否在本次登录中强制要求玩家重新选择角色，目前仅对 Authlib-Injector 生效。
        ''' </summary>
        Public ForceReselectProfile As Boolean = False

        Public Sub New(Type As McLoginType)
            Me.Type = Type
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return GetHash(UserName & Password & BaseUrl & Token & Type) Mod Integer.MaxValue
        End Function

    End Class
    Public Class McLoginMs
        Inherits McLoginData

        ''' <summary>
        ''' 缓存的 OAuth Refresh Token。若没有则为空字符串。
        ''' </summary>
        Public OAuthRefreshToken As String = ""
        Public AccessToken As String = ""
        Public Uuid As String = ""
        Public UserName As String = ""

        Public Sub New()
            Type = McLoginType.Ms
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return GetHash(OAuthRefreshToken & AccessToken & Uuid & UserName) Mod Integer.MaxValue
        End Function
    End Class
    Public Class McLoginLegacy
        Inherits McLoginData
        ''' <summary>
        ''' 登录用户名。
        ''' </summary>
        Public UserName As String
        ''' <summary>
        ''' 皮肤种类。
        ''' </summary>
        Public SkinType As Integer
        ''' <summary>
        ''' 若采用正版皮肤，则为该皮肤名。
        ''' </summary>
        Public SkinName As String

        Public Sub New()
            Type = McLoginType.Legacy
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return GetHash(UserName & SkinType & SkinName & Type) Mod Integer.MaxValue
        End Function
    End Class

    '登录返回结果
    Public Structure McLoginResult
        Public Name As String
        Public Uuid As String
        Public AccessToken As String
        Public Type As String
        ''' <summary>
        ''' 仅用于登录方式为 Mojang 正版时的 Refresh Login，其余时间为一个无意义的 ID（一般为玩家的 UUID）。
        ''' </summary>
        Public ClientToken As String
        ''' <summary>
        ''' 用于登录的邮箱。仅用于登录方式为 Mojang 正版时的 launcher_profile 更新。
        ''' </summary>
        Public Email As String
        ''' <summary>
        ''' 进行微软登录时返回的 profile 信息。
        ''' </summary>
        Public ProfileJson As String
    End Structure

    ''' <summary>
    ''' 根据登录信息获取玩家的 MC 用户名。如果无法获取则返回 Nothing。
    ''' </summary>
    Public Function McLoginName() As String
        '根据当前登录方式优先返回
        Select Case Setup.Get("LoginType")
            Case McLoginType.Mojang
                If Setup.Get("CacheMojangName") <> "" Then Return Setup.Get("CacheMojangName")
            Case McLoginType.Ms
                If Setup.Get("CacheMsName") <> "" Then Return Setup.Get("CacheMsName")
            Case McLoginType.Legacy
                If Setup.Get("LoginLegacyName") <> "" Then Return Setup.Get("LoginLegacyName").ToString.Split("¨")(0)
            Case McLoginType.Nide
                If Setup.Get("CacheNideName") <> "" Then Return Setup.Get("CacheNideName")
            Case McLoginType.Auth
                If Setup.Get("CacheAuthName") <> "" Then Return Setup.Get("CacheAuthName")
        End Select
        '查找所有可能的项
        If Setup.Get("CacheMsName") <> "" Then Return Setup.Get("CacheMsName")
        If Setup.Get("CacheMojangName") <> "" Then Return Setup.Get("CacheMojangName")
        If Setup.Get("CacheNideName") <> "" Then Return Setup.Get("CacheNideName")
        If Setup.Get("CacheAuthName") <> "" Then Return Setup.Get("CacheAuthName")
        If Setup.Get("LoginLegacyName") <> "" Then Return Setup.Get("LoginLegacyName").ToString.Split("¨")(0)
        Return Nothing
    End Function
    ''' <summary>
    ''' 当前是否可以进行登录。若不可以则会返回错误原因。
    ''' </summary>
    Public Function McLoginAble() As String
        Select Case Setup.Get("LoginType")
            Case McLoginType.Mojang
                If Setup.Get("CacheMojangAccess") = "" Then
                    Return FrmLoginMojang.IsVaild()
                Else
                    Return ""
                End If
            Case McLoginType.Ms
                If Setup.Get("CacheMsOAuthRefresh") = "" Then
                    Return FrmLoginMs.IsVaild()
                Else
                    Return ""
                End If
            Case McLoginType.Legacy
                Return FrmLoginLegacy.IsVaild()
            Case McLoginType.Nide
                If Setup.Get("CacheNideAccess") = "" Then
                    Return FrmLoginNide.IsVaild()
                Else
                    Return ""
                End If
            Case McLoginType.Auth
                If Setup.Get("CacheAuthAccess") = "" Then
                    Return FrmLoginAuth.IsVaild()
                Else
                    Return ""
                End If
            Case Else
                Return "未知的登录方式"
        End Select
    End Function
    ''' <summary>
    ''' 登录输入是否可以进行登录。若不可以则会返回错误原因。
    ''' </summary>
    Public Function McLoginAble(LoginData As McLoginData) As String
        Select Case LoginData.Type
            Case McLoginType.Mojang
                Return PageLoginMojang.IsVaild(LoginData)
            Case McLoginType.Ms
                Return PageLoginMs.IsVaild(LoginData)
            Case McLoginType.Legacy
                Return PageLoginLegacy.IsVaild(LoginData)
            Case McLoginType.Nide
                Return PageLoginNide.IsVaild(LoginData)
            Case McLoginType.Auth
                Return PageLoginAuth.IsVaild(LoginData)
            Case Else
                Return "未知的登录方式"
        End Select
    End Function

    '登录主模块加载器
    Public McLoginLoader As New LoaderTask(Of McLoginData, McLoginResult)("登录", AddressOf McLoginStart, AddressOf McLoginInput, ThreadPriority.BelowNormal) With {.ReloadTimeout = 1, .ProgressWeight = 15, .Block = False}
    Public Function McLoginInput() As McLoginData
        Dim LoginData As McLoginData = Nothing
        Dim LoginType As McLoginType = Setup.Get("LoginType")
        Try
            Select Case LoginType
                Case McLoginType.Legacy
                    LoginData = PageLoginLegacy.GetLoginData()
                Case McLoginType.Mojang
                    If Setup.Get("CacheMojangAccess") = "" Then
                        LoginData = PageLoginMojang.GetLoginData()
                    Else
                        LoginData = PageLoginMojangSkin.GetLoginData()
                    End If
                Case McLoginType.Ms
                    If Setup.Get("CacheMsOAuthRefresh") = "" Then
                        LoginData = PageLoginMs.GetLoginData()
                    Else
                        LoginData = PageLoginMsSkin.GetLoginData()
                    End If
                Case McLoginType.Nide
                    If Setup.Get("CacheNideAccess") = "" Then
                        LoginData = PageLoginNide.GetLoginData()
                    Else
                        LoginData = PageLoginNideSkin.GetLoginData()
                    End If
                Case McLoginType.Auth
                    If Setup.Get("CacheAuthAccess") = "" Then
                        LoginData = PageLoginAuth.GetLoginData()
                    Else
                        LoginData = PageLoginAuthSkin.GetLoginData()
                    End If
            End Select
        Catch ex As Exception
            Log(ex, "获取登录输入信息失败（" & GetStringFromEnum(LoginType) & "）", LogLevel.Feedback)
        End Try
        Return LoginData
    End Function
    Private Sub McLoginStart(Data As LoaderTask(Of McLoginData, McLoginResult))
        McLaunchLog("登录线程已启动")
        '校验登录信息
        Dim CheckResult As String = McLoginAble(Data.Input)
        If Not CheckResult = "" Then Throw New ArgumentException(CheckResult)
        '获取对应加载器
        Dim Loader As LoaderBase = Nothing
        Select Case Data.Input.Type
            Case McLoginType.Mojang
                Loader = McLoginMojangLoader
            Case McLoginType.Ms
                Loader = McLoginMsLoader
            Case McLoginType.Legacy
                Loader = McLoginLegacyLoader
            Case McLoginType.Nide
                Loader = McLoginNideLoader
            Case McLoginType.Auth
                Loader = McLoginAuthLoader
        End Select
        '尝试加载
        Loader.WaitForExit(Data.Input, McLoginLoader, Data.IsForceRestarting)
        Data.Output = CType(Loader, Object).Output
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(True, False)) '刷新自动填充列表
    End Sub

#End Region
#Region "分方式登录模块"

    '各个登录方式的主对象与输入构造
    Public McLoginMojangLoader As New LoaderTask(Of McLoginServer, McLoginResult)("Loader Login Mojang", AddressOf McLoginServerStart) With {.ReloadTimeout = 60000}
    Public McLoginMsLoader As New LoaderTask(Of McLoginMs, McLoginResult)("Loader Login Ms", AddressOf McLoginMsStart) With {.ReloadTimeout = 300000}
    Public McLoginLegacyLoader As New LoaderTask(Of McLoginLegacy, McLoginResult)("Loader Login Legacy", AddressOf McLoginLegacyStart)
    Public McLoginNideLoader As New LoaderTask(Of McLoginServer, McLoginResult)("Loader Login Nide", AddressOf McLoginServerStart) With {.ReloadTimeout = 60000}
    Public McLoginAuthLoader As New LoaderTask(Of McLoginServer, McLoginResult)("Loader Login Auth", AddressOf McLoginServerStart) With {.ReloadTimeout = 60000}

    '主加载函数，返回所有需要的登录信息
    Private Sub McLoginMsStart(Data As LoaderTask(Of McLoginMs, McLoginResult))
        Dim Input As McLoginMs = Data.Input
        Dim LogUsername As String = Input.UserName
        McLaunchLog("登录方式：微软正版（" & If(LogUsername = "", "尚未登录", LogUsername) & "）")
        Data.Progress = 0.05
        '检查是否已经登录完成
        If Input.AccessToken <> "" AndAlso Not Data.IsForceRestarting Then
            Data.Output = New McLoginResult With {.AccessToken = Input.AccessToken, .Name = Input.UserName, .Uuid = Input.Uuid, .Type = "Microsoft", .ClientToken = Input.Uuid}
            GoTo SkipLogin
        End If
        '尝试登录
        Dim OAuthTokens As String()
        If Setup.Get("CacheMsOAuthRefresh") = "" Then
            '无 RefreshToken
Relogin:
            Dim OAuthCode As String = MsLoginStep1(Data)
            If Data.IsAborted Then Throw New ThreadInterruptedException
            Data.Progress = 0.2
            OAuthTokens = MsLoginStep2(OAuthCode, False)
        Else
            '有 RefreshToken
            OAuthTokens = MsLoginStep2(Setup.Get("CacheMsOAuthRefresh"), True)
        End If
        '要求重新打开登录网页认证
        If OAuthTokens(0) = "Relogin" Then GoTo Relogin
        Data.Progress = 0.35
        If Data.IsAborted Then Throw New ThreadInterruptedException
        Dim OAuthAccessToken As String = OAuthTokens(0)
        Dim OAuthRefreshToken As String = OAuthTokens(1)
        Dim XBLToken As String = MsLoginStep3(OAuthAccessToken)
        Data.Progress = 0.5
        If Data.IsAborted Then Throw New ThreadInterruptedException
        Dim Tokens = MsLoginStep4(XBLToken)
        Data.Progress = 0.65
        If Data.IsAborted Then Throw New ThreadInterruptedException
        Dim AccessToken As String = MsLoginStep5(Tokens)
        Data.Progress = 0.8
        If Data.IsAborted Then Throw New ThreadInterruptedException
        Dim Result = MsLoginStep6(AccessToken)
        '输出登录结果
        Setup.Set("CacheMsOAuthRefresh", OAuthRefreshToken)
        Setup.Set("CacheMsAccess", AccessToken)
        Setup.Set("CacheMsUuid", Result(0))
        Setup.Set("CacheMsName", Result(1))
        Data.Output = New McLoginResult With {.AccessToken = AccessToken, .Name = Result(1), .Uuid = Result(0), .Type = "Microsoft", .ClientToken = Result(0), .ProfileJson = Result(2)}
        '解锁主题
SkipLogin:
        McLaunchLog("微软登录完成")
        Data.Progress = 0.95
        If ThemeUnlock(10, False) Then MyMsgBox("感谢你对正版游戏的支持！" & vbCrLf & "隐藏主题 跳票红 已解锁！", "提示")
    End Sub
    Private Sub McLoginServerStart(Data As LoaderTask(Of McLoginServer, McLoginResult))
        Dim Input As McLoginServer = Data.Input
        Dim NeedRefresh As Boolean = False
        Dim LogUsername As String = Input.UserName
        If LogUsername.Contains("@") AndAlso Setup.Get("UiLauncherEmail") Then
            LogUsername = AccountFilter(LogUsername)
        End If
        McLaunchLog("登录方式：" & Input.Description & "（" & LogUsername & "）")
        Data.Progress = 0.05
        '尝试登录
        If (Not Data.Input.ForceReselectProfile) AndAlso Setup.Get("Cache" & Input.Token & "Username") = Data.Input.UserName AndAlso Setup.Get("Cache" & Input.Token & "Pass") = Data.Input.Password AndAlso Not Setup.Get("Cache" & Input.Token & "Access") = "" AndAlso Not Setup.Get("Cache" & Input.Token & "Client") = "" AndAlso Not Setup.Get("Cache" & Input.Token & "Uuid") = "" AndAlso Not Setup.Get("Cache" & Input.Token & "Name") = "" Then
            '尝试验证登录
            Try
                If Data.IsAborted Then Throw New ThreadInterruptedException
                McLoginRequestValidate(Data)
                GoTo LoginFinish
            Catch ex As Exception
                Dim AllMessage = GetString(ex)
                McLaunchLog("验证登录失败：" & AllMessage)
                If (AllMessage.Contains("超时") OrElse AllMessage.Contains("imeout")) AndAlso Not AllMessage.Contains("403") Then
                    McLaunchLog("已触发超时登录失败")
                    Throw New Exception("$登录失败：连接登录服务器超时。" & vbCrLf & "请检查你的网络状况是否良好，或尝试使用 VPN！")
                End If
            End Try
            Data.Progress = 0.25
            '尝试刷新登录
Refresh:
            Try
                If Data.IsAborted Then Throw New ThreadInterruptedException
                McLoginRequestRefresh(Data, NeedRefresh)
                GoTo LoginFinish
            Catch ex As Exception
                McLaunchLog("刷新登录失败：" & GetString(ex))
            End Try
            Data.Progress = If(NeedRefresh, 0.85, 0.45)
        End If
        '尝试普通登录
        Try
            If Data.IsAborted Then Throw New ThreadInterruptedException
            NeedRefresh = McLoginRequestLogin(Data)
        Catch ex As Exception
            McLaunchLog("登录失败：" & GetString(ex))
            Throw
        End Try
        If NeedRefresh Then
            Data.Progress = 0.65
            GoTo Refresh
        End If
LoginFinish:
        Data.Progress = 0.95
        '保存启动记录
        Dim Dict As New Dictionary(Of String, String)
        Dim Emails As New List(Of String)
        Dim Passwords As New List(Of String)
        Try
            If Not Setup.Get("Login" & Input.Token & "Email") = "" Then Emails.AddRange(Setup.Get("Login" & Input.Token & "Email").ToString.Split("¨"))
            If Not Setup.Get("Login" & Input.Token & "Pass") = "" Then Passwords.AddRange(Setup.Get("Login" & Input.Token & "Pass").ToString.Split("¨"))
            For i = 0 To Emails.Count - 1
                Dict.Add(Emails(i), Passwords(i))
            Next
            Dict.Remove(Input.UserName)
            Emails = New List(Of String)(Dict.Keys)
            Emails.Insert(0, Input.UserName)
            Passwords = New List(Of String)(Dict.Values)
            Passwords.Insert(0, Input.Password)
            Setup.Set("Login" & Input.Token & "Email", Join(Emails, "¨"))
            Setup.Set("Login" & Input.Token & "Pass", Join(Passwords, "¨"))
        Catch ex As Exception
            Log(ex, "保存启动记录失败", LogLevel.Hint)
            Setup.Set("Login" & Input.Token & "Email", "")
            Setup.Set("Login" & Input.Token & "Pass", "")
        End Try
    End Sub
    Private Sub McLoginLegacyStart(Data As LoaderTask(Of McLoginLegacy, McLoginResult))
        Dim Input As McLoginLegacy = Data.Input
        McLaunchLog("登录方式：离线（" & Input.UserName & "）")
        Data.Progress = 0.1
        With Data.Output
            .Name = Input.UserName
            .Uuid = McLoginLegacyUuid(Input.UserName)
            .Type = "Legacy"
        End With
        '根据离线皮肤获取实际使用的 Uuid
        Select Case Input.SkinType
            Case 0
                '默认，不需要处理
            Case 1
                'Steve
                Do Until McSkinSex(Data.Output.Uuid) = "Steve"
                    If Data.Output.Uuid.EndsWith("FFFFF") Then Data.Output.Uuid = Mid(Data.Output.Uuid, 1, 32 - 5) & "00000"
                    Data.Output.Uuid = Mid(Data.Output.Uuid, 1, 32 - 5) & (Long.Parse(Right(Data.Output.Uuid, 5), Globalization.NumberStyles.AllowHexSpecifier) + 1).ToString("X")
                Loop
            Case 2
                'Alex
                Do Until McSkinSex(Data.Output.Uuid) = "Alex"
                    If Data.Output.Uuid.EndsWith("FFFFF") Then Data.Output.Uuid = Mid(Data.Output.Uuid, 1, 32 - 5) & "00000"
                    Data.Output.Uuid = Mid(Data.Output.Uuid, 1, 32 - 5) & (Long.Parse(Right(Data.Output.Uuid, 5), Globalization.NumberStyles.AllowHexSpecifier) + 1).ToString("X")
                Loop
            Case 3
                '使用正版用户名
                Try
                    If Not Input.SkinName = "" Then
                        Log("[Skin] 由于离线皮肤设置，使用正版 UUID：" & Input.SkinName)
                        Data.Output.Uuid = McLoginMojangUuid(Input.SkinName, False)
                    End If
                Catch ex As Exception
                    Log(ex, "离线启动时使用的正版皮肤获取失败")
                    MyMsgBox("由于设置的离线启动时使用的正版皮肤获取失败，游戏将以无皮肤的方式启动。" & vbCrLf & "请检查你的网络是否通畅，或尝试使用 VPN！" & vbCrLf & vbCrLf & "详细的错误信息：" & ex.Message, "皮肤获取失败")
                End Try
            Case 4
                '自定义
                Do Until McSkinSex(Data.Output.Uuid) = If(Setup.Get("LaunchSkinSlim"), "Alex", "Steve")
                    If Data.Output.Uuid.EndsWith("FFFFF") Then Data.Output.Uuid = Mid(Data.Output.Uuid, 1, 32 - 5) & "00000"
                    Data.Output.Uuid = Mid(Data.Output.Uuid, 1, 32 - 5) & (Long.Parse(Right(Data.Output.Uuid, 5), Globalization.NumberStyles.AllowHexSpecifier) + 1).ToString("X")
                Loop
        End Select
        '将结果扩展到所有项目中
        'Data.Output.AccessToken = Setup.Get("CacheMojangAccess") '不能优先使用缓存的 AccessToken，这会导致离线用户名设置失效
        'If Data.Output.AccessToken.Length < 300 Then
        Data.Output.AccessToken = Data.Output.Uuid
        'Data.Output.ClientToken = Setup.Get("CacheMojangClient")
        'If Data.Output.AccessToken.Length <> 32 Then
        Data.Output.ClientToken = Data.Output.Uuid
        '保存启动记录
        Dim Names As New List(Of String)
        If Not Setup.Get("LoginLegacyName") = "" Then Names.AddRange(Setup.Get("LoginLegacyName").ToString.Split("¨"))
        Names.Remove(Input.UserName)
        Names.Insert(0, Input.UserName)
        Setup.Set("LoginLegacyName", Join(Names.ToArray, "¨"))
    End Sub

    'Server 登录：三种验证方式的请求
    Private Sub McLoginRequestValidate(ByRef Data As LoaderTask(Of McLoginServer, McLoginResult))
        McLaunchLog("验证登录开始（Validate, " & Data.Input.Token & "）")
        '提前缓存信息，否则如果在登录请求过程中退出登录，设置项目会被清空，导致输出存在空值
        Dim AccessToken As String = Setup.Get("Cache" & Data.Input.Token & "Access")
        Dim ClientToken As String = Setup.Get("Cache" & Data.Input.Token & "Client")
        Dim Uuid As String = Setup.Get("Cache" & Data.Input.Token & "Uuid")
        Dim Name As String = Setup.Get("Cache" & Data.Input.Token & "Name")
        '发送登录请求
        NetRequestRetry(
               Url:=Data.Input.BaseUrl & "/validate",
               Method:="POST",
               Data:="{""accessToken"":""" & AccessToken & """,""clientToken"":""" & ClientToken & """,""requestUser"":true}",
               ContentType:="application/json; charset=utf-8") '没有返回值的
        '将登录结果输出
        Data.Output.AccessToken = AccessToken
        Data.Output.ClientToken = ClientToken
        Data.Output.Uuid = Uuid
        Data.Output.Name = Name
        Data.Output.Type = Data.Input.Token
        Data.Output.Email = Data.Input.UserName
        '不更改缓存，直接结束
        McLaunchLog("验证登录成功（Validate, " & Data.Input.Token & "）")
    End Sub
    Private Sub McLoginRequestRefresh(ByRef Data As LoaderTask(Of McLoginServer, McLoginResult), RequestUser As Boolean)
        McLaunchLog("刷新登录开始（Refresh, " & Data.Input.Token & "）")
        Dim LoginJson As JObject = GetJson(NetRequestRetry(
               Url:=Data.Input.BaseUrl & "/refresh",
               Method:="POST",
               Data:="{" &
               If(RequestUser, "
               ""requestUser"": true,
               ""selectedProfile"": {
                   ""id"":""" & Setup.Get("Cache" & Data.Input.Token & "Uuid") & """,
                   ""name"":""" & Setup.Get("Cache" & Data.Input.Token & "Name") & """},", "") & "
               ""accessToken"":""" & Setup.Get("Cache" & Data.Input.Token & "Access") & """,
               ""clientToken"":""" & Setup.Get("Cache" & Data.Input.Token & "Client") & """}",
               ContentType:="application/json; charset=utf-8"))
        '将登录结果输出
        If LoginJson("selectedProfile") Is Nothing Then Throw New Exception("选择的角色 " & Setup.Get("Cache" & Data.Input.Token & "Name") & " 无效！")
        Data.Output.AccessToken = LoginJson("accessToken").ToString
        Data.Output.ClientToken = LoginJson("clientToken").ToString
        Data.Output.Uuid = LoginJson("selectedProfile")("id").ToString
        Data.Output.Name = LoginJson("selectedProfile")("name").ToString
        Data.Output.Type = Data.Input.Token
        Data.Output.Email = Data.Input.UserName
        '保存缓存
        Setup.Set("Cache" & Data.Input.Token & "Access", Data.Output.AccessToken)
        Setup.Set("Cache" & Data.Input.Token & "Client", Data.Output.ClientToken)
        Setup.Set("Cache" & Data.Input.Token & "Uuid", Data.Output.Uuid)
        Setup.Set("Cache" & Data.Input.Token & "Name", Data.Output.Name)
        Setup.Set("Cache" & Data.Input.Token & "Username", Data.Input.UserName)
        Setup.Set("Cache" & Data.Input.Token & "Pass", Data.Input.Password)
        McLaunchLog("刷新登录成功（Refresh, " & Data.Input.Token & "）")
    End Sub
    Private Function McLoginRequestLogin(ByRef Data As LoaderTask(Of McLoginServer, McLoginResult)) As Boolean
        Try
            Dim NeedRefresh As Boolean = False
            McLaunchLog("登录开始（Login, " & Data.Input.Token & "）")
            Dim LoginJson As JObject = GetJson(NetRequestRetry(
                   Url:=Data.Input.BaseUrl & "/authenticate",
                   Method:="POST",
                   Data:="{""agent"": {""name"": ""Minecraft"",""version"": 1},""username"":""" & Data.Input.UserName & """,""password"":""" & Data.Input.Password & """,""requestUser"":true}",
                   ContentType:="application/json; charset=utf-8"))
            '检查登录结果
            If LoginJson("availableProfiles").Count = 0 Then
                If Data.Input.Type = McLoginType.Auth Then
                    If Data.Input.ForceReselectProfile Then Hint("你还没有创建角色，无法更换！", HintType.Critical)
                    Throw New Exception("$你还没有创建角色，请在创建角色后再试！")
                Else
                    Throw New Exception("$你还没有购买 Minecraft 正版，请在购买后再试！")
                End If
            ElseIf Data.Input.ForceReselectProfile AndAlso LoginJson("availableProfiles").Count = 1 Then
                Hint("你的账户中只有一个角色，无法更换！", HintType.Critical)
            End If
            Dim SelectedName As String = Nothing
            Dim SelectedId As String = Nothing
            If (LoginJson("selectedProfile") Is Nothing OrElse Data.Input.ForceReselectProfile) AndAlso LoginJson("availableProfiles").Count > 1 Then
                '要求选择档案；优先从缓存读取
                NeedRefresh = True
                Dim CacheId As String = Setup.Get("Cache" & Data.Input.Token & "Uuid")
                For Each Profile In LoginJson("availableProfiles")
                    If Profile("id").ToString = CacheId Then
                        SelectedName = Profile("name").ToString
                        SelectedId = Profile("id").ToString
                        McLaunchLog("根据缓存选择的角色：" & SelectedName)
                    End If
                Next
                '缓存无效，要求玩家选择
                If SelectedName Is Nothing Then
                    McLaunchLog("要求玩家选择角色")
                    RunInUiWait(Sub()
                                    Dim SelectionControl As New List(Of IMyRadio)
                                    Dim SelectionJson As New List(Of JToken)
                                    For Each Profile In LoginJson("availableProfiles")
                                        SelectionControl.Add(New MyRadioBox With {.Text = Profile("name").ToString})
                                        SelectionJson.Add(Profile)
                                    Next
                                    Dim SelectedIndex As Integer = MyMsgBoxSelect(SelectionControl, "选择使用的角色")
                                    SelectedName = SelectionJson(SelectedIndex)("name").ToString
                                    SelectedId = SelectionJson(SelectedIndex)("id").ToString
                                End Sub)
                    McLaunchLog("玩家选择的角色：" & SelectedName)
                End If
            Else
                SelectedName = LoginJson("selectedProfile")("name").ToString
                SelectedId = LoginJson("selectedProfile")("id").ToString
            End If
            '将登录结果输出
            Data.Output.AccessToken = LoginJson("accessToken").ToString
            Data.Output.ClientToken = LoginJson("clientToken").ToString
            Data.Output.Name = SelectedName
            Data.Output.Uuid = SelectedId
            Data.Output.Type = Data.Input.Token
            Data.Output.Email = Data.Input.UserName
            '保存缓存
            Setup.Set("Cache" & Data.Input.Token & "Access", Data.Output.AccessToken)
            Setup.Set("Cache" & Data.Input.Token & "Client", Data.Output.ClientToken)
            Setup.Set("Cache" & Data.Input.Token & "Uuid", Data.Output.Uuid)
            Setup.Set("Cache" & Data.Input.Token & "Name", Data.Output.Name)
            Setup.Set("Cache" & Data.Input.Token & "Username", Data.Input.UserName)
            Setup.Set("Cache" & Data.Input.Token & "Pass", Data.Input.Password)
            McLaunchLog("登录成功（Login, " & Data.Input.Token & "）")
            Return NeedRefresh
        Catch ex As Exception
            Dim AllMessage As String = GetString(ex)
            Log(ex, "登录失败原始错误信息", LogLevel.Normal)
            If AllMessage.Contains("410") AndAlso AllMessage.Contains("Migrated") Then
                Throw New Exception("$登录失败：该 Mojang 账号已迁移至微软账号，请在上方的登录方式中选择 微软 并再次尝试登录！")
            ElseIf AllMessage.Contains("403") Then
                Select Case Data.Input.Type
                    Case McLoginType.Auth
                        Throw New Exception("$登录失败，以下为可能的原因：" & vbCrLf &
                                            " - 输入的账号或密码错误。" & vbCrLf &
                                            " - 登录尝试过于频繁，导致被暂时屏蔽。请不要操作，等待 10 分钟后再试。" & vbCrLf &
                                            " - 只注册了账号，但没有在皮肤站新建角色。")
                    Case McLoginType.Legacy
                        Throw
                    Case McLoginType.Mojang
                        If AllMessage.Contains("Invalid username or password") Then
                            Throw New Exception("$登录失败：输入的账号或密码有误。")
                        Else
                            Throw New Exception("$登录尝试过于频繁，导致被 Mojang 暂时屏蔽。请不要操作，等待 10 分钟后再试。")
                        End If
                    Case McLoginType.Ms
                        Throw New Exception("$登录失败，以下为可能的原因：" & vbCrLf &
                                            " - 登录尝试过于频繁，导致被暂时屏蔽。请不要操作，等待 10 分钟后再试。" & vbCrLf &
                                            " - 账号类别错误。如果你在使用 Mojang 账号，请将登录方式切换为 Mojang。")
                    Case McLoginType.Nide
                        Throw New Exception("$登录失败，以下为可能的原因：" & vbCrLf &
                                            " - 输入的账号或密码错误。" & vbCrLf &
                                            " - 密码错误次数过多，导致被暂时屏蔽。请不要操作，等待 10 分钟后再试。" & vbCrLf &
                                            If(Data.Input.UserName.Contains("@"), "", " - 登录账号应为邮箱或统一通行证账号，而非游戏角色 ID。" & vbCrLf) &
                                            " - 只注册了账号，但没有加入对应服务器。")
                End Select
            ElseIf AllMessage.Contains("超时") OrElse AllMessage.Contains("imeout") Then
                Throw New Exception("$登录失败：连接登录服务器超时。" & vbCrLf & "请检查你的网络状况是否良好，或尝试使用 VPN！")
            ElseIf AllMessage.Contains("网络请求失败") Then
                Throw New Exception("$登录失败：连接登录服务器失败。" & vbCrLf & "请检查你的网络状况是否良好，或尝试使用 VPN！")
            ElseIf ex.Message.StartsWith("$") Then
                Throw
            Else
                Throw New Exception("登录失败：" & ex.Message, ex)
            End If
            Return False
        End Try
    End Function

    '微软登录步骤 1：打开网页认证，获取 OAuth Code
    Private Function MsLoginStep1(Data As LoaderTask(Of McLoginMs, McLoginResult)) As String
        McLaunchLog("开始微软登录步骤 1")
        If OsVersion <= New Version(10, 0, 17763, 0) Then 'TODO: 添加设置以强制使用网页中继登录
            'Windows 7 或老版 Windows 10 登录
            MyMsgBox("PCL2 即将打开登录网页。登录后会转到一个空白页面（这代表登录成功了），请将该空白页面的网址复制到 PCL2。" & vbCrLf &
                     "如果网络环境不佳，登录网页可能一直加载不出来，此时请尝试使用 VPN 或代理服务器，然后再试。", "登录说明", "开始", ForceWait:=True)
            OpenWebsite(FormLoginOAuth.LoginUrl1)
            Dim Result As String = MyMsgBoxInput("", New ObjectModel.Collection(Of Validate) From {New ValidateRegex("(?<=code\=)[^&]+", "返回网址应以 https://login.live.com/oauth20_desktop.srf?code= 开头")}, "https://login.live.com/oauth20_desktop.srf?code=XXXXXX", "输入登录返回码", "确定", "取消")
            If Result Is Nothing Then
                McLaunchLog("微软登录已在步骤 1 被取消")
                Throw New ThreadInterruptedException("$$")
            Else
                Return RegexSeek(Result, "(?<=code\=)[^&]+")
            End If
        Else
            'Windows 10 登录
            Dim ReturnCode As String = Nothing
            Dim ReturnEx As Exception = Nothing
            Dim IsFinished As LoadState = LoadState.Loading
            Dim LoginForm As FormLoginOAuth = Nothing
            RunInUi(Sub()
                        Try
                            LoginForm = New FormLoginOAuth
                            LoginForm.Show()
                            AddHandler LoginForm.OnLoginSuccess, Sub(Code As String)
                                                                     ReturnCode = Code
                                                                     IsFinished = LoadState.Finished
                                                                 End Sub
                            AddHandler LoginForm.OnLoginCanceled, Sub() IsFinished = LoadState.Aborted
                        Catch ex As Exception
                            ReturnEx = ex
                            IsFinished = LoadState.Failed
                        End Try
                    End Sub)
            Do While IsFinished = LoadState.Loading AndAlso Not Data.IsAborted
                Thread.Sleep(20)
            Loop
            RunInUi(Sub() If LoginForm IsNot Nothing Then LoginForm.Close())
            If IsFinished = LoadState.Finished Then
                Return ReturnCode
            ElseIf IsFinished = LoadState.Failed Then
                Throw ReturnEx
            Else
                McLaunchLog("微软登录已在步骤 1 被取消")
                Throw New ThreadInterruptedException("$$")
            End If
        End If
    End Function
    '微软登录步骤 2：从 OAuth Code 或 OAuth RefreshToken 获取 {OAuth AccessToken, OAuth RefreshToken}
    Private Function MsLoginStep2(Code As String, IsRefresh As Boolean) As String()
        McLaunchLog("开始微软登录步骤 2（" & If(IsRefresh, "", "非") & "刷新登录）")

        Dim Request As String
        If IsRefresh Then
            Request = "client_id=00000000402b5328" & "&" &
                      "refresh_token=" & Uri.EscapeDataString(Code) & "&" &
                      "grant_type=refresh_token" & "&" &
                      "redirect_uri=" & Uri.EscapeDataString("https://login.live.com/oauth20_desktop.srf") & "&" &
                      "scope=" & Uri.EscapeDataString("service::user.auth.xboxlive.com::MBI_SSL")
        Else
            Request = "client_id=00000000402b5328" & "&" &
                      "code=" & Uri.EscapeDataString(Code) & "&" &
                      "grant_type=authorization_code" & "&" &
                      "redirect_uri=" & Uri.EscapeDataString("https://login.live.com/oauth20_desktop.srf") & "&" &
                      "scope=" & Uri.EscapeDataString("service::user.auth.xboxlive.com::MBI_SSL")
        End If
        Dim Result As String
        Try
            Result = NetRequestMuity("https://login.live.com/oauth20_token.srf", "POST", Request, "application/x-www-form-urlencoded", 1)
        Catch ex As Exception
            If ex.Message.Contains("must sign in again") Then
                Return {"Relogin", ""}
            Else
                Throw
            End If
        End Try

        Dim ResultJson As JObject = GetJson(Result)
        Dim AccessToken As String = ResultJson("access_token").ToString
        Dim RefreshToken As String = ResultJson("refresh_token").ToString
        Return {AccessToken, RefreshToken}
    End Function
    '微软登录步骤 3：从 OAuth AccessToken 获取 XBLToken
    Private Function MsLoginStep3(AccessToken As String) As String
        McLaunchLog("开始微软登录步骤 3")

        Dim Request As String = "{
                                    ""Properties"": {
                                        ""AuthMethod"": ""RPS"",
                                        ""SiteName"": ""user.auth.xboxlive.com"",
                                        ""RpsTicket"": """ & AccessToken & """
                                    },
                                    ""RelyingParty"": ""http://auth.xboxlive.com"",
                                    ""TokenType"": ""JWT""
                                 }"
        Dim Result As String = NetRequestMuity("https://user.auth.xboxlive.com/user/authenticate", "POST", Request, "application/json", 3)

        Dim ResultJson As JObject = GetJson(Result)
        Dim XBLToken As String = ResultJson("Token").ToString
        Return XBLToken
    End Function
    '微软登录步骤 4：从 XBLToken 获取 {XSTSToken, UHS}
    Private Function MsLoginStep4(XBLToken As String) As String()
        McLaunchLog("开始微软登录步骤 4")

        Dim Request As String = "{
                                    ""Properties"": {
                                        ""SandboxId"": ""RETAIL"",
                                        ""UserTokens"": [
                                            """ & XBLToken & """
                                        ]
                                    },
                                    ""RelyingParty"": ""rp://api.minecraftservices.com/"",
                                    ""TokenType"": ""JWT""
                                 }"
        Dim Result As String
        Try
            Result = NetRequestMuity("https://xsts.auth.xboxlive.com/xsts/authorize", "POST", Request, "application/json", 3)
        Catch ex As Net.WebException
            If ex.Message.Contains("2148916233") Then
                Throw New Exception("$该微软账号尚未购买 Minecraft Java 版或注册 XBox 账户！")
            ElseIf ex.Message.Contains("2148916238") Then
                If MyMsgBox("该账号年龄不足，你需要先修改出生日期，然后才能登录。" & vbCrLf &
                            "该账号目前填写的年龄是否在 13 岁以上？", "登录提示", "13 岁以上", "12 岁以下", "我不知道") = 1 Then
                    OpenWebsite("https://account.live.com/editprof.aspx")
                    MyMsgBox("请在打开的网页中修改账号的出生日期（至少改为 18 岁以上）。" & vbCrLf &
                             "在修改成功后等待一分钟，然后再回到 PCL2，就可以正常登录了！", "登录提示")
                Else
                    OpenWebsite("https://support.microsoft.com/zh-cn/account-billing/如何更改-microsoft-帐户上的出生日期-837badbc-999e-54d2-2617-d19206b9540a")
                    MyMsgBox("请根据打开的网页的说明，修改账号的出生日期（至少改为 18 岁以上）。" & vbCrLf &
                             "在修改成功后等待一分钟，然后再回到 PCL2，就可以正常登录了！", "登录提示")
                End If
                Throw New Exception("$$")
            Else
                Throw
            End If
        End Try

        Dim ResultJson As JObject = GetJson(Result)
        Dim XSTSToken As String = ResultJson("Token").ToString
        Dim UHS As String = ResultJson("DisplayClaims")("xui")(0)("uhs").ToString
        Return {XSTSToken, UHS}
    End Function
    '微软登录步骤 5：从 {XSTSToken, UHS} 获取 Minecraft AccessToken
    Private Function MsLoginStep5(Tokens As String()) As String
        McLaunchLog("开始微软登录步骤 5")

        Dim Request As String = "{""identityToken"": ""XBL3.0 x=" & Tokens(1) & ";" & Tokens(0) & """}"
        Dim Result As String
        Try
            Result = NetRequestMuity("https://api.minecraftservices.com/authentication/login_with_xbox", "POST", Request, "application/json", 2)
        Catch ex As Net.WebException
            Dim Message As String = GetString(ex)
            If Message.Contains("(429)") Then
                Log(ex, "微软登录第 5 步汇报 429")
                Throw New Exception("$登录尝试太过频繁，请等待几分钟后再试！")
            ElseIf Message.Contains("(403)") Then
                Log(ex, "微软登录第 5 步汇报 403")
                Throw New Exception("$当前 IP 的登录尝试异常。" & vbCrLf & "如果你使用了 VPN 或加速器，请把它们关掉或更换节点后再试！")
            Else
                Throw
            End If
        End Try

        Dim ResultJson As JObject = GetJson(Result)
        Dim AccessToken As String = ResultJson("access_token").ToString
        Return AccessToken
    End Function
    '微软登录步骤 6：从 Minecraft AccessToken 获取 {UUID, UserName, ProfileJson}
    Private Function MsLoginStep6(AccessToken As String) As String()
        McLaunchLog("开始微软登录步骤 6")

        Dim Result As String
        Try
            Result = NetRequestMuity("https://api.minecraftservices.com/minecraft/profile", "GET", "", "application/json", 2, New Dictionary(Of String, String) From {{"Authorization", "Bearer " & AccessToken}})
        Catch ex As Net.WebException
            Dim Message As String = GetString(ex)
            If Message.Contains("(429)") Then
                Log(ex, "微软登录第 6 步汇报 429")
                Throw New Exception("$登录尝试太过频繁，请等待几分钟后再试！")
            ElseIf Message.Contains("(404)") Then
                Log(ex, "微软登录第 6 步汇报 404")
                Throw New Exception("$你可能没有在购买后去 Minecraft 官网创建游戏档案，或者没有购买 Minecraft。")
            Else
                Throw
            End If
        End Try
        Dim ResultJson As JObject = GetJson(Result)
        Dim UUID As String = ResultJson("id").ToString
        Dim UserName As String = ResultJson("name").ToString
        Return {UUID, UserName, Result}
    End Function

    '根据用户名返回对应 Uuid，需要多线程
    Public Function McLoginMojangUuid(Name As String, ThrowOnNotFound As Boolean)
        If Name.Trim.Length = 0 Then Return StrFill("", "0", 32)
        '从缓存获取
        Dim Uuid As String = ReadIni(PathTemp & "Cache\Uuid\Mojang.ini", Name, "")
        If Len(Uuid) = 32 Then Return Uuid
        '从官网获取
        Try
            Dim GotJson As JObject = NetGetCodeByRequestRetry("https://api.mojang.com/users/profiles/minecraft/" & Name, IsJson:=True)
            If GotJson Is Nothing Then Throw New FileNotFoundException("正版玩家档案不存在（" & Name & "）")
            Uuid = If(GotJson("id"), "")
        Catch ex As Exception
            Log(ex, "从官网获取正版 Uuid 失败（" & Name & "）")
            If Not ThrowOnNotFound AndAlso ex.GetType.Name = "FileNotFoundException" Then
                Uuid = McLoginLegacyUuid(Name) '玩家档案不存在
            Else
                Throw New Exception("从官网获取正版 Uuid 失败", ex)
            End If
        End Try
        '写入缓存
        If Not Len(Uuid) = 32 Then Throw New Exception("获取的正版 Uuid 长度不足（" & Uuid & "）")
        WriteIni(PathTemp & "Cache\Uuid\Mojang.ini", Name, Uuid)
        Return Uuid
    End Function
    Public Function McLoginLegacyUuid(Name As String)
        Dim FullUuid As String = StrFill(Name.Length.ToString("X"), "0", 16) & StrFill(GetHash(Name).ToString("X"), "0", 16)
        Return FullUuid.Substring(0, 12) & "3" & FullUuid.Substring(13, 3) & "9" & FullUuid.Substring(17, 15)
    End Function

#End Region

#Region "Java 处理"

    Private McLaunchJavaSelected As JavaEntry = Nothing
    Private Sub McLaunchJavaValidate()
        Dim MinVer As New Version(0, 0, 0, 0), MaxVer As New Version(999, 999, 999, 999)

        'MC 大版本检测
        If (McVersionCurrent.ReleaseTime >= New Date(2021, 11, 16) AndAlso McVersionCurrent.Version.McCodeMain = 99) OrElse
            (McVersionCurrent.Version.McCodeMain >= 18 AndAlso McVersionCurrent.Version.McCodeMain <> 99) Then
            '1.18 pre2+：至少 Java 17
            MinVer = New Version(1, 17, 0, 0)
        ElseIf (McVersionCurrent.ReleaseTime >= New Date(2021, 5, 11) AndAlso McVersionCurrent.Version.McCodeMain = 99) OrElse
           (McVersionCurrent.Version.McCodeMain >= 17 AndAlso McVersionCurrent.Version.McCodeMain <> 99) Then
            '21w19a+：至少 Java 16
            MinVer = New Version(1, 16, 0, 0)
        ElseIf McVersionCurrent.ReleaseTime.Year >= 2017 Then 'Minecraft 1.12 与 1.11 的分界线正好是 2017 年，太棒了
            '1.12+：至少 Java 8
            MinVer = New Version(1, 8, 0, 0)
        ElseIf McVersionCurrent.ReleaseTime.Year >= 2001 Then '避免某些版本的 1960 癌
            '1.11-：最高 Java 12
            MaxVer = New Version(1, 12, 999, 999)
        End If

        'Forge 检测
        If McVersionCurrent.Version.HasForge Then
            If McVersionCurrent.Version.McName = "1.7.2" Then
                '1.7.2：必须 Java 7
                MinVer = New Version(1, 7, 0, 0) : MaxVer = New Version(1, 7, 999, 999)
            ElseIf McVersionCurrent.Version.McCodeMain <= 12 AndAlso McVersionCurrent.Version.McCodeMain <> -1 AndAlso VersionSortBoolean("14.23.5.2855", McVersionCurrent.Version.ForgeVersion) Then
                '1.12，Forge 14.23.5.2855 及更低：Java 8
                MaxVer = New Version(1, 8, 999, 999)
            ElseIf McVersionCurrent.Version.McCodeMain <= 14 AndAlso McVersionCurrent.Version.McCodeMain <> -1 AndAlso VersionSortBoolean("28.2.23", McVersionCurrent.Version.ForgeVersion) Then
                '1.13 - 1.14，Forge 28.2.23 及更低：Java 8 - 10
                MinVer = New Version(1, 8, 0, 0) : MaxVer = New Version(1, 10, 999, 999)
            ElseIf McVersionCurrent.Version.McCodeMain <= 16 AndAlso McVersionCurrent.Version.McCodeMain <> -1 Then
                '1.15 - 1.16：Java 8 - 12， 12 - 15未测试
                MinVer = New Version(1, 8, 0, 0) : MaxVer = New Version(1, 15, 999, 999)
            End If
        End If

        '统一通行证检测
        If Setup.Get("LoginType") = McLoginType.Nide Then
            '至少 Java 8.101 (1.8.0.101)
            MinVer = If(New Version(1, 8, 0, 101) > MinVer, New Version(1, 8, 0, 101), MinVer)
        End If

        '选择 Java
        McLaunchLog("Java 版本需求：最低 " & MinVer.ToString & "，最高 " & MaxVer.ToString)
        McLaunchJavaSelected = JavaSelect(MinVer, MaxVer, McVersionCurrent)
        If McLaunchJavaSelected IsNot Nothing Then
            McLaunchLog("选择的 Java：" & McLaunchJavaSelected.ToString)
            Exit Sub
        End If

        '无合适的 Java
        McLaunchLog("无合适的 Java，取消启动")
        If MinVer >= New Version(1, 17, 0, 0) Then
            '缺少 Java 17
            JavaMissing(17)
        ElseIf MinVer >= New Version(1, 16, 0, 0) Then
            '缺少 Java 16
            JavaMissing(16)
        ElseIf MaxVer < New Version(1, 8, 0, 0) Then
            '缺少 Java 7
            JavaMissing(7)
        Else
            '缺少 Java 8 较新版
            JavaMissing(8)
        End If
        Throw New Exception("$$")

    End Sub

#End Region

#Region "启动参数"

    Private McLaunchArgument As String

    '主方法，合并 Jvm、Game、Replace 三部分的参数数据
    Private Sub McLaunchArgumentMain(Loader As LoaderTask(Of String, List(Of McLibToken)))
        McLaunchLog("开始获取 Minecraft 启动参数")
        '获取基准字符串与参数信息
        Dim Arguments As String
        If McVersionCurrent.JsonObject("arguments") IsNot Nothing AndAlso McVersionCurrent.JsonObject("arguments")("jvm") IsNot Nothing Then
            McLaunchLog("获取新版 JVM 参数")
            Arguments = McLaunchArgumentsJvmNew(McVersionCurrent)
            McLaunchLog("新版 JVM 参数获取成功：")
            McLaunchLog(Arguments)
        Else
            McLaunchLog("获取旧版 JVM 参数")
            Arguments = McLaunchArgumentsJvmOld(McVersionCurrent)
            McLaunchLog("旧版 JVM 参数获取成功：")
            McLaunchLog(Arguments)
        End If
        If McVersionCurrent.JsonObject("minecraftArguments") IsNot Nothing Then
            McLaunchLog("获取旧版 Game 参数")
            Arguments += " " & McLaunchArgumentsGameOld(McVersionCurrent)
            McLaunchLog("旧版 Game 参数获取成功")
        End If
        If McVersionCurrent.JsonObject("arguments") IsNot Nothing AndAlso McVersionCurrent.JsonObject("arguments")("game") IsNot Nothing Then
            McLaunchLog("获取新版 Game 参数")
            Arguments += " " & McLaunchArgumentsGameNew(McVersionCurrent)
            McLaunchLog("新版 Game 参数获取成功")
        End If
        '替换参数
        Dim ReplaceArguments = McLaunchArgumentsReplace(McVersionCurrent, Loader)
        If String.IsNullOrWhiteSpace(ReplaceArguments("${version_type}")) Then
            '若自定义信息为空，则去掉该部分
            Arguments = Arguments.Replace(" --versionType ${version_type}", "")
            ReplaceArguments("${version_type}") = """"""
        End If
        For Each entry As KeyValuePair(Of String, String) In ReplaceArguments
            Arguments = Arguments.Replace(entry.Key, If(entry.Value.Contains(" ") OrElse entry.Value.Contains(":\"), """" & entry.Value & """", entry.Value))
        Next
        'MJSB
        Arguments = Arguments.Replace(" -Dos.name=Windows 10", " -Dos.name=""Windows 10""")
        '全屏
        If Setup.Get("LaunchArgumentWindowType") = 0 Then Arguments += " --fullscreen"
        '进服
        Dim Server As String = If(String.IsNullOrEmpty(Loader.Input), Setup.Get("VersionServerEnter", McVersionCurrent), Loader.Input)
        If Server.Length > 0 Then
            If Server.Contains(":") Then
                '包含端口号
                Arguments += " --server " & Server.Split(":")(0) & " --port " & Server.Split(":")(1)
            Else
                '不包含端口号
                Arguments += " --server " & Server & " --port 25565"
            End If
            'OptiFine 警告
            If McVersionCurrent.Version.HasOptiFine Then
                Hint("OptiFine 与自动进入服务器可能不兼容，有概率导致材质丢失甚至游戏崩溃！", HintType.Critical)
            End If
        End If
        '自定义
        Dim ArgumentGame As String = Setup.Get("VersionAdvanceGame", Version:=McVersionCurrent)
        Arguments += " " & If(ArgumentGame = "", Setup.Get("LaunchAdvanceGame"), ArgumentGame)
        '输出
        McLaunchLog("Minecraft 启动参数：")
        McLaunchLog(Arguments.Replace(McLoginLoader.Output.AccessToken, McLoginLoader.Output.AccessToken.Substring(0, 22) & "[数据删除]" & McLoginLoader.Output.AccessToken.Substring(30)))
        McLaunchArgument = Arguments
    End Sub

    'Jvm 部分（第一段）
    Private Function McLaunchArgumentsJvmOld(Version As McVersion) As String
        '存储以空格为间隔的启动参数列表
        Dim DataList As New List(Of String)

        '输出固定参数
        DataList.Add("-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump")
        Dim ArgumentJvm As String = Setup.Get("VersionAdvanceJvm", Version:=McVersionCurrent)
        If ArgumentJvm = "" Then ArgumentJvm = Setup.Get("LaunchAdvanceJvm")
        If Not ArgumentJvm.Contains("-Dlog4j2.formatMsgNoLookups=true") Then ArgumentJvm += " -Dlog4j2.formatMsgNoLookups=true"
        DataList.Insert(0, ArgumentJvm) '可变 JVM 参数
        DataList.Add("-Xmn256m")
        DataList.Add("-Xmx" & Math.Floor(PageVersionSetup.GetRam(McVersionCurrent) * 1024) & "m")
        DataList.Add("""-Djava.library.path=" & Version.Path & Version.Name & "-natives""")
        DataList.Add("-cp ${classpath}") '把支持库添加进启动参数表

        '统一通行证
        If McLoginLoader.Output.Type = "Nide" Then
            DataList.Insert(0, "-Dnide8auth.client=true -javaagent:nide8auth.jar=" & Setup.Get("VersionServerNide", Version:=McVersionCurrent))
        End If
        'Authlib-Injector
        If McLoginLoader.Output.Type = "Auth" Then
            Dim Server As String = Setup.Get("VersionServerAuthServer", Version:=McVersionCurrent)
            Dim Response As String = NetGetCodeByRequestRetry(Server, Encoding.UTF8)
            DataList.Insert(0, "-javaagent:authlib-injector.jar=" & Server &
                              " -Dauthlibinjector.side=client" &
                              " -Dauthlibinjector.yggdrasil.prefetched=" & Convert.ToBase64String(Encoding.UTF8.GetBytes(Response)))
        End If

        '添加 MainClass
        If Version.JsonObject("mainClass") Is Nothing Then
            Throw New Exception("版本 Json 中没有 mainClass 项！")
        Else
            DataList.Add(Version.JsonObject("mainClass"))
        End If

        Return Join(DataList, " ")
    End Function
    Private Function McLaunchArgumentsJvmNew(Version As McVersion) As String
        Dim DataList As New List(Of String)

        '获取 Json 中的 DataList
        Dim CurrentVersion As McVersion = Version
NextVersion:
        If CurrentVersion.JsonObject("arguments") IsNot Nothing AndAlso CurrentVersion.JsonObject("arguments")("jvm") IsNot Nothing Then
            For Each SubJson As JToken In CurrentVersion.JsonObject("arguments")("jvm")
                If SubJson.Type = JTokenType.String Then
                    '字符串类型
                    DataList.Add(SubJson.ToString)
                Else
                    '非字符串类型
                    If McJsonRuleCheck(SubJson("rules")) Then
                        '满足准则
                        If SubJson("value").Type = JTokenType.String Then
                            DataList.Add(SubJson("value").ToString)
                        Else
                            For Each value As JToken In SubJson("value")
                                DataList.Add(value.ToString)
                            Next
                        End If
                    End If
                End If
            Next
        End If
        If CurrentVersion.InheritVersion <> "" Then
            CurrentVersion = New McVersion(CurrentVersion.InheritVersion)
            GoTo NextVersion
        End If

        '内存、Log4j 防御参数等
        SecretLaunchJvmArgs(DataList)

        '统一通行证
        If McLoginLoader.Output.Type = "Nide" Then
            DataList.Insert(0, "-javaagent:nide8auth.jar=" & Setup.Get("VersionServerNide", Version:=McVersionCurrent))
        End If
        'Authlib-Injector
        If McLoginLoader.Output.Type = "Auth" Then
            Dim Server As String = Setup.Get("VersionServerAuthServer", Version:=McVersionCurrent)
            Try
                Dim Response As String = NetGetCodeByRequestRetry(Server, Encoding.UTF8)
                DataList.Insert(0, "-javaagent:authlib-injector.jar=" & Server &
                              " -Dauthlibinjector.side=client" &
                              " -Dauthlibinjector.yggdrasil.prefetched=" & Convert.ToBase64String(Encoding.UTF8.GetBytes(Response)))
            Catch ex As Exception
                Throw New Exception("无法连接到第三方登录服务器（" & If(Server, Nothing) & "）")
            End Try
        End If

        '将 "-XXX" 与后面 "XXX" 合并到一起
        '如果不合并，会导致 Forge 1.17 启动无效，它有两个 --add-exports，进一步导致其中一个在后面被去重
        Dim DeDuplicateDataList As New List(Of String)
        For i = 0 To DataList.Count - 1
            Dim CurrentEntry As String = DataList(i)
            If DataList(i).StartsWith("-") Then
                Do While i < DataList.Count - 1
                    If DataList(i + 1).StartsWith("-") Then
                        Exit Do
                    Else
                        i += 1
                        CurrentEntry += " " + DataList(i)
                    End If
                Loop
            End If
            DeDuplicateDataList.Add(CurrentEntry.Trim.Replace("McEmu= ", "McEmu="))
        Next

        '去重
        Dim Result As String = Join(ArrayNoDouble(DeDuplicateDataList), " ")

        '添加 MainClass
        If Version.JsonObject("mainClass") Is Nothing Then
            Throw New Exception("版本 Json 中没有 mainClass 项！")
        Else
            Result += " " & Version.JsonObject("mainClass").ToString
        End If

        Return Result
    End Function

    'Game 部分（第二段）
    Private Function McLaunchArgumentsGameOld(Version As McVersion) As String
        Dim DataList As New List(Of String)

        '本地化 Minecraft 启动信息
        Dim BasicString As String = Version.JsonObject("minecraftArguments").ToString
        If Not BasicString.Contains("--height") Then BasicString += " --height ${resolution_height} --width ${resolution_width}"
        DataList.Add(BasicString)

        McLaunchArgumentsGameOld = Join(DataList, " ")

        '特别改变 OptiFineTweaker
        If (Version.Version.HasForge OrElse Version.Version.HasLiteLoader) AndAlso Version.Version.HasOptiFine Then
            '把 OptiFineForgeTweaker 放在最后，不然会导致崩溃！
            If McLaunchArgumentsGameOld.Contains("--tweakClass optifine.OptiFineForgeTweaker") Then
                Log("[Launch] 发现正确的 OptiFineForge TweakClass，目前参数：" & McLaunchArgumentsGameOld)
                McLaunchArgumentsGameOld = McLaunchArgumentsGameOld.Replace(" --tweakClass optifine.OptiFineForgeTweaker", "").Replace("--tweakClass optifine.OptiFineForgeTweaker ", "") & " --tweakClass optifine.OptiFineForgeTweaker"
            End If
            If McLaunchArgumentsGameOld.Contains("--tweakClass optifine.OptiFineTweaker") Then
                Log("[Launch] 发现错误的 OptiFineForge TweakClass，目前参数：" & McLaunchArgumentsGameOld)
                McLaunchArgumentsGameOld = McLaunchArgumentsGameOld.Replace(" --tweakClass optifine.OptiFineTweaker", "").Replace("--tweakClass optifine.OptiFineTweaker ", "") & " --tweakClass optifine.OptiFineForgeTweaker"
                Try
                    WriteFile(Version.Path & Version.Name & ".json", ReadFile(Version.Path & Version.Name & ".json").Replace("optifine.OptiFineTweaker", "optifine.OptiFineForgeTweaker"))
                Catch ex As Exception
                    Log(ex, "替换 OptiFineForge TweakClass 失败")
                End Try
            End If
        End If
    End Function
    Private Function McLaunchArgumentsGameNew(Version As McVersion) As String
        Dim DataList As New List(Of String)

        '获取 Json 中的 DataList
        Dim CurrentVersion As McVersion = Version
NextVersion:
        If CurrentVersion.JsonObject("arguments") IsNot Nothing AndAlso CurrentVersion.JsonObject("arguments")("game") IsNot Nothing Then
            For Each SubJson As JToken In CurrentVersion.JsonObject("arguments")("game")
                If SubJson.Type = JTokenType.String Then
                    '字符串类型
                    DataList.Add(SubJson.ToString)
                Else
                    '非字符串类型
                    If McJsonRuleCheck(SubJson("rules")) Then
                        '满足准则
                        If SubJson("value").Type = JTokenType.String Then
                            DataList.Add(SubJson("value").ToString)
                        Else
                            For Each value As JToken In SubJson("value")
                                DataList.Add(value.ToString)
                            Next
                        End If
                    End If
                End If
            Next
        End If
        If CurrentVersion.InheritVersion <> "" Then
            CurrentVersion = New McVersion(CurrentVersion.InheritVersion)
            GoTo NextVersion
        End If

        '将 "-XXX" 与后面 "XXX" 合并到一起
        '如果不进行合并 Impact 会启动无效，它有两个 --tweakclass
        Dim DeDuplicateDataList As New List(Of String)
        For i = 0 To DataList.Count - 1
            Dim CurrentEntry As String = DataList(i)
            If DataList(i).StartsWith("-") Then
                Do While i < DataList.Count - 1
                    If DataList(i + 1).StartsWith("-") Then
                        Exit Do
                    Else
                        i += 1
                        CurrentEntry += " " + DataList(i)
                    End If
                Loop
            End If
            DeDuplicateDataList.Add(CurrentEntry)
        Next
        '去重
        McLaunchArgumentsGameNew = Join(ArrayNoDouble(DeDuplicateDataList), " ")

        '特别改变 OptiFineTweaker
        If (Version.Version.HasForge OrElse Version.Version.HasLiteLoader) AndAlso Version.Version.HasOptiFine Then
            '把 OptiFineForgeTweaker 放在最后，不然会导致崩溃！
            If McLaunchArgumentsGameNew.Contains("--tweakClass optifine.OptiFineForgeTweaker") Then
                Log("[Launch] 发现正确的 OptiFineForge TweakClass，目前参数：" & McLaunchArgumentsGameNew)
                McLaunchArgumentsGameNew = McLaunchArgumentsGameNew.Replace(" --tweakClass optifine.OptiFineForgeTweaker", "").Replace("--tweakClass optifine.OptiFineForgeTweaker ", "") & " --tweakClass optifine.OptiFineForgeTweaker"
            End If
            If McLaunchArgumentsGameNew.Contains("--tweakClass optifine.OptiFineTweaker") Then
                Log("[Launch] 发现错误的 OptiFineForge TweakClass，目前参数：" & McLaunchArgumentsGameNew)
                McLaunchArgumentsGameNew = McLaunchArgumentsGameNew.Replace(" --tweakClass optifine.OptiFineTweaker", "").Replace("--tweakClass optifine.OptiFineTweaker ", "") & " --tweakClass optifine.OptiFineForgeTweaker"
                Try
                    WriteFile(Version.Path & Version.Name & ".json", ReadFile(Version.Path & Version.Name & ".json").Replace("optifine.OptiFineTweaker", "optifine.OptiFineForgeTweaker"))
                Catch ex As Exception
                    Log(ex, "替换 OptiFineForge TweakClass 失败")
                End Try
            End If
        End If
    End Function

    '替换 Arguments
    Private Function McLaunchArgumentsReplace(Version As McVersion, ByRef Loader As LoaderTask(Of String, List(Of McLibToken))) As Dictionary(Of String, String)
        Dim GameArguments As New Dictionary(Of String, String)

        '基础参数
        GameArguments.Add("${classpath_separator}", ";")
        GameArguments.Add("${natives_directory}", Version.Path & Version.Name & "-natives")
        GameArguments.Add("${library_directory}", PathMcFolder & "libraries")
        GameArguments.Add("${libraries_directory}", PathMcFolder & "libraries")
        GameArguments.Add("${launcher_name}", "PCL2")
        GameArguments.Add("${launcher_version}", VersionCode)
        GameArguments.Add("${version_name}", Version.Name)
        Dim ArgumentInfo As String = Setup.Get("VersionArgumentInfo", Version:=McVersionCurrent)
        GameArguments.Add("${version_type}", If(ArgumentInfo = "", Setup.Get("LaunchArgumentInfo"), ArgumentInfo))
        GameArguments.Add("${game_directory}", Left(McVersionCurrent.PathIndie, McVersionCurrent.PathIndie.Count - 1))
        GameArguments.Add("${assets_root}", PathMcFolder & "assets")
        GameArguments.Add("${user_properties}", "{}")
        GameArguments.Add("${auth_player_name}", McLoginLoader.Output.Name)
        GameArguments.Add("${auth_uuid}", McLoginLoader.Output.Uuid)
        GameArguments.Add("${auth_access_token}", McLoginLoader.Output.AccessToken)
        GameArguments.Add("${access_token}", McLoginLoader.Output.AccessToken)
        GameArguments.Add("${auth_session}", McLoginLoader.Output.AccessToken)
        GameArguments.Add("${user_type}", If(McLoginLoader.Output.Type = "Legacy", "Legacy", "Mojang"))
        Dim GameSize As Size = Setup.GetLaunchArgumentWindowSize()
        GameArguments.Add("${resolution_width}", GameSize.Width)
        GameArguments.Add("${resolution_height}", GameSize.Height)

        'Assets 相关参数
        GameArguments.Add("${game_assets}", PathMcFolder & "assets\virtual\legacy") '1.5.2 的 pre-1.6 资源索引应与 legacy 合并
        GameArguments.Add("${assets_index_name}", McAssetsGetIndexName(Version))

        '支持库参数
        Dim LibList As List(Of McLibToken) = McLibListGet(Version, Not (Version.Version.HasForge AndAlso Version.Version.McCodeMain >= 17))
        Loader.Output = LibList
        Dim CpStrings As New List(Of String)
        Dim OptiFineCp As String = Nothing
        For Each Library As McLibToken In LibList
            If Library.IsNatives Then Continue For
            If Library.Name IsNot Nothing AndAlso Library.Name = "optifine:OptiFine" Then
                OptiFineCp = Library.LocalPath
            Else
                CpStrings.Add(Library.LocalPath)
            End If
        Next
        If OptiFineCp IsNot Nothing Then CpStrings.Insert(CpStrings.Count - 2, OptiFineCp)
        GameArguments.Add("${classpath}", Join(CpStrings, ";"))

        Return GameArguments
    End Function

#End Region

#Region "解压 Natives"

    Private Sub McLaunchNatives(Loader As LoaderTask(Of List(Of McLibToken), Integer))

        '创建文件夹
        Directory.CreateDirectory(McVersionCurrent.Path & McVersionCurrent.Name & "-natives")

        '解压文件
        McLaunchLog("正在解压 Natives 文件")
        Dim ExistFiles As New List(Of String)
        For Each Native As McLibToken In Loader.Input
            If Not Native.IsNatives Then Continue For
            Dim Zip As ZipArchive
            Try
                Zip = New ZipArchive(New FileStream(Native.LocalPath, FileMode.Open))
            Catch ex As InvalidDataException
                Log(ex, "打开 Natives 文件失败（" & Native.LocalPath & "）")
                File.Delete(Native.LocalPath)
                Throw New Exception("无法打开 Natives 文件（" & Native.LocalPath & "），该文件可能已损坏，请重新尝试启动游戏")
            End Try
            For Each Entry In Zip.Entries
                Dim FileName As String = Entry.FullName
                If FileName.EndsWith(".dll") Then
                    '实际解压文件的步骤
                    Dim FilePath As String = McVersionCurrent.Path & McVersionCurrent.Name & "-natives\" & FileName
                    ExistFiles.Add(FilePath)
                    Dim OriginalFile As New FileInfo(FilePath)
                    If OriginalFile.Exists Then
                        If OriginalFile.Length = Entry.Length Then
                            If ModeDebug Then McLaunchLog("无需解压：" & FilePath)
                            Continue For
                        End If
                        '删除原文件
                        Try
                            File.Delete(FilePath)
                        Catch ex As UnauthorizedAccessException
                            McLaunchLog("删除原 dll 访问被拒绝，这通常代表有一个 MC 正在运行，跳过解压：" & FilePath)
                            McLaunchLog("实际的错误信息：" & GetString(ex))
                            Exit For
                        End Try
                    End If
                    '解压新文件
                    WriteFile(FilePath, Entry.Open)
                    McLaunchLog("已解压：" & FilePath)
                End If
            Next
            If Zip IsNot Nothing Then Zip.Dispose()
        Next

        '删除多余文件
        For Each FileName As String In Directory.GetFiles(McVersionCurrent.Path & McVersionCurrent.Name & "-natives")
            If ExistFiles.Contains(FileName) Then Continue For
            Try
                McLaunchLog("删除：" & FileName)
                File.Delete(FileName)
            Catch ex As UnauthorizedAccessException
                McLaunchLog("删除多余文件访问被拒绝，跳过删除步骤")
                McLaunchLog("实际的错误信息：" & GetString(ex))
                Exit Sub
            End Try
        Next

    End Sub

#End Region

#Region "启动与前后处理"

    Private Sub McLaunchPrerun()

        '更新 launcher_profiles.json
        Try
            '确保可用
            If Not (McLoginLoader.Output.Type = "Mojang" OrElse McLoginLoader.Output.Type = "Microsoft") Then Exit Try
            McFolderLauncherProfilesJsonCreate(PathMcFolder)
            '构建需要替换的 Json 对象
            Dim ReplaceJsonString As String = "
            {
              ""authenticationDatabase"": {
                ""00000111112222233333444445555566"": {
                  ""accessToken"": """ & McLoginLoader.Output.AccessToken & """,
                  ""username"": """ & If(McLoginLoader.Output.Email, McLoginLoader.Output.Name).Replace("""", "-") & """,
                  ""profiles"": {
                    ""66666555554444433333222221111100"": {
                        ""displayName"": """ & McLoginLoader.Output.Name & """
                    }
                  }
                }
              },
              ""clientToken"": """ & McLoginLoader.Output.ClientToken & """,
              ""selectedUser"": {
                ""account"": ""00000111112222233333444445555566"", 
                ""profile"": ""66666555554444433333222221111100""
              }
            }"
            Dim ReplaceJson As JObject = GetJson(ReplaceJsonString)
            '更新文件
            Dim Profiles As JObject = GetJson(ReadFile(PathMcFolder & "launcher_profiles.json"))
            Profiles.Merge(ReplaceJson)
            WriteFile(PathMcFolder & "launcher_profiles.json", Profiles.ToString, Encoding:=Encoding.Default)
            McLaunchLog("已更新 launcher_profiles.json")
        Catch ex As Exception
            Log(ex, "更新 launcher_profiles.json 失败，将在删除文件后重试")
            Try
                File.Delete(PathMcFolder & "launcher_profiles.json")
                McFolderLauncherProfilesJsonCreate(PathMcFolder)
                '构建需要替换的 Json 对象
                Dim ReplaceJsonString As String = "
                    {
                      ""authenticationDatabase"": {
                        ""00000111112222233333444445555566"": {
                          ""accessToken"": """ & McLoginLoader.Output.AccessToken & """,
                          ""username"": """ & If(McLoginLoader.Output.Email, McLoginLoader.Output.Name).Replace("""", "-") & """,
                          ""profiles"": {
                            ""66666555554444433333222221111100"": {
                                ""displayName"": """ & McLoginLoader.Output.Name & """
                            }
                          }
                        }
                      },
                      ""clientToken"": """ & McLoginLoader.Output.ClientToken & """,
                      ""selectedUser"": {
                        ""account"": ""00000111112222233333444445555566"", 
                        ""profile"": ""66666555554444433333222221111100""
                      }
                    }"
                Dim ReplaceJson As JObject = GetJson(ReplaceJsonString)
                '更新文件
                Dim Profiles As JObject = GetJson(ReadFile(PathMcFolder & "launcher_profiles.json"))
                Profiles.Merge(ReplaceJson)
                WriteFile(PathMcFolder & "launcher_profiles.json", Profiles.ToString, Encoding:=Encoding.Default)
                McLaunchLog("已在删除后更新 launcher_profiles.json")
            Catch exx As Exception
                Log(exx, "更新 launcher_profiles.json 失败", LogLevel.Feedback)
            End Try
        End Try

        '更新 options.txt
        Dim SetupFileAddress As String = McVersionCurrent.PathIndie & "options.txt"
        Try
            '语言
            If Setup.Get("ToolHelpChinese") Then
                If Not File.Exists(SetupFileAddress) OrElse Not Directory.Exists(McVersionCurrent.PathIndie & "saves") Then
                    McLaunchLog("已根据设置自动修改语言为中文")
                    WriteIni(SetupFileAddress, "lang", "-") '触发缓存更改，避免删除后重新下载残留缓存
                    If McVersionCurrent.Version.McCodeMain >= 12 Then
                        WriteIni(SetupFileAddress, "lang", "zh_cn")
                    Else
                        WriteIni(SetupFileAddress, "lang", "zh_CN")
                    End If
                    WriteIni(SetupFileAddress, "forceUnicodeFont", "true")
                Else
                    McLaunchLog("并非首次启动，不修改语言")
                End If
            End If
            '窗口
            Select Case Setup.Get("LaunchArgumentWindowType")
                Case 0 '全屏
                    WriteIni(SetupFileAddress, "fullscreen", "true")
                Case 5 '保持默认
                Case Else '其他
                    WriteIni(SetupFileAddress, "fullscreen", "false")
            End Select
        Catch ex As Exception
            Log(ex, "更新 options.txt 失败", LogLevel.Hint)
        End Try

        '离线皮肤 Alex 警告
        Try
            If McVersionCurrent.Version.McCodeMain <= 7 AndAlso McVersionCurrent.Version.McCodeMain >= 2 AndAlso '1.7 ~ 1.2
               McLoginLoader.Input.Type = McLoginType.Legacy AndAlso '离线登录
               (Setup.Get("LaunchSkinType") = 2 OrElse '强制 Alex
               (Setup.Get("LaunchSkinType") = 4 AndAlso Setup.Get("LaunchSkinSlim"))) Then '或选用 Alex 皮肤
                Hint("此 Minecraft 版本尚不支持 Alex 皮肤，你的皮肤可能会显示为 Steve！", HintType.Critical)
            End If
        Catch ex As Exception
            Log(ex, "检查离线皮肤失效失败")
        End Try

        '离线皮肤资源包
        Try
            Directory.CreateDirectory(McVersionCurrent.PathIndie & "resourcepacks\")
            Dim ZipFileAddress As String = McVersionCurrent.PathIndie & "resourcepacks\PCL2 Skin.zip"
            Dim NewTypeSetup As Boolean = McVersionCurrent.Version.McCodeMain >= 13 OrElse McVersionCurrent.Version.McCodeMain < 6
            If McLoginLoader.Input.Type = McLoginType.Legacy AndAlso Setup.Get("LaunchSkinType") = 4 AndAlso File.Exists(PathTemp & "CustomSkin.png") Then
                Directory.CreateDirectory(PathTemp)
                Dim MetaFileAddress As String = PathTemp & "pack.mcmeta"
                Dim PackPicAddress As String = PathTemp & "pack.png"
                Dim PackFormat As Integer
                Select Case McVersionCurrent.Version.McCodeMain
                    Case 0, 1, 2, 3, 4, 5
                        '更早的版本没有资源包；如果判断失败该值为 -1，不会跑到这
                        McLaunchLog("Minecraft 版本过老，尚不支持自定义离线皮肤")
                        GoTo IgnoreCustomSkin
                    Case 6, 7, 8
                        PackFormat = 1
                    Case 9, 10
                        PackFormat = 2
                    Case 11, 12
                        PackFormat = 3
                    Case 13, 14
                        PackFormat = 4
                    Case 15
                        PackFormat = 5
                    Case 16
                        PackFormat = 6
                    Case 17
                        PackFormat = 7
                    Case 18
                        If McVersionCurrent.Version.McCodeSub <= 2 Then
                            PackFormat = 8
                        Else
                            PackFormat = 9
                        End If
                    Case 19
                        PackFormat = 9
                    Case Else
                        PackFormat = 10
                End Select
                McLaunchLog("正在构建自定义皮肤资源包，格式为：" & PackFormat)
                '准备文件
                Dim Bit As New MyBitmap(PathImage & "Heads/Logo.png")
                Bit.Save(PackPicAddress)
                WriteFile(MetaFileAddress, "{""pack"":{""pack_format"":" & PackFormat & ",""description"":""PCL2 自定义离线皮肤资源包""}}")
                Dim Skin As New MyBitmap(PathTemp & "CustomSkin.png")
                If (McVersionCurrent.Version.McCodeMain = 6 OrElse McVersionCurrent.Version.McCodeMain = 7) AndAlso Skin.Pic.Height = 64 Then
                    McLaunchLog("该 Minecraft 版本不支持双层皮肤，已进行裁剪")
                    Skin = Skin.Clip(New System.Drawing.Rectangle(0, 0, 64, 32))
                End If
                Skin.Save(Path & "PCL\CustomSkin_Cliped.png")
                '构建压缩文件
                Using ZipFile As New FileStream(ZipFileAddress, FileMode.Create)
                    Using ZipAr As New ZipArchive(ZipFile, ZipArchiveMode.Create)
                        ZipAr.CreateEntryFromFile(MetaFileAddress, "pack.mcmeta")
                        ZipAr.CreateEntryFromFile(PackPicAddress, "pack.png")
                        ZipAr.CreateEntryFromFile(Path & "PCL\CustomSkin_Cliped.png", "assets/minecraft/textures/entity/" & If(Setup.Get("LaunchSkinSlim"), "alex.png", "steve.png"))
                    End Using
                End Using
                File.Delete(Path & "PCL\CustomSkin_Cliped.png")
                '更改设置文件
                IniClearCache(SetupFileAddress)
                Dim EnabledResourcePack As String = ReadIni(SetupFileAddress, "resourcePacks", "[]").TrimStart("[").TrimEnd("]")
                If NewTypeSetup Then
                    If EnabledResourcePack = "" Then EnabledResourcePack = """vanilla"""
                    Dim EnabledResourcePacks As New List(Of String)(EnabledResourcePack.Split(","))
                    Dim NewResourcePacks As New List(Of String)
                    For Each Res In EnabledResourcePacks
                        If Res <> """file/PCL2 Skin.zip""" AndAlso Res <> "" Then NewResourcePacks.Add(Res)
                    Next
                    NewResourcePacks.Add("""file/PCL2 Skin.zip""")
                    Dim Result As String = "[" & Join(NewResourcePacks, ",") & "]"
                    WriteIni(SetupFileAddress, "resourcePacks", Result)
                Else
                    Dim EnabledResourcePacks As New List(Of String)(EnabledResourcePack.Split(","))
                    Dim NewResourcePacks As New List(Of String)
                    For Each Res In EnabledResourcePacks
                        If Res <> """PCL2 Skin.zip""" AndAlso Res <> "" Then NewResourcePacks.Add(Res)
                    Next
                    NewResourcePacks.Add("""PCL2 Skin.zip""")
                    Dim Result As String = "[" & Join(NewResourcePacks, ",") & "]"
                    WriteIni(SetupFileAddress, "resourcePacks", Result)
                End If
IgnoreCustomSkin:
            ElseIf File.Exists(ZipFileAddress) Then
                McLaunchLog("正在清空自定义皮肤资源包")
                '删除压缩文件
                File.Delete(ZipFileAddress)
                '更改设置文件
                IniClearCache(SetupFileAddress)
                Dim EnabledResourcePack As String = ReadIni(SetupFileAddress, "resourcePacks", "[]").TrimStart("[").TrimEnd("]")
                If NewTypeSetup Then
                    If EnabledResourcePack = "" Then EnabledResourcePack = """vanilla"""
                    Dim EnabledResourcePacks As New List(Of String)(EnabledResourcePack.Split(","))
                    EnabledResourcePacks.Remove("""file/PCL2 Skin.zip""")
                    Dim Result As String = "[" & Join(EnabledResourcePacks, ",") & "]"
                    WriteIni(SetupFileAddress, "resourcePacks", Result)
                Else
                    Dim EnabledResourcePacks As New List(Of String)(EnabledResourcePack.Split(","))
                    EnabledResourcePacks.Remove("""PCL2 Skin.zip""")
                    Dim Result As String = "[" & Join(EnabledResourcePacks, ",") & "]"
                    WriteIni(SetupFileAddress, "resourcePacks", Result)
                End If
            End If
        Catch ex As Exception
            Log(ex, "离线皮肤资源包设置失败", LogLevel.Hint)
        End Try

    End Sub
    Private Sub McLaunchRun(Loader As LoaderTask(Of Integer, Process))

        '启动信息
        Dim GameProcess = New Process()
        Dim StartInfo As New ProcessStartInfo(McLaunchJavaSelected.PathJavaw)

        '设置环境变量
        If StartInfo.EnvironmentVariables.ContainsKey("appdata") Then
            StartInfo.EnvironmentVariables("appdata") = PathMcFolder
        Else
            StartInfo.EnvironmentVariables.Add("appdata", PathMcFolder)
        End If
        Dim Paths As New List(Of String)(StartInfo.EnvironmentVariables("Path").Split(";"))
        Paths.Add(McLaunchJavaSelected.PathFolder)
        StartInfo.EnvironmentVariables("Path") = Join(ArrayNoDouble(Paths), ";")

        '设置其他参数
        StartInfo.WorkingDirectory = McVersionCurrent.PathIndie
        StartInfo.UseShellExecute = False
        StartInfo.RedirectStandardOutput = True
        StartInfo.RedirectStandardError = True
        StartInfo.CreateNoWindow = False
        StartInfo.Arguments = McLaunchArgument
        GameProcess.StartInfo = StartInfo

        '开始进程
        GameProcess.Start()
        McLaunchLog("已启动游戏进程：" & McLaunchJavaSelected.PathJavaw)
        Loader.Output = GameProcess
        McLaunchProcess = GameProcess

        '输出 bat
        Try
            Dim CmdString As String =
                "@echo off" & vbCrLf &
                "title 启动 - " & McVersionCurrent.Name & vbCrLf &
                "echo 游戏正在启动，请稍候。" & vbCrLf &
                "set APPDATA=""" & PathMcFolder & """" & vbCrLf &
                "cd /D """ & PathMcFolder & """" & vbCrLf &
                """" & McLaunchJavaSelected.PathJava & """ " & McLaunchArgument & vbCrLf &
                "echo 游戏已退出。" & vbCrLf &
                "pause"
            WriteFile(Path & "PCL\LatestLaunch.bat", CmdString, Encoding:=Encoding.Default)
        Catch ex As Exception
            Log(ex, "输出启动脚本失败")
        End Try

        '进程优先级处理
        Try
            If GameProcess.HasExited Then Exit Try '可能在启动游戏进程的同时刚好取消了启动
            GameProcess.PriorityBoostEnabled = True
            Select Case Setup.Get("LaunchArgumentPriority")
                Case 0 '高
                    GameProcess.PriorityClass = ProcessPriorityClass.AboveNormal
                Case 2 '低
                    GameProcess.PriorityClass = ProcessPriorityClass.BelowNormal
                Case Else '中
            End Select
        Catch ex As Exception
            Log(ex, "设置进程优先级失败", LogLevel.Feedback)
        End Try

    End Sub
    Private Sub McLaunchWait(Loader As LoaderTask(Of Process, Integer))

        '输出信息
        McLaunchLog("")
        McLaunchLog("~ 基础参数 ~")
        McLaunchLog("PCL2 版本：" & VersionDisplayName & " (" & VersionCode & ")")
        McLaunchLog("游戏版本：" & McVersionCurrent.Version.ToString & "（" & McVersionCurrent.Version.McCodeMain & "." & McVersionCurrent.Version.McCodeSub & "）")
        McLaunchLog("资源版本：" & McAssetsGetIndexName(McVersionCurrent))
        McLaunchLog("版本继承：" & If(McVersionCurrent.InheritVersion = "", "无", McVersionCurrent.InheritVersion))
        McLaunchLog("分配的内存：" & PageVersionSetup.GetRam(McVersionCurrent) & " GB（" & Math.Round(PageVersionSetup.GetRam(McVersionCurrent) * 1024) & " MB）")
        McLaunchLog("MC 文件夹：" & PathMcFolder)
        McLaunchLog("版本文件夹：" & McVersionCurrent.Path)
        McLaunchLog("版本隔离：" & (McVersionCurrent.PathIndie = McVersionCurrent.Path))
        McLaunchLog("HMCL 格式：" & McVersionCurrent.IsHmclFormatJson)
        McLaunchLog("Java 信息：" & If(McLaunchJavaSelected IsNot Nothing, McLaunchJavaSelected.ToString, "无可用 Java"))
        McLaunchLog("环境变量：" & If(McLaunchJavaSelected IsNot Nothing, If(McLaunchJavaSelected.HasEnvironment, "已设置", "未设置"), "未设置"))
        McLaunchLog("")
        McLaunchLog("~ 登录参数 ~")
        McLaunchLog("玩家用户名：" & McLoginLoader.Output.Name)
        McLaunchLog("AccessToken：" & McLoginLoader.Output.AccessToken.Substring(0, 22) & "[数据删除]" & McLoginLoader.Output.AccessToken.Substring(30))
        McLaunchLog("ClientToken：" & McLoginLoader.Output.ClientToken)
        McLaunchLog("UUID：" & McLoginLoader.Output.Uuid)
        McLaunchLog("登录方式：" & McLoginLoader.Output.Type)
        McLaunchLog("")

        '获取窗口标题
        Dim WindowTitle As String = Setup.Get("VersionArgumentTitle", Version:=McVersionCurrent)
        If WindowTitle = "" Then WindowTitle = Setup.Get("LaunchArgumentTitle")
        WindowTitle = ArgumentReplace(WindowTitle)

        '初始化等待
        Dim Watcher As New Watcher(Loader, McVersionCurrent, WindowTitle)
        McLaunchWatcher = Watcher

        '等待
        Do While Watcher.State = Watcher.MinecraftState.Loading
            Thread.Sleep(100)
        Loop
        If Watcher.State = Watcher.MinecraftState.Crashed Then
            Throw New Exception("$$")
        End If

    End Sub
    Private Sub McLaunchEnd()
        McLaunchLog("开始启动结束处理")

        '暂停或开始音乐播放
        If Setup.Get("UiMusicStop") Then
            RunInUi(Sub() If MusicPause() Then Log("[Music] 已根据设置，在启动后暂停音乐播放"))
        ElseIf Setup.Get("UiMusicStart") Then
            RunInUi(Sub() If MusicResume() Then Log("[Music] 已根据设置，在启动后开始音乐播放"))
        End If

        '启动器可见性
        McLaunchLog("启动器可见性：" & Setup.Get("LaunchArgumentVisible"))
        Select Case Setup.Get("LaunchArgumentVisible")
            Case 0
                '直接关闭
                McLaunchLog("已根据设置，在启动后关闭启动器")
                RunInUi(Sub() FrmMain.EndProgram(False))
            Case 2, 3
                '隐藏
                McLaunchLog("已根据设置，在启动后隐藏启动器")
                RunInUi(Sub() FrmMain.Hidden = True)
            Case 4
                '最小化
                McLaunchLog("已根据设置，在启动后最小化启动器")
                RunInUi(Sub() FrmMain.WindowState = WindowState.Minimized)
            Case 5
                '啥都不干
        End Select

        '启动计数
        Setup.Set("SystemLaunchCount", Setup.Get("SystemLaunchCount") + 1)

    End Sub

#End Region

End Module
