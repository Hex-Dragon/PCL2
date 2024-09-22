Imports System.Security.Principal

Public Class PageVersionScreenshot

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
        LoadFileList()
        AniControlEnabled -= 1
    End Sub

    Private Sub RefreshUI()
        If FileList.Count.Equals(0) Then
            PanNoPic.Visibility = Visibility.Visible
            PanContent.Visibility = Visibility.Collapsed
        Else
            PanNoPic.Visibility = Visibility.Collapsed
            PanContent.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub LoadFileList()
        Log("[Screenshot] 刷新截图文件")
        FileList.Clear()
        If Directory.Exists(ScreenshotPath) Then FileList = Directory.EnumerateFiles(ScreenshotPath, "*.png", SearchOption.AllDirectories).ToList()
        PanList.Children.Clear()
        If ModeDebug Then Log("[Screenshot] 共发现 " & FileList.Count & " 个截图文件", LogLevel.Debug)
        For Each i In FileList
            Dim myCard As New MyCard With {
            .Height = Double.NaN, ' 允许高度自适应
            .Width = Double.NaN,  ' 允许宽度自适应
            .Margin = New Thickness(7),
            .Tag = i
            }
            Dim grid As New Grid
            myCard.Children.Add(grid)

            grid.RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(7)})
            grid.RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(120)})
            grid.RowDefinitions.Add(New RowDefinition)

            '图片
            Dim image As New Image
            Dim bitmapImage As New BitmapImage()
            bitmapImage.BeginInit()
            bitmapImage.UriSource = New Uri(i) ' 直接使用文件路径加载图片
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad ' 立即加载并释放文件流
            bitmapImage.EndInit()
            bitmapImage.Freeze() ' 冻结图像以提高性能
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
                .Logo = Logo.IconButtonOpen
            }
            AddHandler btnOpen.Click, AddressOf btnOpen_Click
            stackPanel.Children.Add(btnOpen)
            Dim btnDelete As New MyIconTextButton With {
                .Name = "BtnDelete",
                .Text = "删除",
                .LogoScale = 0.8,
                .Logo = Logo.IconButtonDelete
            }
            AddHandler btnDelete.Click, AddressOf btnDelete_Click
            stackPanel.Children.Add(btnDelete)
            Dim btnCopy As New MyIconTextButton With {
            .Name = "BtnCopy",
            .Text = "复制",
            .LogoScale = 0.8,
            .Logo = Logo.IconButtonCopy
            }
            AddHandler btnCopy.Click, AddressOf btnCopy_Click
            stackPanel.Children.Add(btnCopy)

            PanList.Children.Add(myCard)
        Next
        RefreshUI()
    End Sub

    Private Sub RemoveItem(Path As String)
        For Each i In PanList.Children
            If CType(i, MyCard).Tag.Equals(Path) Then
                PanList.Children.Remove(i)
                Exit For
            End If
        Next
    End Sub

    Private Function GetPathFromSender(sender As MyIconTextButton) As String
        Return CType(CType(CType(sender.Parent, StackPanel).Parent, Grid).Parent, MyCard).Tag
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
