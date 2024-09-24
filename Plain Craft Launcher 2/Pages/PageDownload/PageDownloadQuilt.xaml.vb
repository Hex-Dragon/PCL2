Public Class PageDownloadQuilt

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, CardVersions, CardTip, DlQuiltListLoader, AddressOf Load_OnFinish)
    End Sub
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

    Private Sub Load_OnFinish()
        '结果数据化
        Try
            Dim Versions As JArray = DlQuiltListLoader.Output.Value("installer")
            PanVersions.Children.Clear()
            For Each Version In Versions
                PanVersions.Children.Add(QuiltDownloadListItem(Version, AddressOf Quilt_Selected))
            Next
            CardVersions.Title = "版本列表 (" & Versions.Count & ")"
        Catch ex As Exception
            Log(ex, "可视化 Quilt 版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub Quilt_Selected(sender As MyListItem, e As EventArgs)
        McDownloadQuiltLoaderSave(sender.Tag)
    End Sub

    Private Sub BtnWeb_Click(sender As Object, e As EventArgs) Handles BtnWeb.Click
        OpenWebsite("https://quiltmc.org")
    End Sub

End Class
