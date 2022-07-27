Public Class CrashAnalyzer
    Private Shared IsAnalyzeCacheCleared As Boolean = False
    Private Shared IsAnalyzeCacheClearedLock As New Object

    '构造函数
    Private TempFolder As String
    Public Sub New(UUID As Integer)
        '清理缓存
        SyncLock IsAnalyzeCacheClearedLock
            If Not IsAnalyzeCacheCleared Then
                Try
                    DeleteDirectory(PathTemp & "CrashAnalyzer")
                Catch ex As Exception
                    Log(ex, "清理崩溃分析缓存失败")
                End Try
                IsAnalyzeCacheCleared = True
            End If
        End SyncLock
        '构建文件结构
        TempFolder = PathTemp & "CrashAnalyzer\" & UUID & RandomInteger(0, 99999999) & "\"
        DeleteDirectory(TempFolder)
        Directory.CreateDirectory(TempFolder & "Temp\")
        Directory.CreateDirectory(TempFolder & "Report\")
        Log("[Crash] 崩溃分析暂存文件夹：" & TempFolder)
    End Sub

    '1：准备用于分析的 Log 文件
    Private AnalyzeRawFiles As New List(Of KeyValuePair(Of String, String())) '暂存的日志文件：文件完整路径 -> 文件内容
    ''' <summary>
    ''' 将可用于分析的日志存储到 AnalyzeRawFiles。
    ''' </summary>
    ''' <param name="LatestLog">从 PCL2 捕获到的最后 200 行程序输出。</param>
    Public Sub Collect(VersionPathIndie As String, Optional LatestLog As IList(Of String) = Nothing)
        Log("[Crash] 步骤 1：收集日志文件")

        '简单收集可能的日志文件路径
        Dim PossibleLogs As New List(Of String)
        Try
            Dim DirInfo As New DirectoryInfo(VersionPathIndie & "crash-reports\")
            If DirInfo.Exists Then
                For Each File In DirInfo.EnumerateFiles
                    PossibleLogs.Add(File.FullName)
                Next
            End If
        Catch ex As Exception
            Log(ex, "收集 Minecraft 崩溃日志文件夹下的日志失败")
        End Try
        Try
            For Each File In New DirectoryInfo(VersionPathIndie).Parent.Parent.EnumerateFiles
                If If(File.Extension, "") <> ".log" Then Continue For
                PossibleLogs.Add(File.FullName)
            Next
        Catch ex As Exception
            Log(ex, "收集 Minecraft 主文件夹下的日志失败")
        End Try
        Try
            For Each File In New DirectoryInfo(VersionPathIndie).EnumerateFiles
                If If(File.Extension, "") <> ".log" Then Continue For
                PossibleLogs.Add(File.FullName)
            Next
        Catch ex As Exception
            Log(ex, "收集 Minecraft 隔离文件夹下的日志失败")
        End Try
        PossibleLogs.Add(VersionPathIndie & "logs\latest.log") 'Minecraft 日志
        PossibleLogs.Add(VersionPathIndie & "logs\debug.log") 'Minecraft Debug 日志
        PossibleLogs = ArrayNoDouble(PossibleLogs)

        '确定最新的日志文件
        Dim RightLogs As New List(Of String)
        For Each LogFile In PossibleLogs
            Try
                Dim Info As New FileInfo(LogFile)
                If Not Info.Exists Then Continue For
                Dim Time = Math.Abs((Info.LastWriteTime - Date.Now).TotalMinutes)
                If Time < 3 AndAlso Info.Length > 0 Then
                    RightLogs.Add(LogFile)
                    Log("[Crash] 可能可用的日志文件：" & LogFile & "（" & Math.Round(Time, 1) & " 分钟）")
                End If
            Catch ex As Exception
                Log(ex, "确认崩溃日志时间失败（" & LogFile & "）")
            End Try
        Next
        If RightLogs.Count = 0 Then Log("[Crash] 未发现可能可用的日志文件")

        '将可能可用的日志文件导出
        For Each FilePath In RightLogs
            Try
                If FilePath.Contains("crash-") Then
                    AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(FilePath, ReadFile(FilePath).Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Split(vbCr)))
                Else
                    AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(FilePath, File.ReadAllLines(FilePath, Encoding.UTF8)))
                End If
            Catch ex As Exception
                Log(ex, "读取可能的崩溃日志文件失败（" & FilePath & "）")
            End Try
        Next
        If LatestLog IsNot Nothing AndAlso LatestLog.Count > 0 Then
            Dim RawOutput As String = Join(LatestLog, vbCrLf)
            Log("[Crash] 以下为游戏输出的最后一段内容：" & vbCrLf & RawOutput)
            WriteFile(TempFolder & "RawOutput.log", RawOutput)
            AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(TempFolder & "RawOutput.log", LatestLog.ToArray))
            LatestLog.Clear()
        End If

        Log("[Crash] 步骤 1：收集日志文件完成，收集到 " & AnalyzeRawFiles.Count & " 个文件")
    End Sub
    ''' <summary>
    ''' 从文件路径直接导入日志文件或崩溃报告压缩包。
    ''' </summary>
    Public Sub Import(FilePath As String)
        Log("[Crash] 步骤 1：自主导入日志文件")

        '解压压缩包
        Try
            Dim Info As New FileInfo(FilePath)
            If Not Info.Exists OrElse Info.Length = 0 Then Exit Try
            If Not FilePath.ToLower.EndsWith(".jar") AndAlso ExtractFile(FilePath, TempFolder & "Temp\") Then
                '解压成功
                Log("[Crash] 已解压导入的日志文件：" & FilePath)
            Else
                '解压失败
                File.Copy(FilePath, TempFolder & "Temp\" & GetFileNameFromPath(FilePath))
                Log("[Crash] 已复制导入的日志文件：" & FilePath)
            End If
        Catch ex As Exception
            Log(ex, "解压导入文件中的压缩包失败")
        End Try

        '导入其中的日志文件
        For Each TargetFile As FileInfo In New DirectoryInfo(TempFolder & "Temp\").EnumerateFiles
            Try
                If Not TargetFile.Exists OrElse TargetFile.Length = 0 Then Continue For
                Dim Ext As String = TargetFile.Extension.ToLower
                If Ext = ".log" OrElse Ext = ".txt" Then
                    If TargetFile.Name.StartsWith("crash-") Then
                        AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(TargetFile.FullName, ReadFile(TargetFile.FullName).Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Split(vbCr)))
                    Else
                        AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(TargetFile.FullName, File.ReadAllLines(TargetFile.FullName, Encoding.UTF8)))
                    End If
                End If
            Catch ex As Exception
                Log(ex, "导入单个日志文件失败")
            End Try
        Next

        Log("[Crash] 步骤 1：自主导入日志文件，收集到 " & AnalyzeRawFiles.Count & " 个文件")
    End Sub

    '2：确认实际用于分析的 Log 文本
    Private Enum AnalyzeFileType
        HsErr
        MinecraftLog
        ExtraLog
        CrashReport
    End Enum
    ''' <summary>
    ''' 从 AnalyzeRawFiles 中提取实际有用的文本片段存储到 AnalyzeFiles，并整理可用于生成报告的文件。
    ''' 返回找到的用于分析的项目数。
    ''' </summary>
    Public Function Prepare() As Integer
        Log("[Crash] 步骤 2：准备日志文本")

        '对日志文件进行分类
        Dim TotalFiles As New List(Of KeyValuePair(Of AnalyzeFileType, KeyValuePair(Of String, String())))
        For Each LogFile In AnalyzeRawFiles
            Dim FileName As String = GetFileNameFromPath(LogFile.Key)
            Dim TargetType As AnalyzeFileType
            If FileName.StartsWith("hs_err") Then
                TargetType = AnalyzeFileType.HsErr
            ElseIf FileName.StartsWith("crash-") Then
                TargetType = AnalyzeFileType.CrashReport
            ElseIf FileName = "latest.log" OrElse FileName = "latest log.txt" OrElse
                   FileName = "debug.log" OrElse FileName = "debug log.txt" OrElse FileName = "游戏崩溃前的输出.txt" OrElse
                   FileName = "rawoutput.log" OrElse FileName = "启动器日志.txt" OrElse FileName = "PCL2 启动器日志.txt" OrElse FileName = "log1.txt" Then
                TargetType = AnalyzeFileType.MinecraftLog
            ElseIf FileName.EndsWith(".log") OrElse FileName.EndsWith(".txt") Then
                TargetType = AnalyzeFileType.ExtraLog
            Else
                Log("[Crash] " & FileName & " 分类为 Ignore")
                Continue For
            End If
            If LogFile.Value.Count = 0 Then
                Log("[Crash] " & FileName & " 由于内容为空跳过")
            Else
                TotalFiles.Add(New KeyValuePair(Of AnalyzeFileType, KeyValuePair(Of String, String()))(TargetType, LogFile))
                Log("[Crash] " & FileName & " 分类为 " & GetStringFromEnum(TargetType))
            End If
        Next

        '将分类后的文件分别写入
        For Each SelectType In {AnalyzeFileType.MinecraftLog, AnalyzeFileType.HsErr, AnalyzeFileType.ExtraLog, AnalyzeFileType.CrashReport}
            '获取该种类的所有文件 {文件路径 -> 文件内容行}
            Dim SelectedFiles As New List(Of KeyValuePair(Of String, String()))
            For Each File In TotalFiles
                If SelectType = File.Key Then SelectedFiles.Add(File.Value)
            Next
            If SelectedFiles.Count = 0 Then Continue For
            Try
                '根据文件类别判断
                Select Case SelectType
                    Case AnalyzeFileType.HsErr, AnalyzeFileType.CrashReport
                        '获取文件的修改日期
                        Dim DatedFiles As New SortedList(Of Date, KeyValuePair(Of String, String()))()
                        For Each File In SelectedFiles
                            Try
                                DatedFiles.Add(New FileInfo(File.Key).LastWriteTime, File)
                            Catch ex As Exception
                                Log(ex, "获取日志文件修改时间失败")
                                DatedFiles.Add(New Date(1900, 1, 1), File)
                            End Try
                        Next
                        '输出最新的文件
                        Dim NewestFile As KeyValuePair(Of String, String()) = DatedFiles.Last.Value
                        OutputFiles.Add(NewestFile.Key)
                        If SelectType = AnalyzeFileType.HsErr Then
                            LogHs = GetHeadTailLines(NewestFile.Value, 200, 100)
                            Log("[Crash] 输出报告：" & NewestFile.Key & "，作为虚拟机错误信息")
                            Log("[Crash] 导入分析：" & NewestFile.Key & "，作为虚拟机错误信息")
                        Else
                            LogCrash = GetHeadTailLines(NewestFile.Value, 300, 700)
                            Log("[Crash] 输出报告：" & NewestFile.Key & "，作为 Minecraft 崩溃报告")
                            Log("[Crash] 导入分析：" & NewestFile.Key & "，作为 Minecraft 崩溃报告")
                        End If
                    Case AnalyzeFileType.MinecraftLog
                        LogMc = ""
                        '创建文件名词典
                        Dim FileNameDict As New Dictionary(Of String, KeyValuePair(Of String, String()))
                        For Each SelectedFile In SelectedFiles
                            DictionaryAdd(FileNameDict, GetFileNameFromPath(SelectedFile.Key).ToLower, SelectedFile)
                            OutputFiles.Add(SelectedFile.Key)
                            Log("[Crash] 输出报告：" & SelectedFile.Key & "，作为 Minecraft 或启动器日志")
                        Next
                        '选择一份最佳的来自启动器的游戏日志
                        For Each FileName As String In {"rawoutput.log", "启动器日志.txt", "log1.txt", "游戏崩溃前的输出.txt", "PCL2 启动器日志.txt"}
                            If FileNameDict.ContainsKey(FileName) Then
                                Dim CurrentLog = FileNameDict(FileName)
                                '截取 “以下为游戏输出的最后一段内容” 后的内容
                                Dim HasLauncherMark As Boolean = False
                                For Each Line In CurrentLog.Value
                                    If HasLauncherMark Then
                                        LogMc += Line & vbLf
                                    ElseIf Line.Contains("以下为游戏输出的最后一段内容") Then
                                        HasLauncherMark = True
                                        Log("[Crash] 找到 PCL2 输出的游戏实时日志头")
                                    End If
                                Next
                                '导入后 500 行
                                If Not HasLauncherMark Then LogMc += GetHeadTailLines(CurrentLog.Value, 0, 500)
                                LogMc = LogMc.TrimEnd(vbCrLf.ToCharArray) & vbLf & "[" '路径包含中文且存在编码问题导致找不到或无法加载主类的末端检测
                                Log("[Crash] 导入分析：" & CurrentLog.Key & "，作为启动器日志")
                                Exit For
                            End If
                        Next
                        '选择一份最佳的 Minecraft Log
                        For Each FileName As String In {"latest.log", "latest log.txt", "debug.log", "debug log.txt"}
                            If FileNameDict.ContainsKey(FileName) Then
                                Dim CurrentLog = FileNameDict(FileName)
                                LogMc += GetHeadTailLines(CurrentLog.Value, 250, 500) & vbLf & "["
                                Log("[Crash] 导入分析：" & CurrentLog.Key & "，作为 Minecraft 日志")
                                Exit For
                            End If
                        Next
                        '检查错误
                        If LogMc = "" Then
                            LogMc = Nothing
                            Throw New Exception("无法找到匹配的 Minecraft Log")
                        End If
                    Case AnalyzeFileType.ExtraLog
                        '全部丢过去
                        For Each SelectedFile In SelectedFiles
                            OutputFiles.Add(SelectedFile.Key)
                            Log("[Crash] 输出报告：" & SelectedFile.Key & "，作为额外日志")
                        Next
                End Select
            Catch ex As Exception
                Log(ex, "分类处理日志文件时出错")
            End Try
        Next

        '获取种类数
        Dim ResultCount As Integer = If(LogMc Is Nothing, 0, 1) + If(LogHs Is Nothing, 0, 1) + If(LogCrash Is Nothing, 0, 1)
        If ResultCount = 0 Then
            Log("[Crash] 步骤 2：准备日志文本完成，没有任何可供分析的日志")
        Else
            Log(("[Crash] 步骤 2：准备日志文本完成，找到" & If(LogMc Is Nothing, "", "游戏日志、") & If(LogHs Is Nothing, "", "虚拟机日志、") & If(LogCrash Is Nothing, "", "崩溃日志、")).TrimEnd("、") & "用作分析")
        End If
        Return ResultCount

    End Function
    ''' <summary>
    ''' 输出字符串的前后某些行，并统一行尾为 vbLf (\r)、删除空行。
    ''' </summary>
    Private Function GetHeadTailLines(Raw As String(), HeadLines As Integer, TailLines As Integer) As String
        If Raw.Length <= HeadLines + TailLines Then Return Join(Raw, vbLf)
        Dim Result As New StringBuilder
        For i = 0 To Raw.Count - 1
            If i < HeadLines OrElse Raw.Count - i < TailLines Then
                If Raw(i) = "" Then Continue For
                Result.Append(Raw(i) & vbLf)
            End If
        Next
        Return Result.ToString
    End Function

    '3：根据文本分析崩溃原因
    Private LogMc As String = Nothing, LogHs As String = Nothing, LogCrash As String = Nothing
    Private LogAll As String
    '可能导致崩溃的原因与附加信息
    Private CrashReasons As New Dictionary(Of CrashReason, List(Of String))
    ''' <summary>
    ''' 导致崩溃的原因枚举。
    ''' </summary>
    Private Enum CrashReason
        Mod文件被解压
        内存不足
        使用JDK
        显卡不支持OpenGL
        使用OpenJ9
        Java版本过高
        显卡驱动不支持导致无法设置像素格式
        路径包含中文且存在编码问题导致找不到或无法加载主类
        Intel驱动不兼容导致EXCEPTION_ACCESS_VIOLATION 'https://bugs.mojang.com/browse/MC-32606
        AMD驱动不兼容导致EXCEPTION_ACCESS_VIOLATION 'https://bugs.mojang.com/browse/MC-31618
        Nvidia驱动不兼容导致EXCEPTION_ACCESS_VIOLATION
        玩家手动触发调试崩溃
        光影或资源包导致OpenGL1282错误
        文件或内容校验失败
        确定Mod导致游戏崩溃
        Mod配置文件导致游戏崩溃
        ModMixin失败
        Mod加载器报错
        Mod初始化失败
        崩溃日志堆栈分析发现关键字
        崩溃日志堆栈分析发现Mod名称
        MC日志堆栈分析发现关键字
        OptiFine导致无法加载世界 'https://www.minecraftforum.net/forums/support/java-edition-support/3051132-exception-ticking-world
        特定方块导致崩溃
        特定实体导致崩溃
        材质过大或显卡配置不足
        没有可用的分析文件
        使用32位Java导致JVM无法分配足够多的内存
        Mod重复安装
        Fabric报错
        Fabric报错并给出解决方案
        Forge报错
        低版本Forge与高版本Java不兼容
        版本Json中存在多个Forge
        Mod过多导致超出ID限制
    End Enum
    ''' <summary>
    ''' 根据 AnalyzeLogs 与可能的版本信息分析崩溃原因。
    ''' </summary>
    Public Sub Analyze(Optional Version As McVersion = Nothing)
        Log("[Crash] 步骤 3：分析崩溃原因")
        LogAll = (If(LogMc, "") & If(LogHs, "") & If(LogCrash, "")).ToLower

        '1. 精准日志匹配 1
        AnalyzeCrit1()
        If CrashReasons.Count > 0 Then GoTo Done

        '2. 堆栈分析
        If LogAll.Contains("forge") OrElse LogAll.Contains("fabric") OrElse LogAll.Contains("liteloader") Then
            '来源崩溃日志的分析
            If LogCrash IsNot Nothing Then
                Log("[Crash] 开始进行崩溃日志堆栈分析")
                Dim Keywords = AnalyzeStackKeyword(LogCrash.Replace("A detailed walkthrough of the error", "¨").Split("¨").First)
                If Keywords.Count > 0 Then
                    Dim Names = AnalyzeModName(Keywords)
                    If Names Is Nothing Then
                        AppendReason(CrashReason.崩溃日志堆栈分析发现关键字, Keywords)
                    Else
                        AppendReason(CrashReason.崩溃日志堆栈分析发现Mod名称, Names)
                    End If
                    GoTo Done
                End If
            End If
            '来自 Minecraft 日志的分析
            If LogMc IsNot Nothing Then
                Dim Fatals = RegexSearch(LogMc, "/FATAL] [\w\W]+?(?=[\n]+\[)")
                Log("[Crash] 开始进行 Minecraft 日志堆栈分析，发现 " & Fatals.Count & " 个报错项")
                If Fatals.Count > 0 Then
                    Dim Keywords As New List(Of String)
                    For Each Fatal In Fatals
                        Keywords.AddRange(AnalyzeStackKeyword(Fatal))
                    Next
                    If Keywords.Count > 0 Then
                        AppendReason(CrashReason.MC日志堆栈分析发现关键字, ArrayNoDouble(Keywords))
                        GoTo Done
                    End If
                End If
            End If
        Else
            Log("[Crash] 可能并未安装 Mod，不进行堆栈分析")
        End If

        '3. 精准日志匹配 2
        AnalyzeCrit2()

        '输出到日志
Done:
        If CrashReasons.Count = 0 Then
            Log("[Crash] 步骤 3：分析崩溃原因完成，未找到可能的原因")
        Else
            Log("[Crash] 步骤 3：分析崩溃原因完成，找到 " & CrashReasons.Count & " 条可能的原因")
            For Each Reason In CrashReasons
                Log("[Crash]  - " & GetStringFromEnum(Reason.Key) & If(Reason.Value.Count > 0, "（" & Join(Reason.Value, "；") & "）", ""))
            Next
        End If
    End Sub
    ''' <summary>
    ''' 增加一个可能的崩溃原因。
    ''' </summary>
    Private Sub AppendReason(Reason As CrashReason, Optional Additional As ICollection(Of String) = Nothing)
        If CrashReasons.ContainsKey(Reason) Then
            If Additional IsNot Nothing Then
                CrashReasons(Reason).AddRange(Additional)
                CrashReasons(Reason) = ArrayNoDouble(CrashReasons(Reason))
            End If
        Else
            CrashReasons.Add(Reason, New List(Of String)(If(Additional, {})))
        End If
        Log("[Crash] 可能的崩溃原因：" & GetStringFromEnum(Reason) & If(Additional IsNot Nothing AndAlso Additional.Count > 0, "（" & Join(Additional, "；") & "）", ""))
    End Sub
    Private Sub AppendReason(Reason As CrashReason, Additional As String)
        AppendReason(Reason, If(String.IsNullOrEmpty(Additional), Nothing, New List(Of String) From {Additional}))
    End Sub

    '具体的分析代码
    ''' <summary>
    ''' 进行精准日志匹配。匹配优先级高于堆栈分析的崩溃。
    ''' </summary>
    Private Sub AnalyzeCrit1()

        '空白分析
        If LogMc Is Nothing AndAlso LogHs Is Nothing AndAlso LogCrash Is Nothing Then
            AppendReason(CrashReason.没有可用的分析文件)
            Exit Sub
        End If

        '崩溃报告分析，高优先级
        If LogCrash IsNot Nothing Then
            If LogCrash.Contains("Unable to make protected final java.lang.Class java.lang.ClassLoader.defineClass") Then AppendReason(CrashReason.Java版本过高)
        End If

        '游戏日志分析
        If LogMc IsNot Nothing Then
            If LogMc.Contains("Found multiple arguments for option fml.forgeVersion, but you asked for only one") Then AppendReason(CrashReason.版本Json中存在多个Forge)
            If LogMc.Contains("The driver does not appear to support OpenGL") Then AppendReason(CrashReason.显卡不支持OpenGL)
            If LogMc.Contains("java.lang.ClassCastException: java.base/jdk") Then AppendReason(CrashReason.使用JDK)
            If LogMc.Contains("java.lang.ClassCastException: class jdk.") Then AppendReason(CrashReason.使用JDK)
            If LogMc.Contains("Open J9 is not supported") OrElse LogMc.Contains("OpenJ9 is incompatible") OrElse LogMc.Contains(".J9VMInternals.") Then AppendReason(CrashReason.使用OpenJ9)
            If LogMc.Contains("because module java.base does not export") Then AppendReason(CrashReason.Java版本过高)
            If LogMc.Contains("java.lang.ClassNotFoundException: java.lang.invoke.LambdaMetafactory") Then AppendReason(CrashReason.Java版本过高)
            If LogMc.Contains("The directories below appear to be extracted jar files. Fix this before you continue.") Then AppendReason(CrashReason.Mod文件被解压)
            If LogMc.Contains("Extracted mod jars found, loading will NOT continue") Then AppendReason(CrashReason.Mod文件被解压)
            If LogMc.Contains("Couldn't set pixel format") Then AppendReason(CrashReason.显卡驱动不支持导致无法设置像素格式)
            If LogMc.Contains("java.lang.OutOfMemoryError") Then AppendReason(CrashReason.内存不足)
            If LogMc.Contains("java.lang.NoSuchMethodError: sun.security.util.ManifestEntryVerifier") Then AppendReason(CrashReason.低版本Forge与高版本Java不兼容)
            If LogMc.Contains("1282: Invalid operation") Then AppendReason(CrashReason.光影或资源包导致OpenGL1282错误)
            If LogMc.Contains("signer information does not match signer information of other classes in the same package") Then AppendReason(CrashReason.文件或内容校验失败, If(RegexSeek(LogMc, "(?<=class "")[^']+(?=""'s signer information)"), "").TrimEnd(vbCrLf))
            If LogMc.Contains("An exception was thrown, the game will display an error screen and halt.") Then AppendReason(CrashReason.Forge报错, If(RegexSeek(LogMc, "(?<=the game will display an error screen and halt[\s\S]+?Exception: )[\s\S]+?(?=\n\tat)"), "").TrimEnd(vbCrLf))
            If LogMc.Contains("A potential solution has been determined:") Then AppendReason(CrashReason.Fabric报错并给出解决方案, Join(RegexSearch(If(RegexSeek(LogMc, "(?<=A potential solution has been determined:\n)(\t - [^\n]+\n)+"), ""), "(?<=\t)[^\n]+"), vbLf))
            If LogMc.Contains("Maybe try a lower resolution resourcepack?") Then AppendReason(CrashReason.材质过大或显卡配置不足)
            If LogMc.Contains("java.lang.NoSuchMethodError: net.minecraft.world.server.ChunkManager$ProxyTicketManager.shouldForceTicks(J)Z") AndAlso LogMc.Contains("OptiFine") Then AppendReason(CrashReason.OptiFine导致无法加载世界)
            If LogMc.Contains("Could not reserve enough space") Then
                If LogMc.Contains("for 1048576KB object heap") Then
                    AppendReason(CrashReason.使用32位Java导致JVM无法分配足够多的内存)
                Else
                    AppendReason(CrashReason.内存不足)
                End If
            End If
            'Mod 重复安装
            If LogMc.Contains("DuplicateModsFoundException") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(LogMc, "(?<=\n\t[\w]+ : [A-Z]{1}:[^\n]+(/|\\))[^/\\\n]+?.jar", RegularExpressions.RegexOptions.IgnoreCase))
            If LogMc.Contains("Found a duplicate mod") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(If(RegexSeek(LogMc, "Found a duplicate mod[^\n]+"), ""), "[^\\/]+.jar", RegularExpressions.RegexOptions.IgnoreCase))
            If LogMc.Contains("ModResolutionException: Duplicate") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(If(RegexSeek(LogMc, "ModResolutionException: Duplicate[^\n]+"), ""), "[^\\/]+.jar", RegularExpressions.RegexOptions.IgnoreCase))
            '找不到或无法加载主类
            If (RegexCheck(LogMc, "^[^\n.]+.\w+.[^\n]+\n\[$") OrElse RegexCheck(LogMc, "^\[[^\]]+\] [^\n.]+.\w+.[^\n]+\n\[")) AndAlso
               Not (LogMc.Contains("at net.") OrElse LogMc.Contains("/INFO]")) AndAlso
               LogHs Is Nothing AndAlso LogCrash Is Nothing Then
                AppendReason(CrashReason.路径包含中文且存在编码问题导致找不到或无法加载主类)
            End If
            'Mod 导致的崩溃
            If LogMc.Contains("Mixin prepare failed ") OrElse LogMc.Contains("mixin.injection.throwables.InjectionError") OrElse LogMc.Contains(".mixins.json] FAILED during )") Then
                Dim ModId As String = RegexSeek(LogMc, "(?<=in )[^./ ]+(?=.mixins.json.+failed injection check)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= failed .+ in )[^./ ]+(?=.mixins.json)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= in config \[)[^./ ]+(?=.mixins.json\] FAILED during )")
                AppendReason(CrashReason.ModMixin失败, TryAnalyzeModName(If(ModId, "").TrimEnd((vbCrLf & " ").ToCharArray)))
            End If
            If LogMc.Contains("Caught exception from ") Then AppendReason(CrashReason.确定Mod导致游戏崩溃, TryAnalyzeModName(If(RegexSeek(LogMc, "[^\n]+?(?)"), "").TrimEnd((vbCrLf & " ").ToCharArray)))
            If LogMc.Contains("Failed to create mod instance.") Then AppendReason(CrashReason.Mod初始化失败, TryAnalyzeModName(If(RegexSeek(LogMc, "(?<=Failed to create mod instance. ModID: )[^,]+"), If(RegexSeek(LogMc, "(?<=Failed to create mod instance. ModId )[^\n]+(?= for )"), "")).TrimEnd(vbCrLf)))
        End If

        '虚拟机日志分析
        If LogHs IsNot Nothing Then
            If LogHs.Contains("The system is out of physical RAM or swap space") Then AppendReason(CrashReason.内存不足)
            If LogHs.Contains("Out of Memory Error") Then AppendReason(CrashReason.内存不足)
            If LogHs.Contains("EXCEPTION_ACCESS_VIOLATION") Then
                If LogHs.Contains("# C  [ig") Then AppendReason(CrashReason.Intel驱动不兼容导致EXCEPTION_ACCESS_VIOLATION)
                If LogHs.Contains("# C  [atio") Then AppendReason(CrashReason.AMD驱动不兼容导致EXCEPTION_ACCESS_VIOLATION)
                If LogHs.Contains("# C  [nvoglv") Then AppendReason(CrashReason.Nvidia驱动不兼容导致EXCEPTION_ACCESS_VIOLATION)
            End If
        End If

        '崩溃报告分析
        If LogCrash IsNot Nothing Then
            If LogCrash.Contains("maximum id range exceeded") Then AppendReason(CrashReason.Mod过多导致超出ID限制)
            If LogCrash.Contains("Entity being rendered") AndAlso LogCrash.Contains(vbTab & "Entity's Exact location: ") Then AppendReason(CrashReason.特定实体导致崩溃, If(RegexSeek(LogCrash, "(?<=\tEntity Type: )[^\n]+(?= \()"), "") & " (" & If(RegexSeek(LogCrash, "(?<=\tEntity's Exact location: )[^\n]+"), "").TrimEnd(vbCrLf.ToCharArray) & ")")
            If LogCrash.Contains("java.lang.OutOfMemoryError") Then AppendReason(CrashReason.内存不足)
            If LogCrash.Contains("Pixel format not accelerated") Then AppendReason(CrashReason.显卡驱动不支持导致无法设置像素格式)
            If LogCrash.Contains("Manually triggered debug crash") Then AppendReason(CrashReason.玩家手动触发调试崩溃)
            'Mod 导致的崩溃
            If LogCrash.Contains("-- MOD ") Then
                Dim LogLeft = LogCrash.Split("-- MOD").Last
                If LogLeft.Contains("Failure message: MISSING") Then
                    AppendReason(CrashReason.确定Mod导致游戏崩溃, If(RegexSeek(LogCrash, "(?<=Mod File: ).+"), "").TrimEnd((vbCrLf & " ").ToCharArray))
                Else
                    AppendReason(CrashReason.Mod加载器报错, If(RegexSeek(LogCrash, "(?<=Failure message: )[\w\W]+?(?=\tMod)"), "").Replace(vbTab, " ").TrimEnd((vbCrLf & " ").ToCharArray))
                End If
            End If
            If LogCrash.Contains("Multiple entries with same key: ") Then AppendReason(CrashReason.确定Mod导致游戏崩溃, TryAnalyzeModName(If(RegexSeek(LogCrash, "(?<=Multiple entries with same key: )[^=]+"), "").TrimEnd((vbCrLf & " ").ToCharArray)))
            If LogCrash.Contains("LoaderExceptionModCrash: Caught exception from ") Then AppendReason(CrashReason.确定Mod导致游戏崩溃, TryAnalyzeModName(If(RegexSeek(LogCrash, "(?<=LoaderExceptionModCrash: Caught exception from )[^\n]+"), "").TrimEnd((vbCrLf & " ").ToCharArray)))
            If LogCrash.Contains("Failed loading config file ") Then AppendReason(CrashReason.Mod配置文件导致游戏崩溃, {TryAnalyzeModName(If(RegexSeek(LogCrash, "(?<=Failed loading config file .+ for modid )[^\n]+"), "").TrimEnd(vbCrLf)).First, If(RegexSeek(LogCrash, "(?<=Failed loading config file ).+(?= of type)"), "").TrimEnd(vbCrLf)})
        End If

    End Sub
    ''' <summary>
    ''' 进行精准日志匹配。匹配优先级低于堆栈分析的崩溃。
    ''' </summary>
    Private Sub AnalyzeCrit2()

        '游戏日志分析
        If LogMc IsNot Nothing Then
            If LogMc.Contains("]: Warnings were found!") Then AppendReason(CrashReason.Fabric报错, If(RegexSeek(LogMc, "(?<=\]: Warnings were found! ?[\n]+)[\w\W]+?(?=[\n]+\[)"), "").Trim(vbCrLf.ToCharArray))
        End If

        '崩溃报告分析
        If LogCrash IsNot Nothing Then
            If LogCrash.Contains(vbTab & "Block location: World: ") Then AppendReason(CrashReason.特定方块导致崩溃, If(RegexSeek(LogCrash, "(?<=\tBlock: Block\{)[^\}]+"), "") & " " & If(RegexSeek(LogCrash, "(?<=\tBlock location: World: )\([^\)]+\)"), ""))
            If LogCrash.Contains(vbTab & "Entity's Exact location: ") Then AppendReason(CrashReason.特定实体导致崩溃, If(RegexSeek(LogCrash, "(?<=\tEntity Type: )[^\n]+(?= \()"), "") & " (" & If(RegexSeek(LogCrash, "(?<=\tEntity's Exact location: )[^\n]+"), "").TrimEnd(vbCrLf.ToCharArray) & ")")
        End If

    End Sub
    ''' <summary>
    ''' 从堆栈中提取 Mod ID 关键字。若失败则返回空列表。
    ''' </summary>
    Private Function AnalyzeStackKeyword(ErrorStack As String) As List(Of String)

        '进行正则匹配
        Dim StackSearchResults As List(Of String) = RegexSearch(If(ErrorStack, "") & vbCrLf, "(?<=\n[^{]+)[a-zA-Z]+\w+\.[a-zA-Z]+[\w\.]+(?=\.[\w\.$]+\.)")

        '检查堆栈开头
        Dim PossibleStacks As New List(Of String)
        For Each Stack As String In StackSearchResults
            'If Not Stack.Contains(".") Then Continue For
            For Each IgnoreStack In {
                "java", "sun", "javax", "jdk",
                "org.lwjgl", "com.sun", "net.minecraftforge", "com.mojang", "net.minecraft", "cpw.mods", "com.google", "org.apache", "org.spongepowered", "net.fabricmc", "com.mumfrey",
                "com.electronwill.nightconfig",
                "MojangTricksIntelDriversForPerformance_javaw"}
                If Stack.StartsWith(IgnoreStack) Then GoTo NextStack
            Next
            PossibleStacks.Add(Stack.Trim) '.Split("$").First)
NextStack:
        Next
        PossibleStacks = ArrayNoDouble(PossibleStacks)
        Log("[Crash] 找到 " & PossibleStacks.Count & " 条可能的堆栈信息")
        If PossibleStacks.Count = 0 Then Return New List(Of String)
        For Each Stack As String In PossibleStacks
            Log("[Crash]  - " & Stack)
        Next

        '检查堆栈关键词
        Dim PossibleWords As New List(Of String)
        For Each Stack As String In PossibleStacks
            Dim Splited = Stack.Split(".")
            For i = 0 To Math.Min(3, Splited.Count - 1) '最多取前 4 节
                Dim Word As String = Splited(i)
                If Word.Length <= 2 OrElse Word.StartsWith("func_") Then Continue For
                If {"com", "org", "net", "asm", "fml", "mod", "jar", "sun", "lib", "map", "gui", "dev", "nio", "api",
                    "core", "init", "mods", "main", "file", "game", "load", "read", "done", "util", "tile", "item", "base",
                    "forge", "setup", "block", "model", "mixin", "event",
                    "common", "server", "config", "loader", "launch", "entity", "assist", "client", "modapi", "mojang", "shader", "events", "github",
                    "preinit", "preload", "machine", "reflect", "channel", "general", "handler",
                    "optifine", "minecraft", "transformers", "universal", "internal", "multipart", "minecraftforge", "override"
                   }.Contains(Word.ToLower) Then Continue For
                PossibleWords.Add(Word.Trim)
            Next
        Next
        PossibleWords = ArrayNoDouble(PossibleWords)
        Log("[Crash] 从堆栈信息中找到 " & PossibleWords.Count & " 个可能的 Mod ID 关键词")
        If PossibleWords.Count > 0 Then Log("[Crash]  - " & Join(PossibleWords, ", "))
        If PossibleWords.Count > 10 Then
            Log("[Crash] 关键词过多，考虑匹配出错，不纳入考虑")
            Return New List(Of String)
        Else
            Return PossibleWords
        End If

    End Function
    ''' <summary>
    ''' 根据 Mod 关键词与详细信息（崩溃报告第二部分）与，获取实际的 Mod 名称。
    ''' 若失败则返回 Nothing。
    ''' </summary>
    Private Function AnalyzeModName(Keywords As List(Of String)) As List(Of String)
        Dim ModFileNames As New List(Of String)

        '预处理关键词（分割括号）
        Dim RealKeywords As New List(Of String)
        For Each Keyword In Keywords
            For Each SubKeyword In Keyword.Split("(")
                RealKeywords.Add(SubKeyword.Trim(" )".ToCharArray))
            Next
        Next
        Keywords = RealKeywords

        '获取崩溃报告对应部分
        If LogCrash Is Nothing Then Return Nothing
        If Not LogCrash.Contains("A detailed walkthrough of the error") Then Return Nothing
        Dim Details As String = LogCrash.Replace("A detailed walkthrough of the error", "¨")
        Dim IsFabricDetail As Boolean = Details.Contains("Fabric Mods") '是否为 Fabric 信息格式
        If IsFabricDetail Then
            Details = Details.Replace("Fabric Mods", "¨")
            Log("[Crash] 检测到 Fabric Mod 信息格式")
        End If
        Details = Details.Split("¨").Last

        '[Forge] 获取所有包含 .jar 的行
        '[Fabric] 获取所有包含 Mod 信息的行
        Dim ModNameLines As New List(Of String)
        For Each Line In Details.Split(vbLf)
            If Line.ToLower.Contains(".jar") OrElse
               (IsFabricDetail AndAlso Line.StartsWith(vbTab & vbTab) AndAlso Not RegexCheck(Line, "\t\tfabric[\w-]*: Fabric")) Then ModNameLines.Add(Line)
        Next
        Log("[Crash] 找到 " & ModNameLines.Count & " 个可能的 Mod 项目行")

        '获取 Mod ID 与关键词的匹配行
        Dim HintLines As New List(Of String)
        For Each KeyWord As String In Keywords
            For Each ModString As String In ModNameLines
                Dim RealModString As String = ModString.ToLower.Replace("_", "")
                If Not RealModString.Contains(KeyWord.ToLower.Replace("_", "")) Then Continue For
                If RealModString.Contains("minecraft.jar") OrElse RealModString.Contains(" forge-") Then Continue For
                HintLines.Add(ModString.Trim(vbCrLf.ToCharArray))
                Exit For
            Next
        Next
        HintLines = ArrayNoDouble(HintLines)
        Log("[Crash] 找到 " & HintLines.Count & " 个可能的崩溃 Mod 匹配行")
        For Each ModName As String In HintLines
            Log("[Crash]  - " & ModName)
        Next

        '从 Mod 匹配行中提取 .jar 文件的名称
        For Each Line As String In HintLines
            Dim Name As String
            If IsFabricDetail Then
                Name = RegexSeek(Line, "(?<=: )[^\n]+(?= [^\n]+)")
            Else
                Name = RegexSeek(Line, "(?<=\()[^\t]+.jar(?=\))|(?<=(\t\t)|(\| ))[^\t\|]+.jar", RegularExpressions.RegexOptions.IgnoreCase)
            End If
            If Name IsNot Nothing Then ModFileNames.Add(Name)
        Next
        ModFileNames = ArrayNoDouble(ModFileNames)
        Log("[Crash] 找到 " & ModFileNames.Count & " 个可能的崩溃 Mod 文件名")
        For Each ModFileName As String In ModFileNames
            Log("[Crash]  - " & ModFileName)
        Next

        Return If(ModFileNames.Count = 0, Nothing, ModFileNames)
    End Function
    ''' <summary>
    ''' 尝试获取 Mod 名称，若失败则返回原关键字。
    ''' </summary>
    Private Function TryAnalyzeModName(Keywords As String) As List(Of String)
        Dim RawList As New List(Of String) From {Keywords}
        If String.IsNullOrEmpty(Keywords) Then Return RawList
        Return If(AnalyzeModName(RawList), RawList)
    End Function

    '4：根据原因输出信息
    Private OutputFiles As New List(Of String)
    ''' <summary>
    ''' 弹出崩溃弹窗，并指导导出崩溃报告。
    ''' </summary>
    Public Sub Output(IsHandAnalyze As Boolean, Optional ExtraFiles As List(Of String) = Nothing)
        '弹窗提示
        FrmMain.ShowWindowToTop()
        Select Case MyMsgBox(GetAnalyzeResult(IsHandAnalyze), If(IsHandAnalyze, "错误报告分析结果", "Minecraft 出现错误"), "确定", If(IsHandAnalyze, "", "导出错误报告"))
            Case 2
                Dim FileAddress As String = Nothing
                Try
                    '获取文件路径
                    RunInUiWait(Sub() FileAddress = SelectAs("选择保存位置", "错误报告-" & Date.Now.ToString("G").Replace("/", "-").Replace(":", ".").Replace(" ", "_") & ".zip", "Minecraft 错误报告(*.zip)|*.zip"))
                    If String.IsNullOrEmpty(FileAddress) Then Exit Sub
                    Directory.CreateDirectory(GetPathFromFullPath(FileAddress))
                    If File.Exists(FileAddress) Then File.Delete(FileAddress)
                    '输出诊断信息
                    FeedbackInfo()
                    LogFlush()
                    '复制文件
                    If ExtraFiles IsNot Nothing Then OutputFiles.AddRange(ExtraFiles)
                    For Each OutputFile In OutputFiles
                        Try
                            Dim FileName As String = GetFileNameFromPath(OutputFile)
                            Select Case FileName
                                Case "LatestLaunch.bat"
                                    FileName = "启动脚本.bat"
                                Case "Log1.txt"
                                    FileName = "PCL2 启动器日志.txt"
                                Case "RawOutput.log"
                                    FileName = "游戏崩溃前的输出.txt"
                            End Select
                            File.Copy(OutputFile, TempFolder & "Report\" & FileName, True)
                        Catch ex As Exception
                            Log(ex, "复制错误报告文件失败（" & OutputFile & "）")
                        End Try
                    Next
                    '导出报告
                    Compression.ZipFile.CreateFromDirectory(TempFolder & "Report\", FileAddress)
                    DeleteDirectory(TempFolder & "Report\")
                    Hint("错误报告已导出！", HintType.Finish)
                Catch ex As Exception
                    Log(ex, "导出错误报告失败", LogLevel.Feedback)
                    Exit Sub
                End Try
                Try
                    Process.Start("explorer", "/select," & FileAddress)
                Catch ex As Exception
                    Log(ex, "打开错误报告的存放文件夹失败")
                End Try
        End Select
    End Sub
    ''' <summary>
    ''' 获取崩溃分析的结果描述。
    ''' </summary>
    Private Function GetAnalyzeResult(IsHandAnalyze As Boolean) As String

        '非一个结果的处理
        Dim ResultString As String
        If CrashReasons.Count = 0 Then
            If IsHandAnalyze Then
                Return "很抱歉，PCL2 无法确定错误原因。"
            Else
                Return "很抱歉，你的游戏出现了一些问题……" & vbCrLf &
                       "如果要寻求帮助，请向他人发送错误报告文件，而不是发送这个窗口的截图。" & vbCrLf &
                       "你也可以查看错误报告，其中可能会有出错的原因。"
            End If
        ElseIf CrashReasons.Count >= 2 Then
            Hint("错误分析时发现数个可能的原因，窗口中仅显示了第一条，你可以在启动器日志中检查更多可能的原因！", HintType.Finish)
        End If

        '根据不同原因判断
        Dim Additional As List(Of String) = CrashReasons.First.Value
        Select Case CrashReasons.First.Key
            Case CrashReason.Mod文件被解压
                ResultString = "由于 Mod 文件被解压了，导致游戏无法继续运行。\n直接把整个 Mod 文件放进 Mod 文件夹中即可，若解压就会导致游戏出错。\n\n请删除 Mod 文件夹中已被解压的 Mod，然后再启动游戏。"
            Case CrashReason.内存不足
                ResultString = "Minecraft 内存不足，导致其无法继续运行。\n这很可能是由于你为游戏分配的内存不足，或是游戏的配置要求过高。\n\n你可以在启动设置中增加为游戏分配的内存，删除配置要求较高的材质、Mod、光影。\n如果这依然不奏效，请在开始游戏前尽量关闭其他软件，或者……换台电脑？\h"
            Case CrashReason.使用OpenJ9
                ResultString = "游戏因为使用 Open J9 而崩溃了。\n请在启动设置的 Java 选择一项中改用非 OpenJ9 的 Java 8，然后再启动游戏。\n如果你没有安装 JRE 8，你可以从网络中下载、安装一个。"
            Case CrashReason.使用JDK
                ResultString = "游戏似乎因为使用 JDK，或 Java 版本过高而崩溃了。\n请在启动设置的 Java 选择一项中改用 JRE 8（Java 8），然后再启动游戏。\n如果你没有安装 JRE 8，你可以从网络中下载、安装一个。"
            Case CrashReason.Java版本过高
                ResultString = "游戏似乎因为你所使用的 Java 版本过高而崩溃了。\n请在启动设置的 Java 选择一项中改用 JRE 8（Java 8），然后再启动游戏。\n如果你没有安装 JRE 8，你可以从网络中下载、安装一个。"
            Case CrashReason.使用32位Java导致JVM无法分配足够多的内存
                If Environment.Is64BitOperatingSystem Then
                    ResultString = "你似乎正在使用 32 位 Java，这会导致 Minecraft 无法使用 1GB 以上的内存，进而造成崩溃。\n\n请在启动设置的 Java 选择一项中改用 64 位的 Java 再启动游戏，然后再启动游戏。\n如果你没有安装 64 位的 Java，你可以从网络中下载、安装一个。"
                Else
                    ResultString = "你正在使用 32 位的操作系统，这会导致 Minecraft 无法使用 1GB 以上的内存，进而造成崩溃。\n\n你或许只能重装 64 位的操作系统来解决此问题。\n如果你的电脑内存在 2GB 以内，那或许只能换台电脑了……\h"
                End If
            Case CrashReason.崩溃日志堆栈分析发现关键字, CrashReason.MC日志堆栈分析发现关键字
                If Additional.Count = 1 Then
                    ResultString = "你的游戏遇到了一些问题，这可能是某些 Mod 所引起的，PCL2 找到了一个可疑的关键词：" & Additional.First & "。\n\n如果你知道它对应的 Mod，那么有可能就是它引起的错误，你也可以查看错误报告获取详情。\h"
                Else
                    ResultString = "你的游戏遇到了一些问题，这可能是某些 Mod 所引起的，PCL2 找到了以下可疑的关键词：\n - " & Join(Additional, ", ") & "\n\n如果你知道这些关键词对应的 Mod，那么有可能就是它引起的错误，你也可以查看错误报告获取详情。\h"
                End If
            Case CrashReason.崩溃日志堆栈分析发现Mod名称
                If Additional.Count = 1 Then
                    ResultString = "名为 " & Additional.First & " 的 Mod 可能导致了游戏出错。\n\e\h"
                Else
                    ResultString = "可能是以下 Mod 导致了游戏出错：\n - " & Join(Additional, "\n - ") & "\n\e\h"
                End If
            Case CrashReason.确定Mod导致游戏崩溃
                If Additional.Count = 1 Then
                    ResultString = "名称或 ID 为 " & Additional.First & " 的 Mod 导致了游戏出错。\n\e\h"
                Else
                    ResultString = "以下 Mod 导致了游戏出错：\n - " & Join(Additional, "\n - ") & "\n\e\h"
                End If
            Case CrashReason.ModMixin失败
                If Additional.Count = 1 Then
                    ResultString = "名称或 ID 为 " & Additional.First & " 的 Mod 注入失败，导致游戏出错。\n这一般代表着该 Mod 存在 Bug，或与当前环境不兼容。\n\e\h"
                Else
                    ResultString = "以下 Mod 导致了游戏出错：\n - " & Join(Additional, "\n - ") & "\n这一般代表着这些 Mod 存在 Bug，或与当前环境不兼容。\n\e\h"
                End If
            Case CrashReason.Mod配置文件导致游戏崩溃
                If Additional(1) Is Nothing Then
                    ResultString = "名称或 ID 为 " & Additional.First & " 的 Mod 导致了游戏出错。\n\e\h"
                Else
                    ResultString = "名称或 ID 为 " & Additional.First & " 的 Mod 导致了游戏出错：\n其配置文件 " & Additional(1) & " 存在异常，无法读取。"
                End If
            Case CrashReason.Mod初始化失败
                If Additional.Count = 1 Then
                    ResultString = "名为 " & Additional.First & " 的 Mod 初始化失败，导致游戏无法继续加载。\n\e\h"
                Else
                    ResultString = "以下 Mod 初始化失败，导致游戏无法继续加载：\n - " & Join(Additional, "\n - ") & "\n\e\h"
                End If
            Case CrashReason.特定方块导致崩溃
                If Additional.Count = 1 Then
                    ResultString = "游戏似乎因为方块 " & Additional.First & " 出现了问题。\n\n你可以创建一个新世界，并观察游戏的运行情况：\n - 若正常运行，则是该方块导致出错，你或许需要使用一些方式删除此方块。\n - 若仍然出错，问题就可能来自其他原因……\h"
                Else
                    ResultString = "游戏似乎因为世界中的某些方块出现了问题。\n\n你可以创建一个新世界，并观察游戏的运行情况：\n - 若正常运行，则是某些方块导致出错，你或许需要删除该世界。\n - 若仍然出错，问题就可能来自其他原因……\h"
                End If
            Case CrashReason.Mod重复安装
                If Additional.Count >= 2 Then
                    ResultString = "你重复安装了多个相同的 Mod：\n - " & Join(Additional, "\n - ") & "\n\n每个 Mod 只能出现一次，请删除重复的 Mod，然后再启动游戏。"
                Else
                    ResultString = "你可能重复安装了多个相同的 Mod，导致游戏无法继续加载。\n\n每个 Mod 只能出现一次，请删除重复的 Mod，然后再启动游戏。\e\h"
                End If
            Case CrashReason.特定实体导致崩溃
                If Additional.Count = 1 Then
                    ResultString = "游戏似乎因为实体 " & Additional.First & " 出现了问题。\n\n你可以创建一个新世界，并生成一个该实体，然后观察游戏的运行情况：\n - 若正常运行，则是该实体导致出错，你或许需要使用一些方式删除此实体。\n - 若仍然出错，问题就可能来自其他原因……\h"
                Else
                    ResultString = "游戏似乎因为世界中的某些实体出现了问题。\n\n你可以创建一个新世界，并生成各种实体，观察游戏的运行情况：\n - 若正常运行，则是某些实体导致出错，你或许需要删除该世界。\n - 若仍然出错，问题就可能来自其他原因……\h"
                End If
            Case CrashReason.低版本Forge与高版本Java不兼容
                ResultString = "由于低版本 Forge 与当前 Java 不兼容，导致了游戏崩溃。\n\n请尝试以下解决方案：\n - 更新 Forge 到 36.2.26 或更高版本\n - 换用版本低于 8.0.320 的 Java"
            Case CrashReason.版本Json中存在多个Forge
                ResultString = "可能由于使用其他启动器修改了 Forge 版本，当前版本的文件存在异常，导致了游戏崩溃。\n请尝试重新全新安装 Forge，而非使用其他启动器修改 Forge 版本。"
            Case CrashReason.玩家手动触发调试崩溃
                ResultString = "* 事实上，你的游戏没有任何问题，这是你自己触发的崩溃。\n* 你难道没有更重要的事要做吗？"
            Case CrashReason.路径包含中文且存在编码问题导致找不到或无法加载主类
                ResultString = "由于游戏路径含有中文字符，且 Java 或系统编码存在错误，导致游戏无法运行。\n\n解决这一问题的最简单方法是检查游戏的完整路径，并删除各个文件夹名中的中文字符。\n这包括了游戏的版本名、它所处的 .minecraft 文件夹及其之前的文件夹名。\h"
            Case CrashReason.OptiFine导致无法加载世界 'https://www.minecraftforum.net/forums/support/java-edition-support/3051132-exception-ticking-world
                ResultString = "你所使用的 OptiFine 可能导致了你的游戏出现问题。\n\n该问题只在特定 OptiFine 版本中出现，你可以尝试更换 OptiFine 的版本。\h"
            Case CrashReason.显卡驱动不支持导致无法设置像素格式, CrashReason.Intel驱动不兼容导致EXCEPTION_ACCESS_VIOLATION, CrashReason.AMD驱动不兼容导致EXCEPTION_ACCESS_VIOLATION, CrashReason.Nvidia驱动不兼容导致EXCEPTION_ACCESS_VIOLATION, CrashReason.显卡不支持OpenGL
                If LogAll.Contains("hd graphics ") Then
                    ResultString = "你的显卡驱动存在问题，或未使用独立显卡，导致游戏无法正常运行。\n\n如果你的电脑存在独立显卡，请使用独立显卡而非 Intel 核显启动 PCL2 与 Minecraft。\n如果问题依然存在，请尝试升级你的显卡驱动到最新版本，或回退到出厂版本。\n如果还是不行，还可以尝试使用 8.0.51 或更低版本的 Java。\h"
                Else
                    ResultString = "你的显卡驱动存在问题，导致游戏无法正常运行。\n\n请尝试升级你的显卡驱动到最新版本，或回退到出厂版本，然后再启动游戏。\n如果还是不行，可以尝试使用 8.0.51 或更低版本的 Java。\n如果问题依然存在，那么你可能需要换个更好的显卡……\h"
                End If
            Case CrashReason.材质过大或显卡配置不足
                ResultString = "你所使用的材质分辨率过高，或显卡配置不足，导致游戏无法继续运行。\n\n如果你正在使用高清材质，请将它移除。\n如果你没有使用材质，那么你可能需要更新显卡驱动，或者换个更好的显卡……\h"
            Case CrashReason.光影或资源包导致OpenGL1282错误
                ResultString = "你所使用的光影或材质导致游戏出现了一些问题……\n\n请尝试删除你所添加的这些额外资源。\h"
            Case CrashReason.Mod过多导致超出ID限制
                ResultString = "你所安装的 Mod 过多，超出了游戏的 ID 限制，导致了游戏崩溃。\n请尝试安装 JEID 等修复 Mod，或删除部分大型 Mod。"
            Case CrashReason.文件或内容校验失败
                ResultString = "部分文件或内容校验失败，导致游戏出现了问题。\n\n请尝试删除游戏（包括 Mod）并重新下载，或尝试在重新下载时使用 VPN。\h"
            Case CrashReason.Fabric报错
                If Additional.Count = 1 Then
                    ResultString = "Fabric 提供了以下错误信息：\n" & Additional.First & "\n\n请根据上述信息进行对应处理，如果看不懂英文可以使用翻译软件。"
                Else
                    ResultString = "Fabric 可能已经提供了错误信息，请根据错误报告中的日志信息进行对应处理，如果看不懂英文可以使用翻译软件。\n如果没有看到报错信息，可以查看错误报告了解错误具体是如何发生的。\h"
                End If
            Case CrashReason.Mod加载器报错
                If Additional.Count = 1 Then
                    ResultString = "Mod 加载器提供了以下错误信息：\n" & Additional.First & "\n\n请根据上述信息进行对应处理，如果看不懂英文可以使用翻译软件。"
                Else
                    ResultString = "Mod 加载器可能已经提供了错误信息，请根据错误报告中的日志信息进行对应处理，如果看不懂英文可以使用翻译软件。\n如果没有看到报错信息，可以查看错误报告了解错误具体是如何发生的。\h"
                End If
            Case CrashReason.Fabric报错并给出解决方案
                If Additional.Count = 1 Then
                    ResultString = "Fabric 提供了以下解决方案：\n" & Additional.First & "\n\n请根据上述信息进行对应处理，如果看不懂英文可以使用翻译软件。"
                Else
                    ResultString = "Fabric 可能已经提供了解决方案，请根据错误报告中的日志信息进行对应处理，如果看不懂英文可以使用翻译软件。\n如果没有看到报错信息，可以查看错误报告了解错误具体是如何发生的。\h"
                End If
            Case CrashReason.Forge报错
                If Additional.Count = 1 Then
                    ResultString = "Forge 提供了以下错误信息：\n" & Additional.First & "\n\n请根据上述信息进行对应处理，如果看不懂英文可以使用翻译软件。"
                Else
                    ResultString = "Forge 可能已经提供了错误信息，请根据错误报告中的日志信息进行对应处理，如果看不懂英文可以使用翻译软件。\n如果没有看到报错信息，可以查看错误报告了解错误具体是如何发生的。\h"
                End If
            Case CrashReason.没有可用的分析文件
                ResultString = "你的游戏出现了一些问题，但 PCL2 未能找到相关记录文件，因此无法进行分析。\h"
            Case Else
                ResultString = "PCL2 获取到了没有详细信息的错误原因（" & CrashReasons.First.Key & "），请向 PCL2 作者提交反馈以获取详情。\h"
        End Select

        ResultString = ResultString.
            Replace("\n", vbCrLf).
            Replace("\h", If(IsHandAnalyze, "", vbCrLf & "如果要寻求帮助，请向他人发送错误报告文件，而不是发送这个窗口的截图。")).
            Replace("\e", If(IsHandAnalyze, "", vbCrLf & "你可以查看错误报告了解错误具体是如何发生的。")).
            Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr, vbCrLf)
        Return ResultString.Trim(vbCrLf.ToCharArray)
    End Function

End Class
