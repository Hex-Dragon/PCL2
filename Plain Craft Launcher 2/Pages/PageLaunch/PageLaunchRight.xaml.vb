Public Class PageLaunchRight
    Implements IRefreshable

    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
        PanScroll = PanBack '不知道为啥不能在 XAML 设置
        PanLog.Visibility = If(ModeDebug, Visibility.Visible, Visibility.Collapsed)
        '快照版提示
#If BETA Then
        PanHint.Visibility = Visibility.Collapsed
#Else
        PanHint.Visibility = If(ThemeCheckGold(), Visibility.Collapsed, Visibility.Visible)
        LabHint1.Text = "快照版包含尚未正式发布的测试功能，仅用于赞助者本人尝鲜。请不要发给其他人或者用来制作整合包哦！"
        LabHint2.Text = $"若已累积赞助￥23.33，在爱发电私信发送 {vbLQ}解锁码{vbRQ} 即可永久隐藏此提示。"
#End If
    End Sub

    '暂时关闭快照版提示
#If Not BETA Then
    Private Sub BtnHintClose_Click(sender As Object, e As EventArgs) Handles BtnHintClose.Click
        AniDispose(PanHint, True)
    End Sub
#End If

#Region "自定义主页"

    ''' <summary>
    ''' 刷新自定义主页。
    ''' </summary>
    Private Sub Refresh() Handles Me.Loaded
        RunInNewThread(
        Sub()
            Try
                SyncLock RefreshLock
                    RefreshReal()
                End SyncLock
            Catch ex As Exception
                Log(ex, "加载 PCL 主页自定义信息失败", LogLevel.Msgbox)
            End Try
        End Sub, $"刷新自定义主页 #{GetUuid()}")
    End Sub

    ''' <summary>
    ''' 获取自定义主页的内容；在需要从网络获取主页信息时返回Url且GetOnline将为真
    ''' </summary>
    Private Function GetCustomMainpageTarget(ByRef GetOnline As Boolean) As String
        Select Case Setup.Get("UiCustomType")
            Case 1
                Log("[Page] 主页自定义数据来源：本地文件")
                GetOnline = False
                Return ReadFile(Path & "PCL\Custom.xaml") 'ReadFile 会进行存在检测
            Case 2
                GetOnline = True
                Return Setup.Get("UiCustomNet")
            Case 3
                Dim UiCustomPreset As Integer = Setup.Get("UiCustomPreset")
                GetOnline = True
                Select Case UiCustomPreset
                    Case 0
                        Log("[Page] 主页预设：你知道吗")
                        GetOnline = False
                        Return "
                            <local:MyCard Title=""你知道吗？"" Margin=""0,0,0,15"">
                                <TextBlock Margin=""25,38,23,15"" FontSize=""13.5"" IsHitTestVisible=""False"" Text=""{hint}"" TextWrapping=""Wrap"" Foreground=""{DynamicResource ColorBrush1}"" />
                                <local:MyIconButton Height=""22"" Width=""22"" Margin=""9"" VerticalAlignment=""Top"" HorizontalAlignment=""Right"" 
                                    EventType=""刷新主页"" EventData=""/""
                                    Logo=""M875.52 148.48C783.36 56.32 655.36 0 512 0 291.84 0 107.52 138.24 30.72 332.8l122.88 46.08C204.8 230.4 348.16 128 512 128c107.52 0 199.68 40.96 271.36 112.64L640 384h384V0L875.52 148.48zM512 896c-107.52 0-199.68-40.96-271.36-112.64L384 640H0v384l148.48-148.48C240.64 967.68 368.64 1024 512 1024c220.16 0 404.48-138.24 481.28-332.8L870.4 645.12C819.2 793.6 675.84 896 512 896z"" />
                            </local:MyCard>"
                    Case 1
                        Log("[Page] 主页预设：回声洞")
                        GetOnline = False
                        Return "
                            <local:MyCard Title=""回声洞"" Margin=""0,0,0,15"">
                                <TextBlock Margin=""25,38,23,15"" FontSize=""13.5"" IsHitTestVisible=""False"" Text=""{cave}"" TextWrapping=""Wrap"" Foreground=""{DynamicResource ColorBrush1}"" />
                                <local:MyIconButton Height=""22"" Width=""22"" Margin=""9"" VerticalAlignment=""Top"" HorizontalAlignment=""Right"" 
                                    EventType=""刷新主页"" EventData=""/""
                                    Logo=""M875.52 148.48C783.36 56.32 655.36 0 512 0 291.84 0 107.52 138.24 30.72 332.8l122.88 46.08C204.8 230.4 348.16 128 512 128c107.52 0 199.68 40.96 271.36 112.64L640 384h384V0L875.52 148.48zM512 896c-107.52 0-199.68-40.96-271.36-112.64L384 640H0v384l148.48-148.48C240.64 967.68 368.64 1024 512 1024c220.16 0 404.48-138.24 481.28-332.8L870.4 645.12C819.2 793.6 675.84 896 512 896z"" />
                            </local:MyCard>"
                    Case 2
                        Log("[Page] 主页预设：Minecraft 新闻")
                        Return "http://pcl.mcnews.thestack.top"
                    Case 3
                        Log("[Page] 主页预设：简单主页")
                        Return "https://raw.gitcode.com/mfn233/PCL-Mainpage/raw/main/Custom.xaml"
                    Case 4
                        Log("[Page] 主页预设：每日整合包推荐")
                        Return "https://pclsub.sodamc.com/"
                    Case 5
                        Log("[Page] 主页预设：Minecraft 皮肤推荐")
                        Return "https://forgepixel.com/pcl_sub_file"
                    Case 6
                        Log("[Page] 主页预设：OpenBMCLAPI 仪表盘 Lite")
                        Return "https://pcl-bmcl.milu.ink/"
                    Case 7
                        Log("[Page] 主页预设：主页市场")
                        Return "https://pclhomeplazaoss.lingyunawa.top:26994/d/Homepages/JingHai-Lingyun/Custom.xaml"
                    Case 8
                        Log("[Page] 主页预设：更新日志")
                        Return "https://pclhomeplazaoss.lingyunawa.top:26994/d/Homepages/Joker2184/UpdateHomepage.xaml"
                    Case 9
                        Log("[Page] 主页预设：PCL 新功能说明书")
                        Return "https://raw.gitcode.com/WForst-Breeze/WhatsNewPCL/raw/main/Custom.xaml"
                    Case 10
                        Log("[Page] 主页预设：OpenMCIM Dashboard")
                        Return "https://files.mcimirror.top/PCL"
                    Case 11
                        Log("[Page] 主页预设：杂志主页")
                        Return "https://pclhomeplazaoss.lingyunawa.top:26994/d/Homepages/Ext1nguisher/Custom.xaml"
                    Case Else
                        Hint($"[Page] 主页预设：未知的预设主页类型：{UiCustomPreset}", HintType.Critical)
                        Setup.Reset("UiCustomPreset")
                        Setup.Reset("UiCustomType")
                        GetOnline = False
                        Return ""
                End Select
            Case Else
                GetOnline = False
                Return ""
        End Select
    End Function

    Private Sub RefreshReal()
        Dim GetOnline As Boolean
        Dim Target As String = GetCustomMainpageTarget(GetOnline)
        If Not GetOnline Then
            RunInUi(Sub() LoadContent(Target))
        Else '需要从网络上获取主页内容
            MainpageLoader.Start(Target, IsForceRestart:=True) '强制启动，以免被不智能的LoaderTask#ShouldStart掐掉，我自己来判断是否无需重新获取
        End If
    End Sub
    Private RefreshLock As New Object

    ''' <summary>
    ''' 获取来自网络的主页内容，负责判断缓存是否可用，几乎立刻就能刷新一次主页内容。
    ''' Input - 目标Url；
    ''' Output - 现在应当被显示的主页内容。
    ''' </summary>
    Private MainpageLoader As New LoaderTask(Of String, String)("自定义主页获取", AddressOf MainpageLoaderSub) With {
        .OnStateChanged = Sub(Loader As LoaderTask(Of String, String)) If Loader.State = LoadState.Finished Then LoadContent(Loader.Output) '如果运行成功刷新主页
    }
    Private Sub MainpageLoaderSub(Task As LoaderTask(Of String, String))
        '这个Loader应当尽快结束，不做任何联网操作，因为急着结束后触发UI更改（New LoaderTask的时候挂进去的那个钩子），需要联网的时候调用别的Loader再调用回来
        Dim Target As String = Task.Input '#3721 中连续触发两次导致内容变化 '修 #5057 的时候直接挪过来了，需不需要有待论证
        If Target <> Setup.Get("CacheSavedPageUrl") OrElse Not File.Exists(PathTemp & "Cache\Custom.xaml") Then
            '缓存无效
            Log("[Page] 主页自定义数据来源：联网全新下载")
            Hint("正在加载主页……")
            Task.Output = "" '清空主页内容
            MainpageDownloaderLoader.Start(New Tuple(Of String, Boolean)(Target, True), IsForceRestart:=True) '它运行结束后会调用回来，预计进入下一个case
        Else
            '缓存有效
            Log("[Page] 主页自定义数据来源：联网缓存文件")
            Task.Output = ReadFile(PathTemp & "Cache\Custom.xaml")
            MainpageDownloaderLoader.Start(New Tuple(Of String, Boolean)(Target, False), IsForceRestart:=True) '检查版本，如果版本不同还会更新缓存并调用回来
        End If
    End Sub

    ''' <summary>
    ''' 从网上下载主页，可选是否进行版本检查。
    ''' Input1 - 目标Url；
    ''' Input2 - 是否跳过版本检查，如果为假则判断版本与远程相同时不会下载；
    ''' Output - 获取到的内容。
    ''' 如果没被版本检查掐掉，会联网下载主页内容，更新缓存并调用MainpageLoader。
    ''' </summary>
    Private MainpageDownloaderLoader As New LoaderTask(Of Tuple(Of String, Boolean), String)("自定义主页联网下载", AddressOf MainpageDownloaderLoaderSub)
    Private Sub MainpageDownloaderLoaderSub(Task As LoaderTask(Of Tuple(Of String, Boolean), String))
        Dim Address = Task.Input.Item1
        Try
            '联网获取版本，不加IsForceRestart:=True以允许Loader自动使用缓存
            MainpageVersionGetterLoader.WaitForExit(Address)
            Dim VersionOnline As String = MainpageVersionGetterLoader.Output
            '进行版本检查
            Dim VersionCached = Setup.Get("CacheSavedPageVersion")
            Log($"本地缓存的主页版本信息：'{VersionCached}'")
            If Task.Input.Item2 OrElse (VersionCached <> VersionOnline) Then
                '开始下载主页
                Dim FileContent As String = NetGetCodeByRequestRetry(Address)
                Log($"[Page] 成功联网下载自定义主页，内容长度：{FileContent.Length}，来源：{Address}")
                '写入缓存
                Setup.Set("CacheSavedPageUrl", Address)
                Setup.Set("CacheSavedPageVersion", VersionOnline)
                WriteFile(PathTemp & "Cache\Custom.xaml", FileContent)
                '运行完成。call一下MainpageLoader，再调用回来的时候预计不会进入这个case
                Task.Output = FileContent
                MainpageLoader.Start(Address, IsForceRestart:=True)
            Else
                Log($"[Page] 自定义主页版本已是最新，跳过下载")
            End If
        Catch ex As Exception
            Log(ex, $"联网下载自定义主页失败（{Address}）", LogLevel.Msgbox)
        End Try
    End Sub

    ''' <summary>
    ''' 获取联网自定义主页版本。
    ''' Input - 目标Url；
    ''' Output - 获取到的版本信息。
    ''' </summary>
    Private MainpageVersionGetterLoader As New LoaderTask(Of String, String)("自定义主页版本获取", AddressOf MainpageVersionGetterLoaderSub) With {.ReloadTimeout = 10 * 60 * 1000}
    Private Sub MainpageVersionGetterLoaderSub(Task As LoaderTask(Of String, String))
        Dim Address = Task.Input
        Dim VersionAddress As String = ""
        Try
            '制作版本校验地址
            If Address.Contains(".xaml") Then
                VersionAddress = Address.Replace(".xaml", ".xaml.ini")
            Else
                VersionAddress = Address.BeforeFirst("?")
                If Not VersionAddress.EndsWith("/") Then VersionAddress += "/"
                VersionAddress += "version"
                If Address.Contains("?") Then VersionAddress += Address.AfterLast("?")
            End If
            Log($"[Page] 连接至'{VersionAddress}'获取自定义主页版本信息")
            '连接网站
            Dim Result = NetGetCodeByRequestOnce(VersionAddress, Timeout:=10000)
            If Result.Length > 1000 Then Throw New Exception($"获取的自定义主页版本过长（{Result.Length} 字符）")
            Log($"[Page] 成功从主页'{Address}'获取到版本信息：'{Result}'")
            Task.Output = Result
        Catch ex As Exception
            Log(ex, $"联网获取自定义主页版本失败", LogLevel.Developer)
            Log($"[Page] 无法检查联网自定义主页版本，将直接下载，检查源：{VersionAddress}")
            Task.Output = $"<{Address}版本获取失败>"
        End Try
    End Sub

    ''' <summary>
    ''' 立即强制刷新自定义主页。
    ''' 必须在 UI 线程调用。
    ''' </summary>
    Public Sub ForceRefresh() Implements IRefreshable.Refresh
        Log("[Page] 要求强制刷新自定义主页")
        ClearCache()
        '实际的刷新
        If FrmMain.PageCurrent.Page = FormMain.PageType.Launch Then
            PanBack.ScrollToHome()
            Refresh()
        Else
            FrmMain.PageChange(FormMain.PageType.Launch)
        End If
    End Sub

    ''' <summary>
    ''' 清空自定义主页缓存信息。
    ''' </summary>
    Private Sub ClearCache()
        LoadedContentHash = -1
        Setup.Set("CacheSavedPageUrl", "")
        Setup.Set("CacheSavedPageVersion", "")
        MainpageVersionGetterLoader.Input = ""
        Log("[Page] 已清空自定义主页缓存")
    End Sub

    ''' <summary>
    ''' 从文本内容中加载自定义主页。
    ''' 必须在 UI 线程调用。
    ''' </summary>
    Private Sub LoadContent(Content As String)
        SyncLock LoadContentLock
            '如果加载目标内容一致则不加载
            Dim Hash = Content.GetHashCode()
            If Hash = LoadedContentHash Then Exit Sub
            LoadedContentHash = Hash
            '实际加载内容
            PanCustom.Children.Clear()
            If String.IsNullOrWhiteSpace(Content) Then
                Log($"[Page] 实例化：清空自定义主页 UI，来源为空")
                Return
            End If
            Try
                Content = HelpArgumentReplace(Content)
                If Content.Contains("xmlns") Then Content = Content.RegexReplace("xmlns[^""']*(""|')[^""']*(""|')", "").Replace("xmlns", "") '禁止声明命名空间
                Content = "<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:sys=""clr-namespace:System;assembly=mscorlib"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Plain Craft Launcher 2"">" & Content & "</StackPanel>"
                Log($"[Page] 实例化：加载自定义主页 UI 开始，最终内容长度：{Content.Count}")
                PanCustom.Children.Add(GetObjectFromXML(Content))
            Catch ex As UnauthorizedAccessException
                Log(ex, "加载失败的自定义主页内容：" & vbCrLf & Content)
                If MyMsgBox(ex.Message, "加载自定义主页失败", "重试", "取消") = 1 Then
                    GoTo Refresh '防止 SyncLock 死锁
                End If
            Catch ex As Exception
                Log(ex, "加载失败的自定义主页内容：" & vbCrLf & Content)
                If MyMsgBox($"自定义主页内容编写有误，请根据下列错误信息进行检查：{vbCrLf}{GetExceptionSummary(ex)}", "加载自定义主页失败", "重试", "取消") = 1 Then
                    GoTo Refresh '防止 SyncLock 死锁
                End If
            End Try
            Log($"[Page] 实例化：加载自定义主页 UI 完成")
        End SyncLock
        Return
Refresh:
        ForceRefresh()
    End Sub
    Private LoadedContentHash As Integer = -1
    Private LoadContentLock As New Object

#End Region

End Class
