Public Class PageVersionSetup

    Private Shadows IsLoaded As Boolean = False

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        RefreshRam(False)

        '由于各个版本不同，每次都需要重新加载
        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1

        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

        '内存自动刷新
        Dim timer As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 1)}
        AddHandler timer.Tick, AddressOf RefreshRam
        timer.Start()

    End Sub
    Public Sub Reload()
        Try

            '启动参数
            TextArgumentTitle.Text = Setup.Get("VersionArgumentTitle", Version:=PageVersionLeft.Version)
            TextArgumentInfo.Text = Setup.Get("VersionArgumentInfo", Version:=PageVersionLeft.Version)
            Dim Indie As Integer = Setup.Get("VersionArgumentIndie", Version:=PageVersionLeft.Version)
            If Indie = -1 Then
                Dim ModFolder As New DirectoryInfo(PageVersionLeft.Version.Path & "mods\")
                Dim SaveFolder As New DirectoryInfo(PageVersionLeft.Version.Path & "saves\")
                If (ModFolder.Exists AndAlso ModFolder.EnumerateFiles.Count > 0) OrElse (SaveFolder.Exists AndAlso SaveFolder.EnumerateFiles.Count > 0) Then
                    '自动开启
                    Setup.Set("VersionArgumentIndie", 1, Version:=PageVersionLeft.Version)
                    Log("[Setup] 已自动开启单版本隔离：" & PageVersionLeft.Version.Name)
                    Indie = 1
                Else
                    '使用全局设置
                    Setup.Set("VersionArgumentIndie", 0, Version:=PageVersionLeft.Version)
                    Log("[Setup] 版本隔离使用全局设置：" & PageVersionLeft.Version.Name)
                    Indie = 0
                End If
            End If
            ComboArgumentIndie.SelectedIndex = Indie
            TextAdvanceJvm.Text = Setup.Get("VersionAdvanceJvm", Version:=PageVersionLeft.Version)
            TextAdvanceGame.Text = Setup.Get("VersionAdvanceGame", Version:=PageVersionLeft.Version)
            ComboAdvanceAssets.SelectedIndex = Setup.Get("VersionAdvanceAssets", Version:=PageVersionLeft.Version)
            RefreshJavaComboBox()

            '游戏内存
            CType(FindName("RadioRamType" & Setup.Load("VersionRamType", Version:=PageVersionLeft.Version)), MyRadioBox).Checked = True
            SliderRamCustom.Value = Setup.Get("VersionRamCustom", Version:=PageVersionLeft.Version)

            '服务器
            TextServerEnter.Text = Setup.Get("VersionServerEnter", Version:=PageVersionLeft.Version)
            ComboServerLogin.SelectedIndex = Setup.Get("VersionServerLogin", Version:=PageVersionLeft.Version)
            ComboServerLoginLast = ComboServerLogin.SelectedIndex
            ServerLogin(ComboServerLogin.SelectedIndex)
            TextServerNide.Text = Setup.Get("VersionServerNide", Version:=PageVersionLeft.Version)
            TextServerAuthServer.Text = Setup.Get("VersionServerAuthServer", Version:=PageVersionLeft.Version)
            TextServerAuthName.Text = Setup.Get("VersionServerAuthName", Version:=PageVersionLeft.Version)
            TextServerAuthRegister.Text = Setup.Get("VersionServerAuthRegister", Version:=PageVersionLeft.Version)

        Catch ex As Exception
            Log(ex, "重载版本独立设置时出错", LogLevel.Feedback)
        End Try
    End Sub

    '初始化
    Public Sub Reset()
        Try

            Setup.Reset("VersionServerEnter", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionServerLogin", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionServerNide", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionServerAuthServer", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionServerAuthRegister", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionServerAuthName", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionArgumentTitle", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionArgumentInfo", Version:=PageVersionLeft.Version)
            Setup.Set("VersionArgumentIndie", 0, Version:=PageVersionLeft.Version)
            Setup.Reset("VersionRamType", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionRamCustom", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionAdvanceJvm", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionAdvanceGame", Version:=PageVersionLeft.Version)
            Setup.Reset("VersionAdvanceAssets", Version:=PageVersionLeft.Version)

            Setup.Reset("VersionArgumentJavaSelect", Version:=PageVersionLeft.Version)
            JavaSearchLoader.Start(IsForceRestart:=True)

            Log("[Setup] 已初始化版本独立设置")
            Hint("已初始化版本独立设置！", HintType.Finish, False)
        Catch ex As Exception
            Log(ex, "初始化版本独立设置失败", LogLevel.Msgbox)
        End Try

        Reload()
    End Sub

    '将控件改变路由到设置改变
    Private Shared Sub RadioBoxChange(sender As MyRadioBox, e As Object) Handles RadioRamType0.Check, RadioRamType1.Check, RadioRamType2.Check
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag.ToString.Split("/")(0), Val(sender.Tag.ToString.Split("/")(1)), Version:=PageVersionLeft.Version)
    End Sub
    Private Shared Sub TextBoxChange(sender As MyTextBox, e As Object) Handles TextServerEnter.ValidatedTextChanged, TextArgumentInfo.ValidatedTextChanged, TextAdvanceGame.ValidatedTextChanged, TextAdvanceJvm.ValidatedTextChanged, TextServerNide.ValidatedTextChanged, TextServerAuthName.ValidatedTextChanged, TextServerAuthRegister.ValidatedTextChanged, TextServerAuthServer.ValidatedTextChanged, TextArgumentTitle.ValidatedTextChanged
        If AniControlEnabled = 0 Then
            Dim HandledText As String = sender.Text
            If sender.Tag = "VersionServerAuthServer" OrElse sender.Tag = "VersionServerAuthRegister" Then HandledText = HandledText.TrimEnd("/")
            Setup.Set(sender.Tag, HandledText, Version:=PageVersionLeft.Version)
        End If
    End Sub
    Private Shared Sub SliderChange(sender As MySlider, e As Object) Handles SliderRamCustom.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Value, Version:=PageVersionLeft.Version)
    End Sub
    Private Shared Sub ComboChange(sender As MyComboBox, e As Object) Handles ComboArgumentIndie.SelectionChanged, ComboAdvanceAssets.SelectionChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.SelectedIndex, Version:=PageVersionLeft.Version)
    End Sub

#Region "游戏内存"

    Public Sub RamType(Type As Integer)
        If SliderRamCustom Is Nothing Then Exit Sub
        SliderRamCustom.IsEnabled = (Type = 1)
    End Sub

    ''' <summary>
    ''' 刷新 UI 上的 RAM 显示。
    ''' </summary>
    Public Sub RefreshRam(ShowAnim As Boolean)
        If LabRamGame Is Nothing OrElse LabRamUsed Is Nothing OrElse FrmMain.PageCurrent <> FormMain.PageType.VersionSetup OrElse FrmVersionLeft.PageID <> FormMain.PageSubType.VersionSetup Then Exit Sub
        '获取内存情况
        Dim RamGame As Double = GetRam(PageVersionLeft.Version)
        Dim RamTotal As Double = Math.Round(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024 / 1024 * 10) / 10
        Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024 * 10) / 10
        Dim RamGameActual As Double = Math.Min(RamGame, RamAvailable)
        Dim RamUsed As Double = RamTotal - RamAvailable
        Dim RamEmpty As Double = Math.Round(MathRange(RamTotal - RamUsed - RamGame, 0, 1000) * 10) / 10
        '设置最大可用内存
        If RamTotal <= 1.5 Then
            SliderRamCustom.MaxValue = Math.Max(Math.Floor((RamTotal - 0.3) / 0.1), 1)
        ElseIf RamTotal <= 8 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 1.5) / 0.5) + 12
        ElseIf RamTotal <= 16 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 8) / 1) + 25
        ElseIf RamTotal <= 32 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 16) / 2) + 33
        Else
            SliderRamCustom.MaxValue = Math.Min(Math.Floor((RamTotal - 32) / 4) + 41, 49)
        End If
        '设置文本
        LabRamGame.Text = If(RamGame = Math.Floor(RamGame), RamGame & ".0", RamGame) & " GB" &
                          If(RamGame <> RamGameActual, " (可用 " & If(RamGameActual = Math.Floor(RamGameActual), RamGameActual & ".0", RamGameActual) & " GB)", "")
        LabRamUsed.Text = If(RamUsed = Math.Floor(RamUsed), RamUsed & ".0", RamUsed) & " GB"
        LabRamTotal.Text = " / " & If(RamTotal = Math.Floor(RamTotal), RamTotal & ".0", RamTotal) & " GB"
        LabRamWarn.Visibility = If(RamGame = 1 AndAlso Not JavaUse64Bit(PageVersionLeft.Version) AndAlso Not Is32BitSystem, Visibility.Visible, Visibility.Collapsed)
        If ShowAnim Then
            '宽度动画
            AniStart({
                AaGridLengthWidth(ColumnRamUsed, RamUsed - ColumnRamUsed.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaGridLengthWidth(ColumnRamGame, RamGameActual - ColumnRamGame.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaGridLengthWidth(ColumnRamEmpty, RamEmpty - ColumnRamEmpty.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong))
            }, "VersionSetup Ram Grid")
        Else
            '宽度设置
            ColumnRamUsed.Width = New GridLength(RamUsed, GridUnitType.Star)
            ColumnRamGame.Width = New GridLength(RamGameActual, GridUnitType.Star)
            ColumnRamEmpty.Width = New GridLength(RamEmpty, GridUnitType.Star)
        End If
    End Sub
    Private Sub RefreshRam() Handles SliderRamCustom.Change, RadioRamType0.Check, RadioRamType1.Check, RadioRamType2.Check
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
                        }, "VersionSetup Ram TextLeft")
                Case 1
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
                Case 2
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, 1 - LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
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
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("VersionSetup Ram TextRight")) Then
                '需要动画
                AniStart({
                        AaX(LabRamGame, TotalWidth - LabGameWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, TotalWidth - LabGameTitleWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "VersionSetup Ram TextRight")
            Else
                '不需要动画
                LabRamGame.Margin = New Thickness(TotalWidth - LabGameWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(TotalWidth - LabGameTitleWidth, 0, 0, 5)
            End If
        Else
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("VersionSetup Ram TextRight")) Then
                '需要动画
                AniStart({
                        AaX(LabRamGame, 2 + RectUsedWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, 2 + RectUsedWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "VersionSetup Ram TextRight")
            Else
                '不需要动画
                LabRamGame.Margin = New Thickness(2 + RectUsedWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(2 + RectUsedWidth, 0, 0, 5)
            End If
        End If
        RamTextRight = Right
    End Sub

    ''' <summary>
    ''' 获取当前设置的 RAM 值。单位为 GB。
    ''' </summary>
    Public Shared Function GetRam(Version As McVersion) As Double
        '使用全局设置
        If Setup.Get("VersionRamType", Version:=Version) = 2 Then
            Return PageSetupLaunch.GetRam(Version, True)
        End If
        '使用当前版本的设置
        Dim RamGive As Double
        If Setup.Get("VersionRamType", Version:=Version) = 0 Then
            '自动配置
            Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024 * 10) / 10
            '确定需求的内存值
            Dim RamMininum As Double '无论如何也需要保证的最低限度内存
            Dim RamTarget1 As Double '估计能勉强带动了的内存
            Dim RamTarget2 As Double '估计没啥问题了的内存
            Dim RamTarget3 As Double '安装过多附加组件需要的内存
            If Version IsNot Nothing AndAlso Not Version.IsLoaded Then Version.Load()
            If Version IsNot Nothing AndAlso Version.Version.Modable Then
                '可安装 Mod 的版本
                Dim ModDir As New DirectoryInfo(Version.PathIndie & "mods\")
                Dim ModCount As Integer = If(ModDir.Exists, ModDir.GetFiles.Length, 0)
                RamMininum = 0.4 + ModCount / 150
                RamTarget1 = 1.5 + ModCount / 100
                RamTarget2 = 3 + ModCount / 60
                RamTarget3 = 6 + ModCount / 30
            ElseIf Version IsNot Nothing AndAlso Version.Version.HasOptiFine Then
                'OptiFine 版本
                RamMininum = 0.3
                RamTarget1 = 1.5
                RamTarget2 = 3
                RamTarget3 = 6
            Else
                '普通版本
                RamMininum = 0.3
                RamTarget1 = 1.5
                RamTarget2 = 2.5
                RamTarget3 = 4
            End If
            Dim RamDelta As Double
            '预分配内存，阶段一，0 ~ T1，100%
            RamDelta = RamTarget1
            RamAvailable = Math.Max(0, RamAvailable - 0.1)
            RamGive += Math.Min(RamAvailable * 1, RamDelta)
            RamAvailable -= RamDelta / 1 + 0.1
            If RamAvailable < 0.1 Then GoTo PreFin
            '预分配内存，阶段二，T1 ~ T2，80%
            RamDelta = RamTarget2 - RamTarget1
            RamAvailable = Math.Max(0, RamAvailable - 0.1)
            RamGive += Math.Min(RamAvailable * 0.8, RamDelta)
            RamAvailable -= RamDelta / 0.8 + 0.1
            If RamAvailable < 0.1 Then GoTo PreFin
            '预分配内存，阶段三，T2 ~ T3，60%
            RamDelta = RamTarget3 - RamTarget2
            RamAvailable = Math.Max(0, RamAvailable - 0.2)
            RamGive += Math.Min(RamAvailable * 0.6, RamDelta)
            RamAvailable -= RamDelta / 0.6 + 0.2
            If RamAvailable < 0.1 Then GoTo PreFin
            '预分配内存，阶段四，T3 ~ T3 * 2，40%
            RamDelta = RamTarget3
            RamAvailable = Math.Max(0, RamAvailable - 0.3)
            RamGive += Math.Min(RamAvailable * 0.4, RamDelta)
            RamAvailable -= RamDelta / 0.4 + 0.3
            If RamAvailable < 0.1 Then GoTo PreFin
PreFin:
            '不低于最低值
            RamGive = Math.Round(Math.Max(RamGive, RamMininum), 1)
        Else
            '手动配置
            Dim Value As Integer = Setup.Get("VersionRamCustom", Version:=Version)
            If Value <= 12 Then
                RamGive = Value * 0.1 + 0.3
            ElseIf Value <= 25 Then
                RamGive = (Value - 12) * 0.5 + 1.5
            ElseIf Value <= 33 Then
                RamGive = (Value - 25) * 1 + 8
            ElseIf Value <= 41 Then
                RamGive = (Value - 33) * 2 + 16
            Else
                RamGive = (Value - 41) * 4 + 32
            End If
        End If
        '若使用 32 位 Java，则限制为 1G
        If Not JavaUse64Bit(PageVersionLeft.Version) Then RamGive = Math.Min(1, RamGive)
        Return RamGive
    End Function

#End Region

#Region "服务器"

    '全局
    Private ComboServerLoginLast As Integer
    Private Sub ComboServerLogin_Changed() Handles ComboServerLogin.SelectionChanged, TextServerNide.ValidatedTextChanged, TextServerAuthServer.ValidatedTextChanged, TextServerAuthRegister.ValidatedTextChanged
        If AniControlEnabled <> 0 Then Exit Sub
        ServerLogin(ComboServerLogin.SelectedIndex)
        '检查是否输入正确，正确才触发设置改变
        If ComboServerLogin.SelectedIndex = 3 AndAlso TextServerNide.ValidateResult <> "" Then Exit Sub
        If ComboServerLogin.SelectedIndex = 4 AndAlso TextServerAuthServer.ValidateResult <> "" Then Exit Sub
        '检查结果是否发生改变，未改变则不触发设置改变
        If ComboServerLoginLast = ComboServerLogin.SelectedIndex Then Exit Sub
        '触发
        ComboServerLoginLast = ComboServerLogin.SelectedIndex
        ComboChange(ComboServerLogin, Nothing)
    End Sub
    Public Sub ServerLogin(Type As Integer)
        If LabServerNide Is Nothing Then Exit Sub
        LabServerNide.Visibility = If(Type = 3, Visibility.Visible, Visibility.Collapsed)
        TextServerNide.Visibility = If(Type = 3, Visibility.Visible, Visibility.Collapsed)
        PanServerNide.Visibility = If(Type = 3, Visibility.Visible, Visibility.Collapsed)
        LabServerAuthName.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        TextServerAuthName.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        LabServerAuthRegister.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        TextServerAuthRegister.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        LabServerAuthServer.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        TextServerAuthServer.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        BtnServerAuthLittle.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        CardServer.TriggerForceResize()
    End Sub

    '统一通行证
    Private Sub BtnServerNideWeb_Click(sender As Object, e As EventArgs) Handles BtnServerNideWeb.Click
        OpenWebsite("https://login.mc-user.com:233/server/intro")
    End Sub
    Private Sub BtnServerNideBbs_Click(sender As Object, e As EventArgs) Handles BtnServerNideBbs.Click
        OpenWebsite("https://www.mcbbs.net/thread-729821-1-1.html")
    End Sub

    'Little Skin
    Private Sub BtnServerAuthLittle_Click(sender As Object, e As EventArgs) Handles BtnServerAuthLittle.Click
        TextServerAuthServer.Text = "https://littleskin.cn/api/yggdrasil"
        TextServerAuthRegister.Text = "https://littleskin.cn/auth/register"
        TextServerAuthName.Text = "Little Skin 登录"
    End Sub

#End Region

#Region "Java 选择"

    '刷新 Java 下拉框显示
    Public Sub RefreshJavaComboBox()
        If ComboArgumentJava Is Nothing Then Exit Sub
        '初始化列表
        ComboArgumentJava.Items.Clear()
        ComboArgumentJava.Items.Add(New MyComboBoxItem With {.Content = "使用全局设置", .Tag = "使用全局设置"})
        ComboArgumentJava.Items.Add(New MyComboBoxItem With {.Content = "自动选择适合所选游戏版本的 Java", .Tag = "自动选择"})
        '更新列表
        Dim SelectedItem As MyComboBoxItem = Nothing
        Dim SelectedBySetup As String = Setup.Get("VersionArgumentJavaSelect", Version:=PageVersionLeft.Version)
        Try
            For Each Java In JavaList
                Dim ListItem = New MyComboBoxItem With {.Content = Java.ToString, .ToolTip = Java.PathFolder, .Tag = Java}
                ToolTipService.SetPlacement(ListItem, Primitives.PlacementMode.Right)
                ToolTipService.SetHorizontalOffset(ListItem, 5)
                ComboArgumentJava.Items.Add(ListItem)
                '判断人为选中
                If SelectedBySetup = "" OrElse SelectedBySetup = "使用全局设置" Then Continue For
                If JavaEntry.FromJson(GetJson(SelectedBySetup)).PathFolder = Java.PathFolder Then SelectedItem = ListItem
            Next
        Catch ex As Exception
            Setup.Set("VersionArgumentJavaSelect", "使用全局设置", Version:=PageVersionLeft.Version)
            Log(ex, "更新版本设置 Java 下拉框失败", LogLevel.Feedback)
        End Try
        '更新选择项
        If SelectedItem Is Nothing AndAlso JavaList.Count > 0 Then
            If SelectedBySetup = "" Then
                SelectedItem = ComboArgumentJava.Items(1) '选中 “自动选择”
            Else
                SelectedItem = ComboArgumentJava.Items(0) '选中 “使用全局设置”
            End If
        End If
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
        If "使用全局设置".Equals(SelectedJava) Then
            '选择 “自动”
            Setup.Set("VersionArgumentJavaSelect", "使用全局设置", Version:=PageVersionLeft.Version)
            Log("[Java] 修改版本 Java 选择设置：使用全局设置")
        ElseIf "自动选择".Equals(SelectedJava) Then
            '选择 “自动”
            Setup.Set("VersionArgumentJavaSelect", "", Version:=PageVersionLeft.Version)
            Log("[Java] 修改版本 Java 选择设置：自动选择")
        Else
            '选择指定项
            Setup.Set("VersionArgumentJavaSelect", CType(SelectedJava.ToJson(), JObject).ToString(Newtonsoft.Json.Formatting.None), Version:=PageVersionLeft.Version)
            Log("[Java] 修改版本 Java 选择设置：" & SelectedJava.ToString)
        End If
        RefreshRam(True)
    End Sub

#End Region

End Class
