Class PageLoginProfile
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload() Handles Me.Loaded
        RefreshProfileList()
        FrmLoginProfileSkin = Nothing
        RunInNewThread(Sub()
                           Thread.Sleep(800)
                           RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
                       End Sub)
    End Sub
    ''' <summary>
    ''' 刷新档案列表
    ''' </summary>
    Private Sub RefreshProfileList()
        Log("[Profile] 刷新档案列表")
        StackProfile.Children.Clear()
        GetProfile()
        Try
            For Each Profile In ProfileList
                StackProfile.Children.Add(ProfileListItem(Profile, AddressOf SelectProfile))
            Next
            Log($"[Profile] 档案列表刷新完成")
        Catch ex As Exception
            Log(ex, "读取档案列表失败", LogLevel.Feedback)
        End Try
        If Not ProfileList.Any() Then
            Setup.Set("HintProfileSelect", True)
            HintCreate.Visibility = Visibility.Visible
        Else
            HintCreate.Visibility = Visibility.Collapsed
        End If
    End Sub

#Region "控件"
    Private Sub SelectProfile(sender As MyListItem, e As EventArgs)
        SelectedProfile = sender.Tag
        Log($"[Profile] 选定档案: {sender.Tag.Username}, 以 {sender.Tag.Type} 方式验证")
        LastUsedProfile = ProfileList.IndexOf(sender.Tag) '获取当前档案的序号
        RunInUi(Sub()
                    FrmLaunchLeft.RefreshPage(True)
                    FrmLaunchLeft.BtnLaunch.IsEnabled = True
                End Sub)
    End Sub
    Public Function ProfileListItem(Profile As McProfile, OnClick As MyListItem.ClickEventHandler)
        Dim LogoPath As String = PathTemp & $"Cache\Skin\Head\{Profile.SkinHeadId}.png"
        If Not (File.Exists(LogoPath) AndAlso Not New FileInfo(LogoPath).Length = 0) Then
            LogoPath = Logo.IconButtonUser
        End If
        Dim NewItem As New MyListItem With {
                .Title = Profile.Username,
                .Info = GetProfileInfo(Profile),
                .Type = MyListItem.CheckType.Clickable,
                .Logo = LogoPath,
                .Tag = Profile
        }
        AddHandler NewItem.Click, OnClick
        NewItem.ContentHandler = AddressOf ProfileContMenuBuild
        Return NewItem
    End Function
    Private Sub ProfileContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnUUID As New MyIconButton With {.Logo = Logo.IconButtonInfo, .ToolTip = "更改 UUID", .Tag = sender.Tag}
        ToolTipService.SetPlacement(BtnUUID, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnUUID, 30)
        ToolTipService.SetHorizontalOffset(BtnUUID, 2)
        AddHandler BtnUUID.Click, AddressOf EditProfile
        Dim BtnDelete As New MyIconButton With {.Logo = Logo.IconButtonDelete, .ToolTip = "删除档案", .Tag = sender.Tag}
        ToolTipService.SetPlacement(BtnDelete, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnDelete, 30)
        ToolTipService.SetHorizontalOffset(BtnDelete, 2)
        AddHandler BtnDelete.Click, AddressOf DeleteProfile
        If sender.Tag.Type = 0 Then
            sender.Buttons = {BtnUUID, BtnDelete}
        ElseIf sender.Tag.Type = 3 Then
            sender.Buttons = {BtnDelete}
        Else
            sender.Buttons = {BtnDelete}
        End If
    End Sub
    '创建档案
    Private Sub BtnNew_Click(sender As Object, e As EventArgs) Handles BtnNew.Click
        RunInNewThread(Sub()
                           CreateProfile()
                           RunInUi(Sub() RefreshProfileList())
                       End Sub)
    End Sub
    '编辑档案
    Private Sub EditProfile(sender As Object, e As EventArgs)
        EditOfflineUuid(sender.Tag)
    End Sub
    '删除档案
    Private Sub DeleteProfile(sender As Object, e As EventArgs)
        If MyMsgBox($"你正在选择删除此档案，该操作无法撤销。{vbCrLf}确定继续？", "删除档案确认", "继续", "取消", IsWarn:=True, ForceWait:=True) = 2 Then Exit Sub
        RemoveProfile(sender.Tag)
        RunInUi(Sub() RefreshProfileList())
    End Sub
    '导入 / 导出档案
    Private Sub BtnPort_Click() Handles BtnPort.Click
        MigrateProfile()
        RunInUi(Sub() RefreshProfileList())
    End Sub
#End Region

End Class
