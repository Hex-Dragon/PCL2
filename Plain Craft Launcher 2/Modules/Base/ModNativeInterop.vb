Imports System.IO.Pipes
Imports System.Runtime.InteropServices
Imports Newtonsoft.Json

Public Module ModNativeInterop

#Region "本地进程管理"

    Public CurrentProcess As Process = Process.GetCurrentProcess()

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Function GetNamedPipeClientProcessId(ByVal pipeHandle As IntPtr, ByRef clientProcessId As UInteger) As Boolean
    End Function

#End Region

#Region "命名管道通信"

    Private ReadOnly PipeEncoding As Encoding = Encoding.UTF8
    Private Const PipeEndingChar = ChrW(27) '\033 (ESC)

    ''' <summary>
    ''' 在新的工作线程启动命名管道服务端
    ''' </summary>
    ''' <param name="identifier">服务端标识，用于日志标识及工作线程的命名</param>
    ''' <param name="pipeName">命名管道名称</param>
    ''' <param name="loopCallback">客户端连接后的回调函数，将会提供用于读取和写入数据的流，以及客户端进程 ID，返回 <c>true</c> 表示继续等待下一个客户端连接，返回 <c>false</c> 则停止服务端运行</param>
    ''' <param name="stopWhenException">指定当回调函数抛出异常时是否停止服务端运行，使用 <c>true</c> 表示停止</param>
    ''' <param name="security">命名管道安全策略</param>
    Public Sub StartPipeServer(identifier As String, pipeName As String, loopCallback As Func(Of StreamReader, StreamWriter, Process, Boolean), Optional stopWhenException As Boolean = False, Optional security As PipeSecurity = Nothing)
        Dim pipe As NamedPipeServerStream = If(security Is Nothing,
            New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024, 1024),
            New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024, 1024, security))
        Dim threadName = $"PipeServer/{identifier}"

        RunInNewThread(
            Sub()
                Log($"[Pipe] {identifier}: {pipeName} 服务端已在 '{threadName}' 工作线程启动", LogLevel.Debug)
                Dim hasNextLoop = True, connected = False
                Dim reader As StreamReader = Nothing, writer As StreamWriter = Nothing

                While hasNextLoop
                    Try
                        hasNextLoop = False
                        pipe.WaitForConnection() '等待客户端连接
                        connected = True
                        Log($"[Pipe] {identifier}: 客户端已连接", LogLevel.Debug)
                        '初始化读取/写入流
                        reader = New StreamReader(pipe, PipeEncoding, False, 1024, True)
                        writer = New StreamWriter(pipe, PipeEncoding, 1024, True)
                        '获取客户端进程对象
                        Dim clientProcessId As UInteger = Nothing
                        GetNamedPipeClientProcessId(pipe.SafePipeHandle.DangerousGetHandle(), clientProcessId)
                        Dim clientProcess = Process.GetProcessById(clientProcessId)
                        '执行回调函数
                        hasNextLoop = loopCallback(reader, writer, clientProcess)
                        '写入终止符
                        writer.Write(PipeEndingChar)
                        writer.Flush() '刷新写入缓冲
                        reader.Read() '等待客户端
                    Catch ex As Exception
                        If Not pipe.IsConnected AndAlso connected AndAlso TypeOf ex Is IOException Then
                            Log($"[Pipe] {identifier}: 客户端连接已丢失", LogLevel.Debug)
                            hasNextLoop = True
                        Else
                            Log(ex, $"[Pipe] {identifier}: 服务端出错", LogLevel.Hint)
                            If stopWhenException Then hasNextLoop = False
                        End If
                    Finally
                        pipe.Disconnect() '确保已断开连接
                        connected = False
                        Log($"[Pipe] {identifier}: 已断开连接", LogLevel.Debug)
                    End Try
                End While

                reader?.Close()
                writer?.Close()
                pipe.Close()
                Log($"[Pipe] {identifier}: 服务端已停止", LogLevel.Debug)
            End Sub,
            threadName)
    End Sub

    ''' <summary>
    ''' 用于终止 Pipe RPC 执行过程并返回错误信息的异常<br/>
    ''' 当抛出该异常时 RPC 服务端将会返回内容为 <c>Reason</c> 的 <c>ERR</c> 响应
    ''' </summary>
    Public Class PipeRPCException
        Inherits Exception

        Public ReadOnly Reason As String

        Public Sub New(reason As String)
            Me.Reason = reason
        End Sub

    End Class

    Public Enum RPCResponseStatus
        SUCCESS
        FAILURE
        ERR
    End Enum

    Public Enum RPCResponseType
        EMPTY
        TEXT
        JSON
        BASE64
    End Enum

    ''' <summary>
    ''' Pipe RPC 响应
    ''' </summary>
    Public Class RPCResponse

        Public Property Status As RPCResponseStatus

        Public Property Type As RPCResponseType

        Public Property Name As String

        Public Property Content As String

        Public Sub New(status As RPCResponseStatus, Optional type As RPCResponseType = RPCResponseType.EMPTY, Optional content As String = Nothing, Optional name As String = Nothing)
            If content IsNot Nothing AndAlso type = RPCResponseType.EMPTY Then Throw New ArgumentException("Empty response with non-null content")
            Me.Status = status
            Me.Type = type
            Me.Content = content
            Me.Name = name
        End Sub

        'STATUS type [name]
        '[content]
        Public Sub Response(writer As StreamWriter)
            writer.WriteLine($"{Status} {Type.ToString().ToLowerInvariant()}{If(Name Is Nothing, "", $" {Name}")}")
            If Content IsNot Nothing Then writer.WriteLine(Content)
        End Sub

        Public Shared ReadOnly EmptySuccess As New RPCResponse(RPCResponseStatus.SUCCESS)

        Public Shared ReadOnly EmptyFailure As New RPCResponse(RPCResponseStatus.FAILURE)

        Public Shared Function Err(content As String, Optional name As String = Nothing) As RPCResponse
            Return New RPCResponse(RPCResponseStatus.ERR, RPCResponseType.TEXT, content, name)
        End Function

        Public Shared Function Success(type As RPCResponseType, content As String, Optional name As String = Nothing) As RPCResponse
            Return New RPCResponse(RPCResponseStatus.SUCCESS, type, content, name)
        End Function

        Public Shared Function Failure(type As RPCResponseType, content As String, Optional name As String = Nothing) As RPCResponse
            Return New RPCResponse(RPCResponseStatus.FAILURE, type, content, name)
        End Function

    End Class

    Public Class RPCPropertyOperationFailedException
        Inherits Exception
    End Class

    ''' <summary>
    ''' RPC 属性<br/>
    ''' 大多数时候只需要使用构造方法，其他结构保留供内部使用
    ''' </summary>
    Public Class RPCProperty

        Public Delegate Sub GetValueDelegate(ByRef outValue)
        Public Event GetValue As GetValueDelegate

        Public Delegate Sub SetValueDelegate(value As String, ByRef success As Boolean)
        Public Event SetValue As SetValueDelegate

        Public ReadOnly Name As String
        Public ReadOnly Settable As Boolean = True

        Public Property Value As String
            Get
                Dim _value As String = Nothing
                RaiseEvent GetValue(_value)
                Return _value
            End Get
            Set(_value As String)
                Dim success = True
                RaiseEvent SetValue(_value, success)
                If Not success Then Throw New RPCPropertyOperationFailedException()
            End Set
        End Property

        ''' <param name="onGetValue">默认的 <c>GetValue</c> 回调</param>
        ''' <param name="onSetValue">默认的 <c>SetValue</c> 回调</param>
        ''' <param name="settable">指定该属性是否可更改，若该值为 <c>false</c> 的同时 <paramref name="onSetValue"/> 为 <c>null</c>，则该属性成为只读属性</param>
        Public Sub New(name As String, onGetValue As Func(Of String), Optional onSetValue As Action(Of String) = Nothing, Optional settable As Boolean = False)
            Me.Name = name
            AddHandler GetValue,
            Sub(ByRef outValue)
                outValue = onGetValue()
            End Sub
            If onSetValue IsNot Nothing Then
                AddHandler SetValue,
                Sub(value As String, ByRef success As Boolean)
                    onSetValue(value)
                End Sub
            ElseIf Not settable Then
                Me.Settable = False
                AddHandler SetValue,
                Sub(value As String, ByRef success As Boolean)
                    success = False
                End Sub
            End If
        End Sub

    End Class

    ''' <summary>
    ''' RPC 函数<br/>
    ''' 接收参数并返回响应内容
    ''' </summary>
    ''' <param name="argument">参数</param>
    ''' <returns>响应内容</returns>
    Public Delegate Function RPCFunction(argument As String, content As String, indent As Boolean) As RPCResponse

    ''' <summary>
    ''' Pipe RPC 相关功能的静态方法类
    ''' </summary>
    Public Class PipeRPC

        Private Shared ReadOnly EchoPipeName As String = $"PCLCE_RPC@{CurrentProcess.Id}"

        Private Shared ReadOnly RequestTypeArray As String() = {"GET", "SET", "REQ"}
        Private Shared ReadOnly RequestType As New HashSet(Of String)(RequestTypeArray)

        Private Shared ReadOnly PropertyMap As New Dictionary(Of String, RPCProperty)
        Private Shared ReadOnly PropertyArray As RPCProperty() = {
            New RPCProperty("version",
                Function()
                    Return VersionBaseName
                End Function),
            New RPCProperty("branch",
                Function()
                    Return VersionBranchName
                End Function)
        }

        ''' <summary>
        ''' 添加一个新的 RPC 属性，若有多个使用 foreach 即可
        ''' </summary>
        ''' <param name="prop">要添加的属性</param>
        ''' <returns>是否成功添加（若已存在相同名称的属性则无法添加）</returns>
        Public Shared Function AddProperty(prop As RPCProperty) As Boolean
            Dim key = prop.Name
            If PropertyMap.ContainsKey(key) Then Return False
            PropertyMap(key) = prop
            Return True
        End Function

        ''' <summary>
        ''' 通过指定的名称删除已存在的 RPC 属性
        ''' </summary>
        ''' <param name="name">属性名称</param>
        ''' <returns>是否成功删除（若不存在该名称则无法删除）</returns>
        Public Shared Function RemoveProperty(name As String) As Boolean
            Return PropertyMap.Remove(name)
        End Function

        ''' <summary>
        ''' 删除已存在的 RPC 属性，实质上仍然是通过属性的名称删除，但会检查是否是同一个对象
        ''' </summary>
        ''' <param name="prop">要删除的属性</param>
        ''' <returns></returns>
        Public Shared Function RemoveProperty(prop As RPCProperty) As Boolean
            Dim key = prop.Name
            Dim value As RPCProperty = Nothing
            Dim result = PropertyMap.TryGetValue(key, value)
            If (Not result) OrElse (Not value.Equals(prop)) Then Return False
            PropertyMap.Remove(key)
            Return True
        End Function

        Private Shared ReadOnly FunctionMap As New Dictionary(Of String, RPCFunction)

        ''' <summary>
        ''' 添加一个新的 RPC 函数，若有多个使用 foreach 即可
        ''' </summary>
        ''' <param name="name">函数名称</param>
        ''' <param name="func">函数过程</param>
        ''' <returns>是否成功添加（若已存在相同名称的函数则无法添加）</returns>
        Public Shared Function AddFunction(name As String, func As RPCFunction) As Boolean
            If FunctionMap.ContainsKey(name) Then Return False
            FunctionMap(name) = func
            Return True
        End Function

        ''' <summary>
        ''' 通过指定的名称删除已存在的 RPC 函数
        ''' </summary>
        ''' <param name="name">函数名称</param>
        ''' <returns>是否成功删除（若不存在该名称则无法删除）</returns>
        Public Shared Function RemoveFunction(name As String) As Boolean
            Return FunctionMap.Remove(name)
        End Function

        Private Shared Function EchoPipeCallback(reader As StreamReader, writer As StreamWriter, client As Process) As Boolean
            Try
                'GET/SET/REQ [target]
                '[content]
                Dim header = reader.ReadLine() '读入请求头
                Log($"[PipeRPC] 客户端请求: {header}")

                Dim args = header.Split({" "c}, 2) '分离请求类型和参数
                If args.Length < 2 OrElse args(1).Length = 0 Then Throw New PipeRPCException("请求参数过少")
                Dim type = args(0).ToUpperInvariant()
                If Not RequestType.Contains(type) Then Throw New PipeRPCException($"请求类型必须为 {RequestTypeArray.Join("/")} 其中之一")
                Dim target = args(1)

                '读入请求内容（可能没有）
                Dim buffer As New StringBuilder()
                Dim tmp As Char = ChrW(reader.Read())
                While tmp <> PipeEndingChar
                    buffer.Append(tmp)
                    tmp = ChrW(reader.Read())
                End While
                Dim content = If(buffer.Length = 0, Nothing, buffer.ToString())

                Select Case type
                    Case "GET", "SET"
                        target = target.ToLowerInvariant()
                        Dim prop As RPCProperty = Nothing
                        Dim result = PropertyMap.TryGetValue(target, prop)
                        If Not result Then Throw New PipeRPCException($"不存在属性 {target}")
                        Dim response As RPCResponse
                        If (type = "GET") Then
                            Try
                                Dim value = prop.Value
                                response = New RPCResponse(RPCResponseStatus.SUCCESS, RPCResponseType.TEXT, value, target)
                                Log($"[PipeRPC] 返回值: {value}")
                            Catch ex As RPCPropertyOperationFailedException
                                response = RPCResponse.EmptyFailure
                                Log("[PipeRPC] 设置失败: 只写属性或请求被拒绝")
                            End Try
                        Else
                            If prop.Settable Then
                                Try
                                    prop.Value = content
                                    response = RPCResponse.EmptySuccess
                                    Log($"[PipeRPC] 设置成功: {content}")
                                Catch ex As RPCPropertyOperationFailedException
                                    response = RPCResponse.EmptyFailure
                                    Log("[PipeRPC] 设置失败: 请求被拒绝")
                                End Try
                            Else
                                response = RPCResponse.EmptyFailure
                                Log("[PipeRPC] 设置失败: 只读属性")
                            End If
                        End If
                        response.Response(writer)

                    Case "REQ"
                        Dim targetArgs = target.Split({" "c}, 2) '分离函数名和参数
                        Dim name = targetArgs(0).ToLowerInvariant()
                        Dim indent = False '检测缩进指示
                        If (name.EndsWith("$")) Then
                            indent = True
                            name = name.Substring(0, name.Length - 1)
                        End If
                        Dim func As RPCFunction = Nothing
                        Dim result = FunctionMap.TryGetValue(name, func)
                        If Not result Then Throw New PipeRPCException($"不存在函数 {name}")
                        Dim argument As String = Nothing
                        If (targetArgs.Length > 1) Then argument = targetArgs(1)
                        Log($"[PipeRPC] 正在调用函数 {name} {argument}")
                        Dim response = func(argument, content, indent)
                        response.Response(writer)
                        Log($"[PipeRPC] 返回状态 {response.Status}")
                End Select

            Catch ex As Exception
                If TypeOf ex Is PipeRPCException Then
                    Dim reason = CType(ex, PipeRPCException).Reason
                    RPCResponse.Err(reason).Response(writer)
                    Log($"[PipeRPC] 出错: {reason}")
                Else
                    RPCResponse.Err(ex.ToString(), "stacktrace").Response(writer)
                    Log(ex, "[PipeRPC] 处理请求时发生异常", LogLevel.Debug)
                End If
            End Try
            Return True
        End Function

        Private Shared Function JsonIndent(indent As Boolean) As Formatting
            Return If(indent, Formatting.Indented, Formatting.None)
        End Function

        Private Shared Function _PingCallback(argument As String, content As String, indent As Boolean) As RPCResponse
            Return RPCResponse.EmptySuccess
        End Function

        ' 用于序列化 JSON 并响应客户端 info 请求的类型
        Private Class RPCLauncherInfo
            Public path As String = PathWithName
            Public config_path As String = PathAppdataConfig
            Public window As Long = Handle.ToInt64()
            Public version As New LauncherVersion()
            Class LauncherVersion
                Public name As String = VersionBaseName
                Public commit As String = CommitHash
                Public branch As String = VersionBranchName
                Public branch_code As String = VersionBranchCode
                Public upstream As String = UpstreamVersion
            End Class
        End Class

        Private Shared Function _InfoCallback(argument As String, content As String, indent As Boolean) As RPCResponse
            Dim json = JsonConvert.SerializeObject(New RPCLauncherInfo(), JsonIndent(indent))
            Return RPCResponse.Success(RPCResponseType.JSON, json)
        End Function

        Private Shared _pendingLauncherLogs As New List(Of String)
        Private Shared _pendingLauncherLogsStart As DateTime
        Private Shared _lastUpdatedWatchers As New Dictionary(Of String, Watcher)()

        Public Shared Sub PrintLog(line As String)
            _pendingLauncherLogs.Add(line)
        End Sub

        Private Shared Function LogPipeCallback(reader As StreamReader, writer As StreamWriter, client As Process) As Boolean
        End Function

        '用于反序列化客户端 log 读取请求 JSON 的类型
        Private Class RPCLogOpenRequest
            Public id As String '请求读取的 log id
            Public client As Long '前来连接的客户端的 process id，用于鉴权
            Public timeout As Integer = 5 '期望等待时间 (s)，最大 30
        End Class

        '用于序列化服务端 log 信息响应的 JSON 的类型
        Private Class RPCLogInfoResponse
            Public launcher As New LauncherLog()
            Class LauncherLog
                Public id As String = "launcher"
                Public pending As Integer = _pendingLauncherLogs.Count
                Public start As DateTime = _pendingLauncherLogsStart
            End Class
            Public minecraft As MinecraftLog() = MinecraftLog.GenerateLogInfo()
            Class MinecraftLog
                Public id As String
                Public pending As Integer
                Public name As String
                Public version As String
                Public state As Watcher.MinecraftState
                Public realtime As Boolean
                Shared Function GenerateLogInfo() As MinecraftLog()
                    _lastUpdatedWatchers.Clear()
                    If Not HasRunningMinecraft Then Return {}
                    Dim infoList As New List(Of MinecraftLog)
                    For Each watcher In McWatcherList
                        Dim id = $"mc@{watcher.GameProcess.Id}"
                        _lastUpdatedWatchers(id) = watcher
                        Dim state = watcher.State
                        Dim pending = watcher.FullLog.Count
                        Dim realtime = watcher.RealTimeLog
                        Dim game = watcher.Version
                        Dim name = game.Name
                        Dim version = game.Version.McVersion.ToString()
                        infoList.Add(New MinecraftLog With {
                            .id = id, .name = name, .pending = pending, .realtime = realtime,
                            .state = state, .version = version})
                    Next
                    Return infoList.ToArray()
                End Function
            End Class
        End Class

        Private Shared Function _LogCallback(argument As String, content As String, indent As Boolean) As RPCResponse
            If argument Is Nothing Then Return RPCResponse.Err("日志请求参数过少")
            argument = argument.ToLowerInvariant()
            If argument = "info" Then
                Dim json = JsonConvert.SerializeObject(New RPCLogInfoResponse(), JsonIndent(indent))
                Return RPCResponse.Success(RPCResponseType.JSON, json)
            End If
            Return RPCResponse.Err("日志请求参数有误")
        End Function

        Private Shared Sub AddPredefinedFunctions()
            AddFunction("ping", AddressOf _PingCallback)
            AddFunction("info", AddressOf _InfoCallback)
            AddFunction("log", AddressOf _LogCallback)
        End Sub

        ''' <summary>
        ''' 请勿调用该方法
        ''' </summary>
        Public Shared Sub Start()
            Log("[PipeRPC] 正在加载预设 RPC 属性")
            For Each prop In PropertyArray
                PropertyMap(prop.Name) = prop
            Next
            Log("[PipeRPC] 正在加载预设 RPC 函数")
            AddPredefinedFunctions()
            Log("[PipeRPC] 正在初始化 Echo 服务端")
            StartPipeServer("RPC-Echo", EchoPipeName, AddressOf EchoPipeCallback)
        End Sub

    End Class

    ''' <summary>
    ''' 初始化并启动 Pipe RPC 服务端，该方法应在启动器初始化时被调用，请勿重复调用
    ''' </summary>
    Public Sub StartEchoPipe()
        RunInNewThread(AddressOf PipeRPC.Start, "PipeRPC Loading")
    End Sub

#End Region

End Module
