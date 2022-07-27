Public Class PageVersionMod

#Region "初始化"

    Private IsLoad As Boolean = False
    Public Sub PageOther_Loaded() Handles Me.Loaded

        PanBack.ScrollToHome()
        RefreshList()

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
            PanBack.ScrollToHome()
            SearchBox.Text = ""
        End If
    End Sub

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, McModLoader, AddressOf Load_Finish, AutoRun:=False)
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        If McModLoader.State = LoadState.Failed Then
            LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", LoaderFolderRunType.ForceRun)
        End If
    End Sub

#End Region

#Region "UI 化"

    ''' <summary>
    ''' 将 Mod 列表加载为 UI。
    ''' </summary>
    Private Sub Load_Finish(Loader As LoaderTask(Of String, List(Of McMod)))
        Dim List As List(Of McMod) = Loader.Output
        Try
            PanList.Children.Clear()

            '判断应该显示哪一个页面
            If List.Count = 0 Then
                PanEmpty.Visibility = Visibility.Visible
                PanBack.Visibility = Visibility.Collapsed
                Exit Sub
            Else
                PanBack.Visibility = Visibility.Visible
                PanEmpty.Visibility = Visibility.Collapsed
            End If

            '建立 StackPanel
            Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, If(List.Count > 0, 20, 0)), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0)}
            For Each ModEntity As McMod In List
                NewStack.Children.Add(McModListItem(ModEntity))
            Next
            '建立 MyCard
            Dim NewCard As New MyCard With {.Title = McModGetTitle(List), .Margin = New Thickness(0, 0, 0, 15)}
            NewCard.Children.Add(NewStack)
            PanList.Children.Add(NewCard)
            '显示提示
            If List.Count > 0 AndAlso Not Setup.Get("HintModDisable") Then
                Setup.Set("HintModDisable", True)
                Hint("直接点击某个 Mod 项即可将它禁用！")
            End If

        Catch ex As Exception
            Log(ex, "加载 Mod 列表 UI 失败", LogLevel.Feedback)
        End Try
    End Sub
    ''' <summary>
    ''' 获取 Card 的标题。
    ''' </summary>
    Private Function McModGetTitle(List As List(Of McMod)) As String
        Dim Counter = {0, 0, 0}
        For Each ModEntity As McMod In List
            Counter(ModEntity.State) += 1
        Next
        If List.Count = 0 Then
            '全空
            Return "未找到任何 Mod"
        ElseIf Counter(McMod.McModState.Disabled) = 0 AndAlso Counter(McMod.McModState.Unavaliable) = 0 Then
            '只有启用的 Mod
            Return "Mod 列表（" & Counter(McMod.McModState.Fine) & "）"
        Else
            '混合种类
            Dim TypeList As New List(Of String)
            If Counter(McMod.McModState.Fine) > 0 Then TypeList.Add("启用 " & Counter(McMod.McModState.Fine))
            If Counter(McMod.McModState.Disabled) > 0 Then TypeList.Add("禁用 " & Counter(McMod.McModState.Disabled))
            If Counter(McMod.McModState.Unavaliable) > 0 Then TypeList.Add("错误 " & Counter(McMod.McModState.Unavaliable))
            Return "Mod 列表（" & Join(TypeList, "，") & "）"
        End If
    End Function
    Private Function McModListItem(Entry As McMod) As MyListItem
        '图标
        Dim Logo As String
        Select Case Entry.State
            Case McMod.McModState.Fine
                Logo = "pack://application:,,,/images/Blocks/RedstoneLampOn.png"
            Case McMod.McModState.Disabled
                Logo = "pack://application:,,,/images/Blocks/RedstoneLampOff.png"
            Case Else '出错
                Logo = "pack://application:,,,/images/Blocks/RedstoneBlock.png"
        End Select
        '文本
        Dim Title As String
        If Entry.State = McMod.McModState.Disabled Then
            Title = GetFileNameWithoutExtentionFromPath(Entry.Path.Substring(0, Entry.Path.Count - ".disabled".Count)) & "（已禁用）"
        Else
            Title = GetFileNameWithoutExtentionFromPath(Entry.Path)
        End If
        Dim Desc As String
        If Entry.Version Is Nothing OrElse Entry.Description IsNot Nothing Then
            Desc = Entry.Name & If(Entry.Version Is Nothing, "", " (" & Entry.Version & ")") & " : " & If(Entry.Description, Entry.Path)
        ElseIf Entry.IsFileAvailable Then
            Desc = Entry.Path
        Else
            Desc = "存在错误 : " & Entry.Path
        End If
        'Desc = If(Entry.ModId, "无可用名称") & " (" & If(Entry.Version, "无可用版本") & ")"
        'If Entry.Dependencies.Count > 0 Then
        '    Dim DepList As New List(Of String)
        '    For Each Dep In Entry.Dependencies
        '        DepList.Add(Dep.Key & If(Dep.Value Is Nothing, "", "@" & Dep.Value))
        '    Next
        '    Desc += " : " & Join(DepList, ", ")
        'End If
        '实例化
        Dim NewItem As New MyListItem With {.Logo = Logo, .SnapsToDevicePixels = True, .Title = Title, .Info = Desc, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry}
        NewItem.PaddingRight = If(Entry.IsFileAvailable, 73, 55)
        '事件
        NewItem.ContentHandler = AddressOf McModContent
        Return NewItem
    End Function
    Private Sub McModContent(sender As MyListItem, e As EventArgs)
        Dim ModEntity As McMod = sender.Tag
        '注册点击事件
        AddHandler sender.Click, AddressOf Item_Click
        '图标按钮
        Dim BtnDel As New MyIconButton With {.LogoScale = 1.1, .Logo = Logo.IconButtonDelete, .Tag = sender}
        BtnDel.ToolTip = "删除"
        ToolTipService.SetPlacement(BtnDel, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnDel, 30)
        ToolTipService.SetHorizontalOffset(BtnDel, 2)
        AddHandler BtnDel.Click, AddressOf Delete_Click
        Dim BtnOpen As New MyIconButton With {.LogoScale = 1.15, .Logo = Logo.IconButtonOpen, .Tag = sender}
        BtnOpen.ToolTip = "打开文件位置"
        ToolTipService.SetPlacement(BtnOpen, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnOpen, 30)
        ToolTipService.SetHorizontalOffset(BtnOpen, 2)
        AddHandler BtnOpen.Click, AddressOf Open_Click
        If ModEntity.IsFileAvailable Then
            Dim BtnCont As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .Tag = sender}
            BtnCont.ToolTip = "详情"
            ToolTipService.SetPlacement(BtnCont, Primitives.PlacementMode.Center)
            ToolTipService.SetVerticalOffset(BtnCont, 30)
            ToolTipService.SetHorizontalOffset(BtnCont, 2)
            AddHandler BtnCont.Click, AddressOf Info_Click
            AddHandler sender.MouseRightButtonDown, AddressOf Info_Click
            sender.Buttons = {BtnCont, BtnOpen, BtnDel}
        Else
            sender.Buttons = {BtnOpen, BtnDel}
        End If
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
    ''' 一键启用 / 禁用。
    ''' </summary>
    Private Sub BtnManageChange_Click(sender As MyButton, e As EventArgs) Handles BtnManageEnabled.Click, BtnManageDisabled.Click
        Dim IsSuccessful As Boolean = True
        Try
            Dim IsDisable As Boolean = sender.Text.Contains("禁用")
            For Each ModEntity In McModLoader.Output
                Dim NewPath As String = Nothing
                If ModEntity.State = McMod.McModState.Fine And IsDisable Then
                    '禁用
                    NewPath = ModEntity.Path & ".disabled"
                ElseIf ModEntity.State = McMod.McModState.Disabled AndAlso Not IsDisable Then
                    '启用
                    NewPath = ModEntity.Path.Substring(0, ModEntity.Path.Count - ".disabled".Count)
                Else
                    Continue For
                End If
                '重命名
                Try
                    If File.Exists(ModEntity.Path) Then
                        File.Delete(NewPath)
                        FileSystem.Rename(ModEntity.Path, NewPath)
                    Else
                        Throw New FileNotFoundException("未找到文件：" & ModEntity.Path)
                    End If
                Catch ex As Exception
                    Log(ex, "全局状态改变中重命名 Mod 失败")
                    IsSuccessful = False
                End Try
            Next
        Catch ex As Exception
            Log(ex, "改变全部 Mod 状态失败", LogLevel.Msgbox)
            IsSuccessful = False
        Finally
            If Not IsSuccessful Then Hint("由于文件被占用，部分 Mod 的状态切换失败，请尝试关闭正在运行的游戏后再试！", HintType.Critical)
            RefreshList()
        End Try
    End Sub

#End Region

#Region "单个 Mod 项"

    '点击：切换可用状态 / 显示错误原因
    Public Sub Item_Click(sender As MyListItem, e As EventArgs)
        Try

            Dim ModEntity As McMod = sender.Tag
            Dim NewPath As String = Nothing
            Select Case ModEntity.State
                Case McMod.McModState.Fine
                    '前置检测警告
                    If ModEntity.IsPresetMod Then
                        If MyMsgBox("该 Mod 可能为其他 Mod 的前置，如果禁用可能导致其他 Mod 无法使用。" & vbCrLf & "你确定要继续禁用吗？", "警告", "禁用", "取消") = 2 Then Exit Sub
                    End If
                    NewPath = ModEntity.Path & ".disabled"
                Case McMod.McModState.Disabled
                    NewPath = ModEntity.Path.Substring(0, ModEntity.Path.Count - ".disabled".Count)
                Case McMod.McModState.Unavaliable
                    MyMsgBox("无法读取此 Mod 的信息。" & vbCrLf & vbCrLf & "详细的错误信息：" & GetString(ModEntity.FileUnavailableReason, False), "Mod 读取失败")
                    Exit Sub
            End Select
            '重命名
            Dim NewModEntity As New McMod(NewPath)
            Try
                File.Delete(NewPath)
                FileSystem.Rename(ModEntity.Path, NewPath)
            Catch ex As FileNotFoundException
                Log(ex, "未找到理应存在的 Mod 文件（" & ModEntity.Path & "）")
                FileSystem.Rename(NewPath, ModEntity.Path)
                NewModEntity = New McMod(ModEntity.Path)
            End Try
            '更改 Loader 中的列表
            Dim IndexOfLoader As Integer = McModLoader.Output.IndexOf(ModEntity)
            McModLoader.Output.RemoveAt(IndexOfLoader)
            McModLoader.Output.Insert(IndexOfLoader, NewModEntity)
            '更改 UI 中的列表
            Dim Parent As StackPanel = sender.Parent
            Dim IndexOfUi As Integer = Parent.Children.IndexOf(sender)
            Parent.Children.RemoveAt(IndexOfUi)
            Parent.Children.Insert(IndexOfUi, McModListItem(NewModEntity))
            '仅在非搜索页面才执行，以确保搜索页内外显示一致
            If String.IsNullOrWhiteSpace(SearchBox.Text) Then
                '改变禁用数量的显示
                CType(Parent.Parent, MyCard).Title = McModGetTitle(McModLoader.Output)
                '更新加载器状态
                LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", LoaderFolderRunType.UpdateOnly)
            End If

        Catch ex As Exception
            Log(ex, "单个状态改变中重命名 Mod 失败")
            Hint("切换 Mod 状态失败，请尝试关闭正在运行的游戏后再试！")
            LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", LoaderFolderRunType.UpdateOnly)
        End Try
    End Sub
    '删除
    Public Sub Delete_Click(sender As MyIconButton, e As EventArgs)
        Try

            Dim ListItem As MyListItem = sender.Tag
            Dim ModEntity As McMod = ListItem.Tag
            '前置检测警告
            If ModEntity.IsPresetMod Then
                If MyMsgBox("该 Mod 可能为其他 Mod 的前置，如果删除可能导致其他 Mod 无法使用。" & vbCrLf & "你确定要继续删除吗？", "警告", "删除", "取消", IsWarn:=True) = 2 Then Exit Sub
            End If
            '删除
            If File.Exists(ModEntity.Path) Then
                My.Computer.FileSystem.DeleteFile(ModEntity.Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Else
                '在不明原因下删除的文件可能还在列表里，如果玩家又点了一次就会出错，总之加个判断也不碍事
                Log("[System] 需要删除的 Mod 文件不存在（" & ModEntity.Path & "）", LogLevel.Hint)
                Exit Sub
            End If
            '更改 Loader 中的列表
            McModLoader.Output.Remove(ModEntity)
            '更改 UI 中的列表
            Dim Parent As StackPanel = ListItem.Parent
            Parent.Children.Remove(ListItem)
            If Parent.Children.Count = 0 Then
                RefreshList(True)
            Else
                CType(Parent.Parent, MyCard).Title = McModGetTitle(McModLoader.Output)
            End If
            '显示提示
            Hint("已将 " & ModEntity.FileName & " 删除到回收站！", HintType.Finish)
            LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", LoaderFolderRunType.UpdateOnly)

        Catch ex As Exception
            Log(ex, "删除 Mod 失败", LogLevel.Feedback)
        End Try
    End Sub
    '详情
    Public Sub Info_Click(sender As Object, e As EventArgs)
        Try

            Dim ListItem As MyListItem
            ListItem = If(TypeOf sender Is MyIconButton, sender.Tag, sender)
            Dim ModEntity As McMod = ListItem.Tag
            '获取信息
            Dim ContentLines As New List(Of String)
            If ModEntity.Description IsNot Nothing Then ContentLines.Add(ModEntity.Description & vbCrLf)
            If ModEntity.Authors IsNot Nothing Then ContentLines.Add("作者：" & ModEntity.Authors)
            ContentLines.Add("文件：" & ModEntity.FileName & "（" & GetString(New FileInfo(ModEntity.Path).Length) & "）")
            If ModEntity.Version IsNot Nothing Then ContentLines.Add("版本：" & ModEntity.Version)
            If ModeDebug Then
                Dim DebugInfo As New List(Of String)
                If ModEntity.ModId IsNot Nothing Then
                    DebugInfo.Add("Mod ID：" & ModEntity.ModId)
                End If
                If ModEntity.Dependencies.Count > 0 Then
                    DebugInfo.Add("依赖于：")
                    For Each Dep In ModEntity.Dependencies
                        DebugInfo.Add(" - " & Dep.Key & If(Dep.Value Is Nothing, "", "，版本：" & Dep.Value))
                    Next
                End If
                If DebugInfo.Count > 0 Then
                    ContentLines.Add(vbCrLf & "—————— 调试信息 ——————")
                    ContentLines.AddRange(DebugInfo)
                End If
            End If
            '获取用于搜索的 Mod 名称
            Dim ModOriginalName As String = ModEntity.Name.Replace(" ", "+")
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
            If ModEntity.Url Is Nothing Then
                If MyMsgBox(Join(ContentLines, vbCrLf), ModEntity.Name, "确定", "百科搜索") = 2 Then
                    OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                End If
            Else
                Select Case MyMsgBox(Join(ContentLines, vbCrLf), ModEntity.Name, "确定", "百科搜索", "打开官网")
                    Case 2
                        OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                    Case 3
                        OpenWebsite(ModEntity.Url)
                End Select
            End If

        Catch ex As Exception
            Log(ex, "获取 Mod 详情失败", LogLevel.Feedback)
        End Try
    End Sub
    '打开文件所在的位置
    Public Sub Open_Click(sender As MyIconButton, e As EventArgs)
        Try

            Dim ListItem As MyListItem = sender.Tag
            Dim ModEntity As McMod = ListItem.Tag
            OpenExplorer("/select,""" & ModEntity.Path & """")

        Catch ex As Exception
            Log(ex, "打开 Mod 文件位置失败", LogLevel.Feedback)
        End Try
    End Sub

#End Region

    Public Sub SearchRun() Handles SearchBox.TextChanged
        If String.IsNullOrWhiteSpace(SearchBox.Text) Then
            '隐藏
            LoaderFolderRun(McModLoader, PageVersionLeft.Version.PathIndie & "mods\", LoaderFolderRunType.RunOnUpdated)
            AniStart({
                     AaOpacity(PanSearch, -PanSearch.Opacity, 100),
                     AaCode(Sub()
                                PanSearch.Height = 0
                                PanSearch.Visibility = Visibility.Collapsed
                                PanList.Visibility = Visibility.Visible
                                PanManage.Visibility = Visibility.Visible
                            End Sub,, True),
                     AaOpacity(PanList, 1 - PanList.Opacity, 150, 30),
                     AaOpacity(PanManage, 1 - PanManage.Opacity, 150, 30)
                }, "FrmVersionMod Search Switch", True)
        Else
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of McMod))
            For Each Entry As McMod In McModLoader.Output
                QueryList.Add(New SearchEntry(Of McMod) With {
                    .Item = Entry,
                    .SearchSource = New List(Of KeyValuePair(Of String, Double)) From {
                        New KeyValuePair(Of String, Double)(Entry.Name, 1),
                        New KeyValuePair(Of String, Double)(Entry.FileName, 1),
                        New KeyValuePair(Of String, Double)(If(Entry.Description, ""), 0.5)
                    }
                })
            Next
            '进行搜索，构造列表
            Dim SearchResult = Search(QueryList, SearchBox.Text, MaxBlurCount:=5, MinBlurSimilarity:=0.35)
            PanSearchList.Children.Clear()
            If SearchResult.Count = 0 Then
                PanSearch.Title = "无搜索结果"
                PanSearchList.Visibility = Visibility.Collapsed
            Else
                PanSearch.Title = "搜索结果"
                For Each Result In SearchResult
                    Dim Item = McModListItem(Result.Item)
                    If ModeDebug Then Item.Info = If(Result.AbsoluteRight, "完全匹配，", "") & "相似度：" & Math.Round(Result.Similarity, 3) & "，" & Item.Info
                    PanSearchList.Children.Add(Item)
                Next
                PanSearchList.Visibility = Visibility.Visible
            End If
            '显示
            AniStart({
                     AaOpacity(PanList, -PanList.Opacity, 100),
                     AaOpacity(PanManage, -PanManage.Opacity, 100),
                     AaCode(Sub()
                                PanList.Visibility = Visibility.Collapsed
                                PanManage.Visibility = Visibility.Collapsed
                                PanSearch.Visibility = Visibility.Visible
                                PanSearch.TriggerForceResize()
                            End Sub,, True),
                     AaOpacity(PanSearch, 1 - PanSearch.Opacity, 200, 60)
                }, "FrmVersionMod Search Switch", True)
        End If
    End Sub

End Class
