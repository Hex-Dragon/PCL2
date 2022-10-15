Imports NetFwTypeLib

Public Class PageLinkLeft

    Private IsLoad As Boolean = False
    Private IsPageSwitched As Boolean = False '如果在 Loaded 前切换到其他页面，会导致触发 Loaded 时再次切换一次
    Private Sub PageLinkLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If IsLoad Then Exit Sub
        IsLoad = True
        '切换默认页面
        If IsPageSwitched Then Exit Sub
        ItemHiper.SetChecked(True, False, False)
    End Sub
    Private Sub PageOtherLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        IsPageSwitched = False
    End Sub

#Region "Windows 防火墙"

    '为联机模块添加防火墙通行权限的函数库
    '不加防火墙权限你要我怎么办，这联机根本连都连不上，球球你们别看到防火墙就说是后门了，真的很恼火……

    ''' <summary>
    ''' 获取某个文件是否被 Windows 防火墙阻止。
    ''' </summary>
    Public Shared Function FirewallIsBlock(FilePath As String) As Boolean
        '获取配置
        If FirewallPolicy Is Nothing Then
            Try
                FirewallPolicy = FirewallPolicyGet()
            Catch ex As Exception
                Log(ex, "Windows 防火墙：可能关闭得太死了，根本检测不到，笑死", LogLevel.Normal)
                Return False
            End Try
        End If
        '检查是否开启
        If Not FirewallPolicy.CurrentProfile.FirewallEnabled Then
            Log("[Link] Windows 防火墙：防火墙关闭")
            Return False
        End If
        '检查是否允许例外
        If FirewallPolicy.CurrentProfile.ExceptionsNotAllowed Then
            Log("[Link] Windows 防火墙：防火墙开启，不允许例外")
            Return True
        End If
        '检查白名单
        Dim Target As String = FilePath.Replace(PathTemp.Split("\").First, "").ToLower
        For Each App As INetFwAuthorizedApplication In FirewallPolicy.CurrentProfile.AuthorizedApplications
            If ModeDebug Then Log("[Link] 防火墙白名单（" & App.Enabled & "）：" & App.ProcessImageFileName)
            If Not App.Enabled Then Continue For
            '信标的奇妙盘符 E:0/xxxxx/xxx 导致误判……
            Dim FileNameTrimmed As String = App.ProcessImageFileName.Replace(App.ProcessImageFileName.Split("\").First, "")
            If App.ProcessImageFileName.ToLower.Contains(FilePath) Then
                Log("[Link] Windows 防火墙：开启，已在白名单中（" & FilePath & "）")
                Return False
            End If
        Next
        Log("[Link] Windows 防火墙：开启，未在白名单中（" & FilePath & "）")
        Return True
    End Function
    ''' <summary>
    ''' 添加防火墙通行权限。需要管理员权限。
    ''' </summary>
    Public Shared Sub FirewallAddAuthorized(DisplayName As String, FilePath As String)
        Log("[Link] Windows 防火墙：添加通行权限（" & FilePath & "）")
        '获取配置
        If FirewallPolicy Is Nothing Then FirewallPolicy = FirewallPolicyGet()
        '添加项目
        FirewallPolicy.GetProfileByType(NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_DOMAIN).AuthorizedApplications.Add(New FirewallApp(DisplayName, FilePath))
        FirewallPolicy.GetProfileByType(NET_FW_PROFILE_TYPE_.NET_FW_PROFILE_STANDARD).AuthorizedApplications.Add(New FirewallApp(DisplayName, FilePath))
        FirewallPolicy.CurrentProfile.AuthorizedApplications.Add(New FirewallApp(DisplayName, FilePath))
    End Sub
    Private Class FirewallApp
        Implements INetFwAuthorizedApplication
        Public Property Name As String Implements INetFwAuthorizedApplication.Name
        Public Property ProcessImageFileName As String Implements INetFwAuthorizedApplication.ProcessImageFileName
        Public Property IpVersion As NET_FW_IP_VERSION_ = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY Implements INetFwAuthorizedApplication.IpVersion
        Public Property Scope As NET_FW_SCOPE_ = NET_FW_SCOPE_.NET_FW_SCOPE_ALL Implements INetFwAuthorizedApplication.Scope
        Public Property RemoteAddresses As String = "*" Implements INetFwAuthorizedApplication.RemoteAddresses
        Public Property Enabled As Boolean = True Implements INetFwAuthorizedApplication.Enabled
        Public Sub New(DisplayName As String, FilePath As String)
            Name = DisplayName
            ProcessImageFileName = FilePath
        End Sub
    End Class
    '获取防火墙配置
    Public Shared FirewallPolicy As INetFwPolicy = Nothing
    Public Shared Function FirewallPolicyGet() As INetFwPolicy
        Const CLSID_FIREWALL_MANAGER As String = "{304CE942-6E39-40D8-943A-B913C40C9CD4}"
        Dim objType As Type = Type.GetTypeFromCLSID(New Guid(CLSID_FIREWALL_MANAGER))
        Return TryCast(Activator.CreateInstance(objType), INetFwMgr).LocalPolicy
    End Function

#End Region

#Region "页面切换"

    ''' <summary>
    ''' 当前页面的编号。
    ''' </summary>
    Public PageID As FormMain.PageSubType = FormMain.PageSubType.LinkHiper

    ''' <summary>
    ''' 勾选事件改变页面。
    ''' </summary>
    Private Sub PageCheck(sender As MyListItem, e As RouteEventArgs) Handles ItemHiper.Check, ItemIoi.Check, ItemSetup.Check, ItemHelp.Check, ItemFeedback.Check
        '尚未初始化控件属性时，sender.Tag 为 Nothing，会导致切换到页面 0
        '若使用 IsLoaded，则会导致模拟点击不被执行（模拟点击切换页面时，控件的 IsLoaded 为 False）
        If sender.Tag IsNot Nothing Then PageChange(Val(sender.Tag))
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        If ID = -1 Then ID = PageID
        Select Case ID
            Case 0, FormMain.PageSubType.LinkHiper
                If FrmLinkHiper Is Nothing Then FrmLinkHiper = New PageLinkHiper
                Return FrmLinkHiper
            Case FormMain.PageSubType.LinkIoi
                If FrmLinkIoi Is Nothing Then FrmLinkIoi = New PageLinkIoi
                Return FrmLinkIoi
            Case FormMain.PageSubType.LinkSetup
                If FrmSetupLink Is Nothing Then FrmSetupLink = New PageSetupLink
                Return FrmSetupLink
            Case FormMain.PageSubType.LinkHelp
                If FrmLinkHelp Is Nothing Then FrmLinkHelp = PageOtherHelp.GetHelpPage(PathTemp & "Help\启动器\联机.json")
                Return FrmLinkHelp
            Case FormMain.PageSubType.LinkFeedback
                If FrmLinkFeedback Is Nothing Then FrmLinkFeedback = New PageLinkFeedback
                Return FrmLinkFeedback
            Case Else
                Throw New Exception("未知的更多子页面种类：" & ID)
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
            PageChangeRun(PageGet(ID))
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

    Public Sub Reset(sender As Object, e As EventArgs)
        If MyMsgBox("是否要初始化联机页的所有设置？该操作不可撤销。", "初始化确认",, "取消", IsWarn:=True) = 1 Then
            If IsNothing(FrmSetupLink) Then FrmSetupLink = New PageSetupLink
            FrmSetupLink.Reset()
        End If
    End Sub

    Private Sub BtnHiperStop_Loaded(sender As Object, e As RoutedEventArgs)
        sender.Visibility = If(PageLinkHiper.HiperState = LoadState.Finished OrElse PageLinkHiper.HiperState = LoadState.Loading, Visibility.Visible, Visibility.Collapsed)
    End Sub
    Private Sub BtnHiperStop_Click(sender As Object, e As EventArgs)
        PageLinkHiper.ModuleStopManually()
    End Sub
    Private Sub BtnIoiStop_Loaded(sender As Object, e As RoutedEventArgs)
        sender.Visibility = If(PageLinkIoi.InitLoader.State = LoadState.Finished, Visibility.Visible, Visibility.Collapsed)
    End Sub
    Private Sub BtnIoiStop_Click(sender As Object, e As EventArgs)
        PageLinkIoi.ModuleStopManually()
    End Sub

End Class
