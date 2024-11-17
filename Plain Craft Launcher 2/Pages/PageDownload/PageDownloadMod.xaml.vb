Public Class PageDownloadMod

    Public Const PageSize = 40

    '加载器信息
    Public Shared Loader As New LoaderTask(Of CompProjectRequest, Integer)("CompProject Mod", AddressOf CompProjectsGet, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}
    Public Shared Storage As New CompProjectStorage
    Public Shared Page As Integer = 0
    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
        If McVersionHighest = -1 Then McVersionHighest = Math.Max(McVersionHighest, Integer.Parse(CType(TextSearchVersion.Items(1), MyComboBoxItem).Content.ToString.Split(".")(1)))
    End Sub
    Private Shared Function LoaderInput() As CompProjectRequest
        Dim Request As New CompProjectRequest(CompType.Mod, Storage, (Page + 1) * PageSize)
        If FrmDownloadMod IsNot Nothing Then
            Dim ModLoader As CompModLoaderType = Val(FrmDownloadMod.ComboSearchLoader.SelectedItem.Tag)
            Dim GameVersion As String = If(FrmDownloadMod.TextSearchVersion.Text = "全部 (也可自行输入)", Nothing,
                    If(FrmDownloadMod.TextSearchVersion.Text.Contains(".") OrElse FrmDownloadMod.TextSearchVersion.Text.Contains("w"), FrmDownloadMod.TextSearchVersion.Text, Nothing))
            If GameVersion IsNot Nothing AndAlso GameVersion.Contains(".") AndAlso Val(GameVersion.Split(".")(1)) < 14 AndAlso '1.14-
                ModLoader = CompModLoaderType.Forge Then '选择了 Forge
                ModLoader = CompModLoaderType.Any '此时，视作没有筛选 Mod Loader（因为部分老 Mod 没有设置自己支持的加载器）
            End If
            With Request
                .SearchText = FrmDownloadMod.TextSearchName.Text
                .GameVersion = GameVersion
                .Tag = FrmDownloadMod.ComboSearchTag.SelectedItem.Tag
                .ModLoader = ModLoader
                .Source = CType(Val(FrmDownloadMod.ComboSearchSource.SelectedItem.Tag), CompSourceType)
            End With
        End If
        Return Request
    End Function

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            Log($"[Comp] 开始可视化 Mod 列表，已储藏 {Storage.Results.Count} 个结果，当前在第 {Page + 1} 页")
            '列表项
            PanProjects.Children.Clear()
            For i = Math.Min(Page * PageSize, Storage.Results.Count - 1) To Math.Min((Page + 1) * PageSize - 1, Storage.Results.Count - 1)
                PanProjects.Children.Add(Storage.Results(i).ToCompItem(Loader.Input.GameVersion Is Nothing, Loader.Input.ModLoader = CompModLoaderType.Any))
            Next
            '页码
            CardPages.Visibility = If(Storage.Results.Count > 40 OrElse
                                      Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal,
                                      Visibility.Visible, Visibility.Collapsed)
            LabPage.Text = Page + 1
            BtnPageFirst.IsEnabled = Page > 1
            BtnPageFirst.Opacity = If(Page > 1, 1, 0.2)
            BtnPageLeft.IsEnabled = Page > 0
            BtnPageLeft.Opacity = If(Page > 0, 1, 0.2)
            Dim IsRightEnabled As Boolean = '由于 WPF 的未知 bug，读取到的 IsEnabled 可能是错误的值（#3319）
                Storage.Results.Count > PageSize * (Page + 1) OrElse
                Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal
            BtnPageRight.IsEnabled = IsRightEnabled
            BtnPageRight.Opacity = If(IsRightEnabled, 1, 0.2)
            '错误信息
            If Storage.ErrorMessage Is Nothing Then
                HintError.Visibility = Visibility.Collapsed
            Else
                HintError.Visibility = Visibility.Visible
                HintError.Text = Storage.ErrorMessage
            End If
            '强制返回顶部
            PanBack.ScrollToTop()
        Catch ex As Exception
            Log(ex, "可视化 Mod 列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If Loader.Error IsNot Nothing Then ErrorMessage = Loader.Error.Message
                If ErrorMessage.Contains("不是有效的 json 文件") Then
                    Log("[Download] 下载的 Mod 列表 json 文件损坏，已自动重试", LogLevel.Debug)
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    '切换页码

    Private Sub BtnPageFirst_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageFirst.Click
        ChangePage(0)
    End Sub
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
        Log($"[Download] Mod 切换到第 {Page + 1} 页")
        RunInThread(
        Sub()
            Thread.Sleep(100) '等待向上滚的动画结束
            RunInUi(Sub() CardPages.IsEnabled = True)
            Loader.Start()
        End Sub)
    End Sub

#Region "搜索"

    '搜索按钮
    Private Sub StartNewSearch() Handles BtnSearchRun.Click
        Page = 0
        If Loader.ShouldStart(LoaderInput()) Then Storage = New CompProjectStorage '避免连续搜索两次使得 CompProjectStorage 引用丢失（#1311）
        Loader.Start()
    End Sub
    Private Sub EnterTrigger(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyDown, TextSearchVersion.KeyDown
        If e.Key = Key.Enter Then StartNewSearch()
    End Sub

    '重置按钮
    Private Sub BtnSearchReset_Click(sender As Object, e As EventArgs) Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.Text = "全部 (也可自行输入)"
        TextSearchVersion.SelectedIndex = 0
        ComboSearchSource.SelectedIndex = 0
        ComboSearchTag.SelectedIndex = 0
        ComboSearchLoader.SelectedIndex = 0
        Loader.LastFinishedTime = 0 '要求强制重新开始
    End Sub

    '版本选择
    '#3067：当下拉菜单展开时，程序会被 WPF 挂起，因而无法更新 Grid 布局，所以必须延迟到下拉菜单收起后才能更新
    Private Sub TextSearchVersion_TextChanged() Handles TextSearchVersion.TextChanged
        If Not TextSearchVersion.IsDropDownOpen Then UpdateSearchLoaderVisibility()
    End Sub
    Private Sub UpdateSearchLoaderVisibility() Handles TextSearchVersion.DropDownClosed
        If TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w") Then
            ComboSearchLoader.Visibility = Visibility.Visible
            Grid.SetColumnSpan(TextSearchVersion, 1)
        Else
            ComboSearchLoader.Visibility = Visibility.Collapsed
            Grid.SetColumnSpan(TextSearchVersion, 2)
            ComboSearchLoader.SelectedIndex = 0
        End If
    End Sub

#End Region

End Class
