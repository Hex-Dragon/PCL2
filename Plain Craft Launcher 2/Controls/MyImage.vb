Imports System.Threading.Tasks

Public Class MyImage
    Inherits Image

    '事件

    Private _SourceData As String = ""
    Private _UseCache As Boolean = False

    Private FileCacheExpiredTime As TimeSpan = New TimeSpan(7, 0, 0, 0) ' 一个星期的缓存有效期

    ''' <summary>
    ''' 是否使用缓存，需要先于 Source 属性设置，否则无效
    ''' </summary>
    ''' <returns></returns>
    Public Property UseCache As Boolean
        Get
            Return _UseCache
        End Get
        Set(value As Boolean)
            _UseCache = value
        End Set
    End Property

    ''' <summary>
    ''' 重写Image的Source属性
    ''' </summary>
    Public Shadows Property Source As String
        Get
            Return _SourceData
        End Get
        Set(value As String)
            If String.IsNullOrEmpty(value) Then Exit Property
            _SourceData = value
            Try
                If Not value.StartsWithF("http") Then ' 本地资源直接使用
                    MyBase.Source = New MyBitmap(_SourceData)
                    Exit Property
                End If
                Dim NeedDownload As Boolean = True '是否需要下载/本地是否有有效缓存
                Dim TempFilePath As String = PathTemp & "Cache\MyImage\" & GetHash(_SourceData) & ".png"
                If _UseCache And File.Exists(TempFilePath) And (DateTime.Now - File.GetCreationTime(TempFilePath)) < FileCacheExpiredTime Then NeedDownload = False ' 缓存文件存在且未过期，不需要重下
                If Not NeedDownload Then
                    MyBase.Source = New MyBitmap(TempFilePath)
                    Exit Property
                End If
                ' 开一个线程处理在线图片
                RunInNewThread(Sub() PicLoader(_SourceData, TempFilePath), "MyImage PicLoader " & GetUuid() & "#", ThreadPriority.BelowNormal)

            Catch ex As Exception
                Log(ex, "加载图片失败")
            End Try
        End Set
    End Property
    Public Shared Shadows ReadOnly SourceProperty As DependencyProperty = DependencyProperty.Register("Source", GetType(String), GetType(MyImage), New PropertyMetadata(New PropertyChangedCallback(
                                                                                                                                                               Sub(sender As DependencyObject, e As DependencyPropertyChangedEventArgs)
                                                                                                                                                                   If Not IsNothing(sender) Then
                                                                                                                                                                       If String.IsNullOrEmpty(e.NewValue.ToString()) Then Exit Sub
                                                                                                                                                                       CType(sender, MyImage).Source = e.NewValue.ToString()
                                                                                                                                                                   End If
                                                                                                                                                               End Sub)))

    Private Sub PicLoader(FileUrl As String, TempFilePath As String)
        Dim Retried As Boolean = False
RetryStart:
        Try
            If File.Exists(TempFilePath) Then '先显示着旧图片，下载新图片
                Rename(TempFilePath, TempFilePath & ".old")
                RunInUi(Sub()
                            MyBase.Source = New MyBitmap(TempFilePath & ".old")
                        End Sub)
            End If
            NetDownload(FileUrl, TempFilePath)
            RunInUi(Sub()
                        MyBase.Source = New MyBitmap(TempFilePath)
                    End Sub)
            File.Delete(TempFilePath & ".old")
        Catch ex As Exception
            If Not Retried Then
                Retried = True
                GoTo RetryStart
            Else
                Log(ex, $"[MyImage] 下载图片失败")
            End If
        End Try
    End Sub

End Class
