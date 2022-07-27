Public Class PageLoginMojangSkin

    Public Sub New()
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinMojang
    End Sub
    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Skin.Loader.Start()
    End Sub

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        TextName.Text = Setup.Get("CacheMojangName")
        TextEmail.Text = Setup.Get("CacheMojangUsername")
        TextEmail.Visibility = If(Setup.Get("UiLauncherEmail"), Visibility.Collapsed, Visibility.Visible)
        '皮肤在 Loaded 加载
    End Sub
    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginServer
        Return New McLoginServer(McLoginType.Mojang) With {.BaseUrl = "https://authserver.mojang.com", .Token = "Mojang", .UserName = Setup.Get("CacheMojangUsername"), .Password = Setup.Get("CacheMojangPass"), .Description = "Mojang 正版", .Type = McLoginType.Mojang}
    End Function

    Private Sub PageLoginMojangSkin_MouseEnter(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        AniStart({
                 AaOpacity(BtnEdit, 1 - BtnEdit.Opacity, 80),
                 AaHeight(BtnEdit, 25.5 - BtnEdit.Height, 140,, New AniEaseOutFluent),
                 AaHeight(BtnEdit, -1.5, 50, 140, New AniEaseInFluent),
                 AaOpacity(BtnExit, 1 - BtnExit.Opacity, 80),
                 AaHeight(BtnExit, 25.5 - BtnExit.Height, 140,, New AniEaseOutFluent),
                 AaHeight(BtnExit, -1.5, 50, 140, New AniEaseInFluent)
        }, "PageLoginMojangSkin Button")
    End Sub
    Private Sub PageLoginMojangSkin_MouseLeave(sender As Object, e As MouseEventArgs) Handles PanData.MouseLeave
        AniStart({
                 AaOpacity(BtnEdit, -BtnEdit.Opacity, 120,, New AniEaseOutFluent),
                 AaHeight(BtnEdit, 14 - BtnEdit.Height, 120,, New AniEaseInFluent),
                 AaOpacity(BtnExit, -BtnExit.Opacity, 120,, New AniEaseOutFluent),
                 AaHeight(BtnExit, 14 - BtnExit.Height, 120,, New AniEaseInFluent)
        }, "PageLoginMojangSkin Button")
    End Sub

    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles BtnEdit.Click
        Select Case MyMsgBox("你希望通过哪种途径更改密码？" & vbCrLf & "首次通过个人档案修改密码可能需要验证你的密保问题。", "更改密码", "个人档案", "密保邮箱", "取消")
            Case 1
                '个人档案
                OpenWebsite("https://www.minecraft.net/zh-hans/profile/")
            Case 2
                '密保邮箱
                OpenWebsite("https://account.mojang.com/password")
            Case 3
                '取消
        End Select
    End Sub
    Private Sub BtnExit_Click() Handles BtnExit.Click
        Setup.Set("CacheMojangAccess", "")
        FrmLaunchLeft.RefreshPage(False, True)
    End Sub

    Private Sub Skin_Click(sender As Object, e As MouseButtonEventArgs) Handles Skin.Click
        If McLoginLoader.State = LoadState.Failed Then
            Hint("登录失败，无法更改皮肤！", HintType.Critical)
            Exit Sub
        End If
        Dim SkinInfo As McSkinInfo = McSkinSelect(False)
        If Not SkinInfo.IsVaild Then Exit Sub
        Hint("正在更改皮肤……")
        RunInNewThread(Async Sub()
                           Try

                               If McLoginLoader.State = LoadState.Loading Then McLoginLoader.WaitForExit() '等待登录结束
                               Dim AccessToken As String = Setup.Get("CacheMojangAccess")
                               Dim Uuid As String = Setup.Get("CacheMojangUuid")

                               Dim Client As New Net.Http.HttpClient With {.Timeout = New TimeSpan(0, 0, 10)}
                               Client.DefaultRequestHeaders.Authorization = New Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken)
                               Client.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
                               Client.DefaultRequestHeaders.UserAgent.Add(New Net.Http.Headers.ProductInfoHeaderValue("MojangSharp", "0.1"))
                               Dim Contents As New Net.Http.MultipartFormDataContent From {
                                      {New Net.Http.StringContent(If(SkinInfo.IsSlim, "slim", "")), "model"},
                                      {New Net.Http.ByteArrayContent(File.ReadAllBytes(SkinInfo.LocalFile)), "file", GetFileNameFromPath(SkinInfo.LocalFile)}
                                  }
                               Dim Result As String = Await (Await Client.PutAsync(New Uri("https://api.mojang.com/user/profile/" & Uuid & "/skin"), Contents)).Content.ReadAsStringAsync
                               If RegexCheck(Result, "^[0-9a-f]{59,64}$") Then
                                   MySkin.ReloadCache("http://textures.minecraft.net/texture/" & Result, False)
                               ElseIf Result.ToLower.Contains("ip not secured") Then
                                   If MyMsgBox("首次操作需要在官网验证密保问题。" & vbCrLf & "验证完成后，你即可使用 PCL 更改皮肤，且无需再次验证。", "验证密保问题", "开始验证", "取消") = 1 Then
                                       OpenWebsite("https://www.minecraft.net/zh-hans/profile/skin")
                                   End If
                               ElseIf Result.Contains("request requires user authentication") Then
                                   Hint("正在重新登录，请稍后再次尝试更改皮肤！")
                                   RunInUi(Sub()
                                               BtnExit_Click()
                                               If String.IsNullOrEmpty(McLoginAble()) Then McLoginLoader.Start()
                                           End Sub)
                               ElseIf Result.Contains("""errorMessage""") Then
                                   Hint("更改皮肤失败：" & GetJson(Result)("errorMessage"), HintType.Critical)
                               Else
                                   Throw New Exception("未知错误（" & Result & "）")
                               End If
                           Catch ex As Exception
                               If ex.GetType.Equals(GetType(Tasks.TaskCanceledException)) Then
                                   Hint("更改皮肤失败：与 Mojang 皮肤服务器的连接超时，请检查你的网络是否通畅！", HintType.Critical)
                               Else
                                   Log(ex, "更改皮肤失败", LogLevel.Hint)
                               End If
                           End Try
                       End Sub, "Mojang Skin Upload")
    End Sub

End Class
