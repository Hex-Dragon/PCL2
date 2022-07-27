'提供不同的输入验证方法，名称以 Validate 开头

''' <summary>
''' 输入验证规则基类。要查看所有的输入验证规则，可在输入 Validate 后查看自动补全。
''' </summary>
Public MustInherit Class Validate
    ''' <summary>
    ''' 验证某字符串是否符合验证要求。若符合，返回空字符串；若不符合，返回错误原因。
    ''' </summary>
    Public MustOverride Function Validate(Str As String) As String
End Class

''' <summary>
''' 若为空则直接通过检查。
''' </summary>
Public Class ValidateNullable
    Inherits Validate
    Public Overrides Function Validate(Str As String) As String
        If IsNothing(Str) OrElse String.IsNullOrEmpty(Str) Then Return Nothing
        Return ""
    End Function
End Class

''' <summary>
''' 不能为 Nothing 或空字符串（不包括全空格检查）。
''' </summary>
Public Class ValidateNullOrEmpty
    Inherits Validate
    Public Overrides Function Validate(Str As String) As String
        If IsNothing(Str) OrElse String.IsNullOrEmpty(Str) Then Return "输入内容不能为空！"
        Return ""
    End Function
End Class

''' <summary>
''' 不能为 Nothing 或空字符串（包括全空格检查）。
''' </summary>
Public Class ValidateNullOrWhiteSpace
    Inherits Validate
    Public Overrides Function Validate(Str As String) As String
        If IsNothing(Str) OrElse String.IsNullOrWhiteSpace(Str) Then Return "输入内容不能为空！"
        Return ""
    End Function
End Class

''' <summary>
''' 必须满足正则表达式。
''' </summary>
Public Class ValidateRegex
    Inherits Validate
    Public Property Regex As String
    Public Property ErrorDescription As String = ""
    Public Sub New()
    End Sub '用于 XAML 初始化
    Public Sub New(Regex As String, Optional ErrorDescription As String = "")
        Me.Regex = Regex
        Me.ErrorDescription = ErrorDescription
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Not RegexCheck(Str, Regex) Then Return ErrorDescription
        Return ""
    End Function
End Class

''' <summary>
''' 必须是一个完整网址。
''' </summary>
Public Class ValidateHttp
    Inherits Validate
    Public Sub New()
    End Sub '用于 XAML 初始化
    Public Overrides Function Validate(Str As String) As String
        If Str.EndsWith("/") Then Str = Str.Substring(0, Str.Length - 1)
        If Not RegexCheck(Str, "^(http[s]?)\://") Then Return "输入的网址无效！"
        Return ""
    End Function
End Class

''' <summary>
''' 必须为整数。
''' </summary>
Public Class ValidateInteger
    Inherits Validate
    Public Property Min As Integer
    Public Property Max As Integer = Integer.MaxValue
    Public Sub New()
    End Sub '用于 XAML 初始化
    Public Sub New(Min As Integer, Max As Integer)
        Me.Min = Min
        Me.Max = Max
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Str.Length > 9 Then Return "请输入一个大小合理的数字！"
        Dim Valed As Integer = Val(Str)
        If Valed.ToString <> Str Then Return "请输入一个整数！"
        If Val(Str) > Max Then Return "不可超过 " & Max & "！"
        If Val(Str) < Min Then Return "不可低于 " & Min & "！"
        Return ""
    End Function
End Class

''' <summary>
''' 长度限制。
''' </summary>
Public Class ValidateLength
    Inherits Validate
    Public Property Min As Integer = 0
    Public Property Max As Integer = Integer.MaxValue
    Public Sub New()
    End Sub '用于 XAML 初始化
    Public Sub New(Min As Integer, Optional Max As Integer = Integer.MaxValue)
        Me.Min = Min
        Me.Max = Max
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Len(Str) <> Max AndAlso Max = Min Then Return "长度必须为 " & Max & " 个字符！"
        If Len(Str) > Max Then Return "长度最长为 " & Max & " 个字符！"
        If Len(Str) < Min Then Return "长度至少需 " & Min & " 个字符！"
        Return ""
    End Function
End Class

''' <summary>
''' 不能包含某些特定字符串。忽略大小写。
''' </summary>
Public Class ValidateExcept
    Inherits Validate
    Public Property Excepts As ObjectModel.Collection(Of String) = New ObjectModel.Collection(Of String)
    Public Property ErrorMessage As String
    Public Sub New()
        ErrorMessage = "输入内容不能包含 %！"
    End Sub '用于 XAML 初始化
    Public Sub New(Excepts As ObjectModel.Collection(Of String), Optional ErrorMessage As String = "输入内容不能包含 %！")
        Me.Excepts = Excepts
        Me.ErrorMessage = ErrorMessage
    End Sub
    Public Sub New(Excepts As IEnumerable, Optional ErrorMessage As String = "输入内容不能包含 %！")
        Me.Excepts = New ObjectModel.Collection(Of String)
        Me.ErrorMessage = ErrorMessage
        For Each Data As String In Excepts
            Me.Excepts.Add(Data)
        Next
    End Sub
    Public Overrides Function Validate(Str As String) As String
        For Each Ch As String In Excepts
            If Str.IndexOf(Ch, StringComparison.OrdinalIgnoreCase) >= 0 Then
                If IsNothing(ErrorMessage) Then ErrorMessage = ""
                Return ErrorMessage.Replace("%", Ch)
            End If
        Next
        Return ""
    End Function

End Class

''' <summary>
''' 不能与某些特定字符串相同。
''' </summary>
Public Class ValidateExceptSame
    Inherits Validate
    Public Property Excepts As ObjectModel.Collection(Of String) = New ObjectModel.Collection(Of String)
    Public Property ErrorMessage As String
    Public Property IgnoreCase As Boolean = False
    Public Sub New(Excepts As ObjectModel.Collection(Of String), Optional ErrorMessage As String = "输入内容不能为 %！", Optional IgnoreCase As Boolean = False)
        Me.Excepts = Excepts
        Me.ErrorMessage = ErrorMessage
        Me.IgnoreCase = IgnoreCase
    End Sub
    Public Sub New(Excepts As IEnumerable, Optional ErrorMessage As String = "输入内容不能为 %！", Optional IgnoreCase As Boolean = False)
        Me.Excepts = New ObjectModel.Collection(Of String)
        For Each Data As String In Excepts
            Me.Excepts.Add(Data)
        Next
        Me.ErrorMessage = ErrorMessage
        Me.IgnoreCase = IgnoreCase
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Str Is Nothing Then Return ErrorMessage.Replace("%", "null")
        For Each Ch As String In Excepts
            If IgnoreCase Then
                If Str.ToLower = Ch.ToLower Then Return ErrorMessage.Replace("%", Ch)
            Else
                If Str.Equals(Ch) Then Return ErrorMessage.Replace("%", Ch) '使用 = 不确定是否会忽略大小写
            End If
        Next
        Return ""
    End Function

End Class

''' <summary>
''' 对文件夹名的粗略的特化检测。
''' </summary>
Public Class ValidateFolderName
    Inherits Validate
    Public Property Path As String
    Public Property UseMinecraftCharCheck As Boolean = True
    Public Property IgnoreCase As Boolean = True
    Private ReadOnly PathIgnore As IEnumerable(Of DirectoryInfo)
    Public Sub New(Path As String, Optional UseMinecraftCharCheck As Boolean = True, Optional IgnoreCase As Boolean = True)
        Me.Path = Path
        Me.IgnoreCase = IgnoreCase
        Me.UseMinecraftCharCheck = UseMinecraftCharCheck
        On Error Resume Next
        PathIgnore = New DirectoryInfo(Path).EnumerateDirectories
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Try
            '检查是否为空
            Dim LengthCheck As String = New ValidateNullOrWhiteSpace().Validate(Str)
            If Not LengthCheck = "" Then Return LengthCheck
            '检查空格
            If Str.StartsWith(" ") Then Return "文件夹名不能以空格开头！"
            If Str.EndsWith(" ") Then Return "文件夹名不能以空格结尾！"
            '检查长度
            LengthCheck = New ValidateLength(1, 100).Validate(Str)
            If Not LengthCheck = "" Then Return LengthCheck
            '检查尾部小数点
            If Str.EndsWith(".") Then Return "文件夹名不能以小数点结尾！"
            '检查特殊字符
            Dim CharactCheck As String = New ValidateExcept(IO.Path.GetInvalidFileNameChars() & If(UseMinecraftCharCheck, "!;", ""), "文件夹名不可包含 % 字符！").Validate(Str)
            If Not CharactCheck = "" Then Return CharactCheck
            '检查特殊字符串
            Dim InvalidStrCheck As String = New ValidateExceptSame({"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"}, "文件夹名不可为 %！").Validate(Str)
            If Not InvalidStrCheck = "" Then Return InvalidStrCheck
            '检查文件夹重名
            Dim Arr As New List(Of String)
            If PathIgnore IsNot Nothing Then
                For Each Folder As DirectoryInfo In PathIgnore
                    Arr.Add(Folder.Name)
                Next
            End If
            Dim SameNameCheck = New ValidateExceptSame(Arr, "不可与现有文件夹重名！", IgnoreCase).Validate(Str)
            If Not SameNameCheck = "" Then Return SameNameCheck
            Return ""
        Catch ex As Exception
            Log(ex, "检查文件夹名出错")
            Return "错误：" & ex.Message
        End Try
    End Function
End Class

''' <summary>
''' 要求输入一个可用的文件夹路径。
''' </summary>
Public Class ValidateFolderPath
    Inherits Validate
    Public Property UseMinecraftCharCheck As Boolean = True
    Public Sub New()
    End Sub
    Public Sub New(UseMinecraftCharCheck As Boolean)
        Me.UseMinecraftCharCheck = UseMinecraftCharCheck
    End Sub
    Public Overrides Function Validate(Str As String) As String
        '去除尾部斜线，统一为 \
        Str = Str.Replace("/", "\")
        If Not Str.TrimEnd("\").EndsWith(":") Then Str = Str.TrimEnd("\")
        '检查是否为空
        Dim LengthCheck As String = New ValidateNullOrWhiteSpace().Validate(Str)
        If Not LengthCheck = "" Then Return LengthCheck
        '检查长度
        LengthCheck = New ValidateLength(1, 254).Validate(Str)
        If Not LengthCheck = "" Then Return LengthCheck
        '检查开头
        If Str.StartsWith("\\Mac\") Then GoTo Fin
        For Each Drive As DriveInfo In My.Computer.FileSystem.Drives
            If Str.ToUpper = Drive.Name Then Return ""
            If Str.ToUpper.StartsWith(Drive.Name) Then GoTo Fin
        Next
        Return "文件夹路径头存在错误！"
Fin:
        '对首层以外的路径检查
        For i = If(Str.StartsWith("\\Mac\"), 2, 1) To Str.Split("\").Count - 1
            Dim SubStr As String = Str.Split("\")(i)
            '检查是否为空
            Dim SubLengthCheck As String = New ValidateNullOrWhiteSpace().Validate(SubStr)
            If Not SubLengthCheck = "" Then Return "文件夹路径存在错误！"
            '检查特殊字符
            Dim CharactCheck As String = New ValidateExcept(IO.Path.GetInvalidFileNameChars() & If(UseMinecraftCharCheck, "!;", ""), "路径中存在无效字符！").Validate(SubStr)
            If Not CharactCheck = "" Then Return CharactCheck
            '检查头部空格
            If SubStr.StartsWith(" ") Then Return "文件夹名不能以空格开头！"
            If SubStr.EndsWith(" ") Then Return "文件夹名不能以空格结尾！"
            '检查尾部小数点
            If SubStr.EndsWith(".") Then Return "文件夹名不能以小数点结尾！"
            '检查特殊字符串
            Dim InvalidStrCheck As String = New ValidateExceptSame({"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"}, "文件夹名不可为 %！").Validate(SubStr)
            If Not InvalidStrCheck = "" Then Return InvalidStrCheck
        Next
        Return ""
    End Function
End Class

''' <summary>
''' 敏感词过滤。
''' </summary>
Public Class ValidateFilter
    Inherits Validate
    Private Shared Excepts As String() = SecretDecrypt(SecretSensitiveWordsRaw).Split("|")
    Public Sub New()
    End Sub '用于 XAML 初始化
    Public Overrides Function Validate(Str As String) As String
        '提取无间隔符的字符串
        Dim RealStr As String = ""
        For Each St In If(Str, "").ToLower
            If String.IsNullOrWhiteSpace(St.ToString) OrElse ("!@#$%^&*()_+-={}[]|\;':"",./<>? ~　·！￥…（）—，。、《》？‘；：【】｛｝" & vbLQ & vbRQ).ToCharArray.Contains(St) Then Continue For
            RealStr += St
        Next
        '检查
        For Each Except In Excepts
            If RealStr.Contains(Except) Then Return "存在敏感内容！"
        Next
        Return ""
    End Function
End Class
