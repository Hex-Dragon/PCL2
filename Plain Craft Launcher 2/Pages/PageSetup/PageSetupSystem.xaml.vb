Class PageSetupSystem

#Region "语言"
    'Private Sub PageSetupUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
    '  AniControlEnabled -= 1

    '  '读取设置
    '  Select Case Lang
    '      Case "zh_CN"
    '          ComboLang.SelectedIndex = 0
    '      Case "zh_HK"
    '          ComboLang.SelectedIndex = 1
    '      Case "en_US"
    '          ComboLang.SelectedIndex = 2
    '  End Select
    '  CheckDebug.Checked = ReadReg("SystemDebugMode", "False")

    '  AniControlEnabled += 1
    'End Sub

    'Private Sub RefreshLang() Handles ComboLang.SelectionChanged
    '  If IsLoaded Then
    '      If Not ComboLang.IsLoaded Then Exit Sub
    '      Lang = CType(ComboLang.SelectedItem, MyComboBoxItem).Tag
    '      Application.Current.Resources.MergedDictionaries(1) = New ResourceDictionary With {.Source = New Url("Languages\" & Lang & ".xaml", UrlKind.Relative)}
    '      WriteReg("Lang", Lang)
    '  End If
    'End Sub
#End Region

    Private Shadows IsLoaded As Boolean = False

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

#If Not BETA Then
        ItemSystemUpdateDownload.Content = "在有新版本时自动下载（更新快照版可能需要更新密钥）"
#End If

        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

        AniControlEnabled += 1
        Reload()
        SliderLoad()
        AniControlEnabled -= 1

    End Sub
    Public Sub Reload()

        '下载
        SliderDownloadThread.Value = Setup.Get("ToolDownloadThread")
        SliderDownloadSpeed.Value = Setup.Get("ToolDownloadSpeed")
        ComboDownloadVersion.SelectedIndex = Setup.Get("ToolDownloadVersion")
        ComboDownloadTranslate.SelectedIndex = Setup.Get("ToolDownloadTranslate")
        CheckDownloadKeepModpack.Checked = Setup.Get("ToolDownloadKeepModpack")
        CheckDownloadIgnoreQuilt.Checked = Setup.Get("ToolDownloadIgnoreQuilt")
        CheckDownloadCert.Checked = Setup.Get("ToolDownloadCert")

        'Minecraft 更新提示
        CheckUpdateRelease.Checked = Setup.Get("ToolUpdateRelease")
        CheckUpdateSnapshot.Checked = Setup.Get("ToolUpdateSnapshot")

        '辅助设置
        CheckHelpChinese.Checked = Setup.Get("ToolHelpChinese")

        '系统设置
        ComboSystemUpdate.SelectedIndex = Setup.Get("SystemSystemUpdate")
        ComboSystemActivity.SelectedIndex = Setup.Get("SystemSystemActivity")
        TextSystemCache.Text = Setup.Get("SystemSystemCache")

        '调试选项
        CheckDebugMode.Checked = Setup.Get("SystemDebugMode")
        SliderDebugAnim.Value = Setup.Get("SystemDebugAnim")
        CheckDebugDelay.Checked = Setup.Get("SystemDebugDelay")
        CheckDebugSkipCopy.Checked = Setup.Get("SystemDebugSkipCopy")

    End Sub

    '初始化
    Public Sub Reset()
        Try
            Setup.Reset("ToolDownloadThread")
            Setup.Reset("ToolDownloadSpeed")
            Setup.Reset("ToolDownloadVersion")
            Setup.Reset("ToolDownloadTranslate")
            Setup.Reset("ToolDownloadKeepModpack")
            Setup.Reset("ToolDownloadIgnoreQuilt")
            Setup.Reset("ToolDownloadCert")
            Setup.Reset("ToolUpdateRelease")
            Setup.Reset("ToolUpdateSnapshot")
            Setup.Reset("ToolHelpChinese")
            Setup.Reset("SystemDebugMode")
            Setup.Reset("SystemDebugAnim")
            Setup.Reset("SystemDebugDelay")
            Setup.Reset("SystemDebugSkipCopy")
            Setup.Reset("SystemSystemCache")
            Setup.Reset("SystemSystemUpdate")
            Setup.Reset("SystemSystemActivity")

            Log("[Setup] 已初始化启动器页设置")
            Hint("已初始化启动器页设置！", HintType.Finish, False)
        Catch ex As Exception
            Log(ex, "初始化启动器页设置失败", LogLevel.Msgbox)
        End Try

        Reload()
    End Sub

    '将控件改变路由到设置改变
    Private Shared Sub CheckBoxChange(sender As MyCheckBox, e As Object) Handles CheckDebugMode.Change, CheckDebugDelay.Change, CheckDebugSkipCopy.Change, CheckUpdateRelease.Change, CheckUpdateSnapshot.Change, CheckHelpChinese.Change, CheckDownloadKeepModpack.Change, CheckDownloadIgnoreQuilt.Change, CheckDownloadCert.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Checked)
    End Sub
    Private Shared Sub SliderChange(sender As MySlider, e As Object) Handles SliderDebugAnim.Change, SliderDownloadThread.Change, SliderDownloadSpeed.Change
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Value)
    End Sub
    Private Shared Sub ComboChange(sender As MyComboBox, e As Object) Handles ComboDownloadVersion.SelectionChanged, ComboDownloadTranslate.SelectionChanged, ComboSystemUpdate.SelectionChanged, ComboSystemActivity.SelectionChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.SelectedIndex)
    End Sub
    Private Shared Sub TextBoxChange(sender As MyTextBox, e As Object) Handles TextSystemCache.ValidatedTextChanged
        If AniControlEnabled = 0 Then Setup.Set(sender.Tag, sender.Text)
    End Sub

    '滑动条
    Private Sub SliderLoad()
        SliderDownloadThread.GetHintText = Function(v) v + 1
        SliderDownloadSpeed.GetHintText =
        Function(v)
            Select Case v
                Case Is <= 14
                    Return (v + 1) * 0.1 & " M/s"
                Case Is <= 31
                    Return (v - 11) * 0.5 & " M/s"
                Case Is <= 41
                    Return (v - 21) & " M/s"
                Case Else
                    Return "无限制"
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(v > 29, "关闭", (v / 10 + 0.1) & "x")
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Exit Sub
        If Not Setup.Get("HintDownloadThread") Then
            Setup.Set("HintDownloadThread", True)
            MyMsgBox("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。" & vbCrLf &
                     "一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", IsWarn:=True)
        End If
    End Sub

    '识别码/解锁码替代入口
    Private Sub BtnSystemIdentify_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemIdentify.Click
        PageOtherAbout.CopyUniqueAddress()
    End Sub
    Private Sub BtnSystemUnlock_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemUnlock.Click
        DonateCodeInput()
    End Sub

    '调试模式
    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint("部分调试信息将在刷新或启动器重启后切换显示！",, False)
    End Sub

    '自动更新
    Private Sub ComboSystemActivity_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemActivity.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If ComboSystemActivity.SelectedIndex = 2 Then
            If MyMsgBox("若选择此项，即使在将来出现严重问题时，你也无法获取相关通知。" & vbCrLf &
                        "例如，如果发现某个版本游戏存在严重 Bug，你可能就会因为无法得到通知而导致无法预知的后果。" & vbCrLf & vbCrLf &
                        "一般选择 仅在有重要通知时显示公告 就可以让你尽量不受打扰了。" & vbCrLf &
                        "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
                ComboSystemActivity.SelectedItem = e.RemovedItems(0)
            End If
        End If
    End Sub
    Private Sub ComboSystemUpdate_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemUpdate.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If ComboSystemUpdate.SelectedIndex = 3 Then
            If MyMsgBox("若选择此项，即使在启动器将来出现严重问题时，你也无法获取更新并获得修复。" & vbCrLf &
                        "例如，如果官方修改了登录方式，从而导致现有启动器无法登录，你可能就会因为无法更新而无法开始游戏。" & vbCrLf & vbCrLf &
                        "一般选择 仅在有重大漏洞更新时显示提示 就可以让你尽量不受打扰了。" & vbCrLf &
                        "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
                ComboSystemUpdate.SelectedItem = e.RemovedItems(0)
            End If
        End If
    End Sub
    Private Sub BtnSystemUpdate_Click(sender As Object, e As EventArgs) Handles BtnSystemUpdate.Click
        UpdateCheckByButton()
    End Sub
    ''' <summary>
    ''' 启动器是否已经是最新版？
    ''' 若返回 Nothing，则代表无更新缓存文件或出错。
    ''' </summary>
    Public Shared Function IsLauncherNewest() As Boolean?
        Try
            '确认服务器公告是否正常
            Dim ServerContent As String = ReadFile(PathTemp & "Cache\Notice.cfg")
            If ServerContent.Split("|").Count < 3 Then Return Nothing
            '确认是否为最新
#If BETA Then
            Dim NewVersionCode As Integer = ServerContent.Split("|")(2)
#Else
            Dim NewVersionCode As Integer = ServerContent.Split("|")(1)
#End If
            Return NewVersionCode <= VersionCode
        Catch ex As Exception
            Log(ex, "确认启动器更新失败", LogLevel.Feedback)
            Return Nothing
        End Try
    End Function

#Region "导入/导出设置"
    '导出设置
    Private Sub BtnSystemSettingExp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingExp.Click
        Dim encodedExport As Boolean = False
        Select Case MyMsgBox("是否需要导出账号密码、主题颜色等个人设置？" & vbCrLf &
                             "如果确定，则应妥善保存该设置，避免被他人窃取。这部分设置仅对这台电脑有效。",
                 Button1:="否", Button2:="是", Button3:="取消")
            Case 1
                encodedExport = False
            Case 2
                encodedExport = True
            Case 3
                Exit Sub
        End Select
        Dim savePath As String = SelectAs("选择保存位置", "PCL 导出配置.ini", "PCL 配置文件(*.ini)|*.ini", Path).Replace("/", "\")
        If savePath = "" Then Exit Sub
        If Setup.SetupExport(savePath, ExportEncoded:=encodedExport) Then
            Hint("配置导出成功！", HintType.Finish)
            OpenExplorer($"/select,""{savePath}""")
        End If
    End Sub

    '导入
    Private Sub BtnSystemSettingImp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingImp.Click
        If MyMsgBox("导入设置后，现有的设置将会被覆盖，建议先导出现有设置。" & vbCrLf &
                    "当前设置将会被备份到 PCL 文件夹下的 Setup.ini.old 文件，如有需要可以自行还原。" & vbCrLf &
                    "是否继续？", Button1:="继续", Button2:="取消") = 1 Then
            Dim sourcePath As String = SelectFile("PCL 配置文件(*.ini)|*.ini", "选择配置文件")
            If sourcePath = "" Then Exit Sub
            If Setup.SetupImport(sourcePath) Then
                '把导入的设置 UI 化
                If FrmSetupLaunch IsNot Nothing Then FrmSetupLaunch.Reload()
                If FrmSetupUI IsNot Nothing Then FrmSetupUI.Reload()
                If FrmSetupSystem IsNot Nothing Then FrmSetupSystem.Reload()
                Hint("配置导入成功！", HintType.Finish)
            End If
        End If
    End Sub

#End Region

End Class
