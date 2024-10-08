Imports System.Threading.Tasks

Public Class MyImage
    Inherits Image

    '事件

    Private _SourceData As String = ""
    Private _UseCache As Boolean = False
    Private _DownloadTask As Task

    Private FileCacheExpiredTime As TimeSpan = New TimeSpan(7, 0, 0, 0) ' 一个星期的缓存有效期

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
                If _DownloadTask IsNot Nothing AndAlso Not _DownloadTask.IsCompleted Then ' 之前下载任务还在，直接砍了
                    _DownloadTask.Dispose()
                End If
                Dim NeedDownload As Boolean = True '是否需要下载/本地是否有有效缓存
                Dim TempFilePath As String = PathTemp & "Cache\MyImage\" & GetHash(_SourceData) & ".png"
                If _UseCache And File.Exists(TempFilePath) And (DateTime.Now - File.GetCreationTime(TempFilePath)) < FileCacheExpiredTime Then NeedDownload = False ' 缓存文件存在且未过期，不需要重下
                If Not NeedDownload Then
                    MyBase.Source = New MyBitmap(TempFilePath)
                    Exit Property
                End If
                ' 异步下载图片
                Dim TaskID = 0
                _DownloadTask = New Task(Sub()
                                             NetDownload(_SourceData, TempFilePath, True)
                                         End Sub)
                TaskID = _DownloadTask.Id
                _DownloadTask.Start()
                _DownloadTask.ContinueWith(Sub(t)
                                               If t.IsCompleted AndAlso t.Id = TaskID Then ' 任务没有被干掉
                                                   MyBase.Source = New MyBitmap(TempFilePath)
                                               End If
                                           End Sub, TaskScheduler.FromCurrentSynchronizationContext())
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

End Class
