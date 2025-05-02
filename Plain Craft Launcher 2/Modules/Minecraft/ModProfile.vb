Imports System.Security.Cryptography

Public Module ModProfile

    ''' <summary>
    ''' 当前选定的档案
    ''' </summary>
    Public SelectedProfile As McProfile = Nothing
    ''' <summary>
    ''' 上次选定的档案编号
    ''' </summary>
    Public LastUsedProfile As Integer = Nothing
    ''' <summary>
    ''' 档案列表
    ''' </summary>
    Public ProfileList As New List(Of McProfile)
    Private IsFirstLoad As Boolean = True
    Public IsCreatingProfile As Boolean = False
    ''' <summary>
    ''' 档案操作日志
    ''' </summary>
    Public Sub ProfileLog(Content As String)
        Dim Output As String = "[Profile] " & Content
        Log(Output)
    End Sub

#Region "类型声明"
    Public Class McProfile
        ''' <summary>
        ''' 档案类型
        ''' </summary>
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
        ''' <summary>
        ''' 登录密码，用于第三方验证
        ''' </summary>
        Public Password As String
        ''' <summary>
        ''' 联网验证档案的验证有效期
        ''' </summary>
        Public Expires As Int64
        ''' <summary>
        ''' 档案描述，暂时没做功能
        ''' </summary>
        Public Desc As String
        Public ClientToken As String
        ''' <summary>
        ''' 原始 JSON 数据，用于正版验证部分功能
        ''' </summary>
        Public RawJson As String
        ''' <summary>
        ''' 用于档案列表头像显示的皮肤 ID
        ''' </summary>
        Public SkinHeadId As String
    End Class
#End Region

#Region "读写档案"
    ''' <summary>
    ''' 重新获取已有档案列表
    ''' </summary>
    Public Sub GetProfile()
        ProfileLog("开始获取本地档案")
        ProfileList.Clear()
        Try
            If Not Directory.Exists(PathAppdataConfig) Then Directory.CreateDirectory(PathAppdataConfig)
            If Not File.Exists(PathAppdataConfig & "Profiles.json") Then
                File.Create(PathAppdataConfig & "Profiles.json").Close()
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
                        .AccessToken = SecretDecrypt(Profile("accessToken")),
                        .RefreshToken = SecretDecrypt(Profile("refreshToken")),
                        .Expires = Profile("expires"),
                        .Desc = Profile("desc"),
                        .RawJson = SecretDecrypt(Profile("rawJson")),
                        .SkinHeadId = Profile("skinHeadId")
                    }
                ElseIf Profile("type") = "authlib" Then
                    NewProfile = New McProfile With {
                        .Type = McLoginType.Auth,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("username"),
                        .AccessToken = SecretDecrypt(Profile("accessToken")),
                        .RefreshToken = SecretDecrypt(Profile("refreshToken")),
                        .Expires = Profile("expires"),
                        .Server = Profile("server"),
                        .ServerName = Profile("serverName"),
                        .Name = SecretDecrypt(Profile("name")),
                        .Password = SecretDecrypt(Profile("password")),
                        .ClientToken = SecretDecrypt(Profile("clientToken")),
                        .Desc = Profile("desc"),
                        .SkinHeadId = Profile("skinHeadId")
                    }
                Else
                    NewProfile = New McProfile With {
                        .Type = McLoginType.Legacy,
                        .Uuid = Profile("uuid"),
                        .Username = Profile("username"),
                        .Desc = Profile("desc"),
                        .SkinHeadId = Profile("skinHeadId")
                    }
                End If
                ProfileList.Add(NewProfile)
            Next
            ProfileLog($"获取到 {ProfileList.Count} 个档案")
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
    Public Sub SaveProfile(Optional ListJson As JArray = Nothing)
        Try
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
                    If Profile.Type = McLoginType.Ms Then
                        ProfileJobj = New JObject From {
                            {"type", "microsoft"},
                            {"uuid", Profile.Uuid},
                            {"username", Profile.Username},
                            {"accessToken", SecretEncrypt(Profile.AccessToken)},
                            {"refreshToken", SecretEncrypt(Profile.RefreshToken)},
                            {"expires", Profile.Expires},
                            {"desc", Profile.Desc},
                            {"rawJson", SecretEncrypt(Profile.RawJson)},
                            {"skinHeadId", Profile.SkinHeadId}
                        }
                    ElseIf Profile.Type = McLoginType.Auth Then
                        ProfileJobj = New JObject From {
                            {"type", "authlib"},
                            {"uuid", Profile.Uuid},
                            {"username", Profile.Username},
                            {"accessToken", SecretEncrypt(Profile.AccessToken)},
                            {"refreshToken", SecretEncrypt(Profile.RefreshToken)},
                            {"expires", Profile.Expires},
                            {"server", Profile.Server},
                            {"serverName", Profile.ServerName},
                            {"name", SecretEncrypt(Profile.Name)},
                            {"password", SecretEncrypt(Profile.Password)},
                            {"clientToken", SecretEncrypt(Profile.ClientToken)},
                            {"desc", Profile.Desc},
                            {"skinHeadId", Profile.SkinHeadId}
                        }
                    Else
                        ProfileJobj = New JObject From {
                            {"type", "offline"},
                            {"uuid", Profile.Uuid},
                            {"username", Profile.Username},
                            {"desc", Profile.Desc},
                            {"skinHeadId", Profile.SkinHeadId}
                        }
                    End If
                    List.Add(ProfileJobj)
                Next
                ProfileLog($"开始保存档案，共 {List.Count} 个")
                Json = New JObject From {
                {"lastUsed", LastUsedProfile},
                {"profiles", List}
            }
            End If
            WriteFile(PathAppdataConfig & "Profiles.json", Json.ToString, False)
            ProfileLog($"档案已保存")
        Catch ex As Exception
            Log(ex, "写入档案列表失败", LogLevel.Feedback)
        End Try
    End Sub
#End Region

#Region "新建与编辑"
    ''' <summary>
    ''' 新建档案
    ''' </summary>
    Public Sub CreateProfile()
        Dim SelectedAuthTypeNum As Integer? = Nothing '验证类型序号
        RunInUiWait(Sub()
                        Dim AuthTypeList As New List(Of IMyRadio) From {
                            New MyRadioBox With {.Text = "离线验证"},
                            New MyRadioBox With {.Text = "正版验证"},
                            New MyRadioBox With {.Text = "第三方验证"}
                        }
                        SelectedAuthTypeNum = MyMsgBoxSelect(AuthTypeList, "新建档案 - 选择验证类型", "继续", "取消")
                    End Sub)
        If SelectedAuthTypeNum Is Nothing Then Exit Sub
        If SelectedAuthTypeNum = 1 Then '正版验证
            RunInUi(Sub() FrmLaunchLeft.RefreshPage(True, McLoginType.Ms))
        ElseIf SelectedAuthTypeNum = 2 Then '第三方验证
            RunInUi(Sub() FrmLaunchLeft.RefreshPage(True, McLoginType.Auth))
        Else '离线验证
            Dim UserName As String = Nothing '玩家 ID
            Dim UserUuid As String = Nothing 'UUID
            RunInUiWait(Sub() UserName = MyMsgBoxInput("新建档案 - 输入档案名称", HintText:="3 - 16 位，只可以使用英文字母、数字与下划线",
                                                       ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(3, 16), New ValidateRegex("([A-z]|[0-9]|_)+")},
                                                       Button1:="继续", Button2:="取消"))
            If UserName = Nothing Then Exit Sub
            Dim UuidType As Integer = Nothing
            RunInUiWait(Sub()
                            Dim UuidTypeList As New List(Of IMyRadio) From {
                                New MyRadioBox With {.Text = "行业规范 UUID（推荐）"},
                                New MyRadioBox With {.Text = "官方版 PCL UUID（若单人存档的部分信息丢失，可尝试此项）"},
                                New MyRadioBox With {.Text = "自定义"}
                            }
                            UuidType = MyMsgBoxSelect(UuidTypeList, "新建档案 - 选择 UUID 类型", "继续")
                        End Sub)
            If UuidType = 0 Then
                UserUuid = GetOfflineUuid(UserName, False)
            ElseIf UuidType = 1 Then
                UserUuid = GetOfflineUuid(UserName, IsLegacy:=True)
            Else
                UserUuid = MyMsgBoxInput("新建档案 - 输入 UUID", HintText:="32 位，不含连字符",
                                         ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(32, 32), New ValidateRegex("([A-z]|[0-9]){32}", "UUID 只应该包括英文字母和数字！")},
                                         Button1:="继续", Button2:="取消")
            End If
            If UserUuid = Nothing Then Exit Sub
            Dim NewProfile = New McProfile With {
                .Type = McLoginType.Legacy,
                .Uuid = UserUuid,
                .Username = UserName,
                .Desc = ""}
            ProfileList.Add(NewProfile)
            SaveProfile()
            Hint("档案新建成功！", HintType.Finish)
        End If
    End Sub
    ''' <summary>
    ''' 编辑当前档案的 ID
    ''' </summary>
    Public Sub EditProfileID()
        If SelectedProfile.Type = McLoginType.Ms Then
            Dim NewUsername As String = Nothing
            RunInUiWait(Sub() NewUsername = MyMsgBoxInput("输入新的玩家 ID", DefaultInput:=SelectedProfile.Username,
                                                          ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(3, 16), New ValidateRegex("([A-z]|[0-9]|_)+")},
                                                          HintText:="3 - 16 个字符，只可以包含大小写字母、数字、下划线", Button1:="确认", Button2:="取消"))
            If NewUsername = Nothing Then Exit Sub
            Dim Result As String = NetRequestRetry($"https://api.minecraftservices.com/minecraft/profile/name/", "PUT", "", "application/json", 2, New Dictionary(Of String, String) From {{"Authorization", "Bearer " & SelectedProfile.AccessToken}})
            Try
                Dim ResultJson As JObject = GetJson(Result)
                Hint($"玩家 ID 修改成功，当前 ID 为：{ResultJson("name")}", HintType.Finish)
            Catch ex As WebException
                Dim Message As String = GetExceptionSummary(ex)
                If Message.Contains("(400)") Then
                    MyMsgBox("玩家 ID 修改失败，因为不符合规范！", "ID 修改失败", "确认", IsWarn:=True)
                ElseIf Message.Contains("(403)") Then
                    If Message.Contains("DUPLICATE") Then
                        MyMsgBox("玩家 ID 修改失败，因为该 ID 已被使用！", "ID 修改失败", "确认", IsWarn:=True)
                    End If
                Else
                    Throw
                End If
            End Try
        ElseIf SelectedProfile.Type = McLoginType.Auth Then
            Dim Server As String = SelectedProfile.Server
            OpenWebsite(Server.ToString.Replace("/api/yggdrasil/authserver" + If(Server.EndsWithF("/"), "/", ""), "/user/profile"))
        Else
            Dim NewUsername As String = Nothing
            RunInUiWait(Sub() NewUsername = MyMsgBoxInput("输入新的玩家 ID", DefaultInput:=SelectedProfile.Username,
                                                          ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(3, 16), New ValidateRegex("([A-z]|[0-9]|_)+")},
                                                          HintText:="3 - 16 个字符，只可以包含大小写字母、数字、下划线", Button1:="确认", Button2:="取消"))
            If NewUsername = Nothing Then Exit Sub
            EditOfflineUuid(SelectedProfile, GetOfflineUuid(NewUsername))
        End If
    End Sub
    ''' <summary>
    ''' 编辑离线档案的 UUID
    ''' </summary>
    ''' <param name="Profile">目标档案</param>
    Public Sub EditOfflineUuid(Profile As McProfile, Optional Uuid As String = Nothing)
        Dim ProfileIndex = ProfileList.IndexOf(Profile)
        Dim NewUuid As String
        If Uuid IsNot Nothing Then
            NewUuid = Uuid
            GoTo Write
        End If
        Dim UuidType As Integer
        Dim UuidTypeInput As Integer? = Nothing
        RunInUiWait(Sub()
                        Dim UuidTypeList As New List(Of IMyRadio) From {
                                New MyRadioBox With {.Text = "行业规范 UUID（推荐）"},
                                New MyRadioBox With {.Text = "官方版 PCL UUID（若单人存档的部分信息丢失，可尝试此项）"},
                                New MyRadioBox With {.Text = "自定义"}
                            }
                        UuidTypeInput = MyMsgBoxSelect(UuidTypeList, "新建档案 - 选择 UUID 类型", "继续", "取消")
                    End Sub)
        If UuidTypeInput Is Nothing Then Exit Sub
        UuidType = UuidTypeInput
        If UuidType = 0 Then
            NewUuid = GetOfflineUuid(Profile.Username, False)
        ElseIf UuidType = 1 Then
            NewUuid = GetOfflineUuid(Profile.Username, IsLegacy:=True)
        Else
            NewUuid = MyMsgBoxInput($"更改档案 {Profile.Username} 的 UUID", DefaultInput:=Profile.Uuid, HintText:="32 位，不含连字符", ValidateRules:=New ObjectModel.Collection(Of Validate) From {New ValidateLength(32, 32), New ValidateRegex("([A-z]|[0-9]){32}", "UUID 只应该包括英文字母和数字！")}, Button1:="继续", Button2:="取消")
        End If
        If NewUuid = Nothing Then Exit Sub
Write:
        ProfileList(ProfileIndex).Uuid = NewUuid
        SelectedProfile = ProfileList(ProfileIndex)
        SaveProfile()
        Hint("档案信息已保存！", HintType.Finish)
    End Sub
    ''' <summary>
    ''' 删除特定档案
    ''' </summary>
    ''' <param name="Profile">目标档案</param>
    Public Sub RemoveProfile(Profile As McProfile)
        ProfileList.Remove(Profile)
        LastUsedProfile = Nothing
        SaveProfile()
        Hint("档案删除成功！", HintType.Finish)
    End Sub
#End Region

#Region "导入与导出"
    Public Sub MigrateProfile()
        Dim Type As Integer = 3
        RunInUiWait(Sub()
                        If ProfileList.Any() Then
                            Type = MyMsgBox($"PCL CE 支持导入 HMCL 的全局账户列表，抑或是导出档案列表至 HMCL 全局账户列表。{vbCrLf}你想要...？", "导入 / 导出档案", "导入", "导出", "取消", ForceWait:=True)
                            If Type = 3 Then Exit Sub
                        Else
                            Type = MyMsgBox($"PCL CE 支持导入 HMCL 的全局账户列表，抑或是导出档案列表至 HMCL 全局账户列表。{vbCrLf}由于目前 PCL CE 不存在任何可用档案，无法导出档案。", "导入 / 导出档案", "导入", "取消", ForceWait:=True)
                            If Type = 2 Then Exit Sub
                        End If
                    End Sub)
        Dim OutsidePath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\.hmcl\accounts.json"
        If Type = 1 Then '导入
            Hint("正在导入，请稍后...", HintType.Info)
            RunInNewThread(Sub()
                               Dim ImportList As JArray = JArray.Parse(ReadFile(OutsidePath))
                               Dim OutputList As New List(Of McProfile)
                               Dim ImportNum As Integer = 0
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
                                                               .RawJson = "",
                                                               .SkinHeadId = ""
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
                                                               .Desc = "",
                                                               .SkinHeadId = ""
                                                           }
                                       Dim Response As String = Nothing
                                       Try
                                           Response = NetGetCodeByRequestRetry(NewProfile.Server.Replace("/authserver", ""), Encoding.UTF8)
                                           Dim ServerName As String = JObject.Parse(Response)("meta")("serverName").ToString()
                                           NewProfile.ServerName = ServerName
                                       Catch ex As Exception
                                           ProfileLog("获取服务器名称失败，继续档案添加流程: " & ex.ToString())
                                       End Try
                                       OutputList.Add(NewProfile)
                                   Else
                                       NewProfile = New McProfile With {
                                                               .Type = McLoginType.Legacy,
                                                               .Uuid = Profile("uuid"),
                                                               .Username = Profile("username"),
                                                               .Desc = "",
                                                               .SkinHeadId = ""
                                                           }
                                       OutputList.Add(NewProfile)
                                   End If
                                   ImportNum += 1
                               Next
                               For Each Profile In OutputList
                                   ProfileList.Add(Profile)
                               Next
                               SaveProfile()
                               Hint($"已导入 {ImportNum} 个档案，部分档案可能需要重新验证密码！", HintType.Finish)
                               RunInUi(Sub() FrmLoginProfile.RefreshProfileList())
                           End Sub, "Profile Import")
        Else '导出
            Hint("正在导出，请稍后...", HintType.Info)
            Dim ExistList As JArray = JArray.Parse(ReadFile(OutsidePath))
            Dim OutputList As JArray = New JArray
            Dim OutputNum As Integer = 0
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
                Else
                    NewProfile = New JObject From {
                                           {"uuid", Profile.Uuid},
                                           {"username", Profile.Username},
                                           {"type", "offline"}
                                       }
                End If
                OutputList.Add(NewProfile)
                OutputNum += 1
            Next
            For Each Profile In OutputList
                ExistList.Add(Profile)
            Next
            WriteFile(OutsidePath, ExistList.ToString())
            Hint($"已导出 {OutputNum} 个档案，部分档案可能需要重新验证密码！", HintType.Finish)
        End If
    End Sub
#End Region

#Region "离线 UUID 获取"
    ''' <summary>
    ''' 获取离线 UUID
    ''' </summary>
    ''' <param name="UserName">玩家 ID</param>
    ''' <param name="IsSplited">返回的 UUID 是否有连字符分割</param>
    ''' <param name="IsLegacy">是否使用旧版 PCL 生成方式，若为 True 则返回的 UUID 总是不带连字符</param>
    Public Function GetOfflineUuid(UserName As String, Optional IsSplited As Boolean = False, Optional IsLegacy As Boolean = False) As String
        If IsLegacy Then
            Dim FullUuid As String = StrFill(UserName.Length.ToString("X"), "0", 16) & StrFill(GetHash(UserName).ToString("X"), "0", 16)
            Return FullUuid.Substring(0, 12) & "3" & FullUuid.Substring(13, 3) & "9" & FullUuid.Substring(17, 15)
        Else
            Dim MD5 As New MD5CryptoServiceProvider
            Dim Hash As Byte() = MD5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + UserName))
            Hash(6) = Hash(6) And &HF
            Hash(6) = Hash(6) Or &H30
            Hash(8) = Hash(8) And &H3F
            Hash(8) = Hash(8) Or &H80
            Dim Parsed As New Guid(ToUuidString(Hash))
            ProfileLog("获取到离线 UUID: " & Parsed.ToString())
            If IsSplited Then
                Return Parsed.ToString()
            Else
                Return Parsed.ToString().Replace("-", "")
            End If
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

#Region "档案信息获取"
    ''' <summary>
    ''' 获取档案详情信息用于显示
    ''' </summary>
    ''' <param name="Profile">目标档案</param>
    ''' <returns>显示的详情信息</returns>
    Public Function GetProfileInfo(Profile As McProfile)
        Dim Info As String = Nothing
        If Profile.Type = 3 Then
            Info += "第三方验证"
            If Not String.IsNullOrWhiteSpace(Profile.ServerName) Then Info += $" / {Profile.ServerName}"
        ElseIf Profile.Type = 5 Then
            Info += "正版验证"
        Else
            Info += "离线验证"
        End If
        If Not String.IsNullOrWhiteSpace(Profile.Desc) Then Info += $"，{Profile.Desc}"
        Return Info
    End Function
    ''' <summary>
    ''' 获取当前档案的验证信息。
    ''' <param name="TargetAuthType">验证类型，若为新档案需填</param>
    ''' </summary>
    Public Function GetLoginData(Optional TargetAuthType As McLoginType = Nothing) As McLoginData
        Dim AuthType As McLoginType = Nothing
        If SelectedProfile Is Nothing Then '新档案
            If Not TargetAuthType = Nothing Then
                AuthType = TargetAuthType
            Else
                AuthType = McLoginType.Legacy
            End If
            If AuthType = McLoginType.Auth Then
                Return New McLoginServer(McLoginType.Auth) With {
                    .Description = "Authlib-Injector",
                    .Type = McLoginType.Auth,
                    .IsExist = (FrmLoginAuth Is Nothing)
                }
            ElseIf AuthType = McLoginType.Ms Then
                Return New McLoginMs
            Else
                Return New McLoginLegacy
            End If
        Else '已有档案
            AuthType = SelectedProfile.Type
            If AuthType = McLoginType.Auth Then
                Return New McLoginServer(McLoginType.Auth) With {
                    .BaseUrl = SelectedProfile.Server,
                    .UserName = SelectedProfile.Name,
                    .Password = SelectedProfile.Password,
                    .Description = "Authlib-Injector",
                    .Type = McLoginType.Auth,
                    .IsExist = (FrmLoginAuth Is Nothing)
                }
            ElseIf AuthType = McLoginType.Ms Then
                If McLoginMsLoader.State = LoadState.Finished Then
                    Return New McLoginMs With {.OAuthRefreshToken = SelectedProfile.RefreshToken,
                        .UserName = SelectedProfile.Username, .AccessToken = SelectedProfile.AccessToken,
                        .Uuid = SelectedProfile.Uuid, .ProfileJson = SelectedProfile.RawJson}
                Else
                    Return New McLoginMs With {.OAuthRefreshToken = SelectedProfile.RefreshToken, .UserName = SelectedProfile.Name}
                End If
            Else
                Return New McLoginLegacy With {.UserName = SelectedProfile.Username, .Uuid = SelectedProfile.Uuid}
            End If
        End If
        Return Nothing
    End Function
    ''' <summary>
    ''' 检查当前档案是否有效
    ''' </summary>
    ''' <returns>若档案验证有效，则返回空字符串，否则返回错误原因</returns>
    Public Function IsProfileVaild()
        Select Case SelectedProfile.Type
            Case McLoginType.Legacy
                If SelectedProfile.Username.Trim = "" Then Return "玩家名不能为空！"
                If SelectedProfile.Username.Contains("""") Then Return "玩家名不能包含英文引号！"
                If McVersionCurrent IsNot Nothing AndAlso
                   ((McVersionCurrent.Version.McCodeMain = 20 AndAlso McVersionCurrent.Version.McCodeSub >= 3) OrElse McVersionCurrent.Version.McCodeMain > 20) AndAlso
                   SelectedProfile.Username.Trim.Length > 16 Then
                    Return "自 1.20.3 起，玩家名至多只能包含 16 个字符！"
                End If
                Return ""
            Case McLoginType.Ms
                Return ""
            Case McLoginType.Auth
                Return ""
        End Select
        Return "未知的验证方式"
    End Function
#End Region

#Region "皮肤"
    Public IsMsSkinChanging As Boolean = False
    Public Sub ChangeSkinMs()
        '检查条件，获取新皮肤
        If IsMsSkinChanging Then
            Hint("正在更改皮肤中，请稍候！")
            Exit Sub
        End If
        If McLoginLoader.State = LoadState.Failed Then
            Hint("登录失败，无法更改皮肤！", HintType.Critical)
            Exit Sub
        End If
        Dim SkinInfo As McSkinInfo = McSkinSelect()
        If Not SkinInfo.IsVaild Then Exit Sub
        Hint("正在更改皮肤……")
        IsMsSkinChanging = True
        '开始实际获取
        RunInNewThread(
        Async Sub()
            Try
Retry:
                If McLoginMsLoader.State = LoadState.Loading Then McLoginMsLoader.WaitForExit() '等待登录结束
                Dim AccessToken As String = SelectedProfile.AccessToken
                Dim Uuid As String = SelectedProfile.Uuid

                Dim Client As New Net.Http.HttpClient With {.Timeout = New TimeSpan(0, 0, 30)}
                Client.DefaultRequestHeaders.Authorization = New Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken)
                Client.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
                Client.DefaultRequestHeaders.UserAgent.Add(New Net.Http.Headers.ProductInfoHeaderValue("MojangSharp", "0.1"))
                Dim Contents As New Net.Http.MultipartFormDataContent From {
                    {New Net.Http.StringContent(If(SkinInfo.IsSlim, "slim", "classic")), "variant"},
                    {New Net.Http.ByteArrayContent(ReadFileBytes(SkinInfo.LocalFile)), "file", GetFileNameFromPath(SkinInfo.LocalFile)}
                }
                Dim Result As String = Await (Await Client.PostAsync(New Uri("https://api.minecraftservices.com/minecraft/profile/skins"), Contents)).Content.ReadAsStringAsync
                If Result.Contains("request requires user authentication") Then
                    Hint("正在登录，将在登录完成后继续更改皮肤……")
                    McLoginMsLoader.Start(GetLoginData(), IsForceRestart:=True)
                    GoTo Retry
                ElseIf Result.Contains("""error""") Then
                    Hint("更改皮肤失败：" & GetJson(Result)("error"), HintType.Critical)
                    Exit Sub
                End If
                '获取新皮肤地址
                Log("[Skin] 皮肤修改返回值：" & vbCrLf & Result)
                Dim ResultJson As JObject = GetJson(Result)
                If ResultJson.ContainsKey("errorMessage") Then Throw New Exception(ResultJson("errorMessage").ToString) '#5309
                For Each Skin As JObject In ResultJson("skins")
                    If Skin("state").ToString = "ACTIVE" Then
                        MySkin.ReloadCache(Skin("url"))
                        Exit Sub
                    End If
                Next
                Throw New Exception("未知错误（" & Result & "）")
            Catch ex As Exception
                If ex.GetType.Equals(GetType(Tasks.TaskCanceledException)) Then
                    Hint("更改皮肤失败：与 Mojang 皮肤服务器的连接超时，请检查你的网络是否通畅！", HintType.Critical)
                Else
                    Log(ex, "更改皮肤失败", LogLevel.Hint)
                End If
            Finally
                IsMsSkinChanging = False
            End Try
        End Sub, "Ms Skin Upload")
    End Sub
#End Region

#Region "旧版迁移"
    ''' <summary>
    ''' 从旧版配置文件迁移档案
    ''' </summary>
    Public Sub MigrateOldProfile()
        ProfileLog("开始从旧版配置迁移档案")
        Dim ProfileCount As Integer = 0
        '正版档案
        If Not Setup.Get("LoginMsJson") = "{}" Then
            Dim OldMsJson As JObject = GetJson(Setup.Get("LoginMsJson"))
            ProfileLog($"找到 {OldMsJson.Count} 个旧版正版档案信息")
            For Each Profile In OldMsJson
                Dim NewProfile As New McProfile With {.Username = Profile.Key}
                ProfileList.Add(NewProfile)
                ProfileCount += 1
            Next
            SaveProfile()
            ProfileLog("旧版正版档案迁移完成")
            Setup.Reset("LoginMsJson")
        Else
            ProfileLog("无旧版正版档案信息")
        End If
        '离线档案
        If Not String.IsNullOrWhiteSpace(Setup.Get("LoginLegacyName")) Then
            Dim OldOfflineInfo As String() = Setup.Get("LoginLegacyName").Split("¨")
            ProfileLog($"找到 {OldOfflineInfo.Count} 个旧版离线档案信息")
            For Each OfflineId In OldOfflineInfo
                Dim NewProfile As New McProfile With {.Username = OfflineId, .Uuid = GetOfflineUuid(OfflineId, IsLegacy:=True)} '迁移的档案默认使用旧版 UUID 生成方式以避免存档丢失
                ProfileList.Add(NewProfile)
                ProfileCount += 1
            Next
            SaveProfile()
            ProfileLog("旧版离线档案迁移完成")
            Setup.Reset("LoginLegacyName")
        Else
            ProfileLog("无旧版离线档案信息")
        End If
        '第三方验证档案
        If Not (String.IsNullOrWhiteSpace(Setup.Get("LoginAuthName")) OrElse String.IsNullOrWhiteSpace(Setup.Get("CacheAuthUuid")) OrElse String.IsNullOrWhiteSpace(Setup.Get("CacheAuthServerServer")) OrElse String.IsNullOrWhiteSpace(Setup.Get("CacheAuthUsername")) OrElse String.IsNullOrWhiteSpace(Setup.Get("CacheAuthPass"))) Then
            ProfileLog($"找到旧版第三方验证档案信息")
            Dim NewProfile As New McProfile With {.Username = Setup.Get("CacheAuthName"), .Uuid = Setup.Get("CacheAuthUuid"),
                    .Name = Setup.Get("CacheAuthUsername"), .Password = Setup.Get("CacheAuthPass"), .Server = Setup.Get("CacheAuthServerServer") & "/authserver"}
            ProfileList.Add(NewProfile)
            SaveProfile()
            ProfileLog("旧版第三方验证档案迁移完成")
            ProfileCount += 1
            Setup.Reset("LoginAuthName")
            Setup.Reset("CacheAuthUuid")
            Setup.Reset("CacheAuthServerServer")
            Setup.Reset("CacheAuthUsername")
            Setup.Reset("CacheAuthPass")
        Else
            ProfileLog("无旧版第三方验证档案信息")
        End If
        If Not ProfileCount = 0 Then Hint($"已自动从旧版配置文件迁移档案，共迁移了 {ProfileCount} 个档案")
        ProfileLog("档案迁移结束")
    End Sub
#End Region

#Region "获取正版档案 UUID"
    ''' <summary>
    ''' 根据用户名返回对应 UUID，需要多线程
    ''' </summary>
    ''' <param name="Name">玩家 ID</param>
    Public Function McLoginMojangUuid(Name As String, ThrowOnNotFound As Boolean)
        If Name.Trim.Length = 0 Then Return StrFill("", "0", 32)
        '从缓存获取
        Dim Uuid As String = ReadIni(PathTemp & "Cache\Uuid\Mojang.ini", Name, "")
        If Len(Uuid) = 32 Then Return Uuid
        '从官网获取
        Try
            Dim GotJson As JObject = NetGetCodeByRequestRetry("https://api.mojang.com/users/profiles/minecraft/" & Name, IsJson:=True)
            If GotJson Is Nothing Then Throw New FileNotFoundException("正版玩家档案不存在（" & Name & "）")
            Uuid = If(GotJson("id"), "")
        Catch ex As Exception
            Log(ex, "从官网获取正版 UUID 失败（" & Name & "）")
            If Not ThrowOnNotFound AndAlso ex.GetType.Name = "FileNotFoundException" Then
                Uuid = GetOfflineUuid(Name, IsLegacy:=True) '玩家档案不存在
            Else
                Throw New Exception("从官网获取正版 UUID 失败", ex)
            End If
        End Try
        '写入缓存
        If Not Len(Uuid) = 32 Then Throw New Exception("获取的正版 UUID 长度不足（" & Uuid & "）")
        WriteIni(PathTemp & "Cache\Uuid\Mojang.ini", Name, Uuid)
        Return Uuid
    End Function
#End Region

End Module