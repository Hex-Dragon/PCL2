Public Class MyScrollViewer
    Inherits ScrollViewer
    Public Sub New()
        [AddHandler](MouseWheelEvent, New MouseWheelEventHandler(AddressOf MouseWheelEventSub), handledEventsToo:=True)
    End Sub

    Public Property DeltaMult As Double = 1


    Private RealOffset As Double
    Protected Overrides Sub OnMouseWheel(e As MouseWheelEventArgs)
        'ScrollViewer 会直接把事件移交 ScrollInfo 并 Handle 掉，在此覆盖掉它的行为
    End Sub
    Private Sub MouseWheelEventSub(sender As Object, e As MouseWheelEventArgs)
        If e.Handled Then
            Dim ShouldProcess As Boolean = False
            '特判：如果事件被处理但是鼠标处在一个没有垂直滚动栏的 FlowDocumentScrollViewer 上，依然处理该事件
            Dim Element = TryCast(e.Source, DependencyObject)
            While Element IsNot Nothing
                If TypeOf Element Is FlowDocumentScrollViewer Then
                    If CType(Element, FlowDocumentScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Hidden Then ShouldProcess = True
                    Exit While
                End If
                Element = If(TryCast(Element, FrameworkContentElement)?.Parent, TryCast(Element, FrameworkElement)?.Parent)
            End While
            If Not ShouldProcess Then Exit Sub
        End If
        e.Handled = True
        If e.Delta = 0 OrElse ActualHeight = 0 OrElse ScrollableHeight = 0 Then Exit Sub
        PerformVerticalOffsetDelta(-e.Delta)
        '关闭 Tooltip (#2552)
        For Each TooltipBorder In Application.ShowingTooltips
            AniStart(AaOpacity(TooltipBorder, -1, 100), $"Hide Tooltip {GetUuid()}")
        Next
    End Sub
    Public Sub PerformVerticalOffsetDelta(Delta As Double)
        AniStart(
            AaDouble(
            Sub(AnimDelta As Double)
                RealOffset = MathClamp(RealOffset + AnimDelta, 0, ExtentHeight - ActualHeight)
                ScrollToVerticalOffset(RealOffset)
            End Sub, Delta * DeltaMult, 300,, New AniEaseOutFluent(6)))
    End Sub
    Private Sub MyScrollViewer_ScrollChanged(sender As Object, e As ScrollChangedEventArgs) Handles Me.ScrollChanged
        RealOffset = VerticalOffset
        If FrmMain IsNot Nothing AndAlso (e.VerticalChange OrElse e.ViewportHeightChange) Then FrmMain.BtnExtraBack.ShowRefresh()
    End Sub
    Private Sub MyScrollViewer_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        FrmMain.BtnExtraBack.ShowRefresh()
    End Sub

    Public ScrollBar As MyScrollBar
    Private Sub Load() Handles Me.Loaded
        ScrollBar = GetTemplateChild("PART_VerticalScrollBar")
    End Sub

    Private Sub MyScrollViewer_PreviewGotKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs) Handles Me.PreviewGotKeyboardFocus
        If e.NewFocus IsNot Nothing AndAlso TypeOf e.NewFocus Is MySlider Then e.Handled = True '#3854，阻止获得焦点时自动滚动
    End Sub
End Class
