Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports System.Windows.Forms.LinkLabel
Imports System.Xml

Public Class PageOtherVote

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherFeedback_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True
        LoadList()

    End Sub

    Public Sub LoadList()
        PanContent.Children.Clear()
        PanContent.Children.Add(New MyLoading() With {.Text = "加载投票列表中……", .HorizontalAlignment = HorizontalAlignment.Center})
        Dim content As String
        Dim thread As Thread = RunInNewThread(Sub()
                                                  content = NetGetCodeByRequestRetry("https://github.com/Hex-Dragon/PCL2/discussions/categories/%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8?discussions_q=is%3Aopen+category%3A%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8+sort%3Atop")
                                              End Sub, "GetVoteList")
        While thread.IsAlive
            Thread.Sleep(100)
        End While

        PanContent.Children.Clear()
        If content Is Nothing Then Exit Sub
        Dim regexPattern As String = "<a[^>]*?\bdata-hovercard-type=""discussion""[^>]*?href=""(.*?)""[^>]*?>(.*?)</a>"

        ' 使用正则表达式匹配
        Dim matches As MatchCollection = Regex.Matches(content, regexPattern, RegexOptions.Singleline)

        For Each match As Match In matches
            If match.Success Then
                ' 输出匹配到的href和文本内容
                Dim item As MyListItem = New MyListItem With {.Title = match.Groups(2).Value.Trim(), .Logo = "https://github.githubassets.com/assets/1fa9c-fd94340a7b39.png", .Type = MyListItem.CheckType.Clickable}
                AddHandler item.Click, Sub()
                                           OpenWebsite("https://github.com/" & match.Groups(1).Value)
                                       End Sub
                PanContent.Children.Add(item)
            End If
        Next
        Exit Sub
        Dim doc As New XmlDocument()
        ' 加载XML内容
        doc.LoadXml(content)
        ' 获取所有a元素
        Dim links As XmlNodeList = doc.SelectNodes("//a")
        ' 遍历所有a元素
        For Each link As XmlNode In links
            If link.Attributes("data-hovercard-type").Value.Equals("discussion") Then
                Dim item As MyListItem = New MyListItem With {.Title = link.InnerText}
                PanContent.Children.Add(item)
            End If
        Next
    End Sub

    Private Sub Vote_Click(sender As Object, e As MouseButtonEventArgs)
        PageOtherLeft.TryVote()
    End Sub
End Class
