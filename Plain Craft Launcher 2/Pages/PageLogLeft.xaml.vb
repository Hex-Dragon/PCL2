Public Class PageLogLeft
    Public ShownLogs As New List(Of KeyValuePair(Of Integer, Watcher))
    Public FlowDocuments As New Dictionary(Of Integer, FlowDocument)
    Public CurrentUuid As Integer
    Public CurrentLog As Watcher
    Public IsLoading As Integer = 0
    Private Sub PageLogLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Reload()
        FrmMain.BtnExtraLog.ShowRefresh()
    End Sub
    Private Sub PageLogLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        FrmMain.BtnExtraLog.ShowRefresh()
    End Sub
    Private Sub Reload()
        Try
            If ShownLogs.Count = 0 Then
                FrmMain.PageChange(FrmMain.PageCurrentSub)
                Return
            End If
            IsLoading += 1

            '创建 UI
            FrmLogLeft.PanList.Children.Clear()

            '测试核心列表
            'TODO(i18n): 文本 @ PageLog 左侧 - 列表标题
            FrmLogLeft.PanList.Children.Add(New TextBlock With {.Text = "测试版本列表", .Margin = New Thickness(13, 18, 5, 4), .Opacity = 0.6, .FontSize = 12})
            For Each item In ShownLogs
                '添加控件
                Dim Uuid As Integer = item.Key
                Dim Version As McVersion = item.Value.Version
                Dim Proc As Process = item.Value.GameProcess
                Dim NewItem As New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.RadioBox, .MinPaddingRight = 30, .Title = Version.Name, .Info = $"{Version.Version} - {Proc.StartTime:HH:mm:ss}", .Height = 40, .Tag = Uuid}
                AddHandler NewItem.Changed, AddressOf FrmLogLeft.Version_Change
                'Dim KillButton As New MyIconButton With {.Logo = Logo.IconButtonCross, .LogoScale = 0.85}
                Dim RemoveButton As New MyIconButton With {.Logo = Logo.IconButtonDelete, .LogoScale = 1.1}
                'AddHandler KillButton.Click, AddressOf FrmLogLeft.Kill_Click
                AddHandler RemoveButton.Click, AddressOf FrmLogLeft.Remove_Click
                NewItem.Buttons = {RemoveButton}
                If Uuid = CurrentUuid Then NewItem.Checked = True
                FrmLogLeft.PanList.Children.Add(NewItem)
            Next

            '通知日志保留设置
            'TODO(i18n): 文本 @ PageLog 左侧 - 日志保留设置通知
            If Not Setup.Get("HintMaxLog") Then
                Setup.Set("HintMaxLog", True)
                Hint("实时日志默认只保留 500 行，你可以在 实时日志行数 设置中修改！")
            End If
            IsLoading -= 1
        Catch ex As Exception
            Log(ex, "构建游戏实时日志 UI 出错", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub OnLogOutput(sender As Watcher, e As LogOutputEventArgs)
        For Each Item In ShownLogs
            If Item.Value.GameProcess.Id = sender.GameProcess.Id Then
                Dim Uuid As Integer = Item.Key
                Dim Margin As Thickness
                If Item.Value.GameProcess.HasExited Then
                    Margin = New Thickness(0, 12, 0, 0)
                Else
                    Margin = New Thickness(0)
                End If
                RunInUi(Sub()
                            Dim Paragraph As New Paragraph(New Run(e.LogText)) With {.Foreground = e.Color, .Margin = Margin}
                            FlowDocuments(Uuid).Blocks.Add(Paragraph)
                            Dim MaxLog As ULong = Setup.Get("SystemMaxLog")
                            Select Case MaxLog
                                Case Is <= 5
                                    MaxLog = MaxLog * 10 + 50
                                Case Is <= 13
                                    MaxLog = MaxLog * 50 - 150
                                Case Is <= 28
                                    MaxLog = MaxLog * 100 - 800
                                Case Else
                                    MaxLog = 18446744073709551615UL
                            End Select
                            While FlowDocuments(Uuid).Blocks.Count > MaxLog
                                FlowDocuments(Uuid).Blocks.Remove(FlowDocuments(Uuid).Blocks.FirstBlock)
                            End While
                        End Sub)
                Return
            End If
        Next
    End Sub
    Public Sub Add(watcher As Watcher)
        Dim uuid As Integer = GetUuid()
        ShownLogs.Add(New KeyValuePair(Of Integer, Watcher)(uuid, watcher))
        AddHandler watcher.LogOutput, AddressOf OnLogOutput
        RunInUi(Sub() FlowDocuments.Add(uuid, New FlowDocument)) 'TODO：在 UI 线程创建
        SelectionChange(uuid)
        FrmMain.BtnExtraLog.ShowRefresh()
    End Sub
    Public Sub SelectionChange(Uuid As Integer)
        If IsLoading > 0 Then Exit Sub
        'If CurrentUuid > 0 Then FlowDocuments(CurrentUuid) = FrmLogRight.PanLog.Document
        If Uuid <= 0 Then
            CurrentUuid = -1
            CurrentLog = Nothing
        Else
            For Each item In ShownLogs
                If item.Key = Uuid Then
                    CurrentUuid = Uuid
                    CurrentLog = item.Value
                    Exit For
                End If
            Next
        End If
        RunInUi(Sub()
                    FrmLogRight.Reload()
                    Reload()
                End Sub)
    End Sub
    Public Sub RemoveItem(Uuid As Integer)
        For i = 0 To ShownLogs.Count - 1
            Dim item = ShownLogs(i)
            If item.Key <> Uuid Then Continue For
            ShownLogs.RemoveAt(i)
            If CurrentUuid = item.Key Then
                If ShownLogs.Count = 0 Then
                    '没有可以显示的了
                    SelectionChange(-1)
                Else
                    SelectionChange(ShownLogs({{i, ShownLogs.Count - 1}.Min, 0}.Max).Key)
                End If
            Else
                RunInUi(Sub()
                            FrmLogRight.Reload()
                            Reload()
                        End Sub)
            End If
            Exit For
        Next
        FrmMain.BtnExtraLog.ShowRefresh()
    End Sub
    'Public Sub Kill_Click(sender As Object, e As RoutedEventArgs)
    '    Dim Uuid As Integer = (CType(CType(sender, MyIconButton).Parent, MyListItem).Tag)
    '    For Each item In ShownLogs
    '        If item.Key = Uuid Then
    '            item.Value.proc.Kill()
    '        End If
    '    Next
    'End Sub
    Public Sub Remove_Click(sender As Object, e As RoutedEventArgs)
        RemoveItem(CType(CType(sender, MyIconButton).Parent, MyListItem).Tag)
    End Sub

    '点击选项
    Public Sub Version_Change(sender As Object, e As RouteEventArgs)
        SelectionChange(CType(sender, MyListItem).Tag)
    End Sub

End Class
