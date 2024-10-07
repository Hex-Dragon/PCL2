Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports System.Windows.Forms.LinkLabel
Imports System.Xml

Public Class PageOtherVote
    Public Class Vote
        Public Title As String
        Public Url As String
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
    Public Shared Loader As New LoaderTask(Of String, List(Of Vote))("VoteList", AddressOf VoteListGet, AddressOf LoaderInput)

    Private Shared Function LoaderInput() As String
        Return "" ' awa?
    End Function

    Public Shared Sub VoteListGet(Task As LoaderTask(Of String, List(Of Vote)))
        Dim content = NetGetCodeByRequestRetry("https://github.com/Hex-Dragon/PCL2/discussions/categories/%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8?discussions_q=is%3Aopen+category%3A%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8+sort%3Atop")
        If content Is Nothing Then Exit Sub
        Dim regexPattern As String = "<a[^>]*?\bdata-hovercard-type=""discussion""[^>]*?href=""(.*?)""[^>]*?>(.*?)</a>"

        Dim res As List(Of Vote) = New List(Of Vote)

        ' 使用正则表达式匹配
        Dim matches As MatchCollection = Regex.Matches(content, regexPattern, RegexOptions.Singleline)
        For Each match As Match In matches
            If match.Success Then
                ' 输出匹配到的href和文本内容
                Dim item As Vote = New Vote With {.Title = match.Groups(2).Value.Trim(), .Url = "https://github.com/" & match.Groups(1).Value}
                res.Add(item)
            End If
        Next
        Task.Output = res
    End Sub


    Public Sub LoadList()
        PanList.Children.Clear()
        For Each item In Loader.Output
            Dim ele As MyListItem = New MyListItem With {.Logo = "https://github.githubassets.com/assets/1fa9c-fd94340a7b39.png", .Type = MyListItem.CheckType.Clickable, .Title = item.Title}
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
