Imports System.Security.Principal

Public Class PageVersionWorld

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        WorldPath = PageVersionLeft.Version.PathIndie + "saves\"
        If Not Directory.Exists(WorldPath) Then Directory.CreateDirectory(WorldPath)
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
        AniControlEnabled -= 1
    End Sub

    Private Sub RefreshUI()
        PanCard.Title = $"存档列表 ({FileList.Count})"
        If FileList.Count.Equals(0) Then
            PanNoWorld.Visibility = Visibility.Visible
            PanContent.Visibility = Visibility.Collapsed
        Else
            PanNoWorld.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub LoadFileList()
        Log("[World] 刷新存档文件")
        FileList.Clear()
        FileList = Directory.EnumerateDirectories(WorldPath).ToList()
        If ModeDebug Then Log("[World] 共发现 " & FileList.Count & " 个存档文件夹", LogLevel.Debug)
        PanList.Children.Clear()
        For Each i In FileList
            Dim SaveLogo = i + "\icon.png"
            If Not File.Exists(SaveLogo) Then SaveLogo = PathImage & "Icons/NoIcon.png"
            Dim worldItem As MyListItem = New MyListItem With {
            .Logo = SaveLogo,
            .Title = GetFolderNameFromPath(i),
            .Info = $"创建时间：{ Directory.GetCreationTime(i).ToString("yyyy'/'MM'/'dd")}，最后修改时间：{Directory.GetLastWriteTime(i).ToString("yyyy'/'MM'/'dd")}",
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
            Dim BtnInfo As MyIconButton = New MyIconButton With {
                .Logo = Logo.IconButtonInfo,
                .ToolTip = "详情",
                .Tag = i
            }
            AddHandler BtnInfo.Click, AddressOf BtnInfo_Click
            worldItem.Buttons = {BtnOpen, BtnDelete, BtnCopy, BtnInfo}
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
            Hint("已将存档移至回收站！")
        Catch ex As Exception
            Log(ex, "删除存档失败！", LogLevel.Hint)
        End Try
    End Sub
    Private Sub BtnCopy_Click(sender As Object, e As MouseButtonEventArgs)
        Dim Path As String = GetPathFromSender(sender)
        If Directory.Exists(Path) Then
            Clipboard.SetFileDropList(New Specialized.StringCollection() From {Path})
            Hint("已复制存档文件夹到剪贴板！")
        Else
            Hint("存档文件夹不存在！")
        End If
    End Sub
    Private Sub BtnInfo_Click(sender As Object, e As MouseButtonEventArgs)
        Dim Path As String = GetPathFromSender(sender)
        Dim infos As List(Of String) = New List(Of String)
        infos.Add("名称：" & GetFileNameFromPath(Path))
        infos.Add("创建日期：" & Directory.GetCreationTime(Path).ToString("yyyy'/'MM'/'dd"))
        infos.Add("最后一次修改日期：" & Directory.GetLastWriteTime(Path).ToString("yyyy'/'MM'/'dd"))
        infos.Add("玩家数量：" & Directory.GetFiles(Path & "\playerdata", "*.dat", SearchOption.TopDirectoryOnly).Count())
        infos.Add("数据包数量：" & (Directory.GetDirectories(Path + "\datapacks").Count() + Directory.GetFiles(Path + "\datapacks").Count()).ToString())
        MyMsgBox(infos.Join(vbCrLf), "存档详细信息")
    End Sub
    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(WorldPath) Then Directory.CreateDirectory(WorldPath)
        OpenExplorer("""" & WorldPath & """")
    End Sub
    Private Sub BtnOpen_Click(sender As Object, e As MouseButtonEventArgs)
        OpenExplorer("""" & sender.Tag & """")
    End Sub
    Private Sub BtnPaste_Click(sender As Object, e As MouseButtonEventArgs)
        Dim files As Specialized.StringCollection = Clipboard.GetFileDropList()
        Dim Copied = 0
        For Each i In files
            Try
                If Directory.Exists(i) Then
                    If (Directory.Exists(WorldPath & GetFolderNameFromPath(i))) Then
                        Hint("发现同名文件夹，无法粘贴：" & GetFolderNameFromPath(i))
                    Else
                        CopyDirectory(i, WorldPath & GetFolderNameFromPath(i))
                        Copied += 1
                    End If
                Else
                    Hint("源文件夹不存在或源目标不是文件夹")
                End If
            Catch ex As Exception
                Log(ex, "粘贴存档文件夹失败", LogLevel.Hint)
                Continue For
            End Try
        Next
        Hint("已粘贴 " & Copied & " 个文件夹")
        LoadFileList()
    End Sub
End Class
