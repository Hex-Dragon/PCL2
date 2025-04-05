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
        Dim SelectedAuthTypeNum As Integer? = Nothing '验证类型序号
        Dim UserName As String = Nothing '玩家 ID
        Dim UserUuid As String = Nothing 'UUID
        Dim AuthName As String = Nothing '验证使用的用户名（离线验证为空）
        Dim AuthPassword As String = Nothing '验证使用的密码（离线验证为空）
        RunInUiWait(Sub() SelectedAuthTypeNum = MyMsgBoxSelect(AuthTypeList, "新建档案 - 选择验证类型", "继续", "取消") + 1)
        If SelectedAuthTypeNum Is Nothing Then Exit Sub
        If SelectedAuthTypeNum = 1 Then '离线验证
            SelectedAuthType = "offline"
            UserName = MyMsgBoxInput("新建档案 - 输入档案名称", HintText:="只可以使用英文字母、数字与下划线", Button1:="继续", Button2:="取消")
            If UserName = Nothing Then Exit Sub
            UserUuid = GetPlayerUuid(UserName, False)
            NewProfile = New JObject From {
                {"type", SelectedAuthType},
                {"uuid", UserUuid},
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

#Region "离线 UUID 获取"
    Public Function GetPlayerUuid(UserName As String, Optional IsSplited As Boolean = False) As String
        Dim MD5 As New MD5CryptoServiceProvider
        Dim Hash As Byte() = MD5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + UserName))
        Hash(6) = Hash(6) And &HF
        Hash(6) = Hash(6) Or &H30
        Hash(8) = Hash(8) And &H3F
        Hash(8) = Hash(8) Or &H80
        Dim Parsed As New Guid(ToUuidString(Hash))
        If IsSplited Then
            Return Parsed.ToString()
        Else
            Return Parsed.ToString().Replace("-", "")
        End If
    End Function
    Public Function ToUuidString(ByVal Bytes As Byte()) As String
        Dim msb As Long = 0
        Dim lsb As Long = 0
        For i As Integer = 0 To 7
            msb = (msb << 8) Or (Bytes(i) And &HFF)
        Next
        For i As Integer = 8 To 15
            lsb = (lsb << 8) Or (Bytes(i) And &HFF)
        Next
        Return (Digits(msb >> 32, 8) + "-" + Digits(msb >> 16, 4) + "-" + Digits(msb, 4) + "-" + Digits(lsb >> 48, 4) + "-" + Digits(lsb, 12))
    End Function
    Public Function Digits(ByVal Val As Long, ByVal Digs As Integer)
        Dim hi As Long = 1L << (Digs * 4)
        Return (hi Or (Val And (hi - 1))).ToString("X").Substring(1)
    End Function
#End Region

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
        Dim BtnChangeRole As New MyIconButton With {.Logo = Logo.IconButtonCard, .ToolTip = "切换角色", .Tag = sender.Tag}
        ToolTipService.SetPlacement(BtnChangeRole, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnChangeRole, 30)
        ToolTipService.SetHorizontalOffset(BtnChangeRole, 2)
        AddHandler BtnChangeRole.Click, AddressOf ChangeRole
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
        If sender.Tag("type") = "offline" Then
            sender.Buttons = {BtnUUID, BtnDelete}
        ElseIf sender.Tag("type") = "authlib" Then
            'sender.Buttons = {BtnDelete}
            sender.Buttons = {BtnChangeRole, BtnDelete}
        Else
            sender.Buttons = {BtnDelete}
        End If
    End Sub
    Private Sub ChangeRole(sender As Object, e As EventArgs)
        If FrmLoginAuthSkin Is Nothing Then FrmLoginAuthSkin = New PageLoginAuthSkin
        FrmLoginAuthSkin.BtnEdit_Click(sender, e)
        Exit Sub
        Dim ProfileIndex = ProfileList.IndexOf(sender.Tag)
        RunInUi(Sub()
                    If McLoginLoader.State = LoadState.Loading Then
                        Log("[Launch] 要求更换角色，但登录加载器繁忙", LogLevel.Debug)
                        If CType(McLoginLoader.Input, McLoginServer).ForceReselectProfile Then
                            Hint("正在尝试更换，请稍候！")
                            Exit Sub
                        Else
                            Hint("正在登录中，请稍后再更换角色！", HintType.Critical)
                            Exit Sub
                        End If
                    End If
                End Sub)
        Hint("正在尝试更换，请稍候！")
        Setup.Set("CacheAuthUuid", "") '清空选择缓存
        Setup.Set("CacheAuthName", "")
        RunInThread(
        Sub()
            Try
                Dim Data As McLoginServer
                Data = New McLoginServer(McLoginType.Auth) With {
                    .Token = "Auth",
                    .BaseUrl = sender.Tag("server"),
                    .UserName = sender.Tag("name"),
                    .Password = sender.Tag("password"),
                    .Description = "Authlib-Injector",
                    .Type = McLoginType.Auth,
                    .ForceReselectProfile = True
                }
                McLoginLoader.WaitForExit(Data, IsForceRestart:=True)
                RunInUi(Sub() Reload(True))
            Catch ex As Exception
                Log(ex, "更换角色失败", LogLevel.Hint)
            End Try
        End Sub)
    End Sub
    Private Sub EditProfile(sender As Object, e As EventArgs)
        Dim ProfileIndex = ProfileList.IndexOf(sender.Tag)
        Dim NewUuid As String = MyMsgBoxInput($"更改档案 {sender.Tag("username")} 的 UUID", DefaultInput:=sender.Tag("uuid"), ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(32, 32)}, HintText:="32 位，不包括连字符", Button1:="确定", Button2:="取消")
        If NewUuid = Nothing Then Exit Sub
        ProfileList(ProfileIndex)("uuid") = NewUuid
        WriteProfileJson()
        Hint("档案信息已保存！", HintType.Finish)
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
