Public Class UpdatesMinioModel '社区自己的更新系统格式
    Implements IUpdateSource

    Private _baseUrl As String
    Public Sub New(BaseUrl As String, Optional Name As String = "Minio")
        _baseUrl = BaseUrl
        SourceName = Name
    End Sub
    Public Function IsAvailable() As Boolean Implements IUpdateSource.IsAvailable
        Return Not String.IsNullOrWhiteSpace(_baseUrl)
    End Function

    Private _remoteVersionData As UpdateInfo = Nothing
    Private _remoteAnnounceData As AnnouncementInfoModel = Nothing

    Public Function EnsureLatestData() As Boolean Implements IUpdateSource.EnsureLatestData
        '先检查缓存
        Dim RemoteCache As JObject = NetGetCodeByRequestRetry($"{_baseUrl}api/cache.json", IsJson:=True)
        Dim UpdatesCacheFile = $"{PathTemp}Cache/upd_{SourceName}_updates.json"
        Dim AnnouncementCacheFile = $"{PathTemp}Cache/upd_{SourceName}_announcement.json"
        Dim HasChange = False
        If GetFileMD5(UpdatesCacheFile) <> RemoteCache("updates") Then
            WriteFile(UpdatesCacheFile, NetGetCodeByRequestRetry($"{_baseUrl}api/updates.json"))
            HasChange = True
        End If
        If GetFileMD5(AnnouncementCacheFile) <> RemoteCache("announcement") Then
            WriteFile(AnnouncementCacheFile, NetGetCodeByRequestRetry($"{_baseUrl}api/announcement.json"))
            HasChange = True
        End If
        _remoteVersionData = CType(GetJson(ReadFile(UpdatesCacheFile)), JObject).ToObject(Of UpdateInfo)()
        _remoteAnnounceData = CType(GetJson(ReadFile(AnnouncementCacheFile)), JObject).ToObject(Of AnnouncementInfoModel)()
        Return HasChange
    End Function

    Public Function GetLatestVersion(channel As UpdateChannel, arch As UpdateArch) As VersionDataModel Implements IUpdateSource.GetLatestVersion
        If _remoteVersionData Is Nothing Then EnsureLatestData()
        '确定版本通道名称
        Dim ChannelName As String = String.Empty
        ChannelName += If(channel = UpdateChannel.stable, "sr", "fr")
        ChannelName += arch.ToString()
        Dim targetData = _remoteVersionData.assets.Where(Function(x) x.version.channel = ChannelName).First()
        Return New VersionDataModel() With {
            .Source = SourceName,
            .IsArchive = True,
            .download_url = targetData.downloads,
            .sha256 = targetData.sha256,
            .version_code = targetData.version.code,
            .version_name = targetData.version.name,
            .Desc = targetData.changelog}
    End Function

    Public Function GetAnnouncementList() As AnnouncementInfoModel Implements IUpdateSource.GetAnnouncementList
        If _remoteAnnounceData Is Nothing Then EnsureLatestData()
        Return _remoteAnnounceData
    End Function

    Property SourceName As String Implements IUpdateSource.SourceName
End Class
