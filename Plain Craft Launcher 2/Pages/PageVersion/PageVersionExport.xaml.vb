Public Class PageVersionExport
    Private Sub PageVersionExport_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TbExportName.HintText = PageVersionLeft.Version.Name
        Reload()
    End Sub

    Public ItemVersion As MyListItem
    Public Sub Reload()
        PanDisplayItem.Children.Clear()
        ItemVersion = PageSelectRight.McVersionListItem(PageVersionLeft.Version)
        ItemVersion.IsHitTestVisible = False
        PanDisplayItem.Children.Add(ItemVersion)
    End Sub

    Private Type As ModpackType = ModpackType.Modrinth
    Private SelectedSaves As New List(Of Integer)
    Private ReadOnly Property Saves As List(Of String)
        Get
            If Directory.Exists(PageVersionLeft.Version.Path & "saves\") Then
                Return Directory.EnumerateDirectories(PageVersionLeft.Version.Path & "saves\").ToList
            End If
            Return New List(Of String)
        End Get
    End Property

#Region "整合包信息 & 预览栏"
    Private Sub TbExportName_TextChanged(sender As MyTextBox, e As TextChangedEventArgs) Handles TbExportName.TextChanged
        ItemVersion.Title = If(String.IsNullOrWhiteSpace(sender.Text), PageVersionLeft.Version.Name, sender.Text)
    End Sub
#End Region

#Region "导出"
    Private Function IncludePCL() As Boolean
        Return True
    End Function
    'Private Sub ComboExportType_Change(sender As ComboBox, e As SelectionChangedEventArgs) Handles ComboExportType.SelectionChanged
    '    If AniControlEnabled <> 0 Then Exit Sub
    '    Type = sender.SelectedIndex
    '    Select Case Type
    '        Case ModpackType.Modrinth
    '            If IncludePCL() Then 'TODO：修改这里的条件
    '                If GridCompressed IsNot Nothing Then GridCompressed.Visibility = Visibility.Visible
    '                If GridModrinth IsNot Nothing Then GridModrinth.Visibility = Visibility.Collapsed
    '            Else
    '                If GridCompressed IsNot Nothing Then GridCompressed.Visibility = Visibility.Collapsed
    '                If GridModrinth IsNot Nothing Then GridModrinth.Visibility = Visibility.Visible
    '            End If
    '    End Select
    '    CardExport.TriggerForceResize()
    'End Sub
    Private Sub BtnExportExport_Click() Handles BtnExportExport.Click
        If IncludePCL() AndAlso Val(VersionBranchCode) <> 50 Then
            If MyMsgBox("你当前的 PCL 不是正式版，有二次分发（制作压缩包）的限制。" & vbCrLf &
                     "请使用正式版再次尝试该操作！" & vbCrLf &
                     "其他类型的整合包（例如 Modrinth 整合包）没有该限制。", Button1:="下载正式版", Button2:="取消") = 1 Then
                OpenWebsite("https://afdian.net/p/0164034c016c11ebafcb52540025c377")
            End If
            Exit Sub
        End If
        Dim savePath As String = SelectAs(
            "选择导出位置", $"整合包 - {If(String.IsNullOrWhiteSpace(TbExportName.Text), PageVersionLeft.Version.Name, TbExportName.Text)}" & If(Type = ModpackType.Modrinth, ".mrpack", ".zip"),
            If(Type = ModpackType.Modrinth, "Modrinth 整合包(*.mrpack)|*.mrpack", "整合包文件(*.zip)|*.zip"))
        If String.IsNullOrEmpty(savePath) Then Exit Sub
        ModpackExport(New ExportOptions(PageVersionLeft.Version, savePath, Type, {}, 'TODO：改成要保留的文件列表
                                        Name:=TbExportName.Text, VerID:=TbExportVersion.Text))
    End Sub

#End Region

End Class
