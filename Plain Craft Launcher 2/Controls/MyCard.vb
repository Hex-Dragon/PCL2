Imports System.Threading.Tasks

Public Class MyCard

    '控件
    Inherits Grid
    Private ReadOnly MainGrid As Grid
    Public ReadOnly Property MainChrome As MyDropShadow
    Private ReadOnly MainBorder As Border
    Private IsThemeChanging As Boolean = False
    Public Property BorderChild As UIElement
        Get
            Return MainBorder.Child
        End Get
        Set(value As UIElement)
            MainBorder.Child = value
        End Set
    End Property
    Private _MainTextBlock As TextBlock
    Public Property MainTextBlock As TextBlock
        Get
            Init() '当父级触发 Loaded 时，本卡片可能尚未触发 Loaded（该事件从父级向子级调用），因此这会是 null。手动触发以确保控件已加载。
            Return _MainTextBlock
        End Get
        Set(value As TextBlock)
            _MainTextBlock = value
        End Set
    End Property
    Private _MainSwap As Shapes.Path
    Public Property MainSwap As Shapes.Path
        Get
            Init()
            Return _MainSwap
        End Get
        Set(value As Shapes.Path)
            _MainSwap = value
        End Set
    End Property

    '属性
    Public Uuid As Integer = GetUuid()
    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return MainTextBlock.Inlines
        End Get
    End Property
    Public Property CornerRadius As CornerRadius
        Get
            Return MainChrome.CornerRadius
        End Get
        Set(value As CornerRadius)
            MainChrome.CornerRadius = value
            MainBorder.CornerRadius = value
        End Set
    End Property
    Public Property Title As String
        Get
            Return GetValue(TitleProperty)
        End Get
        Set(value As String)
            SetValue(TitleProperty, value)
            If _MainTextBlock IsNot Nothing Then MainTextBlock.Text = value
        End Set
    End Property
    Public Shared ReadOnly TitleProperty As DependencyProperty = DependencyProperty.Register("Title", GetType(String), GetType(MyCard), New PropertyMetadata(""))

    Private Async Sub _ThemeChanged(sender As Object, e As Boolean)
        Dim bgBrush As SolidColorBrush = Application.Current.Resources("ColorBrushSemiWhite")
        IsThemeChanging = True
        AniStart({AaColor(MainBorder, Border.BackgroundProperty, New MyColor(bgBrush) - MainBorder.Background, 300)}, "MyCard Theme " & Uuid)
        Await Task.Delay(300)
        MainBorder.Background = bgBrush
        IsThemeChanging = False
    End Sub

    'UI 建立
    Public Sub New()
        MainChrome = New MyDropShadow With {
            .Margin = New Thickness(-3, -3, -3, -3 - GetWPFSize(1)), .ShadowRadius = 3, .Opacity = DropShadowIdleOpacity, .CornerRadius = New CornerRadius(5)}
        MainChrome.SetResourceReference(MyDropShadow.ColorProperty, "ColorObject1")
        Children.Insert(0, MainChrome)
        MainBorder = New Border With {.CornerRadius = New CornerRadius(5), .IsHitTestVisible = False}
        Children.Insert(1, MainBorder)
        MainGrid = New Grid
        Children.Add(MainGrid)
    End Sub
    Private IsLoad As Boolean = False
    Private Sub Init() Handles Me.Loaded
        AddHandler ThemeChanged, AddressOf _ThemeChanged
        If IsLoad Then Return
        IsLoad = True
        '初次加载限定
        If MainTextBlock Is Nothing Then
            MainTextBlock = New TextBlock With {.HorizontalAlignment = HorizontalAlignment.Left, .VerticalAlignment = VerticalAlignment.Top, .Margin = New Thickness(15, 12, 0, 0), .FontWeight = FontWeights.Bold, .FontSize = 13, .IsHitTestVisible = False}
            MainTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
            MainTextBlock.SetBinding(TextBlock.TextProperty, New Binding("Title") With {.Source = Me, .Mode = BindingMode.OneWay})
            MainGrid.Children.Add(MainTextBlock)
        End If
        If CanSwap OrElse SwapControl IsNot Nothing Then
            If SwapControl Is Nothing AndAlso Children.Count > 3 Then SwapControl = Children(3)
            MainSwap = New Shapes.Path With {.HorizontalAlignment = HorizontalAlignment.Right, .Stretch = Stretch.Uniform, .Height = 6, .Width = 10, .VerticalAlignment = VerticalAlignment.Top, .Margin = New Thickness(0, 17, 16, 0), .Data = New GeometryConverter().ConvertFromString("M2,4 l-2,2 10,10 10,-10 -2,-2 -8,8 -8,-8 z"), .RenderTransform = New RotateTransform(180), .RenderTransformOrigin = New Point(0.5, 0.5)}
            MainSwap.SetResourceReference(Shapes.Path.FillProperty, "ColorBrush1")
            MainGrid.Children.Add(MainSwap)
        End If
        '更新背景色
        MainBorder.Background = Application.Current.Resources("ColorBrushSemiWhite")
        '改变默认的折叠
        If IsSwaped AndAlso SwapControl IsNot Nothing Then
            MainSwap.RenderTransform = New RotateTransform(If(SwapLogoRight, 270, 0))
            '取消由于高度变化被迫触发的高度动画
            Dim RawUseAnimation As Boolean = UseAnimation
            UseAnimation = False
            Height = SwapedHeight
            AniStop("MyCard Height " & Uuid)
            IsHeightAnimating = False
            RunInUi(Sub() UseAnimation = RawUseAnimation, True)
        End If
    End Sub
    Private Sub Dispose() Handles Me.Unloaded
        RemoveHandler ModSecret.ThemeChanged, AddressOf _ThemeChanged
    End Sub
    Public Sub StackInstall()
        StackInstall(SwapControl, InstallMethod)
        TriggerForceResize()
    End Sub
    Public Shared Sub StackInstall(ByRef Stack As StackPanel, InstallMethod As Action(Of StackPanel))
        If Stack.Tag Is Nothing Then Exit Sub
        Try
            InstallMethod(Stack)
        Catch ex As Exception
            Log(ex, "[MyCard] InstallMethod 调用失败")
        End Try
        Stack.Children.Add(New FrameworkElement With {.Height = 18}) '下边距，同时适应折叠
        Stack.Tag = Nothing
    End Sub

    '动画
    Private Const DropShadowIdleOpacity As Double = 0.07
    Private Const DropShadowHoverOpacity As Double = 0.4
    Public Property HasMouseAnimation As Boolean = True
    Private Sub MyCard_MouseEnter(sender As Object, e As MouseEventArgs) Handles Me.MouseEnter
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If Not IsNothing(MainTextBlock) Then AniList.Add(AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush2", 90))
        If Not IsNothing(MainSwap) Then AniList.Add(AaColor(MainSwap, Shapes.Path.FillProperty, "ColorBrush2", 90))
        AniList.AddRange({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject4", 90),
            AaOpacity(MainChrome, DropShadowHoverOpacity - MainChrome.Opacity, 90)
        })
        If Not IsThemeChanging Then AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub
    Private Sub MyCard_MouseLeave(sender As Object, e As MouseEventArgs) Handles Me.MouseLeave
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If Not IsNothing(MainTextBlock) Then AniList.Add(AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush1", 90))
        If Not IsNothing(MainSwap) Then AniList.Add(AaColor(MainSwap, Shapes.Path.FillProperty, "ColorBrush1", 90))
        AniList.AddRange({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject1", 90),
            AaOpacity(MainChrome, DropShadowIdleOpacity - MainChrome.Opacity, 90)
        })
        If Not IsThemeChanging Then AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub

#Region "高度改变动画"

    ''' <summary>
    ''' 是否启用高度改变动画。
    ''' </summary>
    Public Property UseAnimation As Boolean = True
    Private IsHeightAnimating As Boolean = False
    Private ActualUsedHeight As Double '回滚实际高度（例如 NaN）
    Private Sub MySizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        If Not UseAnimation Then Return
        Dim DeltaHeight As Double = If(IsSwaped, SwapedHeight, e.NewSize.Height) - e.PreviousSize.Height
        '卡片的进入时动画已被页面通用切换动画替代
        If e.PreviousSize.Height = 0 OrElse IsHeightAnimating OrElse Math.Abs(DeltaHeight) < 1 OrElse ActualHeight = 0 Then Return
        StartHeightAnimation(DeltaHeight, e.PreviousSize.Height, False)
    End Sub
    Private Sub StartHeightAnimation(Delta As Double, PreviousHeight As Double, IsLoadAnimation As Boolean)
        If IsHeightAnimating OrElse FrmMain Is Nothing Then Return '避免 XAML 设计器出错

        Dim AnimList As New List(Of AniData)
        Dim AbsDelta = Math.Abs(Delta)

        If AbsDelta <= 800 Then
            '短距离，直接使用 150ms 的缓动动画
            AnimList.Add(AaHeight(Me, Delta, 150,, New AniEaseOutFluent(AniEasePower.ExtraStrong)))
        Else
            Dim EaseLength As Integer, EaseTime As Integer
            Dim InitSpeed As Integer '到达缓动区前的初速度
            If Delta < 0 AndAlso AbsDelta - EaseLength > 5000 * 0.1 Then
                '收回距离过长 (>0.1s)，强制以 100ms 完成匀速段，然后让减速段更长
                EaseLength = 200
                EaseTime = 150
                InitSpeed = (AbsDelta - EaseLength) / 0.1
            ElseIf Delta > 0 AndAlso AbsDelta - EaseLength > 5000 * 0.6 Then
                '展开距离过长 (>0.6s)，以 5000 速度展示 300ms 匀速段，剩下的距离全部归入减速段
                InitSpeed = 5000
                EaseLength = AbsDelta - InitSpeed * 0.3
                EaseTime = 400
            Else
                '中程，匀速地快速展开（或收回）
                EaseLength = 150
                EaseTime = 200
                InitSpeed = 4000
            End If
            '匀速段
            AnimList.Add(AaHeight(Me, (AbsDelta - EaseLength) * Math.Sign(Delta),
                (AbsDelta - EaseLength) / InitSpeed * 1000))
            '减速段
            AnimList.Add(AaHeight(Me, EaseLength * Math.Sign(Delta),
                EaseTime,, New AniEaseOutFluentWithInitial(InitSpeed, EaseTime / 1000, EaseLength), True))
        End If

        AnimList.Add(AaCode(
        Sub()
            IsHeightAnimating = False
            Height = ActualUsedHeight
            If IsSwaped AndAlso SwapControl IsNot Nothing Then SwapControl.Visibility = Visibility.Collapsed
        End Sub,, True))
        AniStart(AnimList, "MyCard Height " & Uuid)
        IsHeightAnimating = True
        ActualUsedHeight = If(IsSwaped, SwapedHeight, Height)
        Height = PreviousHeight
    End Sub
    ''' <summary>
    ''' 通知 MyCard，控件内容已改变，需要中断动画并更新高度。
    ''' </summary>
    Public Sub TriggerForceResize()
        Height = If(IsSwaped, SwapedHeight, Double.NaN)
        AniStop("MyCard Height " & Uuid)
        IsHeightAnimating = False
    End Sub

#End Region

#Region "折叠"

    '若设置了 CanSwap，或 SwapControl 不为空，则判定为会进行折叠
    '这是因为不能直接在 XAML 中设置 SwapControl
    Public SwapControl As Object
    Public Property CanSwap As Boolean = False

    ''' <summary>
    ''' 数据转为列表项的转换方法
    ''' </summary>
    ''' <returns></returns>
    Public Property InstallMethod As Action(Of StackPanel)

    ''' <summary>
    ''' 是否已被折叠。
    ''' </summary>
    Public Property IsSwaped As Boolean
        Get
            Return _IsSwaped
        End Get
        Set(value As Boolean)
            If _IsSwaped = value Then Return
            _IsSwaped = value
            If SwapControl Is Nothing Then Return
            '展开
            If Not IsSwaped AndAlso TypeOf SwapControl Is StackPanel Then StackInstall(SwapControl, InstallMethod)
            '若尚未加载，会在 Loaded 事件中触发无动画的折叠，不需要在这里进行
            If Not IsLoaded Then Return
            '更新高度
            SwapControl.Visibility = Visibility.Visible
            TriggerForceResize()
            '改变箭头
            AniStart(AaRotateTransform(MainSwap, If(_IsSwaped, If(SwapLogoRight, 270, 0), 180) - CType(MainSwap.RenderTransform, RotateTransform).Angle, 250,, New AniEaseOutFluent(AniEasePower.ExtraStrong)), "MyCard Swap " & Uuid, True)
        End Set
    End Property
    Private _IsSwaped As Boolean = False

    Public Property SwapLogoRight As Boolean = False
    Private IsMouseDown As Boolean = False
    Public Event PreviewSwap(sender As Object, e As RouteEventArgs)
    Public Event Swap(sender As Object, e As RouteEventArgs)
    Public Const SwapedHeight As Integer = 40
    Private Sub MyCard_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Dim Pos As Double = Mouse.GetPosition(Me).Y
        If Not IsSwaped AndAlso
            (SwapControl Is Nothing OrElse Pos > If(IsSwaped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Return '检测点击位置；或已经不在可视树上的误判
        IsMouseDown = True
    End Sub
    Private Sub MyCard_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonUp
        If Not IsMouseDown Then Return
        IsMouseDown = False

        Dim Pos As Double = Mouse.GetPosition(Me).Y
        If Not IsSwaped AndAlso
            (SwapControl Is Nothing OrElse Pos > If(IsSwaped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Return '检测点击位置；或已经不在可视树上的误判

        Dim ee = New RouteEventArgs(True)
        RaiseEvent PreviewSwap(Me, ee)
        If ee.Handled Then
            IsMouseDown = False
            Return
        End If

        IsSwaped = Not IsSwaped
        Log("[Control] " & If(IsSwaped, "折叠卡片", "展开卡片") & If(Title Is Nothing, "", "：" & Title))
        RaiseEvent Swap(Me, ee)
    End Sub
    Private Sub MyCard_MouseLeave_Swap(sender As Object, e As MouseEventArgs) Handles Me.MouseLeave
        IsMouseDown = False
    End Sub

#End Region

End Class
Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyCard, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        If Control.IsHitTestVisible Then
            Control.IsHitTestVisible = False
            AniStart({
                AaScaleTransform(Control, -0.08, 200,, New AniEaseInFluent),
                AaOpacity(Control, -1, 200,, New AniEaseOutFluent),
                AaHeight(Control, -Control.ActualHeight, 150, 100, New AniEaseOutFluent),
                AaCode(
                Sub()
                    If RemoveFromChildren Then
                        If Control.Parent Is Nothing Then Return
                        CType(Control.Parent, Object).Children.Remove(Control)
                    Else
                        Control.Visibility = Visibility.Collapsed
                    End If
                    If CallBack IsNot Nothing Then CallBack(Control)
                End Sub,, True)
            }, "MyCard Dispose " & Control.Uuid)
        Else
            If RemoveFromChildren Then
                If Control.Parent Is Nothing Then Return
                CType(Control.Parent, Object).Children.Remove(Control)
            Else
                Control.Visibility = Visibility.Collapsed
            End If
            If CallBack IsNot Nothing Then CallBack(Control)
        End If
    End Sub
End Module