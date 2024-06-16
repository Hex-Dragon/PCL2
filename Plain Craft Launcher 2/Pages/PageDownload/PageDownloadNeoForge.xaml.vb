Public Class PageDownloadNeoForge

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, CardVersions, CardTip, DlNeoForgeListLoader, AddressOf Load_OnFinish)
    End Sub
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

    Private Sub Load_OnFinish()
        '结果数据化
        Try
            Dim Versions As List(Of DlNeoForgeVersionEntry) = DlNeoForgeListLoader.Output
            PanVersions.Children.Clear()
            For Each Version In Versions
                PanVersions.Children.Add(NeoForgeDownloadListItem(Version, AddressOf NeoForge_Selected, True))
            Next
            CardVersions.Title = "版本列表 (" & Versions.Count & ")"
        Catch ex As Exception
            Log(ex, "可视化 NeoForge 版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub NeoForge_Selected(sender As MyListItem, e As EventArgs)
        McDownloadNeoForgeSave(sender.Tag)
    End Sub

    Private Sub BtnWeb_Click(sender As Object, e As EventArgs) Handles BtnWeb.Click
        OpenWebsite("https://neoforged.net/")
    End Sub

End Class
