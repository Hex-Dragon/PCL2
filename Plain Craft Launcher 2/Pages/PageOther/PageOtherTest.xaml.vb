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
        Hint("尚未制作……")
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
