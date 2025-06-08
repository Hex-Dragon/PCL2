Public Class PageSetupLaunch

    Private IsLoad As Boolean = False

    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        RefreshRam(False)
        If McVersionCurrent Is Nothing Then
            BtnSwitch.Visibility = Visibility.Collapsed
        Else
            BtnSwitch.Visibility = Visibility.Visible
        End If

        '非重复加载部分
        If IsLoad Then Return
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
            '启动参数
            TextArgumentTitle.Text = Setup.Get("LaunchArgumentTitle")
            TextArgumentInfo.Text = Setup.Get("LaunchArgumentInfo")
            ComboArgumentIndieV2.SelectedIndex = Setup.Get("LaunchArgumentIndieV2")
            ComboArgumentVisibie.SelectedIndex = Setup.Get("LaunchArgumentVisible")
            ComboArgumentPriority.SelectedIndex = Setup.Get("LaunchArgumentPriority")
            ComboArgumentWindowType.SelectedIndex = Setup.Get("LaunchArgumentWindowType")
            TextArgumentWindowWidth.Text = Setup.Get("LaunchArgumentWindowWidth")
            TextArgumentWindowHeight.Text = Setup.Get("LaunchArgumentWindowHeight")
            CheckArgumentRam.Checked = Setup.Get("LaunchArgumentRam")
            ComboMsAuthType.SelectedIndex = Setup.Get("LoginMsAuthType")
            'CheckArgumentJavaTraversal.Checked = Setup.Get("LaunchArgumentJavaTraversal")

            '游戏内存
            CType(FindName("RadioRamType" & Setup.Load("LaunchRamType")), MyRadioBox).Checked = True
            SliderRamCustom.Value = Setup.Get("LaunchRamCustom")

            '高级设置
            TextAdvanceJvm.Text = Setup.Get("LaunchAdvanceJvm")
            TextAdvanceGame.Text = Setup.Get("LaunchAdvanceGame")
            TextAdvanceRun.Text = Setup.Get("LaunchAdvanceRun")
            CheckAdvanceRunWait.Checked = Setup.Get("LaunchAdvanceRunWait")
            CheckAdvanceDisableRW.Checked = Setup.Get("LaunchAdvanceDisableRW")
            CheckAdvanceGraphicCard.Checked = Setup.Get("LaunchAdvanceGraphicCard")
            If IsArm64System Then
                CheckAdvanceDisableJLW.Checked = True
                CheckAdvanceDisableJLW.IsEnabled = False
                CheckAdvanceDisableJLW.ToolTip = "在启动游戏时不使用 Java Wrapper 进行包装。&#xa;由于系统为 ARM64 架构，Java Wrapper 已被强制禁用。"
            Else
                CheckAdvanceDisableJLW.Checked = Setup.Get("LaunchAdvanceDisableJLW")
            End If

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
            Setup.Reset("LaunchArgumentIndieV2")
            Setup.Reset("LaunchArgumentVisible")
            Setup.Reset("LaunchArgumentWindowType")
            Setup.Reset("LaunchArgumentWindowWidth")
            Setup.Reset("LaunchArgumentWindowHeight")
            Setup.Reset("LaunchArgumentPriority")
            Setup.Reset("LaunchArgumentRam")
            Setup.Reset("LaunchArgumentJavaTraversal")
            Setup.Reset("LaunchRamType")
            Setup.Reset("LaunchRamCustom")
            Setup.Reset("LaunchAdvanceJvm")
            Setup.Reset("LaunchAdvanceGame")
            Setup.Reset("LaunchAdvanceRun")
            Setup.Reset("LaunchAdvanceRunWait")
            Setup.Reset("LaunchAdvanceDisableJLW")
            Setup.Reset("LaunchAdvanceGraphicCard")
            Setup.Reset("LoginMsAuthType")
            Setup.Reset("LaunchArgumentJavaUser")
            Setup.Reset("LaunchArgumentJavaSelect")

            Log("[Setup] 已初始化启动设置")
            Hint("已初始化启动设置！", HintType.Finish, False)
        Catch ex As Exception
            Log(ex, "初始化启动设置失败", LogLevel.Msgbox)
        End Try

        Reload()
    End Sub

    '将控件改变路由到设置改变
    Private Shared Sub RadioBoxChange(sender As MyRadioBox, e As Object) Handles RadioRamType0.Check, RadioRamType1.Check
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag.ToString.Split("/")(0), Val(sender.Tag.ToString.Split("/")(1)))
    End Sub
    Private Shared Sub TextBoxChange(sender As MyTextBox, e As Object) Handles TextArgumentWindowHeight.ValidatedTextChanged, TextArgumentWindowWidth.ValidatedTextChanged, TextArgumentInfo.ValidatedTextChanged, TextAdvanceGame.ValidatedTextChanged, TextAdvanceJvm.ValidatedTextChanged, TextArgumentTitle.ValidatedTextChanged, TextAdvanceRun.ValidatedTextChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Text)
    End Sub
    Private Shared Sub SliderChange(sender As MySlider, e As Object) Handles SliderRamCustom.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Value)
    End Sub
    Private Shared Sub ComboChange(sender As MyComboBox, e As Object) Handles ComboArgumentIndieV2.SelectionChanged, ComboArgumentVisibie.SelectionChanged, ComboArgumentWindowType.SelectionChanged, ComboArgumentPriority.SelectionChanged, ComboMsAuthType.SelectionChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.SelectedIndex)
    End Sub
    Private Shared Sub CheckBoxChange(sender As MyCheckBox, e As Object) Handles CheckAdvanceRunWait.Change, CheckArgumentRam.Change, CheckAdvanceDisableJLW.Change, CheckAdvanceGraphicCard.Change, CheckAdvanceDisableRW.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Checked)
    End Sub

#Region "下载正版皮肤"
    Private Sub BtnSkinSave_Click(sender As Object, e As EventArgs) Handles BtnSkinSave.Click
        Dim ID As String = TextSkinID.Text
        Hint("正在获取皮肤...")
        RunInNewThread(Sub()
                           Try
                               If ID.Count < 3 Then
                                   Hint("这不是一个有效的 ID...")
                               Else
                                   Dim Result As String = McLoginMojangUuid(ID, True)
                                   Result = McSkinGetAddress(Result, "Mojang")
                                   Result = McSkinDownload(Result)
                                   RunInUi(Sub()
                                               Dim Path As String = SelectSaveFile("保存皮肤", ID & ".png", "皮肤图片文件(*.png)|*.png")
                                               CopyFile(Result, Path)
                                               Hint($"玩家 {ID} 的皮肤已保存！", HintType.Finish)
                                           End Sub)
                               End If
                           Catch ex As Exception
                               If GetExceptionSummary(ex).Contains("429") Then
                                   Hint("获取皮肤太过频繁，请 5 分钟之后再试！", HintType.Critical)
                                   Log("获取正版皮肤失败（" & ID & "）：获取皮肤太过频繁，请 5 分钟后再试！")
                               Else
                                   Log(ex, "获取正版皮肤失败（" & ID & "）")
                               End If
                           End Try
                       End Sub)
    End Sub
    Private Sub BtnSkinCache_Click(sender As Object, e As EventArgs) Handles BtnSkinCache.Click
        MySkin.RefreshCache(Nothing)
    End Sub
#End Region

#Region "游戏内存"

    Public Sub RamType(Type As Integer)
        If SliderRamCustom Is Nothing Then Return
        SliderRamCustom.IsEnabled = (Type = 1)
    End Sub

    ''' <summary>
    ''' 刷新 UI 上的 RAM 显示。
    ''' </summary>
    Public Sub RefreshRam(ShowAnim As Boolean)
        If LabRamGame Is Nothing OrElse LabRamUsed Is Nothing OrElse FrmMain.PageCurrent <> FormMain.PageType.Setup OrElse FrmSetupLeft.PageID <> FormMain.PageSubType.SetupLaunch Then Return
        '获取内存情况
        Dim RamGame As Double = Math.Round(GetRam(McVersionCurrent, False), 5)
        Dim RamTotal As Double = Math.Round(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024 / 1024, 1)
        Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024, 1)
        Dim RamGameActual As Double = Math.Round(Math.Min(RamGame, RamAvailable), 5)
        Dim RamUsed As Double = Math.Round(RamTotal - RamAvailable, 5)
        Dim RamEmpty As Double = Math.Round(MathClamp(RamTotal - RamUsed - RamGame, 0, 1000), 1)
        '设置最大可用内存
        If RamTotal <= 1.5 Then
            SliderRamCustom.MaxValue = Math.Max(Math.Floor((RamTotal - 0.3) / 0.1), 1)
        ElseIf RamTotal <= 8 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 1.5) / 0.5) + 12
        ElseIf RamTotal <= 16 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 8) / 1) + 25
        Else
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 16) / 2) + 33
        End If
        '设置文本
        LabRamGame.Text = If(RamGame = Math.Floor(RamGame), RamGame & ".0", RamGame) & " GB" &
                          If(RamGame <> RamGameActual, " (可用 " & If(RamGameActual = Math.Floor(RamGameActual), RamGameActual & ".0", RamGameActual) & " GB)", "")
        LabRamUsed.Text = If(RamUsed = Math.Floor(RamUsed), RamUsed & ".0", RamUsed) & " GB"
        LabRamTotal.Text = " / " & If(RamTotal = Math.Floor(RamTotal), RamTotal & ".0", RamTotal) & " GB"
        LabRamWarn.Visibility = If(RamGame = 1 AndAlso Not IsGameSet64BitJava() AndAlso Not Is32BitSystem AndAlso Javas.JavaList.Any, Visibility.Visible, Visibility.Collapsed)
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
        If If(Is32BitJava, Not IsGameSet64BitJava(If(UseVersionJavaSetup, Version, Nothing))) Then RamGive = Math.Min(1, RamGive)
        Return RamGive
    End Function

#End Region

#Region "其他选项"

    Private Sub WindowTypeUIRefresh() Handles ComboArgumentWindowType.SelectionChanged
        If ComboArgumentWindowType Is Nothing Then Return
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
        If AniControlEnabled <> 0 Then Return
        If ComboArgumentVisibie.SelectedIndex = 0 Then
            If MyMsgBox("若在游戏启动后立即关闭启动器，崩溃检测、更改游戏标题等功能将失效。" & vbCrLf &
                        "如果想保留这些功能，可以选择让启动器在游戏启动后隐藏，游戏退出后自动关闭。", "提醒", "继续", "取消") = 2 Then
                ComboArgumentVisibie.SelectedItem = e.RemovedItems(0)
            End If
        End If
    End Sub

    '开启自动内存优化的警告
    Private Sub CheckArgumentRam_Change() Handles CheckArgumentRam.Change
        If AniControlEnabled <> 0 Then Return
        If Not CheckArgumentRam.Checked Then Return
        If MyMsgBox("内存优化会显著延长启动耗时，建议仅在内存不足时开启。" & vbCrLf &
                    "如果你在使用机械硬盘，这还可能导致一小段时间的严重卡顿。" &
                    If(IsAdmin(), "", $"{vbCrLf}{vbCrLf}每次启动游戏，PCL 都需要申请管理员权限以进行内存优化。{vbCrLf}若想自动授予权限，可以右键 PCL，打开 属性 → 兼容性 → 以管理员身份运行此程序。"),
                    "提醒", "确定", "取消") = 2 Then
            CheckArgumentRam.Checked = False
        End If
    End Sub

    '版本隔离提示
    Private Sub ComboArgumentIndie_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndieV2.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        MyMsgBox("默认策略只会对今后新安装的版本生效。" & vbCrLf & "已有版本的隔离策略需要在它的版本设置中调整。")
    End Sub

    'Java 管理跳转
    Private Sub BtnJavaManage_Click(sender As Object, e As RouteEventArgs) Handles BtnGotoJavaManage.Click
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.SetupJava})
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

    '切换到版本独立设置
    Private Sub BtnSwitch_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSwitch.Click
        McVersionCurrent.Load()
        PageVersionLeft.Version = McVersionCurrent
        FrmMain.PageChange(FormMain.PageType.VersionSetup, FormMain.PageSubType.VersionSetup)
    End Sub

End Class
