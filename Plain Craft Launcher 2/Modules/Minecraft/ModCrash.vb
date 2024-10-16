﻿Public Class CrashAnalyzer
    Private Shared IsAnalyzeCacheCleared As Boolean = False
    Private Shared IsAnalyzeCacheClearedLock As New Object

    '构造函数
    Private TempFolder As String
    Public Sub New(UUID As Integer)
        '清理缓存
        SyncLock IsAnalyzeCacheClearedLock
            If Not IsAnalyzeCacheCleared Then
                Try
                    DeleteDirectory(PathTemp & "CrashAnalyzer\")
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
    ''' <param name="LatestLog">从 PCL 捕获到的最后 200 行程序输出。</param>
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
        PossibleLogs = PossibleLogs.Distinct.ToList

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
        If Not RightLogs.Any() Then Log("[Crash] 未发现可能可用的日志文件")

        '将可能可用的日志文件导出
        For Each FilePath In RightLogs
            Try
                If FilePath.Contains("crash-") Then
                    AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(FilePath, ReadFile(FilePath).Split(vbCrLf.ToCharArray)))
                Else
                    AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(FilePath, ReadFile(FilePath, Encoding.UTF8).Split(vbCrLf.ToCharArray)))
                End If
            Catch ex As Exception
                Log(ex, "读取可能的崩溃日志文件失败（" & FilePath & "）")
            End Try
        Next
        If LatestLog IsNot Nothing AndAlso LatestLog.Any Then
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

        '尝试视作压缩包解压
        Try
            Dim Info As New FileInfo(FilePath)
            If Info.Exists AndAlso Info.Length > 0 AndAlso Not FilePath.EndsWithF(".jar", True) Then
                ExtractFile(FilePath, TempFolder & "Temp\")
                Log("[Crash] 已解压导入的日志文件：" & FilePath)
                GoTo Extracted
            End If
        Catch
        End Try
        '并非压缩包
        CopyFile(FilePath, TempFolder & "Temp\" & GetFileNameFromPath(FilePath))
        Log("[Crash] 已复制导入的日志文件：" & FilePath)
Extracted:

        '导入其中的日志文件
        For Each TargetFile As FileInfo In New DirectoryInfo(TempFolder & "Temp\").EnumerateFiles.ToList()
            Try
                If Not TargetFile.Exists OrElse TargetFile.Length = 0 Then Continue For
                Dim Ext As String = TargetFile.Extension.ToLower
                If Ext = ".log" OrElse Ext = ".txt" Then
                    If TargetFile.Name.StartsWithF("crash-") Then
                        AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(TargetFile.FullName, ReadFile(TargetFile.FullName).Split(vbCrLf.ToCharArray)))
                    Else
                        AnalyzeRawFiles.Add(New KeyValuePair(Of String, String())(TargetFile.FullName, ReadFile(TargetFile.FullName, Encoding.UTF8).Split(vbCrLf.ToCharArray)))
                    End If
                Else
                    File.Delete(TargetFile.FullName)
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
    Private DirectFile As KeyValuePair(Of String, String())? = Nothing '在弹窗中选择直接打开的文件
    ''' <summary>
    ''' 从 AnalyzeRawFiles 中提取实际有用的文本片段存储到 AnalyzeFiles，并整理可用于生成报告的文件。
    ''' 返回找到的用于分析的项目数。
    ''' </summary>
    Public Function Prepare() As Integer
        Log("[Crash] 步骤 2：准备日志文本")

        '对日志文件进行分类
        DirectFile = Nothing
        Dim TotalFiles As New List(Of KeyValuePair(Of AnalyzeFileType, KeyValuePair(Of String, String())))
        For Each LogFile In AnalyzeRawFiles
            Dim MatchName As String = GetFileNameFromPath(LogFile.Key).ToLower
            Dim TargetType As AnalyzeFileType
            If MatchName.StartsWithF("hs_err") Then
                TargetType = AnalyzeFileType.HsErr
                DirectFile = LogFile
            ElseIf MatchName.StartsWithF("crash-") Then
                TargetType = AnalyzeFileType.CrashReport
                DirectFile = LogFile
            ElseIf MatchName = "latest.log" OrElse MatchName = "latest log.txt" OrElse
                   MatchName = "debug.log" OrElse MatchName = "debug log.txt" OrElse
                   MatchName = "游戏崩溃前的输出.txt" OrElse MatchName = "rawoutput.log" Then
                TargetType = AnalyzeFileType.MinecraftLog
                If DirectFile Is Nothing Then DirectFile = LogFile
            ElseIf MatchName = "启动器日志.txt" OrElse MatchName = "PCL2 启动器日志.txt" OrElse MatchName = "PCL 启动器日志.txt" OrElse MatchName = "log1.txt" Then
                If LogFile.Value.Any(Function(s) s.Contains("以下为游戏输出的最后一段内容")) Then
                    TargetType = AnalyzeFileType.MinecraftLog
                    If DirectFile Is Nothing Then DirectFile = LogFile
                Else
                    TargetType = AnalyzeFileType.ExtraLog
                End If
            ElseIf MatchName.EndsWithF(".log", True) OrElse MatchName.EndsWithF(".txt", True) Then
                TargetType = AnalyzeFileType.ExtraLog
            Else
                Log("[Crash] " & MatchName & " 分类为 Ignore")
                Continue For
            End If
            If Not LogFile.Value.Any() Then
                Log("[Crash] " & MatchName & " 由于内容为空跳过")
            Else
                TotalFiles.Add(New KeyValuePair(Of AnalyzeFileType, KeyValuePair(Of String, String()))(TargetType, LogFile))
                Log("[Crash] " & MatchName & " 分类为 " & GetStringFromEnum(TargetType))
            End If
        Next

        '将分类后的文件分别写入
        For Each SelectType In {AnalyzeFileType.MinecraftLog, AnalyzeFileType.HsErr, AnalyzeFileType.ExtraLog, AnalyzeFileType.CrashReport}
            '获取该种类的所有文件 {文件路径 -> 文件内容行}
            Dim SelectedFiles As New List(Of KeyValuePair(Of String, String()))
            For Each File In TotalFiles
                If SelectType = File.Key Then SelectedFiles.Add(File.Value)
            Next
            If Not SelectedFiles.Any() Then Continue For
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
                        LogMcDebug = ""
                        '创建文件名词典
                        Dim FileNameDict As New Dictionary(Of String, KeyValuePair(Of String, String()))
                        For Each SelectedFile In SelectedFiles
                            FileNameDict(GetFileNameFromPath(SelectedFile.Key).ToLower) = SelectedFile
                            OutputFiles.Add(SelectedFile.Key)
                            Log("[Crash] 输出报告：" & SelectedFile.Key & "，作为 Minecraft 或启动器日志")
                        Next
                        '选择一份最佳的来自启动器的游戏日志
                        For Each FileName As String In {"rawoutput.log", "启动器日志.txt", "log1.txt", "游戏崩溃前的输出.txt", "PCL2 启动器日志.txt", "PCL 启动器日志.txt"}
                            If FileNameDict.ContainsKey(FileName) Then
                                Dim CurrentLog = FileNameDict(FileName)
                                '截取 “以下为游戏输出的最后一段内容” 后的内容
                                Dim HasLauncherMark As Boolean = False
                                For Each Line In CurrentLog.Value
                                    If HasLauncherMark Then
                                        LogMc += Line & vbLf
                                    ElseIf Line.Contains("以下为游戏输出的最后一段内容") Then
                                        HasLauncherMark = True
                                        Log("[Crash] 找到 PCL 输出的游戏实时日志头")
                                    End If
                                Next
                                '导入后 500 行
                                If Not HasLauncherMark Then LogMc += GetHeadTailLines(CurrentLog.Value, 0, 500)
                                LogMc = LogMc.TrimEnd(vbCrLf.ToCharArray)
                                Log("[Crash] 导入分析：" & CurrentLog.Key & "，作为启动器日志")
                                Exit For
                            End If
                        Next
                        '选择一份最佳的 Minecraft Log
                        For Each FileName As String In {"latest.log", "latest log.txt", "debug.log", "debug log.txt"}
                            If FileNameDict.ContainsKey(FileName) Then
                                Dim CurrentLog = FileNameDict(FileName)
                                LogMc += GetHeadTailLines(CurrentLog.Value, 250, 500)
                                Log("[Crash] 导入分析：" & CurrentLog.Key & "，作为 Minecraft 日志")
                                Exit For
                            End If
                        Next
                        '查找 Debug Log
                        For Each FileName As String In {"debug.log", "debug log.txt"}
                            If FileNameDict.ContainsKey(FileName) Then
                                Dim CurrentLog = FileNameDict(FileName)
                                LogMcDebug += GetHeadTailLines(CurrentLog.Value, 1000, 0)
                                Log("[Crash] 导入分析：" & CurrentLog.Key & "，作为 Minecraft Debug 日志")
                                Exit For
                            End If
                        Next
                        '检查错误
                        If LogMc = "" Then
                            LogMc = Nothing
                            Throw New Exception("无法找到匹配的 Minecraft Log")
                        End If
                        If LogMcDebug = "" Then LogMcDebug = Nothing
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
            Log(("[Crash] 步骤 2：准备日志文本完成，找到" & If(LogMc Is Nothing, "", "游戏日志、") & If(LogMcDebug Is Nothing, "", "游戏 Debug 日志、") & If(LogHs Is Nothing, "", "虚拟机日志、") & If(LogCrash Is Nothing, "", "崩溃日志、")).TrimEnd("、") & "用作分析")
        End If
        Return ResultCount

    End Function
    ''' <summary>
    ''' 输出字符串的前后某些行，并统一行尾为 vbLf (正则 \n)、删除空行和重复行。
    ''' </summary>
    Private Function GetHeadTailLines(Raw As String(), HeadLines As Integer, TailLines As Integer) As String
        If Raw.Length <= HeadLines + TailLines Then Return Join(Raw.Distinct, vbLf)
        Dim Lines As New List(Of String)
        Dim RealHeadLines As Integer = 0, ViewedLines As Integer
        For ViewedLines = 0 To Raw.Length - 1
            If Lines.Contains(Raw(ViewedLines)) Then Continue For
            RealHeadLines += 1
            Lines.Add(Raw(ViewedLines))
            If RealHeadLines >= HeadLines Then Exit For
        Next
        Dim RealTailLines = 0
        For i = Raw.Length - 1 To ViewedLines Step -1
            If Lines.Contains(Raw(i)) Then Continue For
            RealTailLines += 1
            Lines.Insert(RealHeadLines, Raw(i))
            If RealTailLines >= TailLines Then Exit For
        Next
        Dim Result As New StringBuilder
        For Each Line In Lines
            If Line = "" Then Continue For
            Result.Append(Line)
            Result.Append(vbLf)
        Next
        Return Result.ToString
    End Function

    '3：根据文本分析崩溃原因
    Private LogMc As String = Nothing, LogMcDebug As String = Nothing, LogHs As String = Nothing, LogCrash As String = Nothing
    Private LogAll As String
    '可能导致崩溃的原因与附加信息
    Private CrashReasons As New Dictionary(Of CrashReason, List(Of String))
    ''' <summary>
    ''' 导致崩溃的原因枚举。
    ''' </summary>
    Private Enum CrashReason
        Mod文件被解压
        MixinBootstrap缺失
        内存不足
        使用JDK
        显卡不支持OpenGL
        使用OpenJ9
        Java版本过高
        Java版本不兼容
        Mod名称包含特殊字符
        显卡驱动不支持导致无法设置像素格式
        极短的程序输出
        Intel驱动不兼容导致EXCEPTION_ACCESS_VIOLATION 'https://bugs.mojang.com/browse/MC-32606
        AMD驱动不兼容导致EXCEPTION_ACCESS_VIOLATION 'https://bugs.mojang.com/browse/MC-31618
        Nvidia驱动不兼容导致EXCEPTION_ACCESS_VIOLATION
        玩家手动触发调试崩溃
        光影或资源包导致OpenGL1282错误
        文件或内容校验失败
        确定Mod导致游戏崩溃
        怀疑Mod导致游戏崩溃
        Mod配置文件导致游戏崩溃
        ModMixin失败
        Mod加载器报错
        Mod初始化失败
        堆栈分析发现关键字
        堆栈分析发现Mod名称
        OptiFine导致无法加载世界 'https://www.minecraftforum.net/forums/support/java-edition-support/3051132-exception-ticking-world
        特定方块导致崩溃
        特定实体导致崩溃
        材质过大或显卡配置不足
        没有可用的分析文件
        使用32位Java导致JVM无法分配足够多的内存
        Mod重复安装
        OptiFine与Forge不兼容
        Fabric报错
        Fabric报错并给出解决方案
        Forge报错
        低版本Forge与高版本Java不兼容
        版本Json中存在多个Forge
        Mod过多导致超出ID限制
        NightConfig的Bug
        ShadersMod与OptiFine同时安装
        Forge安装不完整
        Mod需要Java11
        Mod缺少前置或MC版本错误
    End Enum
    ''' <summary>
    ''' 根据 AnalyzeLogs 与可能的版本信息分析崩溃原因。
    ''' </summary>
    Public Sub Analyze(Optional Version As McVersion = Nothing)
        Log("[Crash] 步骤 3：分析崩溃原因")
        LogAll = (If(LogMc, "") & If(LogMcDebug, "") & If(LogHs, "") & If(LogCrash, ""))

        '1. 精准日志匹配，中/高优先级
        AnalyzeCrit1()
        If CrashReasons.Any Then GoTo Done
        AnalyzeCrit2()
        If CrashReasons.Any Then GoTo Done

        '2. 堆栈分析
        If LogAll.Contains("orge") OrElse LogAll.Contains("abric") OrElse LogAll.Contains("uilt") OrElse LogAll.Contains("iteloader") Then
            Dim Keywords As New List(Of String)
            '崩溃日志
            If LogCrash IsNot Nothing Then
                Log("[Crash] 开始进行崩溃日志堆栈分析")
                Keywords.AddRange(AnalyzeStackKeyword(LogCrash.Before("System Details")))
            End If
            'Minecraft 日志
            If LogMc IsNot Nothing Then
                Dim Fatals As List(Of String) = RegexSearch(LogMc, "/FATAL] .+?(?=[\n]+\[)")
                If LogMc.Contains("Unreported exception thrown!") Then Fatals.Add(LogMc.Between("Unreported exception thrown!", "at oolloo.jlw.Wrapper"))
                Log("[Crash] 开始进行 Minecraft 日志堆栈分析，发现 " & Fatals.Count & " 个报错项")
                For Each Fatal In Fatals
                    Keywords.AddRange(AnalyzeStackKeyword(Fatal))
                Next
            End If
            '虚拟机日志
            If LogHs IsNot Nothing Then
                Log("[Crash] 开始进行虚拟机堆栈分析")
                Dim StackLogs As String = LogHs.Between("T H R E A D", "Registers:")
                Keywords.AddRange(AnalyzeStackKeyword(StackLogs))
            End If
            'Mod 名称分析
            If Keywords.Any Then
                Dim Names = AnalyzeModName(Keywords)
                If Names Is Nothing Then
                    AppendReason(CrashReason.堆栈分析发现关键字, Keywords)
                Else
                    AppendReason(CrashReason.堆栈分析发现Mod名称, Names)
                End If
                GoTo Done
            End If
        Else
            Log("[Crash] 可能并未安装 Mod，不进行堆栈分析")
        End If

        '3. 精准日志匹配，低优先级
        AnalyzeCrit3()

        '输出到日志
Done:
        If Not CrashReasons.Any() Then
            Log("[Crash] 步骤 3：分析崩溃原因完成，未找到可能的原因")
        Else
            Log("[Crash] 步骤 3：分析崩溃原因完成，找到 " & CrashReasons.Count & " 条可能的原因")
            For Each Reason In CrashReasons
                Log("[Crash]  - " & GetStringFromEnum(Reason.Key) & If(Reason.Value.Any, "（" & Join(Reason.Value, "；") & "）", ""))
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
                CrashReasons(Reason) = CrashReasons(Reason).Distinct.ToList
            End If
        Else
            CrashReasons.Add(Reason, New List(Of String)(If(Additional, {})))
        End If
        Log("[Crash] 可能的崩溃原因：" & GetStringFromEnum(Reason) & If(Additional IsNot Nothing AndAlso Additional.Any, "（" & Join(Additional, "；") & "）", ""))
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
            If LogMc.Contains("TRANSFORMER/net.optifine/net.optifine.reflect.Reflector.<clinit>(Reflector.java") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("java.lang.NoSuchMethodError: 'void net.minecraft.client.renderer.texture.SpriteContents.<init>") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("java.lang.NoSuchMethodError: 'java.lang.String com.mojang.blaze3d.systems.RenderSystem.getBackendDescription") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("java.lang.NoSuchMethodError: 'void net.minecraft.client.renderer.block.model.BakedQuad.<init>") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("java.lang.NoSuchMethodError: 'void net.minecraftforge.client.gui.overlay.ForgeGui.renderSelectedItemName") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("java.lang.NoSuchMethodError: 'void net.minecraft.server.level.DistanceManager") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("java.lang.NoSuchMethodError: 'net.minecraft.network.chat.FormattedText net.minecraft.client.gui.Font.ellipsize") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            If LogMc.Contains("Open J9 is not supported") OrElse LogMc.Contains("OpenJ9 is incompatible") OrElse LogMc.Contains(".J9VMInternals.") Then AppendReason(CrashReason.使用OpenJ9)
            If LogMc.Contains("java.lang.NoSuchFieldException: ucp") Then AppendReason(CrashReason.Java版本过高)
            If LogMc.Contains("because module java.base does not export") Then AppendReason(CrashReason.Java版本过高)
            If LogMc.Contains("java.lang.ClassNotFoundException: jdk.nashorn.api.scripting.NashornScriptEngineFactory") Then AppendReason(CrashReason.Java版本过高)
            If LogMc.Contains("java.lang.ClassNotFoundException: java.lang.invoke.LambdaMetafactory") Then AppendReason(CrashReason.Java版本过高)
            If LogMc.Contains("The directories below appear to be extracted jar files. Fix this before you continue.") Then AppendReason(CrashReason.Mod文件被解压)
            If LogMc.Contains("Extracted mod jars found, loading will NOT continue") Then AppendReason(CrashReason.Mod文件被解压)
            If LogMc.Contains("java.lang.ClassNotFoundException: org.spongepowered.asm.launch.MixinTweaker") Then AppendReason(CrashReason.MixinBootstrap缺失)
            If LogMc.Contains("Couldn't set pixel format") Then AppendReason(CrashReason.显卡驱动不支持导致无法设置像素格式)
            If LogMc.Contains("java.lang.OutOfMemoryError") OrElse LogMc.Contains("an out of memory error") Then AppendReason(CrashReason.内存不足)
            If LogMc.Contains("java.lang.RuntimeException: Shaders Mod detected. Please remove it, OptiFine has built-in support for shaders.") Then AppendReason(CrashReason.ShadersMod与OptiFine同时安装)
            If LogMc.Contains("java.lang.NoSuchMethodError: sun.security.util.ManifestEntryVerifier") Then AppendReason(CrashReason.低版本Forge与高版本Java不兼容)
            If LogMc.Contains("1282: Invalid operation") Then AppendReason(CrashReason.光影或资源包导致OpenGL1282错误)
            If LogMc.Contains("signer information does not match signer information of other classes in the same package") Then AppendReason(CrashReason.文件或内容校验失败, If(RegexSeek(LogMc, "(?<=class "")[^']+(?=""'s signer information)"), "").TrimEnd(vbCrLf))
            If LogMc.Contains("Maybe try a lower resolution resourcepack?") Then AppendReason(CrashReason.材质过大或显卡配置不足)
            If LogMc.Contains("java.lang.NoSuchMethodError: net.minecraft.world.server.ChunkManager$ProxyTicketManager.shouldForceTicks(J)Z") AndAlso LogMc.Contains("OptiFine") Then AppendReason(CrashReason.OptiFine导致无法加载世界)
            If LogMc.Contains("Unsupported class file major version") Then AppendReason(CrashReason.Java版本不兼容)
            If LogMc.Contains("com.electronwill.nightconfig.core.io.ParsingException: Not enough data available") Then AppendReason(CrashReason.NightConfig的Bug)
            If LogMc.Contains("Cannot find launch target fmlclient, unable to launch") Then AppendReason(CrashReason.Forge安装不完整)
            If LogMc.Contains("Invalid paths argument, contained no existing paths") AndAlso LogMc.Contains("libraries\net\minecraftforge\fmlcore") Then AppendReason(CrashReason.Forge安装不完整)
            If LogMc.Contains("Invalid module name: '' is not a Java identifier") Then AppendReason(CrashReason.Mod名称包含特殊字符)
            If LogMc.Contains("has been compiled by a more recent version of the Java Runtime (class file version 55.0), this version of the Java Runtime only recognizes class file versions up to") Then AppendReason(CrashReason.Mod需要Java11)
            If LogMc.Contains("java.lang.RuntimeException: java.lang.NoSuchMethodException: no such method: sun.misc.Unsafe.defineAnonymousClass(Class,byte[],Object[])Class/invokeVirtual") Then AppendReason(CrashReason.Mod需要Java11)
            If LogMc.Contains("java.lang.IllegalArgumentException: The requested compatibility level JAVA_11 could not be set. Level is not supported by the active JRE or ASM version") Then AppendReason(CrashReason.Mod需要Java11)
            If LogMc.Contains("Unsupported major.minor version") Then AppendReason(CrashReason.Java版本不兼容)
            If LogMc.Contains("Invalid maximum heap size") Then AppendReason(CrashReason.使用32位Java导致JVM无法分配足够多的内存)
            If LogMc.Contains("Could not reserve enough space") Then
                If LogMc.Contains("for 1048576KB object heap") Then
                    AppendReason(CrashReason.使用32位Java导致JVM无法分配足够多的内存)
                Else
                    AppendReason(CrashReason.内存不足)
                End If
            End If
            '确定的 Mod 导致崩溃
            If LogMc.Contains("Caught exception from ") Then AppendReason(CrashReason.确定Mod导致游戏崩溃, TryAnalyzeModName(RegexSeek(LogMc, "(?<=Caught exception from )[^\n]+?")?.TrimEnd((vbCrLf & " ").ToCharArray)))
            'Mod 重复安装
            If LogMc.Contains("DuplicateModsFoundException") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(LogMc, "(?<=\n\t[\w]+ : [A-Z]{1}:[^\n]+(/|\\))[^/\\\n]+?.jar", RegularExpressions.RegexOptions.IgnoreCase))
            If LogMc.Contains("Found a duplicate mod") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(If(RegexSeek(LogMc, "Found a duplicate mod[^\n]+"), ""), "[^\\/]+.jar", RegularExpressions.RegexOptions.IgnoreCase))
            If LogMc.Contains("Found duplicate mods") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(LogMc, "(?<=Mod ID: ')\w+?(?=' from mod files:)").Distinct.ToList)
            If LogMc.Contains("ModResolutionException: Duplicate") Then AppendReason(CrashReason.Mod重复安装, RegexSearch(If(RegexSeek(LogMc, "ModResolutionException: Duplicate[^\n]+"), ""), "[^\\/]+.jar", RegularExpressions.RegexOptions.IgnoreCase))
            'Mod 缺少前置
            If LogMc.Contains("Missing or unsupported mandatory dependencies:") Then
                AppendReason(CrashReason.Mod缺少前置或MC版本错误,
                    RegexSearch(LogMc, "(?<=Missing or unsupported mandatory dependencies:)([\n\r]+\t(.*))+", RegularExpressions.RegexOptions.IgnoreCase).
                    Select(Function(s) s.Trim((vbCrLf & vbTab & " ").ToCharArray)).Distinct().ToList())
            End If
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
            If LogCrash.Contains("java.lang.OutOfMemoryError") Then AppendReason(CrashReason.内存不足)
            If LogCrash.Contains("Pixel format not accelerated") Then AppendReason(CrashReason.显卡驱动不支持导致无法设置像素格式)
            If LogCrash.Contains("Manually triggered debug crash") Then AppendReason(CrashReason.玩家手动触发调试崩溃)
            If LogCrash.Contains("has mods that were not found") AndAlso RegexCheck(LogCrash, "The Mod File [^\n]+optifine\\OptiFine[^\n]+ has mods that were not found") Then AppendReason(CrashReason.OptiFine与Forge不兼容)
            'Mod 导致的崩溃
            If LogCrash.Contains("-- MOD ") Then
                If LogCrash.Between("-- MOD ", "Failure message:").ContainsF(".jar", True) Then
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
    ''' 进行精准日志匹配。匹配优先级高于堆栈分析的崩溃，但低于上面的。
    ''' 如果第一步已经找到了原因则不执行该检测。
    ''' </summary>
    Private Sub AnalyzeCrit2()

        '游戏日志分析
        If LogMc IsNot Nothing Then
            '常规信息
            If LogMc.Contains("An exception was thrown, the game will display an error screen and halt.") Then AppendReason(CrashReason.Forge报错, RegexSeek(LogMc, "(?<=the game will display an error screen and halt.[\n\r]+[^\n]+?Exception: )[\s\S]+?(?=\n\tat)")?.Trim(vbCrLf))
            If LogMc.Contains("A potential solution has been determined:") Then AppendReason(CrashReason.Fabric报错并给出解决方案, Join(RegexSearch(If(RegexSeek(LogMc, "(?<=A potential solution has been determined:\n)((\t)+ - [^\n]+\n)+"), ""), "(?<=(\t)+)[^\n]+"), vbLf))
            If LogMc.Contains("A potential solution has been determined, this may resolve your problem:") Then AppendReason(CrashReason.Fabric报错并给出解决方案, Join(RegexSearch(If(RegexSeek(LogMc, "(?<=A potential solution has been determined, this may resolve your problem:\n)((\t)+ - [^\n]+\n)+"), ""), "(?<=(\t)+)[^\n]+"), vbLf))
            If LogMc.Contains("确定了一种可能的解决方法，这样做可能会解决你的问题：") Then AppendReason(CrashReason.Fabric报错并给出解决方案, Join(RegexSearch(If(RegexSeek(LogMc, "(?<=确定了一种可能的解决方法，这样做可能会解决你的问题：\n)((\t)+ - [^\n]+\n)+"), ""), "(?<=(\t)+)[^\n]+"), vbLf))
            'Mixin 崩溃
            If LogMc.Contains("Mixin prepare failed ") OrElse LogMc.Contains("Mixin apply failed ") OrElse LogMc.Contains("MixinApplyError") OrElse
               LogMc.Contains("mixin.injection.throwables.") OrElse LogMc.Contains(".json] FAILED during )") Then
                Dim ModId As String = RegexSeek(LogMc, "(?<=in )[^./ ]+(?=.mixins.json.+failed injection check)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<=in mixins.)[^./ ]+(?=.json.+failed injection check)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= failed .+ in )[^./ ]+(?=.mixins.json)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= failed .+ in mixins.)[^./ ]+(?=.json)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= failed mixins.)[^./ ]+(?=.json:)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= in config \[)[^./ ]+(?=.mixins.json\] FAILED during )")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<= in config \[mixins.)[^./ ]+(?=.json\] FAILED during )")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<=from mod )[^./ ]+(?=\] from)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<=for mod )[^./ ]+(?= failed)")
                '兜底名称判断
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "[^./ \]]+(?=.mixins.json)")
                If ModId Is Nothing Then ModId = RegexSeek(LogMc, "(?<=mixins.)[^./ \]]+(?=.json)")
                AppendReason(CrashReason.ModMixin失败, TryAnalyzeModName(If(ModId, "").TrimEnd((vbCrLf & " ").ToCharArray)))
            Else
                '在 #3104 的情况下，这一句导致 OptiFabric 的 Mixin 失败错判为 Fabric Loader 加载失败
                If LogMc.Contains("due to errors, provided by ") Then AppendReason(CrashReason.确定Mod导致游戏崩溃, TryAnalyzeModName(If(RegexSeek(LogMc, "(?<=due to errors, provided by ')[^']+"), "").TrimEnd((vbCrLf & " ").ToCharArray)))
            End If
        End If

        '崩溃报告分析
        If LogCrash IsNot Nothing Then
            If LogCrash.Contains("Suspected Mod") Then
                Dim SuspectsRaw As String = LogCrash.Between("Suspected Mod", "Stacktrace")
                If Not SuspectsRaw.StartsWithF("s: None") Then 'Suspected Mods: None
                    Dim Suspects = RegexSearch(SuspectsRaw, "(?<=\n\t[^(\t]+\()[^)\n]+")
                    If Suspects.Any Then AppendReason(CrashReason.怀疑Mod导致游戏崩溃, TryAnalyzeModName(Suspects))
                End If
            End If
        End If

    End Sub
    ''' <summary>
    ''' 进行精准日志匹配。匹配优先级低于堆栈分析的崩溃。
    ''' </summary>
    Private Sub AnalyzeCrit3()

        '游戏日志分析
        If LogMc IsNot Nothing Then
            '极短的程序输出
            If Not (LogMc.Contains("at net.") OrElse LogMc.Contains("INFO]")) AndAlso LogHs Is Nothing AndAlso LogCrash Is Nothing AndAlso LogMc.Length < 100 Then
                AppendReason(CrashReason.极短的程序输出, LogMc)
            End If
            'Mixin 失败可以导致大量 Mod 实例创建失败
            If LogMc.Contains("Failed to create mod instance.") Then AppendReason(CrashReason.Mod初始化失败, TryAnalyzeModName(If(RegexSeek(LogMc, "(?<=Failed to create mod instance. ModID: )[^,]+"), If(RegexSeek(LogMc, "(?<=Failed to create mod instance. ModId )[^\n]+(?= for )"), "")).TrimEnd(vbCrLf)))
            '注意：Fabric 的 Warnings were found! 不一定是崩溃原因，它可能是单纯的警报
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
        ErrorStack = vbLf & If(ErrorStack, "") & vbLf

        '进行正则匹配
        Dim StackSearchResults As New List(Of String)
        StackSearchResults.AddRange(RegexSearch(ErrorStack, "(?<=\n[^{]+)[a-zA-Z_]+\w+\.[a-zA-Z_]+[\w\.]+(?=\.[\w\.$]+\.)"))
        StackSearchResults.AddRange(RegexSearch(ErrorStack, "(?<=at [^(]+?\.\w+\$\w+\$)[\w\$]+?(?=\$\w+\()").Select(Function(s) s.Replace("$", "."))) 'Mixin 堆栈：xxx.xxx.xxxx$xxxx$xxx
        StackSearchResults = StackSearchResults.Distinct.ToList

        '检查堆栈开头
        Dim PossibleStacks As New List(Of String)
        For Each Stack As String In StackSearchResults
            'If Not Stack.Contains(".") Then Continue For
            For Each IgnoreStack In {
                "java", "sun", "javax", "jdk", "oolloo",
                "org.lwjgl", "com.sun", "net.minecraftforge", "paulscode.sound", "com.mojang", "net.minecraft", "cpw.mods", "com.google", "org.apache", "org.spongepowered", "net.fabricmc", "com.mumfrey",
                "com.electronwill.nightconfig", "it.unimi.dsi",
                "MojangTricksIntelDriversForPerformance_javaw"}
                If Stack.StartsWithF(IgnoreStack) Then GoTo NextStack
            Next
            PossibleStacks.Add(Stack.Trim)
NextStack:
        Next
        PossibleStacks = PossibleStacks.Distinct.ToList
        Log("[Crash] 找到 " & PossibleStacks.Count & " 条可能的堆栈信息")
        If Not PossibleStacks.Any() Then Return New List(Of String)
        For Each Stack As String In PossibleStacks
            Log("[Crash]  - " & Stack)
        Next

        '检查堆栈关键词
        Dim PossibleWords As New List(Of String)
        For Each Stack As String In PossibleStacks
            Dim Splited = Stack.Split(".")
            For i = 0 To Math.Min(3, Splited.Count - 1) '最多取前 4 节
                Dim Word As String = Splited(i)
                If Word.Length <= 2 OrElse Word.StartsWithF("func_") Then Continue For
                If {"com", "org", "net", "asm", "fml", "mod", "jar", "sun", "lib", "map", "gui", "dev", "nio", "api", "dsi", "top", "mcp",
                    "core", "init", "mods", "main", "file", "game", "load", "read", "done", "util", "tile", "item", "base", "oshi", "impl", "data", "pool", "task",
                    "forge", "setup", "block", "model", "mixin", "event", "unimi", "netty", "world",
                    "gitlab", "common", "server", "config", "loader", "launch", "entity", "assist", "client", "plugin", "modapi", "mojang", "shader", "events", "github", "recipe", "render", "packet", "events",
                    "preinit", "preload", "machine", "reflect", "channel", "general", "handler", "content", "systems", "modules", "service",
                    "fastutil", "optifine", "internal", "platform", "override", "fabricmc", "neoforge",
                    "injection", "listeners", "scheduler", "minecraft", "transformer", "transformers", "neoforged", "universal", "multipart", "minecraftforge", "blockentity", "spongepowered", "electronwill"
                   }.Contains(Word.ToLower) Then Continue For
                PossibleWords.Add(Word.Trim)
            Next
        Next
        PossibleWords = PossibleWords.Distinct.ToList
        Log("[Crash] 从堆栈信息中找到 " & PossibleWords.Count & " 个可能的 Mod ID 关键词")
        If PossibleWords.Any Then Log("[Crash]  - " & Join(PossibleWords, ", "))
        If PossibleWords.Count > 10 Then
            Log("[Crash] 关键词过多，考虑匹配出错，不纳入考虑")
            Return New List(Of String)
        Else
            Return PossibleWords
        End If

    End Function
    ''' <summary>
    ''' 根据 Mod 关键词尝试获取实际的 Mod 名称。
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

        '从崩溃报告获取 Mod 信息
        If LogCrash IsNot Nothing AndAlso LogCrash.Contains("A detailed walkthrough of the error") Then
            Dim Details As String = LogCrash.Replace("A detailed walkthrough of the error", "¨")
            Dim IsFabricDetail As Boolean = Details.Contains("Fabric Mods") '是否为 Fabric 信息格式
            If IsFabricDetail Then
                Details = Details.Replace("Fabric Mods", "¨")
                Log("[Crash] 崩溃报告中检测到 Fabric Mod 信息格式")
            End If
            Details = Details.After("¨")

            '[Forge] 获取所有包含 .jar 的行
            '[Fabric] 获取所有包含 Mod 信息的行
            Dim ModNameLines As New List(Of String)
            For Each Line In Details.Split(vbLf)
                If (Line.ContainsF(".jar", True) AndAlso Line.Length - Line.Replace(".jar", "").Length = 4) OrElse '只有一个 .jar
                   (IsFabricDetail AndAlso Line.StartsWithF(vbTab & vbTab) AndAlso Not RegexCheck(Line, "\t\tfabric[\w-]*: Fabric")) Then ModNameLines.Add(Line)
            Next
            Log("[Crash] 崩溃报告中找到 " & ModNameLines.Count & " 个可能的 Mod 项目行")

            '获取 Mod ID 与关键词的匹配行
            Dim HintLines As New List(Of String)
            For Each KeyWord As String In Keywords
                For Each ModString As String In ModNameLines
                    Dim RealModString As String = ModString.ToLower.Replace("_", "")
                    If Not RealModString.Contains(KeyWord.ToLower.Replace("_", "")) Then Continue For
                    If RealModString.Contains("minecraft.jar") OrElse RealModString.Contains(" forge-") OrElse RealModString.Contains(" mixin-") Then Continue For
                    HintLines.Add(ModString.Trim(vbCrLf.ToCharArray))
                    Exit For
                Next
            Next
            HintLines = HintLines.Distinct.ToList
            Log("[Crash] 崩溃报告中找到 " & HintLines.Count & " 个可能的崩溃 Mod 匹配行")
            For Each ModLine As String In HintLines
                Log("[Crash]  - " & ModLine)
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

        End If

        '从 debug.log 获取 Mod 信息
        If LogMcDebug IsNot Nothing Then

            'Forge: Found valid mod file YungsBetterStrongholds-1.20-Forge-4.0.1.jar with {betterstrongholds} mods - versions {1.20-Forge-4.0.1}
            Dim ModNameLines As List(Of String) = RegexSearch(LogMcDebug, "(?<=valid mod file ).*", RegularExpressions.RegexOptions.Multiline)
            Log("[Crash] Debug 信息中找到 " & ModNameLines.Count & " 个可能的 Mod 项目行")

            '获取 Mod ID 与关键词的匹配行
            Dim HintLines As New List(Of String)
            For Each KeyWord As String In Keywords
                For Each ModString As String In ModNameLines
                    If ModString.Contains($"{{{KeyWord}}}") Then HintLines.Add(ModString)
                Next
            Next
            HintLines = HintLines.Distinct.ToList
            Log("[Crash] Debug 信息中找到 " & HintLines.Count & " 个可能的崩溃 Mod 匹配行")
            For Each ModLine As String In HintLines
                Log("[Crash]  - " & ModLine)
            Next

            '从 Mod 匹配行中提取 .jar 文件的名称
            For Each Line As String In HintLines
                Dim Name As String
                Name = RegexSeek(Line, ".*(?= with)")
                If Name IsNot Nothing Then ModFileNames.Add(Name)
            Next

        End If

        '输出
        ModFileNames = ModFileNames.Distinct.ToList
        If Not ModFileNames.Any() Then
            Return Nothing
        Else
            Log("[Crash] 找到 " & ModFileNames.Count & " 个可能的崩溃 Mod 文件名")
            For Each ModFileName As String In ModFileNames
                Log("[Crash]  - " & ModFileName)
            Next
            Return ModFileNames
        End If
    End Function
    ''' <summary>
    ''' 尝试从关键字获取 Mod 名称，若失败则返回原关键字。
    ''' </summary>
    Private Function TryAnalyzeModName(Keyword As String) As List(Of String)
        Dim RawList As New List(Of String) From {If(Keyword, "")}
        If String.IsNullOrEmpty(Keyword) Then Return RawList
        Return If(AnalyzeModName(RawList), RawList)
    End Function
    ''' <summary>
    ''' 尝试从关键字获取 Mod 名称，若失败则返回原关键字。
    ''' </summary>
    Private Function TryAnalyzeModName(Keywords As List(Of String)) As List(Of String)
        If Not Keywords.Any Then Return Keywords
        Return If(AnalyzeModName(Keywords), Keywords)
    End Function

    '4：根据原因输出信息
    Private OutputFiles As New List(Of String)
    ''' <summary>
    ''' 弹出崩溃弹窗，并指导导出崩溃报告。
    ''' </summary>
    Public Sub Output(IsHandAnalyze As Boolean, Optional ExtraFiles As List(Of String) = Nothing)
        '弹窗提示
        FrmMain.ShowWindowToTop()
        Select Case MyMsgBox(GetAnalyzeResult(IsHandAnalyze), If(IsHandAnalyze, GetLang("LangModCrashDialogTitleAnalysisResult"), GetLang("LangModCrashDialogTitleMcError")),
            GetLang("LangDialogBtnOK"), If(IsHandAnalyze OrElse DirectFile Is Nothing, "", GetLang("LangModCrashViewLog")), If(IsHandAnalyze, "", GetLang("LangModCrashExportCrashReport")),
            Button2Action:=If(IsHandAnalyze OrElse DirectFile Is Nothing, Nothing,
            Sub()
                '弹窗选择：查看日志
                If File.Exists(DirectFile.Value.Key) Then
                    ShellOnly("notepad", DirectFile.Value.Key)
                Else
                    Dim FilePath As String = PathTemp & "Crash.txt"
                    WriteFile(FilePath, Join(DirectFile.Value.Value, vbCrLf))
                    ShellOnly(FilePath)
                End If
            End Sub))
            Case 3
                '弹窗选择：导出错误报告
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
                        Dim FileName As String = GetFileNameFromPath(OutputFile)
                        Select Case FileName
                            Case "LatestLaunch.bat"
                                FileName = "启动脚本.bat"
                            Case "Log1.txt"
                                FileName = "PCL 启动器日志.txt"
                            Case "RawOutput.log"
                                FileName = "游戏崩溃前的输出.txt"
                        End Select
                        If File.Exists(OutputFile) Then
                            Dim FileEncoding As Encoding = GetEncoding(ReadFileBytes(OutputFile))
                            WriteFile(TempFolder & "Report\" & FileName,
                                      SecretFilter(ReadFile(OutputFile, FileEncoding), If(FileName = "启动脚本.bat", "F", "*")),
                                      Encoding:=FileEncoding)
                            Log($"[Crash] 导出文件：{FileName}，编码：{FileEncoding.HeaderName}")
                        End If
                    Next
                    '导出报告
                    Compression.ZipFile.CreateFromDirectory(TempFolder & "Report\", FileAddress)
                    DeleteDirectory(TempFolder & "Report\")
                    Hint(GetLang("LangModCrashHintExportCrashReportSuccess"), HintType.Finish)
                Catch ex As Exception
                    Log(ex, "导出错误报告失败", LogLevel.Feedback)
                    Exit Sub
                End Try
                OpenExplorer("/select,""" & FileAddress & """")
        End Select
    End Sub
    ''' <summary>
    ''' 获取崩溃分析的结果描述。
    ''' </summary>
    Private Function GetAnalyzeResult(IsHandAnalyze As Boolean) As String

        '没有结果的处理
        If Not CrashReasons.Any() Then
            If IsHandAnalyze Then
                Return GetLang("LangModCrashCrashReasonNoReason")
            Else
                Return GetLang("LangModCrashCrashReasonHelpTip")
            End If
        End If

        '根据不同原因判断
        Dim Results As New List(Of String)
        For Each Reason In CrashReasons
            Dim Additional As List(Of String) = Reason.Value
            Select Case Reason.Key
                Case CrashReason.Mod文件被解压
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAA"))
                Case CrashReason.内存不足
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAB"))
                Case CrashReason.使用OpenJ9
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAC"))
                Case CrashReason.使用JDK
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAD"))
                Case CrashReason.Java版本过高
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAE"))
                Case CrashReason.Java版本不兼容
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAF"))
                Case CrashReason.Mod名称包含特殊字符
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAG"))
                Case CrashReason.MixinBootstrap缺失
                    Results.Add(GetLang("LangModCrashCrashReasonReasonAH"))
                Case CrashReason.使用32位Java导致JVM无法分配足够多的内存
                    If Environment.Is64BitOperatingSystem Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAI"))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAJ"))
                    End If
                Case CrashReason.Mod缺少前置或MC版本错误
                    If Additional.Any Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAK", Join(Additional, "\n - ")))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAL"))
                    End If
                Case CrashReason.堆栈分析发现关键字
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAM", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAN", Join(Additional, ", ")))
                    End If
                Case CrashReason.堆栈分析发现Mod名称, CrashReason.怀疑Mod导致游戏崩溃
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAO", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAP", Join(Additional, "\n - ")))
                    End If
                Case CrashReason.确定Mod导致游戏崩溃
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAQ", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAR", Join(Additional, "\n - ")))
                    End If
                Case CrashReason.ModMixin失败
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAS", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAT", Join(Additional, "\n - ")))
                    End If
                Case CrashReason.Mod配置文件导致游戏崩溃
                    If Additional(1) Is Nothing Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAU", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAV", Additional.First, Additional(1)))
                    End If
                Case CrashReason.Mod初始化失败
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAW", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAX", Join(Additional, "\n - ")))
                    End If
                Case CrashReason.特定方块导致崩溃
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAY", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonAZ"))
                    End If
                Case CrashReason.Mod重复安装
                    If Additional.Count >= 2 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBA", Join(Additional, "\n - ")))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBB"))
                    End If
                Case CrashReason.特定实体导致崩溃
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBC", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBD"))
                    End If
                Case CrashReason.OptiFine与Forge不兼容
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBE"))
                Case CrashReason.ShadersMod与OptiFine同时安装
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBF"))
                Case CrashReason.低版本Forge与高版本Java不兼容
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBG"))
                Case CrashReason.版本Json中存在多个Forge
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBH"))
                Case CrashReason.玩家手动触发调试崩溃
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBI"))
                Case CrashReason.Mod需要Java11
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBJ"))
                Case CrashReason.极短的程序输出
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBK", Additional.First))
                Case CrashReason.OptiFine导致无法加载世界 'https://www.minecraftforum.net/forums/support/java-edition-support/3051132-exception-ticking-world
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBL"))
                Case CrashReason.显卡驱动不支持导致无法设置像素格式, CrashReason.Intel驱动不兼容导致EXCEPTION_ACCESS_VIOLATION, CrashReason.AMD驱动不兼容导致EXCEPTION_ACCESS_VIOLATION, CrashReason.Nvidia驱动不兼容导致EXCEPTION_ACCESS_VIOLATION, CrashReason.显卡不支持OpenGL
                    If LogAll.Contains("hd graphics ") Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBM"))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBN"))
                    End If
                Case CrashReason.材质过大或显卡配置不足
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBO"))
                Case CrashReason.NightConfig的Bug
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBP"))
                Case CrashReason.光影或资源包导致OpenGL1282错误
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBQ"))
                Case CrashReason.Mod过多导致超出ID限制
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBR"))
                Case CrashReason.文件或内容校验失败
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBS"))
                Case CrashReason.Forge安装不完整
                    Results.Add(GetLang("LangModCrashCrashReasonReasonBT"))
                Case CrashReason.Fabric报错
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBU", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBV"))
                    End If
                Case CrashReason.Mod加载器报错
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBW", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBX"))
                    End If
                Case CrashReason.Fabric报错并给出解决方案
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBY", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonBZ"))
                    End If
                Case CrashReason.Forge报错
                    If Additional.Count = 1 Then
                        Results.Add(GetLang("LangModCrashCrashReasonReasonCA", Additional.First))
                    Else
                        Results.Add(GetLang("LangModCrashCrashReasonReasonCB"))
                    End If
                Case CrashReason.没有可用的分析文件
                    Results.Add(GetLang("LangModCrashCrashReasonReasonCC"))
                Case Else
                    Results.Add(GetLang("LangModCrashCrashReasonReasonFeedback", CrashReasons.First.Key))
            End Select
        Next

        Return Join(Results, "\n\n" & GetLang("LangModCrashCrashReasonDialogContentAdditional")).
                    Replace("\n", vbCrLf).
                    Replace("\h", "").
                    Replace("\e", If(IsHandAnalyze, "", vbCrLf & GetLang("LangModCrashCrashReasonDialogContentViewLogTip"))).
                    Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr, vbCrLf).
                    Trim(vbCrLf.ToCharArray) &
                If(Not Results.Any(Function(r) r.EndsWithF("\h")) OrElse IsHandAnalyze, "",
                    vbCrLf & GetLang("LangModCrashCrashReasonDialogContentAskHelpTip") &
                    If(If(PageSetupSystem.IsLauncherNewest(), True), "",
                    vbCrLf & vbCrLf & GetLang("LangModCrashCrashReasonDialogContentUpgradeTip")))
    End Function

End Class
