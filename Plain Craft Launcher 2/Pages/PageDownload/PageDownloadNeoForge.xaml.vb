Public Class PageDownloadNeoForge

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanMain, CardTip, DlNeoForgeListLoader, AddressOf Load_OnFinish)
    End Sub
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

    Private Sub Load_OnFinish()
        '结果数据化
        Try
            Dim Versions = Sort(DlNeoForgeListLoader.Output.Value, AddressOf VersionSortBoolean)
            'Dim Versions As List(Of DlNeoForgeVersionEntry) = DlNeoForgeListLoader.Output
            PanMain.Children.Clear()
            For Each Version As String In Versions
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Version.Replace("_p", " P"), .Margin = New Thickness(0, 0, 0, 15), .SwapType = 5}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Version}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.IsSwaped = True
                PanMain.Children.Add(NewCard)
            Next
        Catch ex As Exception
            Log(ex, "可视化 NeoForge 版本列表出错", LogLevel.Feedback)
        End Try
    End Sub
    Public Sub NeoForge_StateChanged(sender As MyLoading, newState As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState)
        If newState <> MyLoading.MyLoadingState.Stop Then Exit Sub

        Dim Card As MyCard = CType(sender.Parent, FrameworkElement).Parent
        Dim Loader As LoaderTask(Of String, List(Of DlNeoForgeVersionEntry)) = sender.State
        '载入列表
        Card.SwapControl.Children.Clear()
        Card.SwapControl.Tag = Loader.Output
        Card.SwapType = 6
        Card.StackInstall()
    End Sub
    Public Sub NeoForge_Click(sender As MyLoading, e As MouseButtonEventArgs)
        If sender.State.LoadingState = MyLoading.MyLoadingState.Error Then
            CType(sender.State, LoaderTask(Of String, List(Of DlNeoForgeVersionEntry))).Start(IsForceRestart:=True)
        End If
    End Sub
    Private Sub NeoForge_Selected(sender As MyListItem, e As EventArgs)
        McDownloadNeoForgeSave(sender.Tag)
    End Sub
    Public Sub DownloadStart(sender As MyListItem, e As Object)
        McDownloadNeoForge(sender.Tag)
    End Sub
    Private Sub BtnWeb_Click(sender As Object, e As EventArgs) Handles BtnWeb.Click
        OpenWebsite("https://neoforged.net/")
    End Sub

End Class
