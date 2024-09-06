﻿Imports System.Windows.Navigation

Public Class FormLoginOAuth

    Public Event OnLoginSuccess(code As String)
    Public Event OnLoginCanceled(IsSwitch As Boolean)

    '跳转事件
    Private IsLoginSuccessed As Boolean = False
    Private IsSwitch As Boolean = False
    Private Sub Browser_Navigating(sender As WebBrowser, e As NavigatingCancelEventArgs) Handles Browser1.Navigating, Browser2.Navigating, Browser3.Navigating
        Dim Url As String = e.Uri.AbsoluteUri
        If ModeDebug Then Log("[Login] 登录浏览器 " & sender.Tag & " 导向：" & Url)
        If Url.Contains("code=") Then
            Dim Code As String = RegexSeek(Url, "(?<=code\=)[^&]+")
            Log("[Login] 抓取到 OAuth 返回码：" & Code)
            IsLoginSuccessed = True
            RaiseEvent OnLoginSuccess(Code)
        ElseIf Url.Contains("github.") Then
            Hint(GetLang("LangLoginOAuthNoGithubLogin"), HintType.Critical)
            Close()
        End If
    End Sub

    '已导航事件
    Private IsFirstLoaded As Boolean = False
    Private IsFirstLoadedLock As New Object
    Private Sub Browser_Navigated(sender As WebBrowser, e As NavigationEventArgs) Handles Browser1.Navigated, Browser2.Navigated, Browser3.Navigated
        SyncLock IsFirstLoadedLock
            If IsFirstLoaded Then Exit Sub
            IsFirstLoaded = True
        End SyncLock
        RunInThread(Sub()
                        Thread.Sleep(1000)
                        RunInUi(Sub()
                                    sender.Visibility = Visibility.Visible
                                    PanLoading.Visibility = Visibility.Collapsed
                                    Log("[Login] 已将登录窗口切换至浏览器")
                                End Sub)
                    End Sub)
    End Sub

    '窗体的进出事件
    Public Shared LoginUrl1 As String = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?prompt=login&client_id=00000000402b5328&response_type=code&scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&redirect_uri=https:%2F%2Flogin.live.com%2Foauth20_desktop.srf"
    Public Shared LoginUrl2 As String = "https://login.live.com/oauth20_authorize.srf?prompt=login&client_id=00000000402b5328&response_type=code&scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&redirect_uri=https:%2F%2Flogin.live.com%2Foauth20_desktop.srf"
    Private Sub FrmLoginOAuth_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Browser1.Navigate(LoginUrl1)
        RunInThread(Sub()
                        '估计是由于时间戳原因，同时导航会导致误报密码失败
                        Thread.Sleep(2500)
                        RunInUi(Sub()
                                    Try '防止在 Dispose 之后调用
                                        If Not IsFirstLoaded Then Browser2.Navigate(LoginUrl1)
                                    Catch
                                    End Try
                                End Sub)
                        Thread.Sleep(2500)
                        RunInUi(Sub()
                                    Try
                                        If Not IsFirstLoaded Then Browser3.Navigate(LoginUrl2)
                                    Catch
                                    End Try
                                End Sub)
                    End Sub)
    End Sub
    Private Sub FormLoginOAuth_Closed() Handles Me.Closed
        RunInThread(Sub()
                        Thread.Sleep(1000) '释放会卡一下，所以稍等一下……
                        RunInUiWait(Sub()
                                        Try
                                            Browser1.Dispose()
                                        Catch ex As Exception
                                            Log(ex, "释放微软登录浏览器 1 失败")
                                        End Try
                                    End Sub)
                        Thread.Sleep(200)
                        RunInUiWait(Sub()
                                        Try
                                            Browser2.Dispose()
                                        Catch ex As Exception
                                            Log(ex, "释放微软登录浏览器 2 失败")
                                        End Try
                                    End Sub)
                        Thread.Sleep(200)
                        RunInUiWait(Sub()
                                        Try
                                            Browser3.Dispose()
                                        Catch ex As Exception
                                            Log(ex, "释放微软登录浏览器 3 失败")
                                        End Try
                                    End Sub)
                    End Sub)
        If Not IsLoginSuccessed Then RaiseEvent OnLoginCanceled(IsSwitch)
        FrmMain.Focus()
    End Sub
    Private Sub HintSwitch_Click() Handles HintSwitch.Click
        IsSwitch = True
        Close()
    End Sub
End Class
