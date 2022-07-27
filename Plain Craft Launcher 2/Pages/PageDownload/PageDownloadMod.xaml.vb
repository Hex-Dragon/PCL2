Public Class PageDownloadMod

    '初始化加载器信息
    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, CardProjects, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Public Shared Loader As New LoaderTask(Of DlCfProjectRequest, DlCfProjectResult)("DlCfProject Mod", AddressOf DlCfProjectSub, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}
    Private Shared Function LoaderInput() As DlCfProjectRequest
        If FrmDownloadMod IsNot Nothing AndAlso FrmDownloadMod.IsLoaded Then
            Return New DlCfProjectRequest With {
                .SearchFilter = FrmDownloadMod.TextSearchName.Text,
                .GameVersion = If(FrmDownloadMod.TextSearchVersion.Text.Contains("."), FrmDownloadMod.TextSearchVersion.Text, Nothing),
                .CategoryId = Val(FrmDownloadMod.ComboSearchTag.SelectedItem.Tag),
                .IsModPack = False,
                .ModLoader = Val(FrmDownloadMod.ComboSearchLoader.SelectedItem.Tag)
            }  '处理列表加载失败后的点击加载器重试
        Else
            Return New DlCfProjectRequest With {.IsModPack = False}
        End If
    End Function
    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            PanProjects.Children.Clear()
            If Loader.Input.SearchFilter = "" Then
                CardProjects.Title = "热门 Mod"
            Else
                CardProjects.Title = "搜索结果 (" & Loader.Output.Projects.Count & If(Loader.Output.Projects.Count < Loader.Output.RealCount, "+", "") & ")"
            End If
            For Each Project In Loader.Output.Projects
                PanProjects.Children.Add(Project.ToCfItem(Loader.Input.GameVersion Is Nothing, Loader.Input.ModLoader Is Nothing OrElse Loader.Input.ModLoader = 0, AddressOf ProjectClick))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Mod 列表出错", LogLevel.Feedback)
        End Try
    End Sub
    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If DlCfFileLoader.Error IsNot Nothing Then ErrorMessage = DlCfFileLoader.Error.Message
                If ErrorMessage.Contains("不是有效的 Json 文件") Then
                    Log("[Download] 下载的 Mod 列表 Json 文件损坏，已自动重试", LogLevel.Debug)
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    '进入详情页面

    Public Sub ProjectClick(sender As MyCfItem, e As EventArgs)
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CfDetail, .Additional = sender.Tag})
    End Sub

    '搜索

    Private Sub TextSearchName_KeyUp(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyUp, TextSearchVersion.KeyUp
        If e.Key = Key.Enter Then StartSearch()
    End Sub
    Private Shared Sub StartSearch() Handles BtnSearchRun.Click
        Loader.Start()
    End Sub
    Private Sub BtnSearchReset_Click(sender As Object, e As EventArgs) Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.SelectedIndex = 0
        TextSearchVersion.Text = ""
        ComboSearchTag.SelectedIndex = 0
        ComboSearchLoader.SelectedIndex = 0
        Loader.LastFinishedTime = 0 '要求强制重新开始
    End Sub
    Private Sub TextSearchVersion_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextSearchVersion.TextChanged
        If TextSearchVersion.Text = "" Then
            ComboSearchLoader.Visibility = Visibility.Collapsed
            Grid.SetColumnSpan(TextSearchVersion, 2)
            ComboSearchLoader.SelectedIndex = 0
        Else
            ComboSearchLoader.Visibility = Visibility.Visible
            Grid.SetColumnSpan(TextSearchVersion, 1)
        End If
    End Sub

End Class
