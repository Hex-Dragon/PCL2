Public Class PageLoginMojang

    Private IsFirstLoad As Boolean = True
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        '记住密码
        CheckRemember.Checked = Setup.Get("LoginRemember")
        If KeepInput AndAlso Not IsFirstLoad Then '避免第一次就以 KeepInput 的方式加载，导致文本框里没东西
            '保留输入，只刷新下拉框列表
            Dim Input As String = ComboName.Text
            ComboName.ItemsSource = If(Setup.Get("LoginMojangEmail") = "", Nothing, Setup.Get("LoginMojangEmail").ToString.Split("¨"))
            ComboName.Text = Input
        Else
            '不保留输入，刷新列表后自动选择第一项
            If Setup.Get("LoginMojangEmail") = "" Then
                ComboName.ItemsSource = Nothing
            Else
                ComboName.ItemsSource = Setup.Get("LoginMojangEmail").ToString.Split("¨")
                ComboName.Text = Setup.Get("LoginMojangEmail").ToString.Split("¨")(0)
                If Setup.Get("LoginRemember") Then TextPass.Password = Setup.Get("LoginMojangPass").ToString.Split("¨")(0).Trim
            End If
        End If
        IsFirstLoad = False
    End Sub
    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginServer
        If FrmLoginMojang Is Nothing Then
            Return New McLoginServer(McLoginType.Mojang) With {.BaseUrl = "https://authserver.mojang.com", .Token = "Mojang", .UserName = "", .Password = "", .Type = McLoginType.Mojang, .Description = "Mojang 正版"}
        Else
            Return New McLoginServer(McLoginType.Mojang) With {.BaseUrl = "https://authserver.mojang.com", .Token = "Mojang", .UserName = If(FrmLoginMojang.ComboName.Text, "").Replace("¨", "").Trim(), .Password = If(FrmLoginMojang.TextPass.Password, "").Replace("¨", "").Trim(), .Type = McLoginType.Mojang, .Description = "Mojang 正版"}
        End If
    End Function
    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginServer) As String
        If LoginData.UserName = "" Then Return "邮箱不能为空！"
        If Not LoginData.UserName.Contains("@") Then Return "邮箱格式错误！"
        If LoginData.Password = "" Then Return "密码不能为空！"
        Return ""
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    '保存输入信息
    Private Sub ComboMojangName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles ComboName.TextChanged
        If sender.Text = "" Then TextPass.Password = ""
        If AniControlEnabled = 0 Then Setup.Set("CacheMojangAccess", "") '迫使其不进行 Validate
    End Sub
    Private Sub TextMojangPass_PasswordChanged(sender As Object, e As RoutedEventArgs) Handles TextPass.PasswordChanged
        If AniControlEnabled = 0 Then Setup.Set("CacheMojangAccess", "")
    End Sub
    Private Sub ComboMojangName_SelectionChanged(sender As MyComboBox, e As SelectionChangedEventArgs) Handles ComboName.SelectionChanged
        If sender.SelectedIndex = -1 OrElse Not Setup.Get("LoginRemember") Then
            TextPass.Password = ""
        Else
            TextPass.Password = Setup.Get("LoginMojangPass").ToString.Split("¨")(sender.SelectedIndex).Trim
        End If
    End Sub
    Private Sub CheckRememberChange(sender As MyCheckBox, e As Object) Handles CheckRemember.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Checked)
    End Sub

    '链接处理
    Private Sub InputChanged() Handles ComboName.TextChanged, TextPass.PasswordChanged
        BtnLink.Content = If(ComboName.Text = "" AndAlso TextPass.Password = "", "购买正版", "找回密码")
    End Sub
    Private Sub BtnMojang_Click(sender As Object, e As EventArgs) Handles BtnLink.Click
        If BtnLink.Content = "购买正版" Then
            OpenWebsite("https://www.minecraft.net/store/minecraft-java-edition/buy")
        Else
            OpenWebsite("https://account.mojang.com/password")
        End If
    End Sub

End Class
