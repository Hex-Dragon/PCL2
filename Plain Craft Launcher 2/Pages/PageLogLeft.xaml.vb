Public Class PageLogLeft
    Public ShownLogs As List(Of Process)
    Private Sub PageLogLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        LogListUI()
    End Sub
    Private Sub LogListUI()
        Try
            'TODO：完成日志显示
        Catch ex As Exception
            'TODO：完成错误处理
        End Try
    End Sub
    Public Sub AddProcess(proc As Process)
        ShownLogs.Add(proc)
        LogListUI()
    End Sub
End Class
