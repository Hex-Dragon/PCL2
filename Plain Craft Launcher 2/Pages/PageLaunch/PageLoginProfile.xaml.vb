Imports System.Security.Cryptography

Class PageLoginProfile
    Private IsReloaded As Boolean = False
    Public Shared IsProfileSelected As Boolean = False
    Public Shared IsProfileCreating As Boolean = False
    Private IsFirstLoad As Boolean = True
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        RefreshProfileList()
        RunInNewThread(Sub()
                           Thread.Sleep(800)
                           RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
                       End Sub)
        IsReloaded = True
    End Sub
    ''' <summary>
    ''' 当前选定的档案
    ''' </summary>
    Public Shared SelectedProfile As JObject = Nothing
    ''' <summary>
    ''' 上次选定的档案
    ''' </summary>
    Public Shared LastUsedProfile As Integer = Nothing
    ''' <summary>
    ''' 档案列表
    ''' </summary>
    Public Shared ProfileList As New JArray
    ''' <summary>
    ''' 刷新档案列表
    ''' </summary>
    Private Sub RefreshProfileList()
        Log("[Profile] 刷新档案列表")
        StackProfile.Children.Clear()
        Try
            If Not File.Exists(PathAppdataConfig & "Profiles.json") Then
                File.Create(PathAppdataConfig & "Profiles.json")
                WriteFile(PathAppdataConfig & "Profiles.json", "{""lastUsed"":0,""profiles"":[]}", False) '创建档案列表文件
            End If
            Dim ProfileJobj As JObject = JObject.Parse(ReadFile(PathAppdataConfig & "Profiles.json"))
            LastUsedProfile = ProfileJobj("lastUsed")
            ProfileList = ProfileJobj("profiles")
            For Each Profile In ProfileList
                If Profile.ToString Is Nothing OrElse Profile.ToString = "" Then '兜底
                    ProfileList.Remove(Profile)
                    WriteProfileJson()
                    Continue For
                End If
                StackProfile.Children.Add(ProfileListItem(Profile, AddressOf SelectProfile))
            Next
            If IsFirstLoad Then
                SelectedProfile = ProfileList(LastUsedProfile)
                IsFirstLoad = False
            End If
        Catch ex As Exception
            Log(ex, "读取档案列表失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub SelectProfile(sender As MyListItem, e As EventArgs)
        SelectedProfile = sender.Tag
        Log($"[Profile] 选定档案: {sender.Tag("username")}, 以 {sender.Tag("type")} 方式验证")
        Select Case SelectedProfile("type").ToString
            Case "offline"
                Setup.Set("LoginType", McLoginType.Legacy)
            Case "microsoft"
                Setup.Set("LoginType", McLoginType.Ms)
            Case "authlib"
                Setup.Set("LoginType", McLoginType.Auth)
        End Select
        LastUsedProfile = ProfileList.IndexOf(sender.Tag) '获取当前档案的序号
        IsProfileSelected = True
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
    End Sub
    Private Sub BtnNew_Click(sender As Object, e As EventArgs) Handles BtnNew.Click
        Dim AuthTypeList As New List(Of IMyRadio) From {
            New MyRadioBox With {.Text = "离线验证", .Tag = "offline"},
            New MyRadioBox With {.Text = "正版验证", .Tag = "microsoft"},
            New MyRadioBox With {.Text = "第三方验证", .Tag = "authlib"}
        }
        Dim NewProfile As JObject = Nothing
        Dim SelectedAuthType As String = Nothing '验证类型
        Dim SelectedAuthTypeNum As Integer = Nothing '验证类型序号
        Dim UserName As String = Nothing '玩家 ID
        Dim Uuid As String = Nothing 'UUID
        Dim AuthName As String = Nothing '验证使用的用户名（离线验证为空）
        Dim AuthPassword As String = Nothing '验证使用的密码（离线验证为空）
        RunInUiWait(Sub() SelectedAuthTypeNum = MyMsgBoxSelect(AuthTypeList, "新建档案 - 选择验证类型", "继续", "取消") + 1)
        If SelectedAuthTypeNum = Nothing Then Exit Sub
        If SelectedAuthTypeNum = 1 Then '离线验证
            SelectedAuthType = "offline"
            UserName = MyMsgBoxInput("新建档案 - 输入档案名称", HintText:="只可以使用英文字母、数字与下划线", Button1:="继续", Button2:="取消")
            If UserName = Nothing Then Exit Sub
            Uuid = "a295ef70c3d64e65a0deca0d9b9b1ca7"
            'Uuid = Function()
            '           Dim UuidBukkit As String = Guid.NewGuid.ToString
            '           Dim MD5 As MD5 = MD5.Create
            '           Dim Hash As Byte() = MD5.ComputeHash(Encoding.UTF8.GetBytes(UserName))
            '           Hash(6) = CByte(((Hash(6) & "0x0f") Or "0x30"))
            '           Hash(8) = CByte(((Hash(8) & "0x0f") Or "0x30"))
            '           Return Uuid
            '       End Function()
            NewProfile = New JObject From {
                {"type", SelectedAuthType},
                {"uuid", Uuid},
                {"username", UserName},
                {"desc", ""}
            }
        End If
        If SelectedAuthTypeNum = 2 Then '正版验证
            IsProfileCreating = True
            FrmLaunchLeft.RefreshPage(False, True, True, "microsoft")
            Exit Sub
        End If
        If SelectedAuthTypeNum = 3 Then '第三方验证
            IsProfileCreating = True
            Dim AuthServer As String = Nothing
            SelectedAuthType = "authlib"
            FrmLaunchLeft.RefreshPage(False, True, True, "authlib")
            Exit Sub
        End If
        ProfileList.Add(NewProfile)
        WriteProfileJson()
        Hint("档案新建成功！", HintType.Finish)
        RefreshProfileList()
    End Sub
    Public Function ProfileListItem(Json As JObject, OnClick As MyListItem.ClickEventHandler)
        Dim NewItem As New MyListItem With {
                .Title = Json("username").ToString,
                .Info = GetProfileInfo(Json("type").ToString, Desc:=Json("desc").ToString, ServerName:=If(Json("type").ToString = "authlib", Json("serverName").ToString, Nothing)),
                .Type = MyListItem.CheckType.Clickable,
                .Logo = "pack://application:,,,/images/Blocks/Grass.png",
                .Tag = Json
        }
        AddHandler NewItem.Click, OnClick
        NewItem.ContentHandler = AddressOf ProfileContMenuBuild
        Return NewItem
    End Function
    Private Sub ProfileContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnDelete As New MyIconButton With {.Logo = Logo.IconButtonDelete, .ToolTip = "删除档案", .Tag = sender.Tag}
        ToolTipService.SetPlacement(BtnDelete, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnDelete, 30)
        ToolTipService.SetHorizontalOffset(BtnDelete, 2)
        AddHandler BtnDelete.Click, AddressOf DeleteProfile
        sender.Buttons = {BtnDelete}
    End Sub
    Private Sub DeleteProfile(sender As Object, e As EventArgs)
        If MyMsgBox($"你正在选择删除此档案，该操作无法撤销。{vbCrLf}确定继续？", "删除档案确认", "继续", "取消", IsWarn:=True, ForceWait:=True) = 2 Then Exit Sub
        ProfileList.Remove(sender.Tag)
        LastUsedProfile = 0
        WriteProfileJson()
        Hint("档案删除成功！", HintType.Finish)
        RefreshProfileList()
    End Sub
    ''' <summary>
    ''' 以当前的档案列表写入配置文件
    ''' </summary>
    Public Shared Sub WriteProfileJson()
        Try
            Log("[Profile] 写入档案列表")
            Dim Json As New JObject From {
                {"lastUsed", LastUsedProfile},
                {"profiles", ProfileList}
            }
            WriteFile(PathAppdataConfig & "Profiles.json", Json.ToString, False)
        Catch ex As Exception
            Log(ex, "写入档案列表失败", LogLevel.Feedback)
        End Try
    End Sub
    ''' <summary>
    ''' 获取档案详情信息用于显示
    ''' </summary>
    ''' <param name="Type">验证方式</param>
    ''' <param name="ServerName">可选，验证服务器名称</param>
    ''' <param name="Desc">可选，用户自定义描述</param>
    ''' <returns>显示的详情信息</returns>
    Public Shared Function GetProfileInfo(Type As String, Optional ServerName As String = Nothing, Optional Desc As String = Nothing)
        Dim Info As String = Nothing
        If Type = "offline" Then Info += "离线验证"
        If Type = "microsoft" Then Info += "正版验证"
        If Type = "authlib" Then
            Info += "第三方验证"
            If ServerName IsNot Nothing AndAlso Not ServerName = "" Then Info += $" / {ServerName}"
        End If
        If Desc IsNot Nothing AndAlso Not Desc = "" Then Info += $"，{Desc}"
        Return Info
    End Function
End Class
