Public Class PageDownloadCompFavorites

    Public Const PageSize = 40

    '加载器信息
    Public Shared Loader As New LoaderTask(Of String, Integer)("CompProject Mod", AddressOf CompFavoritesGet, AddressOf LoaderInput)
    Public Shared Storage As New CompProjectStorage

    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, Nothing, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Private Shared Function LoaderInput() As String
        Return ReadReg("CustomCompFavorites")
    End Function
    Private Shared Function CompFavoritesGet(Task As LoaderTask(Of String, Integer))

    End Function

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try

        Catch ex As Exception
            Log(ex, "可视化收藏夹列表出错", LogLevel.Feedback)
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


#Region "搜索"

    '搜索按钮
    Private Sub StartNewSearch() Handles PanSearchBox.TextInput
        Loader.Start()
    End Sub

#End Region

End Class
