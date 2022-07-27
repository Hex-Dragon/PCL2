Public Module ModDownload

#Region "DlClient* | Minecraft 客户端"

    ''' <summary>
    ''' 返回某 Minecraft 版本对应的原版主 Jar 文件的下载信息，要求对应依赖版本已存在。
    ''' 失败则抛出异常，不需要下载则返回 Nothing。
    ''' </summary>
    Public Function DlClientJarGet(Version As McVersion, ReturnNothingOnFileUseable As Boolean) As NetFile
        '获取底层继承版本
        Do While Not String.IsNullOrEmpty(Version.InheritVersion)
            Version = New McVersion(Version.InheritVersion)
        Loop
        '检查 Json 是否标准
        If Version.JsonObject("downloads") Is Nothing OrElse Version.JsonObject("downloads")("client") Is Nothing OrElse Version.JsonObject("downloads")("client")("url") Is Nothing Then
            Throw New Exception("底层版本 " & Version.Name & " 中无 Jar 文件下载信息")
        End If
        '检查文件
        Dim Checker As New FileChecker(MinSize:=1024, ActualSize:=If(Version.JsonObject("downloads")("client")("size"), -1), Hash:=Version.JsonObject("downloads")("client")("sha1"))
        If ReturnNothingOnFileUseable Then
            '是否跳过
            Dim IsSetupSkip As Boolean = Setup.Get("LaunchAdvanceAssets")
            Select Case Setup.Get("VersionAdvanceAssets", Version:=Version)
                Case 0 '使用全局设置
                Case 1 '开启
                    IsSetupSkip = False
                Case 2 '关闭
                    IsSetupSkip = True
            End Select
            If IsSetupSkip AndAlso File.Exists(Version.Path & Version.Name & ".jar") Then Return Nothing '跳过校验
            If Checker.Check(Version.Path & Version.Name & ".jar") Is Nothing Then Return Nothing '通过校验
        End If
        '返回下载信息
        Dim JarUrl As String = Version.JsonObject("downloads")("client")("url")
        Return New NetFile(DlSourceLauncherOrMetaGet(JarUrl), Version.Path & Version.Name & ".jar", Checker)
    End Function

    ''' <summary>
    ''' 返回某 Minecraft 版本对应的原版主 AssetIndex 文件的下载信息，要求对应依赖版本已存在。
    ''' 若未找到，则会返回 Legacy 资源文件或 Nothing。
    ''' </summary>
    Public Function DlClientAssetIndexGet(Version As McVersion) As NetFile
        '获取底层继承版本
        Do While Not String.IsNullOrEmpty(Version.InheritVersion)
            Version = New McVersion(Version.InheritVersion)
        Loop
        '获取信息
        Dim IndexInfo = McAssetsGetIndex(Version, True, True)
        Dim IndexAddress As String = PathMcFolder & "assets\indexes\" & IndexInfo("id").ToString & ".json"
        Log("[Download] 版本 " & Version.Name & " 对应的资源文件索引为 " & IndexInfo("id").ToString)
        Dim IndexUrl As String = If(IndexInfo("url"), "")
        If IndexUrl = "" Then
            Return Nothing
        Else
            Return New NetFile(DlSourceLauncherOrMetaGet(IndexUrl, False), IndexAddress, New FileChecker(CanUseExistsFile:=False, IsJson:=True))
        End If
    End Function

    ''' <summary>
    ''' 构造补全某 Minecraft 版本的所有文件的加载器列表。失败会抛出异常。
    ''' </summary>
    Public Function DlClientFix(Version As McVersion, CheckAssetsHash As Boolean, AssetsIndexBehaviour As AssetsIndexExistsBehaviour, SkipAssetsDownloadWhileSetupRequired As Boolean) As List(Of LoaderBase)
        Dim Loaders As New List(Of LoaderBase)

#Region "下载支持库文件"
        Dim LoadersLib As New List(Of LoaderBase) From {
                New LoaderTask(Of String, List(Of NetFile))("分析缺失支持库文件", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(Version)) With {.ProgressWeight = 1},
                New LoaderDownload("下载支持库文件", New List(Of NetFile)) With {.ProgressWeight = 15}
            }
        '构造加载器
        Loaders.Add(New LoaderCombo(Of String)("下载支持库文件（主加载器）", LoadersLib) With {.Block = False, .Show = False, .ProgressWeight = 16})
#End Region

#Region "下载资源文件"
        Dim IsSetupSkip As Boolean = Setup.Get("LaunchAdvanceAssets")
        Select Case Setup.Get("VersionAdvanceAssets", Version:=Version)
            Case 0
                '使用全局设置
            Case 1
                '开启
                IsSetupSkip = False
            Case 2
                '关闭
                IsSetupSkip = True
        End Select
        If IsSetupSkip Then Log("[Download] 已跳过 Assets 下载")
        If (Not SkipAssetsDownloadWhileSetupRequired) OrElse Not IsSetupSkip Then
            Dim LoadersAssets As New List(Of LoaderBase)
            '获取资源文件索引地址
            LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析资源文件索引地址",
                                                                Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                    Try
                                                                        Dim IndexFile = DlClientAssetIndexGet(Version)
                                                                        Dim IndexFileInfo As New FileInfo(IndexFile.LocalPath)
                                                                        If AssetsIndexBehaviour <> AssetsIndexExistsBehaviour.AlwaysDownload AndAlso IndexFile.Check.Check(IndexFile.LocalPath) Is Nothing Then
                                                                            Task.Output = New List(Of NetFile)
                                                                        Else
                                                                            Task.Output = New List(Of NetFile) From {IndexFile}
                                                                        End If
                                                                    Catch ex As Exception
                                                                        Throw New Exception("分析资源文件索引地址失败", ex)
                                                                    End Try
                                                                End Sub) With {.ProgressWeight = 0.5, .Show = False})
            '下载资源文件索引
            LoadersAssets.Add(New LoaderDownload("下载资源文件索引", New List(Of NetFile)) With {.ProgressWeight = 2})
            '要求独立更新索引
            If AssetsIndexBehaviour = AssetsIndexExistsBehaviour.DownloadInBackground Then
                Dim LoadersAssetsUpdate As New List(Of LoaderBase)
                Dim TempAddress As String = Nothing
                Dim RealAddress As String = Nothing
                LoadersAssetsUpdate.Add(New LoaderTask(Of String, List(Of NetFile))("后台分析资源文件索引地址", Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                                                        Dim BackAssetsFile As NetFile = DlClientAssetIndexGet(Version)
                                                                                                        RealAddress = BackAssetsFile.LocalPath
                                                                                                        TempAddress = PathTemp & "Cache\" & BackAssetsFile.LocalName
                                                                                                        BackAssetsFile.LocalPath = TempAddress
                                                                                                        Task.Output = New List(Of NetFile) From {BackAssetsFile}
                                                                                                    End Sub))
                LoadersAssetsUpdate.Add(New LoaderDownload("后台下载资源文件索引", New List(Of NetFile)))
                LoadersAssetsUpdate.Add(New LoaderTask(Of List(Of NetFile), String)("后台复制资源文件索引", Sub(Task As LoaderTask(Of List(Of NetFile), String))
                                                                                                      Try
                                                                                                          File.Copy(TempAddress, RealAddress, True)
                                                                                                          McLaunchLog("后台更新资源文件索引成功：" & TempAddress)
                                                                                                      Catch ex As Exception
                                                                                                          Log(ex, "后台更新资源文件索引失败", LogLevel.Debug)
                                                                                                          McLaunchLog("后台更新资源文件索引失败：" & TempAddress)
                                                                                                      End Try
                                                                                                  End Sub))
                Dim Updater As New LoaderCombo(Of String)("后台更新资源文件索引", LoadersAssetsUpdate)
                Log("[Download] 开始后台更新资源文件索引")
                Updater.Start()
            End If
            '获取资源文件地址
            LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析缺失资源文件",
                                                                Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                    Task.Output = McAssetsFixList(McAssetsGetIndexName(Version), CheckAssetsHash, Task)
                                                                End Sub) With {.ProgressWeight = 3})
            '下载资源文件
            LoadersAssets.Add(New LoaderDownload("下载资源文件", New List(Of NetFile)) With {.ProgressWeight = 25})
            '构造加载器
            Loaders.Add(New LoaderCombo(Of String)("下载资源文件（主加载器）", LoadersAssets) With {.Block = False, .Show = False, .ProgressWeight = 30.5})
        End If
#End Region

        Return Loaders
    End Function
    Public Enum AssetsIndexExistsBehaviour
        ''' <summary>
        ''' 如果文件存在，则不进行下载。
        ''' </summary>
        DontDownload
        ''' <summary>
        ''' 如果文件存在，则启动新的下载加载器进行独立的更新。
        ''' </summary>
        DownloadInBackground
        ''' <summary>
        ''' 如果文件存在，也同样进行下载。
        ''' </summary>
        AlwaysDownload
    End Enum

#End Region

#Region "DlClientList | Minecraft 客户端 版本列表"

    '主加载器
    Public Structure DlClientListResult
        ''' <summary>
        ''' 数据来源名称，如“Mojang”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的 Json 数据。
        ''' </summary>
        Public Value As JObject
        '''' <summary>
        '''' 官方源的失败原因。若没有则为 Nothing。
        '''' </summary>
        'Public OfficialError As Exception
    End Structure
    ''' <summary>
    ''' Minecraft 客户端 版本列表，主加载器。
    ''' </summary>
    Public DlClientListLoader As New LoaderTask(Of Integer, DlClientListResult)("DlClientList Main", AddressOf DlClientListMain)
    Private Sub DlClientListMain(Loader As LoaderTask(Of Integer, DlClientListResult))
        Select Case Setup.Get("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)(DlClientListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)(DlClientListMojangLoader, 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)(DlClientListMojangLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)(DlClientListBmclapiLoader, 35)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)(DlClientListMojangLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlClientListResult), Integer)(DlClientListBmclapiLoader, 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    '各个下载源的分加载器
    ''' <summary>
    ''' Minecraft 客户端 版本列表，Mojang 官方源加载器。
    ''' </summary>
    Public DlClientListMojangLoader As New LoaderTask(Of Integer, DlClientListResult)("DlClientList Mojang", AddressOf DlClientListMojangMain)
    Private IsNewClientVersionHinted As Boolean = False
    Private Sub DlClientListMojangMain(Loader As LoaderTask(Of Integer, DlClientListResult))
        Dim Json As JObject = NetGetCodeByRequestRetry("https://launchermeta.mojang.com/mc/game/version_manifest.json", IsJson:=True)
        Try
            Dim Versions As JArray = Json("versions")
            If Versions.Count < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Json.ToString & "）")
            '添加 PCL2 特供项
            If File.Exists(PathTemp & "Cache\download.json") Then Versions.Merge(GetJson(ReadFile(PathTemp & "Cache\download.json")))
            '返回
            Loader.Output = New DlClientListResult With {.IsOfficial = True, .SourceName = "Mojang 官方源", .Value = Json}
            '解析更新提示（Release）
            Dim Version As String = Json("latest")("release")
            If Setup.Get("ToolUpdateRelease") AndAlso Not Setup.Get("ToolUpdateReleaseLast") = "" AndAlso Version IsNot Nothing AndAlso Not Setup.Get("ToolUpdateReleaseLast") = Version Then
                McDownloadClientUpdateHint(Version, Json)
                IsNewClientVersionHinted = True
            End If
            Setup.Set("ToolUpdateReleaseLast", Version)
            '解析更新提示（Snapshot）
            Version = Json("latest")("snapshot")
            If Setup.Get("ToolUpdateSnapshot") AndAlso Not Setup.Get("ToolUpdateSnapshotLast") = "" AndAlso Version IsNot Nothing AndAlso Not Setup.Get("ToolUpdateSnapshotLast") = Version AndAlso Not IsNewClientVersionHinted Then
                McDownloadClientUpdateHint(Version, Json)
            End If
            Setup.Set("ToolUpdateSnapshotLast", If(Version, "Nothing"))
        Catch ex As Exception
            Throw New Exception("版本列表解析失败", ex)
        End Try
    End Sub
    ''' <summary>
    ''' Minecraft 客户端 版本列表，BMCLAPI 源加载器。
    ''' </summary>
    Public DlClientListBmclapiLoader As New LoaderTask(Of Integer, DlClientListResult)("DlClientList Bmclapi", AddressOf DlClientListBmclapiMain)
    Private Sub DlClientListBmclapiMain(Loader As LoaderTask(Of Integer, DlClientListResult))
        Dim Json As JObject = NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/mc/game/version_manifest.json", IsJson:=True)
        Try
            Dim Versions As JArray = Json("versions")
            If Versions.Count < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Json.ToString & "）")
            '添加 PCL2 特供项
            If File.Exists(PathTemp & "Cache\download.json") Then Versions.Merge(GetJson(ReadFile(PathTemp & "Cache\download.json")))
            '返回
            Loader.Output = New DlClientListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Json}
        Catch ex As Exception
            Throw New Exception("版本列表解析失败（" & Json.ToString & "）", ex)
        End Try
    End Sub

    ''' <summary>
    ''' 获取某个版本的 Json 下载地址，若失败则返回 Nothing。必须在工作线程执行。
    ''' </summary>
    Public Function DlClientListGet(Id As String)
        Try
            '确认 Minecraft 版本列表已完成获取
            Select Case DlClientListLoader.State
                Case LoadState.Loading
                    DlClientListLoader.WaitForExit()
                Case LoadState.Failed, LoadState.Aborted, LoadState.Waiting
                    DlClientListLoader.WaitForExit(IsForceRestart:=True)
            End Select
            '确认版本格式标准
            Id = Id.Replace("_", "-") '1.7.10_pre4 在版本列表中显示为 1.7.10-pre4
            If Id <> "1.0" AndAlso Id.EndsWith(".0") Then Id = Left(Id, Id.Length - 2) 'OptiFine 1.8 的下载会触发此问题，显示版本为 1.8.0
            '查找版本并开始
            For Each Version As JObject In DlClientListLoader.Output.Value("versions")
                If Version("id") = Id Then
                    Return Version("url").ToString
                End If
            Next
            Log("未发现版本 " & Id & " 的 Json 下载地址，版本列表返回为：" & vbCrLf & DlClientListLoader.Output.Value.ToString, LogLevel.Debug)
            Return Nothing
        Catch ex As Exception
            Log(ex, "获取版本 " & Id & " 的 Json 下载地址失败")
            Return Nothing
        End Try
    End Function

#End Region

#Region "DlOptiFineList | OptiFine 版本列表"

    Public Structure DlOptiFineListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As List(Of DlOptiFineListEntry)
    End Structure
    Public Class DlOptiFineListEntry
        ''' <summary>
        ''' 显示名称，已去除 HD_U 字样，如“1.12.2 C8”。
        ''' </summary>
        Public NameDisplay As String
        ''' <summary>
        ''' 原始文件名称，如“preview_OptiFine_1.11_HD_U_E1_pre.jar”。
        ''' </summary>
        Public NameFile As String
        ''' <summary>
        ''' 对应的版本名称，如“1.13.2-OptiFine_HD_U_E6”。
        ''' </summary>
        Public NameVersion As String
        ''' <summary>
        ''' 是否为测试版。
        ''' </summary>
        Public IsPreview As Boolean
        ''' <summary>
        ''' 对应的 Minecraft 版本，如“1.12.2”。
        ''' </summary>
        Public Property Inherit As String
            Get
                Return _inherit
            End Get
            Set(value As String)
                If value.EndsWith(".0") Then value = Left(value, value.Length - 2)
                _inherit = value
            End Set
        End Property
        Private _inherit As String
        ''' <summary>
        ''' 发布时间，格式为“yyyy/mm/dd”。OptiFine 源无此数据。
        ''' </summary>
        Public ReleaseTime As String
    End Class

    ''' <summary>
    ''' OptiFine 版本列表，主加载器。
    ''' </summary>
    Public DlOptiFineListLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Main", AddressOf DlOptiFineListMain)
    Private Sub DlOptiFineListMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Select Case Setup.Get("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListOfficialLoader, 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListBmclapiLoader, 35)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListBmclapiLoader, 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    ''' <summary>
    ''' OptiFine 版本列表，官方源。
    ''' </summary>
    Public DlOptiFineListOfficialLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Official", AddressOf DlOptiFineListOfficialMain)
    Private Sub DlOptiFineListOfficialMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Dim Result As String = NetGetCodeByClient("https://optifine.net/downloads", Encoding.Default)
        If Result.Length < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")
        '获取所有版本信息
        Dim ReleaseTime As List(Of String) = RegexSearch(Result, "(?<=Date'>)[^<]+")
        Dim Name As List(Of String) = RegexSearch(Result, "(?<=OptiFine_)[0-9A-Za-z_.]+(?=.jar"")")
        If Not ReleaseTime.Count = Name.Count Then Throw New Exception("版本与发布时间数据无法对应")
        If ReleaseTime.Count < 10 Then Throw New Exception("获取到的版本数量不足（" & Result & "）")
        '转化为列表输出
        Dim Versions As New List(Of DlOptiFineListEntry)
        For i = 0 To ReleaseTime.Count - 1
            Name(i) = Name(i).Replace("_", " ")
            Dim Entry As New DlOptiFineListEntry With {
                         .NameDisplay = Name(i).ToString.Replace("HD U ", "").Replace(".0 ", " "),
                         .ReleaseTime = Join({ReleaseTime(i).Split(".")(2), ReleaseTime(i).Split(".")(1), ReleaseTime(i).Split(".")(0)}, "/"),
                         .IsPreview = Name(i).ToString.ToLower.Contains("pre"),
                         .Inherit = Name(i).ToString.Split(" ")(0),
                         .NameFile = If(Name(i).ToString.ToLower.Contains("pre"), "preview_", "") &
                                                        "OptiFine_" & Name(i).ToString.Replace(" ", "_") & ".jar"}
            Entry.NameVersion = Entry.Inherit & "-OptiFine_" & Name(i).ToString.Replace(" ", "_").Replace(Entry.Inherit & "_", "")
            Versions.Add(Entry)
        Next
        Loader.Output = New DlOptiFineListResult With {.IsOfficial = True, .SourceName = "OptiFine 官方源", .Value = Versions}
    End Sub

    ''' <summary>
    ''' OptiFine 版本列表，BMCLAPI。
    ''' </summary>
    Public DlOptiFineListBmclapiLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Bmclapi", AddressOf DlOptiFineListBmclapiMain)
    Private Sub DlOptiFineListBmclapiMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Dim Json As JArray = NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/optifine/versionList", IsJson:=True)
        Try
            Dim Versions As New List(Of DlOptiFineListEntry)
            For Each Token As JObject In Json
                Dim Entry As New DlOptiFineListEntry With {
                             .NameDisplay = (Token("mcversion").ToString & Token("type").ToString.Replace("HD_U", "").Replace("_", " ") & " " & Token("patch").ToString).Replace(".0 ", " "),
                             .ReleaseTime = "",
                             .IsPreview = Token("patch").ToString.ToLower.Contains("pre"),
                             .Inherit = Token("mcversion").ToString,
                             .NameFile = Token("filename").ToString
                         }
                Entry.NameVersion = Entry.Inherit & "-OptiFine_" & (Token("type").ToString & " " & Token("patch").ToString).Replace(".0 ", " ").Replace(" ", "_").Replace(Entry.Inherit & "_", "")
                Versions.Add(Entry)
            Next
            Loader.Output = New DlOptiFineListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("版本列表解析失败（" & Json.ToString & "）", ex)
        End Try
    End Sub

#End Region

#Region "DlForgeList | Forge Minecraft 版本列表"

    Public Structure DlForgeListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As List(Of String)
    End Structure

    ''' <summary>
    ''' Forge 版本列表，主加载器。
    ''' </summary>
    Public DlForgeListLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Main", AddressOf DlForgeListMain)
    Private Sub DlForgeListMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Select Case Setup.Get("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListOfficialLoader, 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListBmclapiLoader, 35)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListBmclapiLoader, 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    ''' <summary>
    ''' Forge 版本列表，官方源。
    ''' </summary>
    Public DlForgeListOfficialLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Official", AddressOf DlForgeListOfficialMain)
    Private Sub DlForgeListOfficialMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Dim Result As String = NetGetCodeByRequestRetry("http://files.minecraftforge.net/maven/net/minecraftforge/forge/index_1.2.4.html", Encoding.Default, "text/html")
        If Result.Length < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")
        '获取所有版本信息
        Dim Names As List(Of String) = RegexSearch(Result, "(?<=a href=""index_)[0-9.]+(_pre[0-9]?)?(?=.html)")
        Names.Add("1.2.4") '1.2.4 不会被匹配上
        If Names.Count < 10 Then Throw New Exception("获取到的版本数量不足（" & Result & "）")
        Loader.Output = New DlForgeListResult With {.IsOfficial = True, .SourceName = "Forge 官方源", .Value = Names}
    End Sub

    ''' <summary>
    ''' Forge 版本列表，BMCLAPI。
    ''' </summary>
    Public DlForgeListBmclapiLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Bmclapi", AddressOf DlForgeListBmclapiMain)
    Private Sub DlForgeListBmclapiMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Dim Result As String = NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/forge/minecraft", Encoding.Default)
        If Result.Length < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")
        '获取所有版本信息
        Dim Names As List(Of String) = RegexSearch(Result, "[0-9.]+(_pre[0-9]?)?")
        If Names.Count < 10 Then Throw New Exception("获取到的版本数量不足（" & Result & "）")
        Loader.Output = New DlForgeListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Names}
    End Sub

#End Region

#Region "DlForgeVersion | Forge 版本列表"

    Public Class DlForgeVersionEntry
        ''' <summary>
        ''' 完整的版本名，如 “14.22.1.2478”。
        ''' </summary>
        Public Version As String
        ''' <summary>
        ''' 对应的 Minecraft 版本，如“1.12.2”。
        ''' </summary>
        Public Inherit As String
        ''' <summary>
        ''' 发布时间，格式为“yyyy/MM/dd HH:mm”。
        ''' </summary>
        Public ReleaseTime As String
        ''' <summary>
        ''' 文件的 MD5 或 SHA1（BMCLAPI 的老版本是 MD5，新版本是 SHA1；官方源总是 MD5）。
        ''' </summary>
        Public Hash As String = Nothing
        ''' <summary>
        ''' 是否为推荐版本。
        ''' </summary>
        Public IsRecommended As Boolean
        ''' <summary>
        ''' 安装类型。有 installer、client、universal 三种。
        ''' </summary>
        Public Category As String
        ''' <summary>
        ''' 版本分支。若无分支则为 Nothing。
        ''' </summary>
        Public Branch As String = Nothing
        ''' <summary>
        ''' 是否为新版 Forge。（即为 Minecraft 1.13+）
        ''' </summary>
        Public ReadOnly Property IsNewType As Boolean
            Get
                Return Version.Split(".")(0) >= 20
            End Get
        End Property
        ''' <summary>
        ''' 构建数。
        ''' </summary>
        Public ReadOnly Property Build As Integer
            Get
                Dim Version = Me.Version.Split(".")
                If Version(0) < 15 Then
                    Return Version(Version.Count - 1)
                Else
                    Return Version(Version.Count - 1) + 10000
                End If
            End Get
        End Property
        ''' <summary>
        ''' 用于下载的文件版本名。可能在 Version 的基础上添加了分支。
        ''' </summary>
        Public ReadOnly Property FileVersion As String
            Get
                Return Version & If(Branch Is Nothing, ""， "-" & Branch)
            End Get
        End Property
        ''' <summary>
        ''' 即将下载的文件全名。
        ''' </summary>
        Public ReadOnly Property FileName As String
            Get
                Return "forge-" & Inherit & "-" & FileVersion & "-" & Category & "." & FileSuffix
            End Get
        End Property
        ''' <summary>
        ''' 文件扩展名。
        ''' </summary>
        Public ReadOnly Property FileSuffix As String
            Get
                If Category = "installer" Then
                    Return "jar"
                Else
                    Return "zip"
                End If
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Forge 版本列表，主加载器。
    ''' </summary>
    Public Sub DlForgeVersionMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim DlForgeVersionOfficialLoader As New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion Official", AddressOf DlForgeVersionOfficialMain)
        Dim DlForgeVersionBmclapiLoader As New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion Bmclapi", AddressOf DlForgeVersionBmclapiMain)
        Select Case Setup.Get("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionOfficialLoader, 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionBmclapiLoader, 35)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionBmclapiLoader, 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    ''' <summary>
    ''' Forge 版本列表，官方源。
    ''' </summary>
    Public Sub DlForgeVersionOfficialMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim Result As String
        Try
            Result = NetGetCodeByDownload("http://files.minecraftforge.net/maven/net/minecraftforge/forge/index_" & Loader.Input & ".html")
        Catch ex As Exception
            If GetString(ex).Contains("(404)") Then
                Throw New Exception("没有可用版本")
            Else
                Throw
            End If
        End Try
        If Result.Length < 1000 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")
        Dim Versions As New List(Of DlForgeVersionEntry)
        Try
            '分割版本信息
            Dim VersionCodes = Mid(Result, 1, Result.LastIndexOf("</table>")).Replace("<td class=""download-version", "¨").Split("¨")
            '获取所有版本信息
            For i = 1 To VersionCodes.Count - 1
                Dim VersionCode = VersionCodes(i)
                Try
                    '基础信息获取
                    Dim Name As String = RegexSeek(VersionCode, "(?<=[^(0-9)]+)[0-9\.]+")
                    Dim IsRecommended As Boolean = VersionCode.Contains("fa promo-recommended")
                    Dim Inherit As String = Loader.Input
                    '分支获取
                    Dim Branch As String = RegexSeek(VersionCode, "(?<=Branch:</strong>[\s]*)[^<]+")
                    Branch = RegexSeek(If(Branch, ""), "[^\s]+")
                    If String.IsNullOrWhiteSpace(Branch) Then Branch = Nothing
                    If Name = "11.15.1.2318" OrElse Name = "11.15.1.1902" OrElse Name = "11.15.1.1890" Then Branch = "1.8.9"
                    If Branch Is Nothing AndAlso Inherit = "1.7.10" AndAlso Name.Split(".")(3) >= 1300 Then Branch = "1.7.10"
                    '发布时间获取
                    Dim ReleaseTimeOriginal = RegexSeek(VersionCode, "(?<=""download-time"" title="")[^""]+")
                    Dim ReleaseTimeSplit = ReleaseTimeOriginal.Split(" -:".ToCharArray) '原格式："2021-02-15 03:24:02"
                    Dim ReleaseDate As New Date(ReleaseTimeSplit(0), ReleaseTimeSplit(1), ReleaseTimeSplit(2), '年月日
                                                ReleaseTimeSplit(3), ReleaseTimeSplit(4), ReleaseTimeSplit(5), '时分秒
                                                0, DateTimeKind.Utc) '以 UTC 时间作为标准
                    Dim ReleaseTime As String = ReleaseDate.ToLocalTime.ToString("yyyy/MM/dd HH:mm") '时区与格式转换
                    '分类与 MD5 获取
                    Dim MD5 As String, Category As String
                    If VersionCode.Contains("classifier-installer""") Then
                        '类型为 installer.jar，支持范围 ~753 (~ 1.6.1 部分), 738~684 (1.5.2 全部)
                        VersionCode = VersionCode.Substring(VersionCode.IndexOf("installer.jar"))
                        MD5 = RegexSeek(VersionCode, "(?<=MD5:</strong> )[^<]+")
                        Category = "installer"
                    ElseIf VersionCode.Contains("classifier-universal""") Then
                        '类型为 universal.zip，支持范围 751~449 (1.6.1 部分), 682~183 (1.5.1 ~ 1.3.2 部分)
                        VersionCode = VersionCode.Substring(VersionCode.IndexOf("universal.zip"))
                        MD5 = RegexSeek(VersionCode, "(?<=MD5:</strong> )[^<]+")
                        Category = "universal"
                    Else
                        '类型为 client.zip，支持范围 182~ (1.3.2 部分 ~)
                        VersionCode = VersionCode.Substring(VersionCode.IndexOf("client.zip"))
                        MD5 = RegexSeek(VersionCode, "(?<=MD5:</strong> )[^<]+")
                        Category = "client"
                    End If
                    '添加进列表
                    Versions.Add(New DlForgeVersionEntry With {.Category = Category, .Version = Name, .IsRecommended = IsRecommended, .Hash = MD5.Trim(vbCr, vbLf), .Inherit = Inherit, .ReleaseTime = ReleaseTime, .Branch = Branch})
                Catch ex As Exception
                    Throw New Exception("版本信息提取失败（" & VersionCode & "）", ex)
                End Try
            Next
        Catch ex As Exception
            Throw New Exception("版本列表解析失败（" & Result & "）", ex)
        End Try
        If Versions.Count = 0 Then Throw New Exception("没有可用版本")
        Loader.Output = Versions
    End Sub

    ''' <summary>
    ''' Forge 版本列表，BMCLAPI。
    ''' </summary>
    Public Sub DlForgeVersionBmclapiMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim Json As JArray = NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/forge/minecraft/" & Loader.Input, IsJson:=True)
        Dim Versions As New List(Of DlForgeVersionEntry)
        Try
            Dim Recommended As String = McDownloadForgeRecommendedGet(Loader.Input)
            For Each Token As JObject In Json
                '分类与 Hash 获取
                Dim Hash As String = Nothing, Category As String = "unknown", Proi As Integer = -1
                For Each File As JObject In Token("files")
                    Select Case File("category").ToString
                        Case "installer"
                            If File("format").ToString = "jar" Then
                                '类型为 installer.jar，支持范围 ~753 (~ 1.6.1 部分), 738~684 (1.5.2 全部)
                                Hash = File("hash")
                                Category = "installer"
                                Proi = 2
                            End If
                        Case "universal"
                            If Proi <= 1 AndAlso File("format").ToString = "zip" Then
                                '类型为 universal.zip，支持范围 751~449 (1.6.1 部分), 682~183 (1.5.1 ~ 1.3.2 部分)
                                Hash = File("hash")
                                Category = "universal"
                                Proi = 1
                            End If
                        Case "client"
                            If Proi <= 0 AndAlso File("format").ToString = "zip" Then
                                '类型为 client.zip，支持范围 182~ (1.3.2 部分 ~)
                                Hash = File("hash")
                                Category = "client"
                                Proi = 0
                            End If
                    End Select
                Next
                '获取 Entry
                Dim Inherit As String = Loader.Input
                Dim Branch As String = Token("branch")
                Dim Name As String = Token("version")
                If Name = "11.15.1.2318" OrElse Name = "11.15.1.1902" OrElse Name = "11.15.1.1890" Then Branch = "1.8.9"
                If Branch Is Nothing AndAlso Inherit = "1.7.10" AndAlso Name.Split(".")(3) >= 1300 Then Branch = "1.7.10"
                '基础信息获取
                Dim Entry = New DlForgeVersionEntry With {.Hash = Hash, .Category = Category, .Version = Name, .Branch = Branch, .Inherit = Inherit, .IsRecommended = Recommended = Name}
                Dim TimeSplit = Token("modified").ToString.Split("-"c, "T"c, ":"c, "."c, " "c, "/"c)
                Entry.ReleaseTime = Token("modified").ToObject(Of Date).ToLocalTime.ToString("yyyy/MM/dd HH:mm")
                '添加项
                Versions.Add(Entry)
            Next
        Catch ex As Exception
            Throw New Exception("版本列表解析失败（" & Json.ToString & "）", ex)
        End Try
        If Versions.Count = 0 Then Throw New Exception("没有可用版本")
        Loader.Output = Versions
    End Sub

#End Region

#Region "DlLiteLoaderList | LiteLoader 版本列表"

    Public Structure DlLiteLoaderListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As List(Of DlLiteLoaderListEntry)
        ''' <summary>
        ''' 官方源的失败原因。若没有则为 Nothing。
        ''' </summary>
        Public OfficialError As Exception
    End Structure
    Public Class DlLiteLoaderListEntry
        ''' <summary>
        ''' 实际的文件名，如“liteloader-installer-1.12-00-SNAPSHOT.jar”。
        ''' </summary>
        Public FileName As String
        ''' <summary>
        ''' 是否为测试版。
        ''' </summary>
        Public IsPreview As Boolean
        ''' <summary>
        ''' 对应的 Minecraft 版本，如“1.12.2”。
        ''' </summary>
        Public Inherit As String
        ''' <summary>
        ''' 是否为 1.7 及更早的远古版。
        ''' </summary>
        Public IsLegacy As Boolean
        ''' <summary>
        ''' 发布时间，格式为“yyyy/mm/dd HH:mm:ss”。
        ''' </summary>
        Public ReleaseTime As String
        ''' <summary>
        ''' 文件的 MD5。
        ''' </summary>
        Public MD5 As String
        ''' <summary>
        ''' 对应的 Json 项。
        ''' </summary>
        Public JsonToken As JToken
    End Class

    ''' <summary>
    ''' LiteLoader 版本列表，主加载器。
    ''' </summary>
    Public DlLiteLoaderListLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Main", AddressOf DlLiteLoaderListMain)
    Private Sub DlLiteLoaderListMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Select Case Setup.Get("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListOfficialLoader, 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListBmclapiLoader, 35)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListBmclapiLoader, 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    ''' <summary>
    ''' LiteLoader 版本列表，官方源。
    ''' </summary>
    Public DlLiteLoaderListOfficialLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Official", AddressOf DlLiteLoaderListOfficialMain)
    Private Sub DlLiteLoaderListOfficialMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Dim Result As JObject = NetGetCodeByRequestRetry("http://dl.liteloader.com/versions/versions.json", IsJson:=True)
        Try
            Dim Json As JObject = Result("versions")
            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Pair As KeyValuePair(Of String, JToken) In Json
                If Pair.Key.StartsWith("1.6") OrElse Pair.Key.StartsWith("1.5") Then Continue For
                Dim RealEntry As JToken = If(Pair.Value("artefacts"), Pair.Value("snapshots"))("com.mumfrey:liteloader")("latest")
                Versions.Add(New DlLiteLoaderListEntry With {
                             .Inherit = Pair.Key,
                             .IsLegacy = Pair.Key.Split(".")(1) < 8,
                             .IsPreview = RealEntry("stream").ToString.ToLower = "snapshot",
                             .FileName = "liteloader-installer-" & Pair.Key & If(Pair.Key = "1.8" OrElse Pair.Key = "1.9", ".0", "") & "-00-SNAPSHOT.jar",
                             .MD5 = RealEntry("md5"),
                             .ReleaseTime = GetLocalTime(GetDate(RealEntry("timestamp"))).ToString("yyyy/MM/dd HH:mm:ss"),
                             .JsonToken = RealEntry
                         })
            Next
            Loader.Output = New DlLiteLoaderListResult With {.IsOfficial = True, .SourceName = "LiteLoader 官方源", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("版本列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

    ''' <summary>
    ''' LiteLoader 版本列表，BMCLAPI。
    ''' </summary>
    Public DlLiteLoaderListBmclapiLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Bmclapi", AddressOf DlLiteLoaderListBmclapiMain)
    Private Sub DlLiteLoaderListBmclapiMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Dim Result As JObject = NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/versions.json", IsJson:=True)
        Try
            Dim Json As JObject = Result("versions")
            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Pair As KeyValuePair(Of String, JToken) In Json
                If Pair.Key.StartsWith("1.6") OrElse Pair.Key.StartsWith("1.5") Then Continue For
                Dim RealEntry As JToken = If(Pair.Value("artefacts"), Pair.Value("snapshots"))("com.mumfrey:liteloader")("latest")
                Versions.Add(New DlLiteLoaderListEntry With {
                             .Inherit = Pair.Key,
                             .IsLegacy = Pair.Key.Split(".")(1) < 8,
                             .IsPreview = RealEntry("stream").ToString.ToLower = "snapshot",
                             .FileName = "liteloader-installer-" & Pair.Key & If(Pair.Key = "1.8" OrElse Pair.Key = "1.9", ".0", "") & "-00-SNAPSHOT.jar",
                             .MD5 = RealEntry("md5"),
                             .ReleaseTime = GetLocalTime(GetDate(RealEntry("timestamp"))).ToString("yyyy/MM/dd HH:mm:ss"),
                             .JsonToken = RealEntry
                         })
            Next
            Loader.Output = New DlLiteLoaderListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("版本列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

#End Region

#Region "DlFabricList | Fabric 列表"

    Public Structure DlFabricListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As JObject
    End Structure

    ''' <summary>
    ''' Fabric 列表，主加载器。
    ''' </summary>
    Public DlFabricListLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Main", AddressOf DlFabricListMain)
    Private Sub DlFabricListMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Select Case Setup.Get("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListOfficialLoader, 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListBmclapiLoader, 35)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListBmclapiLoader, 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    ''' <summary>
    ''' Fabric 列表，官方源。
    ''' </summary>
    Public DlFabricListOfficialLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Official", AddressOf DlFabricListOfficialMain)
    Private Sub DlFabricListOfficialMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Dim Result As JObject = NetGetCodeByRequestRetry("https://meta.fabricmc.net/v2/versions", IsJson:=True)
        Try
            Dim Output = New DlFabricListResult With {.IsOfficial = True, .SourceName = "Fabric 官方源", .Value = Result}
            If Output.Value("game") Is Nothing OrElse Output.Value("loader") Is Nothing OrElse Output.Value("installer") Is Nothing Then Throw New Exception("获取到的列表缺乏必要项")
            Loader.Output = Output
        Catch ex As Exception
            Throw New Exception("列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Fabric 列表，BMCLAPI。
    ''' </summary>
    Public DlFabricListBmclapiLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Bmclapi", AddressOf DlFabricListBmclapiMain)
    Private Sub DlFabricListBmclapiMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Dim Result As JObject = NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/fabric-meta/v2/versions", IsJson:=True)
        Try
            Dim Output = New DlFabricListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Result}
            If Output.Value("game") Is Nothing OrElse Output.Value("loader") Is Nothing OrElse Output.Value("installer") Is Nothing Then Throw New Exception("获取到的列表缺乏必要项")
            Loader.Output = Output
        Catch ex As Exception
            Throw New Exception("列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Fabric API 列表，官方源。
    ''' </summary>
    Public DlFabricApiLoader As New LoaderTask(Of Integer, List(Of DlCfFile))("Fabric API List Loader",
        Sub(Task As LoaderTask(Of Integer, List(Of DlCfFile))) Task.Output = DlCfGetFiles(306612, False))

    ''' <summary>
    ''' OptiFabric 列表，官方源。
    ''' </summary>
    Public DlOptiFabricLoader As New LoaderTask(Of Integer, List(Of DlCfFile))("OptiFabric List Loader",
        Sub(Task As LoaderTask(Of Integer, List(Of DlCfFile))) Task.Output = DlCfGetFiles(322385, False))

#End Region

#Region "DlCfProject | CurseForge 工程"

    Private DlCfProjectDb As JObject = Nothing
    Private DlCfProjectCache As New Dictionary(Of Integer, DlCfProject)

    ''' <summary>
    ''' CurseForge 工程列表获取、搜索请求。
    ''' </summary>
    Public Class DlCfProjectRequest
        Public CategoryId As Integer = 0 '421: Mod(?), 4780: Fabric
        Public IsModPack As Boolean = False
        Public GameVersion As String = Nothing
        Public PageSize As String = 50
        Public SearchFilter As String = Nothing
        Public Index As Integer? = Nothing
        Public ModLoader As Integer? = Nothing
        Public Function GetAddress() As String
            GetAddress = "https://api.curseforge.com/v1/mods/search?gameId=432&sortField=Featured&sortOrder=desc" &
                         "&pageSize=" & PageSize &
                         "&categoryId=" & CategoryId &
                         If(IsModPack, "&classId=4471", "&classId=6") &
                         If(String.IsNullOrEmpty(GameVersion), "", "&gameVersion=" & GameVersion) &
                         If(String.IsNullOrEmpty(SearchFilter), "", "&searchFilter=" & Net.WebUtility.UrlEncode(SearchFilter)) &
                         If(Index IsNot Nothing, "&index=" & Index, "") &
                         If(ModLoader IsNot Nothing AndAlso ModLoader > 0, "&modLoaderType=" & ModLoader, "")
        End Function

#Region "相同判断"

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim request = TryCast(obj, DlCfProjectRequest)
            Return request IsNot Nothing AndAlso request.GetAddress = GetAddress()
        End Function
        Public Shared Operator =(left As DlCfProjectRequest, right As DlCfProjectRequest) As Boolean
            Return EqualityComparer(Of DlCfProjectRequest).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As DlCfProjectRequest, right As DlCfProjectRequest) As Boolean
            Return Not left = right
        End Operator

#End Region

    End Class

    ''' <summary>
    ''' CurseForge 工程信息。
    ''' </summary>
    Public Class DlCfProject

        Public Id As Integer
        Public Name As String
        Public McWikiId As Integer = 0
        Public Description As String
        Public Website As String
        Public DateUpdate As Date
        Public DownloadCount As Integer
        Public ModLoaders As New List(Of String)
        Public GameVersionDesc As String
        Private CategoryDesc As List(Of String)
        Private Thumb As String
        Public IsModPack As Boolean
        Public FileIndexes As New List(Of Integer)
        Public Files As List(Of DlCfFile)
        Private _MCBBS As String
        Public Property MCBBS As String
            Get
                If DlCfProjectDb Is Nothing Then
                    DlCfProjectDb = GetJson(DecodeBytes(GetResources("ModData")))
                End If
                If _MCBBS Is Nothing Then
                    If Website IsNot Nothing Then
                        Dim Keyword As String = Website.TrimEnd("/").Split("/").Last
                        If DlCfProjectDb.ContainsKey(Keyword) Then
                            Dim Result As String() = DlCfProjectDb(Keyword).ToString.Split("|")
                            If Result.Length = 3 Then _MCBBS = Result.Last
                        End If
                    End If
                End If
                Return _MCBBS
            End Get
            Set(value As String)
                _MCBBS = value
            End Set
        End Property
        Private _ChineseName As String
        Public Property ChineseName As String
            Get
                If DlCfProjectDb Is Nothing Then
                    DlCfProjectDb = GetJson(DecodeBytes(GetResources("ModData")))
                End If
                If _ChineseName Is Nothing Then
                    _ChineseName = Name
                    If Website IsNot Nothing Then
                        Dim Keyword As String = Website.TrimEnd("/").Split("/").Last
                        If DlCfProjectDb.ContainsKey(Keyword) Then
                            Dim Result As String = DlCfProjectDb(Keyword)
                            McWikiId = Result.Split("|")(0)
                            If Not Result.Split("|")(1) = "~" Then '使用原名
                                _ChineseName = Result.Split("|")(1) '& " (" & Name & ")"
                            End If
                        End If
                    End If
                End If
                Return _ChineseName
            End Get
            Set(ByVal value As String)
                _ChineseName = value
            End Set
        End Property

        ''' <summary>
        ''' 从 CurseForge 工程 Json 项中初始化实例。若出错会抛出异常。
        ''' </summary>
        Public Sub New(Data As JObject)
            'https://api.curseforge.com/v1/mods/search?gameId=432&pageSize=5&categoryID=421
            Id = Data("id")
            Name = Data("name")
            Description = Data("summary")
            Website = Data("links")("websiteUrl")
            DateUpdate = Data("dateModified")
            DownloadCount = Data("downloadCount")
            IsModPack = Not Website.Contains("/mc-mods/")
            '获取常见文件
            For Each File In If(Data("latestFiles"), {})
                Dim NewFile As New DlCfFile(File, IsModPack)
                If Not NewFile.IsAvailable Then Continue For
            Next
            '获取游戏版本、Mod Loader
            Dim GameVersions As New List(Of Integer)
            ModLoaders = New List(Of String)
            For Each File In If(Data("latestFilesIndexes"), {})
                Dim Version As String = File("gameVersion")
                If Not Version.Contains("1.") Then Continue For
                GameVersions.Add(Version.Split(".")(1).Split("-").First)
                Select Case If(File("modLoader"), "0").ToString
                    Case 1
                        ModLoaders.Add("Forge")
                    Case 2
                        ModLoaders.Add("Cauldron")
                    Case 3
                        ModLoaders.Add("LiteLoader")
                    Case 4
                        ModLoaders.Add("Fabric")
                End Select
                FileIndexes.Add(File("fileId"))
            Next
            GameVersions = Sort(GameVersions.Distinct.ToList, AddressOf VersionSortBoolean)
            ModLoaders = ModLoaders.Distinct.ToList
            ModLoaders.Sort()
            If GameVersions.Count = 0 Then
                GameVersionDesc = ""
            Else
                Dim SpaVersions As New List(Of String)
                For i = 0 To GameVersions.Count - 1
                    Dim StartVersion As Integer = GameVersions(i), EndVersion As Integer = GameVersions(i)
                    For ii = i + 1 To GameVersions.Count - 1
                        If GameVersions(ii) = EndVersion - 1 Then
                            EndVersion = GameVersions(ii)
                            i = ii
                        Else
                            Exit For
                        End If
                    Next
                    If StartVersion = EndVersion Then
                        SpaVersions.Add("1." & StartVersion)
                    Else
                        SpaVersions.Add("1." & StartVersion & "-1." & EndVersion)
                    End If
                Next
                GameVersionDesc = "[" & Join(SpaVersions, ", ") & "] "
            End If
            '获取分类
            Dim Categories = Data("categories")
            Dim CategoryId As New List(Of Integer)
            For Each CategoryToken In Categories
                CategoryId.Add(CategoryToken("id"))
            Next
            CategoryId = ArrayNoDouble(CategoryId)
            CategoryId.Sort()
            CategoryDesc = New List(Of String)
            For Each Category In CategoryId
                Dim Desc As String
                Select Case Category

                    Case 406 : Desc = "世界生成"
                    Case 407 : Desc = "生物群系"
                    Case 409 : Desc = "天然结构"
                    Case 410 : Desc = "维度"
                    Case 411 : Desc = "生物"
                    Case 412 : Desc = "科技"
                    Case 414 : Desc = "交通与移动"
                    Case 415 : Desc = "管道与物流"
                    Case 417 : Desc = "能源"
                    Case 4558 : Desc = "红石"
                    Case 420 : Desc = "仓储"
                    Case 416 : Desc = "农业"
                    Case 436 : Desc = "食物"
                    Case 419 : Desc = "魔法"
                    Case 434 : Desc = "装备与工具"
                    Case 422 : Desc = "冒险与探索"
                    Case 424 : Desc = "建筑与装饰"
                    Case 423 : Desc = "信息显示"
                    Case 425 : Desc = "杂项"
                    Case 421 : Desc = "支持库"
                    Case 435 : Desc = "服务器"

                    Case 4471 : Desc = "科幻"
                    Case 4483 : Desc = "战斗"
                    Case 4477 : Desc = "小游戏"
                    Case 4478 : Desc = "任务"
                    Case 4484 : Desc = "多人"
                    Case 4476 : Desc = "探索"
                    Case 4736 : Desc = "空岛"
                    Case 4475 : Desc = "冒险"
                    Case 4487 : Desc = "FTB"
                    Case 4480 : Desc = "基于地图"
                    Case 4479 : Desc = "硬核"
                    Case 4472 : Desc = "科技"
                    Case 4473 : Desc = "魔法"
                    Case 4481 : Desc = "小型整合"
                    Case 4482 : Desc = "大型整合"

                    Case Else : Continue For
                End Select
                CategoryDesc.Add(Desc)
            Next
            If CategoryDesc.Count = 0 Then CategoryDesc.Add("杂类")
            '获取图片
            If Data("logo").Count > 0 Then
                If GetPixelSize(1) > 1.25 Then
                    Thumb = Data("logo")("thumbnailUrl").ToString '使用 256x256 图标
                Else
                    Thumb = Data("logo")("thumbnailUrl").ToString.Replace("/256/256/", "/64/64/") '使用 64x64 图标
                End If
            End If
            '保存缓存
            If Not DlCfProjectCache.ContainsKey(Id) OrElse DlCfProjectCache(Id).Files Is Nothing Then DictionaryAdd(DlCfProjectCache, Id, Me)
        End Sub

        Public Function ToCfItem(ShowVersionDesc As Boolean, ShowLoaderDesc As Boolean, Optional OnClick As MyCfItem.ClickEventHandler = Nothing) As MyCfItem
            Dim NewItem As New MyCfItem With {.Tag = Me}
            NewItem.LabTitle.Text = ChineseName
            NewItem.LabInfo.Text = Description.Replace(vbCr, "").Replace(vbLf, "")
            NewItem.LabLeft.Text = If(ModLoaders.Count > 0 AndAlso ShowLoaderDesc, "[" & Join(ModLoaders, " & ") & "] ", "") &
                                   If(ShowVersionDesc, GameVersionDesc, "") &
                                   Join(CategoryDesc, "，") & " (" &
                                   GetTimeSpanString(DateUpdate - Date.Now) & "更新" &
                                   If(DownloadCount > 0, "，" & If(DownloadCount > 100000, Math.Round(DownloadCount / 10000) & " 万次下载）", DownloadCount & " 次下载）"), "")
            If Thumb Is Nothing Then
                NewItem.Logo = "pack://application:,,,/images/Icons/NoIcon.png"
            Else
                NewItem.Logo = Thumb
            End If
            If OnClick IsNot Nothing Then
                AddHandler NewItem.Click, OnClick
                NewItem.HasAnimation = True
            Else
                NewItem.HasAnimation = False
            End If
            '结束
            Return NewItem
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim project = TryCast(obj, DlCfProject)
            Return project IsNot Nothing AndAlso Id = project.Id
        End Function
        Public Shared Operator =(left As DlCfProject, right As DlCfProject) As Boolean
            Return EqualityComparer(Of DlCfProject).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As DlCfProject, right As DlCfProject) As Boolean
            Return Not left = right
        End Operator
    End Class

    Public Class DlCfProjectResult
        Public Projects As List(Of DlCfProject)
        Public Index As Integer
        Public RealCount As Long
    End Class
    ''' <summary>
    ''' CurseForge 工程列表获取事件。
    ''' </summary>
    Public Sub DlCfProjectSub(Task As LoaderTask(Of DlCfProjectRequest, DlCfProjectResult))
        Dim RawFilter As String = If(Task.Input.SearchFilter, "")
        Task.Input.SearchFilter = RawFilter
        Log("[Download] CurseForge 工程列表搜索原始文本：" & RawFilter)
        '中文请求关键字处理
        Dim IsChineseSearch As Boolean = RegexCheck(RawFilter, "[\u4e00-\u9fbb]")
        If IsChineseSearch AndAlso Not String.IsNullOrEmpty(RawFilter) Then
            If Task.Input.IsModPack Then Throw New Exception("整合包搜索仅支持英文")
            '释放资料库文件
            If DlCfProjectDb Is Nothing Then
                DlCfProjectDb = GetJson(DecodeBytes(GetResources("ModData")))
            End If
            '构造搜索请求
            Dim SearchEntries As New List(Of SearchEntry(Of String))
            For Each Entry In DlCfProjectDb
                If Entry.Value.ToString.Contains("动态的树") Then Continue For '傻逼 Mod 的附属太多了
                SearchEntries.Add(New SearchEntry(Of String) With {
                    .Item = Entry.Value.ToString.Split("|")(1),
                    .SearchSource = New List(Of KeyValuePair(Of String, Double)) From {
                        New KeyValuePair(Of String, Double)(Entry.Value.ToString.Replace(" (", "|").Split("|")(1), 1)
                    }
                })
            Next
            '获取搜索结果
            Dim SearchResults = Search(SearchEntries, Task.Input.SearchFilter, 3)
            If SearchResults.Count = 0 Then Throw New Exception("无搜索结果，请尝试搜索英文名称")
            Dim SearchResult As String = ""
            For i = 0 To SearchResults.Count - 1
                If Not SearchResults(i).AbsoluteRight AndAlso i >= Math.Min(2, SearchResults.Count - 1) Then Exit For '把 3 个结果拼合以提高准确度
                SearchResult += SearchResults(i).Item.Replace(" (", "|").Split("|").Last.TrimEnd(")") & " "
            Next
            Log("[Download] CurseForge 工程列表中文搜索原始关键词：" & SearchResult, LogLevel.Developer)
            '去除常见连接词
            Dim RealFilter As String = ""
            For Each Word In SearchResult.Split(" ")
                If {"the", "of", "a", "mod"}.Contains(Word.ToLower) Then Continue For
                If SearchResult.Split(" ").Count > 3 AndAlso {"ftb"}.Contains(Word.ToLower) Then Continue For
                RealFilter += Word & " "
            Next
            Task.Input.SearchFilter = RealFilter
            Log("[Download] CurseForge 工程列表中文搜索最终关键词：" & RealFilter, LogLevel.Developer)
        End If
        '驼峰英文请求关键字处理
        Dim SpacedKeywords = RegexReplace(Task.Input.SearchFilter, "$& ", "([A-Z]+|[a-z]+?)(?=[A-Z]+[a-z]+[a-z ]*)")
        Dim ConnectedKeywords = Task.Input.SearchFilter.Replace(" ", "")
        Dim AllPossibleKeywords = SpacedKeywords & " " & If(IsChineseSearch, Task.Input.SearchFilter, ConnectedKeywords & " " & RawFilter)
        '最终处理关键字：分割、去重
        Dim RightKeywords As New List(Of String)
        For Each Keyword In AllPossibleKeywords.Split(" ")
            If Keyword.Trim = "" Then Continue For
            RightKeywords.Add(Keyword)
        Next
        Task.Input.SearchFilter = Join(ArrayNoDouble(RightKeywords), " ").ToLower
        Log("[Download] CurseForge 工程列表搜索最终文本：" & Task.Input.SearchFilter, LogLevel.Developer)
        '正式获取
        Dim Url = Task.Input.GetAddress()
        Log("[Download] 开始获取 CurseForge 工程列表：" & Url)
        Dim RequestResult As JObject = NetGetCodeByRequestRetry(Url, IsJson:=True, Encode:=Encoding.UTF8)
        Dim FileList = GetCfProjectListFromJson(RequestResult("data"), Task.Input.IsModPack)
        If FileList.Count = 0 Then Throw New Exception("无搜索结果")
        '重新排序
        If Not String.IsNullOrEmpty(Task.Input.SearchFilter) Then
            '排序分 = 搜索相对相似度 + 默认排序相对活跃度
            Dim Scores As New Dictionary(Of DlCfProject, Double)
            Dim Entry As New List(Of SearchEntry(Of DlCfProject))
            For i = 1 To FileList.Count
                Dim File = FileList(i - 1)
                Scores.Add(File, (1 - (i / FileList.Count)))
                Entry.Add(New SearchEntry(Of DlCfProject) With {.Item = File, .SearchSource = New List(Of KeyValuePair(Of String, Double)) From {
                          New KeyValuePair(Of String, Double)(If(IsChineseSearch, File.ChineseName, File.Name), 1),
                          New KeyValuePair(Of String, Double)(File.Description, 0.05)}})
            Next
            Dim SearchResult = Search(Entry, RawFilter, 101, -1)
            For Each OneResult In SearchResult
                Scores(OneResult.Item) += OneResult.Similarity / SearchResult(0).Similarity
            Next
            '根据排序分得出结果
            Dim ResultList = Sort(Scores.ToList, Function(Left As KeyValuePair(Of DlCfProject, Double), Right As KeyValuePair(Of DlCfProject, Double))
                                                     Return Left.Value > Right.Value
                                                 End Function)
            FileList = New List(Of DlCfProject)
            For Each Result In ResultList
                FileList.Add(Result.Key)
            Next
        End If
        Task.Output = New DlCfProjectResult With {.Projects = FileList, .Index = RequestResult("pagination")("index"), .RealCount = RequestResult("pagination")("totalCount")}
    End Sub
    ''' <summary>
    ''' 从 API 返回的 Json 数组中获取工程信息数组。
    ''' </summary>
    Private Function GetCfProjectListFromJson(Json As JArray, IsModPack As Boolean) As List(Of DlCfProject)
        Dim FileList As New List(Of DlCfProject)
        For Each JsonEntry As JObject In Json
            Dim Result = GetCfProjectFromJson(JsonEntry, IsModPack)
            If Result IsNot Nothing Then FileList.Add(Result)
        Next
        Return FileList
    End Function
    ''' <summary>
    ''' 从 API 返回的 Json 中获取工程信息。
    ''' </summary>
    Private Function GetCfProjectFromJson(Json As JObject, IsModPack As Boolean) As DlCfProject
        If Json("links")("websiteUrl") Is Nothing Then
            Log("[Download] 发现关键项为 Nothing 的工程：" & If(Json, "").ToString, LogLevel.Debug)
            Return Nothing
        End If
        If IsModPack = Json("links")("websiteUrl").ToString.Contains("curseforge.com/minecraft/mc-mods") Then
            Log("[Download] 返回的工程与要求的类别不一致：" & Json("links")("websiteUrl").ToString, LogLevel.Debug)
            Return Nothing
        End If
        Return New DlCfProject(Json)
    End Function

#End Region

#Region "DlCfFile | CurseForge Files"

    Public DlCfFileLoader As New LoaderTask(Of KeyValuePair(Of Integer, Boolean), List(Of DlCfFile))("DlCfFile Main", Sub(Task As LoaderTask(Of KeyValuePair(Of Integer, Boolean), List(Of DlCfFile))) Task.Output = DlCfGetFiles(Task.Input.Key, Task.Input.Value))

    ''' <summary>
    ''' CurseForge 工程下的单一版本文件。
    ''' </summary>
    Public Class DlCfFile

        Public IsModPack As Boolean
        Public FileId As Integer
        Public DisplayName As String
        Public [Date] As Date
        Public GameVersion As String()
        Public ReleaseType As Integer
        Public FileName As String
        Public DownloadCount As Integer
        Public Dependencies As New List(Of Integer)
        Private DownloadAddress As String()

        Public ReadOnly Property ReleaseTypeString As String
            Get
                Select Case ReleaseType
                    Case 1
                        Return "正式版"
                    Case 2
                        Return If(ModeDebug, "Beta 测试版", "测试版")
                    Case Else
                        Return If(ModeDebug, "Alpha 测试版", "测试版")
                End Select
            End Get
        End Property
        Public ReadOnly Property IsAvailable As Boolean
            Get
                Return DisplayName IsNot Nothing AndAlso GameVersion IsNot Nothing AndAlso FileName IsNot Nothing AndAlso DownloadAddress(0).StartsWith("http")
            End Get
        End Property

        ''' <summary>
        ''' 从 CurseForge 文件 Json 项中初始化实例。若出错会抛出异常。
        ''' </summary>
        Public Sub New(Data As JObject, IsModPack As Boolean)
            FileId = Data("id")
            DisplayName = Data("displayName").ToString.Replace("	", "").Trim(" ")
            [Date] = Data("fileDate")
            ReleaseType = Data("releaseType")
            DownloadCount = Data("downloadCount")
            FileName = Data("fileName")
            Me.IsModPack = IsModPack
            '获取下载地址
            Dim Url = Data("downloadUrl").ToString
            If Url = "" Then Url = "https://media.forgecdn.net/files/" & FileId.ToString.Substring(0, 4) & "/" & FileId.ToString.Substring(4).TrimStart("0") & "/" & FileName
            Url = Url.Replace(FileName, Net.WebUtility.UrlEncode(FileName)) '对文件名进行编码
            DownloadAddress = ArrayNoDouble({Url.Replace("-service.overwolf.wtf", ".forgecdn.net").Replace("://edge", "://media"), Url.Replace("-service.overwolf.wtf", ".forgecdn.net"), Url.Replace("://edge", "://media"), Url})
            '获取前置 Mod 的 Addon ID
            If Not IsModPack Then
                For Each Dep In Data("dependencies")
                    If Dep("relationType").ToObject(Of Integer) = 3 AndAlso '是一个依赖
                        Dep("modId").ToObject(Of Integer) <> 306612 Then '排除 Fabric API
                        Dependencies.Add(Dep("modId"))
                    End If
                Next
            End If
            '获取游戏版本
            Dim Versions As New List(Of String)
            For Each Version In Data("gameVersions")
                If Version.ToString.StartsWith("1.") OrElse Version.ToString.Contains("w") Then
                    Versions.Add(Version.ToString.Trim.ToLower)
                End If
            Next
            If Versions.Count > 1 Then
                GameVersion = Sort(Versions, AddressOf VersionSortBoolean).ToArray
                If IsModPack Then GameVersion = {GameVersion(0)}
            ElseIf Versions.Count = 1 Then
                GameVersion = Versions.ToArray
                'ElseIf Data("gameVersion").Count = 1 AndAlso Not Data("gameVersion")(0).ToString.Contains("1.") Then
                '    GameVersion = {"1.16.4"}
            Else
                GameVersion = {"未知版本"}
            End If
        End Sub

        ''' <summary>
        ''' 获取对应的下载文件信息。
        ''' </summary>
        ''' <param name="LocalAddress">本地文件夹或文件地址。</param>
        Public Function GetDownloadFile(LocalAddress As String, IsFullPath As Boolean) As NetFile
            Return New NetFile(DownloadAddress, LocalAddress & If(IsFullPath, "", FileName))
        End Function

        Public Function ToListItem(OnClick As MyListItem.ClickEventHandler, Optional OnSaveClick As MyIconButton.ClickEventHandler = Nothing) As MyListItem

            '获取描述信息
            Dim Info As String = ""
            If Not IsModPack Then
                Info += "适用于 " & Join(GameVersion, "、").Replace("-snapshot", " 快照") &
                          If(ModeDebug AndAlso Dependencies.Count > 0, "，" & Dependencies.Count & " 个前置，", "，")
            End If
            Info += If(DownloadCount > 0, If(DownloadCount > 100000, Math.Round(DownloadCount / 10000) & " 万次下载，", DownloadCount & " 次下载，"), "")
            Info += GetTimeSpanString([Date] - Date.Now) & "更新"
            Info += If(ReleaseType <> 1, "，" & ReleaseTypeString, "")

            '建立控件
            Dim NewItem As New MyListItem With {
                .Title = DisplayName, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Me,
                .Info = Info
            }
            Select Case ReleaseType
                Case 1 'R
                    NewItem.Logo = "pack://application:,,,/images/Icons/R.png"
                Case 2 'B
                    NewItem.Logo = "pack://application:,,,/images/Icons/B.png"
                Case Else 'A
                    NewItem.Logo = "pack://application:,,,/images/Icons/A.png"
            End Select
            AddHandler NewItem.Click, OnClick

            '建立另存为按钮
            If OnSaveClick IsNot Nothing Then
                Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
                ToolTipService.SetPlacement(BtnSave, Primitives.PlacementMode.Center)
                ToolTipService.SetVerticalOffset(BtnSave, 30)
                ToolTipService.SetHorizontalOffset(BtnSave, 2)
                AddHandler BtnSave.Click, OnSaveClick
                NewItem.Buttons = {BtnSave}
                NewItem.PaddingRight = 35
            End If

            '结束
            Return NewItem
        End Function
        Public Overrides Function ToString() As String
            Return DisplayName
        End Function

    End Class

    ''' <summary>
    ''' 获取某个 CurseForge 工程下的全部文件列表。
    ''' 必须在工作线程执行，失败会抛出异常。
    ''' </summary>
    Public Function DlCfGetFiles(ProjectId As Integer, IsModPack As Boolean) As List(Of DlCfFile)
        '获取工程对象
        Dim TargetProject As DlCfProject
        If DlCfProjectCache.ContainsKey(ProjectId) Then
            TargetProject = DlCfProjectCache(ProjectId)
        Else
            TargetProject = GetCfProjectFromJson(NetGetCodeByRequestRetry("https://api.curseforge.com/v1/mods/" & ProjectId, IsJson:=True, Encode:=Encoding.UTF8)("data"), IsModPack)
        End If
        '获取工程对象的文件列表
        If TargetProject.Files Is Nothing Then
            Log("[Download] 开始获取 CurseForge 工程 ID 为 " & ProjectId & " 的文件列表")
            Dim Json As JArray
            If TargetProject.IsModPack Then
                '整合包使用全部文件
                Json = NetGetCodeByRequestRetry("https://api.curseforge.com/v1/mods/" & ProjectId & "/files?pageSize=999", Accept:="application/json", IsJson:=True)("data")
            Else
                'Mod 使用每个版本最新的文件
                'TODO: 完整 Mod 文件列表支持
                Json = GetJson(NetRequestRetry("https://api.curseforge.com/v1/mods/files", "POST", "{""fileIds"": [" & Join(TargetProject.FileIndexes, ",") & "]}", "application/json"))("data")
            End If
            Dim Files As New Dictionary(Of Integer, DlCfFile)
            For Each JsonEntry As JObject In Json
                Dim NewFile As New DlCfFile(JsonEntry, IsModPack)
                If NewFile.IsAvailable AndAlso Not Files.ContainsKey(NewFile.FileId) Then Files.Add(NewFile.FileId, NewFile)
            Next
            TargetProject.Files = Files.Values.ToList
        End If
        '获取前置 Mod 列表
        If IsModPack Then GoTo ExitSub
        Dim Deps As New List(Of Integer)
        For Each File In TargetProject.Files
            For Each AddonId In File.Dependencies
                If Not Deps.Contains(AddonId) AndAlso Not DlCfProjectCache.ContainsKey(AddonId) Then Deps.Add(AddonId)
            Next
        Next
        If Deps.Count > 0 Then
            '获取前置 Mod 工程信息
            Log("[Download] 文件列表中需要获取的前置 Mod：" & Join(Deps, "，"))
            GetCfProjectListFromJson(GetJson(NetRequestRetry("https://api.curseforge.com/v1/mods", "POST", "{""modIds"": [" & Join(Deps, ",") & "]}", "application/json"))("data"), False)
        End If
        '返回
ExitSub:
        Return TargetProject.Files
    End Function

    Public Sub DlCfFilesPreload(Stack As StackPanel, Entrys As List(Of DlCfFile), OnClick As MyListItem.ClickEventHandler)
        '获取卡片对应的前置 ID
        '如果为整合包就不会有 Dependencies 信息，所以不用管
        Dim Deps As New List(Of Integer)
        For Each File In Entrys
            For Each AddonId In If(File.Dependencies, New List(Of Integer))
                If Not Deps.Contains(AddonId) Then
                    If Not DlCfProjectCache.ContainsKey(AddonId) Then
                        Log("[Download] 未找到 ID 为 " & AddonId & " 的前置 Mod 信息", LogLevel.Developer)
                        Continue For
                    End If
                    Deps.Add(AddonId)
                End If
            Next
        Next
        Deps.Remove(306612) '移除 Fabric API
        If Deps.Count = 0 Then Exit Sub
        Deps.Sort()
        '添加开头间隔
        Stack.Children.Add(New TextBlock With {.Text = "前置 Mod", .FontSize = 14, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 2, 0, 5)})
        '添加前置 Mod 列表
        For Each Dep In Deps
            Dim Item = DlCfProjectCache(Dep).ToCfItem(False, True, AddressOf FrmDownloadMod.ProjectClick)
            'Item.Margin = New Thickness(5, 0, 0, 0)
            Stack.Children.Add(Item)
        Next
        '添加结尾间隔
        Stack.Children.Add(New TextBlock With {.Text = "可选版本", .FontSize = 14, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 12, 0, 5)})
    End Sub

#End Region

#Region "DlSource | 镜像下载源"

    Public Function DlSourceResourceGet(MojangBase As String) As String()
        Return {MojangBase.Replace("http://resources.download.minecraft.net", "https://download.mcbbs.net/assets"),
                MojangBase,
                MojangBase.Replace("http://resources.download.minecraft.net", "https://bmclapi2.bangbang93.com/assets")
               }
    End Function

    Public Function DlSourceLibraryGet(MojangBase As String) As String()
        Return {MojangBase.Replace("https://libraries.minecraft.net", "https://download.mcbbs.net/maven"),
                MojangBase.Replace("https://libraries.minecraft.net", "https://download.mcbbs.net/libraries"),
                MojangBase,
                MojangBase.Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven"),
                MojangBase.Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/libraries")
               }
    End Function

    Public Function DlSourceLauncherOrMetaGet(MojangBase As String, Optional IsStatic As Boolean = True) As String()
        If MojangBase Is Nothing Then Throw New Exception("无对应的 Json 下载地址")
        If IsStatic Then
            Return {MojangBase.Replace("https://launcher.mojang.com", "https://download.mcbbs.net").Replace("https://launchermeta.mojang.com", "https://download.mcbbs.net"),
                    MojangBase,
                    MojangBase.Replace("https://launcher.mojang.com", "https://bmclapi2.bangbang93.com").Replace("https://launchermeta.mojang.com", "https://bmclapi2.bangbang93.com")
                   }
        Else
            Return {MojangBase.Replace("https://launcher.mojang.com", "https://download.mcbbs.net").Replace("https://launchermeta.mojang.com", "https://download.mcbbs.net"),
                    MojangBase,
                    MojangBase.Replace("https://launcher.mojang.com", "https://bmclapi2.bangbang93.com").Replace("https://launchermeta.mojang.com", "https://bmclapi2.bangbang93.com")
                   }
        End If
    End Function

    Private Sub DlSourceLoader(Of InputType, OutputType)(MainLoader As LoaderTask(Of InputType, OutputType),
                                                         LoaderList As List(Of KeyValuePair(Of LoaderTask(Of InputType, OutputType), Integer)),
                                                         Optional IsForceRestart As Boolean = False)
        Dim WaitCycle As Integer = 0
        Do While True
            '检查加载结束
            Dim IsAllFailed As Boolean = True
            For Each SubLoader In LoaderList
                If WaitCycle = 0 Then
                    '如果要强制刷新，就不使用已经加载好的值
                    If IsForceRestart Then Exit For
                    '如果输入不一样，就不使用已经加载好的值
                    If (SubLoader.Key.Input Is Nothing Xor MainLoader.Input Is Nothing) OrElse
                       (SubLoader.Key.Input IsNot Nothing AndAlso Not SubLoader.Key.Input.Equals(MainLoader.Input)) Then Continue For
                End If
                If SubLoader.Key.State <> LoadState.Failed Then IsAllFailed = False
                If SubLoader.Key.State = LoadState.Finished Then
                    MainLoader.Output = SubLoader.Key.Output
                    DlSourceLoaderAbort(LoaderList)
                    Exit Sub
                ElseIf IsAllFailed Then
                    If WaitCycle < SubLoader.Value * 100 Then WaitCycle = SubLoader.Value * 100
                End If
                '由于 Forge BMCLAPI 没有可用版本导致强制失败
                '在没有可用版本时，官方源会一直卡住，直接使用 BMCLAPI 判定失败即可
                If SubLoader.Key.Error IsNot Nothing AndAlso SubLoader.Key.Error.Message.Contains("没有可用版本") Then
                    For Each SubLoader2 In LoaderList
                        If WaitCycle < SubLoader2.Value * 100 Then WaitCycle = SubLoader2.Value * 100
                    Next
                End If
            Next
            '启动加载源
            If WaitCycle = 0 Then
                '启动第一个源
                LoaderList(0).Key.Start(MainLoader.Input, IsForceRestart)
            End If
            For i = 0 To LoaderList.Count - 1
                If WaitCycle = LoaderList(i).Value * 100 Then
                    If i < LoaderList.Count - 1 Then
                        '启动下一个源
                        LoaderList(i + 1).Key.Start(MainLoader.Input, IsForceRestart)
                    Else
                        '失败
                        Dim ErrorInfo As Exception = Nothing
                        For ii = 0 To LoaderList.Count - 1
                            LoaderList(ii).Key.Input = Nothing '重置输入，以免以同样的输入“重试加载”时直接失败
                            If LoaderList(ii).Key.Error IsNot Nothing Then
                                If ErrorInfo Is Nothing OrElse LoaderList(ii).Key.Error.Message.Contains("没有可用版本") Then
                                    ErrorInfo = LoaderList(ii).Key.Error
                                End If
                            End If
                        Next
                        If ErrorInfo Is Nothing Then ErrorInfo = New TimeoutException("下载源连接超时")
                        DlSourceLoaderAbort(LoaderList)
                        Throw ErrorInfo
                    End If
                    Exit For
                End If
            Next
            '计时
            If MainLoader.IsAborted Then
                DlSourceLoaderAbort(LoaderList)
                Exit Sub
            End If
            Thread.Sleep(10)
            WaitCycle += 1
        Loop
    End Sub
    Private Sub DlSourceLoaderAbort(Of InputType, OutputType)(LoaderList As List(Of KeyValuePair(Of LoaderTask(Of InputType, OutputType), Integer)))
        For Each Loader In LoaderList
            If Loader.Key.State = LoadState.Loading Then Loader.Key.Abort()
        Next
    End Sub

#End Region

End Module
