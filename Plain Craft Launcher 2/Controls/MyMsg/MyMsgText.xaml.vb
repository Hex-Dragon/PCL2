Public Class MyMsgText

    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly Uuid As Integer = GetUuid()

    Public Sub New(Converter As MyMsgBoxConverter)
        Try

            InitializeComponent()
            Btn1.Name = Btn1.Name & GetUuid()
            Btn2.Name = Btn2.Name & GetUuid()
            Btn3.Name = Btn3.Name & GetUuid()
            MyConverter = Converter
            LabTitle.Text = Converter.Title
            LabCaption.Text = Converter.Content
            Btn1.Text = Converter.Button1
            If Converter.IsWarn Then
                Btn1.ColorType = MyButton.ColorState.Red
                LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight")
                ShapeLine.SetResourceReference(Rectangle.FillProperty, "ColorBrushRedLight")
            End If
            Btn2.Text = Converter.Button2
            Btn3.Text = Converter.Button3
            Btn2.Visibility = If(Converter.Button2 = "", Visibility.Collapsed, Visibility.Visible)
            Btn3.Visibility = If(Converter.Button3 = "", Visibility.Collapsed, Visibility.Visible)
            ShapeLine.StrokeThickness = GetWPFSize(1)

        Catch ex As Exception
            Log(ex, "普通弹窗初始化失败", LogLevel.Hint)
        End Try
    End Sub

    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Try

            'UI 初始化
            If Btn2.IsVisible AndAlso Not Btn1.ColorType = MyButton.ColorState.Red Then Btn1.ColorType = MyButton.ColorState.Highlight
            Measure(New Size(FrmMain.Width - 20, FrmMain.Height - 20))
            Arrange(New Rect(0, 0, FrmMain.Width - 20, FrmMain.Height - 20))
            Btn1.Focus()
            '动画
            AniStart({
                AaColor(FrmMain.PanMsg, BackgroundProperty, New MyColor(60, 0, 0, 0), 250, , New AniEaseInFluent),
                AaColor(PanBorder, Border.BackgroundProperty, New MyColor(255, 0, 0, 0), 150, , New AniEaseInFluent),
                AaOpacity(EffectShadow, 0.75, 400, 50),
                AaWidth(ShapeLine, ShapeLine.ActualWidth, 250, 100, New AniEaseOutFluent),
                AaOpacity(ShapeLine, 1, 200, 100),
                AaWidth(LabTitle, LabTitle.ActualWidth, 200, 150, New AniEaseOutFluent),
                AaOpacity(LabTitle, 1, 150, 150),
                AaOpacity(PanCaption, 1, 150, 150),
                AaOpacity(Btn1, 1, 150, 100),
                AaOpacity(Btn2, 1, 150, 150),
                AaOpacity(Btn3, 1, 150, 200),
                AaCode(Sub()
                           ShapeLine.MinWidth = ShapeLine.ActualWidth
                           ShapeLine.HorizontalAlignment = HorizontalAlignment.Stretch
                           ShapeLine.Width = Double.NaN
                           LabTitle.Width = Double.NaN
                           LabTitle.TextTrimming = TextTrimming.CharacterEllipsis
                       End Sub, 350)
            }, "MyMsgBox Start " & Uuid)
            '动画初始化
            ShapeLine.Width = 0
            ShapeLine.HorizontalAlignment = HorizontalAlignment.Center
            LabTitle.Width = 0
            LabTitle.Opacity = 0
            LabTitle.TextWrapping = TextWrapping.NoWrap
            PanCaption.Opacity = 0
            '记录日志
            Log("[Control] 普通弹窗：" & LabTitle.Text & vbCrLf & LabCaption.Text)

        Catch ex As Exception
            Log(ex, "普通弹窗加载失败", LogLevel.Hint)
        End Try
    End Sub
    Private Sub Close()
        '结束线程阻塞
        If MyConverter.ForceWait OrElse Not MyConverter.Button2 = "" Then MyConverter.WaitFrame.Continue = False
        Interop.ComponentDispatcher.PopModal()
        '弹窗动画
        LabTitle.TextTrimming = TextTrimming.None
        LabTitle.TextWrapping = TextWrapping.NoWrap
        AniStart({
            AaColor(FrmMain.PanMsg, Grid.BackgroundProperty, New MyColor(-60, 0, 0, 0), 350, , New AniEaseInFluent),
            AaColor(PanBorder, Border.BackgroundProperty, New MyColor(-255, 0, 0, 0), 300, 100, New AniEaseInFluent),
            AaOpacity(EffectShadow, -0.75, 150),
            AaWidth(ShapeLine, -ShapeLine.ActualWidth, 250, , New AniEaseInFluent(AniEasePower.Weak)),
            AaOpacity(ShapeLine, -1, 200),
            AaWidth(LabTitle, -LabTitle.ActualWidth, 250),
            AaOpacity(LabTitle, -1, 200),
            AaOpacity(PanCaption, -1, 200),
            AaOpacity(Btn1, -1, 150),
            AaOpacity(Btn2, -1, 150, 50),
            AaOpacity(Btn3, -1, 150, 100),
            AaCode(Sub() CType(Parent, Grid).Children.Remove(Me), , True)
        }, "MyMsgBox Close " & Uuid)
        '动画初始化
        ShapeLine.MinWidth = 0
    End Sub

    Public Sub Btn1_Click() Handles Btn1.Click
        If MyConverter.IsExited Then Exit Sub
        MyConverter.IsExited = True
        MyConverter.Result = 1
        Close()
    End Sub
    Private Sub Btn2_Click() Handles Btn2.Click
        If MyConverter.IsExited Then Exit Sub
        MyConverter.IsExited = True
        MyConverter.Result = 2
        Close()
    End Sub
    Private Sub Btn3_Click() Handles Btn3.Click
        If MyConverter.IsExited Then Exit Sub
        MyConverter.IsExited = True
        MyConverter.Result = 3
        Close()
    End Sub

    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        On Error Resume Next
        If e.GetPosition(ShapeLine).Y <= 2 Then FrmMain.DragMove()
    End Sub

End Class
