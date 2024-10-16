Public Class PageDownloadCompFavorites

    '加载器信息
    Public Shared Loader As New LoaderTask(Of List(Of String), List(Of CompProject))("CompProject Favorites", AddressOf CompFavoritesGet, AddressOf LoaderInput)

    Private Sub PageDownloadCompFavorites_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, Nothing, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Private Sub PageDownloadCompFavorites_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        If Loader.Input IsNot Nothing AndAlso Not Loader.Input.Equals(CompFavorites.GetAll()) Then
            Loader.Start()
        End If
    End Sub

    Private Shared Function LoaderInput() As List(Of String)
        Return CompFavorites.GetAll().Clone() '复制而不是直接引用！
    End Function
    Private Shared Sub CompFavoritesGet(Task As LoaderTask(Of List(Of String), List(Of CompProject)))
        Task.Output = CompFavorites.GetAllCompProjects(Task.Input)
    End Sub

    Private CompItemList As New List(Of MyMiniCompItem)
    Private SelectedItemList As New List(Of MyMiniCompItem)

    '结果 UI 化
    Private Sub Load_OnFinish()
        CompItemList.Clear()
        For Each item In Loader.Output
            Dim CompItem = item.ToMiniCompItem()

            '----添加按钮----
            '删除按钮
            Dim Btn_Delete As New MyIconButton
            Btn_Delete.Logo = Logo.IconButtonLikeFill
            Btn_Delete.ToolTip = "取消收藏"
            ToolTipService.SetPlacement(Btn_Delete, Primitives.PlacementMode.Center)
            ToolTipService.SetVerticalOffset(Btn_Delete, 30)
            ToolTipService.SetHorizontalOffset(Btn_Delete, 2)
            AddHandler Btn_Delete.Click, Sub(sender As Object, e As EventArgs)
                                             If CompItem Is Nothing Then Exit Sub
                                             If CompFavorites.Del(CompItem.Entry.Id) Then Hint($"已取消收藏 {CompItem.Entry.TranslatedName}！", HintType.Finish)
                                             CompItemList.Remove(CompItem)
                                             RefreshContent()
                                             RefreshCardTitle()
                                         End Sub
            CompItem.Buttons = {Btn_Delete}
            '---操作逻辑---
            '右键查看详细信息界面
            AddHandler CompItem.MouseRightButtonUp, Sub(sender As Object, e As EventArgs)
                                                        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CompDetail,
                   .Additional = {CompItem.Entry, New List(Of String), String.Empty, CompModLoaderType.Any}})
                                                    End Sub

            CompItemList.Add(CompItem)
        Next
        Try
            If Loader.Input.Any() Then '有收藏
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
        Dim DataSource As List(Of MyMiniCompItem) = If(IsSearching, SearchResult, CompItemList)
        For Each item As MyMiniCompItem In DataSource
            If IsSearching Then
                CardProjectsMod.Visibility = Visibility.Visible
                CardProjectsModpack.Visibility = Visibility.Collapsed
                PanProjectsMod.Children.Add(item)
                Continue For
            Else
                CardProjectsModpack.Visibility = Visibility.Visible
                CardProjectsMod.Visibility = Visibility.Visible
            End If
            If item.Entry.Type = CompType.Mod Then
                PanProjectsMod.Children.Add(item)
            ElseIf item.Entry.Type = CompType.ModPack Then
                PanProjectsModpack.Children.Add(item)
            Else
                Log("[Favorites] 未知工程类型：" & item.Entry.Type)
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
            ModRes = If(Loader.Output.Exists(Function(e) e.Type = CompType.Mod), PanProjectsMod.Children.Count, 0)
            CardProjectsMod.Title = $"Mod ({ModRes})"
            ModpackRes = If(Loader.Output.Exists(Function(e) e.Type = CompType.ModPack), PanProjectsModpack.Children.Count, 0)
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

    Private SearchResult As List(Of MyMiniCompItem)
    Public Sub SearchRun() Handles PanSearchBox.TextChanged
        If IsSearching Then
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of MyMiniCompItem))
            For Each Entry As MyMiniCompItem In CompItemList
                Dim SearchSource As New List(Of KeyValuePair(Of String, Double))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Entry.RawName, 1))
                If Entry.Description IsNot Nothing AndAlso Entry.Description <> "" Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Description, 0.4))
                End If
                If Entry.Entry.TranslatedName <> Entry.Entry.RawName Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Entry.TranslatedName, 1))
                SearchSource.Add(New KeyValuePair(Of String, Double)(String.Join("", Entry.Entry.Tags), 0.2))
                QueryList.Add(New SearchEntry(Of MyMiniCompItem) With {.Item = Entry, .SearchSource = SearchSource})
            Next
            '进行搜索
            SearchResult = Search(QueryList, PanSearchBox.Text, MaxBlurCount:=6, MinBlurSimilarity:=0.35).Select(Function(r) r.Item).ToList
        End If
        RefreshContent()
        RefreshCardTitle()
    End Sub

#End Region

End Class
