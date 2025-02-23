Imports System.Windows.Markup

<ContentProperty("Inlines")>
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
            SetStyle()
        End Set
    End Property

    Public Enum HintType
        Note
        Warning
        Caution
    End Enum
    Private _Type As HintType = HintType.Note
    Public Property Type As HintType
        Get
            Return _Type
        End Get
        Set(value As HintType)
            _Type = value
            SetStyle()
        End Set
    End Property

    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return LabText.Inlines
        End Get
    End Property
    Public Property Text As String
        Get
            Return LabText.Text
        End Get
        Set(value As String)
            LabText.Text = value
        End Set
    End Property

    Public Property CanClose As Boolean
        Get
            Return BtnClose.Visibility = Visibility.Visible
        End Get
        Set(value As Boolean)
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

    '触发点击事件
    Private IsMouseDown As Boolean = False
    Private Sub MyHint_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonUp
        If Not IsMouseDown Then Return
        IsMouseDown = False
        Log("[Control] 按下提示条" & If(String.IsNullOrEmpty(Name), "", "：" & Name))
        e.Handled = True
        ModEvent.TryStartEvent(EventType, EventData)
    End Sub
    Private Sub MyHint_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        IsMouseDown = True
    End Sub
    Private Sub MyHint_MouseLeave() Handles Me.MouseLeave
        IsMouseDown = False
    End Sub
    Public Property EventType As String
        Get
            Return GetValue(EventTypeProperty)
        End Get
        Set(value As String)
            SetValue(EventTypeProperty, value)
        End Set
    End Property
    Public Shared ReadOnly EventTypeProperty As DependencyProperty = DependencyProperty.Register("EventType", GetType(String), GetType(MyHint), New PropertyMetadata(Nothing))
    Public Property EventData As String
        Get
            Return GetValue(EventDataProperty)
        End Get
        Set(value As String)
            SetValue(EventDataProperty, value)
        End Set
    End Property
    Public Shared ReadOnly EventDataProperty As DependencyProperty = DependencyProperty.Register("EventData", GetType(String), GetType(MyHint), New PropertyMetadata(Nothing))

    Private Sub _ThemeChanged(sender As Object, e As Boolean)
        SetStyle()
    End Sub

    Public Sub New()
        InitializeComponent()
        SetStyle()
        AddHandler ModSecret.ThemeChanged, AddressOf _ThemeChanged

    End Sub

    Private Sub SetStyle()
        If Type = HintType.Note Then
            If IsWarn Then
                BorderBrush = New MyColor("#CCFF4444")
                Gradient1.Color = New MyColor(CType(If(IsDarkMode, "#BBFF8888", "#BBFFBBBB"), String))
                Gradient2.Color = New MyColor(CType(If(IsDarkMode, "#BBFF6666", "#BBFF8888"), String))
                Path.Fill = New MyColor("#BF0000")
                LabText.Foreground = New MyColor("#BF0000")
                BtnClose.Foreground = New MyColor("#BF0000")
                Path.Data = (New GeometryConverter).ConvertFromString("F1 M 58.5832,55.4172L 17.4169,55.4171C 15.5619,53.5621 15.5619,50.5546 17.4168,48.6996L 35.201,15.8402C 37.056,13.9852 40.0635,13.9852 41.9185,15.8402L 58.5832,48.6997C 60.4382,50.5546 60.4382,53.5622 58.5832,55.4172 Z M 34.0417,25.7292L 36.0208,41.9584L 39.9791,41.9583L 41.9583,25.7292L 34.0417,25.7292 Z M 38,44.3333C 36.2511,44.3333 34.8333,45.7511 34.8333,47.5C 34.8333,49.2489 36.2511,50.6667 38,50.6667C 39.7489,50.6667 41.1666,49.2489 41.1666,47.5C 41.1666,45.7511 39.7489,44.3333 38,44.3333 Z ")
                Return
            Else
                BorderBrush = New MyColor("#CC4D76FF")
                Gradient1.Color = New MyColor("#BBB0D0FF")
                Gradient2.Color = New MyColor("#BB9EBAFF")
                Path.Fill = New MyColor("#0062BF")
                LabText.Foreground = New MyColor("#0062BF")
                BtnClose.Foreground = New MyColor("#0062BF")
                Path.Data = (New GeometryConverter).ConvertFromString("F1M38,19C48.4934,19 57,27.5066 57,38 57,48.4934 48.4934,57 38,57 27.5066,57 19,48.4934 19,38 19,27.5066 27.5066,19 38,19z M33.25,33.25L33.25,36.4167 36.4166,36.4167 36.4166,47.5 33.25,47.5 33.25,50.6667 44.3333,50.6667 44.3333,47.5 41.1666,47.5 41.1666,36.4167 41.1666,33.25 33.25,33.25z M38.7917,25.3333C37.48,25.3333 36.4167,26.3967 36.4167,27.7083 36.4167,29.02 37.48,30.0833 38.7917,30.0833 40.1033,30.0833 41.1667,29.02 41.1667,27.7083 41.1667,26.3967 40.1033,25.3333 38.7917,25.3333z")
                Return
            End If
        End If

        Select Case Type
            Case HintType.Warning
                BorderBrush = New MyColor("#CCE69900")
                Gradient1.Color = New MyColor("#BBFFF4CE")
                Gradient2.Color = New MyColor("#BBFFF5CE")
                Path.Fill = New MyColor("#957500")
                LabText.Foreground = New MyColor("#957500")
                BtnClose.Foreground = New MyColor("#957500")
                Path.Data = (New GeometryConverter).ConvertFromString("F1 M 58.5832,55.4172L 17.4169,55.4171C 15.5619,53.5621 15.5619,50.5546 17.4168,48.6996L 35.201,15.8402C 37.056,13.9852 40.0635,13.9852 41.9185,15.8402L 58.5832,48.6997C 60.4382,50.5546 60.4382,53.5622 58.5832,55.4172 Z M 34.0417,25.7292L 36.0208,41.9584L 39.9791,41.9583L 41.9583,25.7292L 34.0417,25.7292 Z M 38,44.3333C 36.2511,44.3333 34.8333,45.7511 34.8333,47.5C 34.8333,49.2489 36.2511,50.6667 38,50.6667C 39.7489,50.6667 41.1666,49.2489 41.1666,47.5C 41.1666,45.7511 39.7489,44.3333 38,44.3333 Z ")
                Return
            Case HintType.Caution
                BorderBrush = New MyColor("#CCFF4444")
                Gradient1.Color = New MyColor(CType(If(IsDarkMode, "#BBFF8888", "#BBFFBBBB"), String))
                Gradient2.Color = New MyColor(CType(If(IsDarkMode, "#BBFF6666", "#BBFF8888"), String))
                Path.Fill = New MyColor("#BF0000")
                LabText.Foreground = New MyColor("#BF0000")
                BtnClose.Foreground = New MyColor("#BF0000")
                Path.Data = (New GeometryConverter).ConvertFromString("F1 M1024,1024z M0,0z M512,0C229.23,0 0,229.23 0,512 0,794.77 229.23,1024 512,1024 794.768,1024 1024,794.77 1024,512 1024,229.23 794.77,0 512,0z M746.76,656.252C754.568,664.06,754.566,676.724,746.762,684.536L684.534,746.76C676.726,754.568,664.064,754.574,656.248,746.762L512,602.51 367.75,746.76C359.94,754.572,347.276,754.568,339.466,746.76L277.24,684.536C269.43,676.728,269.428,664.064,277.24,656.252L421.492,512 277.242,367.75C269.432,359.942,269.432,347.276,277.242,339.466L339.468,277.242C347.278,269.43,359.942,269.432,367.752,277.242L512,421.49 656.252,277.24C664.058,269.428,676.722,269.43,684.534,277.24L746.76,339.464C754.566,347.276,754.568,359.938,746.76,367.748L602.51,512 746.76,656.252z")
                Return
            Case Else
                BorderBrush = New MyColor("#CC4D76FF")
                Gradient1.Color = New MyColor("#BBB0D0FF")
                Gradient2.Color = New MyColor("#BB9EBAFF")
                Path.Fill = New MyColor("#0062BF")
                LabText.Foreground = New MyColor("#0062BF")
                BtnClose.Foreground = New MyColor("#0062BF")
                Path.Data = (New GeometryConverter).ConvertFromString("F1M38,19C48.4934,19 57,27.5066 57,38 57,48.4934 48.4934,57 38,57 27.5066,57 19,48.4934 19,38 19,27.5066 27.5066,19 38,19z M33.25,33.25L33.25,36.4167 36.4166,36.4167 36.4166,47.5 33.25,47.5 33.25,50.6667 44.3333,50.6667 44.3333,47.5 41.1666,47.5 41.1666,36.4167 41.1666,33.25 33.25,33.25z M38.7917,25.3333C37.48,25.3333 36.4167,26.3967 36.4167,27.7083 36.4167,29.02 37.48,30.0833 38.7917,30.0833 40.1033,30.0833 41.1667,29.02 41.1667,27.7083 41.1667,26.3967 40.1033,25.3333 38.7917,25.3333z")
                Return
        End Select
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
