Class PageLoginProfileSkin
    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload() Handles Me.Loaded
        Log("[Profile] 刷新档案界面")
        Skin.Clear()
        If SelectedProfile.Type = McLoginType.Ms Then
            BtnEdit.Visibility = Visibility.Visible
            Log("[Profile] 使用正版皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinMs
        ElseIf SelectedProfile.Type = McLoginType.Auth Then
            BtnEdit.Visibility = Visibility.Visible
            Log("[Profile] 使用 Authlib 皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinAuth
        Else
            BtnEdit.Visibility = Visibility.Collapsed
            Log("[Profile] 使用离线皮肤加载器")
            Skin.Loader = PageLaunchLeft.SkinLegacy
        End If
        Skin.Loader.Start(IsForceRestart:=True)
        TextName.Text = SelectedProfile.Username
        TextType.Text = GetProfileInfo(SelectedProfile)
    End Sub

#Region "控制与编辑"
    '显示 / 隐藏控制
    Private Sub ShowPanel(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        AniStart(AaOpacity(PanButtons, 1 - PanButtons.Opacity, 120), "PageLoginProfileSkin Button")
    End Sub
    Public Sub HidePanel() Handles PanData.MouseLeave
        If BtnEdit.ContextMenu.IsOpen OrElse BtnSkin.ContextMenu.IsOpen OrElse PanData.IsMouseOver Then Exit Sub
        AniStart(AaOpacity(PanButtons, -PanButtons.Opacity, 120), "PageLoginProfileSkin Button")
    End Sub
    '皮肤与披风子菜单
    Private Sub BtnSkin_Click(sender As Object, e As RoutedEventArgs) Handles BtnSkin.Click
        BtnSkin.ContextMenu.IsOpen = True
    End Sub
    '账号信息子菜单
    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles BtnEdit.Click
        BtnEdit.ContextMenu.IsOpen = True
    End Sub
    '修改密码
    Public Sub BtnEditPassword_Click(sender As Object, e As RoutedEventArgs)
        If SelectedProfile.Type = 5 Then
            OpenWebsite("https://account.live.com/password/Change")
        ElseIf SelectedProfile.Type = 3 Then
            Dim Server As String = SelectedProfile.Server
            OpenWebsite(Server.ToString.Replace("/api/yggdrasil/authserver" + If(Server.EndsWithF("/"), "/", ""), "/user/profile"))
        Else
            Hint("当前档案不支持修改密码！")
        End If
    End Sub
    '修改 ID
    Public Sub BtnEditName_Click(sender As Object, e As RoutedEventArgs)
        EditProfileID()
    End Sub
    '选择档案
    Private Sub ChangeProfile(sender As Object, e As EventArgs) Handles BtnSelect.Click
        SelectedProfile = Nothing
        RunInUi(Sub()
                    FrmLaunchLeft.RefreshPage(True)
                    FrmLaunchLeft.BtnLaunch.IsEnabled = False
                End Sub)
    End Sub
    '修改皮肤
    Private Sub Skin_Click(sender As Object, e As RoutedEventArgs)
        If SelectedProfile.Type = McLoginType.Ms Then
            ChangeSkinMs()
        ElseIf SelectedProfile.Type = McLoginType.Auth Then
            OpenWebsite(SelectedProfile.Server.BeforeFirst("api/yggdrasil/authserver") + "user/closet")
            Hint("请移步至皮肤站修改！")
        Else
            Hint("当前档案不支持修改皮肤！")
        End If
    End Sub
    '保存皮肤
    Private Sub BtnSkinSave_Click(sender As Object, e As RoutedEventArgs)
        Skin.BtnSkinSave_Click()
    End Sub
    '刷新皮肤
    Private Sub BtnSkinRefresh_Click(sender As Object, e As RoutedEventArgs)
        Skin.RefreshClick()
    End Sub
    '修改披风
    Private Sub BtnSkinCape_Click(sender As Object, e As RoutedEventArgs)
        If SelectedProfile.Type = McLoginType.Ms Then
            Skin.BtnSkinCape_Click()
        ElseIf SelectedProfile.Type = McLoginType.Auth Then
            OpenWebsite(SelectedProfile.Server.BeforeFirst("api/yggdrasil/authserver") + "user/closet")
            Hint("请移步至皮肤站修改！")
        Else
            Hint("当前档案不支持修改披风！")
        End If
    End Sub
#End Region

End Class
