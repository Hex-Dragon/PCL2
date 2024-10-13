Public Class MyImage
    Inherits Image

    Private FileCacheExpiredTime As TimeSpan = New TimeSpan(7, 0, 0, 0) ' 一个星期的缓存有效期

    ''' <summary>
    ''' 是否使用缓存
    ''' </summary>
    ''' <returns></returns>
    Public Property UseCache As Boolean
        Get
            Return GetValue(UseCacheProperty)
        End Get
        Set(value As Boolean)
            SetValue(UseCacheProperty, value)
        End Set
    End Property
    Public Shared ReadOnly UseCacheProperty As DependencyProperty = DependencyProperty.Register("UseCache", GetType(Boolean), GetType(MyImage), New PropertyMetadata(True))

    Private _SourceData As String = ""
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
                If UseCache AndAlso File.Exists(TempFilePath) AndAlso (DateTime.Now - File.GetCreationTime(TempFilePath)) < FileCacheExpiredTime Then NeedDownload = False ' 缓存文件存在且未过期，不需要重下
                If Not NeedDownload Then
                    MyBase.Source = New MyBitmap(TempFilePath)
                    Exit Property
                End If

                If File.Exists(TempFilePath) Then '先显示着旧图片，下载新图片
                    Rename(TempFilePath, TempFilePath & ".old")
                    MyBase.Source = New MyBitmap(TempFilePath & ".old")
                End If
                ' 开一个线程下载在线图片
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
            Dim UnCompleteFile As String = TempFilePath & ".dl" '加一个下载中的后缀，防止中途关闭程序下载中断但是没下完，从而导致第二次显示的是损坏的图片……
            NetDownload(FileUrl, UnCompleteFile)
            Rename(UnCompleteFile, TempFilePath)
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
