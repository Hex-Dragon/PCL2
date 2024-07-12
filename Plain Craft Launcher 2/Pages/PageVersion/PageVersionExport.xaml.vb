Public Class PageVersionExport

    Private CurrentVersion As McVersion = PageVersionLeft.Version
    Private Type As ModpackType = ModpackType.Compressed
    Private SelectedSaves As New List(Of Integer)
    Private ReadOnly Property Saves As List(Of String)
        Get
            If Directory.Exists(CurrentVersion.Path & "saves\") Then
                Return Directory.EnumerateDirectories(CurrentVersion.Path & "saves\").ToList
            End If
            Return New List(Of String)
        End Get
    End Property

    Private Sub PageVersionExport_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        CurrentVersion = PageVersionLeft.Version '各个版本不同，每次都需要重新加载
        TbExportName.HintText = CurrentVersion.Name
    End Sub

#Region "控件事件"




#End Region

End Class
