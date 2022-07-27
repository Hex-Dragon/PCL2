Public Class PageLoginLegacy

    Public Sub New()
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinLegacy
    End Sub
    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Skin.Loader.Start()
    End Sub

    Public IsReloaded As Boolean = False
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        If KeepInput AndAlso IsReloaded Then '避免第一次就以 KeepInput 的方式加载，导致文本框里没东西
            '保留输入，只刷新下拉框列表
            Dim Input As String = ComboName.Text
            ComboName.ItemsSource = If(Setup.Get("LoginLegacyName") = "", Nothing, Setup.Get("LoginLegacyName").ToString.Split("¨"))
            ComboName.Text = Input
        Else
            '不保留输入，刷新列表后自动选择第一项
            If Setup.Get("LoginLegacyName") = "" Then
                ComboName.ItemsSource = Nothing
            Else
                ComboName.ItemsSource = Setup.Get("LoginLegacyName").ToString.Split("¨")
                ComboName.Text = Setup.Get("LoginLegacyName").ToString.Split("¨")(0)
            End If
        End If
        IsReloaded = True
    End Sub
    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginLegacy
        If FrmLoginLegacy Is Nothing Then
            Return New McLoginLegacy With {.UserName = "", .SkinType = Setup.Get("LaunchSkinType"), .SkinName = Setup.Get("LaunchSkinID")}
        Else
            Return New McLoginLegacy With {.UserName = FrmLoginLegacy.ComboName.Text.Replace("¨", "").Trim, .SkinType = Setup.Get("LaunchSkinType"), .SkinName = Setup.Get("LaunchSkinID")}
        End If
    End Function
    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginLegacy) As String
        If LoginData.UserName.Trim = "" Then Return "玩家名不能为空！"
        If LoginData.UserName.Contains("""") Then Return "玩家名不能包含英文引号！"
        Return ""
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    Private Sub ComboLegacy_TextChanged(sender As Object, e As TextChangedEventArgs) Handles ComboName.TextChanged
        If Setup.Get("LaunchSkinType") = 0 Then
            PageLaunchLeft.SkinLegacy.Start(IsForceRestart:=True)
        End If
    End Sub
    Private Sub Skin_Click() Handles Skin.Click
        If (Setup.Get("UiHiddenPageSetup") OrElse Setup.Get("UiHiddenSetupLaunch")) AndAlso Not PageSetupUI.HiddenForceShow Then
            Hint("启动设置已被禁用！", HintType.Critical)
        Else
            FrmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.SetupLaunch) '切换到皮肤设置页面
        End If
    End Sub

End Class
