Public Class PageOtherFeedback

    Public Class Feedback
        Public Property User As String
        Public Property Title As String
        Public Property Time As Date
        Public Property Content As String
        Public Property Url As String
        Public Property ID As String
        Public Property Tags As New List(Of String)
        Public Property Open As Boolean = True
    End Class

    Enum TagID As Int64
        NewIssue = 4365827012
        Bug = 4365944566
        Improve = 4365949262
        Processing = 4365819896
        WaitingResponse = 4365816377
        Completed = 4365809832
        Decline = 4365654603
        NewFeture = 4365949953
        Ignored = 4365654601
        Duplicate = 4365654597
    End Enum

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherFeedback_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PageLoaderInit(Load, PanLoad, PanContent, PanInfo, Loader, AddressOf RefreshList, AddressOf LoaderInput)
        '重复加载部分
        PanBack.ScrollToHome()
        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

    End Sub

    Public Loader As New LoaderTask(Of Integer, List(Of Feedback))("FeedbackList", AddressOf FeedbackListGet, AddressOf LoaderInput)

    Private Function LoaderInput() As Integer
        Return 0 ' awa?
    End Function

    Public Sub FeedbackListGet(Task As LoaderTask(Of Integer, List(Of Feedback)))
        Dim list As JArray
        list = NetGetCodeByRequestRetry("https://api.github.com/repos/Hex-Dragon/PCL2/issues?state=all&sort=created&per_page=200", BackupUrl:="https://api.kkgithub.com/repos/Hex-Dragon/PCL2/issues?state=all&sort=created&per_page=200", IsJson:=True, UseBrowserUserAgent:=True) ' 获取近期 200 条数据就够了
        If list Is Nothing Then Throw New Exception("无法获取到内容")
        Dim res As List(Of Feedback) = New List(Of Feedback)
        For Each i As JObject In list
            Dim item As Feedback = New Feedback With {.Title = i("title").ToString(),
                .Url = i("html_url").ToString(),
                .Content = i("body").ToString(),
                .Time = Date.Parse(i("created_at").ToString()),
                .User = i("user")("login").ToString(),
                .ID = i("number"),
                .Open = i("state").ToString().Equals("open")}
            Dim thisTags As JArray = i("labels")
            For Each thisTag As JObject In thisTags
                item.Tags.Add(thisTag("id"))
            Next
            res.Add(item)
        Next
        Task.Output = res
    End Sub

    Public Sub RefreshList()
        PanListCompleted.Children.Clear()
        PanListProcessing.Children.Clear()
        PanListWaitingResponse.Children.Clear()
        PanListDecline.Children.Clear()
        For Each item In Loader.Output
            Dim ele As New MyListItem With {.Title = item.Title, .Type = MyListItem.CheckType.Clickable}
            Dim StatusDesc As String = "???"
            If item.Tags.Contains(TagID.Duplicate) Then Continue For
            If item.Tags.Contains(TagID.NewIssue) Then
                ele.Logo = PathImage & "Blocks/Grass.png"
                StatusDesc = "未查看"
            End If
            If item.Open Then
                If item.Tags.Contains(TagID.Processing) Then
                    ele.Logo = PathImage & "Blocks/CommandBlock.png"
                    StatusDesc = "处理中"
                End If
                If item.Tags.Contains(TagID.Bug) Then
                    ele.Logo = PathImage & "Blocks/RedstoneBlock.png"
                    StatusDesc = "处理中-Bug"
                End If
                If item.Tags.Contains(TagID.Improve) Then
                    ele.Logo = PathImage & "Blocks/Anvil.png"
                    StatusDesc = "处理中-优化"
                End If
                If item.Tags.Contains(TagID.WaitingResponse) Then
                    ele.Logo = PathImage & "Blocks/RedstoneLampOff.png"
                    StatusDesc = "等待提交者"
                End If
                If item.Tags.Contains(TagID.NewFeture) Then
                    ele.Logo = PathImage & "Blocks/Egg.png"
                    StatusDesc = "处理中-新功能"
                End If
            End If
            If item.Tags.Contains(TagID.Completed) Then
                ele.Logo = PathImage & "Blocks/GrassPath.png"
                StatusDesc = "已完成"
            End If
            If item.Tags.Contains(TagID.Decline) Then
                ele.Logo = PathImage & "Blocks/CobbleStone.png"
                StatusDesc = "已拒绝"
            End If
            If item.Tags.Contains(TagID.Ignored) Then
                ele.Logo = PathImage & "Blocks/CobbleStone.png"
                StatusDesc = "已忽略"
            End If
            ele.Info = item.User & " | " & item.Time
            ele.Tags = StatusDesc
            AddHandler ele.Click, Sub()
                                      Select Case MyMsgBox($"提交者：{item.User}（{GetTimeSpanString(item.Time - DateTime.Now, False)}）{vbCrLf}状态：{StatusDesc}{vbCrLf}{vbCrLf}{item.Content}",
                                               "#" & item.ID & " " & item.Title,
                                               Button2:="查看详情")
                                          Case 2
                                              OpenWebsite(item.Url)
                                      End Select
                                  End Sub
            If StatusDesc.StartsWithF("处理中") Then
                PanListProcessing.Children.Add(ele)
            ElseIf StatusDesc.Equals("等待提交者") Then
                PanListWaitingResponse.Children.Add(ele)
            ElseIf StatusDesc.Equals("已完成") Then
                PanListCompleted.Children.Add(ele)
            ElseIf StatusDesc.Equals("未查看") Then
                PanListNewIssue.Children.Add(ele)
            ElseIf StatusDesc.Equals("已拒绝") Then
                PanListDecline.Children.Add(ele)
            End If
            PanContentDecline.Visibility = If(PanListDecline.Children.Count.Equals(0), Visibility.Collapsed, Visibility.Visible)
            PanContentCompleted.Visibility = If(PanListCompleted.Children.Count.Equals(0), Visibility.Collapsed, Visibility.Visible)
            PanContentNewIssue.Visibility = If(PanListNewIssue.Children.Count.Equals(0), Visibility.Collapsed, Visibility.Visible)
            PanContentWaitingResponse.Visibility = If(PanListWaitingResponse.Children.Count.Equals(0), Visibility.Collapsed, Visibility.Visible)
            PanContentProcessing.Visibility = If(PanListProcessing.Children.Count.Equals(0), Visibility.Collapsed, Visibility.Visible)
        Next
    End Sub

    Private Sub Feedback_Click(sender As Object, e As MouseButtonEventArgs)
        PageOtherLeft.TryFeedback()
    End Sub
End Class
