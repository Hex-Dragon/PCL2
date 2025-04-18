'由于包含加解密等安全信息，本文件中的部分代码已被删除

Imports System.Security.Cryptography

Friend Module ModSecret

#Region "杂项"

    '在开源版的注册表与常规版的注册表隔离，以防数据冲突
    Public Const RegFolder As String = "PCLDebug"
    '用于微软登录的 ClientId
    Public Const OAuthClientId As String = ""
    'CurseForge API Key
    Public Const CurseForgeAPIKey As String = ""

    Friend Sub SecretOnApplicationStart()
        '提升 UI 线程优先级
        Thread.CurrentThread.Priority = ThreadPriority.Highest
        '确保 .NET Framework 版本
        Try
            Dim VersionTest As New FormattedText("", Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First, 96, New MyColor, DPI)
        Catch ex As UriFormatException '修复 #3555
            Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"), EnvironmentVariableTarget.User)
            Dim VersionTest As New FormattedText("", Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First, 96, New MyColor, DPI)
        End Try
        '检测当前文件夹权限
        Try
            Directory.CreateDirectory(Path & "PCL")
        Catch ex As Exception
            MsgBox($"PCL 无法创建 PCL 文件夹（{Path & "PCL"}），请尝试：" & vbCrLf &
                  "1. 将 PCL 移动到其他文件夹" & If(Path.StartsWithF("C:", True), "，例如 C 盘和桌面以外的其他位置。", "。") & vbCrLf &
                  "2. 删除当前目录中的 PCL 文件夹，然后再试。" & vbCrLf &
                  "3. 右键 PCL 选择属性，打开 兼容性 中的 以管理员身份运行此程序。",
                MsgBoxStyle.Critical, "运行环境错误")
            Environment.[Exit](ProcessReturnValues.Cancel)
        End Try
        If Not CheckPermission(Path & "PCL") Then
            MsgBox("PCL 没有对当前文件夹的写入权限，请尝试：" & vbCrLf &
                  "1. 将 PCL 移动到其他文件夹" & If(Path.StartsWithF("C:", True), "，例如 C 盘和桌面以外的其他位置。", "。") & vbCrLf &
                  "2. 删除当前目录中的 PCL 文件夹，然后再试。" & vbCrLf &
                  "3. 右键 PCL 选择属性，打开 兼容性 中的 以管理员身份运行此程序。",
                MsgBoxStyle.Critical, "运行环境错误")
            Environment.[Exit](ProcessReturnValues.Cancel)
        End If
        '开源版本提示
        MyMsgBox($"该版本中无法使用以下特性：
- CurseForge API 调用：需要你自行申请 API Key，然后添加到 ModSecret.vb 的开头
- 正版登录：需要你自行申请 Client ID，然后添加到 ModSecret.vb 的开头
- 更新与联网通知：避免滥用隐患
- 主题切换：这是需要赞助解锁的纪念性质的功能，别让赞助者太伤心啦……
- 百宝箱：开发早期往里面塞了些开发工具，整理起来太麻烦了", "开源版本说明")
    End Sub

    ''' <summary>
    ''' 获取设备标识码。
    ''' </summary>
    Friend Function SecretGetUniqueAddress() As String
        Return "0000-0000-0000-0000"
    End Function

        ' PCL.ModSecret
' Token: 0x0600050D RID: 1293 RVA: 0x00030FFC File Offset: 0x0002F1FC
Public Sub UpdateStart(BaseUrl As String, Slient As Boolean, Optional ReceivedKey As String = Nothing, Optional ForceValidated As Boolean = False)
	Dim CS$<>8__locals1 As ModSecret._Closure$__55-0 = New ModSecret._Closure$__55-0(CS$<>8__locals1)
	CS$<>8__locals1.$VB$Local_BaseUrl = BaseUrl
	If ModSecret._FieldService Then
		If Not Slient Then
			ModMain.Hint("PCL 正在下载更新，更新结束时将自动重启，请稍候！", ModMain.HintType.Info, True)
			ModSecret._FilterService = False
		End If
		If ModSecret.definitionService Then
			ModSecret.UpdateRestart(True)
			Return
		End If
	Else
		ModSecret._FieldService = True
		ModSecret._FilterService = Slient
		ModBase.Log("[System] 更新已开始，静默：" + Conversions.ToString(ModSecret._FilterService), ModBase.LogLevel.Normal, "出现错误")
		CS$<>8__locals1.$VB$Local_UpdateKey = "Publi2"
		If Not ModSecret._FilterService Then
			ModMain.Hint("PCL 正在下载更新，更新结束时将自动重启，请稍候！", ModMain.HintType.Info, True)
		End If
		ModBase.RunInUi(Sub()
			Try
				Dim CS$<>8__locals2 As ModSecret._Closure$__55-1 = New ModSecret._Closure$__55-1(CS$<>8__locals2)
				CS$<>8__locals1.$VB$Local_BaseUrl = CS$<>8__locals1.$VB$Local_BaseUrl.Replace("{KEY}", CS$<>8__locals1.$VB$Local_UpdateKey)
				Dim array As String()
				If CS$<>8__locals1.$VB$Local_BaseUrl.EndsWithF(String.Format("pcl2-server-1253424809.file.myqcloud.com/update/{0}.zip{{CDN}}", "Publi2"), True) Then
					array = New String() { "https://github.com/Hex-Dragon/PCL2/raw/main/%E6%9C%80%E6%96%B0%E6%AD%A3%E5%BC%8F%E7%89%88.zip", "https://gitcode.net/LTCat_/pcl2/-/raw/master/%E6%9C%80%E6%96%B0%E6%AD%A3%E5%BC%8F%E7%89%88.zip?inline=false", CS$<>8__locals1.$VB$Local_BaseUrl }
				Else
					array = New String() { CS$<>8__locals1.$VB$Local_BaseUrl }
				End If
				Dim CS$<>8__locals3 As ModSecret._Closure$__55-1 = CS$<>8__locals2
				Dim text As String = "启动器更新"
				Dim array2 As ModLoader.LoaderBase() = New ModLoader.LoaderBase(1) {}
				array2(0) = New ModNet.LoaderDownload("下载更新文件", New List(Of ModNet.NetFile)() From { New ModNet.NetFile(array, ModBase.schemaStrategy + "Update.zip", New ModBase.FileChecker(1048576L, -1L, Nothing, False, False), False) }) With { .ProgressWeight = 1.0 }
				Dim num As Integer = 1
				Dim text2 As String = "安装更新"
				Dim action As Action(Of ModLoader.LoaderTask(Of String, Integer))
				If ModSecret._Closure$__.$IR55-1 IsNot Nothing Then
					action = ModSecret._Closure$__.$IR55-1
				Else
					Dim action2 As Action(Of ModLoader.LoaderTask(Of String, Integer)) = Sub(a0 As ModLoader.LoaderTask(Of String, Integer))
						Dim vb$AnonymousDelegate_ As VB$AnonymousDelegate_3
						If ModSecret._Closure$__.$I55-1 IsNot Nothing Then
							vb$AnonymousDelegate_ = ModSecret._Closure$__.$I55-1
						Else
							Dim vb$AnonymousDelegate_2 As VB$AnonymousDelegate_3 = Sub()
								Dim text3 As String = ModBase.Path + "PCL\Plain Craft Launcher 2.exe"
								If File.Exists(text3) Then
									File.Delete(text3)
									ModBase.Log("[System] 已清理存在的更新文件：" + text3, ModBase.LogLevel.Normal, "出现错误")
								Else
									ModBase.Log("[System] 无需清理目标更新文件：" + text3, ModBase.LogLevel.Normal, "出现错误")
								End If
								ModBase.ExtractFile(ModBase.schemaStrategy + "Update.zip", ModBase.Path + "PCL\", Nothing)
								File.Delete(ModBase.schemaStrategy + "Update.zip")
								ModBase.Log("[System] 更新文件解压完成", ModBase.LogLevel.Normal, "出现错误")
								If ModSecret._FilterService Then
									ModSecret.definitionService = True
									Return
								End If
								If ModLaunch.m_RuleInfo.State = ModBase.LoadState.Loading Then
									ModMain.Hint("更新已准备就绪，PCL 将在游戏启动完成后重启！", ModMain.HintType.Finish, True)
									While ModLaunch.m_RuleInfo.State = ModBase.LoadState.Loading
										Thread.Sleep(10)
									End While
								End If
								ModSecret.UpdateRestart(True)
							End Sub
							vb$AnonymousDelegate_ = vb$AnonymousDelegate_2
							ModSecret._Closure$__.$I55-1 = vb$AnonymousDelegate_2
						End If
						vb$AnonymousDelegate_()
					End Sub
					action = action2
					ModSecret._Closure$__.$IR55-1 = action2
				End If
				array2(num) = New ModLoader.LoaderTask(Of String, Integer)(text2, action, Nothing, ThreadPriority.Normal) With { .ProgressWeight = 0.1 }
				CS$<>8__locals3.$VB$Local_Loader = New ModLoader.LoaderCombo(Of String)(text, array2)
				CS$<>8__locals2.$VB$Local_Loader.OnStateChanged = Sub(a0 As ModLoader.LoaderBase)
					MyBase._Lambda$__2()
				End Sub
				CS$<>8__locals2.$VB$Local_Loader.Start(Nothing, False)
				If Not ModSecret._FilterService Then
					ModLoader.LoaderTaskbarAdd(Of String)(CS$<>8__locals2.$VB$Local_Loader)
					ModMain._PredicateTask.BtnExtraDownload.ShowRefresh()
				End If
			Catch ex As Exception
				ModBase.Log(ex, "开始启动器更新失败", ModBase.LogLevel.Feedback, "出现错误")
			End Try
		End Sub, False)
	End If
End Sub


    Friend Sub SecretLaunchJvmArgs(ByRef DataList As List(Of String))
        Dim DataJvmCustom As String = Setup.Get("VersionAdvanceJvm", Version:=McVersionCurrent)
        DataList.Insert(0, If(DataJvmCustom = "", Setup.Get("LaunchAdvanceJvm"), DataJvmCustom)) '可变 JVM 参数
        McLaunchLog("当前剩余内存：" & Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024 * 10) / 10 & "G")
        DataList.Add("-Xmn" & Math.Floor(PageVersionSetup.GetRam(McVersionCurrent) * 1024 * 0.15) & "m")
        DataList.Add("-Xmx" & Math.Floor(PageVersionSetup.GetRam(McVersionCurrent) * 1024) & "m")
        If Not DataList.Any(Function(d) d.Contains("-Dlog4j2.formatMsgNoLookups=true")) Then DataList.Add("-Dlog4j2.formatMsgNoLookups=true")
    End Sub

    ''' <summary>
    ''' 打码字符串中的 AccessToken。
    ''' </summary>
    Friend Function SecretFilter(Raw As String, FilterChar As Char) As String
        '打码 "accessToken " 后的内容
        If Raw.Contains("accessToken ") Then
            For Each Token In RegexSearch(Raw, "(?<=accessToken ([^ ]{5}))[^ ]+(?=[^ ]{5})")
                Raw = Raw.Replace(Token, New String(FilterChar, Token.Count))
            Next
        End If
        '打码当前登录的结果
        Dim AccessToken As String = McLoginLoader.Output.AccessToken
        If AccessToken Is Nothing OrElse AccessToken.Length < 10 OrElse Not Raw.ContainsF(AccessToken, True) OrElse
            McLoginLoader.Output.Uuid = McLoginLoader.Output.AccessToken Then 'UUID 和 AccessToken 一样则不打码
            Return Raw
        Else
            Return Raw.Replace(AccessToken, Left(AccessToken, 5) & New String(FilterChar, AccessToken.Length - 10) & Right(AccessToken, 5))
        End If
    End Function

#End Region

#Region "网络鉴权"

    Friend Function SecretCdnSign(UrlWithMark As String)
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Client As WebClient, Optional UseBrowserUserAgent As Boolean = False)
        If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
            Client.Headers("User-Agent") = "LogStatistic" '#4951
        ElseIf UseBrowserUserAgent Then
            Client.Headers("User-Agent") = "PCL2/" & VersionStandardCode & " Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36"
        Else
            Client.Headers("User-Agent") = "PCL2/" & VersionStandardCode
        End If
        Client.Headers("Referer") = "http://" & VersionCode & ".open.pcl2.server/"
        If Url.Contains("api.curseforge.com") Then Client.Headers("x-api-key") = CurseForgeAPIKey
    End Sub
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Request As HttpWebRequest, Optional UseBrowserUserAgent As Boolean = False)
        If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
            Request.UserAgent = "LogStatistic" '#4951
        ElseIf UseBrowserUserAgent Then
            Request.UserAgent = "PCL2/" & VersionStandardCode & " Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36"
        Else
            Request.UserAgent = "PCL2/" & VersionStandardCode
        End If
        Request.Referer = "http://" & VersionCode & ".open.pcl2.server/"
        If Url.Contains("api.curseforge.com") Then Request.Headers("x-api-key") = CurseForgeAPIKey
    End Sub

#End Region

#Region "字符串加解密"

    ''' <summary>
    ''' 获取八位密钥。
    ''' </summary>
    Private Function SecretKeyGet(Key As String) As String
        Return "00000000"
    End Function
    ''' <summary>
    ''' 加密字符串。
    ''' </summary>
    Friend Function SecretEncrypt(SourceString As String, Optional Key As String = "") As String
        Key = SecretKeyGet(Key)
        Dim btKey As Byte() = Encoding.UTF8.GetBytes(Key)
        Dim btIV As Byte() = Encoding.UTF8.GetBytes("87160295")
        Dim des As New DESCryptoServiceProvider
        Using MS As New MemoryStream
            Dim inData As Byte() = Encoding.UTF8.GetBytes(SourceString)
            Using cs As New CryptoStream(MS, des.CreateEncryptor(btKey, btIV), CryptoStreamMode.Write)
                cs.Write(inData, 0, inData.Length)
                cs.FlushFinalBlock()
                Return Convert.ToBase64String(MS.ToArray())
            End Using
        End Using
    End Function
    ''' <summary>
    ''' 解密字符串。
    ''' </summary>
    Friend Function SecretDecrypt(SourceString As String, Optional Key As String = "") As String
        Key = SecretKeyGet(Key)
        Dim btKey As Byte() = Encoding.UTF8.GetBytes(Key)
        Dim btIV As Byte() = Encoding.UTF8.GetBytes("87160295")
        Dim des As New DESCryptoServiceProvider
        Using MS As New MemoryStream
            Dim inData As Byte() = Convert.FromBase64String(SourceString)
            Using cs As New CryptoStream(MS, des.CreateDecryptor(btKey, btIV), CryptoStreamMode.Write)
                cs.Write(inData, 0, inData.Length)
                cs.FlushFinalBlock()
                Return Encoding.UTF8.GetString(MS.ToArray())
            End Using
        End Using
    End Function

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As Object = 0
    Public ThemeDontClick As Integer = 0

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Hint("该版本中不包含主题功能……")
    End Sub
    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Exit Sub
            '顶部条背景
            Dim Brush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            If ThemeNow = 5 Then
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue, ColorSat, 25)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue, ColorSat, 15)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue, ColorSat, 25)})
                FrmMain.PanTitle.Background = Brush
                FrmMain.PanTitle.Background.Freeze()
            ElseIf Not (ThemeNow = 12 OrElse ThemeDontClick = 2) Then
                If TypeOf ColorHueTopbarDelta Is Integer Then
                    Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue - ColorHueTopbarDelta, ColorSat, 48 + ColorLightAdjust)})
                    Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue, ColorSat, 54 + ColorLightAdjust)})
                    Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + ColorHueTopbarDelta, ColorSat, 48 + ColorLightAdjust)})
                Else
                    Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + ColorHueTopbarDelta(0), ColorSat, 48 + ColorLightAdjust)})
                    Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + ColorHueTopbarDelta(1), ColorSat, 54 + ColorLightAdjust)})
                    Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + ColorHueTopbarDelta(2), ColorSat, 48 + ColorLightAdjust)})
                End If
                FrmMain.PanTitle.Background = Brush
                FrmMain.PanTitle.Background.Freeze()
            Else
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue - 21, ColorSat, 53 + ColorLightAdjust)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.33, .Color = New MyColor().FromHSL2(ColorHue - 7, ColorSat, 47 + ColorLightAdjust)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.67, .Color = New MyColor().FromHSL2(ColorHue + 7, ColorSat, 47 + ColorLightAdjust)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + 21, ColorSat, 53 + ColorLightAdjust)})
                FrmMain.PanTitle.Background = Brush
            End If
            '主页面背景
            If Setup.Get("UiBackgroundColorful") Then
                Brush = New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
                Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 20, Math.Min(60, ColorSat) * 0.5, 80)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.9, 90)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 20, Math.Min(60, ColorSat) * 0.5, 80)})
                FrmMain.PanForm.Background = Brush
            Else
                FrmMain.PanForm.Background = New MyColor(245, 245, 245)
            End If
            FrmMain.PanForm.Background.Freeze()
        End Sub)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function
    Friend Function ThemeCheckGold(Optional Code As String = Nothing) As Boolean
        Return False
    End Function
    Friend Function DonateCodeInput() As Boolean?
        Return Nothing
    End Function

#End Region

#Region "更新"

    Public IsUpdateStarted As Boolean = False
    Public IsUpdateWaitingRestart As Boolean = False
    Public Sub UpdateCheckByButton()
        Hint("该版本中不包含更新功能……")
    End Sub
    Public Sub UpdateStart(BaseUrl As String, Slient As Boolean, Optional ReceivedKey As String = Nothing, Optional ForceValidated As Boolean = False)
    End Sub
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
    End Sub
    ''' <summary>
    ''' 确保 PathTemp 下的 Latest.exe 是最新正式版的 PCL，它会被用于整合包打包。
    ''' 如果不是，则下载一个。
    ''' </summary>
    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)
        '注意：如果要自行实现这个功能，请换用另一个文件路径，以免与官方版本冲突
    End Sub

#End Region

#Region "联网通知"

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 服务", Sub() Log("[Server] 该版本中不包含更新通知功能……"), Priority:=ThreadPriority.BelowNormal)

#End Region

End Module
