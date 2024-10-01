Public Class MyImage

    '自定义属性
    Public Uuid As Integer = GetUuid()
    Private _Uri As String = ""

    Public Property Source As String
        Get
            Return _Uri
        End Get
        Set(value As String)
            _Uri = value
            RefreshImage()
        End Set
    End Property '显示文本
    Public Shared ReadOnly SourceProperty As DependencyProperty = DependencyProperty.Register("Source", GetType(String), GetType(MyImage), New PropertyMetadata(New PropertyChangedCallback(
                                                                                                                                                               Sub(sender As DependencyObject, e As DependencyPropertyChangedEventArgs)
                                                                                                                                                                   If Not IsNothing(sender) Then
                                                                                                                                                                       CType(sender, MyImage)._Uri = e.NewValue.ToString()
                                                                                                                                                                       CType(sender, MyImage).RefreshImage()
                                                                                                                                                                   End If
                                                                                                                                                               End Sub)))

    Public Sub RefreshImage()
        If Me Is Nothing Then Exit Sub
        Try
            PanContent.Source = New MyBitmap(_Uri.ToString())
        Catch ex As Exception
            Log(ex, "刷新图片内容失败")
        End Try
    End Sub
End Class
