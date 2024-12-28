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
    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Hint("为便于维护，开源内容中不包含百宝箱功能……")
    End Sub
    Public Shared Sub Jrrp()
        Hint("为便于维护，开源内容中不包含百宝箱功能……")
    End Sub
    Public Shared Sub RubbishClear()
        Hint("为便于维护，开源内容中不包含百宝箱功能……")
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
End Class
