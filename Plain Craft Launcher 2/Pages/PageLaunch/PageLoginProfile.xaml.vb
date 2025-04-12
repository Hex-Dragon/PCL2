Imports System.Security.Cryptography
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel

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
    Public Shared SelectedProfile As McProfile = Nothing
    ''' <summary>
    ''' 上次选定的档案
    ''' </summary>
    Public Shared LastUsedProfile As Integer = Nothing
    ''' <summary>
    ''' 档案列表
    ''' </summary>
    Public Shared ProfileList As New List(Of McProfile)

    Public Class McProfile
        Public Type As McLoginType
        Public Uuid As String
        ''' <summary>
        ''' 玩家 ID
        ''' </summary>
        Public Username As String
        ''' <summary>
        ''' 验证服务器地址，用于第三方验证
        ''' </summary>
        Public Server As String
        ''' <summary>
        ''' 验证服务器名称，来自第三方验证服务器返回的 Metadata
        ''' </summary>
        Public ServerName As String
        Public AccessToken As String
        Public RefreshToken As String
        ''' <summary>
        ''' 登录用户名，用于第三方验证
        ''' </summary>
        Public Name As String
        Public Password As String
        Public Expires As Int64
        Public Desc As String
        Public ClientToken As String
        Public RawJson As String
    End Class

#Region "读写档案列表"
    ''' <summary>
    ''' 刷新档案列表
    ''' </summary>
    Private Sub RefreshProfileList()
        Log("[Profile] 刷新档案列表")
        StackProfile.Children.Clear()
        ProfileList.Clear()
        Try
            If Not File.Exists(PathAppdataConfig & "Profiles.json") Then
                File.Create(PathAppdataConfig & "Profiles.json")
                WriteFile(PathAppdataConfig & "Profiles.json", "{""lastUsed"":0,""profiles"":[]}", False) '创建档案列表文件
            End If
            Dim ProfileJobj As JObject = JObject.Parse(ReadFile(PathAppdataConfig & "Profiles.json"))
            LastUsedProfile = ProfileJobj("lastUsed")
            Dim ProfileListJobj As JArray = ProfileJobj("profiles")
            For Each Profile In ProfileListJobj
                Dim NewProfile As McProfile = Nothing
                If Profile("type") = "microsoft" Then
                    NewProfile = New McProfile With {
                        .Type = McLoginType.Ms,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("username"),
                        .AccessToken = Profile("accessToken"),
                        .RefreshToken = Profile("refreshToken"),
                        .Expires = Profile("expires"),
                        .Desc = Profile("desc"),
                        .RawJson = Profile("rawJson")
                    }
                ElseIf Profile("type") = "authlib" Then
                    NewProfile = New McProfile With {
                        .Type = McLoginType.Auth,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("username"),
                        .AccessToken = Profile("accessToken"),
                        .RefreshToken = Profile("refreshToken"),
                        .Expires = Profile("expires"),
                        .Server = Profile("server"),
                        .ServerName = Profile("serverName"),
                        .Name = Profile("name"),
                        .Password = Profile("password"),
                        .ClientToken = Profile("clientToken"),
                        .Desc = Profile("desc")
                    }
                Else
                    NewProfile = New McProfile With {
                        .Type = McLoginType.Legacy,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("username"),
                        .Desc = Profile("desc")
                    }
                End If
                ProfileList.Add(NewProfile)
                StackProfile.Children.Add(ProfileListItem(NewProfile, AddressOf SelectProfile))
            Next
            Log($"[Profile] 档案刷新完成，获取到 {ProfileList.Count} 个档案")
            If IsFirstLoad Then
                If Not ProfileList.Count = 0 Then SelectedProfile = ProfileList(LastUsedProfile)
                IsFirstLoad = False
            End If
        Catch ex As Exception
            Log(ex, "读取档案列表失败", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 以当前的档案列表写入配置文件
    ''' </summary>
    Public Shared Sub WriteProfileJson(Optional ListJson As JArray = Nothing)
        Try
            Log("[Profile] 写入档案列表")
            Dim Json As New JObject
            If ListJson IsNot Nothing Then
                Json = New JObject From {
                {"lastUsed", LastUsedProfile},
                {"profiles", ListJson}
            }
            Else
                Dim List As New JArray
                For Each Profile In ProfileList
                    Dim ProfileJobj As JObject = Nothing
                    If Profile.Type = 5 Then
                        ProfileJobj = New JObject From {
                            {"type", "microsoft"},
                            {"uuid", Profile.Uuid},
                            {"username", Profile.Username},
                            {"accessToken", Profile.AccessToken},
                            {"refreshToken", Profile.RefreshToken},
                            {"expires", Profile.Expires},
                            {"desc", Profile.Desc},
                            {"rawJson", Profile.RawJson}
                        }
                    ElseIf Profile.Type = 3 Then
                        ProfileJobj = New JObject From {
                            {"type", "authlib"},
                            {"uuid", Profile.Uuid},
                            {"username", Profile.Username},
                            {"accessToken", Profile.AccessToken},
                            {"refreshToken", Profile.RefreshToken},
                            {"expires", Profile.Expires},
                            {"server", Profile.Server},
                            {"serverName", Profile.ServerName},
                            {"name", Profile.Name},
                            {"password", Profile.Password},
                            {"clientToken", Profile.ClientToken},
                            {"desc", Profile.Desc}
                        }
                    Else
                        ProfileJobj = New JObject From {
                            {"type", "offline"},
                            {"uuid", Profile.Uuid},
                            {"username", Profile.Username},
                            {"desc", Profile.Desc}
                        }
                    End If
                    List.Add(ProfileJobj)
                Next
                Log($"[Profile] 开始写入档案，共 {List.Count} 个")
                Json = New JObject From {
                {"lastUsed", LastUsedProfile},
                {"profiles", List}
            }
            End If
            WriteFile(PathAppdataConfig & "Profiles.json", Json.ToString, False)
        Catch ex As Exception
            Log(ex, "写入档案列表失败", LogLevel.Feedback)
        End Try
    End Sub
#End Region

#Region "控件"
    Private Sub SelectProfile(sender As MyListItem, e As EventArgs)
        SelectedProfile = sender.Tag
        Log($"[Profile] 选定档案: {sender.Tag.Username}, 以 {sender.Tag.Type} 方式验证")
        Select Case SelectedProfile.Type
            Case 0
                Setup.Set("LoginType", McLoginType.Legacy)
            Case 5
                Setup.Set("LoginType", McLoginType.Ms)
            Case 3
                Setup.Set("LoginType", McLoginType.Auth)
        End Select
        LastUsedProfile = ProfileList.IndexOf(sender.Tag) '获取当前档案的序号
        IsProfileSelected = True
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
        RunInUi(Sub() FrmLaunchLeft.BtnLaunch.IsEnabled = True)
    End Sub
    Public Function ProfileListItem(Profile As McProfile, OnClick As MyListItem.ClickEventHandler)
        Dim LogoPath As String = Nothing
        If File.Exists(PathTemp & $"Cache\Skin\Head\{Profile.Type}_{Profile.Username}_{Profile.Uuid}.png") Then
            'Log($"[Profile] 档案 {Json("username")} ({Json("type")}) 存在可用头像文件")
            LogoPath = PathTemp & $"Cache\Skin\Head\{Profile.Type}_{Profile.Username}_{Profile.Uuid}.png"
        Else
            LogoPath = Logo.IconButtonUser
        End If
        Dim NewItem As New MyListItem With {
                .Title = Profile.Username,
                .Info = GetProfileInfo(Profile.Type, Desc:=Profile.Desc, ServerName:=If(Profile.Type = 3, Profile.ServerName, Nothing)),
                .Type = MyListItem.CheckType.Clickable,
                .Logo = LogoPath,
                .Tag = Profile
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
        If sender.Tag.Type = 0 Then
            sender.Buttons = {BtnUUID, BtnDelete}
        ElseIf sender.Tag.Type = 3 Then
            sender.Buttons = {BtnDelete}
            'sender.Buttons = {BtnChangeRole, BtnDelete}
        Else
            sender.Buttons = {BtnDelete}
        End If
    End Sub

    Private Sub BtnNew_Click(sender As Object, e As EventArgs) Handles BtnNew.Click
        Dim AuthTypeList As New List(Of IMyRadio) From {
            New MyRadioBox With {.Text = "离线验证"},
            New MyRadioBox With {.Text = "正版验证"},
            New MyRadioBox With {.Text = "第三方验证"}
        }
        Dim NewProfile As McProfile = Nothing
        Dim SelectedAuthTypeNum As Integer? = Nothing '验证类型序号
        Dim UserName As String = Nothing '玩家 ID
        Dim UserUuid As String = Nothing 'UUID
        RunInUiWait(Sub() SelectedAuthTypeNum = MyMsgBoxSelect(AuthTypeList, "新建档案 - 选择验证类型", "继续", "取消"))
        If SelectedAuthTypeNum Is Nothing Then Exit Sub
        If SelectedAuthTypeNum = 1 Then '正版验证
            IsProfileCreating = True
            FrmLaunchLeft.RefreshPage(False, True, True, McLoginType.Ms)
            Exit Sub
        ElseIf SelectedAuthTypeNum = 2 Then '第三方验证
            IsProfileCreating = True
            Dim AuthServer As String = Nothing
            FrmLaunchLeft.RefreshPage(False, True, True, McLoginType.Auth)
            Exit Sub
        Else '离线验证
            UserName = MyMsgBoxInput("新建档案 - 输入档案名称", HintText:="3 - 16 位，只可以使用英文字母、数字与下划线", ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(3, 16), New ValidateRegex("([A-z]|[0-9]|_)+")}, Button1:="继续", Button2:="取消")
            If UserName = Nothing Then Exit Sub
            Dim UuidTypeList As New List(Of IMyRadio) From {
                New MyRadioBox With {.Text = "行业规范 UUID（推荐）"},
                New MyRadioBox With {.Text = "官方版 PCL UUID（若单人存档的部分信息丢失，可尝试此项）"},
                New MyRadioBox With {.Text = "自定义"}
            }
            Dim UuidType As Integer = Nothing
            Dim UuidTypeInput = MyMsgBoxSelect(UuidTypeList, "新建档案 - 选择 UUID 类型", "继续", "取消")
            If UuidTypeInput Is Nothing Then Exit Sub
            UuidType = UuidTypeInput
            If UuidType = 0 Then
                UserUuid = GetPlayerUuid(UserName, False)
            ElseIf UuidType = 1 Then
                UserUuid = McLoginLegacyUuid(UserName)
            Else
                UserUuid = MyMsgBoxInput("新建档案 - 输入 UUID", HintText:="32 位，不含连字符", ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(32, 32), New ValidateRegex("([A-z]|[0-9]){32}", "UUID 只应该包括英文字母和数字！")}, Button1:="继续", Button2:="取消")
            End If
            If UserUuid = Nothing Then Exit Sub
            NewProfile = New McProfile With {
                .Type = McLoginType.Legacy,
                .Uuid = UserUuid,
                .Username = UserName,
                .Desc = ""}
        End If
        ProfileList.Add(NewProfile)
        WriteProfileJson()
        Hint("档案新建成功！", HintType.Finish)
        RefreshProfileList()
    End Sub
#End Region

#Region "离线 UUID 获取"
    ''' <summary>
    ''' 获取离线 UUID
    ''' </summary>
    ''' <param name="UserName">用户名</param>
    ''' <param name="IsSplited">返回的 UUID 是否有连字符分割</param>
    ''' <returns></returns>
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

#Region "档案编辑"
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
                    .BaseUrl = sender.Tag.Server,
                    .UserName = sender.Tag.Name,
                    .Password = sender.Tag.Password,
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
        Dim NewUuid As String = Nothing
        Dim UuidTypeList As New List(Of IMyRadio) From {
                New MyRadioBox With {.Text = "行业规范 UUID（推荐）"},
                New MyRadioBox With {.Text = "官方版 PCL UUID（若单人存档的部分信息丢失，可尝试此项）"},
                New MyRadioBox With {.Text = "自定义"}
            }
        Dim UuidType As Integer = Nothing
        Dim UuidTypeInput = MyMsgBoxSelect(UuidTypeList, "选择 UUID 类型", "继续", "取消")
        If UuidTypeInput Is Nothing Then Exit Sub
        UuidType = UuidTypeInput
        If UuidType = 0 Then
            NewUuid = GetPlayerUuid(sender.Tag.UserName, False)
        ElseIf UuidType = 1 Then
            NewUuid = McLoginLegacyUuid(sender.Tag.UserName)
        Else
            NewUuid = MyMsgBoxInput($"更改档案 {sender.Tag.Username} 的 UUID", DefaultInput:=sender.Tag.Uuid, HintText:="32 位，不含连字符", ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(32, 32), New ValidateRegex("([A-z]|[0-9]){32}", "UUID 只应该包括英文字母和数字！")}, Button1:="继续", Button2:="取消")
        End If
        If NewUuid = Nothing Then Exit Sub
        ProfileList(ProfileIndex).Uuid = NewUuid
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
#End Region

#Region "档案迁移"
    Private Sub MigrateProfile() Handles BtnPort.Click
        Dim Type As Integer = 3
        RunInUiWait(Sub() Type = MyMsgBox($"PCL CE 支持导入 HMCL 的全局账户列表，抑或是导出档案列表至 HMCL 全局账户列表。{vbCrLf}你想要...？", "导入 / 导出档案", "导入", "导出", "取消", ForceWait:=True))
        If Type = 3 Then Exit Sub
        Dim OutsidePath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\.hmcl\accounts.json"
        If Type = 1 Then
            RunInNewThread(Sub()
                               Dim ImportList As JArray = JArray.Parse(ReadFile(OutsidePath))
                               Dim OutputList As New List(Of McProfile)
                               For Each Profile In ImportList
                                   Dim NewProfile As McProfile = Nothing
                                   If Profile("type") = "microsoft" Then
                                       NewProfile = New McProfile With {
                        .Type = McLoginType.Ms,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("displayName"),
                        .AccessToken = "",
                        .RefreshToken = "",
                        .Expires = 1743779140286,
                        .Desc = "",
                        .RawJson = ""
                    }
                                       OutputList.Add(NewProfile)
                                   ElseIf Profile("type") = "authlibInjector" Then
                                       NewProfile = New McProfile With {
                        .Type = McLoginType.Auth,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("displayName"),
                        .AccessToken = "",
                        .RefreshToken = "",
                        .Expires = 1743779140286,
                        .Server = Profile("serverBaseURL"),
                        .ServerName = "",
                        .Name = Profile("username"),
                        .Password = "",
                        .ClientToken = Profile("clientToken"),
                        .Desc = ""
                    }
                                       Dim Response As String = NetGetCodeByRequestRetry(NewProfile.Server.Replace("/authserver", ""), Encoding.UTF8)
                                       Dim ServerName As String = JObject.Parse(Response)("meta")("serverName").ToString()
                                       NewProfile.ServerName = ServerName
                                       OutputList.Add(NewProfile)
                                   Else
                                       NewProfile = New McProfile With {
                        .Type = McLoginType.Legacy,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("username"),
                        .Desc = ""
                    }
                                       OutputList.Add(NewProfile)
                                   End If
                               Next
                               For Each Profile In OutputList
                                   ProfileList.Add(Profile)
                               Next
                               WriteProfileJson()
                               RunInUi(Sub() RefreshProfileList())
                           End Sub)
            Hint("档案导入成功，部分档案可能需要重新验证密码！", HintType.Finish)
        Else
            RunInNewThread(Sub()
                               Dim ExistList As JArray = JArray.Parse(ReadFile(OutsidePath))
                               Dim OutputList As JArray = New JArray
                               For Each Profile In ProfileList
                                   Dim NewProfile As JObject = Nothing
                                   If Profile.Type = 5 Then
                                       NewProfile = New JObject From {
                                           {"uuid", Profile.Uuid},
                                           {"displayName", Profile.Username},
                                           {"tokenType", "Bearer"},
                                           {"accessToken", ""},
                                           {"refreshToken", ""},
                                           {"notAfter", 1743779140286},
                                           {"userid", ""},
                                           {"type", "microsoft"}
                                       }
                                       OutputList.Add(NewProfile)
                                   ElseIf Profile.Type = 3 Then
                                       NewProfile = New JObject From {
                                           {"serverBaseURL", Profile.Server},
                                           {"clientToken", ""},
                                           {"displayName", Profile.Username},
                                           {"accessToken", ""},
                                           {"type", "authlibInjector"},
                                           {"uuid", Profile.Uuid},
                                           {"username", Profile.Name}
                                       }
                                       OutputList.Add(NewProfile)
                                   Else
                                       NewProfile = New JObject From {
                                           {"uuid", Profile.Uuid},
                                           {"username", Profile.Username},
                                           {"type", "offline"}
                                       }
                                       OutputList.Add(NewProfile)
                                   End If
                               Next
                               For Each Profile In OutputList
                                   ExistList.Add(Profile)
                               Next
                               WriteFile(OutsidePath, ExistList.ToString())
                           End Sub)
            Hint("档案导出成功，部分档案可能需要重新验证密码！", HintType.Finish)
        End If
    End Sub
#End Region

    ''' <summary>
    ''' 获取档案详情信息用于显示
    ''' </summary>
    ''' <param name="Type">验证方式</param>
    ''' <param name="ServerName">可选，验证服务器名称</param>
    ''' <param name="Desc">可选，用户自定义描述</param>
    ''' <returns>显示的详情信息</returns>
    Public Shared Function GetProfileInfo(Type As McLoginType, Optional ServerName As String = Nothing, Optional Desc As String = Nothing)
        Dim Info As String = Nothing
        If Type = 3 Then
            Info += "第三方验证"
            If ServerName IsNot Nothing AndAlso Not ServerName = "" Then Info += $" / {ServerName}"
        ElseIf Type = 5 Then
            Info += "正版验证"
        Else
            Info += "离线验证"
        End If
        If Desc IsNot Nothing AndAlso Not Desc = "" Then Info += $"，{Desc}"
        Return Info
    End Function
End Class
