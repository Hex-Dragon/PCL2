Public Class PageVersionExport
    Private Sub PageVersionExport_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Reload()
    End Sub

    Public ItemVersion As MyListItem
    Private SelectedEntries As New List(Of String)
    Public Sub Reload()
        AniControlEnabled += 1

        TbExportName.HintText = PageVersionLeft.Version.Name
        TbExportDesc.HintText = PageVersionLeft.Version.Info
        PanDisplayItem.Children.Clear()
        ItemVersion = PageSelectRight.McVersionListItem(PageVersionLeft.Version.Load())
        ItemVersion.IsHitTestVisible = False
        If Not String.IsNullOrEmpty(CustomLogo) Then ItemVersion.Logo = CustomLogo
        PanDisplayItem.Children.Add(ItemVersion)
        ItemVersion.Title = If(String.IsNullOrWhiteSpace(TbExportName.Text), PageVersionLeft.Version.Name, TbExportName.Text)
        ItemVersion.Info = If(String.IsNullOrWhiteSpace(TbExportDesc.Text), PageVersionLeft.Version.Info, TbExportDesc.Text)
#If BETA Then
        CheckIncludePCL.IsEnabled = True
#Else
        CheckIncludePCL.IsEnabled = False
        TbHint.Text = "你可以在此勾选需要包含进整合包的文件或文件夹，其他文件或文件夹在高级选项添加。" & vbCrLf & "你当前的 PCL 不是正式版，有二次分发的限制，因此不能选择是否包含 PCL 程序。"
#End If
        CheckIncludeSetup.IsEnabled = CheckIncludePCL.IsEnabled
        ReloadFileList()

        AniControlEnabled -= 1
    End Sub
    Private Sub ReloadFileList()
        Dim tb As TextBlock = PanFileList.Children(0)
        PanFileList.Children.Clear()
        PanFileList.Children.Add(tb)

        For Each d In Directory.EnumerateDirectories(PageVersionLeft.Version.Path)
            If Directory.EnumerateFileSystemEntries(d).Count = 0 Then Continue For
            d = GetFolderNameFromPath(d)
            If IsVerRedundant(d) Then Continue For
            Dim title As String = ""
            Dim listItem As New MyListItem
            If Titles.TryGetValue(d, title) Then 'title 作为引用类型传入
                listItem.Title = title
                listItem.Info = d
            Else
                listItem.Title = d
            End If
            '图标按钮
            Dim btnSwap As New MyIconButton
            btnSwap.Path = New Shapes.Path With {.HorizontalAlignment = HorizontalAlignment.Right, .Stretch = Stretch.Uniform, .Height = 6, .Width = 10, .VerticalAlignment = VerticalAlignment.Top, .Margin = New Thickness(0, 17, 16, 0), .Data = New GeometryConverter().ConvertFromString("M2,4 l-2,2 10,10 10,-10 -2,-2 -8,8 -8,-8 z"), .RenderTransform = New RotateTransform(180), .RenderTransformOrigin = New Point(0.5, 0.5)}
            AddHandler btnSwap.Click, AddressOf BtnSwap_Click
            listItem.Buttons = {btnSwap}
            listItem.Type = MyListItem.CheckType.CheckBox
            listItem.Height = 35
            listItem.Logo = Logo.IconButtonOpen
            PanFileList.Children.Add(listItem)
        Next
    End Sub
    Private Titles As New Dictionary(Of String, String) From {
        {"saves", "存档"},
        {"resourcepacks", "资源包"},
        {"config", "Mod 配置"},
        {"shaderpacks", "光影包"},
        {"options.txt", "游戏配置"},
        {"servers.dat", "服务器列表"}
    }
    Private Sub BtnSwap_Click(sender As MyIconButton, e As Object)
        MsgBox("测试") 'TODO：处理展开操作
    End Sub
    Private ReadOnly Property Saves As List(Of String)
        Get
            If Directory.Exists(PageVersionLeft.Version.Path & "saves\") Then
                Return Directory.EnumerateDirectories(PageVersionLeft.Version.Path & "saves\").ToList
            End If
            Return New List(Of String)
        End Get
    End Property

#Region "整合包信息 & 预览栏"
    Private Sub TbExportName_TextChanged(sender As MyTextBox, e As TextChangedEventArgs) Handles TbExportName.TextChanged
        ItemVersion.Title = If(String.IsNullOrWhiteSpace(sender.Text), PageVersionLeft.Version.Name, sender.Text)
    End Sub

    Private Sub TbExportDesc_TextChanged(sender As MyTextBox, e As TextChangedEventArgs) Handles TbExportDesc.TextChanged
        ItemVersion.Info = If(String.IsNullOrWhiteSpace(sender.Text), PageVersionLeft.Version.Info, sender.Text)
    End Sub

    Private CustomLogoVal As String
    ''' <summary>
    ''' 自定义 Logo 的路径。
    ''' </summary>
    Public Property CustomLogo() As String
        Get
            Return CustomLogoVal
        End Get
        Set(value As String)
            If AniControlEnabled <> 0 Then Exit Property
            CustomLogoVal = value
            ItemVersion.Logo = value
            Reload()
        End Set
    End Property

    Private Sub ComboDisplayLogo_SelectionChanged(sender As MyComboBox, e As SelectionChangedEventArgs) Handles ComboDisplayLogo.SelectionChanged
        If Not AniControlEnabled = 0 Then Exit Sub
        '选择 自定义 时修改图片
        If ComboDisplayLogo.SelectedItem Is ItemDisplayLogoCustom Then
            Dim FileName As String = SelectFile("常用图片文件(*.png;*.jpg;*.gif)|*.png;*.jpg;*.gif", "选择图片")
            If String.IsNullOrWhiteSpace(FileName) Then Exit Sub
            CustomLogo = FileName
        Else
            CustomLogo = sender.SelectedItem.Tag
        End If
    End Sub
#End Region

#Region "基本信息"
    Private Sub CheckIncludePCL_Change(sender As Object, user As Boolean) Handles CheckIncludePCL.Change
        CheckIncludeSetup.IsEnabled = CheckIncludePCL.IsEnabled
    End Sub
#End Region

#Region "导出"
    'Private Sub ComboExportType_Change(sender As ComboBox, e As SelectionChangedEventArgs) Handles ComboExportType.SelectionChanged
    '    If AniControlEnabled <> 0 Then Exit Sub
    '    Type = sender.SelectedIndex
    '    Select Case Type
    '        Case ModpackType.Modrinth
    '            If IncludePCL() Then 'TODO：修改这里的条件
    '                If GridCompressed IsNot Nothing Then GridCompressed.Visibility = Visibility.Visible
    '                If GridModrinth IsNot Nothing Then GridModrinth.Visibility = Visibility.Collapsed
    '            Else
    '                If GridCompressed IsNot Nothing Then GridCompressed.Visibility = Visibility.Collapsed
    '                If GridModrinth IsNot Nothing Then GridModrinth.Visibility = Visibility.Visible
    '            End If
    '    End Select
    '    CardExport.TriggerForceResize()
    'End Sub
    Private Sub BtnExportExport_Click() Handles BtnExportExport.Click
        '获取需要包含的文件夹
        Dim contains As New List(Of String)
        For Each c In PanFiles.Children
            If TypeOf c IsNot MyCheckBox Then Continue For
            If String.IsNullOrEmpty(c.Tag) Then Continue For
            contains.Add(c.Tag)
        Next
        Dim savePath As String = SelectAs(
            "选择导出位置", $"整合包 - {If(String.IsNullOrWhiteSpace(TbExportName.Text), PageVersionLeft.Version.Name, TbExportName.Text)}" & If(CheckIncludePCL.Checked, ".zip", ".mrpack"),
            If(CheckIncludePCL.Checked, "整合包文件(*.zip)|*.zip", "Modrinth 整合包(*.mrpack)|*.mrpack"))
        If String.IsNullOrEmpty(savePath) Then Exit Sub
        ModpackExport(New ExportOptions(PageVersionLeft.Version, savePath, CheckIncludePCL.Checked, {}, 'TODO：改成要保留的文件列表
                                        PCLSetupGlobal:=CheckIncludeSetup.Checked, Name:=TbExportName.Text, VerID:=TbExportVersion.Text))
    End Sub

#End Region

End Class
