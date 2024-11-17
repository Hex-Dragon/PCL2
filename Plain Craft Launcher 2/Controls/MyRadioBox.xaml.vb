Public Class MyRadioBox
    Implements IMyRadio

    '基础

    Public Uuid As Integer = GetUuid()
    Public Event PreviewCheck(sender As Object, e As RouteEventArgs)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)
    Public Event Check(sender As Object, e As RouteEventArgs) Implements IMyRadio.Check
    Public Event Changed(sender As Object, e As RouteEventArgs) Implements IMyRadio.Changed

    '自定义属性

    Private _Checked As Boolean = False '是否选中
    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            SetChecked(value, False, True)
        End Set
    End Property
    ''' <summary>
    ''' 手动设置 Checked 属性。
    ''' </summary>
    ''' <param name="value">新的 Checked 属性。</param>
    ''' <param name="user">是否由用户引发。</param>
    ''' <param name="anime">是否执行动画。</param>
    Public Sub SetChecked(value As Boolean, user As Boolean, anime As Boolean)
        Try

            'Preview 事件

            If value AndAlso user Then
                Dim e = New RouteEventArgs(user)
                RaiseEvent PreviewCheck(Me, e)
                If e.Handled Then
                    Radiobox_MouseLeave()
                    Exit Sub
                End If
            End If

            '自定义属性基础

            Dim IsChanged As Boolean = False
            If IsLoaded AndAlso Not value = _Checked Then RaiseEvent PreviewChange(Me, New RouteEventArgs(user))
            If Not value = _Checked Then
                _Checked = value
                IsChanged = True
            End If

            '保证只有一个单选框选中

            If Parent Is Nothing Then Exit Sub
            Dim RadioboxList As New List(Of MyRadioBox)
            Dim CheckedCount As Integer = 0
            '收集控件列表与选中个数
            For Each Control In CType(Parent, Object).Children
                If TypeOf Control Is MyRadioBox Then
                    RadioboxList.Add(Control)
                    If Control.Checked Then CheckedCount += 1
                End If
            Next
            '判断选中情况
            Select Case CheckedCount
                Case 0
                    '没有任何单选框被选中，选择第一个
                    RadioboxList(0).Checked = True
                Case Is > 1
                    '选中项目多于 1 个
                    If Checked Then
                        '如果本控件选中，则取消其他所有控件的选中
                        For Each Control As MyRadioBox In RadioboxList
                            If Control.Checked AndAlso Not Control.Equals(Me) Then Control.Checked = False
                        Next
                    Else
                        '如果本控件未选中，则只保留第一个选中的控件
                        Dim FirstChecked = False
                        For Each Control As MyRadioBox In RadioboxList
                            If Control.Checked Then
                                If FirstChecked Then
                                    Control.Checked = False '修改 Checked 会自动触发 Change 事件，所以不用额外触发
                                Else
                                    FirstChecked = True
                                End If
                            End If
                        Next
                    End If
            End Select
            'End If

            '更改动画

            If Not IsChanged Then Exit Sub
            If IsLoaded AndAlso AniControlEnabled = 0 AndAlso anime Then '防止默认属性变更触发动画
                If Checked Then
                    '由无变有
                    If ShapeDot.Opacity < 0.01 Then ShapeDot.Opacity = 1
                    AniStart({
                              AaScale(ShapeBorder, 10 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent(AniEasePower.Weak), , True),
                              AaScale(ShapeBorder, 8, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.6, New AniEaseOutBack, , True)
                         }, "MyRadioBox Border " & Uuid)
                    AniStart({
                              AaScale(ShapeDot, 9 - ShapeDot.Width, AnimationTimeOfCheck * 2.6,, New AniEaseOutBack(AniEasePower.Weak), , True),
                              AaOpacity(ShapeDot, 1 - ShapeDot.Opacity, AnimationTimeOfCheck * 0.5, AnimationTimeOfCheck * 0.6)
                         }, "MyRadioBox Dot " & Uuid)
                    AniStart({
                              AaColor(ShapeBorder, Ellipse.StrokeProperty, If(IsMouseOver, "ColorBrush3", If(IsEnabled, "ColorBrush2", "ColorBrushGray4")), AnimationTimeOfCheck)
                         }, "MyRadioBox BorderColor " & Uuid)
                Else
                    '由有变无
                    AniStart({
                              AaScale(ShapeBorder, 18 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent, , True)
                         }, "MyRadioBox Border " & Uuid)
                    AniStart({
                              AaScale(ShapeDot, -ShapeDot.Width, AnimationTimeOfCheck, , New AniEaseInFluent, , True),
                              AaOpacity(ShapeDot, -ShapeDot.Opacity, AnimationTimeOfCheck * 0.5, AnimationTimeOfCheck * 0.2)
                         }, "MyRadioBox Dot " & Uuid)
                    AniStart({
                              AaColor(ShapeBorder, Ellipse.StrokeProperty, If(IsMouseOver, "ColorBrush3", If(IsEnabled, "ColorBrush1", "ColorBrushGray4")), AnimationTimeOfCheck)
                         }, "MyRadioBox BorderColor " & Uuid)
                End If
            Else
                '不使用动画
                AniStop("MyRadioBox Border " & Uuid)
                AniStop("MyRadioBox Dot " & Uuid)
                AniStop("MyRadioBox BorderColor " & Uuid)
                If Checked Then
                    ShapeDot.Width = 9
                    ShapeDot.Height = 9
                    ShapeDot.Opacity = 1
                    ShapeDot.Margin = New Thickness(5.5, 0, 0, 0)
                    ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, If(IsEnabled, "ColorBrush2", "ColorBrushGray4"))
                Else
                    ShapeDot.Width = 0
                    ShapeDot.Height = 0
                    ShapeDot.Opacity = 0
                    ShapeDot.Margin = New Thickness(10, 0, 0, 0)
                    ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray4"))
                End If
            End If

            '触发事件
            If Checked Then RaiseEvent Check(Me, New RouteEventArgs(user))
            RaiseEvent Changed(Me, New RouteEventArgs(user))

        Catch ex As Exception
            Log(ex, "单选框勾选改变错误", LogLevel.Hint)
        End Try
    End Sub
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property '内容
    Public Shared ReadOnly TextProperty As DependencyProperty = DependencyProperty.Register("Text", GetType(String), GetType(MyRadioBox), New PropertyMetadata(New PropertyChangedCallback(
    Sub(sender, e) If sender IsNot Nothing Then CType(sender, MyRadioBox).LabText.Text = e.NewValue)))

    '点击事件

    Private MouseDowned As Boolean = False
    Private AllowMouseDown As Boolean = True
    Private Sub Radiobox_MouseUp() Handles Me.MouseLeftButtonUp
        If Not MouseDowned Then Exit Sub
        MouseDowned = False
        Log("[Control] 按下单选框：" & Text)
        SetChecked(True, True, True)
        AniStart(AaColor(ShapeBorder, Ellipse.FillProperty, "ColorBrushHalfWhite", 100), "MyRadioBox Background " & Uuid)
    End Sub
    Private Sub Radiobox_MouseDown() Handles Me.MouseLeftButtonDown
        MouseDowned = True
        Focus()
        AniStart(AaColor(ShapeBorder, Ellipse.FillProperty, "ColorBrushBg1", 100), "MyRadioBox Background " & Uuid)
        If Not Checked Then
            AniStart(AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, , New AniEaseOutFluent(AniEasePower.Strong), Absolute:=True), "MyRadioBox Border " & Uuid)
        End If
    End Sub
    Private Sub Radiobox_MouseLeave() Handles Me.MouseLeave
        If Not MouseDowned Then Exit Sub
        MouseDowned = False
        AniStart(AaColor(ShapeBorder, Ellipse.FillProperty, "ColorBrushHalfWhite", 100), "MyRadioBox Background " & Uuid)
        If Not Checked Then
            AniStart(AaScale(ShapeBorder, 18 - ShapeBorder.Width, 400, , New AniEaseOutFluent(AniEasePower.Strong), Absolute:=True), "MyRadioBox Border " & Uuid)
        End If
    End Sub

    '指向动画

    Private Const AnimationTimeOfMouseIn As Integer = 100 '鼠标指向动画长度
    Private Const AnimationTimeOfMouseOut As Integer = 200 '鼠标指向动画长度
    Private Const AnimationTimeOfCheck As Integer = 150 '勾选状态变更动画长度
    Private Sub Radiobox_IsEnabledChanged() Handles Me.IsEnabledChanged
        If Me.IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
            '有动画
            If Me.IsEnabled Then
                '可用
                Radiobox_MouseLeaveAnimation()
            Else
                '不可用
                AniStart(AaColor(ShapeBorder, Ellipse.StrokeProperty, ColorGray4 - ShapeBorder.Stroke, AnimationTimeOfMouseOut), "MyRadioBox BorderColor " & Uuid)
                AniStart(AaColor(LabText, TextBlock.ForegroundProperty, ColorGray4 - LabText.Foreground, AnimationTimeOfMouseOut), "MyRadioBox TextColor " & Uuid)
            End If
        Else
            '无动画
            AniStop("MyRadioBox BorderColor " & Uuid)
            AniStop("MyRadioBox TextColor " & Uuid)
            LabText.SetResourceReference(TextBlock.ForegroundProperty, If(Me.IsEnabled, "ColorBrush1", "ColorBrushGray4"))
            ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, If(Me.IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray4"))
        End If
    End Sub
    Private Sub Radiobox_MouseEnterAnimation() Handles Me.MouseEnter
        AniStart(AaColor(ShapeBorder, Ellipse.StrokeProperty, "ColorBrush3", AnimationTimeOfMouseIn), "MyRadioBox BorderColor " & Uuid)
        AniStart(AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfMouseIn), "MyRadioBox TextColor " & Uuid)
    End Sub
    Private Sub Radiobox_MouseLeaveAnimation() Handles Me.MouseLeave
        If Not Me.IsEnabled Then Exit Sub 'MouseLeave 比 IsEnabledChanged 后执行，所以如果自定义事件修改了 IsEnabled，将导致显示错误
        If IsLoaded AndAlso AniControlEnabled = 0 Then
            AniStart(AaColor(ShapeBorder, Ellipse.StrokeProperty, If(IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray4"), AnimationTimeOfMouseOut), "MyRadioBox BorderColor " & Uuid)
            AniStart(AaColor(LabText, TextBlock.ForegroundProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray4"), AnimationTimeOfMouseOut), "MyRadioBox TextColor " & Uuid)
        Else
            AniStop("MyRadioBox BorderColor " & Uuid)
            AniStop("MyRadioBox TextColor " & Uuid)
            ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, If(IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray4"))
            LabText.SetResourceReference(TextBlock.ForegroundProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray4"))
        End If
    End Sub

End Class
