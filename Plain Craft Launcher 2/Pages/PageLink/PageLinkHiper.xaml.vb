Public Class PageLinkHiper
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
        FormMain.EndProgramForce(Result.Aborted)
        If IsLoad Then Exit Sub
        IsLoad = True
        '启动监视线程
        If Not IsWatcherStarted Then RunInNewThread(AddressOf WatcherThread, "Hiper Watcher")
        '读取索引码
        Try
            Dim Time As String = Setup.Get("LinkHiperCertTime")
            If Time = "" Then
                Log("[HiPer] 没有缓存凭证")
            ElseIf Date.Parse(Time) > Date.Now Then
                TextCert.Text = Setup.Get("LinkHiperCertLast")
                Log("[HiPer] 缓存凭证尚未过期：" & Time)
                CurrentSubpage = Subpages.PanSelect
            Else
                Log("[HiPer] 缓存凭证已过期：" & Time)
                LabCertTitle.Text = "输入索引码"
                LabCertDesc.Text = "你的 HiPer 索引码已经过期，请输入新的索引码。" & vbCrLf & "如果实在没有索引码，可以在左侧选择 IOI 方式联机。"
            End If
        Catch ex As Exception
            Log(ex, "读取缓存凭证失败")
            Setup.Set("LinkHiperCertTime", "")
        End Try
    End Sub

#End Region

#Region "加载步骤"

    Public Shared PathHiper As String = PathAppdata & "联机模块\"
    Public Shared WithEvents InitLoader As New LoaderCombo(Of Integer)("HiPer 初始化", {
        New LoaderTask(Of Integer, Integer)("网络环境：连通检测", AddressOf InitPingCheck) With {.Block = False, .ProgressWeight = 0.5},
        New LoaderTask(Of Integer, Integer)("网络环境：IP 检测", AddressOf InitIpCheck) With {.Block = False, .ProgressWeight = 1},
        New LoaderTask(Of Integer, Integer)("检查网络环境", AddressOf InitCheck) With {.ProgressWeight = 0.5},
        New LoaderTask(Of Integer, List(Of NetFile))("获取所需文件", AddressOf InitGetFile) With {.ProgressWeight = 4},
        New LoaderDownload("下载所需文件", New List(Of NetFile)) With {.ProgressWeight = 4},
        New LoaderTask(Of Integer, Integer)("启动联机模块", AddressOf InitLaunch) With {.ProgressWeight = 7}
    })

    '检查网络状态
    Private Shared Sub InitPingCheck(Task As LoaderTask(Of Integer, Integer))
        PingTime = 0
        Try
            Log("[HiPer] 网络检测：连通检测开始")
            Dim StartTime = Date.Now.Ticks
            Dim Result As String = NetGetCodeByClient("https://www.baidu.com/duty/", Encoding.UTF8, 20000, "")
            Dim DuringTime As Integer = (Date.Now.Ticks - StartTime) / 10000
            If Result.Contains("百度") Then
                Log("[HiPer] 网络检测：连通检测成功（" & DuringTime & "ms）")
                PingTime = DuringTime
            Else
                Log("[HiPer] 网络检测：连通检测失败（获取的内容有误）")
                PingTime = -1
            End If
        Catch ex As ThreadInterruptedException
        Catch ex As Exception
            Log(ex, "连通检测失败")
            PingTime = -1
        End Try
    End Sub
    Private Shared Sub InitIpCheck()
        IpCheckStatus = LoadState.Loading
        Try
            Log("[HiPer] 网络检测：IP 地址检测开始")
            Dim IpCheckResult As String = NetRequestOnce("https://ipinfo.io/json", "GET", "", "application/json", 10000)
            Dim Country As String = GetJson(IpCheckResult)("country")
            IpIsInChina = Country = "CN"
            IpCheckStatus = LoadState.Finished
            Log("[HiPer] 网络检测：IP 地址检测结果：" & Country)
        Catch ex As ThreadInterruptedException
            IpCheckStatus = LoadState.Aborted
        Catch ex As Exception
            Log(ex, "IP 地址检测失败")
            IpCheckStatus = LoadState.Failed
            IpIsInChina = True
        End Try
    End Sub
    Private Shared PingTime As Integer, IpCheckStatus As LoadState = LoadState.Loading, IpIsInChina As Boolean
    Private Shared Sub InitCheck(Task As LoaderTask(Of Integer, Integer))
        '检查协议
        If Not Setup.Get("LinkEula") Then
Reopen:
            Select Case MyMsgBox("PCL 的联机服务由速聚授权提供。" & vbCrLf & "在使用前，你需要同意速聚的用户服务协议和隐私政策。", "协议授权", "同意", "拒绝", "查看用户服务协议和隐私政策")
                Case 1
                    Setup.Set("LinkEula", True)
                Case 2
                    Throw New Exception("$你拒绝了用户服务协议……")
                Case 3
                    OpenWebsite("https://mp.weixin.qq.com/mp/appmsgalbum?__biz=MzkxMTMyODk3Mg==&action=getalbum&album_id=2585385685407514625&scene=173&from_msgid=2247483720&from_itemidx=1&count=3&nolastread=1#wechat_redirect")
                    GoTo Reopen
            End Select
        End If
        '等待网络环境检查加载器结束
        SetLoadDesc("正在检查网络环境……", "检查网络环境")
        Do While Task.State = LoadState.Loading AndAlso Not (PingTime <> 0 AndAlso IpCheckStatus <> LoadState.Loading)
            Thread.Sleep(50)
        Loop
        '获取网络环境检查结果
        If IpCheckStatus = LoadState.Finished AndAlso Not IpIsInChina Then
            If MyMsgBox("检测到你的 IP 不在中国，这会导致联机变得非常不稳定。" & vbCrLf &
                        "如果你正开着加速器或者 VPN，请先关闭它们，然后再继续……", "警告", "继续", "取消", IsWarn:=True, ForceWait:=True) = 2 Then
                Throw New Exception("$请在关闭加速器或者 VPN 后点击重试。")
            End If
            Task.Progress = 0.5
            InitIpCheck() '重新进行检查
        End If
        '没有联网
        If IpCheckStatus = LoadState.Failed AndAlso PingTime = -1 Then
            Throw New Exception("$PCL 没法连上网……" & vbCrLf & "如果你改变了网络环境，请重启 PCL。")
        End If
    End Sub

    '获取所需文件
    Private Shared Sub InitGetFile(Task As LoaderTask(Of Integer, List(Of NetFile)))
        '初始化
        SetLoadDesc("正在初始化……", "初始化")
        Directory.CreateDirectory(PathHiper)
        WriteFile(PathHiper & "HiPer 联机模块.vbs", "createobject(""wscript.shell"").run """"""" & PathHiper & "HiPer 联机模块.exe"""" v"", 0", Encoding:=Encoding.GetEncoding("GB18030")) '准备启动脚本
        '获取索引码文件
        SetLoadDesc("正在获取索引码文件……", "获取索引码文件")
        Dim Cert As String = RunInUiWait(Function() FrmLinkHiper.TextCert.Text)
        Log("[Hiper] 联机索引码：" & Cert)
        If Cert = Setup.Get("LinkHiperCertLast") AndAlso File.Exists(PathHiper & "cert.yml") Then
            Log("[Hiper] 索引码与上次输入的一致，跳过获取步骤")
        Else
            Dim CertRaw As String
            Try
                CertRaw = NetRequestOnce("https://cert.mcer.cn/" & Cert & ".yml", "GET", "", "")
            Catch ex As Exception
                If GetExceptionSummary(ex).Contains("(404)") Then
                    Throw New CertOutdatedException '索引码无效或已过期
                ElseIf GetExceptionSummary(ex).Contains("too many requests") Then
                    Throw New Exception("你的尝试太频繁了，请暂时啥都别点，等两分钟后再试……")
                Else
                    Throw
                End If
            End Try
            If Not CertRaw.Contains("WARNING <<< AUTO SYNC AREA") Then Throw New Exception("获取到的索引码文件内容有误！")
            WriteFile(PathHiper & "cert.yml", CertRaw)
            Setup.Set("LinkHiperCertLast", Cert)
            Setup.Set("LinkHiperCertTime", "")
        End If
        Task.Progress = 0.25
        '获取 CPU 架构
        SetLoadDesc("正在获取 CPU 架构……", "获取 CPU 架构")
        Dim Architecture As String
        Select Case GetType(String).Assembly.GetName().ProcessorArchitecture
            Case Reflection.ProcessorArchitecture.X86
                Architecture = "386"
            Case Reflection.ProcessorArchitecture.Amd64
                Architecture = "amd64"
            Case Reflection.ProcessorArchitecture.Arm
                Architecture = "arm64"
            Case Else
                Architecture = "arm64"
                Log("[Hiper] 当前 CPU 架构为 " & GetStringFromEnum(GetType(String).Assembly.GetName().ProcessorArchitecture) & "，没有最适合的项，这可能会导致联机模块无法启动！", LogLevel.Debug)
        End Select
        Log("[Hiper] CPU 架构：" & Architecture)
        '检查更新：hiper.exe
        SetLoadDesc("正在检查联机模块本体更新……", "检查联机模块本体更新")
        Dim RequiredFiles As New List(Of NetFile)
        Dim Checksums As String = Nothing, ChecksumHiper As String = Nothing
        If File.Exists(PathHiper & "HiPer 联机模块.exe") Then
            Try
                Checksums = NetGetCodeByDownload({
                    "http://mirror.hiper.cn.s2.the.bb/packages.sha1",
                    "http://mirror.hiper.cn.s3.the.bb:175/packages.sha1",
                    "https://gitcode.net/to/hiper/-/raw/master/packages.sha1",
                    "https://cert.mcer.cn/mirror/packages.sha1"
                })
            Catch ex As Exception
                Log(ex, "获取联机模块更新信息失败，将会强制启动联机模块", LogLevel.Hint)
                GoTo FinishHiperFileCheck
            End Try
            ChecksumHiper = RegexSeek(Checksums, "[0-9a-f]{40}(?=  windows-" & Architecture & "/hiper.exe)")
            If ChecksumHiper Is Nothing Then
                Log("[Hiper] 未找到联机模块更新信息，将会强制启动联机模块", LogLevel.Hint)
                GoTo FinishHiperFileCheck
            End If
            Log("[Hiper] hiper.exe 的所需 SHA1：" & ChecksumHiper)
            If ChecksumHiper = GetAuthSHA1(PathHiper & "HiPer 联机模块.exe") Then
                Log("[Hiper] hiper.exe 文件校验通过，无需重新下载")
                GoTo FinishHiperFileCheck
            End If
        End If
        Log("[Hiper] 需要重新下载 hiper.exe")
        RequiredFiles.Add(New NetFile({"http://mirror.hiper.cn.s2.the.bb/windows-" & Architecture & "/hiper.exe",
                                       "http://mirror.hiper.cn.s3.the.bb:175/windows-" & Architecture & "/hiper.exe",
                                       "https://gitcode.net/to/hiper/-/raw/master/windows-" & Architecture & "/hiper.exe?inline=false",
                                       "https://cert.mcer.cn/mirror/windows-" & Architecture & "/hiper.exe"},
                                      PathHiper & "HiPer 联机模块.exe",
                                      New FileChecker(Hash:=ChecksumHiper, MinSize:=1024 * 200))) 'Hash 的默认值即为 Nothing，所以 Checksum 可以为 Nothing
FinishHiperFileCheck:
        Task.Progress = 0.5
        '检查更新：wintun.dll
        Dim ChecksumWintun As String = Nothing
        If File.Exists(PathHiper & "wintun.dll") Then
            Try
                If Checksums Is Nothing Then
                    Checksums = NetGetCodeByDownload({
                        "http://mirror.hiper.cn.s2.the.bb/packages.sha1",
                        "http://mirror.hiper.cn.s3.the.bb:175/packages.sha1",
                        "https://gitcode.net/to/hiper/-/raw/master/packages.sha1",
                        "https://cert.mcer.cn/mirror/packages.sha1"
                    })
                End If
            Catch ex As Exception
                Log(ex, "获取联机模块更新信息失败，将会强制启动联机模块", LogLevel.Hint)
                GoTo FinishWintunFileCheck
            End Try
            ChecksumWintun = RegexSeek(Checksums, "[0-9a-f]{40}(?=  windows-" & Architecture & "/wintun.dll)")
            If ChecksumWintun Is Nothing Then
                Log("[Hiper] 未找到联机模块更新信息，将会强制启动联机模块", LogLevel.Hint)
                GoTo FinishWintunFileCheck
            End If
            Log("[Hiper] wintun.dll 的所需 SHA1：" & ChecksumWintun)
            If ChecksumWintun = GetAuthSHA1(PathHiper & "wintun.dll") Then
                Log("[Hiper] wintun.dll 文件校验通过，无需重新下载")
                GoTo FinishWintunFileCheck
            End If
        End If
        Log("[Hiper] 需要重新下载 wintun.dll")
        RequiredFiles.Add(New NetFile({"http://mirror.hiper.cn.s2.the.bb/windows-" & Architecture & "/wintun.dll",
                                       "http://mirror.hiper.cn.s3.the.bb:175/windows-" & Architecture & "/wintun.dll",
                                       "https://gitcode.net/to/hiper/-/raw/master/windows-" & Architecture & "/wintun.dll?inline=false",
                                       "https://cert.mcer.cn/mirror/windows-" & Architecture & "/wintun.dll"},
                                      PathHiper & "wintun.dll",
                                      New FileChecker(Hash:=ChecksumWintun, MinSize:=1024 * 200))) 'Hash 的默认值即为 Nothing，所以 Checksum 可以为 Nothing
FinishWintunFileCheck:
        Task.Progress = 0.75
        '检查更新：MCB
        If Not IsServerSide Then
            SetLoadDesc("正在检查联机模块组件更新……", "检查联机模块组件更新")
            Dim ChecksumMcb As String = Nothing
            If File.Exists(PathHiper & "MCB 联机模块.exe") Then
                Try
                    Checksums = NetGetCodeByDownload({
                        "http://mirror.hiper.cn.s2.the.bb/utils/minecraft-broadcast/packages.sha1",
                        "http://mirror.hiper.cn.s3.the.bb:175/utils/minecraft-broadcast/packages.sha1",
                        "https://gitcode.net/to/hiper/-/raw/master/utils/minecraft-broadcast/packages.sha1",
                        "https://cert.mcer.cn/mirror/utils/minecraft-broadcast/packages.sha1"
                    })
                Catch ex As Exception
                    Log(ex, "获取联机模块组件更新信息失败，将会强制启动联机模块组件", LogLevel.Hint)
                    GoTo FinishMcbFileCheck
                End Try
                ChecksumMcb = RegexSeek(Checksums, "[0-9a-f]{40}(?=  mcb-windows-" & Architecture & ".exe)")
                If ChecksumMcb Is Nothing Then
                    Log("[Hiper] 未找到联机模块组件更新信息，将会强制启动联机模块组件", LogLevel.Hint)
                    GoTo FinishMcbFileCheck
                End If
                Log("[Hiper] mcb.exe 的所需 SHA1：" & ChecksumMcb)
                If ChecksumMcb = GetAuthSHA1(PathHiper & "MCB 联机模块.exe") Then
                    Log("[Hiper] mcb.exe 文件校验通过，无需重新下载")
                    GoTo FinishMcbFileCheck
                End If
            End If
            Log("[Hiper] 需要重新下载 mcb.exe")
            RequiredFiles.Add(New NetFile({
                                       "http://mirror.hiper.cn.s2.the.bb/utils/minecraft-broadcast/mcb-windows-" & Architecture & ".exe",
                                       "http://mirror.hiper.cn.s3.the.bb:175/utils/minecraft-broadcast/mcb-windows-" & Architecture & ".exe",
                                       "https://gitcode.net/to/hiper/-/raw/master/utils/minecraft-broadcast/mcb-windows-" & Architecture & ".exe?inline=false",
                                       "https://cert.mcer.cn/mirror/utils/minecraft-broadcast/mcb-windows-" & Architecture & ".exe"},
                                      PathHiper & "MCB 联机模块.exe",
                                      New FileChecker(Hash:=ChecksumMcb, MinSize:=1024 * 200))) 'Hash 的默认值即为 Nothing，所以 Checksum 可以为 Nothing
FinishMcbFileCheck:
        End If
        '添加 point.yml
        RequiredFiles.Add(New NetFile({"https://cert.mcer.cn/point.yml"}, PathHiper & "point.yml", New FileChecker(MinSize:=200)))
        '开始下载
        SetLoadDesc("正在下载联机模块……", "下载联机模块")
        Task.Output = RequiredFiles
    End Sub
    Public Class CertOutdatedException
        Inherits Exception
    End Class
    '启动联机模块
    Private Shared Sub InitLaunch(Task As LoaderTask(Of Integer, Integer))
        '关闭运行中的 HiPer，然后再刷新凭证文件
        SetLoadDesc("正在关闭运行中的联机模块……", "关闭运行中的联机模块")
        HiperStop(True)
        '准备凭证文件
        SetLoadDesc("正在准备索引码文件……", "准备索引码文件")
        Dim CertFile As String = ReadFile(PathHiper & "cert.yml")
        '添加防火墙配置
        If IsServerSide Then
            CertFile += ("\n\ninbound:\n  port: " & HostPort & "\n  proto: tcp\n  host: any").Replace("\n", vbCrLf)
        Else
            CertFile += ("\n\noutbound:\n  port: " & HostPort & "\n  proto: tcp\n  host: " & HostIp).Replace("\n", vbCrLf)
        End If
        CertFile += "\nlogging:\n  format: json".Replace("\n", vbCrLf)
        '更新 point.yml 段
        Const SyncAreaRegex As String = "(?<=#[ ]*WARNING >>> AUTO SYNC AREA)[\s\S]+?(?=#[ ]*WARNING <<< AUTO SYNC AREA)"
        CertFile = CertFile.Replace(RegexSeek(CertFile, SyncAreaRegex), RegexSeek(ReadFile(PathHiper & "point.yml"), SyncAreaRegex))
        WriteFile(PathHiper & "config.yml", CertFile)
        AllNodes = RegexSearch(CertFile.Replace(vbLf, "cr"), "(?<="")[0-9]+.[0-9]+.[0-9]+.[0-9]+(?=""( )?:( )?cr    - "")").Distinct.
                   Where(Function(l) Not (l = "6.6.1.1" OrElse l = "6.6.2.2" OrElse l = "6.6.3.3")).ToList
        If AllNodes.Count < 2 Then Throw New Exception("$索引码文件格式有误，未找到节点 IP！")
        Task.Progress = 0.05
        '尝试启动
        HiperStart(Task) '启动失败会直接抛出异常
        Task.Progress = 0.2
        '检查连接情况
        SetLoadDesc("正在连接到联机节点 (1/2)……" & If(IpIsInChina, "", vbCrLf & "由于网络不在中国，可能要花费很长时间。"), "检查联机节点")
        If Not IsServerSide AndAlso HostIp = HiperIp Then Throw New Exception("$还搁这自己连自己？！不是吧？！")
        Dim PingNodeResults As New List(Of Integer)
        Dim UnpingedNodes As New List(Of String)(AllNodes)
        Do Until PingNodeResults.Count >= 2 OrElse Task.IsAborted
            Dim SelectedNode = RandomOne(UnpingedNodes)
            Dim PingNode = Ping(SelectedNode, 4000)
            If PingNode = -1 Then
                Task.Progress += (If(PingNodeResults.Count = 0, 0.58, 0.88) - Task.Progress) * 0.3
            Else
                SetLoadDesc("正在连接到联机节点 (2/2)……" & If(IpIsInChina, "", vbCrLf & "由于网络不在中国，可能要花费很长时间。"), "检查联机节点")
                Task.Progress = 0.6
                PingNodeResults.Add(PingNode)
                UnpingedNodes.Remove(SelectedNode)
            End If
        Loop
        PingNodes = PingNodeResults.Min
        Task.Progress = 0.9
        '与房主 Ping 一下看看
        If Not IsServerSide Then
            SetLoadDesc("正在连接到房主……", "检查房主")
            HostPing = -1 '实际上不使用本次 Ping 的结果，因为它真的偏大……
            Do
                Dim PingHost = Ping(HostIp, 4000, False)
                If PingHost = -1 Then
                    Thread.Sleep(500)
                Else
                    Exit Do
                End If
            Loop Until Task.IsAborted
            '在等待 2 秒后再次触发 Ping，以刷新显示的 Ping
            RunInNewThread(Sub()
                               Thread.Sleep(2000)
                               TaskPingHost.Start(True, IsForceRestart:=True)
                           End Sub, "HiPer Delayed Ping")
        End If
        '刷新完成页面
        SetLoadDesc("正在加载完成页面……", "加载完成页面")
        RunInUiWait(AddressOf FrmLinkHiper.WatcherTimer1)
    End Sub
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
            RunInUi(Sub() If FrmLinkLeft IsNot Nothing Then CType(FrmLinkLeft.ItemHiper.Buttons(0), MyIconButton).Visibility = If(HiperState = LoadState.Finished OrElse HiperState = LoadState.Loading, Visibility.Visible, Visibility.Collapsed))
        End Set
    End Property
    Public Shared Sub ModuleStopManually() '关闭联机模块按钮
        HiperExit(False)
    End Sub

    Private Shared HiperIp As String = Nothing
    Private Shared HiperProcessId As Integer = -1, McbProcessId As Integer = -1
    Private Shared HiperCertTime As Date = Date.Now

    ''' <summary>
    ''' 若程序正在运行，则结束程序进程，同时初始化状态数据。返回是否关闭了相关进程。
    ''' </summary>
    Public Shared Function HiperStop(SleepWhenKilled As Boolean) As Boolean
        Return False
        HiperStop = False
        '修改凭证
        Dim ConfigContent As String = ReadFile(PathHiper & "config.yml")
        If Not ConfigContent.Contains("enable: false") Then
            WriteFile(PathHiper & "config.yml", ConfigContent & vbCrLf & "enable: false")
        End If
        '关闭所有进程
        For Each ProcessObject In Process.GetProcesses
            Dim IsHiper As Boolean = ProcessObject.ProcessName = "HiPer 联机模块"
            Dim IsMcb As Boolean = ProcessObject.ProcessName = "MCB 联机模块"
            If Not IsHiper AndAlso Not IsMcb Then Continue For
            HiperStop = True
            Try
                If IsMcb Then
                    ProcessObject.Kill()
                    Log("[HiPer] 已结束进程 PID " & ProcessObject.Id & "：" & ProcessObject.ProcessName)
                    If HiperStop AndAlso SleepWhenKilled Then Thread.Sleep(1000) '等待 1 秒确认进程已退出
                Else 'IsHiper
                    If SleepWhenKilled Then
                        Thread.Sleep(4000) '等待 4 秒确认进程已退出
                        If Not ProcessObject.HasExited Then
                            ProcessObject.Kill()
                            Log("[HiPer] 已结束进程 PID " & ProcessObject.Id & "：" & ProcessObject.ProcessName)
                        End If
                    End If
                End If
            Catch ex As Exception
                Log(ex, "结束进程失败（" & ProcessObject.Id & "，" & ProcessObject.ProcessName & "）")
            End Try
        Next
        '初始化
        HiperState = LoadState.Waiting
        HiperProcessId = -1 : McbProcessId = -1
        HiperCertTime = Nothing
    End Function
    ''' <summary>
    ''' 启动程序，并等待初始化完成后退出运行，同时更新 HiperIp。
    ''' 若启动失败，则会直接抛出异常。
    ''' 若程序正在运行，则会先停止其运行。
    ''' </summary>
    Public Shared Sub HiperStart(Task As LoaderTask(Of Integer, Integer))
        Try
            SetLoadDesc("正在启动联机模块……", "启动联机模块")
            PossibleFailReason = Nothing
            '启动 Hiper
            Log("[Hiper] 启动 Hiper 进程")
            DeleteDirectory(PathHiper & "logs") '清理日志
            Dim HiperInfo = New ProcessStartInfo With {
                .FileName = "wscript",
                .Arguments = """" & PathHiper & "HiPer 联机模块.vbs""",
                .Verb = "runas" '需要管理员权限
            }
            Dim HiperVbsProcess As New Process() With {.StartInfo = HiperInfo}
            HiperVbsProcess.Start()
            '查找真正的 Hiper 进程
            SetLoadDesc("联机模块正在启动……", "联机模块加载")
            Do
                Dim GotProcesses = Process.GetProcesses.
                    Where(Function(l) l.ProcessName = "HiPer 联机模块" AndAlso Math.Abs((l.StartTime - Date.Now).TotalMinutes) < 1)
                If GotProcesses.Count > 0 Then
                    HiperProcessId = GotProcesses.First.Id
                    Log("[Hiper] 已发现 Hiper 进程，PID：" & HiperProcessId)
                    Exit Do
                End If
                Thread.Sleep(50)
            Loop While Task.State = LoadState.Loading AndAlso Not Task.IsAborted
            If HiperProcessId = -1 Then Throw New Exception("联机模块未能成功启动！")
            HiperState = LoadState.Loading
            Task.Progress = 0.15
            '抓取日志
            Dim LogLine As Integer = 0
            Do
                Thread.Sleep(100)
                If Not File.Exists(PathHiper & "logs\hiper.log") Then Continue Do
                Dim LogLines As String() = ReadFile(PathHiper & "logs\hiper.log").TrimEnd(vbLf).Split(vbLf)
                For i = LogLine To LogLines.Count - 1
                    HiperLogLine(LogLines(i), Task)
                Next
                LogLine = LogLines.Count
            Loop While HiperState = LoadState.Loading AndAlso Not Task.IsAborted AndAlso Process.GetProcesses.Any(Function(l) l.Id = HiperProcessId)
            '输出
            If HiperState = LoadState.Finished Then
                Log("[Hiper] Hiper 启动完成")
            ElseIf PossibleFailReason IsNot Nothing AndAlso PossibleFailReason.Contains("索引码已过期") Then
                Throw New CertOutdatedException()
            Else
                Throw New Exception(If(PossibleFailReason, "联机模块因未知原因启动失败！"))
            End If
            '启动 MCB
            Task.Progress = 0.25
            If Not IsServerSide Then
                Try
                    Log("[Hiper] 启动 MCB 进程")
                    Dim McbInfo = New ProcessStartInfo With {
                        .FileName = PathHiper & "MCB 联机模块.exe", .WorkingDirectory = PathHiper,
                        .UseShellExecute = False, .CreateNoWindow = True,
                        .RedirectStandardError = True, .RedirectStandardOutput = True,
                        .Arguments = "-addr " & HostIp & ":" & HostPort & " -motd ""PCL 联机房间"""
                    }
                    Dim McbProcess As New Process() With {.StartInfo = McbInfo}
                    McbProcess.Start()
                    McbProcessId = McbProcess.Id
                Catch ex As Exception
                    Throw New Exception("联机模块组件启动失败", ex)
                End Try
            End If
        Catch ex As Exception
            Try
                HiperStop(True) '由于启动失败停止进程
            Catch
            End Try
            HiperState = LoadState.Failed
            Throw
        End Try
    End Sub

    'Hiper 日志
    Private Shared Sub HiperLogLine(Content As String, Task As LoaderTask(Of Integer, Integer))

        '检查报错
        Dim ContentTest As String = Content.ToLower
        Dim ErrorMessage As String = Nothing
        Dim ContentJson As JObject = Nothing
        If Content.StartsWith("{""") AndAlso Content.EndsWith("}") Then
            ContentJson = GetJson(Content)
            ContentTest = If(ContentJson("error"), ContentJson("msg")).ToString.ToLower
            ErrorMessage = ContentJson("error")
        End If
        If ContentTest.Contains("hiper certificate for this host is expired") Then
            PossibleFailReason = "$你的索引码已过期，请更换新的索引码！"
        ElseIf ContentTest.Contains("error creating interface: access is denied") Then
            PossibleFailReason = "$没有获得管理员权限，无法创建网络通道！"
        ElseIf ContentTest.Contains("cannot create a file when that file already exists") Then
            PossibleFailReason = "$请不要重复开启多个联机模块！"
        ElseIf Content.Contains("failed to load config") Then
            PossibleFailReason = "$索引码文件内容存在错误！"
        ElseIf Content.Contains("system cannot find the file specified") Then
            PossibleFailReason = "$创建网络通道失败！" & vbCrLf & "如果你曾启动过 VPN 软件，请先打开那个软件，然后关掉它，最后再重试。虽然不知道为啥，但这样大概管用……"
        End If
        If PossibleFailReason Is Nothing AndAlso ErrorMessage IsNot Nothing Then PossibleFailReason = ErrorMessage

        '检查结束
        If ContentJson IsNot Nothing Then
            Dim Message As String = ContentJson("msg")
            If Message = "HiPer interface is active" Then
                HiperIp = ContentJson("network").ToString.Split("/").First
                If IsServerSide Then HostIp = HiperIp
                Log("[Hiper] Hiper 启动完成，IP：" & HiperIp)
                HiperState = LoadState.Finished
            ElseIf Message = "Validity of client certificate" Then
                Dim VaildTime As String = ContentJson("valid")
                Date.TryParseExact(VaildTime, "yyyy-MM-dd HH:mm:ss", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, HiperCertTime)
                Log("[Hiper] 索引码到期时间：" & HiperCertTime.ToString)
                Setup.Set("LinkHiperCertTime", HiperCertTime.ToString)
            End If
        End If

        '写入日志
        If ModeDebug OrElse Content.Contains("""error""") Then Log("[Hiper] " & Content)

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
                    '索引码剩余时间
                    Dim Span As TimeSpan = HiperCertTime - Date.Now
                    If Span.TotalDays >= 30 Then
                        LabFinishTime.Text = "> 30 天"
                    ElseIf Span.TotalDays >= 4 Then
                        LabFinishTime.Text = Span.Days & " 天"
                    ElseIf Span.TotalDays >= 1 Then
                        LabFinishTime.Text = Span.Days & " 天" & If(Span.Hours > 0, " " & Span.Hours & " 小时", "")
                    ElseIf Span.TotalMinutes >= 10 Then
                        LabFinishTime.Text = Span.Hours & ":" & Span.Minutes.ToString.PadLeft(2, "0") & "'"
                    Else
                        LabFinishTime.Text = Span.Minutes & "'" & Span.Seconds.ToString.PadLeft(2, "0") & """"
                    End If
                    '提示索引码即将到期
                    If Span.TotalSeconds <= 5 * 60 AndAlso Span.TotalSeconds > 5 * 60 - 1 AndAlso Setup.Get("LinkHiperCertWarn") Then
                        MyMsgBox("你的索引码还有不到 5 分钟就要过期了！" & vbCrLf & "你可以在设置中关闭这个提示……", "索引码即将过期", "我知道了……")
                        ShowWindowToTop(Handle)
                        Beep()
                    End If
                    '检查索引码到期
                    If Span.TotalSeconds < 2 Then
                        LabCertTitle.Text = "索引码已过期"
                        LabCertDesc.Text = "你的 HiPer 索引码已经过期，请输入新的索引码。" & vbCrLf & "如果实在没有索引码，可以在左侧选择 IOI 方式联机。"
                        TextCert.Text = ""
                        HiperExit(True)
                        ShowWindowToTop(Handle)
                        Beep()
                        Exit Sub
                    End If
                    '网络质量
                    Dim QualityScore As Integer = If(IpIsInChina, 0, -2)
                    QualityScore -= Math.Ceiling((Math.Min(PingTime, 600) + Math.Min(PingNodes, 600)) / 80)
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
                        If FrmLinkHiper IsNot Nothing AndAlso FrmLinkHiper.LabFinishPing.IsLoaded Then
                            FrmLinkHiper.LabFinishPing.Text = HostPing & "ms"
                        End If
                    End If
                End Sub)
    End Sub
    '每 15 秒执行的 Timer
    Private Shared HostPing As Integer = -1
    Private Sub WatcherTimer15()
        If Not (HiperState = LoadState.Finished OrElse HiperState = LoadState.Loading) Then Exit Sub
        '检查 HiPer 崩溃
        Try
            Process.GetProcessById(HiperProcessId)
        Catch
            Dim LogLines = ReadFile(PathHiper & "logs\hiper.log").TrimEnd(vbLf).Split(vbLf)
            HiperExit(False)
            MyMsgBox("由于联机模块异常退出，已退出联机房间。" & vbCrLf & vbCrLf &
                     "联机模块最后的日志：" & vbCrLf & Join(LogLines.Skip(LogLines.Count - 3).ToList, vbCrLf), "联机断开")
        End Try
        '下面的部分需要完全完成加载
        If HiperState <> LoadState.Finished Then Exit Sub
        '检查 MCB 崩溃
        Try
            If Not IsServerSide Then Process.GetProcessById(McbProcessId)
        Catch
            HiperExit(False)
            MyMsgBox("由于 MCB 联机模块异常退出，已退出联机房间。", "联机断开")
        End Try
        '重新检查 Ping
        If Not IsServerSide Then
            Dim PingHostFailedCount As Integer = 0
            Do
                TaskPingHost.WaitForExit(True, IsForceRestart:=True)
                If TaskPingHost.Output = -1 Then
                    PingHostFailedCount += 1
                    Log("[HiPer] Ping 房主失败（第 " & PingHostFailedCount & " 次）")
                    If PingHostFailedCount >= 3 Then
                        HiperExit(False) : MyMsgBox("与房主断开连接，已退出联机房间。", "联机断开") : Exit Sub
                    End If
                Else
                    If PingHostFailedCount > 0 Then Log("[HiPer] Ping 房主已恢复成功（" & HostPing & "ms）")
                    Exit Do
                End If
            Loop While True
        End If
    End Sub

#End Region

#Region "PanCert | 索引码输入页面"

    '检测输入
    Private Sub TextCert_ValidateChanged(sender As Object, e As EventArgs) Handles TextCert.ValidateChanged
        BtnCertDone.IsEnabled = TextCert.ValidateResult = ""
    End Sub
    Private Sub TextCert_KeyDown(sender As Object, e As KeyEventArgs) Handles TextCert.KeyDown
        If e.Key = Key.Enter AndAlso BtnCertDone.IsEnabled Then BtnCertDone_Click() '允许回车确认
    End Sub

    '确认
    Private Sub BtnCertDone_Click() Handles BtnCertDone.Click
        CurrentSubpage = Subpages.PanSelect
    End Sub

#End Region

#Region "PanSelect | 种类选择页面"

    '返回
    Private Sub BtnSelectReturn_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnSelectReturn.MouseLeftButtonUp
        LabCertTitle.Text = "输入索引码"
        LabCertDesc.Text = "你需要获取索引码才能使用 HiPer。" & vbCrLf & "如果实在没有索引码，可以在左侧选择 IOI 方式联机。"
        CurrentSubpage = Subpages.PanCert
    End Sub

    '创建房间
    Private Sub BtnSelectCreate_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnSelectCreate.MouseLeftButtonUp
        ''获取端口号
        'Dim PortInput As String = MyMsgBoxInput("", New ObjectModel.Collection(Of Validate) From {
        '                                           New ValidateInteger(2, 65535),
        '                                           New ValidateExceptSame({"55555", "55557"}, "端口不能为 %！")
        '                                       }, "在 MC 的暂停画面选择【对局域网开放】", "输入端口号", "确定", "取消")
        'If PortInput Is Nothing Then Exit Sub
        ''开始
        'RoomCreate(Val(PortInput))
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
        '        '获取信息
        '        Dim Code As String = MyMsgBoxInput("", New ObjectModel.Collection(Of Validate) From {New ValidateLength(8, 99)}, "",
        '                        "输入联机码", "确定", "取消")
        '        If Code Is Nothing Then Exit Sub
        '        '记录信息
        '        If Not Code.EndsWith(RequestVersion) OrElse Not Code.StartsWith("P") Then GoTo WrongCode
        '        Dim Ip As String, Port As Integer
        '        Try
        '            Dim IpAndPort As Long = RadixConvert(Code.Substring(1, Code.Length - 2).ToUpper, 36, 10)
        '            'IpAndPort = HostPort + IpParts(0) * 65536 + IpParts(1) * 65536 * 256 + IpParts(2) * 65536 * 256 * 256 + IpParts(3) * 65536 * 256 * 256 * 256
        '            Ip = Math.Floor(IpAndPort / (65536L * 256 * 256 * 256))
        '            IpAndPort = IpAndPort Mod (65536L * 256 * 256 * 256)
        '            Ip = Math.Floor(IpAndPort / (65536L * 256 * 256)) & "." & Ip
        '            IpAndPort = IpAndPort Mod (65536L * 256 * 256)
        '            Ip = Math.Floor(IpAndPort / (65536L * 256)) & "." & Ip
        '            IpAndPort = IpAndPort Mod (65536L * 256)
        '            Ip = Math.Floor(IpAndPort / (65536L)) & "." & Ip
        '            Port = IpAndPort Mod 65536
        '        Catch
        '            GoTo WrongCode
        '        End Try
        '        '启动
        '        RoomJoin(Ip, Port)
        '        Exit Sub
        'WrongCode:
        '        If Not Code.StartsWith("P") AndAlso Code.Length >= 49 Then Hint("你输入的可能是 IOI 的联机码，请在左侧的联机方式中选择 IOI！", HintType.Critical) : Exit Sub
        '        If Code.StartsWith("P") AndAlso Not Code.EndsWith(RequestVersion) Then Hint("你的 PCL 版本与房主的 PCL 版本不一致！", HintType.Critical) : Exit Sub
        '        Hint("你输入的联机码无效！", HintType.Critical)
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
        Select Case NewState
            Case LoadState.Loading
                UpdateProgress(0)
                If IsServerSide Then
                    LabLoadTitle.Text = "正在创建联机房间"
                    Log("[Hiper] 正在创建联机房间，端口 " & HostPort)
                Else
                    LabLoadTitle.Text = "正在加入联机房间"
                    Log("[Hiper] 正在加入联机房间，目标 IP " & HostIp & ":" & HostPort)
                End If
                LabLoadDesc.Text = "正在初始化……"
                LoadStep = "准备初始化"
            Case LoadState.Failed
                UpdateProgress(1)
                If IsServerSide Then
                    LabLoadTitle.Text = "创建联机房间失败"
                Else
                    LabLoadTitle.Text = "加入联机房间失败"
                End If
                Dim RealException As Exception = If(Loader.Error.InnerException, Loader.Error)
                If TypeOf RealException Is CertOutdatedException Then
                    LabCertTitle.Text = "索引码无效"
                    LabCertDesc.Text = "你的 HiPer 索引码无效或者已经过期！" & vbCrLf & "请重新输入索引码。"
                    HiperExit(True)
                ElseIf RealException.Message.StartsWith("$") Then
                    LabLoadDesc.Text = RealException.Message.TrimStart("$") & vbCrLf &
                                       "点击镐子重试，或者点击灰色的 × 取消。"
                Else
                    LabLoadDesc.Text = LoadStep & "失败：" & GetExceptionSummary(RealException) & vbCrLf &
                                       "点击镐子重试，或者点击灰色的 × 取消。"
                End If
                Log(Loader.Error, "HiPer 联机尝试失败")
            Case LoadState.Finished
                UpdateProgress(1)
                CurrentSubpage = Subpages.PanFinish
                BtnFinishPing.Visibility = If(IsServerSide, Visibility.Collapsed, Visibility.Visible)
                LineFinishPing.Visibility = If(IsServerSide, Visibility.Collapsed, Visibility.Visible)
                BtnFinishCopy.Visibility = If(IsServerSide, Visibility.Visible, Visibility.Collapsed)
                If IsServerSide Then
                    LabFinishTitle.Text = "已创建联机房间"
                    LabFinishDesc.Text = "已在端口 " & HostPort & " 创建了联机房间。" & vbCrLf & "点击下方的复制联机码按钮，然后把联机码发给朋友吧！"
                Else
                    LabFinishTitle.Text = "已加入联机房间"
                    LabFinishDesc.Text = "启动游戏，进入多人游戏页面后稍等片刻，房间就会出现在服务器列表的最下方。" & vbCrLf & "如果提示无效会话，不要退出联机房间，重新启动游戏即可！"
                End If
                LabFinishIp.Text = HostIp & ":" & HostPort
                Log("[Hiper] 已完成连接")
        End Select
    End Sub
    Private Shared LoadStep As String = "准备初始化"
    Private Shared Sub SetLoadDesc(Intro As String, [Step] As String)
        Log("[Hiper] 连接步骤：" & Intro)
        LoadStep = [Step]
        RunInUiWait(Sub()
                        If FrmLinkHiper Is Nothing OrElse Not FrmLinkHiper.LabLoadDesc.IsLoaded Then Exit Sub
                        FrmLinkHiper.LabLoadDesc.Text = Intro
                        FrmLinkHiper.UpdateProgress()
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
        HiperStop(False)
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
        HiperExit(False)
    End Sub

    '复制联机码
    Private Sub BtnFinishCopy_Click(sender As Object, e As EventArgs) Handles BtnFinishCopy.Click
        Dim IpParts = HostIp.Split(".").Select(Function(l) Val(l)).ToList
        Dim IpAndPort As Long = HostPort + IpParts(0) * 65536 + IpParts(1) * 65536 * 256 + IpParts(2) * 65536 * 256 * 256 + IpParts(3) * 65536 * 256 * 256 * 256
        ClipboardSet("P" & RadixConvert(IpAndPort, 10, 36) & RequestVersion, False)
        Hint("已复制联机码！", HintType.Finish)
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
        PanCert
        PanSelect
        PanFinish
    End Enum
    Private _CurrentSubpage As Subpages = Subpages.PanCert
    Public Property CurrentSubpage As Subpages
        Get
            Return _CurrentSubpage
        End Get
        Set(value As Subpages)
            If _CurrentSubpage = value Then Exit Property
            _CurrentSubpage = value
            Log("[Hiper] 子页面更改为 " & GetStringFromEnum(value))
            PageOnContentExit()
            If value = Subpages.PanSelect Then
                LabSelectCode.Text = "(" & TextCert.Text.Substring(0, Math.Min(TextCert.Text.Length, 3)) & "…)"
            End If
        End Set
    End Property

    Private Sub PageLinkHiper_OnPageEnter() Handles Me.PageEnter
        FrmLinkHiper.PanCert.Visibility = If(CurrentSubpage = Subpages.PanCert, Visibility.Visible, Visibility.Collapsed)
        FrmLinkHiper.PanSelect.Visibility = If(CurrentSubpage = Subpages.PanSelect, Visibility.Visible, Visibility.Collapsed)
        FrmLinkHiper.PanFinish.Visibility = If(CurrentSubpage = Subpages.PanFinish, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Shared Sub HiperExit(ExitToCertPage As Boolean)
        Log("[Hiper] 要求退出 Hiper（当前加载器状态为 " & GetStringFromEnum(InitLoader.State) & "）")
        HiperStop(False)
        If InitLoader.State = LoadState.Loading Then InitLoader.Abort()
        If InitLoader.State = LoadState.Failed Then InitLoader.State = LoadState.Waiting
        RunInUi(Sub()
                    If FrmLinkHiper Is Nothing OrElse Not FrmLinkHiper.IsLoaded Then Exit Sub
                    FrmLinkHiper.CurrentSubpage = If(ExitToCertPage, Subpages.PanCert, Subpages.PanSelect)
                    FrmLinkHiper.PageOnContentExit()
                End Sub)
    End Sub

#End Region

End Class
