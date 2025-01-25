Imports System.Security.Cryptography.X509Certificates
Imports NAudio.CoreAudioApi
Imports Open.Nat

Public Class PageLinkDirect
    Public Const RequestVersion As Char = "2"

    '记录的启动情况
    Public Shared IsServerSide As Boolean
    Private Shared HostIp As String
    Private Shared HostPort As Integer

#Region "初始化"

    '加载器初始化
    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, InitLoader, AutoRun:=False)
        '注册自定义的 OnStateChanged
        AddHandler InitLoader.OnStateChangedUi, AddressOf OnLoadStateChanged
    End Sub

    Private IsLoad As Boolean = False
    Private Sub OnLoaded() Handles Me.Loaded
        'FormMain.EndProgramForce(Result.Aborted)
        If IsLoad Then Exit Sub
        IsLoad = True
        '启动监视线程
        If Not IsWatcherStarted Then RunInNewThread(AddressOf WatcherThread, "Hiper Watcher")
    End Sub

#End Region

#Region "加载步骤"

    Public Shared WithEvents InitLoader As New LoaderCombo(Of Integer)("HiPer 初始化", {
        New LoaderTask(Of Integer, Integer)("检查网络环境", AddressOf InitCheck) With {.ProgressWeight = 0.5}
    })
    Private Shared Sub InitCheck(Task As LoaderTask(Of Integer, Integer))
    End Sub
    '启动联机模块
    Private Shared Async Function InitLaunch() As Tasks.Task
        Dim worlds = Await ModLink.MCInstanceFinding()
        If Not worlds.Any Then
            Hint("没有找到可用的联机世界")
            Exit Function
        End If
        Hint($"找到了 {worlds.Count} 个世界")
    End Function
    Private Shared PingNodes As Integer, AllNodes As List(Of String)

#End Region

#Region "进程管理"

    Private Shared _HiperState As LoadState = LoadState.Waiting
    Public Shared Property HiperState As LoadState
        Get
            Return _HiperState
        End Get
        Set(value As LoadState)
            _HiperState = value
            RunInUi(Sub() If FrmLinkLeft IsNot Nothing Then CType(FrmLinkLeft.ItemDirect.Buttons(0), MyIconButton).Visibility = If(HiperState = LoadState.Finished OrElse HiperState = LoadState.Loading, Visibility.Visible, Visibility.Collapsed))
        End Set
    End Property

    Private Shared HiperIp As String = Nothing
    Private Shared HiperProcessId As Integer = -1, McbProcessId As Integer = -1
    Private Shared HiperCertTime As Date = Date.Now

    ''' <summary>
    ''' 启动程序，并等待初始化完成后退出运行，同时更新 HiperIp。
    ''' 若启动失败，则会直接抛出异常。
    ''' 若程序正在运行，则会先停止其运行。
    ''' </summary>
    Public Shared Sub HiperStart(Task As LoaderTask(Of Integer, Integer))
    End Sub

    'Hiper 日志
    Private Shared Sub HiperLogLine(Content As String, Task As LoaderTask(Of Integer, Integer))
    End Sub
    Private Shared PossibleFailReason As String = Nothing

#End Region

#Region "监视线程"

    '主 Timer 线程
    Private IsWatcherStarted As Boolean = False
    Private Sub WatcherThread()
        Dim Sec15 As Integer = 0
        Do While True
            Try
                For i = 1 To 5
                    Thread.Sleep(200)
                    If InitLoader.State = LoadState.Loading Then
                        RunInUi(AddressOf UpdateProgress)
                    End If
                Next
                Thread.Sleep(1000)
                Sec15 += 1
                WatcherTimer1()
                If Sec15 = 15 Then
                    Sec15 = 0
                    WatcherTimer15()
                End If
            Catch ex As Exception
                Log(ex, "联机模块主时钟出错", LogLevel.Feedback)
                Thread.Sleep(20000)
            End Try
        Loop
    End Sub

    '每 1 秒执行的 Timer
    Private Sub WatcherTimer1()
        If HiperState <> LoadState.Finished Then Exit Sub
        RunInUi(Sub()
                    '网络质量
                    Dim QualityScore As Integer = 0
                    QualityScore -= Math.Ceiling((Math.Min(0, 600) + Math.Min(PingNodes, 600)) / 80)
                    Select Case QualityScore
                        Case Is >= -1
                            LabFinishQuality.Text = "优秀"
                        Case Is >= -2
                            LabFinishQuality.Text = "优良"
                        Case Is >= -3
                            LabFinishQuality.Text = "良好"
                        Case Is >= -5
                            LabFinishQuality.Text = "一般"
                        Case Is >= -7
                            LabFinishQuality.Text = "较差"
                        Case Else
                            LabFinishQuality.Text = "很差"
                    End Select
                    'Ping
                    If HostPing <> -1 Then
                        If FrmLinkDirect IsNot Nothing AndAlso FrmLinkDirect.LabFinishPing.IsLoaded Then
                            FrmLinkDirect.LabFinishPing.Text = HostPing & "ms"
                        End If
                    End If
                End Sub)
    End Sub
    '每 15 秒执行的 Timer
    Private Shared HostPing As Integer = -1
    Private Sub WatcherTimer15()
    End Sub

#End Region

#Region "PanSelect | 种类选择页面"

    Private Property _LoadingWorldInfo As Boolean = False
    '创建房间
    Private Async Sub BtnSelectCreate_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnSelectCreate.MouseLeftButtonUp
        If _LoadingWorldInfo Then
            Hint("加载中，请稍候……")
            Exit Sub
        End If
        _LoadingWorldInfo = True
        Await InitLaunch()
        _LoadingWorldInfo = False
    End Sub
    Private Sub RoomCreate(Port As Integer)
        '记录信息
        HostIp = Nothing : HostPort = Port
        IsServerSide = True
        '启动
        InitLoader.Start(IsForceRestart:=True)
    End Sub

    '加入房间
    Private Sub BtnSelectJoin_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnSelectJoin.MouseLeftButtonUp
        MyMsgBoxInput("输入大厅编号", HintText:="例如：01509230")
    End Sub
    Private Sub RoomJoin(Ip As String, Port As Integer)
        '记录信息
        HostIp = Ip : HostPort = Port
        IsServerSide = False
        '启动
        InitLoader.Start(IsForceRestart:=True)
    End Sub

#End Region

#Region "PanLoad | 加载中页面"

    '承接状态切换的 UI 改变
    Private Sub OnLoadStateChanged(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
    End Sub
    Private Shared LoadStep As String = "准备初始化"
    Private Shared Sub SetLoadDesc(Intro As String, [Step] As String)
        Log("[Hiper] 连接步骤：" & Intro)
        LoadStep = [Step]
        RunInUiWait(Sub()
                        If FrmLinkDirect Is Nothing OrElse Not FrmLinkDirect.LabLoadDesc.IsLoaded Then Exit Sub
                        FrmLinkDirect.LabLoadDesc.Text = Intro
                        FrmLinkDirect.UpdateProgress()
                    End Sub)
    End Sub

    '承接重试
    Private Sub CardLoad_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles CardLoad.MouseLeftButtonUp
        If Not InitLoader.State = LoadState.Failed Then Exit Sub
        InitLoader.Start(IsForceRestart:=True)
    End Sub

    '取消加载
    Private Sub CancelLoad() Handles BtnLoadCancel.Click
        If InitLoader.State = LoadState.Loading Then
            CurrentSubpage = Subpages.PanSelect
            InitLoader.Abort()
        Else
            InitLoader.State = LoadState.Waiting
        End If
    End Sub

    '进度改变
    Private Sub UpdateProgress(Optional Value As Double = -1)
        If Value = -1 Then Value = InitLoader.Progress
        Dim DisplayingProgress As Double = ColumnProgressA.Width.Value
        If Math.Round(Value - DisplayingProgress, 3) = 0 Then Exit Sub
        If DisplayingProgress > Value Then
            ColumnProgressA.Width = New GridLength(Value, GridUnitType.Star)
            ColumnProgressB.Width = New GridLength(1 - Value, GridUnitType.Star)
            AniStop("Hiper Progress")
        Else
            Dim NewProgress As Double = If(Value = 1, 1, (Value - DisplayingProgress) * 0.2 + DisplayingProgress)
            AniStart({
                AaGridLengthWidth(ColumnProgressA, NewProgress - ColumnProgressA.Width.Value, 300, Ease:=New AniEaseOutFluent),
                AaGridLengthWidth(ColumnProgressB, (1 - NewProgress) - ColumnProgressB.Width.Value, 300, Ease:=New AniEaseOutFluent)
            }, "Hiper Progress")
        End If
    End Sub
    Private Sub CardResized() Handles CardLoad.SizeChanged
        RectProgressClip.Rect = New Rect(0, 0, CardLoad.ActualWidth, 12)
    End Sub

#End Region

#Region "PanFinish | 加载完成页面"

    '复制 IP
    Private Sub BtnFinishIp_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnFinishIp.MouseLeftButtonUp
        ClipboardSet(LabFinishIp.Text)
    End Sub

    '退出
    Private Sub BtnFinishExit_Click(sender As Object, e As EventArgs) Handles BtnFinishExit.Click
        If IsServerSide AndAlso MyMsgBox("你确定要关闭联机房间吗？", "确认退出", "确定", "取消", IsWarn:=True) = 2 Then Exit Sub
    End Sub

    '复制联机码
    Private Sub BtnFinishCopy_Click(sender As Object, e As EventArgs) Handles BtnFinishCopy.Click
    End Sub

    'Ping 房主
    Private Sub BtnFinishPing_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnFinishPing.MouseLeftButtonUp
        LabFinishPing.Text = "检测中"
        If TaskPingHost.State = LoadState.Loading Then Exit Sub
        TaskPingHost.Start(True, IsForceRestart:=True)
    End Sub
    Private Shared TaskPingHost As New LoaderTask(Of Boolean, Integer)("HiPer Ping Host",
    Sub(Task As LoaderTask(Of Boolean, Integer))
        HostPing = -1
        HostPing = Ping(HostIp, 5000, Task.Input)
    End Sub)

#End Region

#Region "子页面管理"

    Public Enum Subpages
        PanSelect
        PanFinish
    End Enum
    Private _CurrentSubpage As Subpages = Subpages.PanSelect
    Public Property CurrentSubpage As Subpages
        Get
            Return _CurrentSubpage
        End Get
        Set(value As Subpages)
            If _CurrentSubpage = value Then Exit Property
            _CurrentSubpage = value
            Log("[Hiper] 子页面更改为 " & GetStringFromEnum(value))
            PageOnContentExit()
        End Set
    End Property

    Private Sub PageLinkHiper_OnPageEnter() Handles Me.PageEnter
        FrmLinkDirect.PanSelect.Visibility = If(CurrentSubpage = Subpages.PanSelect, Visibility.Visible, Visibility.Collapsed)
        FrmLinkDirect.PanFinish.Visibility = If(CurrentSubpage = Subpages.PanFinish, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Shared Sub HiperExit(ExitToCertPage As Boolean)
        Log("[Hiper] 要求退出 Hiper（当前加载器状态为 " & GetStringFromEnum(InitLoader.State) & "）")
        If InitLoader.State = LoadState.Loading Then InitLoader.Abort()
        If InitLoader.State = LoadState.Failed Then InitLoader.State = LoadState.Waiting
        RunInUi(Sub()
                    If FrmLinkDirect Is Nothing OrElse Not FrmLinkDirect.IsLoaded Then Exit Sub
                    FrmLinkDirect.CurrentSubpage = Subpages.PanSelect
                    FrmLinkDirect.PageOnContentExit()
                End Sub)
    End Sub

#End Region

End Class
