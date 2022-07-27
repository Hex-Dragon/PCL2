Public Class PageDownloadCfDetail
    Private CfItem As MyCfItem = Nothing
    Private Project As DlCfProject

#Region "加载器"

    '初始化加载器信息
    Private Sub PageDownloadCfDetail_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        Project = FrmMain.PageCurrent.Additional
        PageLoaderInit(Load, PanLoad, PanMain, CardIntro, DlCfFileLoader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Private Function LoaderInput() As KeyValuePair(Of Integer, Boolean)
        Return New KeyValuePair(Of Integer, Boolean)(Project.Id, Project.IsModPack)
    End Function
    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case DlCfFileLoader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If DlCfFileLoader.Error IsNot Nothing Then ErrorMessage = DlCfFileLoader.Error.Message
                If ErrorMessage.Contains("不是有效的 Json 文件") Then
                    Log("[Download] 下载的 Mod 列表 Json 文件损坏，已自动重试", LogLevel.Debug)
                    PageLoaderRestart()
                End If
        End Select
    End Sub
    '结果 UI 化
    Private Class VersionSorterWithSelect
        Implements IComparer(Of String)
        Public Top As String = ""
        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare
            If x = y Then Return 0
            If x = Top Then Return -1
            If y = Top Then Return 1
            Return -VersionSortInteger(x, y)
        End Function
        Public Sub New(Optional Top As String = "")
            Me.Top = If(Top, "")
        End Sub
    End Class
    Private Sub Load_OnFinish()

        '初始化字典，并将当前版本排在最上面
        Dim TopVersion As String
        If Project.IsModPack Then
            TopVersion = If(PageDownloadPack.Loader.Input.GameVersion, "")
        Else
            TopVersion = If(PageDownloadMod.Loader.Input.GameVersion, "")
        End If
        Dim Dict As New SortedDictionary(Of String, List(Of DlCfFile))(New VersionSorterWithSelect(TopVersion))
        'PCL#8826 汇报了奇怪的这个 Try 块里出现 NPE 的问题，但实在找不到，就瞎加点检测
        Try
            Dict.Add("未知版本", New List(Of DlCfFile))
            If DlCfFileLoader.Output Is Nothing Then
                Log("[Download] 列表加载输出为 Nothing，请反馈此问题", LogLevel.Feedback)
                Exit Try
            End If
            For Each Version As DlCfFile In DlCfFileLoader.Output
                If Version Is Nothing Then
                    Log("[Download] 列表中的一个版本为 Nothing，请反馈此问题", LogLevel.Feedback)
                    Continue For
                End If
                If Version.GameVersion Is Nothing Then
                    Log("[Download] 列表中的一个版本没有任何适配的游戏版本，返回为 Nothing，请反馈此问题", LogLevel.Feedback)
                    Continue For
                End If
                For Each GameVersion In Version.GameVersion
                    '决定添加到哪个版本
                    Dim TargetVersion As String
                    If GameVersion Is Nothing OrElse GameVersion.Split(".").Count < 2 Then
                        TargetVersion = "未知版本"
                    Else
                        TargetVersion = GameVersion
                    End If
                    '实际进行添加
                    If Not Dict.ContainsKey(TargetVersion) Then Dict.Add(TargetVersion, New List(Of DlCfFile))
                    If Not Dict(TargetVersion).Contains(Version) Then Dict(TargetVersion).Add(Version)
                Next
            Next
        Catch ex As Exception
            Log(ex, "准备工程下载列表出错", LogLevel.Feedback)
        End Try

#Region "转化为 UI"
        Try
            '清空当前
            PanMain.Children.Clear()
            '转化为 UI
            For Each Pair As KeyValuePair(Of String, List(Of DlCfFile)) In Dict
                If Pair.Value.Count = 0 Then Continue For
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Pair.Key, .Margin = New Thickness(0, 0, 0, 15), .SwapType = If(Project.IsModPack, 9, 8)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                PanMain.Children.Add(NewCard)
                '确定卡片是否展开
                If Pair.Key = TopVersion Then
                    MyCard.StackInstall(NewStack, 8, Pair.Key)
                Else
                    NewCard.IsSwaped = True
                End If
                '增加提示
                If Pair.Key = "未知版本" Then
                    NewStack.Children.Add(New MyHint With {.Text = "由于 API 的版本信息更新缓慢，可能无法识别刚更新不久的 MC 版本，只需等待几天即可自动恢复正常。", .IsWarn = False, .Margin = New Thickness(0, 0, 0, 7)})
                End If
            Next
            '如果只有一张卡片，展开第一张卡片
            If PanMain.Children.Count = 1 Then
                CType(PanMain.Children(0), MyCard).IsSwaped = False
            End If
        Catch ex As Exception
            Log(ex, "可视化工程下载列表出错", LogLevel.Feedback)
        End Try
#End Region

    End Sub

#End Region

    Private IsFirstInit As Boolean = True
    Public Sub Init() Handles Me.OnPageEnter
        AniControlEnabled += 1
        Project = FrmMain.PageCurrent.Additional
        PanBack.ScrollToHome()

        '重启加载器
        If IsFirstInit Then
            '在 Me.Initialized 已经初始化了加载器，不再重复初始化
            IsFirstInit = False
        Else
            PageLoaderRestart(IsForceRestart:=True)
        End If

        '放置当前工程
        If CfItem IsNot Nothing Then PanIntro.Children.Remove(CfItem)
        CfItem = Project.ToCfItem(True, True)
        CfItem.Margin = New Thickness(-7, -7, 0, 8)
        PanIntro.Children.Insert(0, CfItem)

        '决定按钮显示
        BtnIntroWiki.Visibility = If(Project.McWikiId = 0, Visibility.Collapsed, Visibility.Visible)
        BtnIntroMCBBS.Visibility = If(Project.MCBBS Is Nothing, Visibility.Collapsed, Visibility.Visible)

        AniControlEnabled -= 1
    End Sub

    '整合包下载（安装）
    Public Sub Install_Click(sender As MyListItem, e As EventArgs)
        Try

            '获取基本信息
            Dim File As DlCfFile = sender.Tag
            Dim LoaderName As String = "CurseForge 整合包下载：" & Project.ChineseName & " "

            '获取版本名
            Dim PackName As String = Project.ChineseName.Replace(".zip", "").Replace(".rar", "").Replace("\", "＼").Replace("/", "／").Replace("|", "｜").Replace(":", "：").Replace("<", "＜").Replace(">", "＞").Replace("*", "＊").Replace("?", "？").Replace("""", "").Replace("： ", "：")
            Dim Validate As New ValidateFolderName(PathMcFolder & "versions")
            If Validate.Validate(PackName) <> "" Then PackName = ""
            Dim VersionName As String = MyMsgBoxInput(PackName, New ObjectModel.Collection(Of Validate) From {Validate},
                                                  Title:="输入版本名", Button2:="取消")
            If String.IsNullOrEmpty(VersionName) Then Exit Sub

            '构造步骤加载器
            Dim Loaders As New List(Of LoaderBase)
            Dim Target As String = PathMcFolder & "versions\" & VersionName & "\原始整合包.zip"
            Loaders.Add(New LoaderDownload("下载整合包文件", New List(Of NetFile) From {File.GetDownloadFile(Target, True)}) With {.ProgressWeight = 10, .Block = True})
            Loaders.Add(New LoaderTask(Of Integer, Integer)("准备安装整合包", Sub() ModpackInstall(Target, VersionName)) With {.ProgressWeight = 0.1})

            '启动
            Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = Sub(MyLoader)
                                                                                                      Select Case MyLoader.State
                                                                                                          Case LoadState.Failed
                                                                                                              Hint(MyLoader.Name & "失败：" & GetString(MyLoader.Error), HintType.Critical)
                                                                                                          Case LoadState.Aborted
                                                                                                              Hint(MyLoader.Name & "已取消！", HintType.Info)
                                                                                                          Case LoadState.Loading
                                                                                                              Exit Sub '不重新加载版本列表
                                                                                                      End Select
                                                                                                      McInstallFailedClearFolder(MyLoader)
                                                                                                  End Sub}
            Loader.Start(PathMcFolder & "versions\" & VersionName & "\")
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "下载 CurseForge 整合包失败", LogLevel.Feedback)
        End Try
    End Sub
    'Mod 下载；整合包另存为
    Public Shared DownloadFolder = Nothing '仅在本次缓存的下载文件夹
    Public Sub Save_Click(sender As Object, e As EventArgs)
        Try
            Dim Desc As String = If(Project.IsModPack, "整合包", "Mod ")

            '确认默认保存位置
            Dim DefaultFolder As String = Nothing
            If Not Project.IsModPack Then
                DefaultFolder = DownloadFolder
                If McVersionCurrent IsNot Nothing Then
                    If Not McVersionCurrent.IsLoaded Then McVersionCurrent.Load()
                    If McVersionCurrent.Version.Modable Then
                        DefaultFolder = McVersionCurrent.PathIndie & "mods\"
                        Directory.CreateDirectory(DefaultFolder)
                    End If
                End If
                If String.IsNullOrEmpty(DefaultFolder) Then DefaultFolder = Nothing
            End If

            '获取基本信息
            Dim File As DlCfFile = If(TypeOf sender Is MyListItem, sender, sender.Parent).Tag
            Dim ChineseName As String = If(Project.ChineseName = Project.Name, "",
                Project.ChineseName.Replace(" (", "Å").Split("Å").First.Replace("\", "＼").Replace("/", "／").Replace("|", "｜").Replace(":", "：").Replace("<", "＜").Replace(">", "＞").Replace("*", "＊").Replace("?", "？").Replace("""", "").Replace("： ", "："))
            Dim FileName As String
            Select Case Setup.Get("ToolDownloadTranslate")
                Case 0
                    FileName = If(ChineseName = "", "", "[" & ChineseName & "] ") & File.FileName
                Case 1
                    FileName = If(ChineseName = "", "", ChineseName & "-") & File.FileName
                Case 2
                    FileName = File.FileName & If(ChineseName = "", "", "-" & ChineseName)
                Case Else
                    FileName = File.FileName
            End Select
            Dim Target As String
            If File.FileName.EndsWith(".litemod") Then
                Target = SelectAs("选择保存位置", FileName, Desc & "文件|" & If(Project.IsModPack, "*.zip", "*.litemod"), DefaultFolder)
            Else
                Target = SelectAs("选择保存位置", FileName, Desc & "文件|" & If(Project.IsModPack, "*.zip", "*.jar"), DefaultFolder)
            End If
            If Not Target.Contains("\") Then Exit Sub
            Dim LoaderName As String = Desc & "下载：" & File.DisplayName & " "
            If Target <> DefaultFolder AndAlso Not Project.IsModPack Then DownloadFolder = GetPathFromFullPath(Target)

            '构造步骤加载器
            Dim Loaders As New List(Of LoaderBase)
            Loaders.Add(New LoaderDownload("下载文件", New List(Of NetFile) From {File.GetDownloadFile(Target, True)}) With {.ProgressWeight = 6, .Block = True})

            '启动
            Dim Loader As New LoaderCombo(Of Integer)(LoaderName, Loaders) With {.OnStateChanged = AddressOf DownloadStateSave}
            Loader.Start(1)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Log(ex, "保存 CurseForge 文件失败", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub BtnIntroCf_Click(sender As Object, e As EventArgs) Handles BtnIntroCf.Click
        OpenWebsite(Project.Website)
    End Sub
    Private Sub BtnIntroWiki_Click(sender As Object, e As EventArgs) Handles BtnIntroWiki.Click
        OpenWebsite("https://www.mcmod.cn/class/" & Project.McWikiId & ".html")
    End Sub
    Private Sub BtnIntroMCBBS_Click(sender As Object, e As EventArgs) Handles BtnIntroMCBBS.Click
        OpenWebsite("https://www.mcbbs.net/thread-" & Project.MCBBS & "-1-1.html")
    End Sub

End Class
