Public Class PageDownloadCleanroom

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanMain, CardTip, DlCleanroomListLoader, AddressOf Load_OnFinish)
    End Sub
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

    Private Sub Load_OnFinish()
        '结果数据化
        Try
            '归类
            Dim Dict = DlCleanroomListLoader.Output.Value.GroupBy(Function(d) d.Inherit).OrderByDescending(Function(g) g.Key).
                ToDictionary(Function(g) g.Key, Function(g) g.ToList())
            '清空当前
            PanMain.Children.Clear()
            '转化为 UI
            For Each Pair As KeyValuePair(Of String, List(Of DlCleanroomListEntry)) In Dict
                If Not Pair.Value.Any() Then Continue For
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Pair.Key & " (" & Pair.Value.Count & ")", .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.IsSwaped = True
                NewCard.InstallMethod = Sub(Stack As StackPanel)
                                            For Each item In Stack.Tag
                                                Stack.Children.Add(CleanroomDownloadListItem(item, AddressOf CleanroomSave_Click, True))
                                            Next
                                        End Sub
                PanMain.Children.Add(NewCard)
            Next
        Catch ex As Exception
            Log(ex, "可视化 Cleanroom 版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '介绍栏
    Private Sub BtnWeb_Click(sender As Object, e As EventArgs) Handles BtnWeb.Click
        OpenWebsite("https://cleanroommc.com/zh/")
    End Sub

End Class
