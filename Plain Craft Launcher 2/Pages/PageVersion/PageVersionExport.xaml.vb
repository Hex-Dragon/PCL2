Public Class PageVersionExport
    Private CurrentVer As String = ""
    Private Sub PageVersionExport_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Reload()
    End Sub

    Public ItemVersion As MyListItem
    Public Sub Reload()
        AniControlEnabled += 1

        If CurrentVer <> PageVersionLeft.Version.Path Then
            '说明切换到另一个版本了，所有的绝对路径都要重置，否则爆炸
            CurrentPath = ""
            Selected = New List(Of String)
            CurrentVer = PageVersionLeft.Version.Path
        End If

        TbExportName.ShowValidateResult = True
        TbExportName.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFileName() With {.AllowNull = True}}
        TbExportName.Validate()

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
        TbHint.Text = "你可以在此勾选需要包含进整合包的文件或文件夹，其他文件或文件夹在高级选项添加。" & vbCrLf & "你当前的 PCL 不是正式版，有二次分发的限制，因此整合包不能包含 PCL 程序。"
#End If
        CheckIncludeSetup.IsEnabled = CheckIncludePCL.IsEnabled
        ReloadFileList(CurrentPath)

        AniControlEnabled -= 1
    End Sub

#Region "文件列表"
    ''' <summary>
    ''' 当前所在的文件夹。注意是绝对路径！以 \ 结尾。
    ''' </summary>
    Private CurrentPath As String = ""
    Private Sub ReloadFileList(Optional NewPath As String = "")
        If String.IsNullOrWhiteSpace(NewPath) Then NewPath = PageVersionLeft.Version.Path
        Dim tb As TextBlock = PanFileList.Children(0)
        PanFileList.Children.Clear()
        PanFileList.Children.Add(tb)

        If NewPath <> PageVersionLeft.Version.Path Then
            Dim listItem As New MyListItem With {.Title = "返回上一级目录", .Tag = "..", .Height = 35, .Type = MyListItem.CheckType.Clickable}
            AddHandler listItem.Click, AddressOf ListItem_Click
            PanFileList.Children.Add(listItem)
        End If

        Dim fileCount As Integer = 0

        For Each d In Directory.EnumerateDirectories(NewPath)
            If Directory.EnumerateFileSystemEntries(d).Count = 0 Then Continue For
            d = GetFolderNameFromPath(d)
            If IsVerRedundant(NewPath & d) OrElse IsMustExport(d) Then Continue For
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
            btnSwap.Logo = Logo.IconButtonRight
            btnSwap.LogoScale = 1.0
            btnSwap.Tag = d
            AddHandler btnSwap.Click, AddressOf BtnSwap_Click
            listItem.Buttons = {btnSwap}
            listItem.Type = MyListItem.CheckType.CheckBox
            listItem.Height = 35
            listItem.Logo = Logo.IconButtonOpen
            listItem.Tag = d & "\"
            Select Case IsSelected(NewPath & d & "\")
                Case 1
                    listItem.Half = True
                Case 2
                    listItem.Checked = True
            End Select
            AddHandler listItem.Click, AddressOf ListItem_Click
            PanFileList.Children.Add(listItem)
            fileCount += 1
        Next

        For Each f In Directory.EnumerateFiles(NewPath)
            f = GetFileNameFromPath(f)
            If IsVerRedundant(NewPath & f) OrElse IsMustExport(f) Then Continue For
            Dim title As String = ""
            Dim listItem As New MyListItem
            If Titles.TryGetValue(f, title) Then 'title 作为引用类型传入
                listItem.Title = title
                listItem.Info = f
            Else
                listItem.Title = f
            End If
            listItem.Type = MyListItem.CheckType.CheckBox
            listItem.Height = 35
            listItem.Tag = f
            listItem.Checked = (IsSelected(NewPath & f) = 2)
            AddHandler listItem.Click, AddressOf ListItem_Click
            PanFileList.Children.Add(listItem)
            fileCount += 1
        Next

        CurrentPath = NewPath
        CardMore.Title = If(NewPath = PageVersionLeft.Version.Path, "高级选项", $"高级选项 ({NewPath.Replace(PageVersionLeft.Version.Path, "").TrimEnd("\")})")
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
        Dim actualPath As String = CurrentPath & sender.Tag & "\"
        'Dim relative As String = actualPath.Replace(PageVersionLeft.Version.Path, "")

        If Not Directory.Exists(actualPath) Then
            Hint($"找不到目录：{actualPath}", HintType.Critical)
            Exit Sub
        End If
        ReloadFileList(actualPath)
    End Sub
    ''' <summary>
    ''' 选中的文件列表，绝对路径。
    ''' </summary>
    Private Selected As New List(Of String)
    Private Sub SelectedChange(IsAdd As Boolean, TargetPath As String)
        If File.Exists(TargetPath) Then
            If IsAdd Then Selected.Add(TargetPath) Else Selected.Remove(TargetPath)
            Exit Sub
        End If
        If Directory.Exists(TargetPath) Then
            For Each d In Directory.EnumerateDirectories(TargetPath)
                SelectedChange(IsAdd, d)
            Next
            For Each f In Directory.EnumerateFiles(TargetPath)
                If IsAdd Then Selected.Add(f) Else Selected.Remove(f)
            Next
        End If
        Selected = Selected.Distinct().ToList()
    End Sub
    ''' <summary>
    ''' 获取 TargetPath 是否选中。
    ''' </summary>
    ''' <returns>全部未选中为 0，部分选中为 1，全部选中为 2。</returns>
    Private Function IsSelected(TargetPath As String, Optional AllowEmpty As Boolean = False) As Integer
        If File.Exists(TargetPath) Then
            Return If(Selected.Contains(TargetPath), 2, 0)
        End If
        If Directory.Exists(TargetPath) Then
            Dim di As New DirectoryInfo(TargetPath)
            If di.GetDirectories.Length + di.GetFiles.Length = 0 Then Return If(AllowEmpty, 2, 0)
            '如果有一个文件不在列表，则 hasExclude = True
            Dim hasExclude As Boolean = False
            '如果有一个文件在列表，则 hasInclude = True
            Dim hasInclude As Boolean = False
            For Each d In Directory.EnumerateDirectories(TargetPath)
                If Directory.EnumerateFileSystemEntries(d).Count = 0 Then Continue For
                Select Case IsSelected(d, True)
                    Case 0
                        hasExclude = True
                    Case 1
                        Return 1
                    Case 2
                        hasInclude = True
                End Select
                If hasInclude AndAlso hasExclude Then Return 1 '如果有文件在、有文件不在列表中，可以断定部分选中
            Next
            For Each f In Directory.EnumerateFiles(TargetPath)
                If Selected.Contains(f) Then hasInclude = True Else hasExclude = True
                If hasInclude AndAlso hasExclude Then Return 1 '如果有文件在、有文件不在列表中，可以断定部分选中
            Next
            If hasExclude AndAlso Not hasInclude Then Return 0
            If hasInclude AndAlso Not hasExclude Then Return 2
        End If
        Return 0
    End Function
    Private Sub ListItem_Click(sender As MyListItem, e As Object)
        If sender.Tag = ".." Then '回到上一级目录
            ReloadFileList(GetPathFromFullPath(CurrentPath))
            Exit Sub
        End If
        SelectedChange(Not sender.Checked, '非常难绷的逻辑，很难描述清楚，自己打断点看 Checked 属性值吧……
                       CurrentPath & sender.Tag)
    End Sub

    'Private ReadOnly Property Saves As List(Of String)
    '    Get
    '        If Directory.Exists(PageVersionLeft.Version.Path & "saves\") Then
    '            Return Directory.EnumerateDirectories(PageVersionLeft.Version.Path & "saves\").ToList
    '        End If
    '        Return New List(Of String)
    '    End Get
    'End Property

    '复选框与高级选项同步
    Private Sub PanCommonFiles_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles PanCommonFiles.MouseLeftButtonUp
        For Each c In PanCommonFiles.Children
            If TypeOf c IsNot MyCheckBox Then Continue For
            If String.IsNullOrEmpty(c.Tag) Then Continue For
            For Each l In PanFileList.Children
                If TypeOf l Is MyListItem Then
                    If l.Tag.Replace("\", "") = c.Tag Then
                        l.Checked = c.Checked
                        SelectedChange(c.Checked, PageVersionLeft.Version.Path & c.Tag)
                    End If
                End If
            Next
        Next
    End Sub
    '高级选项与复选框同步
    Private Sub PanFileList_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles PanFileList.MouseLeftButtonUp
        For Each c In PanCommonFiles.Children
            If TypeOf c Is MyCheckBox Then
                c.Checked = (IsSelected(PageVersionLeft.Version.Path & c.Tag) = 2)
            End If
        Next
    End Sub
#End Region

#Region "整合包信息 & 预览栏"
    Private Sub TbExportName_TextChanged(sender As MyTextBox, e As TextChangedEventArgs) Handles TbExportName.TextChanged
        If String.IsNullOrEmpty(TbExportName.ValidateResult) Then
            ItemVersion.Title = If(String.IsNullOrWhiteSpace(sender.Text), PageVersionLeft.Version.Name, sender.Text)
        End If
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
        Dim savePath As String = SelectAs(
            "选择导出位置", $"整合包 - {If(String.IsNullOrWhiteSpace(TbExportName.Text), PageVersionLeft.Version.Name, TbExportName.Text)}" & If(CheckIncludePCL.Checked, ".zip", ".mrpack"),
            If(CheckIncludePCL.Checked, "整合包文件(*.zip)|*.zip", "Modrinth 整合包(*.mrpack)|*.mrpack"))
        If String.IsNullOrEmpty(savePath) Then Exit Sub

        '获取需要包含的文件
        Dim contains As List(Of String) = Selected
        For Each c In PanCommonFiles.Children
            If TypeOf c IsNot MyCheckBox Then Continue For
            If String.IsNullOrEmpty(c.Tag) Then Continue For
            If c.Checked Then contains.Add(PageVersionLeft.Version.Path & c.Tag)
        Next
        ModpackExport(New ExportOptions(PageVersionLeft.Version, savePath, CheckIncludePCL.Checked, contains.Distinct.ToArray,
                                        PCLSetupGlobal:=CheckIncludeSetup.Checked, Name:=TbExportName.Text, VerID:=TbExportVersion.Text))
    End Sub

#End Region

End Class
