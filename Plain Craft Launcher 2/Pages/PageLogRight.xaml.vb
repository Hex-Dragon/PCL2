Public Class PageLogRight
    Public Sub Refresh()
        '初始化
        PanLog.Background = If(IsDarkMode, New MyColor(6, 20, 35), New MyColor(197, 209, 233))
        If FrmLogLeft.CurrentLog Is Nothing Then
            PanAllBack.Visibility = Visibility.Collapsed
            PanEmpty.Visibility = Visibility.Visible
            CardOperation.Visibility = Visibility.Collapsed
        Else
            PanAllBack.Visibility = Visibility.Visible
            PanEmpty.Visibility = Visibility.Collapsed
            CardOperation.Visibility = Visibility.Visible
            '绑定事件
            AddHandler FrmLogLeft.CurrentLog.LogOutput, AddressOf OnLogOutput
        End If
    End Sub
    Private Sub PageLogRight_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Refresh()
    End Sub
    Private Sub OnLogOutput(sender As McGameLog, e As LogOutputEventArgs)
        RunInUi(Sub()
                    Dim paragraph As New Paragraph(New Run(e.LogText)) With {
                        .Foreground = e.Color
                    }
                    PanLog.Document.Blocks.Add(paragraph)
                    If CheckAutoScroll.Checked Then PanBack.ScrollToBottom()
                    LabFatal.Text = FrmLogLeft.CurrentLog.CountFatal
                    LabError.Text = FrmLogLeft.CurrentLog.CountError
                    LabWarn.Text = FrmLogLeft.CurrentLog.CountWarn
                    LabInfo.Text = FrmLogLeft.CurrentLog.CountInfo
                    LabDebug.Text = FrmLogLeft.CurrentLog.CountDebug
                End Sub)
    End Sub
End Class
