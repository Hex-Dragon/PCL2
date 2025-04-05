Imports System.Collections.ObjectModel

Public Module ModEvent
#Region "事件执行方法"
    ''' <summary>
    ''' 供外部调用的自定义事件执行接口，会在新建工作线程中运行逻辑。
    ''' </summary>
    ''' <param name="SingleEventType">xaml中的EventType</param>
    ''' <param name="SingleEventData">xaml中的EventData</param>
    ''' <param name="EventCollection">xaml中的CustomEvents</param>
    Public Sub ProcessCustomEvents(SingleEventType As String, SingleEventData As String, EventCollection As CustomEventCollection)
        RunInNewThread(
            Sub()
                Try
                    '先执行单个事件
                    StartCustomEvent(SingleEventType, SingleEventData)
                    '再挨个执行事件集合中的事件
                    If EventCollection IsNot Nothing Then
                        For Each AnEvent In RunInUiWait(Function() EventCollection.DeepCloneToStructureList())
                            StartCustomEvent(AnEvent.EventType, AnEvent.EventData)
                        Next
                    End If
                Catch ex As Exception
                    Log(ex, "自定义事件执行器发生异常", LogLevel.Feedback)
                End Try
            End Sub, "CustomEventProcessor " & GetUuid())
    End Sub
    ''' <summary>
    ''' 对当前线程阻塞地调用自定义事件处理器，接受来自 xaml 原始输入作为参数。
    ''' </summary>
    Private Sub StartCustomEvent(Type As String, DataRaw As String)
        If String.IsNullOrEmpty(Type) Then Exit Sub
        Dim Data = If(DataRaw Is Nothing, {""}, DataRaw.Split("|"))
        Log("[Control] 执行自定义事件：" & Type & ", " & Join(Data, ", "))
        '检测是否存在对应事件
        If Not CustomEventProcessorsDict.ContainsKey(Type) Then
            MyMsgBox($"未知的事件类型：'{Type}'{vbCrLf}请检查事件类型填写是否正确，或者 PCL 是否为最新版本。", "事件执行失败", ForceWait:=True)
            Exit Sub
        End If
        '调用事件处理器
        Try
            CustomEventProcessorsDict(Type)(Data)
        Catch ex As Exception
            Log(ex, $"自定义事件执行失败：{Type}", LogLevel.Msgbox, "自定义主页事件执行失败")
        End Try
    End Sub

    ''' <summary>
    ''' 存储各个自定义事件处理器的字典，以 EventType 为键，执行自定义事件时在工作线程中调用 Action 并传入分割后的 EventData 。
    ''' Action 应当在事件逻辑结束后，即可以开始下一个事件时再结束；以便于挨个调用事件不打架。
    ''' </summary>
    Private ReadOnly CustomEventProcessorsDict As New Dictionary(Of String, Action(Of String())) From {
        {"打开网页",
        Sub(Data As String())
            Data(0) = Data(0).Replace("\", "/")
            If Not Data(0).Contains("://") OrElse Data(0).StartsWithF("file", True) Then '为了支持更多协议（#2200）
                MyMsgBox("打开网页事件中，EventData 必须为一个网址。" & vbCrLf & "如果想要启动程序，请将 EventType 改为 打开文件。", "事件执行失败")
                Exit Sub
            End If
            Hint("网页正在开启中，请稍候……")
            OpenWebsite(Data(0))
        End Sub},
        {"打开文件",
        Sub(Data As String())
            Dim ActualPaths = GetEventAbsoluteUrls(Data(0), "打开文件")
            Dim Location = ActualPaths(0), WorkingDir = ActualPaths(1)
            Log($"[Control] 打开文件：路径：{Location}，工作目录：{WorkingDir}")
            If Not CommandRunUserConfirm(If(Data.Length < 2, Location, $"{Location} {Data(1)}")) Then Exit Sub
            Dim Info As New ProcessStartInfo With {
                .Arguments = If(Data.Length < 2, "", Data(1)),
                .FileName = Location,
                .WorkingDirectory = ShortenPath(WorkingDir)
            }
            Process.Start(Info)
        End Sub},
        {"打开帮助",
        Sub(Data As String())
            Dim ActualPaths = GetEventAbsoluteUrls(Data(0), "打开帮助")
            Dim Location = ActualPaths(0)
            Log($"[Control] 打开帮助：{Location}")
            PageOtherHelp.EnterHelpPage(Location)
        End Sub},
        {"执行命令",
        Sub(Data As String())
            Dim ActualPaths = GetEventAbsoluteUrls(Data(0), "执行命令")
            Dim Location = ActualPaths(0), WorkingDir = ActualPaths(1)
            Log($"[Control] 执行命令：路径：{Location}，工作目录：{WorkingDir}")
            If Not CommandRunUserConfirm(If(Data.Length < 2, Location, $"{Location} {Data(1)}")) Then Exit Sub
            Dim Info As New ProcessStartInfo With {
                .Arguments = If(Data.Length < 2, "", Data(1)),
                .FileName = Location,
                .WorkingDirectory = ShortenPath(WorkingDir)
            }
            Process.Start(Info)
        End Sub},
        {"启动游戏",
        Sub(Data As String())
            If Data(0) = "\current" Then
                If McVersionCurrent Is Nothing Then
                    Hint("请先选择一个 Minecraft 版本！", HintType.Critical)
                    Exit Sub
                Else
                    Data(0) = McVersionCurrent.Name
                End If
            End If
            RunInUiWait(
            Sub()
                If McLaunchStart(New McLaunchOptions With {.Version = New McVersion(Data(0)), .ServerIp = If(Data.Length >= 2, Data(1), Nothing)}) Then
                    Hint("正在启动 " & Data(0) & "……")
                End If
            End Sub)
        End Sub},
        {"复制文本", Sub(Data As String()) ClipboardSet(Join(Data, "|"))},
        {"刷新主页",
        Sub(Data As String())
            RunInUiWait(Sub() FrmLaunchRight.ForceRefresh())
            If Data(0) = "" Then Hint("已刷新主页！", HintType.Finish)
        End Sub},
        {"刷新帮助", Sub() RunInUi(Sub() PageOtherLeft.RefreshHelp())},
        {"今日人品", Sub() PageOtherTest.Jrrp()},
        {"内存优化",
        Sub(Data As String())
            If Data.Length >= 1 AndAlso Data(0) = "前台运行" Then
                PageOtherTest.MemoryOptimize(Data.Length >= 2 AndAlso Data(1) = "显示提示")
            Else
                RunInNewThread(Sub() PageOtherTest.MemoryOptimize(True))
            End If
        End Sub},
        {"弹出窗口", Sub(Data As String()) MyMsgBox(Data(1).Replace("\n", vbCrLf), Data(0).Replace("\n", vbCrLf), ForceWait:=True)},
        {"切换页面", Sub(Data As String()) RunInUiWait(Sub() FrmMain.PageChange(Val(Data(0)), Val(Data(1))))},
        {"安装整合包", Sub() RunInUiWait(Sub() ModpackInstall())},
        {"导入整合包", Sub() RunInUiWait(Sub() ModpackInstall())},
        {"下载文件",
        Sub(Data As String())
            Data(0) = Data(0).Replace("\", "/")
            If Not (Data(0).StartsWithF("http://", True) OrElse Data(0).StartsWithF("https://", True)) Then
                MyMsgBox("EventData 必须为以 http:// 或 https:// 开头的网址。" & vbCrLf & "PCL 不支持其他乱七八糟的下载协议。", "事件执行失败")
                Exit Sub
            End If
            Try
                Select Case Data.Length
                    Case 1
                        PageOtherTest.StartCustomDownload(Data(0), GetFileNameFromPath(Data(0)))
                    Case 2
                        PageOtherTest.StartCustomDownload(Data(0), Data(1))
                    Case Else
                        PageOtherTest.StartCustomDownload(Data(0), Data(1), Data(2))
                End Select
            Catch
                PageOtherTest.StartCustomDownload(Data(0), "未知")
            End Try
        End Sub}
    }

#End Region
#Region "工具方法"
    ''' <summary>
    ''' 弹窗询问用户是否同意继续执行打开类事件。
    ''' </summary>
    Private Function CommandRunUserConfirm(TextShowToUser As String) As Boolean
        If Setup.Get("HintCustomCommand") Then Return True
        Select Case MyMsgBox("即将执行：" & TextShowToUser & vbCrLf & "请在确认该操作没有安全隐患后继续。", "执行确认", "继续", "继续且今后不再要求确认", "取消")
            Case 1
                Return True
            Case 2
                Setup.Set("HintCustomCommand", True)
                Return True
            Case Else '3
                Log("[Control] 用户取消了打开类事件")
                Return False
        End Select
    End Function

    ''' <summary>
    ''' 返回自定义事件的绝对 Url。实际返回 {绝对 Url, WorkingDir}。
    ''' 失败会抛出异常。
    ''' </summary>
    Public Function GetEventAbsoluteUrls(RelativeUrl As String, EventType As String) As String()

        '网页确认
        If RelativeUrl.StartsWithF("http", True) Then
            If RunInUi() Then
                Throw New Exception("能打开联网帮助页面的 MyListItem 必须手动设置 Title、Info 属性！")
            End If
            '获取文件名
            Dim RawFileName As String
            Try
                RawFileName = GetFileNameFromPath(RelativeUrl)
                If Not RawFileName.EndsWithF(".json", True) Then Throw New Exception("未指向 .json 后缀的文件")
            Catch ex As Exception
                Throw New Exception("联网帮助页面须指向一个帮助 JSON 文件，并在同路径下包含相应 XAML 文件！" & vbCrLf &
                                    "例如：" & vbCrLf &
                                    " - https://www.baidu.com/test.json（填写这个路径）" & vbCrLf &
                                    " - https://www.baidu.com/test.xaml（同时也需要包含这个文件）", ex)
            End Try
            '下载文件
            Dim LocalTemp As String = RequestTaskTempFolder() & RawFileName
            Log("[Event] 转换网络资源：" & RelativeUrl & " -> " & LocalTemp)
            Try
                NetDownloadByClient(RelativeUrl, LocalTemp)
                NetDownloadByClient(RelativeUrl.Replace(".json", ".xaml"), LocalTemp.Replace(".json", ".xaml"))
            Catch ex As Exception
                Throw New Exception("下载指定的文件失败！" & vbCrLf &
                                    "注意，联网帮助页面须指向一个帮助 JSON 文件，并在同路径下包含相应 XAML 文件！" & vbCrLf &
                                    "例如：" & vbCrLf &
                                    " - https://www.baidu.com/test.json（填写这个路径）" & vbCrLf &
                                    " - https://www.baidu.com/test.xaml（同时也需要包含这个文件）", ex)
            End Try
            RelativeUrl = LocalTemp
        End If
        RelativeUrl = RelativeUrl.Replace("/", "\").ToLower.TrimStart("\")

        '确认实际路径
        Dim Location As String, WorkingDir As String = Path & "PCL"
        HelpTryExtract()
        If RelativeUrl.Contains(":\") Then
            '绝对路径
            Location = RelativeUrl
            Log("[Control] 自定义事件中由绝对路径" & EventType & "：" & Location)
        ElseIf File.Exists(Path & "PCL\" & RelativeUrl) Then
            '相对 PCL 文件夹的路径
            Location = Path & "PCL\" & RelativeUrl
            Log("[Control] 自定义事件中由相对 PCL 文件夹的路径" & EventType & "：" & Location)
        ElseIf File.Exists(Path & "PCL\Help\" & RelativeUrl) Then
            '相对 PCL 本地帮助文件夹的路径
            Location = Path & "PCL\Help\" & RelativeUrl
            WorkingDir = Path & "PCL\Help\"
            Log("[Control] 自定义事件中由相对 PCL 本地帮助文件夹的路径" & EventType & "：" & Location)
        ElseIf EventType = "打开帮助" AndAlso File.Exists(PathTemp & "Help\" & RelativeUrl) Then
            '相对 PCL 自带帮助文件夹的路径
            Location = PathTemp & "Help\" & RelativeUrl
            WorkingDir = PathTemp & "Help\"
            Log("[Control] 自定义事件中由相对 PCL 自带帮助文件夹的路径" & EventType & "：" & Location)
        ElseIf EventType = "打开文件" OrElse EventType = "执行命令" Then
            '直接使用原有路径启动程序
            Location = RelativeUrl
            Log("[Control] 自定义事件中直接" & EventType & "：" & Location)
        Else
            '打开帮助，但是格式不对劲
            Throw New FileNotFoundException("未找到 EventData 指向的本地 xaml 文件：" & RelativeUrl, RelativeUrl)
        End If

        Return {Location, WorkingDir}
    End Function

#End Region
End Module
#Region "数据结构类声明"
Public Class CustomEventCollection
    Inherits ObservableCollection(Of CustomEvent)
    Public Function DeepCloneToStructureList() As IList(Of CustomEventStructure)
        DeepCloneToStructureList = New List(Of CustomEventStructure)
        For Each AnEvent In Me
            DeepCloneToStructureList.Add(New CustomEventStructure With {.EventType = AnEvent.EventType, .EventData = AnEvent.EventData})
        Next
    End Function
End Class
Public Class CustomEvent
    Inherits DependencyObject
    Public Property EventType As String
        Get
            Return GetValue(EventTypeProperty)
        End Get
        Set(value As String)
            SetValue(EventTypeProperty, value)
        End Set
    End Property
    Public Shared ReadOnly EventTypeProperty As DependencyProperty = DependencyProperty.Register("EventType", GetType(String), GetType(CustomEvent), New PropertyMetadata(Nothing))
    Public Property EventData As String
        Get
            Return GetValue(EventDataProperty)
        End Get
        Set(value As String)
            SetValue(EventDataProperty, value)
        End Set
    End Property
    Public Shared ReadOnly EventDataProperty As DependencyProperty = DependencyProperty.Register("EventData", GetType(String), GetType(CustomEvent), New PropertyMetadata(Nothing))
End Class
Public Structure CustomEventStructure
    Public EventType As String
    Public EventData As String
End Structure

#End Region