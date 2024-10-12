Public Class PageDownloadCompFavorites

    '加载器信息
    Public Shared Loader As New LoaderTask(Of List(Of CompProject), Integer)("CompProject Favorites", AddressOf CompFavoritesGet, AddressOf LoaderInput)

    Private Sub PageDownloadMod_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, Nothing, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Private Shared Function LoaderInput() As List(Of CompProject)
        Return CompFavorites.GetAll()
    End Function
    Private Shared Sub CompFavoritesGet(Task As LoaderTask(Of List(Of CompProject), Integer))
        Task.Output = Task.Input.Count
        ' TODO: 刷新已存储的收藏
    End Sub

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            If Loader.Output.Equals(0) Then '没收藏
                PanSearchBox.Visibility = Visibility.Collapsed
                CardProjectsMod.Visibility = Visibility.Collapsed
                CardProjectsModpack.Visibility = Visibility.Collapsed
                CardNoContent.Visibility = Visibility.Visible
            Else '有收藏
                PanSearchBox.Visibility = Visibility.Visible
                CardProjectsMod.Visibility = Visibility.Visible
                CardProjectsModpack.Visibility = Visibility.Visible
                CardNoContent.Visibility = Visibility.Collapsed

                PanProjectsMod.Children.Clear()
                PanProjectsModpack.Children.Clear()
                For Each item As CompProject In Loader.Input
                    Dim EleItem As MyCompItem = item.ToCompItem(True, True)
                    If item.Type = CompType.Mod Then
                        PanProjectsMod.Children.Add(EleItem)
                    ElseIf item.Type = CompType.ModPack Then
                        PanProjectsModpack.Children.Add(EleItem)
                    Else
                        Log("未知工程类型：" & item.Type)
                    End If
                Next

                Dim NoContentTip As TextBlock = New TextBlock With {.Text = "暂时没有收藏内容", .Margin = New Thickness(0, 0, 0, 9), .HorizontalAlignment = HorizontalAlignment.Center, .FontSize = 19, .UseLayoutRounding = True, .SnapsToDevicePixels = True, .Foreground = DirectCast(FindResource("ColorBrush3"), SolidColorBrush)}
                If PanProjectsMod.Children.Count.Equals(0) Then
                    PanProjectsMod.Children.Add(NoContentTip)
                End If

                If PanProjectsModpack.Children.Count.Equals(0) Then
                    PanProjectsModpack.Children.Add(NoContentTip)
                End If
            End If
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
