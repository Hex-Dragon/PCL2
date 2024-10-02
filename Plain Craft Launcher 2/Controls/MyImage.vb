Public Class MyImage
    Inherits Image

    '事件

    Public Uuid As Integer = GetUuid()

    Private _SourceData As String = ""

    ''' <summary>
    ''' 重写Image的Source属性
    ''' </summary>
    Public Shadows Property Source As String
        Get
            Return _SourceData
        End Get
        Set(value As String)
            SetImage(value)
        End Set
    End Property
    Public Shared Shadows ReadOnly SourceProperty As DependencyProperty = DependencyProperty.Register("Source", GetType(String), GetType(MyImage), New PropertyMetadata(New PropertyChangedCallback(
                                                                                                                                                               Sub(sender As DependencyObject, e As DependencyPropertyChangedEventArgs)
                                                                                                                                                                   If Not IsNothing(sender) Then
                                                                                                                                                                       CType(sender, MyImage).SetImage(e.NewValue)
                                                                                                                                                                   End If
                                                                                                                                                               End Sub)))

    Private Sub SetImage(source As String)
        If Me Is Nothing Then Exit Sub
        If String.IsNullOrEmpty(source) Then Exit Sub
        _SourceData = source
        MyBase.Source = New MyBitmap(_SourceData)
    End Sub
End Class
