Imports PCL.Core.Helper
Imports PCL.Core.Model

Public Module ModJava
    Public JavaListCacheVersion As Integer = 7

    Private _javas As JavaManage = Nothing
    ''' <summary>
    ''' 目前所有可用的 Java。
    ''' </summary>
    Public ReadOnly Property Javas As JavaManage
        Get
            InitJava()
            Return _javas
        End Get
    End Property

    Private ReadOnly _javasLock As New Object
    Public Sub InitJava()
        SyncLock _javasLock
            If _javas Is Nothing Then
                Dim storeCache = JavaGetCache()
                _javas = New JavaManage()
                If storeCache IsNot Nothing Then
                    _javas.SetCache(storeCache)
                End If
                Log("[Java] 开始搜索 Java")
                _javas.ScanJava().GetAwaiter().GetResult()
                JavaSetCahce(_javas.GetCache())
                Log("[Java] 搜索到如下 Java:" & vbCrLf & _javas.JavaList.Select(Function(x) x.ToString()).Join(vbCrLf))
            End If
        End SyncLock
    End Sub

    Public Sub JavaSetCahce(caches As List(Of JavaLocalCache))
        Dim newCache = JToken.FromObject(caches).ToString(Newtonsoft.Json.Formatting.None)
        Setup.Set("LaunchArgumentJavaUser", newCache)
    End Sub


    Public Function JavaGetCache() As List(Of JavaLocalCache)
        Dim storeCache = Nothing
        Try
            storeCache = JToken.Parse(Setup.Get("LaunchArgumentJavaUser")).ToObject(Of List(Of JavaLocalCache))
        Catch ex As Exception
            Log("[Java] 解析原有记录错误，可能由于旧版本配置导致")
        End Try
        Return storeCache
    End Function

    ''' <summary>
    ''' 添加一个用户导入的 Java
    ''' </summary>
    ''' <param name="jPath">java.exe 文件位置</param>
    ''' <returns>如果添加成功则返回 true，已经存在或者添加失败返回 false</returns>
    Public Function JavaAddNew(jPath As String) As Boolean
        Try
            If Javas.HasJava(jPath) Then
                Return False
            Else
                Javas.Add(jPath)
                JavaSetCahce(Javas.GetCache())
                Return True
            End If
        Catch ex As Exception
            Log(ex, "[Java] 添加新 Java 失败", LogLevel.Hint)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' 防止多个需要 Java 的部分同时要求下载 Java（#3797）。
    ''' </summary>
    Public JavaLock As New Object
    ''' <summary>
    ''' 根据要求返回最适合的 Java，若找不到则返回 Nothing。
    ''' 最小与最大版本在与输入相同时也会通过。
    ''' 必须在工作线程调用，且必须包括 SyncLock JavaLock。
    ''' </summary>
    Public Function JavaSelect(CancelException As String,
                               Optional MinVersion As Version = Nothing,
                               Optional MaxVersion As Version = Nothing,
                               Optional RelatedVersion As McVersion = Nothing) As Java
        Dim IsVersionSuit = Function(ver As Version)
                                Return ver >= MinVersion AndAlso ver <= MaxVersion
                            End Function
        Dim UserTarget = GetUserSetJava(RelatedVersion)
        If UserTarget IsNot Nothing AndAlso IsVersionSuit(UserTarget.Version) Then
            Return UserTarget
        End If
        Dim ret = Javas.SelectSuitableJava(MinVersion, MaxVersion).Result.FirstOrDefault()
        If ret Is Nothing AndAlso MinVersion.Major = 1 AndAlso MinVersion.Minor = 8 Then
            ret = Javas.SelectSuitableJava(New Version(8, 0, 0, 0), If(MaxVersion.Major = 1, New Version(MaxVersion.Minor, 999, 999, 999), MaxVersion)).Result.FirstOrDefault()
        End If
        Return ret
    End Function

    ''' <summary>
    ''' 获取指定游戏实例所要求的版本
    ''' </summary>
    ''' <param name="Mc">实例</param>
    ''' <returns>如果有设置为 Java 实例，否则为 null</returns>
    Public Function GetUserSetJava(Mc As McVersion) As Java
        Dim UserSetupVersion As String = Setup.Get("VersionArgumentJavaSelect", Version:=Mc)
        If UserSetupVersion = "使用全局设置" Then
            Return Nothing
        Else
            Return Java.Parse(UserSetupVersion)
        End If
    End Function

    ''' <summary>
    ''' 是否强制指定了 64 位 Java。如果没有强制指定，返回是否安装了 64 位 Java。
    ''' </summary>
    Public Function IsGameSet64BitJava(Optional RelatedVersion As McVersion = Nothing) As Boolean
        Try
            '检查强制指定
            Dim UserSetup As String = Setup.Get("LaunchArgumentJavaSelect")
            If RelatedVersion IsNot Nothing Then
                Dim UserSetupVersion As String = Setup.Get("VersionArgumentJavaSelect", Version:=RelatedVersion)
                If UserSetupVersion <> "使用全局设置" Then
                    If File.Exists(UserSetupVersion) Then
                        Dim k = Java.Parse(UserSetup)
                        Return If(k IsNot Nothing, k.Is64Bit, False)
                    Else
                        Setup.Set("VersionArgumentJavaSelect", "", Version:=RelatedVersion)
                    End If
                End If
            End If
            If Not String.IsNullOrEmpty(UserSetup) AndAlso Not File.Exists(UserSetup) Then
                Setup.Set("LaunchArgumentJavaSelect", "")
                UserSetup = String.Empty
            End If
            If String.IsNullOrEmpty(UserSetup) Then
                Return Javas.JavaList.Any(Function(x) x.Is64Bit)
            End If
            Dim j = Java.Parse(UserSetup)
            Return j IsNot Nothing AndAlso j.Is64Bit
        Catch ex As Exception
            Log(ex, "检查 Java 类别时出错", LogLevel.Feedback)
            If RelatedVersion IsNot Nothing Then Setup.Set("VersionArgumentJavaSelect", "", Version:=RelatedVersion)
            Setup.Set("LaunchArgumentJavaSelect", "")
        End Try
        Return True
    End Function

#Region "下载"

    ''' <summary>
    ''' 提示 Java 缺失，并弹窗确认是否自动下载。返回玩家选择是否下载。
    ''' </summary>
    Public Function JavaDownloadConfirm(VersionDescription As String, Optional ForcedManualDownload As Boolean = False) As Boolean
        If ForcedManualDownload Then
            MyMsgBox($"PCL 未找到 {VersionDescription}。" & vbCrLf &
                     $"请自行搜索并安装 {VersionDescription}，安装后在 设置 → 启动选项 → 游戏 Java 中重新搜索或导入。",
                     "未找到 Java")
            Return False
        Else
            Return MyMsgBox($"PCL 未找到 {VersionDescription}，是否需要 PCL 自动下载？" & vbCrLf &
                            $"如果你已经安装了 {VersionDescription}，可以在 设置 → 启动选项 → 游戏 Java 中手动导入。",
                            "自动下载 Java？", "自动下载", "取消") = 1
        End If
    End Function

    ''' <summary>
    ''' 获取下载 Java 8/14/17/21 的加载器。需要开启 IsForceRestart 以正常刷新 Java 列表。
    ''' </summary>
    Public Function JavaFixLoaders(Version As Integer) As LoaderCombo(Of Integer)
        Dim JavaDownloadLoader As New LoaderDownload("下载 Java 文件", New List(Of NetFile)) With {.ProgressWeight = 10}
        Dim Loader = New LoaderCombo(Of Integer)($"下载 Java {Version}", {
            New LoaderTask(Of Integer, List(Of NetFile))("获取 Java 下载信息", AddressOf JavaFileList) With {.ProgressWeight = 2},
            JavaDownloadLoader
        })
        AddHandler JavaDownloadLoader.OnStateChangedThread,
        Sub(Raw As LoaderBase, NewState As LoadState, OldState As LoadState)
            If (NewState = LoadState.Failed OrElse NewState = LoadState.Aborted) AndAlso LastJavaBaseDir IsNot Nothing Then
                Log($"[Java] 由于下载未完成，清理未下载完成的 Java 文件：{LastJavaBaseDir}", LogLevel.Debug)
                DeleteDirectory(LastJavaBaseDir)
            ElseIf NewState = LoadState.Finished Then
                Javas.ScanJava().GetAwaiter().GetResult()
                LastJavaBaseDir = Nothing
            End If
        End Sub
        JavaDownloadLoader.HasOnStateChangedThread = True
        Return Loader
    End Function
    Private LastJavaBaseDir As String = Nothing '用于在下载中断或失败时删除未完成下载的 Java 文件夹，防止残留只下了一半但 -version 能跑的 Java
    Private Sub JavaFileList(Loader As LoaderTask(Of Integer, List(Of NetFile)))
        Log("[Java] 开始获取 Java 下载信息")
        Dim IndexFileStr As String = NetGetCodeByLoader(DlVersionListOrder(
            {"https://piston-meta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json"},
            {"https://bmclapi2.bangbang93.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json"}
        ), IsJson:=True)
        '获取下载地址
        Dim MainEntry As JObject = CType(GetJson(IndexFileStr), JObject)($"windows-x{If(Is32BitSystem, "86", "64")}")
        Dim Entries = MainEntry.Children.Reverse. '选择最靠后的一项（最新）
            SelectMany(Function(e As JProperty) CType(e.Value, JArray).Select(Function(v) New KeyValuePair(Of String, JObject)(e.Name, v)))
        Dim TargetEntry = Entries.First(Function(t) t.Value("version")("name").ToString.StartsWithF(Loader.Input))
        Dim Address As String = TargetEntry.Value("manifest")("url")
        Log($"[Java] 准备下载 Java {TargetEntry.Value("version")("name")}（{TargetEntry.Key}）：{Address}")
        '获取文件列表
        Dim ListFileStr As String = NetGetCodeByLoader(DlSourceOrder({Address}, {Address.Replace("piston-meta.mojang.com", "bmclapi2.bangbang93.com")}), IsJson:=True)
        LastJavaBaseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\.minecraft\runtime\" & TargetEntry.Key & "\"
        Dim Results As New List(Of NetFile)
        For Each File As JProperty In CType(GetJson(ListFileStr), JObject)("files")
            If CType(File.Value, JObject)("downloads")?("raw") Is Nothing Then Continue For
            Dim Info As JObject = CType(File.Value, JObject)("downloads")("raw")
            Dim Checker As New FileChecker(ActualSize:=Info("size"), Hash:=Info("sha1"))
            If Checker.Hash = "12976a6c2b227cbac58969c1455444596c894656" OrElse Checker.Hash = "c80e4bab46e34d02826eab226a4441d0970f2aba" OrElse Checker.Hash = "84d2102ad171863db04e7ee22a259d1f6c5de4a5" Then
                '跳过 3 个无意义大量重复文件（#3827）
                Continue For
            End If
            If Checker.Check(LastJavaBaseDir & File.Name) Is Nothing Then Continue For '跳过已存在的文件
            Dim Url As String = Info("url")
            Results.Add(New NetFile(DlSourceOrder({Url}, {Url.Replace("piston-data.mojang.com", "bmclapi2.bangbang93.com")}), LastJavaBaseDir & File.Name, Checker))
        Next
        Loader.Output = Results
        Log($"[Java] 需要下载 {Results.Count} 个文件，目标文件夹：{LastJavaBaseDir}")
    End Sub

#End Region

End Module
