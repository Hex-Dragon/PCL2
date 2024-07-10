Class PageSetupSystem

#Region "语言"
    Private Sub PageSetupUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        AniControlEnabled -= 1

        '读取设置
        Select Case Lang
            Case "en_US"
                ComboBackgroundSuit.SelectedIndex = 0
            Case "zh_CN"
                ComboBackgroundSuit.SelectedIndex = 1
            Case "zh_HK"
                ComboBackgroundSuit.SelectedIndex = 2
            Case "zh_TW"
                ComboBackgroundSuit.SelectedIndex = 3
        End Select

        AniControlEnabled += 1
    End Sub

    Private Sub RefreshLang() Handles ComboBackgroundSuit.SelectionChanged
        If IsLoaded Then
            If Not ComboBackgroundSuit.IsLoaded Then Exit Sub
            Lang = CType(ComboBackgroundSuit.SelectedItem, MyComboBoxItem).Tag
            Application.Current.Resources.MergedDictionaries(1) = New ResourceDictionary With {.Source = New Uri("pack://application:,,,/Resources/Language/" & Lang & ".xaml", UriKind.RelativeOrAbsolute)}
            WriteReg("Lang", Lang)
        End If
    End Sub
#End Region

    Private Shadows IsLoaded As Boolean = False

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

#If Not BETA Then
        ItemSystemUpdateDownload.Content = GetLang("LangPageSetupSystemSystemLaunchUpdateE")
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
            Hint(GetLang("LangPageSetupSystemLaunchResetSuccess"), HintType.Finish, False)
        Catch ex As Exception
            Log(ex, GetLang("LangPageSetupSystemLaunchResetFail"), LogLevel.Msgbox)
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
                    Return GetLang("LangPageSetupSystemDownloadSpeedUnlimit")
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(v > 29, GetLang("LangPageSetupSystemDebugAnimSpeedDisable"), (v / 10 + 0.1) & "x")
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Exit Sub
        If Not Setup.Get("HintDownloadThread") Then
            Setup.Set("HintDownloadThread", True)
            MyMsgBox(GetLang("LangPageSetupSystemDownloadSpeedDialogThreadTooMuchContent"), GetLang("LangDialogTitleWarning"), GetLang("LangDialogBtnIC"), IsWarn:=True)
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
        If AniControlEnabled = 0 Then Hint(GetLang("LangPageSetupSystemDebugNeedRestart"),, False)
    End Sub

    '自动更新
    Private Sub ComboSystemActivity_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemActivity.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If ComboSystemActivity.SelectedIndex = 2 Then
            If MyMsgBox(GetLang("LangPageSetupSystemLaunchDialogAnnouncementSilentContent"), GetLang("LangDialogTitleWarning"), GetLang("LangPageSetupSystemLaunchDialogAnnouncementBtnConfirm"), GetLang("LangDialogBtnCancel"), IsWarn:=True) = 2 Then
                ComboSystemActivity.SelectedItem = e.RemovedItems(0)
            End If
        End If
    End Sub
    Private Sub ComboSystemUpdate_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemUpdate.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If ComboSystemUpdate.SelectedIndex = 3 Then
            If MyMsgBox(GetLang("LangPageSetupSystemLaunchDialogAnnouncementDisableContent"), GetLang("LangDialogTitleWarning"), GetLang("LangPageSetupSystemLaunchDialogAnnouncementBtnConfirm"), GetLang("LangDialogBtnCancel"), IsWarn:=True) = 2 Then
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
            Log(ex, GetLang("LangPageSetupSystemSystemLaunchUpdateFail"), LogLevel.Feedback)
            Return Nothing
        End Try
    End Function

End Class
