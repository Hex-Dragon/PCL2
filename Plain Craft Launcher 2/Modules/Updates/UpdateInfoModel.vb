Public Class UpdateInfo
    Public Property assets As List(Of UpdateAssetInfo)
End Class

Public Class UpdateAssetInfo
    Public Property file_name As String
    Public Property version As UpdateAssetVersionInfo
    Public Property upd_time As String
    Public Property downloads As List(Of String)
    Public Property sha256 As String
    Public Property changelog As String
End Class

Public Class UpdateAssetVersionInfo
    Public Property channel As String
    Public Property name As String
    Public Property code As Integer
End Class