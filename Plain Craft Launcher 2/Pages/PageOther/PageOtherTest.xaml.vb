Public Class PageOtherTest
    Public Sub New()
        AddHandler MyBase.Loaded, Sub(sender As Object, e As RoutedEventArgs)
                                      Me.MeLoaded()
                                  End Sub
        InitializeComponent()
    End Sub
    Private Sub MeLoaded()
        TextDownloadFolder.Text = Setup.Get("CacheDownloadFolder")
        TextDownloadFolder.Validate()
        If Not String.IsNullOrEmpty(TextDownloadFolder.ValidateResult) OrElse String.IsNullOrEmpty(TextDownloadFolder.Text) Then
            TextDownloadFolder.Text = ModBase.Path + "PCL\MyDownload\"
        End If
        TextDownloadFolder.Validate()
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

End Class
