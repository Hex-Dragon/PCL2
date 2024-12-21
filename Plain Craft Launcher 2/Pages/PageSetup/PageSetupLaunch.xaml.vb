Public Class PageSetupLaunch

    Private IsLoad As Boolean = False

    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        RefreshRam(False)

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1

        '内存自动刷新
        Dim timer As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 1)}
        AddHandler timer.Tick, AddressOf RefreshRam
        timer.Start()

    End Sub
    Public Sub Reload()
        Try

            '离线皮肤
            CType(FindName("RadioSkinType" & Setup.Load("LaunchSkinType")), MyRadioBox).Checked = True
            TextSkinID.Text = Setup.Get("LaunchSkinID")

            '启动参数
            TextArgumentTitle.Text = Setup.Get("LaunchArgumentTitle")
            TextArgumentInfo.Text = Setup.Get("LaunchArgumentInfo")
            ComboArgumentIndie.SelectedIndex = Setup.Get("LaunchArgumentIndie")
            ComboArgumentVisibie.SelectedIndex = Setup.Get("LaunchArgumentVisible")
            ComboArgumentPriority.SelectedIndex = Setup.Get("LaunchArgumentPriority")
            ComboArgumentWindowType.SelectedIndex = Setup.Get("LaunchArgumentWindowType")
            TextArgumentWindowWidth.Text = Setup.Get("LaunchArgumentWindowWidth")
            TextArgumentWindowHeight.Text = Setup.Get("LaunchArgumentWindowHeight")
            CheckArgumentRam.Checked = Setup.Get("LaunchArgumentRam")
            RefreshJavaComboBox()

            '游戏内存
            CType(FindName("RadioRamType" & Setup.Load("LaunchRamType")), MyRadioBox).Checked = True
            SliderRamCustom.Value = Setup.Get("LaunchRamCustom")

            '高级设置
            TextAdvanceJvm.Text = Setup.Get("LaunchAdvanceJvm")
            TextAdvanceGame.Text = Setup.Get("LaunchAdvanceGame")
            TextAdvanceRun.Text = Setup.Get("LaunchAdvanceRun")
            CheckAdvanceRunWait.Checked = Setup.Get("LaunchAdvanceRunWait")
            CheckAdvanceAssets.Checked = Setup.Get("LaunchAdvanceAssets")
            CheckAdvanceJava.Checked = Setup.Get("LaunchAdvanceJava")

        Catch ex As NullReferenceException
            Log(ex, "启动设置项存在异常，已被自动重置", LogLevel.Msgbox)
            Reset()
        Catch ex As Exception
            Log(ex, "重载启动设置时出错", LogLevel.Feedback)
        End Try
    End Sub

    '初始化
    Public Sub Reset()
        Try
            Setup.Reset("LaunchArgumentTitle")
            Setup.Reset("LaunchArgumentInfo")
            Setup.Reset("LaunchArgumentIndie")
            Setup.Reset("LaunchArgumentVisible")
            Setup.Reset("LaunchArgumentWindowType")
            Setup.Reset("LaunchArgumentWindowWidth")
            Setup.Reset("LaunchArgumentWindowHeight")
            Setup.Reset("LaunchArgumentPriority")
            Setup.Reset("LaunchArgumentRam")
            Setup.Reset("LaunchRamType")
            Setup.Reset("LaunchRamCustom")
            Setup.Reset("LaunchSkinType")
            Setup.Reset("LaunchSkinID")
            Setup.Reset("LaunchAdvanceJvm")
            Setup.Reset("LaunchAdvanceGame")
            Setup.Reset("LaunchAdvanceJava")
            Setup.Reset("LaunchAdvanceAssets")
            Setup.Reset("LaunchAdvanceRun")
            Setup.Reset("LaunchAdvanceRunWait")

            Setup.Reset("LaunchArgumentJavaAll")
            Setup.Reset("LaunchArgumentJavaSelect")
            JavaSearchLoader.Start(IsForceRestart:=True)

            Log("[Setup] 已初始化启动设置")
            Hint("已初始化启动设置！", HintType.Finish, False)
        Catch ex As Exception
            Log(ex, "初始化启动设置失败", LogLevel.Msgbox)
        End Try

        Reload()
    End Sub

    '将控件改变路由到设置改变
    Private Shared Sub RadioBoxChange(sender As MyRadioBox, e As Object) Handles RadioSkinType0.Check, RadioSkinType1.Check, RadioSkinType2.Check, RadioSkinType3.Check, RadioSkinType4.Check, RadioRamType0.Check, RadioRamType1.Check
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag.ToString.Split("/")(0), Val(sender.Tag.ToString.Split("/")(1)))
    End Sub
    Private Shared Sub TextBoxChange(sender As MyTextBox, e As Object) Handles TextSkinID.ValidatedTextChanged, TextArgumentWindowHeight.ValidatedTextChanged, TextArgumentWindowWidth.ValidatedTextChanged, TextArgumentInfo.ValidatedTextChanged, TextAdvanceGame.ValidatedTextChanged, TextAdvanceJvm.ValidatedTextChanged, TextArgumentTitle.ValidatedTextChanged, TextAdvanceRun.ValidatedTextChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Text)
    End Sub
    Private Shared Sub SliderChange(sender As MySlider, e As Object) Handles SliderRamCustom.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Value)
    End Sub
    Private Shared Sub ComboChange(sender As MyComboBox, e As Object) Handles ComboArgumentIndie.SelectionChanged, ComboArgumentVisibie.SelectionChanged, ComboArgumentWindowType.SelectionChanged, ComboArgumentPriority.SelectionChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.SelectedIndex)
    End Sub
    Private Shared Sub CheckBoxChange(sender As MyCheckBox, e As Object) Handles CheckAdvanceAssets.Change, CheckAdvanceJava.Change, CheckAdvanceRunWait.Change, CheckArgumentRam.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Checked)
    End Sub

#Region "离线皮肤"

    Private Sub BtnSkinChange_Click(sender As Object, e As EventArgs) Handles BtnSkinChange.Click
        Dim SkinInfo As McSkinInfo = McSkinSelect()
        If Not SkinInfo.IsVaild Then Exit Sub
        ChangeSkin(SkinInfo)
    End Sub
    Private Sub RadioSkinType3_Check(sender As Object, e As RouteEventArgs) Handles RadioSkinType4.PreviewCheck
        If Not (AniControlEnabled = 0 AndAlso e.RaiseByMouse) Then Exit Sub
        '已有图片则不再选择
        If File.Exists(PathAppdata & "CustomSkin.png") Then Exit Sub
        '没有图片则要求选择
        Dim SkinInfo As McSkinInfo = McSkinSelect()
        If Not SkinInfo.IsVaild Then
            e.Handled = True
            Exit Sub
        End If
        '正式改变
        If Not ChangeSkin(SkinInfo) Then e.Handled = True
    End Sub
    '返回是否成功改变
    Private Function ChangeSkin(SkinInfo As McSkinInfo) As Boolean
        Try
            '拷贝文件
            File.Delete(PathAppdata & "CustomSkin.png")
            CopyFile(SkinInfo.LocalFile, PathAppdata & "CustomSkin.png")
            '将单层皮肤扩展到双层
            Dim Bitmap As New MyBitmap(PathAppdata & "CustomSkin.png")
            If Bitmap.Pic.Width = 64 AndAlso Bitmap.Pic.Height = 32 Then
                Dim Img As System.Drawing.Image = Bitmap
                Dim NewBitmap As New System.Drawing.Bitmap(64, 64)
                Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(NewBitmap)
                    g.DrawImageUnscaled(Img, New System.Drawing.Point(0, 0))
                End Using
                File.Delete(PathAppdata & "CustomSkin.png")
                NewBitmap.Save(PathAppdata & "CustomSkin.png")
            End If
            '更新设置
            Setup.Set("LaunchSkinSlim", SkinInfo.IsSlim)
            ChangeSkin = True
        Catch ex As Exception
            Log(ex, "改变离线皮肤失败", LogLevel.Msgbox)
            ChangeSkin = False
        Finally
            '设置当前显示
            PageLaunchLeft.SkinLegacy.Start(IsForceRestart:=True)
        End Try
    End Function
    Private Sub BtnSkinDelete_Click(sender As Object, e As EventArgs) Handles BtnSkinDelete.Click
        Try
            File.Delete(PathAppdata & "CustomSkin.png")
            RadioSkinType0.SetChecked(True, True, True)
            Hint("离线皮肤已清空！", HintType.Finish)
        Catch ex As Exception
            Log(ex, "清空离线皮肤失败", LogLevel.Msgbox)
        End Try
    End Sub
    Private Sub BtnSkinSave_Click(sender As Object, e As EventArgs) Handles BtnSkinSave.Click
        MySkin.Save(PageLaunchLeft.SkinLegacy)
    End Sub
    Private Sub BtnSkinCache_Click(sender As Object, e As EventArgs) Handles BtnSkinCache.Click
        MySkin.RefreshCache(Nothing)
    End Sub

#End Region

#Region "游戏内存"

    Public Sub RamType(Type As Integer)
        If SliderRamCustom Is Nothing Then Exit Sub
        SliderRamCustom.IsEnabled = (Type = 1)
    End Sub

    ''' <summary>
    ''' 刷新 UI 上的 RAM 显示。
    ''' </summary>
    Public Sub RefreshRam(ShowAnim As Boolean)
        If LabRamGame Is Nothing OrElse LabRamUsed Is Nothing OrElse FrmMain.PageCurrent <> FormMain.PageType.Setup OrElse FrmSetupLeft.PageID <> FormMain.PageSubType.SetupLaunch Then Exit Sub
        '获取内存情况
        Dim RamGame As Double = GetRam(McVersionCurrent, False)
        Dim RamTotal As Double = Math.Round(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024 / 1024 * 10) / 10
        Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024 * 10) / 10
        Dim RamGameActual As Double = Math.Min(RamGame, RamAvailable)
        Dim RamUsed As Double = RamTotal - RamAvailable
        Dim RamEmpty As Double = Math.Round(MathClamp(RamTotal - RamUsed - RamGame, 0, 1000) * 10) / 10
        '设置最大可用内存
        If RamTotal <= 1.5 Then
            SliderRamCustom.MaxValue = Math.Max(Math.Floor((RamTotal - 0.3) / 0.1), 1)
        ElseIf RamTotal <= 8 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 1.5) / 0.5) + 12
        ElseIf RamTotal <= 16 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 8) / 1) + 25
        Else
            SliderRamCustom.MaxValue = Math.Min(Math.Floor((RamTotal - 16) / 2) + 33, 41)
        End If
        '设置文本
        LabRamGame.Text = If(RamGame = Math.Floor(RamGame), RamGame & ".0", RamGame) & " GB" &
                          If(RamGame <> RamGameActual, " (可用 " & If(RamGameActual = Math.Floor(RamGameActual), RamGameActual & ".0", RamGameActual) & " GB)", "")
        LabRamUsed.Text = If(RamUsed = Math.Floor(RamUsed), RamUsed & ".0", RamUsed) & " GB"
        LabRamTotal.Text = " / " & If(RamTotal = Math.Floor(RamTotal), RamTotal & ".0", RamTotal) & " GB"
        LabRamWarn.Visibility = If(RamGame = 1 AndAlso Not JavaIs64Bit() AndAlso Not Is32BitSystem AndAlso JavaList.Any, Visibility.Visible, Visibility.Collapsed)
        If ShowAnim Then
            '宽度动画
            AniStart({
                AaGridLengthWidth(ColumnRamUsed, RamUsed - ColumnRamUsed.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaGridLengthWidth(ColumnRamGame, RamGameActual - ColumnRamGame.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaGridLengthWidth(ColumnRamEmpty, RamEmpty - ColumnRamEmpty.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong))
            }, "SetupLaunch Ram Grid")
        Else
            '宽度设置
            ColumnRamUsed.Width = New GridLength(RamUsed, GridUnitType.Star)
            ColumnRamGame.Width = New GridLength(RamGameActual, GridUnitType.Star)
            ColumnRamEmpty.Width = New GridLength(RamEmpty, GridUnitType.Star)
        End If
    End Sub
    Private Sub RefreshRam() Handles SliderRamCustom.Change, RadioRamType0.Check, RadioRamType1.Check
        RefreshRam(True)
    End Sub

    Private RamTextLeft As Integer = 2, RamTextRight As Integer = 1
    ''' <summary>
    ''' 刷新 UI 上的文本位置。
    ''' </summary>
    Private Sub RefreshRamText() Handles RectRamGame.SizeChanged, RectRamEmpty.SizeChanged, LabRamGame.SizeChanged
        '获取宽度信息
        Dim RectUsedWidth = RectRamUsed.ActualWidth
        Dim TotalWidth = PanRamDisplay.ActualWidth
        Dim LabGameWidth = LabRamGame.ActualWidth, LabUsedWidth = LabRamUsed.ActualWidth, LabTotalWidth = LabRamTotal.ActualWidth
        Dim LabGameTitleWidth = LabRamGameTitle.ActualWidth, LabUsedTitleWidth = LabRamUsedTitle.ActualWidth
        '左侧
        Dim Left As Integer
        If RectUsedWidth - 30 < LabUsedWidth OrElse RectUsedWidth - 30 < LabUsedTitleWidth Then
            '全写不下了
            Left = 0
        ElseIf RectUsedWidth - 25 < (LabUsedWidth + LabTotalWidth) Then
            '显示不下完整数据
            Left = 1
        Else
            '正常
            Left = 2
        End If
        If RamTextLeft <> Left Then
            RamTextLeft = Left
            Select Case Left
                Case 0
                    AniStart({
                            AaOpacity(LabRamUsed, -LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, -LabRamUsedTitle.Opacity, 100)
                        }, "SetupLaunch Ram TextLeft")
                Case 1
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "SetupLaunch Ram TextLeft")
                Case 2
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, 1 - LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "SetupLaunch Ram TextLeft")
            End Select
        End If
        '右侧
        Dim Right As Integer
        If TotalWidth < LabGameWidth + 2 + RectUsedWidth OrElse TotalWidth < LabGameTitleWidth + 2 + RectUsedWidth Then
            '挤到最右边
            Right = 0
        Else
            '正常情况
            Right = 1
        End If
        If Right = 0 Then
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("SetupLaunch Ram TextRight")) Then
                '需要动画
                AniStart({
                        AaX(LabRamGame, TotalWidth - LabGameWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, TotalWidth - LabGameTitleWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "SetupLaunch Ram TextRight")
            Else
                '不需要动画
                AniStop("SetupLaunch Ram TextRight")
                LabRamGame.Margin = New Thickness(TotalWidth - LabGameWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(TotalWidth - LabGameTitleWidth, 0, 0, 5)
            End If
        Else
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("SetupLaunch Ram TextRight")) Then
                '需要动画
                AniStart({
                        AaX(LabRamGame, 2 + RectUsedWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, 2 + RectUsedWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "SetupLaunch Ram TextRight")
            Else
                '不需要动画
                AniStop("SetupLaunch Ram TextRight")
                LabRamGame.Margin = New Thickness(2 + RectUsedWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(2 + RectUsedWidth, 0, 0, 5)
            End If
        End If
        RamTextRight = Right
    End Sub

    ''' <summary>
    ''' 获取当前设置的 RAM 值。单位为 GB。
    ''' </summary>
    Public Shared Function GetRam(Version As McVersion, UseVersionJavaSetup As Boolean, Optional Is32BitJava As Boolean? = Nothing) As Double

        '------------------------------------------
        ' 修改下方代码时需要一并修改 PageVersionSetup
        '------------------------------------------

        Dim RamGive As Double
        If Setup.Get("LaunchRamType") = 0 Then
            '自动配置
            Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024 * 10) / 10
            '确定需求的内存值
            Dim RamMininum As Double '无论如何也需要保证的最低限度内存
            Dim RamTarget1 As Double '估计能勉强带动了的内存
            Dim RamTarget2 As Double '估计没啥问题了的内存
            Dim RamTarget3 As Double '放一百万个材质和 Mod 和光影需要的内存
            If Version IsNot Nothing AndAlso Not Version.IsLoaded Then Version.Load()
            If Version IsNot Nothing AndAlso Version.Modable Then
                '可安装 Mod 的版本
                Dim ModDir As New DirectoryInfo(Version.PathIndie & "mods\")
                Dim ModCount As Integer = If(ModDir.Exists, ModDir.GetFiles.Length, 0)
                RamMininum = 0.5 + ModCount / 150
                RamTarget1 = 1.5 + ModCount / 90
                RamTarget2 = 2.7 + ModCount / 50
                RamTarget3 = 4.5 + ModCount / 25
            ElseIf Version IsNot Nothing AndAlso Version.Version.HasOptiFine Then
                'OptiFine 版本
                RamMininum = 0.5
                RamTarget1 = 1.5
                RamTarget2 = 3
                RamTarget3 = 5
            Else
                '普通版本
                RamMininum = 0.5
                RamTarget1 = 1.5
                RamTarget2 = 2.5
                RamTarget3 = 4
            End If
            Dim RamDelta As Double
            '预分配内存，阶段一，0 ~ T1，100%
            RamDelta = RamTarget1
            RamGive += Math.Min(RamAvailable, RamDelta)
            RamAvailable -= RamDelta
            If RamAvailable < 0.1 Then GoTo PreFin
            '预分配内存，阶段二，T1 ~ T2，70%
            RamDelta = RamTarget2 - RamTarget1
            RamGive += Math.Min(RamAvailable * 0.7, RamDelta)
            RamAvailable -= RamDelta / 0.7
            If RamAvailable < 0.1 Then GoTo PreFin
            '预分配内存，阶段三，T2 ~ T3，40%
            RamDelta = RamTarget3 - RamTarget2
            RamGive += Math.Min(RamAvailable * 0.4, RamDelta)
            RamAvailable -= RamDelta / 0.4
            If RamAvailable < 0.1 Then GoTo PreFin
            '预分配内存，阶段四，T3 ~ T3 * 2，15%
            RamDelta = RamTarget3
            RamGive += Math.Min(RamAvailable * 0.15, RamDelta)
            RamAvailable -= RamDelta / 0.15
            If RamAvailable < 0.1 Then GoTo PreFin
PreFin:
            '不低于最低值
            RamGive = Math.Round(Math.Max(RamGive, RamMininum), 1)
        Else
            '手动配置
            Dim Value As Integer = Setup.Get("LaunchRamCustom")
            If Value <= 12 Then
                RamGive = Value * 0.1 + 0.3
            ElseIf Value <= 25 Then
                RamGive = (Value - 12) * 0.5 + 1.5
            ElseIf Value <= 33 Then
                RamGive = (Value - 25) * 1 + 8
            Else
                RamGive = (Value - 33) * 2 + 16
            End If
        End If
        '若使用 32 位 Java，则限制为 1G
        If If(Is32BitJava, Not JavaIs64Bit(If(UseVersionJavaSetup, Version, Nothing))) Then RamGive = Math.Min(1, RamGive)
        '封顶 32G
        Return Math.Min(32, RamGive)
    End Function

#End Region

#Region "Java 选择"

    '刷新 Java 下拉框显示
    Public Sub RefreshJavaComboBox()
        If ComboArgumentJava Is Nothing Then Exit Sub
        '初始化列表
        ComboArgumentJava.Items.Clear()
        ComboArgumentJava.Items.Add(New MyComboBoxItem With {.Content = "自动选择合适的 Java", .Tag = "自动选择"})
        '更新列表
        Dim SelectedItem As MyComboBoxItem = Nothing
        Dim SelectedBySetup As String = Setup.Get("LaunchArgumentJavaSelect")
        Try
            For Each Java In Sort(JavaList.Clone(), Function(l, r) l.VersionCode < r.VersionCode)
                Dim ListItem = New MyComboBoxItem With {.Content = Java.ToString, .ToolTip = Java.PathFolder, .Tag = Java}
                ToolTipService.SetHorizontalOffset(ListItem, 400)
                ComboArgumentJava.Items.Add(ListItem)
                '判断人为选中
                If SelectedBySetup = "" Then Continue For
                If JavaEntry.FromJson(GetJson(SelectedBySetup)).PathFolder = Java.PathFolder Then SelectedItem = ListItem
            Next
        Catch ex As Exception
            Setup.Set("LaunchArgumentJavaSelect", "")
            Log(ex, "更新设置 Java 下拉框失败", LogLevel.Feedback)
        End Try
        '更新选择项
        If SelectedItem Is Nothing AndAlso JavaList.Any Then SelectedItem = ComboArgumentJava.Items(0) '选中 “自动选择”
        ComboArgumentJava.SelectedItem = SelectedItem
        '结束处理
        If SelectedItem Is Nothing Then
            ComboArgumentJava.Items.Clear()
            ComboArgumentJava.Items.Add(New ComboBoxItem With {.Content = "未找到可用的 Java", .IsSelected = True})
        End If
        RefreshRam(True)
    End Sub
    '阻止在特定情况下展开下拉框
    Private Sub ComboArgumentJava_DropDownOpened(sender As Object, e As EventArgs) Handles ComboArgumentJava.DropDownOpened
        If ComboArgumentJava.SelectedItem Is Nothing OrElse ComboArgumentJava.Items(0).Content = "未找到可用的 Java" OrElse ComboArgumentJava.Items(0).Content = "加载中……" Then
            ComboArgumentJava.IsDropDownOpen = False
        End If
    End Sub

    '下拉框选择更改
    Private Sub JavaSelectionUpdate() Handles ComboArgumentJava.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        'Java 不可用时也不清空，会导致刷新时找不到对象
        If ComboArgumentJava.SelectedItem Is Nothing OrElse ComboArgumentJava.SelectedItem.Tag Is Nothing Then Exit Sub
        '设置新的 Java
        Dim SelectedJava = ComboArgumentJava.SelectedItem.Tag
        If "自动选择".Equals(SelectedJava) Then
            '选择 “自动”
            Setup.Set("LaunchArgumentJavaSelect", "")
            Log("[Java] 修改 Java 选择设置：自动选择")
        Else
            '选择指定项
            Setup.Set("LaunchArgumentJavaSelect", CType(SelectedJava.ToJson(), JObject).ToString(Newtonsoft.Json.Formatting.None))
            Log("[Java] 修改 Java 选择设置：" & SelectedJava.ToString)
        End If
        RefreshRam(True)
    End Sub

    '手动选择
    Private Sub BtnArgumentJavaSelect_Click(sender As Object, e As EventArgs) Handles BtnArgumentJavaSelect.Click
        If JavaSearchLoader.State = LoadState.Loading Then
            Hint("正在搜索 Java，请稍候！", HintType.Critical)
            Exit Sub
        End If
        '选择 Java
        Dim JavaSelected As String = SelectFile("javaw.exe|javaw.exe", "选择 bin 文件夹中的 javaw.exe 文件")
        If JavaSelected = "" Then Exit Sub
        JavaSelected = GetPathFromFullPath(JavaSelected)
        Try
            '验证 Java 可用
            Dim NewEntry As New JavaEntry(JavaSelected, True)
            NewEntry.Check()
            '加入列表
            Dim JavaNewList As New JArray From {NewEntry.ToJson}
            For Each JsonEntry In GetJson(Setup.Get("LaunchArgumentJavaAll"))
                Dim Entry = JavaEntry.FromJson(JsonEntry)
                If Entry.PathFolder = NewEntry.PathFolder Then Continue For
                JavaNewList.Add(JsonEntry)
            Next
            Setup.Set("LaunchArgumentJavaAll", JavaNewList.ToString(Newtonsoft.Json.Formatting.None))
            '重新加载列表
            JavaSearchLoader.Start(IsForceRestart:=True)
            Hint("已将该 Java 加入 Java 列表！", HintType.Finish)
        Catch ex As Exception
            Log(ex, "该 Java 存在异常，无法使用", LogLevel.Msgbox, "异常的 Java")
            Exit Sub
        End Try
    End Sub
    '自动查找
    Private Sub BtnArgumentJavaSearch_Click(sender As Object, e As EventArgs) Handles BtnArgumentJavaSearch.Click
        If JavaSearchLoader.State = LoadState.Loading Then
            Hint("正在搜索 Java，请稍候！", HintType.Critical)
            Exit Sub
        End If
        RunInThread(
        Sub()
            Hint("正在搜索 Java！")
            JavaSearchLoader.WaitForExit(IsForceRestart:=True)
            If Not JavaList.Any() Then
                Hint("未找到可用的 Java！", HintType.Critical)
            Else
                Hint("已找到 " & JavaList.Count & " 个 Java，请检查下拉框查看列表！", HintType.Finish)
            End If
        End Sub)
    End Sub

#End Region

#Region "其他选项"

    Private Sub WindowTypeUIRefresh() Handles ComboArgumentWindowType.SelectionChanged
        If ComboArgumentWindowType Is Nothing Then Exit Sub
        If ComboArgumentWindowType.SelectedIndex = 3 AndAlso LabArgumentWindowMiddle IsNot Nothing AndAlso LabArgumentWindowMiddle.Visibility = Visibility.Collapsed Then
            LabArgumentWindowMiddle.Visibility = Visibility.Visible
            TextArgumentWindowHeight.Visibility = Visibility.Visible
            TextArgumentWindowWidth.Visibility = Visibility.Visible
        ElseIf ComboArgumentWindowType.SelectedIndex <> 3 AndAlso LabArgumentWindowMiddle IsNot Nothing AndAlso LabArgumentWindowMiddle.Visibility = Visibility.Visible Then
            LabArgumentWindowMiddle.Visibility = Visibility.Collapsed
            TextArgumentWindowHeight.Visibility = Visibility.Collapsed
            TextArgumentWindowWidth.Visibility = Visibility.Collapsed
        End If
    End Sub

    '可见性选择直接关闭的警告
    Private Sub ComboArgumentVisibie_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentVisibie.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If ComboArgumentVisibie.SelectedIndex = 0 Then
            If MyMsgBox("若在游戏启动后立即关闭启动器，崩溃检测、更改游戏标题等功能将失效。" & vbCrLf &
                        "如果想保留这些功能，可以选择让启动器在游戏启动后隐藏，游戏退出后自动关闭。", "提醒", "继续", "取消") = 2 Then
                ComboArgumentVisibie.SelectedItem = e.RemovedItems(0)
            End If
        End If
    End Sub

    '开启自动内存优化的警告
    Private Sub CheckArgumentRam_Change() Handles CheckArgumentRam.Change
        If AniControlEnabled <> 0 Then Exit Sub
        If Not CheckArgumentRam.Checked Then Return
        If MyMsgBox("内存优化会显著延长启动耗时，建议仅在内存不足时开启。" & vbCrLf &
                    "如果你在使用机械硬盘，这还可能导致一小段时间的严重卡顿。" &
                    If(IsAdmin(), "", $"{vbCrLf}{vbCrLf}每次启动游戏，PCL 都需要申请管理员权限以进行内存优化。{vbCrLf}若想自动授予权限，可以右键 PCL，打开 属性 → 兼容性 → 以管理员身份运行此程序。"),
                    "提醒", "确定", "取消") = 2 Then
            CheckArgumentRam.Checked = False
        End If
    End Sub

    '版本隔离警告
    Private IsReverting As Boolean = False
    Private Sub ComboArgumentIndie_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndie.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If IsReverting Then Exit Sub
        If MyMsgBox("调整版本隔离后，你可能得把游戏存档、Mod 等文件手动迁移到新的游戏文件夹中。" & vbCrLf &
                    "如果修改后发现存档消失，把这项设置改回来就能恢复。" & vbCrLf &
                    "如果你不会迁移存档，不建议修改这项设置！",
                    "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            IsReverting = True
            ComboArgumentIndie.SelectedItem = e.RemovedItems(0)
            IsReverting = False
        End If
    End Sub

#End Region

#Region "高级设置"

    Private Sub TextAdvanceRun_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceRun.TextChanged
        CheckAdvanceRunWait.Visibility = If(TextAdvanceRun.Text = "", Visibility.Collapsed, Visibility.Visible)
    End Sub

    'JVM 参数重设
    Private Sub TextAdvanceJvm_TextChanged() Handles TextAdvanceJvm.ValidatedTextChanged
        BtnAdvanceJvmReset.Visibility = If(TextAdvanceJvm.Text = Setup.GetDefault("LaunchAdvanceJvm"), Visibility.Hidden, Visibility.Visible)
    End Sub
    Private Sub BtnAdvanceJvmReset_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJvmReset.Click
        Setup.Reset("LaunchAdvanceJvm")
        Reload()
    End Sub

#End Region

End Class
