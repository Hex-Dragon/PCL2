﻿Public Class MyCheckBox

    '基础

    Public Uuid As Integer = GetUuid()
    ''' <summary>
    ''' 复选框勾选状态改变。
    ''' </summary>
    ''' <param name="user">是否为用户手动改变的勾选状态。</param>
    Public Event Change(sender As Object, user As Boolean)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)
    Public Sub RaiseChange()
        RaiseEvent Change(Me, False)
    End Sub '使外部程序引发本控件的 Change 事件

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
    Private Const AnimationTimeOfCheck As Integer = 150 '勾选状态变更动画长度
    ''' <summary>
    ''' 手动设置 Checked 属性。
    ''' </summary>
    ''' <param name="value">新的 Checked 属性。</param>
    ''' <param name="user">是否由用户引发。</param>
    ''' <param name="anime">是否执行动画。</param>
    Public Sub SetChecked(value As Boolean, user As Boolean, anime As Boolean)
        Try
            If value = _Checked Then Exit Sub

            'Preview 事件

            If value AndAlso user Then
                Dim e = New RouteEventArgs(user)
                RaiseEvent PreviewChange(Me, e)
                If e.Handled Then
                    MouseDowned = True
                    Checkbox_MouseLeave()
                    MouseDowned = False
                    Exit Sub
                End If
            End If

            _Checked = value
            If IsLoaded Then RaiseEvent Change(Me, user)

            '更改动画

            If IsLoaded AndAlso AniControlEnabled = 0 AndAlso anime Then '防止默认属性变更触发动画
                AllowMouseDown = False
                If Checked Then
                    '由无变有
                    AniStart({
                          AaScale(ShapeBorder, 12 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent, , True),
                          AaScaleTransform(ShapeCheck, 1 - CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleX, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.7, New AniEaseOutBack(AniEasePower.Weak)),
                          AaScale(ShapeBorder, 6, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.7, New AniEaseOutBack, , True)
                     }, "MyCheckBox Scale " & Uuid)
                    AniStart({
                          AaColor(ShapeBorder, Border.BorderBrushProperty, If(IsEnabled, If(IsMouseOver, "ColorBrush3", "ColorBrush2"), "ColorBrushGray4"), AnimationTimeOfCheck)
                     }, "MyCheckBox BorderColor " & Uuid)
                    AniStart({
                          AaCode(Sub() AllowMouseDown = True, AnimationTimeOfCheck * 2)
                     }, "MyCheckBox AllowMouseDown " & Uuid)
                Else
                    '由有变无
                    AniStart({
                          AaScale(ShapeBorder, 12 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent, , True),
                          AaScaleTransform(ShapeCheck, -CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleX, AnimationTimeOfCheck * 0.9, , New AniEaseInFluent(AniEasePower.Weak)),
                          AaScale(ShapeBorder, 6, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.7, New AniEaseOutBack, , True)
                     }, "MyCheckBox Scale " & Uuid)
                    AniStart({
                          AaColor(ShapeBorder, Border.BorderBrushProperty, If(IsEnabled, If(IsMouseOver, "ColorBrush3", "ColorBrush1"), "ColorBrushGray4"), AnimationTimeOfCheck)
                     }, "MyCheckBox BorderColor " & Uuid)
                    AniStart({
                          AaCode(Sub() AllowMouseDown = True, AnimationTimeOfCheck * 2)
                     }, "MyCheckBox AllowMouseDown " & Uuid)
                End If
            Else
                '不使用动画
                AniStop("MyCheckBox Scale " & Uuid)
                AniStop("MyCheckBox BorderColor " & Uuid)
                AniStop("MyCheckBox AllowMouseDown " & Uuid)
                If Checked Then
                    CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleX = 1
                    CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleY = 1
                    ShapeBorder.SetResourceReference(Border.BorderBrushProperty, If(IsEnabled, "ColorBrush2", "ColorBrushGray4"))
                Else
                    CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleX = 0
                    CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleY = 0
                    ShapeBorder.SetResourceReference(Border.BorderBrushProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray4"))
                End If
            End If

        Catch ex As Exception
            Log(ex, "设置 Checked 失败")
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
    Public Shared ReadOnly TextProperty As DependencyProperty = DependencyProperty.Register("Text", GetType(String), GetType(MyCheckbox), New PropertyMetadata(New PropertyChangedCallback(
                                                                                                                                                               Sub(sender As DependencyObject, e As DependencyPropertyChangedEventArgs)
                                                                                                                                                                   If Not IsNothing(sender) Then CType(sender, MyCheckbox).LabText.Text = e.NewValue
                                                                                                                                                               End Sub)))

    '点击事件

    Private MouseDowned As Boolean = False
    Private AllowMouseDown As Boolean = True
    Private Sub Checkbox_MouseUp() Handles Me.MouseLeftButtonUp
        If Not MouseDowned Then Exit Sub
        Log("[Control] 按下复选框（" & (Not Checked).ToString & "）：" & Text)
        MouseDowned = False
        SetChecked(Not Checked, True, True)
        AniStart(AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " & Uuid)
    End Sub
    Private Sub Checkbox_MouseDown() Handles Me.MouseLeftButtonDown
        If Not AllowMouseDown Then Exit Sub
        MouseDowned = True
        Focus()
        AniStart(AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushBg1", 100), "MyCheckBox Background " & Uuid)
        If Checked Then
            AniStart({
                     AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, , New AniEaseOutFluent(AniEasePower.Strong), Absolute:=True),
                     AaScaleTransform(ShapeCheck, 0.9 - CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleX, 1000, , New AniEaseOutFluent(AniEasePower.Strong))
                 }, "MyCheckBox Scale " & Uuid)
        Else
            AniStart(AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, , New AniEaseOutFluent(AniEasePower.Strong), Absolute:=True), "MyCheckBox Scale " & Uuid)
        End If
    End Sub
    Private Sub Checkbox_MouseLeave() Handles Me.MouseLeave
        If Not MouseDowned Then Exit Sub
        MouseDowned = False
        AniStart(AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " & Uuid)
        If Checked Then
            AniStart({
                     AaScale(ShapeBorder, 18 - ShapeBorder.Width, 400, , New AniEaseOutFluent(AniEasePower.Strong), Absolute:=True),
                     AaScaleTransform(ShapeCheck, 1 - CType(ShapeCheck.RenderTransform, ScaleTransform).ScaleX, 500, , New AniEaseOutFluent(AniEasePower.Strong))
                 }, "MyCheckBox Scale " & Uuid)
        Else
            AniStart(AaScale(ShapeBorder, 18 - ShapeBorder.Width, 400, , New AniEaseOutFluent(AniEasePower.Strong), Absolute:=True), "MyCheckBox Scale " & Uuid)
        End If
    End Sub

    '指向动画

    Private Const AnimationTimeOfMouseIn As Integer = 100
    Private Const AnimationTimeOfMouseOut As Integer = 200
    Private Sub Checkbox_IsEnabledChanged() Handles Me.IsEnabledChanged
        If IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
            '有动画
            If IsEnabled Then
                '可用
                Checkbox_MouseLeaveAnimation()
            Else
                '不可用
                AniStart({
                         AaColor(ShapeBorder, Border.BorderBrushProperty, ColorGray4 - ShapeBorder.BorderBrush, AnimationTimeOfMouseOut)
                 }, "MyCheckBox BorderColor " & Uuid)
                AniStart({
                         AaColor(LabText, TextBlock.ForegroundProperty, ColorGray4 - LabText.Foreground, AnimationTimeOfMouseOut)
                 }, "MyCheckBox TextColor " & Uuid)
            End If
        Else
            '无动画
            AniStop("MyCheckBox TextColor " & Uuid)
            AniStop("MyCheckBox BorderColor " & Uuid)
            LabText.SetResourceReference(TextBlock.ForegroundProperty, If(Me.IsEnabled, "ColorBrush1", "ColorBrushGray4"))
            ShapeBorder.SetResourceReference(Border.BorderBrushProperty, If(Me.IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray4"))
        End If
    End Sub
    Private Sub Checkbox_MouseEnterAnimation() Handles Me.MouseEnter
        AniStart({
                 AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfMouseIn)
         }, "MyCheckBox TextColor " & Uuid)
        AniStart({
                 AaColor(ShapeBorder, Border.BorderBrushProperty, "ColorBrush3", AnimationTimeOfMouseIn)
         }, "MyCheckBox BorderColor " & Uuid)
    End Sub
    Private Sub Checkbox_MouseLeaveAnimation() Handles Me.MouseLeave
        If Not IsEnabled Then Exit Sub 'MouseLeave 比 IsEnabledChanged 后执行，所以如果自定义事件修改了 IsEnabled，将导致显示错误
        AniStart({
                 AaColor(LabText, TextBlock.ForegroundProperty, If(Me.IsEnabled, "ColorBrush1", "ColorBrushGray4"), AnimationTimeOfMouseOut)
         }, "MyCheckBox TextColor " & Uuid)
        AniStart({
                 AaColor(ShapeBorder, Border.BorderBrushProperty, If(Me.IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray4"), AnimationTimeOfMouseOut)
         }, "MyCheckBox BorderColor " & Uuid)
    End Sub

End Class
