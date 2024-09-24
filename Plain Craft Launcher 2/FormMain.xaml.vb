Imports System.ComponentModel

Public Class FormMain

#Region "基础"

    '更新日志
    Private Sub ShowUpdateLog(LastVersion As Integer)
        Dim FeatureCount As Integer = 0, BugCount As Integer = 0
        Dim FeatureList As New List(Of KeyValuePair(Of Integer, String))
        '统计更新日志条目
#If BETA Then
        If LastVersion < 336 Then 'Release 2.8.6
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "下载 Mod 时会使用 MCIM 国内镜像源"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "Mod 管理页面允许筛选可更新/启用/禁用的 Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "打开 PCL 时会自动安装同目录下的 modpack.zip"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "爱发电域名迁移至 afdian.com"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复 1.20.1+ 离线登录使用正版皮肤时无法保存游戏的 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复安装的 1.14~1.15 Forge+OptiFine 无法进入世界的 Bug"))
            FeatureCount += 19
            BugCount += 24
        End If
        If LastVersion < 332 Then 'Release 2.8.3
            If LastVersion = 330 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复部分玩家无法启动 MC 的 Bug"))
        End If
        If LastVersion < 330 Then 'Release 2.8.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "NeoForge 兼容与自动安装"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持编译、运行 PCL 开源代码"))
            FeatureCount += 15
            BugCount += 22
        End If
        If LastVersion < 326 Then 'Release 2.7.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "会自动隐藏明显不可用的自动安装选项"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化正版登录流程和 MC 性能"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复正版登录时弹出脚本错误提示的 Bug"))
            FeatureCount += 17
            BugCount += 19
        End If
        If LastVersion < 323 Then 'Release 2.7.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "添加 启动游戏前进行内存优化 设置"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化 MC 性能"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复安装 OptiFine 有概率失败的 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复启动 Fabric 1.20.5+ 时无法正确选择 Java 的 Bug"))
            FeatureCount += 22
            BugCount += 21
        End If
        If LastVersion < 321 Then 'Release 2.7.1
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复启动部分整合包导致设置丢失的 Bug"))
            BugCount += 1
        End If
        If LastVersion < 319 Then 'Release 2.7.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持更新 Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持查看可更新的 Mod 的更新日志"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持滑动鼠标快速选中、取消选中多个 Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法启动 MC 24w14a+ 的 Bug"))
            FeatureCount += 10
            BugCount += 10
        End If
        If LastVersion < 317 Then 'Release 2.6.15
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法安装 Forge 的 Bug"))
            FeatureCount += 2
            BugCount += 2
        End If
        If LastVersion < 315 Then 'Release 2.6.14
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化 MC 下载，尝试解决各种导致 MC 下载失败的问题"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "由于 MCBBS 关站，移除其相关内容"))
            FeatureCount += 17
            BugCount += 16
        End If
        If LastVersion < 313 Then 'Release 2.6.13
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法启动 Forge 1.18.3+ 的 Bug"))
            FeatureCount += 6
            BugCount += 10
        End If
        If LastVersion < 311 Then 'Release 2.6.12
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "Mod 管理页面将显示 Mod 的中文名、图标、标签等信息"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "从 Mod 管理页面查看 Mod 信息时会跳转到其下载详情页面"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "重新设计 Mod 管理页面的交互与样式"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法安装 Forge 1.18.3+ 的 Bug"))
            FeatureCount += 27
            BugCount += 25
        End If
        If LastVersion < 308 Then 'Release 2.6.10
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "为版本独立设置添加忽略 Java 兼容性警告选项"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复开始或结束游戏时可能报错的 Bug"))
            FeatureCount += 31
            BugCount += 37
        End If
        If LastVersion < 302 Then 'Release 2.6.7
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "再次修复网络条件差时可能无法下载 MC 的 Bug"))
            BugCount += 1
        End If
        If LastVersion < 300 Then 'Release 2.6.6
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持选择多种预设的主页"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "Mod 管理中允许多选 Mod 进行批量操作"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化崩溃分析，添加多种崩溃情况的判断"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化联网获取的主页的加载与缓存"))
            FeatureCount += 40
            BugCount += 34
        End If
        If LastVersion < 296 Then 'Release 2.6.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增内存优化功能，可以将所有程序的物理内存占用降低约 1/3"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "在选择 OptiFine 与 Fabric 后会自动选择 OptiFabric"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化崩溃分析，添加多种崩溃情况的判断"))
            FeatureCount += 21
            BugCount += 30
        End If
        If LastVersion < 293 Then 'Release 2.6.1
            BugCount += 1
        End If
        If LastVersion < 292 Then 'Release 2.6.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "支持在多个正版账号间切换"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "在缺少 Java 时会自动下载所需的 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "重新制作正版登录页面"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "彻底移除 Mojang 登录"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "添加 CurseForge / Modrinth 来源筛选"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "Mod / 整合包下载会单独列出筛选的版本"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "为下载管理和音乐播放按钮添加进度条"))
            FeatureCount += 24
            BugCount += 24
        End If
        If LastVersion < 286 Then 'Release 2.5.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持搜索、安装 Modrinth 的 Mod 与整合包"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "资源搜索页面支持翻页"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "在全局启动设置中添加了 启动前执行命令 选项"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "重做资源下载的资源项界面"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "在选择下载 Fabric 时会自动选择 Fabric API"))
            FeatureCount += 35
            BugCount += 36
        End If
#Else
        '5：          FEAT+
        '4：     IMP+ FEAT*
        '3：BUG+ IMP* FEAT-
        '2：BUG* IMP-
        '1：BUG-
        If LastVersion < 335 Then 'Snapshot 2.8.6
            BugCount += 2
        End If
        If LastVersion < 334 Then 'Snapshot 2.8.5
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "Mod 管理页面允许筛选可更新/启用/禁用的 Mod"))
            If LastVersion = 333 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法安装愚人节和预发布版本的 Bug"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法导出错误报告的 Bug"))
            End If
            FeatureCount += 6
            BugCount += 7
        End If
        If LastVersion < 333 Then 'Snapshot 2.8.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "下载 Mod 时会使用 MCIM 国内镜像源"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "打开 PCL 时会自动安装同目录下的 modpack.zip"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "爱发电域名迁移至 afdian.com"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复 1.20.1+ 离线登录使用正版皮肤时无法保存游戏的 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复安装的 1.14~1.15 Forge+OptiFine 无法进入世界的 Bug"))
            FeatureCount += 13
            BugCount += 17
        End If
        If LastVersion < 331 Then 'Snapshot 2.8.3
            If LastVersion = 329 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复部分玩家无法启动 MC 的 Bug"))
        End If
        If LastVersion < 329 Then 'Snapshot 2.8.2
            If LastVersion >= 327 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法安装 Beta 版 NeoForge 的整合包的 Bug"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复自动安装无法选择部分 OptiFine 的 Bug"))
            End If
            FeatureCount += 4
            BugCount += 8
        End If
        If LastVersion < 328 Then 'Snapshot 2.8.1
            If LastVersion = 327 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法安装 Forge 1.12.2- 的 Bug"))
            If LastVersion = 327 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法输入解锁码的 Bug"))
            If LastVersion = 327 Then BugCount += 1
        End If
        If LastVersion < 327 Then 'Snapshot 2.8.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "NeoForge 兼容与自动安装"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持编译、运行 PCL 开源代码"))
            FeatureCount += 11
            BugCount += 14
        End If
        If LastVersion < 325 Then 'Snapshot 2.7.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "会自动隐藏明显不可用的自动安装选项"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化正版登录流程和 MC 性能"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复正版登录时弹出脚本错误提示的 Bug"))
            FeatureCount += 17
            BugCount += 19
        End If
        If LastVersion < 324 Then 'Snapshot 2.7.3
            FeatureCount += 4
            BugCount += 3
        End If
        If LastVersion < 322 Then 'Snapshot 2.7.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "添加 启动游戏前进行内存优化 设置"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复安装 OptiFine 有概率失败的 Bug"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复启动 Fabric 1.20.5+ 时无法正确选择 Java 的 Bug"))
            FeatureCount += 18
            BugCount += 18
        End If
        If LastVersion < 320 Then 'Snapshot 2.7.1
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复启动部分整合包导致设置丢失的 Bug"))
            BugCount += 1
        End If
        If LastVersion < 318 Then 'Snapshot 2.7.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持更新 Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持查看可更新的 Mod 的更新日志"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持滑动鼠标快速选中、取消选中多个 Mod"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法启动 MC 24w14a+ 的 Bug"))
            FeatureCount += 10
            BugCount += 10
        End If
        If LastVersion < 316 Then 'Snapshot 2.6.15
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复无法安装 Forge 的 Bug"))
            FeatureCount += 2
            BugCount += 2
        End If
        If LastVersion < 314 Then 'Snapshot 2.6.14
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化 MC 下载，尝试解决各种导致 MC 下载失败的问题"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "由于 MCBBS 关站，移除其相关内容"))
            FeatureCount += 17
            BugCount += 16
        End If
        If LastVersion < 312 Then 'Snapshot 2.6.13
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法启动 Forge 1.18.3+ 的 Bug"))
            FeatureCount += 6
            BugCount += 10
        End If
        If LastVersion < 310 Then 'Snapshot 2.6.12
            If LastVersion = 309 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "Mod 管理页面添加启用、禁用单个 Mod 的快捷按钮"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复无法安装 Forge 1.18.3+ 的 Bug"))
            FeatureCount += 13
            BugCount += 12
        End If
        If LastVersion < 309 Then 'Snapshot 2.6.11
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "Mod 管理页面将显示 Mod 的中文名、图标、标签等信息"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "从 Mod 管理页面查看 Mod 信息时会跳转到其下载详情页面"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "重新设计 Mod 管理页面的交互与样式"))
            FeatureCount += 15
            BugCount += 13
        End If
        If LastVersion < 307 Then 'Snapshot 2.6.10
            FeatureCount += 2
            BugCount += 3
        End If
        If LastVersion < 306 Then 'Snapshot 2.6.9
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "为版本独立设置添加忽略 Java 兼容性警告选项"))
            FeatureCount += 9
            BugCount += 11
        End If
        If LastVersion < 305 Then 'Snapshot 2.6.8
            FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复开始或结束游戏时可能报错的 Bug"))
            FeatureCount += 20
            BugCount += 25
        End If
        If LastVersion < 303 Then 'Snapshot 2.6.7
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "再次修复网络条件差时可能无法下载 MC 的 Bug"))
            BugCount += 1
        End If
        If LastVersion < 301 Then 'Snapshot 2.6.6
            FeatureCount += 4
            BugCount += 2
        End If
        If LastVersion < 299 Then 'Snapshot 2.6.5
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持选择多种预设的主页"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化联网获取的主页的加载与缓存"))
            FeatureCount += 18
            BugCount += 13
        End If
        If LastVersion < 298 Then 'Snapshot 2.6.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "Mod 管理中允许多选 Mod 进行批量操作"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化崩溃分析，添加多种崩溃情况的判断"))
            FeatureCount += 18
            BugCount += 19
        End If
        If LastVersion < 297 Then 'Snapshot 2.6.3
            FeatureCount += 12
            BugCount += 14
        End If
        If LastVersion < 294 Then 'Snapshot 2.6.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增内存优化功能，可以将所有程序的物理内存占用降低约 1/3"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "在选择 OptiFine 与 Fabric 后会自动选择 OptiFabric"))
            FeatureCount += 9
            BugCount += 16
        End If
        If LastVersion < 291 Then 'Snapshot 2.6.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "支持在多个正版账号间切换"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "Mod / 整合包下载会单独列出筛选的版本"))
            FeatureCount += 10
            BugCount += 2
        End If
        If LastVersion < 289 Then 'Snapshot 2.5.4
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "重新制作正版登录页面"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "彻底移除 Mojang 登录"))
            FeatureCount += 6
            BugCount += 14
        End If
        If LastVersion < 288 Then 'Snapshot 2.5.3
            FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "在缺少 Java 时会自动下载所需的 Java"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "添加 CurseForge / Modrinth 来源筛选"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "为下载管理和音乐播放按钮添加进度条"))
            FeatureCount += 8
            BugCount += 8
        End If
        If LastVersion < 285 Then 'Snapshot 2.5.2
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "重做资源下载的资源项界面"))
            If LastVersion = 284 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复打开部分帮助报错的 Bug"))
            FeatureCount += 8
            BugCount += 8
        End If
        If LastVersion < 284 Then 'Snapshot 2.5.1
            If LastVersion >= 275 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "在全局启动设置中添加了 启动前执行命令 选项"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "在选择下载 Fabric 时会自动选择 Fabric API"))
            FeatureCount += 22
            BugCount += 21
        End If
        If LastVersion < 283 Then 'Snapshot 2.5.0
            FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "支持搜索、下载 Modrinth 中的 Mod 与整合包"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "资源搜索页面支持翻页"))
            FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "支持安装 Modrinth 整合包"))
            FeatureCount += 5
            BugCount += 5
        End If
#End If
        '整理更新日志文本
        Dim ContentList As New List(Of String)
        Dim SortedFeatures = Sort(FeatureList, Function(Left, Right) Left.Key > Right.Key)
        If Not SortedFeatures.Any() AndAlso FeatureCount = 0 AndAlso BugCount = 0 Then ContentList.Add("龙猫忘记写更新日志啦！可以去提醒他一下……")
        For i = 0 To Math.Min(9, SortedFeatures.Count - 1) '最多取 10 项
            ContentList.Add(SortedFeatures(i).Value)
        Next
        If SortedFeatures.Count > 10 Then FeatureCount += SortedFeatures.Count - 10
        If FeatureCount > 0 OrElse BugCount > 0 Then
            ContentList.Add(If(FeatureCount > 0, FeatureCount & " 项小调整与修改", "") &
                If(FeatureCount > 0 AndAlso BugCount > 0, "，", "") &
                If(BugCount > 0, "修复了 " & BugCount & " 个 Bug", "") &
                "，详见完整更新日志")
        End If
        Dim Content As String = "· " & Join(ContentList, vbCrLf & "· ")
        '输出更新日志
        RunInNewThread(
        Sub()
            If MyMsgBox(Content, "PCL 已更新至 " & VersionDisplayName, "确定", "完整更新日志") = 2 Then
                OpenWebsite("https://afdian.com/a/LTCat?tab=feed")
            End If
        End Sub, "UpdateLog Output")
    End Sub

    '窗口加载
    Private IsWindowLoadFinished As Boolean = False
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
        ''开启管理员权限下的文件拖拽，但下列代码也没用（#2531）
        'If IsAdmin() Then
        '    Log("[Start] PCL 正以管理员权限运行")
        '    ChangeWindowMessageFilter(&H233, 1)
        '    ChangeWindowMessageFilter(&H4A, 1)
        '    ChangeWindowMessageFilter(&H49, 1)
        'End If
        '切换到首页
        If Not IsNothing(FrmLaunchLeft.Parent) Then FrmLaunchLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If Not IsNothing(FrmLaunchRight.Parent) Then FrmLaunchRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PanMainLeft.Child = FrmLaunchLeft
        PanMainRight.Child = FrmLaunchRight
        FrmLaunchRight.PageState = MyPageRight.PageStates.ContentStay
        '模式提醒
#If DEBUG Then
        Hint("[开发者模式] PCL 正以开发者模式运行，这可能会造成严重的性能下降，请务必立即向开发者反馈此问题！", HintType.Critical)
#End If
        If ModeDebug Then Hint("[调试模式] PCL 正以调试模式运行，这可能会导致性能下降，若无必要请不要开启！")
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
        PageSetupUI.BackgroundRefresh(False, True)
        MusicRefreshPlay(False, True)
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
        'PLC 彩蛋
        If RandomInteger(1, 1000) = 233 Then
            ShapeTitleLogo.Data = New GeometryConverter().ConvertFromString("M26,29 v-25 h5 a7,7 180 0 1 0,14 h-5 M80,6.5 a10,11.5 180 1 0 0,18   M47,2.5 v24.5 h12   M98,2 v27   M107,2 v27")
        End If
        '加载窗口
        ThemeRefreshMain()
        Try
            Height = ReadReg("WindowHeight", MinHeight + 50)
            Width = ReadReg("WindowWidth", MinWidth + 50)
        Catch ex As Exception '修复 #2019
            Log(ex, "读取窗口默认大小失败", LogLevel.Hint)
            Height = MinHeight + 50
            Width = MinWidth + 50
        End Try
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
            AaOpacity(Me, Setup.Get("UiLauncherTransparent") / 1000 + 0.4, 250, 100),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 600, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(
            Sub()
                PanBack.RenderTransform = Nothing
                IsWindowLoadFinished = True
                Log($"[System] DPI：{DPI}，系统版本：{Environment.OSVersion.VersionString}，PCL 位置：{PathWithName}")
            End Sub, , True)
        }, "Form Show")
        'Timer 启动
        AniStart()
        TimerMainStart()
        '加载池
        RunInNewThread(
        Sub()
            'EULA 提示
            If Not Setup.Get("SystemEula") Then
                Select Case MyMsgBox("在使用 PCL 前，请同意 PCL 的用户协议与免责声明。", "协议授权", "同意", "拒绝", "查看用户协议与免责声明",
                                   Button3Action:=Sub() OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW"))
                    Case 1
                        Setup.Set("SystemEula", True)
                    Case 2
                        EndProgram(False)
                End Select
            End If
            '启动加载器池
            Try
                JavaListInit() '延后到同意协议后再执行，避免在初次启动时进行进程操作
                Thread.Sleep(200)
                DlClientListMojangLoader.Start(1)
                RunCountSub()
                ServerLoader.Start(1)
            Catch ex As Exception
                Log(ex, "初始化加载池运行失败", LogLevel.Feedback)
            End Try
            '清理自动更新文件
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
                MyMsgBox("欢迎使用 PCL 快照版！" & vbCrLf &
                         "快照版包含尚未在正式版发布的测试性功能，仅用于赞助者本人尝鲜。所以请不要发给其他人或者用于制作整合包哦！" & vbCrLf &
                         "如果你并非通过赞助或赞助者本人邀请进群获得的本程序，那么可能是有人在违规传播，记得提醒他一下啦。", "快照版使用说明")
        End Select
        If Setup.Get("SystemCount") >= 99 Then
            If ThemeUnlock(6, False) Then
                MyMsgBox("你已经使用了 99 次 PCL 啦，感谢你长期以来的支持！" & vbCrLf &
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
        '被移除的窗口设置选项
        If Setup.Get("LaunchArgumentWindowType") = 5 Then Setup.Set("LaunchArgumentWindowType", 1)
        '修改主题设置项名称
        If LowerVersionCode <= 207 Then
            Dim UnlockedTheme As New List(Of String) From {"2"}
            UnlockedTheme.AddRange(New List(Of String)(Setup.Get("UiLauncherThemeHide").ToString.Split("|")))
            UnlockedTheme.AddRange(New List(Of String)(Setup.Get("UiLauncherThemeHide2").ToString.Split("|")))
            Setup.Set("UiLauncherThemeHide2", Join(UnlockedTheme.Distinct.ToList, "|"))
        End If
        '重置欧皇彩
        If LastVersionCode <= 115 AndAlso Setup.Get("UiLauncherThemeHide2").ToString.Split("|").Contains("13") Then
            Dim UnlockedTheme As New List(Of String)(Setup.Get("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("13")
            Setup.Set("UiLauncherThemeHide2", Join(UnlockedTheme, "|"))
            MyMsgBox("由于新版 PCL 修改了欧皇彩的解锁方式，你需要重新解锁欧皇彩。" & vbCrLf &
                     "多谢各位的理解啦！", "重新解锁提醒")
        End If
        '重置滑稽彩
        If LastVersionCode <= 152 AndAlso Setup.Get("UiLauncherThemeHide2").ToString.Split("|").Contains("12") Then
            Dim UnlockedTheme As New List(Of String)(Setup.Get("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("12")
            Setup.Set("UiLauncherThemeHide2", Join(UnlockedTheme, "|"))
            MyMsgBox("由于新版 PCL 修改了滑稽彩的解锁方式，你需要重新解锁滑稽彩。" & vbCrLf &
                     "多谢各位的理解啦！", "重新解锁提醒")
        End If
        '移动自定义皮肤
        If LastVersionCode <= 161 AndAlso File.Exists(Path & "PCL\CustomSkin.png") AndAlso Not File.Exists(PathAppdata & "CustomSkin.png") Then
            CopyFile(Path & "PCL\CustomSkin.png", PathAppdata & "CustomSkin.png")
            Log("[Start] 已移动离线自定义皮肤 (162)")
        End If
        If LastVersionCode <= 263 AndAlso File.Exists(PathTemp & "CustomSkin.png") AndAlso Not File.Exists(PathAppdata & "CustomSkin.png") Then
            CopyFile(PathTemp & "CustomSkin.png", PathAppdata & "CustomSkin.png")
            Log("[Start] 已移动离线自定义皮肤 (264)")
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
                RunInNewThread(
                Sub()
                    Log("[System] 正在强行停止任务")
                    For Each Task As LoaderBase In LoaderTaskbar.ToList()
                        Task.Abort()
                    Next
                End Sub, "强行停止下载任务")
            Else
                Exit Sub
            End If
        End If
        '关闭
        RunInUiWait(
            Sub()
                IsHitTestVisible = False
                If PanBack.RenderTransform Is Nothing Then
                    Dim TransformPos As New TranslateTransform(0, 0)
                    Dim TransformRotate As New RotateTransform(0)
                    Dim TransformScale As New ScaleTransform(1, 1)
                    PanBack.RenderTransform = New TransformGroup() With {.Children = New TransformCollection({TransformRotate, TransformPos, TransformScale})}
                    AniStart({
                        AaOpacity(Me, -Opacity, 140, 40, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaDouble(
                        Sub(i)
                            TransformScale.ScaleX += i
                            TransformScale.ScaleY += i
                        End Sub, 0.88 - TransformScale.ScaleX, 180),
                        AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 180, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaDouble(Sub(i) TransformRotate.Angle += i, 0.6 - TransformRotate.Angle, 180, 0, New AniEaseInoutFluent(AniEasePower.Weak)),
                        AaCode(
                        Sub()
                            IsHitTestVisible = False
                            Top = -10000
                            ShowInTaskbar = False
                        End Sub, 210),
                        AaCode(AddressOf EndProgramForce, 230)
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
        PageLinkHiper.HiperStop(False)
        If IsUpdateWaitingRestart Then UpdateRestart(False)
        If ReturnCode = Result.Exception Then
            If Not IsLogShown Then
                FeedbackInfo()
                Log("请在 https://github.com/Hex-Dragon/PCL2/issues 提交错误报告，以便于作者解决此问题！")
                IsLogShown = True
                ShellOnly(Path & "PCL\Log1.txt")
            End If
            Thread.Sleep(500) '防止 PCL 在记事本打开前就被掐掉
        End If
        Log("[System] 程序已退出，返回值：" & GetStringFromEnum(CType(ReturnCode, [Enum])))
        LogFlush()
        If ReturnCode = Result.Success Then
            Process.GetCurrentProcess.Kill()
        Else
            Environment.Exit(ReturnCode)
            Process.GetCurrentProcess.Kill()
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
        If WindowState = WindowState.Maximized Then WindowState = WindowState.Normal '修复 #1938
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
        '调用弹窗：回车选择第一个，Esc 选择最后一个
        If PanMsg.Children.Count > 0 Then
            If e.Key = Key.Enter Then
                CType(PanMsg.Children(0), Object).Btn1_Click()
                Exit Sub
            ElseIf e.Key = Key.Escape Then
                Dim Msg As Object = PanMsg.Children(0)
                If TypeOf Msg Is MyMsgText AndAlso Msg.Btn3.Visibility = Visibility.Visible Then
                    Msg.Btn3_Click()
                ElseIf Msg.Btn2.Visibility = Visibility.Visible Then
                    Msg.Btn2_Click()
                Else
                    Msg.Btn1_Click()
                End If
                Exit Sub
            End If
        End If
        '按 ESC 返回上一级
        If e.Key = Key.Escape Then TriggerPageBack()
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
    End Sub
    Private Sub FormMain_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseDown
        '鼠标侧键返回上一级
        If e.ChangedButton = MouseButton.XButton1 OrElse e.ChangedButton = MouseButton.XButton2 Then TriggerPageBack()
    End Sub
    Private Sub TriggerPageBack()
        If PageCurrent = PageType.Download AndAlso PageCurrentSub = PageSubType.DownloadInstall AndAlso FrmDownloadInstall.IsInSelectPage Then
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
                FrmVersionMod.ReloadModList()
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
                    If Str.StartsWithF("authlib-injector:yggdrasil-server:") Then
                        'Authlib 拖拽
                        e.Handled = True
                        Dim AuthlibServer As String = Net.WebUtility.UrlDecode(Str.Substring("authlib-injector:yggdrasil-server:".Length))
                        Log("[System] Authlib 拖拽：" & AuthlibServer)
                        If Not String.IsNullOrEmpty(New ValidateHttp().Validate(AuthlibServer)) Then
                            Hint($"输入的 Authlib 验证服务器不符合网址格式（{AuthlibServer}）！", HintType.Critical)
                            Exit Sub
                        End If
                        Dim TargetVersion = If(PageCurrent = PageType.VersionSetup, PageVersionLeft.Version, McVersionCurrent)
                        If TargetVersion Is Nothing Then
                            Hint("请先下载游戏，再设置第三方登录！", HintType.Critical)
                            Exit Sub
                        End If
                        If AuthlibServer = "https://littleskin.cn/api/yggdrasil" Then
                            'LittleSkin
                            If MyMsgBox($"是否要在版本 {TargetVersion.Name} 中开启 LittleSkin 登录？" & vbCrLf &
                                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then
                                Exit Sub
                            End If
                            Setup.Set("VersionServerLogin", 4, Version:=TargetVersion)
                            Setup.Set("VersionServerAuthServer", "https://littleskin.cn/api/yggdrasil", Version:=TargetVersion)
                            Setup.Set("VersionServerAuthRegister", "https://littleskin.cn/auth/register", Version:=TargetVersion)
                            Setup.Set("VersionServerAuthName", "LittleSkin 登录", Version:=TargetVersion)
                        Else
                            '第三方 Authlib 服务器
                            If MyMsgBox($"是否要在版本 {TargetVersion.Name} 中开启第三方登录？" & vbCrLf &
                                        $"登录服务器：{AuthlibServer}" & vbCrLf & vbCrLf &
                                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then
                                Exit Sub
                            End If
                            Setup.Set("VersionServerLogin", 4, Version:=TargetVersion)
                            Setup.Set("VersionServerAuthServer", AuthlibServer, Version:=TargetVersion)
                            Setup.Set("VersionServerAuthRegister", AuthlibServer.Replace("api/yggdrasil", "auth/register"), Version:=TargetVersion)
                            Setup.Set("VersionServerAuthName", "", Version:=TargetVersion)
                        End If
                        If PageCurrent = PageType.VersionSetup AndAlso PageCurrentSub = PageSubType.VersionSetup Then
                            '正在服务器选项页，需要刷新设置项显示
                            FrmVersionSetup.Reload()
                        ElseIf PageCurrent = PageType.Launch Then
                            '正在主页，需要刷新左边栏
                            FrmLaunchLeft.RefreshPage(True, False)
                        End If
                    ElseIf Str.StartsWithF("file:///") Then
                        '文件拖拽（例如从浏览器下载窗口拖入）
                        Dim FilePath = Net.WebUtility.UrlDecode(Str).Substring("file:///".Length).Replace("/", "\")
                        e.Handled = True
                        FileDrag(New List(Of String) From {FilePath})
                    End If
                Catch ex As Exception
                    Log(ex, "无法接取文本拖拽事件", LogLevel.Developer)
                    Exit Sub
                End Try
            ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
                '获取文件并检查
                Dim FilePathRaw = e.Data.GetData(DataFormats.FileDrop)
                If FilePathRaw Is Nothing Then '#2690
                    Hint("请将文件解压后再拖入！", HintType.Critical)
                    Exit Sub
                End If
                e.Handled = True
                FileDrag(CType(FilePathRaw, IEnumerable(Of String)))
            End If
        Catch ex As Exception
            Log(ex, "接取拖拽事件失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub FileDrag(FilePathList As IEnumerable(Of String))
        RunInNewThread(
        Sub()
            Dim FilePath As String = FilePathList.First
            Log("[System] 接受文件拖拽：" & FilePath & If(FilePathList.Any, $" 等 {FilePathList.Count} 个文件", ""), LogLevel.Developer)
            '基础检查
            If Directory.Exists(FilePathList.First) AndAlso Not File.Exists(FilePathList.First) Then
                Hint("请拖入一个文件，而非文件夹！", HintType.Critical)
                Exit Sub
            ElseIf Not File.Exists(FilePathList.First) Then
                Hint("拖入的文件不存在：" & FilePathList.First, HintType.Critical)
                Exit Sub
            End If
            '多文件拖拽
            If FilePathList.Count > 1 Then
                '必须要求全部为 Jar 文件
                For Each File In FilePathList
                    If Not {"jar", "litemod", "disabled", "old"}.Contains(File.After(".").ToLower) Then
                        Hint("一次请只拖入一个文件！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End If
            '自定义主页
            Dim Extension As String = FilePath.After(".").ToLower
            If Extension = "xaml" Then
                Log("[System] 文件后缀为 XAML，作为自定义主页加载")
                If File.Exists(Path & "PCL\Custom.xaml") Then
                    If MyMsgBox("已存在一个自定义主页文件，是否要将它覆盖？", "覆盖确认", "覆盖", "取消") = 2 Then
                        Exit Sub
                    End If
                End If
                CopyFile(FilePath, Path & "PCL\Custom.xaml")
                RunInUi(Sub()
                            Setup.Set("UiCustomType", 1)
                            FrmLaunchRight.ForceRefresh()
                            Hint("已加载主页自定义文件！", HintType.Finish)
                        End Sub)
                Exit Sub
            End If
            'Mod 安装
            If {"jar", "litemod", "disabled", "old"}.Any(Function(t) t = Extension) Then
                Log("[System] 文件为 jar / litemod 格式，尝试作为 Mod 安装")
                '获取并检查目标版本
                Dim TargetVersion As McVersion = McVersionCurrent
                If PageCurrent = PageType.VersionSetup Then TargetVersion = PageVersionLeft.Version
                If PageCurrent = PageType.VersionSelect OrElse TargetVersion Is Nothing OrElse Not TargetVersion.Modable Then
                    '正在选择版本，或当前版本不能安装 Mod
                    Hint("若要安装 Mod，请先选择一个可以安装 Mod 的版本！")
                ElseIf Not (PageCurrent = PageType.VersionSetup AndAlso PageCurrentSub = PageSubType.VersionMod) Then
                    '未处于 Mod 管理页面
                    If MyMsgBox($"是否要将这{If(FilePathList.Count = 1, "个", "些")}文件作为 Mod 安装到 {TargetVersion.Name}？", "Mod 安装确认", "确定", "取消") = 1 Then GoTo Install
                Else
                    '处于 Mod 管理页面
Install:
                    Try
                        For Each ModFile In FilePathList
                            Dim NewFileName = GetFileNameFromPath(ModFile).Replace(".disabled", "")
                            If Not NewFileName.Contains(".") Then NewFileName += ".jar" '#4227
                            CopyFile(ModFile, TargetVersion.PathIndie & "mods\" & NewFileName)
                        Next
                        If FilePathList.Count = 1 Then
                            Hint($"已安装 {GetFileNameFromPath(FilePathList.First).Replace(".disabled", "")}！", HintType.Finish)
                        Else
                            Hint($"已安装 {FilePathList.Count} 个 Mod！", HintType.Finish)
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
            If {"zip", "rar", "mrpack"}.Any(Function(t) t = Extension) Then '部分压缩包是 zip 格式但后缀为 rar，总之试一试
                Log("[System] 文件为压缩包，尝试作为整合包安装")
                If ModpackInstall(FilePath, ShowHint:=False) IsNot Nothing Then Exit Sub
            End If
            'RAR 处理
            If Extension = "rar" Then
                Hint("PCL 无法处理 rar 格式的压缩包，请在解压后重新压缩为 zip 格式再试！")
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
            Hint("PCL 无法确定应当执行的文件拖拽操作……")
        End Sub, "文件拖拽")
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
        Set(value As Boolean)
            If _Hidden = value Then Exit Property
            _Hidden = value
            If value Then
                '隐藏
                Left -= 10000
                ShowInTaskbar = False
                Visibility = Visibility.Hidden
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
                    Log("[System] 窗口已置顶，位置：(" & Left & ", " & Top & "), " & Width & " x " & Height)
                End Sub)
    End Sub

#End Region

#Region "切换页面"

    '页面种类与属性
    '注意，这一枚举在 “切换页面” EventType 中调用，应视作公开 API 的一部分
    ''' <summary>
    ''' 页面种类。
    ''' </summary>
    Public Enum PageType
        ''' <summary>
        ''' 启动。
        ''' </summary>
        Launch = 0
        ''' <summary>
        ''' 下载。
        ''' </summary>
        Download = 1
        ''' <summary>
        ''' 联机。
        ''' </summary>
        Link = 2
        ''' <summary>
        ''' 设置。
        ''' </summary>
        Setup = 3
        ''' <summary>
        ''' 更多。
        ''' </summary>
        Other = 4
        ''' <summary>
        ''' 版本选择。这是一个副页面。
        ''' </summary>
        VersionSelect = 5
        ''' <summary>
        ''' 下载管理。这是一个副页面。
        ''' </summary>
        DownloadManager = 6
        ''' <summary>
        ''' 版本设置。这是一个副页面。
        ''' </summary>
        VersionSetup = 7
        ''' <summary>
        ''' 资源工程详情。这是一个副页面。
        ''' </summary>
        CompDetail = 8
        ''' <summary>
        ''' 帮助详情。这是一个副页面。
        ''' </summary>
        HelpDetail = 9
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
        DownloadNeoForge = 7
        DownloadFabric = 8
        DownloadQuilt = 10
        DownloadLiteLoader = 9
        DownloadMod = 11
        DownloadPack = 12
        SetupLaunch = 0
        SetupUI = 1
        SetupSystem = 2
        SetupLink = 3
        LinkHiper = 1
        LinkIoi = 2
        LinkSetup = 4
        LinkHelp = 5
        LinkFeedback = 6
        OtherHelp = 0
        OtherAbout = 1
        OtherTest = 2
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
            Case PageType.CompDetail
                Dim Project As CompProject = Stack.Additional(0)
                Select Case Project.Type
                    Case CompType.Mod
                        Return "Mod 下载 - " & Project.TranslatedName
                    Case CompType.ModPack
                        Return "整合包下载 - " & Project.TranslatedName
                    Case Else 'CompType.ResourcePack
                        Return "资源包下载 - " & Project.TranslatedName
                End Select
            Case PageType.HelpDetail
                Dim Entry As HelpEntry = Stack.Additional(0)
                Return Entry.Title
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
    ''' 上一个主页面。
    ''' </summary>
    Public PageLast As PageStackData = PageType.Launch
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
    Public PageStack As New List(Of PageStackData)
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
    Private Sub CancelLink(sender As Object, e As RouteEventArgs) Handles BtnTitleSelect2.PreviewClick
        If MyMsgBox("由于联机提供商要求新联机强制付费，且高度商业化，PCL 将暂时关闭联机功能，不再使用该联机模块。" & vbCrLf &
                    "PCL、HMCL、BakaXL 将合作开发新的跨启动器联机功能，在开发结束后将同步开放，请各位多多理解。",
                    "联机功能已暂时关闭", "查看详情", "确定") = 1 Then
            OpenWebsite("https://www.bilibili.com/read/cv19845645")
        End If
        e.Handled = True
    End Sub
    ''' <summary>
    ''' 通过点击返回按钮或手动触发返回来改变页面。
    ''' </summary>
    Public Sub PageBack() Handles BtnTitleInner.Click
        If PageStack.Any() Then
            PageChangeActual(PageStack(0))
        Else
            PageChange(PageType.Launch)
        End If
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
                If PageStack.Any Then
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
                Else
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
                End If
            End If
#End Region

#Region "实际更改页面框架 UI"
            PageLast = PageCurrent
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
                Case PageType.CompDetail 'Mod 信息
                    If FrmDownloadCompDetail Is Nothing Then FrmDownloadCompDetail = New PageDownloadCompDetail
                    PageChangeAnim(New MyPageLeft, FrmDownloadCompDetail)
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
        If PageStack.Any Then
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
        Else
            '主页面 → 主页面，无事发生
        End If
    End Sub

    '左边栏改变
    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        If Not e.WidthChanged Then Exit Sub
        PanMainLeft_Resize(e.NewSize.Width)
    End Sub
    Private Sub PanMainLeft_Resize(NewWidth As Double)
        Dim Delta As Double = NewWidth - RectLeftBackground.Width
        If Math.Abs(Delta) > 0.1 AndAlso AniControlEnabled = 0 Then
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

    ''' <summary>
    ''' 返回顶部。
    ''' </summary>
    Public Sub BackToTop() Handles BtnExtraBack.Click
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        If RealScroll IsNot Nothing Then
            RealScroll.PerformVerticalOffsetDelta(-RealScroll.VerticalOffset)
        Else
            Log("[UI] 无法返回顶部，未找到合适的 RealScroll", LogLevel.Hint)
        End If
    End Sub
    Private Function BtnExtraBack_ShowCheck() As Boolean
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        Return RealScroll IsNot Nothing AndAlso RealScroll.Visibility = Visibility.Visible AndAlso RealScroll.VerticalOffset > Height + If(BtnExtraBack.Show, 0, 700)
    End Function
    Private Function BtnExtraBack_GetRealChild() As MyScrollViewer
        If PanMainRight.Child Is Nothing OrElse TypeOf PanMainRight.Child IsNot MyPageRight Then Return Nothing
        Return CType(PanMainRight.Child, MyPageRight).PanScroll
    End Function

#End Region

    '愚人节鼠标位置
    Public lastMouseArg As MouseEventArgs = Nothing
    Private Sub FormMain_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        lastMouseArg = e
    End Sub

End Class
