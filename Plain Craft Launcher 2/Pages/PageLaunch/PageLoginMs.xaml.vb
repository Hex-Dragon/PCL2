Public Class PageLoginMs
    Private Sub BtnBack_Click(sender As Object, e As EventArgs) Handles BtnBack.Click
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
    End Sub
    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        BtnLogin.IsEnabled = False
        BtnBack.IsEnabled = False
        BtnLogin.Text = "0%"
        RunInNewThread(
        Sub()
            Try
                McLoginMsLoader.Start(GetLoginData(McLoginType.Ms), IsForceRestart:=True)
                Do While McLoginMsLoader.State = LoadState.Loading
                    RunInUi(Sub() BtnLogin.Text = Math.Round(McLoginMsLoader.Progress * 100) & "%")
                    Thread.Sleep(50)
                Loop
                If McLoginMsLoader.State = LoadState.Finished Then
                    RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
                ElseIf McLoginMsLoader.State = LoadState.Aborted Then
                    Throw New ThreadInterruptedException
                ElseIf McLoginMsLoader.Error Is Nothing Then
                    Throw New Exception("未知错误！")
                Else
                    Throw New Exception(McLoginMsLoader.Error.Message, McLoginMsLoader.Error)
                End If
            Catch ex As ThreadInterruptedException
                Hint("已取消登录！")
            Catch ex As Exception
                If ex.Message = "$$" Then
                ElseIf ex.Message.StartsWith("$") Then
                    Hint(ex.Message.TrimStart("$"), HintType.Critical)
                ElseIf TypeOf ex Is Security.Authentication.AuthenticationException AndAlso ex.Message.ContainsF("SSL/TLS") Then
                    Log(ex, "正版登录验证失败，请考虑在 [设置 → 其他] 中关闭 [在正版登录时验证 SSL 证书]，然后再试。" & vbCrLf & vbCrLf & "原始错误信息：", LogLevel.Msgbox)
                Else
                    Log(ex, "正版登录尝试失败", LogLevel.Msgbox)
                End If
            Finally
                RunInUi(
                Sub()
                    BtnLogin.IsEnabled = True
                    BtnBack.IsEnabled = True
                    BtnLogin.Text = "登录"
                End Sub)
            End Try
        End Sub, "Ms Login")
    End Sub
End Class
