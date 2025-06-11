Public Class PageLoginAuth
    Public Shared DraggedAuthServer As String = Nothing
    Private Sub Reload() Handles Me.Loaded
        If DraggedAuthServer IsNot Nothing Then
            TextServer.Text = DraggedAuthServer
            DraggedAuthServer = Nothing
        End If
    End Sub
    Private Sub BtnBack_Click(sender As Object, e As EventArgs) Handles BtnBack.Click
        RunInUi(Sub()
                    TextServer.Text = Nothing
                    TextName.Text = Nothing
                    TextPass.Password = Nothing
                    FrmLaunchLeft.RefreshPage(True)
                End Sub)
    End Sub
    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        If Not String.IsNullOrWhiteSpace(TextServer.ValidateResult) Then 
            Hint("输入的验证服务器地址无效", HintType.Critical)
            Exit Sub
        End If
        If String.IsNullOrWhiteSpace(TextServer.Text) OrElse String.IsNullOrWhiteSpace(TextName.Text) OrElse String.IsNullOrWhiteSpace(TextPass.Password) Then
            Hint("验证服务器、用户名与密码均不能为空！", HintType.Critical)
            Exit Sub
        End If
        BtnLogin.IsEnabled = False
        BtnBack.IsEnabled = False
        Dim LoginData As New McLoginServer(McLoginType.Auth) With {.BaseUrl = If(TextServer.Text.EndsWithF("/"),
            TextServer.Text & "authserver", TextServer.Text & "/authserver"), .UserName = TextName.Text, .Password = TextPass.Password, .Description = "Authlib-Injector", .Type = McLoginType.Auth}
        RunInNewThread(Sub()
                           Try
                               IsCreatingProfile = True
                               McLoginAuthLoader.Start(LoginData, IsForceRestart:=True)
                               Do While McLoginAuthLoader.State = LoadState.Loading
                                   RunInUi(Sub() BtnLogin.Text = Math.Round(McLoginAuthLoader.Progress * 100) & "%")
                                   Thread.Sleep(50)
                               Loop
                               If McLoginAuthLoader.State = LoadState.Finished Then
                                   RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
                               ElseIf McLoginAuthLoader.State = LoadState.Aborted Then
                                   Throw New ThreadInterruptedException
                               ElseIf McLoginAuthLoader.Error Is Nothing Then
                                   Throw New Exception("未知错误！")
                               Else
                                   Throw New Exception(McLoginAuthLoader.Error.Message, McLoginAuthLoader.Error)
                               End If
                           Catch ex As ThreadInterruptedException
                               Hint("已取消登录！")
                           Catch ex As Exception
                               If ex.Message = "$$" Then
                               ElseIf ex.Message.StartsWith("$") Then
                                   Hint(ex.Message.TrimStart("$"), HintType.Critical)
                               Else
                                   Log(ex, "第三方登录尝试失败", LogLevel.Msgbox)
                               End If
                           Finally
                               RunInUi(
                               Sub()
                                   IsCreatingProfile = False
                                   BtnLogin.IsEnabled = True
                                   BtnBack.IsEnabled = True
                                   BtnLogin.Text = "登录"
                               End Sub)
                           End Try
                       End Sub)
    End Sub
    '获取验证服务器名称
    Private Sub GetServerName() Handles TextServer.LostKeyboardFocus
        Dim Link As String = TextServer.Text
        Dim Name As String = Nothing
        RunInNewThread(Sub()
                           Try
                               Dim Response As String = NetGetCodeByRequestRetry(Link, Encoding.UTF8)
                               Name = JObject.Parse(Response)("meta")("serverName").ToString()
                           Catch ex As Exception
                               Name = Nothing
                           End Try
                           RunInUi(Sub()
                                       If Name = Nothing Then
                                           TextServerName.Visibility = Visibility.Hidden
                                       Else
                                           TextServerName.Text = "验证服务器: " & Name
                                           TextServerName.Visibility = Visibility.Visible
                                       End If
                                   End Sub)
                       End Sub)
    End Sub
    '链接处理
    Private Sub ComboName_TextChanged() Handles TextName.TextChanged
        BtnLink.Content = If(TextName.Text = "", "注册账号", "找回密码")
    End Sub
    Private Sub Btn_Click(sender As Object, e As EventArgs) Handles BtnLink.Click
        If BtnLink.Content = "注册账号" Then
            OpenWebsite(If(McVersionCurrent IsNot Nothing, Setup.Get("VersionServerAuthRegister", Version:=McVersionCurrent), ""))
        Else
            Dim Website As String = If(McVersionCurrent IsNot Nothing, Setup.Get("VersionServerAuthRegister", Version:=McVersionCurrent), "")
            OpenWebsite(Website.Replace("/auth/register", "/auth/forgot"))
        End If
    End Sub
    '切换注册按钮可见性
    Private Sub ReloadRegisterButton() Handles Me.Loaded
        Dim Address As String = If(McVersionCurrent IsNot Nothing, Setup.Get("VersionServerAuthRegister", Version:=McVersionCurrent), "")
        BtnLink.Visibility = If(String.IsNullOrEmpty(New ValidateHttp().Validate(Address)), Visibility.Visible, Visibility.Collapsed)
    End Sub
End Class
