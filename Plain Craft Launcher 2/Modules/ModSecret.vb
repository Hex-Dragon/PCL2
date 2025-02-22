'由于包含加解密等安全信息，本文件中的部分代码已被删除

Imports System.ComponentModel
Imports System.Net
Imports System.Reflection
Imports System.Security.Cryptography
Imports NAudio.Midi
Imports System.Management
Imports System

Friend Module ModSecret

#Region "杂项"

#If RELEASE Or BETA Then
    Public Const RegFolder As String = "PCLCE" 'PCL 社区版的注册表与 PCL 的注册表隔离，以防数据冲突
#Else
    Public Const RegFolder As String = "PCLCEDebug" '社区开发版的注册表与社区常规版的注册表隔离，以防数据冲突
#End If

    '用于微软登录的 ClientId
    Public Const OAuthClientId As String = ""
    'CurseForge API Key
    Public Const CurseForgeAPIKey As String = ""
    ' LittleSkin OAuth ClientId
    Public Const LittleSkinClientId As String = ""

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
            Environment.[Exit](Result.Cancel)
        End Try
        If Not CheckPermission(Path & "PCL") Then
            MsgBox("PCL 没有对当前文件夹的写入权限，请尝试：" & vbCrLf &
                  "1. 将 PCL 移动到其他文件夹" & If(Path.StartsWithF("C:", True), "，例如 C 盘和桌面以外的其他位置。", "。") & vbCrLf &
                  "2. 删除当前目录中的 PCL 文件夹，然后再试。" & vbCrLf &
                  "3. 右键 PCL 选择属性，打开 兼容性 中的 以管理员身份运行此程序。",
                MsgBoxStyle.Critical, "运行环境错误")
            Environment.[Exit](Result.Cancel)
        End If
        '开源版本提示
        If Setup.Get("UiLauncherCEHint") Then
            MyMsgBox($"你正在使用来自 PCL-Community 的 PCL2 社区版本，遇到问题请不要向官方仓库反馈！
PCL-Community 及其成员与龙腾猫跃无从属关系，且均不会为您的使用做担保。

该版本中暂时无法使用以下特性：
- 更新与联网通知：在做了在做了.jpg
- 主题切换：这是需要赞助解锁的纪念性质的功能，社区版不会制作

该版本中的以下特性与原版有所区别：
- 百宝箱：主线分支没有提供相关内容", "社区版本说明", "我知道了")
        End If
    End Sub

    ''' <summary>
    ''' 获取设备标识码。
    ''' </summary>
    Friend Function SecretGetUniqueAddress() As String
        ' 彩蛋（你居然会无聊到翻源代码）
        Dim code As String = "PCL2-CECE-GOOD-2025"
        Dim rawCode As String = "5202-DOOG-ECEC-2LCP"
        Try
            Dim searcher As New ManagementObjectSearcher("select ProcessorId from Win32_Processor") ' 获取 CPU 序列号
            For Each obj As ManagementObject In searcher.Get()
                rawCode = obj("ProcessorId").ToString()
                Exit For
            Next
            Using sha256 As SHA256 = SHA256.Create() ' SHA256 加密
                Dim hash As Byte() = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawCode))
                code = BitConverter.ToString(hash).Replace("-", "")
            End Using
            Dim sum As Integer = 0
            For Each c As Char In rawCode ' 获取数字和
                If Char.IsDigit(c) Then
                    sum += Val(c)
                End If
            Next
            Dim startIndex = sum + 5
            code = code.Substring(startIndex, 16)
            code = code.Insert(4, "-").Insert(9, "-").Insert(14, "-")
        Catch ex As Exception
            Log(ex, "[Secret] 获取设备标识码失败")
        End Try
        Return code
    End Function

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
            Client.Headers("User-Agent") = "PCL2/" & UpstreamVersion & "." & VersionBranchCode & " PCLCE/" & VersionStandardCode & " Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36"
        Else
            Client.Headers("User-Agent") = "PCL2/" & UpstreamVersion & "." & VersionBranchCode & " PCLCE/" & VersionStandardCode
        End If
        Client.Headers("Referer") = "http://" & VersionCode & ".ce.open.pcl2.server/"
        If Url.Contains("api.curseforge.com") Then Client.Headers("x-api-key") = CurseForgeAPIKey
    End Sub
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Request As HttpWebRequest, Optional UseBrowserUserAgent As Boolean = False)
        If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
            Request.UserAgent = "LogStatistic" '#4951
        ElseIf UseBrowserUserAgent Then
            Request.UserAgent = "PCL2/" & UpstreamVersion & "." & VersionBranchCode & " PCLCE/" & VersionStandardCode & " Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36"
        Else
            Request.UserAgent = "PCL2/" & UpstreamVersion & "." & VersionBranchCode & " PCLCE/" & VersionStandardCode
        End If
        Request.Referer = "http://" & VersionCode & ".ce.open.pcl2.server/"
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

    Public IsDarkMode As Boolean = False

    Public ColorDark1 As New MyColor(235, 235, 235)
    Public ColorDark2 As New MyColor(102, 204, 255)
    Public ColorDark3 As New MyColor(51, 187, 255)
    Public ColorDark6 As New MyColor(93, 101, 103)
    Public ColorDark7 As New MyColor(69, 75, 79)
    Public ColorDark8 As New MyColor(59, 64, 65)
    Public ColorLight1 As New MyColor(52, 61, 74)
    Public ColorLight2 As New MyColor(11, 91, 203)
    Public ColorLight3 As New MyColor(19, 112, 243)
    Public ColorLight6 As New MyColor(213, 230, 253)
    Public ColorLight7 As New MyColor(222, 236, 253)
    Public ColorLight8 As New MyColor(234, 242, 254)
    Public Color1 As MyColor = If(IsDarkMode, ColorDark1, ColorLight1)
    Public Color2 As MyColor = If(IsDarkMode, ColorDark2, ColorLight2)
    Public Color3 As MyColor = If(IsDarkMode, ColorDark3, ColorLight3)
    'Public Color2 As New MyColor(11, 91, 203)
    'Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As MyColor = If(IsDarkMode, ColorDark6, ColorLight6)
    Public Color7 As MyColor = If(IsDarkMode, ColorDark7, ColorLight7)
    Public Color8 As MyColor = If(IsDarkMode, ColorDark8, ColorLight8)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGrayDark1 As New MyColor(245, 245, 245)
    Public ColorGrayDark2 As New MyColor(240, 240, 240)
    Public ColorGrayDark3 As New MyColor(235, 235, 235)
    Public ColorGrayDark4 As New MyColor(204, 204, 204)
    Public ColorGrayDark5 As New MyColor(166, 166, 166)
    Public ColorGrayDark6 As New MyColor(140, 140, 140)
    Public ColorGrayDark7 As New MyColor(115, 115, 115)
    Public ColorGrayDark8 As New MyColor(64, 64, 64)
    Public ColorGrayLight1 As New MyColor(64, 64, 64)
    Public ColorGrayLight2 As New MyColor(115, 115, 115)
    Public ColorGrayLight3 As New MyColor(140, 140, 140)
    Public ColorGrayLight4 As New MyColor(166, 166, 166)
    Public ColorGrayLight5 As New MyColor(204, 204, 204)
    Public ColorGrayLight6 As New MyColor(235, 235, 235)
    Public ColorGrayLight7 As New MyColor(240, 240, 240)
    Public ColorGrayLight8 As New MyColor(245, 245, 245)
    Public ColorGray1 As MyColor = If(IsDarkMode, ColorGrayDark1, ColorGrayLight1)
    Public ColorGray2 As MyColor = If(IsDarkMode, ColorGrayDark2, ColorGrayLight2)
    Public ColorGray3 As MyColor = If(IsDarkMode, ColorGrayDark3, ColorGrayLight3)
    Public ColorGray4 As MyColor = If(IsDarkMode, ColorGrayDark4, ColorGrayLight4)
    Public ColorGray5 As MyColor = If(IsDarkMode, ColorGrayDark5, ColorGrayLight5)
    Public ColorGray6 As MyColor = If(IsDarkMode, ColorGrayDark6, ColorGrayLight6)
    Public ColorGray7 As MyColor = If(IsDarkMode, ColorGrayDark7, ColorGrayLight7)
    Public ColorGray8 As MyColor = If(IsDarkMode, ColorGrayDark8, ColorGrayLight8)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    'Public ColorHue As Integer = If(IsDarkMode, 200, 210), ColorSat As Integer = If(IsDarkMode, 100, 85), ColorLightAdjust As Integer = If(IsDarkMode, 15, 0), ColorHueTopbarDelta As Object = 0
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As Object = 0
    Public ThemeDontClick As Integer = 0

    '深色模式事件

    ' 定义自定义事件
    Public Event ThemeChanged As EventHandler(Of Boolean)

    ' 触发事件的函数
    Public Sub RaiseThemeChanged(isDarkMode As Boolean)
        RaiseEvent ThemeChanged("", isDarkMode)
    End Sub

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        RaiseThemeChanged(IsDarkMode)
        ThemeRefreshColor()
        ThemeRefreshMain()
    End Sub
    Public Function GetDarkThemeLight(OriginalLight As Double) As Double
        If IsDarkMode Then
            Return OriginalLight * 0.1
        Else
            Return OriginalLight
        End If
    End Function
    Public Sub ThemeRefreshColor()
        ColorGray1 = If(IsDarkMode, ColorGrayDark1, ColorGrayLight1)
        ColorGray2 = If(IsDarkMode, ColorGrayDark2, ColorGrayLight2)
        ColorGray3 = If(IsDarkMode, ColorGrayDark3, ColorGrayLight3)
        ColorGray4 = If(IsDarkMode, ColorGrayDark4, ColorGrayLight4)
        ColorGray5 = If(IsDarkMode, ColorGrayDark5, ColorGrayLight5)
        ColorGray6 = If(IsDarkMode, ColorGrayDark6, ColorGrayLight6)
        ColorGray7 = If(IsDarkMode, ColorGrayDark7, ColorGrayLight7)
        ColorGray8 = If(IsDarkMode, ColorGrayDark8, ColorGrayLight8)

        If IsDarkMode Then
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(ColorDark1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(ColorDark2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(ColorDark3)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(ColorDark6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(ColorDark7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(ColorDark8)
            Application.Current.Resources("ColorBrushGray1") = New SolidColorBrush(ColorGrayDark1)
            Application.Current.Resources("ColorBrushGray2") = New SolidColorBrush(ColorGrayDark2)
            Application.Current.Resources("ColorBrushGray3") = New SolidColorBrush(ColorGrayDark3)
            Application.Current.Resources("ColorBrushGray4") = New SolidColorBrush(ColorGrayDark4)
            Application.Current.Resources("ColorBrushGray5") = New SolidColorBrush(ColorGrayDark5)
            Application.Current.Resources("ColorBrushGray6") = New SolidColorBrush(ColorGrayDark6)
            Application.Current.Resources("ColorBrushGray7") = New SolidColorBrush(ColorGrayDark7)
            Application.Current.Resources("ColorBrushGray8") = New SolidColorBrush(ColorGrayDark8)
            Application.Current.Resources("ColorBrushHalfWhite") = New SolidColorBrush(Color.FromArgb(85, 90, 90, 90))
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorDark2)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(Color.FromArgb(190, 90, 90, 90))
            Application.Current.Resources("ColorBrushBackgroundTransparentSidebar") = New SolidColorBrush(Color.FromArgb(235, 43, 43, 43))
            Application.Current.Resources("ColorBrushTransparent") = New SolidColorBrush(Color.FromArgb(0, 43, 43, 43))
            Application.Current.Resources("ColorBrushToolTip") = New SolidColorBrush(Color.FromArgb(229, 90, 90, 90))
            Application.Current.Resources("ColorBrushWhite") = New SolidColorBrush(Color.FromRgb(43, 43, 43))
            Application.Current.Resources("ColorBrushMsgBox") = New SolidColorBrush(Color.FromRgb(43, 43, 43))
            Application.Current.Resources("ColorBrushMsgBoxText") = New SolidColorBrush(ColorDark1)
            Application.Current.Resources("ColorBrushMemory") = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        Else
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(ColorLight1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(ColorLight2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(ColorLight3)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(ColorLight6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(ColorLight7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(ColorLight8)
            Application.Current.Resources("ColorBrushGray1") = New SolidColorBrush(ColorGrayLight1)
            Application.Current.Resources("ColorBrushGray2") = New SolidColorBrush(ColorGrayLight2)
            Application.Current.Resources("ColorBrushGray3") = New SolidColorBrush(ColorGrayLight3)
            Application.Current.Resources("ColorBrushGray4") = New SolidColorBrush(ColorGrayLight4)
            Application.Current.Resources("ColorBrushGray5") = New SolidColorBrush(ColorGrayLight5)
            Application.Current.Resources("ColorBrushGray6") = New SolidColorBrush(ColorGrayLight6)
            Application.Current.Resources("ColorBrushGray7") = New SolidColorBrush(ColorGrayLight7)
            Application.Current.Resources("ColorBrushGray8") = New SolidColorBrush(ColorGrayLight8)
            Application.Current.Resources("ColorBrushHalfWhite") = New SolidColorBrush(Color.FromArgb(85, 255, 255, 255))
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorBrushBackgroundTransparentSidebar") = New SolidColorBrush(Color.FromArgb(210, 255, 255, 255))
            Application.Current.Resources("ColorBrushTransparent") = New SolidColorBrush(Color.FromArgb(0, 255, 255, 255))
            Application.Current.Resources("ColorBrushToolTip") = New SolidColorBrush(Color.FromArgb(229, 255, 255, 255))
            Application.Current.Resources("ColorBrushWhite") = New SolidColorBrush(Color.FromRgb(255, 255, 255))
            Application.Current.Resources("ColorBrushMsgBox") = New SolidColorBrush(Color.FromRgb(251, 251, 251))
            Application.Current.Resources("ColorBrushMsgBoxText") = New SolidColorBrush(ColorLight1)
            Application.Current.Resources("ColorBrushMemory") = New SolidColorBrush(Color.FromRgb(0, 0, 0))
        End If
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
                Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 20, Math.Min(60, ColorSat) * 0.5, GetDarkThemeLight(80))})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.9, GetDarkThemeLight(90))})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 20, Math.Min(60, ColorSat) * 0.5, GetDarkThemeLight(80))})
                FrmMain.PanForm.Background = Brush
            Else
                FrmMain.PanForm.Background = New MyColor(If(IsDarkMode, 20, 245), If(IsDarkMode, 20, 245), If(IsDarkMode, 20, 245))
            End If
            FrmMain.PanForm.Background.Freeze()
        End Sub)
    End Sub
    Public Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Public Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function
    Public Function ThemeCheckGold(Optional Code As String = Nothing) As Boolean
        Return False
    End Function
    Public Function DonateCodeInput() As Boolean?
        Return Nothing
    End Function

#End Region

#Region "更新"

    Public IsUpdateStarted As Boolean = False
    Public IsUpdateWaitingRestart As Boolean = False
    Public LatestVersion As String = VersionBaseName
    Public LatestVersionCode As Integer = VersionCode
    Public Const PysioServer As String = "https://minioapi.pysio.online/pcl2-ce/"
    Private RemoteFileName As String = "PCL2_CE.exe"
    Public Sub UpdateCheckByButton()
        Hint("正在获取更新信息...")
        If IsUpdateStarted Then
            Exit Sub
        End If
        RunInNewThread(Sub()
                           Try
                               UpdateLatestVersionInfo()
                               NoticeUserUpdate()
                           Catch ex As Exception
                               Log(ex, "[Update] 获取启动器更新信息失败", LogLevel.Hint)
                               Hint("获取启动器更新信息失败，请检查网络连接", HintType.Critical)
                           End Try
                       End Sub)
    End Sub
    Public Sub UpdateLatestVersionInfo()
        Log("[System] 正在获取版本信息")
        Dim LatestReleaseInfoJson As JObject = Nothing
        Dim Server As String = Nothing
        Dim IsBeta As Boolean = Setup.Get("SystemSystemUpdateBranch")
        Log($"[System] 启动器为 Fast Ring：{IsBeta}")
        If Setup.Get("SystemSystemServer") = 0 Then 'Pysio 源
            Log("[System] 使用 Pysio 源获取版本信息")
            If IsArm64System Then
                Server = PysioServer + "updateARM_v2.json"
            Else
                Server = PysioServer + "update_v2.json"
            End If
        Else 'GitHub 源
            Log("[System] 使用 GitHub 源获取版本信息")
            If IsArm64System Then
                Server = "https://github.com/PCL-Community/PCL2_CE_Server/raw/main/updateARM_v2.json"
            Else
                Server = "https://github.com/PCL-Community/PCL2_CE_Server/raw/main/update_v2.json"
            End If
        End If
        LatestReleaseInfoJson = GetJson(NetRequestRetry(Server, "GET", "", "application/x-www-form-urlencoded"))
        LatestVersion = LatestReleaseInfoJson("latests")(If(IsBeta, "fast", "slow"))("version").ToString()
        LatestVersionCode = LatestReleaseInfoJson("latests")(If(IsBeta, "fast", "slow"))("code")
        RemoteFileName = LatestReleaseInfoJson("latests")(If(IsBeta, "fast", "slow"))("file").ToString()
    End Sub

    Public Sub NoticeUserUpdate(Optional Silent As Boolean = False)
        If LatestVersionCode > VersionCode Then
            If Not Val(Environment.OSVersion.Version.ToString().Split(".")(2)) >= 19042 AndAlso Not LatestVersion.StartsWithF("2.9.") Then
                If MyMsgBox($"发现了启动器更新（版本 {LatestVersion}），但是由于你的 Windows 版本过低，不满足新版本要求。{vbCrLf}你需要更新到 Windows 10 20H2 或更高版本才可以继续更新。", "启动器更新 - 系统版本过低", "升级 Windows 10", "取消", IsWarn:=True, ForceWait:=True) = 1 Then OpenWebsite("https://www.microsoft.com/zh-cn/software-download/windows10")
                Exit Sub
            End If
            If MyMsgBox($"启动器有新版本可用（｛VersionBaseName｝ -> {LatestVersion}），是否更新？", "启动器更新", "更新", "取消") = 1 Then
                UpdateStart(LatestVersion, False)
            End If
        Else
            If Not Silent Then Hint("启动器已是最新版 " + VersionBaseName + "，无须更新啦！", HintType.Finish)
        End If
    End Sub
    Public Sub UpdateStart(VersionStr As String, Slient As Boolean, Optional ReceivedKey As String = Nothing, Optional ForceValidated As Boolean = False)
        Dim DlLink As String = Nothing
        If Setup.Get("SystemSystemServer") = 0 Then 'Pysio 源
            DlLink = PysioServer + RemoteFileName
        Else 'GitHub 源
            DlLink = "https://github.com/PCL-Community/PCL2-CE/releases/download/" + VersionStr + "/" + RemoteFileName
        End If
        Dim DlTargetPath As String = Path + "PCL\Plain Craft Launcher 2.exe"
        RunInNewThread(Sub()
                           Try
                               '构造步骤加载器
                               Dim Loaders As New List(Of LoaderBase)
                               '下载
                               Dim Address As New List(Of String)
                               Address.Add(DlLink)
                               Loaders.Add(New LoaderDownload("下载更新文件", New List(Of NetFile) From {New NetFile(Address.ToArray, DlTargetPath, New FileChecker(MinSize:=1024 * 64))}) With {.ProgressWeight = 15})
                               If Not Slient Then
                                   Loaders.Add(New LoaderTask(Of Integer, Integer)("安装更新", Sub() UpdateRestart(True)))
                               End If
                               '启动
                               Dim Loader As New LoaderCombo(Of JObject)("启动器更新", Loaders)
                               Loader.Start()
                               If Slient Then
                                   IsUpdateWaitingRestart = True
                               Else
                                   LoaderTaskbarAdd(Loader)
                                   FrmMain.BtnExtraDownload.ShowRefresh()
                                   FrmMain.BtnExtraDownload.Ribble()
                               End If
                           Catch ex As Exception
                               Log(ex, "[Update] 下载启动器更新文件失败", LogLevel.Hint)
                               Hint("下载启动器更新文件失败，请检查网络连接", HintType.Critical)
                           End Try
                       End Sub)
    End Sub
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
        Try
            Dim fileName As String = Path + "PCL\Plain Craft Launcher 2.exe"
            If Not File.Exists(fileName) Then
                Log("[System] 更新失败：未找到更新文件")
                Exit Sub
            End If
            ' id old new restart
            Dim text As String = String.Concat(New String() {"--update ", Process.GetCurrentProcess().Id, " """, PathWithName, """ """, fileName, """ ", TriggerRestartAndByEnd})
            Log("[System] 更新程序启动，参数：" + text, LogLevel.Normal, "出现错误")
            Process.Start(New ProcessStartInfo(fileName) With {.WindowStyle = ProcessWindowStyle.Hidden, .CreateNoWindow = True, .Arguments = text})
            If TriggerRestartAndByEnd Then
                FrmMain.EndProgram(False)
                Log("[System] 已由于更新强制结束程序", LogLevel.Normal, "出现错误")
            End If
        Catch ex As Win32Exception
            Log(ex, "自动更新时触发 Win32 错误，疑似被拦截", LogLevel.Debug, "出现错误")
            If MyMsgBox(String.Format("由于被 Windows 安全中心拦截，或者存在权限问题，导致 PCL 无法更新。{0}请将 PCL 所在文件夹加入白名单，或者手动用 {1}PCL\Plain Craft Launcher 2.exe 替换当前文件！", vbCrLf, ModBase.Path), "更新失败", "查看帮助", "确定", "", True, True, False, Nothing, Nothing, Nothing) = 1 Then
                TryStartEvent("打开帮助", "启动器/Microsoft Defender 添加排除项.json")
            End If
        End Try
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
        Try
            Dim ps = Process.GetProcessById(ProcessId)
            If Not ps.HasExited Then
                ps.Kill()
            End If
        Catch ex As Exception
        End Try
        Dim ex2 As Exception = Nothing
        Dim num As Integer = 0
        Do
            Try
                If File.Exists(OldFileName) Then
                    File.Delete(OldFileName)
                End If
                If Not File.Exists(OldFileName) Then
                    Exit Try
                End If
            Catch ex3 As Exception
                ex2 = ex3
            Finally
                Thread.Sleep(500)
            End Try
            num += 1
        Loop While num <= 4
        If (Not File.Exists(OldFileName)) AndAlso File.Exists(NewFileName) Then
            Try
                CopyFile(NewFileName, OldFileName)
            Catch ex4 As UnauthorizedAccessException
                MsgBox("PCL 更新失败：权限不足。请手动复制 PCL 文件夹下的新版本程序。" & vbCrLf & "若 PCL 位于桌面或 C 盘，你可以尝试将其挪到其他文件夹，这可能可以解决权限问题。" & vbCrLf + GetExceptionSummary(ex4), MsgBoxStyle.Critical, "更新失败")
            Catch ex5 As Exception
                MsgBox("PCL 更新失败：无法复制新文件。请手动复制 PCL 文件夹下的新版本程序。" & vbCrLf + GetExceptionSummary(ex5), MsgBoxStyle.Critical, "更新失败")
                Return
            End Try
            If TriggerRestart Then
                Try
                    Process.Start(OldFileName)
                Catch ex6 As Exception
                    MsgBox("PCL 更新失败：无法重新启动。" & vbCrLf + GetExceptionSummary(ex6), MsgBoxStyle.Critical, "更新失败")
                End Try
            End If
            Return
        End If
        If TypeOf ex2 Is UnauthorizedAccessException Then
            MsgBox(String.Concat(New String() {"由于权限不足，PCL 无法完成更新。请尝试：" & vbCrLf,
                                 If((Path.StartsWithF(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), False) OrElse Path.StartsWithF(Environment.GetFolderPath(Environment.SpecialFolder.Personal), False)),
                                 " - 将 PCL 文件移动到桌面、文档以外的文件夹（这或许可以一劳永逸地解决权限问题）" & vbCrLf, ""),
                                 If(Path.StartsWithF("C", True),
                                 " - 将 PCL 文件移动到 C 盘以外的文件夹（这或许可以一劳永逸地解决权限问题）" & vbCrLf, ""),
                                 " - 右键以管理员身份运行 PCL" & vbCrLf & " - 手动复制已下载到 PCL 文件夹下的新版本程序，覆盖原程序" & vbCrLf & vbCrLf,
                                 GetExceptionSummary(ex2)}), MsgBoxStyle.Critical, "更新失败")
            Return
        End If
        MsgBox("PCL 更新失败：无法删除原文件。请手动复制已下载到 PCL 文件夹下的新版本程序覆盖原程序。" & vbCrLf + GetExceptionSummary(ex2), MsgBoxStyle.Critical, "更新失败")
    End Sub

#End Region

#Region "联网通知"

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 服务", AddressOf LoadOnlineInfo, Priority:=ThreadPriority.BelowNormal)

    Private Sub LoadOnlineInfo()
        Select Case Setup.Get("SystemSystemUpdate")
            Case 0
                UpdateLatestVersionInfo()
                If LatestVersionCode > VersionCode Then
                    UpdateStart(LatestVersion, True) '静默更新
                End If
            Case 1
                UpdateLatestVersionInfo()
                NoticeUserUpdate(True)
            Case 2, 3
                Exit Sub
        End Select
    End Sub

#End Region

End Module
