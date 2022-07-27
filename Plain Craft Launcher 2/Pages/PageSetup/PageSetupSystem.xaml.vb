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
    Private Shared Sub CheckBoxChange(sender As MyCheckBox, e As Object) Handles CheckDebugMode.Change, CheckDebugDelay.Change, CheckDebugSkipCopy.Change, CheckUpdateRelease.Change, CheckUpdateSnapshot.Change, CheckHelpChinese.Change
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
        SliderDownloadThread.GetHintText = Function(Value As Integer)
                                               Return Value + 1
                                           End Function
        SliderDownloadSpeed.GetHintText = Function(Value As Integer)
                                              If Value <= 14 Then
                                                  Return (Value + 1) * 0.1 & " M/s"
                                              ElseIf Value <= 31 Then
                                                  Return (Value - 11) * 0.5 & " M/s"
                                              ElseIf Value <= 41 Then
                                                  Return (Value - 21) & " M/s"
                                              Else
                                                  Return "无限制"
                                              End If
                                          End Function
        SliderDebugAnim.GetHintText = Function(Value As Integer)
                                          Return If(Value > 29, "关闭", (Value / 10 + 0.1) & "x")
                                      End Function
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Exit Sub
        If Not Setup.Get("HintDownloadThread") Then
            Setup.Set("HintDownloadThread", True)
            MyMsgBox("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。" & vbCrLf &
                     "一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", IsWarn:=True)
        End If
    End Sub

    '调试模式
    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint("部分调试信息将在刷新或启动器重启后切换显示！",, False)
    End Sub

    '清理缓存
    Private IsClearingCache As Boolean = False
    Private Sub BtnSystemCacheClear_Click(sender As Object, e As EventArgs) Handles BtnSystemCacheClear.Click
        If HasDownloadingTask() Then
            Hint("请在所有下载任务完成后再清理缓存！", HintType.Critical)
            Exit Sub
        End If
        If MyMsgBox("你确定要清理缓存吗？" & vbCrLf & "在清理缓存后，PCL2 会被强制关闭，以避免缓存缺失带来的异常。", "清理缓存", "确定", "取消") = 2 Then
            Exit Sub
        End If
        If IsClearingCache Then Exit Sub
        IsClearingCache = True
        Try
            Dim TotalSize As ULong = DeleteCacheDirectory(PathTemp)
            If TotalSize <= 0 Then
                Hint("没有可清理的缓存！")
            Else
                MyMsgBox("已清理 " & GetString(TotalSize) & " 缓存！" & vbCrLf & "PCL2 即将自动关闭。", "缓存已清理", ForceWait:=True)
                FrmMain.EndProgram(False)
            End If
        Catch ex As Exception
            Log(ex, "清理缓存失败", LogLevel.Hint)
        Finally
            IsClearingCache = False
        End Try
    End Sub
    Private Function DeleteCacheDirectory(Path As String, Optional TotalSize As ULong = 0) As ULong
        If Not Directory.Exists(Path) Then Return TotalSize
        Dim Temp As String()
        Temp = Directory.GetFiles(Path)
        For Each str As String In Temp
            If str = PathTemp & "CustomSkin.png" Then Continue For '不删除自定义皮肤
            Try
                Dim Info As New FileInfo(str)
                Dim FileActualSize = Math.Ceiling(Info.Length / 4096) * 4096
                Info.Delete()
                TotalSize += FileActualSize
            Catch ex As Exception
                Log(ex, "删除失败的缓存文件（" & str & "）")
            End Try
        Next
        Temp = Directory.GetDirectories(Path)
        For Each str As String In Temp
            TotalSize += DeleteCacheDirectory(str)
        Next
        Return TotalSize
    End Function

    '自动更新
    Private Sub ComboSystemActivity_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemActivity.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        If ComboSystemActivity.SelectedIndex = 2 Then
            If MyMsgBox("若选择此项，即使在将来出现严重问题时，你也无法获取相关通知。" & vbCrLf &
                        "例如，如果发现某个版本游戏存在严重 Bug，你可能就会因为无法得到通知而导致无法预知的后果。" & vbCrLf & vbCrLf &
                        "一般选择 仅在有重要通知时显示公告 就可以让你尽量不受打扰了。" & vbCrLf &
                        "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "继续", "取消") = 2 Then
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
                        "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "继续", "取消") = 2 Then
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

End Class
