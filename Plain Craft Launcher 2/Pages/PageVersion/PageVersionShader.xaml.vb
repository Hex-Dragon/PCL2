Imports System.Security.Principal

Public Class PageVersionShader

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        ShaderPath = PageVersionLeft.Version.PathIndie + "shaderpacks"
        If Not Directory.Exists(ShaderPath) Then Directory.CreateDirectory(ShaderPath)
        Reload()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

    End Sub

    Dim FileList As List(Of String) = New List(Of String)
    Dim ShaderPath As String

    ''' <summary>
    ''' 确保当前页面上的信息已正确显示。
    ''' </summary>
    Public Sub Reload()
        AniControlEnabled += 1
        PanBack.ScrollToHome()
        LoadFileList()
        AniControlEnabled -= 1
    End Sub

    Private Sub RefreshUI()
        PanCard.Title = $"光影包列表 ({FileList.Count})"
        If FileList.Count.Equals(0) Then
            PanNoWorld.Visibility = Visibility.Visible
            PanContent.Visibility = Visibility.Collapsed
        Else
            PanNoWorld.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub LoadFileList()
        Log("[World] 刷新光影包文件")
        FileList.Clear()
        FileList = Directory.EnumerateFiles(ShaderPath, "*.zip").ToList()
        If ModeDebug Then Log("[World] 共发现 " & FileList.Count & " 个光影包文件", LogLevel.Debug)
        PanList.Children.Clear()
        For Each i In FileList
            Dim worldItem As MyListItem = New MyListItem With {
            .Title = GetFileNameWithoutExtentionFromPath(i),
            .Info = $"引入时间：{ File.GetCreationTime(i).ToString("yyyy'/'MM'/'dd")}",
            .Tag = i
            }
            Dim BtnDelete As MyIconButton = New MyIconButton With {
                .Logo = Logo.IconButtonDelete,
                .ToolTip = "删除",
                .Tag = i
            }
            AddHandler BtnDelete.Click, AddressOf BtnDelete_Click
            Dim BtnCopy As MyIconButton = New MyIconButton With {
                .Logo = Logo.IconButtonCopy,
                .ToolTip = "复制",
                .Tag = i
            }
            AddHandler BtnCopy.Click, AddressOf BtnCopy_Click
            worldItem.Buttons = {BtnDelete, BtnCopy}
            PanList.Children.Add(worldItem)
        Next
        RefreshUI()
    End Sub

    Private Function GetPathFromSender(sender As Object) As String
        Return CType(sender, MyIconButton).Tag
    End Function

    Private Sub RemoveItem(Path As String)
        For Each i In PanList.Children
            If CType(i, MyListItem).Tag.Equals(Path) Then
                PanList.Children.Remove(CType(i, MyListItem))
                FileList.Remove(Path)
                Exit For
            End If
        Next
        RefreshUI()
    End Sub

    Private Sub BtnDelete_Click(sender As Object, e As MouseButtonEventArgs)
        Path = GetPathFromSender(sender)
        RemoveItem(Path)
        Try
            My.Computer.FileSystem.DeleteDirectory(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Hint("已将光影包移至回收站！")
        Catch ex As Exception
            Log(ex, "删除光影包失败！", LogLevel.Hint)
        End Try
    End Sub
    Private Sub BtnCopy_Click(sender As Object, e As MouseButtonEventArgs)
        Dim Path As String = GetPathFromSender(sender)
        If File.Exists(Path) Then
            Clipboard.SetFileDropList(New Specialized.StringCollection() From {Path})
            Hint("已复制光影包文件到剪贴板！")
        Else
            Hint("光影包不存在！")
        End If
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(ShaderPath) Then Directory.CreateDirectory(ShaderPath)
        OpenExplorer("""" & ShaderPath & """")
    End Sub
End Class