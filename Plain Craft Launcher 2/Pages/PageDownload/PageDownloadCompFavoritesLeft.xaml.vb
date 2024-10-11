Public Class PageDownloadCompFavoritesLeft

    Private IsLoad As Boolean = False
    Private IsPageSwitched As Boolean = False '如果在 Loaded 前切换到其他页面，会导致触发 Loaded 时再次切换一次
    Private Sub PageSetupLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        If IsLoad Then Exit Sub
        IsLoad = True

    End Sub

#Region "页面切换"

    ''' <summary>
    ''' 当前页面的编号。从左往右从 0 开始计算。
    ''' </summary>
    Public PageID As FormMain.PageSubType
    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' 勾选事件改变页面。
    ''' </summary>
    Private Sub PageCheck(sender As MyListItem, e As EventArgs) Handles ItemLaunch.Check, ItemUI.Check
        '尚未初始化控件属性时，sender.Tag 为 Nothing，会跳过切换，且由于 PageID 默认为 0 而切换到第一个页面
        '若使用 IsLoaded，则会导致模拟点击不被执行（模拟点击切换页面时，控件的 IsLoaded 为 False）
        If sender.Tag IsNot Nothing Then PageChange(Val(sender.Tag))
    End Sub

    ''' <summary>
    ''' 获取当前导航指定的右页面。
    ''' </summary>
    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.CompFavoritesMod
                If FrmDownloadCompFavoritesMod Is Nothing Then FrmDownloadCompFavoritesMod = New PageDownloadCompFavorites
                Return FrmDownloadCompFavoritesMod
            Case FormMain.PageSubType.CompFavoritesModpack
                If FrmDownloadCompFavoritesModpack Is Nothing Then FrmDownloadCompFavoritesModpack = New PageDownloadCompFavorites
                Return FrmSetupUI
            Case Else
                Throw New Exception("未知的设置子页面种类：" & ID)
        End Select
    End Function

    ''' <summary>
    ''' 切换现有页面。
    ''' </summary>
    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Exit Sub
        AniControlEnabled += 1
        IsPageSwitched = True
        Try

            Select Case ID
                Case FormMain.PageSubType.CompFavoritesMod
                    If IsNothing(FrmDownloadCompFavoritesMod) Then FrmDownloadCompFavoritesMod = New PageDownloadCompFavorites
                    PageChangeRun(FrmDownloadCompFavoritesMod)
                Case FormMain.PageSubType.CompFavoritesModpack
                    If IsNothing(FrmDownloadCompFavoritesModpack) Then FrmDownloadCompFavoritesModpack = New PageDownloadCompFavorites
                    PageChangeRun(FrmDownloadCompFavoritesModpack)
                Case Else
                    Throw New Exception("未知的设置子页面种类：" & ID)
            End Select

            PageID = ID
        Catch ex As Exception
            Log(ex, "切换设置分页面失败（ID " & ID & "）", LogLevel.Feedback)
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub
    Private Shared Sub PageChangeRun(Target As MyPageRight)
        AniStop("FrmMain PageChangeRight") '停止主页面的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
        If Target.Parent IsNot Nothing Then Target.SetValue(ContentPresenter.ContentProperty, Nothing)
        FrmMain.PageRight = Target
        CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnExit()
        AniStart({
                         AaCode(Sub()
                                    CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnForceExit()
                                    FrmMain.PanMainRight.Child = FrmMain.PageRight
                                    FrmMain.PageRight.Opacity = 0
                                End Sub, 130),
                         AaCode(Sub()
                                    '延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                                    FrmMain.PageRight.Opacity = 1
                                    FrmMain.PageRight.PageOnEnter()
                                End Sub, 30, True)
                     }, "PageLeft PageChange")
    End Sub

#End Region

    '强制刷新
    Public Sub Refresh(sender As Object, e As EventArgs) '由边栏按钮匿名调用
        Hint("正在刷新……", Log:=False)
    End Sub
End Class
