Public Class PageVersionExport
    Public Sub BtnExportExport_Click() Handles BtnExportExport.Click
        ModpackExport(New ExportOptions(McVersionCurrent,
                                        Path & "export.zip",
                                        ModpackType.Compressed,
                                        {"新的世界", "雕塑"},
                                        PCLSetupGlobal:=False,
                                        PCLSetupVer:=True))
    End Sub
End Class
