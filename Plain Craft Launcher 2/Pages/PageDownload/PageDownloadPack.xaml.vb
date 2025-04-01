Public Class PageDownloadPack

    Public Const PageSize = 40

    '加载器信息
    Public Loader As New LoaderTask(Of CompProjectRequest, Integer)("CompProject ModPack", AddressOf CompProjectsGet, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}
    Public Storage As New CompProjectStorage
    Public Page As Integer = 0
    Private Sub PageDownloadPack_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
        If McVersionHighest = -1 Then McVersionHighest = Math.Max(McVersionHighest, Integer.Parse(CType(TextSearchVersion.Items(1), MyComboBoxItem).Content.ToString.Split(".")(1)))
    End Sub
    Private Function LoaderInput() As CompProjectRequest
        Return New CompProjectRequest(CompType.ModPack, Storage, (Page + 1) * PageSize) With {
            .SearchText = TextSearchName.Text,
            .GameVersion = If(TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w"), TextSearchVersion.Text, Nothing),
            .Tag = ComboSearchTag.SelectedValue,
            .Source = ComboSearchSource.SelectedValue
        }
    End Function

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            Log($"[Comp] 开始可视化整合包列表，已储藏 {Storage.Results.Count} 个结果，当前在第 {Page + 1} 页")
            '列表项
            PanProjects.Children.Clear()
            For i = Math.Min(Page * PageSize, Storage.Results.Count - 1) To Math.Min((Page + 1) * PageSize - 1, Storage.Results.Count - 1)
                PanProjects.Children.Add(Storage.Results(i).ToCompItem(Loader.Input.GameVersion Is Nothing, True))
            Next
            '页码
            ShouldCardPagesExit = False
            LabPage.Text = Page + 1
            BtnPageLeft.Tag = Page - 1
            BtnPageRight.Tag = Page + 1
            BtnPageFirst.IsEnabled = Page > 1
            BtnPageLeft.IsEnabled = Page > 0
            BtnPageRight.IsEnabled = Storage.Results.Count > PageSize * (Page + 1) OrElse
                                     Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal
            '错误信息
            HintError.Text = If(Storage.ErrorMessage, "")
            '强制返回顶部
            PanBack.ScrollToTop()
        Catch ex As Exception
            Log(ex, "可视化整合包列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        If Loader.State = LoadState.Failed AndAlso Loader.Error?.Message?.Contains("不是有效的 json 文件") Then
            Log("[Download] 下载的 Mod 列表 json 文件损坏，已自动重试", LogLevel.Debug)
            PageLoaderRestart()
        End If
    End Sub

    ''' <summary>
    ''' 翻页卡片是否应该在下一次刷新时触发退出动画
    ''' </summary>
    Public ShouldCardPagesExit As Boolean = False

    '添加翻页卡片的动画
    Private Sub HideControlOnForceExit() Handles Me.HideControlsOnForceExit
        CardPages.Visibility = Visibility.Collapsed
    End Sub
    Private Sub ModifyEnterAnimControl(ControlList As List(Of FrameworkElement)) Handles Me.ModifyEnterAnimControls
        If PageState = PageStates.ContentEnter AndAlso CardPages.Visibility <> Visibility.Visible Then
            ControlList.Add(CardPages)
        End If
    End Sub
    Private Sub ModifyExitAnimControl(ControlList As List(Of FrameworkElement)) Handles Me.ModifyExitAnimControls
        If ShouldCardPagesExit AndAlso PageState = PageStates.ContentExit Then
            ShouldCardPagesExit = False
            ControlList.Add(CardPages)
        ElseIf PageState = PageStates.PageExit Then
            ControlList.Add(CardPages)
        End If
    End Sub

    '切换页码
    Private Sub ChangePageOnClickButton(sender As Object, e As EventArgs) Handles BtnPageFirst.Click, BtnPageLeft.Click, BtnPageRight.Click
        If Loader.State <> LoadState.Finished Then Exit Sub
        Dim NewPage As Integer
        Try
            NewPage = CType(sender, FrameworkElement).Tag
        Catch ex As Exception
            Log(ex, "整合包下载页面翻页按钮点击处理失败", LogLevel.Feedback)
            Exit Sub
        End Try
        If NewPage = Page Then Exit Sub
        Page = NewPage
        FrmMain.BackToTop()
        Log($"[Download] 整合包切换到第 {Page + 1} 页")
        RunInThread(Sub()
                        Thread.Sleep(100) '等待向上滚的动画结束
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
        If Loader.ShouldStart(LoaderInput()) Then
            ShouldCardPagesExit = True
            Storage = New CompProjectStorage '避免连续搜索两次使得 CompProjectStorage 引用丢失（#1311）
        End If
        Loader.Start()
    End Sub
    Private Sub EnterTrigger(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyDown, TextSearchVersion.KeyDown
        If e.Key = Key.Enter Then StartNewSearch()
    End Sub

    '重置按钮
    Private Sub BtnSearchReset_Click(sender As Object, e As EventArgs) Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.SelectedIndex = 0
        ComboSearchSource.SelectedIndex = 0
        ComboSearchTag.SelectedIndex = 0
        Loader.LastFinishedTime = 0 '要求强制重新开始
    End Sub

#End Region

End Class
