Imports System.Windows.Forms

Public Class MyLocalModItem

#Region "基础属性"
    Public Uuid As Integer = GetUuid()

    'Logo
    Private _Logo As String = ""
    Public Property Logo As String
        Get
            Return _Logo
        End Get
        Set(value As String)
            If _Logo = value OrElse value Is Nothing Then Exit Property
            _Logo = value
            If ModeDebug AndAlso Not _Logo = PathImage & "Icons/NoIcon.png" Then Log($"[LocalModItem] Mod {Title} 的图标：{value}")
            Dim FileAddress = PathTemp & "CompLogo\" & GetHash(_Logo) & ".png"
            Try
                If _Logo.StartsWithF("http", True) Then
                    '网络图片
                    If File.Exists(FileAddress) Then
                        PathLogo.Source = New MyBitmap(FileAddress)
                    Else
                        PathLogo.Source = New MyBitmap(PathImage & "Icons/NoIcon.png")
                        RunInNewThread(Sub() LogoLoader(FileAddress), "Comp Logo Loader " & Uuid & "#", ThreadPriority.BelowNormal)
                    End If
                Else
                    '位图
                    PathLogo.Source = New MyBitmap(_Logo)
                End If
            Catch ex As IOException
                Log(ex, "加载本地 Mod 图标时读取失败（" & FileAddress & "）")
            Catch ex As ArgumentException
                '考虑缓存的图片本身可能有误
                Log(ex, "可视化本地 Mod 图标失败（" & FileAddress & "）")
                Try
                    File.Delete(FileAddress)
                    Log("[LocalModItem] 已清理损坏的本地 Mod 图标：" & FileAddress)
                Catch exx As Exception
                    Log(exx, "清理损坏的本地 Mod 图标缓存失败（" & FileAddress & "）", LogLevel.Hint)
                End Try
            Catch ex As Exception
                Log(ex, "加载本地 Mod 图标失败（" & value & "）")
            End Try
        End Set
    End Property
    '后台加载 Logo
    Private Sub LogoLoader(LocalFileAddress As String)
        Dim Retried As Boolean = False
        Dim DownloadEnd As String = GetUuid()
RetryStart:
        Try
            'CurseForge 图片使用缩略图
            Dim Url As String = _Logo
            If Url.Contains("/256/256/") AndAlso GetPixelSize(1) <= 1.25 AndAlso Not Retried Then '#3075：部分 Mod 不存在 64x64 图标，所以重试时不再缩小
                Url = Url.Replace("/256/256/", "/64/64/")
            End If
            '下载图片
            NetDownload(Url, LocalFileAddress & DownloadEnd, True)
            Dim LoadError As Exception = Nothing
            RunInUiWait(
            Sub()
                Try
                    '在地址更换时取消加载
                    If LocalFileAddress <> $"{PathTemp}CompLogo\{GetHash(_Logo)}.png" Then Exit Sub
                    '在完成正常加载后才保存缓存图片
                    PathLogo.Source = New MyBitmap(LocalFileAddress & DownloadEnd)
                Catch ex As Exception
                    Log(ex, "读取本地 Mod 图标失败（" & LocalFileAddress & "）")
                    File.Delete(LocalFileAddress & DownloadEnd)
                    LoadError = ex
                End Try
            End Sub)
            If LoadError IsNot Nothing Then Throw LoadError
            If File.Exists(LocalFileAddress) Then
                File.Delete(LocalFileAddress & DownloadEnd)
            Else
                FileIO.FileSystem.MoveFile(LocalFileAddress & DownloadEnd, LocalFileAddress)
            End If
        Catch ex As Exception
            If Not Retried Then
                Retried = True
                GoTo RetryStart
            Else
                Log(ex, $"下载本地 Mod 图标失败（{_Logo}）")
                RunInUi(Sub() PathLogo.Source = New MyBitmap(PathImage & "Icons/NoIcon.png"))
            End If
        End Try
    End Sub

    '标题
    Private _Title As String
    Public Property Title As String
        Get
            Return _Title
        End Get
        Set(value As String)
            Dim RawValue = value
            Select Case Entry.State
                Case McMod.McModState.Fine
                    LabTitle.TextDecorations = Nothing
                Case McMod.McModState.Disabled
                    LabTitle.TextDecorations = TextDecorations.Strikethrough
                Case McMod.McModState.Unavailable
                    LabTitle.TextDecorations = TextDecorations.Strikethrough
                    value &= " [错误]"
            End Select
            If LabTitle.Text = value Then Exit Property
            LabTitle.Text = value
            _Title = RawValue
        End Set
    End Property

    '副标题
    Public Property SubTitle As String
        Get
            Return If(LabSubtitle?.Text, "")
        End Get
        Set(value As String)
            If LabSubtitle.Text = value Then Exit Property
            LabSubtitle.Text = value
            LabSubtitle.Visibility = If(value = "", Visibility.Collapsed, Visibility.Visible)
        End Set
    End Property

    '描述
    Public Property Description As String
        Get
            Return LabInfo.Text
        End Get
        Set(value As String)
            If LabInfo.Text = value Then Exit Property
            LabInfo.Text = value
        End Set
    End Property

    'Tag
    Public WriteOnly Property Tags As List(Of String)
        Set(value As List(Of String))
            PanTags.Children.Clear()
            PanTags.Visibility = If(value.Any(), Visibility.Visible, Visibility.Collapsed)
            For Each TagText In value
                Dim NewTag = GetObjectFromXML(
                "<Border xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                         Background=""#11000000"" Padding=""3,1"" CornerRadius=""3"" Margin=""0,0,3,0"" 
                         SnapsToDevicePixels=""True"" UseLayoutRounding=""False"">
                   <TextBlock Text=""" & TagText & """ Foreground=""#868686"" FontSize=""11"" />
                </Border>")
                PanTags.Children.Add(NewTag)
            Next
        End Set
    End Property

    '相关联的 Mod
    Public Property Entry As McMod
        Get
            Return Tag
        End Get
        Set(value As McMod)
            Tag = value
        End Set
    End Property

#End Region

#Region "点击与勾选"

    '触发点击事件
    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        If IsMouseDown Then
            RaiseEvent Click(sender, e)
            If e.Handled Then Exit Sub
            Log("[Control] 按下本地 Mod 列表项：" & LabTitle.Text)
        End If
    End Sub

    '鼠标点击判定
    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        If Not IsMouseDirectlyOver Then Exit Sub
        IsMouseDown = True
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = False
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        IsMouseDown = False
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = True
    End Sub

    '滑动选中
    Private Shared SwipeStart As Integer, SwipeEnd As Integer
    Private Shared Swiping As Boolean = False
    Private Shared SwipToState As Boolean '被滑动到的目标应将 Checked 改为此值
    Private Sub Button_MouseSwipeStart(sender As Object, e As Object) Handles Me.MouseLeftButtonDown
        If Parent Is Nothing Then Exit Sub 'Mod 可能已被删除（#3824）
        '开始滑动
        Dim Index = CType(Parent, StackPanel).Children.IndexOf(Me)
        SwipeStart = Index
        SwipeEnd = Index
        Swiping = True
        SwipToState = Not Checked
        FrmVersionMod.CardSelect.IsHitTestVisible = False '暂时禁用下边栏
    End Sub
    Private Sub Button_MouseSwipe(sender As Object, e As Object) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonUp
        If Parent Is Nothing Then Exit Sub 'Mod 可能已被删除（#3824）
        '结束滑动
        If Mouse.LeftButton <> MouseButtonState.Pressed OrElse Not Swiping Then
            Swiping = False
            FrmVersionMod.CardSelect.IsHitTestVisible = True
            Exit Sub
        End If
        '计算滑动范围
        Dim Index = CType(Parent, StackPanel).Children.IndexOf(Me)
        SwipeStart = Math.Min(SwipeStart, Index)
        SwipeEnd = Math.Max(SwipeEnd, Index)
        '勾选所有范围中的项
        If SwipeStart = SwipeEnd Then Exit Sub
        For i = SwipeStart To SwipeEnd
            Dim Item As MyLocalModItem = CType(Parent, StackPanel).Children(i)
            Item.InitLate(Item, e)
            Item.Checked = SwipToState
        Next
    End Sub

    '勾选状态
    Public Event Check(sender As Object, e As RouteEventArgs)
    Public Event Changed(sender As Object, e As RouteEventArgs)
    Private _Checked As Boolean = False
    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            Try
                '触发属性值修改
                Dim RawValue = _Checked
                If value = _Checked Then Exit Property
                _Checked = value
                Dim ChangedEventArgs As New RouteEventArgs(False)
                If IsInitialized Then
                    RaiseEvent Changed(Me, ChangedEventArgs)
                    If ChangedEventArgs.Handled Then
                        _Checked = RawValue
                        Exit Property
                    End If
                End If
                If value Then
                    Dim CheckEventArgs As New RouteEventArgs(False)
                    RaiseEvent Check(Me, CheckEventArgs)
                    If CheckEventArgs.Handled Then Exit Property
                End If
                '更改动画
                If IsVisibleInForm() Then
                    Dim Anim As New List(Of AniData)
                    If Checked Then
                        '由无变有
                        Dim Delta = 32 - RectCheck.ActualHeight
                        Anim.Add(AaHeight(RectCheck, Delta * 0.4, 200,, New AniEaseOutFluent(AniEasePower.Weak)))
                        Anim.Add(AaHeight(RectCheck, Delta * 0.6, 300,, New AniEaseOutBack(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, 1 - RectCheck.Opacity, 30))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                        RectCheck.Margin = New Thickness(-3, 0, 0, 0)
                        Anim.Add(AaColor(LabTitle, TextBlock.ForegroundProperty, If(Entry.State = McMod.McModState.Fine, "ColorBrush2", "ColorBrush5"), 200))
                    Else
                        '由有变无
                        Anim.Add(AaHeight(RectCheck, -RectCheck.ActualHeight, 120,, New AniEaseInFluent(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, -RectCheck.Opacity, 70, 40))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                        Anim.Add(AaColor(LabTitle, TextBlock.ForegroundProperty, If(LabTitle.TextDecorations Is Nothing, "ColorBrush1", "ColorBrushGray4"), 120))
                    End If
                    AniStart(Anim, "MyLocalModItem Checked " & Uuid)
                Else
                    '不在窗口上时直接设置
                    RectCheck.VerticalAlignment = VerticalAlignment.Center
                    RectCheck.Margin = New Thickness(-3, 0, 0, 0)
                    If Checked Then
                        RectCheck.Height = 32
                        RectCheck.Opacity = 1
                        LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.State = McMod.McModState.Fine, "ColorBrush2", "ColorBrush5"))
                    Else
                        RectCheck.Height = 0
                        RectCheck.Opacity = 0
                        LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.State = McMod.McModState.Fine, "ColorBrush1", "ColorBrushGray4"))
                    End If
                    AniStop("MyLocalModItem Checked " & Uuid)
                End If
            Catch ex As Exception
                Log(ex, "设置 Checked 失败")
            End Try
        End Set
    End Property


#End Region

#Region "后加载内容"

    '右下角状态指示图标
    Private ImgState As Image

    '指向背景
    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .Name = "RectBack",
                    .CornerRadius = New CornerRadius(3),
                    .RenderTransform = New ScaleTransform(0.8, 0.8),
                    .RenderTransformOrigin = New Point(0.5, 0.5),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .SnapsToDevicePixels = True,
                    .IsHitTestVisible = False,
                    .Opacity = 0
                }
                Rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                Rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6")
                SetColumnSpan(Rect, 999)
                SetRowSpan(Rect, 999)
                Children.Insert(0, Rect)
                _RectBack = Rect
                '<!--<Border x:Name = "RectBack" CornerRadius="3" RenderTransformOrigin="0.5,0.5" SnapsToDevicePixels="True" 
                'IsHitTestVisible = "False" Opacity="0" BorderThickness="1" 
                'Grid.ColumnSpan = "4" Background="{DynamicResource ColorBrush7}" BorderBrush="{DynamicResource ColorBrush6}"/>-->
            End If
            Return _RectBack
        End Get
    End Property

    '按钮
    Public ButtonHandler As Action(Of MyLocalModItem, EventArgs)
    Public ButtonStack As FrameworkElement
    Private _Buttons As IEnumerable(Of MyIconButton)
    Public Property Buttons As IEnumerable(Of MyIconButton)
        Get
            Return _Buttons
        End Get
        Set(value As IEnumerable(Of MyIconButton))
            _Buttons = value
            '移除原 Stack
            If ButtonStack IsNot Nothing Then
                Children.Remove(ButtonStack)
                ButtonStack = Nothing
            End If
            If Not value.Any() Then Exit Property
            '添加新 Stack
            ButtonStack = New StackPanel With {.Opacity = 0, .Margin = New Thickness(0, 0, 5, 0), .SnapsToDevicePixels = False, .Orientation = Orientation.Horizontal,
                .HorizontalAlignment = HorizontalAlignment.Right, .VerticalAlignment = VerticalAlignment.Center, .UseLayoutRounding = False}
            SetColumnSpan(ButtonStack, 10) : SetRowSpan(ButtonStack, 10)
            '构造按钮
            For Each Btn As MyIconButton In value
                If Btn.Height.Equals(Double.NaN) Then Btn.Height = 25
                If Btn.Width.Equals(Double.NaN) Then Btn.Width = 25
                CType(ButtonStack, StackPanel).Children.Add(Btn)
            Next
            Children.Add(ButtonStack)
        End Set
    End Property

    '勾选条
    Private _RectCheck As Border
    Public ReadOnly Property RectCheck As Border
        Get
            If _RectCheck Is Nothing Then
                _RectCheck = New Border With {.Width = 5, .Height = If(Checked, Double.NaN, 0), .CornerRadius = New CornerRadius(2, 2, 2, 2),
                    .VerticalAlignment = If(Checked, VerticalAlignment.Stretch, VerticalAlignment.Center),
                    .HorizontalAlignment = HorizontalAlignment.Left, .UseLayoutRounding = False, .SnapsToDevicePixels = False,
                    .Margin = If(Checked, New Thickness(-3, 6, 0, 6), New Thickness(-3, 0, 0, 0))}
                _RectCheck.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                SetRowSpan(_RectCheck, 10)
                Children.Add(_RectCheck)
            End If
            Return _RectCheck
        End Get
    End Property

#End Region

    Public Sub Refresh()
        RunInUi(
        Sub()
            '更新
            If Entry.CanUpdate Then
                BtnUpdate.Visibility = Visibility.Visible
                Dim CurrentName = Entry.CompFile.FileName.Replace(".jar", "")
                Dim NewestName = Entry.UpdateFile.FileName.Replace(".jar", "")
                '简化名称对比
                Dim CurrentSegs = CurrentName.Split("-"c).ToList()
                Dim NewestSegs = NewestName.Split("-"c).ToList()
                Dim Shortened As Boolean = False
                For Each Seg In CurrentSegs.ToList()
                    If Not NewestSegs.Contains(Seg) Then Continue For
                    CurrentSegs.Remove(Seg)
                    NewestSegs.Remove(Seg)
                    Shortened = True
                Next
                If Shortened AndAlso CurrentSegs.Any() AndAlso NewestSegs.Any() Then
                    CurrentName = Join(CurrentSegs, "-")
                    NewestName = Join(NewestSegs, "-")
                    Entry._Version = CurrentName '使用网络信息作为显示的版本号
                End If
                BtnUpdate.ToolTip = $"当前版本：{CurrentName} ({Entry.CompFile.ReleaseDate:yyyy/MM/dd HH:mm:ss}){vbCrLf}最新版本：{NewestName} ({Entry.UpdateFile.ReleaseDate:yyyy/MM/dd HH:mm:ss}){vbCrLf}点击以更新，右键查看更新日志。"
            Else
                BtnUpdate.Visibility = Visibility.Collapsed
            End If
            '标题
            If Entry.Comp Is Nothing Then
                Title = Entry.Name
                SubTitle = If(Entry.Version Is Nothing, "", "  |  " & Entry.Version)
            Else
                Dim Titles = Entry.Comp.GetControlTitle(False)
                Title = Titles.Key
                SubTitle = Titles.Value & If(Entry.Version Is Nothing, "", "  |  " & Entry.Version)
            End If
            If Checked Then
                LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.State = McMod.McModState.Fine, "ColorBrush2", "ColorBrush5"))
            Else
                LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.State = McMod.McModState.Fine, "ColorBrush1", "ColorBrushGray4"))
            End If
            '描述
            Dim NewDescription As String
            Select Case Entry.State
                Case McMod.McModState.Fine
                    NewDescription = GetFileNameWithoutExtentionFromPath(Entry.Path)
                Case McMod.McModState.Disabled
                    NewDescription = GetFileNameWithoutExtentionFromPath(Entry.Path.Replace(".disabled", "").Replace(".old", ""))
                Case Else 'McMod.McModState.Unavailable
                    NewDescription = GetFileNameFromPath(Entry.Path)
            End Select
            If Entry.Comp IsNot Nothing Then
                NewDescription += ": " & Entry.Comp.Description.Replace(vbCr, "").Replace(vbLf, "")
            ElseIf Entry.Description IsNot Nothing Then
                NewDescription += ": " & Entry.Description.Replace(vbCr, "").Replace(vbLf, "")
            ElseIf Not Entry.IsFileAvailable Then
                NewDescription += ": " & "存在错误，无法获取信息"
            End If
            Description = NewDescription
            '主 Logo
            Logo = If(Entry.Comp Is Nothing, PathImage & "Icons/NoIcon.png", Entry.Comp.GetControlLogo())
            '图标右下角的 Logo
            If Entry.State = McMod.McModState.Fine Then
                If ImgState IsNot Nothing Then
                    Children.Remove(ImgState)
                    ImgState = Nothing
                End If
            Else
                If ImgState Is Nothing Then
                    ImgState = New Image With {
                        .Width = 20, .Height = 20, .Margin = New Thickness(0, 0, -5, -3), .IsHitTestVisible = False,
                        .HorizontalAlignment = HorizontalAlignment.Right, .VerticalAlignment = VerticalAlignment.Bottom
                    }
                    RenderOptions.SetBitmapScalingMode(ImgState, BitmapScalingMode.HighQuality)
                    SetColumn(ImgState, 1) : SetRow(ImgState, 1) : SetRowSpan(ImgState, 2)
                    Children.Add(ImgState)
                    '<Image x:Name="ImgState" RenderOptions.BitmapScalingMode="HighQuality" Width="16" Height="16" Margin="0,0,-3,-1"
                    '       Grid.Column="1" Grid.Row="1" Grid.RowSpan="2" IsHitTestVisible="False"
                    '       HorizontalAlignment="Right" VerticalAlignment="Bottom"
                    '       Source="/Images/Icons/Unavailable.png" />
                End If
                ImgState.Source = New MyBitmap(PathImage & $"Icons/{Entry.State}.png")
            End If
            '标签
            If Entry.Comp IsNot Nothing Then Tags = Entry.Comp.Tags
        End Sub)
    End Sub

    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonDown, Me.MouseLeftButtonUp, Me.Changed
        InitLate(sender, e)
        '触发颜色动画
        Dim Time As Integer = If(IsMouseOver, 120, 180)
        Dim Ani As New List(Of AniData)
        'ButtonStack
        If ButtonStack IsNot Nothing Then
            If IsMouseOver Then
                Ani.Add(AaOpacity(ButtonStack, 1 - ButtonStack.Opacity, Time * 0.7, Time * 0.3))
                Ani.Add(AaDouble(Sub(i) ColumnPaddingRight.Width = New GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + i)),
                    5 + Buttons.Count * 25 - ColumnPaddingRight.Width.Value, Time * 0.3, Time * 0.7))
            Else
                Ani.Add(AaOpacity(ButtonStack, -ButtonStack.Opacity, Time * 0.4))
                Ani.Add(AaDouble(Sub(i) ColumnPaddingRight.Width = New GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + i)),
                    4 - ColumnPaddingRight.Width.Value, Time * 0.4))
            End If
        End If
        'RectBack
        If IsMouseOver OrElse Checked Then
            Ani.AddRange({
                AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrushBg1"), Time),
                AaOpacity(RectBack, 1 - RectBack.Opacity, Time,, New AniEaseOutFluent)
            })
            If IsMouseDown Then
                Ani.Add(AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
            Else
                Ani.Add(AaScaleTransform(RectBack, 1 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
            End If
        Else
            Ani.AddRange({
                AaOpacity(RectBack, -RectBack.Opacity, Time),
                AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time,, New AniEaseOutFluent),
                AaScaleTransform(RectBack, -0.196, 1,,, True)
            })
        End If
        AniStart(Ani, "LocalModItem Color " & Uuid)
    End Sub

    '触发虚拟化内容
    Private Sub InitLate(sender As Object, e As EventArgs)
        If ButtonHandler IsNot Nothing Then
            ButtonHandler(sender, e)
            ButtonHandler = Nothing
        End If
    End Sub

    '显示更新日志
    Private Sub ShowUpdateLog(sender As Object, e As MouseButtonEventArgs) Handles BtnUpdate.PreviewMouseRightButtonUp
        e.Handled = True
        Dim CurseForgeUrl As String = Entry.ChangelogUrls.FirstOrDefault(Function(x) x.Contains("curseforge.com"))
        Dim ModrinthUrl As String = Entry.ChangelogUrls.FirstOrDefault(Function(x) x.Contains("modrinth.com"))
        If CurseForgeUrl Is Nothing OrElse ModrinthUrl Is Nothing Then
            OpenWebsite(Entry.ChangelogUrls.First)
        Else
            Select Case MyMsgBox("要在哪个网站上查看更新日志？", "查看更新日志", "Modrinth", "CurseForge", "取消")
                Case 1
                    OpenWebsite(ModrinthUrl)
                Case 2
                    OpenWebsite(CurseForgeUrl)
            End Select
        End If
    End Sub

    '触发更新
    Private Sub BtnUpdate_Click(sender As Object, e As EventArgs) Handles BtnUpdate.Click
        FrmVersionMod.UpdateMods({Entry})
    End Sub

    '自适应（#4465）
    Private Sub PanTitle_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanTitle.SizeChanged
        If ColumnExtend.Width.IsStar AndAlso ColumnExtend.ActualWidth < 0.5 Then
            '压缩 Subtitle
            ColumnSubtitle.Width = New GridLength(1, GridUnitType.Star)
            ColumnExtend.Width = New GridLength(0, GridUnitType.Pixel)
        ElseIf Not ColumnExtend.Width.IsStar AndAlso Not LabSubtitle.IsTextTrimmed Then
            '向右展开 Subtitle
            ColumnSubtitle.Width = GridLength.Auto
            ColumnExtend.Width = New GridLength(1, GridUnitType.Star)
        End If
    End Sub

End Class
