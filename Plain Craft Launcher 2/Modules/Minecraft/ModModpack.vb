Public Module ModModpack

    '触发整合包安装的外部接口
    ''' <summary>
    ''' 弹窗要求选择一个整合包文件并进行安装。
    ''' </summary>
    Public Sub ModpackInstall()
        Dim File As String = SelectFile("压缩包文件(*.rar;*.zip)|*.rar;*.zip", "选择整合包压缩文件") '选择整合包文件
        If String.IsNullOrEmpty(File) Then Exit Sub
        RunInThread(Sub() ModpackInstall(File))
    End Sub
    ''' <summary>
    ''' 安装一个给定的整合包文件，返回是否安装成功。必须在工作线程执行。
    ''' </summary>
    Public Function ModpackInstall(File As String, Optional VersionName As String = Nothing, Optional ShowHint As Boolean = True) As Boolean
        Log("[ModPack] 整合包安装请求：" & If(File, "null"))
        Dim Archive As Compression.ZipArchive = Nothing
        Dim ArchiveBaseFolder As String = ""
        Try
            '获取整合包种类与关键 Json
            Dim PackType As Integer = -1
            Try
                Archive = New Compression.ZipArchive(New FileStream(File, FileMode.Open))
                '从根目录判断整合包类型
                If Archive.GetEntry("mcbbs.packmeta") IsNot Nothing Then PackType = 3 : Exit Try 'MCBBS 整合包（优先于 manifest.json 判断）
                If Archive.GetEntry("manifest.json") IsNot Nothing Then
                    Dim Json As JObject = GetJson(ReadFile(Archive.GetEntry("manifest.json").Open, Encoding.UTF8))
                    If Json("addons") Is Nothing Then
                        PackType = 0 : Exit Try 'CurseForge 整合包
                    Else
                        PackType = 3 : Exit Try 'MCBBS 整合包
                    End If
                End If
                If Archive.GetEntry("modpack.json") IsNot Nothing Then PackType = 1 : Exit Try 'HMCL 整合包
                If Archive.GetEntry("mmc-pack.json") IsNot Nothing Then PackType = 2 : Exit Try 'MMC 整合包
                '从一级目录判断整合包类型
                For Each Entry In Archive.Entries
                    Dim FullNames As String() = Entry.FullName.Split("/")
                    ArchiveBaseFolder = FullNames(0) & "/"
                    If Entry.FullName.EndsWith("/versions/") AndAlso FullNames.Count = 3 Then PackType = 9 : Exit Try '压缩包
                    '确定为一级目录下
                    If FullNames.Count <> 2 Then Continue For
                    '判断是否为关键文件
                    If FullNames(1) = "mcbbs.packmeta" Then PackType = 3 : Exit Try 'MCBBS 整合包（优先于 manifest.json 判断）
                    If FullNames(1) = "manifest.json" Then
                        Dim Json As JObject = GetJson(ReadFile(Entry.Open, Encoding.UTF8))
                        If Json("addons") Is Nothing Then
                            PackType = 0 : Exit Try 'CurseForge 整合包
                        Else
                            PackType = 3 : ArchiveBaseFolder = "overrides/" : Exit Try 'MCBBS 整合包
                        End If
                    End If
                    If FullNames(1) = "modpack.json" Then PackType = 1 : Exit Try 'HMCL 整合包
                    If FullNames(1) = "mmc-pack.json" Then PackType = 2 : Exit Try 'MMC 整合包
                Next
            Catch ex As Exception
                If File.ToLower.EndsWith(".rar") Then
                    Log(ex, "PCL2 无法处理 rar 格式的压缩包，请在解压后重新压缩为 zip 格式再试", If(ShowHint, LogLevel.Hint, LogLevel.Normal))
                    Return False
                Else
                    Log(ex, "打开整合包文件失败，文件可能损坏或为不支持的压缩包格式", If(ShowHint, LogLevel.Hint, LogLevel.Normal))
                    Return False
                End If
            End Try
            '执行对应的安装方法
            Select Case PackType
                Case 0
                    Log("[ModPack] 整合包种类：CurseForge")
                    InstallPackCurseForge(File, Archive, ArchiveBaseFolder, VersionName)
                Case 1
                    Log("[ModPack] 整合包种类：HMCL")
                    InstallPackHMCL(File, Archive, ArchiveBaseFolder)
                Case 2
                    Log("[ModPack] 整合包种类：MMC")
                    InstallPackMMC(File, Archive, ArchiveBaseFolder)
                Case 3
                    Log("[ModPack] 整合包种类：MCBBS")
                    InstallPackMCBBS(File, Archive, ArchiveBaseFolder)
                Case 9
                    Log("[ModPack] 整合包种类：压缩包")
                    Archive.Dispose()
                    Archive = Nothing
                    InstallPackCompress(File, ArchiveBaseFolder)
                Case Else
                    If ShowHint Then
                        Hint("未能识别该整合包的种类，无法安装！", HintType.Critical)
                    Else
                        Log("[ModPack] 未能识别该整合包的种类，无法安装！")
                    End If
                    Return False
            End Select
            Return True
        Catch ex As Exception
            Log(ex, "准备安装整合包失败", LogLevel.Feedback)
            Return False
        Finally
            If Archive IsNot Nothing Then Archive.Dispose()
        End Try
    End Function

    '整合包缓存清理
    Private IsInstallCacheCleared As Boolean = False
    Private IsInstallCacheClearing As Boolean = False
    Private Sub UnpackFiles(InstallTemp As String, FileAddress As String)
        '清理缓存文件夹
        If Not IsInstallCacheCleared Then
            IsInstallCacheCleared = True
            IsInstallCacheClearing = True
            Try
                Log("[ModPack] 开始清理整合包安装缓存")
                DeleteDirectory(PathTemp & "PackInstall\")
                Log("[ModPack] 已清理整合包安装缓存")
            Catch ex As Exception
                Log(ex, "清理整合包安装缓存失败")
            Finally
                IsInstallCacheClearing = False
            End Try
        ElseIf IsInstallCacheClearing Then
            '等待另一个整合包安装的清理步骤完成
            Do While IsInstallCacheClearing
                Thread.Sleep(1)
            Loop
        End If
        '解压文件
        Dim RetryCount As Integer = 1
        Dim Encode = Encoding.Default
        Try
Retry:
            '完全不知道为啥会出现文件正在被另一进程使用的问题，总之多试试
            Directory.CreateDirectory(InstallTemp)
            DeleteDirectory(InstallTemp)
            Compression.ZipFile.ExtractToDirectory(FileAddress, InstallTemp, Encode)
        Catch ex As Exception
            Log(ex, "第 " & RetryCount & " 次解压尝试失败")
            If TypeOf ex Is ArgumentException Then
                Encode = Encoding.UTF8
                Log("[ModPack] 已切换压缩包解压编码为 UTF8")
            End If
            If RetryCount < 5 Then
                Thread.Sleep(RetryCount * 2000)
                RetryCount += 1
                GoTo Retry
            Else
                Throw
            End If
        End Try
    End Sub

#Region "不同类型整合包的安装方法"

    'CurseForge
    ''' <summary>
    ''' 获取安装 CurseForge 整合包的加载器，若失败或跳过则返回 Nothing。
    ''' 加载器以安装目标版本文件夹为输入。
    ''' </summary>
    Private Function InstallPackCurseForgeLoader(FileAddress As String, Archive As Compression.ZipArchive, ArchiveBaseFolder As String, VersionName As String) As LoaderCombo(Of String)
        '读取 Json 文件
        Dim Json As JObject
        Try
            Json = GetJson(ReadFile(Archive.GetEntry(ArchiveBaseFolder & "manifest.json").Open))
            If Json("minecraft") Is Nothing OrElse Json("minecraft")("version") Is Nothing Then Throw New Exception("整合包未提供 Minecraft 版本信息")
        Catch ex As Exception
            Log(ex, "整合包安装信息存在问题", LogLevel.Hint)
            Return Nothing
        End Try
        '获取 Mod API 版本信息
        Dim ForgeVersion As String = Nothing
        Dim FabricVersion As String = Nothing
        For Each Entry In If(Json("minecraft")("modLoaders"), {})
            Dim Id As String = If(Entry("id"), "").ToString.ToLower
            If Id.StartsWith("forge-") Then
                'Forge 指定
                If Id.Contains("recommended") Then
                    Log("[ModPack] 该整合包版本过老，已不支持进行安装！", LogLevel.Hint)
                    Return Nothing
                End If
                Try
                    Log("[ModPack] 整合包 Forge 版本：" & Id)
                    ForgeVersion = Id.Split("-")(1)
                    Exit For
                Catch ex As Exception
                    Log(ex, "读取整合包 Forge 版本失败：" & Id)
                End Try
            ElseIf Id.StartsWith("fabric-") Then
                'Fabric 指定
                Try
                    Log("[ModPack] 整合包 Fabric 版本：" & Id)
                    FabricVersion = Id.Split("-")(1)
                    Exit For
                Catch ex As Exception
                    Log(ex, "读取整合包 Fabric 版本失败：" & Id)
                End Try
            End If
        Next
        '解压与配置文件
        Dim InstallTemp As String = PathTemp & "PackInstall\" & RandomInteger(0, 100000) & "\"
        Dim InstallLoaders As New List(Of LoaderBase)
        Dim OverrideHome As String = If(Json("overrides"), "")
        If OverrideHome <> "" Then
            InstallLoaders.Add(New LoaderTask(Of String, Integer)("解压整合包文件",
                                                                  Sub(Task As LoaderTask(Of String, Integer))
                                                                      UnpackFiles(InstallTemp, FileAddress)
                                                                      Task.Progress = 0.5
                                                                      '复制结果
                                                                      If Directory.Exists(InstallTemp & ArchiveBaseFolder & OverrideHome) Then
                                                                          My.Computer.FileSystem.CopyDirectory(InstallTemp & ArchiveBaseFolder & OverrideHome, PathMcFolder & "versions\" & VersionName)
                                                                      Else
                                                                          Log("[ModPack] 整合包中未找到 override 目录，已跳过")
                                                                      End If
                                                                      Task.Progress = 0.9
                                                                      '开启版本隔离
                                                                      WriteIni(PathMcFolder & "versions\" & VersionName & "\PCL\Setup.ini", "VersionArgumentIndie", 1)
                                                                  End Sub) With {
                                                                  .ProgressWeight = New FileInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False}) '每 6M 需要 1s
        End If
        '获取 Mod 列表
        Dim ModFileList As New List(Of Integer)
        For Each ModEntry In If(Json("files"), {})
            If ModEntry("projectID") Is Nothing OrElse ModEntry("fileID") Is Nothing Then
                Hint("某项 Mod 缺少必要信息，已跳过：" & ModEntry.ToString)
                Continue For
            End If
            If ModEntry("required") IsNot Nothing AndAlso Not ModEntry("required").ToObject(Of Boolean) Then Continue For
            ModFileList.Add(ModEntry("fileID"))
        Next
        If ModFileList.Count > 0 Then
            '获取 Mod 下载信息
            InstallLoaders.Add(New LoaderTask(Of Integer, JArray)(
                               "获取 Mod 下载信息",
                               Sub(Task As LoaderTask(Of Integer, JArray))
                                   Task.Output = GetJson(NetRequestRetry("https://api.curseforge.com/v1/mods/files", "POST", "{""fileIds"": [" & Join(ModFileList, ",") & "]}", "application/json"))("data")
                                   '如果文件已被删除，则 API 会跳过那一项
                                   If ModFileList.Count > Task.Output.Count Then Throw New Exception("整合包所需要的部分 Mod 版本已被 Mod 作者删除，因此无法完成整合包安装，请联系整合包作者更新整合包中的 Mod 版本")
                               End Sub) With {.ProgressWeight = ModFileList.Count / 10}) '每 10 Mod 需要 1s
            '构造 NetFile
            InstallLoaders.Add(New LoaderTask(Of JArray, List(Of NetFile))("构造 Mod 下载信息",
                                                                   Sub(Task As LoaderTask(Of JArray, List(Of NetFile)))
                                                                       Dim FileList As New Dictionary(Of Integer, NetFile)
                                                                       For Each ModJson In Task.Input
                                                                           '跳过重复的 Mod（疑似 CurseForge Bug）
                                                                           If FileList.ContainsKey(ModJson("id").ToObject(Of Integer)) Then Continue For
                                                                           '实际的添加
                                                                           FileList.Add(ModJson("id"), New DlCfFile(ModJson, False).GetDownloadFile(PathMcFolder & "versions\" & VersionName & "\mods\", False))
                                                                           Task.Progress += 1 / (1 + ModFileList.Count)
                                                                       Next
                                                                       Task.Output = FileList.Values.ToList
                                                                   End Sub) With {.ProgressWeight = ModFileList.Count / 200, .Show = False}) '每 200 Mod 需要 1s
            '下载 Mod 文件
            InstallLoaders.Add(New LoaderDownload("下载 Mod", New List(Of NetFile)) With {.ProgressWeight = ModFileList.Count * 1.5}) '每个 Mod 需要 1.5s
        End If
        '构造加载器
        Dim Request As New McInstallRequest With {
            .TargetVersionName = VersionName,
            .MinecraftName = Json("minecraft")("version").ToString,
            .ForgeVersion = ForgeVersion,
            .FabricVersion = FabricVersion
        }
        Dim InstallExpectTime As Double = 0
        For Each InstallLoader In InstallLoaders
            InstallExpectTime += InstallLoader.ProgressWeight
        Next
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request, True)
        If MergeLoaders Is Nothing Then Return Nothing
        Dim MergeExpectTime As Double = 0
        For Each MergeLoader In MergeLoaders
            MergeExpectTime += MergeLoader.ProgressWeight
        Next
        '构造 Libraries 加载器（为了使得 Mods 下载结束后再构造，这样才会下载 JumpLoader 文件）
        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析游戏支持库文件（副加载器）", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionName))) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载游戏支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 7, .Show = False})
        '构造总加载器
        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)("整合包安装", InstallLoaders) With {.Show = False, .Block = False, .ProgressWeight = InstallExpectTime})
        Loaders.Add(New LoaderCombo(Of String)("游戏安装", MergeLoaders) With {.Show = False, .ProgressWeight = MergeExpectTime})
        Loaders.Add(New LoaderCombo(Of String)("下载游戏支持库文件", LoadersLib) With {.ProgressWeight = 8})

        '重复任务检查
        Dim LoaderName As String = "CurseForge 整合包安装：" & VersionName & " "
        SyncLock LoaderTaskbarLock
            For i = 0 To LoaderTaskbar.Count - 1
                If LoaderTaskbar(i).Name = LoaderName Then
                    Hint("该整合包正在安装中！", HintType.Critical)
                    Return Nothing
                End If
            Next
        End SyncLock

        '启动
        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        Return Loader
    End Function
    Private Sub InstallPackCurseForge(FileAddress As String, Archive As Compression.ZipArchive, ArchiveBaseFolder As String, Optional VersionName As String = Nothing)

        '获取版本名
        Dim ShowRibble As Boolean = VersionName Is Nothing
        If VersionName Is Nothing Then
            Dim Json As JObject
            Try
                Json = GetJson(ReadFile(Archive.GetEntry(ArchiveBaseFolder & "manifest.json").Open))
                If Json("minecraft") Is Nothing OrElse Json("minecraft")("version") Is Nothing Then Throw New Exception("整合包未提供 Minecraft 版本信息")
            Catch ex As Exception
                Log(ex, "整合包安装信息存在问题", LogLevel.Hint)
                Exit Sub
            End Try
            Dim PackName As String = If(Json("name"), "")
            Dim Validate As New ValidateFolderName(PathMcFolder & "versions")
            If Validate.Validate(PackName) <> "" Then PackName = ""
            VersionName = MyMsgBoxInput(PackName, New ObjectModel.Collection(Of Validate) From {Validate},
                                                      Title:="输入版本名", Button2:="取消")
            If String.IsNullOrEmpty(VersionName) Then Exit Sub
        End If

        '启动加载器
        Dim Loader = InstallPackCurseForgeLoader(FileAddress, Archive, ArchiveBaseFolder, VersionName)
        If Loader Is Nothing Then Exit Sub
        Loader.Start(PathMcFolder & "versions\" & VersionName & "\")
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        If ShowRibble Then FrmMain.BtnExtraDownload.Ribble()

    End Sub

    'HMCL
    Private Sub InstallPackHMCL(FileAddress As String, Archive As Compression.ZipArchive, ArchiveBaseFolder As String)
        '读取 Json 文件
        Dim Json As JObject
        Try
            Json = GetJson(ReadFile(Archive.GetEntry(ArchiveBaseFolder & "modpack.json").Open, Encoding.UTF8))
        Catch ex As Exception
            Log(ex, "整合包安装信息存在问题", LogLevel.Hint)
            Exit Sub
        End Try
        '获取版本名
        Dim PackName As String = If(Json("name"), "")
        Dim Validate As New ValidateFolderName(PathMcFolder & "versions")
        If Validate.Validate(PackName) <> "" Then PackName = ""
        Dim VersionName As String = MyMsgBoxInput(PackName, New ObjectModel.Collection(Of Validate) From {Validate},
                                                  Title:="输入版本名", Button2:="取消")
        If VersionName Is Nothing Then Exit Sub
        '解压与配置文件
        Dim InstallTemp As String = PathTemp & "PackInstall\" & RandomInteger(0, 100000) & "\"
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)("解压整合包文件",
                                                              Sub(Task As LoaderTask(Of String, Integer))
                                                                  UnpackFiles(InstallTemp, FileAddress)
                                                                  Task.Progress = 0.5
                                                                  '复制结果
                                                                  If Directory.Exists(InstallTemp & ArchiveBaseFolder & "minecraft") Then
                                                                      My.Computer.FileSystem.CopyDirectory(InstallTemp & ArchiveBaseFolder & "minecraft", PathMcFolder & "versions\" & VersionName)
                                                                  Else
                                                                      Log("[ModPack] 整合包中未找到 minecraft override 目录，已跳过")
                                                                  End If
                                                                  Task.Progress = 0.9
                                                                  '开启版本隔离
                                                                  WriteIni(PathMcFolder & "versions\" & VersionName & "\PCL\Setup.ini", "VersionArgumentIndie", 1)
                                                              End Sub) With {.ProgressWeight = New FileInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False}) '每 6M 需要 1s
        '构造加载器
        If Json("gameVersion") Is Nothing Then Throw New Exception("整合包未提供游戏版本信息")
        Dim Request As New McInstallRequest With {
            .TargetVersionName = VersionName,
            .MinecraftName = Json("gameVersion").ToString
        }
        Dim InstallExpectTime As Double = 0
        For Each InstallLoader In InstallLoaders
            InstallExpectTime += InstallLoader.ProgressWeight
        Next
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request, True)
        If MergeLoaders Is Nothing Then Exit Sub
        Dim MergeExpectTime As Double = 0
        For Each MergeLoader In MergeLoaders
            MergeExpectTime += MergeLoader.ProgressWeight
        Next
        '构造 Libraries 加载器（为了使得 Mods 下载结束后再构造，这样才会下载 JumpLoader 文件）
        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, String)("重命名版本 Json（副加载器）",
                                                         Sub()
                                                             Dim RealFileName As String = PathMcFolder & "versions\" & VersionName & "\" & VersionName & ".json"
                                                             Dim OldFileName As String = PathMcFolder & "versions\" & VersionName & "\pack.json"
                                                             If File.Exists(OldFileName) Then
                                                                 '修改 id
                                                                 Dim FileJson = GetJson(ReadFile(OldFileName))
                                                                 FileJson("id") = VersionName
                                                                 '替换文件
                                                                 File.Delete(OldFileName)
                                                                 WriteFile(RealFileName, FileJson.ToString)
                                                                 Log("[ModPack] 已重命名版本 Json：" & RealFileName)
                                                             End If
                                                         End Sub) With {.ProgressWeight = 0.1, .Show = False})
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析游戏支持库文件（副加载器）", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionName))) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载游戏支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 7, .Show = False})
        '构造总加载器
        Dim Loaders As New List(Of LoaderBase) From {
            New LoaderCombo(Of String)("游戏安装", MergeLoaders) With {.Show = False, .Block = False, .ProgressWeight = MergeExpectTime},
            New LoaderCombo(Of String)("整合包安装", InstallLoaders) With {.Show = False, .ProgressWeight = InstallExpectTime},
            New LoaderCombo(Of String)("下载游戏支持库文件", LoadersLib) With {.ProgressWeight = 8}
        }

        '重复任务检查
        Dim LoaderName As String = "HMCL 整合包安装：" & VersionName & " "
        SyncLock LoaderTaskbarLock
            For i = 0 To LoaderTaskbar.Count - 1
                If LoaderTaskbar(i).Name = LoaderName Then
                    Hint("该整合包正在安装中！", HintType.Critical)
                    Exit Sub
                End If
            Next
        End SyncLock

        '启动
        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        'If Archive IsNot Nothing Then Archive.Dispose() '解除占用，以免在加载器中触发 “正由另一进程使用，因此该进程无法访问此文件”
        Loader.Start(PathMcFolder & "versions\" & VersionName & "\")
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        FrmMain.BtnExtraDownload.Ribble()
    End Sub

    'MMC
    Private Sub InstallPackMMC(FileAddress As String, Archive As Compression.ZipArchive, ArchiveBaseFolder As String)
        '读取 Json 文件
        Dim PackJson As JObject, PackInstance As String
        Try
            PackJson = GetJson(ReadFile(Archive.GetEntry(ArchiveBaseFolder & "mmc-pack.json").Open, Encoding.UTF8))
            PackInstance = ReadFile(Archive.GetEntry(ArchiveBaseFolder & "instance.cfg").Open, Encoding.UTF8)
        Catch ex As Exception
            Log(ex, "整合包安装信息存在问题", LogLevel.Hint)
            Exit Sub
        End Try
        '获取版本名
        Dim PackName As String = If(RegexSeek(PackInstance, "(?<=\nname\=)[^\n]+"), "")
        Dim Validate As New ValidateFolderName(PathMcFolder & "versions")
        If Validate.Validate(PackName) <> "" Then PackName = ""
        Dim VersionName As String = MyMsgBoxInput(PackName, New ObjectModel.Collection(Of Validate) From {Validate},
                                                  Title:="输入版本名", Button2:="取消")
        If VersionName Is Nothing Then Exit Sub
        '解压与配置文件
        Dim InstallTemp As String = PathTemp & "PackInstall\" & RandomInteger(0, 100000) & "\"
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)("解压整合包文件",
                                                              Sub(Task As LoaderTask(Of String, Integer))
                                                                  UnpackFiles(InstallTemp, FileAddress)
                                                                  Task.Progress = 0.5
                                                                  '复制结果
                                                                  If Directory.Exists(InstallTemp & ArchiveBaseFolder & ".minecraft") Then
                                                                      My.Computer.FileSystem.CopyDirectory(InstallTemp & ArchiveBaseFolder & ".minecraft", PathMcFolder & "versions\" & VersionName)
                                                                  Else
                                                                      Log("[ModPack] 整合包中未找到 override .minecraft 目录，已跳过")
                                                                  End If
                                                                  Task.Progress = 0.9
                                                                  '开启版本隔离
                                                                  WriteIni(PathMcFolder & "versions\" & VersionName & "\PCL\Setup.ini", "VersionArgumentIndie", 1)
                                                              End Sub) With {.ProgressWeight = New FileInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False}) '每 6M 需要 1s
        '构造版本安装请求
        If PackJson("components") Is Nothing Then Throw New Exception("整合包未提供游戏版本信息")
        Dim Request As New McInstallRequest With {.TargetVersionName = VersionName}
        For Each Component In PackJson("components")
            Select Case If(Component("uid"), "").ToString
                Case "org.lwjgl"
                    Log("[ModPack] 已跳过 LWJGL 项")
                Case "net.minecraft"
                    Request.MinecraftName = Component("version")
                Case "net.minecraftforge"
                    Request.ForgeVersion = Component("version")
            End Select
        Next
        '构造加载器
        Dim InstallExpectTime As Double = 0
        For Each InstallLoader In InstallLoaders
            InstallExpectTime += InstallLoader.ProgressWeight
        Next
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request, True)
        If MergeLoaders Is Nothing Then Exit Sub
        Dim MergeExpectTime As Double = 0
        For Each MergeLoader In MergeLoaders
            MergeExpectTime += MergeLoader.ProgressWeight
        Next
        '构造 Libraries 加载器（为了使得 Mods 下载结束后再构造，这样才会下载 JumpLoader 文件）
        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析游戏支持库文件（副加载器）", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionName))) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载游戏支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 7, .Show = False})
        '构造总加载器
        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)("游戏安装", MergeLoaders) With {.Show = False, .Block = False, .ProgressWeight = MergeExpectTime})
        Loaders.Add(New LoaderCombo(Of String)("整合包安装", InstallLoaders) With {.Show = False, .ProgressWeight = InstallExpectTime})
        Loaders.Add(New LoaderCombo(Of String)("下载游戏支持库文件", LoadersLib) With {.ProgressWeight = 8})

        '重复任务检查
        Dim LoaderName As String = "MMC 整合包安装：" & VersionName & " "
        SyncLock LoaderTaskbarLock
            For i = 0 To LoaderTaskbar.Count - 1
                If LoaderTaskbar(i).Name = LoaderName Then
                    Hint("该整合包正在安装中！", HintType.Critical)
                    Exit Sub
                End If
            Next
        End SyncLock

        '启动
        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        Loader.Start(PathMcFolder & "versions\" & VersionName & "\")
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        FrmMain.BtnExtraDownload.Ribble()
    End Sub

    'MCBBS
    Private Sub InstallPackMCBBS(FileAddress As String, Archive As Compression.ZipArchive, ArchiveBaseFolder As String)
        '读取 Json 文件
        Dim Json As JObject
        Try
            Dim Entry = If(Archive.GetEntry(ArchiveBaseFolder & "mcbbs.packmeta"), Archive.GetEntry(ArchiveBaseFolder & "manifest.json"))
            Json = GetJson(ReadFile(Entry.Open, Encoding.UTF8))
        Catch ex As Exception
            Log(ex, "整合包安装信息存在问题", LogLevel.Hint)
            Exit Sub
        End Try
        '获取版本名
        Dim PackName As String = If(Json("name"), "")
        Dim Validate As New ValidateFolderName(PathMcFolder & "versions")
        If Validate.Validate(PackName) <> "" Then PackName = ""
        Dim VersionName As String = MyMsgBoxInput(PackName, New ObjectModel.Collection(Of Validate) From {Validate},
                                                  Title:="输入版本名", Button2:="取消")
        If VersionName Is Nothing Then Exit Sub
        '解压与配置文件
        Dim InstallTemp As String = PathTemp & "PackInstall\" & RandomInteger(0, 100000) & "\"
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)("解压整合包文件",
                                                              Sub(Task As LoaderTask(Of String, Integer))
                                                                  UnpackFiles(InstallTemp, FileAddress)
                                                                  Task.Progress = 0.5
                                                                  '复制结果
                                                                  If Directory.Exists(InstallTemp & ArchiveBaseFolder & "overrides") Then
                                                                      My.Computer.FileSystem.CopyDirectory(InstallTemp & ArchiveBaseFolder & "overrides", PathMcFolder & "versions\" & VersionName)
                                                                  Else
                                                                      Log("[ModPack] 整合包中未找到 overrides 目录，已跳过")
                                                                  End If
                                                                  Task.Progress = 0.9
                                                                  '开启版本隔离
                                                                  WriteIni(PathMcFolder & "versions\" & VersionName & "\PCL\Setup.ini", "VersionArgumentIndie", 1)
                                                              End Sub) With {.ProgressWeight = New FileInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False}) '每 6M 需要 1s
        '构造加载器
        If Json("addons") Is Nothing Then Throw New Exception("整合包未提供游戏版本信息")
        Dim Addons As New Dictionary(Of String, String)
        For Each Entry In Json("addons")
            Addons.Add(Entry("id"), Entry("version"))
        Next
        If Not Addons.ContainsKey("game") Then Throw New Exception("整合包未提供游戏版本信息")
        Dim Request As New McInstallRequest With {
            .TargetVersionName = VersionName,
            .MinecraftName = Addons("game"),
            .OptiFineVersion = If(Addons.ContainsKey("optifine"), Addons("optifine"), Nothing),
            .ForgeVersion = If(Addons.ContainsKey("forge"), Addons("forge"), Nothing),
            .FabricVersion = If(Addons.ContainsKey("fabric"), Addons("fabric"), Nothing)
        }
        Dim InstallExpectTime As Double = 0
        For Each InstallLoader In InstallLoaders
            InstallExpectTime += InstallLoader.ProgressWeight
        Next
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request, True)
        If MergeLoaders Is Nothing Then Exit Sub
        Dim MergeExpectTime As Double = 0
        For Each MergeLoader In MergeLoaders
            MergeExpectTime += MergeLoader.ProgressWeight
        Next
        '构造 Libraries 加载器（为了使得 Mods 下载结束后再构造，这样才会下载 JumpLoader 文件）
        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析游戏支持库文件（副加载器）", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionName))) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载游戏支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 7, .Show = False})
        '构造总加载器
        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)("游戏安装", MergeLoaders) With {.Show = False, .Block = False, .ProgressWeight = MergeExpectTime})
        Loaders.Add(New LoaderCombo(Of String)("整合包安装", InstallLoaders) With {.Show = False, .ProgressWeight = InstallExpectTime})
        Loaders.Add(New LoaderCombo(Of String)("下载游戏支持库文件", LoadersLib) With {.ProgressWeight = 8})

        '重复任务检查
        Dim LoaderName As String = "MCBBS 整合包安装：" & VersionName & " "
        SyncLock LoaderTaskbarLock
            For i = 0 To LoaderTaskbar.Count - 1
                If LoaderTaskbar(i).Name = LoaderName Then
                    Hint("该整合包正在安装中！", HintType.Critical)
                    Exit Sub
                End If
            Next
        End SyncLock

        '启动
        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        'If Archive IsNot Nothing Then Archive.Dispose() '解除占用，以免在加载器中触发 “正由另一进程使用，因此该进程无法访问此文件”
        Loader.Start(PathMcFolder & "versions\" & VersionName & "\")
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        FrmMain.BtnExtraDownload.Ribble()
    End Sub

    '普通压缩包
    Private Sub InstallPackCompress(FileAddress As String, ArchiveBaseFolder As String)
        MyMsgBox("请在接下来打开的窗口中选择安装目标文件夹，它必须是一个空文件夹。", "安装提示", "继续", ForceWait:=True)
        '获取解压路径
        Dim TargetFolder As String = SelectFolder("选择安装目标（必须是一个空文件夹）")
        If String.IsNullOrEmpty(TargetFolder) Then Exit Sub
        If TargetFolder.Contains("!") OrElse TargetFolder.Contains(";") Then Hint("Minecraft 文件夹路径中不能含有感叹号或分号！", HintType.Critical) : Exit Sub
        If Directory.GetFileSystemEntries(TargetFolder).Length > 0 Then Hint("请选择一个空文件夹作为安装目标！", HintType.Critical) : Exit Sub
        '要求显示名称
        Dim NewName As String = MyMsgBoxInput(GetFolderNameFromPath(TargetFolder), New ObjectModel.Collection(Of Validate) From {
                   New ValidateNullOrWhiteSpace, New ValidateLength(1, 30), New ValidateExcept({">", "|"})
                },, "输入它在列表中的显示名称",, "取消")
        If String.IsNullOrWhiteSpace(NewName) Then Exit Sub
        '解压
        Hint("正在解压压缩包……")
        UnpackFiles(TargetFolder, FileAddress)
        '加入文件夹列表
        PageSelectLeft.AddFolder(TargetFolder, NewName, False)
        Hint("已加入游戏文件夹列表：" & NewName, HintType.Finish)
    End Sub

#End Region

End Module
