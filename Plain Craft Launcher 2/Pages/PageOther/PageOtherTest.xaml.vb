Imports System.Net

Public Class PageOtherTest
    Public Sub New()
        AddHandler Loaded, Sub(sender As Object, e As RoutedEventArgs)
                               MeLoaded()
                           End Sub
        InitializeComponent()
    End Sub
    Private Sub MeLoaded()
        BtnDownloadStart.IsEnabled = False

        TextDownloadFolder.Text = Setup.Get("CacheDownloadFolder")
        TextDownloadFolder.Validate()

        If Not String.IsNullOrEmpty(TextDownloadFolder.ValidateResult) OrElse String.IsNullOrEmpty(TextDownloadFolder.Text) Then
            TextDownloadFolder.Text = ModBase.Path + "PCL\MyDownload\"
        End If

        TextDownloadFolder.Validate()
        TextDownloadName.Validate()
    End Sub
    Private Sub StartButtonRefresh()
        BtnDownloadStart.IsEnabled = String.IsNullOrEmpty(TextDownloadFolder.ValidateResult) AndAlso
                                     String.IsNullOrEmpty(TextDownloadUrl.ValidateResult) AndAlso
                                     String.IsNullOrEmpty(TextDownloadName.ValidateResult)

        BtnDownloadOpen.IsEnabled = String.IsNullOrEmpty(TextDownloadFolder.ValidateResult)
    End Sub
    Private Sub SaveCacheDownloadFolder() Handles TextDownloadFolder.ValidatedTextChanged
        Setup.Set("CacheDownloadFolder", TextDownloadFolder.Text)
        TextDownloadName.Validate()
    End Sub
    Private Shared Sub DownloadState(Loader As ModLoader.LoaderCombo(Of Integer))
        Try
            Select Case Loader.State
                Case LoadState.Finished
                    Hint(Loader.Name + "完成！", ModMain.HintType.Finish, True)
                    Beep()
                Case LoadState.Failed
                    Log(Loader.Error, Loader.Name + "失败", ModBase.LogLevel.Msgbox, "出现错误")
                    Beep()
                Case LoadState.Aborted
                    Hint(Loader.Name + "已取消！", ModMain.HintType.Info, True)
            End Select
        Catch ex As Exception
        End Try
    End Sub

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Try
            If String.IsNullOrWhiteSpace(Folder) Then
                Folder = SelectAs("选择文件保存位置", FileName, Nothing, Nothing)
                If Not Folder.Contains("\") Then
                    Return
                End If
                If Folder.EndsWith(FileName) Then
                    Folder = Strings.Mid(Folder, 1, Folder.Length - FileName.Length)
                End If
            End If
            Folder = Folder.Replace("/", "\").TrimEnd(New Char() {"\"c}) + "\"
            Try
                Directory.CreateDirectory(Folder)
                CheckPermissionWithException(Folder)
            Catch ex As Exception
                Log(ex, "访问文件夹失败（" + Folder + "）", ModBase.LogLevel.Hint, "出现错误")
                Return
            End Try
            Log("[Download] 自定义下载文件名：" + FileName, LogLevel.Normal, "出现错误")
            Log("[Download] 自定义下载文件目标：" + Folder, ModBase.LogLevel.Normal, "出现错误")
            Dim uuid As Integer = GetUuid()
            Dim loaderDownload As LoaderDownload = New ModNet.LoaderDownload("自定义下载文件：" + FileName + " ", New List(Of NetFile)() From {New NetFile(New String() {Url}, Folder + FileName, Nothing, True)})
            Dim loaderCombo As LoaderCombo(Of Integer) = New LoaderCombo(Of Integer)("自定义下载 (" + uuid.ToString() + ") ", New LoaderBase() {loaderDownload}) With {.OnStateChanged = AddressOf DownloadState}
            loaderCombo.Start()
            LoaderTaskbarAdd(Of Integer)(loaderCombo)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Log(ex, "开始自定义下载失败", LogLevel.Feedback, "出现错误")
        End Try
    End Sub
    Public Shared Sub Jrrp()
        Hint("为便于维护，开源内容中不包含百宝箱功能……")
    End Sub
    Public Shared Sub RubbishClear()
        RunInUi(
            Sub()
                If Not IsNothing(FrmOtherTest) AndAlso Not IsNothing(FrmOtherTest.BtnClear) Then
                    FrmOtherTest.BtnClear.IsEnabled = False
                End If
            End Sub)
        RunInNewThread(
            Sub()
                Try
                    If Not HasRunningMinecraft Or ModLaunch.McLaunchLoader.State = LoadState.Loading Then
                        If HasDownloadingTask() Then
                            Hint("请在所有下载任务完成后再来清理吧……")
                            Return
                        End If
                        If Not McFolderList.Any() Then
                            McFolderListLoader.Start()
                        End If
                        Log(String.Format("[Test] 当前缓存文件夹：{0}，默认缓存文件夹：{1}", PathTemp, IO.Path.GetTempPath() + "PCL\"))
                        If String.Compare(PathTemp, IO.Path.GetTempPath() + "PCL\") = 0 Then
                            If Setup.Get("HintClearRubbish") <= 2 Then
                                If MyMsgBox("即将清理游戏日志、错误报告、缓存等文件。" & vbCrLf & "虽然应该没人往这些地方放重要文件，但还是问一下，是否确认继续？" & vbCrLf & vbCrLf & "在完成清理后，PCL 将自动重启。", "清理确认", "确定", "取消") = 2 Then
                                    Return
                                End If
                                Setup.Set("HintClearRubbish", Setup.Get("HintClearRubbish") + 1)
                            End If
                        ElseIf MyMsgBox("即将清理游戏日志、错误报告、缓存等文件。" & vbCrLf & vbCrLf & "你已将缓存文件夹手动修改为：" + PathTemp + vbCrLf & "清理过程中，将删除该文件夹中的所有内容，且无法恢复。请确认其中没有除了 PCL 缓存以外的重要文件！" & vbCrLf & vbCrLf & "在完成清理后，PCL 将自动重启。", "清理确认", "确定", "取消") = 2 Then
                            Return
                        End If

                        '清理的文件数量
                        Dim num As Integer = 0
                        '所有 Minecraft 文件夹
                        Dim cleanMcFolderList As List(Of DirectoryInfo) = New List(Of DirectoryInfo)()

                        If Not McFolderList.Any() Then
                            McFolderListLoader.WaitForExit()
                        End If

                        '寻找所有 Minecraft 文件夹
                        For Each mcFolder As McFolder In McFolderList
                            cleanMcFolderList.Add(New DirectoryInfo(mcFolder.Path))
                            Dim dirInfo As DirectoryInfo = New DirectoryInfo(mcFolder.Path + "versions")
                            If dirInfo.Exists Then
                                For Each item As DirectoryInfo In dirInfo.EnumerateDirectories()
                                    cleanMcFolderList.Add(item)
                                Next
                            End If
                        Next

                        '删除 Minecraft 的缓存
                        For Each dirInfo As DirectoryInfo In cleanMcFolderList
                            '删除日志和崩溃报告并计数
                            num += DeleteDirectory(dirInfo.FullName + If(dirInfo.FullName.EndsWith("\"), "", "\") + "crash-reports\", True)
                            num += DeleteDirectory(dirInfo.FullName + If(dirInfo.FullName.EndsWith("\"), "", "\") + "logs\", True)
                            For Each fileInfo As FileInfo In dirInfo.EnumerateFiles("*")
                                If fileInfo.Name.StartsWith("hs_err_pid") OrElse fileInfo.Name.EndsWith(".log") OrElse fileInfo.Name = "WailaErrorOutput.txt" Then
                                    fileInfo.Delete()
                                    num += 1
                                End If
                            Next

                            '删除 Natives 文件
                            For Each dirInfo2 As DirectoryInfo In dirInfo.EnumerateDirectories()
                                If dirInfo2.Name = dirInfo2.Name + "-natives" OrElse dirInfo2.Name = "natives-windows-x86_64" Then
                                    num += DeleteDirectory(dirInfo2.FullName, True)
                                End If
                            Next
                        Next

                        '删除 PCL 的缓存
                        num += DeleteDirectory(PathTemp, True)
                        num += DeleteDirectory(OsDrive + "ProgramData\PCL\", True)

                        MyMsgBox(String.Format("清理了 {0} 个文件！", num) + vbCrLf & "PCL 即将自动重启……", "缓存已清理", "确定", "", "", False, True, True, Nothing, Nothing, Nothing)

                        Process.Start(New ProcessStartInfo(PathWithName))
                        FormMain.EndProgramForce(Result.Success)
                    End If
                    Hint("请先关闭所有运行中的游戏……")
                Catch ex As Exception
                    Log(ex, "清理垃圾失败", LogLevel.Hint, "出现错误")
                Finally
                    RunInUiWait(
                        Sub()
                            If Not IsNothing(FrmOtherTest) AndAlso Not IsNothing(FrmOtherTest.BtnClear) Then
                                FrmOtherTest.BtnClear.IsEnabled = True
                            End If
                        End Sub)
                End Try
            End Sub, "Rubbish Clear")
    End Sub
    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If ShowHint Then Hint("为便于维护，开源内容中不包含百宝箱功能……")
    End Sub
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
    End Sub
    Public Shared Function GetRandomCave() As String
        Return "为便于维护，开源内容中不包含百宝箱功能……"
    End Function
    Public Shared Function GetRandomHint() As String
        Return "为便于维护，开源内容中不包含百宝箱功能……"
    End Function
    Public Shared Function GetRandomPresetHint() As String
        Return "为便于维护，开源内容中不包含百宝箱功能……"
    End Function

    Private Sub TextDownloadUrl_TextChanged(sender As Object, e As TextChangedEventArgs)
        Try
            If Not String.IsNullOrEmpty(TextDownloadName.Text) OrElse String.IsNullOrEmpty(TextDownloadUrl.Text) Then
                Return
            End If
            TextDownloadName.Text = GetFileNameFromPath(WebUtility.UrlDecode(TextDownloadUrl.Text))
        Catch
        End Try
    End Sub

    Private Sub MyTextButton_Click(sender As Object, e As EventArgs)
        Dim text = SelectFolder("选择文件夹")
        If Not String.IsNullOrEmpty(text) Then
            TextDownloadFolder.Text = text
        End If
    End Sub

    Private Sub BtnDownloadOpen_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim text As String = TextDownloadFolder.Text
            Directory.CreateDirectory(text)
            Process.Start(text)
        Catch ex As Exception
            Log(ex, "打开下载文件夹失败", ModBase.LogLevel.Debug, "出现错误")
        End Try
    End Sub

    Private Sub BtnDownloadStart_Click(sender As Object, e As MouseButtonEventArgs)
        StartCustomDownload(TextDownloadUrl.Text, TextDownloadName.Text, TextDownloadFolder.Text)
        TextDownloadUrl.Text = ""
        TextDownloadUrl.Validate()
        TextDownloadUrl.ForceShowAsSuccess()
        TextDownloadName.Text = ""
        TextDownloadName.Validate()
        TextDownloadName.ForceShowAsSuccess()
        StartButtonRefresh()
    End Sub

    Private Sub TextDownloadUrl_ValidateChanged(sender As Object, e As EventArgs) Handles TextDownloadUrl.ValidateChanged
        StartButtonRefresh()
    End Sub
    Private Sub TextDownloadFolder_ValidateChanged(sender As Object, e As EventArgs) Handles TextDownloadFolder.ValidateChanged
        StartButtonRefresh()
    End Sub
    Private Sub TextDownloadName_ValidateChanged(sender As Object, e As EventArgs) Handles TextDownloadName.ValidateChanged
        StartButtonRefresh()
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As MouseButtonEventArgs)
        RubbishClear()
    End Sub
End Class
