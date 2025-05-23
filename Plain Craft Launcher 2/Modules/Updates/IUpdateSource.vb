Public Interface IUpdateSource
    ''' <summary>
    ''' 是否可用，根据本地情况判断
    ''' </summary>
    ''' <returns></returns>
    Function IsAvailable() As Boolean
    ''' <summary>
    ''' 确保最新版本
    ''' </summary>
    ''' <returns>True 表示更新成功，False 表示没有数据更新</returns>
    Function EnsureLatestData() As Boolean
    Function GetLatestVersion(channel As UpdateChannel, arch As UpdateArch) As VersionDataModel
    Function GetAnnouncementList() As AnnouncementInfoModel
    Property SourceName As String
End Interface
