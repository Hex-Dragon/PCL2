Imports System.ComponentModel

Public Class FormMain

#Region "基础"

    '更新日志
    Private Sub ShowUpdateLog(LastVersion As Integer)
        Dim FeatureCount As Integer = 0, BugCount As Integer = 0
        Dim FeatureList As New List(Of KeyValuePair(Of Integer, String))
        '统计更新日志条目
#If BETA Then
        If LastVersion < 257 Then 'Release 2.3.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复下载部分 Mod、整合包的 Bug"))
            FeatureCount += 4
            BugCount += 4
        End If
        If LastVersion < 255 Then 'Release 2.2.14
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法启动 Minecraft 1.19-Pre1 的 Bug"))
            FeatureCount += 5
            BugCount += 3
        End If
        If LastVersion < 253 Then 'Release 2.2.13
            FeatureList.Add(New KeyValuePair(Of Integer, String)(7, "支持在安装菜单中直接安装 OptiFabric"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持调整 Mod 文件名中中文译名的位置"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "崩溃分析优化，支持分析更多 Forge 相关的崩溃"))
            FeatureCount += 13
            BugCount += 27
        End If
        If LastVersion < 250 Then 'Release 2.2.11
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持将 Mod 文件拖入窗口进行安装"))
            If LastVersion = 246 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法使用中文搜索 Mod 的 Bug"))
            FeatureCount += 8
            BugCount += 33
        End If
        If LastVersion < 246 Then 'Release 2.2.9
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "Mod 下载支持筛选 Forge Mod 与 Fabric Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "大幅修改联机页面布局（作为联机优化的第二个阶段）"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "Mod、整合包下载的综合优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复使用离线登录进入 Forge 的存档可能导致游戏崩溃"))
            FeatureCount += 13
            BugCount += 11
        End If
        If LastVersion < 242 Then 'Release 2.2.7
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复 MC 所包含的一个严重安全漏洞"))
            If LastVersion >= 236 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复可能无法更改登录信息的 Bug"))
        End If
        If LastVersion < 238 Then 'Release 2.2.6
            If LastVersion = 236 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法使用联机模块的 Bug"))
            FeatureCount += 1
            BugCount += 2
        End If
        If LastVersion < 236 Then 'Release 2.2.5
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "网络状态检测支持检测 Windows 防火墙与网络延迟"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "重制右侧页面的切换动画，卡片与提示条将逐个进入退出，并具有独特的位移动画"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "添加 MC 1.18 中要求 Java 17 的检测与提醒"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "优化页面切换、下载的性能"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "允许拖拽按钮的设置第三方登录"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复网络状态检测的结果总为 A 级的 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法下载新版本的 Forge 的 Bug"))
            FeatureCount += 25
            BugCount += 22
        End If
        If LastVersion < 233 Then 'Release 2.2.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(7, "微软账号支持更换皮肤与披风"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持拖拽整合包文件到 PCL2 进行快捷安装"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "Mod 下载支持显示对应的 MC 版本与 Mod 加载器，且可以跳转到 MCBBS 介绍页"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持安装 1.17 OptiFine + Forge"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "使用 Mojang 账号登录将会显示迁移提醒"))
            FeatureCount += 12
            BugCount += 26
        End If
        If LastVersion < 231 Then 'Release 2.2.1, Also 229
            FeatureList.Add(New KeyValuePair(Of Integer, String)(9, "联机功能的早期测试（尚未完成，延迟很高，之后会优化的）"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "添加 1.18 实验性快照 5 的特供下载，且支持列表的联网更新"))
            FeatureCount += 11
            BugCount += 3
        End If
        If LastVersion < 226 Then 'Release 2.1.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持 1.17.1 Forge、Fabric 的安装与启动"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "优化 Forge 下载与安装速度，提高安装稳定性"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "更新内置帮助库，添加数个指南页面与整合包、存档的安装教程"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持将游戏文件夹压缩包作为游戏整合包导入"))
            FeatureCount += 12
            BugCount += 13
        End If
        If LastVersion < 224 Then 'Release 2.1.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "界面与动画综合优化，重新制作主题配色"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "添加 1.18 实验性快照的特供下载"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "更新内置帮助库，添加光影、数据包、Mod 等资源的安装教程"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "游戏崩溃分析优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "若设置为自动更新，PCL2 会在关闭时才替换文件"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "追加大量千万████的可能性"))
            'FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持 1.17 Forge 的安装与启动"))
            FeatureCount += 33
            BugCount += 20
        End If
        If LastVersion < 217 Then 'Release 2.0.5
            FeatureList.Add(New KeyValuePair(Of Integer, String)(7, "可以根据游戏版本自动选择所需的 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "允许在 Java 版本检测不通过时强制指定使用 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "允许为不同版本独立设置 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "Java 选择与搜索的综合优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法使用 Mod、整合包搜索功能的 Bug"))
            FeatureCount += 8
            BugCount += 20
        End If
        If LastVersion < 214 Then 'Release 2.0.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "添加一键关闭所有运行中的 Minecraft 的按钮"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "增加对 MC 1.17 需要 Java 16 的检测与说明"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "Minecraft 崩溃分析优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法修改部分个性化设置的严重 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复 Windows 7 可能无法登录微软账号的严重 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复网络不稳定时安装整合包极易失败的严重 Bug"))
            FeatureCount += 10
            BugCount += 13
        End If
        If LastVersion < 212 Then 'Release 2.0.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(9, "添加帮助页面，且可以自行添加、删除其中的内容"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持安装 MCBBS 格式的整合包"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "自定义主页/帮助页面的 XAML 功能扩展"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "同时安装 OptiFine 与 Forge 时，OptiFine 将作为 Mod 安装"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "优化 CurseForge 搜索"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化多个报错提示，更加人性化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "适配适用于快照版 MC 的 OptiFine"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复进入 OptiFine 下载页面导致卡死的严重 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法启动部分 OptiFine+Forge 版本的 Bug"))
            FeatureCount += 20
            BugCount += 42
        End If
#Else
        '9：大+
        '8：中大+
        '7：中+，大*
        '6：中小型+，中大*
        '5：小型+，中*
        '4：中小*
        '3：小*
        '2：极度严重的 Bug
        '1：严重的 Bug
        If LastVersion < 259 Then 'Snapshot 2.3.1
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "解决了联机人数 ≥3 人时出现的频繁掉线或突发高延迟的问题"))
            FeatureCount += 14
        End If
        If LastVersion < 258 Then 'Snapshot 2.3.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复下载部分 Mod、整合包的 Bug"))
            FeatureCount += 4
            BugCount += 4
        End If
        If LastVersion < 256 Then 'Snapshot 2.2.14
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法启动 Minecraft 1.19-Pre1 的 Bug"))
            FeatureCount += 5
            BugCount += 3
        End If
        If LastVersion < 254 Then 'Snapshot 2.2.13
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持调整 Mod 文件名中中文译名的位置"))
            FeatureCount += 3
            BugCount += 17
        End If
        If LastVersion < 252 Then 'Snapshot 2.2.12
            FeatureList.Add(New KeyValuePair(Of Integer, String)(7, "支持在安装菜单中直接安装 OptiFabric"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "崩溃分析优化，支持分析更多 Forge 相关的崩溃"))
            If LastVersion = 251 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复同时安装 Forge 和 OptiFine 时 OptiFine 无效的 Bug"))
            FeatureCount += 10
            BugCount += 10
        End If
        If LastVersion < 251 Then 'Snapshot 2.2.11
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持将 Mod 文件拖入窗口进行安装"))
            FeatureCount += 2
            BugCount += 16
        End If
        If LastVersion < 249 Then 'Snapshot 2.2.10
            If LastVersion = 247 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法使用中文搜索 Mod 的 Bug"))
            FeatureCount += 6
            BugCount += 17
        End If
        If LastVersion < 247 Then 'Snapshot 2.2.9
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "Mod 下载支持筛选 Forge Mod 与 Fabric Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "Mod、整合包下载的综合优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复使用离线登录进入 Forge 的存档可能导致游戏崩溃"))
            FeatureCount += 8
            BugCount += 4
        End If
        If LastVersion < 245 Then 'Snapshot 2.2.8
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "大幅修改联机页面布局（作为联机优化的第二个阶段）"))
            FeatureCount += 5
            BugCount += 7
        End If
        If LastVersion < 243 Then 'Snapshot 2.2.7
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复 MC 所包含的一个严重安全漏洞"))
            If LastVersion >= 236 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复可能无法更改登录信息的 Bug"))
        End If
        If LastVersion < 239 Then 'Snapshot 2.2.6
            If LastVersion >= 235 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复自动更新由于路径错误失败的 Bug"))
            If LastVersion = 237 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法使用联机模块的 Bug"))
            FeatureCount += 1
            BugCount += 1
        End If
        If LastVersion < 237 Then 'Snapshot 2.2.5
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "允许拖拽按钮的设置第三方登录"))
            If LastVersion = 235 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复在启动游戏时有可能卡死的 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法下载新版本的 Forge 的 Bug"))
            FeatureCount += 2
            BugCount += 3
        End If
        If LastVersion < 235 Then 'Snapshot 2.2.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "网络状态检测支持检测 Windows 防火墙与网络延迟"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "重制右侧页面的切换动画，卡片与提示条将逐个进入退出，并具有独特的位移动画"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "添加 MC 1.18 中要求 Java 17 的检测与提醒"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "优化页面切换、下载的性能"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复网络状态检测的结果总为 A 级的 Bug"))
            FeatureCount += 23
            BugCount += 19
        End If
        If LastVersion < 234 Then 'Snapshot 2.2.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持拖拽整合包文件到 PCL2 进行快捷安装"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持安装 1.17 OptiFine + Forge"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "Mod 详情页面支持跳转 MCBBS 介绍页"))
            FeatureCount += 10
            BugCount += 17
        End If
        If LastVersion < 232 Then 'Snapshot 2.2.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(7, "微软账号支持更换皮肤与披风"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持显示 Mod 与整合包对应的 MC 版本与 Mod 加载器名称"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "使用 Mojang 账号登录将会显示迁移提醒"))
            FeatureCount += 2
            BugCount += 9
        End If
        If LastVersion < 230 Then 'Snapshot 2.2.1, Also 228
            If LastVersion = 227 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "联机功能的小优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "添加 1.18 实验性快照 5 的特供下载，且支持列表的联网更新"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复 Win7 上使用联机功能导致日志文件极大，占用硬盘空间的 Bug"))
            FeatureCount += 12
            BugCount += 4
        End If
        If LastVersion < 227 Then 'Snapshot 2.2.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(9, "新增联机功能的早期测试版本"))
            FeatureCount += 2
            BugCount += 2
        End If
        If LastVersion < 225 Then 'Snapshot 2.1.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "支持 1.17.1 Forge、Fabric 的安装与启动"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "优化 Forge 下载与安装速度，提高安装稳定性"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "更新内置帮助库，添加数个指南页面与整合包、存档的安装教程"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持将游戏文件夹压缩包作为游戏整合包导入"))
            FeatureCount += 12
            BugCount += 13
        End If
        If LastVersion < 223 Then 'Snapshot 2.1.3
            BugCount += 4
        End If
        If LastVersion < 222 Then 'Snapshot 2.1.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "更新内置帮助库，添加光影、数据包、Mod 的安装教程"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "游戏崩溃分析优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复登录微软账号启动游戏时报错的 Bug"))
            FeatureCount += 8
            BugCount += 11
        End If
        If LastVersion < 221 Then 'Snapshot 2.1.1
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "界面与动画综合优化，重新制作主题配色"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "若识别码不变，更新密钥在输入一次后会一直有效"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "若设置为自动更新，PCL2 会在关闭时才替换文件"))
            FeatureCount += 10
            BugCount += 5
        End If
        If LastVersion < 220 Then 'Snapshot 2.1.0
            'FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "界面与动画综合优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "追加大量千万████的可能性"))
            FeatureCount += 15
        End If
        If LastVersion < 219 Then 'Snapshot 2.0.5
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法使用 Mod、整合包搜索功能的 Bug"))
            BugCount += 2
        End If
        If LastVersion < 218 Then 'Snapshot 2.0.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "允许为不同版本独立设置 Java"))
            BugCount += 12
        End If
        If LastVersion < 216 Then 'Snapshot 2.0.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(7, "可以根据游戏版本自动选择所需的 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "允许在 Java 版本检测不通过时强制指定使用 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "Java 选择与搜索的综合优化"))
            FeatureCount += 8
            BugCount += 7
        End If
        If LastVersion < 215 Then 'Snapshot 2.0.2
            If LastVersion = 213 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复账号切换、版本选择下拉框无法展开的严重 Bug"))
            If LastVersion = 213 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复误判部分客户端需要 Java 16，导致无法启动游戏的严重 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复 Windows 7 可能无法登录微软账号的严重 Bug"))
            BugCount += 1
        End If
        If LastVersion < 213 Then 'Snapshot 2.0.1
            FeatureList.Add(New KeyValuePair(Of Integer, String)(6, "添加一键关闭所有运行中的 Minecraft 的按钮"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "增加对 MC 1.17 需要 Java 16 的检测与说明"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "Minecraft 崩溃分析优化"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复网络不稳定时安装整合包极易失败的严重 Bug"))
            FeatureCount += 10
            BugCount += 12
        End If
        If LastVersion < 211 Then 'Snapshot 2.0.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "适配适用于快照版 MC 的 OptiFine"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复进入 OptiFine 下载页面导致卡死的严重 Bug"))
            BugCount += 3
        End If
#End If
        '整理更新日志文本
        Dim ContentList As New List(Of String)
        Dim SortedFeatures = Sort(FeatureList, Function(Left As KeyValuePair(Of Integer, String), Right As KeyValuePair(Of Integer, String)) As Boolean
                                                   Return Left.Key > Right.Key
                                               End Function)
        If SortedFeatures.Count = 0 AndAlso FeatureCount = 0 AndAlso BugCount = 0 Then
            ContentList.Add("龙猫忘记写更新日志啦！可以去提醒他一下……")
        End If
        For i = 0 To Math.Min(9, SortedFeatures.Count - 1) '最多取 10 项
            ContentList.Add(SortedFeatures(i).Value)
        Next
        If SortedFeatures.Count > 10 Then FeatureCount += SortedFeatures.Count - 10
        If FeatureCount > 0 OrElse BugCount > 0 Then
            ContentList.Add(If(FeatureCount > 0, "其他 " & FeatureCount & " 项小调整与修改", "") &
                        If(FeatureCount > 0 AndAlso BugCount > 0, "，", "") &
                        If(BugCount > 0, "修复了 " & BugCount & " 个 Bug", "") &
                        "，详见完整更新日志")
        End If
        Dim Content As String = "· " & Join(ContentList, vbCrLf & "· ")
        '输出更新日志
        RunInNewThread(Sub()
                           If MyMsgBox(Content, "PCL2 已更新至 " & VersionDisplayName, "确定", "完整更新日志") = 2 Then
                               OpenWebsite("https://afdian.net/@LTCat?tab=feed")
                           End If
                       End Sub, "UpdateLog Output")
    End Sub

    '窗口加载
    Private IsWindowLoadFinished As Boolean = False
    Public Shared IsLinkRestart As Boolean = False '是否为联机提权后自动重启
    Public Sub New()
        ApplicationStartTick = GetTimeTick()
        '窗体参数初始化
        FrmMain = Me
        FrmLaunchLeft = New PageLaunchLeft
        FrmLaunchRight = New PageLaunchRight
        '版本号改变
        Dim LastVersion As Integer = Setup.Get("SystemLastVersionReg")
        If LastVersion < VersionCode Then
            '触发升级
            UpgradeSub(LastVersion)
        ElseIf LastVersion > VersionCode Then
            '触发降级
            DowngradeSub(LastVersion)
        End If
        ''刷新语言
        'Lang = ReadReg("Lang", "zh_CN")
        'Application.Current.Resources.MergedDictionaries(1) = New ResourceDictionary With {.Source = New Uri("Resources\Language\" & Lang & ".xaml", UriKind.Relative)}
        '刷新主题
        ThemeCheckAll(False)
        Setup.Load("UiLauncherTheme")
        '加载 UI
        InitializeComponent()
        Opacity = 0
        '切换到首页
        If Not IsNothing(FrmLaunchLeft.Parent) Then FrmLaunchLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If Not IsNothing(FrmLaunchRight.Parent) Then FrmLaunchRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PanMainLeft.Child = FrmLaunchLeft
        PanMainRight.Child = FrmLaunchRight
        FrmLaunchRight.PageState = MyPageRight.PageStates.ContentStay
        If IsLinkRestart Then PageChange(PageType.Link)
        '模式提醒
#If DEBUG Then
        Hint("[开发者模式] PCL 正以开发者模式运行，这可能会造成严重的性能下降，请务必立即向开发者反馈此问题！", HintType.Critical)
#End If
        If ModeDebug Then Hint("[调试模式] PCL 正以调试模式运行，这可能会造成性能的下降，若无必要请不要开启！")
        '尽早执行的加载池
        McFolderListLoader.Start(0) '为了让下载已存在文件检测可以正常运行，必须跑一次；为了让启动按钮尽快可用，需要尽早执行；为了与 PageLaunchLeft 联动，需要为 0 而不是 GetUuid

        Log("[Start] 第二阶段加载用时：" & GetTimeTick() - ApplicationStartTick & " ms")
    End Sub
    Private Sub FormMain_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ApplicationStartTick = GetTimeTick()
        Handle = New Interop.WindowInteropHelper(Me).Handle
        '读取设置
        Setup.Load("UiBackgroundOpacity")
        Setup.Load("UiBackgroundBlur")
        Setup.Load("UiLogoType")
        Setup.Load("UiHiddenPageDownload")
        BackgroundRefresh(False, True)
        MusicRefreshPlay(False, True)
        JavaListInit()
        '扩展按钮
        BtnExtraDownload.ShowCheck = AddressOf BtnExtraDownload_ShowCheck
        BtnExtraBack.ShowCheck = AddressOf BtnExtraBack_ShowCheck
        BtnExtraApril.ShowCheck = AddressOf BtnExtraApril_ShowCheck
        BtnExtraShutdown.ShowCheck = AddressOf BtnExtraShutdown_ShowCheck
        BtnExtraApril.ShowRefresh()
        '初始化尺寸改变
        Dim Resizer As New MyResizer(Me)
        Resizer.addResizerDown(ResizerB)
        Resizer.addResizerLeft(ResizerL)
        Resizer.addResizerLeftDown(ResizerLB)
        Resizer.addResizerLeftUp(ResizerLT)
        Resizer.addResizerRight(ResizerR)
        Resizer.addResizerRightDown(ResizerRB)
        Resizer.addResizerRightUp(ResizerRT)
        Resizer.addResizerUp(ResizerT)
        '#If DEBUG Then
        '        MinWidth = 200
        '        MinHeight = 150
        '#End If
        'PLC 彩蛋
        If RandomInteger(1, 1000) = 23 Then
            ShapeTitleLogo.Data = New GeometryConverter().ConvertFromString("M26,29 v-25 h5 a7,7 180 0 1 0,14 h-5 M80,6.5 a10,11.5 180 1 0 0,18   M47,2.5 v24.5 h12   M98,2 v27   M107,2 v27")
        End If
        '加载窗口
        ThemeRefreshMain()
        Height = ReadReg("WindowHeight", MinHeight + 50)
        Width = ReadReg("WindowWidth", MinWidth + 50)
        Topmost = False
        If FrmStart IsNot Nothing Then FrmStart.Close(New TimeSpan(0, 0, 0, 0, 400 / AniSpeed))
        '更改窗口
        Top = (GetWPFSize(My.Computer.Screen.WorkingArea.Height) - Height) / 2
        Left = (GetWPFSize(My.Computer.Screen.WorkingArea.Width) - Width) / 2
        IsSizeSaveable = True
        ShowWindowToTop()
        Dim HwndSource As Interop.HwndSource = PresentationSource.FromVisual(Me)
        HwndSource.AddHook(New Interop.HwndSourceHook(AddressOf WndProc))
        AniStart({
                     AaCode(Sub() AniControlEnabled -= 1, 50),
                     AaOpacity(Me, Setup.Get("UiLauncherTransparent") / 1000 + 0.4, 300, 100),
                     AaScaleTransform(PanBack, 0.05, 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
                     AaCode(Sub()
                                PanBack.RenderTransform = Nothing
                                'If OsVersion > New Version(10, 1, 0, 0) Then RectForm.RadiusX = 5 : RectForm.RadiusY = 5 'Win11 下圆角
                                IsWindowLoadFinished = True
                                Log("[System] DPI：" & DPI & "，工作区尺寸：" & My.Computer.Screen.WorkingArea.Width & " x " & My.Computer.Screen.WorkingArea.Height & "，系统版本：" & OsVersion.ToString)
                            End Sub, , True)
                 }, "Form Show")
        'Timer 启动
        AniStartRun()
        TimerMainStartRun()
        '加载池
        RunInNewThread(Sub()
                           If Not Setup.Get("SystemEula") Then
Reopen:
                               Select Case MyMsgBox("在使用 PCL2 前，请同意 PCL2 的用户协议与免责声明。", "协议授权", "同意", "拒绝", "打开用户协议与免责声明页面")
                                   Case 1
                                       Setup.Set("SystemEula", True)
                                   Case 2
                                       EndProgram(False)
                                   Case 3
                                       OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW")
                                       GoTo Reopen
                               End Select
                           End If
                           Try
                               Thread.Sleep(200)
                               If Setup.Get("LinkAuto") Then PageLinkIoi.InitLoader.Start()
                               If Not Setup.Get("HintFeedback") = "" Then FeedbackLoader.Start()
                               DlClientListMojangLoader.Start(1)
                               RunCountSub()
                               ServerLoader.Start(1)
                           Catch ex As Exception
                               Log(ex, "初始化加载池运行失败", LogLevel.Feedback)
                           End Try
                           Try
                               If File.Exists(Path & "PCL\Plain Craft Launcher 2.exe") Then File.Delete(Path & "PCL\Plain Craft Launcher 2.exe")
                           Catch ex As Exception
                               Log(ex, "清理自动更新文件失败")
                           End Try
                       End Sub, "Start Loader", ThreadPriority.Lowest)

        Log("[Start] 第三阶段加载用时：" & GetTimeTick() - ApplicationStartTick & " ms")
    End Sub
    '根据打开次数触发的事件
    Private Sub RunCountSub()
        Setup.Set("SystemCount", Setup.Get("SystemCount") + 1)
#If Not BETA Then
        Select Case Setup.Get("SystemCount")
            Case 1
                MyMsgBox("欢迎使用 PCL2 快照版！" & vbCrLf &
                         "快照版包含尚未在正式版发布的测试性功能，仅用于赞助者本人尝鲜。所以请不要发给其他人或者用于制作整合包哦！" & vbCrLf &
                         "如果你并非通过赞助或赞助者本人邀请进群获得的本程序，那么可能是有人在违规传播，记得提醒他一下啦。", "快照版使用说明")
        End Select
        If Setup.Get("SystemCount") >= 99 Then
            If ThemeUnlock(6, False) Then
                MyMsgBox("你已经使用了 99 次 PCL2 啦，感谢你长期以来的支持！" & vbCrLf &
                         "隐藏主题 铁杆粉 已解锁！", "提示")
            End If
        End If
#End If
    End Sub
    '升级与降级事件
    Private Sub UpgradeSub(LastVersionCode As Integer)
        Log("[Start] 版本号从 " & LastVersionCode & " 升高到 " & VersionCode)
        Setup.Set("SystemLastVersionReg", VersionCode)
        '检查有记录的最高版本号
        Dim LowerVersionCode As Integer
#If BETA Then
        LowerVersionCode = Setup.Get("SystemHighestBetaVersionReg")
        If LowerVersionCode < VersionCode Then
            Setup.Set("SystemHighestBetaVersionReg", VersionCode)
            Log("[Start] 最高版本号从 " & LowerVersionCode & " 升高到 " & VersionCode)
        End If
#Else
        LowerVersionCode = Setup.Get("SystemHighestAlphaVersionReg")
        If LowerVersionCode < VersionCode Then
            Setup.Set("SystemHighestAlphaVersionReg", VersionCode)
            Log("[Start] 最高版本号从 " & LowerVersionCode & " 升高到 " & VersionCode)
        End If
#End If
        '修改主题设置项名称
        If LowerVersionCode <= 207 Then
            Dim UnlockedTheme As New List(Of String) From {"2"}
            UnlockedTheme.AddRange(New List(Of String)(Setup.Get("UiLauncherThemeHide").ToString.Split("|")))
            UnlockedTheme.AddRange(New List(Of String)(Setup.Get("UiLauncherThemeHide2").ToString.Split("|")))
            Setup.Set("UiLauncherThemeHide2", Join(ArrayNoDouble(UnlockedTheme), "|"))
        End If
        '重置欧皇彩
        If LastVersionCode <= 115 AndAlso Setup.Get("UiLauncherThemeHide2").ToString.Split("|").Contains("13") Then
            Dim UnlockedTheme As New List(Of String)(Setup.Get("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("13")
            Setup.Set("UiLauncherThemeHide2", Join(UnlockedTheme, "|"))
            MyMsgBox("由于新版 PCL2 修改了欧皇彩的解锁方式，你需要重新解锁欧皇彩。" & vbCrLf &
                     "多谢各位的理解啦！", "重新解锁提醒")
        End If
        '重置滑稽彩
        If LastVersionCode <= 152 AndAlso Setup.Get("UiLauncherThemeHide2").ToString.Split("|").Contains("12") Then
            Dim UnlockedTheme As New List(Of String)(Setup.Get("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("12")
            Setup.Set("UiLauncherThemeHide2", Join(UnlockedTheme, "|"))
            MyMsgBox("由于新版 PCL2 修改了滑稽彩的解锁方式，你需要重新解锁滑稽彩。" & vbCrLf &
                     "多谢各位的理解啦！", "重新解锁提醒")
        End If
        '移动自定义皮肤
        If LastVersionCode <= 161 AndAlso File.Exists(Path & "PCL\CustomSkin.png") AndAlso Not File.Exists(PathTemp & "CustomSkin.png") Then
            File.Copy(Path & "PCL\CustomSkin.png", PathTemp & "CustomSkin.png")
            Log("[Start] 已移动离线自定义皮肤")
        End If
        '解除帮助页面的隐藏
        If LastVersionCode <= 205 Then
            Setup.Set("UiHiddenOtherHelp", False)
            Log("[Start] 已解除帮助页面的隐藏")
        End If
        '输出更新日志
        If LastVersionCode = 0 Then Exit Sub
        If LowerVersionCode >= VersionCode Then Exit Sub
        ShowUpdateLog(LowerVersionCode)
    End Sub
    Private Sub DowngradeSub(LastVersionCode As Integer)
        Log("[Start] 版本号从 " & LastVersionCode & " 降低到 " & VersionCode)
        Setup.Set("SystemLastVersionReg", VersionCode)
    End Sub

#End Region

#Region "自定义窗口"

    '关闭
    Private Sub FormMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        EndProgram(True)
        e.Cancel = True
    End Sub
    ''' <summary>
    ''' 正常关闭程序。程序将在执行此方法后约 0.3s 退出。
    ''' </summary>
    ''' <param name="SendWarning">是否在还有下载任务未完成时发出警告。</param>
    Public Sub EndProgram(SendWarning As Boolean)
        '发出警告
        If SendWarning AndAlso HasDownloadingTask() Then
            If MyMsgBox("还有下载任务尚未完成，是否确定退出？", "提示", "确定", "取消") = 1 Then
                '强行结束下载任务
                RunInNewThread(Sub()
                                   Log("[System] 正在强行停止任务")
                                   For Each Task As LoaderBase In LoaderTaskbar.ToArray
                                       Task.Abort()
                                   Next
                               End Sub, "强行停止下载任务")
            Else
                Exit Sub
            End If
        End If
        '关闭
        RunInUiWait(Sub()
                        IsHitTestVisible = False
                        If PanBack.RenderTransform Is Nothing Then
                            PanBack.RenderTransform = New ScaleTransform
                            AniStart({
                                AaOpacity(Me, -Opacity, 100, 100),
                                AaScaleTransform(PanBack, -0.05, 200,, New AniEaseInBack),
                                AaCode(Sub()
                                           IsHitTestVisible = False
                                           Top = -10000
                                           ShowInTaskbar = False
                                       End Sub, 225),
                                AaCode(AddressOf EndProgramForce, 250)
                            }, "Form Close")
                        Else
                            EndProgramForce()
                        End If
                        Log("[System] 收到关闭指令")
                    End Sub)
    End Sub
    Private Shared IsLogShown As Boolean = False
    Public Shared Sub EndProgramForce(Optional ReturnCode As Result = Result.Success)
        On Error Resume Next
        IsProgramEnded = True
        AniControlEnabled += 1
        PageLinkIoi.IoiStop(False)
        If IsUpdateWaitingRestart Then UpdateRestart(False)
        If ReturnCode = Result.Exception Then
            If Not IsLogShown Then
                FeedbackInfo()
                Log("请在 https://jinshuju.net/f/rP4b6E?x_field_1=crash 提交错误报告，以便于作者解决此问题！")
                IsLogShown = True
                ShellAndGetExitCode(Path & "PCL\Log1.txt")
            End If
            Thread.Sleep(500) '防止 PCL 在记事本打开前就被掐掉
        End If
        Log("[System] 程序已退出，返回值：" & GetStringFromEnum(CType(ReturnCode, [Enum])))
        LogFlush()
        If ReturnCode = Result.Success Then
            Process.GetCurrentProcess.Kill()
        Else
            Environment.Exit(ReturnCode)
        End If
    End Sub
    Private Sub BtnTitleClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleClose.Click
        EndProgram(True)
    End Sub

    '移动
    Private Sub FormDragMove(sender As Object, e As MouseButtonEventArgs) Handles PanTitle.MouseLeftButtonDown, PanMsg.MouseLeftButtonDown
        On Error Resume Next
        If sender.IsMouseDirectlyOver Then DragMove()
    End Sub

    '改变大小
    ''' <summary>
    ''' 是否可以向注册表储存尺寸改变信息。以此避免初始化时误储存。
    ''' </summary>
    Public IsSizeSaveable As Boolean = False
    Private Sub FormMain_SizeChanged() Handles Me.SizeChanged, Me.Loaded
        If IsSizeSaveable Then
            WriteReg("WindowHeight", Height)
            WriteReg("WindowWidth", Width)
        End If
        RectForm.Rect = New Rect(0, 0, BorderForm.ActualWidth, BorderForm.ActualHeight)
        PanForm.Width = BorderForm.ActualWidth + 0.001
        PanForm.Height = BorderForm.ActualHeight + 0.001
        PanMain.Width = PanForm.Width
        PanMain.Height = Math.Max(0, PanForm.Height - PanTitle.ActualHeight)
    End Sub

    '最小化
    Private Sub BtnTitleMin_Click() Handles BtnTitleMin.Click
        WindowState = WindowState.Minimized
    End Sub

#End Region

#Region "窗体事件"

    '按键事件
    Private Sub FormMain_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.IsRepeat Then Exit Sub
        '调用弹窗回车
        If e.Key = Key.Enter AndAlso PanMsg.Children.Count > 0 Then
            CType(PanMsg.Children(0), Object).Btn1_Click()
            Exit Sub
        End If
        '更改隐藏版本可见性
        If e.Key = Key.F11 AndAlso PageCurrent = FormMain.PageType.VersionSelect Then
            FrmSelectRight.ShowHidden = Not FrmSelectRight.ShowHidden
            LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Exit Sub
        End If
        '更改功能隐藏可见性
        If e.Key = Key.F12 Then
            PageSetupUI.HiddenForceShow = Not PageSetupUI.HiddenForceShow
            If PageSetupUI.HiddenForceShow Then
                Hint("功能隐藏设置已暂时关闭！", HintType.Finish)
            Else
                Hint("功能隐藏设置已重新开启！", HintType.Finish)
            End If
            PageSetupUI.HiddenRefresh()
            Exit Sub
        End If
        '调用启动游戏
        If e.Key = Key.Enter AndAlso PageCurrent = FormMain.PageType.Launch Then
            If IsAprilEnabled AndAlso Not IsAprilGiveup Then
                Hint("木大！")
            Else
                FrmLaunchLeft.LaunchButtonClick()
            End If
        End If
        '修复按下 Alt 后误认为弹出系统菜单导致的冻结
        If e.SystemKey = Key.LeftAlt OrElse e.SystemKey = Key.RightAlt Then e.Handled = True
        '按 ESC 返回上一级
        If e.Key = Key.Escape Then TriggerPageBack()
    End Sub
    Private Sub FormMain_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseDown
        '鼠标侧键返回上一级
        If e.ChangedButton = MouseButton.XButton1 OrElse e.ChangedButton = MouseButton.XButton2 Then TriggerPageBack()
    End Sub
    Private Sub TriggerPageBack()
        If PageCurrent = PageType.Download AndAlso PageCurrentSub = PageSubType.DownloadInstall Then
            FrmDownloadInstall.ExitSelectPage()
        Else
            PageBack()
        End If
    End Sub

    '切回窗口
    Private Sub FormMain_Activated() Handles Me.Activated
        Try
            If PageCurrent = PageType.VersionSetup AndAlso PageCurrentSub = PageSubType.VersionMod Then
                'Mod 管理自动刷新
                FrmVersionMod.RefreshList()
            ElseIf PageCurrent = PageType.VersionSelect Then
                '版本选择自动刷新
                LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.RunOnUpdated, MaxDepth:=1, ExtraPath:="versions\")
            End If
        Catch ex As Exception
            Log(ex, "切回窗口时出错", LogLevel.Feedback)
        End Try
    End Sub

    '文件拖放
    Private Sub FrmMain_PreviewDragOver(sender As Object, e As DragEventArgs) Handles Me.PreviewDragOver
        If e.Data.GetFormats.Contains("FileDrop") Then
            e.Effects = DragDropEffects.Link
        Else
            e.Effects = DragDropEffects.None
        End If
    End Sub
    Private Sub FrmMain_Drop(sender As Object, e As DragEventArgs) Handles Me.PreviewDrop
        Try
            If e.Data.GetDataPresent(DataFormats.Text) Then
                '获取文本
                Try
                    Dim Str As String = e.Data.GetData(DataFormats.Text)
                    Log("[System] 接受文本拖拽：" & Str)
                    If Str.StartsWith("authlib-injector:yggdrasil-server:") Then
                        'Authlib 拖拽
                        e.Handled = True
                        Dim AuthlibServer As String = Net.WebUtility.UrlDecode(Str.Substring("authlib-injector:yggdrasil-server:".Length))
                        Log("[System] Authlib 拖拽：" & AuthlibServer)
                        If Not String.IsNullOrEmpty(New ValidateHttp().Validate(AuthlibServer)) Then
                            Hint("输入的 Authlib 验证服务器不符合网址格式（" & AuthlibServer & "）！", HintType.Critical)
                            Exit Sub
                        End If
                        If McVersionCurrent Is Nothing Then
                            Hint("请先下载游戏，再设置第三方登录！", HintType.Critical)
                            Exit Sub
                        End If
                        If AuthlibServer = "https://littleskin.cn/api/yggdrasil" Then
                            'Little Skin
                            If MyMsgBox("是否要在版本 " & McVersionCurrent.Name & " 中开启 Little Skin 登录？" & vbCrLf &
                                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then
                                Exit Sub
                            End If
                            Setup.Set("VersionServerLogin", 4, Version:=McVersionCurrent)
                            Setup.Set("VersionServerAuthServer", "https://littleskin.cn/api/yggdrasil", Version:=McVersionCurrent)
                            Setup.Set("VersionServerAuthRegister", "https://littleskin.cn/auth/register", Version:=McVersionCurrent)
                            Setup.Set("VersionServerAuthName", "Little Skin 登录", Version:=McVersionCurrent)
                        Else
                            '第三方 Authlib 服务器
                            If MyMsgBox("是否要在版本 " & McVersionCurrent.Name & " 中开启第三方登录？" & vbCrLf &
                                        "登录服务器：" & AuthlibServer & vbCrLf & vbCrLf &
                                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then
                                Exit Sub
                            End If
                            Setup.Set("VersionServerLogin", 4, Version:=McVersionCurrent)
                            Setup.Set("VersionServerAuthServer", AuthlibServer, Version:=McVersionCurrent)
                            Setup.Set("VersionServerAuthRegister", AuthlibServer.Replace("api/yggdrasil", "auth/register"), Version:=McVersionCurrent)
                            Setup.Set("VersionServerAuthName", "", Version:=McVersionCurrent)
                        End If
                        If PageCurrent = PageType.VersionSetup AndAlso PageCurrentSub = PageSubType.VersionSetup Then
                            '正在服务器选项页，需要刷新设置项显示
                            FrmVersionSetup.Reload()
                        ElseIf PageCurrent = PageType.Launch Then
                            '正在主页，需要刷新左边栏
                            FrmLaunchLeft.RefreshPage(True, False)
                        End If
                    End If
                Catch ex As Exception
                    Log(ex, "无法接取文本拖拽事件", LogLevel.Developer)
                    Exit Sub
                End Try
            ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
                '获取文件并检查
                Dim FilePathList As New List(Of String)(CType(e.Data.GetData(DataFormats.FileDrop), Array))
                e.Handled = True
                If Directory.Exists(FilePathList.First) AndAlso Not File.Exists(FilePathList.First) Then
                    Hint("请拖入一个文件，而非文件夹！", HintType.Critical)
                    Exit Sub
                End If
                '多文件拖拽
                If FilePathList.Count > 1 Then
                    '必须要求全部为 jar 文件
                    For Each File In FilePathList
                        If File.Split(".").Last.ToLower <> "jar" Then
                            Hint("一次请只拖入一个文件！", HintType.Critical)
                            Exit Sub
                        End If
                    Next
                End If
                '实际执行事件
                Dim FilePath As String = FilePathList.First
                Log("[System] 接受文件拖拽：" & FilePath, LogLevel.Developer)
                RunInNewThread(Sub()
                                   'Mod 安装
                                   If FilePath.Split(".").Last.ToLower = "jar" Then
                                       Log("[System] 文件为 jar 格式，尝试作为 Mod 安装")
                                       '获取并检查目标版本
                                       Dim TargetVersion As McVersion = McVersionCurrent
                                       If PageCurrent = PageType.VersionSetup Then TargetVersion = PageVersionLeft.Version
                                       If PageCurrent = PageType.VersionSelect OrElse TargetVersion Is Nothing OrElse Not TargetVersion.Version.Modable Then
                                           '正在选择版本，或当前版本不能安装 Mod
                                           Hint("若要安装 Mod，请先选择一个可以安装 Mod 的版本！")
                                       ElseIf Not (PageCurrent = PageType.VersionSetup AndAlso PageCurrentSub = PageSubType.VersionMod) Then
                                           '未处于 Mod 管理页面
                                           If MyMsgBox("是否要将这些文件作为 Mod 安装到 " & TargetVersion.Name & "？", "Mod 安装确认", "确定", "取消") = 1 Then GoTo Install
                                       Else
                                           '处于 Mod 管理页面
Install:
                                           Try
                                               For Each ModFile In FilePathList
                                                   File.Copy(ModFile, TargetVersion.PathIndie & "mods\" & GetFileNameFromPath(ModFile), True)
                                               Next
                                               If FilePathList.Count = 1 Then
                                                   Hint("已安装 " & GetFileNameFromPath(FilePathList.First) & "！", HintType.Finish)
                                               Else
                                                   Hint("已安装 " & FilePathList.Count & " 个 Mod！", HintType.Finish)
                                               End If
                                               '刷新列表
                                               If PageCurrent = PageType.VersionSetup AndAlso PageCurrentSub = PageSubType.VersionMod Then
                                                   LoaderFolderRun(McModLoader, TargetVersion.PathIndie & "mods\", LoaderFolderRunType.ForceRun)
                                               End If
                                           Catch ex As Exception
                                               Log(ex, "复制 Mod 文件失败", LogLevel.Msgbox)
                                           End Try
                                       End If
                                       Exit Sub
                                   End If
                                   '安装整合包
                                   If {"zip", "rar"}.Contains(FilePath.Split(".").Last.ToLower) Then '部分压缩包是 zip 格式但后缀为 rar，总之试一试
                                       Log("[System] 文件为压缩包，尝试作为整合包安装")
                                       If ModpackInstall(FilePath, ShowHint:=False) Then Exit Sub
                                   End If
                                   'RAR 处理
                                   If FilePath.Split(".").Last.ToLower = "rar" Then
                                       Hint("PCL2 无法处理 rar 格式的压缩包，请在解压后重新压缩为 zip 格式再试！")
                                       Exit Sub
                                   End If
                                   '错误报告分析
                                   Try
                                       Log("[System] 尝试进行错误报告分析")
                                       Dim Analyzer As New CrashAnalyzer(GetUuid())
                                       Analyzer.Import(FilePath)
                                       If Analyzer.Prepare() = 0 Then Exit Try
                                       Analyzer.Analyze()
                                       Analyzer.Output(True, New List(Of String))
                                       Exit Sub
                                   Catch ex As Exception
                                       Log(ex, "自主错误报告分析失败", LogLevel.Feedback)
                                   End Try
                                   '未知操作
                                   Hint("PCL2 无法确定应当执行的文件拖拽操作……")
                               End Sub, "文件拖拽")
            End If
        Catch ex As Exception
            Log(ex, "接取拖拽事件失败", LogLevel.Feedback)
        End Try
    End Sub

    '接受到 Windows 窗体事件
    Public IsSystemTimeChanged As Boolean = False
    Private Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        If msg = 30 Then
            Dim NowDate = Date.Now
            If NowDate.Date = ApplicationOpenTime.Date Then
                Log("[System] 系统时间微调为：" & NowDate.ToLongDateString & " " & NowDate.ToLongTimeString)
                IsSystemTimeChanged = False
            Else
                Log("[System] 系统时间修改为：" & NowDate.ToLongDateString & " " & NowDate.ToLongTimeString)
                IsSystemTimeChanged = True
            End If
        ElseIf msg = 400 * 16 + 2 Then
            Log("[System] 收到置顶信息：" & hwnd.ToInt64)
            If Not IsWindowLoadFinished Then
                Log("[System] 窗口尚未加载完成，忽略置顶请求")
                Return IntPtr.Zero
            End If
            ShowWindowToTop()
            handled = True
        End If
        Return IntPtr.Zero
    End Function

    '窗口隐藏与置顶
    Private _Hidden As Boolean = False
    Public Property Hidden As Boolean
        Get
            Return _Hidden
        End Get
        Set(ByVal value As Boolean)
            If _Hidden = value Then Exit Property
            _Hidden = value
            If value Then
                '隐藏
                Left -= 10000
                ShowInTaskbar = False
                Visibility = Visibility.Hidden
                'SetWindowLong(Handle, GWL_EXSTYLE, GetWindowLong(Handle, GWL_EXSTYLE) Or WS_EX_TOOLWINDOW)
                Log("[System] 窗口已隐藏，位置：(" & Left & "," & Top & ")")
            Else
                '取消隐藏
                If Left < -2000 Then Left += 10000
                ShowWindowToTop()
            End If
        End Set
    End Property
    ''' <summary>
    ''' 把当前窗口拖到最前面。
    ''' </summary>
    Public Sub ShowWindowToTop()
        RunInUi(Sub()
                    '这一坨乱七八糟的，别改，改了指不定就炸了，自己电脑还复现不出来
                    Visibility = Visibility.Visible
                    ShowInTaskbar = True
                    WindowState = WindowState.Normal
                    Hidden = False
                    Topmost = True '偶尔 SetForegroundWindow 失效
                    Topmost = False
                    SetForegroundWindow(Handle)
                    Focus()
                    'ShowWindow(Handle, 1) 'SW_RESTORE
                    'SetWindowPos(Handle, 0, 0, 0, 0, 0, 3) 'InsertAfter: TOP, NOMOVE & NOSIZE
                    'SetFocus(Handle)
                    Log("[System] 窗口已置顶，位置：(" & Left & ", " & Top & "), " & Width & " x " & Height)
                End Sub)
    End Sub
    'Private Declare Function SetFocus Lib "user32" (hWnd As IntPtr) As IntPtr
    'Private Declare Function SetWindowPos Lib "user32" (hWnd As IntPtr, hWndInsertAfter As IntPtr, x As Integer, y As Integer, cx As Integer, cy As Integer, flags As UInteger) As Boolean
    'Private Declare Function AttachThreadInput Lib "user32" (attachFrom As IntPtr, attachTo As IntPtr, isAttach As Boolean) As Boolean
    'Private Declare Function SendMessage Lib "user32" (hWnd As IntPtr, msg As UInteger, wParam As Long, lParam As Long) As IntPtr

#End Region

#Region "切换页面"

    '页面种类与属性
    ''' <summary>
    ''' 页面种类。
    ''' </summary>
    Public Enum PageType
        ''' <summary>
        ''' 启动。
        ''' </summary>
        Launch
        ''' <summary>
        ''' 下载。
        ''' </summary>
        Download
        ''' <summary>
        ''' 联机。
        ''' </summary>
        Link
        ''' <summary>
        ''' 设置。
        ''' </summary>
        Setup
        ''' <summary>
        ''' 更多。
        ''' </summary>
        Other
        ''' <summary>
        ''' 版本选择。这是一个副页面。
        ''' </summary>
        VersionSelect
        ''' <summary>
        ''' 下载管理。这是一个副页面。
        ''' </summary>
        DownloadManager
        ''' <summary>
        ''' 版本设置。这是一个副页面。
        ''' </summary>
        VersionSetup
        ''' <summary>
        ''' CurseForge 工程详情。这是一个副页面。
        ''' </summary>
        CfDetail
        ''' <summary>
        ''' 帮助详情。这是一个副页面。
        ''' </summary>
        HelpDetail
    End Enum
    ''' <summary>
    ''' 次要页面种类。其数值必须与 StackPanel 中的下标一致。
    ''' </summary>
    Public Enum PageSubType
        [Default] = 0
        DownloadInstall = 1
        DownloadClient = 4
        DownloadOptiFine = 5
        DownloadForge = 6
        DownloadFabric = 7
        DownloadLiteLoader = 8
        DownloadMod = 10
        DownloadPack = 11
        SetupLaunch = 0
        SetupUI = 1
        SetupSystem = 2
        SetupLink = 3
        LinkCato = 2
        LinkIoi = 3
        LinkSetup = 5
        LinkHelp = 6
        LinkFeedback = 7
        OtherHelp = 0
        OtherAbout = 1
        OtherTest = 2
        OtherFeedback = 3
        VersionOverall = 0
        VersionSetup = 1
        VersionMod = 2
        VersionModDisabled = 3
    End Enum
    ''' <summary>
    ''' 获取次级页面的名称。若并非次级页面则返回空字符串，故可以以此判断是否为次级页面。
    ''' </summary>
    Private Function PageNameGet(Stack As PageStackData) As String
        Select Case Stack.Page
            Case PageType.VersionSelect
                Return "版本选择"
            Case PageType.DownloadManager
                Return "下载管理"
            Case PageType.VersionSetup
                Return "版本设置 - " & If(PageVersionLeft.Version Is Nothing, "未知版本", PageVersionLeft.Version.Name)
            Case PageType.CfDetail
                If Stack.Additional Is Nothing Then
                    Log("[Control] CurseForge 工程详情页面未提供关键项", LogLevel.Feedback)
                    Return "未知页面"
                Else
                    Dim Project As DlCfProject = Stack.Additional
                    Return If(Project.IsModPack, "整合包下载 - ", "Mod 下载 - ") & Project.ChineseName
                End If
            Case PageType.HelpDetail
                If Stack.Additional Is Nothing Then
                    Log("[Control] 帮助详情页面未提供关键项", LogLevel.Msgbox)
                    Return "未知页面"
                Else
                    Dim Entry As HelpEntry = Stack.Additional(0)
                    Return Entry.Title
                End If
            Case Else
                Return ""
        End Select
    End Function
    ''' <summary>
    ''' 刷新次级页面的名称。
    ''' </summary>
    Public Sub PageNameRefresh(Type As PageStackData)
        LabTitleInner.Text = PageNameGet(Type)
    End Sub
    ''' <summary>
    ''' 刷新次级页面的名称。
    ''' </summary>
    Public Sub PageNameRefresh()
        PageNameRefresh(PageCurrent)
    End Sub

    '页面状态存储
    ''' <summary>
    ''' 当前的主页面。
    ''' </summary>
    Public PageCurrent As PageStackData = PageType.Launch
    ''' <summary>
    ''' 当前的子页面。
    ''' </summary>
    Public ReadOnly Property PageCurrentSub As PageSubType
        Get
            Select Case PageCurrent
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    Return FrmDownloadLeft.PageID
                Case PageType.Link
                    If FrmLinkLeft Is Nothing Then FrmLinkLeft = New PageLinkLeft
                    Return FrmLinkLeft.PageID
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    Return FrmSetupLeft.PageID
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    Return FrmOtherLeft.PageID
                Case PageType.VersionSetup
                    If FrmVersionLeft Is Nothing Then FrmVersionLeft = New PageVersionLeft
                    Return FrmVersionLeft.PageID
                Case Else
                    Return 0 '没有子页面
            End Select
        End Get
    End Property
    ''' <summary>
    ''' 上层页面的编号堆栈，用于返回。
    ''' </summary>
    Private ReadOnly PageStack As New List(Of PageStackData)
    Public Class PageStackData

        Public Page As PageType
        Public Additional As Object

        Public Overrides Function Equals(other As Object) As Boolean
            If other Is Nothing Then Return False
            If TypeOf other Is PageStackData Then
                Dim PageOther As PageStackData = other
                If Page <> PageOther.Page Then Return False
                If Additional Is Nothing Then
                    Return PageOther.Additional Is Nothing
                Else
                    Return PageOther.Additional IsNot Nothing AndAlso Additional.Equals(PageOther.Additional)
                End If
            ElseIf TypeOf other Is Integer Then
                If Page <> other Then Return False
                Return Additional Is Nothing
            Else
                Return False
            End If
        End Function
        Public Shared Operator =(left As PageStackData, right As PageStackData) As Boolean
            Return EqualityComparer(Of PageStackData).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As PageStackData, right As PageStackData) As Boolean
            Return Not left = right
        End Operator
        Public Shared Widening Operator CType(Value As PageType) As PageStackData
            Return New PageStackData With {.Page = Value}
        End Operator
        Public Shared Widening Operator CType(Value As PageStackData) As PageType
            Return Value.Page
        End Operator
    End Class
    Public PageLeft As MyPageLeft, PageRight As MyPageRight

    '引发实际页面切换的入口
    Private IsChangingPage As Boolean = False
    ''' <summary>
    ''' 切换页面，并引起对应选择 UI 的改变。
    ''' </summary>
    Public Sub PageChange(Stack As PageStackData, Optional SubType As PageSubType = PageSubType.Default)
        If PageNameGet(Stack) = "" Then
            '切换到主页面
            PageChangeExit()
            IsChangingPage = True '防止下面的勾选直接触发了 PageChangeActual
            CType(PanTitleSelect.Children(Stack), MyRadioButton).SetChecked(True, True, PageNameGet(PageCurrent) = "")
            IsChangingPage = False
            Select Case Stack.Page
                Case PageType.Link
                    If FrmLinkLeft Is Nothing Then FrmLinkLeft = New PageLinkLeft
                    CType(FrmLinkLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    CType(FrmDownloadLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    CType(FrmSetupLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    CType(FrmOtherLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
            End Select
            PageChangeActual(Stack, SubType)
        Else
            '切换到次页面
            Select Case Stack.Page
                Case PageType.VersionSetup
                    If FrmVersionLeft Is Nothing Then FrmVersionLeft = New PageVersionLeft
                    CType(FrmVersionLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
            End Select
            PageChangeActual(Stack, SubType)
        End If
    End Sub
    ''' <summary>
    ''' 通过点击导航栏改变页面。
    ''' </summary>
    Private Sub BtnTitleSelect_Click(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnTitleSelect0.Check, BtnTitleSelect1.Check, BtnTitleSelect2.Check, BtnTitleSelect3.Check, BtnTitleSelect4.Check
        If IsChangingPage Then Exit Sub
        PageChangeActual(Val(sender.Tag))
    End Sub
    ''' <summary>
    ''' 通过点击返回按钮或手动触发返回来改变页面。
    ''' </summary>
    Public Sub PageBack() Handles BtnTitleInner.Click
        If PageStack.Count = 0 Then Exit Sub
        PageChangeActual(PageStack(0))
    End Sub

    '实际处理页面切换
    ''' <summary>
    ''' 切换现有页面的实际方法。
    ''' </summary>
    Private Sub PageChangeActual(Stack As PageStackData, Optional SubType As PageSubType = -1)
        If PageCurrent = Stack AndAlso (PageCurrentSub = SubType OrElse SubType = -1) Then Exit Sub
        AniControlEnabled += 1
        Try

#Region "子页面处理"
            Dim PageName As String = PageNameGet(Stack)
            If PageName = "" Then
                '即将切换到一个顶级页面
                PageChangeExit()
            Else
                '即将切换到一个子页面
                If PageStack.Count = 0 Then
                    '主页面 → 子页面，进入
                    PanTitleInner.Visibility = Visibility.Visible
                    PanTitleMain.IsHitTestVisible = False
                    PanTitleInner.IsHitTestVisible = True
                    PageNameRefresh(Stack)
                    AniStart({
                                 AaOpacity(PanTitleMain, -PanTitleMain.Opacity, 150),
                                 AaX(PanTitleMain, 12 - PanTitleMain.Margin.Left, 150,, New AniEaseInFluent(AniEasePower.Weak)),
                                 AaOpacity(PanTitleInner, 1 - PanTitleInner.Opacity, 150, 200),
                                 AaX(PanTitleInner, -PanTitleInner.Margin.Left, 350, 200, New AniEaseOutBack),
                                 AaCode(Sub() PanTitleMain.Visibility = Visibility.Collapsed,, True)
                        }, "FrmMain Titlebar FirstLayer")
                    PageStack.Insert(0, PageCurrent)
                Else
                    '子页面 → 另一个子页面，更新
                    AniStart({
                                 AaOpacity(LabTitleInner, -LabTitleInner.Opacity, 130),
                                 AaCode(Sub() LabTitleInner.Text = PageName,, True),
                                 AaOpacity(LabTitleInner, 1, 150, 30)
                        }, "FrmMain Titlebar SubLayer")
                    If PageStack.Contains(Stack) Then
                        '返回到更上层的子页面
                        Do While PageStack.Contains(Stack)
                            PageStack.RemoveAt(0)
                        Loop
                    Else
                        '进入更深层的子页面
                        PageStack.Insert(0, PageCurrent)
                    End If
                End If
            End If
#End Region

#Region "实际更改页面框架 UI"
            PageCurrent = Stack
            Select Case Stack.Page
                Case PageType.Launch '启动
                    PageChangeAnim(FrmLaunchLeft, FrmLaunchRight)
                Case PageType.Download '下载
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    'PageGet 方法会在未设置 SubType 时指定默认值，并建立相关页面的实例
                    PageChangeAnim(FrmDownloadLeft, FrmDownloadLeft.PageGet(SubType))
                Case PageType.Link '联机
                    If FrmLinkLeft Is Nothing Then FrmLinkLeft = New PageLinkLeft
                    If FrmLinkIoi Is Nothing Then FrmLinkIoi = New PageLinkIoi
                    PageChangeAnim(FrmLinkLeft, FrmLinkLeft.PageGet(SubType))
                Case PageType.Setup '设置
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    PageChangeAnim(FrmSetupLeft, FrmSetupLeft.PageGet(SubType))
                Case PageType.Other '更多
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    PageChangeAnim(FrmOtherLeft, FrmOtherLeft.PageGet(SubType))
                Case PageType.VersionSelect '版本选择
                    If FrmSelectLeft Is Nothing Then FrmSelectLeft = New PageSelectLeft
                    If FrmSelectRight Is Nothing Then FrmSelectRight = New PageSelectRight
                    PageChangeAnim(FrmSelectLeft, FrmSelectRight)
                Case PageType.DownloadManager '下载管理
                    If FrmSpeedLeft Is Nothing Then FrmSpeedLeft = New PageSpeedLeft
                    If FrmSpeedRight Is Nothing Then FrmSpeedRight = New PageSpeedRight
                    PageChangeAnim(FrmSpeedLeft, FrmSpeedRight)
                Case PageType.VersionSetup '版本设置
                    If FrmVersionLeft Is Nothing Then FrmVersionLeft = New PageVersionLeft
                    PageChangeAnim(FrmVersionLeft, FrmVersionLeft.PageGet(SubType))
                Case PageType.CfDetail 'Mod 信息
                    If FrmDownloadCfDetail Is Nothing Then FrmDownloadCfDetail = New PageDownloadCfDetail
                    PageChangeAnim(New MyPageLeft, FrmDownloadCfDetail)
                Case PageType.HelpDetail '帮助详情
                    PageChangeAnim(New MyPageLeft, Stack.Additional(1))
            End Select
#End Region

#Region "设置为最新状态"
            BtnExtraDownload.ShowRefresh()
            BtnExtraApril.ShowRefresh()
#End Region

            Log("[Control] 切换主要页面：" & GetStringFromEnum(Stack) & ", " & SubType)
        Catch ex As Exception
            Log(ex, "切换主要页面失败（ID " & PageCurrent.Page & "）", LogLevel.Feedback)
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub
    Private Sub PageChangeAnim(TargetLeft As FrameworkElement, TargetRight As FrameworkElement)
        AniStop("FrmMain LeftChange")
        AniStop("PageLeft PageChange") '停止左边栏变更导致的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
        AniControlEnabled += 1
        '清除新页面关联性
        If Not IsNothing(TargetLeft.Parent) Then TargetLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If Not IsNothing(TargetRight) AndAlso Not IsNothing(TargetRight.Parent) Then TargetRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PageLeft = TargetLeft
        PageRight = TargetRight
        '触发页面通用动画
        CType(PanMainLeft.Child, MyPageLeft).TriggerHideAnimation()
        CType(PanMainRight.Child, MyPageRight).PageOnExit()
        AniControlEnabled -= 1
        '执行动画
        AniStart({
            AaCode(Sub()
                       AniControlEnabled += 1
                       CType(PanMainRight.Child, MyPageRight).PageOnForceExit()
                       '把新页面添加进容器
                       PanMainLeft.Child = PageLeft
                       PageLeft.Opacity = 0
                       PanMainLeft.Background = Nothing
                       AniControlEnabled -= 1
                       RunInUi(Sub() PanMainLeft_Resize(PanMainLeft.ActualWidth), True)
                   End Sub, 130),
            AaCode(Sub()
                       '延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                       PageLeft.Opacity = 1
                       PageLeft.TriggerShowAnimation()
                   End Sub, 30, True)
            }, "FrmMain PageChangeLeft")
        AniStart({
            AaCode(Sub()
                       AniControlEnabled += 1
                       CType(PanMainRight.Child, MyPageRight).PageOnForceExit()
                       '把新页面添加进容器
                       PanMainRight.Child = PageRight
                       PageRight.Opacity = 0
                       PanMainRight.Background = Nothing
                       AniControlEnabled -= 1
                       RunInUi(Sub() BtnExtraBack.ShowRefresh(), True)
                   End Sub, 130),
            AaCode(Sub()
                       '延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                       PageRight.Opacity = 1
                       PageRight.PageOnEnter()
                   End Sub, 30, True)
            }, "FrmMain PageChangeRight")
    End Sub
    ''' <summary>
    ''' 退出子界面。
    ''' </summary>
    Private Sub PageChangeExit()
        If PageStack.Count = 0 Then
            '主页面 → 主页面，无事发生
        Else
            '子页面 → 主页面，退出
            PanTitleMain.Visibility = Visibility.Visible
            PanTitleMain.IsHitTestVisible = True
            PanTitleInner.IsHitTestVisible = False
            AniStart({
                         AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                         AaX(PanTitleInner, -18 - PanTitleInner.Margin.Left, 150,, New AniEaseInFluent),
                         AaOpacity(PanTitleMain, 1 - PanTitleMain.Opacity, 150, 200),
                         AaX(PanTitleMain, -PanTitleMain.Margin.Left, 350, 200, New AniEaseOutBack(AniEasePower.Weak)),
                         AaCode(Sub() PanTitleInner.Visibility = Visibility.Collapsed,, True)
                }, "FrmMain Titlebar FirstLayer")
            PageStack.Clear()
        End If
    End Sub

    '左边栏改变
    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        If Not e.WidthChanged Then Exit Sub
        PanMainLeft_Resize(e.NewSize.Width)
    End Sub
    Private Sub PanMainLeft_Resize(NewWidth As Double)
        Dim Delta As Double = NewWidth - RectLeftBackground.Width
        If Math.Abs(Delta) < 0.1 Then Exit Sub
        If AniControlEnabled = 0 Then
            If PanMain.Opacity < 0.1 Then PanMainLeft.IsHitTestVisible = False '避免左边栏指向背景未能完美覆盖左边栏
            If NewWidth > 0 Then
                '宽度足够，显示
                AniStart({
                              AaWidth(RectLeftBackground, NewWidth - RectLeftBackground.Width, 400,, New AniEaseOutFluent(AniEasePower.ExtraStrong)),
                              AaOpacity(RectLeftShadow, 1 - RectLeftShadow.Opacity, 200),
                              AaCode(Sub() PanMainLeft.IsHitTestVisible = True, 250)
                         }, "FrmMain LeftChange", True)
            Else
                '宽度不足，隐藏
                AniStart({
                              AaWidth(RectLeftBackground, -RectLeftBackground.Width, 200,, New AniEaseOutFluent),
                              AaOpacity(RectLeftShadow, -RectLeftShadow.Opacity, 200),
                              AaCode(Sub() PanMainLeft.IsHitTestVisible = True, 170)
                         }, "FrmMain LeftChange", True)
            End If
        Else
            RectLeftBackground.Width = NewWidth
            PanMainLeft.IsHitTestVisible = True
            AniStop("FrmMain LeftChange")
        End If
    End Sub

#End Region

#Region "控件拖动"

    '在时钟中调用，使得即使鼠标在窗口外松开，也可以释放控件
    Public Sub DragTick()
        If DragControl Is Nothing Then Exit Sub
        If Not Mouse.LeftButton = MouseButtonState.Pressed Then
            DragStop()
        End If
    End Sub
    '在鼠标移动时调用，以改变 Slider 位置
    Public Sub DragDoing() Handles PanBack.MouseMove
        If DragControl Is Nothing Then Exit Sub
        If Mouse.LeftButton = MouseButtonState.Pressed Then
            DragControl.DragDoing()
        Else
            DragStop()
        End If
    End Sub
    Public Sub DragStop()
        '存在其他线程调用的可能性，因此需要确保在 UI 线程运行
        RunInUi(Sub()
                    If DragControl Is Nothing Then Exit Sub
                    Dim Control = DragControl
                    DragControl = Nothing
                    Control.DragStop() '控件会在该事件中判断 DragControl，所以得放在后面
                End Sub)
    End Sub

#End Region

#Region "附加按钮"

    '音乐
    Private Sub BtnExtraMusic_Click(sender As Object, e As EventArgs) Handles BtnExtraMusic.Click
        MusicControlPause()
    End Sub
    Private Sub BtnExtraMusic_RightClick(sender As Object, e As EventArgs) Handles BtnExtraMusic.RightClick
        MusicControlNext()
    End Sub

    '下载管理
    Private Sub BtnExtraDownload_Click(sender As Object, e As EventArgs) Handles BtnExtraDownload.Click
        PageChange(PageType.DownloadManager)
    End Sub
    Private Function BtnExtraDownload_ShowCheck() As Boolean
        Return HasDownloadingTask() AndAlso Not PageCurrent = PageType.DownloadManager
    End Function

    '投降
    Public Sub AprilGiveup() Handles BtnExtraApril.Click
        If IsAprilEnabled AndAlso Not IsAprilGiveup Then
            Hint("=D", HintType.Finish)
            IsAprilGiveup = True
            FrmLaunchLeft.AprilScaleTrans.ScaleX = 1
            FrmLaunchLeft.AprilScaleTrans.ScaleY = 1
            BtnExtraApril.ShowRefresh()
        End If
    End Sub
    Public Function BtnExtraApril_ShowCheck() As Boolean
        Return IsAprilEnabled AndAlso Not IsAprilGiveup AndAlso PageCurrent = PageType.Launch
    End Function

    '关闭 Minecraft
    Public Sub BtnExtraShutdown_Click() Handles BtnExtraShutdown.Click
        Try
            If McLaunchLoaderReal IsNot Nothing Then McLaunchLoaderReal.Abort()
            For Each Watcher In McWatcherList
                Watcher.Kill()
            Next
            Hint("已关闭运行中的 Minecraft！", HintType.Finish)
        Catch ex As Exception
            Log(ex, "强制关闭所有 Minecraft 失败", LogLevel.Feedback)
        End Try
    End Sub
    Public Function BtnExtraShutdown_ShowCheck() As Boolean
        Return HasRunningMinecraft
    End Function

    '返回顶部
    Private Sub BtnExtraBack_Click(sender As Object, e As EventArgs) Handles BtnExtraBack.Click
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        RealScroll.PerformVerticalOffsetDelta(-RealScroll.VerticalOffset)
    End Sub
    Private Function BtnExtraBack_ShowCheck() As Boolean
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        Return RealScroll IsNot Nothing AndAlso RealScroll.Visibility = Visibility.Visible AndAlso RealScroll.VerticalOffset > Height + If(BtnExtraBack.Show, 0, 1500)
    End Function
    Private Function BtnExtraBack_GetRealChild() As MyScrollViewer
        If PanMainRight.Child Is Nothing Then Return Nothing
        Dim RightChild = CType(PanMainRight.Child, AdornerDecorator).Child
        If RightChild Is Nothing Then
            Return Nothing
        ElseIf TypeOf RightChild Is MyScrollViewer Then
            Return RightChild
        ElseIf TypeOf RightChild Is Grid AndAlso TypeOf CType(RightChild, Grid).Children(0) Is MyScrollViewer Then
            Return CType(RightChild, Grid).Children(0)
        Else
            Return Nothing
        End If
    End Function

#End Region

    '愚人节鼠标位置
    Public lastMouseArg As MouseEventArgs = Nothing
    Private Sub FormMain_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        lastMouseArg = e
    End Sub

End Class
