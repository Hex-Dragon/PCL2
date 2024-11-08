Public Class PageOtherHelpDetail
    Implements IRefreshable
    Public Entry As HelpEntry

    Public Sub Refresh() Implements IRefreshable.Refresh
        Init(New HelpEntry(Entry.RawPath))
    End Sub

    Private Sub PageOtherHelpDetail_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PanBack.ScrollToTop()
    End Sub

    ''' <summary>
    ''' 根据特定帮助项初始化页面 UI，返回是否成功加载。
    ''' </summary>
    Public Function Init(Entry As HelpEntry) As Boolean
        Dim FileContent = "<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Plain Craft Launcher 2"">" & If(Entry.XamlContent, "") & "</StackPanel>"
        Try
            If String.IsNullOrEmpty(Entry.XamlContent) Then Throw New Exception("帮助 xaml 文件为空")
            Me.Entry = Entry
            PanCustom.Children.Clear()
            FileContent = HelpArgumentReplace(FileContent)
            PanCustom.Children.Add(GetObjectFromXML(FileContent))
            Return True
        Catch ex As Exception
            Log("[System] 自定义信息内容：" & vbCrLf & FileContent)
            Log(ex, "加载帮助 xaml 文件失败", LogLevel.Msgbox)
            Return False
        End Try
    End Function

End Class
