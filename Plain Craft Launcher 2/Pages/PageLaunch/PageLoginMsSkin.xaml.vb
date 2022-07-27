Public Class PageLoginMsSkin

    Public Sub New()
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinMs
    End Sub
    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Skin.Loader.Start()
    End Sub

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        TextName.Text = Setup.Get("CacheMsName")
        '皮肤在 Loaded 加载
    End Sub
    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginMs
        If McLoginMsLoader.State = LoadState.Finished Then
            Return New McLoginMs With {.OAuthRefreshToken = Setup.Get("CacheMsOAuthRefresh"), .UserName = Setup.Get("CacheMsName"), .AccessToken = Setup.Get("CacheMsAccess"), .Uuid = Setup.Get("CacheMsUuid")}
        Else
            Return New McLoginMs With {.OAuthRefreshToken = Setup.Get("CacheMsOAuthRefresh"), .UserName = Setup.Get("CacheMsName")}
        End If
    End Function

    Private Sub PageLoginMsSkin_MouseEnter(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        AniStart({
                 AaOpacity(BtnEdit, 1 - BtnEdit.Opacity, 80),
                 AaHeight(BtnEdit, 25.5 - BtnEdit.Height, 140,, New AniEaseOutFluent),
                 AaHeight(BtnEdit, -1.5, 50, 140, New AniEaseInFluent),
                 AaOpacity(BtnExit, 1 - BtnExit.Opacity, 80),
                 AaHeight(BtnExit, 25.5 - BtnExit.Height, 140,, New AniEaseOutFluent),
                 AaHeight(BtnExit, -1.5, 50, 140, New AniEaseInFluent)
        }, "PageLoginMsSkin Button")
    End Sub
    Private Sub PageLoginMsSkin_MouseLeave(sender As Object, e As MouseEventArgs) Handles PanData.MouseLeave
        AniStart({
                 AaOpacity(BtnEdit, -BtnEdit.Opacity, 120,, New AniEaseOutFluent),
                 AaHeight(BtnEdit, 14 - BtnEdit.Height, 120,, New AniEaseInFluent),
                 AaOpacity(BtnExit, -BtnExit.Opacity, 120,, New AniEaseOutFluent),
                 AaHeight(BtnExit, 14 - BtnExit.Height, 120,, New AniEaseInFluent)
        }, "PageLoginMsSkin Button")
    End Sub

    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles BtnEdit.Click
        OpenWebsite("https://account.microsoft.com/security")
    End Sub
    Private Sub BtnExit_Click() Handles BtnExit.Click
        Setup.Set("CacheMsOAuthRefresh", "")
        Setup.Set("CacheMsAccess", "")
        Setup.Set("CacheMsUuid", "")
        Setup.Set("CacheMsName", "")
        McLoginMsLoader.Abort()
        FrmLaunchLeft.RefreshPage(False, True)
    End Sub

    Private IsChanging As Boolean = False
    Private Sub Skin_Click(sender As Object, e As MouseButtonEventArgs) Handles Skin.Click
        '检查条件，获取新皮肤
        If IsChanging Then
            Hint("正在更改皮肤中，请稍候！")
            Exit Sub
        End If
        If McLoginLoader.State = LoadState.Failed Then
            Hint("登录失败，无法更改皮肤！", HintType.Critical)
            Exit Sub
        End If
        Dim SkinInfo As McSkinInfo = McSkinSelect(False)
        If Not SkinInfo.IsVaild Then Exit Sub
        Hint("正在更改皮肤……")
        IsChanging = True
        '开始实际获取
        RunInNewThread(Async Sub()
                           Try
Retry:
                               If McLoginMsLoader.State = LoadState.Loading Then McLoginMsLoader.WaitForExit() '等待登录结束
                               Dim AccessToken As String = Setup.Get("CacheMsAccess")
                               Dim Uuid As String = Setup.Get("CacheMsUuid")

                               Dim Client As New Net.Http.HttpClient With {.Timeout = New TimeSpan(0, 0, 30)}
                               Client.DefaultRequestHeaders.Authorization = New Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken)
                               Client.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
                               Client.DefaultRequestHeaders.UserAgent.Add(New Net.Http.Headers.ProductInfoHeaderValue("MojangSharp", "0.1"))
                               Dim Contents As New Net.Http.MultipartFormDataContent From {
                                      {New Net.Http.StringContent(If(SkinInfo.IsSlim, "slim", "classic")), "variant"},
                                      {New Net.Http.ByteArrayContent(File.ReadAllBytes(SkinInfo.LocalFile)), "file", GetFileNameFromPath(SkinInfo.LocalFile)}
                                  }
                               Dim Result As String = Await (Await Client.PostAsync(New Uri("https://api.minecraftservices.com/minecraft/profile/skins"), Contents)).Content.ReadAsStringAsync
                               If Result.Contains("request requires user authentication") Then
                                   Hint("正在登录，将在登录完成后继续更改皮肤……")
                                   McLoginMsLoader.Start(GetLoginData(), IsForceRestart:=True)
                                   GoTo Retry
                               ElseIf Result.Contains("""error""") Then
                                   Hint("更改皮肤失败：" & GetJson(Result)("error"), HintType.Critical)
                                   Exit Sub
                               End If
                               '获取新皮肤地址
                               Log("[Skin] 皮肤修改返回值：" & vbCrLf & Result)
                               Dim ResultJson As JObject = GetJson(Result)
                               For Each Skin As JObject In ResultJson("skins")
                                   If Skin("state").ToString = "ACTIVE" Then
                                       MySkin.ReloadCache(Skin("url"), True)
                                       Exit Sub
                                   End If
                               Next
                               Throw New Exception("未知错误（" & Result & "）")
                           Catch ex As Exception
                               If ex.GetType.Equals(GetType(Tasks.TaskCanceledException)) Then
                                   Hint("更改皮肤失败：与 Mojang 皮肤服务器的连接超时，请检查你的网络是否通畅！", HintType.Critical)
                               Else
                                   Log(ex, "更改皮肤失败", LogLevel.Hint)
                               End If
                           Finally
                               IsChanging = False
                           End Try
                       End Sub, "Ms Skin Upload")
    End Sub

End Class
