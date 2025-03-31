Public Class PageDownloadMod

    Public Const PageSize = 40
    ''' <summary>
    ''' 在切换到该页面时自动设置的目标版本。
    ''' </summary>
    Public Shared TargetVersion As McVersion = Nothing

    '加载器信息
    Public Loader As New LoaderTask(Of CompProjectRequest, Integer)("CompProject Mod", AddressOf CompProjectsGet, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}
    Public Storage As New CompProjectStorage
    Public Page As Integer = 0
    Private IsLoaderInited As Boolean = False
    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Loaded
        '不知道从 Initialized 改成 Loaded 会不会有问题，但用 Initialized 会导致初始的筛选器修改被覆盖回默认值
        If TargetVersion IsNot Nothing Then
            '设置目标
            ResetFilter() '重置筛选器
            TextSearchVersion.Text = TargetVersion.Version.McName
            If TargetVersion.Version.HasForge Then
                ComboSearchLoader.SelectedValue = CType(CompModLoaderType.Forge, Integer).ToString()
            ElseIf TargetVersion.Version.HasFabric Then
                ComboSearchLoader.SelectedValue = CType(CompModLoaderType.Fabric, Integer).ToString()
            ElseIf TargetVersion.Version.HasNeoForge Then
                ComboSearchLoader.SelectedValue = CType(CompModLoaderType.NeoForge, Integer).ToString()
            End If
            TargetVersion = Nothing
            '如果已经完成请求，则重新开始
            If IsLoaderInited Then StartNewSearch()
            PanScroll.ScrollToHome()
        End If
        '加载器初始化
        If IsLoaderInited Then Return
        IsLoaderInited = True
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
        If McVersionHighest = -1 Then McVersionHighest = Math.Max(McVersionHighest, Integer.Parse(CType(TextSearchVersion.Items(1), MyComboBoxItem).Content.ToString.Split(".")(1)))
    End Sub
    Private Function LoaderInput() As CompProjectRequest
        Dim ModLoader As CompModLoaderType = ComboSearchLoader.SelectedValue
        Dim GameVersion As String = Nothing
        If TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w") Then
            GameVersion = TextSearchVersion.Text
            Dim Spl = GameVersion.Split(".")
            If Spl.Length > 1 AndAlso Val(Spl(1)) < 14 AndAlso ModLoader = CompModLoaderType.Forge Then
                ModLoader = CompModLoaderType.Any
            End If
        End If
        Return New CompProjectRequest(CompType.Mod, Storage, (Page + 1) * PageSize) With {
            .SearchText = TextSearchName.Text,
            .GameVersion = GameVersion,
            .Tag = ComboSearchTag.SelectedItem.Tag,
            .ModLoader = ModLoader,
            .Source = ComboSearchSource.SelectedValue
        }
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
            HintError.Text = If(Storage.ErrorMessage, "")
            '强制返回顶部
            PanBack.ScrollToTop()
        Catch ex As Exception
            Log(ex, "可视化 Mod 列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        If Loader.State = LoadState.Failed AndAlso Loader.Error?.Message?.Contains("不是有效的 json 文件") Then
            Log("[Download] 下载的 Mod 列表 json 文件损坏，已自动重试", LogLevel.Debug)
            PageLoaderRestart()
        End If
        CardPages.IsEnabled = Loader.State = LoadState.Finished
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
    Private Sub ResetFilter() Handles BtnSearchReset.Click
        TextSearchName.Text = ""
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
        Else
            ComboSearchLoader.Visibility = Visibility.Collapsed
            ComboSearchLoader.SelectedIndex = 0
        End If
    End Sub

#End Region

End Class
