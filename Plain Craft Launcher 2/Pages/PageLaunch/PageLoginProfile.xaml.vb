Class PageLoginProfile
    Private IsReloaded As Boolean = False
    Public Shared IsProfileSelected As Boolean = False
    Public Shared IsProfileCreating As Boolean = False
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        RefreshProfileList()
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
    Public Shared ProfileList As JArray = Nothing
    Private Sub RefreshProfileList() Handles BtnTestRefresh.Click
        Log("[Profile] 刷新档案列表")
        StackProfile.Children.Clear()
        Try
            Dim Reader As New StreamReader(PathAppdataConfig & "Profiles.json")
            Dim ProfileJobj As JObject = JObject.Parse(Reader.ReadToEnd())
            Reader.Close()
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
            IsProfileSelected = True
            RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
        Catch ex As Exception
            Log(ex, "读取档案列表失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Sub SelectProfile(sender As MyListItem, e As EventArgs)
        SelectedProfile = sender.Tag
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
        RunInUiWait(Sub() SelectedAuthTypeNum = MyMsgBoxSelect(AuthTypeList, "新建档案 - 选择验证类型", "继续", "取消"))
        If SelectedAuthTypeNum = 0 Then '离线验证
            SelectedAuthType = "offline"
            UserName = MyMsgBoxInput("新建档案 - 输入档案名称", HintText:="只可以使用英文字母、数字与下划线", Button1:="继续", Button2:="取消")
            NewProfile = New JObject From {
            {"type", SelectedAuthType},
            {"uuid", Uuid},
            {"username", UserName},
            {"desc", ""}
            }
        End If
        If SelectedAuthTypeNum = 1 Then '正版验证
            IsProfileCreating = True
            FrmLaunchLeft.RefreshPage(False, True, True, "microsoft")
            Exit Sub
        End If
        If SelectedAuthTypeNum = 2 Then '第三方验证
            IsProfileCreating = True
            Dim AuthServer As String = Nothing
            SelectedAuthType = "authlib"
            AuthServer = MyMsgBoxInput("新建档案 - 输入验证服务器地址", ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateHttp()}, Button1:="继续", Button2:="取消")
            AuthName = MyMsgBoxInput("新建档案 - 输入用户名或邮箱", Button1:="继续", Button2:="取消")
            AuthPassword = MyMsgBoxInput("新建档案 - 输入密码", Button1:="继续", Button2:="取消")
            NewProfile = New JObject From {
            {"type", SelectedAuthType},
            {"uuid", Uuid},
            {"username", UserName},
            {"server", AuthServer},
            {"serverName", "Default"},
            {"name", AuthName},
            {"password", AuthPassword},
            {"accessToken", ""},
            {"refreshToken", ""},
            {"expires", 114514},
            {"desc", ""}
            }
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
        WriteProfileJson()
        Hint("档案删除成功！", HintType.Finish)
        RefreshProfileList()
    End Sub
    Public Shared Sub WriteProfileJson()
        Try
            Dim Writer As New StreamWriter(PathAppdataConfig & "Profiles.json", False)
            Dim Json As New JObject From {
                {"lastUsed", LastUsedProfile},
                {"profiles", ProfileList}
            }
            Writer.WriteLine(Json.ToString)
            Writer.Close()
        Catch ex As Exception
            Log(ex, "写入档案列表失败", LogLevel.Feedback)
        End Try
    End Sub
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
