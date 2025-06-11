Public Class PageLogRight
    Public Sub Init() Handles Me.Initialized
        PanLogCard.Inlines.Clear()
        'TODO(i18n): 文本 @ 标题栏 - 实时日志卡片标题
        PanLogCard.Inlines.Add(New Run("实时日志"))
        PanLogCard.Inlines.Add(New Run(" | "))
        LabDebug = New Run("0 Debug") With {.Foreground = Application.Current.Resources("ColorBrushDebug")}
        PanLogCard.Inlines.Add(LabDebug)
        PanLogCard.Inlines.Add(New Run(" | "))
        LabInfo = New Run("0 Info") With {.Foreground = Application.Current.Resources(If(IsDarkMode, "ColorBrushInfoDark", "ColorBrushInfo"))}
        PanLogCard.Inlines.Add(LabInfo)
        PanLogCard.Inlines.Add(New Run(" | "))
        LabWarn = New Run("0 Warn") With {.Foreground = Application.Current.Resources("ColorBrushWarn")}
        PanLogCard.Inlines.Add(LabWarn)
        PanLogCard.Inlines.Add(New Run(" | "))
        LabError = New Run("0 Error") With {.Foreground = Application.Current.Resources("ColorBrushError")}
        PanLogCard.Inlines.Add(LabError)
        PanLogCard.Inlines.Add(New Run(" | "))
        LabFatal = New Run("0 Fatal") With {.Foreground = Application.Current.Resources("ColorBrushFatal")}
        PanLogCard.Inlines.Add(LabFatal)
    End Sub

    Private Sub PageLogRight_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1
    End Sub
    Public Sub Reload()
        '初始化
        If FrmLogLeft.CurrentLog Is Nothing OrElse FrmLogLeft.CurrentUuid <= 0 OrElse FrmLogLeft.ShownLogs.Count = 0 Then
            FrmMain.PageChange(FrmMain.PageCurrent)
            Return
        End If
        PanAllBack.Visibility = Visibility.Visible
        CardOperation.Visibility = Visibility.Visible
        BtnOperationKill.IsEnabled = Not FrmLogLeft.CurrentLog.GameProcess.HasExited
        BtnOperationExportStackDump.IsEnabled = (Not FrmLogLeft.CurrentLog.GameProcess.HasExited) And Not String.IsNullOrWhiteSpace(FrmLogLeft.CurrentLog.JStackPath)
        SliderMaxLog.Value = Setup.Get("SystemMaxLog")
        SliderMaxLog.GetHintText =
            Function(v)
                'y = 10x + 50 (0 <= x <= 5, 50 <= y <= 100)
                'y = 50x - 150 (5 < x <= 13, 100 < y <= 500)
                'y = 100x - 800 (13 < x <= 28, 500 < y <= 2000)
                Select Case v
                    Case Is <= 5
                        Return v * 10 + 50
                    Case Is <= 13
                        Return v * 50 - 150
                    Case Is <= 28
                        Return v * 100 - 800
                    Case Else
                        Return "无限制"
                End Select
            End Function
        '绑定日志输出
        PanLog.Document = FrmLogLeft.FlowDocuments(FrmLogLeft.CurrentUuid)
        '绑定事件
        AddHandler FrmLogLeft.CurrentLog.LogOutput, AddressOf OnLogOutput
        AddHandler FrmLogLeft.CurrentLog.GameExit, AddressOf OnGameExit
        RefreshLabText()
    End Sub

    Private Sub RefreshLabText()
        '刷新计数器

        LabFatal.Text = $"{FrmLogLeft.CurrentLog.CountFatal} Fatal"
        LabError.Text = $"{FrmLogLeft.CurrentLog.CountError} Error"
        LabWarn.Text = $"{FrmLogLeft.CurrentLog.CountWarn} Warn"
        LabInfo.Text = $"{FrmLogLeft.CurrentLog.CountInfo} Info"
        LabDebug.Text = $"{FrmLogLeft.CurrentLog.CountDebug} Debug"
    End Sub

    Public LabDebug As Run = Nothing
    Public LabInfo As Run = Nothing
    Public LabWarn As Run = Nothing
    Public LabError As Run = Nothing
    Public LabFatal As Run = Nothing

    Private Sub OnLogOutput(sender As Watcher, e As LogOutputEventArgs)
        RunInUi(Sub()
                    If FrmLogLeft.CurrentLog IsNot Nothing Then
                        If CheckAutoScroll.Checked Then PanBack.ScrollToBottom()
                        RefreshLabText()
                    End If
                End Sub)
    End Sub

#Region "卡片按钮"
    Private Sub BtnOperationClear_Click(sender As Object, e As RouteEventArgs) Handles BtnOperationClear.Click
        FrmLogLeft.FlowDocuments(FrmLogLeft.CurrentUuid).Blocks.Clear()
    End Sub

    Private Sub BtnOperationExport_Click(sender As Object, e As RouteEventArgs) Handles BtnOperationExport.Click
        'TODO(i18n): 文本 @ 文件选择弹窗 - 窗口标题 & 类型选择器选项
        Dim SavePath As String = SelectSaveFile("选择导出位置", $"游戏日志 - {FrmLogLeft.CurrentLog.Version.Name}.log", "游戏日志(*.log)|*.log")
        If SavePath.Length < 3 Then Exit Sub
        File.WriteAllLines(SavePath, FrmLogLeft.CurrentLog.FullLog)
        'TODO(i18n): 文本 @ 左下角提示 - 导出成功提示
        Hint("日志已导出！", HintType.Finish)
        OpenExplorer(SavePath)
    End Sub

    Private Sub BtnOperationKill_Click(sender As Object, e As RouteEventArgs) Handles BtnOperationKill.Click
        If FrmLogLeft.CurrentLog.State <= Watcher.MinecraftState.Running Then
            FrmLogLeft.CurrentLog.Kill()
            'TODO(i18n): 文本 @ 左下角提示 - 客户端关闭提示
            Hint($"已关闭游戏 {FrmLogLeft.CurrentLog.Version.Name}！", HintType.Finish)
        End If
    End Sub

    Private Sub BtnOperationExportStackDump_Click(sender As Object, e As RouteEventArgs) Handles BtnOperationExportStackDump.Click
        Dim SavePath As String = SelectSaveFile("选择导出位置", $"游戏运行栈 - {Date.Now.ToString("G").Replace("/", "-").Replace(":", ".").Replace(" ", "_")}.log", "游戏运行栈(*.log)|*.log")
        If SavePath.Length < 3 Then Exit Sub
        'TODO(i18n): 文本 @ 左下角提示 - 导出运行栈提示
        Hint("正在导出运行栈，请稍等（可能需要 15 秒 ~ 1 分钟）", HintType.Info)
        BtnOperationExportStackDump.IsEnabled = False
        RunInNewThread(Sub()
                           Dim Dump = FrmLogLeft.CurrentLog.ExportStackDump(SavePath)
                           File.WriteAllLines(SavePath, Dump)
                           RunInUi(Sub()
                                       'TODO(i18n): 文本 @ 左下角提示 - 导出运行栈提示
                                       Hint("运行栈已导出！", HintType.Finish)
                                       BtnOperationExportStackDump.IsEnabled = True
                                   End Sub)
                           OpenExplorer(SavePath)
                       End Sub)

    End Sub

    Private Sub OnGameExit()
        RunInUi(Sub() BtnOperationKill.IsEnabled = False)
        RunInUi(Sub() BtnOperationExportStackDump.IsEnabled = False)
    End Sub
#End Region

#Region "滑动条"
    Private Sub SliderMaxLog_ValueChanged(sender As MySlider, user As Boolean) Handles SliderMaxLog.Change
        Setup.Set(sender.Tag, sender.Value)
        If FrmSetupSystem Is Nothing Then Exit Sub
        FrmSetupSystem.SliderMaxLog.Value = sender.Value
    End Sub
#End Region

End Class
