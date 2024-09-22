Public Class MyCompItem

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
                Log(ex, "加载资源工程图标时读取失败（" & FileAddress & "）")
            Catch ex As ArgumentException
                '考虑缓存的图片本身可能有误
                Log(ex, "可视化资源工程图标失败（" & FileAddress & "）")
                Try
                    File.Delete(FileAddress)
                    Log("[Comp] 已清理损坏的资源工程图标：" & FileAddress)
                Catch exx As Exception
                    Log(exx, "清理损坏的资源工程图标缓存失败（" & FileAddress & "）", LogLevel.Hint)
                End Try
            Catch ex As Exception
                Log(ex, "加载资源工程图标失败（" & value & "）")
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
            If Url.Contains("/256/256/") AndAlso GetPixelSize(1) <= 1.25 AndAlso Not Retried Then
                Url = Url.Replace("/256/256/", "/64/64/") '#3075：部分 Mod 不存在 64x64 图标，所以重试时不再缩小
            End If
            '下载图片
            NetDownload(Url, LocalFileAddress & DownloadEnd, True)
            If Url.EndsWithF("webp") Then
                Log($"[Comp] Webp 格式转换：{LocalFileAddress}")
                Dim dec = New Imazen.WebP.SimpleDecoder()
                Dim picFile = File.ReadAllBytes(LocalFileAddress & DownloadEnd)
                dec.DecodeFromBytes(picFile, picFile.Length).Save(LocalFileAddress & DownloadEnd)
            End If
            Dim LoadError As Exception = Nothing
            RunInUiWait(
            Sub()
                Try
                    '在地址更换时取消加载
                    If LocalFileAddress <> $"{PathTemp}CompLogo\{GetHash(_Logo)}.png" Then Exit Sub
                    '在完成正常加载后才保存缓存图片
                    PathLogo.Source = New MyBitmap(LocalFileAddress & DownloadEnd)
                Catch ex As Exception
                    Log(ex, $"读取资源工程图标失败（{LocalFileAddress}）")
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
                Log(ex, $"下载资源工程图标失败（{_Logo}）")
                RunInUi(Sub() PathLogo.Source = New MyBitmap(PathImage & "Icons/NoIcon.png"))
            End If
        End Try
    End Sub

    '标题
    Public Property Title As String
        Get
            Return LabTitle.Text
        End Get
        Set(value As String)
            If LabTitle.Text = value Then Exit Property
            LabTitle.Text = value
        End Set
    End Property

    '副标题
    Public Property SubTitle As String
        Get
            Return If(LabTitleRaw?.Text, "")
        End Get
        Set(value As String)
            If LabTitleRaw.Text = value Then Exit Property
            LabTitleRaw.Text = value
            LabTitleRaw.Visibility = If(value = "", Visibility.Collapsed, Visibility.Visible)
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
    '指向时扩展描述
    Private Sub LabInfo_MouseEnter(sender As Object, e As MouseEventArgs) Handles LabInfo.MouseEnter
        If IsTextTrimmed(LabInfo) Then
            ToolTipInfo.Content = LabInfo.Text
            ToolTipInfo.Width = LabInfo.ActualWidth + 25
            LabInfo.ToolTip = ToolTipInfo
        Else
            LabInfo.ToolTip = Nothing
        End If
    End Sub
    Private Function IsTextTrimmed(textBlock As TextBlock) As Boolean
        Dim typeface As New Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch)
        Dim formattedText As New FormattedText(textBlock.Text, Thread.CurrentThread.CurrentCulture, textBlock.FlowDirection, typeface, textBlock.FontSize, textBlock.Foreground, DPI)
        Return formattedText.Width > textBlock.ActualWidth
    End Function

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

#End Region

#Region "点击"

    '触发点击事件
    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        If IsMouseDown Then
            RaiseEvent Click(sender, e)
            If e.Handled Then Exit Sub
            Log("[Control] 按下资源工程列表项：" & LabTitle.Text)
        End If
    End Sub
    Private Sub ProjectClick(sender As MyCompItem, e As EventArgs) Handles Me.Click
        '记录当前展开的卡片标题（#2712）
        Dim Titles As New List(Of String)
        If FrmMain.PageCurrent.Page = FormMain.PageType.CompDetail Then
            For Each Card As MyCard In FrmDownloadCompDetail.PanMain.Children
                If Card.Title <> "" AndAlso Not Card.IsSwaped Then Titles.Add(Card.Title)
            Next
            Log("[Comp] 记录当前已展开的卡片：" & String.Join("、", Titles))
            FrmMain.PageCurrent.Additional(1) = Titles
        End If
        '打开详情页
        Dim TargetVersion As String
        Dim TargetLoader As CompModLoaderType
        If FrmMain.PageCurrent.Page = FormMain.PageType.CompDetail Then
            TargetVersion = FrmMain.PageCurrent.Additional(2)
            TargetLoader = FrmMain.PageCurrent.Additional(3)
        Else
            Select Case CType(sender.Tag, CompProject).Type
                Case CompType.Mod
                    TargetVersion = If(PageDownloadMod.Loader.Input.GameVersion, "")
                    TargetLoader = PageDownloadMod.Loader.Input.ModLoader
                Case CompType.ModPack
                    TargetVersion = If(PageDownloadPack.Loader.Input.GameVersion, "")
                Case CompType.Shader
                    TargetVersion = If(PageDownloadShader.Loader.Input.GameVersion, "")
                Case Else 'CompType.ResourcePack
                    'FUTURE: Res
                    TargetVersion = "" 'If(PageDownloadResource.Loader.Input.GameVersion, "")
            End Select
        End If
        If CType(sender.Tag, CompProject).Type <> CompType.Mod Then TargetLoader = CompModLoaderType.Any
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CompDetail,
                           .Additional = {sender.Tag, New List(Of String), TargetVersion, TargetLoader}})
    End Sub

    '鼠标点击判定
    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        If IsMouseOver AndAlso CanInteraction Then IsMouseDown = True
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        IsMouseDown = False
    End Sub

#End Region

#Region "后加载指向背景"

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

#End Region

    Private StateLast As String
    ''' <summary>
    ''' 是否允许交互。目前仅用于 PageDownloadCompDetail 的顶部栏展示：若关闭碰撞检测，则无法展开 Tooltip。
    ''' </summary>
    Public CanInteraction As Boolean = True
    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonDown, Me.MouseLeftButtonUp
        If Not CanInteraction Then Exit Sub
        '判断当前颜色
        Dim StateNew As String, Time As Integer
        If IsMouseOver Then
            If IsMouseDown Then
                StateNew = "MouseDown"
                Time = 120
            Else
                StateNew = "MouseOver"
                Time = 120
            End If
        Else
            StateNew = "Idle"
            Time = 180
        End If
        If StateLast = StateNew Then Exit Sub
        StateLast = StateNew
        '触发颜色动画
        If IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
            '有动画
            Dim Ani As New List(Of AniData)
            If IsMouseOver Then
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
                             AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrush7"), Time),
                             AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time,, New AniEaseOutFluent),
                             AaScaleTransform(RectBack, -0.196, 1,,, True)
                         })
            End If
            AniStart(Ani, "CompItem Color " & Uuid)
        Else
            '无动画
            AniStop("CompItem Color " & Uuid)
            If _RectBack IsNot Nothing Then RectBack.Opacity = 0
        End If
    End Sub

End Class
