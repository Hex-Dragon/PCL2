Imports System.IO.Compression

Public Module ModMinecraft

#Region "Java"

    '列表初始化

    Public JavaListCacheVersion As Integer = 1
    ''' <summary>
    ''' 目前所有可用的 Java。
    ''' </summary>
    Public JavaList As List(Of JavaEntry)
    ''' <summary>
    ''' 初始化 Java 列表，但除非没有 Java，否则不进行检查。
    ''' </summary>
    Public Sub JavaListInit()
        JavaList = New List(Of JavaEntry)
        Try
            If Setup.Get("CacheJavaListVersion") < JavaListCacheVersion Then
                '不使用缓存
                Log("[Java] 要求 Java 列表缓存更新")
                Setup.Set("CacheJavaListVersion", JavaListCacheVersion)
            Else
                '使用缓存
                For Each JsonEntry In GetJson(Setup.Get("LaunchArgumentJavaAll"))
                    JavaList.Add(JavaEntry.FromJson(JsonEntry))
                Next
            End If
            If JavaList.Count = 0 Then
                Log("[Java] 初始化未找到可用的 Java，将自动触发搜索", LogLevel.Developer)
                JavaSearchLoader.Start(0)
            Else
                Log("[Java] 缓存中有 " & JavaList.Count & " 个可用的 Java")
            End If
        Catch ex As Exception
            Log(ex, "初始化 Java 列表失败", LogLevel.Feedback)
            Setup.Set("LaunchArgumentJavaAll", "[]")
        End Try
    End Sub

    '基础环境

    ''' <summary>
    ''' Path 环境变量。
    ''' </summary>
    Private ReadOnly Property PathEnv As String
        Get
            If _PathEnv Is Nothing Then _PathEnv = Environment.GetEnvironmentVariable("Path")
            Return _PathEnv
        End Get
    End Property
    Private _PathEnv As String = Nothing

    Public Class JavaEntry

        '路径
        ''' <summary>
        ''' Java.exe 文件的完整路径。
        ''' </summary>
        Public ReadOnly Property PathJava As String
            Get
                Return PathFolder & "java.exe"
            End Get
        End Property
        ''' <summary>
        ''' Javaw.exe 文件的完整路径。
        ''' </summary>
        Public ReadOnly Property PathJavaw As String
            Get
                Return PathFolder & "javaw.exe"
            End Get
        End Property
        ''' <summary>
        ''' Javaw.exe 文件所在文件夹的路径，以 \ 结尾且只含有 \。
        ''' </summary>
        Public PathFolder As String
        ''' <summary>
        ''' 是否为用户手动导入的 Java。
        ''' </summary>
        Public IsUserImport As Boolean

        '版本信息
        ''' <summary>
        ''' Java 的详细版本。若不足 4 位会在前方补 1，例如 1.16.0.1。
        ''' 其大版本号为 Minor。
        ''' </summary>
        Public Version As Version
        ''' <summary>
        ''' Java 的大版本号。
        ''' </summary>
        Public ReadOnly Property VersionCode As Integer
            Get
                Return Version.Minor
            End Get
        End Property
        ''' <summary>
        ''' 是否为 Java Runtime Environment。
        ''' </summary>
        Public IsJre As Boolean
        ''' <summary>
        ''' 是否为 64 位 Java。
        ''' </summary>
        Public Is64Bit As Boolean
        ''' <summary>
        ''' 是否已设置环境变量。
        ''' </summary>
        Public ReadOnly Property HasEnvironment As Boolean
            Get
                If PathFolder Is Nothing OrElse PathEnv Is Nothing Then Return False
                Return PathEnv.Replace("\", "").Replace("/", "").ToLower.Contains(PathFolder.Replace("\", "").ToLower)
            End Get
        End Property

        '序列化
        Public Function ToJson() As JObject
            Return New JObject({New JProperty("Path", PathFolder), New JProperty("VersionString", Version.ToString), New JProperty("IsJre", IsJre), New JProperty("Is64Bit", Is64Bit), New JProperty("IsUserImport", IsUserImport)})
        End Function
        Public Shared Function FromJson(Data As JObject) As JavaEntry
            Return New JavaEntry(Data("Path"), Data("IsUserImport")) With {.Version = New Version(Data("VersionString")), .IsJre = Data("IsJre"), .Is64Bit = Data("Is64Bit")}
        End Function
        ''' <summary>
        ''' 转化为用户友好的字符串输出。
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim VersionString = Version.ToString
            If VersionString.StartsWith("1.") Then VersionString = Mid(VersionString, 3)
            Return If(IsJre, "Java ", "JDK ") & VersionCode & " (" & VersionString & ")，" & If(Is64Bit, "64", "32") & " 位" & If(IsUserImport, "，手动导入", "") & "：" & PathFolder
        End Function

        '构造
        ''' <summary>
        ''' 输入 javaw.exe 文件所在文件夹的路径，不限制结尾。
        ''' </summary>
        Public Sub New(Folder As String, IsUserImport As Boolean)
            If Not Folder.EndsWith("\") Then Folder += "\"
            PathFolder = Folder.Replace("/", "\")
            Me.IsUserImport = IsUserImport
        End Sub

        '方法
        Private IsChecked As Boolean = False
        ''' <summary>
        ''' 检查并获取 Java 详细信息。在 Java 存在异常时抛出错误。
        ''' </summary>
        Public Sub Check()
            If IsChecked Then Exit Sub
            Dim Output As String = Nothing
            Try
                '确定文件存在
                If Not File.Exists(PathJavaw) Then
                    Throw New FileNotFoundException("未找到 javaw.exe 文件", PathJavaw)
                End If
                If Not File.Exists(PathFolder & "java.exe") Then
                    Throw New FileNotFoundException("未找到 java.exe 文件", PathFolder & "java.exe")
                End If
                IsJre = Not File.Exists(PathFolder & "javac.exe")
                '运行 -version
                Output = ShellAndGetOutput(PathFolder & "java.exe", "-version", 8000).ToLower
                If ModeDebug Then Log("[Java] Java 检查输出：" & PathFolder & "java.exe" & vbCrLf & Output)
                '获取详细信息
                Dim VersionString = If(RegexSeek(Output, "(?<=version "")[^""]+"), If(RegexSeek(Output, "(?<=openjdk )[0-9]+"), "")).Replace("_", ".").Split("-").First
                Do While VersionString.Split(".").Count < 4
                    If VersionString.StartsWith("1.") Then
                        VersionString = VersionString & ".0"
                    Else
                        VersionString = "1." & VersionString
                    End If
                Loop
                Version = New Version(VersionString)
                If Version.Minor = 0 Then
                    Log("[Java] 疑似 X.0.X.X 格式版本号：" & Version.ToString)
                    Version = New Version(1, Version.Major, Version.Build, Version.Revision)
                End If
                Is64Bit = Output.Contains("64-bit")
                If Version.Minor <= 4 OrElse Version.Minor >= 25 Then Throw New Exception("分析详细信息失败，获取的版本为 " & Version.ToString)
            Catch ex As Exception
                Log("[Java] 检查失败的 Java 输出：" & PathFolder & "java.exe" & vbCrLf & If(Output, "无程序输出"))
                ex = New Exception("检查 Java 详细信息失败（" & If(PathJavaw, "Nothing") & "）", ex)
                Throw ex
            End Try
            IsChecked = True
        End Sub

    End Class

    '搜索

    ''' <summary>
    ''' 模糊搜索并获取所有可用的 Java，并在结束后更新设置页面显示。输出将直接写入 JavaList。
    ''' </summary>
    Public JavaSearchLoader As New LoaderTask(Of Integer, Integer)("Java Search Loader", AddressOf JavaSearchLoaderSub)
    Private Sub JavaSearchLoaderSub(Loader As LoaderTask(Of Integer, Integer))
        If FrmSetupLaunch IsNot Nothing Then
            RunInUiWait(Sub()
                            FrmSetupLaunch.ComboArgumentJava.Items.Clear()
                            FrmSetupLaunch.ComboArgumentJava.Items.Add(New ComboBoxItem With {.Content = "加载中……", .IsSelected = True})
                        End Sub)
        End If
        If FrmVersionSetup IsNot Nothing Then
            RunInUiWait(Sub()
                            FrmVersionSetup.ComboArgumentJava.Items.Clear()
                            FrmVersionSetup.ComboArgumentJava.Items.Add(New ComboBoxItem With {.Content = "加载中……", .IsSelected = True})
                        End Sub)
        End If

        Try

            '可能包含 Java 的文件夹列表，以 “\” 结尾，且仅包含 “\”
            'Key：文件夹地址
            'Value: 是否为玩家手动导入
            Dim JavaPreList As New Dictionary(Of String, Boolean)

#Region "模糊查找可能可用的 Java"

            '查找环境变量中的 Java
            For Each PathInEnv As String In Split(PathEnv.Replace("\\", "\").Replace("/", "\"), ";")
                PathInEnv = PathInEnv.Trim(" """.ToCharArray())
                If Not PathInEnv.EndsWith("\") Then PathInEnv += "\"
                '粗略检查有效性
                If File.Exists(PathInEnv & "javaw.exe") Then DictionaryAdd(JavaPreList, PathInEnv, False)
            Next
            '查找磁盘中的 Java
            For Each Disk As DriveInfo In DriveInfo.GetDrives()
                JavaSearchFolder(Disk.Name, JavaPreList, False)
            Next
            '查找 APPDATA 文件夹中的 Java
            JavaSearchFolder(PathAppdata, JavaPreList, False)
            '查找启动器目录中的 Java
            JavaSearchFolder(Path, JavaPreList, False, IsFullSearch:=True)
            '查找所选 Minecraft 文件夹中的 Java
            If Not String.IsNullOrWhiteSpace(PathMcFolder) AndAlso Path <> PathMcFolder Then
                JavaSearchFolder(PathMcFolder, JavaPreList, False, IsFullSearch:=True)
            End If

            '若[不全]为符号链接，则清除符号链接的地址
            Dim JavaWithoutReparse As New Dictionary(Of String, Boolean)
            For Each Pair In JavaPreList
                Dim Folder As String = Pair.Key.Replace("\\", "\").Replace("/", "\")
                Dim Info As FileSystemInfo = New FileInfo(Folder & "javaw.exe")
                Do
                    If Info.Attributes.HasFlag(FileAttributes.ReparsePoint) Then
                        Log("[Java] 位于 " & Folder & " 的 Java 包含符号链接")
                        Continue For
                    End If
                    Info = If(TypeOf Info Is FileInfo, CType(Info, FileInfo).Directory, CType(Info, DirectoryInfo).Parent)
                Loop While Info IsNot Nothing
                Log("[Java] 位于 " & Folder & " 的 Java 不含符号链接")
                JavaWithoutReparse.Add(Pair.Key, Pair.Value)
            Next
            If JavaWithoutReparse.Count > 0 Then JavaPreList = JavaWithoutReparse

            '若不全为 javapath_target，则清除二重引用的地址
            Dim JavaWithoutInherit As New Dictionary(Of String, Boolean)
            For Each Pair In JavaPreList
                If Pair.Key.Contains("javapath_target_") Then
                    Log("[Java] 位于 " & Pair.Key & " 的 Java 包含二重引用")
                    Continue For
                End If
                Log("[Java] 位于 " & Pair.Key & " 的 Java 不含二重引用")
                JavaWithoutInherit.Add(Pair.Key, Pair.Value)
            Next
            If JavaWithoutInherit.Count > 0 Then JavaPreList = JavaWithoutInherit

#End Region

#Region "添加玩家手动导入的 Java"

            Dim ImportedJava As String = Setup.Get("LaunchArgumentJavaAll")
            Try
                For Each JavaJsonObject In GetJson(ImportedJava)
                    Dim Entry = JavaEntry.FromJson(JavaJsonObject)
                    If Entry.IsUserImport Then DictionaryAdd(JavaPreList, Entry.PathFolder, True)
                Next
            Catch ex As Exception
                Log(ex, "Java 列表已损坏", LogLevel.Feedback)
                Setup.Set("LaunchArgumentJavaAll", "[]")
            End Try

#End Region

            '确保可用并获取详细信息，转入正式列表
            Dim NewJavaList As New List(Of JavaEntry)
            For Each Entry In JavaPreList
                NewJavaList.Add(New JavaEntry(Entry.Key, Entry.Value))
            Next
            NewJavaList = Sort(JavaCheckList(NewJavaList), AddressOf JavaSorter)

            '修改设置项
            Dim AllList As New JArray
            For Each Java In NewJavaList
                AllList.Add(Java.ToJson)
            Next
            Setup.Set("LaunchArgumentJavaAll", AllList.ToString(Newtonsoft.Json.Formatting.None))
            JavaList = NewJavaList

        Catch ex As Exception
            Log(ex, "搜索 Java 时出错", LogLevel.Feedback)
            JavaList = New List(Of JavaEntry)
        End Try

        Log("[Java] Java 搜索完成，发现 " & JavaList.Count & " 个 Java")
        If FrmSetupLaunch IsNot Nothing Then RunInUi(Sub() FrmSetupLaunch.RefreshJavaComboBox())
        If FrmVersionSetup IsNot Nothing Then RunInUi(Sub() FrmVersionSetup.RefreshJavaComboBox())
    End Sub

    ''' <summary>
    ''' 多线程检查列表中的所有 Java 项。
    ''' </summary>
    Private Function JavaCheckList(JavaEntries As List(Of JavaEntry)) As List(Of JavaEntry)
        Log("[Java] 开始确认列表 Java 状态，共 " & JavaEntries.Count & " 项")
        JavaCheckList = New List(Of JavaEntry)
        Dim ListLock As New Object

        '启动检查线程
        Dim CheckThreads As New List(Of Thread)
        For Each Entry In JavaEntries
            Dim CheckThread As New Thread(Sub()
                                              Try
                                                  Entry.Check()
                                                  Log("[Java] " & Entry.ToString)
                                                  SyncLock ListLock
                                                      JavaCheckList.Add(Entry)
                                                  End SyncLock
                                              Catch ex As Exception
                                                  If Entry.IsUserImport Then
                                                      Log(ex, "位于 " & Entry.PathFolder & " 的 Java 存在异常，将被自动移除", LogLevel.Hint)
                                                  Else
                                                      Log(ex, "位于 " & Entry.PathFolder & " 的 Java 存在异常")
                                                  End If
                                              End Try
                                          End Sub)
            CheckThreads.Add(CheckThread)
            CheckThread.Start()
        Next

        '等待构造线程完成
Wait:
        Thread.Sleep(10)
        For Each CheckThread In CheckThreads
            If CheckThread.IsAlive Then GoTo Wait
        Next
    End Function
    ''' <summary>
    ''' 模糊搜索指定文件夹下的 Java，并只进行粗略的检查。这不会搜索全部路径。
    ''' </summary>
    ''' <param name="OriginalPath">开始搜索的起始路径，不限制结尾。</param>
    ''' <param name="IsFullSearch">搜索当前文件夹下的全部文件夹（此参数不会传递到子文件夹）。</param>
    Private Sub JavaSearchFolder(OriginalPath As String, ByRef Results As Dictionary(Of String, Boolean), Source As Boolean, Optional IsFullSearch As Boolean = False)
        Try
            Log("[Java] 开始" & If(IsFullSearch, "完全", "部分") & "遍历查找：" & OriginalPath)
            JavaSearchFolder(New DirectoryInfo(OriginalPath), Results, Source, IsFullSearch)
        Catch ex As UnauthorizedAccessException
            Log("[Java] 遍历查找 Java 时遭遇无权限的文件夹：" & OriginalPath)
        Catch ex As Exception
            Log(ex, "遍历查找 Java 时出错（" & OriginalPath & "）")
        End Try
    End Sub
    ''' <summary>
    ''' 模糊搜索指定文件夹下的 Java，并只进行粗略的检查。这不会搜索全部路径。
    ''' </summary>
    ''' <param name="OriginalPath">开始搜索的起始路径，不限制结尾。</param>
    ''' <param name="IsFullSearch">搜索当前文件夹下的全部文件夹（此参数不会传递到子文件夹）。</param>
    Private Sub JavaSearchFolder(OriginalPath As DirectoryInfo, ByRef Results As Dictionary(Of String, Boolean), Source As Boolean, Optional IsFullSearch As Boolean = False)
        Try
            '确认目录存在
            If Not OriginalPath.Exists Then Exit Sub
            Dim Path As String = OriginalPath.FullName.Replace("\\", "\")
            If Not Path.EndsWith("\") Then Path += "\"
            '若该目录有 Java，则加入结果
            If File.Exists(Path & "javaw.exe") Then DictionaryAdd(Results, Path, Source)
            '查找其下的所有文件夹
            For Each FolderInfo As DirectoryInfo In OriginalPath.EnumerateDirectories
                If FolderInfo.Attributes.HasFlag(FileAttributes.ReparsePoint) Then Continue For '跳过符号链接
                Dim SearchEntry = GetFolderNameFromPath(FolderInfo.Name).ToLower '用于搜索的字符串
                If IsFullSearch OrElse
                        FolderInfo.Parent.Name.ToLower = "users" OrElse
                        SearchEntry.Contains("java") OrElse SearchEntry.Contains("jdk") OrElse SearchEntry.Contains("env") OrElse
                        SearchEntry.Contains("环境") OrElse SearchEntry.Contains("run") OrElse SearchEntry.Contains("软件") OrElse
                        SearchEntry.Contains("jre") OrElse SearchEntry = "bin" OrElse SearchEntry.Contains("mc") OrElse
                        SearchEntry.Contains("software") OrElse SearchEntry.Contains("cache") OrElse SearchEntry.Contains("temp") OrElse
                        SearchEntry.Contains("corretto") OrElse SearchEntry.Contains("roaming") OrElse SearchEntry.Contains("users") OrElse
                        SearchEntry.Contains("craft") OrElse SearchEntry.Contains("program") OrElse SearchEntry.Contains("世界") OrElse
                        SearchEntry.Contains("net") OrElse SearchEntry.Contains("游戏") OrElse SearchEntry.Contains("oracle") OrElse
                        SearchEntry.Contains("game") OrElse SearchEntry.Contains("file") OrElse SearchEntry.Contains("data") OrElse
                        SearchEntry.Contains("jvm") OrElse SearchEntry.Contains("服务") OrElse SearchEntry.Contains("server") OrElse
                        SearchEntry.Contains("客户") OrElse SearchEntry.Contains("client") OrElse SearchEntry.Contains("整合") OrElse
                        SearchEntry.Contains("应用") OrElse SearchEntry.Contains("运行") OrElse SearchEntry.Contains("前置") OrElse
                        SearchEntry.Contains("mojang") OrElse SearchEntry.Contains("官启") OrElse SearchEntry.Contains("新建文件夹") OrElse
                        SearchEntry.Contains("eclipse") OrElse SearchEntry.Contains("microsoft") OrElse SearchEntry.Contains("hotspot") OrElse
                        SearchEntry.Contains("runtime") OrElse SearchEntry.Contains("x86") OrElse SearchEntry.Contains("x64") OrElse
                        SearchEntry.Contains("forge") OrElse SearchEntry.Contains("原版") OrElse SearchEntry.Contains("optifine") OrElse
                        SearchEntry.Contains("官方") OrElse SearchEntry.Contains("启动") OrElse SearchEntry.Contains("hmcl") OrElse
                        SearchEntry.Contains("mod") OrElse SearchEntry.Contains("高清") OrElse SearchEntry.Contains("download") OrElse
                        SearchEntry.Contains("launch") OrElse SearchEntry.Contains("程序") OrElse SearchEntry.Contains("path") OrElse
                        SearchEntry.Contains("国服") OrElse SearchEntry.Contains("网易") OrElse SearchEntry.Contains("ext") OrElse '网易 Java 文件夹名
                        SearchEntry.Contains("netease") OrElse SearchEntry.Contains("1.") OrElse SearchEntry.Contains("启动") Then
                    JavaSearchFolder(FolderInfo, Results, Source)
                End If
            Next
        Catch ex As UnauthorizedAccessException
            Log("[Java] 遍历查找 Java 时遭遇无权限的文件夹：" & OriginalPath.FullName)
        Catch ex As Exception
            Log(ex, "遍历查找 Java 时出错（" & OriginalPath.FullName & "）")
        End Try
    End Sub
    Public PathAppdata As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\"

    '获取

    ''' <summary>
    ''' 根据要求返回最适合的 Java，若找不到则返回 Nothing。
    ''' 最小与最大版本在与输入相同时也会通过。
    ''' 必须在工作线程调用。
    ''' </summary>
    Public Function JavaSelect(Optional MinVersion As Version = Nothing, Optional MaxVersion As Version = Nothing, Optional RelatedVersion As McVersion = Nothing) As JavaEntry
        Try
            Dim AllowedJavaList As New List(Of JavaEntry)

            '添加特定的 Java
            Dim JavaPreList As New Dictionary(Of String, Boolean)
            If PathMcFolder.Split("\").Count > 3 Then JavaSearchFolder(GetPathFromFullPath(PathMcFolder), JavaPreList, False, True) 'Minecraft 文件夹的父文件夹（如果不是根目录的话）
            JavaSearchFolder(PathMcFolder, JavaPreList, False, True) 'Minecraft 文件夹
            If RelatedVersion IsNot Nothing Then JavaSearchFolder(RelatedVersion.Path, JavaPreList, False, True) '所选版本文件夹
            Dim TargetJavaList As New List(Of JavaEntry)
            For Each Entry In JavaPreList
                TargetJavaList.Add(New JavaEntry(Entry.Key, Entry.Value))
            Next
            If TargetJavaList.Count > 0 Then
                TargetJavaList = JavaCheckList(TargetJavaList)
                Log("[Java] 检查后找到 " & TargetJavaList.Count & " 个特定目录下的 Java")
            End If

RetryGet:
            '等待进行中的搜索结束
            If JavaSearchLoader.State <> LoadState.Finished AndAlso JavaSearchLoader.State <> LoadState.Waiting Then JavaSearchLoader.WaitForExit()
            Select Case JavaSearchLoader.State
                Case LoadState.Failed
                    Throw JavaSearchLoader.Error
                Case LoadState.Aborted
                    Throw New ThreadInterruptedException("Java 搜索加载器已中断")
            End Select
            Dim AllJavaList As New List(Of JavaEntry)
            AllJavaList.AddRange(TargetJavaList)
            AllJavaList.AddRange(JavaList)

            '根据选定条件进行过滤
            For Each Java In AllJavaList
                If MinVersion IsNot Nothing AndAlso Java.Version < MinVersion Then Continue For
                If MaxVersion IsNot Nothing AndAlso Java.Version > MaxVersion Then Continue For
                If Java.Is64Bit AndAlso Is32BitSystem Then Continue For
                AllowedJavaList.Add(Java)
            Next

            '若未找到适合的 Java，尝试触发搜索
            If AllowedJavaList.Count = 0 AndAlso JavaSearchLoader.State = LoadState.Waiting Then
                Log("[Java] 未找到满足条件的 Java，尝试进行搜索")
                JavaSearchLoader.Start()
                GoTo RetryGet
            End If

            '若依然未找到适合的 Java，直接返回
            If AllowedJavaList.Count = 0 Then Return Nothing

            '检查用户指定的 Java
            Dim UserSetup As String = Setup.Get("LaunchArgumentJavaSelect")
            If RelatedVersion IsNot Nothing Then
                Dim UserSetupVersion As String = Setup.Get("VersionArgumentJavaSelect", Version:=RelatedVersion)
                If UserSetupVersion <> "使用全局设置" Then UserSetup = UserSetupVersion
            End If
            If UserSetup <> "" Then
                Dim UserJava As JavaEntry
                Try
                    UserJava = JavaEntry.FromJson(GetJson(UserSetup))
                Catch ex As Exception
                    Setup.Set("LaunchArgumentJavaSelect", "")
                    Log(ex, "获取储存的 Java 失败")
                    GoTo UserPass
                End Try
                For Each Java In AllowedJavaList
                    If Java.PathFolder = UserJava.PathFolder Then
                        '直接使用指定的 Java
                        AllowedJavaList = New List(Of JavaEntry) From {UserJava}
                        GoTo UserPass
                    End If
                Next
                Log("[Java] 发现用户指定的不兼容 Java：" & UserJava.ToString)
                If MyMsgBox("你在启动设置中指定了使用下列 Java：" & vbCrLf &
                            " - " & UserJava.ToString & vbCrLf &
                            vbCrLf &
                            "该 Java 可能不兼容当前游戏版本，游戏存在崩溃风险。是否要强制使用该 Java？", "Java 兼容性警告", "取消，让 PCL2 自动选择", "继续") = 2 Then
                    '强制使用指定的 Java
                    Log("[Java] 已强制使用用户指定的不兼容 Java")
                    AllowedJavaList = New List(Of JavaEntry) From {UserJava}
                End If
            End If

            '检查特定的 Java
            For Each Java In AllowedJavaList
                If TargetJavaList.Contains(Java) Then
                    '直接使用指定的 Java
                    AllowedJavaList = New List(Of JavaEntry) From {Java}
                    Log("[Java] 使用特定目录下的 Java：" & Java.ToString)
                    GoTo UserPass
                End If
            Next
UserPass:

            '对适合的 Java 进行排序
            AllowedJavaList = Sort(AllowedJavaList, AddressOf JavaSorter)

            '检查选定的 Java，若测试失败则尝试进行搜索
            Dim SelectedJava = AllowedJavaList.First
            Try
                SelectedJava.Check()
            Catch ex As Exception
                Log(ex, "找到的 Java 已无法使用，尝试进行搜索")
                JavaSearchLoader.Start(IsForceRestart:=True)
                GoTo RetryGet
            End Try

            '返回
            Log("[Java] 选定的 Java：" & SelectedJava.ToString)
            Return SelectedJava

        Catch ex As ThreadInterruptedException
            Log(ex, "查找符合条件的 Java 时出现加载器中断")
            Return Nothing
        Catch ex As Exception
            Log(ex, "查找符合条件的 Java 失败", LogLevel.Feedback)
            Return Nothing
        End Try
    End Function
    ''' <summary>
    ''' 返回是否安装了 64 位 Java，或在强制指定的时候是否指定 64 位 Java。
    ''' </summary>
    Public Function JavaUse64Bit(Optional RelatedVersion As McVersion = Nothing) As Boolean
        Try
            '检查强制指定
            Dim UserSetup As String = Setup.Get("LaunchArgumentJavaSelect")
            If RelatedVersion IsNot Nothing Then
                Dim UserSetupVersion As String = Setup.Get("VersionArgumentJavaSelect", Version:=RelatedVersion)
                If UserSetupVersion <> "使用全局设置" Then UserSetup = UserSetupVersion
            End If
            If UserSetup <> "" Then
                Dim UserJava = JavaEntry.FromJson(GetJson(UserSetup))
                For Each Java In JavaList
                    If Java.PathFolder = UserJava.PathFolder Then Return UserJava.Is64Bit
                Next
            End If
            '检查列表
            For Each Java In JavaList
                If Java.Is64Bit Then Return True
            Next
            Return False
        Catch ex As Exception
            Log(ex, "检查 Java 类别时出错", LogLevel.Feedback)
            Setup.Set("LaunchArgumentJavaSelect", "")
            Setup.Set("VersionArgumentJavaSelect", "", Version:=RelatedVersion)
            Return True
        End Try
    End Function
    ''' <summary>
    ''' 用于 Java 排序的函数。
    ''' </summary>
    Public Function JavaSorter(Left As JavaEntry, Right As JavaEntry) As Boolean
        '1. 尽量在当前文件夹或当前 Minecraft 文件夹
        Dim ProgramPathParent As String, MinecraftPathParent As String = ""
        ProgramPathParent = If(New DirectoryInfo(Path).Parent, New DirectoryInfo(Path)).FullName
        If PathMcFolder <> "" Then MinecraftPathParent = If(New DirectoryInfo(PathMcFolder).Parent, New DirectoryInfo(PathMcFolder)).FullName
        If Left.PathFolder.StartsWith(ProgramPathParent) AndAlso Not Right.PathFolder.StartsWith(ProgramPathParent) Then Return True
        If Not Left.PathFolder.StartsWith(ProgramPathParent) AndAlso Right.PathFolder.StartsWith(ProgramPathParent) Then Return False
        If PathMcFolder <> "" Then
            If Left.PathFolder.StartsWith(MinecraftPathParent) AndAlso Not Right.PathFolder.StartsWith(MinecraftPathParent) Then Return True
            If Not Left.PathFolder.StartsWith(MinecraftPathParent) AndAlso Right.PathFolder.StartsWith(MinecraftPathParent) Then Return False
        End If
        '2. 尽量使用 64 位
        If Left.Is64Bit AndAlso Not Right.Is64Bit Then Return True
        If Not Left.Is64Bit AndAlso Right.Is64Bit Then Return False
        '3. 尽量不使用 JDK
        If Left.IsJre AndAlso Not Right.IsJre Then Return True
        If Not Left.IsJre AndAlso Right.IsJre Then Return False
        '4. Java 大版本
        If Left.VersionCode <> Right.VersionCode Then
            '                             Java  7   8   9  10  11  12 13 14 15  16  17  18  19
            Dim Weight = {0, 1, 2, 3, 4, 5, 6, 14, 29, 10, 11, 12, 13, 9, 8, 7, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28}
            Return Weight.ElementAtOrDefault(Left.VersionCode) >= Weight.ElementAtOrDefault(Right.VersionCode)
        End If
        '5. 最次级版本号更接近 51
        Return Math.Abs(Left.Version.Revision - 51) <= Math.Abs(Right.Version.Revision - 51)
    End Function

    '缺失
    ''' <summary>
    ''' 提示 Java 缺失并跳转下载。支持 Java 7、8、16。
    ''' </summary>
    Public Sub JavaMissing(VersionCode As Integer)
        Select Case VersionCode
            Case 7
                MyMsgBox("PCL2 未找到 Java 7。" & vbCrLf &
                         "请自行百度安装 Java 7，安装后在 PCL2 的 设置 → 启动设置 → 游戏 Java 中通过搜索或导入，确保安装的 Java 已列入 Java 列表。",
                         "未找到 Java")
            Case 8
                If Is32BitSystem Then
                    OpenWebsite("https://wwa.lanzoui.com/i7RyXq0jbub")
                Else
                    OpenWebsite("https://wwa.lanzoui.com/i2UyMq0jaqb")
                End If
                MyMsgBox("PCL2 未找到版本适宜的 Java 8。" & vbCrLf &
                         "请在打开的网页中下载安装包并安装，安装后在 PCL2 的 设置 → 启动设置 → 游戏 Java 中通过搜索或导入，确保安装的 Java 已列入 Java 列表。",
                         "未找到 Java")
            Case 16, 17
                If Is32BitSystem Then
                    MyMsgBox("该版本的 MC 已不支持 32 位操作系统。你必须增加内存并重装为 64 位系统才能继续。", "系统兼容性提示")
                Else
                    OpenWebsite("https://www.oracle.com/java/technologies/downloads/#jdk17-windows")
                    MyMsgBox("PCL2 未找到 Java " & VersionCode & "。" & vbCrLf &
                             "请在打开的网页中选择 x64 Installer，下载并安装。" & vbCrLf &
                             "安装后在 PCL2 的 设置 → 启动设置 → 游戏 Java 中通过搜索或导入，确保安装的 Java 已列入 Java 列表。",
                             "未找到 Java")
                End If
        End Select
    End Sub

#End Region

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
        Public VersionCount As Integer
        Public Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is McFolder) Then
                Return False
            End If

            Dim folder = DirectCast(obj, McFolder)
            Return Name = folder.Name AndAlso
                   Path = folder.Path AndAlso
                   Type = folder.Type AndAlso
                   VersionCount = folder.VersionCount
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
#Region "获取可用版本数"
                If Directory.Exists(Folder.Path & "versions\") Then
                    Folder.VersionCount = New DirectoryInfo(Folder.Path & "versions\").GetDirectories.Count
                    '减去隐藏版本的个数
                    For Each Dir As DirectoryInfo In New DirectoryInfo(Folder.Path & "versions\").GetDirectories
                        If ReadIni(Dir.FullName & "\PCL\Setup.ini", "Hide", "False") Then Folder.VersionCount -= 1
                    Next
                End If
#End Region
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
        ""PCL2"": {
            ""icon"": ""Grass"",
            ""name"": ""PCL2"",
            ""lastVersionId"": ""latest-release"",
            ""type"": ""latest-release"",
            ""lastUsed"": """ & Date.Now.ToString("yyyy-MM-dd") & "T" & Date.Now.ToString("HH:mm:ss") & ".0000Z""
        }
    },
    ""selectedProfile"": ""PCL2"",
    ""clientToken"": ""23323323323323323323323323323333""
}"
            WriteFile(Folder & "launcher_profiles.json", ResultJson, Encoding:=Encoding.Default)
            Log("[Minecraft] 已创建 launcher_profiles.json：" & Folder)
        Catch ex As Exception
            Log(ex, "创建 launcher_profiles.json 失败（" & Folder & "）", LogLevel.Feedback)
        End Try
    End Sub

#End Region

#Region "版本处理"

    Public Const McVersionCacheVersion As Integer = 24

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
                Return GetPathIndie(Version.Modable)
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
                        'TODO: [Quilt 支持] 从 Quilt 版本中获取版本号
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
                    If Not File.Exists(Path & Name & ".json") Then Throw New Exception("未找到版本 Json 文件")
                    _JsonText = ReadFile(Path & Name & ".json")
                    '如果 ReadFile 失败会返回空字符串；这可能是由于文件被临时占用，故延时后重试
                    If _JsonText.Length = 0 Then
                        If RunInUi() Then
                            Log("[Minecraft] 版本 Json 文件为空或读取失败，由于代码在主线程运行，将不再进行重试", LogLevel.Debug)
                            Throw New Exception("版本 Json 文件为空或读取失败")
                        Else
                            Log("[Minecraft] 版本 Json 文件为空或读取失败，将在 2s 后重试读取（" & Path & Name & ".json）", LogLevel.Debug)
                            Thread.Sleep(2000)
                            _JsonText = ReadFile(Path & Name & ".json")
                            If _JsonText.Length = 0 Then Throw New Exception("版本 Json 文件为空或读取失败")
                        End If
                    End If
                    If _JsonText.Length < 100 Then Throw New Exception("版本 Json 文件有误，内容为：" & _JsonText)
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
                            Log(ex, "合并版本依赖项 Json 失败（" & If(InheritVersion, "null").ToString & "）")
                        End Try
                    Catch ex As Exception
                        Throw New Exception("版本 Json 不规范（" & If(Name, "null") & "）", ex)
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
                Info = "该文件夹不存在"
                Return False
            End If
            '检查权限
            Try
                Directory.CreateDirectory(Path & "PCL\")
                CheckPermissionWithException(Path & "PCL\")
            Catch ex As Exception
                State = McVersionState.Error
                Info = "PCL2 没有对该文件夹的访问权限，请右键以管理员身份运行 PCL2"
                Log(ex, "没有访问版本文件夹的权限")
                Return False
            End Try
            '确认 Json 可用性
            Try
                Dim JsonObjCheck = JsonObject
            Catch ex As Exception
                Log(ex, "版本 Json 可用性检查失败（" & Name & "）")
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
                Info = "未知错误：" & GetString(ex)
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
                Directory.CreateDirectory(Path & "PCL")
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
                        If If(JsonObject("type"), "").ToString = "fool" OrElse GetMcFoolName(Name) <> "" Then
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
                        'TODO: [Quilt 支持] 确认这里的玩意儿对不对
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
                            If Version.McName.ToLower.Contains("pre") OrElse Version.McName.ToLower.Contains("rc") Then
                                Info = "预发布版 " & Version.McName
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
                            Info = GetMcFoolName(Name)
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
                WriteIni(Path & "PCL\Setup.ini", "State", State)
                WriteIni(Path & "PCL\Setup.ini", "Info", Info)
                WriteIni(Path & "PCL\Setup.ini", "Logo", Logo)
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
                Info = "未知错误：" & GetString(ex)
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
        ''' 该版本是否可以安装 Mod。
        ''' </summary>
        Public ReadOnly Property Modable As Boolean
            Get
                Return HasFabric OrElse HasForge OrElse HasLiteLoader
            End Get
        End Property

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
            Return "我们加入了 20 亿个新的世界，让无限的想象变成了现实！"
        ElseIf Name = "15w14a" Then
            Return "作为一款全年龄向的游戏，我们需要和平，需要爱与拥抱。"
        ElseIf Name = "1.rv-pre1" Then
            Return "是时候将现代科技带入 Minecraft 了！"
        ElseIf Name = "3d shareware v1.34" Then
            Return "我们从地下室的废墟里找到了这个开发于 1994 年的杰作！"
        ElseIf Name = "22w13oneblockatatime" Then
            Return "一次一个方块更新！迎接全新的挖掘、合成与骑乘玩法吧！"
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
            If McVersionList.Count = 0 Then
                McVersionCurrent = Nothing
                Setup.Set("LaunchVersionSelect", "")
                Log("[Minecraft] 未找到可用 Minecraft 版本")
            Else
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
                        If Version.State <> McVersionState.Error Then
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
                If NewerVersion.ContainsKey(Code & "-" & McVersionState.OptiFine) Then
                    Dim OptiFineVersion As McVersion = NewerVersion(Code & "-" & McVersionState.OptiFine)
                    If NewerVersion.ContainsKey(Code & "-" & McVersionState.Original) AndAlso Not NewerVersion(Code & "-" & McVersionState.Original).Version.McName = OptiFineVersion.InheritVersion Then
                        '同时存在 OptiFine 与原版，但 OptiFine 不是该版的依赖版本，则一定为两个不同版本，且原版较新
                        VersionUseful.Add(NewerVersion(Code & "-" & McVersionState.Original))
                        VersionList.Remove(NewerVersion(Code & "-" & McVersionState.Original))
                    End If
                    VersionUseful.Add(OptiFineVersion)
                    VersionList.Remove(OptiFineVersion)
                Else
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
    Public Function McSkinSelect(MustHaveTwoLayers As Boolean) As McSkinInfo
        Dim FileName As String = SelectFile("皮肤文件(*.png)|*.png", "选择皮肤文件")

        '验证有效性
        If FileName = "" Then Return New McSkinInfo With {.IsVaild = False}
        Try
            Dim Image As New MyBitmap(FileName)
            If MustHaveTwoLayers Then
                If Image.Pic.Width = 64 AndAlso Image.Pic.Height = 32 Then
                    Hint("自定义离线皮肤只支持 64x64 像素的双层皮肤！", HintType.Critical)
                    Return New McSkinInfo With {.IsVaild = False}
                End If
                If Image.Pic.Width <> 64 OrElse Image.Pic.Height <> 64 Then
                    Hint("自定义离线皮肤图片大小应为 64x64 像素！", HintType.Critical)
                    Return New McSkinInfo With {.IsVaild = False}
                End If
            Else
                If Image.Pic.Width <> 64 OrElse Not (Image.Pic.Height = 32 OrElse Image.Pic.Height = 64) Then
                    Hint("皮肤图片大小应为 64x32 像素或 64x64 像素！", HintType.Critical)
                    Return New McSkinInfo With {.IsVaild = False}
                End If
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
                IsRightRule = IsRightRule AndAlso IsNothing(Rule("features")("is_demo_user")) '只反选是否为 Demo 用户
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
                '根据 Inherit 获取最深层版本
                '此外，HMCL 项直接使用自身的 Jar
                Dim OriginalVersion As McVersion = Version
                Do Until OriginalVersion.InheritVersion = ""
                    If OriginalVersion.InheritVersion = OriginalVersion.Name Then Exit Do
                    OriginalVersion = New McVersion(PathMcFolder & "versions\" & OriginalVersion.InheritVersion & "\")
                Loop
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
                Log("[Minecraft] 可能缺少前置版本 " & RealVersion.Name & "，找不到对应的 Json 文件", LogLevel.Debug)
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
            Log(ex, "版本缺失主 Jar 文件所必须的信息", LogLevel.Developer)
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
            Dim TargetFile = Version.PathIndie & "nide8auth.jar"
            Dim Checker As New FileChecker(MinSize:=173000)
            If (IsSetupSkip AndAlso File.Exists(TargetFile)) OrElse Checker.Check(TargetFile) IsNot Nothing Then
                Result.Add(New NetFile({"https://login.mc-user.com:233/index/jar"}, TargetFile, Checker))
            End If
        End If
        'Authlib-Injector 文件
        If Setup.Get("VersionServerLogin", Version:=Version) = 4 Then
            Dim TargetFile = Version.PathIndie & "authlib-injector.jar"
            Dim DownloadInfo As JObject = Nothing
            '获取下载信息
            Try
                Log("[Minecraft] 开始获取 Authlib-Injector 下载信息")
                DownloadInfo = GetJson(NetGetCodeByDownload({"https://download.mcbbs.net/mirrors/authlib-injector/artifact/latest.json", "https://bmclapi2.bangbang93.com/mirrors/authlib-injector/artifact/latest.json"}, IsJson:=True))
            Catch ex As Exception
                Log("获取 Authlib-Injector 下载信息失败", LogLevel.Hint)
            End Try
            '校验文件
            If DownloadInfo IsNot Nothing Then
                Dim Checker As New FileChecker(Hash:=DownloadInfo("checksums")("sha256").ToString)
                If (IsSetupSkip AndAlso File.Exists(TargetFile)) OrElse Checker.Check(TargetFile) IsNot Nothing Then
                    '开始下载
                    Dim DownloadAddress As String = DownloadInfo("download_url")
                    Log("[Minecraft] Authlib-Injector 需要更新：" & DownloadAddress)
                    Result.Add(New NetFile({
                            DownloadAddress.Replace("bmclapi2.bangbang93.com", "download.mcbbs.net"), DownloadAddress
                        }, TargetFile, New FileChecker(Hash:=DownloadInfo("checksums")("sha256").ToString)))
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
            Result.Add(New NetFile(ArrayNoDouble(Urls.ToArray()), Token.LocalPath, Checker))
        Next
        '去重并返回
        Return ArrayNoDouble(Result, Function(Left As NetFile, Right As NetFile)
                                         Return Left.LocalPath = Right.LocalPath
                                     End Function)
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
                Result.Add(New NetFile(DlSourceResourceGet("http://resources.download.minecraft.net/" & Left(Token.Hash, 2) & "/" & Token.Hash), Token.LocalPath, New FileChecker(ActualSize:=If(Token.Size = 0, -1, Token.Size), Hash:=Token.Hash)))
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
                _FileUnavailableReason = New UnauthorizedAccessException("没有读取此文件的权限，请尝试右键以管理员身份运行 PCL2", ex)
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
                        DictionaryAdd(TomlData.Last.Value, Key, Value)
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
                For Each File As FileInfo In New DirectoryInfo(Loader.Input).EnumerateFiles("*.*", SearchOption.AllDirectories)
                    Dim DirName As String = File.DirectoryName.ToLower
                    If DirName.StartsWith(Loader.Input & "memory_repo") OrElse DirName.StartsWith(".") OrElse
                       ((DirName & "\") <> Loader.Input AndAlso DirName.Contains("voxel")) Then Continue For
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
                Result.Add("检查 Mod 时出错：" & GetString(ex) & vbCrLf & " - " & ModEntity.FileName)
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
            Dim MsgResult As Integer
            If (Date.Now - Time).TotalDays > 1 Then
                '1 天以上
                MsgResult = MyMsgBox("新版本：" & VersionName & vbCrLf & "更新时间：" & Time.ToString, "Minecraft 更新提示", "下载", "更新日志", "取消")
            ElseIf (Date.Now - Time).TotalHours > 3 Then
                '1 天 ~ 3 小时
                MsgResult = MyMsgBox("新版本：" & VersionName & vbCrLf & "更新于：" & GetTimeSpanString(Time - Date.Now), "Minecraft 更新提示", "下载", "更新日志", "取消")
            Else
                '不到 3 小时
                MsgResult = MyMsgBox("新版本：" & VersionName & vbCrLf & "更新于：" & GetTimeSpanString(Time - Date.Now), "Minecraft 更新提示", "下载", "取消")
                If MsgResult = 2 Then MsgResult = 3 '把 “取消” 移位
            End If
            '弹窗结果
            Select Case MsgResult
                Case 1
                    '下载
                    McDownloadClient(NetPreDownloadBehaviour.HintWhileExists, VersionName, Version("url").ToString)
                Case 2
                    '更新日志
                    McUpdateLogShow(Version)
            End Select

        Catch ex As Exception
            Log(ex, "Minecraft 更新提示发送失败（" & If(VersionName, "Nothing") & "）", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 比较两个版本名的排序，若 Left 较新则返回 True。无法比较两个 Pre 的大小。
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
        Dim Lefts = RegexSearch(Left.ToLower, "[a-z]+|[0-9]+")
        Dim Rights = RegexSearch(Right.ToLower, "[a-z]+|[0-9]+")
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
    ''' <summary>
    ''' 对 PCL 约定的替换标记进行处理。
    ''' </summary>
    Public Function ArgumentReplace(Raw As String) As String
        If Raw Is Nothing Then Return Nothing
        Raw = Raw.Replace("{user}", If(IsNothing(McLoginLoader.Output), "尚未登录", McLoginLoader.Output.Name))
        If IsNothing(McLoginLoader.Input) Then
            Raw = Raw.Replace("{login}", "尚未登录")
        Else
            Select Case McLoginLoader.Input.Type
                Case McLoginType.Legacy
                    Raw = Raw.Replace("{login}", "离线")
                Case McLoginType.Mojang
                    Raw = Raw.Replace("{login}", "Mojang 正版")
                Case McLoginType.Ms
                    Raw = Raw.Replace("{login}", "微软正版")
                Case McLoginType.Nide
                    Raw = Raw.Replace("{login}", "统一通行证")
                Case McLoginType.Auth
                    Raw = Raw.Replace("{login}", "Authlib-Injector")
            End Select
        End If
        If McVersionCurrent Is Nothing Then
            Raw = Raw.Replace("{name}", "无可用版本")
            Raw = Raw.Replace("{version}", "无可用版本")
        Else
            Raw = Raw.Replace("{name}", McVersionCurrent.Name)
            If Not McVersionCurrent.IsLoaded Then McVersionCurrent.Load()
            If {"unknown", "old", "pending"}.Contains(McVersionCurrent.Version.McName.ToLower) Then
                Raw = Raw.Replace("{version}", McVersionCurrent.Name)
            Else
                Raw = Raw.Replace("{version}", McVersionCurrent.Version.McName)
            End If
        End If
        Raw = Raw.Replace("{path}", Path)
        Return Raw
    End Function

End Module
