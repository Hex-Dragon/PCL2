Imports System.IO.Compression

Public Module ModMod

    Public Class McMod

#Region "基础"

        ''' <summary>
        ''' Mod 文件的地址。
        ''' </summary>
        Public ReadOnly Path As String
        Public Sub New(Path As String)
            Me.Path = If(Path, "")
        End Sub

        ''' <summary>
        ''' Mod 的完整文件名。
        ''' </summary>
        Public ReadOnly Property FileName As String
            Get
                Return GetFileNameFromPath(Path)
            End Get
        End Property

        ''' <summary>
        ''' Mod 的完整文件名，去除最后的 .disabled。
        ''' </summary>
        Public ReadOnly Property RawFileName As String
            Get
                Return FileName.Replace(".disabled", "")
            End Get
        End Property

        ''' <summary>
        ''' Mod 的状态。
        ''' </summary>
        Public ReadOnly Property State As McModState
            Get
                Load()
                If Not IsFileAvailable Then
                    Return McModState.Unavaliable
                ElseIf Path.EndsWith(".disabled") Then
                    Return McModState.Disabled
                Else
                    Return McModState.Fine
                End If
            End Get
        End Property
        Public Enum McModState As Integer
            Fine = 0
            Disabled = 1
            Unavaliable = 2
        End Enum

#End Region

#Region "信息项"

        ''' <summary>
        ''' Mod 的名称。若不可用则为 ModID 或无扩展的文件名。
        ''' </summary>
        Public Property Name As String
            Get
                If _Name Is Nothing Then Load()
                If _Name Is Nothing Then _Name = _ModId
                If _Name Is Nothing Then _Name = GetFileNameWithoutExtentionFromPath(Path)
                Return _Name
            End Get
            Set(value As String)
                If _Name Is Nothing AndAlso value IsNot Nothing AndAlso
                   value.ToLower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                    _Name = value
                End If
            End Set
        End Property
        Private _Name As String = Nothing

        ''' <summary>
        ''' Mod 的描述信息。
        ''' </summary>
        Public Property Description As String
            Get
                If _Description Is Nothing Then Load()
                If _Description Is Nothing AndAlso FileUnavailableReason IsNot Nothing Then _Description = FileUnavailableReason.Message
                'If _Description Is Nothing Then _Description = Path
                Return _Description
            End Get
            Set(value As String)
                If _Description Is Nothing AndAlso value IsNot Nothing AndAlso value.Count > 2 Then
                    _Description = value.ToString.Trim(vbLf)
                    '优化显示：若以 [a-zA-Z0-9] 结尾，加上小数点句号
                    If _Description.ToLower.LastIndexOfAny("qwertyuiopasdfghjklzxcvbnm0123456789") = _Description.Count - 1 Then _Description += "."
                End If
            End Set
        End Property
        Private _Description As String = Nothing

        ''' <summary>
        ''' Mod 的版本，不保证符合版本格式规范。
        ''' </summary>
        Public Property Version As String
            Get
                If _Version Is Nothing Then Load()
                Return _Version
            End Get
            Set(value As String)
                If _Version IsNot Nothing AndAlso (_Version.Contains(".") OrElse _Version.Contains("-")) Then Exit Property
                If value IsNot Nothing AndAlso value.ToLower.Contains("version") Then value = "version" '需要修改的标识
                _Version = value
            End Set
        End Property
        Private _Version As String = Nothing

        ''' <summary>
        ''' 用于依赖检查的 ModID。
        ''' </summary>
        Public Property ModId As String
            Get
                If _ModId Is Nothing Then Load()
                Return _ModId
            End Get
            Set(value As String)
                If value Is Nothing Then Exit Property
                value = RegexSeek(value.ToLower, "[0-9a-z_-]+")
                If value IsNot Nothing AndAlso value.ToLower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                    If Not PossibleModId.Contains(value) Then PossibleModId.Add(value)
                    If _ModId Is Nothing Then _ModId = value
                End If
            End Set
        End Property
        Private _ModId As String = Nothing
        ''' <summary>
        ''' 其他可能的 ModID。
        ''' </summary>
        Public PossibleModId As New List(Of String)

        ''' <summary>
        ''' Mod 的主页。
        ''' </summary>
        Public Property Url As String
            Get
                If _Url Is Nothing Then Load()
                Return _Url
            End Get
            Set(value As String)
                If _Url Is Nothing AndAlso value IsNot Nothing AndAlso value.StartsWith("http") Then
                    _Url = value
                End If
            End Set
        End Property
        Private _Url As String = Nothing

        ''' <summary>
        ''' Mod 的作者列表。
        ''' </summary>
        Public Property Authors As String
            Get
                If _Authors Is Nothing Then Load()
                Return _Authors
            End Get
            Set(value As String)
                If _Authors Is Nothing AndAlso Not String.IsNullOrWhiteSpace(value) Then
                    _Authors = value
                End If
            End Set
        End Property
        Private _Authors As String = Nothing

        ''' <summary>
        ''' 依赖项，其中包括了 Minecraft 的版本要求。格式为 ModID - VersionRequirement，若无版本要求则为 Nothing。
        ''' </summary>
        Public ReadOnly Property Dependencies As Dictionary(Of String, String)
            Get
                Load()
                Return _Dependencies
            End Get
        End Property
        Private _Dependencies As New Dictionary(Of String, String)
        Private Sub AddDependency(ModID As String, Optional VersionRequirement As String = Nothing)
            '确保信息正确
            If ModID Is Nothing OrElse ModID.Count < 2 Then Exit Sub
            ModID = ModID.ToLower
            If ModID = "name" OrElse Val(ModID).ToString = ModID Then Exit Sub '跳过 name 与纯数字 id
            If VersionRequirement Is Nothing OrElse ((Not VersionRequirement.Contains(".")) AndAlso (Not VersionRequirement.Contains("-"))) OrElse VersionRequirement.Contains("$") Then
                VersionRequirement = Nothing
            Else
                If (Not VersionRequirement.StartsWith("[")) AndAlso (Not VersionRequirement.StartsWith("(")) AndAlso (Not VersionRequirement.EndsWith("]")) AndAlso (Not VersionRequirement.EndsWith(")")) Then VersionRequirement = "[" & VersionRequirement & ",)"
            End If
            '向依赖项中添加
            If _Dependencies.ContainsKey(ModID) Then
                If _Dependencies(ModID) Is Nothing Then _Dependencies(ModID) = VersionRequirement
            Else
                _Dependencies.Add(ModID, VersionRequirement)
            End If
        End Sub

#End Region

#Region "加载步骤标记"

        '1. 进行文件可用性检查
        '   成功：继续第二步。
        '   失败：标记 FileUnavailableReason， 并停止后续加载。
        ''' <summary>
        ''' 是否已进行 Mod 文件的基础加载。（这包括第一步和第二步）
        ''' </summary>
        Private IsLoaded As Boolean = False
        ''' <summary>
        ''' Mod 文件是否可被正常读取。
        ''' </summary>
        Public ReadOnly Property IsFileAvailable As Boolean
            Get
                Load()
                Return FileUnavailableReason Is Nothing
            End Get
        End Property
        ''' <summary>
        ''' Mod 文件出错的原因。若无错误，则为 Nothing。
        ''' </summary>
        Public ReadOnly Property FileUnavailableReason As Exception
            Get
                Load()
                Return _FileUnavailableReason
            End Get
        End Property
        Private _FileUnavailableReason As Exception = Nothing

        '2. 进行 .class 以外的信息获取
        '   成功：标记 IsInfoWithoutClassAvailable。
        '   失败：什么也不干。如果需要补充信息的话，检测到 IsInfoWithoutClassAvailable 为 False，会自动继续加载。
        ''' <summary>
        ''' 是否已在不获取 .class 文件的前提下完成了所需信息的加载。
        ''' </summary>
        Private IsInfoWithoutClassAvailable As Boolean = False

        '3. 尝试从 .class 文件中获取信息
        '   成功：标记 IsInfoWithClassAvailable。
        '   失败：什么也不干。
        ''' <summary>
        ''' 是否已进行 .class 文件的信息获取。
        ''' </summary>
        Private IsInfoWithClassLoaded As Boolean = False
        ''' <summary>
        ''' 是否已在 .class 文件中完成了所需信息的加载。
        ''' </summary>
        Private IsInfoWithClassAvailable As Boolean = False

#End Region

#Region "加载"

        ''' <summary>
        ''' 初始化所有数据。
        ''' </summary>
        Private Sub Init()
            _Name = Nothing
            _Description = Nothing
            _Version = Nothing
            _ModId = Nothing
            PossibleModId = New List(Of String)
            _Dependencies = New Dictionary(Of String, String)
            IsLoaded = False
            _FileUnavailableReason = Nothing
            IsInfoWithoutClassAvailable = False
            IsInfoWithClassLoaded = False
            IsInfoWithClassAvailable = False
        End Sub

        ''' <summary>
        ''' 进行文件可用性检查与 .class 以外的信息获取。
        ''' </summary>
        Public Sub Load(Optional ForceReload As Boolean = False)
            If IsLoaded AndAlso Not ForceReload Then Exit Sub
            '初始化
            Init()
            Dim Jar As ZipArchive = Nothing
            Try
                '阶段 1：基础可用性检查、打开 Jar 文件
                If Path.Length < 2 Then Throw New FileNotFoundException("错误的 Mod 文件路径（" & If(Path, "null") & "）")
                If Not File.Exists(Path) Then Throw New FileNotFoundException("未找到 Mod 文件（" & Path & "）")
                Jar = New ZipArchive(New FileStream(Path, FileMode.Open))
                If Jar.Entries.Count = 0 Then Throw New FileFormatException("文件内容为空")
                '阶段 2：信息获取
                LoadWithoutClass(Jar)
                '阶段 3: Class 信息获取
                If Not IsInfoWithoutClassAvailable Then LoadWithClass(Jar)
            Catch ex As UnauthorizedAccessException
                Log(ex, "Mod 文件由于无权限无法打开（" & Path & "）", LogLevel.Developer)
                _FileUnavailableReason = New UnauthorizedAccessException("没有读取此文件的权限，请尝试右键以管理员身份运行 PCL", ex)
            Catch ex As Exception
                Log(ex, "Mod 文件无法打开（" & Path & "）", LogLevel.Developer)
                _FileUnavailableReason = ex
            Finally
                If Jar IsNot Nothing Then Jar.Dispose()
            End Try
            '完成标记
            IsLoaded = True
        End Sub

        ''' <summary>
        ''' 进行不使用 .class 文件的信息获取。
        ''' </summary>
        Private Sub LoadWithoutClass(Jar As ZipArchive)

#Region "尝试使用 mcmod.info"
            Try
                '获取信息文件
                Dim InfoEntry As ZipArchiveEntry = Jar.GetEntry("mcmod.info")
                Dim InfoString As String = Nothing
                If InfoEntry IsNot Nothing Then
                    InfoString = ReadFile(InfoEntry.Open())
                    If InfoString.Length < 15 Then InfoString = Nothing
                End If
                If InfoString Is Nothing Then Exit Try
                '获取可用 Json 项
                Dim InfoObject As JObject
                Dim JsonObject = GetJson(InfoString)
                If JsonObject.Type = JTokenType.Array Then
                    InfoObject = JsonObject(0)
                Else
                    InfoObject = JsonObject("modList")(0)
                End If
                '从文件中获取 Mod 信息项
                Name = InfoObject("name")
                Description = InfoObject("description")
                Version = InfoObject("version")
                Url = InfoObject("url")
                ModId = InfoObject("modid")
                Dim AuthorJson As JArray = InfoObject("authorList")
                If AuthorJson IsNot Nothing Then
                    Dim Author As New List(Of String)
                    For Each Token In AuthorJson
                        Author.Add(Token.ToString)
                    Next
                    If Author.Count > 0 Then Authors = Join(Author, ", ")
                End If
                Dim Reqs As JArray = InfoObject("requiredMods")
                If Reqs IsNot Nothing Then
                    For Each Token As String In Reqs
                        If Not String.IsNullOrEmpty(Token) Then
                            Token = Token.Substring(Token.IndexOf(":") + 1)
                            If Token.Contains("@") Then
                                AddDependency(Token.Split("@")(0), Token.Split("@")(1))
                            Else
                                AddDependency(Token)
                            End If
                        End If
                    Next
                End If
                Reqs = InfoObject("dependancies")
                If Reqs IsNot Nothing Then
                    For Each Token As String In Reqs
                        If Not String.IsNullOrEmpty(Token) Then
                            Token = Token.Substring(Token.IndexOf(":") + 1)
                            If Token.Contains("@") Then
                                AddDependency(Token.Split("@")(0), Token.Split("@")(1))
                            Else
                                AddDependency(Token)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                Log(ex, "读取 mcmod.info 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
#Region "尝试使用 mods.toml"
            Try
                '获取 mods.toml 文件
                Dim TomlEntry As ZipArchiveEntry = Jar.GetEntry("META-INF/mods.toml")
                Dim TomlText As String = Nothing
                If TomlEntry IsNot Nothing Then
                    TomlText = ReadFile(TomlEntry.Open())
                    If TomlText.Length < 15 Then TomlText = Nothing
                End If
                If TomlText Is Nothing Then Exit Try
                '文件标准化：统一换行符为 vbLf，去除注释、头尾的空格、空行
                Dim Lines As New List(Of String)
                For Each Line In TomlText.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(vbLf) '统一换行符
                    If Line.StartsWith("#") Then '去除注释
                        Continue For
                    ElseIf Line.Contains("#") Then
                        Line = Line.Substring(0, Line.IndexOf("#"))
                    End If
                    Line = Line.Trim(New Char() {" "c, "	"c, "　"c}) '去除头尾的空格
                    If Line.Count > 0 Then Lines.Add(Line) '去除空行
                Next
                '读取文件数据
                Dim TomlData As New List(Of KeyValuePair(Of String, Dictionary(Of String, Object))) From {New KeyValuePair(Of String, Dictionary(Of String, Object))("", New Dictionary(Of String, Object))}
                For i = 0 To Lines.Count - 1
                    Dim Line As String = Lines(i)
                    If Line.StartsWith("[") AndAlso Line.EndsWith("]") Then
                        '段落标记
                        Dim Header = Line.Trim("[]".ToCharArray)
                        TomlData.Add(New KeyValuePair(Of String, Dictionary(Of String, Object))(Header, New Dictionary(Of String, Object)))
                    ElseIf Line.Contains("=") Then
                        '字段标记
                        Dim Key As String = Line.Substring(0, Line.IndexOf("=")).TrimEnd(New Char() {" "c, "	"c, "　"c})
                        Dim RawValue As String = Line.Substring(Line.IndexOf("=") + 1).TrimStart(New Char() {" "c, "	"c, "　"c})
                        Dim Value As Object
                        If RawValue.StartsWith("""") AndAlso RawValue.EndsWith("""") Then
                            '单行字符串
                            Value = RawValue.Trim("""")
                        ElseIf RawValue.StartsWith("'''") Then
                            '多行字符串
                            Dim ValueLines As New List(Of String) From {RawValue.TrimStart("'")}
                            If ValueLines(0).EndsWith("'''") Then '把多行字符串按单行写法写的错误处理（#2732）
                                ValueLines(0) = ValueLines(0).TrimEnd("'")
                            Else
                                Do Until i >= Lines.Count - 1
                                    i += 1
                                    Dim ValueLine As String = Lines(i)
                                    If ValueLine.EndsWith("'''") Then
                                        ValueLines.Add(ValueLine.TrimEnd("'"))
                                        Exit Do
                                    Else
                                        ValueLines.Add(ValueLine)
                                    End If
                                Loop
                            End If
                            Value = Join(ValueLines, vbLf).Trim(vbLf).Replace(vbLf, vbCrLf)
                        ElseIf RawValue.ToLower = "true" OrElse RawValue.ToLower = "false" Then
                            '布尔型
                            Value = (RawValue.ToLower = "true")
                        ElseIf Val(RawValue).ToString = RawValue Then
                            '数字型
                            Value = Val(RawValue)
                        Else
                            '不知道是个啥玩意儿，直接存储
                            Value = RawValue
                        End If
                        TomlData.Last.Value(Key) = Value
                    Else
                        '不知道是个啥玩意儿
                        Exit Try
                    End If
                Next
                '从文件数据中获取信息
                Dim ModEntry As Dictionary(Of String, Object) = Nothing
                For Each TomlSubData In TomlData
                    If TomlSubData.Key = "mods" Then
                        ModEntry = TomlSubData.Value
                        Exit For
                    End If
                Next
                If ModEntry Is Nothing OrElse Not ModEntry.ContainsKey("modId") Then Exit Try
                ModId = ModEntry("modId")
                If _ModId Is Nothing Then Exit Try '设置了无效的 ModID
                If ModEntry.ContainsKey("displayName") Then Name = ModEntry("displayName")
                If ModEntry.ContainsKey("description") Then Description = ModEntry("description")
                If ModEntry.ContainsKey("version") Then Version = ModEntry("version")
                If TomlData(0).Value.ContainsKey("displayURL") Then Url = TomlData(0).Value("displayURL")
                If TomlData(0).Value.ContainsKey("authors") Then Authors = TomlData(0).Value("authors")
                For Each TomlSubData In TomlData
                    If TomlSubData.Key.ToLower = $"dependencies.{ModId.ToLower}" Then
                        Dim DepEntry As Dictionary(Of String, Object) = TomlSubData.Value
                        If DepEntry.ContainsKey("modId") AndAlso DepEntry.ContainsKey("mandatory") AndAlso DepEntry("mandatory") AndAlso
                           DepEntry.ContainsKey("side") AndAlso Not DepEntry("side").ToString.ToLower = "server" Then
                            AddDependency(DepEntry("modId"), If(DepEntry.ContainsKey("versionRange"), DepEntry("versionRange"), Nothing))
                        End If
                    End If
                Next
                '加载成功
                GoTo Finished
            Catch ex As Exception
                Log(ex, "读取 mods.toml 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
#Region "尝试使用 fabric.mod.json"
            Try
                '获取 fabric.mod.json 文件
                Dim FabricEntry As ZipArchiveEntry = Jar.GetEntry("fabric.mod.json")
                Dim FabricText As String = Nothing
                If FabricEntry IsNot Nothing Then
                    FabricText = ReadFile(FabricEntry.Open(), Encoding.UTF8)
                    If Not FabricText.Contains("schemaVersion") Then FabricText = Nothing
                End If
                If FabricText Is Nothing Then Exit Try
                Dim FabricObject As JObject = GetJson(FabricText)
GotFabric:
                '从文件中获取 Mod 信息项
                If FabricObject.ContainsKey("name") Then Name = FabricObject("name")
                If FabricObject.ContainsKey("version") Then Version = FabricObject("version")
                If FabricObject.ContainsKey("description") Then Description = FabricObject("description")
                If FabricObject.ContainsKey("id") Then ModId = FabricObject("id")
                If FabricObject.ContainsKey("contact") Then Url = If(FabricObject("contact")("homepage"), "")
                Dim AuthorJson As JArray = FabricObject("authors")
                If AuthorJson IsNot Nothing Then
                    Dim Author As New List(Of String)
                    For Each Token In AuthorJson
                        Author.Add(Token.ToString)
                    Next
                    If Author.Count > 0 Then Authors = Join(Author, ", ")
                End If
                'If (Not FabricObject.ContainsKey("serverSideOnly")) OrElse FabricObject("serverSideOnly")("value").ToObject(Of Boolean) = False Then
                '    '添加 Minecraft 依赖
                '    Dim DepMinecraft As String = If(If(FabricObject("acceptedMinecraftVersions") IsNot Nothing, FabricObject("acceptedMinecraftVersions")("value"), ""), "")
                '    If DepMinecraft <> "" Then AddDependency("minecraft", DepMinecraft)
                '    '添加其他依赖
                '    Dim Deps As String = If(If(FabricObject("dependencies") IsNot Nothing, FabricObject("dependencies")("value"), ""), "")
                '    If Deps <> "" Then
                '        For Each Dep In Deps.Split(";")
                '            If Dep = "" OrElse Not Dep.StartsWith("required-") Then Continue For
                '            Dep = Dep.Substring(Dep.IndexOf(":") + 1)
                '            If Dep.Contains("@") Then
                '                AddDependency(Dep.Split("@")(0), Dep.Split("@")(1))
                '            Else
                '                AddDependency(Dep)
                '            End If
                '        Next
                '    End If
                'End If
                '加载成功
                GoTo Finished
            Catch ex As Exception
                Log(ex, "读取 fabric.mod.json 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
#Region "尝试使用 fml_cache_annotation.json"
            Try
                '获取 fml_cache_annotation.json 文件
                Dim FmlEntry As ZipArchiveEntry = Jar.GetEntry("META-INF/fml_cache_annotation.json")
                Dim FmlText As String = Nothing
                If FmlEntry IsNot Nothing Then
                    FmlText = ReadFile(FmlEntry.Open(), Encoding.UTF8)
                    If Not FmlText.Contains("Lnet/minecraftforge/fml/common/Mod;") Then FmlText = Nothing
                End If
                If FmlText Is Nothing Then Exit Try
                Dim FmlJson As JObject = GetJson(FmlText)
                '获取可用 Json 项
                Dim FmlObject As JObject = Nothing
                For Each ModFilePair In FmlJson
                    Dim ModFileAnnos As JArray = ModFilePair.Value("annotations")
                    If ModFileAnnos IsNot Nothing Then
                        '先获取 Mod
                        For Each ModFileAnno In ModFileAnnos
                            Dim Name As String = If(ModFileAnno("name"), "")
                            If Name = "Lnet/minecraftforge/fml/common/Mod;" Then
                                FmlObject = ModFileAnno("values")
                                GoTo Got
                            End If
                        Next
                    End If
                Next
                Exit Try
Got:
                '从文件中获取 Mod 信息项
                If FmlObject.ContainsKey("useMetadata") AndAlso If(FmlObject("useMetadata")("value"), "").ToString.ToLower = "true" Then
                    '要求使用 mcmod.info 中的信息
                    Dim value As String = FmlObject("modid")("value")
                    If value Is Nothing Then Exit Try
                    value = RegexSeek(value.ToLower, "[0-9a-z_]+")
                    If value IsNot Nothing AndAlso value.ToLower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                        If Not PossibleModId.Contains(value) Then PossibleModId.Add(value)
                    End If
                    Exit Try
                End If
                If FmlObject.ContainsKey("name") Then Name = FmlObject("name")("value")
                If FmlObject.ContainsKey("version") Then Version = FmlObject("version")("value")
                If FmlObject.ContainsKey("modid") Then ModId = FmlObject("modid")("value")
                If (Not FmlObject.ContainsKey("serverSideOnly")) OrElse FmlObject("serverSideOnly")("value").ToObject(Of Boolean) = False Then
                    '添加 Minecraft 依赖
                    Dim DepMinecraft As String = If(If(FmlObject("acceptedMinecraftVersions") IsNot Nothing, FmlObject("acceptedMinecraftVersions")("value"), ""), "")
                    If DepMinecraft <> "" Then AddDependency("minecraft", DepMinecraft)
                    '添加其他依赖
                    Dim Deps As String = If(If(FmlObject("dependencies") IsNot Nothing, FmlObject("dependencies")("value"), ""), "")
                    If Deps <> "" Then
                        For Each Dep In Deps.Split(";")
                            If Dep = "" OrElse Not Dep.StartsWith("required-") Then Continue For
                            Dep = Dep.Substring(Dep.IndexOf(":") + 1)
                            If Dep.Contains("@") Then
                                AddDependency(Dep.Split("@")(0), Dep.Split("@")(1))
                            Else
                                AddDependency(Dep)
                            End If
                        Next
                    End If
                End If
            Catch ex As Exception
                Log(ex, "读取 fml_cache_annotation.json 时出现未知错误（" & Path & "）", LogLevel.Developer)
            End Try
#End Region
Finished:
#Region "将 Version 代号转换为 META-INF 中的版本"
            If _Version = "version" Then
                Try
                    Dim MetaEntry As ZipArchiveEntry = Jar.GetEntry("META-INF/MANIFEST.MF")
                    If MetaEntry IsNot Nothing Then
                        Dim MetaString As String = ReadFile(MetaEntry.Open()).Replace(" :", ":").Replace(": ", ":")
                        If MetaString.Contains("Implementation-Version:") Then
                            MetaString = MetaString.Substring(MetaString.IndexOf("Implementation-Version:") + "Implementation-Version:".Count)
                            MetaString = MetaString.Substring(0, MetaString.IndexOfAny(vbCrLf.ToCharArray)).Trim
                            Version = MetaString
                        End If
                    End If
                Catch ex As Exception
                    Log("获取 META-INF 中的版本信息失败（" & Path & "）", LogLevel.Developer)
                    Version = Nothing
                End Try
            End If
            If _Version IsNot Nothing AndAlso Not (_Version.Contains(".") OrElse _Version.Contains("-")) Then Version = Nothing
#End Region

            IsInfoWithoutClassAvailable = _ModId IsNot Nothing AndAlso _Version IsNot Nothing
        End Sub

        ''' <summary>
        ''' 进行使用 .class 文件的信息获取。
        ''' </summary>
        Private Sub LoadWithClass(Jar As ZipArchive)
            Try
                '查找入口点文件
                Dim ModClass As String = Nothing
                Dim ModClassNotBest As String = Nothing '非完美匹配
                For Each Entry In Jar.Entries
                    If Entry.Name.EndsWith(".class") Then
                        Dim Temp As String = ReadFile(Entry.Open()).ToLower
                        If Temp.Contains("#lnet/minecraftforge/fml/common/mod;") Then
                            ModClass = Temp
                            Exit For
                        ElseIf Temp.Contains("modid") Then
                            ModClassNotBest = Temp
                        End If
                    End If
                Next
                If ModClass Is Nothing Then ModClass = ModClassNotBest
                If ModClass Is Nothing Then Throw New FileNotFoundException("未找到 Mod 入口点")
                ModClass = ModClass.Replace("ljava/lang/string;", "")
                If ModClass.Count > 3000 Then ModClass = ModClass.Substring(0, 3000) '如果文件过大，截取前 3000 Byte
                Dim IndexHead As Integer, IndexTail As Integer, IncreaseCount As Integer
                '获取 ModID
                If _ModId Is Nothing Then
                    IndexHead = ModClass.IndexOf("modid") + "modid".Length + 1
                    IncreaseCount = 0
                    Do While Convert.ToInt32(ModClass(IndexHead)) < 32
                        IndexHead += 1
                        IncreaseCount += 1
                        If IncreaseCount > 10 Then Throw New Exception("ModID 头匹配失败")
                    Loop
                    IndexTail = IndexHead + 1
                    IncreaseCount = 0
                    Do Until Convert.ToInt32(ModClass(IndexTail)) < 32
                        IndexTail += 1
                        IncreaseCount += 1
                        If IncreaseCount > 50 Then Throw New Exception("ModID 尾匹配失败")
                    Loop
                    ModId = ModClass.Substring(IndexHead, IndexTail - IndexHead)
                End If
                '获取 Version
                If _Version Is Nothing AndAlso ModClass.Contains("version") Then
                    IndexHead = ModClass.IndexOf("version") + "version".Length + 1
                    IncreaseCount = 0
                    Do While Convert.ToInt32(ModClass(IndexHead)) < 32
                        IndexHead += 1
                        IncreaseCount += 1
                        If IncreaseCount > 10 Then GoTo VersionFindFail
                    Loop
                    IndexTail = IndexHead + 1
                    IncreaseCount = 0
                    Do While ModClass(IndexTail) = "."c OrElse ModClass(IndexTail) = "-"c OrElse
                         (ModClass(IndexTail) >= "0"c AndAlso ModClass(IndexTail) <= "9"c) OrElse
                         (ModClass(IndexTail) >= "a"c AndAlso ModClass(IndexTail) <= "z"c)
                        IndexTail += 1
                        IncreaseCount += 1
                        If IncreaseCount > 50 Then GoTo VersionFindFail
                    Loop
                    _Version = ModClass.Substring(IndexHead, IndexTail - IndexHead)
                End If
VersionFindFail:
                '获取 Dependencies
                IndexHead = ModClass.IndexOf("dependencies")
                If IndexHead > 0 Then
                    If ModClass.Count >= IndexHead + 300 Then ModClass = ModClass.Substring(IndexHead, 299)
                    Dim Deps As List(Of String) = RegexSearch(ModClass, "(?<=required-((before|after|before-client|after-client)?):)[0-9a-z]+(@[\(\[]{1}[0-9.,]+[\)\]]{1})?")
                    For Each Token As String In Deps
                        If Not String.IsNullOrEmpty(Token) Then
                            If Token.Contains("@") Then
                                AddDependency(Token.Split("@")(0), Token.Split("@")(1))
                            Else
                                AddDependency(Token)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                Log(ex, "Mod Class 信息不可用（" & If(Path, "null") & "）", LogLevel.Normal)
            End Try
            IsInfoWithClassAvailable = _ModId IsNot Nothing AndAlso _Version IsNot Nothing
        End Sub

#End Region

#Region "网络信息"

        ''' <summary>
        ''' 该 Mod 关联的网络项目。
        ''' </summary>
        Public Property Comp As CompProject
            Get
                Return _Comp
            End Get
            Set(value As CompProject)
                _Comp = value
                RaiseEvent OnGetCompProject(Me)
            End Set
        End Property
        Private _Comp As CompProject
        Public IsCompFromModrinth = False
        Public Event OnGetCompProject(sender As McMod)

        ''' <summary>
        ''' 获取用于 CurseForge 信息获取的 Hash 值（MurmurHash2）。
        ''' </summary>
        Public ReadOnly Property CurseForgeHash As UInteger
            Get
                If _CurseForgeHash Is Nothing Then
                    '读取文件
                    Dim data As New List(Of Byte)
                    For Each b As Byte In File.ReadAllBytes(Path)
                        If b = 9 OrElse b = 10 OrElse b = 13 OrElse b = 32 Then Continue For
                        data.Add(b)
                    Next
                    '计算 MurmurHash2
                    Dim length As Integer = data.Count
                    Dim h As UInteger = 1 Xor length '1 是种子
                    Dim i As Integer
                    For i = 0 To length - 4 Step 4
                        Dim k As UInteger = data(i) Or CUInt(data(i + 1)) << 8 Or CUInt(data(i + 2)) << 16 Or CUInt(data(i + 3)) << 24
                        k = (k * &H5BD1E995L) And &HFFFFFFFFL
                        k = k Xor (k >> 24)
                        k = (k * &H5BD1E995L) And &HFFFFFFFFL
                        h = (h * &H5BD1E995L) And &HFFFFFFFFL
                        h = h Xor k
                    Next
                    Select Case length - i
                        Case 3
                            h = h Xor (data(i) Or CUInt(data(i + 1)) << 8)
                            h = h Xor (CUInt(data(i + 2)) << 16)
                            h = (h * &H5BD1E995L) And &HFFFFFFFFL
                        Case 2
                            h = h Xor (data(i) Or CUInt(data(i + 1)) << 8)
                            h = (h * &H5BD1E995L) And &HFFFFFFFFL
                        Case 1
                            h = h Xor data(i)
                            h = (h * &H5BD1E995L) And &HFFFFFFFFL
                    End Select
                    h = h Xor (h >> 13)
                    h = (h * &H5BD1E995L) And &HFFFFFFFFL
                    h = h Xor (h >> 15)
                    _CurseForgeHash = h
                End If
                Return _CurseForgeHash
            End Get
        End Property
        Private _CurseForgeHash As UInteger?

        ''' <summary>
        ''' 获取用于 Modrinth 信息获取的 Hash 值（SHA1）。
        ''' </summary>
        Public ReadOnly Property ModrinthHash As String
            Get
                If _ModrinthHash Is Nothing Then
                    _ModrinthHash = GetFileSHA1(Path)
                End If
                Return _ModrinthHash
            End Get
        End Property
        Private _ModrinthHash As String

#End Region

        ''' <summary>
        ''' 是否可能为前置 Mod。
        ''' </summary>
        Public Function IsPresetMod() As Boolean
            Return Dependencies.Count = 0 AndAlso (Name IsNot Nothing) AndAlso (Name.ToLower.Contains("core") OrElse Name.ToLower.Contains("lib"))
        End Function

        ''' <summary>
        ''' 根据完整文件路径的文件扩展名判断是否为 Mod 文件。
        ''' </summary>
        Public Shared Function IsModFile(Path As String)
            If Path Is Nothing OrElse Not Path.Contains(".") Then Return False
            Path = Path.ToLower
            If Path.EndsWith(".jar") OrElse Path.EndsWith(".zip") OrElse Path.EndsWith(".litemod") OrElse
               Path.EndsWith(".jar.disabled") OrElse Path.EndsWith(".zip.disabled") OrElse Path.EndsWith(".litemod.disabled") Then Return True
            Return False
        End Function

    End Class

    '加载 Mod 列表
    Public McModLoader As New LoaderTask(Of String, List(Of McMod))("Mod List Loader", AddressOf McModLoad)
    Private Sub McModLoad(Loader As LoaderTask(Of String, List(Of McMod)))
        Try
            RunInUiWait(Sub() If FrmVersionMod IsNot Nothing Then FrmVersionMod.Load.ShowProgress = False)

            '获取 Mod 文件夹下的可用文件列表
            Dim ModFileList As New List(Of FileInfo)
            If Directory.Exists(Loader.Input) Then
                Dim RawName As String = Loader.Input.ToLower
                For Each File As FileInfo In EnumerateFiles(Loader.Input)
                    If File.DirectoryName.ToLower & "\" <> RawName Then
                        '仅当 Forge 1.13- 且文件夹名与版本号相同时，才加载该子文件夹下的 Mod
                        If Not (PageVersionLeft.Version IsNot Nothing AndAlso PageVersionLeft.Version.Version.HasForge AndAlso
                                PageVersionLeft.Version.Version.McCodeMain < 13 AndAlso
                                File.Directory.Name = "1." & PageVersionLeft.Version.Version.McCodeMain & "." & PageVersionLeft.Version.Version.McCodeSub) Then
                            Continue For
                        End If
                    End If
                    If McMod.IsModFile(File.FullName) Then ModFileList.Add(File)
                Next
            End If

            '确定是否显示进度
            Loader.Progress = 0.05
            If ModFileList.Count > 50 Then RunInUi(Sub() If FrmVersionMod IsNot Nothing Then FrmVersionMod.Load.ShowProgress = True)

            '获取本地文件缓存
            Dim CachePath As String = PathTemp & "Cache\LocalMod.json"
            Dim Cache As New JObject
            Try
                Dim CacheContent As String = ReadFile(CachePath)
                If CacheContent <> "" Then
                    Cache = GetJson(CacheContent)
                    If Not Cache.ContainsKey("version") OrElse Cache("version").ToObject(Of Integer) <> LocalModCacheVersion Then
                        Log($"[Mod] 本地 Mod 信息缓存版本已过期，将弃用这些缓存信息")
                        Cache = New JObject
                    End If
                End If
            Catch ex As Exception
                Log(ex, "读取本地 Mod 信息缓存失败，已重置")
            End Try
            Cache("version") = LocalModCacheVersion

            '加载 Mod 列表
            Dim ModList As New List(Of McMod)
            Dim ModUpdateList As New List(Of McMod)
            For Each ModFile As FileInfo In ModFileList
                Loader.Progress += 0.95 / ModFileList.Count
                If Loader.IsAborted Then Exit Sub
                '加载 McMod 对象
                Dim ModEntry As New McMod(ModFile.FullName)
                ModEntry.Load()
                ModList.Add(ModEntry)
                '读取 Comp 缓存
                If ModEntry.State = McMod.McModState.Unavaliable Then Continue For
                Dim Hash = ModEntry.ModrinthHash
                If Cache.ContainsKey(Hash) Then
                    ModEntry.Comp = New CompProject(Cache(Hash))
                    ModEntry.IsCompFromModrinth = Not ModEntry.Comp.FromCurseForge
                    '如果缓存中的信息在 6 小时以内更新过，则无需重新获取
                    If Date.Now - Cache(Hash)("CacheTime").ToObject(Of Date) < New TimeSpan(6, 0, 0) Then Continue For
                End If
                ModUpdateList.Add(ModEntry)
            Next
            Loader.Progress = 0.99
            Log($"[Mod] 共有 {ModList.Count} 个 Mod，其中 {ModUpdateList.Where(Function(m) m.Comp Is Nothing).Count} 个需要联网获取信息，{ModUpdateList.Where(Function(m) m.Comp IsNot Nothing).Count} 个需要更新信息")

            '排序
            ModList = Sort(ModList, Function(Left As McMod, Right As McMod) As Boolean
                                        If (Left.State = McMod.McModState.Unavaliable) <> (Right.State = McMod.McModState.Unavaliable) Then
                                            Return Left.State = McMod.McModState.Unavaliable
                                        Else
                                            Return Not Right.FileName.CompareTo(Left.FileName)
                                        End If
                                    End Function)

            '回设
            If Loader.IsAborted Then Exit Sub
            Loader.Output = ModList

            '开始联网加载
            If ModUpdateList.Any() Then
                McModDetailLoader.Start(New KeyValuePair(Of List(Of McMod), JObject)(ModUpdateList, Cache), IsForceRestart:=True)
            End If

            Loader.Progress = 1
        Catch ex As Exception
            Log(ex, "Mod 列表加载失败", LogLevel.Debug)
            Throw
        End Try
    End Sub

    '联网加载 Mod 详情
    Private Const LocalModCacheVersion As Integer = 1
    Public McModDetailLoader As New LoaderTask(Of KeyValuePair(Of List(Of McMod), JObject), Integer)("Mod List Detail Loader", AddressOf McModDetailLoad)
    Private Sub McModDetailLoad(Loader As LoaderTask(Of KeyValuePair(Of List(Of McMod), JObject), Integer))
        Dim Mods As List(Of McMod) = Loader.Input.Key
        Dim Cache As JObject = Loader.Input.Value
        '获取作为检查目标的加载器和版本
        Dim TargetMcVersion As McVersionInfo = PageVersionLeft.Version.Version
        Dim ModLoaders As New List(Of String)
        If TargetMcVersion.HasForge Then ModLoaders.Add("forge")
        If TargetMcVersion.HasFabric Then ModLoaders.Add("fabric")
        If TargetMcVersion.HasLiteLoader Then ModLoaders.Add("liteloader")
        If Not ModLoaders.Any() Then ModLoaders.AddRange({"forge", "fabric", "liteloader"})
        Dim McVersions As New List(Of String) From {TargetMcVersion.McName}
        If TargetMcVersion.McCodeMain > 0 AndAlso TargetMcVersion.McCodeMain < 99 Then
            McVersions.Add($"1.{TargetMcVersion.McCodeMain}")
            For i = 1 To TargetMcVersion.McCodeSub
                McVersions.Add($"1.{TargetMcVersion.McCodeMain}.{i}")
            Next
        End If
        McVersions = McVersions.Distinct().ToList()
        '开始网络获取
        Log($"[Mod] 目标加载器：{ModLoaders.Join("/")}，版本：{McVersions.Join("/")}")
        Dim CompletedThread As Integer = 0
        Dim MainThread As Thread = Thread.CurrentThread
        '从 Modrinth 获取信息
        RunInNewThread(
        Sub()
            Try
                '步骤 1：获取 Hash 与对应的工程 ID
                Dim ModrinthHashes = Mods.Select(Function(m) m.ModrinthHash).ToList()
                Dim ModrinthUpdate = CType(GetJson(NetRequestRetry("https://api.modrinth.com/v2/version_files/update", "POST",
                    $"{{""hashes"": [""{ModrinthHashes.Join(""",""")}""], ""algorithm"": ""sha1"", 
                    ""loaders"": [""{ModLoaders.Join(""",""")}""],""game_versions"": [""{McVersions.Join(""",""")}""]}}", "application/json")), JObject)
                Log($"[Mod] 从 Modrinth 获取到 {ModrinthUpdate.Count} 个本地 Mod 的对应信息")
                '步骤 2：尝试读取工程信息缓存，构建其他 Mod 的对应关系
                If ModrinthUpdate.Count = 0 Then Exit Sub
                Dim ModrinthMapping As New Dictionary(Of String, McMod)
                For Each ModEntity In Mods
                    If Not ModrinthUpdate.ContainsKey(ModEntity.ModrinthHash) Then Continue For
                    Dim ProjectId = ModrinthUpdate(ModEntity.ModrinthHash)("project_id").ToString
                    If CompProjectCache.ContainsKey(ProjectId) Then
                        ModEntity.Comp = CompProjectCache(ProjectId)
                    Else
                        ModrinthMapping.Add(ProjectId, ModEntity)
                    End If
                Next
                If Loader.IsAbortedWithThread(MainThread) Then Exit Sub
                Log($"[Mod] 读取缓存后还需要从 Modrinth 获取 {ModrinthMapping.Count} 个本地 Mod 的工程信息")
                '步骤 3：获取工程信息
                If Not ModrinthMapping.Any() Then Exit Sub
                Dim ModrinthProject = CType(GetJson(NetRequestRetry(
                    $"https://api.modrinth.com/v2/projects?ids=[""{ModrinthMapping.Keys.Join(""",""")}""]",
                    "GET", "", "application/json")), JArray)
                For Each ProjectJson In ModrinthProject
                    Dim Project As New CompProject(ProjectJson)
                    Dim Entry = ModrinthMapping(Project.Id)
                    If Entry.Comp IsNot Nothing AndAlso Not Entry.IsCompFromModrinth Then
                        Project.LogoUrl = Entry.Comp.LogoUrl 'Modrinth 部分 Logo 加载不出来
                    End If
                    Entry.IsCompFromModrinth = True
                    Entry.Comp = Project
                Next
                Log($"[Mod] 从 Modrinth 获取本地 Mod 信息结束")
            Catch ex As Exception
                Log(ex, "从 Modrinth 获取本地 Mod 信息失败")
            Finally
                CompletedThread += 1
            End Try
        End Sub, "Mod List Detail Loader Modrinth")
        '从 CurseForge 获取信息
        RunInNewThread(
        Sub()
            Try
                '步骤 1：获取 Hash 与对应的工程 ID
                Dim CurseForgeHashes As New List(Of UInteger)
                For Each ModEntity In Mods
                    CurseForgeHashes.Add(ModEntity.CurseForgeHash)
                    If Loader.IsAbortedWithThread(MainThread) Then Exit Sub
                Next
                Dim CurseForgeRaw = CType(CType(GetJson(NetRequestRetry("https://api.curseforge.com/v1/fingerprints/432/", "POST",
                                    $"{{""fingerprints"": [{CurseForgeHashes.Join(",")}]}}", "application/json")), JObject)("data")("exactMatches"), JContainer)
                Log($"[Mod] 从 CurseForge 获取到 {CurseForgeRaw.Count} 个本地 Mod 的对应信息")
                '步骤 2：尝试读取工程信息缓存，构建其他 Mod 的对应关系
                If CurseForgeRaw.Count = 0 Then Exit Sub
                Dim CurseForgeMapping As New Dictionary(Of Integer, McMod)
                For Each Project In CurseForgeRaw
                    Dim ProjectId = Project("id").ToString
                    Dim Hash As UInteger = Project("file")("fileFingerprint")
                    Dim ModEntity = Mods.Find(Function(m) m.CurseForgeHash = Hash)
                    If CompProjectCache.ContainsKey(ProjectId) Then
                        ModEntity.Comp = CompProjectCache(ProjectId)
                    Else
                        CurseForgeMapping.Add(ProjectId, ModEntity)
                    End If
                Next
                If Loader.IsAbortedWithThread(MainThread) Then Exit Sub
                Log($"[Mod] 读取缓存后还需要从 CurseForge 获取 {CurseForgeMapping.Count} 个本地 Mod 的工程信息")
                '步骤 3：获取工程信息
                If Not CurseForgeMapping.Any() Then Exit Sub
                Dim CurseForgeProject = CType(GetJson(NetRequestRetry("https://api.curseforge.com/v1/mods", "POST",
                                    $"{{""modIds"": [{CurseForgeMapping.Keys.Join(",")}]}}", "application/json")), JObject)("data")
                For Each ProjectJson In CurseForgeProject
                    Dim Project As New CompProject(ProjectJson)
                    Dim Entry = CurseForgeMapping(Project.Id) '倒查防止 CurseForge 返回的内容有漏
                    If Entry.Comp IsNot Nothing AndAlso Entry.IsCompFromModrinth Then
                        Entry.Comp.LogoUrl = Project.LogoUrl 'Modrinth 部分 Logo 加载不出来
                        Entry.Comp = Entry.Comp '再次触发修改事件
                        Continue For
                    End If
                    Entry.IsCompFromModrinth = False
                    Entry.Comp = Project
                Next
                Log($"[Mod] 从 CurseForge 获取本地 Mod 信息结束")
            Catch ex As Exception
                Log(ex, "从 CurseForge 获取本地 Mod 信息失败")
            Finally
                CompletedThread += 1
            End Try
        End Sub, "Mod List Detail Loader CurseForge")
        '等待线程结束
        Do Until CompletedThread = 2
            If Loader.IsAborted Then Exit Sub
            Thread.Sleep(10)
        Loop
        '保存缓存
        Mods = Mods.Where(Function(m) m.Comp IsNot Nothing).ToList()
        Log($"[Mod] 联网获取本地 Mod 信息完成，为 {Mods.Count} 个 Mod 更新缓存")
        If Not Mods.Any() Then Exit Sub
        For Each ModEntity In Mods
            Cache(ModEntity.ModrinthHash) = ModEntity.Comp.ToJson()
        Next
        WriteFile(PathTemp & "Cache\LocalMod.json", Cache.ToString(If(ModeDebug, Newtonsoft.Json.Formatting.Indented, Newtonsoft.Json.Formatting.None)))
    End Sub

#If DEBUG Then
    ''' <summary>
    ''' 检查 Mod 列表中存在的错误，返回错误信息的集合。
    ''' </summary>
    Public Function McModCheck(Version As McVersion, Mods As List(Of McMod)) As List(Of String)
        Dim Result As New List(Of String)
        '令所有 Mod 进行基础检查，并归纳需要检查的 Mod
        Dim CurrentModList As New List(Of McMod)
        For Each ModEntity In Mods
            If Not ModEntity.IsFileAvailable Then
                Result.Add("无法读取的 Mod 文件。" & vbCrLf & " - " & ModEntity.Path)
                Continue For
            End If
            If ModEntity.State = McMod.McModState.Fine AndAlso ModEntity.ModId IsNot Nothing Then CurrentModList.Add(ModEntity)
        Next
        '添加默认依赖
        Dim CurrentDependencies As New Dictionary(Of String, String()) '{DependencyVersion, Path}
        If Version.State = McVersionState.Forge Then CurrentDependencies.Add("forge", {Version.Version.ForgeVersion, "Forge"})
        CurrentDependencies.Add("minecraft", {Version.Version.McName, "Minecraft"})
        '检查重复的 Mod，并添加对应的依赖
        For Each ModEntity In CurrentModList
            For Each PossibleModId In ModEntity.PossibleModId
                If CurrentDependencies.ContainsKey(PossibleModId) Then
                    If CurrentDependencies(PossibleModId)(2) = 1 Then
                        Result.Add("重复添加了相同的 Mod，请尝试删除其中一个（ModID：" & PossibleModId & "）。" & vbCrLf &
                                       " - " & ModEntity.FileName & vbCrLf &
                                       " - " & CurrentDependencies(PossibleModId)(1))
                    Else
                        Log("[Minecraft] 由于可能有多个 ModID，跳过疑似的重复项（ModID：" & PossibleModId & "）。" & vbCrLf &
                                       " - " & ModEntity.FileName & vbCrLf &
                                       " - " & CurrentDependencies(PossibleModId)(1), LogLevel.Developer)
                    End If
                Else
                    CurrentDependencies.Add(PossibleModId, {ModEntity.Version, ModEntity.FileName, ModEntity.PossibleModId.Count})
                End If
            Next
        Next
        '检查依赖
        For Each ModEntity In CurrentModList
            Try
                For Each Dependency In ModEntity.Dependencies
                    Dim ReqId As String = Dependency.Key
                    If ReqId.Count < 2 Then Continue For '确保正常
                    If ReqId = ModEntity.ModId Then Continue For '跳过自体引用
                    If ReqId = "forgemultipartcbe" Then Continue For '跳过莫名其妙的引用
                    If Dependency.Value IsNot Nothing Then
                        '获取分段后的详细版本信息
                        Dim ReqVersion As String = Dependency.Value
                        Dim ReqVersionHeadCanEqual As Boolean = ReqVersion.StartsWith("[")
                        Dim ReqVersionTailCanEqual As Boolean = ReqVersion.EndsWith("]")
                        Dim ReqVersionHead As String
                        Dim ReqVersionTail As String
                        If ReqVersion.Contains(",") Then
                            ReqVersionHead = ReqVersion.Split(",")(0).Trim("([ ".ToCharArray())
                            ReqVersionTail = ReqVersion.Split(",")(1).Trim("]) ".ToCharArray())
                        Else
                            ReqVersionHead = ReqVersion.Trim("([]) ".ToCharArray())
                            ReqVersionTail = ReqVersionHead
                            If ReqId = "minecraft" AndAlso ReqVersionHead.Split(".").Count = 2 Then
                                ReqVersionTail = ReqVersionHead.Split(".")(0) & "." & (Val(ReqVersionHead.Split(".")(1)) + 1)
                                ReqVersionTailCanEqual = False
                            End If
                        End If
                        If ReqVersionHead.StartsWith("1.") AndAlso ReqVersionHead.Contains("-") Then ReqVersionHead = ReqVersionHead.Substring(ReqVersionHead.LastIndexOf("-") + 1)
                        If ReqVersionTail.StartsWith("1.") AndAlso ReqVersionTail.Contains("-") Then ReqVersionTail = ReqVersionTail.Substring(ReqVersionTail.LastIndexOf("-") + 1)
                        '获取报错描述文本
                        Dim VersionRequire As String
                        If ReqVersionHead = ReqVersionTail Then
                            VersionRequire = "应为 " & ReqVersionHead
                        ElseIf ReqVersionHead.Contains(".") AndAlso ReqVersionTail.Contains(".") Then
                            VersionRequire = "应为 " & ReqVersionHead & " 至 " & ReqVersionTail
                        ElseIf ReqVersionHead.Contains(".") Then
                            If ReqVersionHeadCanEqual Then
                                VersionRequire = "最低应为 " & ReqVersionHead
                            Else
                                VersionRequire = "应高于 " & ReqVersionHead
                            End If
                        ElseIf ReqVersionTail.Contains(".") Then
                            If ReqVersionTailCanEqual Then
                                VersionRequire = "最高应为 " & ReqVersionHead
                            Else
                                VersionRequire = "应低于 " & ReqVersionHead
                            End If
                        Else
                            VersionRequire = ""
                        End If
                        '检查前置 Mod 是否存在，并获取其版本
                        If Not CurrentDependencies.ContainsKey(ReqId) Then
                            Result.Add("缺少前置 Mod：" & ReqId & If(VersionRequire = "", "", "，其版本" & VersionRequire) & "。" & vbCrLf & " - " & ModEntity.FileName)
                            Continue For
                        End If
                        Dim CurrentVersion As String = If(CurrentDependencies(ReqId)(0), "0.0")
                        If CurrentVersion.StartsWith("1.") AndAlso CurrentVersion.Contains("-") Then CurrentVersion = CurrentVersion.Substring(CurrentVersion.LastIndexOf("-") + 1)
                        '对比前置 Mod 头部版本
                        If ReqVersionHead.Contains(".") Then
                            If VersionSortInteger(ReqVersionHead, CurrentVersion) > If(ReqVersionHeadCanEqual, 0, -1) Then
                                Result.Add(ReqId.Substring(0, 1).ToUpper & ReqId.Substring(1) & " 版本过低，其版本" & VersionRequire & "，而当前版本为 " & CurrentVersion & "。" & vbCrLf &
                                           " - " & ModEntity.FileName & If(ReqId <> "minecraft" AndAlso ReqId <> "forge", vbCrLf & " - 前置：" & CurrentDependencies(ReqId)(1), ""))
                                Continue For
                            End If
                        End If
                        '对比前置 Mod 尾部版本
                        If ReqVersionTail.Contains(".") Then
                            If VersionSortInteger(CurrentVersion, ReqVersionTail) > If(ReqVersionTailCanEqual, 0, -1) Then
                                Result.Add(ReqId.Substring(0, 1).ToUpper & ReqId.Substring(1) & " 版本过高，其版本" & VersionRequire & "，而当前版本为 " & CurrentVersion & "。" & vbCrLf &
                                           " - " & ModEntity.FileName & If(ReqId <> "minecraft" AndAlso ReqId <> "forge", vbCrLf & " - 前置：" & CurrentDependencies(ReqId)(1), ""))
                                Continue For
                            End If
                        End If
                    Else
                        If Not CurrentDependencies.ContainsKey(Dependency.Key) Then
                            Result.Add("缺少前置 Mod：" & Dependency.Key & "。" & vbCrLf & " - " & ModEntity.FileName)
                            Continue For
                        End If
                    End If
                Next
            Catch ex As Exception
                Result.Add("检查 Mod 时出错：" & GetExceptionSummary(ex) & vbCrLf & " - " & ModEntity.FileName)
                Log(ex, "检查 Mod 时出错")
            End Try
        Next
        If Result.Count = 0 Then
            Log("[Minecraft] Mod 检查未发现异常")
        Else
            Log("[Minecraft] Mod 检查异常结果：" & vbCrLf & Join(Result, vbCrLf))
        End If
        Return Result
    End Function
#End If

End Module
