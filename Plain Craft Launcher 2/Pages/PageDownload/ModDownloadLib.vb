Imports System.IO.Compression

Public Module ModDownloadLib

#Region "Minecraft 下载"

    ''' <summary>
    ''' 下载某个 Minecraft 版本，这会创造一个单独的下载任务，失败会跳过执行并要求反馈。
    ''' 返回正在下载的任务，若跳过或失败，则返回 Nothing。
    ''' </summary>
    ''' <param name="Id">所下载的 Minecraft 的版本名。</param>
    ''' <param name="JsonUrl">Json 文件的 Mojang 官方地址。</param>
    Public Function McDownloadClient(Behaviour As NetPreDownloadBehaviour, Id As String, Optional JsonUrl As String = Nothing) As LoaderCombo(Of String)
        Try
            Dim VersionFolder As String = PathMcFolder & "versions\" & Id & "\"

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "Minecraft " & Id & " 下载" Then
                        If Behaviour = NetPreDownloadBehaviour.ExitWhileExistsOrDownloading Then Return LoaderTaskbar(i)
                        Hint("该版本正在下载中！", HintType.Critical)
                        Return LoaderTaskbar(i)
                    End If
                Next
            End SyncLock

            '已有版本检查
            If Behaviour <> NetPreDownloadBehaviour.IgnoreCheck AndAlso File.Exists(VersionFolder & Id & ".json") AndAlso File.Exists(VersionFolder & Id & ".jar") Then
                If Behaviour = NetPreDownloadBehaviour.ExitWhileExistsOrDownloading Then Return Nothing
                If MyMsgBox("版本 " & Id & " 已存在，是否重新下载？" & vbCrLf & "这会覆盖版本的 Json 与 Jar 文件，但不会影响版本隔离的文件。", "版本已存在", "继续", "取消") = 1 Then
                    File.Delete(VersionFolder & Id & ".jar")
                    File.Delete(VersionFolder & Id & ".json")
                Else
                    Return Nothing
                End If
            End If

            '启动
            Dim Loader As New LoaderCombo(Of String)("Minecraft " & Id & " 下载", McDownloadClientLoader(Id, JsonUrl)) With {.OnStateChanged = AddressOf McInstallState}
            Loader.Start(VersionFolder)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            Return Loader

        Catch ex As Exception
            Log(ex, "开始 Minecraft 下载失败", LogLevel.Feedback)
            Return Nothing
        End Try
    End Function
    ''' <summary>
    ''' 保存某个 Minecraft 版本的核心文件（仅 Json 与核心 Jar）。
    ''' </summary>
    ''' <param name="Id">所下载的 Minecraft 的版本名。</param>
    ''' <param name="JsonUrl">Json 文件的 Mojang 官方地址。</param>
    Public Sub McDownloadClientCore(Id As String, JsonUrl As String, Behaviour As NetPreDownloadBehaviour)
        Try
            Dim VersionFolder As String = SelectFolder()
            If Not VersionFolder.Contains("\") Then Exit Sub
            VersionFolder = VersionFolder & Id & "\"

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "Minecraft " & Id & " 下载" Then
                        If Behaviour = NetPreDownloadBehaviour.ExitWhileExistsOrDownloading Then Exit Sub
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            Dim Loaders As New List(Of LoaderBase)
            '下载版本 Json 文件
            Loaders.Add(New LoaderDownload("下载版本 Json 文件", New List(Of NetFile) From {
                New NetFile(DlSourceLauncherOrMetaGet(JsonUrl), VersionFolder & Id & ".json", New FileChecker(CanUseExistsFile:=False, IsJson:=True))
            }) With {.ProgressWeight = 2})
            '获取支持库文件地址
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析核心 Jar 文件下载地址",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionFolder), True)) With {.ProgressWeight = 0.5, .Show = False})
            '下载支持库文件
            Loaders.Add(New LoaderDownload("下载核心 Jar 文件", New List(Of NetFile)) With {.ProgressWeight = 5})

            '启动
            Dim Loader As New LoaderCombo(Of String)("Minecraft " & Id & " 下载", Loaders) With {.OnStateChanged = AddressOf DownloadStateSave}
            Loader.Start(Id)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 Minecraft 下载失败", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 获取下载某个 Minecraft 版本的加载器列表。
    ''' 它必须安装到 PathMcFolder，但是可以自定义版本名（不过自定义的版本名不会修改 Json 中的 id 项）。
    ''' </summary>
    Private Function McDownloadClientLoader(Id As String, Optional JsonUrl As String = Nothing, Optional VersionName As String = Nothing) As List(Of LoaderBase)
        VersionName = If(VersionName, Id)
        Dim VersionFolder As String = PathMcFolder & "versions\" & VersionName & "\"

        Dim Loaders As New List(Of LoaderBase)

        '下载版本 Json 文件
        If JsonUrl Is Nothing Then
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("获取原版 Json 文件下载地址",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                Dim JsonAddress As String = DlClientListGet(Id)
                                                                Task.Output = New List(Of NetFile) From {New NetFile(DlSourceLauncherOrMetaGet(JsonAddress), VersionFolder & VersionName & ".json")}
                                                            End Sub) With {.ProgressWeight = 2, .Show = False})
        End If
        Loaders.Add(New LoaderDownload("下载原版 Json 文件", New List(Of NetFile) From {
                New NetFile(DlSourceLauncherOrMetaGet(If(JsonUrl, "")), VersionFolder & VersionName & ".json", New FileChecker(CanUseExistsFile:=False, IsJson:=True))
            }) With {.ProgressWeight = 3})

        '下载支持库文件
        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析原版支持库文件（副加载器）",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionFolder))) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载原版支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 13, .Show = False})
        Loaders.Add(New LoaderCombo(Of String)(McDownloadClientLibName, LoadersLib) With {.Block = False, .ProgressWeight = 14})

        '下载资源文件
        Dim LoadersAssets As New List(Of LoaderBase)
        LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析资源文件索引地址（副加载器）",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                Try
                                                                    Dim Version As New McVersion(VersionFolder)
                                                                    Task.Output = New List(Of NetFile) From {DlClientAssetIndexGet(Version)}
                                                                Catch ex As Exception
                                                                    Throw New Exception("分析资源文件索引地址失败", ex)
                                                                End Try
                                                                '顺手添加 Json 项目
                                                                Try
                                                                    Dim VersionJson As JObject = GetJson(ReadFile(VersionFolder & VersionName & ".json"))
                                                                    VersionJson.Add("clientVersion", Id)
                                                                    WriteFile(VersionFolder & VersionName & ".json", VersionJson.ToString)
                                                                Catch ex As Exception
                                                                    Throw New Exception("添加客户端版本失败", ex)
                                                                End Try
                                                            End Sub) With {.ProgressWeight = 1, .Show = False})
        LoadersAssets.Add(New LoaderDownload("下载资源文件索引（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 3, .Show = False})
        LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析所需资源文件（副加载器）",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                Task.Output = McAssetsFixList(McAssetsGetIndexName(New McVersion(VersionFolder)), True, Task)
                                                            End Sub) With {.ProgressWeight = 3, .Show = False})
        LoadersAssets.Add(New LoaderDownload("下载资源文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 14, .Show = False})
        Loaders.Add(New LoaderCombo(Of String)("下载原版资源文件", LoadersAssets) With {.Block = False, .ProgressWeight = 21})

        Return Loaders

    End Function
    Private Const McDownloadClientLibName As String = "下载原版支持库文件"

#End Region

#Region "Minecraft 下载菜单"

    Public Function McDownloadListItem(Entry As JObject, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyListItem
        '确定图标
        Dim Logo As String
        Select Case Entry("type")
            Case "release"
                Logo = "pack://application:,,,/images/Blocks/Grass.png"
            Case "snapshot"
                Logo = "pack://application:,,,/images/Blocks/CommandBlock.png"
            Case "special"
                Logo = "pack://application:,,,/images/Blocks/GoldBlock.png"
            Case Else
                Logo = "pack://application:,,,/images/Blocks/CobbleStone.png"
        End Select
        '建立控件
        Dim NewItem As New MyListItem With {.Logo = Logo, .SnapsToDevicePixels = True, .Title = Entry("id").ToString, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 60}
        If Entry("lore") Is Nothing Then
            NewItem.Info = Entry("releaseTime").Value(Of Date).ToString("yyyy/MM/dd")
        Else
            NewItem.Info = Entry("lore").ToString
        End If
        If Entry("url").ToString.Contains("pcl") Then NewItem.Info = "[PCL2 特别提供] " & NewItem.Info
        AddHandler NewItem.Click, OnClick
        '建立菜单
        If IsSaveOnly Then
            NewItem.ContentHandler = AddressOf McDownloadSaveMenuBuild
        Else
            NewItem.ContentHandler = AddressOf McDownloadMenuBuild
        End If
        '结束
        Return NewItem
    End Function
    Private Sub McDownloadSaveMenuBuild(sender As Object, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        ToolTipService.SetPlacement(BtnInfo, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnInfo, 30)
        ToolTipService.SetHorizontalOffset(BtnInfo, 2)
        AddHandler BtnInfo.Click, AddressOf McDownloadMenuLog
        sender.Buttons = {BtnInfo}
    End Sub
    Private Sub McDownloadMenuBuild(sender As Object, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        ToolTipService.SetPlacement(BtnSave, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnSave, 30)
        ToolTipService.SetHorizontalOffset(BtnSave, 2)
        AddHandler BtnSave.Click, AddressOf McDownloadMenuSave
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        ToolTipService.SetPlacement(BtnInfo, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnInfo, 30)
        ToolTipService.SetHorizontalOffset(BtnInfo, 2)
        AddHandler BtnInfo.Click, AddressOf McDownloadMenuLog
        sender.Buttons = {BtnSave, BtnInfo}
    End Sub
    Private Sub McDownloadMenuLog(sender As Object, e As RoutedEventArgs)
        Dim Version As JToken
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        McUpdateLogShow(Version)
    End Sub
    Public Sub McDownloadMenuSave(sender As Object, e As RoutedEventArgs)
        Dim Version As MyListItem
        If TypeOf sender Is MyListItem Then
            Version = sender
        ElseIf TypeOf sender.Parent Is MyListItem Then
            Version = sender.Parent
        Else
            Version = sender.Parent.Parent
        End If
        McDownloadClientCore(Version.Title, Version.Tag("url").ToString, NetPreDownloadBehaviour.HintWhileExists)
    End Sub
    ''' <summary>
    ''' 显示某 Minecraft 版本的更新日志。
    ''' </summary>
    ''' <param name="VersionJson">在 version_manifest.json 中的对应项。</param>
    Public Sub McUpdateLogShow(VersionJson As JToken)
        Dim WikiName As String
        Dim Id As String = VersionJson("id").ToString.ToLower
        If Id = "3d shareware v1.34" Then
            WikiName = "3D_Shareware_v1.34"
        ElseIf Id = "2.0" Then
            WikiName = "Java版2.0"
        ElseIf Id = "1.rv-pre1" Then
            WikiName = "Java版1.RV-Pre1"
        ElseIf Id = "combat test 1" OrElse Id.Contains("combat-1") OrElse Id.Contains("combat-212796") Then
            WikiName = "Java版1.14.3_-_Combat_Test"
        ElseIf Id = "combat test 2" OrElse Id.Contains("combat-2") OrElse Id.Contains("combat-0") Then
            WikiName = "Java版Combat_Test_2"
        ElseIf Id = "combat test 3" OrElse Id = "1.14_combat-3" Then
            WikiName = "Java版Combat_Test_3"
        ElseIf Id = "combat test 4" OrElse Id = "1.15_combat-1" Then
            WikiName = "Java版Combat_Test_4"
        ElseIf Id = "combat test 5" OrElse Id = "1.15_combat-6" Then
            WikiName = "Java版Combat_Test_5"
        ElseIf Id = "combat test 6" OrElse Id = "1.16_combat-0" Then
            WikiName = "Java版Combat_Test_6"
        ElseIf Id = "combat test 7c" OrElse Id = "1.16_combat-3" Then
            WikiName = "Java版Combat_Test_7c"
        ElseIf Id = "combat test 8b" OrElse Id = "1.16_combat-5" Then
            WikiName = "Java版Combat_Test_8b"
        ElseIf Id = "combat test 8c" OrElse Id = "1.16_combat-6" Then
            WikiName = "Java版Combat_Test_8c"
        ElseIf Id = "1.0.0-rc2-2" Then
            WikiName = "Java版RC2"
        ElseIf Id.StartsWith("1.19_deep_dark_experimental_snapshot-") OrElse Id.StartsWith("1_19_deep_dark_experimental_snapshot-") Then
            WikiName = Id.Replace("1_19", "1.19").Replace("1.19_deep_dark_experimental_snapshot-", "Java版Deep_Dark_Experimental_Snapshot_")
        ElseIf Id = "b1.9-pre6" Then
            WikiName = "Java版Beta_1.9_Prerelease_6"
        ElseIf Id.Contains("b1.9") Then
            WikiName = "Java版Beta_1.9_Prerelease"
        ElseIf VersionJson("type") = "release" OrElse VersionJson("type") = "snapshot" OrElse VersionJson("type") = "special" Then
            WikiName = If(Id.Contains("w"), "", "Java版") & Id.Replace(" Pre-Release ", "-pre")
        ElseIf Id.StartsWith("b") Then
            WikiName = "Java版" & Id.TrimEnd("a", "b", "c", "d", "e").Replace("b", "Beta_")
        ElseIf Id.StartsWith("a") Then
            WikiName = "Java版" & Id.TrimEnd("a", "b", "c", "d", "e").Replace("a", "Alpha_v")
        ElseIf Id = "inf-20100618" Then
            WikiName = "Java版Infdev_20100618"
        ElseIf Id = "c0.30_01c" OrElse Id = "c0.30_survival" OrElse Id.Contains("生存测试") Then
            WikiName = "Java版Classic_0.30（生存模式）"
        ElseIf Id = "c0.31" Then
            WikiName = "Java版Indev_0.31（2010年1月30日）"
        ElseIf Id.StartsWith("c") Then
            WikiName = "Java版" & Id.Replace("c", "Classic_")
        ElseIf Id.StartsWith("rd-") Then
            WikiName = "Java版Pre-classic_" & Id
        Else
            Log("[Error] 未知的版本格式：" & Id & "。", LogLevel.Feedback)
            Exit Sub
        End If
        OpenWebsite("https://minecraft.fandom.com/zh/wiki/" & WikiName.Replace("_experimental-snapshot-", "-exp"))
    End Sub

#End Region

#Region "OptiFine 下载"

    Public Sub McDownloadOptiFine(DownloadInfo As DlOptiFineListEntry)
        Try
            Dim Id As String = DownloadInfo.NameVersion
            Dim VersionFolder As String = PathMcFolder & "versions\" & Id & "\"
            Dim IsNewVersion As Boolean = Val(DownloadInfo.Inherit.Split(".")(1)) >= 14
            Dim Target As String = If(IsNewVersion,
                PathTemp & "Cache\Code\" & DownloadInfo.NameVersion & "_" & GetUuid(),
                PathMcFolder & "libraries\optifine\OptiFine\" & DownloadInfo.NameFile.Replace("OptiFine_", "").Replace(".jar", "").Replace("preview_", "") & "\" & DownloadInfo.NameFile.Replace("OptiFine_", "OptiFine-").Replace("preview_", ""))

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "OptiFine " & DownloadInfo.NameDisplay & " 下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            '已有版本检查
            If File.Exists(VersionFolder & Id & ".json") Then
                If MyMsgBox("版本 " & Id & " 已存在，是否重新下载？" & vbCrLf & "这会覆盖版本的 Json 文件，但不会影响版本隔离的文件。", "版本已存在", "继续", "取消") = 1 Then
                    File.Delete(VersionFolder & Id & ".jar")
                    File.Delete(VersionFolder & Id & ".json")
                Else
                    Exit Sub
                End If
            End If

            '启动
            Dim Loader As New LoaderCombo(Of String)("OptiFine " & DownloadInfo.NameDisplay & " 下载", McDownloadOptiFineLoader(DownloadInfo)) With {.OnStateChanged = AddressOf McInstallState}
            Loader.Start(VersionFolder)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 OptiFine 下载失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub McDownloadOptiFineSave(DownloadInfo As DlOptiFineListEntry)
        Try
            Dim Id As String = DownloadInfo.NameVersion
            Dim Target As String = SelectAs("选择保存位置", DownloadInfo.NameFile, "OptiFine Jar (*.jar)|*.jar")
            If Not Target.Contains("\") Then Exit Sub

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "OptiFine " & DownloadInfo.NameDisplay & " 下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            Dim Loader As New LoaderCombo(Of DlOptiFineListEntry)("OptiFine " & DownloadInfo.NameDisplay & " 下载", McDownloadOptiFineSaveLoader(DownloadInfo, Target)) With {.OnStateChanged = AddressOf DownloadStateSave}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 OptiFine 下载失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub McDownloadOptiFineInstall(BaseMcFolderHome As String, Target As String, Task As LoaderTask(Of List(Of NetFile), Boolean))
        '选择 Java
        Dim Java = JavaSelect(New Version(1, 8, 0, 0))
        If Java Is Nothing Then
            JavaMissing(8)
            Throw New Exception("未找到用于安装 OptiFine 的 Java")
        End If
        '开始启动
        Dim Info = New ProcessStartInfo With {
            .FileName = Java.PathJavaw,
            .Arguments = "-Duser.home=""" & BaseMcFolderHome & """ -cp """ & Target & """ optifine.Installer",
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardError = True,
            .RedirectStandardOutput = True,
            .WorkingDirectory = BaseMcFolderHome
        }
        If Info.EnvironmentVariables.ContainsKey("appdata") Then
            Info.EnvironmentVariables("appdata") = BaseMcFolderHome
        Else
            Info.EnvironmentVariables.Add("appdata", BaseMcFolderHome)
        End If
        Log("[Download] 开始安装 OptiFine：" & Target)
        Dim TotalLength As Integer = 0
        Dim process As New Process With {.StartInfo = Info}
        Dim LastResult As String = ""
        Using outputWaitHandle As New AutoResetEvent(False)
            Using errorWaitHandle As New AutoResetEvent(False)
                AddHandler process.OutputDataReceived, Function(sender, e)
                                                           Try
                                                               If e.Data Is Nothing Then
                                                                   outputWaitHandle.[Set]()
                                                               Else
                                                                   LastResult = e.Data
                                                                   If ModeDebug Then Log("[Installer] " & LastResult)
                                                                   TotalLength += 1
                                                                   Task.Progress += 0.9 / 7000
                                                               End If
                                                           Catch ex As ObjectDisposedException
                                                           Catch ex As Exception
                                                               Log(ex, "读取 OptiFine 安装器信息失败")
                                                           End Try
                                                           Try
                                                               If Task.State = LoadState.Aborted AndAlso Not process.HasExited Then
                                                                   Log("[Installer] 由于任务取消，已中止 OptiFine 安装")
                                                                   process.Kill()
                                                               End If
                                                           Catch
                                                           End Try
                                                           Return Nothing
                                                       End Function
                AddHandler process.ErrorDataReceived, Function(sender, e)
                                                          Try
                                                              If e.Data Is Nothing Then
                                                                  errorWaitHandle.[Set]()
                                                              Else
                                                                  LastResult = e.Data
                                                                  If ModeDebug Then Log("[Installer] " & LastResult)
                                                                  TotalLength += 1
                                                                  Task.Progress += 0.9 / 7000
                                                              End If
                                                          Catch ex As ObjectDisposedException
                                                          Catch ex As Exception
                                                              Log(ex, "读取 OptiFine 安装器错误信息失败")
                                                          End Try
                                                          Try
                                                              If Task.State = LoadState.Aborted AndAlso Not process.HasExited Then
                                                                  Log("[Installer] 由于任务取消，已中止 OptiFine 安装")
                                                                  process.Kill()
                                                              End If
                                                          Catch
                                                          End Try
                                                          Return Nothing
                                                      End Function
                process.Start()
                process.BeginOutputReadLine()
                process.BeginErrorReadLine()
                '等待
                Do Until process.HasExited
                    Thread.Sleep(10)
                Loop
                '输出
                outputWaitHandle.WaitOne(10000)
                errorWaitHandle.WaitOne(10000)
                process.Dispose()
                If TotalLength < 1000 Then Throw New Exception("安装器运行出错，末行为 " & LastResult)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' 获取下载某个 OptiFine 版本的加载器列表。
    ''' </summary>
    Private Function McDownloadOptiFineLoader(DownloadInfo As DlOptiFineListEntry, Optional McFolder As String = Nothing, Optional ClientDownloadLoader As LoaderCombo(Of String) = Nothing, Optional FixLibrary As Boolean = True) As List(Of LoaderBase)

        '参数初始化
        McFolder = If(McFolder, PathMcFolder)
        Dim IsCustomFolder As Boolean = McFolder <> PathMcFolder
        Dim Id As String = DownloadInfo.NameVersion
        Dim VersionFolder As String = McFolder & "versions\" & Id & "\"
        Dim IsNewVersion As Boolean = DownloadInfo.Inherit.Contains("w") OrElse Val(DownloadInfo.Inherit.Split(".")(1)) >= 14
        Dim Target As String = If(IsNewVersion,
            PathTemp & "Cache\Code\" & DownloadInfo.NameVersion & "_" & GetUuid(),
            McFolder & "libraries\optifine\OptiFine\" & DownloadInfo.NameFile.Replace("OptiFine_", "").Replace(".jar", "").Replace("preview_", "") & "\" & DownloadInfo.NameFile.Replace("OptiFine_", "OptiFine-").Replace("preview_", ""))
        Dim Loaders As New List(Of LoaderBase)

        '获取下载地址
        Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("获取 OptiFine 主文件下载地址",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                '启动依赖版本的下载
                                                                If ClientDownloadLoader Is Nothing Then
                                                                    If IsCustomFolder Then Throw New Exception("如果没有指定原版下载器，则不能指定 MC 安装文件夹")
                                                                    ClientDownloadLoader = McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, DownloadInfo.Inherit)
                                                                End If
                                                                Task.Progress = 0.1
                                                                Dim Sources As New List(Of String)
                                                                'BMCLAPI 源
                                                                If DownloadInfo.IsPreview Then
                                                                    Sources.Add("https://download.mcbbs.net/optifine/" & DownloadInfo.Inherit & "/HD_U_" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", "").Replace(" ", "/"))
                                                                    Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & DownloadInfo.Inherit & "/HD_U_" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", "").Replace(" ", "/"))
                                                                Else
                                                                    Sources.Add("https://download.mcbbs.net/optifine/" & DownloadInfo.Inherit & "/HD_U/" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", ""))
                                                                    Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & DownloadInfo.Inherit & "/HD_U/" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", ""))
                                                                End If
                                                                '官方源
                                                                Dim PageData As String
                                                                Try
                                                                    PageData = NetGetCodeByClient("https://optifine.net/adloadx?f=" & DownloadInfo.NameFile, New UTF8Encoding(False), 15000, "text/html")
                                                                    Task.Progress = 0.8
                                                                    Sources.Add("https://optifine.net/" & RegexSearch(PageData, "downloadx\?f=[^""']+")(0))
                                                                    Log("[Download] OptiFine " & DownloadInfo.NameDisplay & " 官方下载地址：" & Sources(0))
                                                                Catch ex As Exception
                                                                    Log(ex, "获取 OptiFine " & DownloadInfo.NameDisplay & " 官方下载地址失败")
                                                                End Try
                                                                Task.Progress = 0.9
                                                                'OptiFine 中文镜像源
                                                                Sources.Add("https://optifine.cn/download/" & DownloadInfo.NameFile)
                                                                '构造文件请求
                                                                Task.Output = New List(Of NetFile) From {New NetFile(Sources.ToArray, Target, New FileChecker(MinSize:=64 * 1024))}
                                                            End Sub) With {.ProgressWeight = 8})
        Loaders.Add(New LoaderDownload("下载 OptiFine 主文件", New List(Of NetFile)) With {.ProgressWeight = 8})
        Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("等待原版下载",
                                                                         Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                                                                             '是否已经存在原版文件
                                                                             If ClientDownloadLoader Is Nothing Then Exit Sub
                                                                             '等待原版文件下载完成
                                                                             For Each Loader In ClientDownloadLoader.GetLoaderList
                                                                                 If Loader.Name <> McDownloadClientLibName Then Continue For
                                                                                 Loader.WaitForExit()
                                                                                 Exit For
                                                                             Next
                                                                             '拷贝原版文件
                                                                             If IsCustomFolder Then
                                                                                 Try
                                                                                     Dim ClientName As String = New DirectoryInfo(ClientDownloadLoader.Input).Name
                                                                                     Directory.CreateDirectory(McFolder & "versions\" & DownloadInfo.Inherit)
                                                                                     If Not File.Exists(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json") Then
                                                                                         File.Copy(ClientDownloadLoader.Input & ClientName & ".json", McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json")
                                                                                     End If
                                                                                     If Not File.Exists(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar") Then
                                                                                         File.Copy(ClientDownloadLoader.Input & ClientName & ".jar", McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar")
                                                                                     End If
                                                                                 Catch ex As Exception
                                                                                     'OptiFine 与 Forge 同时开始复制偶尔会导致冲突出错
                                                                                     Log(ex, "安装 OptiFine 拷贝原版文件时出错")
                                                                                 End Try
                                                                             End If
                                                                         End Sub) With {.ProgressWeight = 0.1, .Show = False})

        '安装（新旧方式均需要原版 Jar 和 Json）
        If IsNewVersion Then
            Log("[Download] 检测为新版 OptiFine：" & DownloadInfo.Inherit)
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("安装 OptiFine（方式 A）",
                                                            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                                                                Dim BaseMcFolderHome As String = PathTemp & "InstallOptiFine" & RandomInteger(0, 100000)
                                                                Dim BaseMcFolder As String = BaseMcFolderHome & "\.minecraft\"
                                                                Try
                                                                    '准备安装环境
                                                                    If Directory.Exists(BaseMcFolder & "versions\" & DownloadInfo.Inherit) Then
                                                                        DeleteDirectory(BaseMcFolder & "versions\" & DownloadInfo.Inherit)
                                                                    End If
                                                                    My.Computer.FileSystem.CreateDirectory(BaseMcFolder & "versions\" & DownloadInfo.Inherit & "\")
                                                                    McFolderLauncherProfilesJsonCreate(BaseMcFolder)
                                                                    File.Copy(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json",
                                                                              BaseMcFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json")
                                                                    File.Copy(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar",
                                                                              BaseMcFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar")
                                                                    Task.Progress = 0.06
                                                                    '进行安装
                                                                    McDownloadOptiFineInstall(BaseMcFolderHome, Target, Task)
                                                                    Task.Progress = 0.96
                                                                    '复制文件
                                                                    File.Delete(BaseMcFolder & "launcher_profiles.json")
                                                                    My.Computer.FileSystem.CopyDirectory(BaseMcFolder, McFolder, True)
                                                                    Task.Progress = 0.98
                                                                    '清理文件
                                                                    My.Computer.FileSystem.DeleteFile(Target)
                                                                    DeleteDirectory(BaseMcFolderHome)
                                                                Catch ex As Exception
                                                                    Throw New Exception("安装新版 OptiFine 版本失败", ex)
                                                                End Try
                                                            End Sub) With {.ProgressWeight = 8})
        Else
            Log("[Download] 检测为旧版 OptiFine：" & DownloadInfo.Inherit)
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("安装 OptiFine（方式 B）",
                                                            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                                                                Try
                                                                    '新建版本文件夹
                                                                    Directory.CreateDirectory(VersionFolder)
                                                                    Task.Progress = 0.1
                                                                    '复制 Jar 文件
                                                                    If File.Exists(VersionFolder & Id & ".jar") Then File.Delete(VersionFolder & Id & ".jar")
                                                                    File.Copy(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar", VersionFolder & Id & ".jar")
                                                                    Task.Progress = 0.7
                                                                    '建立 Json 文件
                                                                    Dim InheritVersion As New McVersion(McFolder & "versions\" & DownloadInfo.Inherit)
                                                                    Dim Json As String = "{
    ""id"": """ & Id & """,
    ""inheritsFrom"": """ & DownloadInfo.Inherit & """,
    ""time"": """ & If(DownloadInfo.ReleaseTime = "", InheritVersion.ReleaseTime.ToString("yyyy-MM-dd"), DownloadInfo.ReleaseTime.Replace("/", "-")) & "T23:33:33+08:00"",
    ""releaseTime"": """ & If(DownloadInfo.ReleaseTime = "", InheritVersion.ReleaseTime.ToString("yyyy-MM-dd"), DownloadInfo.ReleaseTime.Replace("/", "-")) & "T23:33:33+08:00"",
    ""type"": ""release"",
    ""libraries"": [
        {""name"": ""optifine:OptiFine:" & DownloadInfo.NameFile.Replace("OptiFine_", "").Replace(".jar", "").Replace("preview_", "") & """},
        {""name"": ""net.minecraft:launchwrapper:1.12""}
    ],
    ""mainClass"": ""net.minecraft.launchwrapper.Launch"","
                                                                    Task.Progress = 0.8
                                                                    If InheritVersion.IsOldJson Then
                                                                        '输出旧版 Json 格式
                                                                        Json += "
    ""minimumLauncherVersion"": 18,
    ""minecraftArguments"": """ & InheritVersion.JsonObject("minecraftArguments").ToString & "  --tweakClass optifine.OptiFineTweaker""
}"
                                                                    Else
                                                                        '输出新版 Json 格式
                                                                        Json += "
    ""minimumLauncherVersion"": ""21"",
    ""arguments"": {
        ""game"": [
            ""--tweakClass"",
            ""optifine.OptiFineTweaker""
        ]
    }
}"
                                                                    End If
                                                                    WriteFile(VersionFolder & Id & ".json", Json)
                                                                Catch ex As Exception
                                                                    Throw New Exception("安装旧版 OptiFine 版本失败", ex)
                                                                End Try
                                                            End Sub) With {.ProgressWeight = 1})
        End If

        '下载支持库
        If FixLibrary Then
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析 OptiFine 支持库文件",
                                                                Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionFolder))) With {.ProgressWeight = 1, .Show = False})
            Loaders.Add(New LoaderDownload("下载 OptiFine 支持库文件", New List(Of NetFile)) With {.ProgressWeight = 4})
        End If

        Return Loaders
    End Function
    ''' <summary>
    ''' 获取保存某个 OptiFine 版本的加载器列表。
    ''' </summary>
    Private Function McDownloadOptiFineSaveLoader(DownloadInfo As DlOptiFineListEntry, TargetFolder As String) As List(Of LoaderBase)
        Dim Loaders As New List(Of LoaderBase)
        '获取下载地址
        Loaders.Add(New LoaderTask(Of DlOptiFineListEntry, List(Of NetFile))("获取 OptiFine 下载地址",
                                                            Sub(Task As LoaderTask(Of DlOptiFineListEntry, List(Of NetFile)))
                                                                Dim Sources As New List(Of String)
                                                                'BMCLAPI 源
                                                                If DownloadInfo.IsPreview Then
                                                                    Sources.Add("https://download.mcbbs.net/optifine/" & DownloadInfo.Inherit & "/HD_U_" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", "").Replace(" ", "/"))
                                                                    Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & DownloadInfo.Inherit & "/HD_U_" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", "").Replace(" ", "/"))
                                                                Else
                                                                    Sources.Add("https://download.mcbbs.net/optifine/" & DownloadInfo.Inherit & "/HD_U/" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", ""))
                                                                    Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & DownloadInfo.Inherit & "/HD_U/" & DownloadInfo.NameDisplay.Replace(DownloadInfo.Inherit & " ", ""))
                                                                End If
                                                                '官方源
                                                                Dim PageData As String
                                                                Try
                                                                    PageData = NetGetCodeByClient("https://optifine.net/adloadx?f=" & DownloadInfo.NameFile, New UTF8Encoding(False), 15000, "text/html")
                                                                    Task.Progress = 0.8
                                                                    Sources.Add("https://optifine.net/" & RegexSearch(PageData, "downloadx\?f=[^""']+")(0))
                                                                    Log("[Download] OptiFine " & DownloadInfo.NameDisplay & " 官方下载地址：" & Sources(0))
                                                                Catch ex As Exception
                                                                    Log(ex, "获取 OptiFine " & DownloadInfo.NameDisplay & " 官方下载地址失败")
                                                                End Try
                                                                Task.Progress = 0.9
                                                                '构造文件请求
                                                                Task.Output = New List(Of NetFile) From {New NetFile(Sources.ToArray, TargetFolder, New FileChecker(MinSize:=64 * 1024))}
                                                            End Sub) With {.ProgressWeight = 6})
        '下载
        Loaders.Add(New LoaderDownload("下载 OptiFine 主文件", New List(Of NetFile)) With {.ProgressWeight = 10, .Block = True})
        Return Loaders
    End Function

#End Region

#Region "OptiFine 下载菜单"

    Public Function OptiFineDownloadListItem(Entry As DlOptiFineListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyListItem
        '建立控件
        Dim NewItem As New MyListItem With {
            .Title = Entry.NameDisplay, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 60,
            .Info = If(Entry.IsPreview, "测试版", "正式版") & If(Entry.ReleaseTime = "", "", "，发布于 " & Entry.ReleaseTime),
            .Logo = "pack://application:,,,/images/Blocks/GrassPath.png"
        }
        AddHandler NewItem.Click, OnClick
        '建立菜单
        If IsSaveOnly Then
            NewItem.ContentHandler = AddressOf OptiFineSaveContMenuBuild
        Else
            NewItem.ContentHandler = AddressOf OptiFineContMenuBuild
        End If
        '结束
        Return NewItem
    End Function
    Private Sub OptiFineSaveContMenuBuild(sender As Object, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        ToolTipService.SetPlacement(BtnInfo, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnInfo, 30)
        ToolTipService.SetHorizontalOffset(BtnInfo, 2)
        AddHandler BtnInfo.Click, AddressOf OptiFineLog_Click
        sender.Buttons = {BtnInfo}
    End Sub
    Private Sub OptiFineContMenuBuild(sender As Object, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        ToolTipService.SetPlacement(BtnSave, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnSave, 30)
        ToolTipService.SetHorizontalOffset(BtnSave, 2)
        AddHandler BtnSave.Click, AddressOf OptiFineSave_Click
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        ToolTipService.SetPlacement(BtnInfo, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnInfo, 30)
        ToolTipService.SetHorizontalOffset(BtnInfo, 2)
        AddHandler BtnInfo.Click, AddressOf OptiFineLog_Click
        sender.Buttons = {BtnSave, BtnInfo}
    End Sub
    Private Sub OptiFineLog_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlOptiFineListEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        OpenWebsite("https://optifine.net/changelog?f=" & Version.NameFile)
    End Sub
    Public Sub OptiFineSave_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlOptiFineListEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        McDownloadOptiFineSave(Version)
    End Sub

#End Region

#Region "LiteLoader 下载"

    Public Sub McDownloadLiteLoader(DownloadInfo As DlLiteLoaderListEntry)
        Try
            Dim Id As String = DownloadInfo.Inherit
            Dim Target As String = PathTemp & "Download\" & Id & "-Liteloader.jar"
            Dim VersionName As String = DownloadInfo.Inherit & "-LiteLoader"
            Dim VersionFolder As String = PathMcFolder & "versions\" & VersionName & "\"

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "LiteLoader " & Id & " 下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            '已有版本检查
            If File.Exists(VersionFolder & VersionName & ".json") Then
                If MyMsgBox("版本 " & VersionName & " 已存在，是否重新下载？" & vbCrLf & "这会覆盖版本的 Json 文件，但不会影响版本隔离的文件。", "版本已存在", "继续", "取消") = 1 Then
                    File.Delete(VersionFolder & VersionName & ".jar")
                    File.Delete(VersionFolder & VersionName & ".json")
                Else
                    Exit Sub
                End If
            End If

            '启动
            Dim Loader As New LoaderCombo(Of String)("LiteLoader " & Id & " 下载", McDownloadLiteLoaderLoader(DownloadInfo)) With {.OnStateChanged = AddressOf McInstallState}
            Loader.Start(VersionFolder)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 LiteLoader 下载失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub McDownloadLiteLoaderSave(DownloadInfo As DlLiteLoaderListEntry)
        Try
            Dim Id As String = DownloadInfo.Inherit
            Dim Target As String = SelectAs("选择保存位置", DownloadInfo.FileName.Replace("-SNAPSHOT", ""), "LiteLoader 安装器 (*.jar)|*.jar")
            If Not Target.Contains("\") Then Exit Sub

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "LiteLoader " & Id & " 下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            '构造步骤加载器
            Dim Loaders As New List(Of LoaderBase)
            '下载
            Dim Address As New List(Of String)
            If DownloadInfo.IsLegacy Then
                '老版本
                Select Case DownloadInfo.Inherit
                    Case "1.7.10"
                        Address.Add("http://dl.liteloader.com/redist/1.7.10/liteloader-installer-1.7.10-04.jar")
                    Case "1.7.2"
                        Address.Add("http://dl.liteloader.com/redist/1.7.2/liteloader-installer-1.7.2-04.jar")
                    Case "1.6.4"
                        Address.Add("http://dl.liteloader.com/redist/1.6.4/liteloader-installer-1.6.4-01.jar")
                    Case "1.6.2"
                        Address.Add("http://dl.liteloader.com/redist/1.6.2/liteloader-installer-1.6.2-04.jar")
                    Case "1.5.2"
                        'Address.AddRange({"https://download.mcbbs.net/maven/com/mumfrey/liteloader/1.5.2/liteloader-1.5.2.jar",
                        '                  "https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/1.5.2/liteloader-1.5.2.jar"})
                        Address.Add("http://dl.liteloader.com/redist/1.5.2/liteloader-installer-1.5.2-01.jar")
                    Case Else
                        Throw New NotSupportedException("未知的 Minecraft 版本（" & DownloadInfo.Inherit & "）")
                End Select
            Else
                'BMCLAPI 源下载的是安装后的 Jar，而不是 Installer，故无法使用
                ''BMCLAPI 源
                'If DownloadInfo.IsPreview Then
                '    Address.AddRange({"https://download.mcbbs.net/maven/com/mumfrey/liteloader/" & DownloadInfo.Inherit & "-SNAPSHOT/liteloader-" & DownloadInfo.Inherit & "-SNAPSHOT.jar",
                '                      "https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/" & DownloadInfo.Inherit & "-SNAPSHOT/liteloader-" & DownloadInfo.Inherit & "-SNAPSHOT.jar"})
                'Else
                '    Address.AddRange({"https://download.mcbbs.net/maven/com/mumfrey/liteloader/" & DownloadInfo.Inherit & "/liteloader-" & DownloadInfo.Inherit & ".jar",
                '                      "https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/" & DownloadInfo.Inherit & "/liteloader-" & DownloadInfo.Inherit & ".jar"})
                'End If
                '官方源
                Address.Add("http://jenkins.liteloader.com/job/LiteLoaderInstaller%20" & DownloadInfo.Inherit & "/lastSuccessfulBuild/artifact/" & If(DownloadInfo.Inherit = "1.8", "ant/dist/", "build/libs/") & DownloadInfo.FileName)
            End If
            Loaders.Add(New LoaderDownload("下载主文件", New List(Of NetFile) From {New NetFile(Address.ToArray, Target, New FileChecker(MinSize:=1024 * 1024))}) With {.ProgressWeight = 15})
            '启动
            Dim Loader As New LoaderCombo(Of DlLiteLoaderListEntry)("LiteLoader " & Id & " 安装器下载", Loaders) With {.OnStateChanged = AddressOf DownloadStateSave}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 LiteLoader 安装器下载失败", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 获取下载某个 LiteLoader 版本的加载器列表。
    ''' </summary>
    Private Function McDownloadLiteLoaderLoader(DownloadInfo As DlLiteLoaderListEntry, Optional McFolder As String = Nothing, Optional ClientDownloadLoader As LoaderCombo(Of String) = Nothing, Optional FixLibrary As Boolean = True) As List(Of LoaderBase)

        '参数初始化
        McFolder = If(McFolder, PathMcFolder)
        Dim IsCustomFolder As Boolean = McFolder <> PathMcFolder
        Dim Id As String = DownloadInfo.Inherit
        Dim Target As String = PathTemp & "Download\" & Id & "-Liteloader.jar"
        Dim VersionName As String = DownloadInfo.Inherit & "-LiteLoader"
        Dim VersionFolder As String = McFolder & "versions\" & VersionName & "\"
        Dim Loaders As New List(Of LoaderBase)

        '启动依赖版本的下载
        If ClientDownloadLoader Is Nothing Then
            Loaders.Add(New LoaderTask(Of String, String)(
                    "启动 LiteLoader 依赖版本下载",
                    Sub()
                        If IsCustomFolder Then Throw New Exception("如果没有指定原版下载器，则不能指定 MC 安装文件夹")
                        ClientDownloadLoader = McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, DownloadInfo.Inherit)
                    End Sub) With {.ProgressWeight = 0.2, .Show = False, .Block = False})
        End If
        '安装
        Loaders.Add(New LoaderTask(Of String, String)("安装 LiteLoader",
                                                            Sub(Task As LoaderTask(Of String, String))
                                                                Try
                                                                    '新建版本文件夹
                                                                    Directory.CreateDirectory(VersionFolder)
                                                                    '构造版本 Json
                                                                    Dim VersionJson As New JObject
                                                                    VersionJson.Add("id", VersionName)
                                                                    VersionJson.Add("time", DateTime.ParseExact(DownloadInfo.ReleaseTime, "yyyy/MM/dd HH:mm:ss", Globalization.CultureInfo.CurrentCulture))
                                                                    VersionJson.Add("releaseTime", DateTime.ParseExact(DownloadInfo.ReleaseTime, "yyyy/MM/dd HH:mm:ss", Globalization.CultureInfo.CurrentCulture))
                                                                    VersionJson.Add("type", "release")
                                                                    VersionJson.Add("arguments", GetJson("{""game"":[""--tweakClass"",""" & DownloadInfo.JsonToken("tweakClass").ToString & """]}"))
                                                                    VersionJson.Add("libraries", DownloadInfo.JsonToken("libraries"))
                                                                    CType(VersionJson("libraries"), JContainer).Add(GetJson("{""name"": ""com.mumfrey:liteloader:" & DownloadInfo.JsonToken("version").ToString & """,""url"": ""http://dl.liteloader.com/versions/""}"))
                                                                    VersionJson.Add("mainClass", "net.minecraft.launchwrapper.Launch")
                                                                    VersionJson.Add("minimumLauncherVersion", 18)
                                                                    VersionJson.Add("inheritsFrom", DownloadInfo.Inherit)
                                                                    VersionJson.Add("jar", DownloadInfo.Inherit)
                                                                    '输出 Json 文件
                                                                    WriteFile(VersionFolder & VersionName & ".json", VersionJson.ToString)
                                                                Catch ex As Exception
                                                                    Throw New Exception("安装新 LiteLoader 版本失败", ex)
                                                                End Try
                                                            End Sub) With {.ProgressWeight = 1})
        '下载支持库
        If FixLibrary Then
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析 LiteLoader 支持库文件",
                                                                Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionFolder))) With {.ProgressWeight = 1, .Show = False})
            Loaders.Add(New LoaderDownload("下载 LiteLoader 支持库文件", New List(Of NetFile)) With {.ProgressWeight = 6})
        End If

        Return Loaders
    End Function

#End Region

#Region "LiteLoader 下载菜单"

    Public Function LiteLoaderDownloadListItem(Entry As DlLiteLoaderListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyListItem
        '建立控件
        Dim NewItem As New MyListItem With {
            .Title = Entry.Inherit, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 30,
            .Info = If(Entry.IsPreview, "测试版", "稳定版") & If(Entry.ReleaseTime = "", "", "，发布于 " & Entry.ReleaseTime),
            .Logo = "pack://application:,,,/images/Blocks/Egg.png"
        }
        AddHandler NewItem.Click, OnClick
        '建立菜单
        If IsSaveOnly Then
            NewItem.ContentHandler = AddressOf LiteLoaderSaveContMenuBuild
        Else
            NewItem.ContentHandler = AddressOf LiteLoaderContMenuBuild
        End If
        '结束
        Return NewItem
    End Function
    Private Sub LiteLoaderSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        If sender.Tag.IsLegacy Then
            sender.PaddingRight = 0
            sender.Buttons = {}
        Else
            sender.PaddingRight = 30
            Dim BtnList As New MyIconButton With {.Logo = Logo.IconButtonList, .ToolTip = "查看全部版本", .Tag = sender}
            ToolTipService.SetPlacement(BtnList, Primitives.PlacementMode.Center)
            ToolTipService.SetVerticalOffset(BtnList, 30)
            ToolTipService.SetHorizontalOffset(BtnList, 2)
            AddHandler BtnList.Click, AddressOf LiteLoaderAll_Click
            sender.Buttons = {BtnList}
        End If
    End Sub
    Private Sub LiteLoaderContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "保存安装器", .Tag = sender}
        ToolTipService.SetPlacement(BtnSave, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnSave, 30)
        ToolTipService.SetHorizontalOffset(BtnSave, 2)
        AddHandler BtnSave.Click, AddressOf LiteLoaderSave_Click
        If sender.Tag.IsLegacy Then
            sender.PaddingRight = 30
            sender.Buttons = {BtnSave}
        Else
            sender.PaddingRight = 60
            Dim BtnList As New MyIconButton With {.Logo = Logo.IconButtonList, .ToolTip = "查看全部版本", .Tag = sender}
            ToolTipService.SetPlacement(BtnList, Primitives.PlacementMode.Center)
            ToolTipService.SetVerticalOffset(BtnList, 30)
            ToolTipService.SetHorizontalOffset(BtnList, 2)
            AddHandler BtnList.Click, AddressOf LiteLoaderAll_Click
            sender.Buttons = {BtnSave, BtnList}
        End If
    End Sub
    Private Sub LiteLoaderAll_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlLiteLoaderListEntry
        If TypeOf sender.Tag Is DlLiteLoaderListEntry Then
            Version = sender.Tag
        Else
            Version = sender.Tag.Tag
        End If
        OpenWebsite("http://jenkins.liteloader.com/view/" & Version.Inherit)
    End Sub
    Public Sub LiteLoaderSave_Click(sender As Object, e As RoutedEventArgs)
        'ListItem 与小按钮都会调用这个方法
        Dim Version As DlLiteLoaderListEntry
        If TypeOf sender.Tag Is DlLiteLoaderListEntry Then
            Version = sender.Tag
        Else
            Version = sender.Tag.Tag
        End If
        McDownloadLiteLoaderSave(Version)
    End Sub

#End Region

#Region "Forge 下载"

    Public Sub McDownloadForge(DownloadInfo As DlForgeVersionEntry)
        '老版本提示
        If DownloadInfo.Category = "client" Then
            If MyMsgBox("该 Forge 版本过于古老，PCL2 暂不支持该版本的自动安装。" & vbCrLf &
                        "若你仍然希望继续，PCL2 将把安装程序下载到你指定的位置，但不会进行安装。",
                        "版本过老", "继续", "取消") = 1 Then
                McDownloadForgeSave(DownloadInfo)
            End If
            Exit Sub
        End If
        If DownloadInfo.Category = "universal" OrElse DownloadInfo.Inherit.StartsWith("1.5") Then '对该版本自动安装的支持将在之后加入
            If MyMsgBox("该 Forge 版本过于古老，PCL2 暂不支持该版本的自动安装。" & vbCrLf &
                        "若你仍然希望继续，PCL2 将把安装程序下载到你指定的位置，但不会进行安装。",
                        "版本过老", "继续", "取消") = 1 Then
                McDownloadForgeSave(DownloadInfo)
            End If
            Exit Sub
        End If
        '初始化参数
        Dim Id As String = DownloadInfo.Inherit & "-forge-" & DownloadInfo.Version
        Dim Target As String = PathTemp & "Cache\Code\ForgeInstall-" & DownloadInfo.Version & "_" & GetUuid() & "." & DownloadInfo.FileSuffix
        Dim VersionFolder As String = PathMcFolder & "versions\" & Id & "\"
        Dim DisplayName As String = "Forge " & DownloadInfo.Inherit & " - " & DownloadInfo.Version
        Try

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = DisplayName & " 下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            '已有版本检查
            If File.Exists(VersionFolder & Id & ".json") Then
                If MyMsgBox("版本 " & Id & " 已存在，是否重新下载？" & vbCrLf & "这会覆盖版本的 Json 文件，但不会影响版本隔离的文件。", "版本已存在", "继续", "取消") = 1 Then
                    File.Delete(VersionFolder & Id & ".jar")
                    File.Delete(VersionFolder & Id & ".json")
                Else
                    Exit Sub
                End If
            End If

            '启动
            Dim Loader As New LoaderCombo(Of String)(DisplayName & " 下载", McDownloadForgeLoader(DownloadInfo.Version, DownloadInfo.Inherit, DownloadInfo)) With {.OnStateChanged = AddressOf McInstallState}
            Loader.Start(VersionFolder)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 Forge 下载失败", LogLevel.Feedback)
        Finally
            '删除安装包
            Try
                If File.Exists(Target) Then File.Delete(Target)
            Catch
            End Try
        End Try
    End Sub
    Public Sub McDownloadForgeSave(DownloadInfo As DlForgeVersionEntry)
        Try
            Dim Target As String = SelectAs("选择保存位置", DownloadInfo.FileName, "Forge 文件 (*." & DownloadInfo.FileSuffix & ")|*." & DownloadInfo.FileSuffix)
            Dim DisplayName As String = "Forge " & DownloadInfo.Inherit & " - " & DownloadInfo.Version
            If Not Target.Contains("\") Then Exit Sub

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = DisplayName & " 下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            '构造步骤加载器
            Dim Loaders As New List(Of LoaderBase)
            '获取下载地址
            Loaders.Add(New LoaderTask(Of DlForgeVersionEntry, List(Of NetFile))("获取下载地址",
                                                            Sub(Task As LoaderTask(Of DlForgeVersionEntry, List(Of NetFile)))
                                                                Dim Files As New List(Of NetFile)
                                                                Files.Add(New NetFile({
                                                                        "https://download.mcbbs.net/maven/net/minecraftforge/forge/" & DownloadInfo.Inherit & "-" & DownloadInfo.FileVersion & "/" & DownloadInfo.FileName,
                                                                        "https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/" & DownloadInfo.Inherit & "-" & DownloadInfo.FileVersion & "/" & DownloadInfo.FileName,
                                                                        "http://files.minecraftforge.net/maven/net/minecraftforge/forge/" & DownloadInfo.Inherit & "-" & DownloadInfo.FileVersion & "/" & DownloadInfo.FileName
                                                                    }, Target, New FileChecker(MinSize:=64 * 1024, Hash:=DownloadInfo.Hash)))
                                                                Task.Output = Files
                                                            End Sub) With {.ProgressWeight = 0.1, .Show = False})
            '下载
            Loaders.Add(New LoaderDownload("下载主文件", New List(Of NetFile)) With {.ProgressWeight = 6})

            '启动
            Dim Loader As New LoaderCombo(Of DlForgeVersionEntry)(DisplayName & " 下载", Loaders) With {.OnStateChanged = AddressOf DownloadStateSave}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 Forge 下载失败", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub ForgeInjector(Target As String, Task As LoaderTask(Of Boolean, Boolean), McFolder As String)
        '选择 Java
        Dim Java = JavaSelect(New Version(1, 8, 0, 60))
        If Java Is Nothing Then
            JavaMissing(8)
            Throw New Exception("未找到用于安装 Forge 的 Java")
        End If
        '开始启动
        Dim Argument As String = String.Format("-cp ""{0};{1}"" com.bangbang93.ForgeInstaller ""{2}", PathTemp & "Cache\forge_installer.jar", Target, McFolder)
        Dim Info = New ProcessStartInfo With {
            .FileName = Java.PathJavaw,
            .Arguments = Argument,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardError = True,
            .RedirectStandardOutput = True
        }
        Log("[Download] 开始安装 Forge：" & Argument)
        Dim process As New Process With {.StartInfo = Info}
        Dim LastResults As New Queue(Of String)
        Using outputWaitHandle As New AutoResetEvent(False)
            Using errorWaitHandle As New AutoResetEvent(False)
                AddHandler process.OutputDataReceived, Function(sender, e)
                                                           Try
                                                               If e.Data Is Nothing Then
                                                                   outputWaitHandle.[Set]()
                                                               Else
                                                                   LastResults.Enqueue(e.Data)
                                                                   If LastResults.Count > 100 Then LastResults.Dequeue()
                                                                   ForgeInjectorLine(e.Data, Task)
                                                               End If
                                                           Catch ex As ObjectDisposedException
                                                           Catch ex As Exception
                                                               Log(ex, "读取 Forge 安装器信息失败")
                                                           End Try
                                                           Try
                                                               If Task.State = LoadState.Aborted AndAlso Not process.HasExited Then
                                                                   Log("[Installer] 由于任务取消，已中止 Forge 安装")
                                                                   process.Kill()
                                                               End If
                                                           Catch
                                                           End Try
                                                           Return Nothing
                                                       End Function
                AddHandler process.ErrorDataReceived, Function(sender, e)
                                                          Try
                                                              If e.Data Is Nothing Then
                                                                  errorWaitHandle.[Set]()
                                                              Else
                                                                  LastResults.Enqueue(e.Data)
                                                                  If LastResults.Count > 100 Then LastResults.Dequeue()
                                                                  ForgeInjectorLine(e.Data, Task)
                                                              End If
                                                          Catch ex As ObjectDisposedException
                                                          Catch ex As Exception
                                                              Log(ex, "读取 Forge 安装器错误信息失败")
                                                          End Try
                                                          Try
                                                              If Task.State = LoadState.Aborted AndAlso Not process.HasExited Then
                                                                  Log("[Installer] 由于任务取消，已中止 Forge 安装")
                                                                  process.Kill()
                                                              End If
                                                          Catch
                                                          End Try
                                                          Return Nothing
                                                      End Function
                process.Start()
                process.BeginOutputReadLine()
                process.BeginErrorReadLine()
                '等待
                Do Until process.HasExited
                    Thread.Sleep(10)
                Loop
                '输出
                outputWaitHandle.WaitOne(10000)
                errorWaitHandle.WaitOne(10000)
                process.Dispose()
                If LastResults.Last = "true" Then Exit Sub
                Log(Join(LastResults, vbCrLf))
                Throw New Exception("Forge 安装器出错，末行为 " & LastResults.Last)
            End Using
        End Using
        ''判断结果
        'Dim ReturnData As String =
        'If ModeDebug Then Log("[Download] Forge 安装器 Log：" & vbCrLf & ReturnData & vbCrLf)
        '                                                            If Not ReturnData.TrimEnd(vbCrLf.ToCharArray()).EndsWith("true") Then
        '    Throw New Exception("自动安装过程出错，日志尾部：" & vbCrLf & ReturnData.Substring(Math.Max(0, ReturnData.Length - 1000)))
        'End If
    End Sub
    Private Sub ForgeInjectorLine(Content As String, Task As LoaderTask(Of Boolean, Boolean))
        If Content.StartsWith("  Data") OrElse Content.StartsWith("  Slim") Then
            If ModeDebug Then Log("[Installer] " & Content)
        ElseIf Content.StartsWith("  Reading patch ") Then
            If ModeDebug Then Log("[Installer] " & Content)
            Task.Progress = 0.86
        Else
            Select Case Content
                Case "Extracting json"
                    Log("[Installer] " & Content)
                    Task.Progress = 0.07
                Case "Downloading libraries"
                    Log("[Installer] " & Content)
                    Task.Progress = 0.08
                Case "  File exists: Checksum validated."
                    If ModeDebug Then Log("[Installer] " & Content)
                    Task.Progress += 0.003
                Case "Building Processors"
                    Task.Progress = 0.38
                Case "Task: DOWNLOAD_MOJMAPS" 'B
                    Task.Progress = 0.4
                Case "Task: MERGE_MAPPING" 'B
                    Task.Progress = 0.6
                Case "Splitting: "
                    Task.Progress = 0.62
                Case "Parameter Annotations" 'B
                    Task.Progress = 0.66
                Case "Processing Complete" 'B
                    Task.Progress = 0.7
                Case "Remapping final jar" 'A
                    Task.Progress = 0.72
                Case "Remapping jar... 50%" 'A
                    Task.Progress = 0.76
                Case "Remapping jar... 100%" 'A
                    Task.Progress = 0.81
                Case "Injecting profile"
                    Task.Progress = 0.91
                Case Else
                    If ModeDebug Then Log("[Installer] " & Content)
                    Exit Sub
            End Select
            Log("[Installer] " & Content)
        End If
    End Sub

    ''' <summary>
    ''' 获取下载某个 Forge 版本的加载器列表。
    ''' </summary>
    Private Function McDownloadForgeLoader(Version As String, Inherit As String, Optional DownloadInfo As DlForgeVersionEntry = Nothing, Optional McFolder As String = Nothing, Optional ClientDownloadLoader As LoaderCombo(Of String) = Nothing, Optional FixLibrary As Boolean = True) As List(Of LoaderBase)

        '参数初始化
        McFolder = If(McFolder, PathMcFolder)
        Dim IsCustomFolder As Boolean = McFolder <> PathMcFolder
        Dim Id As String = Inherit & "-forge-" & Version
        Dim InstallerAddress As String = PathTemp & "Cache\Code\ForgeInstall-" & Version & "_" & RandomInteger(0, 100000)
        Dim VersionFolder As String = McFolder & "versions\" & Id & "\"
        Dim DisplayName As String = "Forge " & Inherit & " - " & Version
        Dim Loaders As New List(Of LoaderBase)
        Dim LibVersionFolder As String = PathMcFolder & "versions\" & Id & "\" '作为 Lib 文件目标的版本文件夹

        '获取下载信息
        If DownloadInfo Is Nothing Then
            Loaders.Add(New LoaderTask(Of String, String)("获取 Mod 加载器详细信息",
                                                            Sub(Task As LoaderTask(Of String, String))
                                                                '获取 Forge 版本列表
                                                                Dim ForgeLoader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("McDownloadForgeLoader " & Inherit, AddressOf DlForgeVersionMain)
                                                                ForgeLoader.WaitForExit(Inherit)
                                                                Task.Progress = 0.8
                                                                '查找对应版本
                                                                For Each ForgeVersion In ForgeLoader.Output
                                                                    If ForgeVersion.Version = Version Then
                                                                        DownloadInfo = ForgeVersion
                                                                        Exit Sub
                                                                    End If
                                                                Next
                                                                Throw New Exception("未能找到 Forge " & Inherit & "-" & Version & " 的详细信息！")
                                                            End Sub) With {.ProgressWeight = 3})
        End If
        '下载 Forge 主文件
        Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("准备 Mod 加载器下载",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                '启动依赖版本的下载
                                                                If ClientDownloadLoader Is Nothing Then
                                                                    If IsCustomFolder Then Throw New Exception("如果没有指定原版下载器，则不能指定 MC 安装文件夹")
                                                                    ClientDownloadLoader = McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, Inherit)
                                                                End If
                                                                '添加主文件
                                                                Dim Files As New List(Of NetFile) From {New NetFile({
                                                                    "https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/" & Inherit & "-" & DownloadInfo.FileVersion & "/" & DownloadInfo.FileName,
                                                                    "http://files.minecraftforge.net/maven/net/minecraftforge/forge/" & Inherit & "-" & DownloadInfo.FileVersion & "/" & DownloadInfo.FileName,
                                                                    "https://download.mcbbs.net/maven/net/minecraftforge/forge/" & Inherit & "-" & DownloadInfo.FileVersion & "/" & DownloadInfo.FileName
                                                                }, InstallerAddress, New FileChecker(MinSize:=64 * 1024, Hash:=DownloadInfo.Hash))}
                                                                Task.Output = Files
                                                            End Sub) With {.ProgressWeight = 0.5, .Show = False})
        Loaders.Add(New LoaderDownload("下载 Mod 加载器主文件", New List(Of NetFile)) With {.ProgressWeight = 9})

        '安装（仅在新版安装时需要原版 Jar）
        If Version.Split(".")(0) >= 20 Then
            Log("[Download] 检测为新版 Forge：" & Version)
            Dim Libs As List(Of McLibToken) = Nothing
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析 Mod 加载器支持库文件", Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                                           Task.Output = New List(Of NetFile)
                                                                                           Dim Installer As ZipArchive = Nothing
                                                                                           Try
                                                                                               '解压并获取、合并两个 Json 的信息
                                                                                               Installer = New ZipArchive(New FileStream(InstallerAddress, FileMode.Open))
                                                                                               Task.Progress = 0.2
                                                                                               Dim Json As JObject = GetJson(ReadFile(Installer.GetEntry("install_profile.json").Open))
                                                                                               Dim Json2 As JObject = GetJson(ReadFile(Installer.GetEntry("version.json").Open))
                                                                                               Json.Merge(Json2)
                                                                                               '获取 Lib 下载信息
                                                                                               Libs = McLibListGetWithJson(Json, True)
                                                                                               '添加 Mappings 下载信息
                                                                                               If Json("data") IsNot Nothing AndAlso Json("data")("MOJMAPS") IsNot Nothing Then
                                                                                                   '下载原版 Json 文件
                                                                                                   Task.Progress = 0.4
                                                                                                   Log("[Download] 需要 Mappings 下载信息，开始获取原版 Json 文件")
                                                                                                   Dim RawJson As JObject = GetJson(NetGetCodeByDownload(DlSourceLauncherOrMetaGet(DlClientListGet(Inherit)), IsJson:=True))
                                                                                                   '[net.minecraft:client:1.17.1-20210706.113038:mappings@txt]
                                                                                                   Dim OriginalName As String = Json("data")("MOJMAPS")("client").ToString.Trim("[]".ToCharArray()).Split("@")(0)
                                                                                                   Dim Address = McLibGet(OriginalName).Replace(".jar", "-mappings.txt")
                                                                                                   Libs.Add(New McLibToken With {.IsJumpLoader = False, .IsNatives = False, .LocalPath = Address, .OriginalName = OriginalName,
                                                                                                            .Url = RawJson("downloads")("client_mappings")("url"), .Size = RawJson("downloads")("client_mappings")("size"), .SHA1 = RawJson("downloads")("client_mappings")("sha1")})
                                                                                               End If
                                                                                               Task.Progress = 0.8
                                                                                               '去除其中的原始 Forge 项
                                                                                               For i = 0 To Libs.Count - 1
                                                                                                   If Libs(i).LocalPath.EndsWith("forge-" & Inherit & "-" & Version & ".jar") Then
                                                                                                       Libs.RemoveAt(i)
                                                                                                       Exit For
                                                                                                   End If
                                                                                               Next
                                                                                               Task.Output = McLibFixFromLibToken(Libs, PathMcFolder)
                                                                                           Catch ex As Exception
                                                                                               Throw New Exception("获取新版 Forge 支持库列表失败", ex)
                                                                                           Finally
                                                                                               '释放文件
                                                                                               If Installer IsNot Nothing Then Installer.Dispose()
                                                                                           End Try
                                                                                       End Sub) With {.ProgressWeight = 2})
            Loaders.Add(New LoaderDownload("下载 Mod 加载器支持库文件", New List(Of NetFile)) With {.ProgressWeight = 12})
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("获取 Mod 下载器支持库文件",
                                                                         Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
#Region "Forge 文件"
                                                                             If IsCustomFolder Then
                                                                                 For Each LibFile As McLibToken In Libs
                                                                                     Dim RealPath As String = LibFile.LocalPath.Replace(PathMcFolder, McFolder)
                                                                                     If Not File.Exists(RealPath) Then
                                                                                         Directory.CreateDirectory(IO.Path.GetDirectoryName(RealPath))
                                                                                         File.Copy(LibFile.LocalPath, RealPath)
                                                                                     End If
                                                                                     If ModeDebug Then Log("[Download] 复制的 Forge 支持库文件：" & LibFile.LocalPath)
                                                                                 Next
                                                                             End If
#End Region
#Region "原版文件"
                                                                             '是否已经存在原版文件
                                                                             If ClientDownloadLoader Is Nothing Then Exit Sub
                                                                             '等待原版文件下载完成
                                                                             For Each Loader In ClientDownloadLoader.GetLoaderList
                                                                                 If Loader.Name <> McDownloadClientLibName Then Continue For
                                                                                 Loader.WaitForExit()
                                                                                 Exit For
                                                                             Next
                                                                             '拷贝原版文件
                                                                             If IsCustomFolder Then
                                                                                 Try
                                                                                     Dim ClientName As String = New DirectoryInfo(ClientDownloadLoader.Input).Name
                                                                                     Directory.CreateDirectory(McFolder & "versions\" & Inherit)
                                                                                     If Not File.Exists(McFolder & "versions\" & Inherit & "\" & Inherit & ".json") Then
                                                                                         File.Copy(ClientDownloadLoader.Input & ClientName & ".json", McFolder & "versions\" & Inherit & "\" & Inherit & ".json")
                                                                                     End If
                                                                                     If Not File.Exists(McFolder & "versions\" & Inherit & "\" & Inherit & ".jar") Then
                                                                                         File.Copy(ClientDownloadLoader.Input & ClientName & ".jar", McFolder & "versions\" & Inherit & "\" & Inherit & ".jar")
                                                                                     End If
                                                                                 Catch ex As Exception
                                                                                     'OptiFine 与 Forge 同时开始复制偶尔会导致冲突出错
                                                                                     Log(ex, "安装 Forge 拷贝原版文件时出错")
                                                                                 End Try
                                                                             End If
#End Region
                                                                         End Sub) With {.ProgressWeight = 0.1, .Show = False})
            Loaders.Add(New LoaderTask(Of Boolean, Boolean)("安装 Mod 加载器（方式 A）",
                                                            Sub(Task As LoaderTask(Of Boolean, Boolean))
                                                                Dim Installer As ZipArchive = Nothing
                                                                Try
                                                                    Log("[Download] 开始进行新版方式 Forge 安装：" & InstallerAddress)
                                                                    '解压并获取信息
                                                                    Installer = New ZipArchive(New FileStream(InstallerAddress, FileMode.Open))
                                                                    Dim Json As JObject = GetJson(ReadFile(Installer.GetEntry("install_profile.json").Open))
                                                                    '新建目标版本文件夹
                                                                    Directory.CreateDirectory(VersionFolder)
                                                                    '记录当前文件夹列表
                                                                    Dim OldList = New DirectoryInfo(McFolder & "versions\").EnumerateDirectories.Select(Function(Info As DirectoryInfo) As String
                                                                                                                                                            Return Info.FullName
                                                                                                                                                        End Function).ToList()
                                                                    Task.Progress = 0.04
                                                                    '释放 launcher_installer.json
                                                                    McFolderLauncherProfilesJsonCreate(McFolder)
                                                                    Task.Progress = 0.05
                                                                    '释放 Forge 注入器
                                                                    WriteFile(PathTemp & "Cache\forge_installer.jar", GetResources("ForgeInstaller"))
                                                                    Task.Progress = 0.06
                                                                    '运行注入器
                                                                    ForgeInjector(InstallerAddress, Task, McFolder)
                                                                    Task.Progress = 0.97
                                                                    '拷贝新增的版本 Json
                                                                    Dim DeltaList = New DirectoryInfo(McFolder & "versions\").EnumerateDirectories.SkipWhile(Function(Info As DirectoryInfo) As Boolean
                                                                                                                                                                 Return OldList.Contains(Info.FullName)
                                                                                                                                                             End Function).ToList()
                                                                    If DeltaList.Count = 1 Then '如果没有新增文件夹，那么预测的文件夹名就是正确的
                                                                        Dim RealFolder As DirectoryInfo = DeltaList(0)
                                                                        Dim JsonFile As FileInfo = RealFolder.EnumerateFiles.First()
                                                                        JsonFile.CopyTo(VersionFolder & Id & ".json", True)
                                                                    End If
                                                                    '新建 mods 文件夹
                                                                    Directory.CreateDirectory(New McVersion(VersionFolder).GetPathIndie(True) & "mods\")
                                                                Catch ex As Exception
                                                                    Throw New Exception("安装新 Forge 版本失败", ex)
                                                                End Try
                                                                '清理文件
                                                                Try
                                                                    If Installer IsNot Nothing Then Installer.Dispose()
                                                                    If File.Exists(InstallerAddress) Then My.Computer.FileSystem.DeleteFile(InstallerAddress)
                                                                Catch ex As Exception
                                                                    Log(ex, "安装 Forge 清理文件时出错")
                                                                End Try
                                                            End Sub) With {.ProgressWeight = 10})
        Else
            Log("[Download] 检测为非新版 Forge：" & Version)
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("安装 Mod 加载器（方式 B）",
                                                            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                                                                Dim Installer As ZipArchive = Nothing
                                                                Try
                                                                    '解压并获取信息
                                                                    Installer = New ZipArchive(New FileStream(InstallerAddress, FileMode.Open))
                                                                    Task.Progress = 0.2
                                                                    Dim Json As JObject = GetJson(ReadFile(Installer.GetEntry("install_profile.json").Open))
                                                                    Task.Progress = 0.4
                                                                    '新建版本文件夹
                                                                    Directory.CreateDirectory(VersionFolder)
                                                                    Task.Progress = 0.5
                                                                    If Json("install") Is Nothing Then
                                                                        '中版格式
                                                                        Log("[Download] 进行中版方式安装：" & InstallerAddress)
                                                                        '建立 Json 文件
                                                                        Dim JsonVersion As JObject = GetJson(ReadFile(Installer.GetEntry(Json("json").ToString.TrimStart("/")).Open))
                                                                        JsonVersion("id") = Id
                                                                        WriteFile(VersionFolder & Id & ".json", JsonVersion.ToString)
                                                                        Task.Progress = 0.6
                                                                        '解压支持库文件
                                                                        Installer.Dispose()
                                                                        ZipFile.ExtractToDirectory(InstallerAddress, InstallerAddress & "_unrar\")
                                                                        My.Computer.FileSystem.CopyDirectory(InstallerAddress & "_unrar\maven\", McFolder & "libraries\", True)
                                                                        DeleteDirectory(InstallerAddress & "_unrar\")
                                                                    Else
                                                                        '旧版格式
                                                                        Log("[Download] 进行旧版方式安装：" & InstallerAddress)
                                                                        '解压 Jar 文件
                                                                        Dim JarAddress As String = McLibGet(Json("install")("path"), CustomMcFolder:=McFolder)
                                                                        If File.Exists(JarAddress) Then File.Delete(JarAddress)
                                                                        WriteFile(JarAddress, Installer.GetEntry(Json("install")("filePath")).Open)
                                                                        Task.Progress = 0.9
                                                                        '建立 Json 文件
                                                                        Json("versionInfo")("id") = Id
                                                                        If Json("versionInfo")("inheritsFrom") Is Nothing Then CType(Json("versionInfo"), JObject).Add("inheritsFrom", Inherit)
                                                                        WriteFile(VersionFolder & Id & ".json", Json("versionInfo").ToString)
                                                                    End If
                                                                    '新建 mods 文件夹
                                                                    Directory.CreateDirectory(New McVersion(VersionFolder).GetPathIndie(True) & "mods\")
                                                                Catch ex As Exception
                                                                    Throw New Exception("非新版方式安装 Forge 失败", ex)
                                                                End Try
                                                                Try
                                                                    '释放文件
                                                                    If Installer IsNot Nothing Then Installer.Dispose()
                                                                    If File.Exists(InstallerAddress) Then My.Computer.FileSystem.DeleteFile(InstallerAddress)
                                                                    If Directory.Exists(InstallerAddress & "_unrar\") Then DeleteDirectory(InstallerAddress & "_unrar\")
                                                                Catch ex As Exception
                                                                    Log(ex, "非新版方式安装 Forge 清理文件时出错")
                                                                End Try
                                                            End Sub) With {.ProgressWeight = 1})
            If FixLibrary Then
                If IsCustomFolder Then Throw New Exception("若需要补全支持库，就不能自定义 MC 文件夹")
                Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析 Mod 加载器支持库文件", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionFolder))) With {.ProgressWeight = 1, .Show = False})
                Loaders.Add(New LoaderDownload("下载 Mod 加载器支持库文件", New List(Of NetFile)) With {.ProgressWeight = 11})
            End If
        End If

        Return Loaders
    End Function

#End Region

#Region "Forge 下载菜单"

    Public Sub ForgeDownloadListItemPreload(Stack As StackPanel, Entrys As List(Of DlForgeVersionEntry), OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean)
        '获取推荐版本与最新版本
        Dim FreshVersion As DlForgeVersionEntry = Nothing
        If Entrys.Count > 0 Then
            FreshVersion = Entrys(0)
        Else
            Log("[System] 未找到可用的 Forge 版本", LogLevel.Debug)
        End If
        Dim RecommendedVersion As DlForgeVersionEntry = Nothing
        For Each Entry In Entrys
            If Entry.IsRecommended Then RecommendedVersion = Entry
        Next
        '若推荐版本与最新版本为同一版本，则仅显示推荐版本
        If FreshVersion IsNot Nothing AndAlso FreshVersion Is RecommendedVersion Then FreshVersion = Nothing
        '显示各个版本
        If RecommendedVersion IsNot Nothing Then
            Dim Recommended = ForgeDownloadListItem(RecommendedVersion, OnClick, IsSaveOnly)
            Recommended.Info = "推荐版" & If(Recommended.Info = "", "", "，" & Recommended.Info)
            Stack.Children.Add(Recommended)
        End If
        If FreshVersion IsNot Nothing Then
            Dim Fresh = ForgeDownloadListItem(FreshVersion, OnClick, IsSaveOnly)
            Fresh.Info = "最新版" & If(Fresh.Info = "", "", "，" & Fresh.Info)
            Stack.Children.Add(Fresh)
        End If
        '添加间隔
        Stack.Children.Add(New TextBlock With {.Text = "全部版本 (" & Entrys.Count & ")", .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 13, 0, 4)})
    End Sub
    Public Function ForgeDownloadListItem(Entry As DlForgeVersionEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyListItem
        '建立控件
        Dim NewItem As New MyListItem With {
            .Title = Entry.Version, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 60,
            .Info = If(Entry.ReleaseTime = "",
                If(ModeDebug, "种类：" & Entry.Category & If(Entry.Branch Is Nothing, "", "，开发分支：" & Entry.Branch), ""),
                "发布于 " & Entry.ReleaseTime & If(ModeDebug, "，种类：" & Entry.Category & If(Entry.Branch Is Nothing, "", "，开发分支：" & Entry.Branch), "")),
            .Logo = "pack://application:,,,/images/Blocks/Anvil.png"
        }
        AddHandler NewItem.Click, OnClick
        '建立菜单
        If IsSaveOnly Then
            NewItem.ContentHandler = AddressOf ForgeSaveContMenuBuild
        Else
            NewItem.ContentHandler = AddressOf ForgeContMenuBuild
        End If
        '结束
        Return NewItem
    End Function
    Private Sub ForgeContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        ToolTipService.SetPlacement(BtnSave, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnSave, 30)
        ToolTipService.SetHorizontalOffset(BtnSave, 2)
        AddHandler BtnSave.Click, AddressOf ForgeSave_Click
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        ToolTipService.SetPlacement(BtnInfo, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnInfo, 30)
        ToolTipService.SetHorizontalOffset(BtnInfo, 2)
        AddHandler BtnInfo.Click, AddressOf ForgeLog_Click
        sender.Buttons = {BtnSave, BtnInfo}
    End Sub
    Private Sub ForgeSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        ToolTipService.SetPlacement(BtnInfo, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnInfo, 30)
        ToolTipService.SetHorizontalOffset(BtnInfo, 2)
        AddHandler BtnInfo.Click, AddressOf ForgeLog_Click
        sender.Buttons = {BtnInfo}
    End Sub
    Private Sub ForgeLog_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlForgeVersionEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        OpenWebsite("http://files.minecraftforge.net/maven/net/minecraftforge/forge/" & Version.Inherit & "-" & Version.Version & "/forge-" & Version.Inherit & "-" & Version.Version & "-changelog.txt")
    End Sub
    Public Sub ForgeSave_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlForgeVersionEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        McDownloadForgeSave(Version)
    End Sub

#End Region

#Region "Forge 推荐版本获取"

    ''' <summary>
    ''' 尝试刷新 Forge 推荐版本缓存。
    ''' </summary>
    Public Sub McDownloadForgeRecommendedRefresh()
        If IsForgeRecommendedRefreshed Then Exit Sub
        IsForgeRecommendedRefreshed = True
        RunInNewThread(Sub()
                           Try
                               Log("[Download] 刷新 Forge 推荐版本缓存开始")
                               Dim Result As String = NetGetCodeByDownload({"https://bmclapi2.bangbang93.com/forge/promos", "https://download.mcbbs.net/forge/promos"})
                               If Result.Length < 1000 Then Throw New Exception("获取的结果过短（" & Result & "）")
                               Dim ResultJson As JContainer = GetJson(Result)
                               '获取所有推荐版本列表
                               Dim RecommendedList As New List(Of String)
                               For Each Version As JObject In ResultJson
                                   If Version("name") Is Nothing OrElse Version("build") Is Nothing Then Continue For
                                   Dim Name As String = Version("name")
                                   If Not Name.EndsWith("-recommended") Then Continue For
                                   '内容为："1.15.2":"31.2.0"
                                   RecommendedList.Add("""" & Name.Replace("-recommended", """:""" & Version("build")("version").ToString & """"))
                               Next
                               If RecommendedList.Count < 5 Then Throw New Exception("获取的推荐版本数过少（" & Result & "）")
                               '保存
                               Dim CacheJson As String = "{" & Join(RecommendedList, ",") & "}"
                               WriteFile(PathTemp & "Cache\ForgeRecommendedList.json", CacheJson)
                               Log("[Download] 刷新 Forge 推荐版本缓存成功")
                           Catch ex As Exception
                               Log(ex, "刷新 Forge 推荐版本缓存失败")
                           End Try
                       End Sub, "ForgeRecommendedRefresh")
    End Sub
    Private IsForgeRecommendedRefreshed As Boolean = False

    ''' <summary>
    ''' 尝试获取某个 MC 版本对应的 Forge 推荐版本。如果不可用会返回 Nothing。
    ''' </summary>
    Public Function McDownloadForgeRecommendedGet(McVersion As String) As String
        Try
            If McVersion Is Nothing Then Return Nothing
            Dim List As String = ReadFile(PathTemp & "Cache\ForgeRecommendedList.json")
            If List Is Nothing OrElse List = "" Then
                Log("[Download] 没有 Forge 推荐版本缓存文件")
                Return Nothing
            End If
            Dim Json As JObject = GetJson(List)
            If Json Is Nothing OrElse (Not If(McVersion, "null").Contains(".")) OrElse Not Json.ContainsKey(McVersion) Then Return Nothing
            Return If(Json(McVersion), "").ToString
        Catch ex As Exception
            Log(ex, "获取 Forge 推荐版本失败（" & If(McVersion, "null") & "）", LogLevel.Feedback)
            Return Nothing
        End Try
    End Function

#End Region

#Region "Fabric 下载"

    Public Sub McDownloadFabricLoaderSave(DownloadInfo As JObject)
        Try
            Dim Url As String = DownloadInfo("url").ToString
            Dim FileName As String = GetFileNameFromPath(Url)
            Dim Version As String = GetFileNameFromPath(DownloadInfo("version").ToString)
            Dim Target As String = SelectAs("选择保存位置", FileName, "Fabric 安装器 (*.jar)|*.jar")
            If Not Target.Contains("\") Then Exit Sub

            '重复任务检查
            SyncLock LoaderTaskbarLock
                For i = 0 To LoaderTaskbar.Count - 1
                    If LoaderTaskbar(i).Name = "Fabric " & Version & " 安装器下载" Then
                        Hint("该版本正在下载中！", HintType.Critical)
                        Exit Sub
                    End If
                Next
            End SyncLock

            '构造步骤加载器
            Dim Loaders As New List(Of LoaderBase)
            '下载
            'BMCLAPI 不支持 Fabric Installer 下载
            Dim Address As New List(Of String)
            'Address.Add(Url.Replace("maven.fabricmc.net", "download.mcbbs.net/maven"))
            Address.Add(Url)
            Loaders.Add(New LoaderDownload("下载主文件", New List(Of NetFile) From {New NetFile(Address.ToArray, Target, New FileChecker(MinSize:=1024 * 64))}) With {.ProgressWeight = 15})
            '启动
            Dim Loader As New LoaderCombo(Of JObject)("Fabric " & Version & " 安装器下载", Loaders) With {.OnStateChanged = AddressOf DownloadStateSave}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始 Fabric 安装器下载失败", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 获取下载某个 Fabric 版本的加载器列表。
    ''' </summary>
    Private Function McDownloadFabricLoader(FabricVersion As String, MinecraftName As String, Optional McFolder As String = Nothing, Optional FixLibrary As Boolean = True) As List(Of LoaderBase)

        '参数初始化
        McFolder = If(McFolder, PathMcFolder)
        Dim IsCustomFolder As Boolean = McFolder <> PathMcFolder
        Dim Id As String = "fabric-loader-" & FabricVersion & "-" & MinecraftName
        Dim VersionFolder As String = McFolder & "versions\" & Id & "\"
        Dim Loaders As New List(Of LoaderBase)

        '下载 Json
        Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("获取 Fabric 主文件下载地址",
                                                            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                                                                '启动依赖版本的下载
                                                                If FixLibrary Then
                                                                    McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, MinecraftName)
                                                                End If
                                                                Task.Progress = 0.5
                                                                '构造文件请求
                                                                Task.Output = New List(Of NetFile) From {New NetFile({
                                                                    "https://download.mcbbs.net/fabric-meta/v2/versions/loader/" & MinecraftName & "/" & FabricVersion & "/profile/json",
                                                                    "https://meta.fabricmc.net/v2/versions/loader/" & MinecraftName & "/" & FabricVersion & "/profile/json"
                                                                }, VersionFolder & Id & ".json", New FileChecker(IsJson:=True))}
                                                                '新建 mods 文件夹
                                                                Directory.CreateDirectory(New McVersion(VersionFolder).GetPathIndie(True) & "mods\")
                                                            End Sub) With {.ProgressWeight = 0.5})
        Loaders.Add(New LoaderDownload("下载 Fabric 主文件", New List(Of NetFile)) With {.ProgressWeight = 2.5})

        '下载支持库
        If FixLibrary Then
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析 Fabric 支持库文件",
                                                                Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(VersionFolder))) With {.ProgressWeight = 1, .Show = False})
            Loaders.Add(New LoaderDownload("下载 Fabric 支持库文件", New List(Of NetFile)) With {.ProgressWeight = 8})
        End If

        Return Loaders
    End Function

#End Region

#Region "Fabric 下载菜单"

    Public Function FabricDownloadListItem(Entry As JObject, OnClick As MyListItem.ClickEventHandler) As MyListItem
        '建立控件
        Dim NewItem As New MyListItem With {
            .Title = Entry("version").ToString.Replace("+build", ""), .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 60,
            .Info = If(Entry("stable").ToObject(Of Boolean), "稳定版", "测试版"),
            .Logo = "pack://application:,,,/images/Blocks/Fabric.png"
        }
        AddHandler NewItem.Click, OnClick
        '结束
        Return NewItem
    End Function
    Public Function FabricApiDownloadListItem(Entry As DlCfFile, OnClick As MyListItem.ClickEventHandler) As MyListItem
        '建立控件
        Dim NewItem As New MyListItem With {
            .Title = Entry.DisplayName.Split("]")(1).Replace("Fabric API ", "").Replace(" build ", ".").Split("+").First.Trim, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 60,
            .Info = Entry.ReleaseTypeString & "，发布于 " & Entry.Date.ToString("yyyy/MM/dd HH:mm"),
            .Logo = "pack://application:,,,/images/Blocks/Fabric.png"
        }
        AddHandler NewItem.Click, OnClick
        '结束
        Return NewItem
    End Function
    Public Function OptiFabricDownloadListItem(Entry As DlCfFile, OnClick As MyListItem.ClickEventHandler) As MyListItem
        '建立控件
        Dim NewItem As New MyListItem With {
            .Title = Entry.DisplayName.ToLower.Replace("optifabric-", "").Replace(".jar", "").Trim.TrimStart("v"), .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .PaddingRight = 60,
            .Info = Entry.ReleaseTypeString & "，发布于 " & Entry.Date.ToString("yyyy/MM/dd HH:mm"),
            .Logo = "pack://application:,,,/images/Blocks/OptiFabric.png"
        }
        AddHandler NewItem.Click, OnClick
        '结束
        Return NewItem
    End Function

#End Region

#Region "合并安装"

    ''' <summary>
    ''' 安装请求。
    ''' </summary>
    Public Class McInstallRequest

        ''' <summary>
        ''' 必填。安装目标版本名称。
        ''' </summary>
        Public TargetVersionName As String

        ''' <summary>
        ''' 必填。欲下载的 Minecraft 的版本名。
        ''' </summary>
        Public MinecraftName As String = Nothing
        ''' <summary>
        ''' 可选。欲下载的 Minecraft Json 地址。
        ''' </summary>
        Public MinecraftJson As String = Nothing

        '若要下载 OptiFine，则需要在下面两项中完成至少一项
        ''' <summary>
        ''' 欲下载的 OptiFine 版本名。例如 HD_U_F6_pre1。
        ''' </summary>
        Public OptiFineVersion As String = Nothing
        ''' <summary>
        ''' 欲下载的 OptiFine 详细信息。
        ''' </summary>
        Public OptiFineEntry As DlOptiFineListEntry = Nothing

        '若要下载 Forge，则需要在下面两项中完成至少一项
        ''' <summary>
        ''' 欲下载的 Forge 版本名。例如 36.1.4。
        ''' </summary>
        Public ForgeVersion As String = Nothing
        ''' <summary>
        ''' 欲下载的 Forge。
        ''' </summary>
        Public ForgeEntry As DlForgeVersionEntry = Nothing

        ''' <summary>
        ''' 欲下载的 Fabric Loader 版本名。
        ''' </summary>
        Public FabricVersion As String = Nothing

        ''' <summary>
        ''' 欲下载的 Fabric API 信息。
        ''' </summary>
        Public FabricApi As DlCfFile = Nothing

        ''' <summary>
        ''' 欲下载的 OptiFabric 信息。
        ''' </summary>
        Public OptiFabric As DlCfFile = Nothing

        ''' <summary>
        ''' 欲下载的 LiteLoader 详细信息。
        ''' </summary>
        Public LiteLoaderEntry As DlLiteLoaderListEntry = Nothing

    End Class

    ''' <summary>
    ''' 安装加载器状态改变后进行提示和重载文件夹列表的方法。
    ''' </summary>
    Public Sub McInstallState(Loader)
        Select Case Loader.State
            Case LoadState.Finished
                WriteIni(PathMcFolder & "PCL.ini", "VersionCache", "") '清空缓存（合并安装会先生成文件夹，这会在刷新时误判为可以使用缓存）
                Hint(Loader.Name & "成功！", HintType.Finish)
            Case LoadState.Failed
                Hint(Loader.Name & "失败：" & GetString(Loader.Error), HintType.Critical)
            Case LoadState.Aborted
                Hint(Loader.Name & "已取消！", HintType.Info)
            Case LoadState.Loading
                Exit Sub '不重新加载版本列表
        End Select
        McInstallFailedClearFolder(Loader)
        LoaderFolderRun(McVersionListLoader, PathMcFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
    End Sub
    ''' <summary>
    ''' 安装加载器状态改变后进行提示的方法。它不会在失败时删除文件夹，也不会刷新 MC 文件夹。
    ''' </summary>
    Public Sub DownloadStateSave(Loader)
        Select Case Loader.State
            Case LoadState.Finished
                Hint(Loader.Name & "成功！", HintType.Finish)
            Case LoadState.Failed
                Hint(Loader.Name & "失败：" & GetString(Loader.Error), HintType.Critical)
            Case LoadState.Aborted
                Hint(Loader.Name & "已取消！", HintType.Info)
            Case LoadState.Loading
                Exit Sub '不重新加载版本列表
        End Select
    End Sub
    Public Sub McInstallFailedClearFolder(Loader)
        Try
            Thread.Sleep(1000) '防止存在尚未完全释放的文件，导致清理失败（例如整合包安装）
            If Loader.State = LoadState.Failed OrElse Loader.State = LoadState.Aborted Then
                '删除版本文件夹
                If Directory.Exists(Loader.Input & "saves\") OrElse Directory.Exists(Loader.Input & "versions\") Then
                    Log("[Download] 由于版本已被独立启动，不清理版本文件夹：" & Loader.Input, LogLevel.Developer)
                Else
                    Log("[Download] 由于下载失败或取消，清理版本文件夹：" & Loader.Input, LogLevel.Developer)
                    DeleteDirectory(Loader.Input)
                End If
            End If
        Catch ex As Exception
            Log(ex, "下载失败或取消后清理版本文件夹失败")
        End Try
    End Sub

    ''' <summary>
    ''' 进行合并安装。返回是否已经开始安装（例如如果没有安装 Java 则会进行提示并返回 False）
    ''' </summary>
    Public Function McInstall(Request As McInstallRequest) As Boolean
        Try
            Dim SubLoaders = McInstallLoader(Request)
            If SubLoaders Is Nothing Then Return False
            Dim Loader As New LoaderCombo(Of String)(Request.TargetVersionName & " 安装", SubLoaders) With {.OnStateChanged = AddressOf McInstallState}

            '启动
            Loader.Start(PathMcFolder & "versions\" & Request.TargetVersionName & "\")
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            Return True

        Catch ex As Exception
            Log(ex, "开始合并安装失败", LogLevel.Feedback)
            Return False
        End Try
    End Function
    ''' <summary>
    ''' 获取合并安装加载器列表，并进行前期的缓存清理与 Java 检查工作。
    ''' 如果出现已知问题且已提示用户，则返回 Nothing。出现异常则直接抛出。
    ''' </summary>
    Public Function McInstallLoader(Request As McInstallRequest, Optional DontFixLibraries As Boolean = False) As List(Of LoaderBase)
        Try
            '清理缓存
            If Not IsInstallTempCleared Then
                IsInstallTempCleared = True
                DeleteDirectory(PathTemp & "Install\", True)
                Log("[Download] 已清理合并安装缓存")
            End If
        Catch ex As Exception
            Log(ex, "清理合并安装缓存失败")
        End Try

        '获取参数
        Dim OutputFolder As String = PathMcFolder & "versions\" & Request.TargetVersionName & "\"
        Dim TempMcFolder As String = PathTemp & "Install\" & Request.TargetVersionName & "\"
        If Directory.Exists(TempMcFolder) Then DeleteDirectory(TempMcFolder)
        Dim OptiFineFolder As String = Nothing
        If Request.OptiFineVersion IsNot Nothing Then
            Request.OptiFineEntry = New DlOptiFineListEntry With {
                .NameDisplay = Request.MinecraftName & " " & Request.OptiFineVersion.Replace("HD_U_", "").Replace("_", ""),
                .Inherit = Request.MinecraftName,
                .IsPreview = Request.OptiFineVersion.ToLower.Contains("pre"),
                .NameVersion = Request.MinecraftName & "-OptiFine_" & Request.OptiFineVersion,
                .NameFile = If(Request.OptiFineVersion.ToLower.Contains("pre"), "preview_", "") &
                    "OptiFine_" & Request.MinecraftName & "_" & Request.OptiFineVersion & ".jar"
            }
        End If
        If Request.OptiFineEntry IsNot Nothing Then OptiFineFolder = TempMcFolder & "versions\" & Request.OptiFineEntry.NameVersion
        Dim ForgeFolder As String = Nothing
        If Request.ForgeEntry IsNot Nothing Then Request.ForgeVersion = If(Request.ForgeVersion, Request.ForgeEntry.Version)
        If Request.ForgeVersion IsNot Nothing Then ForgeFolder = TempMcFolder & "versions\" & Request.MinecraftName & "-forge-" & Request.ForgeVersion
        Dim FabricFolder As String = Nothing
        If Request.FabricVersion IsNot Nothing Then FabricFolder = TempMcFolder & "versions\fabric-loader-" & Request.FabricVersion & "-" & Request.MinecraftName
        Dim LiteLoaderFolder As String = Nothing
        If Request.LiteLoaderEntry IsNot Nothing Then LiteLoaderFolder = TempMcFolder & "versions\" & Request.MinecraftName & "-LiteLoader"

        'OptiFine 是否作为 Mod 进行下载
        Dim OptiFineAsMod As Boolean =
            (Request.OptiFineEntry IsNot Nothing OrElse Request.OptiFineVersion IsNot Nothing) AndAlso Request.FabricVersion IsNot Nothing
        If OptiFineAsMod Then
            Log("[Download] OptiFine 将作为 Mod 进行下载")
            OptiFineFolder = New McVersion(OutputFolder).GetPathIndie(True) & "mods\"
        End If

        '记录日志
        If OptiFineFolder IsNot Nothing Then Log("[Download] OptiFine 缓存：" & OptiFineFolder)
        If ForgeFolder IsNot Nothing Then Log("[Download] Forge 缓存：" & ForgeFolder)
        If FabricFolder IsNot Nothing Then Log("[Download] Fabric 缓存：" & FabricFolder)
        If LiteLoaderFolder IsNot Nothing Then Log("[Download] LiteLoader 缓存：" & LiteLoaderFolder)
        Log("[Download] 对应的原版版本：" & Request.MinecraftName)

        '重复版本检查
        If File.Exists(OutputFolder & Request.TargetVersionName & ".json") Then
            Hint("版本 " & Request.TargetVersionName & " 已经存在！", HintType.Critical)
            Return Nothing
        End If

        Dim LoaderList As New List(Of LoaderBase)
        'Fabric API
        If Request.FabricApi IsNot Nothing Then
            LoaderList.Add(New LoaderDownload("下载 Fabric API", New List(Of NetFile) From {Request.FabricApi.GetDownloadFile(New McVersion(OutputFolder).GetPathIndie(True) & "mods\", False)}) With {.ProgressWeight = 3, .Block = False})
        End If
        'OptiFabric
        If Request.OptiFabric IsNot Nothing Then
            LoaderList.Add(New LoaderDownload("下载 OptiFabric", New List(Of NetFile) From {Request.OptiFabric.GetDownloadFile(New McVersion(OutputFolder).GetPathIndie(True) & "mods\", False)}) With {.ProgressWeight = 3, .Block = False})
        End If
        '原版
        Dim ClientLoader = New LoaderCombo(Of String)("下载原版 " & Request.MinecraftName, McDownloadClientLoader(Request.MinecraftName, Request.MinecraftJson, Request.TargetVersionName)) With {.Show = False, .ProgressWeight = 39, .Block = Request.ForgeVersion Is Nothing AndAlso Request.OptiFineEntry Is Nothing AndAlso Request.FabricVersion Is Nothing AndAlso Request.LiteLoaderEntry Is Nothing}
        LoaderList.Add(ClientLoader)
        'OptiFine
        If Request.OptiFineEntry IsNot Nothing Then
            If OptiFineAsMod Then
                LoaderList.Add(New LoaderCombo(Of String)("下载 OptiFine " & Request.OptiFineEntry.NameDisplay, McDownloadOptiFineSaveLoader(Request.OptiFineEntry, OptiFineFolder & Request.OptiFineEntry.NameFile)) With {.Show = False, .ProgressWeight = 16, .Block = Request.ForgeVersion Is Nothing AndAlso Request.FabricVersion Is Nothing AndAlso Request.LiteLoaderEntry Is Nothing})
            Else
                LoaderList.Add(New LoaderCombo(Of String)("下载 OptiFine " & Request.OptiFineEntry.NameDisplay, McDownloadOptiFineLoader(Request.OptiFineEntry, TempMcFolder, ClientLoader, False)) With {.Show = False, .ProgressWeight = 24, .Block = Request.ForgeVersion Is Nothing AndAlso Request.FabricVersion Is Nothing AndAlso Request.LiteLoaderEntry Is Nothing})
            End If
        End If
        'Forge
        If Request.ForgeVersion IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 Forge " & Request.ForgeVersion, McDownloadForgeLoader(Request.ForgeVersion, Request.MinecraftName, Request.ForgeEntry, TempMcFolder, ClientLoader, False)) With {.Show = False, .ProgressWeight = 25, .Block = Request.FabricVersion Is Nothing AndAlso Request.LiteLoaderEntry Is Nothing})
        End If
        'LiteLoader
        If Request.LiteLoaderEntry IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 LiteLoader " & Request.MinecraftName, McDownloadLiteLoaderLoader(Request.LiteLoaderEntry, TempMcFolder, ClientLoader, False)) With {.Show = False, .ProgressWeight = 1, .Block = Request.FabricVersion Is Nothing})
        End If
        'Fabric Loader
        If Request.FabricVersion IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 Fabric " & Request.FabricVersion, McDownloadFabricLoader(Request.FabricVersion, Request.MinecraftName, TempMcFolder, False)) With {.Show = False, .ProgressWeight = 2, .Block = True})
        End If
        '合并安装
        LoaderList.Add(New LoaderTask(Of String, String)("安装游戏", Sub(Task As LoaderTask(Of String, String))
                                                                     InstallMerge(OutputFolder, OutputFolder, OptiFineFolder, OptiFineAsMod, ForgeFolder, Request.ForgeVersion, FabricFolder, LiteLoaderFolder)
                                                                     Task.Progress = 0.3
                                                                     If Directory.Exists(TempMcFolder & "libraries") Then My.Computer.FileSystem.CopyDirectory(TempMcFolder & "libraries", PathMcFolder & "libraries", True)
                                                                     If Directory.Exists(TempMcFolder & "mods") Then My.Computer.FileSystem.CopyDirectory(TempMcFolder & "mods", PathMcFolder & "mods", True)
                                                                 End Sub) With {.ProgressWeight = 2, .Block = True})
        '补全文件
        If Not DontFixLibraries AndAlso
        (Request.OptiFineEntry IsNot Nothing OrElse (Request.ForgeVersion IsNot Nothing AndAlso Request.ForgeVersion.Split(".")(0) >= 20) OrElse Request.FabricVersion IsNot Nothing OrElse Request.LiteLoaderEntry IsNot Nothing) Then
            Dim LoadersLib As New List(Of LoaderBase)
            LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析游戏支持库文件（副加载器）", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = McLibFix(New McVersion(OutputFolder))) With {.ProgressWeight = 1, .Show = False})
            LoadersLib.Add(New LoaderDownload("下载游戏支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 7, .Show = False})
            LoaderList.Add(New LoaderCombo(Of String)("下载游戏支持库文件", LoadersLib) With {.ProgressWeight = 8})
        End If
        '总加载器
        Return LoaderList
    End Function
    Private IsInstallTempCleared As Boolean = False

    ''' <summary>
    ''' 将多个版本 Json 进行合并，如果目标已存在则直接覆盖。失败会抛出异常。
    ''' </summary>
    Private Sub InstallMerge(OutputFolder As String, MinecraftFolder As String, Optional OptiFineFolder As String = Nothing, Optional OptiFineAsMod As Boolean = False, Optional ForgeFolder As String = Nothing, Optional ForgeVersion As String = Nothing, Optional FabricFolder As String = Nothing, Optional LiteLoaderFolder As String = Nothing)
        Log("[Download] 开始进行版本合并，输出：" & OutputFolder & "，Minecraft：" & MinecraftFolder &
            If(OptiFineFolder IsNot Nothing, "，OptiFine：" & OptiFineFolder, "") &
            If(ForgeFolder IsNot Nothing, "，Forge：" & ForgeFolder, "") &
            If(LiteLoaderFolder IsNot Nothing, "，LiteLoader：" & LiteLoaderFolder, "") &
            If(FabricFolder IsNot Nothing, "，Fabric：" & FabricFolder, ""))
        Directory.CreateDirectory(OutputFolder)

        Dim HasOptiFine As Boolean = OptiFineFolder IsNot Nothing AndAlso Not OptiFineAsMod, HasForge As Boolean = ForgeFolder IsNot Nothing, HasLiteLoader As Boolean = LiteLoaderFolder IsNot Nothing, HasFabric As Boolean = FabricFolder IsNot Nothing
        Dim OutputName As String, MinecraftName As String, OptiFineName As String, ForgeName As String, LiteLoaderName As String, FabricName As String
        Dim OutputJsonPath As String, MinecraftJsonPath As String, OptiFineJsonPath As String = Nothing, ForgeJsonPath As String = Nothing, LiteLoaderJsonPath As String = Nothing, FabricJsonPath As String = Nothing
        Dim OutputJar As String, MinecraftJar As String
#Region "初始化路径信息"
        If Not OutputFolder.EndsWith("\") Then OutputFolder += "\"
        OutputName = GetFolderNameFromPath(OutputFolder)
        OutputJsonPath = OutputFolder & OutputName & ".json"
        OutputJar = OutputFolder & OutputName & ".jar"

        If Not MinecraftFolder.EndsWith("\") Then MinecraftFolder += "\"
        MinecraftName = GetFolderNameFromPath(MinecraftFolder)
        MinecraftJsonPath = MinecraftFolder & MinecraftName & ".json"
        MinecraftJar = MinecraftFolder & MinecraftName & ".jar"

        If HasOptiFine Then
            If Not OptiFineFolder.EndsWith("\") Then OptiFineFolder += "\"
            OptiFineName = GetFolderNameFromPath(OptiFineFolder)
            OptiFineJsonPath = OptiFineFolder & OptiFineName & ".json"
        End If

        If HasForge Then
            If Not ForgeFolder.EndsWith("\") Then ForgeFolder += "\"
            ForgeName = GetFolderNameFromPath(ForgeFolder)
            ForgeJsonPath = ForgeFolder & ForgeName & ".json"
        End If

        If HasLiteLoader Then
            If Not LiteLoaderFolder.EndsWith("\") Then LiteLoaderFolder += "\"
            LiteLoaderName = GetFolderNameFromPath(LiteLoaderFolder)
            LiteLoaderJsonPath = LiteLoaderFolder & LiteLoaderName & ".json"
        End If

        If HasFabric Then
            If Not FabricFolder.EndsWith("\") Then FabricFolder += "\"
            FabricName = GetFolderNameFromPath(FabricFolder)
            FabricJsonPath = FabricFolder & FabricName & ".json"
        End If
#End Region

        Dim OutputJson As JObject, MinecraftJson As JObject, OptiFineJson As JObject = Nothing, ForgeJson As JObject = Nothing, LiteLoaderJson As JObject = Nothing, FabricJson As JObject = Nothing
#Region "读取文件并检查文件是否合规"
        Dim MinecraftJsonText As String = ReadFile(MinecraftJsonPath)
        If Not MinecraftJsonText.StartsWith("{") Then Throw New Exception("Minecraft Json 有误，地址：" & MinecraftJsonPath & "，前段内容：" & MinecraftJsonText.Substring(0, Math.Min(MinecraftJsonText.Length, 1000)))
        MinecraftJson = GetJson(MinecraftJsonText)

        If HasOptiFine Then
            Dim OptiFineJsonText As String = ReadFile(OptiFineJsonPath)
            If Not OptiFineJsonText.StartsWith("{") Then Throw New Exception("OptiFine Json 有误，地址：" & OptiFineJsonPath & "，前段内容：" & OptiFineJsonText.Substring(0, Math.Min(OptiFineJsonText.Length, 1000)))
            OptiFineJson = GetJson(OptiFineJsonText)
        End If

        If HasForge Then
            Dim ForgeJsonText As String = ReadFile(ForgeJsonPath)
            If Not ForgeJsonText.StartsWith("{") Then Throw New Exception("Forge Json 有误，地址：" & ForgeJsonPath & "，前段内容：" & ForgeJsonText.Substring(0, Math.Min(ForgeJsonText.Length, 1000)))
            ForgeJson = GetJson(ForgeJsonText)
        End If

        If HasLiteLoader Then
            Dim LiteLoaderJsonText As String = ReadFile(LiteLoaderJsonPath)
            If Not LiteLoaderJsonText.StartsWith("{") Then Throw New Exception("LiteLoader Json 有误，地址：" & LiteLoaderJsonPath & "，前段内容：" & LiteLoaderJsonText.Substring(0, Math.Min(LiteLoaderJsonText.Length, 1000)))
            LiteLoaderJson = GetJson(LiteLoaderJsonText)
        End If

        If HasFabric Then
            Dim FabricJsonText As String = ReadFile(FabricJsonPath)
            If Not FabricJsonText.StartsWith("{") Then Throw New Exception("Fabric Json 有误，地址：" & FabricJsonPath & "，前段内容：" & FabricJsonText.Substring(0, Math.Min(FabricJsonText.Length, 1000)))
            FabricJson = GetJson(FabricJsonText)
        End If
#End Region

#Region "处理 Json 文件"
        '获取 minecraftArguments
        Dim AllArguments As String =
            If(MinecraftJson("minecraftArguments"), " ").ToString &
            If(OptiFineJson IsNot Nothing, If(OptiFineJson("minecraftArguments"), " ").ToString, " ") &
            If(ForgeJson IsNot Nothing, If(ForgeJson("minecraftArguments"), " ").ToString, " ") &
            If(LiteLoaderJson IsNot Nothing, If(LiteLoaderJson("minecraftArguments"), " ").ToString, " ")
        Dim SplitArguments As New List(Of String)
        For Each Argument In AllArguments.Split("--")
            Argument = Argument.Trim
            If Argument <> "" Then SplitArguments.Add(Argument)
        Next
        SplitArguments = ArrayNoDouble(SplitArguments)
        Dim RealArguments As String = Nothing
        If SplitArguments.Count > 0 Then
            RealArguments = "--" & Join(SplitArguments, " --")
        End If
        '合并
        OutputJson = MinecraftJson
        If HasOptiFine Then
            '合并 OptiFine
            OptiFineJson.Remove("releaseTime")
            OptiFineJson.Remove("time")
            OutputJson.Merge(OptiFineJson)
        End If
        If HasForge Then
            '合并 Forge
            ForgeJson.Remove("releaseTime")
            ForgeJson.Remove("time")
            OutputJson.Merge(ForgeJson)
        End If
        If HasLiteLoader Then
            '合并 LiteLoader
            LiteLoaderJson.Remove("releaseTime")
            LiteLoaderJson.Remove("time")
            OutputJson.Merge(LiteLoaderJson)
        End If
        If HasFabric Then
            '合并 Fabric
            FabricJson.Remove("releaseTime")
            FabricJson.Remove("time")
            OutputJson.Merge(FabricJson)
        End If
        '修改
        If RealArguments IsNot Nothing Then OutputJson("minecraftArguments") = RealArguments
        OutputJson.Remove("_comment_")
        OutputJson.Remove("inheritsFrom")
        OutputJson.Remove("jar")
        OutputJson("id") = OutputName
#End Region

#Region "保存"
        WriteFile(OutputJsonPath, OutputJson.ToString)
        If MinecraftJar <> OutputJar Then '可能是同一个文件
            If File.Exists(OutputJar) Then File.Delete(OutputJar)
            File.Copy(MinecraftJar, OutputJar)
        End If
        Log("[Download] 版本合并 " & OutputName & " 完成")
#End Region

    End Sub

#End Region

End Module
