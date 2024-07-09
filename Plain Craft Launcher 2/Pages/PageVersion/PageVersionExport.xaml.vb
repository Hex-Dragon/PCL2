Public Class PageVersionExport
    Public Sub BtnExportExport_Click() Handles BtnExportExport.Click
        ModpackExport(New ExportOptions With {.Version = McVersionCurrent, .Type = ModpackType.Compressed, .Dest = Path & "export.zip"})
    End Sub
End Class
