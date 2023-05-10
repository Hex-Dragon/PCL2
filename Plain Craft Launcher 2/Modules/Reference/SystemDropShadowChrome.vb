Public NotInheritable Class SystemDropShadowChrome
    Inherits Decorator

    '源码来源于：https://referencesource.microsoft.com/#PresentationFramework.Aero/parent/Shared/Microsoft/Windows/Themes/SystemDropShadowChrome.cs,6d9c27d92a8128c1

    Public Shared ReadOnly ColorProperty As DependencyProperty = DependencyProperty.Register("Color", GetType(Color), GetType(SystemDropShadowChrome), New FrameworkPropertyMetadata(Color.FromArgb(&H71, &H0, &H0, &H0), FrameworkPropertyMetadataOptions.AffectsRender, New PropertyChangedCallback(AddressOf ClearBrushes)))
    Public Property Color As Color
        Get
            Return CType(GetValue(ColorProperty), Color)
        End Get
        Set(value As Color)
            SetValue(ColorProperty, value)
        End Set
    End Property

    Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.Register("CornerRadius", GetType(CornerRadius), GetType(SystemDropShadowChrome), New FrameworkPropertyMetadata(New CornerRadius(), FrameworkPropertyMetadataOptions.AffectsRender, New PropertyChangedCallback(AddressOf ClearBrushes)), New ValidateValueCallback(AddressOf IsCornerRadiusValid))
    Public Property CornerRadius As CornerRadius
        Get
            Return CType(GetValue(CornerRadiusProperty), CornerRadius)
        End Get
        Set(value As CornerRadius)
            SetValue(CornerRadiusProperty, value)
        End Set
    End Property
    Private Shared Function IsCornerRadiusValid(value As Object) As Boolean
        Dim cr As CornerRadius = CType(value, CornerRadius)
        Return Not (cr.TopLeft < 0.0 OrElse cr.TopRight < 0.0 OrElse cr.BottomLeft < 0.0 OrElse cr.BottomRight < 0.0 OrElse Double.IsNaN(cr.TopLeft) OrElse Double.IsNaN(cr.TopRight) OrElse Double.IsNaN(cr.BottomLeft) OrElse Double.IsNaN(cr.BottomRight) OrElse Double.IsInfinity(cr.TopLeft) OrElse Double.IsInfinity(cr.TopRight) OrElse Double.IsInfinity(cr.BottomLeft) OrElse Double.IsInfinity(cr.BottomRight))
    End Function

    Private Const ShadowDepth As Double = 5

    Protected Overrides Sub OnRender(drawingContext As DrawingContext)
        Dim cornerRadius As CornerRadius = Me.CornerRadius
        Dim shadowBounds As New Rect(New Point(ShadowDepth, ShadowDepth), New Size(RenderSize.Width, RenderSize.Height))
        Dim color As Color = Me.Color

        If shadowBounds.Width > 0 AndAlso shadowBounds.Height > 0 AndAlso color.A > 0 Then
            Dim centerWidth As Double = shadowBounds.Right - shadowBounds.Left - 2 * ShadowDepth
            Dim centerHeight As Double = shadowBounds.Bottom - shadowBounds.Top - 2 * ShadowDepth
            Dim maxRadius As Double = Math.Min(centerWidth * 0.5, centerHeight * 0.5)
            cornerRadius.TopLeft = Math.Min(cornerRadius.TopLeft, maxRadius)
            cornerRadius.TopRight = Math.Min(cornerRadius.TopRight, maxRadius)
            cornerRadius.BottomLeft = Math.Min(cornerRadius.BottomLeft, maxRadius)
            cornerRadius.BottomRight = Math.Min(cornerRadius.BottomRight, maxRadius)
            Dim brushes As Brush() = GetBrushes(color, cornerRadius)
            Dim centerTop As Double = shadowBounds.Top + ShadowDepth
            Dim centerLeft As Double = shadowBounds.Left + ShadowDepth
            Dim centerRight As Double = shadowBounds.Right - ShadowDepth
            Dim centerBottom As Double = shadowBounds.Bottom - ShadowDepth
            Dim guidelineSetX As Double() = New Double() {centerLeft, centerLeft + cornerRadius.TopLeft, centerRight - cornerRadius.TopRight, centerLeft + cornerRadius.BottomLeft, centerRight - cornerRadius.BottomRight, centerRight}
            Dim guidelineSetY As Double() = New Double() {centerTop, centerTop + cornerRadius.TopLeft, centerTop + cornerRadius.TopRight, centerBottom - cornerRadius.BottomLeft, centerBottom - cornerRadius.BottomRight, centerBottom}
            drawingContext.PushGuidelineSet(New GuidelineSet(guidelineSetX, guidelineSetY))
            cornerRadius.TopLeft += ShadowDepth
            cornerRadius.TopRight += ShadowDepth
            cornerRadius.BottomLeft += ShadowDepth
            cornerRadius.BottomRight += ShadowDepth
            Dim topLeft As New Rect(shadowBounds.Left, shadowBounds.Top, cornerRadius.TopLeft, cornerRadius.TopLeft)
            drawingContext.DrawRectangle(brushes(Placement.TopLeft), Nothing, topLeft)
            Dim topWidth As Double = guidelineSetX(2) - guidelineSetX(1)

            If topWidth > 0 Then
                Dim top As New Rect(guidelineSetX(1), shadowBounds.Top, topWidth, ShadowDepth)
                drawingContext.DrawRectangle(brushes(Placement.Top), Nothing, top)
            End If

            Dim topRight As New Rect(guidelineSetX(2), shadowBounds.Top, cornerRadius.TopRight, cornerRadius.TopRight)
            drawingContext.DrawRectangle(brushes(Placement.TopRight), Nothing, topRight)
            Dim leftHeight As Double = guidelineSetY(3) - guidelineSetY(1)

            If leftHeight > 0 Then
                Dim left As New Rect(shadowBounds.Left, guidelineSetY(1), ShadowDepth, leftHeight)
                drawingContext.DrawRectangle(brushes(Placement.Left), Nothing, left)
            End If

            Dim rightHeight As Double = guidelineSetY(4) - guidelineSetY(2)

            If rightHeight > 0 Then
                Dim right As New Rect(guidelineSetX(5), guidelineSetY(2), ShadowDepth, rightHeight)
                drawingContext.DrawRectangle(brushes(Placement.Right), Nothing, right)
            End If

            Dim bottomLeft As New Rect(shadowBounds.Left, guidelineSetY(3), cornerRadius.BottomLeft, cornerRadius.BottomLeft)
            drawingContext.DrawRectangle(brushes(Placement.BottomLeft), Nothing, bottomLeft)
            Dim bottomWidth As Double = guidelineSetX(4) - guidelineSetX(3)

            If bottomWidth > 0 Then
                Dim bottom As New Rect(guidelineSetX(3), guidelineSetY(5), bottomWidth, ShadowDepth)
                drawingContext.DrawRectangle(brushes(Placement.Bottom), Nothing, bottom)
            End If

            Dim bottomRight As New Rect(guidelineSetX(4), guidelineSetY(4), cornerRadius.BottomRight, cornerRadius.BottomRight)
            drawingContext.DrawRectangle(brushes(Placement.BottomRight), Nothing, bottomRight)

            If cornerRadius.TopLeft = ShadowDepth AndAlso cornerRadius.TopLeft = cornerRadius.TopRight AndAlso cornerRadius.TopLeft = cornerRadius.BottomLeft AndAlso cornerRadius.TopLeft = cornerRadius.BottomRight Then
                Dim center As New Rect(guidelineSetX(0), guidelineSetY(0), centerWidth, centerHeight)
                drawingContext.DrawRectangle(brushes(Placement.Center), Nothing, center)
            Else
                Dim figure As New PathFigure()

                If cornerRadius.TopLeft > ShadowDepth Then
                    figure.StartPoint = New Point(guidelineSetX(1), guidelineSetY(0))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(1), guidelineSetY(1)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(0), guidelineSetY(1)), True))
                Else
                    figure.StartPoint = New Point(guidelineSetX(0), guidelineSetY(0))
                End If

                If cornerRadius.BottomLeft > ShadowDepth Then
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(0), guidelineSetY(3)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(3), guidelineSetY(3)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(3), guidelineSetY(5)), True))
                Else
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(0), guidelineSetY(5)), True))
                End If

                If cornerRadius.BottomRight > ShadowDepth Then
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(4), guidelineSetY(5)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(4), guidelineSetY(4)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(5), guidelineSetY(4)), True))
                Else
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(5), guidelineSetY(5)), True))
                End If

                If cornerRadius.TopRight > ShadowDepth Then
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(5), guidelineSetY(2)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(2), guidelineSetY(2)), True))
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(2), guidelineSetY(0)), True))
                Else
                    figure.Segments.Add(New LineSegment(New Point(guidelineSetX(5), guidelineSetY(0)), True))
                End If

                figure.IsClosed = True
                figure.Freeze()
                Dim geometry As New PathGeometry()
                geometry.Figures.Add(figure)
                geometry.Freeze()
                drawingContext.DrawGeometry(brushes(Placement.Center), Nothing, geometry)
            End If

            drawingContext.Pop()
        End If
    End Sub

    Private Enum Placement
        TopLeft = 0
        Top = 1
        TopRight = 2
        Left = 3
        Center = 4
        Right = 5
        BottomLeft = 6
        Bottom = 7
        BottomRight = 8
    End Enum
    Private Shared _commonBrushes As Brush()
    Private Shared _commonCornerRadius As CornerRadius
    Private Shared ReadOnly _resourceAccess As New Object()
    Private _brushes As Brush()

    Private Shared Sub ClearBrushes(o As DependencyObject, e As DependencyPropertyChangedEventArgs)
        CType(o, SystemDropShadowChrome)._brushes = Nothing
    End Sub
    Private Shared Function CreateStops(c As Color, cornerRadius As Double) As GradientStopCollection
        Dim gradientScale As Double = 1 / (cornerRadius + ShadowDepth)
        Dim gsc As New GradientStopCollection From {
            New GradientStop(c, (0.5 + cornerRadius) * gradientScale)
        }
        Dim stopColor As Color = c
        stopColor.A = CByte(0.74336 * c.A)
        gsc.Add(New GradientStop(stopColor, (1.5 + cornerRadius) * gradientScale))
        stopColor.A = CByte(0.38053 * c.A)
        gsc.Add(New GradientStop(stopColor, (2.5 + cornerRadius) * gradientScale))
        stopColor.A = CByte(0.12389 * c.A)
        gsc.Add(New GradientStop(stopColor, (3.5 + cornerRadius) * gradientScale))
        stopColor.A = CByte(0.02654 * c.A)
        gsc.Add(New GradientStop(stopColor, (4.5 + cornerRadius) * gradientScale))
        stopColor.A = 0
        gsc.Add(New GradientStop(stopColor, (5 + cornerRadius) * gradientScale))
        gsc.Freeze()
        Return gsc
    End Function
    Private Shared Function CreateBrushes(c As Color, cornerRadius As CornerRadius) As Brush()
        Dim brushes As Brush() = New Brush(8) {}
        brushes(Placement.Center) = New SolidColorBrush(c)
        brushes(Placement.Center).Freeze()
        Dim sideStops As GradientStopCollection = CreateStops(c, 0)
        Dim top As New LinearGradientBrush(sideStops, New Point(0, 1), New Point(0, 0))
        top.Freeze()
        brushes(Placement.Top) = top
        Dim left As New LinearGradientBrush(sideStops, New Point(1, 0), New Point(0, 0))
        left.Freeze()
        brushes(Placement.Left) = left
        Dim right As New LinearGradientBrush(sideStops, New Point(0, 0), New Point(1, 0))
        right.Freeze()
        brushes(Placement.Right) = right
        Dim bottom As New LinearGradientBrush(sideStops, New Point(0, 0), New Point(0, 1))
        bottom.Freeze()
        brushes(Placement.Bottom) = bottom
        Dim topLeftStops As GradientStopCollection

        If cornerRadius.TopLeft = 0 Then
            topLeftStops = sideStops
        Else
            topLeftStops = CreateStops(c, cornerRadius.TopLeft)
        End If

        Dim topLeft As New RadialGradientBrush(topLeftStops) With {
            .RadiusX = 1,
            .RadiusY = 1,
            .Center = New Point(1, 1),
            .GradientOrigin = New Point(1, 1)
        }
        topLeft.Freeze()
        brushes(Placement.TopLeft) = topLeft
        Dim topRightStops As GradientStopCollection

        If cornerRadius.TopRight = 0 Then
            topRightStops = sideStops
        ElseIf cornerRadius.TopRight = cornerRadius.TopLeft Then
            topRightStops = topLeftStops
        Else
            topRightStops = CreateStops(c, cornerRadius.TopRight)
        End If

        Dim topRight As New RadialGradientBrush(topRightStops) With {
            .RadiusX = 1,
            .RadiusY = 1,
            .Center = New Point(0, 1),
            .GradientOrigin = New Point(0, 1)
        }
        topRight.Freeze()
        brushes(Placement.TopRight) = topRight
        Dim bottomLeftStops As GradientStopCollection

        If cornerRadius.BottomLeft = 0 Then
            bottomLeftStops = sideStops
        ElseIf cornerRadius.BottomLeft = cornerRadius.TopLeft Then
            bottomLeftStops = topLeftStops
        ElseIf cornerRadius.BottomLeft = cornerRadius.TopRight Then
            bottomLeftStops = topRightStops
        Else
            bottomLeftStops = CreateStops(c, cornerRadius.BottomLeft)
        End If

        Dim bottomLeft As New RadialGradientBrush(bottomLeftStops) With {
            .RadiusX = 1,
            .RadiusY = 1,
            .Center = New Point(1, 0),
            .GradientOrigin = New Point(1, 0)
        }
        bottomLeft.Freeze()
        brushes(Placement.BottomLeft) = bottomLeft
        Dim bottomRightStops As GradientStopCollection

        If cornerRadius.BottomRight = 0 Then
            bottomRightStops = sideStops
        ElseIf cornerRadius.BottomRight = cornerRadius.TopLeft Then
            bottomRightStops = topLeftStops
        ElseIf cornerRadius.BottomRight = cornerRadius.TopRight Then
            bottomRightStops = topRightStops
        ElseIf cornerRadius.BottomRight = cornerRadius.BottomLeft Then
            bottomRightStops = bottomLeftStops
        Else
            bottomRightStops = CreateStops(c, cornerRadius.BottomRight)
        End If

        Dim bottomRight As New RadialGradientBrush(bottomRightStops) With {
            .RadiusX = 1,
            .RadiusY = 1,
            .Center = New Point(0, 0),
            .GradientOrigin = New Point(0, 0)
        }
        bottomRight.Freeze()
        brushes(Placement.BottomRight) = bottomRight
        Return brushes
    End Function
    Private Function GetBrushes(c As Color, cornerRadius As CornerRadius) As Brush()
        If _commonBrushes Is Nothing Then

            SyncLock _resourceAccess

                If _commonBrushes Is Nothing Then
                    _commonBrushes = CreateBrushes(c, cornerRadius)
                    _commonCornerRadius = cornerRadius
                End If
            End SyncLock
        End If

        If c = CType(_commonBrushes(Placement.Center), SolidColorBrush).Color AndAlso cornerRadius = _commonCornerRadius Then
            _brushes = Nothing
            Return _commonBrushes
        ElseIf _brushes Is Nothing Then
            _brushes = CreateBrushes(c, cornerRadius)
        End If

        Return _brushes
    End Function

End Class
