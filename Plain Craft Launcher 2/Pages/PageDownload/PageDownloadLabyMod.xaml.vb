Public Class PageDownloadLabyMod

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, CardVersions, CardTip, DlLabyModListLoader, AddressOf Load_OnFinish)
    End Sub
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

    Private Sub Load_OnFinish()
        '结果数据化
        Try
            Dim Versions As JObject = DlLabyModListLoader.Output.Value
            Dim ProductionEntry As New JObject
            ProductionEntry.Add("channel", "production")
            ProductionEntry.Add("version", Versions("production")("labyModVersion").ToString)
            Dim SnapshotEntry As New JObject
            SnapshotEntry.Add("channel", "snapshot")
            SnapshotEntry.Add("version", Versions("snapshot")("labyModVersion").ToString)
            PanVersions.Children.Clear()
            PanVersions.Children.Add(LabyModDownloadListItem(ProductionEntry, AddressOf LabyMod_Production_Selected))
            PanVersions.Children.Add(LabyModDownloadListItem(SnapshotEntry, AddressOf LabyMod_Snapshot_Selected))
            CardVersions.Title = "版本列表 (" & Versions.Count & ")"
        Catch ex As Exception
            Log(ex, "可视化 LabyMod 版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub LabyMod_Production_Selected(sender As MyListItem, e As EventArgs)
        McDownloadLabyModProductionLoaderSave()
    End Sub

    Private Sub LabyMod_Snapshot_Selected(sender As MyListItem, e As EventArgs)
        McDownloadLabyModSnapshotLoaderSave()
    End Sub

    Private Sub BtnWeb_Click(sender As Object, e As EventArgs) Handles BtnWeb.Click
        OpenWebsite("https://labymod.net")
    End Sub

End Class
