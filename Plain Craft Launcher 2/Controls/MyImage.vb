Public Class MyImage
    Inherits Image

    '事件

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
            MyBase.Source = New MyBitmap(_SourceData)
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
