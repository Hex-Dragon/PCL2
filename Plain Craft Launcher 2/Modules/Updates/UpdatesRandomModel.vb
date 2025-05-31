Public Class UpdatesRandomModel '社区自己的更新系统格式
    Implements IUpdateSource

    Private _sources As IEnumerable(Of IUpdateSource)
    Public Sub New(Sources As IEnumerable(Of IUpdateSource))
        _sources = Sources
    End Sub
    Public Function IsAvailable() As Boolean Implements IUpdateSource.IsAvailable
        Return _sources IsNot Nothing AndAlso _sources.Count <> 0 AndAlso _sources.Any(Function(x) x.IsAvailable())
    End Function

    Private _curRandomSource As IUpdateSource = Nothing

    Private Function GetAvailableSources() As IEnumerable(Of IUpdateSource)
        Return _sources.Where(Function(x) x.IsAvailable())
    End Function

    Public Function EnsureLatestData() As Boolean Implements IUpdateSource.EnsureLatestData
        Dim AvailableList = GetAvailableSources()
        If AvailableList.Count() = 0 Then Throw New Exception("无法获取到任何可用的更新源。请检查配置或网络连接。")
        _curRandomSource = If(_curRandomSource, AvailableList.ElementAt(RandomInteger(0, AvailableList.Count() - 1)))
        Return _curRandomSource.EnsureLatestData()
    End Function

    Public Function GetLatestVersion(channel As UpdateChannel, arch As UpdateArch) As VersionDataModel Implements IUpdateSource.GetLatestVersion
        If _curRandomSource Is Nothing Then EnsureLatestData()
        If _curRandomSource Is Nothing Then Throw New Exception("没有可用的更新源。请检查配置或网络连接。")
        Return _curRandomSource.GetLatestVersion(channel, arch)
    End Function

    Public Function GetAnnouncementList() As AnnouncementInfoModel Implements IUpdateSource.GetAnnouncementList
        If _curRandomSource Is Nothing Then EnsureLatestData()
        If _curRandomSource Is Nothing Then Throw New Exception("没有可用的更新源。请检查配置或网络连接。")
        Return _curRandomSource.GetAnnouncementList()
    End Function

    Property SourceName As String Implements IUpdateSource.SourceName
End Class
