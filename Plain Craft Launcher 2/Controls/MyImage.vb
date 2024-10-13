Public Class MyImage
    Inherits Image

#Region "辅助属性注册"

    ''' <summary>
    ''' 网络图片的缓存有效期。
    ''' 在这个时间后，才会重新尝试下载图片。
    ''' </summary>
    Public FileCacheExpiredTime As New TimeSpan(7, 0, 0, 0) '7 天

    ''' <summary>
    ''' 是否允许将网络图片存储到本地用作缓存。
    ''' </summary>
    Public Property EnableCache As Boolean
        Get
            Return _EnableCache
        End Get
        Set(value As Boolean)
            _EnableCache = value
        End Set
    End Property
    Private _EnableCache As Boolean = True

    '将 Source 属性映射到 XAML
    Public Shared Shadows ReadOnly SourceProperty As DependencyProperty = DependencyProperty.Register(
        "Source", GetType(String), GetType(MyImage), New PropertyMetadata(New PropertyChangedCallback(
    Sub(sender, e) If sender IsNot Nothing Then CType(sender, MyImage).Source = e.NewValue.ToString())))

#End Region

    Private _Source As String = ""
    ''' <summary>
    ''' 与 Image 的 Source 类似。
    ''' 若输入以 http 开头的字符串，则会尝试下载图片然后显示，图片会保存为本地缓存。
    ''' 支持 WebP 格式的图片。
    ''' </summary>
    Public Shadows Property Source As String '覆写 Image 的 Source 属性
        Get
            Return _Source
        End Get
        Set(value As String)
            If value = "" Then value = Nothing
            If _Source = value Then Exit Property
            _Source = value
            Dim TempPath As String = $"{PathTemp}MyImage\{GetHash(value)}.png"
            Try
                '空
                If value Is Nothing Then
                    MyBase.Source = Nothing
                    Exit Property
                End If
                '本地图片
                If Not value.StartsWithF("http") Then
                    MyBase.Source = New MyBitmap(value)
                    Exit Property
                End If
                '从缓存加载网络图片
                If EnableCache AndAlso File.Exists(TempPath) Then
                    MyBase.Source = New MyBitmap(TempPath)
                    If (Date.Now - File.GetCreationTime(TempPath)) < FileCacheExpiredTime Then
                        Exit Property '无需刷新缓存
                    Else
                        File.Delete(TempPath) '需要刷新缓存
                    End If
                Else
                    MyBase.Source = Nothing '清空显示
                End If
                '下载网络图片
                RunInNewThread(
                Sub()
                    Dim Url As String = value '重新捕获变量，以检测在下载过程中 Source 被修改的情况
                    Dim Retried As Boolean = False
RetryStart:
                    Dim TempDownloadingPath As String = TempPath & RandomInteger(0, 10000000)
                    Try
                        Log("[MyImage] 正在下载图片：" & Url)
                        NetDownload(Url, TempDownloadingPath, True)
                        If Url <> Source Then
                            '若 Source 在下载时被修改，则不显示
                            File.Delete(TempDownloadingPath)
                        ElseIf EnableCache Then
                            '保存缓存并显示
                            Rename(TempDownloadingPath, TempPath)
                            RunInUi(Sub() MyBase.Source = New MyBitmap(TempPath))
                        Else
                            '直接显示
                            RunInUiWait(Sub() MyBase.Source = New MyBitmap(TempDownloadingPath))
                            File.Delete(TempDownloadingPath)
                        End If
                    Catch ex As Exception
                        Try
                            File.Delete(TempDownloadingPath)
                        Catch
                        End Try
                        If Not Retried Then
                            Log(ex, $"下载图片可重试地失败（{Url}）", LogLevel.Developer)
                            Retried = True
                            Thread.Sleep(1000)
                            GoTo RetryStart
                        Else
                            Log(ex, $"下载图片失败（{Url}）", LogLevel.Hint)
                        End If
                    End Try
                End Sub, "MyImage PicLoader " & GetUuid() & "#", ThreadPriority.BelowNormal)
            Catch ex As Exception
                Log(ex, $"加载图片失败（{value}）", LogLevel.Hint)
                Try
                    File.Delete(TempPath) '删除缓存，以免缓存出现问题导致一直加载失败
                Catch
                End Try
            End Try
        End Set
    End Property

End Class
