Public Class PageVersionScreenshot
    Implements IRefreshable
    Private Sub RefreshSelf() Implements IRefreshable.Refresh
        Refresh()
    End Sub
    Public Shared Sub Refresh()
        If FrmVersionScreenshot IsNot Nothing Then FrmVersionScreenshot.Reload()
        FrmVersionLeft.ItemScreenshot.Checked = True
    End Sub

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, ScreenshotLoader, AddressOf UpdateList, AutoRun:=False)
    End Sub

    Public ScreenshotLoader As New LoaderTask(Of Integer, List(Of MyCard))("Screenshot file loader", AddressOf LoadImages)
    Private Page As Integer = 0
    Private MaxPage As Integer = 1
    Private SingleLoadCount As Integer = 20

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        ScreenshotPath = PageVersionLeft.Version.PathIndie + "screenshots\"
        If Not Directory.Exists(ScreenshotPath) Then Directory.CreateDirectory(ScreenshotPath)
        Reload()


        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

    End Sub

    Dim FileList As List(Of String) = New List(Of String)
    Dim ScreenshotPath As String

    ''' <summary>
    ''' 确保当前页面上的信息已正确显示。
    ''' </summary>
    Public Sub Reload()
        AniControlEnabled += 1
        PanBack.ScrollToHome()

        '加载文件列表
        Log("[Screenshot] 刷新截图文件")
        FileList.Clear()
        If Directory.Exists(ScreenshotPath) Then FileList = Directory.EnumerateFiles(ScreenshotPath, "*", SearchOption.TopDirectoryOnly).ToList()
        Log("[Screenshot] 共发现 " & FileList.Count & " 个截图文件")
        'FileList.RemoveAll(Function(c) c.ContainsF("\debug\")) '排除资源包调试输出
        FileList.RemoveAll(Function(c)
                               If File.GetAttributes(c).HasFlag(FileAttributes.Hidden) Then Return True '排除隐藏文件
                               Dim info As New FileInfo(c)
                               If info Is Nothing Then Return True
                               If info.Length < 1024 Then Return True '小于 1 KB 可能为无效文件
                               Dim AllowedSuffix As String() = {".png", ".jpg", ".jpeg", ".bmp", ".webp", ".tiff"}
                               If Not AllowedSuffix.Contains(info.Extension.ToLower()) Then Return True '只允许指定后缀的文件
                               Return False
                           End Function)
        Log("[Screenshot] 筛选后得到 " & FileList.Count & " 个截图文件")
        RefreshTip()
        Page = 1
        MaxPage = FileList.Count / SingleLoadCount + If(FileList.Count Mod SingleLoadCount > 0, 1, 0)
        PanList.Children.Clear()
        If FileList.Count > 0 Then
            SetPageButton()
            PanContent.Visibility = Visibility.Collapsed '龙猫别删这行
            ScreenshotLoader.Start()
        End If

        AniControlEnabled -= 1
    End Sub

    Private Sub LoadImages(Loader As LoaderTask(Of Integer, List(Of MyCard)))
        Dim StartIndex = (Page - 1) * SingleLoadCount
        If StartIndex >= FileList.Count Then Exit Sub
        Dim EndIndex = Math.Min(Page * SingleLoadCount - 1, FileList.Count - 1)
        Dim res As New List(Of MyCard)
        For i = StartIndex To EndIndex
            Dim card As MyCard = Nothing
            Dim FilePath = FileList.ElementAt(i)
            Dim loadSource = FilePath
            Using fs As New FileStream(FilePath, FileMode.Open, FileAccess.Read)
                Dim Header(1) As Byte
                fs.Read(Header, 0, 2)
                fs.Seek(0, SeekOrigin.Begin)
                If Header(0) = 82 AndAlso Header(1) = 73 Then
                    'WebP 格式，需要转换
                    Dim FileBytes(fs.Length - 1) As Byte
                    fs.Read(FileBytes, 0, FileBytes.Length)
                    Dim Pic = MyBitmap.WebPDecoder.DecodeFromBytes(FileBytes)
                    Dim picTempPath = PathTemp & "Screenshot\"
                    Directory.CreateDirectory(picTempPath)
                    loadSource = picTempPath & GetHash(i) & ".png"
                    Pic.Save(loadSource)
                End If
            End Using
            Dim buildTask As New Tasks.Task(Of MyCard)(Function()
                                                           Dim out As MyCard = Nothing
                                                           RunInUiWait(Sub() out = BuildImageCard(loadSource))
                                                           Return out
                                                       End Function)
            buildTask.Start()
            buildTask.Wait()
            card = buildTask.Result
            If card IsNot Nothing Then
                res.Add(card)
            End If
        Next
        Loader.Output = res
    End Sub

    Private Sub SetPageButton()
        Dim LeftRange = 3, RightRange = 3
        Dim StartPage = Math.Max(1, Page - LeftRange)
        Dim EndPage = Math.Min(MaxPage, Page + RightRange)
        Dim BuildButton = Function(Num As Integer)
                              Dim labPage As New MyTextButton
                              labPage.Text = Num.ToString()
                              labPage.Margin = New Thickness(8, 0, 13, 0)
                              labPage.FontSize = 15
                              labPage.VerticalAlignment = VerticalAlignment.Center

                              AddHandler labPage.Click, Sub() ChangePage(Num)

                              Return labPage
                          End Function
        CardPageBtns.Children.Clear()
        For i = StartPage To EndPage
            CardPageBtns.Children.Add(BuildButton(i))
        Next
        BtnPageLeft.Opacity = If(Page = 1, 0.2, 1)
        BtnPageRight.Opacity = If(Page = MaxPage, 0.2, 1)
    End Sub

    Private Sub ChangePage(Num As Integer)
        Page = Math.Max(1, Math.Min(Num, MaxPage))
        If Page <> Num Then
            Hint("再怎么翻也没有了呀……")
            Exit Sub
        End If
        PanScroll.ScrollToTop()
        ScreenshotLoader.Start(IsForceRestart:=True)
    End Sub

    Private Sub ChangePageBtn(sender As Object, e As EventArgs) Handles BtnPageLeft.Click, BtnPageRight.Click
        If (CType(sender, MyIconButton)).Name = "BtnPageLeft" Then ChangePage(Page - 1)
        If (CType(sender, MyIconButton)).Name = "BtnPageRight" Then ChangePage(Page + 1)
    End Sub

    Private Sub RefreshTip()
        If FileList.Count.Equals(0) Then
            PanNoPic.Visibility = Visibility.Visible
            PanContent.Visibility = Visibility.Collapsed
            CardPages.Visibility = Visibility.Collapsed
        Else
            PanNoPic.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
            CardPages.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub UpdateList()
        PanList.Children.Clear()
        For Each item In ScreenshotLoader.Output
            PanList.Children.Add(item)
            item.Opacity = 0
            AniStart({AaOpacity(item, 1)})
        Next
        SetPageButton()
    End Sub

    Private Shared ImageCardCache As New Dictionary(Of String, MyCard)

    Private Function BuildImageCard(FilePath As String) As MyCard
        Try
            If Not File.Exists(FilePath) Then Return Nothing ' 文件在加载途中消失了
            If ImageCardCache.Keys.Contains(FilePath) Then Return ImageCardCache(FilePath)
            Dim myCard As New MyCard With {
            .Height = Double.NaN, ' 允许高度自适应
            .Width = Double.NaN,  ' 允许宽度自适应
            .Margin = New Thickness(7),
            .Tag = FilePath,
            .ToolTip = FilePath.Replace(ScreenshotPath, "")
            }
            Dim grid As New Grid
            myCard.Children.Add(grid)

            grid.RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(9)})
            grid.RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(120)})
            grid.RowDefinitions.Add(New RowDefinition)

            '图片
            Dim image As New Image
            Dim bitmapImage As New BitmapImage()
            Using fs As New FileStream(FilePath, FileMode.Open, FileAccess.Read)
                bitmapImage.BeginInit()
                bitmapImage.DecodePixelHeight = 200
                bitmapImage.DecodePixelWidth = 400
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad
                bitmapImage.StreamSource = fs
                bitmapImage.EndInit()
                bitmapImage.Freeze()
            End Using
            image.Source = bitmapImage
            image.Stretch = Stretch.Uniform ' 使图片自适应控件大小
            Grid.SetRow(image, 1)
            grid.Children.Add(image)

            '按钮
            Dim stackPanel As New StackPanel
            stackPanel.Orientation = Orientation.Horizontal
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center
            stackPanel.Margin = New Thickness(3, 5, 3, 5)
            Grid.SetRow(stackPanel, 2)
            grid.Children.Add(stackPanel)

            Dim btnOpen As New MyIconTextButton With {
            .Name = "BtnOpen",
            .Text = "打开",
            .LogoScale = 0.8,
            .Logo = Logo.IconButtonOpen,
            .Tag = FilePath
            }
            AddHandler btnOpen.Click, AddressOf btnOpen_Click
            stackPanel.Children.Add(btnOpen)
            Dim btnDelete As New MyIconTextButton With {
            .Name = "BtnDelete",
            .Text = "删除",
            .LogoScale = 0.8,
            .Logo = Logo.IconButtonDelete,
            .Tag = FilePath
            }
            AddHandler btnDelete.Click, AddressOf btnDelete_Click
            stackPanel.Children.Add(btnDelete)
            Dim btnCopy As New MyIconTextButton With {
            .Name = "BtnCopy",
            .Text = "复制",
            .LogoScale = 0.8,
            .Logo = Logo.IconButtonCopy,
                .Tag = FilePath
            }
            AddHandler btnCopy.Click, AddressOf btnCopy_Click
            stackPanel.Children.Add(btnCopy)
            ImageCardCache.Add(FilePath, myCard)
            Return myCard
        Catch ex As Exception
            Log(ex, $"[Screenshot] 加载图片 {FilePath} 失败", LogLevel.Hint)
        End Try
        Return Nothing
    End Function

    Private Sub RemoveItem(Path As String)
        Try
            For Each i In PanList.Children
                If CType(i, MyCard).Tag.Equals(Path) Then
                    PanList.Children.Remove(i)
                    Exit For
                End If
            Next
        Catch ex As Exception
            Log(ex, "[Screenshot] 未能找到对应 UI")
        End Try
    End Sub

    Private Function GetPathFromSender(sender As MyIconTextButton) As String
        Return sender.Tag
    End Function

    Private Sub btnOpen_Click(sender As MyIconTextButton, e As EventArgs)
        OpenExplorer("""" & GetPathFromSender(sender) & """")
    End Sub
    Private Sub btnDelete_Click(sender As MyIconTextButton, e As EventArgs)
        Path = GetPathFromSender(sender)
        RemoveItem(Path)
        Try
            My.Computer.FileSystem.DeleteFile(Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
            Hint("已将截图移至回收站！")
        Catch ex As Exception
            Log(ex, "删除截图失败！", LogLevel.Hint)
        End Try
    End Sub
    Private Sub btnCopy_Click(sender As MyIconTextButton, e As EventArgs)
        Dim imagePath As String = GetPathFromSender(sender)
        If File.Exists(imagePath) Then
            Dim TryTime = 0
            While TryTime <= 5
                Try
                    Log("[Screenshot] 尝试复制" & imagePath & "到剪贴板")
                    Clipboard.SetImage(New BitmapImage(New Uri(imagePath)))
                    Hint("已复制截图到剪贴板！")
                    TryTime = 6
                    Exit Sub
                Catch ex As Exception
                    TryTime += 1
                    Log(ex, $"[Screenshot]第 {TryTime} 次复制尝试失败")
                End Try
            End While
            Hint("截图复制失败！", HintType.Critical)
        Else
            Hint("截图文件不存在！")
        End If
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Directory.Exists(ScreenshotPath) Then Directory.CreateDirectory(ScreenshotPath)
        OpenExplorer("""" & ScreenshotPath & """")
    End Sub
End Class
