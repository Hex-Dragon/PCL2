'一个万能的自动图片类型转换工具类

Public Class MyBitmap

    ''' <summary>
    ''' 位图缓存。
    ''' </summary>
    Public Shared BitmapCache As New Dictionary(Of String, MyBitmap)

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
        Dim bitmapData = Bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        Try
            Dim size = rect.Width * rect.Height * 4
            Return BitmapSource.Create(Bitmap.Width, Bitmap.Height, DPI, DPI, PixelFormats.Bgra32, Nothing, bitmapData.Scan0, size, bitmapData.Stride)
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
            If FilePathOrResourceName.StartsWith(PathImage) Then
                '使用缓存
                If BitmapCache.ContainsKey(FilePathOrResourceName) Then
                    Pic = BitmapCache(FilePathOrResourceName).Pic
                Else
                    Pic = New MyBitmap(CType((New ImageSourceConverter).ConvertFromString(FilePathOrResourceName), ImageSource))
                    BitmapCache.Add(FilePathOrResourceName, Pic)
                End If
            Else
                '使用这种自己接管 FileStream 的方法加载才能解除文件占用
                Using InputStream As New FileStream(FilePathOrResourceName, FileMode.Open)
                    Pic = New System.Drawing.Bitmap(InputStream)
                    InputStream.Dispose()
                End Using
            End If
        Catch ex As Exception
            Pic = My.Application.TryFindResource(FilePathOrResourceName)
            If Pic Is Nothing Then
                Pic = New System.Drawing.Bitmap(1, 1)
                Log(ex, "加载位图失败（" & FilePathOrResourceName & "）")
                Throw
            Else
                Log(ex, "指定类型有误的位图加载（" & FilePathOrResourceName & "）", LogLevel.Developer)
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

    ''' <summary>
    ''' 获取旋转的图片，这个方法不会导致原对象改变且会返回一个新的对象。图片长宽必须相等。
    ''' </summary>
    ''' <param name="angle">旋转角度（单位为角度）。</param>
    Public Function Rotation(angle As Double) As System.Drawing.Bitmap
        With Me
            Dim img As System.Drawing.Image = Me.Pic
            Dim bitSize As Single = img.Width
            Dim bmp As New System.Drawing.Bitmap(CInt(bitSize), CInt(bitSize))
            Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(bmp)
                g.TranslateTransform(bitSize / 2, bitSize / 2)
                g.RotateTransform(angle)
                g.TranslateTransform(-bitSize / 2, -bitSize / 2)
                g.DrawImage(img, New System.Drawing.Rectangle(0, 0, img.Width, img.Width))
            End Using
            Return bmp
        End With
    End Function

    ''' <summary>
    ''' 获取裁切的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    ''' </summary>
    Public Function Clip(rect As System.Drawing.Rectangle) As System.Drawing.Bitmap
        With Me
            Dim img As System.Drawing.Image = Pic
            Dim bmp As New System.Drawing.Bitmap(rect.Width, rect.Height)
            Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(bmp)
                g.DrawImageUnscaled(img, rect)
            End Using
            Return bmp
        End With
    End Function

    ''' <summary>
    ''' 获取左右翻转的图片，这个方法不会导致原对象改变。
    ''' </summary>
    Public Function LeftRightFilp() As System.Drawing.Bitmap
        Dim bmp As New System.Drawing.Bitmap(Pic)
        bmp.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX)
        Return bmp
    End Function

    ''' <summary>
    ''' 将图像保存到文件。
    ''' </summary>
    Public Sub Save(FilePath As String)
        Dim encoder As BitmapEncoder = New PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create(Me))
        Using fileStream = New System.IO.FileStream(FilePath, FileMode.Create)
            encoder.Save(fileStream)
        End Using
    End Sub

End Class
