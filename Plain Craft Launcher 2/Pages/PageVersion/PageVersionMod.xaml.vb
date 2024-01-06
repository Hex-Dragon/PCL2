Public Class PageVersionMod

#Region "初始化"

    Private IsLoad As Boolean = False
    Public Sub PageOther_Loaded() Handles Me.Loaded

        If FrmMain.PageLast.Page <> FormMain.PageType.CompDetail Then PanBack.ScrollToHome()
        AniControlEnabled += 1
        SelectedMods.Clear()
        RefreshList()
        ChangeAllSelected(False)
        AniControlEnabled -= 1

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

#If DEBUG Then
        BtnManageCheck.Visibility = Visibility.Visible
#End If

    End Sub
    ''' <summary>
    ''' 刷新 Mod 列表。
    ''' </summary>
    Public Sub RefreshList(Optional ForceReload As Boolean = False)
        If McModLoader.State = LoadState.Loading Then Exit Sub
        If LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", If(ForceReload, LoaderFolderRunType.ForceRun, LoaderFolderRunType.RunOnUpdated)) Then
            Log("[System] 已刷新 Mod 列表")
            PanBack.ScrollToHome()
            SearchBox.Text = ""
        End If
    End Sub

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, McModLoader, AddressOf LoadUIFromLoaderOutput, AutoRun:=False)
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        If McModLoader.State = LoadState.Failed Then
            LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", LoaderFolderRunType.ForceRun)
        End If
    End Sub

#End Region

#Region "UI 化"

    ''' <summary>
    ''' 已加载的 Mod UI 缓存，不确保按显示顺序排列。Key 为 Mod 的 RawFileName。
    ''' </summary>
    Public ModItems As New Dictionary(Of String, MyLocalModItem)
    ''' <summary>
    ''' 将加载器结果的 Mod 列表加载为 UI。
    ''' </summary>
    Private Sub LoadUIFromLoaderOutput()
        Dim Mods As List(Of McMod) = McModLoader.Output
        Try
            '判断应该显示哪一个页面
            If Mods.Any() Then
                PanBack.Visibility = Visibility.Visible
                PanEmpty.Visibility = Visibility.Collapsed
            Else
                PanEmpty.Visibility = Visibility.Visible
                PanBack.Visibility = Visibility.Collapsed
                Exit Sub
            End If
            SearchBox.Text = ""
            '输出结果
            ModItems.Clear()
            For Each ModEntity As McMod In Mods
                ModItems(ModEntity.RawFileName) = McModListItem(ModEntity)
            Next
            RefreshResult(Mods)
        Catch ex As Exception
            Log(ex, "加载 Mod 列表 UI 失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Function McModListItem(Entry As McMod) As MyLocalModItem
        AniControlEnabled += 1
        Dim NewItem As New MyLocalModItem With {.SnapsToDevicePixels = True, .Entry = Entry,
            .ButtonHandler = AddressOf McModContent, .Checked = SelectedMods.Contains(Entry.RawFileName)}
        AddHandler Entry.OnGetCompProject, AddressOf NewItem.Refresh
        NewItem.Refresh()
        AniControlEnabled -= 1
        Return NewItem
    End Function
    Private Sub McModContent(sender As MyLocalModItem, e As EventArgs)
        '点击事件
        AddHandler sender.Changed, AddressOf CheckChanged
        AddHandler sender.Click, Sub(ss As MyLocalModItem, ee As EventArgs) ss.Checked = Not ss.Checked
        '图标按钮
        Dim BtnOpen As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonOpen, .Tag = sender}
        BtnOpen.ToolTip = "打开文件位置"
        ToolTipService.SetPlacement(BtnOpen, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnOpen, 30)
        ToolTipService.SetHorizontalOffset(BtnOpen, 2)
        AddHandler BtnOpen.Click, AddressOf Open_Click
        Dim BtnCont As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonInfo, .Tag = sender}
        BtnCont.ToolTip = "详情"
        ToolTipService.SetPlacement(BtnCont, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnCont, 30)
        ToolTipService.SetHorizontalOffset(BtnCont, 2)
        AddHandler BtnCont.Click, AddressOf Info_Click
        AddHandler sender.MouseRightButtonDown, AddressOf Info_Click
        Dim BtnDelete As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonDelete, .Tag = sender}
        BtnDelete.ToolTip = "删除"
        ToolTipService.SetPlacement(BtnDelete, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnDelete, 30)
        ToolTipService.SetHorizontalOffset(BtnDelete, 2)
        AddHandler BtnDelete.Click, AddressOf Delete_Click
        Dim BtnED As New MyIconButton With {.LogoScale = 1, .Logo = If(sender.Entry.State = McMod.McModState.Fine,
            "M508 990.4c-261.6 0-474.4-212-474.4-474.4S246.4 41.6 508 41.6s474.4 212 474.4 474.4S769.6 990.4 508 990.4zM508 136.8c-209.6 0-379.2 169.6-379.2 379.2 0 209.6 169.6 379.2 379.2 379.2s379.2-169.6 379.2-379.2C887.2 306.4 717.6 136.8 508 136.8zM697.6 563.2 318.4 563.2c-26.4 0-47.2-21.6-47.2-47.2 0-26.4 21.6-47.2 47.2-47.2l379.2 0c26.4 0 47.2 21.6 47.2 47.2C744.8 542.4 724 563.2 697.6 563.2z",
            "M512 0a512 512 0 1 0 512 512A512 512 0 0 0 512 0z m0 921.6a409.6 409.6 0 1 1 409.6-409.6 409.6 409.6 0 0 1-409.6 409.6z M716.8 339.968l-256 253.44L328.192 460.8A51.2 51.2 0 0 0 256 532.992l168.448 168.96a51.2 51.2 0 0 0 72.704 0l289.28-289.792A51.2 51.2 0 0 0 716.8 339.968z"),
            .Tag = sender}
        BtnED.ToolTip = If(sender.Entry.State = McMod.McModState.Fine, "禁用", "启用")
        ToolTipService.SetPlacement(BtnED, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnED, 30)
        ToolTipService.SetHorizontalOffset(BtnED, 2)
        AddHandler BtnED.Click, AddressOf ED_Click
        If sender.Entry.State = McMod.McModState.Unavaliable Then
            sender.Buttons = {BtnCont, BtnOpen, BtnDelete}
        Else
            sender.Buttons = {BtnCont, BtnOpen, BtnED, BtnDelete}
        End If
    End Sub

    ''' <summary>
    ''' 刷新结果显示。
    ''' </summary>
    Private Sub RefreshResult(Mods As List(Of McMod))
        PanList.Children.Clear()
        For Each TargetMod In Mods
            PanList.Children.Add(ModItems(TargetMod.RawFileName))
        Next
        RefreshTitle()
    End Sub
    ''' <summary>
    ''' 刷新卡片标题。
    ''' </summary>
    Private Sub RefreshTitle()
        Dim Mods = PanList.Children.Cast(Of MyLocalModItem).Select(Function(i) i.Entry).ToList
        Dim Counter = {0, 0, 0}
        For Each ModEntity As McMod In Mods
            Counter(ModEntity.State) += 1
        Next
        Dim TypeList As New List(Of String)
        If Counter(McMod.McModState.Disabled) > 0 Then TypeList.Add("禁用 " & Counter(McMod.McModState.Disabled))
        If Counter(McMod.McModState.Unavaliable) > 0 Then TypeList.Add("错误 " & Counter(McMod.McModState.Unavaliable))
        If Counter(McMod.McModState.Fine) > 0 Then TypeList.Insert(0, If(TypeList.Any, "启用 ", "") & Counter(McMod.McModState.Fine))
        If Not IsSearching Then
            PanListBack.Title = "Mod 列表 (" & Join(TypeList, "，") & ")"
        ElseIf TypeList.Any() Then
            PanListBack.Title = "搜索结果 (" & Join(TypeList, "，") & ")"
        Else
            PanListBack.Title = "无搜索结果"
        End If
        PanList.Visibility = If(Mods.Any(), Visibility.Visible, Visibility.Collapsed)
    End Sub

#End Region

#Region "管理"

    ''' <summary>
    ''' 打开 Mods 文件夹。
    ''' </summary>
    Private Sub BtnManageOpen_Click(sender As Object, e As EventArgs) Handles BtnManageOpen.Click, BtnHintOpen.Click
        Try
            Directory.CreateDirectory(PageVersionLeft.Version.PathIndie & "mods\")
            OpenExplorer("""" & PageVersionLeft.Version.PathIndie & "mods\""")
        Catch ex As Exception
            Log(ex, "打开 Mods 文件夹失败", LogLevel.Msgbox)
        End Try
    End Sub

#If DEBUG Then
    ''' <summary>
    ''' 检查 Mod。
    ''' </summary>
    Private Sub BtnManageCheck_Click(sender As Object, e As EventArgs) Handles BtnManageCheck.Click
        Try
            Dim Result = McModCheck(PageVersionLeft.Version, McModLoader.Output)
            If Result.Count > 0 Then
                MyMsgBox(Join(Result, vbCrLf & vbCrLf), "Mod 检查结果")
            Else
                Hint("Mod 检查完成，未发现任何问题！", HintType.Finish)
            End If
        Catch ex As Exception
            Log(ex, "进行 Mod 检查时出错", LogLevel.Feedback)
        End Try
    End Sub
#End If

    ''' <summary>
    ''' 全选。
    ''' </summary>
    Private Sub BtnManageSelectAll_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageSelectAll.Click
        If SelectedMods.Count < PanList.Children.Count Then
            ChangeAllSelected(True)
        Else
            ChangeAllSelected(False)
        End If
    End Sub

    ''' <summary>
    ''' 安装 Mod。
    ''' </summary>
    Private Sub BtnManageInstall_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageInstall.Click
        Hint("将 Mod 文件直接拖入 PCL 窗口即可安装！")
    End Sub

#End Region

#Region "选择"

    '选择的 Mod 的路径（不含 .disabled）
    Public SelectedMods As New List(Of String)

    '单项切换选择状态
    Public Sub CheckChanged(sender As MyLocalModItem, e As RouteEventArgs)
        If AniControlEnabled <> 0 Then Return
        '更新选择了的内容
        Dim SelectedKey As String = sender.Entry.RawFileName
        If sender.Checked Then
            If Not SelectedMods.Contains(SelectedKey) Then SelectedMods.Add(SelectedKey)
        Else
            SelectedMods.Remove(SelectedKey)
        End If
        '更新下边栏 UI
        RefreshBottomBar()
    End Sub

    '改变下边栏状态
    Private ShownCount As Integer = 0
    Private Sub RefreshBottomBar()
        '计数
        Dim NewCount As Integer = SelectedMods.Count
        Dim Selected = NewCount > 0
        If Selected Then LabSelect.Text = $"已选择 {NewCount} 个文件" '取消所有选择时不更新数字
        '按钮可用性
        If Selected Then
            Dim HasEnabled As Boolean = False
            Dim HasDisabled As Boolean = False
            For Each ModEntity In McModLoader.Output
                If SelectedMods.Contains(ModEntity.RawFileName) Then
                    If ModEntity.State = McMod.McModState.Fine Then
                        HasEnabled = True
                    ElseIf ModEntity.State = McMod.McModState.Disabled Then
                        HasDisabled = True
                    End If
                End If
            Next
            BtnSelectDisable.IsEnabled = HasEnabled
            BtnSelectEnable.IsEnabled = HasDisabled
        End If
        '更新显示状态
        CardSelect.IsHitTestVisible = Selected
        If AniControlEnabled = 0 Then
            If Selected Then
                '仅在数量增加时播放出现/跳跃动画
                If ShownCount >= NewCount Then
                    ShownCount = NewCount
                    Return
                Else
                    ShownCount = NewCount
                End If
                '出现/跳跃动画
                CardSelect.Visibility = Visibility.Visible
                AniStart({
                    AaOpacity(CardSelect, 1 - CardSelect.Opacity, 60),
                    AaTranslateY(CardSelect, -27 - TransSelect.Y, 120, Ease:=New AniEaseOutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, 3, 150, 120, Ease:=New AniEaseInoutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, -1, 90, 270, Ease:=New AniEaseInoutFluent(AniEasePower.Weak))
                }, "Mod Sidebar")
            Else
                '不重复播放隐藏动画
                If ShownCount = 0 Then Return
                ShownCount = 0
                '隐藏动画
                AniStart({
                    AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                    AaTranslateY(CardSelect, -10 - TransSelect.Y, 90, Ease:=New AniEaseInFluent(AniEasePower.Weak)),
                    AaCode(Sub() CardSelect.Visibility = Visibility.Collapsed, After:=True)
                }, "Mod Sidebar")
            End If
        Else
            AniStop("Mod Sidebar")
            ShownCount = NewCount
            If Selected Then
                CardSelect.Visibility = Visibility.Visible
                CardSelect.Opacity = 1
                TransSelect.Y = -25
            Else
                CardSelect.Visibility = Visibility.Collapsed
                CardSelect.Opacity = 0
                TransSelect.Y = -10
            End If
        End If
    End Sub

    '切换所有项的选择状态
    Private Sub ChangeAllSelected(Value As Boolean)
        AniControlEnabled += 1
        SelectedMods.Clear()
        For Each Item As MyLocalModItem In PanList.Children
            Item.Checked = Value
            If Value Then SelectedMods.Add(Item.Entry.RawFileName)
        Next
        AniControlEnabled -= 1
        '更新下边栏 UI
        RefreshBottomBar()
    End Sub
    Private Sub UnselectedAllWithAnimation() Handles Load.StateChanged, Me.PageExit
        Dim CacheAniControlEnabled = AniControlEnabled
        AniControlEnabled = 0
        ChangeAllSelected(False)
        AniControlEnabled += CacheAniControlEnabled
    End Sub
    Private Sub PageVersionMod_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If My.Computer.Keyboard.CtrlKeyDown AndAlso e.Key = Key.A Then ChangeAllSelected(True)
    End Sub

#End Region

#Region "下边栏"

    '启用 / 禁用
    Private Sub BtnSelectED_Click(sender As MyIconTextButton, e As RouteEventArgs) Handles BtnSelectEnable.Click, BtnSelectDisable.Click
        EDMods(McModLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName)),
               Not sender.Equals(BtnSelectDisable))
        ChangeAllSelected(False)
    End Sub
    Private Sub EDMods(ModList As IEnumerable(Of McMod), IsEnable As Boolean)
        Dim IsSuccessful As Boolean = True
        For Each ModEntity In ModList.ToList
            Dim NewPath As String = Nothing
            If ModEntity.State = McMod.McModState.Fine AndAlso Not IsEnable Then
                '禁用
                NewPath = ModEntity.Path & ".disabled"
            ElseIf ModEntity.State = McMod.McModState.Disabled AndAlso IsEnable Then
                '启用
                NewPath = ModEntity.Path.Replace(".disabled", "").Replace(".old", "")
            Else
                Continue For
            End If
            '重命名
            Try
                If File.Exists(NewPath) AndAlso Not File.Exists(ModEntity.Path) Then Continue For '因为未知原因 Mod 的状态已经切换完了
                File.Delete(NewPath)
                FileSystem.Rename(ModEntity.Path, NewPath)
            Catch ex As FileNotFoundException
                Log(ex, $"未找到需要重命名的 Mod（{If(ModEntity.Path, "null")}）", LogLevel.Feedback)
                RefreshList(True)
                Return
            Catch ex As Exception
                Log(ex, $"重命名 Mod 失败（{If(ModEntity.Path, "null")}）")
                IsSuccessful = False
            End Try
            '更改 Loader 中的列表
            Dim NewModEntity As New McMod(NewPath)
            NewModEntity.Comp = ModEntity.Comp
            Dim IndexOfLoader As Integer = McModLoader.Output.IndexOf(ModEntity)
            McModLoader.Output.RemoveAt(IndexOfLoader)
            McModLoader.Output.Insert(IndexOfLoader, NewModEntity)
            '更改 UI 中的列表
            Dim NewItem As MyLocalModItem = McModListItem(NewModEntity)
            ModItems(ModEntity.RawFileName) = NewItem
            Dim IndexOfUi As Integer = PanList.Children.IndexOf(PanList.Children.OfType(Of MyLocalModItem).FirstOrDefault(Function(i) i.Entry Is ModEntity))
            If IndexOfUi = -1 Then Continue For '因为未知原因 Mod 的状态已经切换完了
            PanList.Children.RemoveAt(IndexOfUi)
            PanList.Children.Insert(IndexOfUi, NewItem)
        Next
        RefreshTitle() '改变数量显示
        If Not IsSuccessful Then
            Hint("由于文件被占用，Mod 的状态切换失败，请尝试关闭正在运行的游戏后再试！", HintType.Critical)
            RefreshList(True)
        Else
            RefreshBottomBar()
        End If
    End Sub

    '删除
    Private Sub BtnSelectDelete_Click() Handles BtnSelectDelete.Click
        Dim DeleteList As List(Of McMod) = McModLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName)).ToList()
        DeleteMods(DeleteList)
    End Sub
    Private Sub DeleteMods(ModList As IEnumerable(Of McMod))
        Try
            Dim IsSuccessful As Boolean = True
            Dim IsShiftPressed As Boolean = My.Computer.Keyboard.ShiftKeyDown
            For Each ModEntity In ModList
                '删除
                Try
                    If IsShiftPressed Then
                        File.Delete(ModEntity.Path)
                    Else
                        My.Computer.FileSystem.DeleteFile(ModEntity.Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                    End If
                Catch ex As OperationCanceledException
                    Log(ex, "删除 Mod 被主动取消")
                    RefreshList(True)
                    Return
                Catch ex As Exception
                    Log(ex, $"删除 Mod 失败（{ModEntity.Path}）", LogLevel.Msgbox)
                    IsSuccessful = False
                End Try
                '取消选中
                SelectedMods.Remove(ModEntity.RawFileName)
                '更改 Loader 和 UI 中的列表
                McModLoader.Output.Remove(ModEntity)
                ModItems.Remove(ModEntity.RawFileName)
                Dim IndexOfUi As Integer = PanList.Children.IndexOf(PanList.Children.OfType(Of MyLocalModItem).First(Function(i) i.Entry Is ModEntity))
                PanList.Children.RemoveAt(IndexOfUi)
            Next
            RefreshTitle()
            If Not IsSuccessful Then
                Hint("由于文件被占用，Mod 删除失败，请尝试关闭正在运行的游戏后再试！", HintType.Critical)
                RefreshList(True)
            ElseIf PanList.Children.Count = 0 Then
                RefreshList(True) '删除了全部文件
            Else
                RefreshBottomBar()
            End If
            '显示结果提示
            If Not IsSuccessful Then Exit Sub
            If IsShiftPressed Then
                If ModList.Count = 1 Then
                    Hint($"已彻底删除 {ModList.Single.FileName}！", HintType.Finish)
                Else
                    Hint($"已彻底删除 {ModList.Count} 个文件！", HintType.Finish)
                End If
            Else
                If ModList.Count = 1 Then
                    Hint($"已将 {ModList.Single.FileName} 删除到回收站！", HintType.Finish)
                Else
                    Hint($"已将 {ModList.Count} 个文件删除到回收站！", HintType.Finish)
                End If
            End If
        Catch ex As OperationCanceledException
            Log(ex, "删除 Mod 被主动取消")
            RefreshList(True)
        Catch ex As Exception
            Log(ex, "删除 Mod 出现未知错误", LogLevel.Feedback)
            RefreshList(True)
        End Try
    End Sub

    '取消选择
    Private Sub BtnSelectCancel_Click() Handles BtnSelectCancel.Click
        ChangeAllSelected(False)
    End Sub

#End Region

#Region "单个 Mod 项"

    '详情
    Public Sub Info_Click(sender As Object, e As EventArgs)
        Try

            Dim ModEntry As McMod = CType(If(TypeOf sender Is MyIconButton, sender.Tag, sender), MyLocalModItem).Entry
            '加载失败信息
            If ModEntry.State = McMod.McModState.Unavaliable Then
                MyMsgBox("无法读取此 Mod 的信息。" & vbCrLf & vbCrLf & "详细的错误信息：" & GetExceptionDetail(ModEntry.FileUnavailableReason), "Mod 读取失败")
                Return
            End If
            If ModEntry.Comp IsNot Nothing Then
                '跳转到 Mod 下载页面
                FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CompDetail,
                                   .Additional = {ModEntry.Comp, New List(Of String), PageVersionLeft.Version.Version.McName,
                                   If(PageVersionLeft.Version.Version.HasForge, CompModLoaderType.Forge, If(PageVersionLeft.Version.Version.HasFabric, CompModLoaderType.Fabric, CompModLoaderType.Any))}})
            Else
                '获取信息
                Dim ContentLines As New List(Of String)
                If ModEntry.Description IsNot Nothing Then ContentLines.Add(ModEntry.Description & vbCrLf)
                If ModEntry.Authors IsNot Nothing Then ContentLines.Add("作者：" & ModEntry.Authors)
                ContentLines.Add("文件：" & ModEntry.FileName & "（" & GetString(New FileInfo(ModEntry.Path).Length) & "）")
                If ModEntry.Version IsNot Nothing Then ContentLines.Add("版本：" & ModEntry.Version)
                Dim DebugInfo As New List(Of String)
                If ModEntry.ModId IsNot Nothing Then
                    DebugInfo.Add("Mod ID：" & ModEntry.ModId)
                End If
                If ModEntry.Dependencies.Count > 0 Then
                    DebugInfo.Add("依赖于：")
                    For Each Dep In ModEntry.Dependencies
                        DebugInfo.Add(" - " & Dep.Key & If(Dep.Value Is Nothing, "", "，版本：" & Dep.Value))
                    Next
                End If
                If DebugInfo.Count > 0 Then
                    ContentLines.Add("")
                    ContentLines.AddRange(DebugInfo)
                End If
                '获取用于搜索的 Mod 名称
                Dim ModOriginalName As String = ModEntry.Name.Replace(" ", "+")
                Dim ModSearchName As String = ModOriginalName.Substring(0, 1)
                For i = 1 To ModOriginalName.Count - 1
                    Dim IsLastLower As Boolean = ModOriginalName(i - 1).ToString.ToLower.Equals(ModOriginalName(i - 1).ToString)
                    Dim IsCurrentLower As Boolean = ModOriginalName(i).ToString.ToLower.Equals(ModOriginalName(i).ToString)
                    If IsLastLower AndAlso Not IsCurrentLower Then
                        '上一个字母为小写，这一个字母为大写
                        ModSearchName += "+"
                    End If
                    ModSearchName += ModOriginalName(i)
                Next
                ModSearchName = ModSearchName.Replace("++", "+").Replace("pti+Fine", "ptiFine")
                '显示
                If ModEntry.Url Is Nothing Then
                    If MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "百科搜索", "返回") = 1 Then
                        OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                    End If
                Else
                    Select Case MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "打开官网", "百科搜索", "返回")
                        Case 1
                            OpenWebsite(ModEntry.Url)
                        Case 2
                            OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                    End Select
                End If
            End If
        Catch ex As Exception
            Log(ex, "获取 Mod 详情失败", LogLevel.Feedback)
        End Try
    End Sub
    '打开文件所在的位置
    Public Sub Open_Click(sender As MyIconButton, e As EventArgs)
        Try

            Dim ListItem As MyLocalModItem = sender.Tag
            OpenExplorer("/select,""" & ListItem.Entry.Path & """")

        Catch ex As Exception
            Log(ex, "打开 Mod 文件位置失败", LogLevel.Feedback)
        End Try
    End Sub
    '删除
    Public Sub Delete_Click(sender As MyIconButton, e As EventArgs)
        Dim ListItem As MyLocalModItem = sender.Tag
        DeleteMods({ListItem.Entry})
    End Sub
    '启用 / 禁用
    Public Sub ED_Click(sender As MyIconButton, e As EventArgs)
        Dim ListItem As MyLocalModItem = sender.Tag
        EDMods({ListItem.Entry}, ListItem.Entry.State = McMod.McModState.Disabled)
    End Sub

#End Region

#Region "搜索"

    Public ReadOnly Property IsSearching As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(SearchBox.Text)
        End Get
    End Property
    Public Sub SearchRun() Handles SearchBox.TextChanged
        ChangeAllSelected(False)
        If IsSearching Then
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of McMod))
            For Each Entry As McMod In McModLoader.Output
                Dim SearchSource As New List(Of KeyValuePair(Of String, Double))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Name, 1))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.FileName, 1))
                If Entry.Version IsNot Nothing Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Version, 0.2))
                End If
                If Entry.Description IsNot Nothing AndAlso Entry.Description <> "" Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Description, 0.4))
                End If
                If Entry.Comp IsNot Nothing Then
                    If Entry.Comp.RawName <> Entry.Name Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Comp.RawName, 1))
                    If Entry.Comp.TranslatedName <> Entry.Comp.RawName Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Comp.TranslatedName, 1))
                    If Entry.Comp.Description <> Entry.Description Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Comp.Description, 0.4))
                    SearchSource.Add(New KeyValuePair(Of String, Double)(String.Join("", Entry.Comp.Tags), 0.2))
                End If
                QueryList.Add(New SearchEntry(Of McMod) With {.Item = Entry, .SearchSource = SearchSource})
            Next
            '进行搜索
            Dim SearchResult = Search(QueryList, SearchBox.Text, MaxBlurCount:=6, MinBlurSimilarity:=0.35)
            RefreshResult(SearchResult.Select(Function(r) r.Item).ToList)
        Else
            '退出搜索状态
            RefreshResult(McModLoader.Output)
        End If
    End Sub

#End Region

End Class
