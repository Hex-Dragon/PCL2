Imports PCL.ModLaunch

Class PageLoginProfileSkin
    Public Sub New()
        InitializeComponent()
        'Skin.Loader = PageLaunchLeft.SkinLegacy
    End Sub
    Private Sub PageLoginProfile_Loaded() Handles Me.Loaded
        'Skin.Loader.Start()
    End Sub
    Public Shared SelectedProfile As JObject = Nothing
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        Log("[Profile] 刷新档案界面")
        SelectedProfile = PageLoginProfile.SelectedProfile
        If SelectedProfile("type").ToString = "offline" Then
            Log("[Profile] 使用离线皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinLegacy
        ElseIf SelectedProfile("type").ToString = "microsoft" Then
            Log("[Profile] 使用正版皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinMs
        ElseIf SelectedProfile("type").ToString = "authlib" Then
            Log("[Profile] 使用 Authlib 皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinAuth
        Else
            Log("[Profile] 使用离线皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinLegacy
        End If
        Skin.Loader.WaitForExit(IsForceRestart:=True)
        Skin.Clear()
        RunInNewThread(Sub() Skin.Loader.Start())
        TextName.Text = SelectedProfile("username").ToString
        TextType.Text = GetProfileInfo(SelectedProfile("type").ToString)
    End Sub
    Public Shared Function GetProfileInfo(Type As String, Optional Desc As String = Nothing)
        Dim Info As String = Nothing
        If Type = "offline" Then Info += "离线验证"
        If Type = "microsoft" Then Info += "正版验证"
        If Type = "authlib" Then
            Info += "第三方验证"
            If Not SelectedProfile("serverName") = "" Then Info += $" / {SelectedProfile("serverName")}"
        End If
        If Desc IsNot Nothing AndAlso Not Desc = "" Then Info += $"，{Desc}"
        Return Info
    End Function
    Private Sub ChangeProfile(sender As Object, e As EventArgs) Handles BtnSelect.Click
        '选择档案
        PageLoginProfile.IsProfileSelected = False
        PageLoginProfile.SelectedProfile = Nothing
        PageLoginProfile.LastUsedProfile = Nothing
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
    End Sub
    Private Sub PageLoginProfile_MouseEnter(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        AniStart({
                 AaOpacity(BtnSelect, 1 - BtnSelect.Opacity, 200),
                 AaOpacity(BtnSelect, 1 - BtnSelect.Opacity, 200)
        }, "PageLoginProfile Button")
    End Sub
    Private Sub PageLoginProfile_MouseLeave(sender As Object, e As MouseEventArgs) Handles PanData.MouseLeave
        AniStart({
                 AaOpacity(BtnSelect, -BtnSelect.Opacity, 200,, New AniEaseOutFluent),
                 AaOpacity(BtnSelect, -BtnSelect.Opacity, 200,, New AniEaseOutFluent)
        }, "PageLoginProfile Button")
    End Sub

    Private Sub Skin_Click(sender As Object, e As MouseButtonEventArgs) Handles Skin.Click
        Dim Address As String = If(McVersionCurrent IsNot Nothing, Setup.Get("VersionServerAuthRegister", Version:=McVersionCurrent), Setup.Get("CacheAuthServerRegister"))
        If String.IsNullOrEmpty(New ValidateHttp().Validate(Address)) Then OpenWebsite(Address.Replace("/auth/register", "/user/closet"))
    End Sub
#Region "控制"
    ''显示/隐藏控制
    'Private Sub ShowPanel(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
    '    AniStart(AaOpacity(PanButtons, 1 - PanButtons.Opacity, 120), "PageLoginMsSkin Button")
    'End Sub
    'Public Sub HidePanel() Handles PanData.MouseLeave
    '    If BtnEdit.ContextMenu.IsOpen OrElse BtnSkin.ContextMenu.IsOpen OrElse PanData.IsMouseOver Then Exit Sub
    '    AniStart(AaOpacity(PanButtons, -PanButtons.Opacity, 120), "PageLoginMsSkin Button")
    'End Sub
    ''修改账号信息
    'Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles BtnEdit.Click
    '    BtnEdit.ContextMenu.IsOpen = True
    'End Sub
    'Public Sub BtnEditPassword_Click(sender As Object, e As RoutedEventArgs)
    '    OpenWebsite("https://account.live.com/password/Change")
    'End Sub
    'Public Sub BtnEditName_Click(sender As Object, e As RoutedEventArgs)
    '    OpenWebsite("https://www.minecraft.net/zh-hans/msaprofile/mygames/editprofile")
    'End Sub

    ''退出登录
    'Private Sub BtnExit_Click() Handles BtnExit.Click
    '    Setup.Set("CacheMsV2OAuthRefresh", "")
    '    Setup.Set("CacheMsV2Access", "")
    '    Setup.Set("CacheMsV2ProfileJson", "")
    '    Setup.Set("CacheMsV2Uuid", "")
    '    Setup.Set("CacheMsV2Name", "")
    '    McLoginMsLoader.Abort()
    '    FrmLaunchLeft.RefreshPage(False, True)
    'End Sub
#End Region
    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData()
        Dim LoginType As String = Nothing
        If SelectedProfile IsNot Nothing Then
            LoginType = SelectedProfile("type").ToString
        Else
            SelectedProfile = PageLoginProfile.ProfileList(PageLoginProfile.LastUsedProfile)
            LoginType = SelectedProfile("type").ToString
        End If
        If LoginType = "authlib" Then
            Return New McLoginServer(McLoginType.Auth) With {.Token = "Auth",
                .BaseUrl = SelectedProfile("server"),
                .UserName = SelectedProfile("name"),
                .Password = SelectedProfile("password"),
                .Description = "Authlib-Injector",
                .Type = McLoginType.Auth,
                .IsExist = True
            }
        ElseIf LoginType = "microsoft" Then
            Return New McLoginMs With {.OAuthRefreshToken = SelectedProfile("refreshToken"),
                .UserName = SelectedProfile("username"),
                .AccessToken = SelectedProfile("accessToken"),
                .Uuid = SelectedProfile("uuid")
            }
        Else
            Return New McLoginLegacy With {.UserName = SelectedProfile("username"), .Uuid = SelectedProfile("uuid")}
        End If
        Return Nothing
    End Function
    Public Shared Function IsVaild() As String
        If SelectedProfile("type").ToString = "offline" Then
            If SelectedProfile("username").ToString.Trim = "" Then Return "玩家名不能为空！"
            If SelectedProfile("username").ToString.Contains("""") Then Return "玩家名不能包含英文引号！"
            If McVersionCurrent IsNot Nothing AndAlso
               ((McVersionCurrent.Version.McCodeMain = 20 AndAlso McVersionCurrent.Version.McCodeSub >= 3) OrElse McVersionCurrent.Version.McCodeMain > 20) AndAlso
               SelectedProfile("username").ToString.Trim.Length > 16 Then
                Return "自 1.20.3 起，玩家名至多只能包含 16 个字符！"
            End If
        End If
        Return ""
    End Function
    Private Sub Skin_Click(sender As Object, e As RoutedEventArgs) Handles Skin.Click
        If SelectedProfile("type") = "microsoft" Then
            If FrmLoginMsSkin Is Nothing Then FrmLoginMsSkin = New PageLoginMsSkin
            FrmLoginMsSkin.BtnSkinEdit_Click(SelectedProfile, e)
        ElseIf SelectedProfile("type") = "authlib" Then
            Dim Server As String = SelectedProfile("server")
            OpenWebsite(Server.ToString.Replace("/api/yggdrasil/authserver" + If(Server.EndsWithF("/"), "/", ""), "/user/closet"))
        Else
            Hint("当前档案不支持修改皮肤！")
        End If
    End Sub
End Class
