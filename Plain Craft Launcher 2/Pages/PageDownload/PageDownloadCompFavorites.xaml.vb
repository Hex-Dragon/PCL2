Public Class PageDownloadCompFavorites

    '加载器信息
    Public Shared Loader As New LoaderTask(Of List(Of CompFavorites.Data), List(Of CompProject))("CompProject Favorites", AddressOf CompFavoritesGet, AddressOf LoaderInput)

    Private Sub PageDownloadCompFavorites_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, Nothing, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Private Sub PageDownloadCompFavorites_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        If Loader.Input IsNot Nothing AndAlso Not Loader.Input.Equals(CompFavorites.GetAll()) Then
            Loader.Start()
        End If
    End Sub

    Private Shared Function LoaderInput() As List(Of CompFavorites.Data)
        Return CompFavorites.GetAll().Clone() '复制而不是直接引用！
    End Function
    Private Shared Sub CompFavoritesGet(Task As LoaderTask(Of List(Of CompFavorites.Data), List(Of CompProject)))
        Task.Output = CompFavorites.GetAllCompProjects(Task.Input)
    End Sub

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            If Loader.Output.Any() Then '有收藏
                PanSearchBox.Visibility = Visibility.Visible
                CardProjectsMod.Visibility = Visibility.Visible
                CardProjectsModpack.Visibility = Visibility.Visible
                CardNoContent.Visibility = Visibility.Collapsed
                RefreshContent()
            Else '没有收藏
                PanSearchBox.Visibility = Visibility.Collapsed
                CardProjectsMod.Visibility = Visibility.Collapsed
                CardProjectsModpack.Visibility = Visibility.Collapsed
                CardNoContent.Visibility = Visibility.Visible
            End If
            HintGetFail.Visibility = If(Loader.Input.Count = Loader.Output.Count, Visibility.Collapsed, Visibility.Visible)

            RefreshCardTitle()
        Catch ex As Exception
            Log(ex, "可视化收藏夹列表出错", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub RefreshContent()
        PanProjectsMod.Children.Clear()
        PanProjectsModpack.Children.Clear()
        Dim DataSource As List(Of CompProject) = If(IsSearching, SearchResult, Loader.Output)
        For Each item As CompProject In DataSource
            Dim EleItem As MyCompItem = item.ToCompItem(True, True)
            If IsSearching Then
                CardProjectsMod.Visibility = Visibility.Visible
                CardProjectsModpack.Visibility = Visibility.Collapsed
                PanProjectsMod.Children.Add(EleItem)
                Continue For
            Else
                CardProjectsModpack.Visibility = Visibility.Visible
                CardProjectsMod.Visibility = Visibility.Visible
            End If
            If item.Type = CompType.Mod Then
                PanProjectsMod.Children.Add(EleItem)
            ElseIf item.Type = CompType.ModPack Then
                PanProjectsModpack.Children.Add(EleItem)
            Else
                Log("[Favorites] 未知工程类型：" & item.Type)
            End If
        Next
    End Sub

    Private Sub RefreshCardTitle()
        Dim ModRes As Integer = 0
        Dim ModpackRes As Integer = 0
        If IsSearching Then
            ModRes = PanProjectsMod.Children.Count
            CardProjectsMod.Title = $"搜索结果 ({ModRes})"
        Else
            ModRes = If(Loader.Input.Exists(Function(e) e.Type = CompType.Mod), PanProjectsMod.Children.Count, 0)
            CardProjectsMod.Title = $"Mod ({ModRes})"
            ModpackRes = If(Loader.Input.Exists(Function(e) e.Type = CompType.ModPack), PanProjectsModpack.Children.Count, 0)
            CardProjectsModpack.Title = $"整合包 ({ModpackRes})"
        End If
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

    Private ReadOnly Property IsSearching As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(PanSearchBox.Text)
        End Get
    End Property

    Private SearchResult As List(Of CompProject)
    Public Sub SearchRun() Handles PanSearchBox.TextChanged
        If IsSearching Then
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of CompProject))
            For Each Entry As CompProject In Loader.Output
                Dim SearchSource As New List(Of KeyValuePair(Of String, Double))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.RawName, 1))
                If Entry.Description IsNot Nothing AndAlso Entry.Description <> "" Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Description, 0.4))
                End If
                If Entry.TranslatedName <> Entry.RawName Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.TranslatedName, 1))
                SearchSource.Add(New KeyValuePair(Of String, Double)(String.Join("", Entry.Tags), 0.2))
                QueryList.Add(New SearchEntry(Of CompProject) With {.Item = Entry, .SearchSource = SearchSource})
            Next
            '进行搜索
            SearchResult = Search(QueryList, PanSearchBox.Text, MaxBlurCount:=6, MinBlurSimilarity:=0.35).Select(Function(r) r.Item).ToList
        End If
        RefreshContent()
        RefreshCardTitle()
    End Sub

#End Region

End Class
