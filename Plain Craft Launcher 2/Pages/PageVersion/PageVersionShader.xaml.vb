Imports System.Security.Principal

Public Class PageVersionShader
    Implements IRefreshable
    Private Sub RefreshSelf() Implements IRefreshable.Refresh
        Refresh()
    End Sub
    Public Shared Sub Refresh()
        If FrmVersionShader IsNot Nothing Then FrmVersionShader.Reload()
        FrmVersionLeft.ItemShader.Checked = True
    End Sub

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        ShaderPath = PageVersionLeft.Version.PathIndie + "shaderpacks\"
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
            PanNoWorld.UpdateLayout()
        Else
            PanNoWorld.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
            PanContent.UpdateLayout()
        End If
    End Sub

    Private Sub LoadFileList()
        Log("[Shader] 刷新光影包文件")
        FileList.Clear()

        ' 获取所有 .zip 文件
        Dim zipFiles = Directory.EnumerateFiles(ShaderPath, "*.zip").ToList()

        ' 获取所有文件夹
        Dim folders = Directory.EnumerateDirectories(ShaderPath).ToList()

        ' 合并文件和文件夹列表
        FileList = zipFiles.Concat(folders).ToList()

        If ModeDebug Then Log("[Shader] 共发现 " & FileList.Count & " 个光影包文件", LogLevel.Debug)

        PanList.Children.Clear()

        For Each i In FileList
            Dim worldItem As MyListItem = New MyListItem With {
            .Title = If(Directory.Exists(i), GetFolderNameFromPath(i), GetFileNameFromPath(i)),
            .Info = If(Directory.Exists(i),
                       $"类型：文件夹 | 创建时间：{Directory.GetCreationTime(i).ToString("yyyy'/'MM'/'dd")}",
                       $"类型：文件 | 引入时间：{File.GetCreationTime(i).ToString("yyyy'/'MM'/'dd")}"),
            .Tag = i
        }

            Dim BtnOpen As MyIconButton = New MyIconButton With {
            .Logo = Logo.IconButtonOpen,
            .ToolTip = "打开",
            .Tag = i
        }
            AddHandler BtnOpen.Click, AddressOf BtnOpen_Click

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

            worldItem.Buttons = {BtnOpen, BtnDelete, BtnCopy}
            PanList.Children.Add(worldItem)
        Next

        RefreshUI()
    End Sub

    Private Function GetPathFromSender(sender As Object) As String
        Return sender.Tag
    End Function

    Private Sub RemoveItem(Path As String)
        Try
            For Each i In PanList.Children
                If CType(i, MyListItem).Tag.Equals(Path) Then
                    PanList.Children.Remove(CType(i, MyListItem))
                    FileList.Remove(Path)
                    Exit For
                End If
            Next
        Catch ex As Exception
            Log(ex, "未能找到对应 UI")
        End Try
        RefreshUI()
    End Sub

    Private Sub BtnDelete_Click(sender As Object, e As MouseButtonEventArgs)
        Path = GetPathFromSender(sender)
        RemoveItem(Path)
        Try
            If Directory.Exists(Path) Then
                My.Computer.FileSystem.DeleteDirectory(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Else
                My.Computer.FileSystem.DeleteFile(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            End If
            Hint("已将光影包移至回收站！")
        Catch ex As Exception
            Log(ex, "删除光影包失败！", LogLevel.Hint)
        End Try
    End Sub
    Private Sub BtnCopy_Click(sender As Object, e As MouseButtonEventArgs)
        Dim Path As String = GetPathFromSender(sender)
        Try
            If File.Exists(Path) OrElse Directory.Exists(Path) Then
                Clipboard.SetFileDropList(New Specialized.StringCollection() From {Path})
                Hint("已复制光影包文件到剪贴板！")
            Else
                Hint("光影包不存在！")
            End If
        Catch ex As Exception
            Log(ex, "复制失败……", LogLevel.Hint)
        End Try
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(ShaderPath) Then Directory.CreateDirectory(ShaderPath)
        OpenExplorer("""" & ShaderPath & """")
    End Sub

    Private Sub BtnOpen_Click(sender As Object, e As MouseButtonEventArgs)
        OpenExplorerAndSelect(sender.Tag)
    End Sub

    Private Sub BtnPaste_Click(sender As Object, e As MouseButtonEventArgs)
        Dim count = PasteFileFromClipboard(ShaderPath)
        If count > 0 Then
            Hint("已成功导入 " & count & " 个光影包文件（夹）！")
            RefreshUI()
        Else
            Hint("没有光影包文件（夹）可供粘贴！")
        End If
    End Sub
End Class