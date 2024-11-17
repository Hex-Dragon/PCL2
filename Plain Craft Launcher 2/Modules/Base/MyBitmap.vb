'一个万能的自动图片类型转换工具类

Imports System.Drawing.Imaging

Public Class MyBitmap

    ''' <summary>
    ''' 位图缓存。
    ''' </summary>
    Public Shared BitmapCache As New Concurrent.ConcurrentDictionary(Of String, MyBitmap)

    ''' <summary>
    ''' 存储的图片
    ''' </summary>
    Public Pic As System.Drawing.Bitmap

    '自动类型转换
    '支持的类：Image，ImageSource，Bitmap，ImageBrush，BitmapSource
    Public Shared Widening Operator CType(Image As System.Drawing.Image) As MyBitmap
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As System.Drawing.Image
        Return Image.Pic
    End Operator
    Public Shared Widening Operator CType(Image As ImageSource) As MyBitmap
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As ImageSource
        Dim Bitmap = Image.Pic
        Dim rect = New System.Drawing.Rectangle(0, 0, Bitmap.Width, Bitmap.Height)
        Dim bitmapData = Bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
        Try
            Dim size = rect.Width * rect.Height * 4
            Return BitmapSource.Create(Bitmap.Width, Bitmap.Height, Bitmap.HorizontalResolution, Bitmap.VerticalResolution, PixelFormats.Bgra32, Nothing, bitmapData.Scan0, size, bitmapData.Stride)
        Finally
            Bitmap.UnlockBits(bitmapData)
        End Try
    End Operator
    Public Shared Widening Operator CType(Image As System.Drawing.Bitmap) As MyBitmap
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As System.Drawing.Bitmap
        Return Image.Pic
    End Operator
    Public Shared Widening Operator CType(Image As ImageBrush) As MyBitmap
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As ImageBrush
        Return New ImageBrush(New MyBitmap(Image.Pic))
    End Operator

    '构造函数
    Public Sub New()
    End Sub
    Public Sub New(FilePathOrResourceName As String)
        Try
            FilePathOrResourceName = FilePathOrResourceName.Replace("pack://application:,,,/images/", PathImage)
            If FilePathOrResourceName.StartsWithF(PathImage) Then
                '使用缓存
                If BitmapCache.ContainsKey(FilePathOrResourceName) Then
                    Pic = BitmapCache(FilePathOrResourceName).Pic
                Else
                    Pic = New MyBitmap(CType((New ImageSourceConverter).ConvertFromString(FilePathOrResourceName), ImageSource))
                    BitmapCache.TryAdd(FilePathOrResourceName, Pic)
                End If
            Else
                '使用这种自己接管 FileStream 的方法加载才能解除文件占用
                Using InputStream As New FileStream(FilePathOrResourceName, FileMode.Open)
                    '判断是否为 WebP 文件头
                    Dim Header(1) As Byte
                    InputStream.Read(Header, 0, 2)
                    InputStream.Seek(0, SeekOrigin.Begin)
                    If Header(0) = 82 AndAlso Header(1) = 73 Then
                        '读取 WebP
                        Dim FileBytes(InputStream.Length - 1) As Byte
                        InputStream.Read(FileBytes, 0, FileBytes.Length)
                        Pic = WebPDecoder.DecodeFromBytes(FileBytes) '将代码隔离在另外一个类中，这样只要不走进这个分支就不会加载 Imazen.WebP.dll
                    Else
                        Pic = New System.Drawing.Bitmap(InputStream)
                    End If
                End Using
            End If
        Catch ex As Exception
            Pic = My.Application.TryFindResource(FilePathOrResourceName)
            If Pic Is Nothing Then
                Pic = New System.Drawing.Bitmap(1, 1)
                Throw New Exception($"加载 MyBitmap 失败（{FilePathOrResourceName}）", ex)
            Else
                Log(ex, $"指定类型有误的 MyBitmap 加载（{FilePathOrResourceName}）", LogLevel.Developer)
                Exit Try
            End If
        End Try
    End Sub
    Public Sub New(Image As ImageSource)
        Using MS = New MemoryStream()
            Dim Encoder = New PngBitmapEncoder()
            Encoder.Frames.Add(BitmapFrame.Create(Image))
            Encoder.Save(MS)
            Pic = New System.Drawing.Bitmap(MS)
        End Using
    End Sub
    Public Sub New(Image As System.Drawing.Image)
        Pic = Image
    End Sub
    Public Sub New(Image As System.Drawing.Bitmap)
        Pic = Image
    End Sub
    Public Sub New(Image As ImageBrush)
        Using MS = New MemoryStream()
            Dim Encoder = New BmpBitmapEncoder()
            Encoder.Frames.Add(BitmapFrame.Create(Image.ImageSource))
            Encoder.Save(MS)
            Pic = New System.Drawing.Bitmap(MS)
        End Using
    End Sub
    Private Class WebPDecoder '将代码隔离在另外一个类中，这样只要不调用这个方法就不会加载 Imazen.WebP.dll
        Public Shared Function DecodeFromBytes(Bytes As Byte()) As System.Drawing.Bitmap
            If Is32BitSystem Then Throw New Exception("不支持在 32 位系统下加载 WebP 图片。")
            Dim Decoder As New Imazen.WebP.SimpleDecoder()
            Return Decoder.DecodeFromBytes(Bytes, Bytes.Length)
        End Function
    End Class

    ''' <summary>
    ''' 获取裁切的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    ''' </summary>
    Public Function Clip(X As Integer, Y As Integer, Width As Integer, Height As Integer) As MyBitmap
        Dim bmp As New System.Drawing.Bitmap(Width, Height, Pic.PixelFormat)
        bmp.SetResolution(Pic.HorizontalResolution, Pic.VerticalResolution)
        Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(bmp)
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor
            g.TranslateTransform(-X, -Y)
            g.DrawImage(Pic, New System.Drawing.Rectangle(0, 0, Pic.Width, Pic.Height))
        End Using
        Return bmp
    End Function

    ''' <summary>
    ''' 获取旋转或翻转后的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    ''' </summary>
    Public Function RotateFlip(Type As System.Drawing.RotateFlipType) As MyBitmap
        Dim bmp As New System.Drawing.Bitmap(Pic)
        bmp.SetResolution(Pic.HorizontalResolution, Pic.VerticalResolution)
        bmp.RotateFlip(Type)
        Return bmp
    End Function

    ''' <summary>
    ''' 将图像保存到文件。
    ''' </summary>
    Public Sub Save(FilePath As String)
        Dim encoder As BitmapEncoder = New PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create(Me))
        Using fileStream As New FileStream(FilePath, FileMode.Create)
            encoder.Save(fileStream)
        End Using
    End Sub

End Class
