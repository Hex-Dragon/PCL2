Imports System.Security.Principal

Public Class PageVersionWorld

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        WorldPath = PageVersionLeft.Version.PathIndie + "saves"
        Reload()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

    End Sub

    Dim FileList As List(Of String) = New List(Of String)
    Dim WorldPath As String

    ''' <summary>
    ''' 确保当前页面上的信息已正确显示。
    ''' </summary>
    Public Sub Reload()
        AniControlEnabled += 1
        PanBack.ScrollToHome()
        LoadFileList()
        If FileList.Count.Equals(0) Then
            PanNoWorld.Visibility = Visibility.Visible
            PanContent.Visibility = Visibility.Collapsed
        Else
            PanNoWorld.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
        End If
        AniControlEnabled -= 1
    End Sub

    Private Sub LoadFileList()
        Log("[World] 刷新存档文件")
        FileList.Clear()
        FileList = Directory.EnumerateDirectories(WorldPath).ToList()
        If ModeDebug Then Log("[World] 共发现 " & FileList.Count & " 个存档文件夹", LogLevel.Debug)
        PanList.Children.Clear()
        PanCard.Title = $"存档列表 ({FileList.Count})"
        For Each i In FileList
            Dim worldItem As MyListItem = New MyListItem With {
            .Logo = i + "\icon.png",
            .Title = GetFileNameFromPath(i),
            .Info = $"创建时间：{ Directory.GetCreationTime(i).ToString("yyyy'/'MM'/'dd")}，最后修改时间：{Directory.GetLastWriteTime(i).ToString("yyyy'/'MM'/'dd")}"
            }
            PanList.Children.Add(worldItem)
        Next
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(WorldPath) Then Directory.CreateDirectory(WorldPath)
        OpenExplorer("""" & WorldPath & """")
    End Sub
End Class
