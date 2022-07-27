Public Class PageLaunchRight

    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
        PanLog.Visibility = If(ModeDebug, Visibility.Visible, Visibility.Collapsed)
        '刷新主页
        If ShouldRefresh Then
            ShouldRefresh = False
            RefreshCustom()
        End If
        '快照版提示
#If BETA Then
        PanHint.Visibility = Visibility.Collapsed
#Else
        PanHint.Title = "快照版提示"
        PanHint.Visibility = If(ThemeCheckGold(), Visibility.Collapsed, Visibility.Visible)
        LabHint1.Text = "快照版包含尚未在正式版发布的测试性功能，仅用于赞助者本人尝鲜。所以请不要发给其他人或者用于制作整合包哦！如果发现了 Bug，可以在 更多 → 反馈 中提交！"
        LabHint2.Text = "你可以通过赞助￥23.33 档位换取解锁码来隐藏这个提示。"
#End If
    End Sub

    Private LatestFileContent As String = ""
    Private LatestFileLink As String = Nothing
    Private IsRefreshing As Boolean = False
    Public ShouldRefresh As Boolean = True
    ''' <summary>
    ''' 刷新自定义的主页。返回是否成功。
    ''' </summary>
    Private Sub RefreshCustom()
        If IsRefreshing Then
            ShouldRefresh = True
            Exit Sub
        End If
        Try
            IsRefreshing = True
            PanCustom.Children.Clear()
            RunInNewThread(Sub()
                               Dim FileContent As String = ""
                               Select Case Setup.Get("UiCustomType")
                                   Case 0
                                       '啥也不干
                                   Case 1
                                       '加载本地文件
                                       FileContent = ReadFile(Path & "PCL\Custom.xaml") 'ReadFile 会进行存在检测
                                       Log("[System] 尝试从本地文件读取主页自定义数据（" & FileContent.Length & "）")
                                   Case 2
                                       '加载联网文件
                                       Try
                                           Dim Link As String = Setup.Get("UiCustomNet")
                                           If Link = LatestFileLink Then
                                               FileContent = LatestFileContent
                                               Log("[System] 尝试缓存加载主页自定义数据（" & FileContent.Length & "）")
                                           ElseIf Not String.IsNullOrWhiteSpace(Link) Then
                                               Log("[System] 开始从网络读取主页自定义数据（" & Link & "）")
                                               FileContent = NetGetCodeByRequestRetry(Link)
                                               Log("[System] 尝试从网络读取主页自定义数据（" & FileContent.Length & "）")
                                           End If
                                           LatestFileLink = Link
                                           LatestFileContent = FileContent
                                       Catch ex As Exception
                                           Log(ex, "获取 PCL2 主页自定义信息失败", LogLevel.Msgbox)
                                           LatestFileLink = ""
                                       End Try
                               End Select
                               If FileContent = "" Then
                                   IsRefreshing = False
                                   Exit Sub
                               End If
                               FileContent = "<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Plain Craft Launcher 2"">" & FileContent & "</StackPanel>"
                               FileContent = FileContent.Replace("{path}", Path)
                               RunInUi(Sub()
                                           Try
                                               Log("[System] 加载主页自定义数据")
                                               PanCustom.Children.Add(GetObjectFromXML(FileContent))
                                               IsRefreshing = False
                                           Catch ex As Exception
                                               IsRefreshing = False
                                               Log("[System] 自定义信息内容：" & vbCrLf & FileContent)
                                               If MyMsgBox(ex.Message, "加载自定义主页失败", "重试", "取消") = 1 Then
                                                   LatestFileLink = Nothing
                                                   LatestFileContent = ""
                                                   RefreshCustom()
                                               End If
                                           End Try
                                       End Sub)
                           End Sub, "主页自定义刷新")
        Catch ex As Exception
            Log(ex, "加载 PCL2 主页自定义信息失败", LogLevel.Msgbox)
            IsRefreshing = False
        End Try
    End Sub

    Public Sub ForceRefresh(Optional ShowHint As Boolean = True)
        ShouldRefresh = True
        LatestFileContent = ""
        LatestFileLink = Nothing
        If ShowHint Then Hint("已刷新主页！", HintType.Finish)
        FrmMain.PageChange(FormMain.PageType.Launch)
        Init()
    End Sub

    Private Sub BtnMsStart_Click() Handles BtnMsStart.Click
        MyMsgBox("在迁移过程中，你可能需要设置你的档案信息。" & vbCrLf & "在输入年龄或生日时，请注意让你的年龄大于 18 岁，否则可能导致无法登录！", "迁移提示", "继续", ForceWait:=True)
        OpenWebsite("https://www.minecraft.net/zh-hans/account-security")
    End Sub
    Private Sub BtnMsFaq_Click() Handles BtnMsFaq.Click
        OpenWebsite("https://www.mcbbs.net/thread-1252431-1-1.html")
    End Sub

End Class
