Public Class PageVersionExport
    Public Sub BtnExportExport_Click() Handles BtnExportExport.Click
        Dim Type As ModpackType = ModpackType.Modrinth
        If Type = ModpackType.Compressed AndAlso Val(VersionBranchCode) <> 50 Then
            MyMsgBox("你当前的 PCL 不是正式版，且受到再次分发（制作压缩包）的限制。" & vbCrLf &
                     "请使用正式版再次进行该操作再试！" & vbCrLf &
                     "其他类型的整合包（例如 Modrinth 整合包）没有该限制。")
            Exit Sub
        End If
        RunInNewThread(Sub()
                           ModpackExport(New ExportOptions(McVersionCurrent, Path & "exportmr.zip", Type, {"新的世界", "雕塑"},
                                                           PCLSetupGlobal:=False, PCLSetupVer:=True, VerID:="1.2.3"))
                       End Sub, "Modpack Export")
    End Sub
End Class
