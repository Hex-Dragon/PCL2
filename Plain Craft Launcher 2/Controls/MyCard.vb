Imports System.Threading.Tasks

Public Class MyCard

    '控件
    Inherits Grid
    Private ReadOnly MainGrid As Grid
    Private ReadOnly MainChrome As SystemDropShadowChrome
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
        If e Then
            IsThemeChanging = True
            AniStart({AaColor(MainBorder, Border.BackgroundProperty, New MyColor(235, 43, 43, 43) - MainBorder.Background, 300)}, "MyCard Theme " & Uuid)
            Await Task.Delay(300)
            IsThemeChanging = False
        Else
            IsThemeChanging = True
            AniStart({AaColor(MainBorder, Border.BackgroundProperty, New MyColor(205, 255, 255, 255) - MainBorder.Background, 300)}, "MyCard Theme " & Uuid)
            Await Task.Delay(300)
            IsThemeChanging = False

        End If
    End Sub

    'UI 建立
    Public Sub New()
        AddHandler ModSecret.ThemeChanged, AddressOf _ThemeChanged
        MainChrome = New SystemDropShadowChrome With {.Margin = New Thickness(-9.5, -9, 0.5, -0.5), .Opacity = 0.1, .CornerRadius = New CornerRadius(6)}
        MainChrome.SetResourceReference(SystemDropShadowChrome.ColorProperty, "ColorObject1")
        Children.Insert(0, MainChrome)
        MainBorder = New Border With {.Background = New SolidColorBrush(Color.FromArgb(If(IsDarkMode, 235, 205), If(IsDarkMode, 43, 255), If(IsDarkMode, 43, 255), If(IsDarkMode, 43, 255))), .CornerRadius = New CornerRadius(6), .IsHitTestVisible = False}
        Children.Insert(1, MainBorder)
        MainGrid = New Grid
        Children.Add(MainGrid)
    End Sub
    Private IsLoad As Boolean = False
    Private Sub Init() Handles Me.Loaded
        If IsLoad Then Exit Sub
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

    '事件
    Public Property HasMouseAnimation As Boolean = True
    Private Sub MyCard_MouseEnter(sender As Object, e As MouseEventArgs) Handles Me.MouseEnter
        If Not HasMouseAnimation Then Exit Sub
        Dim AniList As New List(Of AniData)
        If Not IsNothing(MainTextBlock) Then AniList.Add(AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush2", 150))
        If Not IsNothing(MainSwap) Then AniList.Add(AaColor(MainSwap, Shapes.Path.FillProperty, "ColorBrush2", 150))
        AniList.AddRange({
                         AaColor(MainChrome, SystemDropShadowChrome.ColorProperty, "ColorObject2", 180),
                         AaColor(MainBorder, Border.BackgroundProperty, New MyColor(If(IsDarkMode, 245, 230), If(IsDarkMode, 48, 255), If(IsDarkMode, 48, 255), If(IsDarkMode, 48, 255)) - MainBorder.Background, 180),
                         AaOpacity(MainChrome, 0.3 - MainChrome.Opacity, 180)
                     })
        If Not IsThemeChanging Then AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub
    Private Sub MyCard_MouseLeave(sender As Object, e As MouseEventArgs) Handles Me.MouseLeave
        If Not HasMouseAnimation Then Exit Sub
        Dim AniList As New List(Of AniData)
        If Not IsNothing(MainTextBlock) Then AniList.Add(AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush1", 250))
        If Not IsNothing(MainSwap) Then AniList.Add(AaColor(MainSwap, Shapes.Path.FillProperty, "ColorBrush1", 250))
        AniList.AddRange({
                         AaColor(MainChrome, SystemDropShadowChrome.ColorProperty, "ColorObject1", 300),
                         AaColor(MainBorder, Border.BackgroundProperty, New MyColor(If(IsDarkMode, 235, 205), If(IsDarkMode, 43, 255), If(IsDarkMode, 43, 255), If(IsDarkMode, 43, 255)) - MainBorder.Background, 300),
                         AaOpacity(MainChrome, 0.1 - MainChrome.Opacity, 300)
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
        If Not UseAnimation Then Exit Sub
        Dim DeltaHeight As Double = If(IsSwaped, SwapedHeight, e.NewSize.Height) - e.PreviousSize.Height
        '卡片的进入时动画已被页面通用切换动画替代
        If e.PreviousSize.Height = 0 OrElse IsHeightAnimating OrElse Math.Abs(DeltaHeight) < 1 OrElse ActualHeight = 0 Then Exit Sub
        StartHeightAnimation(DeltaHeight, e.PreviousSize.Height, False)
    End Sub
    Private Sub StartHeightAnimation(DeltaHeight As Double, PreviousHeight As Double, IsLoadAnimation As Boolean)
        If IsHeightAnimating OrElse FrmMain Is Nothing Then Exit Sub '避免 XAML 设计器出错

        Dim AnimList As New List(Of AniData)
        If DeltaHeight > 10 OrElse (DeltaHeight < -10 AndAlso Not IsNothing(SwapControl)) Then '如果不是需要折叠的卡片，高度减小时的弹跳会吞掉按钮下边框
            '高度增加较大，使用弹起动画
            Dim Delta As Double = MathClamp(Math.Abs(DeltaHeight) * 0.05, 3, 10) * Math.Sign(DeltaHeight)
            AnimList.AddRange({
                AaHeight(Me, DeltaHeight + Delta, 300, If(IsLoadAnimation, 30, 0), If(DeltaHeight > FrmMain.Height, New AniEaseInFluent(AniEasePower.ExtraStrong), New AniEaseOutFluent(AniEasePower.ExtraStrong))),
                AaHeight(Me, -Delta, 150, 260, Ease:=New AniEaseOutFluent(AniEasePower.Strong))
            })
        Else
            '普通的改变就行啦
            AnimList.AddRange({
                AaHeight(Me, DeltaHeight, MathClamp(Math.Abs(DeltaHeight) * 4, 150, 250),, New AniEaseOutFluent)
            })
        End If
        AnimList.Add(AaCode(
        Sub()
            IsHeightAnimating = False
            Height = ActualUsedHeight
            If IsSwaped Then SwapControl.Visibility = Visibility.Collapsed
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
            If _IsSwaped = value Then Exit Property
            _IsSwaped = value
            If SwapControl Is Nothing Then Exit Property
            '展开
            If Not IsSwaped AndAlso TypeOf SwapControl Is StackPanel Then StackInstall(SwapControl, InstallMethod)
            '若尚未加载，会在 Loaded 事件中触发无动画的折叠，不需要在这里进行
            If Not IsLoaded Then Exit Property
            '更新高度
            SwapControl.Visibility = Visibility.Visible
            TriggerForceResize()
            '改变箭头
            AniStart(AaRotateTransform(MainSwap, If(_IsSwaped, If(SwapLogoRight, 270, 0), 180) - CType(MainSwap.RenderTransform, RotateTransform).Angle, 400,, New AniEaseOutBack(AniEasePower.Weak)), "MyCard Swap " & Uuid, True)
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
            (SwapControl Is Nothing OrElse Pos > If(IsSwaped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Exit Sub '检测点击位置；或已经不在可视树上的误判
        IsMouseDown = True
    End Sub
    Private Sub MyCard_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonUp
        If Not IsMouseDown Then Return
        IsMouseDown = False

        Dim Pos As Double = Mouse.GetPosition(Me).Y
        If Not IsSwaped AndAlso
            (SwapControl Is Nothing OrElse Pos > If(IsSwaped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Exit Sub '检测点击位置；或已经不在可视树上的误判

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
                     AaCode(Sub()
                                If RemoveFromChildren Then
                                    If Control.Parent Is Nothing Then Exit Sub
                                    CType(Control.Parent, Object).Children.Remove(Control)
                                Else
                                    Control.Visibility = Visibility.Collapsed
                                End If
                                If CallBack IsNot Nothing Then CallBack(Control)
                            End Sub,, True)
            }, "MyCard Dispose " & Control.Uuid)
        Else
            If RemoveFromChildren Then
                If Control.Parent Is Nothing Then Exit Sub
                CType(Control.Parent, Object).Children.Remove(Control)
            Else
                Control.Visibility = Visibility.Collapsed
            End If
            If CallBack IsNot Nothing Then CallBack(Control)
        End If
    End Sub
End Module