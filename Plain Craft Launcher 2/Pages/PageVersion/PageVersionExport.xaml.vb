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
    Private Sub ComboExportType_Change(sender As ComboBox, e As SelectionChangedEventArgs) Handles ComboExportType.SelectionChanged
        If AniControlEnabled <> 0 Then Exit Sub
        Type = If(sender.SelectedIndex = 0, ModpackType.Compressed, ModpackType.Modrinth)
        Select Case Type
            Case ModpackType.Modrinth
                If GridCompressed IsNot Nothing Then GridCompressed.Visibility = Visibility.Collapsed
                If GridModrinth IsNot Nothing Then GridModrinth.Visibility = Visibility.Visible
            Case ModpackType.Compressed
                If GridCompressed IsNot Nothing Then GridCompressed.Visibility = Visibility.Visible
                If GridModrinth IsNot Nothing Then GridModrinth.Visibility = Visibility.Collapsed
        End Select
        CardExport.TriggerForceResize()
    End Sub

    Private Sub BtnExportExport_Click() Handles BtnExportExport.Click
        If Type = ModpackType.Compressed AndAlso Val(VersionBranchCode) <> 50 Then
            If MyMsgBox("你当前的 PCL 不是正式版，且受到再次分发（制作压缩包）的限制。" & vbCrLf &
                     "请使用正式版再次进行该操作再试！" & vbCrLf &
                     "其他类型的整合包（例如 Modrinth 整合包）没有该限制。", Button1:="下载正式版", Button2:="取消") = 1 Then
                OpenWebsite("https://afdian.net/p/0164034c016c11ebafcb52540025c377")
            End If
            Exit Sub
        End If
        Dim savePath As String = SelectAs(
            "选择导出位置", $"整合包 - {If(String.IsNullOrWhiteSpace(TbExportName.Text), CurrentVersion.Name, TbExportName.Text)}" & If(Type = ModpackType.Modrinth, ".mrpack", ".zip"),
            If(Type = ModpackType.Modrinth, "Modrinth 整合包(*.mrpack)|*.mrpack", "整合包文件(*.zip)|*.zip"))
        If String.IsNullOrEmpty(savePath) Then Exit Sub
        ModpackExport(New ExportOptions(CurrentVersion, savePath, Type, {TbExportSave.Text},
                                        PCLSetupGlobal:=CheckExportGlobal.Checked,
                                        PCLSetupVer:=CheckExportVersion.Checked, Name:=TbExportName.Text,
                                        VerID:=TbExportVersion.Text))
    End Sub

#End Region

End Class
