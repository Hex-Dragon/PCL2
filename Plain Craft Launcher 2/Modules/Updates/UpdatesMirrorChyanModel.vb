Public Class UpdatesMirrorChyanModel 'Mirror 酱的更新格式
    Implements IUpdateSource
    Private Const MirrorChyanBaseUrl As String = "https://mirrorchyan.com/api/resources/{cid}/latest?cdk={cdk}&os=win&arch={arch}&channel={channel}"
    Private Const MyCid As String = "PCL2-CE"

    Public Function IsAvailable() As Boolean Implements IUpdateSource.IsAvailable
        Return Not String.IsNullOrWhiteSpace(Setup.Get("SystemMirrorChyanKey"))
    End Function


    Public Function EnsureLatestData() As Boolean Implements IUpdateSource.EnsureLatestData
        If Not IsAvailable() Then Throw New Exception("没有指定 CDK 无法获取更新信息……")
        Log("[System] 由于 MirrorChyan API 的时效性，将在获取最新版本的时候获取最新数据", LogLevel.Debug)
        Return True
    End Function

    ''' <summary>
    ''' 在调用此函数之前请先调用 <see cref="EnsureLatestData()"/>
    ''' </summary>
    ''' <param name="channel"></param>
    ''' <param name="arch"></param>
    ''' <returns></returns>
    Public Function GetLatestVersion(channel As UpdateChannel, arch As UpdateArch) As VersionDataModel Implements IUpdateSource.GetLatestVersion
        Dim ReqUrl As String = MirrorChyanBaseUrl
        Dim CDKey As String = Setup.Get("SystemMirrorChyanKey")
        ReqUrl = ReqUrl.Replace("{cid}", MyCid)
        ReqUrl = ReqUrl.Replace("{cdk}", CDKey)
        ReqUrl = ReqUrl.Replace("{arch}", arch.ToString())
        ReqUrl = ReqUrl.Replace("{channel}", channel.ToString())
        Dim ret As JObject = NetGetCodeByRequestRetry(ReqUrl, IsJson:=True)
        If CType(ret("code"), Integer) <> 0 Then Throw New Exception("Mirror 酱获取数据不成功")
        Dim data = ret("data")
        Dim upd_url = data("url")?.ToString()
        If data IsNot Nothing AndAlso String.IsNullOrWhiteSpace(upd_url) Then Throw New Exception("无效 CDK")
        Return New VersionDataModel() With {
            .Source = SourceName,
            .IsArchive = False,
            .download_url = New List(Of String) From {upd_url},
            .sha256 = data("sha256")?.ToString(),
            .version_code = data("version_number"),
            .version_name = data("version_name"),
            .Desc = data("release_note")}
    End Function

    Public Function GetAnnouncementList() As AnnouncementInfoModel Implements IUpdateSource.GetAnnouncementList
        Throw New Exception("不支持")
    End Function

    Property SourceName As String = "MirrorChyan" Implements IUpdateSource.SourceName
End Class
