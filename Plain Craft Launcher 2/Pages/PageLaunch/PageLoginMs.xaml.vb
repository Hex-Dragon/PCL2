Public Class PageLoginMs

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
    End Sub
    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginMs
        Return New McLoginMs With {.OAuthRefreshToken = Setup.Get("CacheMsOAuthRefresh"), .UserName = Setup.Get("CacheMsName")}
    End Function
    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginMs) As String
        If LoginData.OAuthRefreshToken = "" Then
            Return "请在登录账号后再继续！"
        Else
            Return ""
        End If
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        BtnLogin.IsEnabled = False
        BtnLogin.Text = "登录中 0%"
        RunInNewThread(Sub()
                           Try
                               McLoginMsLoader.Start(GetLoginData(), IsForceRestart:=True)
                               Do While McLoginMsLoader.State = LoadState.Loading
                                   RunInUi(Sub() BtnLogin.Text = "登录中 " & Math.Round(McLoginMsLoader.Progress * 100) & "%")
                                   Thread.Sleep(50)
                               Loop
                               If McLoginMsLoader.State = LoadState.Finished Then
                                   RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
                               ElseIf McLoginMsLoader.State = LoadState.Aborted Then
                                   Throw New ThreadInterruptedException
                               ElseIf McLoginMsLoader.Error Is Nothing Then
                                   Throw New Exception("未知错误！")
                               Else
                                   Throw New Exception(McLoginMsLoader.Error.Message, McLoginMsLoader.Error)
                               End If
                           Catch ex As ThreadInterruptedException
                               Hint("登录已取消！")
                           Catch ex As Exception
                               If ex.Message = "$$" Then
                               ElseIf ex.Message.StartsWith("$") Then
                                   Hint(ex.Message.TrimStart("$"), HintType.Critical)
                               Else
                                   Log(ex, "微软登录尝试失败", LogLevel.Msgbox)
                               End If
                           Finally
                               RunInUi(Sub()
                                           BtnLogin.IsEnabled = True
                                           BtnLogin.Text = "添加账号"
                                       End Sub)
                           End Try
                       End Sub, "Ms Login")
    End Sub

End Class
