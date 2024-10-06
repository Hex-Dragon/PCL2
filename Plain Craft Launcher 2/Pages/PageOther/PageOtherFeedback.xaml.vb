Public Class PageOtherFeedback

    Public Class Feedback
        Public User As String
        Public Title As String
        Public Time As String
        Public Content As String
        Public Url As String
    End Class

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherFeedback_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PageLoaderInit(Load, PanLoad, PanContent, PanInfo, Loader, AddressOf RefreshList, AddressOf LoaderInput)
        '重复加载部分
        PanBack.ScrollToHome()
        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

    End Sub

    Public Shared Loader As New LoaderTask(Of String, List(Of Feedback))("FeedbackList", AddressOf FeedbackListGet, AddressOf LoaderInput) With {.ReloadTimeout = 60 * 1000}

    Private Shared Function LoaderInput() As String
        Return "" ' awa?
    End Function

    Public Shared Sub FeedbackListGet(Task As LoaderTask(Of String, List(Of Feedback)))
        Dim list As JArray
        list = NetGetCodeByRequestRetry("https://api.github.com/repos/Hex-Dragon/PCL2/issues", IsJson:=True, UseBrowserUserAgent:=True)
        If list Is Nothing Then Throw New Exception("无法获取到内容")
        Dim res As List(Of Feedback) = New List(Of Feedback)
        For Each i As JObject In list
            Dim item As Feedback = New Feedback With {.Title = i("title").ToString(), .Url = i("html_url").ToString(), .Content = i("body").ToString(), .Time = Date.Parse(i("created_at").ToString()).ToLocalTime().ToString(), .User = i("user")("login").ToString()}
            res.Add(item)
        Next
        Task.Output = res
    End Sub

    Public Sub RefreshList()
        For Each item In Loader.Output
            Dim ele As New MyListItem With {.Title = item.Title, .Info = item.User & " | " & item.Time, .Logo = $"https://github.com/{item.User}.png", .Type = MyListItem.CheckType.Clickable}
            AddHandler ele.Click, Sub()
                                      MyMsgBox(item.Content, item.User & " | " & item.Title, Button2:="查看详情", Button2Action:=Sub()
                                                                                                                                 OpenWebsite(item.Url)
                                                                                                                             End Sub)
                                  End Sub
            PanList.Children.Add(ele)
        Next
    End Sub

    Private Sub Feedback_Click(sender As Object, e As MouseButtonEventArgs)
        PageOtherLeft.TryFeedback()
    End Sub
End Class
