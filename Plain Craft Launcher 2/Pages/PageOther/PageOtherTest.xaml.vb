Imports System.ComponentModel
Imports System.Net
Imports System.Runtime.ConstrainedExecution
Imports System.Runtime.InteropServices

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
    <StructLayout(LayoutKind.Sequential)>
    Private Class TokenPrivileges
        Public PrivilegeCount As Integer = 1
        Public Luid As LUID
        Public Attributes As Integer
    End Class
    Private Structure LUID
        Public LowPart As Integer
        Public HighPart As Integer
    End Structure
    <StructLayout(LayoutKind.Sequential)>
    Public Structure SYSTEM_FILECACHE_INFORMATION
        Public CurrentSize As UIntPtr
        Public PeakSize As UIntPtr
        Public PageFaultCount As UInteger
        Public MinimumWorkingSet As UIntPtr
        Public MaximumWorkingSet As UIntPtr
        Public CurrentSizeIncludingTransitionInPages As UIntPtr
        Public PeakSizeIncludingTransitionInPages As UIntPtr
        Public TransitionRePurposeCount As UInteger
        Public Flags As UInteger
    End Structure
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MEMORY_COMBINE_INFORMATION_EX
        Public Handle As IntPtr
        Public PagesCombined As UIntPtr
        Public Flags As UInteger
    End Structure
    Private Declare Ansi Function GetCurrentProcess Lib "kernel32.dll" () As IntPtr
    <ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)>
    Private Declare Auto Function CloseHandle Lib "kernel32.dll" (handle As IntPtr) As Boolean
    Private Declare Auto Function OpenProcessToken Lib "advapi32.dll" (ProcessHandle As HandleRef, DesiredAccess As Integer, <System.Runtime.InteropServices.OutAttribute()> ByRef TokenHandle As IntPtr) As Boolean
    Private Declare Auto Function LookupPrivilegeValue Lib "advapi32.dll" (<MarshalAs(UnmanagedType.LPTStr)> lpSystemName As String, <MarshalAs(UnmanagedType.LPTStr)> lpName As String, <System.Runtime.InteropServices.OutAttribute()> ByRef lpLuid As LUID) As Boolean
    Private Declare Auto Function AdjustTokenPrivileges Lib "advapi32.dll" (TokenHandle As HandleRef, DisableAllPrivileges As Boolean, NewState As TokenPrivileges, BufferLength As Integer, PreviousState As IntPtr, ReturnLength As IntPtr) As Boolean
    Private Declare Ansi Function NtSetSystemInformation Lib "ntdll.dll" (SystemInformationClass As Integer, SystemInformation As IntPtr, SystemInformationLength As Integer) As UInteger
    Private Shared IsMemoryOptimizing
    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If IsMemoryOptimizing Then
            If ShowHint Then
                Hint("内存优化尚未结束，请稍等！", HintType.Info, True)
                Return
            End If
        Else
            IsMemoryOptimizing = True
            Dim num As Long
            If ModBase.IsAdmin() Then
                num = CLng(My.Computer.Info.AvailablePhysicalMemory)
                Try
                    MemoryOptimizeInternal(ShowHint)
                Catch ex As Exception
                    Log(ex, "内存优化失败", If(ShowHint, LogLevel.Hint, LogLevel.Debug), "出现错误")
                    Return
                Finally
                    IsMemoryOptimizing = False
                End Try
                num = Convert.ToInt64(Decimal.Subtract(New Decimal(My.Computer.Info.AvailablePhysicalMemory), New Decimal(num)))
            Else
                Log("[Test] 没有管理员权限，将以命令行方式进行内存优化")
                Try
                    num = CLng(RunAsAdmin("--memory")) * 1024L
                Catch ex2 As Exception
                    Log(ex2, "命令行形式内存优化失败")
                    If ShowHint Then
                        Hint(String.Concat(New String() {"获取管理员权限失败，请尝试右键 PCL，选择 ", vbLQ, "以管理员身份运行", vbRQ, "！"}), HintType.Critical, True)
                    End If
                    Return
                Finally
                    IsMemoryOptimizing = False
                End Try
                If num < 0L Then
                    Return
                End If
            End If
            Dim MemAfter As String = GetString(CLng(My.Computer.Info.AvailablePhysicalMemory))
            Log(String.Format("[Test] 内存优化完成，可用内存改变量：{0}，大致剩余内存：{1}", GetString(num), MemAfter))
            If num > 0L Then
                If ShowHint Then
                    Hint(String.Format("内存优化完成，可用内存增加了 {0}，目前剩余内存 {1}！", GetString(CLng(Math.Round(CDbl(num) * 0.8))), MemAfter), HintType.Finish, True)
                    Return
                End If
            ElseIf ShowHint Then
                ModMain.Hint(String.Format("内存优化完成，已经优化到了最佳状态，目前剩余内存 {0}！", MemAfter), HintType.Info, True)
            End If
        End If
    End Sub
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        If Not IsAdmin() Then
            Throw New Exception("内存优化功能需要管理员权限！" & vbCrLf & "如果需要自动以管理员身份启动 PCL，可以右键 PCL，打开 属性 → 兼容性 → 以管理员身份运行此程序。")
        End If
        Log("[Test] 获取内存优化权限")

        '提权部分
        Try
            Dim processId As IntPtr = GetCurrentProcess()
            Dim luid1 As LUID = Nothing
            Dim luid2 As LUID = Nothing
            Dim hToken As IntPtr = CType(0, IntPtr)
            If OpenProcessToken(New HandleRef(Nothing, processId), 32, hToken) Then
                LookupPrivilegeValue(Nothing, "SeProfileSingleProcessPrivilege", luid1)
                LookupPrivilegeValue(Nothing, "SeIncreaseQuotaPrivilege", luid2)

                Dim tokenPrivileges1 = New TokenPrivileges
                tokenPrivileges1.Luid = luid1
                tokenPrivileges1.Attributes = 2
                Dim tokenPrivileges2 = New TokenPrivileges
                tokenPrivileges2.Luid = luid2
                tokenPrivileges2.Attributes = 2

                AdjustTokenPrivileges(New HandleRef(Nothing, hToken), False, tokenPrivileges1, 0, IntPtr.Zero, IntPtr.Zero)
                AdjustTokenPrivileges(New HandleRef(Nothing, hToken), False, tokenPrivileges2, 0, IntPtr.Zero, IntPtr.Zero)

                CloseHandle(hToken)
            End If
        Catch ex As Exception
            Throw New Exception(String.Format("获取内存优化权限失败（错误代码：{0}）", Marshal.GetLastWin32Error()))
        End Try

        If ShowHint Then
            Hint("正在进行内存优化……", ModMain.HintType.Info, True)
        End If

        '内存优化部分
        Dim NowType As String = "None"
        Try
            Dim info As Integer
            Dim scfi As SYSTEM_FILECACHE_INFORMATION
            Dim combineInfoEx As MEMORY_COMBINE_INFORMATION_EX
            Dim _gcHandle As GCHandle

            NowType = "MemoryEmptyWorkingSets"
            info = 2
            _gcHandle = GCHandle.Alloc(info, GCHandleType.Pinned)
            NtSetSystemInformation(80, _gcHandle.AddrOfPinnedObject(), Marshal.SizeOf(info))
            _gcHandle.Free()
            NowType = "SystemFileCacheInformation"
            scfi.MaximumWorkingSet = UInteger.MaxValue
            scfi.MinimumWorkingSet = UInteger.MaxValue
            _gcHandle = GCHandle.Alloc(scfi, GCHandleType.Pinned)
            NtSetSystemInformation(81, _gcHandle.AddrOfPinnedObject(), Marshal.SizeOf(scfi))
            _gcHandle.Free()
            NowType = "MemoryFlushModifiedList"
            info = 3
            _gcHandle = GCHandle.Alloc(info, GCHandleType.Pinned)
            NtSetSystemInformation(80, _gcHandle.AddrOfPinnedObject(), Marshal.SizeOf(info))
            _gcHandle.Free()
            NowType = "MemoryPurgeStandbyList"
            info = 4
            _gcHandle = GCHandle.Alloc(info, GCHandleType.Pinned)
            NtSetSystemInformation(80, _gcHandle.AddrOfPinnedObject(), Marshal.SizeOf(info))
            _gcHandle.Free()
            NowType = "MemoryPurgeLowPriorityStandbyList"
            info = 5
            _gcHandle = GCHandle.Alloc(info, GCHandleType.Pinned)
            NtSetSystemInformation(80, _gcHandle.AddrOfPinnedObject(), Marshal.SizeOf(info))
            _gcHandle.Free()
            NowType = "SystemRegistryReconciliationInformation"
            NtSetSystemInformation(155, New IntPtr(Nothing), 0)
            NowType = "SystemCombinePhysicalMemoryInformation"
            _gcHandle = GCHandle.Alloc(combineInfoEx, GCHandleType.Pinned)
            NtSetSystemInformation(130, _gcHandle.AddrOfPinnedObject(), Marshal.SizeOf(combineInfoEx))
            _gcHandle.Free()
        Catch ex As Exception
            Throw New Exception(String.Format("内存优化操作 {0} 失败（错误代码：{1}）", NowType))
        End Try

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
    Private Sub BtnMemory_Click(sender As Object, e As MouseButtonEventArgs)
        RunInThread(Sub() MemoryOptimize(True))
    End Sub
End Class
