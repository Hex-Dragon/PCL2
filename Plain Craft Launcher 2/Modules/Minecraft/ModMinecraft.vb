Imports System.IO.Compression

Public Module ModMinecraft

#Region "文件夹"

    ''' <summary>
    ''' 当前的 Minecraft 文件夹路径，以“\”结尾。
    ''' </summary>
    Public PathMcFolder As String
    ''' <summary>
    ''' 当前的 Minecraft 文件夹列表。
    ''' </summary>
    Public McFolderList As New List(Of McFolder)

    Public Class McFolder '必须是 Class，否则不是引用类型，在 ForEach 中不会得到刷新
        Public Name As String
        Public Path As String
        Public Type As McFolderType
        Public Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is McFolder) Then Return False
            Dim folder = DirectCast(obj, McFolder)
            Return Name = folder.Name AndAlso Path = folder.Path AndAlso Type = folder.Type
        End Function
        Public Overrides Function ToString() As String
            Return Path
        End Function
    End Class
    Public Enum McFolderType
        Original
        RenamedOriginal
        Custom
    End Enum

    ''' <summary>
    ''' 加载 Minecraft 文件夹列表。
    ''' </summary>
    Public McFolderListLoader As New LoaderTask(Of Integer, Integer)("Minecraft Folder List", AddressOf McFolderListLoadSub, Priority:=ThreadPriority.AboveNormal)
    Private Sub McFolderListLoadSub()
        Try
            '初始化
            Dim CacheMcFolderList = New List(Of McFolder)

#Region "读取默认（Original）文件夹，即当前、官启文件夹，可能没有结果"

            '扫描当前文件夹
            If CheckPermission(Path) AndAlso Directory.Exists(Path & "versions\") Then CacheMcFolderList.Add(New McFolder With {.Name = "当前文件夹", .Path = Path, .Type = McFolderType.Original})
            For Each Folder As DirectoryInfo In New DirectoryInfo(Path).GetDirectories
                If CheckPermission(Folder.FullName) AndAlso Directory.Exists(Folder.FullName & "versions\") OrElse Folder.Name = ".minecraft" Then CacheMcFolderList.Add(New McFolder With {.Name = "当前文件夹", .Path = Folder.FullName & "\", .Type = McFolderType.Original})
            Next

            '扫描官启文件夹
            Dim MojangPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\.minecraft\"
            If (CacheMcFolderList.Count = 0 OrElse MojangPath <> CacheMcFolderList(0).Path) AndAlso '当前文件夹不是官启文件夹
                CheckPermission(MojangPath) AndAlso Directory.Exists(MojangPath & "versions\") Then '具有权限且存在 versions 文件夹
                CacheMcFolderList.Add(New McFolder With {.Name = "官方启动器文件夹", .Path = MojangPath, .Type = McFolderType.Original})
            End If

#End Region

#Region "读取自定义（Custom）文件夹，可能没有结果"

            '格式：TMZ 12>C://xxx/xx/|Test>D://xxx/xx/|名称>路径
            For Each Folder As String In Setup.Get("LaunchFolders").Split("|")
                If Folder = "" Then Continue For
                If Not Folder.Contains(">") OrElse Not Folder.EndsWith("\") Then
                    Hint("无效的 Minecraft 文件夹：" & Folder, HintType.Critical)
                    Continue For
                End If
                Dim Name As String = Folder.Split(">")(0)
                Dim Path As String = Folder.Split(">")(1)
                If CheckPermission(Path) Then
                    '若已有该文件夹，则直接重命名；没有则添加
                    Dim Renamed As Boolean = False
                    For Each OriginalFolder As McFolder In CacheMcFolderList
                        If OriginalFolder.Path = Path Then
                            OriginalFolder.Name = Name
                            OriginalFolder.Type = McFolderType.RenamedOriginal
                            Renamed = True
                        End If
                    Next
                    If Not Renamed Then CacheMcFolderList.Add(New McFolder With {.Name = Name, .Path = Path, .Type = McFolderType.Custom})
                Else
                    Hint("无效的 Minecraft 文件夹：" & Path, HintType.Critical)
                End If
            Next

            '将自定义文件夹情况同步到设置
            Dim NewSetup As New List(Of String)
            For Each Folder As McFolder In CacheMcFolderList
                If Not Folder.Type = McFolderType.Original Then NewSetup.Add(Folder.Name & ">" & Folder.Path)
            Next
            If NewSetup.Count = 0 Then NewSetup.Add("") '防止 0 元素 Join 返回 Nothing
            Setup.Set("LaunchFolders", Join(NewSetup, "|"))

#End Region

            '若没有可用文件夹，则创建 .minecraft
            If CacheMcFolderList.Count = 0 Then
                Directory.CreateDirectory(Path & ".minecraft\versions\")
                CacheMcFolderList.Add(New McFolder With {.Name = "当前文件夹", .Path = Path & ".minecraft\", .Type = McFolderType.Original})
            End If

            For Each Folder As McFolder In CacheMcFolderList
#Region "更新 launcher_profiles.json"
                McFolderLauncherProfilesJsonCreate(Folder.Path)
#End Region
            Next
            If Setup.Get("SystemDebugDelay") Then Thread.Sleep(RandomInteger(200, 2000))

            '回设
            McFolderList = CacheMcFolderList

        Catch ex As Exception
            Log(ex, "加载 Minecraft 文件夹列表失败", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 为 Minecraft 文件夹创建 launcher_profiles.json 文件。
    ''' </summary>
    Public Sub McFolderLauncherProfilesJsonCreate(Folder As String)
        Try
            If File.Exists(Folder & "launcher_profiles.json") Then Exit Sub
            Dim ResultJson As String =
"{
    ""profiles"":  {
        ""PCL"": {
            ""icon"": ""Grass"",
            ""name"": ""PCL"",
            ""lastVersionId"": ""latest-release"",
            ""type"": ""latest-release"",
            ""lastUsed"": """ & Date.Now.ToString("yyyy-MM-dd") & "T" & Date.Now.ToString("HH:mm:ss") & ".0000Z""
        }
    },
    ""selectedProfile"": ""PCL"",
    ""clientToken"": ""23323323323323323323323323323333""
}"
            WriteFile(Folder & "launcher_profiles.json", ResultJson, Encoding:=Encoding.GetEncoding("GB18030"))
            Log("[Minecraft] 已创建 launcher_profiles.json：" & Folder)
        Catch ex As Exception
            Log(ex, "创建 launcher_profiles.json 失败（" & Folder & "）", LogLevel.Feedback)
        End Try
    End Sub

#End Region

#Region "版本处理"

    Public Const McVersionCacheVersion As Integer = 26

    Private _McVersionCurrent As McVersion
    Private _McVersionLast = 0 '为 0 以保证与 Nothing 不相同，使得 UI 显示可以正常初始化
    ''' <summary>
    ''' 当前的 Minecraft 版本。
    ''' </summary>
    Public Property McVersionCurrent As McVersion
        Get
            Return _McVersionCurrent
        End Get
        Set(value As McVersion)
            If ReferenceEquals(_McVersionLast, value) Then Exit Property
            _McVersionCurrent = value '由于有可能是 Nothing，导致无法初始化，才得这样弄一圈
            _McVersionLast = value
            If value Is Nothing Then Exit Property
            '重置缓存的 Mod 文件夹
            PageDownloadCompDetail.CachedFolder = Nothing
            '统一通行证重判
            If AniControlEnabled = 0 AndAlso
               Setup.Get("VersionServerNide", Version:=value) <> Setup.Get("CacheNideServer") AndAlso
               Setup.Get("VersionServerLogin", Version:=value) = 3 Then
                Setup.Set("CacheNideAccess", "")
                Log("[Launch] 服务器改变，要求重新登录统一通行证")
            End If
            If Setup.Get("VersionServerLogin", Version:=value) = 3 Then
                Setup.Set("CacheNideServer", Setup.Get("VersionServerNide", Version:=value))
            End If
            'Authlib-Injector 重判
            If AniControlEnabled = 0 AndAlso
               Setup.Get("VersionServerAuthServer", Version:=value) <> Setup.Get("CacheAuthServerServer") AndAlso
               Setup.Get("VersionServerLogin", Version:=value) = 4 Then
                Setup.Set("CacheNideServer", "")
                Log("[Launch] 服务器改变，要求重新登录 Authlib-Injector")
            End If
            If Setup.Get("VersionServerLogin", Version:=value) = 4 Then
                Setup.Set("CacheAuthServerServer", Setup.Get("VersionServerAuthServer", Version:=value))
                Setup.Set("CacheAuthServerName", Setup.Get("VersionServerAuthName", Version:=value))
                Setup.Set("CacheAuthServerRegister", Setup.Get("VersionServerAuthRegister", Version:=value))
            End If
        End Set
    End Property

    Public Class McVersion

        ''' <summary>
        ''' 该版本的版本文件夹，以“\”结尾。
        ''' </summary>
        Public ReadOnly Property Path As String
        ''' <summary>
        ''' 应用版本隔离后，该版本所对应的 Minecraft 根文件夹，以“\”结尾。
        ''' </summary>
        Public ReadOnly Property PathIndie As String
            Get
                Return GetPathIndie(Modable)
            End Get
        End Property
        ''' <summary>
        ''' 在不加载版本的情况下获取版本隔离目录。
        ''' </summary>
        Public Function GetPathIndie(Modable As Boolean) As String
            Dim IndieType As Integer = Setup.Get("LaunchArgumentIndie")
            Select Case Setup.Get("VersionArgumentIndie", Version:=Me)
                Case -1
                    '尚未判断
                    Dim ModFolder As New DirectoryInfo(Path & "mods\")
                    Dim SaveFolder As New DirectoryInfo(Path & "saves\")
                    If (ModFolder.Exists AndAlso ModFolder.EnumerateFiles.Count > 0) OrElse
                       (SaveFolder.Exists AndAlso SaveFolder.EnumerateFiles.Count > 0) Then
                        '自动开启
                        Setup.Set("VersionArgumentIndie", 1, Version:=Me)
                        Log("[Setup] 已自动开启单版本隔离：" & Name)
                        IndieType = 4
                    Else
                        '使用全局设置
                        Setup.Set("VersionArgumentIndie", 0, Version:=Me)
                        Log("[Setup] 版本隔离使用全局设置：" & Name)
                    End If
                Case 0
                    '使用全局设置
                Case 1
                    '开启
                    IndieType = 4
                Case 2
                    '关闭
                    IndieType = 0
            End Select
            Select Case IndieType
                Case 0 '关闭
                Case 1 '仅隔离可安装 Mod 的版本
                    If Modable Then Return Path
                Case 2 '仅隔离非正式版
                    If State = McVersionState.Fool OrElse State = McVersionState.Old OrElse State = McVersionState.Snapshot Then Return Path
                Case 3 '隔离非正式版与可安装 Mod 的版本
                    If Modable Then Return Path
                    If State = McVersionState.Fool OrElse State = McVersionState.Old OrElse State = McVersionState.Snapshot Then Return Path
                Case 4 '隔离所有版本
                    Return Path
            End Select
            Return PathMcFolder
        End Function

        ''' <summary>
        ''' 该版本的版本文件夹名称。
        ''' </summary>
        Public ReadOnly Property Name As String
            Get
                If _Name Is Nothing AndAlso Not Path = "" Then _Name = GetFolderNameFromPath(Path)
                Return _Name
            End Get
        End Property
        Private _Name As String = Nothing

        ''' <summary>
        ''' 显示的描述文本。
        ''' </summary>
        Public Info As String = "该版本未被加载，请向作者反馈此问题"
        ''' <summary>
        ''' 该版本的列表检查原始结果，不受自定义影响。
        ''' </summary>
        Public State As McVersionState = McVersionState.Error
        ''' <summary>
        ''' 显示的版本图标。
        ''' </summary>
        Public Logo As String
        ''' <summary>
        ''' 是否为收藏的版本。
        ''' </summary>
        Public IsStar As Boolean = False
        ''' <summary>
        ''' 强制版本分类，0 为未启用，1 为隐藏，2 及以上为其他普通分类。
        ''' </summary>
        Public DisplayType As McVersionCardType = McVersionCardType.Auto
        ''' <summary>
        ''' 该版本是否可以安装 Mod。
        ''' </summary>
        Public ReadOnly Property Modable As Boolean
            Get
                If Not IsLoaded Then Load()
                Return Version.HasFabric OrElse Version.HasForge OrElse Version.HasLiteLoader OrElse
                    DisplayType = McVersionCardType.API '#223
            End Get
        End Property
        ''' <summary>
        ''' 版本信息。
        ''' </summary>
        Public Property Version As McVersionInfo
            Get
                If _Version Is Nothing Then
                    _Version = New McVersionInfo
#Region "获取游戏版本"
                    Try

                        '获取发布时间并判断是否为老版本
                        Try
                            If JsonObject("releaseTime") Is Nothing Then
                                ReleaseTime = New Date(1970, 1, 1, 15, 0, 0) '未知版本也可能显示为 1970 年
                            Else
                                ReleaseTime = JsonObject("releaseTime").ToObject(Of Date)
                            End If
                            If ReleaseTime.Year > 2000 AndAlso ReleaseTime.Year < 2013 Then
                                _Version.McName = "Old"
                                GoTo VersionSearchFinish
                            End If
                        Catch
                            ReleaseTime = New Date(1970, 1, 1, 15, 0, 0)
                        End Try
                        '实验性快照
                        If If(JsonObject("type"), "") = "pending" Then
                            _Version.McName = "pending"
                            GoTo VersionSearchFinish
                        End If
                        '从 JumpLoader 信息中获取版本号
                        If HasJumpLoader Then
                            Try
                                _Version.McName = JsonObject("jumploader")("jars")("minecraft")(0)("gameVersion")
                                GoTo VersionSearchFinish
                            Catch
                            End Try
                        End If
                        '从 PCL 下载的版本信息中获取版本号
                        If JsonObject("clientVersion") IsNot Nothing Then
                            _Version.McName = JsonObject("clientVersion")
                            GoTo VersionSearchFinish
                        End If
                        '从 HMCL 下载的版本信息中获取版本号
                        If JsonObject("patches") IsNot Nothing Then
                            For Each Patch As JObject In JsonObject("patches")
                                If If(Patch("id"), "").ToString = "game" AndAlso Patch("version") IsNot Nothing Then
                                    _Version.McName = Patch("version").ToString
                                    GoTo VersionSearchFinish
                                End If
                            Next
                        End If
                        '从 Forge Arguments 中获取版本号
                        If JsonObject("arguments") IsNot Nothing AndAlso JsonObject("arguments")("game") IsNot Nothing Then
                            Dim Mark As Boolean = False
                            For Each Argument In JsonObject("arguments")("game")
                                If Mark Then
                                    _Version.McName = Argument.ToString
                                    GoTo VersionSearchFinish
                                End If
                                If Argument.ToString = "--fml.mcVersion" Then Mark = True
                            Next
                        End If
                        '从继承版本中获取版本号
                        If Not InheritVersion = "" Then
                            _Version.McName = If(JsonObject("jar"), "").ToString 'LiteLoader 优先使用 Jar
                            If _Version.McName = "" Then _Version.McName = InheritVersion
                            GoTo VersionSearchFinish
                        End If
                        '从下载地址中获取版本号
                        Dim Regex As String = RegexSeek(If(JsonObject("downloads"), "").ToString, "(?<=launcher.mojang.com/mc/game/)[^/]*")
                        If Regex IsNot Nothing Then
                            _Version.McName = Regex
                            GoTo VersionSearchFinish
                        End If
                        '从 Forge 版本中获取版本号
                        Dim LibrariesString As String = JsonObject("libraries").ToString
                        Regex = If(RegexSeek(LibrariesString, "(?<=net.minecraftforge:forge:)1.[0-9+.]+"), RegexSeek(LibrariesString, "(?<=net.minecraftforge:fmlloader:)1.[0-9+.]+"))
                        If Regex IsNot Nothing Then
                            _Version.McName = Regex
                            GoTo VersionSearchFinish
                        End If
                        '从 OptiFine 版本中获取版本号
                        Regex = RegexSeek(LibrariesString, "(?<=optifine:OptiFine:)1.[0-9+.]+")
                        If Regex IsNot Nothing Then
                            _Version.McName = Regex
                            GoTo VersionSearchFinish
                        End If
                        '从 Fabric 版本中获取版本号
                        Regex = RegexSeek(LibrariesString, "(?<=((fabricmc)|(quiltmc)):intermediary:)[^""]*")
                        If Regex IsNot Nothing Then
                            _Version.McName = Regex
                            GoTo VersionSearchFinish
                        End If
                        'FUTURE: [Quilt 支持] 从 Quilt 版本中获取版本号
                        '从 jar 项中获取版本号
                        If JsonObject("jar") IsNot Nothing Then
                            _Version.McName = JsonObject("jar").ToString
                            GoTo VersionSearchFinish
                        End If
                        '非准确的版本判断警告
                        Log("[Minecraft] 无法完全确认 MC 版本号的版本：" & Name)
                        '从文件夹名中获取
                        Regex = RegexSeek(Name, "([0-9w]{5}[a-z]{1})|(1\.[0-9]+(\.[0-9]+)?(-(pre|rc)[1-9]?| Pre-Release( [1-9]{1})?)?)")
                        If Regex IsNot Nothing Then
                            _Version.McName = Regex
                            GoTo VersionSearchFinish
                        End If
                        '从 Json 出现的版本号中获取
                        Dim JsonRaw As JObject = JsonObject.DeepClone()
                        JsonRaw.Remove("libraries")
                        Dim JsonRawText As String = JsonRaw.ToString
                        Regex = RegexSeek(JsonRawText, "([0-9w]{5}[a-z]{1})|(1\.[0-9]+(\.[0-9]+)?(-(pre|rc)[1-9]?| Pre-Release( [1-9]{1})?)?)")
                        If Regex IsNot Nothing Then
                            _Version.McName = Regex
                            GoTo VersionSearchFinish
                        End If
                        '无法获取
                        _Version.McName = "Unknown"
                        Info = "PCL 无法识别该版本，请向作者反馈此问题"
                    Catch ex As Exception
                        Log(ex, "识别 Minecraft 版本时出错")
                        _Version.McName = "Unknown"
                        Info = "无法识别：" & ex.Message
                    End Try
VersionSearchFinish:
                    '获取版本号
                    If _Version.McName.StartsWith("1.") Then
                        Dim SplitVersion = _Version.McName.Split(" "c, "_"c, "-"c, "."c)
                        Dim SplitResult As String
                        '分割获取信息
                        SplitResult = If(SplitVersion.Count >= 2, SplitVersion(1), "0")
                        _Version.McCodeMain = If(SplitResult.Length <= 2, Val(SplitResult), "0")
                        SplitResult = If(SplitVersion.Count >= 3, SplitVersion(2), "0")
                        _Version.McCodeSub = If(SplitResult.Length <= 2, Val(SplitResult), "0")
                    ElseIf _Version.McName.Contains("w") OrElse _Version.McName = "pending" Then
                        _Version.McCodeMain = 99
                        _Version.McCodeSub = 99
                    End If
#End Region
                End If
                Return _Version
            End Get
            Set(value As McVersionInfo)
                _Version = value
            End Set
        End Property
        Private _Version As McVersionInfo = Nothing

        ''' <summary>
        ''' 版本的发布时间。
        ''' </summary>
        Public ReleaseTime As New Date(1970, 1, 1, 15, 0, 0)

        ''' <summary>
        ''' 该版本的 Json 文本。
        ''' </summary>
        Public Property JsonText As String
            Get
                If _JsonText Is Nothing Then
                    If Not File.Exists(Path & Name & ".json") Then Throw New Exception("未找到版本 json 文件：" & Path & Name & ".json")
                    _JsonText = ReadFile(Path & Name & ".json")
                    '如果 ReadFile 失败会返回空字符串；这可能是由于文件被临时占用，故延时后重试
                    If _JsonText.Length = 0 Then
                        If RunInUi() Then
                            Log("[Minecraft] 版本 json 文件为空或读取失败，由于代码在主线程运行，将不再进行重试", LogLevel.Debug)
                            Throw New Exception("版本 Json 文件为空或读取失败")
                        Else
                            Log("[Minecraft] 版本 json 文件为空或读取失败，将在 2s 后重试读取（" & Path & Name & ".json）", LogLevel.Debug)
                            Thread.Sleep(2000)
                            _JsonText = ReadFile(Path & Name & ".json")
                            If _JsonText.Length = 0 Then Throw New Exception("版本 json 文件为空或读取失败")
                        End If
                    End If
                    If _JsonText.Length < 100 Then Throw New Exception("版本 json 文件有误，内容为：" & _JsonText)
                End If
                Return _JsonText
            End Get
            Set(ByVal value As String)
                _JsonText = value
            End Set
        End Property
        Private _JsonText As String = Nothing
        ''' <summary>
        ''' 该版本的 Json 对象。
        ''' 若 Json 存在问题，在获取该属性时即会抛出异常。
        ''' </summary>
        Public Property JsonObject As JObject
            Get
                If _JsonObject Is Nothing Then
                    Dim Text As String = JsonText '触发 JsonText 的 Get 事件
                    Try
                        _JsonObject = GetJson(Text)
                        '转换 HMCL 关键项
                        If _JsonObject.ContainsKey("patches") AndAlso Not _JsonObject.ContainsKey("time") Then
                            IsHmclFormatJson = True
                            '合并 Json
                            'Dim HasOptiFine As Boolean = False, HasForge As Boolean = False
                            Dim CurrentObject As JObject = Nothing
                            Dim SubjsonList As New List(Of JObject)
                            For Each Subjson As JObject In _JsonObject("patches")
                                SubjsonList.Add(Subjson)
                            Next
                            SubjsonList = Sort(SubjsonList, Function(Left As JObject, Right As JObject) As Boolean
                                                                Return Val(If(Left("priority"), "0").ToString) < Val(If(Right("priority"), "0").ToString)
                                                            End Function)
                            For Each Subjson As JObject In SubjsonList
                                Dim Id As String = Subjson("id")
                                If Id IsNot Nothing Then
                                    '合并 Json
                                    Log("[Minecraft] 合并 HMCL 分支项：" & Id)
                                    If CurrentObject IsNot Nothing Then
                                        CurrentObject.Merge(Subjson)
                                    Else
                                        CurrentObject = Subjson
                                    End If
                                Else
                                    Log("[Minecraft] 存在为空的 HMCL 分支项")
                                End If
                            Next
                            _JsonObject = CurrentObject
                            '修改附加项
                            _JsonObject("id") = Name
                            If _JsonObject.ContainsKey("inheritsFrom") Then _JsonObject.Remove("inheritsFrom")
                        End If
                        '与继承版本合并
                        Dim InheritVersion = Nothing
                        Try
                            InheritVersion = If(_JsonObject("inheritsFrom") Is Nothing, "", _JsonObject("inheritsFrom").ToString)
                            If InheritVersion = Name Then
                                Log("[Minecraft] 自引用的继承版本：" & Name, LogLevel.Debug)
                                InheritVersion = ""
                                Exit Try
                            End If
Recheck:
                            If InheritVersion <> "" Then
                                Dim Inherit As New McVersion(InheritVersion)
                                '继续循环
                                If Inherit.InheritVersion = InheritVersion Then Throw New Exception("版本依赖项出现嵌套：" & InheritVersion)
                                InheritVersion = Inherit.InheritVersion
                                '合并
                                Inherit.JsonObject.Merge(_JsonObject)
                                _JsonObject = Inherit.JsonObject
                                GoTo Recheck
                            End If
                        Catch ex As Exception
                            Log(ex, "合并版本依赖项 json 失败（" & If(InheritVersion, "null").ToString & "）")
                        End Try
                    Catch ex As Exception
                        Throw New Exception("版本 json 不规范（" & If(Name, "null") & "）", ex)
                    End Try
                    Try
                        '处理 JumpLoader
                        If Text.Contains("minecraftforge") AndAlso File.Exists(PathIndie & "config\jumploader.json") Then
                            For Each ModFile In Directory.EnumerateFiles(PathIndie & "mods")
                                Dim FileName As String = GetFileNameFromPath(ModFile).ToLower
                                If FileName.EndsWith(".jar") AndAlso FileName.Contains("jumploader") Then
                                    Log("[Minecraft] 发现 JumpLoader 分支项：" & FileName)
                                    HasJumpLoader = True
                                    Exit For
                                End If
                            Next
                        End If
                        If HasJumpLoader Then
                            _JsonObject.Remove("jumploader")
                            _JsonObject.Add("jumploader", GetJson(ReadFile(PathIndie & "config\jumploader.json")))
                        End If
                    Catch ex As Exception
                        Log(ex, "处理 JumpLoader 失败")
                    End Try
                End If
                Return _JsonObject
            End Get
            Set(ByVal value As JObject)
                _JsonObject = value
            End Set
        End Property
        Private _JsonObject As JObject = Nothing
        ''' <summary>
        ''' 是否为旧版 Json 格式。
        ''' </summary>
        Public ReadOnly Property IsOldJson As Boolean
            Get
                Return JsonObject("minecraftArguments") IsNot Nothing
            End Get
        End Property
        ''' <summary>
        ''' Json 是否为 HMCL 格式。
        ''' </summary>
        Public Property IsHmclFormatJson As Boolean = False
        ''' <summary>
        ''' 是否包含 JumpLoader。
        ''' </summary>
        Public Property HasJumpLoader As Boolean = False

        ''' <summary>
        ''' 该版本的依赖版本。若无依赖版本则为空字符串。
        ''' </summary>
        Public ReadOnly Property InheritVersion As String
            Get
                If _InheritVersion Is Nothing Then
                    _InheritVersion = If(JsonObject("inheritsFrom"), "").ToString
                    '由于过老的 LiteLoader 中没有 Inherits（例如 1.5.2），需要手动判断以获取真实继承版本
                    '此外，由于这里的加载早于版本种类判断，所以需要手动判断是否为 LiteLoader
                    '如果版本提供了不同的 Jar，代表所需的 Jar 可能已被更改，则跳过 Inherit 替换
                    If JsonText.Contains("liteloader") AndAlso Version.McName <> Name AndAlso Not JsonText.Contains("logging") Then
                        If If(JsonObject("jar"), Version.McName).ToString = Version.McName Then _InheritVersion = Version.McName
                    End If
                    'HMCL 版本无 Json
                    If IsHmclFormatJson Then _InheritVersion = ""
                End If
                Return _InheritVersion
            End Get
        End Property
        Private _InheritVersion As String = Nothing

        ''' <summary></summary>
        ''' <param name="Path">版本名，或版本文件夹的完整路径（不规定是否以 \ 结尾）。</param>
        Public Sub New(Path As String)
            Me.Path = If(Path.Contains(":"), "", PathMcFolder & "versions\") & '补全完整路径
                      Path &
                      If(Path.EndsWith("\"), "", "\") '补全右划线
        End Sub

        ''' <summary>
        ''' 检查 Minecraft 版本，若检查通过 State 则为 Original 且返回 True。
        ''' </summary>
        Public Function Check() As Boolean

            '检查文件夹
            If Not Directory.Exists(Path) Then
                State = McVersionState.Error
                Info = "未找到版本 " & Name
                Return False
            End If
            '检查权限
            Try
                Directory.CreateDirectory(Path & "PCL\")
                CheckPermissionWithException(Path & "PCL\")
            Catch ex As Exception
                State = McVersionState.Error
                Info = "PCL 没有对该文件夹的访问权限，请右键以管理员身份运行 PCL"
                Log(ex, "没有访问版本文件夹的权限")
                Return False
            End Try
            '确认 Json 可用性
            Try
                Dim JsonObjCheck = JsonObject
            Catch ex As Exception
                Log(ex, "版本 json 可用性检查失败（" & Path & "）")
                JsonText = ""
                JsonObject = Nothing
                Info = ex.Message
                State = McVersionState.Error
                Return False
            End Try
            '检查依赖版本
            Try
                If Not InheritVersion = "" Then
                    If Not File.Exists(GetPathFromFullPath(Path) & InheritVersion & "\" & InheritVersion & ".json") Then
                        State = McVersionState.Error
                        Info = "需要安装 " & InheritVersion & " 作为前置版本"
                        Return False
                    End If
                End If
            Catch ex As Exception
                Log(ex, "依赖版本检查出错（" & Name & "）")
                State = McVersionState.Error
                Info = "未知错误：" & GetExceptionSummary(ex)
                Return False
            End Try

            State = McVersionState.Original
            Return True
        End Function
        ''' <summary>
        ''' 加载 Minecraft 版本的详细信息。不使用其缓存，且会更新缓存。
        ''' </summary>
        Public Function Load() As McVersion
            Try
                '检查版本，若出错则跳过数据确定阶段
                If Not Check() Then GoTo ExitDataLoad
#Region "确定版本分类"
                Select Case Version.McName '在获取 Version.Original 对象时会完成它的加载
                    Case "Unknown"
                        State = McVersionState.Error
                    Case "Old"
                        State = McVersionState.Old
                    Case Else '根据 API 进行筛选
                        Dim RealJson As String = If(JsonObject, JsonText).ToString
                        '愚人节与快照版本
                        If If(JsonObject("type"), "").ToString = "fool" OrElse GetMcFoolName(Version.McName) <> "" Then
                            State = McVersionState.Fool
                        ElseIf Version.McName.ToLower.Contains("w") OrElse Name.ToLower.Contains("combat") OrElse Version.McName.ToLower.Contains("rc") OrElse Version.McName.ToLower.Contains("pre") OrElse Version.McName.Contains("experimental") OrElse If(JsonObject("type"), "").ToString = "snapshot" OrElse If(JsonObject("type"), "").ToString = "pending" Then
                            State = McVersionState.Snapshot
                        End If
                        'OptiFine
                        If RealJson.Contains("optifine") Then
                            State = McVersionState.OptiFine
                            Version.OptiFineVersion = If(RegexSeek(RealJson, "(?<=HD_U_)[^"":/]+"), "未知版本")
                            Version.HasOptiFine = True
                        End If
                        'LiteLoader
                        If RealJson.Contains("liteloader") Then
                            State = McVersionState.LiteLoader
                            Version.HasLiteLoader = True
                        End If
                        'Fabric、Forge
                        'FUTURE: [Quilt 支持] 确认这里的玩意儿对不对
                        If RealJson.Contains("net.fabricmc:fabric-loader") OrElse RealJson.Contains("org.quiltmc:quilt-loader") Then
                            State = McVersionState.Fabric
                            Version.FabricVersion = If(RegexSeek(RealJson, "(?<=(net.fabricmc:fabric-loader:)|(org.quiltmc:quilt-loader:))[0-9\.]+(\+build.[0-9]+)?"), "未知版本").Replace("+build", "")
                            Version.HasFabric = True
                        ElseIf RealJson.Contains("minecraftforge") Then
                            State = McVersionState.Forge
                            Version.ForgeVersion = RegexSeek(RealJson, "(?<=forge:[0-9\.]+(_pre[0-9]*)?\-)[0-9\.]+")
                            If Version.ForgeVersion Is Nothing Then Version.ForgeVersion = RegexSeek(RealJson, "(?<=net\.minecraftforge:minecraftforge:)[0-9\.]+")
                            If Version.ForgeVersion Is Nothing Then Version.ForgeVersion = If(RegexSeek(RealJson, "(?<=net\.minecraftforge:fmlloader:[0-9\.]+-)[0-9\.]+"), "未知版本")
                            Version.HasForge = True
                        End If
                        Version.IsApiLoaded = True
                End Select
#End Region
ExitDataLoad:
                '确定版本图标
                Logo = ReadIni(Path & "PCL\Setup.ini", "Logo", "")
                If Logo = "" OrElse ReadIni(Path & "PCL\Setup.ini", "LogoCustom", "False") = "False" Then
                    Select Case State
                        Case McVersionState.Original
                            Logo = "pack://application:,,,/images/Blocks/Grass.png"
                        Case McVersionState.Snapshot
                            Logo = "pack://application:,,,/images/Blocks/CommandBlock.png"
                        Case McVersionState.Old
                            Logo = "pack://application:,,,/images/Blocks/CobbleStone.png"
                        Case McVersionState.Forge
                            Logo = "pack://application:,,,/images/Blocks/Anvil.png"
                        Case McVersionState.Fabric
                            Logo = "pack://application:,,,/images/Blocks/Fabric.png"
                        Case McVersionState.OptiFine
                            Logo = "pack://application:,,,/images/Blocks/GrassPath.png"
                        Case McVersionState.LiteLoader
                            Logo = "pack://application:,,,/images/Blocks/Egg.png"
                        Case McVersionState.Fool
                            Logo = "pack://application:,,,/images/Blocks/GoldBlock.png"
                        Case Else
                            Logo = "pack://application:,,,/images/Blocks/RedstoneBlock.png"
                    End Select
                End If
                '确定版本描述
                Dim CustomInfo As String = ReadIni(Path & "PCL\Setup.ini", "CustomInfo")
                If CustomInfo = "" Then
                    Select Case State
                        Case McVersionState.Snapshot
                            If Version.McName.ToLower.Contains("pre") Then
                                Info = "预发布版 " & Version.McName
                            ElseIf Version.McName.ToLower.Contains("rc") Then
                                Info = "发布候选 " & Version.McName
                            ElseIf Version.McName.Contains("experimental") OrElse Version.McName = "pending" Then
                                Info = "实验性快照"
                            Else
                                Info = "快照 " & Version.McName
                            End If
                        Case McVersionState.Old
                            Info = "远古版本"
                        Case McVersionState.Original, McVersionState.Forge, McVersionState.Fabric, McVersionState.OptiFine, McVersionState.LiteLoader
                            Info = Version.ToString
                        Case McVersionState.Fool
                            Info = GetMcFoolName(Version.McName)
                        Case McVersionState.Error
                            '已有错误信息
                        Case Else
                            Info = "发生了未知错误，请向作者反馈此问题"
                    End Select
                    If Not State = McVersionState.Error Then
                        If HasJumpLoader Then Info += ", JumpLoader"
                        If Setup.Get("VersionServerLogin", Version:=Me) = 3 Then Info += ", 统一通行证验证"
                        If Setup.Get("VersionServerLogin", Version:=Me) = 4 Then Info += ", Authlib 验证"
                    End If
                Else
                    Info = CustomInfo
                End If
                '确定版本收藏状态
                IsStar = ReadIni(Path & "PCL\Setup.ini", "IsStar", False)
                '确定版本显示种类
                DisplayType = ReadIni(Path & "PCL\Setup.ini", "DisplayType", McVersionCardType.Auto)
                '写入缓存
                If Directory.Exists(Path) Then
                    WriteIni(Path & "PCL\Setup.ini", "State", State)
                    WriteIni(Path & "PCL\Setup.ini", "Info", Info)
                    WriteIni(Path & "PCL\Setup.ini", "Logo", Logo)
                End If
                If State <> McVersionState.Error Then
                    WriteIni(Path & "PCL\Setup.ini", "ReleaseTime", ReleaseTime.ToString("yyyy-MM-dd HH:mm:ss"))
                    WriteIni(Path & "PCL\Setup.ini", "VersionFabric", Version.FabricVersion)
                    WriteIni(Path & "PCL\Setup.ini", "VersionOptiFine", Version.OptiFineVersion)
                    WriteIni(Path & "PCL\Setup.ini", "VersionLiteLoader", Version.HasLiteLoader)
                    WriteIni(Path & "PCL\Setup.ini", "VersionForge", Version.ForgeVersion)
                    WriteIni(Path & "PCL\Setup.ini", "VersionApiCode", Version.SortCode)
                    WriteIni(Path & "PCL\Setup.ini", "VersionOriginal", Version.McName)
                    WriteIni(Path & "PCL\Setup.ini", "VersionOriginalMain", Version.McCodeMain)
                    WriteIni(Path & "PCL\Setup.ini", "VersionOriginalSub", Version.McCodeSub)
                End If
            Catch ex As Exception
                Info = "未知错误：" & GetExceptionSummary(ex)
                Logo = "pack://application:,,,/images/Blocks/RedstoneBlock.png"
                State = McVersionState.Error
                Log(ex, "加载版本失败（" & Name & "）", LogLevel.Feedback)
            Finally
                IsLoaded = True
            End Try
            Return Me
        End Function
        Public IsLoaded As Boolean = False

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim version = TryCast(obj, McVersion)
            Return version IsNot Nothing AndAlso Path = version.Path
        End Function
        Public Shared Operator =(a As McVersion, b As McVersion) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return a.Path = b.Path
        End Operator
        Public Shared Operator <>(a As McVersion, b As McVersion) As Boolean
            Return Not (a = b)
        End Operator

    End Class
    Public Enum McVersionState
        [Error]
        Original
        Snapshot
        Fool
        OptiFine
        Old
        Forge
        LiteLoader
        Fabric
    End Enum

    ''' <summary>
    ''' 某个 Minecraft 实例的版本名、附加组件信息。
    ''' </summary>
    Public Class McVersionInfo

        ''' <summary>
        ''' 版本的 API 信息是否已加载。
        ''' </summary>
        Public IsApiLoaded As Boolean = False

        '原版

        ''' <summary>
        ''' 原版版本名。如 1.12.2，16w01a。
        ''' </summary>
        Public McName As String
        ''' <summary>
        ''' 原版主版本号，如 12（For 1.12.2），快照则固定为 99。不可用则为 -1。
        ''' </summary>
        Public McCodeMain As Integer = -1
        ''' <summary>
        ''' 原版次版本号，如 2（For 1.12.2），快照则固定为 99。不可用则为 -1。
        ''' </summary>
        Public McCodeSub As Integer = -1

        'OptiFine

        ''' <summary>
        ''' 该版本是否通过 Json 安装了 OptiFine。
        ''' </summary>
        Public HasOptiFine As Boolean = False
        ''' <summary>
        ''' OptiFine 版本号，如 C8、C9_pre10。
        ''' </summary>
        Public OptiFineVersion As String = ""

        'Forge

        ''' <summary>
        ''' 该版本是否安装了 Forge。
        ''' </summary>
        Public HasForge As Boolean = False
        ''' <summary>
        ''' Forge 版本号，如 31.1.2、14.23.5.2847。
        ''' </summary>
        Public ForgeVersion As String = ""

        'Fabric

        ''' <summary>
        ''' 该版本是否安装了 Fabric。
        ''' </summary>
        Public HasFabric As Boolean = False
        ''' <summary>
        ''' Fabric 版本号，如 0.7.2.175。
        ''' </summary>
        Public FabricVersion As String = ""

        'LiteLoader

        ''' <summary>
        ''' 该版本是否安装了 LiteLoader。
        ''' </summary>
        Public HasLiteLoader As Boolean = False

        'API

        ''' <summary>
        ''' 生成对此版本信息的用户友好的描述性字符串。
        ''' </summary>
        Public Overrides Function ToString() As String
            ToString = ""
            If HasForge Then ToString += ", Forge" & If(ForgeVersion = "未知版本", "", " " & ForgeVersion)
            If HasFabric Then ToString += ", Fabric" & If(FabricVersion = "未知版本", "", " " & FabricVersion)
            If HasOptiFine Then ToString += ", OptiFine" & If(OptiFineVersion = "未知版本", "", " " & OptiFineVersion)
            If HasLiteLoader Then ToString += ", LiteLoader"
            If ToString = "" Then
                Return "原版 " & McName
            Else
                Return McName & ToString & If(ModeDebug, " (" & SortCode & "#)", "")
            End If
        End Function

        ''' <summary>
        ''' 用于排序比较的编号。
        ''' </summary>
        Public Property SortCode As Integer
            Get
                If _SortCode = -2 Then
                    '初始化
                    Try
                        If HasFabric Then
                            If FabricVersion = "未知版本" Then Return 0
                            Dim SubVersions = FabricVersion.Split(".")
                            If SubVersions.Length >= 3 Then
                                _SortCode = Val(SubVersions(0)) * 10000 + Val(SubVersions(1)) * 100 + Val(SubVersions(2))
                            Else
                                Throw New Exception("无效的 Fabric 版本：" & ForgeVersion)
                            End If
                        ElseIf HasForge Then
                            If ForgeVersion = "未知版本" Then Return 0
                            Dim SubVersions = ForgeVersion.Split(".")
                            If SubVersions.Length = 4 Then
                                _SortCode = Val(SubVersions(0)) * 1000000 + Val(SubVersions(1)) * 10000 + Val(SubVersions(3))
                            ElseIf SubVersions.Length = 3 Then
                                _SortCode = Val(SubVersions(0)) * 1000000 + Val(SubVersions(1)) * 10000 + Val(SubVersions(2))
                            Else
                                Throw New Exception("无效的 Forge 版本：" & ForgeVersion)
                            End If
                        ElseIf HasOptiFine Then
                            If OptiFineVersion = "未知版本" Then Return 0
                            '由对应原版次级版本号（2 位）、字母（2 位）、末尾数字（2 位）、测试标记（2 位，正式版为 99，Pre[x] 为 50+x，Beta[x] 为 x）组成
                            _SortCode =
                                If(McCodeSub >= 0, McCodeSub, 0) * 1000000 +                    '第一段：原版次级版本号（2 位）
                                (Asc(CType(Left(OptiFineVersion.ToUpper, 1), Char)) - Asc("A"c) + 1) * 10000 + '第二段：字母编号（2 位），如 G2 中的 G（7）
                                Val(RegexSeek(Right(OptiFineVersion, OptiFineVersion.Length - 1), "[0-9]+")) * 100         '第三段：末尾数字（2 位），如 C5 beta4 中的 5
                            '第三段：测试标记
                            If OptiFineVersion.ToLower.Contains("pre") Then _SortCode += 50
                            If OptiFineVersion.ToLower.Contains("pre") OrElse OptiFineVersion.ToLower.Contains("beta") Then
                                If Val(Right(OptiFineVersion, 1)) = 0 AndAlso Right(OptiFineVersion, 1) <> "0" Then
                                    _SortCode += 1 '为 pre 或 beta 结尾，视作 1
                                Else
                                    _SortCode += Val(RegexSeek(OptiFineVersion.ToLower, "(?<=((pre)|(beta)))[0-9]+"))
                                End If
                            Else
                                _SortCode += 99
                            End If
                        Else
                            _SortCode = -1
                        End If
                    Catch ex As Exception
                        _SortCode = -1
                        Log(ex, "获取 API 版本信息失败：" & ToString())
                    End Try
                End If
                Return _SortCode
            End Get
            Set(ByVal value As Integer)
                _SortCode = value
            End Set
        End Property
        Private _SortCode As Integer = -2

    End Class

    ''' <summary>
    ''' 根据版本名获取对应的愚人节版本描述。非愚人节版本会返回空字符串。
    ''' </summary>
    Public Function GetMcFoolName(Name As String) As String
        Name = Name.ToLower
        If Name.StartsWith("2.0") Then
            Return "这个秘密计划了两年的更新将游戏推向了一个新高度！"
        ElseIf Name.StartsWith("20w14inf") OrElse Name = "20w14∞" Then
            Return "我们加入了 20 亿个新的维度，让无限的想象变成了现实！"
        ElseIf Name = "15w14a" Then
            Return "作为一款全年龄向的游戏，我们需要和平，需要爱与拥抱。"
        ElseIf Name = "1.rv-pre1" Then
            Return "是时候将现代科技带入 Minecraft 了！"
        ElseIf Name = "3d shareware v1.34" Then
            Return "我们从地下室的废墟里找到了这个开发于 1994 年的杰作！"
        ElseIf Name = "22w13oneblockatatime" Then
            Return "一次一个方块更新！迎接全新的挖掘、合成与骑乘玩法吧！"
        ElseIf Name = "23w13a_or_b" Then
            Return "研究表明玩家喜欢作出选择——越多越好！"
        Else
            Return ""
        End If
    End Function

    ''' <summary>
    ''' 当前按卡片分类的所有版本列表。
    ''' </summary>
    Public McVersionList As New Dictionary(Of McVersionCardType, List(Of McVersion))

#End Region

#Region "版本列表加载"

    ''' <summary>
    ''' 是否要求本次加载强制刷新版本列表。
    ''' </summary>
    Public McVersionListForceRefresh As Boolean = False
    ''' <summary>
    ''' 加载 Minecraft 文件夹的版本列表。
    ''' </summary>
    Public McVersionListLoader As New LoaderTask(Of String, Integer)("Minecraft Version List", AddressOf McVersionListLoad) With {.ReloadTimeout = 1}

    ''' <summary>
    ''' 开始加载当前 Minecraft 文件夹的版本列表。
    ''' </summary>
    Private Sub McVersionListLoad(Loader As LoaderTask(Of String, Integer))
        '开始加载
        Dim Path As String = Loader.Input
        Try
            '初始化
            McVersionList.Clear()

            '检测缓存是否需要更新
            Dim FolderList As New List(Of String)
            If Directory.Exists(Path & "versions") Then '不要使用 CheckPermission，会导致写入时间改变，从而使得文件夹被强制刷新
                Try
                    For Each Folder As DirectoryInfo In New DirectoryInfo(Path & "versions").GetDirectories
                        FolderList.Add(Folder.Name)
                    Next
                Catch ex As Exception
                    Throw New Exception("无法读取版本文件夹，可能是由于没有权限（" & Path & "versions）", ex)
                End Try
            End If
            '没有可用版本
            If FolderList.Count = 0 Then
                WriteIni(Path & "PCL.ini", "VersionCache", "") '清空缓存
                GoTo OnLoaded
            End If
            '有可用版本
            Dim FolderListCheck As Integer = GetHash(McVersionCacheVersion & "#" & Join(FolderList.ToArray, "#")) Mod (Integer.MaxValue - 1) '根据文件夹名列表生成辨识码
            If Not McVersionListForceRefresh AndAlso Val(ReadIni(Path & "PCL.ini", "VersionCache")) = FolderListCheck Then
                '可以使用缓存
                Dim Result = McVersionListLoadCache(Path)
                If Result Is Nothing Then
                    GoTo Reload
                Else
                    McVersionList = Result
                End If
            Else
                '文件夹列表不符
Reload:
                McVersionListForceRefresh = False
                Log("[Minecraft] 文件夹列表变更，重载所有版本")
                WriteIni(Path & "PCL.ini", "VersionCache", FolderListCheck)
                McVersionList = McVersionListLoadNoCache(Path)
            End If
            If Loader.IsAborted Then Exit Sub

            '改变当前选择的版本
OnLoaded:
            If McVersionList.Any(Function(v) v.Key <> McVersionCardType.Error) Then '不能判断 Count = 0，这在只有错误版本时导致了误判
                '尝试读取已储存的选择
                Dim SavedSelection As String = ReadIni(Path & "PCL.ini", "Version")
                If Not SavedSelection = "" Then
                    For Each Card As KeyValuePair(Of McVersionCardType, List(Of McVersion)) In McVersionList
                        For Each Version As McVersion In Card.Value
                            If Version.Name = SavedSelection AndAlso Not Version.State = McVersionState.Error Then
                                '使用已储存的选择
                                McVersionCurrent = Version
                                Setup.Set("LaunchVersionSelect", McVersionCurrent.Name)
                                Log("[Minecraft] 选择该文件夹储存的 Minecraft 版本：" & McVersionCurrent.Path)
                                Exit Sub
                            End If
                        Next
                    Next
                End If
                If Not McVersionList.First.Value(0).State = McVersionState.Error Then
                    '自动选择第一项
                    McVersionCurrent = McVersionList.First.Value(0)
                    Setup.Set("LaunchVersionSelect", McVersionCurrent.Name)
                    Log("[Launch] 自动选择 Minecraft 版本：" & McVersionCurrent.Path)
                End If
            Else
                McVersionCurrent = Nothing
                Setup.Set("LaunchVersionSelect", "")
                Log("[Minecraft] 未找到可用 Minecraft 版本")
            End If
            If Setup.Get("SystemDebugDelay") Then Thread.Sleep(RandomInteger(200, 3000))
        Catch ex As ThreadInterruptedException
        Catch ex As Exception
            WriteIni(Path & "PCL.ini", "VersionCache", "") '要求下次重新加载
            Log(ex, "加载 .minecraft 版本列表失败", LogLevel.Feedback)
        End Try
    End Sub

    '获取版本列表
    Private Function McVersionListLoadCache(Path As String) As Dictionary(Of McVersionCardType, List(Of McVersion))
        Dim ResultVersionList As New Dictionary(Of McVersionCardType, List(Of McVersion))
        Try
            Dim CardCount As Integer = ReadIni(Path & "PCL.ini", "CardCount", -1)
            If CardCount = -1 Then Return Nothing
            For i = 0 To CardCount - 1
                Dim CardType As McVersionCardType = ReadIni(Path & "PCL.ini", "CardKey" & (i + 1), ":")
                Dim VersionList As New List(Of McVersion)

                '循环读取版本
                For Each Folder As String In ReadIni(Path & "PCL.ini", "CardValue" & (i + 1), ":").Split(":")
                    If Folder = "" Then Continue For
                    If File.Exists(Path & "versions\" & Folder & "\.pclignore") Then
                        Log("[Minecraft] 跳过要求忽略的项目：" & Path & "versions\" & Folder)
                        Continue For
                    End If
                    Try

                        '读取单个版本
                        Dim Version As New McVersion(Path & "versions\" & Folder & "\")
                        VersionList.Add(Version)
                        Version.Info = ReadIni(Version.Path & "PCL\Setup.ini", "CustomInfo", "")
                        If Version.Info = "" Then Version.Info = ReadIni(Version.Path & "PCL\Setup.ini", "Info", Version.Info)
                        Version.Logo = ReadIni(Version.Path & "PCL\Setup.ini", "Logo", Version.Logo)
                        Version.ReleaseTime = ReadIni(Version.Path & "PCL\Setup.ini", "ReleaseTime", Version.ReleaseTime)
                        Version.State = ReadIni(Version.Path & "PCL\Setup.ini", "State", Version.State)
                        Version.IsStar = ReadIni(Version.Path & "PCL\Setup.ini", "IsStar", False)
                        Version.DisplayType = ReadIni(Path & "PCL\Setup.ini", "DisplayType", McVersionCardType.Auto)
                        If Version.State <> McVersionState.Error AndAlso
                           ReadIni(Version.Path & "PCL\Setup.ini", "VersionOriginal", "Unknown") <> "Unknown" Then '旧版本可能没有这一项，导致 Version 不加载（#643）
                            Dim VersionInfo As New McVersionInfo With {
                                .FabricVersion = ReadIni(Version.Path & "PCL\Setup.ini", "VersionFabric", ""),
                                .ForgeVersion = ReadIni(Version.Path & "PCL\Setup.ini", "VersionForge", ""),
                                .OptiFineVersion = ReadIni(Version.Path & "PCL\Setup.ini", "VersionOptiFine", ""),
                                .HasLiteLoader = ReadIni(Version.Path & "PCL\Setup.ini", "VersionLiteLoader", False),
                                .SortCode = ReadIni(Version.Path & "PCL\Setup.ini", "VersionApiCode", -1),
                                .McName = ReadIni(Version.Path & "PCL\Setup.ini", "VersionOriginal", "Unknown"),
                                .McCodeMain = ReadIni(Version.Path & "PCL\Setup.ini", "VersionOriginalMain", -1),
                                .McCodeSub = ReadIni(Version.Path & "PCL\Setup.ini", "VersionOriginalSub", -1),
                                .IsApiLoaded = True
                            }
                            VersionInfo.HasFabric = VersionInfo.FabricVersion.Count > 1
                            VersionInfo.HasForge = VersionInfo.ForgeVersion.Count > 1
                            VersionInfo.HasOptiFine = VersionInfo.OptiFineVersion.Count > 1
                            Version.Version = VersionInfo
                        End If

                        '重新检查错误版本
                        If Version.State = McVersionState.Error Then
                            '重新获取版本错误信息
                            Dim OldDesc As String = Version.Info
                            Version.State = McVersionState.Original
                            Version.Check()
                            '校验错误原因是否改变
                            Dim CustomInfo As String = ReadIni(Version.Path & "PCL\Setup.ini", "CustomInfo")
                            If Version.State = McVersionState.Original OrElse (CustomInfo = "" AndAlso Not OldDesc = Version.Info) Then
                                Log("[Minecraft] 版本 " & Version.Name & " 的错误状态已变更，新的状态为：" & Version.Info)
                                Return Nothing
                            End If
                        End If

                        '校验未加载的版本
                        If Version.Logo = "" Then
                            Log("[Minecraft] 版本 " & Version.Name & " 未被加载")
                            Return Nothing
                        End If

                    Catch ex As Exception
                        Log(ex, "读取版本加载缓存失败（" & Folder & "）", LogLevel.Debug)
                        Return Nothing
                    End Try
                Next

                ResultVersionList.Add(CardType, VersionList)
            Next
            Return ResultVersionList
        Catch ex As Exception
            Log(ex, "读取版本缓存失败")
            Return Nothing
        End Try
    End Function
    Private Function McVersionListLoadNoCache(Path As String) As Dictionary(Of McVersionCardType, List(Of McVersion))
        Dim VersionList As New List(Of McVersion)
        Dim ResultVersionList As New Dictionary(Of McVersionCardType, List(Of McVersion))
#Region "循环加载每个版本的信息"
        Dim Dirs As DirectoryInfo() = New DirectoryInfo(Path & "versions").GetDirectories
        For Each Folder As DirectoryInfo In Dirs
            If (Folder.Name = "cache" OrElse Folder.Name = "BLClient" OrElse Folder.Name = "PCL") AndAlso Not File.Exists(Folder.FullName & "\" & Folder.Name & ".json") Then
                Log("[Minecraft] 跳过可能不是版本文件夹的项目：" & Folder.FullName)
                Continue For
            End If
            If File.Exists(Folder.FullName & "\.pclignore") Then
                Log("[Minecraft] 跳过要求忽略的项目：" & Folder.FullName)
                Continue For
            End If
            Dim Version As New McVersion(Folder.FullName & "\")
            VersionList.Add(Version)
            Version.Load()
        Next
#End Region
#Region "将版本分类到各个卡片"
        Try

            '未经过自定义的版本列表
            Dim VersionListOriginal As New Dictionary(Of McVersionCardType, List(Of McVersion))

            '单独列出收藏的版本
            Dim StaredVersions As New List(Of McVersion)
            For Each Version As McVersion In VersionList
                If Version.IsStar AndAlso Not Version.DisplayType = McVersionCardType.Hidden Then StaredVersions.Add(Version)
            Next
            If StaredVersions.Count > 0 Then VersionListOriginal.Add(McVersionCardType.Star, StaredVersions)

            '预先筛选出愚人节的版本
            McVersionFilter(VersionList, VersionListOriginal, {McVersionState.Error}, McVersionCardType.Error)
            McVersionFilter(VersionList, VersionListOriginal, {McVersionState.Fool}, McVersionCardType.Fool)

            '筛选 API 版本
            McVersionFilter(VersionList, VersionListOriginal, {McVersionState.Forge, McVersionState.LiteLoader, McVersionState.Fabric}, McVersionCardType.API)

            '将老版本预先分类入不常用，只剩余原版、快照、OptiFine
            Dim VersionUseful As New List(Of McVersion)
            Dim VersionRubbish As New List(Of McVersion)
            McVersionFilter(VersionList, {McVersionState.Old}, VersionRubbish)

            '确认最新版本，若为快照则加入常用列表
            Dim LargestVersion As McVersion = Nothing '最新的版本
            For Each Version As McVersion In VersionList
                If Version.State = McVersionState.Original OrElse Version.State = McVersionState.Snapshot Then
                    If LargestVersion Is Nothing OrElse Version.ReleaseTime > LargestVersion.ReleaseTime Then LargestVersion = Version
                End If
            Next
            If LargestVersion IsNot Nothing AndAlso LargestVersion.State = McVersionState.Snapshot Then
                VersionUseful.Add(LargestVersion)
                VersionList.Remove(LargestVersion)
            End If

            '将剩余的快照全部拖进不常用列表
            McVersionFilter(VersionList, {McVersionState.Snapshot}, VersionRubbish)

            '获取每个大版本下最新的原版与 OptiFine
            Dim NewerVersion As New Dictionary(Of String, McVersion)
            Dim ExistVersion As New List(Of Integer)
            For Each Version As McVersion In VersionList
                If Version.Version.McCodeMain < 2 Then Continue For '未获取成功的版本
                If Not ExistVersion.Contains(Version.Version.McCodeMain) Then ExistVersion.Add(Version.Version.McCodeMain)
                If NewerVersion.ContainsKey(Version.Version.McCodeMain & "-" & Version.State) Then
                    If Version.Version.HasOptiFine Then
                        'OptiFine 根据排序识别号判断
                        If Version.Version.SortCode > NewerVersion(Version.Version.McCodeMain & "-" & Version.State).Version.SortCode Then NewerVersion(Version.Version.McCodeMain & "-" & Version.State) = Version
                    Else
                        '原版根据发布时间判断
                        If Version.ReleaseTime > NewerVersion(Version.Version.McCodeMain & "-" & Version.State).ReleaseTime Then NewerVersion(Version.Version.McCodeMain & "-" & Version.State) = Version
                    End If
                Else
                    NewerVersion.Add(Version.Version.McCodeMain & "-" & Version.State, Version)
                End If
            Next

            '将每个大版本下的最常规版本加入
            For Each Code As Integer In ExistVersion
                If NewerVersion.ContainsKey(Code & "-" & McVersionState.OptiFine) AndAlso NewerVersion.ContainsKey(Code & "-" & McVersionState.Original) Then
                    '同时存在 OptiFine 与原版
                    Dim OriginalVersion As McVersion = NewerVersion(Code & "-" & McVersionState.Original)
                    Dim OptiFineVersion As McVersion = NewerVersion(Code & "-" & McVersionState.OptiFine)
                    If OriginalVersion.Version.McCodeSub > OptiFineVersion.Version.McCodeSub Then
                        '仅在原版比 OptiFine 更新时才加入原版
                        VersionUseful.Add(OriginalVersion)
                        VersionList.Remove(OriginalVersion)
                    End If
                    VersionUseful.Add(OptiFineVersion)
                    VersionList.Remove(OptiFineVersion)
                ElseIf NewerVersion.ContainsKey(Code & "-" & McVersionState.OptiFine) Then
                    '没有原版，直接加入 OptiFine
                    VersionUseful.Add(NewerVersion(Code & "-" & McVersionState.OptiFine))
                    VersionList.Remove(NewerVersion(Code & "-" & McVersionState.OptiFine))
                ElseIf NewerVersion.ContainsKey(Code & "-" & McVersionState.Original) Then
                    '没有 OptiFine，直接加入原版
                    VersionUseful.Add(NewerVersion(Code & "-" & McVersionState.Original))
                    VersionList.Remove(NewerVersion(Code & "-" & McVersionState.Original))
                End If
            Next

            '将剩余的东西添加进去
            VersionRubbish.AddRange(VersionList)
            If VersionUseful.Count > 0 Then VersionListOriginal.Add(McVersionCardType.OriginalLike, VersionUseful)
            If VersionRubbish.Count > 0 Then VersionListOriginal.Add(McVersionCardType.Rubbish, VersionRubbish)

            '按照自定义版本分类重新添加
            For Each VersionPair In VersionListOriginal
                For Each Version As McVersion In VersionPair.Value
                    Dim RealType = If(Version.DisplayType = 0 OrElse VersionPair.Key = McVersionCardType.Star, VersionPair.Key, Version.DisplayType)
                    If Not ResultVersionList.ContainsKey(RealType) Then ResultVersionList.Add(RealType, New List(Of McVersion))
                    ResultVersionList(RealType).Add(Version)
                Next
            Next

        Catch ex As Exception
            ResultVersionList.Clear()
            Log(ex, "分类版本列表失败", LogLevel.Feedback)
        End Try
#End Region
#Region "对卡片与版本进行排序"

        '对卡片进行整体排序
        Dim SortedVersionList As New Dictionary(Of McVersionCardType, List(Of McVersion))
        For Each SortRule As String In {McVersionCardType.Star, McVersionCardType.API, McVersionCardType.OriginalLike, McVersionCardType.Rubbish, McVersionCardType.Fool, McVersionCardType.Error, McVersionCardType.Hidden}
            If ResultVersionList.ContainsKey(SortRule) Then SortedVersionList.Add(SortRule, ResultVersionList(SortRule))
        Next
        ResultVersionList = SortedVersionList

        '常规版本：快照放在最上面，此后按版本号从高到低排序
        If ResultVersionList.ContainsKey(McVersionCardType.OriginalLike) Then
            Dim OldList As List(Of McVersion) = ResultVersionList(McVersionCardType.OriginalLike)
            '提取快照
            Dim Snapshot As McVersion = Nothing
            For Each Version As McVersion In OldList
                If Version.State = McVersionState.Snapshot Then
                    Snapshot = Version
                    Exit For
                End If
            Next
            If Not IsNothing(Snapshot) Then OldList.Remove(Snapshot)
            '按版本号排序
            Dim NewList As List(Of McVersion) = Sort(OldList, Function(Left As McVersion, Right As McVersion)
                                                                  Return Left.Version.McCodeMain > Right.Version.McCodeMain
                                                              End Function)
            '回设
            If Not IsNothing(Snapshot) Then NewList.Insert(0, Snapshot)
            ResultVersionList(McVersionCardType.OriginalLike) = NewList
        End If

        '不常用版本：按发布时间新旧排序，如果不可用则按名称排序
        If ResultVersionList.ContainsKey(McVersionCardType.Rubbish) Then
            ResultVersionList(McVersionCardType.Rubbish) = Sort(ResultVersionList(McVersionCardType.Rubbish), Function(Left As McVersion, Right As McVersion)
                                                                                                                  Dim LeftYear As Integer = Left.ReleaseTime.Year '+ If(Left.State = McVersionState.Original OrElse Left.Version.HasOptiFine, 100, 0)
                                                                                                                  Dim RightYear As Integer = Right.ReleaseTime.Year '+ If(Right.State = McVersionState.Original OrElse Left.Version.HasOptiFine, 100, 0)
                                                                                                                  If LeftYear > 2000 AndAlso RightYear > 2000 Then
                                                                                                                      If LeftYear <> RightYear Then
                                                                                                                          Return LeftYear > RightYear
                                                                                                                      Else
                                                                                                                          Return Left.ReleaseTime > Right.ReleaseTime
                                                                                                                      End If
                                                                                                                  ElseIf LeftYear > 2000 AndAlso RightYear < 2000 Then
                                                                                                                      Return True
                                                                                                                  ElseIf LeftYear < 2000 AndAlso RightYear > 2000 Then
                                                                                                                      Return False
                                                                                                                  Else
                                                                                                                      Return Left.Name > Right.Name
                                                                                                                  End If
                                                                                                              End Function)
        End If

        'API 版本：优先按版本排序，此后【先放 Fabric，再放 Forge（按版本号从高到低排序），最后放 LiteLoader（按名称排序）】
        If ResultVersionList.ContainsKey(McVersionCardType.API) Then
            ResultVersionList(McVersionCardType.API) = Sort(ResultVersionList(McVersionCardType.API), Function(Left As McVersion, Right As McVersion)
                                                                                                          Dim Basic = VersionSortInteger(Left.Version.McName, Right.Version.McName)
                                                                                                          If Basic <> 0 Then
                                                                                                              Return Basic > 0
                                                                                                          Else
                                                                                                              If Left.Version.HasFabric Xor Right.Version.HasFabric Then
                                                                                                                  Return Left.Version.HasFabric
                                                                                                              ElseIf Left.Version.HasForge Xor Right.Version.HasForge Then
                                                                                                                  Return Left.Version.HasForge
                                                                                                              ElseIf Not Left.Version.SortCode <> Right.Version.SortCode Then
                                                                                                                  Return Left.Version.SortCode > Right.Version.SortCode
                                                                                                              Else
                                                                                                                  Return Left.Name > Right.Name
                                                                                                              End If
                                                                                                          End If
                                                                                                      End Function)
        End If

#End Region
#Region "保存卡片缓存"
        WriteIni(Path & "PCL.ini", "CardCount", ResultVersionList.Count)
        For i = 0 To ResultVersionList.Count - 1
            WriteIni(Path & "PCL.ini", "CardKey" & (i + 1), ResultVersionList.Keys(i))
            Dim Value As String = ""
            For Each Version As McVersion In ResultVersionList.Values(i)
                Value += Version.Name & ":"
            Next
            WriteIni(Path & "PCL.ini", "CardValue" & (i + 1), Value)
        Next
#End Region
        Return ResultVersionList
    End Function
    ''' <summary>
    ''' 筛选特定种类的版本，并直接添加为卡片。
    ''' </summary>
    ''' <param name="VersionList">用于筛选的列表。</param>
    ''' <param name="Formula">需要筛选出的版本类型。-2 代表隐藏的版本。</param>
    ''' <param name="CardType">卡片的名称。</param>
    Private Sub McVersionFilter(ByRef VersionList As List(Of McVersion), ByRef Target As Dictionary(Of McVersionCardType, List(Of McVersion)), Formula As McVersionState(), CardType As McVersionCardType)
        '将某种版本筛选出来
        Dim KeepList As New List(Of McVersion)
        For Each Version As McVersion In VersionList
            For Each Type As Integer In Formula
                If Type = Version.State Then
                    KeepList.Add(Version)
                    GoTo NextVersion
                End If
            Next
NextVersion:
        Next
        '加入版本列表，并从剩余中删除
        If KeepList.Count > 0 Then
            Target.Add(CardType, KeepList)
            For Each Version As McVersion In KeepList
                VersionList.Remove(Version)
            Next
        End If
    End Sub
    ''' <summary>
    ''' 筛选特定种类的版本，并增加入一个已有列表中。
    ''' </summary>
    ''' <param name="VersionList">用于筛选的列表。</param>
    ''' <param name="Formula">需要筛选出的版本类型。-2 代表隐藏的版本。</param>
    ''' <param name="KeepList">传入需要增加入的列表。</param>
    Private Sub McVersionFilter(ByRef VersionList As List(Of McVersion), Formula As McVersionState(), ByRef KeepList As List(Of McVersion))
        For Each Version As McVersion In VersionList
            For Each Type As McVersionState In Formula
                If Type <> Version.State Then Continue For
                KeepList.Add(Version) : Exit For
            Next
NextVersion:
        Next
        If KeepList.Count > 0 Then
            For Each Version As McVersion In KeepList
                VersionList.Remove(Version)
            Next
        End If
    End Sub
    Public Enum McVersionCardType
        Star = -1
        Auto = 0 '仅用于强制版本分类的自动
        Hidden = 1
        API = 2
        OriginalLike = 3
        Rubbish = 4
        Fool = 5
        [Error] = 6
    End Enum

#End Region

#Region "皮肤"

    Public Structure McSkinInfo
        Public IsSlim As Boolean
        Public LocalFile As String
        Public IsVaild As Boolean
    End Structure
    ''' <summary>
    ''' 要求玩家选择一个皮肤文件，并进行相关校验。
    ''' </summary>
    Public Function McSkinSelect() As McSkinInfo
        Dim FileName As String = SelectFile("皮肤文件(*.png)|*.png", "选择皮肤文件")

        '验证有效性
        If FileName = "" Then Return New McSkinInfo With {.IsVaild = False}
        Try
            Dim Image As New MyBitmap(FileName)
            If Image.Pic.Width <> 64 OrElse Not (Image.Pic.Height = 32 OrElse Image.Pic.Height = 64) Then
                Hint("皮肤图片大小应为 64x32 像素或 64x64 像素！", HintType.Critical)
                Return New McSkinInfo With {.IsVaild = False}
            End If
            Dim FileInfo As New FileInfo(FileName)
            If FileInfo.Length > 24 * 1024 Then
                Hint("皮肤文件大小需小于 24 KB，而所选文件大小为 " & Math.Round(FileInfo.Length / 1024, 2) & " KB", HintType.Critical)
                Return New McSkinInfo With {.IsVaild = False}
            End If
        Catch ex As Exception
            Log(ex, "皮肤文件存在错误", LogLevel.Hint)
            Return New McSkinInfo With {.IsVaild = False}
        End Try

        '获取皮肤种类
        Dim IsSlim As Integer = MyMsgBox("此皮肤为 Steve 模型（粗手臂）还是 Alex 模型（细手臂）？", "选择皮肤种类", "Steve 模型", "Alex 模型", "我不知道", HighLight:=False)
        If IsSlim = 3 Then
            Hint("请在皮肤下载页面确认皮肤种类后再使用此皮肤！")
            Return New McSkinInfo With {.IsVaild = False}
        End If

        Return New McSkinInfo With {.IsVaild = True, .IsSlim = IsSlim = 2, .LocalFile = FileName}
    End Function

    ''' <summary>
    ''' 获取 Uuid 对应的皮肤文件地址，失败将抛出异常。
    ''' </summary>
    Public Function McSkinGetAddress(Uuid As String, Type As String) As String
        If Uuid = "" Then Throw New Exception("Uuid 为空。")
        If Uuid.StartsWith("000000") Then Throw New Exception("离线 Uuid 无正版皮肤文件。")
        '尝试读取缓存
        Dim CacheSkinAddress As String = ReadIni(PathTemp & "Cache\Skin\Index" & Type & ".ini", Uuid)
        If Not CacheSkinAddress = "" Then Return CacheSkinAddress
        '获取皮肤地址
        Dim Url As String
        Select Case Type
            Case "Mojang", "Ms"
                Url = "https://sessionserver.mojang.com/session/minecraft/profile/"
            Case "Nide"
                Url = "https://auth.mc-user.com:233/" & If(McVersionCurrent Is Nothing, Setup.Get("CacheNideServer"), Setup.Get("VersionServerNide", Version:=McVersionCurrent)) & "/sessionserver/session/minecraft/profile/"
            Case "Auth"
                Url = If(McVersionCurrent Is Nothing, Setup.Get("CacheAuthServerServer"), Setup.Get("VersionServerAuthServer", Version:=McVersionCurrent)) & "/sessionserver/session/minecraft/profile/"
            Case Else
                Throw New ArgumentException("皮肤地址种类无效：" & If(Type, "null"))
        End Select
        Dim SkinString = NetGetCodeByRequestRetry(Url & Uuid)
        If SkinString = "" Then Throw New Exception("皮肤返回值为空，可能是未设置自定义皮肤的用户")
        '处理皮肤地址
        Dim SkinValue As String
        Try
            For Each SkinProperty In GetJson(SkinString)("properties")
                If SkinProperty("name") = "textures" Then
                    SkinValue = SkinProperty("value")
                    Exit Try
                End If
            Next
            Throw New Exception("未从皮肤返回值中找到符合条件的 Property")
        Catch ex As Exception
            Log(ex, "无法完成解析的皮肤返回值，可能是未设置自定义皮肤的用户：" & SkinString, LogLevel.Developer)
            Throw New Exception("皮肤返回值中不包含皮肤数据项，可能是未设置自定义皮肤的用户", ex)
        End Try
        SkinString = Encoding.GetEncoding("utf-8").GetString(Convert.FromBase64String(SkinValue))
        Dim SkinJson As JObject = GetJson(SkinString.ToLower)
        If SkinJson("textures") Is Nothing OrElse SkinJson("textures")("skin") Is Nothing OrElse SkinJson("textures")("skin")("url") Is Nothing Then
            Throw New Exception("用户未设置自定义皮肤")
        Else
            SkinValue = SkinJson("textures")("skin")("url").ToString
        End If
        '保存缓存
        WriteIni(PathTemp & "Cache\Skin\Index" & Type & ".ini", Uuid, SkinValue)
        Log("[Skin] UUID " & Uuid & " 对应的皮肤文件为 " & SkinValue)
        Return SkinValue
    End Function

    Private ReadOnly McSkinDownloadLock As New Object
    ''' <summary>
    ''' 从 Url 下载皮肤。返回本地文件路径，失败将抛出异常。
    ''' </summary>
    Public Function McSkinDownload(Address As String) As String
        Dim SkinName As String = GetFileNameFromPath(Address)
        Dim FileAddress As String = PathTemp & "Cache\Skin\" & GetHash(Address) & ".png"
        SyncLock McSkinDownloadLock
            If Not File.Exists(FileAddress) Then
                NetDownload(Address, FileAddress & NetDownloadEnd)
                File.Delete(FileAddress)
                FileSystem.Rename(FileAddress & NetDownloadEnd, FileAddress)
                Log("[Minecraft] 皮肤下载成功：" & FileAddress)
            End If
            Return FileAddress
        End SyncLock
    End Function

    ''' <summary>
    ''' 获取 Uuid 对应的皮肤，返回“Steve”或“Alex”。
    ''' </summary>
    Public Function McSkinSex(Uuid As String) As String
        If Not Uuid.Length = 32 Then Return "Steve"
        Dim a = Integer.Parse(Uuid(7), Globalization.NumberStyles.AllowHexSpecifier)
        Dim b = Integer.Parse(Uuid(15), Globalization.NumberStyles.AllowHexSpecifier)
        Dim c = Integer.Parse(Uuid(23), Globalization.NumberStyles.AllowHexSpecifier)
        Dim d = Integer.Parse(Uuid(31), Globalization.NumberStyles.AllowHexSpecifier)
        Return If((a Xor b Xor c Xor d) Mod 2, "Alex", "Steve")
        'Math.floorMod(uuid.hashCode(), 18)

        'Public Function hashCode(ByVal str As String) As Integer
        'Dim hash As Integer = 0
        'Dim n As Integer = str.Length
        'If n = 0 Then
        '    Return hash
        'End If
        'For i As Integer = 0 To n - 1
        '    hash = hash + Asc(str(i)) * (1 << (n - i - 1))
        'Next
        'Return hash
        'End Function
    End Function

#End Region

#Region "支持库文件（Library）"

    Public Class McLibToken
        ''' <summary>
        ''' 文件的完整本地路径。
        ''' </summary>
        Public LocalPath As String
        ''' <summary>
        ''' 文件大小。若无有效数据即为 0。
        ''' </summary>
        Public Size As Long = 0
        ''' <summary>
        ''' 是否为 Natives 文件。
        ''' </summary>
        Public IsNatives As Boolean = False
        ''' <summary>
        ''' 文件的 SHA1。
        ''' </summary>
        Public SHA1 As String = Nothing
        ''' <summary>
        ''' 由 Json 提供的 URL，若没有则为 Nothing。
        ''' </summary>
        Public Property Url As String
            Get
                Return _Url
            End Get
            Set(ByVal value As String)
                '孤儿 Forge 作者喜欢把没有的 URL 写个空字符串
                _Url = If(String.IsNullOrWhiteSpace(value), Nothing, value)
            End Set
        End Property
        Private _Url As String
        ''' <summary>
        ''' 原 Json 中 Name 项除去最后一部分版本号的较前部分。可能为 Nothing。
        ''' </summary>
        Public ReadOnly Property Name As String
            Get
                If OriginalName Is Nothing Then Return Nothing
                Dim Splited As New List(Of String)(OriginalName.Split(":"))
                Splited.RemoveAt(Splited.Count - 1)
                Return Join(Splited, ":")
            End Get
        End Property
        ''' <summary>
        ''' 原 Json 中 Name 项最后一部分的版本号。
        ''' </summary>
        Public ReadOnly Property Version As String
            Get
                Dim Splited = OriginalName.Split(":")
                Return Splited(Splited.Count - 1)
            End Get
        End Property
        ''' <summary>
        ''' 原 Json 中的 Name 项。
        ''' </summary>
        Public OriginalName As String
        ''' <summary>
        ''' 是否为 JumpLoader 项。
        ''' </summary>
        Public IsJumpLoader As Boolean = False

        Public Overrides Function ToString() As String
            Return If(IsNatives, "[Native] ", "") & GetString(Size) & " | " & LocalPath
        End Function
    End Class

    ''' <summary>
    ''' 检查是否符合 Json 中的 Rules。
    ''' </summary>
    ''' <param name="RuleToken">Json 中的 "rules" 项目。</param>
    Public Function McJsonRuleCheck(RuleToken As JToken) As Boolean
        If RuleToken Is Nothing Then Return True

        '初始化
        Dim Required As Boolean = False
        For Each Rule As JToken In RuleToken

            '单条条件验证
            Dim IsRightRule As Boolean = True '是否为正确的规则
            If Rule("os") IsNot Nothing Then '操作系统
                If Rule("os")("name") IsNot Nothing Then '操作系统名称
                    Dim OsName As String = Rule("os")("name").ToString
                    If OsName = "unknown" Then
                    ElseIf OsName = "windows" Then
                        If Rule("os")("version") IsNot Nothing Then '操作系统版本
                            Dim Cr As String = Rule("os")("version").ToString
                            IsRightRule = IsRightRule AndAlso RegexCheck(OSVersion, Cr)
                        End If
                    Else
                        IsRightRule = False
                    End If
                End If
                If Rule("os")("arch") IsNot Nothing Then '操作系统架构
                    IsRightRule = IsRightRule AndAlso ((Rule("os")("arch").ToString = "x86") = Is32BitSystem)
                End If
            End If
            If Not IsNothing(Rule("features")) Then '标签
                IsRightRule = IsRightRule AndAlso IsNothing(Rule("features")("is_demo_user")) '反选是否为 Demo 用户
                If CType(Rule("features"), JObject).Children.Any(Function(j As JProperty) j.Name.StartsWith("is_quick_play")) Then
                    IsRightRule = False '不开 Quick Play，让玩家自己加去
                End If
            End If

            '反选确认
            If Rule("action").ToString = "allow" Then
                If IsRightRule Then Required = True 'allow
            Else
                If IsRightRule Then Required = False 'disallow
            End If

        Next
        Return Required
    End Function
    Private OSVersion As String = My.Computer.Info.OSVersion

    ''' <summary>
    ''' 递归获取 Minecraft 某一版本的完整支持库列表。
    ''' </summary>
    Public Function McLibListGet(Version As McVersion, IncludeVersionJar As Boolean) As List(Of McLibToken)

        '获取当前支持库列表
        Log("[Minecraft] 获取支持库列表：" & Version.Name)
        McLibListGet = McLibListGetWithJson(Version.JsonObject, JumpLoaderFolder:=Version.PathIndie & ".jumploader\")

        '需要添加原版 Jar
        If IncludeVersionJar Then
            Dim RealVersion As McVersion
            Dim RequiredJar As String = Version.JsonObject("jar")?.ToString
            If Version.IsHmclFormatJson OrElse RequiredJar Is Nothing Then
                'HMCL 项直接使用自身的 Jar
                '根据 Inherit 获取最深层版本
                Dim OriginalVersion As McVersion = Version
                '1.17+ 的 Forge 不寻找 Inherit
                If Not (Version.Version.HasForge AndAlso Version.Version.McCodeMain >= 17) Then
                    Do Until OriginalVersion.InheritVersion = ""
                        If OriginalVersion.InheritVersion = OriginalVersion.Name Then Exit Do
                        OriginalVersion = New McVersion(PathMcFolder & "versions\" & OriginalVersion.InheritVersion & "\")
                    Loop
                End If
                '需要新建对象，否则后面的 Check 会导致 McVersionCurrent 的 State 变回 Original
                '复现：启动一个 Snapshot 版本
                RealVersion = New McVersion(OriginalVersion.Path)
            Else
                'Json 已提供 Jar 字段，使用该字段的信息
                RealVersion = New McVersion(RequiredJar)
            End If
            Dim ClientUrl As String, ClientSHA1 As String
            '判断需求的版本是否存在
            '不能调用 RealVersion.Check()，可能会莫名其妙地触发 CheckPermission 正被另一进程使用，导致误判前置不存在
            If Not File.Exists(RealVersion.Path & RealVersion.Name & ".json") Then
                RealVersion = Version
                Log("[Minecraft] 可能缺少前置版本 " & RealVersion.Name & "，找不到对应的 json 文件", LogLevel.Debug)
            End If
            '获取详细下载信息
            If RealVersion.JsonObject("downloads") IsNot Nothing AndAlso RealVersion.JsonObject("downloads")("client") IsNot Nothing Then
                ClientUrl = RealVersion.JsonObject("downloads")("client")("url")
                ClientSHA1 = RealVersion.JsonObject("downloads")("client")("sha1")
            Else
                ClientUrl = Nothing
                ClientSHA1 = Nothing
            End If
            '把所需的原版 Jar 添加进去
            McLibListGet.Add(New McLibToken With {.LocalPath = RealVersion.Path & RealVersion.Name & ".jar", .Size = 0, .IsNatives = False, .Url = ClientUrl, .SHA1 = ClientSHA1})
        End If

    End Function
    ''' <summary>
    ''' 获取 Minecraft 某一版本忽视继承的支持库列表，即结果中没有继承项。
    ''' </summary>
    Public Function McLibListGetWithJson(JsonObject As JObject, Optional KeepSameNameDifferentVersionResult As Boolean = False, Optional CustomMcFolder As String = Nothing, Optional JumpLoaderFolder As String = Nothing) As List(Of McLibToken)
        CustomMcFolder = If(CustomMcFolder, PathMcFolder)
        Dim BasicArray As New List(Of McLibToken)

        '添加基础 Json 项
        Dim AllLibs As JArray = JsonObject("libraries")
        '添加 JumpLoader Json 项
        If JsonObject("jumploader") IsNot Nothing AndAlso JsonObject("jumploader")("jars") IsNot Nothing AndAlso JsonObject("jumploader")("jars")("maven") IsNot Nothing Then
            For Each JumpLoaderToken In JsonObject("jumploader")("jars")("maven")
                AllLibs.Add(JumpLoaderToken)
            Next
        End If

        '转换为 LibToken
        For Each Library As JObject In AllLibs.Children

            '清理 null 项（BakaXL 会把没有的项序列化为 null，但会被 Newtonsoft 转换为 JValue，导致 Is Nothing = false；这导致了 #409）
            For i = Library.Properties.Count - 1 To 0 Step -1
                If Library.Properties(i).Value.Type = JTokenType.Null Then Library.Remove(Library.Properties(i).Name)
            Next

            '检查是否需要（Rules）
            If Not McJsonRuleCheck(Library("rules")) Then Continue For

            '检查 JumpLoader
            Dim IsJumpLoader As Boolean = False
            If Library("mavenPath") IsNot Nothing Then
                IsJumpLoader = True
                If Library("name") Is Nothing Then Library.Add("name", Library("mavenPath")) '这里的修改会导致原 Json 内容改变
                If Library("repoUrl") IsNot Nothing AndAlso Library("url") Is Nothing Then Library.Add("url", Library("repoUrl"))
            End If

            '获取根节点下的 url
            Dim RootUrl As String = Library("url")
            If RootUrl IsNot Nothing Then
                RootUrl += McLibGet(Library("name"), False, True, CustomMcFolder).Replace("\", "/")
            End If

            '根据是否本地化处理（Natives）
            If Library("natives") Is Nothing Then
                '没有 Natives
                Dim LocalPath As String
                If IsJumpLoader Then
                    LocalPath = McLibGet(Library("name"), CustomMcFolder:=If(JumpLoaderFolder, CustomMcFolder))
                Else
                    LocalPath = McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder)
                End If
                Try
                    If Library("downloads") IsNot Nothing AndAlso Library("downloads")("artifact") IsNot Nothing Then
                        BasicArray.Add(New McLibToken With {
                                                       .IsJumpLoader = IsJumpLoader,
                                                       .OriginalName = Library("name"),
                                                       .Url = If(RootUrl, Library("downloads")("artifact")("url")),
                                                       .LocalPath = If(Library("downloads")("artifact")("path") Is Nothing, McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder), CustomMcFolder & "libraries\" & Library("downloads")("artifact")("path").ToString.Replace("/", "\")),
                                                       .Size = Val(Library("downloads")("artifact")("size").ToString),
                                                       .IsNatives = False,
                                                       .SHA1 = Library("downloads")("artifact")("sha1")?.ToString})
                    Else
                        BasicArray.Add(New McLibToken With {.IsJumpLoader = IsJumpLoader, .OriginalName = Library("name"), .Url = RootUrl, .LocalPath = LocalPath, .Size = 0, .IsNatives = False, .SHA1 = Nothing})
                    End If
                Catch ex As Exception
                    Log(ex, "处理实际支持库列表失败（无 Natives，" & If(Library("name"), "Nothing").ToString & "）")
                    BasicArray.Add(New McLibToken With {.IsJumpLoader = IsJumpLoader, .OriginalName = Library("name"), .Url = RootUrl, .LocalPath = LocalPath, .Size = 0, .IsNatives = False, .SHA1 = Nothing})
                End Try
            Else
                '有 Natives
                If Library("natives")("windows") IsNot Nothing Then
                    Try
                        If Library("downloads") IsNot Nothing AndAlso Library("downloads")("classifiers") IsNot Nothing AndAlso Library("downloads")("classifiers")("natives-windows") IsNot Nothing Then
                            BasicArray.Add(New McLibToken With {
                                                       .IsJumpLoader = IsJumpLoader,
                                                       .OriginalName = Library("name"),
                                                       .Url = If(RootUrl, Library("downloads")("classifiers")("natives-windows")("url")),
                                                       .LocalPath = If(Library("downloads")("classifiers")("natives-windows")("path") Is Nothing,
                                                           McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder).Replace(".jar", "-" & Library("natives")("windows").ToString & ".jar").Replace("${arch}", If(Environment.Is64BitOperatingSystem, "64", "32")),
                                                           CustomMcFolder & "libraries\" & Library("downloads")("classifiers")("natives-windows")("path").ToString.Replace("/", "\")),
                                                       .Size = Val(Library("downloads")("classifiers")("natives-windows")("size").ToString),
                                                       .IsNatives = True,
                                                       .SHA1 = Library("downloads")("classifiers")("natives-windows")("sha1").ToString})
                        Else
                            BasicArray.Add(New McLibToken With {.IsJumpLoader = IsJumpLoader, .OriginalName = Library("name"), .Url = RootUrl, .LocalPath = McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder).Replace(".jar", "-" & Library("natives")("windows").ToString & ".jar").Replace("${arch}", If(Environment.Is64BitOperatingSystem, "64", "32")), .Size = 0, .IsNatives = True, .SHA1 = Nothing})
                        End If
                    Catch ex As Exception
                        Log(ex, "处理实际支持库列表失败（有 Natives，" & If(Library("name"), "Nothing").ToString & "）")
                        BasicArray.Add(New McLibToken With {.IsJumpLoader = IsJumpLoader, .OriginalName = Library("name"), .Url = RootUrl, .LocalPath = McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder).Replace(".jar", "-" & Library("natives")("windows").ToString & ".jar").Replace("${arch}", If(Environment.Is64BitOperatingSystem, "64", "32")), .Size = 0, .IsNatives = True, .SHA1 = Nothing})
                    End Try
                End If
            End If

        Next

        '去重
        Dim ResultArray As New Dictionary(Of String, McLibToken)
        For i = 0 To BasicArray.Count - 1
            Dim Key As String = BasicArray(i).Name & BasicArray(i).IsNatives.ToString & BasicArray(i).IsJumpLoader.ToString
            If ResultArray.ContainsKey(Key) Then
                If BasicArray(i).Version <> ResultArray(Key).Version AndAlso
                    (KeepSameNameDifferentVersionResult OrElse BasicArray(i).Version.Contains("natives-windows")) Then
                    'Contains("natives-windows") 源于 1.19-Pre1 开始，lwjgl-3.3.1-natives-windows 与 lwjgl-3.3.1-natives-windows-x86 重复
                    ResultArray.Add(Key & GetUuid(), BasicArray(i))
                ElseIf VersionSortBoolean(BasicArray(i).Version, ResultArray(Key).Version) Then
                    ResultArray(Key) = BasicArray(i)
                End If
            Else
                ResultArray.Add(Key, BasicArray(i))
            End If
        Next i
        Return ResultArray.Values.ToList
    End Function

    ''' <summary>
    ''' 获取版本缺失的支持库文件所对应的 NetTaskFile。
    ''' </summary>
    Public Function McLibFix(Version As McVersion, Optional CoreJarOnly As Boolean = False) As List(Of NetFile)
        If Not Version.IsLoaded Then Version.Load() '确保例如 JumpLoader 等项被合并入 Json
        Dim Result As New List(Of NetFile)

        '更新此方法时需要同步更新 Forge 新版自动安装方法！

        '主 Jar 文件
        Try
            Dim MainJar As NetFile = DlClientJarGet(Version, True)
            If MainJar IsNot Nothing Then Result.Add(MainJar)
        Catch ex As Exception
            Log(ex, "版本缺失主 jar 文件所必须的信息", LogLevel.Developer)
        End Try
        If CoreJarOnly Then Return Result

        '是否跳过校验
        Dim IsSetupSkip As Boolean = Setup.Get("LaunchAdvanceAssets")
        Select Case Setup.Get("VersionAdvanceAssets", Version:=Version)
            Case 0 '使用全局设置
            Case 1 '开启校验
                IsSetupSkip = False
            Case 2 '关闭校验
                IsSetupSkip = True
        End Select

        'Library 文件
        Result.AddRange(McLibFixFromLibToken(McLibListGet(Version, False), JumpLoaderFolder:=Version.PathIndie & ".jumploader\", AllowUnsameFile:=IsSetupSkip))

        '统一通行证文件
        If Setup.Get("VersionServerLogin", Version:=Version) = 3 Then
            Dim TargetFile = PathAppdata & "nide8auth.jar"
            If Not (IsSetupSkip AndAlso File.Exists(TargetFile)) Then
                Dim DownloadInfo As JObject = Nothing
                '获取下载信息
                Try
                    Log("[Minecraft] 开始获取统一通行证下载信息")
                    '测试链接：https://auth.mc-user.com:233/00000000000000000000000000000000/
                    DownloadInfo = GetJson(NetGetCodeByDownload({
                        "https://auth.mc-user.com:233/" & Setup.Get("VersionServerNide", Version:=Version)}, IsJson:=True))
                Catch ex As Exception
                    Log(ex, "获取统一通行证下载信息失败")
                End Try
                '校验文件
                If DownloadInfo IsNot Nothing Then
                    Dim Checker As New FileChecker(Hash:=DownloadInfo("jarHash").ToString)
                    If (IsSetupSkip AndAlso File.Exists(TargetFile)) OrElse Checker.Check(TargetFile) IsNot Nothing Then
                        '开始下载
                        Log("[Minecraft] 统一通行证需要更新：Hash - " & Checker.Hash, LogLevel.Developer)
                        Result.Add(New NetFile({"https://login.mc-user.com:233/index/jar"}, TargetFile, Checker))
                    End If
                End If
            End If
        End If
        'Authlib-Injector 文件
        If Setup.Get("VersionServerLogin", Version:=Version) = 4 OrElse
           (PageLinkHiper.HiperState = LoadState.Finished AndAlso Setup.Get("LoginType") = McLoginType.Legacy) Then 'HiPer 登录转接
            Dim TargetFile = PathAppdata & "authlib-injector.jar"
            If Not (IsSetupSkip AndAlso File.Exists(TargetFile)) Then
                Dim DownloadInfo As JObject = Nothing
                '获取下载信息
                Try
                    Log("[Minecraft] 开始获取 Authlib-Injector 下载信息")
                    DownloadInfo = GetJson(NetGetCodeByDownload({"https://download.mcbbs.net/mirrors/authlib-injector/artifact/latest.json", "https://bmclapi2.bangbang93.com/mirrors/authlib-injector/artifact/latest.json"}, IsJson:=True))
                Catch ex As Exception
                    Log(ex, "获取 Authlib-Injector 下载信息失败")
                End Try
                '校验文件
                If DownloadInfo IsNot Nothing Then
                    Dim Checker As New FileChecker(Hash:=DownloadInfo("checksums")("sha256").ToString)
                    If (IsSetupSkip AndAlso File.Exists(TargetFile)) OrElse Checker.Check(TargetFile) IsNot Nothing Then
                        '开始下载
                        Dim DownloadAddress As String = DownloadInfo("download_url")
                        Log("[Minecraft] Authlib-Injector 需要更新：" & DownloadAddress, LogLevel.Developer)
                        Result.Add(New NetFile({
                                DownloadAddress.Replace("bmclapi2.bangbang93.com", "download.mcbbs.net"), DownloadAddress
                            }, TargetFile, New FileChecker(Hash:=DownloadInfo("checksums")("sha256").ToString)))
                    End If
                End If
            End If
        End If

        Return Result
    End Function
    ''' <summary>
    ''' 将 McLibToken 列表转换为 NetFile。无需下载的文件会被自动过滤。
    ''' </summary>
    Public Function McLibFixFromLibToken(Libs As List(Of McLibToken), Optional CustomMcFolder As String = Nothing, Optional JumpLoaderFolder As String = Nothing, Optional AllowUnsameFile As Boolean = False) As List(Of NetFile)
        CustomMcFolder = If(CustomMcFolder, PathMcFolder)
        Dim Result As New List(Of NetFile)
        '获取
        For Each Token As McLibToken In Libs
            '检查文件
            Dim Checker As FileChecker
            If AllowUnsameFile Then '只要文件存在则通过检查，用于放宽完整性校验的情况
                Checker = New FileChecker(MinSize:=1)
            Else
                Checker = New FileChecker(ActualSize:=If(Token.Size = 0, -1, Token.Size), Hash:=Token.SHA1)
            End If
            If Checker.Check(Token.LocalPath) Is Nothing Then Continue For
            '文件不符合，添加下载
            Dim Urls As New List(Of String)
            If Token.Url IsNot Nothing Then
                '获取 Url 的真实地址
                Dim Url As String = Token.Url
                Url = Url.Replace("https://files.minecraftforge.net", "http://files.minecraftforge.net") '这个鬼东西不让 https
                Urls.Add(Url)
                If Token.Url.Contains("launcher.mojang.com/v1/objects") Then Urls = DlSourceLauncherOrMetaGet(Token.Url).ToList() 'Mappings
                If Token.Url.Contains("maven") Then Urls.Insert(0, Token.Url.Replace(Mid(Token.Url, 1, Token.Url.IndexOf("maven")), "https://download.mcbbs.net/").Replace("maven.fabricmc.net", "maven").Replace("maven.minecraftforge.net", "maven"))
            End If
            If Token.LocalPath.Contains("transformer-discovery-service") Then
                'Transformer 文件释放
                If Not File.Exists(Token.LocalPath) Then WriteFile(Token.LocalPath, GetResources("Transformer"))
                Log("[Download] 已自动释放 Transformer Discovery Service", LogLevel.Developer)
                Continue For
            ElseIf Token.LocalPath.Contains("optifine\OptiFine") Then
                'OptiFine 主 Jar
                Dim OptiFineBase As String = Token.LocalPath.Replace(If(Token.IsJumpLoader, JumpLoaderFolder, CustomMcFolder) & "libraries\optifine\OptiFine\", "").Split("_")(0) & "/" & GetFileNameFromPath(Token.LocalPath).Replace("-", "_")
                OptiFineBase = "/maven/com/optifine/" & OptiFineBase
                If OptiFineBase.Contains("_pre") Then OptiFineBase = OptiFineBase.Replace("com/optifine/", "com/optifine/preview_")
                Urls.Add("http://download.mcbbs.net" & OptiFineBase)
                Urls.Add("http://bmclapi2.bangbang93.com" & OptiFineBase)
            ElseIf Urls.Count <= 2 Then
                '普通文件
                Urls.AddRange(DlSourceLibraryGet("https://libraries.minecraft.net" & Token.LocalPath.Replace(If(Token.IsJumpLoader, JumpLoaderFolder, CustomMcFolder) & "libraries", "").Replace("\", "/")))
            End If
            Result.Add(New NetFile(Urls.Distinct.ToArray, Token.LocalPath, Checker))
        Next
        '去重并返回
        Return Distinct(Result, Function(a, b) a.LocalPath = b.LocalPath)
    End Function
    ''' <summary>
    ''' 获取对应的支持库文件地址。
    ''' </summary>
    ''' <param name="Original">原始地址，如 com.mumfrey:liteloader:1.12.2-SNAPSHOT。</param>
    ''' <param name="WithHead">是否包含 Lib 文件夹头部，若不包含，则会类似以 com\xxx\ 开头。</param>
    Public Function McLibGet(Original As String, Optional WithHead As Boolean = True, Optional IgnoreLiteLoader As Boolean = False, Optional CustomMcFolder As String = Nothing) As String
        CustomMcFolder = If(CustomMcFolder, PathMcFolder)
        Dim Splited = Original.Split(":")
        'If Original.ToLower.Contains("xray") OrElse Original.ToLower.Contains("rift") OrElse Original.ToLower.Contains("mixin") OrElse Original.Contains("net.minecraftforge:forge") OrElse Splited(2).Contains("beta") OrElse (IgnoreLiteLoader AndAlso Original.Contains("liteloader")) Then
        'ElseIf Splited(2).Contains("-") Then
        '    Log("[Launch] 可能存在分段问题的支持库：" & Original)
        'End If
        McLibGet = If(WithHead, CustomMcFolder & "libraries\", "") &
                   Splited(0).Replace(".", "\") & "\" & Splited(1) & "\" & Splited(2) & "\" & Splited(1) & "-" & Splited(2) & ".jar"
        '判断 OptiFine 是否应该使用 installer
        If McLibGet.Contains("optifine\OptiFine\1.12") AndAlso '仅在 1.12 OptiFine 可重现
           File.Exists(CustomMcFolder & "libraries\" & Splited(0).Replace(".", "\") & "\" & Splited(1) & "\" & Splited(2) & "\" & Splited(1) & "-" & Splited(2) & "-installer.jar") Then
            Log("[Launch] 已将 " & Original & " 特判替换为对应的 Installer 文件", LogLevel.Debug)
            McLibGet = McLibGet.Replace(".jar", "-installer.jar")
        End If
    End Function

#End Region

#Region "资源文件（Assets）"

    '获取索引
    ''' <summary>
    ''' 获取某版本资源文件索引的对应 Json 项，详见版本 Json 中的 assetIndex 项。失败会抛出异常。
    ''' </summary>
    Public Function McAssetsGetIndex(Version As McVersion, Optional ReturnLegacyOnError As Boolean = False, Optional CheckURLEmpty As Boolean = False) As JToken
        Dim AssetsName As String
        Try
            Do While True
                Dim Index As JToken = Version.JsonObject("assetIndex")
                If Index IsNot Nothing AndAlso Index("id") IsNot Nothing Then Return Index
                If Version.JsonObject("assets") IsNot Nothing Then AssetsName = Version.JsonObject("assets").ToString
                If CheckURLEmpty AndAlso Index("url") IsNot Nothing Then Return Index
                '下一个版本
                If Version.InheritVersion = "" Then Exit Do
                Version = New McVersion(PathMcFolder & "versions\" & Version.InheritVersion)
            Loop
        Catch ex As Exception
            Log(ex, "获取资源文件索引下载地址失败")
        End Try
        '无法获取到下载地址
        If ReturnLegacyOnError Then
            '返回 assets 文件名会由于没有下载地址导致全局失败
            'If AssetsName IsNot Nothing AndAlso AssetsName <> "legacy" Then
            '    Log("[Minecraft] 无法获取资源文件索引下载地址，使用 assets 项提供的资源文件名：" & AssetsName)
            '    Return GetJson("{""id"": """ & AssetsName & """}")
            'Else
            Log("[Minecraft] 无法获取资源文件索引下载地址，使用默认的 legacy 下载地址")
            Return GetJson("
                    {
                        ""id"": ""legacy"",
                        ""sha1"": ""c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729"",
                        ""size"": 134284,
                        ""url"": ""https://launchermeta.mojang.com/mc-staging/assets/legacy/c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729/legacy.json"",
                        ""totalSize"": 111220701
                    }")
            'End If
        Else
            Throw New Exception("该版本不存在资源文件索引信息")
        End If
    End Function
    ''' <summary>
    ''' 获取某版本资源文件索引名，优先使用 assetIndex，其次使用 assets。失败会返回 legacy。
    ''' </summary>
    Public Function McAssetsGetIndexName(Version As McVersion) As String
        Try
            Do While True
                If Version.JsonObject("assetIndex") IsNot Nothing AndAlso Version.JsonObject("assetIndex")("id") IsNot Nothing Then
                    Return Version.JsonObject("assetIndex")("id").ToString
                End If
                If Version.JsonObject("assets") IsNot Nothing Then
                    Return Version.JsonObject("assets").ToString
                End If
                If Version.InheritVersion = "" Then Exit Do
                Version = New McVersion(PathMcFolder & "versions\" & Version.InheritVersion)
            Loop
        Catch ex As Exception
            Log(ex, "获取资源文件索引名失败")
        End Try
        Return "legacy"
    End Function

    '获取列表
    Private Structure McAssetsToken
        ''' <summary>
        ''' 文件的完整本地路径。
        ''' </summary>
        Public LocalPath As String
        ''' <summary>
        ''' Json 中书写的源路径。例如 minecraft/sounds/mob/stray/death2.ogg 。
        ''' </summary>
        Public SourcePath As String
        ''' <summary>
        ''' 文件大小。若无有效数据即为 0。
        ''' </summary>
        Public Size As Long
        ''' <summary>
        ''' 是否为 Virtual 资源文件。
        ''' </summary>
        Public IsVirtual As Boolean
        ''' <summary>
        ''' 文件的 Hash 校验码。
        ''' </summary>
        Public Hash As String

        Public Overrides Function ToString() As String
            Return If(IsVirtual, "[Virtual] ", "") & GetString(Size) & " | " & LocalPath
        End Function
    End Structure
    ''' <summary>
    ''' 获取 Minecraft 的资源文件列表。失败会抛出异常。
    ''' </summary>
    ''' <param name="Name">版本的资源名称，如“1.13.1”。</param>
    Private Function McAssetsListGet(Name As String) As List(Of McAssetsToken)
        Try

            '初始化
            If Not File.Exists(PathMcFolder & "assets\indexes\" & Name & ".json") Then Throw New FileNotFoundException("Assets 索引文件未找到", PathMcFolder & "assets\indexes\" & Name & ".json")
            McAssetsListGet = New List(Of McAssetsToken)
            Dim Json = GetJson(ReadFile(PathMcFolder & "assets\indexes\" & Name & ".json"))

            '确认 Virtual 与 Map 状态
            Dim IsVirtual As Boolean = False
            If Json("virtual") IsNot Nothing Then IsVirtual = Json("virtual").ToString
            Dim IsMap As Boolean = False
            If Json("map_to_resources") IsNot Nothing Then IsMap = Json("map_to_resources").ToString

            '加载列表
            If IsVirtual OrElse IsMap Then
                For Each File As JProperty In Json("objects").Children
                    McAssetsListGet.Add(New McAssetsToken With {
                                        .IsVirtual = True,
                                        .LocalPath = PathMcFolder & "assets\virtual\legacy\" & File.Name.Replace("/", "\"),
                                        .SourcePath = File.Name,
                                        .Hash = File.Value("hash").ToString,
                                        .Size = File.Value("size").ToString
                                    })
                Next
            Else
                For Each File As JProperty In Json("objects").Children
                    McAssetsListGet.Add(New McAssetsToken With {
                                        .IsVirtual = False,
                                        .LocalPath = PathMcFolder & "assets\objects\" & Left(File.Value("hash").ToString, 2) & "\" & File.Value("hash").ToString,
                                        .SourcePath = File.Name,
                                        .Hash = File.Value("hash").ToString,
                                        .Size = File.Value("size").ToString
                                    })
                Next
            End If

        Catch ex As Exception
            Log(ex, "获取资源文件列表失败：" & Name)
            Throw
        End Try
    End Function

    '获取缺失列表
    ''' <summary>
    ''' 获取版本缺失的资源文件所对应的 NetTaskFile。
    ''' </summary>
    Public Function McAssetsFixList(IndexAddress As String, CheckHash As Boolean, Optional ByRef ProgressFeed As LoaderBase = Nothing) As List(Of NetFile)
        Dim Result As New List(Of NetFile)

        Dim AssetsList As List(Of McAssetsToken)
        Try
            AssetsList = McAssetsListGet(IndexAddress)
            Dim Token As McAssetsToken
            If ProgressFeed IsNot Nothing Then ProgressFeed.Progress = 0.04
            For i = 0 To AssetsList.Count - 1
                '初始化
                Token = AssetsList(i)
                If ProgressFeed IsNot Nothing Then ProgressFeed.Progress = 0.05 + 0.94 * i / AssetsList.Count
                '检查文件是否存在
                Dim File As New FileInfo(Token.LocalPath)
                If File.Exists AndAlso (Token.Size = 0 OrElse Token.Size = File.Length) AndAlso
                    (Not CheckHash OrElse Token.Hash Is Nothing OrElse Token.Hash = GetAuthSHA1(Token.LocalPath)) Then Continue For
                '文件不存在，添加下载
                Result.Add(New NetFile(DlSourceResourceGet("https://resources.download.minecraft.net/" & Left(Token.Hash, 2) & "/" & Token.Hash), Token.LocalPath, New FileChecker(ActualSize:=If(Token.Size = 0, -1, Token.Size), Hash:=Token.Hash)))
            Next
        Catch ex As Exception
            Log(ex, "获取版本缺失的资源文件下载列表失败")
        End Try
        If ProgressFeed IsNot Nothing Then ProgressFeed.Progress = 0.99

        Return Result
    End Function

#End Region

#Region "Mod"

    Public Class McMod

#Region "基础"

        ''' <summary>
        ''' Mod 文件的地址。
        ''' </summary>
        Public ReadOnly Path As String
        Public Sub New(Path As String)
            Me.Path = If(Path, "")
        End Sub

        ''' <summary>
        ''' Mod 的完整文件名。
        ''' </summary>
        Public ReadOnly Property FileName As String
            Get
                Return GetFileNameFromPath(Path)
            End Get
        End Property

        ''' <summary>
        ''' Mod 的状态。
        ''' </summary>
        Public ReadOnly Property State As McModState
            Get
                Load()
                If Not IsFileAvailable Then
                    Return McModState.Unavaliable
                ElseIf Path.EndsWith(".disabled") Then
                    Return McModState.Disabled
                Else
                    Return McModState.Fine
                End If
            End Get
        End Property
        Public Enum McModState As Integer
            Fine = 0
            Disabled = 1
            Unavaliable = 2
        End Enum

#End Region

#Region "信息项"

        ''' <summary>
        ''' Mod 的名称。若不可用则为 ModID 或无扩展的文件名。
        ''' </summary>
        Public Property Name As String
            Get
                If _Name Is Nothing Then Load()
                If _Name Is Nothing Then _Name = _ModId
                If _Name Is Nothing Then _Name = GetFileNameWithoutExtentionFromPath(Path)
                Return _Name
            End Get
            Set(value As String)
                If _Name Is Nothing AndAlso value IsNot Nothing AndAlso
                   value.ToLower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                    _Name = value
                End If
            End Set
        End Property
        Private _Name As String = Nothing

        ''' <summary>
        ''' Mod 的描述信息。
        ''' </summary>
        Public Property Description As String
            Get
                If _Description Is Nothing Then Load()
                If _Description Is Nothing AndAlso FileUnavailableReason IsNot Nothing Then _Description = FileUnavailableReason.Message
                'If _Description Is Nothing Then _Description = Path
                Return _Description
            End Get
            Set(value As String)
                If _Description Is Nothing AndAlso value IsNot Nothing AndAlso value.Count > 2 Then
                    _Description = value.ToString.Trim(vbLf)
                    '优化显示：若以 [a-zA-Z0-9] 结尾，加上小数点句号
                    If _Description.ToLower.LastIndexOfAny("qwertyuiopasdfghjklzxcvbnm0123456789") = _Description.Count - 1 Then _Description += "."
                End If
            End Set
        End Property
        Private _Description As String = Nothing

        ''' <summary>
        ''' Mod 的版本，不保证符合版本格式规范。
        ''' </summary>
        Public Property Version As String
            Get
                If _Version Is Nothing Then Load()
                Return _Version
            End Get
            Set(value As String)
                If _Version IsNot Nothing AndAlso (_Version.Contains(".") OrElse _Version.Contains("-")) Then Exit Property
                If value IsNot Nothing AndAlso value.ToLower.Contains("version") Then value = "version" '需要修改的标识
                _Version = value
            End Set
        End Property
        Private _Version As String = Nothing

        ''' <summary>
        ''' 用于依赖检查的 ModID。
        ''' </summary>
        Public Property ModId As String
            Get
                If _ModId Is Nothing Then Load()
                Return _ModId
            End Get
            Set(value As String)
                If value Is Nothing Then Exit Property
                value = RegexSeek(value.ToLower, "[0-9a-z_]+")
                If value IsNot Nothing AndAlso value.ToLower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                    If Not PossibleModId.Contains(value) Then PossibleModId.Add(value)
                    If _ModId Is Nothing Then _ModId = value
                End If
            End Set
        End Property
        Private _ModId As String = Nothing
        ''' <summary>
        ''' 其他可能的 ModID。
        ''' </summary>
        Public PossibleModId As New List(Of String)

        ''' <summary>
        ''' Mod 的主页。
        ''' </summary>
        Public Property Url As String
            Get
                If _Url Is Nothing Then Load()
                Return _Url
            End Get
            Set(value As String)
                If _Url Is Nothing AndAlso value IsNot Nothing AndAlso value.StartsWith("http") Then
                    _Url = value
                End If
            End Set
        End Property
        Private _Url As String = Nothing

        ''' <summary>
        ''' Mod 的作者列表。
        ''' </summary>
        Public Property Authors As String
            Get
                If _Authors Is Nothing Then Load()
                Return _Authors
            End Get
            Set(value As String)
                If _Authors Is Nothing AndAlso Not String.IsNullOrWhiteSpace(value) Then
                    _Authors = value
                End If
            End Set
        End Property
        Private _Authors As String = Nothing

        ''' <summary>
        ''' 依赖项，其中包括了 Minecraft 的版本要求。格式为 ModID - VersionRequirement，若无版本要求则为 Nothing。
        ''' </summary>
        Public ReadOnly Property Dependencies As Dictionary(Of String, String)
            Get
                Load()
                Return _Dependencies
            End Get
        End Property
        Private _Dependencies As New Dictionary(Of String, String)
        Private Sub AddDependency(ModID As String, Optional VersionRequirement As String = Nothing)
            '确保信息正确
            If ModID Is Nothing OrElse ModID.Count < 2 Then Exit Sub
            ModID = ModID.ToLower
            If ModID = "name" OrElse Val(ModID).ToString = ModID Then Exit Sub '跳过 name 与纯数字 id
            If VersionRequirement Is Nothing OrElse ((Not VersionRequirement.Contains(".")) AndAlso (Not VersionRequirement.Contains("-"))) OrElse VersionRequirement.Contains("$") Then
                VersionRequirement = Nothing
            Else
                If (Not VersionRequirement.StartsWith("[")) AndAlso (Not VersionRequirement.StartsWith("(")) AndAlso (Not VersionRequirement.EndsWith("]")) AndAlso (Not VersionRequirement.EndsWith(")")) Then VersionRequirement = "[" & VersionRequirement & ",)"
            End If
            '向依赖项中添加
            If _Dependencies.ContainsKey(ModID) Then
                If _Dependencies(ModID) Is Nothing Then _Dependencies(ModID) = VersionRequirement
            Else
                _Dependencies.Add(ModID, VersionRequirement)
            End If
        End Sub

#End Region

#Region "加载步骤标记"

        '1. 进行文件可用性检查
        '   成功：继续第二步。
        '   失败：标记 FileUnavailableReason， 并停止后续加载。
        ''' <summary>
        ''' 是否已进行 Mod 文件的基础加载。（这包括第一步和第二步）
        ''' </summary>
        Private IsLoaded As Boolean = False
        ''' <summary>
        ''' Mod 文件是否可被正常读取。
        ''' </summary>
        Public ReadOnly Property IsFileAvailable As Boolean
            Get
                Load()
                Return FileUnavailableReason Is Nothing
            End Get
        End Property
        ''' <summary>
        ''' Mod 文件出错的原因。若无错误，则为 Nothing。
        ''' </summary>
        Public ReadOnly Property FileUnavailableReason As Exception
            Get
                Load()
                Return _FileUnavailableReason
            End Get
        End Property
        Private _FileUnavailableReason As Exception = Nothing

        '2. 进行 .class 以外的信息获取
        '   成功：标记 IsInfoWithoutClassAvailable。
        '   失败：什么也不干。如果需要补充信息的话，检测到 IsInfoWithoutClassAvailable 为 False，会自动继续加载。
        ''' <summary>
        ''' 是否已在不获取 .class 文件的前提下完成了所需信息的加载。
        ''' </summary>
        Private IsInfoWithoutClassAvailable As Boolean = False

        '3. 尝试从 .class 文件中获取信息
        '   成功：标记 IsInfoWithClassAvailable。
        '   失败：什么也不干。
        ''' <summary>
        ''' 是否已进行 .class 文件的信息获取。
        ''' </summary>
        Private IsInfoWithClassLoaded As Boolean = False
        ''' <summary>
        ''' 是否已在 .class 文件中完成了所需信息的加载。
        ''' </summary>
        Private IsInfoWithClassAvailable As Boolean = False

#End Region

#Region "加载"

        ''' <summary>
        ''' 初始化所有数据。
        ''' </summary>
        Private Sub Init()
            _Name = Nothing
            _Description = Nothing
            _Version = Nothing
            _ModId = Nothing
            PossibleModId = New List(Of String)
            _Dependencies = New Dictionary(Of String, String)
            IsLoaded = False
            _FileUnavailableReason = Nothing
            IsInfoWithoutClassAvailable = False
            IsInfoWithClassLoaded = False
            IsInfoWithClassAvailable = False
        End Sub

        ''' <summary>
        ''' 进行文件可用性检查与 .class 以外的信息获取。
        ''' </summary>
        Public Sub Load(Optional ForceReload As Boolean = False)
            If IsLoaded AndAlso Not ForceReload Then Exit Sub
            '初始化
            Init()
            '阶段 1：基础可用性检查
            Dim Jar As ZipArchive
            Try
                '打开 Jar 文件
                If Path.Length < 2 Then Throw New FileNotFoundException("错误的 Mod 文件路径（" & If(Path, "null") & "）")
                If Not File.Exists(Path) Then Throw New FileNotFoundException("未找到 Mod 文件（" & Path & "）")
                Jar = New ZipArchive(New FileStream(Path, FileMode.Open))
                If Jar.Entries.Count = 0 Then Throw New FileFormatException("文件内容为空")
            Catch ex As UnauthorizedAccessException
                Log(ex, "Mod 文件由于无权限无法打开（" & Path & "）", LogLevel.Developer)
                _FileUnavailableReason = New UnauthorizedAccessException("没有读取此文件的权限，请尝试右键以管理员身份运行 PCL", ex)
                Jar = Nothing
            Catch ex As Exception
                Log(ex, "Mod 文件无法打开（" & Path & "）", LogLevel.Developer)
                _FileUnavailableReason = ex
                Jar = Nothing
            End Try
            '阶段 2：信息获取
            If Jar IsNot Nothing Then LoadWithoutClass(Jar)
            '阶段 3: Class 信息获取
            If Jar IsNot Nothing AndAlso Not IsInfoWithoutClassAvailable Then LoadWithClass(Jar)
            '释放文件
            Try
                If Jar IsNot Nothing Then Jar.Dispose()
            Catch
            End Try
            '完成标记
            IsLoaded = True
        End Sub

        ''' <summary>
        ''' 进行不使用 .class 文件的信息获取。
        ''' </summary>
        Private Sub LoadWithoutClass(Jar As ZipArchive)

#Region "尝试使用 mcmod.info"
            Try
                '获取信息文件
                Dim InfoEntry As ZipArchiveEntry = Jar.GetEntry("mcmod.info")
                Dim InfoString As String = Nothing
                If InfoEntry IsNot Nothing Then
                    InfoString = ReadFile(InfoEntry.Open())
                    If InfoString.Length < 15 Then InfoString = Nothing
                End If
                If InfoString Is Nothing Then Exit Try
                '获取可用 Json 项
                Dim InfoObject As JObject
                Dim JsonObject = GetJson(InfoString)
                If JsonObject.Type = JTokenType.Array Then
                    InfoObject = JsonObject(0)
                Else
                    InfoObject = JsonObject("modList")(0)
                End If
                '从文件中获取 Mod 信息项
                Name = InfoObject("name")
                Description = InfoObject("description")
                Version = InfoObject("version")
                Url = InfoObject("url")
                ModId = InfoObject("modid")
                Dim AuthorJson As JArray = InfoObject("authorList")
                If AuthorJson IsNot Nothing Then
                    Dim Author As New List(Of String)
                    For Each Token In AuthorJson
                        Author.Add(Token.ToString)
                    Next
                    If Author.Count > 0 Then Authors = Join(Author, ", ")
                End If
                Dim Reqs As JArray = InfoObject("requiredMods")
                If Reqs IsNot Nothing Then
                    For Each Token As String In Reqs
                        If Not String.IsNullOrEmpty(Token) Then
                            Token = Token.Substring(Token.IndexOf(":") + 1)
                            If Token.Contains("@") Then
                                AddDependency(Token.Split("@")(0), Token.Split("@")(1))
                            Else
                                AddDependency(Token)
                            End If
                        End If
                    Next
                End If
                Reqs = InfoObject("dependancies")
                If Reqs IsNot Nothing Then
                    For Each Token As String In Reqs
                        If Not String.IsNullOrEmpty(Token) Then
                            Token = Token.Substring(Token.IndexOf(":") + 1)
                            If Token.Contains("@") Then
                                AddDependency(Token.Split("@")(0), Token.Split("@")(1))
                            Else
                                AddDependency(Token)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                Log(ex, "读取 mcmod.info 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
#Region "尝试使用 mods.toml"
            Try
                '获取 mods.toml 文件
                Dim TomlEntry As ZipArchiveEntry = Jar.GetEntry("META-INF/mods.toml")
                Dim TomlText As String = Nothing
                If TomlEntry IsNot Nothing Then
                    TomlText = ReadFile(TomlEntry.Open())
                    If TomlText.Length < 15 Then TomlText = Nothing
                End If
                If TomlText Is Nothing Then Exit Try
                '文件标准化：统一换行符为 vbLf，去除注释、头尾的空格、空行
                Dim Lines As New List(Of String)
                For Each Line In TomlText.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(vbLf) '统一换行符
                    If Line.StartsWith("#") Then '去除注释
                        Continue For
                    ElseIf Line.Contains("#") Then
                        Line = Line.Substring(0, Line.IndexOf("#"))
                    End If
                    Line = Line.Trim(New Char() {" "c, "	"c, "　"c}) '去除头尾的空格
                    If Line.Count > 0 Then Lines.Add(Line) '去除空行
                Next
                '读取文件数据
                Dim TomlData As New List(Of KeyValuePair(Of String, Dictionary(Of String, Object))) From {New KeyValuePair(Of String, Dictionary(Of String, Object))("", New Dictionary(Of String, Object))}
                For i = 0 To Lines.Count - 1
                    Dim Line As String = Lines(i)
                    If Line.StartsWith("[[") AndAlso Line.EndsWith("]]") Then
                        '段落标记
                        Dim Header = Line.Replace("[", "").Replace("]", "")
                        TomlData.Add(New KeyValuePair(Of String, Dictionary(Of String, Object))(Header, New Dictionary(Of String, Object)))
                    ElseIf Line.Contains("=") Then
                        '字段标记
                        Dim Key As String = Line.Substring(0, Line.IndexOf("=")).TrimEnd(New Char() {" "c, "	"c, "　"c})
                        Dim RawValue As String = Line.Substring(Line.IndexOf("=") + 1).TrimStart(New Char() {" "c, "	"c, "　"c})
                        Dim Value As Object
                        If RawValue.StartsWith("""") AndAlso RawValue.EndsWith("""") Then
                            '单行字符串
                            Value = RawValue.Trim("""")
                        ElseIf RawValue.StartsWith("'''") Then
                            '多行字符串
                            Dim ValueLines As New List(Of String) From {RawValue.TrimStart("'")}
                            Do Until i >= Lines.Count - 1
                                i += 1
                                Dim ValueLine As String = Lines(i)
                                If ValueLine.EndsWith("'''") Then
                                    ValueLines.Add(ValueLine.TrimEnd("'"))
                                    Exit Do
                                Else
                                    ValueLines.Add(ValueLine)
                                End If
                            Loop
                            Value = Join(ValueLines, vbLf).Trim(vbLf).Replace(vbLf, vbCrLf)
                        ElseIf RawValue.ToLower = "true" OrElse RawValue.ToLower = "false" Then
                            '布尔型
                            Value = (RawValue.ToLower = "true")
                        ElseIf Val(RawValue).ToString = RawValue Then
                            '数字型
                            Value = Val(RawValue)
                        Else
                            '不知道是个啥玩意儿，直接存储
                            Value = RawValue
                        End If
                        TomlData.Last.Value(Key) = Value
                    Else
                        '不知道是个啥玩意儿
                        Exit Try
                    End If
                Next
                '从文件数据中获取信息
                Dim ModEntry As Dictionary(Of String, Object) = Nothing
                For Each TomlSubData In TomlData
                    If TomlSubData.Key = "mods" Then
                        ModEntry = TomlSubData.Value
                        Exit For
                    End If
                Next
                If ModEntry Is Nothing OrElse Not ModEntry.ContainsKey("modId") Then Exit Try
                ModId = ModEntry("modId")
                If _ModId Is Nothing Then Exit Try '设置了无效的 ModID
                If ModEntry.ContainsKey("displayName") Then Name = ModEntry("displayName")
                If ModEntry.ContainsKey("description") Then Description = ModEntry("description")
                If ModEntry.ContainsKey("version") Then Version = ModEntry("version")
                If TomlData(0).Value.ContainsKey("displayURL") Then Url = TomlData(0).Value("displayURL")
                If TomlData(0).Value.ContainsKey("authors") Then Authors = TomlData(0).Value("authors")
                For Each TomlSubData In TomlData
                    If TomlSubData.Key.StartsWith("dependencies") Then
                        Dim DepEntry As Dictionary(Of String, Object) = TomlSubData.Value
                        If DepEntry.ContainsKey("modId") AndAlso DepEntry.ContainsKey("mandatory") AndAlso DepEntry("mandatory") AndAlso
                           DepEntry.ContainsKey("side") AndAlso Not DepEntry("side").ToString.ToLower = "server" Then
                            AddDependency(DepEntry("modId"), If(DepEntry.ContainsKey("versionRange"), DepEntry("versionRange"), Nothing))
                        End If
                    End If
                Next
                '加载成功
                GoTo Finished
            Catch ex As Exception
                Log(ex, "读取 mods.toml 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
#Region "尝试使用 fabric.mod.json"
            Try
                '获取 fabric.mod.json 文件
                Dim FabricEntry As ZipArchiveEntry = Jar.GetEntry("fabric.mod.json")
                Dim FabricText As String = Nothing
                If FabricEntry IsNot Nothing Then
                    FabricText = ReadFile(FabricEntry.Open(), Encoding.UTF8)
                    If Not FabricText.Contains("schemaVersion") Then FabricText = Nothing
                End If
                If FabricText Is Nothing Then Exit Try
                Dim FabricObject As JObject = GetJson(FabricText)
GotFabric:
                '从文件中获取 Mod 信息项
                If FabricObject.ContainsKey("name") Then Name = FabricObject("name")
                If FabricObject.ContainsKey("version") Then Version = FabricObject("version")
                If FabricObject.ContainsKey("description") Then Description = FabricObject("description")
                If FabricObject.ContainsKey("id") Then ModId = FabricObject("id")
                If FabricObject.ContainsKey("contact") Then Url = If(FabricObject("contact")("homepage"), "")
                Dim AuthorJson As JArray = FabricObject("authors")
                If AuthorJson IsNot Nothing Then
                    Dim Author As New List(Of String)
                    For Each Token In AuthorJson
                        Author.Add(Token.ToString)
                    Next
                    If Author.Count > 0 Then Authors = Join(Author, ", ")
                End If
                'If (Not FabricObject.ContainsKey("serverSideOnly")) OrElse FabricObject("serverSideOnly")("value").ToObject(Of Boolean) = False Then
                '    '添加 Minecraft 依赖
                '    Dim DepMinecraft As String = If(If(FabricObject("acceptedMinecraftVersions") IsNot Nothing, FabricObject("acceptedMinecraftVersions")("value"), ""), "")
                '    If DepMinecraft <> "" Then AddDependency("minecraft", DepMinecraft)
                '    '添加其他依赖
                '    Dim Deps As String = If(If(FabricObject("dependencies") IsNot Nothing, FabricObject("dependencies")("value"), ""), "")
                '    If Deps <> "" Then
                '        For Each Dep In Deps.Split(";")
                '            If Dep = "" OrElse Not Dep.StartsWith("required-") Then Continue For
                '            Dep = Dep.Substring(Dep.IndexOf(":") + 1)
                '            If Dep.Contains("@") Then
                '                AddDependency(Dep.Split("@")(0), Dep.Split("@")(1))
                '            Else
                '                AddDependency(Dep)
                '            End If
                '        Next
                '    End If
                'End If
                '加载成功
                GoTo Finished
            Catch ex As Exception
                Log(ex, "读取 fabric.mod.json 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
#Region "尝试使用 fml_cache_annotation.json"
            Try
                '获取 fml_cache_annotation.json 文件
                Dim FmlEntry As ZipArchiveEntry = Jar.GetEntry("META-INF/fml_cache_annotation.json")
                Dim FmlText As String = Nothing
                If FmlEntry IsNot Nothing Then
                    FmlText = ReadFile(FmlEntry.Open(), Encoding.UTF8)
                    If Not FmlText.Contains("Lnet/minecraftforge/fml/common/Mod;") Then FmlText = Nothing
                End If
                If FmlText Is Nothing Then Exit Try
                Dim FmlJson As JObject = GetJson(FmlText)
                '获取可用 Json 项
                Dim FmlObject As JObject = Nothing
                For Each ModFilePair In FmlJson
                    Dim ModFileAnnos As JArray = ModFilePair.Value("annotations")
                    If ModFileAnnos IsNot Nothing Then
                        '先获取 Mod
                        For Each ModFileAnno In ModFileAnnos
                            Dim Name As String = If(ModFileAnno("name"), "")
                            If Name = "Lnet/minecraftforge/fml/common/Mod;" Then
                                FmlObject = ModFileAnno("values")
                                GoTo Got
                            End If
                        Next
                    End If
                Next
                Exit Try
Got:
                '从文件中获取 Mod 信息项
                If FmlObject.ContainsKey("useMetadata") AndAlso If(FmlObject("useMetadata")("value"), "").ToString.ToLower = "true" Then
                    '要求使用 mcmod.info 中的信息
                    Dim value As String = FmlObject("modid")("value")
                    If value Is Nothing Then Exit Try
                    value = RegexSeek(value.ToLower, "[0-9a-z_]+")
                    If value IsNot Nothing AndAlso value.ToLower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                        If Not PossibleModId.Contains(value) Then PossibleModId.Add(value)
                    End If
                    Exit Try
                End If
                If FmlObject.ContainsKey("name") Then Name = FmlObject("name")("value")
                If FmlObject.ContainsKey("version") Then Version = FmlObject("version")("value")
                If FmlObject.ContainsKey("modid") Then ModId = FmlObject("modid")("value")
                If (Not FmlObject.ContainsKey("serverSideOnly")) OrElse FmlObject("serverSideOnly")("value").ToObject(Of Boolean) = False Then
                    '添加 Minecraft 依赖
                    Dim DepMinecraft As String = If(If(FmlObject("acceptedMinecraftVersions") IsNot Nothing, FmlObject("acceptedMinecraftVersions")("value"), ""), "")
                    If DepMinecraft <> "" Then AddDependency("minecraft", DepMinecraft)
                    '添加其他依赖
                    Dim Deps As String = If(If(FmlObject("dependencies") IsNot Nothing, FmlObject("dependencies")("value"), ""), "")
                    If Deps <> "" Then
                        For Each Dep In Deps.Split(";")
                            If Dep = "" OrElse Not Dep.StartsWith("required-") Then Continue For
                            Dep = Dep.Substring(Dep.IndexOf(":") + 1)
                            If Dep.Contains("@") Then
                                AddDependency(Dep.Split("@")(0), Dep.Split("@")(1))
                            Else
                                AddDependency(Dep)
                            End If
                        Next
                    End If
                End If
            Catch ex As Exception
                Log(ex, "读取 fml_cache_annotation.json 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
Finished:
#Region "将 Version 代号转换为 META-INF 中的版本"
            If _Version = "version" Then
                Try
                    Dim MetaEntry As ZipArchiveEntry = Jar.GetEntry("META-INF/MANIFEST.MF")
                    If MetaEntry IsNot Nothing Then
                        Dim MetaString As String = ReadFile(MetaEntry.Open()).Replace(" :", ":").Replace(": ", ":")
                        If MetaString.Contains("Implementation-Version:") Then
                            MetaString = MetaString.Substring(MetaString.IndexOf("Implementation-Version:") + "Implementation-Version:".Count)
                            MetaString = MetaString.Substring(0, MetaString.IndexOfAny(vbCrLf.ToCharArray)).Trim
                            Version = MetaString
                        End If
                    End If
                Catch ex As Exception
                    Log("获取 META-INF 中的版本信息失败（" & Path & "）", LogLevel.Developer)
                    Version = Nothing
                End Try
            End If
            If _Version IsNot Nothing AndAlso Not (_Version.Contains(".") OrElse _Version.Contains("-")) Then Version = Nothing
#End Region

            IsInfoWithoutClassAvailable = _ModId IsNot Nothing AndAlso _Version IsNot Nothing
        End Sub

        ''' <summary>
        ''' 进行使用 .class 文件的信息获取。
        ''' </summary>
        Private Sub LoadWithClass(Jar As ZipArchive)
            Try
                '查找入口点文件
                Dim ModClass As String = Nothing
                Dim ModClassNotBest As String = Nothing '非完美匹配
                For Each Entry In Jar.Entries
                    If Entry.Name.EndsWith(".class") Then
                        Dim Temp As String = ReadFile(Entry.Open()).ToLower
                        If Temp.Contains("#lnet/minecraftforge/fml/common/mod;") Then
                            ModClass = Temp
                            Exit For
                        ElseIf Temp.Contains("modid") Then
                            ModClassNotBest = Temp
                        End If
                    End If
                Next
                If ModClass Is Nothing Then ModClass = ModClassNotBest
                If ModClass Is Nothing Then Throw New FileNotFoundException("未找到 Mod 入口点")
                ModClass = ModClass.Replace("ljava/lang/string;", "")
                If ModClass.Count > 3000 Then ModClass = ModClass.Substring(0, 3000) '如果文件过大，截取前 3000 Byte
                Dim IndexHead As Integer, IndexTail As Integer, IncreaseCount As Integer
                '获取 ModID
                If _ModId Is Nothing Then
                    IndexHead = ModClass.IndexOf("modid") + "modid".Length + 1
                    IncreaseCount = 0
                    Do While Convert.ToInt32(ModClass(IndexHead)) < 32
                        IndexHead += 1
                        IncreaseCount += 1
                        If IncreaseCount > 10 Then Throw New Exception("ModID 头匹配失败")
                    Loop
                    IndexTail = IndexHead + 1
                    IncreaseCount = 0
                    Do Until Convert.ToInt32(ModClass(IndexTail)) < 32
                        IndexTail += 1
                        IncreaseCount += 1
                        If IncreaseCount > 50 Then Throw New Exception("ModID 尾匹配失败")
                    Loop
                    ModId = ModClass.Substring(IndexHead, IndexTail - IndexHead)
                End If
                '获取 Version
                If _Version Is Nothing AndAlso ModClass.Contains("version") Then
                    IndexHead = ModClass.IndexOf("version") + "version".Length + 1
                    IncreaseCount = 0
                    Do While Convert.ToInt32(ModClass(IndexHead)) < 32
                        IndexHead += 1
                        IncreaseCount += 1
                        If IncreaseCount > 10 Then GoTo VersionFindFail
                    Loop
                    IndexTail = IndexHead + 1
                    IncreaseCount = 0
                    Do While ModClass(IndexTail) = "."c OrElse ModClass(IndexTail) = "-"c OrElse
                         (ModClass(IndexTail) >= "0"c AndAlso ModClass(IndexTail) <= "9"c) OrElse
                         (ModClass(IndexTail) >= "a"c AndAlso ModClass(IndexTail) <= "z"c)
                        IndexTail += 1
                        IncreaseCount += 1
                        If IncreaseCount > 50 Then GoTo VersionFindFail
                    Loop
                    _Version = ModClass.Substring(IndexHead, IndexTail - IndexHead)
                End If
VersionFindFail:
                '获取 Dependencies
                IndexHead = ModClass.IndexOf("dependencies")
                If IndexHead > 0 Then
                    If ModClass.Count >= IndexHead + 300 Then ModClass = ModClass.Substring(IndexHead, 299)
                    Dim Deps As List(Of String) = RegexSearch(ModClass, "(?<=required-((before|after|before-client|after-client)?):)[0-9a-z]+(@[\(\[]{1}[0-9.,]+[\)\]]{1})?")
                    For Each Token As String In Deps
                        If Not String.IsNullOrEmpty(Token) Then
                            If Token.Contains("@") Then
                                AddDependency(Token.Split("@")(0), Token.Split("@")(1))
                            Else
                                AddDependency(Token)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                Log(ex, "Mod Class 信息不可用（" & If(Path, "null") & "）", LogLevel.Normal)
            End Try
            IsInfoWithClassAvailable = _ModId IsNot Nothing AndAlso _Version IsNot Nothing
        End Sub

#End Region

        ''' <summary>
        ''' 是否可能为前置 Mod。
        ''' </summary>
        Public Function IsPresetMod() As Boolean
            Return Dependencies.Count = 0 AndAlso (Name IsNot Nothing) AndAlso (Name.ToLower.Contains("core") OrElse Name.ToLower.Contains("lib"))
        End Function

        ''' <summary>
        ''' 根据完整文件路径的文件扩展名判断是否为 Mod 文件。
        ''' </summary>
        Public Shared Function IsModFile(Path As String)
            If Path Is Nothing OrElse Not Path.Contains(".") Then Return False
            Path = Path.ToLower
            If Path.EndsWith(".jar") OrElse Path.EndsWith(".zip") OrElse Path.EndsWith(".litemod") OrElse
               Path.EndsWith(".jar.disabled") OrElse Path.EndsWith(".zip.disabled") OrElse Path.EndsWith(".litemod.disabled") Then Return True
            Return False
        End Function

    End Class

    '加载 Mod 列表
    Public McModLoader As New LoaderTask(Of String, List(Of McMod))("Mod List Loader", AddressOf McModLoad)
    Private Sub McModLoad(Loader As LoaderTask(Of String, List(Of McMod)))
        Try
            RunInUiWait(Sub() If FrmVersionMod IsNot Nothing Then FrmVersionMod.Load.ShowProgress = False)

            '获取 Mod 文件夹下的可用文件列表
            Dim ModFileList As New List(Of FileInfo)
            If Directory.Exists(Loader.Input) Then
                Dim RawName As String = Loader.Input.ToLower
                For Each File As FileInfo In EnumerateFiles(Loader.Input)
                    If File.DirectoryName.ToLower & "\" <> RawName Then
                        '仅当 Forge 1.13- 且文件夹名与版本号相同时，才加载该子文件夹下的 Mod
                        If Not (PageVersionLeft.Version IsNot Nothing AndAlso PageVersionLeft.Version.Version.HasForge AndAlso
                                PageVersionLeft.Version.Version.McCodeMain < 13 AndAlso
                                File.Directory.Name = "1." & PageVersionLeft.Version.Version.McCodeMain & "." & PageVersionLeft.Version.Version.McCodeSub) Then
                            Continue For
                        End If
                    End If
                    If McMod.IsModFile(File.FullName) Then ModFileList.Add(File)
                Next
            End If

            '确定是否显示进度
            Loader.Progress = 0.03
            If ModFileList.Count > 100 Then RunInUi(Sub() If FrmVersionMod IsNot Nothing Then FrmVersionMod.Load.ShowProgress = True)

            '加载为 Mod 列表
            Dim ModList As New List(Of McMod)
            For Each ModFile As FileInfo In ModFileList
                Dim ModEntity As New McMod(ModFile.FullName)
                ModEntity.Load()
                ModList.Add(ModEntity)
                Loader.Progress += 0.9 / ModFileList.Count
            Next
            Loader.Progress = 0.93

            '排序
            ModList = Sort(ModList, Function(Left As McMod, Right As McMod) As Boolean
                                        If (Left.State = McMod.McModState.Unavaliable) <> (Right.State = McMod.McModState.Unavaliable) Then
                                            Return Left.State = McMod.McModState.Unavaliable
                                        Else
                                            Return Left.FileName < Right.FileName
                                        End If
                                    End Function)
            Loader.Progress = 0.98

            '回设
            If Loader.IsAborted Then Exit Sub
            Loader.Output = ModList

        Catch ex As Exception
            Log(ex, "Mod 列表加载失败", LogLevel.Debug)
            Throw
        End Try
    End Sub

#If DEBUG Then
    ''' <summary>
    ''' 检查 Mod 列表中存在的错误，返回错误信息的集合。
    ''' </summary>
    Public Function McModCheck(Version As McVersion, Mods As List(Of McMod)) As List(Of String)
        Dim Result As New List(Of String)
        '令所有 Mod 进行基础检查，并归纳需要检查的 Mod
        Dim CurrentModList As New List(Of McMod)
        For Each ModEntity In Mods
            If Not ModEntity.IsFileAvailable Then
                Result.Add("无法读取的 Mod 文件。" & vbCrLf & " - " & ModEntity.Path)
                Continue For
            End If
            If ModEntity.State = McMod.McModState.Fine AndAlso ModEntity.ModId IsNot Nothing Then CurrentModList.Add(ModEntity)
        Next
        '添加默认依赖
        Dim CurrentDependencies As New Dictionary(Of String, String()) '{DependencyVersion, Path}
        If Version.State = McVersionState.Forge Then CurrentDependencies.Add("forge", {Version.Version.ForgeVersion, "Forge"})
        CurrentDependencies.Add("minecraft", {Version.Version.McName, "Minecraft"})
        '检查重复的 Mod，并添加对应的依赖
        For Each ModEntity In CurrentModList
            For Each PossibleModId In ModEntity.PossibleModId
                If CurrentDependencies.ContainsKey(PossibleModId) Then
                    If CurrentDependencies(PossibleModId)(2) = 1 Then
                        Result.Add("重复添加了相同的 Mod，请尝试删除其中一个（ModID：" & PossibleModId & "）。" & vbCrLf &
                                       " - " & ModEntity.FileName & vbCrLf &
                                       " - " & CurrentDependencies(PossibleModId)(1))
                    Else
                        Log("[Minecraft] 由于可能有多个 ModID，跳过疑似的重复项（ModID：" & PossibleModId & "）。" & vbCrLf &
                                       " - " & ModEntity.FileName & vbCrLf &
                                       " - " & CurrentDependencies(PossibleModId)(1), LogLevel.Developer)
                    End If
                Else
                    CurrentDependencies.Add(PossibleModId, {ModEntity.Version, ModEntity.FileName, ModEntity.PossibleModId.Count})
                End If
            Next
        Next
        '检查依赖
        For Each ModEntity In CurrentModList
            Try
                For Each Dependency In ModEntity.Dependencies
                    Dim ReqId As String = Dependency.Key
                    If ReqId.Count < 2 Then Continue For '确保正常
                    If ReqId = ModEntity.ModId Then Continue For '跳过自体引用
                    If ReqId = "forgemultipartcbe" Then Continue For '跳过莫名其妙的引用
                    If Dependency.Value IsNot Nothing Then
                        '获取分段后的详细版本信息
                        Dim ReqVersion As String = Dependency.Value
                        Dim ReqVersionHeadCanEqual As Boolean = ReqVersion.StartsWith("[")
                        Dim ReqVersionTailCanEqual As Boolean = ReqVersion.EndsWith("]")
                        Dim ReqVersionHead As String
                        Dim ReqVersionTail As String
                        If ReqVersion.Contains(",") Then
                            ReqVersionHead = ReqVersion.Split(",")(0).Trim("([ ".ToCharArray())
                            ReqVersionTail = ReqVersion.Split(",")(1).Trim("]) ".ToCharArray())
                        Else
                            ReqVersionHead = ReqVersion.Trim("([]) ".ToCharArray())
                            ReqVersionTail = ReqVersionHead
                            If ReqId = "minecraft" AndAlso ReqVersionHead.Split(".").Count = 2 Then
                                ReqVersionTail = ReqVersionHead.Split(".")(0) & "." & (Val(ReqVersionHead.Split(".")(1)) + 1)
                                ReqVersionTailCanEqual = False
                            End If
                        End If
                        If ReqVersionHead.StartsWith("1.") AndAlso ReqVersionHead.Contains("-") Then ReqVersionHead = ReqVersionHead.Substring(ReqVersionHead.LastIndexOf("-") + 1)
                        If ReqVersionTail.StartsWith("1.") AndAlso ReqVersionTail.Contains("-") Then ReqVersionTail = ReqVersionTail.Substring(ReqVersionTail.LastIndexOf("-") + 1)
                        '获取报错描述文本
                        Dim VersionRequire As String
                        If ReqVersionHead = ReqVersionTail Then
                            VersionRequire = "应为 " & ReqVersionHead
                        ElseIf ReqVersionHead.Contains(".") AndAlso ReqVersionTail.Contains(".") Then
                            VersionRequire = "应为 " & ReqVersionHead & " 至 " & ReqVersionTail
                        ElseIf ReqVersionHead.Contains(".") Then
                            If ReqVersionHeadCanEqual Then
                                VersionRequire = "最低应为 " & ReqVersionHead
                            Else
                                VersionRequire = "应高于 " & ReqVersionHead
                            End If
                        ElseIf ReqVersionTail.Contains(".") Then
                            If ReqVersionTailCanEqual Then
                                VersionRequire = "最高应为 " & ReqVersionHead
                            Else
                                VersionRequire = "应低于 " & ReqVersionHead
                            End If
                        Else
                            VersionRequire = ""
                        End If
                        '检查前置 Mod 是否存在，并获取其版本
                        If Not CurrentDependencies.ContainsKey(ReqId) Then
                            Result.Add("缺少前置 Mod：" & ReqId & If(VersionRequire = "", "", "，其版本" & VersionRequire) & "。" & vbCrLf & " - " & ModEntity.FileName)
                            Continue For
                        End If
                        Dim CurrentVersion As String = If(CurrentDependencies(ReqId)(0), "0.0")
                        If CurrentVersion.StartsWith("1.") AndAlso CurrentVersion.Contains("-") Then CurrentVersion = CurrentVersion.Substring(CurrentVersion.LastIndexOf("-") + 1)
                        '对比前置 Mod 头部版本
                        If ReqVersionHead.Contains(".") Then
                            If VersionSortInteger(ReqVersionHead, CurrentVersion) > If(ReqVersionHeadCanEqual, 0, -1) Then
                                Result.Add(ReqId.Substring(0, 1).ToUpper & ReqId.Substring(1) & " 版本过低，其版本" & VersionRequire & "，而当前版本为 " & CurrentVersion & "。" & vbCrLf &
                                           " - " & ModEntity.FileName & If(ReqId <> "minecraft" AndAlso ReqId <> "forge", vbCrLf & " - 前置：" & CurrentDependencies(ReqId)(1), ""))
                                Continue For
                            End If
                        End If
                        '对比前置 Mod 尾部版本
                        If ReqVersionTail.Contains(".") Then
                            If VersionSortInteger(CurrentVersion, ReqVersionTail) > If(ReqVersionTailCanEqual, 0, -1) Then
                                Result.Add(ReqId.Substring(0, 1).ToUpper & ReqId.Substring(1) & " 版本过高，其版本" & VersionRequire & "，而当前版本为 " & CurrentVersion & "。" & vbCrLf &
                                           " - " & ModEntity.FileName & If(ReqId <> "minecraft" AndAlso ReqId <> "forge", vbCrLf & " - 前置：" & CurrentDependencies(ReqId)(1), ""))
                                Continue For
                            End If
                        End If
                    Else
                        If Not CurrentDependencies.ContainsKey(Dependency.Key) Then
                            Result.Add("缺少前置 Mod：" & Dependency.Key & "。" & vbCrLf & " - " & ModEntity.FileName)
                            Continue For
                        End If
                    End If
                Next
            Catch ex As Exception
                Result.Add("检查 Mod 时出错：" & GetExceptionSummary(ex) & vbCrLf & " - " & ModEntity.FileName)
                Log(ex, "检查 Mod 时出错")
            End Try
        Next
        If Result.Count = 0 Then
            Log("[Minecraft] Mod 检查未发现异常")
        Else
            Log("[Minecraft] Mod 检查异常结果：" & vbCrLf & Join(Result, vbCrLf))
        End If
        Return Result
    End Function
#End If

#End Region

    ''' <summary>
    ''' 发送 Minecraft 更新提示。
    ''' </summary>
    Public Sub McDownloadClientUpdateHint(VersionName As String, Json As JObject)
        Try

            '获取对应版本
            Dim Version As JToken = Nothing
            For Each Token In Json("versions")
                If Token("id") IsNot Nothing AndAlso Token("id").ToString = VersionName Then
                    Version = Token
                    Exit For
                End If
            Next
            '进行提示
            If Version Is Nothing Then Exit Sub
            Dim Time As Date = Version("releaseTime")
            Dim MsgBoxText As String = $"新版本：{VersionName}{vbCrLf}" &
                If((Date.Now - Time).TotalDays > 1, "更新时间：" & Time.ToString, "更新于：" & GetTimeSpanString(Time - Date.Now, False))
            Dim MsgResult = MyMsgBox(MsgBoxText, "Minecraft 更新提示", "确定", "下载", If((Date.Now - Time).TotalHours > 3, "更新日志", ""),
                Button3Action:=Sub() McUpdateLogShow(Version))
            '弹窗结果
            If MsgResult = 2 Then
                '下载
                McDownloadClient(NetPreDownloadBehaviour.HintWhileExists, VersionName, Version("url").ToString)
            End If

        Catch ex As Exception
            Log(ex, "Minecraft 更新提示发送失败（" & If(VersionName, "Nothing") & "）", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 比较两个版本名的排序，若 Left 较新或相同则返回 True（Left >= Right）。无法比较两个 Pre 的大小。
    ''' 支持的格式范例：未知版本, 1.13.2, 1.7.10-pre4, 1.8_pre, 1.14 Pre-Release 2, 1.14.4 C6
    ''' </summary>
    Public Function VersionSortBoolean(Left As String, Right As String) As Boolean
        Return VersionSortInteger(Left, Right) >= 0
    End Function
    ''' <summary>
    ''' 比较两个版本名的排序，若 Left 较新则返回 1，相同则返回 0，Right 较新则返回 -1。
    ''' 支持的格式范例：未知版本, 1.13.2, 1.7.10-pre4, 1.8_pre, 1.14 Pre-Release 2, 1.14.4 C6
    ''' </summary>
    Public Function VersionSortInteger(Left As String, Right As String) As Integer
        If Left = "未知版本" OrElse Right = "未知版本" Then
            If Left = "未知版本" AndAlso Right <> "未知版本" Then Return 1
            If Left = "未知版本" AndAlso Right = "未知版本" Then Return 0
            If Left <> "未知版本" AndAlso Right = "未知版本" Then Return -1
        End If
        Dim Lefts = RegexSearch(Left.ToLower.Replace("快照", "snapshot"), "[a-z]+|[0-9]+")
        Dim Rights = RegexSearch(Right.ToLower.Replace("快照", "snapshot"), "[a-z]+|[0-9]+")
        Dim i As Integer = 0
        While True
            '两边均缺失，感觉是一个东西
            If Lefts.Count - 1 < i AndAlso Rights.Count - 1 < i Then
                If Left > Right Then
                    Return 1
                ElseIf Left < Right Then
                    Return -1
                Else
                    Return 0
                End If
            End If
            '确定两边的数值
            Dim LeftValue As String = If(Lefts.Count - 1 < i, "-1", Lefts(i))
            Dim RightValue As String = If(Rights.Count - 1 < i, "-1", Rights(i))
            If LeftValue = RightValue Then GoTo NextEntry
            If LeftValue = "pre" OrElse LeftValue = "snapshot" Then LeftValue = "-3"
            If LeftValue = "rc" Then LeftValue = "-2"
            If LeftValue = "experimental" Then LeftValue = "-4"
            Dim LeftValValue = Val(LeftValue)
            If RightValue = "pre" OrElse RightValue = "snapshot" Then RightValue = "-3"
            If RightValue = "rc" Then RightValue = "-2"
            If RightValue = "experimental" Then RightValue = "-4"
            Dim RightValValue = Val(RightValue)
            If LeftValValue = 0 AndAlso RightValValue = 0 Then
                '如果没有数值则直接比较字符串
                If LeftValue > RightValue Then
                    Return 1
                ElseIf LeftValue < RightValue Then
                    Return -1
                End If
            Else
                '如果有数值则比较数值
                '这会使得一边是数字一边是字母时数字方更大
                If LeftValValue > RightValValue Then
                    Return 1
                ElseIf LeftValValue < RightValValue Then
                    Return -1
                End If
            End If
NextEntry:
            i += 1
        End While
        Return 0
    End Function
    ''' <summary>
    ''' 比较两个版本名的排序器。
    ''' </summary>
    Public Class VersionSorter
        Implements IComparer(Of String)
        Public IsDecreased As Boolean = True
        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare
            Return VersionSortInteger(x, y) * If(IsDecreased, -1, 1)
        End Function
        Public Sub New(Optional IsDecreased As Boolean = True)
            Me.IsDecreased = IsDecreased
        End Sub
    End Class

    ''' <summary>
    ''' 为邮箱地址或手机号账号进行部分打码。
    ''' </summary>
    Public Function AccountFilter(Account As String) As String
        If Account.Contains("@") Then
            '是邮箱
            Dim Splits = Account.Split("@")
            'If Splits(0).Count >= 6 Then
            '    '前半部分至少 6 位，屏蔽后 4 位
            '    Return Mid(Splits(0), 1, Splits(0).Count - 4) & "****" & "@" & Splits(1)
            'Else
            '前半部分不到 6 位，返回全 *
            Return "".PadLeft(Splits(0).Count, "*") & "@" & Splits(1)
            'End If
        ElseIf Account.Count >= 6 Then
            '至少 6 位，屏蔽后 4 位
            Return Mid(Account, 1, Account.Count - 4) & "****"
        Else
            '不到 6 位，返回全 *
            Return "".PadLeft(Account.Count, "*")
        End If
    End Function

End Module
