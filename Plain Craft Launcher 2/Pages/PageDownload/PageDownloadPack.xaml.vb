Public Class PageDownloadPack

    '初始化加载器信息
    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, CardProjects, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Public Shared Loader As New LoaderTask(Of DlCfProjectRequest, DlCfProjectResult)("DlCfProject ModPack", AddressOf DlCfProjectSub, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}
    Private Shared Function LoaderInput() As DlCfProjectRequest
        If FrmDownloadPack IsNot Nothing AndAlso FrmDownloadPack.IsLoaded Then
            Return New DlCfProjectRequest With {
                .SearchFilter = FrmDownloadPack.TextSearchName.Text,
                .GameVersion = If(FrmDownloadPack.TextSearchVersion.Text.Contains("."), FrmDownloadPack.TextSearchVersion.Text, Nothing),
                .CategoryId = Val(FrmDownloadPack.ComboSearchTag.SelectedItem.Tag),
                .IsModPack = True
            } '处理列表加载失败后的点击加载器重试
        Else
            Return New DlCfProjectRequest With {.IsModPack = True}
        End If
    End Function
    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            PanProjects.Children.Clear()
            If Loader.Input.SearchFilter = "" Then
                CardProjects.Title = "热门整合包"
            Else
                CardProjects.Title = "搜索结果 (" & Loader.Output.Projects.Count & If(Loader.Output.Projects.Count < Loader.Output.RealCount, "+", "") & ")"
            End If
            For Each Project In Loader.Output.Projects
                PanProjects.Children.Add(Project.ToCfItem(Loader.Input.GameVersion Is Nothing, Loader.Input.ModLoader Is Nothing OrElse Loader.Input.ModLoader = 0, AddressOf ProjectClick))
            Next
        Catch ex As Exception
            Log(ex, "可视化整合包列表出错", LogLevel.Feedback)
        End Try
    End Sub
    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If DlCfFileLoader.Error IsNot Nothing Then ErrorMessage = DlCfFileLoader.Error.Message
                If ErrorMessage.Contains("不是有效的 Json 文件") Then
                    Log("[Download] 下载的整合包列表 Json 文件损坏，已自动重试", LogLevel.Debug)
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    Private Sub Load_OnStart()
        PanLoad.Visibility = Visibility.Visible
        AniStart({
                 AaOpacity(PanLoad, 1 - PanLoad.Opacity, 150),
                 AaOpacity(CardProjects, -CardProjects.Opacity, 150),
                 AaCode(Sub()
                            CardProjects.Visibility = Visibility.Collapsed
                            PanProjects.Children.Clear()
                        End Sub,, True)
            }, "FrmDownloadPack Load Switch")
    End Sub

    Public Sub ProjectClick(sender As MyCfItem, e As EventArgs)
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CfDetail, .Additional = sender.Tag})
    End Sub

    Private Sub TextSearchName_KeyUp(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyUp, TextSearchVersion.KeyUp
        If e.Key = Key.Enter Then StartSearch()
    End Sub
    Private Shared Sub StartSearch() Handles BtnSearchRun.Click
        Loader.Start()
    End Sub
    Private Sub BtnSearchReset_Click(sender As Object, e As EventArgs) Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.SelectedIndex = 0
        TextSearchVersion.Text = "全部"
        ComboSearchTag.SelectedIndex = 0
        Loader.LastFinishedTime = 0 '要求强制重新开始
    End Sub
    Private Sub BtnSearchInstall_Click(sender As Object, e As EventArgs) Handles BtnSearchInstall.Click
        ModpackInstall()
    End Sub

End Class
