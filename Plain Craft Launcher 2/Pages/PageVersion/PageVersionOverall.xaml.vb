Public Class PageVersionOverall

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '更新设置
        ItemDisplayLogoCustom.Tag = "PCL\Logo.png"
        Reload()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True
        PanDisplay.TriggerForceResize()

    End Sub

    Public ItemVersion As MyListItem
    ''' <summary>
    ''' 确保当前页面上的信息已正确显示。
    ''' </summary>
    Private Sub Reload()
        AniControlEnabled += 1

        '刷新设置项目
        ComboDisplayType.SelectedIndex = ReadIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "DisplayType", McVersionCardType.Auto)
        BtnDisplayStar.Text = If(PageVersionLeft.Version.IsStar, "从收藏夹中移除", "加入收藏夹")
        '刷新版本显示
        PanDisplayItem.Children.Clear()
        ItemVersion = PageSelectRight.McVersionListItem(PageVersionLeft.Version)
        ItemVersion.IsHitTestVisible = False
        PanDisplayItem.Children.Add(ItemVersion)
        FrmMain.PageNameRefresh()
        '刷新版本图标
        ComboDisplayLogo.SelectedIndex = 0
        Dim Logo As String = ReadIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "Logo", "")
        Dim LogoCustom As Boolean = ReadIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "LogoCustom", "False")
        If LogoCustom Then
            For Each Selection As MyComboBoxItem In ComboDisplayLogo.Items
                If Selection.Tag = Logo OrElse (Selection.Tag = "PCL\Logo.png" AndAlso Logo.EndsWith("PCL\Logo.png")) Then
                    ComboDisplayLogo.SelectedItem = Selection
                    Exit For
                End If
            Next
        End If

        AniControlEnabled -= 1
    End Sub

#Region "设置临时接口"

    '版本分类
    Private Sub ComboDisplayType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboDisplayType.SelectionChanged
        If Not (IsLoad AndAlso AniControlEnabled = 0) Then Exit Sub
        If ComboDisplayType.SelectedIndex <> 1 Then
            '改为不隐藏
            Try
                WriteIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "DisplayType", ComboDisplayType.SelectedIndex)
                WriteIni(PathMcFolder & "PCL.ini", "VersionCache", "") '要求刷新缓存
                LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Catch ex As Exception
                Log(ex, "修改版本分类失败（" & PageVersionLeft.Version.Name & "）", LogLevel.Feedback)
            End Try
        Else
            '改为隐藏
            Try
                If Not Setup.Get("HintHide") Then
                    If MyMsgBox("确认要从版本列表中隐藏该版本吗？隐藏该版本后，它将不再出现于 PCL 显示的版本列表中。" & vbCrLf & "此后，在版本列表页面按下 F11 才可以查看被隐藏的版本。", "隐藏版本提示",, "取消") <> 1 Then
                        ComboDisplayType.SelectedIndex = 0
                        Exit Sub
                    End If
                    Setup.Set("HintHide", True)
                End If
                WriteIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "DisplayType", McVersionCardType.Hidden)
                WriteIni(PathMcFolder & "PCL.ini", "VersionCache", "") '要求刷新缓存
                LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Catch ex As Exception
                Log(ex, "隐藏版本 " & PageVersionLeft.Version.Name & " 失败", LogLevel.Feedback)
            End Try
        End If
    End Sub
    '更改描述
    Private Sub BtnDisplayDesc_Click(sender As Object, e As EventArgs) Handles BtnDisplayDesc.Click
        Try
            Dim OldInfo As String = ReadIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "CustomInfo")
            Dim NewInfo As String = MyMsgBoxInput(OldInfo, New ObjectModel.Collection(Of Validate), "留空即为使用默认描述", "更改描述",, "取消")
            If NewInfo IsNot Nothing AndAlso OldInfo <> NewInfo Then WriteIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "CustomInfo", NewInfo)
            PageVersionLeft.Version = New McVersion(PageVersionLeft.Version.Name).Load()
            Reload()
            LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Log(ex, "版本 " & PageVersionLeft.Version.Name & " 描述更改失败", LogLevel.Msgbox)
        End Try
    End Sub
    '重命名版本
    Private Sub BtnDisplayRename_Click(sender As Object, e As EventArgs) Handles BtnDisplayRename.Click
        Try
            '确认输入的新名称
            Dim OldName As String = PageVersionLeft.Version.Name
            Dim OldPath As String = PageVersionLeft.Version.Path
            '修改此部分的同时修改快速安装的版本名检测*
            Dim NewName As String = MyMsgBoxInput(OldName, New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(PathMcFolder & "versions", IgnoreCase:=False)},, "重命名版本",, "取消")
            If String.IsNullOrWhiteSpace(NewName) Then Exit Sub
            Dim NewPath As String = PathMcFolder & "versions\" & NewName & "\"
            '获取临时中间名，以防止仅修改大小写的重命名失败
            Dim TempName As String = NewName & "_temp"
            Dim TempPath As String = PathMcFolder & "versions\" & TempName & "\"
            Dim IsCaseChangedOnly As Boolean = NewName.ToLower = OldName.ToLower
            '重新加载版本 Json 信息，避免 HMCL 项被合并
            Dim JsonObject As JObject
            Try
                JsonObject = GetJson(ReadFile(PageVersionLeft.Version.Path & PageVersionLeft.Version.Name & ".json"))
            Catch ex As Exception
                Log(ex, "重命名读取 Json 时失败")
                JsonObject = PageVersionLeft.Version.JsonObject
            End Try
            '重命名主文件夹
            My.Computer.FileSystem.RenameDirectory(OldPath, TempName)
            My.Computer.FileSystem.RenameDirectory(TempPath, NewName)
            '遍历重命名所有文件与文件夹
            For Each Entry As DirectoryInfo In New DirectoryInfo(NewPath).EnumerateDirectories
                If Not Entry.Name.Contains(OldName) Then Continue For
                If IsCaseChangedOnly Then
                    My.Computer.FileSystem.RenameDirectory(Entry.FullName, Entry.Name & "_temp")
                    My.Computer.FileSystem.RenameDirectory(Entry.FullName & "_temp", Entry.Name.Replace(OldName, NewName))
                Else
                    DeleteDirectory(NewPath & Entry.Name.Replace(OldName, NewName))
                    My.Computer.FileSystem.RenameDirectory(Entry.FullName, Entry.Name.Replace(OldName, NewName))
                End If
            Next
            For Each Entry As FileInfo In New DirectoryInfo(NewPath).EnumerateFiles
                If Not Entry.Name.Contains(OldName) Then Continue For
                If IsCaseChangedOnly Then
                    My.Computer.FileSystem.RenameFile(Entry.FullName, Entry.Name & "_temp")
                    My.Computer.FileSystem.RenameFile(Entry.FullName & "_temp", Entry.Name.Replace(OldName, NewName))
                Else
                    If File.Exists(NewPath & Entry.Name.Replace(OldName, NewName)) Then File.Delete(NewPath & Entry.Name.Replace(OldName, NewName))
                    My.Computer.FileSystem.RenameFile(Entry.FullName, Entry.Name.Replace(OldName, NewName))
                End If
            Next
            '替换版本设置文件中的路径
            If File.Exists(NewPath & "PCL\Setup.ini") Then
                WriteFile(NewPath & "PCL\Setup.ini", ReadFile(NewPath & "PCL\Setup.ini").Replace(OldPath, NewPath))
            End If
            '更改已选中的版本
            If ReadIni(PathMcFolder & "PCL.ini", "Version") = OldName Then
                WriteIni(PathMcFolder & "PCL.ini", "Version", NewName)
            End If
            '更改版本 Json
            If File.Exists(NewPath & NewName & ".json") Then
                Try
                    JsonObject("id") = NewName
                    WriteFile(NewPath & NewName & ".json", JsonObject.ToString)
                Catch ex As Exception
                    Log(ex, "重命名 Json 时失败")
                End Try
            End If
            '刷新与提示
            Hint("重命名成功！", HintType.Finish)
            PageVersionLeft.Version = New McVersion(NewName).Load()
            If Not IsNothing(McVersionCurrent) AndAlso McVersionCurrent.Equals(PageVersionLeft.Version) Then WriteIni(PathMcFolder & "PCL.ini", "Version", NewName)
            Reload()
            LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Log(ex, "重命名版本失败", LogLevel.Msgbox)
        End Try
    End Sub
    '收藏夹
    Private Sub BtnDisplayStar_Click(sender As Object, e As EventArgs) Handles BtnDisplayStar.Click
        Try
            WriteIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "IsStar", Not PageVersionLeft.Version.IsStar)
            PageVersionLeft.Version = New McVersion(PageVersionLeft.Version.Name).Load()
            Reload()
            McVersionListForceRefresh = True
            LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Log(ex, "版本 " & PageVersionLeft.Version.Name & " 收藏状态更改失败", LogLevel.Msgbox)
        End Try
    End Sub
    '补全文件
    Private Sub BtnManageCheck_Click(sender As Object, e As EventArgs) Handles BtnManageCheck.Click
        Try
            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = PageVersionLeft.Version.Name & " 文件补全" Then
                        Hint("正在处理中，请稍候！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock
            '启动
            Dim Loader As New LoaderCombo(Of String)(PageVersionLeft.Version.Name & " 文件补全", DlClientFix(PageVersionLeft.Version, True, AssetsIndexExistsBehaviour.AlwaysDownload, False))
            Loader.OnStateChanged = Sub()
                                        Select Case Loader.State
                                            Case LoadState.Finished
                                                Hint(Loader.Name & "成功！", HintType.Finish)
                                            Case LoadState.Failed
                                                Hint(Loader.Name & "失败：" & GetString(Loader.Error), HintType.Critical)
                                            Case LoadState.Aborted
                                                Hint(Loader.Name & "已取消！", HintType.Info)
                                        End Select
                                    End Sub
            Loader.Start(PageVersionLeft.Version.Name)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Log(ex, "尝试补全文件失败（" & PageVersionLeft.Version.Name & "）", LogLevel.Msgbox)
        End Try
    End Sub
    '删除版本
    Private Sub BtnManageDelete_Click(sender As Object, e As EventArgs) Handles BtnManageDelete.Click
        '修改此代码时，同时修改 PageSelectRight 中的代码
        Try
            Dim IsHintIndie As Boolean = PageVersionLeft.Version.State <> McVersionState.Error AndAlso PageVersionLeft.Version.PathIndie <> PathMcFolder
            Select Case MyMsgBox("你确定要删除版本 " & PageVersionLeft.Version.Name & " 吗？" &
                        If(IsHintIndie, vbCrLf & "由于该版本开启了版本隔离，删除版本时该版本对应的存档、资源包、Mod 等文件也将被一并删除！", ""),
                        "版本删除确认", , "取消",, True)
                Case 1
                    FileIO.FileSystem.DeleteDirectory(PageVersionLeft.Version.Path, FileIO.UIOption.AllDialogs, FileIO.RecycleOption.SendToRecycleBin)
                    Hint("版本 " & PageVersionLeft.Version.Name & " 已删除到回收站！", HintType.Finish)
                Case 2
                    '    DeleteDirectory(PageVersionLeft.Version.Path)
                    '    Hint("版本 " & PageVersionLeft.Version.Name & " 已永久删除！", HintType.Finish)
                    'Case 3
                    Exit Sub
            End Select
            LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            FrmMain.PageBack()
        Catch ex As Exception
            Log(ex, "删除版本 " & PageVersionLeft.Version.Name & " 失败", LogLevel.Msgbox)
        End Try
    End Sub
    '打开版本文件夹
    Private Sub BtnManageFolder_Click() Handles BtnManageFolder.Click
        OpenVersionFolder(PageVersionLeft.Version)
    End Sub
    Public Shared Sub OpenVersionFolder(Version As McVersion)
        OpenExplorer("""" & Version.Path & """")
    End Sub
    '版本图标
    Private Sub ComboDisplayLogo_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboDisplayLogo.SelectionChanged
        If Not (IsLoad AndAlso AniControlEnabled = 0) Then Exit Sub
        '选择 自定义 时修改图片
        Try
            If ComboDisplayLogo.SelectedItem Is ItemDisplayLogoCustom Then
                Dim FileName As String = SelectFile("常用图片文件(*.png;*.jpg;*.gif)|*.png;*.jpg;*.gif", "选择图片")
                If FileName = "" Then
                    Reload() '还原选项
                    Exit Sub
                End If
                File.Delete(PageVersionLeft.Version.Path & "PCL\Logo.png")
                Directory.CreateDirectory(PageVersionLeft.Version.Path & "PCL") '虽然不知道为啥，有时候真没这文件夹
                File.Copy(FileName, PageVersionLeft.Version.Path & "PCL\Logo.png")
            Else
                File.Delete(PageVersionLeft.Version.Path & "PCL\Logo.png")
            End If
        Catch ex As Exception
            Log(ex, "更改自定义版本图标失败（" & PageVersionLeft.Version.Name & "）", LogLevel.Feedback)
        End Try
        '进行更改
        Try
            Dim NewLogo As String = ComboDisplayLogo.SelectedItem.Tag
            WriteIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "Logo", NewLogo)
            WriteIni(PageVersionLeft.Version.Path & "PCL\Setup.ini", "LogoCustom", Not NewLogo = "")
            '刷新显示
            WriteIni(PathMcFolder & "PCL.ini", "VersionCache", "") '要求刷新缓存
            PageVersionLeft.Version = New McVersion(PageVersionLeft.Version.Name).Load()
            Reload()
            LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Log(ex, "更改版本图标失败（" & PageVersionLeft.Version.Name & "）", LogLevel.Feedback)
        End Try
    End Sub

#End Region

End Class
