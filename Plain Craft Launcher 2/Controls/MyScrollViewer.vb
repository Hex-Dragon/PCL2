Public Class MyScrollViewer
    Inherits ScrollViewer

    Public Property DeltaMuity As Double = 1
    Private RealOffset As Double
    Private Sub MyScrollViewer_PreviewMouseWheel(sender As Object, e As MouseWheelEventArgs) Handles Me.PreviewMouseWheel
        If e.Delta = 0 OrElse ActualHeight = 0 OrElse ScrollableHeight = 0 Then Exit Sub
        Dim SourceTypeName As String = e.Source.GetType.Name
        If Content.TemplatedParent Is Nothing AndAlso (SourceTypeName.Contains("Combo") OrElse SourceTypeName = "MyTextBox") Then Exit Sub '确定可视树，防止滚动问题
        PerformVerticalOffsetDelta(-e.Delta)
        e.Handled = True
    End Sub
    Public Sub PerformVerticalOffsetDelta(Delta As Double)
        AniStart(AaDouble(Sub(AnimDelta As Double)
                              RealOffset = MathRange(RealOffset + AnimDelta, 0, ExtentHeight - ActualHeight)
                              ScrollToVerticalOffset(RealOffset)
                          End Sub, Delta * DeltaMuity, 300,, New AniEaseOutFluent(6)))
    End Sub
    Private Sub MyScrollViewer_ScrollChanged(sender As Object, e As ScrollChangedEventArgs) Handles Me.ScrollChanged
        RealOffset = VerticalOffset
        If FrmMain IsNot Nothing AndAlso (e.VerticalChange OrElse e.ViewportHeightChange) Then FrmMain.BtnExtraBack.ShowRefresh()
    End Sub
    Private Sub MyScrollViewer_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
        FrmMain.BtnExtraBack.ShowRefresh()
    End Sub

End Class
