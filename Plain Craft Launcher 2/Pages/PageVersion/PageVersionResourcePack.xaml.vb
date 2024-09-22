Imports System.IO.Compression
Imports System.Security.Principal

Public Class PageVersionResourcePack

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        ResourcepacksPath = PageVersionLeft.Version.PathIndie + "resourcepacks\"
        Directory.CreateDirectory(ResourcepacksPath)
        Reload()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

    End Sub

    Dim FileList As List(Of String) = New List(Of String)
    Dim ResourcepacksPath As String

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
        PanCard.Title = $"资源包列表 ({FileList.Count})"
        If FileList.Count.Equals(0) Then
            PanNoWorld.Visibility = Visibility.Visible
            PanContent.Visibility = Visibility.Collapsed
        Else
            PanNoWorld.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub LoadFileList()
        Log("[Resourcepack] 刷新资源包文件")
        FileList.Clear()
        FileList = Directory.EnumerateFiles(ResourcepacksPath, "*.zip").ToList()
        If ModeDebug Then Log("[Resourcepack] 共发现 " & FileList.Count & " 个资源包文件", LogLevel.Debug)
        PanList.Children.Clear()
        Dim ResCachaPath = PageVersionLeft.Version.PathIndie & "PCL\Cache\resourcepacks\"
        If Directory.Exists(ResCachaPath) Then Directory.Delete(ResCachaPath, True)
        Directory.CreateDirectory(ResCachaPath)
        For Each i In FileList
            Dim ResTempFile = ResCachaPath & GetHash(i) & ".png"
            Try
                Dim Archive = New ZipArchive(New FileStream(i, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                Dim pack = Archive.GetEntry("pack.png")
                If pack Is Nothing Then
                    ResTempFile = PathImage & "Icons/NoIcon.png"
                Else
                    pack.ExtractToFile(ResTempFile)
                End If
            Catch ex As Exception
                Log(ex, "[Resourcepack] 提取整合包图片失败！")
                ResTempFile = PathImage & "Icons/NoIcon.png"
            End Try
            Dim worldItem As MyListItem = New MyListItem With {
            .Title = GetFileNameWithoutExtentionFromPath(i),
            .Logo = ResTempFile,
            .Info = $"引入时间：{ File.GetCreationTime(i).ToString("yyyy'/'MM'/'dd")}",
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
            My.Computer.FileSystem.DeleteFile(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Hint("已将资源包移至回收站！")
        Catch ex As Exception
            Log(ex, "删除资源包失败！", LogLevel.Hint)
        End Try
    End Sub
    Private Sub BtnCopy_Click(sender As Object, e As MouseButtonEventArgs)
        Dim Path As String = GetPathFromSender(sender)
        If File.Exists(Path) Then
            Clipboard.SetFileDropList(New Specialized.StringCollection() From {Path})
            Hint("已复制资源包文件到剪贴板！")
        Else
            Hint("资源包不存在！")
        End If
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(ResourcepacksPath) Then Directory.CreateDirectory(ResourcepacksPath)
        OpenExplorer("""" & ResourcepacksPath & """")
    End Sub
    Private Sub BtnOpen_Click(sender As Object, e As MouseButtonEventArgs)
        OpenExplorer("""" & sender.Tag & """")
    End Sub
    Private Sub BtnPaste_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim files As Specialized.StringCollection = Clipboard.GetFileDropList()
            If files.Count.Equals(0) Then
                Hint("剪贴板内无文件可粘贴")
                Exit Sub
            End If
            Dim CopiedFiles = 0
            For Each i In files
                If File.Exists(i) Then
                    Try
                        If File.Exists(ResourcepacksPath & GetFileNameFromPath(i)) Then
                            Hint("已存在同名文件：" & GetFileNameWithoutExtentionFromPath(i))
                        Else
                            File.Copy(i, ResourcepacksPath & GetFileNameFromPath(i))
                            CopiedFiles += 1
                        End If
                    Catch ex As Exception
                        Log(ex, "[Shader] 复制文件时出错")
                        Continue For
                    End Try
                End If
            Next
            Hint("已粘贴 " & CopiedFiles & " 个文件")
            LoadFileList()
        Catch ex As Exception
            Log(ex, "粘贴存档文件夹失败", LogLevel.Hint)
        End Try
    End Sub
End Class
