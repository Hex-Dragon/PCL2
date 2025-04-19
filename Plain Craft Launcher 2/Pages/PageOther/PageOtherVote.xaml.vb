Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports System.Windows.Forms.LinkLabel
Imports System.Xml
Imports NAudio.Gui

Public Class PageOtherVote
    Public Class Vote
        Public Property Title As String
        Public Property Url As String
        Public Property Time As Date
        Public Property Vote As String
    End Class

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherFeedback_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PageLoaderInit(Load, PanLoad, PanContent, PanInfo, Loader, AddressOf LoadList, AddressOf LoaderInput)
        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

    End Sub
    Public Loader As New LoaderTask(Of Integer, List(Of Vote))("VoteList", AddressOf VoteListGet, AddressOf LoaderInput)

    Private Function LoaderInput() As Integer
        Return 0 ' awa?
    End Function

    Public Sub VoteListGet(Task As LoaderTask(Of Integer, List(Of Vote)))
        Dim content = NetGetCodeByRequestRetry("https://github.com/Hex-Dragon/PCL2/discussions/categories/%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8?discussions_q=is%3Aopen+category%3A%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8+sort%3Atop", BackupUrl:="https://kkgithub.com/Hex-Dragon/PCL2/discussions/categories/%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8?discussions_q=is%3Aopen+category%3A%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8+sort%3Atop", UseBrowserUserAgent:=True)
        If content Is Nothing Then Throw New Exception("空内容")

        Dim pattern As String = "<div class=""d-flex flex-auto flex-items-start"">(.*?)<svg aria-hidden=""true"" height=""16"" viewBox=""0 0 16 16"" version=""1.1"" width=""16"" data-view-component=""true"" class=""octicon octicon-comment color-fg-muted mr-1"">"
        Dim matches As MatchCollection = Regex.Matches(content, pattern, RegexOptions.Singleline)
        Dim contentList As New List(Of String)

        For Each match As Match In matches
            If match.Success Then
                contentList.Add(match.Groups(1).Value)
            End If
        Next

        Dim res As List(Of Vote) = New List(Of Vote)

        For Each c In contentList
            Dim item As Vote = New Vote()
            ' 抓取标题和网址
            pattern = "<a[^>]*?\bdata-hovercard-type=""discussion""[^>]*?href=""(.*?)""[^>]*?>(.*?)</a>"
            Dim SingleMatch As Match = Regex.Match(c, pattern, RegexOptions.Singleline)
            ' 使用正则表达式匹配
            If SingleMatch.Success Then
                ' 输出匹配到的href和文本内容
                item.Title = SingleMatch.Groups(2).Value.Trim()
                item.Url = "https://github.com" & SingleMatch.Groups(1).Value
            End If
            ' 抓取时间
            pattern = "<relative-time datetime=""(.*?)"" class=""no-wrap"""
            SingleMatch = Regex.Match(c, pattern, RegexOptions.Singleline)
            If SingleMatch.Success Then item.Time = Date.Parse(SingleMatch.Groups(1).Value).ToLocalTime()
            ' 抓取票数
            pattern = "aria-label=""Upvote: (\d+)"""
            SingleMatch = Regex.Match(c, pattern)
            If SingleMatch.Success Then
                item.Vote = SingleMatch.Groups(1).Value.Trim()
            Else
                item.Vote = "?"
            End If

            res.Add(item)
        Next
        Task.Output = res
    End Sub


    Public Sub LoadList()
        PanList.Children.Clear()
        For Each item In Loader.Output
            Dim ele As MyListItem = New MyListItem With {.Type = MyListItem.CheckType.Clickable, .Title = item.Vote & " 票 | " & item.Title, .Info = item.Time.ToString()}
            AddHandler ele.Click, Sub()
                                      OpenWebsite(item.Url)
                                  End Sub
            PanList.Children.Add(ele)
        Next
    End Sub

    Private Sub Vote_Click(sender As Object, e As MouseButtonEventArgs)
        PageOtherLeft.TryVote()
    End Sub
End Class
