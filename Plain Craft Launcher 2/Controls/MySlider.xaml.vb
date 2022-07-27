Public Class MySlider

    '基础

    Public Uuid As Integer = GetUuid()
    Public Event Change(sender As Object, user As Boolean)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)

    '自定义属性

    Private _MaxValue As Integer = 100
    Public Property MaxValue As Integer
        Get
            Return _MaxValue
        End Get
        Set(value As Integer)
            If value = _MaxValue Then Exit Property
            _MaxValue = value
            RefreshWidth(Nothing, Nothing)
        End Set
    End Property
    Private ChangeByKey As Boolean = False
    Private _Value As Integer = 0
    Public Property Value As Integer
        Get
            Return _Value
        End Get
        Set(newValue As Integer)
            Try

                newValue = MathRange(newValue, 0, MaxValue)
                If _Value = newValue Then Exit Property

                '触发 Preview 事件，修改新值
                Dim OldValue = _Value
                _Value = newValue
                If AniControlEnabled = 0 Then
                    Dim e = New RouteEventArgs(False)
                    RaiseEvent PreviewChange(Me, e)
                    If e.Handled Then
                        _Value = OldValue
                        DragStop()
                        Exit Property
                    End If
                End If

                If IsLoaded AndAlso AniControlEnabled = 0 Then
                    If ActualWidth < 16 Then Exit Property
                    Dim NewWidth As Double = _Value / MaxValue * (ActualWidth - 16)
                    Dim DeltaProcess As Double = Math.Abs(LineFore.Width / (ActualWidth - 16) - _Value / MaxValue)
                    Dim Time As Double = (1 - Math.Pow(1 - DeltaProcess, 3)) * 300 + If(ChangeByKey, 100, 0)
                    AniStart({
                            AaWidth(LineFore, Math.Max(0, NewWidth + If(NewWidth < 0.5, 0, 0.5)) - LineFore.Width, Time,, If(Time > 50, New AniEaseOutFluent, New AniEaseLinear)),
                            AaWidth(LineBack, Math.Max(0, ActualWidth - 16 - NewWidth + If(ActualWidth - 16 - NewWidth < 0.5, 0, 0.5)) - LineBack.Width, Time,, If(Time > 50, New AniEaseOutFluent, New AniEaseLinear)),
                            AaX(ShapeDot, NewWidth - ShapeDot.Margin.Left, Time,, If(Time > 50, New AniEaseOutFluent, New AniEaseLinear))
                         }, "MySlider Progress " & Uuid)
                Else
                    RefreshWidth(Nothing, Nothing)
                End If
                If AniControlEnabled = 0 Then RaiseEvent Change(Me, False)

            Catch ex As Exception
                Log(ex, "滑动条进度改变出错", LogLevel.Hint)
            End Try
        End Set
    End Property
    Private Sub RefreshWidth(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        If Not IsNothing(e) Then PanMain.Width = e.NewSize.Width
        AniStop("MySlider Progress " & Uuid)
        Dim NewWidth As Double = _Value / MaxValue * (ActualWidth - 16)
        LineFore.Width = Math.Max(0, NewWidth + If(NewWidth < 0.5, 0, 0.5))
        LineBack.Width = Math.Max(0, ActualWidth - 16 - NewWidth + If(ActualWidth - 16 - NewWidth < 0.5, 0, 0.5))
        SetLeft(ShapeDot, NewWidth)
    End Sub

    '拖动

    Public GetHintText As [Delegate]
    Private Sub DragStart(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        e.Handled = True '防止 ScrollViewer 失焦问题
        DragControl = Me
        FrmMain.DragDoing()
        AniStart({
                 AaScaleTransform(ShapeDot, 0.8 - CType(ShapeDot.RenderTransform, ScaleTransform).ScaleX, 75,, New AniEaseOutFluent)
            }, "MySlider Scale " & Uuid)
        '更新 Popup
        If GetHintText IsNot Nothing Then
            TextHint.Text = GetHintText.DynamicInvoke(Value)
            Popup.IsOpen = True
            AniStop("MySlider KeyPopup " & Uuid)
        End If
    End Sub
    Public Sub DragDoing()
        Dim Percent As Double = MathRange((Mouse.GetPosition(PanMain).X - 8) / (ActualWidth - 16), 0, 1)
        Dim NewValue As Integer = Percent * MaxValue
        If Not NewValue = Value Then
            Value = NewValue
        End If
        '更新 Popup
        If GetHintText IsNot Nothing Then TextHint.Text = GetHintText.DynamicInvoke(NewValue)
    End Sub
    Public Sub DragStop()
        RefreshColor()
        AniStart({
                 AaScaleTransform(ShapeDot, 1 - CType(ShapeDot.RenderTransform, ScaleTransform).ScaleX, 200,, New AniEaseOutFluent)
            }, "MySlider Scale " & Uuid)
        If GetHintText IsNot Nothing Then Popup.IsOpen = False
    End Sub

    '指向动画

    Private Sub RefreshColor() Handles Me.IsEnabledChanged, Me.MouseEnter, Me.MouseLeave
        Try

            '判断当前颜色
            Dim ColorName As String
            Dim AnimationTime As Integer
            If IsEnabled Then
                If IsMouseOver OrElse (Not IsNothing(DragControl) AndAlso DragControl.Equals(Me)) Then
                    ColorName = "ColorBrush3"
                    AnimationTime = 100
                Else
                    ColorName = "ColorBrush1"
                    AnimationTime = 200
                End If
            Else
                ColorName = "ColorBrushGray4"
                AnimationTime = 200
            End If
            '触发颜色动画
            If IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
                '有动画
                AniStart({AaColor(Me, BorderBrushProperty, ColorName, AnimationTime)}, "MySlider Color " & Uuid)
            Else
                '无动画
                AniStop("MySlider Color " & Uuid)
                SetResourceReference(BorderBrushProperty, ColorName)
            End If

        Catch ex As Exception
            Log(ex, "滑动条颜色改变出错")
        End Try
    End Sub

    '按键改变

    Public Property ValueByKey As UInteger = 1
    Private Sub MySlider_MouseEnter() Handles Me.MouseEnter
        Focus() '确保按键能改变值
    End Sub
    Private Sub MySlider_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        '拒绝一边拖动一边用按键改变
        If ReferenceEquals(Me, DragControl) Then Exit Sub
        '改变值
        If e.Key = Key.Left Then
            ChangeByKey = True
            Value -= ValueByKey
            ChangeByKey = False
            e.Handled = True
        ElseIf e.Key = Key.Right Then
            ChangeByKey = True
            Value += ValueByKey
            ChangeByKey = False
            e.Handled = True
        Else
            Exit Sub
        End If
        '更新 Popup
        If GetHintText IsNot Nothing Then
            TextHint.Text = GetHintText.DynamicInvoke(Value)
            Popup.IsOpen = True
            AniStop("MySlider KeyPopup " & Uuid)
            AniStart(AaCode(Sub() Popup.IsOpen = False, 700 * AniSpeed), "MySlider KeyPopup " & Uuid)
        End If
    End Sub

End Class