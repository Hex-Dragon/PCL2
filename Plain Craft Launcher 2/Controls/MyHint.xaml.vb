Public Class MyHint

    Public Uuid As Integer = GetUuid()

    Private _IsWarn As Boolean = True
    Public Property IsWarn As Boolean
        Get
            Return _IsWarn
        End Get
        Set(value As Boolean)
            If _IsWarn = value Then Exit Property
            _IsWarn = value
            If _IsWarn Then
                BorderBrush = New MyColor("#99FF4444")
                Gradient1.Color = New MyColor("#88FFBBBB")
                Gradient2.Color = New MyColor("#88FF8888")
                Path.Fill = New MyColor("#BF0000")
                LabText.Foreground = New MyColor("#BF0000")
                BtnClose.Foreground = New MyColor("#BF0000")
                Path.Data = (New GeometryConverter).ConvertFromString("F1 M 58.5832,55.4172L 17.4169,55.4171C 15.5619,53.5621 15.5619,50.5546 17.4168,48.6996L 35.201,15.8402C 37.056,13.9852 40.0635,13.9852 41.9185,15.8402L 58.5832,48.6997C 60.4382,50.5546 60.4382,53.5622 58.5832,55.4172 Z M 34.0417,25.7292L 36.0208,41.9584L 39.9791,41.9583L 41.9583,25.7292L 34.0417,25.7292 Z M 38,44.3333C 36.2511,44.3333 34.8333,45.7511 34.8333,47.5C 34.8333,49.2489 36.2511,50.6667 38,50.6667C 39.7489,50.6667 41.1666,49.2489 41.1666,47.5C 41.1666,45.7511 39.7489,44.3333 38,44.3333 Z ")
            Else
                BorderBrush = New MyColor("#994D76FF")
                Gradient1.Color = New MyColor("#88B0D0FF")
                Gradient2.Color = New MyColor("#889EBAFF")
                Path.Fill = New MyColor("#0062BF")
                LabText.Foreground = New MyColor("#0062BF")
                BtnClose.Foreground = New MyColor("#0062BF")
                Path.Data = (New GeometryConverter).ConvertFromString("F1M38,19C48.4934,19 57,27.5066 57,38 57,48.4934 48.4934,57 38,57 27.5066,57 19,48.4934 19,38 19,27.5066 27.5066,19 38,19z M33.25,33.25L33.25,36.4167 36.4166,36.4167 36.4166,47.5 33.25,47.5 33.25,50.6667 44.3333,50.6667 44.3333,47.5 41.1666,47.5 41.1666,36.4167 41.1666,33.25 33.25,33.25z M38.7917,25.3333C37.48,25.3333 36.4167,26.3967 36.4167,27.7083 36.4167,29.02 37.48,30.0833 38.7917,30.0833 40.1033,30.0833 41.1667,29.02 41.1667,27.7083 41.1667,26.3967 40.1033,25.3333 38.7917,25.3333z")
            End If
        End Set
    End Property

    Public Property Text As String
        Get
            Return LabText.Text
        End Get
        Set(ByVal value As String)
            LabText.Text = value
        End Set
    End Property

    Public Property CanClose As Boolean
        Get
            Return BtnClose.Visibility = Visibility.Visible
        End Get
        Set(ByVal value As Boolean)
            BtnClose.Visibility = If(value, Visibility.Visible, Visibility.Collapsed)
        End Set
    End Property

    Public RelativeSetup As String = ""

    Private Sub MyHint_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If CanClose AndAlso Setup.Get(RelativeSetup) Then
            Visibility = Visibility.Collapsed
        End If
    End Sub
    Private Sub BtnClose_Click(sender As Object, e As EventArgs) Handles BtnClose.Click
        Setup.Set(RelativeSetup, True)
        AniDispose(Me, False)
    End Sub

End Class
Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyHint, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        If Not Control.IsHitTestVisible Then Exit Sub
        Control.IsHitTestVisible = False
        AniStart({
                     AaScaleTransform(Control, -0.08, 200,, New AniEaseInFluent),
                     AaOpacity(Control, -1, 200,, New AniEaseOutFluent),
                     AaHeight(Control, -Control.ActualHeight, 150, 100, New AniEaseOutFluent),
                     AaCode(Sub()
                                If RemoveFromChildren Then
                                    CType(Control.Parent, Object).Children.Remove(Control)
                                Else
                                    Control.Visibility = Visibility.Collapsed
                                End If
                                If CallBack IsNot Nothing Then CallBack(Control)
                            End Sub,, True)
            }, "MyCard Dispose " & Control.Uuid)
    End Sub
End Module
