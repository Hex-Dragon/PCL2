Public Module ModEvent

    Public Sub TryStartEvent(Type As String, Data As String)
        If String.IsNullOrWhiteSpace(Type) Then Exit Sub
        Dim RealData As String() = {""}
        If Data IsNot Nothing Then RealData = Data.Split("|")
        StartEvent(Type, RealData)
    End Sub
    Public Sub StartEvent(Type As String, Data As String())
        Try
            Log("[Control] 执行自定义事件：" & Type & ", " & Join(Data, ", "))
            Select Case Type

                Case "打开网页"
                    Data(0) = Data(0).Replace("\", "/")
                    If Not (Data(0).StartsWith("http://") OrElse Data(0).StartsWith("https://")) Then
                        MyMsgBox("EventData 必须为以 http:// 或 https:// 开头的网址。" & vbCrLf & "如果想要启动程序，请将 EventType 改为 打开文件。", "事件执行失败")
                        Exit Sub
                    End If
                    Hint("正在打开网页，请稍候……")
                    OpenWebsite(Data(0))

                Case "打开文件", "打开帮助"
                    RunInThread(Sub()
                                    Try

                                        '确认实际路径
                                        Dim ActualPaths = GetEventAbsoluteUrls(Data(0), Type)
                                        Dim Location = ActualPaths(0), WorkingDir = ActualPaths(1)

                                        '执行
                                        If Type = "打开文件" Then
                                            Dim Info As New ProcessStartInfo With {
                                            .Arguments = If(Data.Length >= 2, Data(1), ""),
                                            .FileName = Location,
                                            .WorkingDirectory = WorkingDir
                                        }
                                            Process.Start(Info)
                                        Else '打开帮助
                                            PageOtherHelp.EnterHelpPage(Location)
                                        End If

                                    Catch ex As Exception
                                        Log(ex, "执行打开类自定义事件失败", LogLevel.Msgbox)
                                    End Try
                                End Sub)
                Case "启动游戏"

                    '初始化与前置条件检测
                    If Not (FrmLaunchLeft.BtnLaunch.IsEnabled AndAlso FrmLaunchLeft.BtnLaunch.Visibility = Visibility.Visible AndAlso FrmLaunchLeft.BtnLaunch.IsHitTestVisible) Then
                        Hint("已有游戏正在启动中！", HintType.Critical) : Exit Sub
                    End If
                    If Not Directory.Exists(PathMcFolder & "versions\" & Data(0)) Then
                        Hint("未在当前 Minecraft 文件夹找到版本 " & Data(0) & "！", HintType.Critical) : Exit Sub
                    End If
                    Dim ButtonVersion As New McVersion(Data(0))
                    ButtonVersion.Load()
                    If ButtonVersion.State = McVersionState.Error Then
                        Hint("无法启动 " & Data(0) & "：" & ButtonVersion.Info, HintType.Critical) : Exit Sub
                    End If

                    '实际启动
                    McVersionCurrent = ButtonVersion
                    Setup.Set("LaunchVersionSelect", McVersionCurrent.Name)
                    FrmLaunchLeft.PageLaunchLeft_Loaded()
                    FrmLaunchLeft.RefreshButtonsUI()
                    FrmMain.AprilGiveup()
                    FrmLaunchLeft.LaunchButtonClick(If(Data.Length >= 2, Data(1), ""))
                    FrmMain.PageChange(FormMain.PageType.Launch)

                Case "复制文本"
                    ClipboardSet(Join(Data, "|"))

                Case "刷新主页"
                    FrmLaunchRight.ForceRefresh()

                Case "刷新帮助"
                    PageOtherLeft.RefreshHelp()

                Case "弹出窗口"
                    MyMsgBox(Data(1).Replace("\n", vbCrLf), Data(0).Replace("\n", vbCrLf))

                Case "下载文件"
                    Data(0) = Data(0).Replace("\", "/")
                    If Not (Data(0).StartsWith("http://") OrElse Data(0).StartsWith("https://")) Then
                        MyMsgBox("EventData 必须为以 http:// 或 https:// 开头的网址。" & vbCrLf & "PCL2 不支持其他乱七八糟的协议。", "事件执行失败")
                        Exit Sub
                    End If
                    PageOtherTest.StartCustomDownload(Data(0))

                Case Else
                    MyMsgBox("未知的事件类型：" & Type & vbCrLf & "请检查事件类型填写是否正确，或者 PCL2 是否为最新版本。", "事件执行失败")
            End Select
        Catch ex As Exception
            Log(ex, "事件执行失败", LogLevel.Msgbox)
        End Try
    End Sub

    ''' <summary>
    ''' 返回自定义事件的绝对 Url。实际返回 {绝对 Url, WorkingDir}。
    ''' 失败会抛出异常。
    ''' </summary>
    Public Function GetEventAbsoluteUrls(RelativeUrl As String, EventType As String) As String()

        '网页确认
        If RelativeUrl.ToLower.StartsWith("http") Then
            If RunInUi() Then
                Throw New Exception("MyListItem 在界面初始化时就需要获取帮助标题等信息，这会导致程序在网络请求时卡死。" & vbCrLf &
                                    "因此，请换用 MyListItem 以外的控件（例如 MyButton）作为联网帮助页面的入口！")
            End If
            '获取文件名
            Dim RawFileName As String
            Try
                RawFileName = GetFileNameFromPath(RelativeUrl)
                If Not RawFileName.ToLower.EndsWith(".json") Then Throw New Exception("未指向 .json 后缀的文件")
            Catch ex As Exception
                Throw New Exception("联网帮助页面须指向一个帮助 JSON 文件，并在同路径下包含相应 XAML 文件！" & vbCrLf &
                                    "例如：" & vbCrLf &
                                    " - https://www.baidu.com/test.json（填写这个路径）" & vbCrLf &
                                    " - https://www.baidu.com/test.xaml（同时也需要包含这个文件）", ex)
            End Try
            '下载文件
            Dim LocalTemp1 As String = PathTemp & "CustomEvent\" & RawFileName
            Dim LocalTemp2 As String = PathTemp & "CustomEvent\" & RawFileName.Replace(".json", ".xaml")
            Log("[Event] 转换网络资源：" & RelativeUrl & " -> " & LocalTemp1)
            Hint("正在获取资源，请稍候……")
            Try
                NetDownload(RelativeUrl, LocalTemp1)
                NetDownload(RelativeUrl.Replace(".json", ".xaml"), LocalTemp1.Replace(".json", ".xaml"))
            Catch ex As Exception
                Throw New Exception("下载指定的文件失败！" & vbCrLf &
                                    "注意，联网帮助页面须指向一个帮助 JSON 文件，并在同路径下包含相应 XAML 文件！" & vbCrLf &
                                    "例如：" & vbCrLf &
                                    " - https://www.baidu.com/test.json（填写这个路径）" & vbCrLf &
                                    " - https://www.baidu.com/test.xaml（同时也需要包含这个文件）", ex)
            End Try
            RelativeUrl = LocalTemp1
        End If
        RelativeUrl = RelativeUrl.Replace("/", "\").ToLower.TrimStart("\")

        '确认实际路径
        Dim Location As String, WorkingDir As String = Path & "PCL"
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
        ElseIf EventType = "打开文件" Then
            '直接使用原有路径启动程序
            Location = RelativeUrl
            Log("[Control] 自定义事件中直接" & EventType & "：" & Location)
        Else
            '打开帮助，但是格式不对劲
            Throw New FileNotFoundException("未找到 EventData 指向的本地 xaml 文件：" & RelativeUrl, RelativeUrl)
        End If

        Return {Location, WorkingDir}
    End Function

End Module
