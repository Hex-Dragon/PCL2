Public Class PageDownloadPack

    Public Const PageSize = 40

    '加载器信息
    Public Shared Loader As New LoaderTask(Of CompProjectRequest, Integer)("CompProject ModPack", AddressOf CompProjectsGet, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}
    Public Shared Storage As New CompProjectStorage
    Public Shared Page As Integer = 0
    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
        McVersionHighest = Math.Max(McVersionHighest, Integer.Parse(CType(TextSearchVersion.Items(1), MyComboBoxItem).Content.ToString.Split(".")(1)))
    End Sub
    Private Shared Function LoaderInput() As CompProjectRequest
        Dim Request As New CompProjectRequest(CompType.ModPack, Storage, (Page + 1) * PageSize)
        If FrmDownloadPack IsNot Nothing AndAlso FrmDownloadPack.IsLoaded Then
            With Request
                .SearchText = FrmDownloadPack.TextSearchName.Text
                .GameVersion = If(FrmDownloadPack.TextSearchVersion.Text.Contains("."), FrmDownloadPack.TextSearchVersion.Text, Nothing)
                .Tag = FrmDownloadPack.ComboSearchTag.SelectedItem.Tag
            End With
        End If
        Return Request
    End Function

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            '列表项
            PanProjects.Children.Clear()
            For i = Math.Min(Page * PageSize, Storage.Results.Count - 1) To Math.Min((Page + 1) * PageSize - 1, Storage.Results.Count - 1)
                PanProjects.Children.Add(
                    Storage.Results(i).ToCompItem(Loader.Input.GameVersion Is Nothing, True, AddressOf ProjectClick))
            Next
            '页码
            CardPages.Visibility = If(Storage.Results.Count > 40 OrElse
                                      Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal,
                                      Visibility.Visible, Visibility.Collapsed)
            LabPage.Text = Page + 1
            BtnPageLeft.IsEnabled = Page > 0
            BtnPageLeft.Opacity = If(BtnPageLeft.IsEnabled, 1, 0.5)
            BtnPageRight.IsEnabled = Storage.Results.Count > PageSize * (Page + 1) OrElse
                                     Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal
            BtnPageRight.Opacity = If(BtnPageRight.IsEnabled, 1, 0.5)
            '强制返回顶部
            PanBack.ScrollToTop()
        Catch ex As Exception
            Log(ex, "可视化整合包列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If Loader.Error IsNot Nothing Then ErrorMessage = Loader.Error.Message
                If ErrorMessage.Contains("不是有效的 Json 文件") Then
                    Log("[Download] 下载的整合包列表 Json 文件损坏，已自动重试", LogLevel.Debug)
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    '进入详情页面

    Public Sub ProjectClick(sender As MyCompItem, e As EventArgs)
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CompDetail, .Additional = sender.Tag})
    End Sub

    '切换页码

    Private Sub BtnPageLeft_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageLeft.Click
        ChangePage(Page - 1)
    End Sub
    Private Sub BtnPageRight_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageRight.Click
        ChangePage(Page + 1)
    End Sub
    Private Sub ChangePage(NewPage As Integer)
        CardPages.IsEnabled = False
        Page = NewPage
        FrmMain.BackToTop()
        Log($"[Download] 整合包切换到第 {Page + 1} 页")
        RunInThread(Sub()
                        Thread.Sleep(100) '等待向上滚的动画结束
                        RunInUi(Sub() CardPages.IsEnabled = True)
                        Loader.Start()
                    End Sub)
    End Sub

    Private Sub BtnSearchInstall_Click(sender As Object, e As EventArgs) Handles BtnSearchInstall.Click
        ModpackInstall()
    End Sub

#Region "搜索"

    '搜索按钮
    Private Sub StartNewSearch() Handles BtnSearchRun.Click
        Page = 0
        Storage = New CompProjectStorage
        Loader.Start()
    End Sub
    Private Sub TextSearchName_KeyUp(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyUp, TextSearchVersion.KeyUp
        If e.Key = Key.Enter Then StartNewSearch()
    End Sub

    '重置按钮
    Private Sub BtnSearchReset_Click(sender As Object, e As EventArgs) Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.SelectedIndex = 0
        TextSearchVersion.Text = ""
        ComboSearchTag.SelectedIndex = 0
        Loader.LastFinishedTime = 0 '要求强制重新开始
    End Sub

#End Region

End Class
