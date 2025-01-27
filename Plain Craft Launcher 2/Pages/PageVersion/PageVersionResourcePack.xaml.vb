Imports System.IO.Compression
Imports System.Security.Principal

Public Class PageVersionResourcePack
    Implements IRefreshable
    Private Sub RefreshSelf() Implements IRefreshable.Refresh
        Refresh()
    End Sub
    Public Shared Sub Refresh()
        If FrmVersionResourcePack IsNot Nothing Then FrmVersionResourcePack.Reload()
        FrmVersionLeft.ItemResourcePack.Checked = True
    End Sub

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

    ''' <summary>
    ''' 文件和文件夹列表
    ''' </summary>
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
            PanNoWorld.UpdateLayout()
        Else
            PanNoWorld.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
            PanContent.UpdateLayout()
        End If
    End Sub

    Private Sub LoadFileList()
        Log("[Resourcepack] 刷新资源包文件")
        FileList.Clear()
        Dim fileRes = Directory.EnumerateFiles(ResourcepacksPath, "*.zip").ToList()
        FileList.AddRange(fileRes)
        Dim FolderRes = Directory.EnumerateDirectories(ResourcepacksPath).ToList()
        FileList.AddRange(FolderRes)
        If ModeDebug Then Log($"[Resourcepack] 共发现 {FileList.Count} 个资源包文件（{fileRes.Count} 个文件，{FolderRes.Count} 个文件夹）", LogLevel.Debug)
        PanList.Children.Clear()
        Dim ResCachaPath = PageVersionLeft.Version.PathIndie & "PCL\Cache\resourcepacks\"
        If Directory.Exists(ResCachaPath) Then Directory.Delete(ResCachaPath, True)
        Directory.CreateDirectory(ResCachaPath)
        For Each i In FileList
            Dim ResTempIconFile = ResCachaPath & GetHash(i) & ".png"
            Dim ResTempDescFile = ResCachaPath & GetHash(i) & ".json"
            Dim ResDesc As String = ""
            Dim isFile = File.Exists(i)

            '提取资源
            Try
                Dim GetResourcepackDesc =
                    Function(Json As JObject)
                        If Json?("pack")?("description").Type = JTokenType.String Then
                            Return Json("pack")("description").ToString()
                        Else
                            Return Json("pack")("description")("fallback").ToString()
                        End If
                    End Function
                If isFile Then '文件类型的资源包
                    Using Archive As New ZipArchive(New FileStream(i, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        Dim pack = Archive.GetEntry("pack.png")
                        Dim desc = Archive.GetEntry("pack.mcmeta")
                        If pack Is Nothing Then
                            ResTempIconFile = PathImage & "Icons/NoIcon.png"
                        Else
                            pack.ExtractToFile(ResTempIconFile)
                        End If
                        If desc IsNot Nothing Then
                            desc.ExtractToFile(ResTempDescFile)
                            ResDesc = GetResourcepackDesc(GetJson(File.ReadAllText(ResTempDescFile, Encoding.UTF8)))
                        End If
                    End Using
                Else '文件夹型资源包
                    ResTempIconFile = i + "\pack.png"
                    ResDesc = GetResourcepackDesc(GetJson(File.ReadAllText(i & "\pack.mcmeta", Encoding.UTF8)))
                End If
            Catch ex As Exception
                Log(ex, "[Resourcepack] 提取资源包信息失败！")
                ResTempIconFile = PathImage & "Icons/NoIcon.png"
                ResDesc = $"引入时间：{ If(isFile, File.GetCreationTime(i), Directory.GetCreationTime(i)).ToString("yyyy'/'MM'/'dd")}"
            End Try

            '防止错误
            If String.IsNullOrEmpty(ResDesc) Then ResDesc = $"引入时间：{ If(isFile, File.GetCreationTime(i), Directory.GetCreationTime(i)).ToString("yyyy'/'MM'/'dd")}"
            If Not File.Exists(ResTempIconFile) Then ResTempIconFile = PathImage & "Icons/NoIcon.png"

            Dim worldItem As MyListItem = New MyListItem With {
                .Title = If(isFile, GetFileNameWithoutExtentionFromPath(i), GetFolderNameFromPath(i)),
                .Logo = ResTempIconFile,
                .Info = ResDesc,
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
            If File.Exists(Path) Then
                My.Computer.FileSystem.DeleteFile(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Else
                My.Computer.FileSystem.DeleteDirectory(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            End If
            Hint("已将资源包移至回收站！")
        Catch ex As Exception
            Log(ex, "删除资源包失败！", LogLevel.Hint)
        End Try
    End Sub
    Private Sub BtnCopy_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim Path As String = GetPathFromSender(sender)
            If File.Exists(Path) Or Directory.Exists(Path) Then
                Clipboard.SetFileDropList(New Specialized.StringCollection() From {Path})
                Hint("已复制资源包文件到剪贴板！")
            Else
                Hint("资源包不存在！")
            End If
        Catch ex As Exception
            Log(ex, "复制失败……", LogLevel.Hint)
        End Try
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(ResourcepacksPath) Then Directory.CreateDirectory(ResourcepacksPath)
        OpenExplorer("""" & ResourcepacksPath & """")
    End Sub
    Private Sub BtnOpen_Click(sender As Object, e As MouseButtonEventArgs)
        OpenExplorerAndSelect(sender.Tag)
    End Sub
    Private Sub BtnPaste_Click(sender As Object, e As MouseButtonEventArgs)
        Dim count = PasteFileFromClipboard(ResourcepacksPath)
        If count > 0 Then
            Hint($"已成功粘贴 {count} 个资源包文件（夹）！")
            RefreshUI()
        Else
            Hint("没有资源包文件（夹）可供粘贴！")
        End If
    End Sub
End Class
