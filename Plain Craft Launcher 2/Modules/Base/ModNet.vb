﻿Imports System.Net

Public Module ModNet
    Public Const NetDownloadEnd As String = ".PCLDownloading"

    ''' <summary>
    ''' 测试 Ping。失败则返回 -1。
    ''' </summary>
    Public Function Ping(Ip As String, Optional Timeout As Integer = 10000, Optional MakeLog As Boolean = True) As Integer
        Dim PingResult As NetworkInformation.PingReply
        Try
            PingResult = (New NetworkInformation.Ping).Send(Ip)
        Catch ex As Exception
            If MakeLog Then Log("[Net] Ping " & Ip & " 失败：" & ex.Message)
            Return -1
        End Try
        If PingResult.Status = NetworkInformation.IPStatus.Success Then
            If MakeLog Then Log("[Net] Ping " & Ip & " 结束：" & PingResult.RoundtripTime & "ms")
            Return PingResult.RoundtripTime
        Else
            If MakeLog Then Log("[Net] Ping " & Ip & " 失败")
            Return -1
        End If
    End Function

    ''' <summary>
    ''' 以 WebClient 获取网页源代码。会进行至多 45 秒 3 次的尝试，允许最长 30s 的超时。
    ''' </summary>
    ''' <param name="Url">网页的 Url。</param>
    ''' <param name="Encoding">网页的编码，通常为 UTF-8。</param>
    Public Function NetGetCodeByClient(Url As String, Encoding As Encoding, Optional Accept As String = "application/json, text/javascript, */*; q=0.01", Optional UseBrowserUserAgent As Boolean = False) As String
        Dim RetryCount As Integer = 0
        Dim RetryException As Exception = Nothing
        Dim StartTime As Long = GetTimeTick()
        Try
Retry:
            Select Case RetryCount
                Case 0 '正常尝试
                    Return NetGetCodeByClient(Url, Encoding, 10000, Accept, UseBrowserUserAgent)
                Case 1 '慢速重试
                    Thread.Sleep(500)
                    Return NetGetCodeByClient(Url, Encoding, 30000, Accept, UseBrowserUserAgent)
                Case Else '快速重试
                    If GetTimeTick() - StartTime > 5500 Then
                        '若前两次加载耗费 5 秒以上，才进行重试
                        Thread.Sleep(500)
                        Return NetGetCodeByClient(Url, Encoding, 4000, Accept, UseBrowserUserAgent)
                    Else
                        Throw RetryException
                    End If
            End Select
        Catch ex As Exception
            Select Case RetryCount
                Case 0
                    RetryException = ex
                    RetryCount += 1
                    GoTo Retry
                Case 1
                    RetryCount += 1
                    GoTo Retry
                Case Else
                    Throw
            End Select
        End Try
    End Function
    Public Function NetGetCodeByClient(Url As String, Encoding As Encoding, Timeout As Integer, Accept As String, Optional UseBrowserUserAgent As Boolean = False) As String
        Url = SecretCdnSign(Url)
        Log("[Net] 获取客户端网络结果：" & Url & "，最大超时 " & Timeout)
        Dim Request As CookieWebClient
        Dim res As HttpWebResponse = Nothing
        Dim HttpStream As Stream = Nothing
        Try
            Request = New CookieWebClient With {
                .Encoding = Encoding,
                .Timeout = Timeout
            }
            Request.Headers("Accept") = Accept
            Request.Headers("Accept-Language") = "en-US,en;q=0.5"
            Request.Headers("X-Requested-With") = "XMLHttpRequest"
            SecretHeadersSign(Url, Request, UseBrowserUserAgent)
            Return Request.DownloadString(Url)
        Catch ex As Exception
            If ex.GetType.Equals(GetType(WebException)) AndAlso CType(ex, WebException).Status = WebExceptionStatus.Timeout Then
                Throw New TimeoutException("连接服务器超时（" & Url & "）", ex)
            Else
                Throw New WebException("获取结果失败，" & ex.Message & "（" & Url & "）", ex)
            End If
        Finally
            If Not IsNothing(HttpStream) Then HttpStream.Dispose()
            If Not IsNothing(res) Then res.Dispose()
        End Try
    End Function

    ''' <summary>
    ''' 以 WebRequest 获取网页源代码或 Json。会进行至多 45 秒 3 次的尝试，允许最长 30s 的超时。
    ''' </summary>
    ''' <param name="Url">网页的 Url。</param>
    ''' <param name="Encode">网页的编码，通常为 UTF-8。</param>
    ''' <param name="BackupUrl">如果第一次尝试失败，换用的备用 URL。</param>
    Public Function NetGetCodeByRequestRetry(Url As String, Optional Encode As Encoding = Nothing, Optional Accept As String = "",
                                             Optional IsJson As Boolean = False, Optional BackupUrl As String = Nothing, Optional UseBrowserUserAgent As Boolean = False)
        Dim RetryCount As Integer = 0
        Dim RetryException As Exception = Nothing
        Dim StartTime As Long = GetTimeTick()
        Try
Retry:
            Select Case RetryCount
                Case 0 '正常尝试
                    Return NetGetCodeByRequestOnce(Url, Encode, 10000, IsJson, Accept, UseBrowserUserAgent)
                Case 1 '慢速重试
                    Thread.Sleep(500)
                    Return NetGetCodeByRequestOnce(If(BackupUrl, Url), Encode, 30000, IsJson, Accept, UseBrowserUserAgent)
                Case Else '快速重试
                    If GetTimeTick() - StartTime > 5500 Then
                        '若前两次加载耗费 5 秒以上，才进行重试
                        Thread.Sleep(500)
                        Return NetGetCodeByRequestOnce(If(BackupUrl, Url), Encode, 4000, IsJson, Accept, UseBrowserUserAgent)
                    Else
                        Throw RetryException
                    End If
            End Select
        Catch ex As ThreadInterruptedException
            Throw
        Catch ex As Exception
            Select Case RetryCount
                Case 0
                    RetryException = ex
                    RetryCount += 1
                    GoTo Retry
                Case 1
                    RetryCount += 1
                    GoTo Retry
                Case Else
                    Throw
            End Select
        End Try
    End Function
    ''' <summary>
    ''' 以 WebRequest 获取网页源代码或 Json。会逐渐生成 4 个尝试线程，并在 60s 后超时。
    ''' </summary>
    ''' <param name="Url">网页的 Url。</param>
    ''' <param name="Encode">网页的编码，通常为 UTF-8。</param>
    Public Function NetGetCodeByRequestMultiple(Url As String, Optional Encode As Encoding = Nothing, Optional Accept As String = "", Optional IsJson As Boolean = False)
        Dim Threads As New List(Of Thread)
        Dim RequestResult = Nothing
        Dim RequestEx As Exception = Nothing
        Dim FailCount As Integer = 0
        For i = 1 To 4
            Dim th As New Thread(Sub()
                                     Try
                                         RequestResult = NetGetCodeByRequestOnce(Url, Encode, 30000, IsJson, Accept)
                                     Catch ex As Exception
                                         FailCount += 1
                                         RequestEx = ex
                                     End Try
                                 End Sub)
            th.Start()
            Threads.Add(th)
            Thread.Sleep(i * 250)
            If RequestResult IsNot Nothing Then GoTo RequestFinished
        Next
        Do While True
            If RequestResult IsNot Nothing Then
RequestFinished:
                Try
                    For Each th In Threads
                        If th.IsAlive Then th.Interrupt()
                    Next
                Catch
                End Try
                Return RequestResult
            ElseIf FailCount = 4 Then
                Try
                    For Each th In Threads
                        If th.IsAlive Then th.Interrupt()
                    Next
                Catch
                End Try
                Throw RequestEx
            End If
            Thread.Sleep(20)
        Loop
        Throw New Exception("未知错误")
    End Function
    Public Function NetGetCodeByRequestOnce(Url As String, Optional Encode As Encoding = Nothing, Optional Timeout As Integer = 30000, Optional IsJson As Boolean = False, Optional Accept As String = "", Optional UseBrowserUserAgent As Boolean = False)
        If RunInUi() AndAlso Not Url.Contains("//127.") Then Throw New Exception("在 UI 线程执行了网络请求")
        Url = SecretCdnSign(Url)
        Log($"[Net] 获取网络结果：{Url}，超时 {Timeout}ms{If(IsJson, "，要求 json", "")}")
        Dim Request As HttpWebRequest = WebRequest.Create(Url)
        Dim Result As New List(Of Byte)
        Try
            If Url.StartsWithF("https", True) Then Request.ProtocolVersion = HttpVersion.Version11
            Request.Timeout = Timeout
            Request.Accept = Accept
            SecretHeadersSign(Url, Request, UseBrowserUserAgent)
            Using res As HttpWebResponse = Request.GetResponse()
                Using HttpStream As Stream = res.GetResponseStream()
                    HttpStream.ReadTimeout = Timeout
                    Dim HttpData As Byte() = New Byte(16384) {}
                    Using Reader As New StreamReader(HttpStream, If(Encode, Encoding.UTF8))
                        Dim ResultString As String = Reader.ReadToEnd
                        Return If(IsJson, GetJson(ResultString), ResultString)
                    End Using
                End Using
            End Using
        Catch ex As ThreadInterruptedException
            Throw
        Catch ex As Exception
            If TypeOf ex Is WebException AndAlso CType(ex, WebException).Status = WebExceptionStatus.Timeout Then
                Throw New TimeoutException($"获取结果失败（{CType(ex, WebException).Status}，{ex.Message}，{Url}）", ex)
            Else
                Throw New WebException($"获取结果失败（{If(TypeOf ex Is WebException, CType(ex, WebException).Status & "，", "")}{ex.Message}，{Url}）", ex)
            End If
        Finally
            Request.Abort()
        End Try
    End Function

    ''' <summary>
    ''' 以多线程下载网页文件的方式获取网页源代码。
    ''' </summary>
    ''' <param name="Url">网页的 Url。</param>
    Public Function NetGetCodeByDownload(Url As String, Optional Timeout As Integer = 45000, Optional IsJson As Boolean = False, Optional UseBrowserUserAgent As Boolean = False) As String
        Dim Temp As String = PathTemp & "Cache\Code\" & Url.GetHashCode() & "_" & GetUuid()
        Dim NewTask As New LoaderDownload("源码获取 " & GetUuid() & "#", New List(Of NetFile) From {New NetFile({Url}, Temp, New FileChecker With {.IsJson = IsJson}, UseBrowserUserAgent)})
        Try
            NewTask.WaitForExitTime(Timeout, TimeoutMessage:="连接服务器超时（" & Url & "）")
            NetGetCodeByDownload = ReadFile(Temp)
            File.Delete(Temp)
        Finally
            NewTask.Abort()
        End Try
    End Function
    ''' <summary>
    ''' 以多线程下载网页文件的方式获取网页源代码。
    ''' </summary>
    ''' <param name="Urls">网页的 Url 列表。</param>
    Public Function NetGetCodeByDownload(Urls As IEnumerable(Of String), Optional Timeout As Integer = 45000, Optional IsJson As Boolean = False, Optional UseBrowserUserAgent As Boolean = False) As String
        Dim Temp As String = PathTemp & "Cache\Code\" & Urls.First.GetHashCode() & "_" & GetUuid()
        Dim NewTask As New LoaderDownload("源码获取 " & GetUuid() & "#", New List(Of NetFile) From {New NetFile(Urls, Temp, New FileChecker With {.IsJson = IsJson}, UseBrowserUserAgent)})
        Try
            NewTask.WaitForExitTime(Timeout, TimeoutMessage:="连接服务器超时（第一下载源：" & Urls.First & "）")
            NetGetCodeByDownload = ReadFile(Temp)
            File.Delete(Temp)
        Finally
            NewTask.Abort()
        End Try
    End Function

    ''' <summary>
    ''' 从网络中直接下载文件。这不能下载 CDN 中的文件。
    ''' </summary>
    ''' <param name="Url">网络 Url。</param>
    ''' <param name="LocalFile">下载的本地地址。</param>
    Public Sub NetDownload(Url As String, LocalFile As String, Optional UseBrowserUserAgent As Boolean = False)
        Log("[Net] 直接下载文件：" & Url)

        '初始化
        Try
            '建立目录
            Directory.CreateDirectory(GetPathFromFullPath(LocalFile))
            '尝试删除原文件
            File.Delete(LocalFile)
        Catch ex As Exception
            Throw New WebException("预处理下载文件路径失败（" & LocalFile & "）。", ex)
        End Try

        '下载
        Using Client As New WebClient
            Try
                SecretHeadersSign(Url, Client, UseBrowserUserAgent)
                Client.DownloadFile(Url, LocalFile)
            Catch ex As Exception
                File.Delete(LocalFile)
                Throw New WebException("直接下载文件失败（" & Url & "）。", ex)
            End Try
        End Using

    End Sub

    ''' <summary>
    ''' 发送一个网络请求并获取返回内容，会重试三次并在最长 45s 后超时。
    ''' </summary>
    ''' <param name="Url">请求的服务器地址。</param>
    ''' <param name="Method">请求方式（POST 或 GET）。</param>
    ''' <param name="Data">请求的内容。</param>
    ''' <param name="ContentType">请求的套接字类型。</param>
    ''' <param name="DontRetryOnRefused">当返回 40x 时不重试。</param>
    Public Function NetRequestRetry(Url As String, Method As String, Data As Object, ContentType As String, Optional DontRetryOnRefused As Boolean = True, Optional Headers As Dictionary(Of String, String) = Nothing) As String
        Dim RetryCount As Integer = 0
        Dim RetryException As Exception = Nothing
        Dim StartTime As Long = GetTimeTick()
        Try
Retry:
            Select Case RetryCount
                Case 0 '正常尝试
                    Return NetRequestOnce(Url, Method, Data, ContentType, 15000, Headers)
                Case 1 '慢速重试
                    Thread.Sleep(500)
                    Return NetRequestOnce(Url, Method, Data, ContentType, 25000, Headers)
                Case Else '快速重试
                    If GetTimeTick() - StartTime > 5500 Then
                        '若前两次加载耗费 5 秒以上，才进行重试
                        Thread.Sleep(500)
                        Return NetRequestOnce(Url, Method, Data, ContentType, 4000, Headers)
                    Else
                        Throw RetryException
                    End If
            End Select
        Catch ex As ThreadInterruptedException
            Throw
        Catch ex As Exception
            If ex.InnerException IsNot Nothing AndAlso ex.InnerException.Message.Contains("(40") AndAlso DontRetryOnRefused Then Throw
            Select Case RetryCount
                Case 0
                    If ModeDebug Then Log(ex, "[Net] 网络请求第一次失败（" & Url & "）")
                    RetryException = ex
                    RetryCount += 1
                    GoTo Retry
                Case 1
                    If ModeDebug Then Log(ex, "[Net] 网络请求第二次失败（" & Url & "）")
                    RetryCount += 1
                    GoTo Retry
                Case Else
                    Throw
            End Select
        End Try
    End Function
    ''' <summary>
    ''' 同时发送多个网络请求并要求返回内容。
    ''' </summary>
    Public Function NetRequestMultiple(Url As String, Method As String, Data As Object, ContentType As String, Optional RequestCount As Integer = 4, Optional Headers As Dictionary(Of String, String) = Nothing, Optional MakeLog As Boolean = True)
        Dim Threads As New List(Of Thread)
        Dim RequestResult = Nothing
        Dim RequestEx As Exception = Nothing
        Dim FailCount As Integer = 0
        For i = 1 To RequestCount
            Dim th As New Thread(
            Sub()
                Try
                    RequestResult = NetRequestOnce(Url, Method, Data, ContentType, 30000, Headers, MakeLog)
                Catch ex As Exception
                    FailCount += 1
                    RequestEx = ex
                End Try
            End Sub)
            th.Start()
            Threads.Add(th)
            Thread.Sleep(i * 250)
            If RequestResult IsNot Nothing Then GoTo RequestFinished
        Next
        Do While True
            If RequestResult IsNot Nothing Then
RequestFinished:
                For Each th In Threads
                    If th.IsAlive Then th.Interrupt()
                Next
                Return RequestResult
            ElseIf FailCount = RequestCount Then
                For Each th In Threads
                    If th.IsAlive Then th.Interrupt()
                Next
                Throw RequestEx
            End If
            Thread.Sleep(20)
        Loop
        Throw New Exception("未知错误")
    End Function
    ''' <summary>
    ''' 发送一次网络请求并获取返回内容。
    ''' </summary>
    Public Function NetRequestOnce(Url As String, Method As String, Data As Object, ContentType As String, Optional Timeout As Integer = 25000, Optional Headers As Dictionary(Of String, String) = Nothing, Optional MakeLog As Boolean = True, Optional UseBrowserUserAgent As Boolean = False) As String
        If RunInUi() AndAlso Not Url.Contains("//127.") Then Throw New Exception("在 UI 线程执行了网络请求")
        Url = SecretCdnSign(Url)
        If MakeLog Then Log("[Net] 发起网络请求（" & Method & "，" & Url & "），最大超时 " & Timeout)
        Dim DataStream As Stream = Nothing
        Dim Resp As WebResponse = Nothing
        Dim Req As HttpWebRequest
        Try
            Req = WebRequest.Create(Url)
            Req.Method = Method
            Dim SendData As Byte()
            If TypeOf Data Is Byte() Then
                SendData = Data
            Else
                SendData = New UTF8Encoding(False).GetBytes(Data.ToString)
            End If
            If Headers IsNot Nothing Then
                For Each Pair In Headers
                    Req.Headers.Add(Pair.Key, Pair.Value)
                Next
            End If
            Req.ContentType = ContentType
            Req.Timeout = Timeout
            SecretHeadersSign(Url, Req, UseBrowserUserAgent)
            If Url.StartsWithF("https", True) Then Req.ProtocolVersion = HttpVersion.Version11
            If Method = "POST" OrElse Method = "PUT" Then
                Req.ContentLength = SendData.Length
                DataStream = Req.GetRequestStream()
                DataStream.WriteTimeout = Timeout
                DataStream.ReadTimeout = Timeout
                DataStream.Write(SendData, 0, SendData.Length)
                DataStream.Close()
            End If
            Resp = Req.GetResponse()
            DataStream = Resp.GetResponseStream()
            DataStream.WriteTimeout = Timeout
            DataStream.ReadTimeout = Timeout
            Using Reader As New StreamReader(DataStream)
                Return Reader.ReadToEnd()
            End Using
        Catch ex As ThreadInterruptedException
            Throw
        Catch ex As WebException
            If ex.Status = WebExceptionStatus.Timeout Then
                ex = New WebException($"连接服务器超时，请检查你的网络环境是否良好（{ex.Message}，{Url}）", ex)
            Else
                '获取请求失败的返回
                Dim Res As String = ""
                Try
                    If ex.Response Is Nothing Then Exit Try
                    DataStream = ex.Response.GetResponseStream()
                    DataStream.WriteTimeout = Timeout
                    DataStream.ReadTimeout = Timeout
                    Using Reader As New StreamReader(DataStream)
                        Res = Reader.ReadToEnd()
                    End Using
                Catch
                End Try
                If Res = "" Then
                    ex = New WebException($"网络请求失败（{ex.Status}，{ex.Message}，{Url}）", ex)
                Else
                    ex = New ResponsedWebException($"服务器返回错误（{ex.Status}，{ex.Message}，{Url}）{vbCrLf}{Res}", Res, ex)
                End If
            End If
            If MakeLog Then Log(ex, "NetRequestOnce 失败", LogLevel.Developer)
            Throw ex
        Catch ex As Exception
            ex = New WebException("网络请求失败（" & Url & "）", ex)
            If MakeLog Then Log(ex, "NetRequestOnce 失败", LogLevel.Developer)
            Throw ex
        Finally
            If DataStream IsNot Nothing Then DataStream.Dispose()
            If Resp IsNot Nothing Then Resp.Dispose()
        End Try
    End Function
    Public Class ResponsedWebException
        Inherits WebException
        ''' <summary>
        ''' 远程服务器给予的回复。
        ''' </summary>
        Public Overloads Property Response As String
        Public Sub New(Message As String, Response As String, InnerException As Exception)
            MyBase.New(Message, InnerException)
            Me.Response = Response
        End Sub
    End Class

    ''' <summary>
    ''' 最大线程数。
    ''' </summary>
    Public NetTaskThreadLimit As Integer
    ''' <summary>
    ''' 速度下限。
    ''' </summary>
    Public NetTaskSpeedLimitLow As Long = 256 * 1024L '256K/s
    ''' <summary>
    ''' 速度上限。若无限制则为 -1。
    ''' </summary>
    Public NetTaskSpeedLimitHigh As Long = -1
    ''' <summary>
    ''' 基于限速，当前可以下载的剩余量。
    ''' </summary>
    Public NetTaskSpeedLimitLeft As Long = -1
    Private ReadOnly NetTaskSpeedLimitLeftLock As New Object
    Private NetTaskSpeedLimitLeftLast As Long
    ''' <summary>
    ''' 正在运行中的线程数。
    ''' </summary>
    Public NetTaskThreadCount As Integer = 0
    Private ReadOnly NetTaskThreadCountLock As New Object

    ''' <summary>
    ''' 下载源。
    ''' </summary>
    Public Class NetSource
        Public Id As Integer
        Public Url As String
        Public FailCount As Integer
        Public Ex As Exception
        Public Thread As NetThread
        Public IsFailed As Boolean
        Public Overrides Function ToString() As String
            Return Url
        End Function
    End Class
    ''' <summary>
    ''' 下载进度标示。
    ''' </summary>
    Public Enum NetState
        ''' <summary>
        ''' 尚未进行已存在检查。
        ''' </summary>
        WaitForCheck = -1
        ''' <summary>
        ''' 尚未开始。
        ''' </summary>
        WaitForDownload = 0
        ''' <summary>
        ''' 正在连接，尚未获取文件大小。
        ''' </summary>
        Connect = 1
        ''' <summary>
        ''' 已获取文件大小，尚未有有效下载。
        ''' </summary>
        [Get] = 2
        ''' <summary>
        ''' 正在下载。
        ''' </summary>
        Download = 3
        ''' <summary>
        ''' 正在合并文件。
        ''' </summary>
        Merge = 4
        ''' <summary>
        ''' 不进行下载，因为已发现现存的文件。
        ''' </summary>
        WaitForCopy = 5
        ''' <summary>
        ''' 已完成。
        ''' </summary>
        Finish = 6
        ''' <summary>
        ''' 已失败或中断。
        ''' </summary>
        [Error] = 7
    End Enum
    ''' <summary>
    ''' 预下载检查行为。
    ''' </summary>
    Public Enum NetPreDownloadBehaviour
        ''' <summary>
        ''' 当文件已存在时，显示提示以提醒用户是否继续下载。
        ''' </summary>
        HintWhileExists
        ''' <summary>
        ''' 当文件已存在或正在下载时，直接退出下载函数执行，不对用户进行提示。
        ''' </summary>
        ExitWhileExistsOrDownloading
        ''' <summary>
        ''' 不进行已存在检查。
        ''' </summary>
        IgnoreCheck
    End Enum

    ''' <summary>
    ''' 下载线程。
    ''' </summary>
    Public Class NetThread
        Implements IEnumerable(Of NetThread)

        ''' <summary>
        ''' 对应的下载任务。
        ''' </summary>
        Public Task As NetFile
        ''' <summary>
        ''' 对应的线程。
        ''' </summary>
        Public Thread As Thread
        ''' <summary>
        ''' 链表中的下一个线程。
        ''' </summary>
        Public NextThread As NetThread
        Private ReadOnly Iterator Property [Next]() As IEnumerable(Of NetThread)
            Get
                Dim CurrentChain As NetThread = Me
                While CurrentChain IsNot Nothing
                    Yield CurrentChain
                    CurrentChain = CurrentChain.NextThread
                End While
            End Get
        End Property
        Public Function GetEnumerator() As IEnumerator(Of NetThread) Implements IEnumerable(Of NetThread).GetEnumerator
            Return [Next].GetEnumerator()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return [Next].GetEnumerator()
        End Function

        ''' <summary>
        ''' 分配给任务中每个线程（无论其是否失败）的编号。
        ''' </summary>
        Public Uuid As Integer
        ''' <summary>
        ''' 是否为第一个线程。
        ''' </summary>
        Public ReadOnly Property IsFirstThread As Boolean
            Get
                Return DownloadStart = 0 AndAlso Task.FileSize = -2
            End Get
        End Property
        ''' <summary>
        ''' 该线程的缓存文件。
        ''' </summary>
        Public Temp As String

        ''' <summary>
        ''' 线程下载起始位置。
        ''' </summary>
        Public DownloadStart As Long
        ''' <summary>
        ''' 线程下载结束位置。
        ''' </summary>
        Public ReadOnly Property DownloadEnd As Long
            Get
                SyncLock Task.LockChain
                    If NextThread Is Nothing Then
                        If Task.IsUnknownSize Then
                            Return 5 * 1024 * 1024 * 1024L '5G
                        Else
                            Return Task.FileSize - 1
                        End If
                    Else
                        Return NextThread.DownloadStart - 1
                    End If
                End SyncLock
            End Get
        End Property
        ''' <summary>
        ''' 线程未下载的文件大小。
        ''' </summary>
        Public ReadOnly Property DownloadUndone As Long
            Get
                Return DownloadEnd - (DownloadStart + DownloadDone) + 1
            End Get
        End Property
        ''' <summary>
        ''' 线程已下载的文件大小。
        ''' </summary>
        Public DownloadDone As Long = 0

        ''' <summary>
        ''' 上次记速时的时间。
        ''' </summary>
        Private SpeedLastTime As Long = GetTimeTick()
        ''' <summary>
        ''' 上次记速时的已下载大小。
        ''' </summary>
        Private SpeedLastDone As Long = 0
        ''' <summary>
        ''' 当前的下载速度，单位为 Byte / 秒。
        ''' </summary>
        Public ReadOnly Property Speed As Long
            Get
                If GetTimeTick() - SpeedLastTime > 200 Then
                    Dim DeltaTime As Long = GetTimeTick() - SpeedLastTime
                    _Speed = (DownloadDone - SpeedLastDone) / (DeltaTime / 1000)
                    SpeedLastDone = DownloadDone
                    SpeedLastTime += DeltaTime
                End If
                Return _Speed
            End Get
        End Property
        Private _Speed As Long = 0

        ''' <summary>
        ''' 线程初始化时的时间。
        ''' </summary>
        Public InitTime As Long = GetTimeTick()
        ''' <summary>
        ''' 上次接受到有效数据的时间，-1 表示尚未有有效数据。
        ''' </summary>
        Public LastReceiveTime As Long = -1

        ''' <summary>
        ''' 当前线程的状态。
        ''' </summary>
        Public State As NetState = NetState.WaitForDownload
        ''' <summary>
        ''' 是否已经结束。
        ''' </summary>
        Public ReadOnly Property IsEnded As Boolean
            Get
                Return State = NetState.Finish OrElse State = NetState.Error
            End Get
        End Property

        ''' <summary>
        ''' 当前选取的是哪一个 Url。
        ''' </summary>
        Public Source As NetSource

    End Class
    ''' <summary>
    ''' 下载单个文件。
    ''' </summary>
    Public Class NetFile

#Region "属性"

        ''' <summary>
        ''' 所属的文件列表任务。
        ''' </summary>
        Public Tasks As New SafeList(Of LoaderDownload)
        ''' <summary>
        ''' 所有下载源。
        ''' </summary>
        Public Sources As NetSource()
        ''' <summary>
        ''' 用于在第一个线程出错时切换下载源。
        ''' </summary>
        Private FirstThreadSource As Integer = 0
        ''' <summary>
        ''' 所有已经被标记为失败的，但未完整尝试过的，不允许断点续传的下载源。
        ''' </summary>
        Public SourcesOnce As New List(Of NetSource)
        ''' <summary>
        ''' 获取从某个源开始，第一个可用的源。
        ''' </summary>
        Private Function GetSource(Optional Id As Integer = 0) As NetSource
            If Id >= Sources.Count OrElse Id < 0 Then Id = 0
            SyncLock LockSource
                If Not IsSourceFailed(False) Then
                    '存在多线程可用源
                    Dim CurrentSource As NetSource = Sources(Id)
                    While CurrentSource.IsFailed
                        Id += 1
                        If Id >= Sources.Count Then Id = 0
                        CurrentSource = Sources(Id)
                    End While
                    Return CurrentSource
                ElseIf SourcesOnce.Any Then
                    '仅存在单线程可用源
                    Return SourcesOnce(0)
                Else
                    '没有可用源
                    Return Nothing
                End If
            End SyncLock
        End Function
        ''' <summary>
        ''' 是否已经没有可用源了。
        ''' </summary>
        Public Function IsSourceFailed(Optional AllowOnceSource As Boolean = True) As Boolean
            If AllowOnceSource AndAlso SourcesOnce.Any Then Return False
            SyncLock LockSource
                For Each Source As NetSource In Sources
                    If Not Source.IsFailed Then Return False
                Next
            End SyncLock
            Return True
        End Function

        ''' <summary>
        ''' 存储在本地的带文件名的地址。
        ''' </summary>
        Public LocalPath As String = Nothing
        ''' <summary>
        ''' 存储在本地的文件名。
        ''' </summary>
        Public LocalName As String = Nothing

        ''' <summary>
        ''' 当前的下载状态。
        ''' </summary>
        Public State As NetState = NetState.WaitForCheck
        ''' <summary>
        ''' 导致下载失败的原因。
        ''' </summary>
        Public Ex As New List(Of Exception)

        ''' <summary>
        ''' 作为文件组成部分的线程链表。
        ''' 如果没有线程，可以为 Nothing。
        ''' </summary>
        Public Threads As NetThread

        ''' <summary>
        ''' 文件的总大小。若为 -2 则为未获取，若为 -1 则为无法获取准确大小。
        ''' </summary>
        Public FileSize As Long = -2
        ''' <summary>
        ''' 该文件是否无法获取准确大小。
        ''' </summary>
        Public IsUnknownSize As Boolean = False
        ''' <summary>
        ''' 该文件是否不需要分割。
        ''' </summary>
        Public ReadOnly Property IsNoSplit As Boolean
            Get
                Return IsUnknownSize OrElse FileSize < FilePieceLimit
            End Get
        End Property
        ''' <summary>
        ''' 为不需要分割的小文件进行临时存储。
        ''' </summary>
        Private SmailFileCache As Queue(Of Byte)

        ''' <summary>
        ''' 文件的已下载大小。
        ''' </summary>
        Public DownloadDone As Long = 0
        Private ReadOnly LockDone As New Object
        ''' <summary>
        ''' 文件的校验规则。
        ''' </summary>
        Public Check As FileChecker
        ''' <summary>
        ''' 下载时是否添加浏览器 UA。
        ''' </summary>
        Public UseBrowserUserAgent As Boolean

        ''' <summary>
        ''' 上次记速时的时间。
        ''' </summary>
        Private SpeedLastTime As Long = GetTimeTick()
        ''' <summary>
        ''' 上次记速时的已下载大小。
        ''' </summary>
        Private SpeedLastDone As Long = 0
        ''' <summary>
        ''' 当前的下载速度，单位为 Byte / 秒。
        ''' </summary>
        Public ReadOnly Property Speed As Long
            Get
                If GetTimeTick() - SpeedLastTime > 200 Then
                    Dim DeltaTime As Long = GetTimeTick() - SpeedLastTime
                    _Speed = (DownloadDone - SpeedLastDone) / (DeltaTime / 1000)
                    SpeedLastDone = DownloadDone
                    SpeedLastTime += DeltaTime
                End If
                Return _Speed
            End Get
        End Property
        Private _Speed As Long = 0

        ''' <summary>
        ''' 该文件是否由本地文件直接拷贝完成。
        ''' </summary>
        Public IsCopy As Boolean = False
        ''' <summary>
        ''' 本文件的显示进度。
        ''' </summary>
        Public ReadOnly Property Progress As Double
            Get
                Select Case State
                    Case NetState.WaitForCheck
                        Return 0
                    Case NetState.WaitForCopy
                        Return 0.2
                    Case NetState.WaitForDownload
                        Return 0.01
                    Case NetState.Connect
                        Return 0.02
                    Case NetState.Get
                        Return 0.04
                    Case NetState.Download
                        '正在下载中，对应 5% ~ 98%
                        Dim OriginalProgress As Double = If(IsUnknownSize, 0.5, DownloadDone / Math.Max(FileSize, 1))
                        OriginalProgress = 1 - (1 - OriginalProgress) ^ 0.9
                        Return OriginalProgress * 0.93 + 0.05
                    Case NetState.Merge
                        Return 0.99
                    Case NetState.Finish, NetState.Error
                        Return 1
                    Case Else
                        Throw New ArgumentOutOfRangeException("文件状态未知：" & State)
                End Select
            End Get
        End Property

        ''' <summary>
        ''' 各个线程建立连接成功的总次数。
        ''' </summary>
        Private ConnectCount As Integer = 0
        ''' <summary>
        ''' 各个线程建立连接成功的总时间。
        ''' </summary>
        Private ConnectTime As Long = 0
        ''' <summary>
        ''' 各个线程建立连接成功的平均时间，单位为毫秒，-1 代表尚未有成功连接。
        ''' </summary>
        Private ReadOnly Property ConnectAverage As Integer
            Get
                SyncLock LockCount
                    Return If(ConnectCount = 0, -1, ConnectTime / ConnectCount)
                End SyncLock
            End Get
        End Property

        Private Const FilePieceLimit As Long = 256 * 1024
        Public ReadOnly LockCount As New Object
        Public ReadOnly LockState As New Object
        Public ReadOnly LockChain As New Object
        Public ReadOnly LockSource As New Object

        Public ReadOnly Uuid As Integer = GetUuid()
        Public Overrides Function Equals(obj As Object) As Boolean
            Dim file = TryCast(obj, NetFile)
            Return file IsNot Nothing AndAlso Uuid = file.Uuid
        End Function

#End Region

        ''' <summary>
        ''' 新建一个需要下载的文件。
        ''' </summary>
        ''' <param name="LocalPath">包含文件名的本地地址。</param>
        Public Sub New(Urls As IEnumerable(Of String), LocalPath As String, Optional Check As FileChecker = Nothing, Optional UseBrowserUserAgent As Boolean = False)
            Dim Sources As New List(Of NetSource)
            Dim Count As Integer = 0
            Urls = Urls.Distinct.ToArray
            For Each Source As String In Urls
                Sources.Add(New NetSource With {.FailCount = 0, .Url = SecretCdnSign(Source.Replace(vbCr, "").Replace(vbLf, "").Trim), .Id = Count, .IsFailed = False, .Ex = Nothing})
                Count += 1
            Next
            Me.Sources = Sources.ToArray
            Me.LocalPath = LocalPath
            Me.Check = Check
            Me.UseBrowserUserAgent = UseBrowserUserAgent
            Me.LocalName = GetFileNameFromPath(LocalPath)
        End Sub

        ''' <summary>
        ''' 尝试开始一个新的下载线程。
        ''' 如果失败，返回 Nothing。
        ''' </summary>
        Public Function TryBeginThread() As NetThread
            Try

                '条件检测
                If NetTaskThreadCount >= NetTaskThreadLimit OrElse IsSourceFailed() OrElse
                    (IsNoSplit AndAlso Threads IsNot Nothing AndAlso Threads.State <> NetState.Error) Then Return Nothing
                If State >= NetState.Merge OrElse State = NetState.WaitForCheck Then Return Nothing
                SyncLock LockState
                    If State < NetState.Connect Then State = NetState.Connect
                End SyncLock
                '初始化参数
                Dim StartPosition As Long, StartSource As NetSource = Nothing
                Dim Th As Thread, ThreadInfo As NetThread
                SyncLock LockChain
                    '获取线程起点与下载源
                    '不分割
                    If IsNoSplit Then GoTo Capture
                    '单线程
                    If IsSourceFailed(False) Then
                        '确认没有其他线程正使用此点
                        If SourcesOnce(0).Thread IsNot Nothing AndAlso SourcesOnce(0).Thread.State <> NetState.Error Then Return Nothing
                        '占用此点
Capture:
                        SmailFileCache = Nothing
                        Threads = Nothing
                        NetManager.DownloadDone -= DownloadDone
                        SyncLock LockDone
                            DownloadDone = 0
                        End SyncLock
                        SpeedLastDone = 0
                        State = NetState.Get
                    End If
                    '首个开始点
                    If Threads Is Nothing Then
                        StartPosition = 0
                        StartSource = GetSource(FirstThreadSource)
                        FirstThreadSource = StartSource.Id + 1
                        GoTo StartThread
                    End If
                    '寻找失败点
                    For Each Thread As NetThread In Threads
                        If Thread.State = NetState.Error AndAlso Thread.DownloadUndone > 0 Then
                            StartPosition = Thread.DownloadStart + Thread.DownloadDone
                            StartSource = GetSource(Thread.Source.Id + 1)
                            GoTo StartThread
                        End If
                    Next
                    '是否禁用多线程，以及规定碎片大小
                    Dim TargetUrl As String = GetSource().Url
                    If TargetUrl.Contains("pcl2-server") OrElse TargetUrl.Contains("mcimirror") OrElse TargetUrl.Contains("github.com") OrElse
                       TargetUrl.Contains("optifine.net") OrElse TargetUrl.Contains("modrinth") Then Return Nothing
                    '寻找最大碎片
                    'FUTURE: 下载引擎重做，计算下载源平均链接时间和线程下载速度，按最高时间节省来开启多线程
                    Dim FilePieceMax As NetThread = Threads
                    For Each Thread As NetThread In Threads
                        If Thread.DownloadUndone > FilePieceMax.DownloadUndone Then FilePieceMax = Thread
                    Next
                    If FilePieceMax Is Nothing OrElse FilePieceMax.DownloadUndone < FilePieceLimit Then Return Nothing
                    StartPosition = FilePieceMax.DownloadEnd - FilePieceMax.DownloadUndone * 0.4
                    StartSource = GetSource()

                    '开始线程
StartThread:
                    If (StartPosition > FileSize AndAlso FileSize >= 0 AndAlso Not IsUnknownSize) OrElse StartPosition < 0 OrElse IsNothing(StartSource) Then Return Nothing
                    '构建线程
                    Dim ThreadUuid As Integer = GetUuid()
                    If Not Tasks.Any() Then Return Nothing '由于中断，已没有可用任务
                    Th = New Thread(AddressOf Thread) With {.Name = $"NetTask {Tasks(0).Uuid}/{Uuid} Download {ThreadUuid}#", .Priority = ThreadPriority.BelowNormal}
                    ThreadInfo = New NetThread With {.Uuid = ThreadUuid, .DownloadStart = StartPosition, .Thread = Th, .Source = StartSource, .Task = Me, .State = NetState.WaitForDownload}
                    '链表处理
                    If ThreadInfo.IsFirstThread OrElse Threads Is Nothing Then
                        Threads = ThreadInfo
                    Else
                        Dim CurrentChain As NetThread = Threads
                        While CurrentChain.DownloadEnd <= StartPosition
                            CurrentChain = CurrentChain.NextThread
                        End While
                        ThreadInfo.NextThread = CurrentChain.NextThread
                        CurrentChain.NextThread = ThreadInfo
                    End If

                End SyncLock
                '开始线程
                SyncLock NetTaskThreadCountLock
                    NetTaskThreadCount += 1
                End SyncLock
                SyncLock LockSource
                    If IsSourceFailed(False) Then SourcesOnce(0).Thread = ThreadInfo
                End SyncLock
                Th.Start(ThreadInfo)
                Return ThreadInfo

            Catch ex As Exception
                Log(ex, "尝试开始下载线程失败（" & If(LocalName, "Nothing") & "）", LogLevel.Hint)
                Return Nothing
            End Try
        End Function
        ''' <summary>
        ''' 每个下载线程执行的代码。
        ''' </summary>
        Private Sub Thread(Info As NetThread)
            If ModeDebug OrElse Info.DownloadStart = 0 Then Log("[Download] " & LocalName & " " & Info.Uuid & "#：开始，起始点 " & Info.DownloadStart & "，" & Info.Source.Url)
            Dim HttpRequest As HttpWebRequest
            Dim ResultStream As Stream = Nothing
            '部分下载源真的特别慢，并且只需要一个请求，例如 Ping 为 20s，如果增长太慢，就会造成类似 2.5s 5s 7.5s 10s 12.5s... 的极大延迟
            '延迟过长会导致某些特别慢的链接迟迟不被掐死
            Dim Timeout As Integer = Math.Min(Math.Max(ConnectAverage, 6000) * (1 + Info.Source.FailCount), 30000)
            Info.State = NetState.Connect
            Try
                Dim HttpDataCount As Integer = 0
                If SourcesOnce.Contains(Info.Source) AndAlso Not Info.Equals(Info.Source.Thread) Then GoTo SourceBreak
                '请求头
                HttpRequest = WebRequest.Create(Info.Source.Url)
                If Info.Source.Url.StartsWithF("https", True) Then HttpRequest.ProtocolVersion = HttpVersion.Version11
                'HttpRequest.Proxy = Nothing 'new WebProxy(Ip, Port)
                HttpRequest.Timeout = Timeout
                HttpRequest.AddRange(Info.DownloadStart)
                SecretHeadersSign(Info.Source.Url, HttpRequest, UseBrowserUserAgent)
                Dim ContentLength As Long = 0
                Using HttpResponse As HttpWebResponse = HttpRequest.GetResponse()
                    If State = NetState.Error Then GoTo SourceBreak '快速中断
                    If ModeDebug AndAlso HttpResponse.ResponseUri.OriginalString <> Info.Source.Url Then
                        Log($"[Download] {LocalName} {Info.Uuid}#：重定向至 {HttpResponse.ResponseUri.OriginalString}")
                    End If
                    ''从响应头获取文件名
                    'If Info.IsFirstThread Then
                    '    Dim FileName As String = GetFileNameFromResponse(HttpResponse)
                    '    If ModeDebug Then Log($"[Download] {LocalName} {Info.Uuid}#：远程文件名：{If(FileName, "未提供")}")
                    '    If FileName IsNot Nothing AndAlso LocalName = "待定" Then
                    '        LocalName = FileName
                    '        Log($"[Download] {LocalName} {Info.Uuid}#：从响应头获取到文件名")
                    '    End If
                    'End If
                    '文件大小校验
                    ContentLength = HttpResponse.ContentLength
                    If ContentLength = -1 Then
                        If FileSize > 1 Then
                            If Info.DownloadStart = 0 Then
                                Log($"[Download] {LocalName} {Info.Uuid}#：文件大小未知，但已从其他下载源获取，不作处理")
                            Else
                                Log($"[Download] {LocalName} {Info.Uuid}#：ContentLength 返回了 -1，无法确定是否支持分段下载，视作不支持")
                                GoTo NotSupportRange
                            End If
                        Else
                            FileSize = -1 : IsUnknownSize = True
                            Log($"[Download] {LocalName} {Info.Uuid}#：文件大小未知")
                        End If
                    ElseIf ContentLength < 0 Then
                        Throw New Exception("获取片大小失败，结果为 " & ContentLength & "。")
                    ElseIf Info.IsFirstThread Then
                        If Check IsNot Nothing Then
                            If ContentLength < Check.MinSize AndAlso Check.MinSize > 0 Then
                                Throw New Exception($"文件大小不足，获取结果为 {ContentLength}，要求至少为 {Check.MinSize}。")
                            End If
                            If ContentLength <> Check.ActualSize AndAlso Check.ActualSize > 0 Then
                                Throw New Exception($"文件大小不一致，获取结果为 {ContentLength}，要求必须为 {Check.ActualSize}。")
                            End If
                        End If
                        FileSize = ContentLength : IsUnknownSize = False
                        Log($"[Download] {LocalName} {Info.Uuid}#：文件大小 {ContentLength}（{GetString(ContentLength)}）")
                        '若文件大小大于 50 M，进行剩余磁盘空间校验
                        If ContentLength > 50 * 1024 * 1024 Then
                            For Each Drive As DriveInfo In DriveInfo.GetDrives
                                Dim DriveName As String = Drive.Name.First.ToString
                                Dim RequiredSpace = If(PathTemp.StartsWithF(DriveName), ContentLength * 1.1, 0) +
                                                If(LocalPath.StartsWithF(DriveName), ContentLength + 5 * 1024 * 1024, 0)
                                If Drive.TotalFreeSpace < RequiredSpace Then
                                    Throw New Exception(DriveName & " 盘空间不足，无法进行下载。" & vbCrLf & "需要至少 " & GetString(RequiredSpace) & " 空间，但当前仅剩余 " & GetString(Drive.TotalFreeSpace) & "。" &
                                                    If(PathTemp.StartsWithF(DriveName), vbCrLf & vbCrLf & "下载时需要与文件同等大小的空间存放缓存，你可以在设置中调整缓存文件夹的位置。", ""))
                                End If
                            Next
                        End If
                    ElseIf FileSize < 0 Then
                        Throw New Exception("非首线程运行时，尚未获取文件大小")
                    ElseIf Info.DownloadStart > 0 AndAlso ContentLength = FileSize Then
NotSupportRange:
                        SyncLock LockSource
                            If SourcesOnce.Contains(Info.Source) Then
                                GoTo SourceBreak
                            Else
                                SourcesOnce.Add(Info.Source)
                            End If
                        End SyncLock
                        Throw New WebException($"该下载源不支持分段下载：Range 起始于 {Info.DownloadStart}，预期 ContentLength 为 {FileSize - Info.DownloadStart}，返回 ContentLength 为 {ContentLength}，总文件大小 {FileSize}")
                    ElseIf Not FileSize - Info.DownloadStart = ContentLength Then
                        Throw New WebException($"获取到的分段大小不一致：Range 起始于 {Info.DownloadStart}，预期 ContentLength 为 {FileSize - Info.DownloadStart}，返回 ContentLength 为 {ContentLength}，总文件大小 {FileSize}")
                    End If
                    'Log($"[Download] {LocalName} {Info.Uuid}#：通过大小检查，文件大小 {FileSize}，起始点 {Info.DownloadStart}，ContentLength {ContentLength}")
                    Info.State = NetState.Get
                    SyncLock LockState
                        If State < NetState.Get Then State = NetState.Get
                    End SyncLock
                    '创建缓存文件
                    If IsNoSplit Then
                        Info.Temp = Nothing
                        SmailFileCache = New Queue(Of Byte)
                    Else
                        Info.Temp = PathTemp & "Download\" & Uuid & "_" & Info.Uuid & "_" & RandomInteger(0, 999999) & ".tmp"
                        ResultStream = New FileStream(Info.Temp, FileMode.Create, FileAccess.Write, FileShare.Read)
                    End If
                    '开始下载
                    Using HttpStream = HttpResponse.GetResponseStream()
                        HttpStream.ReadTimeout = Timeout
                        If Setup.Get("SystemDebugDelay") Then Threading.Thread.Sleep(RandomInteger(50, 3000))
                        Dim HttpData As Byte() = New Byte(16384) {}
                        HttpDataCount = HttpStream.Read(HttpData, 0, 16384)
                        While (IsUnknownSize OrElse Info.DownloadUndone > 0) AndAlso '判断是否下载完成
                            HttpDataCount > 0 AndAlso Not IsProgramEnded AndAlso State < NetState.Merge AndAlso (Not Info.Source.IsFailed OrElse Info.Equals(Info.Source.Thread))
                            '限速
                            While NetTaskSpeedLimitHigh > 0 AndAlso NetTaskSpeedLimitLeft <= 0
                                Threading.Thread.Sleep(16)
                            End While
                            Dim RealDataCount As Integer = If(IsUnknownSize, HttpDataCount, Math.Min(HttpDataCount, Info.DownloadUndone))
                            SyncLock NetTaskSpeedLimitLeftLock
                                If NetTaskSpeedLimitHigh > 0 Then NetTaskSpeedLimitLeft -= RealDataCount
                            End SyncLock
                            Dim DeltaTime = GetTimeTick() - Info.LastReceiveTime
                            If DeltaTime > 1000000 Then DeltaTime = 1 '时间刻反转导致出现极大值
                            If RealDataCount > 0 Then
                                '有数据
                                If Info.DownloadDone = 0 Then
                                    '第一次接受到数据
                                    Info.State = NetState.Download
                                    SyncLock LockState
                                        If State < NetState.Download Then State = NetState.Download
                                    End SyncLock
                                    SyncLock LockCount
                                        ConnectCount += 1
                                        ConnectTime += GetTimeTick() - Info.InitTime
                                    End SyncLock
                                End If
                                SyncLock LockCount
                                    Info.Source.FailCount = 0
                                    For Each Task In Tasks
                                        Task.FailCount = 0
                                    Next
                                End SyncLock
                                NetManager.DownloadDone += RealDataCount
                                SyncLock LockDone
                                    DownloadDone += RealDataCount
                                End SyncLock
                                Info.DownloadDone += RealDataCount
                                If IsNoSplit Then
                                    If HttpData.Count = RealDataCount Then
                                        'SmailFileCache.AddRange(HttpData)
                                        For Each B In HttpData
                                            SmailFileCache.Enqueue(B)
                                        Next
                                    Else
                                        'SmailFileCache.AddRange(HttpData.ToList.GetRange(0, RealDataCount))
                                        For i = 0 To RealDataCount - 1
                                            SmailFileCache.Enqueue(HttpData(i))
                                        Next
                                    End If
                                Else
                                    ResultStream.Write(HttpData, 0, RealDataCount)
                                End If
                                '检查速度是否过慢
                                If DeltaTime > 1500 AndAlso DeltaTime > RealDataCount Then '数据包间隔大于 1.5s，且速度小于 1.5K/s
                                    Throw New TimeoutException("由于速度过慢断开链接，下载 " & RealDataCount & " B，消耗 " & DeltaTime & " ms。")
                                End If
                                Info.LastReceiveTime = GetTimeTick()
                                '已完成
                                If Info.DownloadUndone = 0 AndAlso Not IsUnknownSize Then Exit While
                            ElseIf Info.LastReceiveTime > 0 AndAlso DeltaTime > Timeout Then
                                '无数据，且已超时
                                Throw New TimeoutException("操作超时，无数据。")
                            End If
                            HttpDataCount = HttpStream.Read(HttpData, 0, 16384)
                        End While
                    End Using
                End Using
SourceBreak:
                If State = NetState.Error OrElse Info.Source.IsFailed OrElse (Info.DownloadUndone > 0 AndAlso Not IsUnknownSize) Then
                    '被外部中断
                    Info.State = NetState.Error
                    Log($"[Download] {LocalName} {Info.Uuid}#：中断")
                ElseIf HttpDataCount = 0 AndAlso Info.DownloadUndone > 0 AndAlso Not IsUnknownSize Then
                    '服务器无返回数据
                    Throw New Exception($"返回的 ContentLength 过多：ContentLength 为 {ContentLength}，但获取到的总数据量仅为 {Info.DownloadDone}（全文件总数据量 {DownloadDone}）")
                Else
                    '本线程完成
                    Info.State = NetState.Finish
                    If ModeDebug Then Log($"[Download] {LocalName} {Info.Uuid}#：完成，已下载 {Info.DownloadDone}")
                End If
            Catch ex As Exception
                '状态变更
                SyncLock LockCount
                    Info.Source.FailCount += 1
                    For Each Task In Tasks
                        Task.FailCount += 1
                    Next
                End SyncLock
                Dim IsTimeoutString As String = GetExceptionSummary(ex).ToLower.Replace(" ", "")
                Dim IsTimeout As Boolean = IsTimeoutString.Contains("由于连接方在一段时间后没有正确答复或连接的主机没有反应") OrElse
                                           IsTimeoutString.Contains("超时") OrElse IsTimeoutString.Contains("timeout") OrElse IsTimeoutString.Contains("timedout")
                Log("[Download] " & LocalName & " " & Info.Uuid & If(IsTimeout, "#：超时（" & (Timeout * 0.001) & "s）", "#：出错，" & GetExceptionDetail(ex)))
                Info.State = NetState.Error
                ''使用该下载源的线程是否没有速度
                ''下载超时也会导致没有速度，容易误判下载失败，所以已弃用此方法
                'Dim IsNoSpeed As Boolean = True
                'SyncLock LockChain
                '    If Threads IsNot Nothing Then
                '        For Each Thread As NetThread In Threads
                '            If Thread.Source.Id = Info.Source.Id AndAlso Thread.Speed > 0 Then
                '                IsNoSpeed = False
                '                Exit For
                '            End If
                '        Next
                '    End If
                'End SyncLock
                Info.Source.Ex = ex
                '根据情况判断，是否在多线程下禁用下载源（连续错误过多，或不支持断点续传）
                If ex.Message.Contains("该下载源不支持") OrElse ex.Message.Contains("未能解析") OrElse ex.Message.Contains("(404)") OrElse
                   ex.Message.Contains("(502)") OrElse ex.Message.Contains("无返回数据") OrElse ex.Message.Contains("空间不足") OrElse ex.Message.Contains("获取到的分段大小不一致") OrElse
                   (ex.Message.Contains("(403)") AndAlso Not Info.Source.Url.ContainsF("bmclapi")) OrElse 'BMCLAPI 的部分源在高频率请求下会返回 403，所以不应因此禁用下载源
                   (Info.Source.FailCount >= MathClamp(NetTaskThreadLimit, 5, 30) AndAlso DownloadDone < 1) OrElse
                    Info.Source.FailCount > NetTaskThreadLimit + 2 Then
                    Dim IsThisFail As Boolean = False
                    SyncLock LockSource
                        If Info.Source.Thread IsNot Nothing AndAlso Info.Source.Thread.Equals(Info) Then
                            '单线程下，本线程出错
                            SourcesOnce.RemoveAt(0)
                            GoTo Wrong
                        ElseIf Not Info.Source.IsFailed Then
                            '多线程下，本线程出错
Wrong:
                            Info.Source.IsFailed = True
                            IsThisFail = True
                        End If
                    End SyncLock
                    '本线程引发下载源被禁用
                    If IsThisFail Then
                        Log($"[Download] {LocalName} {Info.Uuid}#：下载源被禁用（{Info.Source.Id}）：{Info.Source.Url}")
                        Log(ex, "下载源 " & Info.Source.Id & " 已被禁用", If(ex.Message.Contains("不支持分段下载") OrElse ex.Message.Contains("(404)") OrElse ex.Message.Contains("(416)"), LogLevel.Developer, LogLevel.Debug))
                        If IsSourceFailed() Then
                            '没有可用源
                            Log("[Download] 文件 " & LocalName & " 已无可用下载源")
                            Dim ExampleEx As Exception = Nothing
                            SyncLock LockSource
                                For Each Source As NetSource In Sources
                                    Log("[Download] 已禁用的下载源：" & Source.Url)
                                    If Source.Ex IsNot Nothing Then
                                        ExampleEx = Source.Ex
                                        Log(Source.Ex, "下载源禁用原因", LogLevel.Developer)
                                    End If
                                Next
                            End SyncLock
                            Fail(ExampleEx)
                        ElseIf ex.Message.Contains("空间不足") Then
                            '没有空间
                            Fail(ex)
                        End If
                    End If
                End If
                '首线程错误
                If FileSize = -2 Then
                    SyncLock LockChain
                        Threads = Nothing
                    End SyncLock
                End If
            Finally
                If ResultStream IsNot Nothing Then ResultStream.Dispose()
                SyncLock NetTaskThreadCountLock
                    NetTaskThreadCount -= 1
                End SyncLock
                '可能在没有下载完的时候开始合并文件了，这造成了大多数合并失败
                If ((FileSize >= 0 AndAlso DownloadDone >= FileSize) OrElse (FileSize = -1 AndAlso DownloadDone > 0)) AndAlso State < NetState.Merge Then Merge()
            End Try
        End Sub
        ''' <summary>
        ''' 从 HTTP 响应头中获取文件名。
        ''' 如果没有，返回 Nothing。
        ''' </summary>
        Private Function GetFileNameFromResponse(response As HttpWebResponse) As String
            Dim header As String = response.Headers("Content-Disposition")
            If String.IsNullOrEmpty(header) Then Return Nothing
            'attachment; filename="filename.ext"
            If Not header.Contains("filename=") Then Return Nothing
            Return header.AfterLast("filename=").Trim(""""c, " "c).BeforeFirst(";")
        End Function

        '下载文件的最终收束事件
        ''' <summary>
        ''' 下载完成。合并文件。
        ''' </summary>
        Private Sub Merge()
            '状态判断
            SyncLock LockState
                If State < NetState.Merge Then
                    State = NetState.Merge
                Else
                    Exit Sub
                End If
            End SyncLock
            Dim RetryCount As Integer = 0
            Dim MergeFile As Stream = Nothing, AddWriter As BinaryWriter = Nothing
            Try
Retry:
                SyncLock LockChain
                    '创建文件夹
                    If File.Exists(LocalPath) Then File.Delete(LocalPath)
                    Dim Info As New FileInfo(LocalPath)
                    Info.Directory.Create()
                    '合并文件
                    If IsNoSplit Then
                        '仅有一个线程，从缓存中输出
                        If ModeDebug Then Log($"[Download] {LocalName}：下载结束，从缓存输出文件，长度：" & SmailFileCache.Count)
                        MergeFile = New FileStream(LocalPath, FileMode.Create)
                        AddWriter = New BinaryWriter(MergeFile)
                        AddWriter.Write(SmailFileCache.ToArray)
                        AddWriter.Dispose() : AddWriter = Nothing
                        MergeFile.Dispose() : MergeFile = Nothing
                    ElseIf Threads.DownloadDone = DownloadDone AndAlso Threads.Temp IsNot Nothing Then
                        '仅有一个文件，直接复制
                        If ModeDebug Then Log($"[Download] {LocalName}：下载结束，仅有一个文件，无需合并")
                        CopyFile(Threads.Temp, LocalPath)
                    Else
                        '有多个线程，合并
                        If ModeDebug Then Log($"[Download] {LocalName}：下载结束，开始合并文件")
                        MergeFile = New FileStream(LocalPath, FileMode.Create)
                        AddWriter = New BinaryWriter(MergeFile)
                        For Each Thread As NetThread In Threads
                            If Thread.DownloadDone = 0 OrElse Thread.Temp Is Nothing Then Continue For
                            Using fs As New FileStream(Thread.Temp, FileMode.Open, FileAccess.Read, FileShare.Read)
                                Using TempReader As New BinaryReader(fs)
                                    AddWriter.Write(TempReader.ReadBytes(Thread.DownloadDone))
                                End Using
                            End Using
                        Next
                        AddWriter.Dispose() : AddWriter = Nothing
                        MergeFile.Dispose() : MergeFile = Nothing
                    End If
                    '写入大小要求
                    If Not IsUnknownSize AndAlso Check IsNot Nothing Then
                        If Check.ActualSize = -1 Then
                            Check.ActualSize = FileSize
                        ElseIf Check.ActualSize <> FileSize Then
                            Throw New Exception($"文件大小不一致：任务要求为 {Check.ActualSize} B，网络获取结果为 {FileSize}B")
                        End If
                    End If
                    '检查文件
                    Dim CheckResult As String = Check?.Check(LocalPath)
                    If CheckResult IsNot Nothing Then
                        Log($"[Download] {LocalName} 文件校验失败，下载线程细节：")
                        For Each Th As NetThread In Threads
                            Log($"[Download]     {Th.Uuid}#，状态 {GetStringFromEnum(Th.State)}，范围 {Th.DownloadStart}~{Th.DownloadStart + Th.DownloadDone}，完成 {Th.DownloadDone}，剩余 {Th.DownloadUndone}")
                        Next
                        Throw New Exception(CheckResult)
                    End If
                    '后处理
                    If IsNoSplit Then
                        SmailFileCache = Nothing
                    Else
                        For Each Thread As NetThread In Threads
                            If Thread.Temp IsNot Nothing Then File.Delete(Thread.Temp)
                        Next
                    End If
                    Finish()
                End SyncLock
            Catch ex As Exception
                Log(ex, "合并文件出错（" & LocalName & "）")
                If MergeFile IsNot Nothing Then
                    MergeFile.Dispose() : MergeFile = Nothing
                End If
                If AddWriter IsNot Nothing Then
                    AddWriter.Dispose() : AddWriter = Nothing
                End If
                '重试
                If RetryCount <= 3 Then
                    Threading.Thread.Sleep(RandomInteger(500, 1000))
                    RetryCount += 1
                    GoTo Retry
                End If
                Fail(ex)
            End Try
        End Sub
        ''' <summary>
        ''' 下载失败。
        ''' </summary>
        Private Sub Fail(Optional RaiseEx As Exception = Nothing)
            SyncLock LockState
                If State >= NetState.Finish Then Exit Sub
                If RaiseEx IsNot Nothing Then Ex.Add(RaiseEx)
                '凉凉
                State = NetState.Error
            End SyncLock
            InterruptAndDelete()
            For Each Task In Tasks
                Task.OnFileFail(Me)
            Next
        End Sub
        ''' <summary>
        ''' 下载中断。
        ''' </summary>
        Public Sub Abort(CausedByTask As LoaderDownload)
            '从特定任务中移除，如果它还属于其他任务，则继续下载
            Tasks.Remove(CausedByTask)
            If Tasks.Any Then Exit Sub
            '确认中断
            SyncLock LockState
                If State >= NetState.Finish Then Exit Sub
                State = NetState.Error
            End SyncLock
            InterruptAndDelete()
        End Sub
        Private Sub InterruptAndDelete()
            On Error Resume Next
            If File.Exists(LocalPath) Then File.Delete(LocalPath)
            SyncLock NetManager.LockRemain
                NetManager.FileRemain -= 1
                Log($"[Download] {LocalName}：状态 {State}，剩余文件 {NetManager.FileRemain}")
            End SyncLock
        End Sub

        '状态改变接口
        ''' <summary>
        ''' 将该文件设置为已下载完成。
        ''' </summary>
        Public Sub Finish(Optional PrintLog As Boolean = True)
            SyncLock LockState
                If State >= NetState.Finish Then Exit Sub
                State = NetState.Finish
            End SyncLock
            SyncLock NetManager.LockRemain
                NetManager.FileRemain -= 1
                If PrintLog Then Log("[Download] " & LocalName & "：已完成，剩余文件 " & NetManager.FileRemain)
            End SyncLock
            For Each Task In Tasks
                Task.OnFileFinish(Me)
            Next
        End Sub

    End Class
    ''' <summary>
    ''' 下载一系列文件的加载器。
    ''' </summary>
    Public Class LoaderDownload
        Inherits LoaderBase

#Region "属性"

        ''' <summary>
        ''' 需要下载的文件。
        ''' </summary>
        Public Files As SafeList(Of NetFile)
        ''' <summary>
        ''' 剩余未完成的文件数。（用于减轻 FilesLock 的占用）
        ''' </summary>
        Private FileRemain As Integer
        Private ReadOnly FileRemainLock As New Object

        ''' <summary>
        ''' 用于显示的百分比进度。
        ''' </summary>
        Public Overrides Property Progress As Double
            Get
                If State >= LoadState.Finished Then Return 1
                If Not Files.Any() Then Return 0 '必须返回 0，否则在获取列表的时候会错觉已经下载完了
                Return _Progress
            End Get
            Set(value As Double)
                Throw New Exception("文件下载不允许指定进度")
            End Set
        End Property
        Private _Progress As Double = 0

        ''' <summary>
        ''' 任务中的文件的连续失败计数。
        ''' </summary>
        Public Property FailCount As Integer
            Get
                Return _FailCount
            End Get
            Set(value As Integer)
                _FailCount = value
                If State = LoadState.Loading AndAlso value >= Math.Min(10000, Math.Max(FileRemain * 5.5, NetTaskThreadLimit * 5.5 + 3)) Then
                    Log("[Download] 由于同加载器中失败次数过多引发强制失败：连续失败了 " & value & " 次", LogLevel.Debug)
                    On Error Resume Next
                    Dim ExList As New List(Of Exception)
                    For Each File In Files
                        For Each Source In File.Sources
                            If Source.Ex IsNot Nothing Then
                                ExList.Add(Source.Ex)
                                If ExList.Count > 10 Then GoTo FinishExCatch
                            End If
                        Next
                    Next
FinishExCatch:
                    OnFail(ExList)
                End If
            End Set
        End Property
        Private _FailCount As Integer = 0

#End Region

        ''' <summary>
        ''' 刷新公开属性。由 NetManager 每 0.1 秒调用一次。
        ''' </summary>
        Public Sub RefreshStat()
            '计算进度
            Dim NewProgress As Double = 0
            Dim TotalProgress As Double = 0
            For Each File In Files
                If File.IsCopy Then
                    NewProgress += File.Progress * 0.2
                    TotalProgress += 0.2
                Else
                    NewProgress += File.Progress
                    TotalProgress += 1
                End If
            Next
            If TotalProgress > 0 Then NewProgress /= TotalProgress
            '刷新进度
            If NewProgress < 1 AndAlso NewProgress > 0 Then NewProgress = 2 * (NewProgress ^ 3) - 3 * (NewProgress ^ 2) + 2 * NewProgress '2x^3-3x^2+2x，模拟开头和结尾更慢的情况
            _Progress = NewProgress
        End Sub

        Public Sub New(Name As String, FileTasks As List(Of NetFile))
            Me.Name = Name
            Files = New SafeList(Of NetFile)(FileTasks)
        End Sub
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            If Input IsNot Nothing Then Files = New SafeList(Of NetFile)(Input)
            '去重
            Dim ResultArray As New SafeList(Of NetFile)
            For i = 0 To Files.Count - 1
                For ii = i + 1 To Files.Count - 1
                    If Files(i).LocalPath = Files(ii).LocalPath Then GoTo NextElement
                Next
                ResultArray.Add(Files(i))
NextElement:
            Next
            Files = ResultArray
            '设置剩余文件数
            SyncLock FileRemainLock
                For Each File In Files
                    If File.State <> NetState.Finish Then FileRemain += 1
                Next
            End SyncLock
            State = LoadState.Loading
            '开始执行
            RunInNewThread(
            Sub()
                Try
                    '输入检测
                    If Not Files.Any() Then
                        OnFinish()
                        Exit Sub
                    End If
                    For Each File As NetFile In Files
                        If File Is Nothing Then Throw New ArgumentException("存在空文件请求！")
                        For Each Source As NetSource In File.Sources
                            If Not (Source.Url.StartsWithF("https://", True) OrElse Source.Url.StartsWithF("http://", True)) Then
                                Source.Ex = New ArgumentException("输入的下载链接不正确！")
                                Source.IsFailed = True
                            End If
                        Next
                        If File.IsSourceFailed() Then Throw New ArgumentException("输入的下载链接不正确！")
                        If Not File.LocalPath.ToLower.Contains(":\") Then Throw New ArgumentException("输入的本地文件地址不正确！")
                        If File.LocalPath.EndsWithF("\") Then Throw New ArgumentException("请输入含文件名的完整文件路径！")
                        '文件夹检测
                        Dim DirPath As String = New FileInfo(File.LocalPath).Directory.FullName
                        If Not Directory.Exists(DirPath) Then Directory.CreateDirectory(DirPath)
                    Next
                    '接入下载管理器
                    NetManager.Start(Me)
                    '将文件分配给多个线程以进行已存在查找
                    Dim Folders As New List(Of String) '可能会用于已存在查找的文件夹列表
                    Dim FoldersFinal As New List(Of String) '最终用于查找的列表
                    If Not Setup.Get("SystemDebugSkipCopy") Then '在设置中禁用复制
                        Folders.Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\.minecraft\") '总是添加官启文件夹，因为 HMCL 会把所有文件存在这里
                        For Each Folder In McFolderList
                            Folders.Add(Folder.Path)
                        Next
                        Folders = Folders.Distinct.ToList
                        For Each Folder In Folders
                            If Folder <> PathMcFolder AndAlso Directory.Exists(Folder) Then FoldersFinal.Add(Folder)
                        Next
                    End If
                    '最多 5 个线程，最少每个线程分配 10 个文件
                    Dim FilesPerThread As Integer = Math.Max(5, Files.Count / 10 + 1)
                    Dim FilesInThread As New List(Of NetFile)
                    For Each File In Files
                        FilesInThread.Add(File)
                        If FilesInThread.Count = FilesPerThread Then
                            Dim FilesToRun As New List(Of NetFile)
                            FilesToRun.AddRange(FilesInThread)
                            RunInNewThread(Sub() StartCopy(FilesToRun, FoldersFinal), "NetTask FileCopy " & Uuid)
                            FilesInThread.Clear()
                        End If
                    Next
                    If FilesInThread.Any Then
                        Dim FilesToRun As New List(Of NetFile)
                        FilesToRun.AddRange(FilesInThread)
                        RunInNewThread(Sub() StartCopy(FilesToRun, FoldersFinal), "NetTask FileCopy " & Uuid)
                        FilesInThread.Clear()
                    End If
                Catch ex As Exception
                    OnFail(New List(Of Exception) From {ex})
                End Try
            End Sub, "NetTask " & Uuid & " Main")
        End Sub
        Private Sub StartCopy(Files As List(Of NetFile), FolderList As List(Of String))
            Try
                If ModeDebug Then Log("[Download] 检查线程分配文件数：" & Files.Count & "，线程名：" & Thread.CurrentThread.Name)
                '试图从已存在的 Minecraft 文件夹中寻找目标文件
                Dim ExistFiles As New List(Of KeyValuePair(Of NetFile, String)) '{NetFile, Target As String}
                For Each File As NetFile In Files
                    Dim ExistFilePath As String = Nothing
                    '判断是否有已存在的文件
                    If File.Check IsNot Nothing AndAlso McFolderList IsNot Nothing AndAlso PathMcFolder IsNot Nothing AndAlso File.Check.CanUseExistsFile AndAlso File.LocalPath.StartsWithF(PathMcFolder) Then
                        Dim Relative = File.LocalPath.Replace(PathMcFolder, "")
                        For Each Folder In FolderList
                            Dim Target = Folder & Relative
                            If File.Check.Check(Target) Is Nothing Then
                                ExistFilePath = Target
                                Exit For
                            End If
                        Next
                    End If
                    '若存在，则改变状态
                    SyncLock LockState
                        If ExistFilePath IsNot Nothing Then
                            File.State = NetState.WaitForCopy
                            File.IsCopy = True
                            ExistFiles.Add(New KeyValuePair(Of NetFile, String)(File, ExistFilePath))
                        Else
                            File.State = NetState.WaitForDownload
                            File.IsCopy = False
                        End If
                    End SyncLock
                Next
                '复制已存在的文件
                For Each FileToken In ExistFiles
                    Dim File As NetFile = FileToken.Key
                    SyncLock LockState
                        If File.State > NetState.WaitForCopy Then Exit Sub
                    End SyncLock
                    Dim LocalPath As String = FileToken.Value
                    Dim RetryCount As Integer = 0
Retry:
                    Try
                        Log("[Download] 复制已存在的文件（" & LocalPath & "）")
                        CopyFile(LocalPath, File.LocalPath)
                        File.Finish(False)
                    Catch ex As Exception
                        RetryCount += 1
                        Log(ex, String.Format("复制已存在的文件失败，重试第 {2} 次（{0} -> {1}）", LocalPath, File.LocalPath, RetryCount))
                        If RetryCount < 3 Then
                            Thread.Sleep(200)
                            GoTo Retry
                        End If
                        File.State = NetState.WaitForDownload
                        File.IsCopy = False
                    End Try
                Next
            Catch ex As Exception
                Log(ex, "下载已存在文件查找失败", LogLevel.Feedback)
            End Try
        End Sub

        Public Sub OnFileFinish(File As NetFile)
            '要求全部文件完成
            SyncLock FileRemainLock
                FileRemain -= 1
                If FileRemain > 0 Then Exit Sub
            End SyncLock
            OnFinish()
        End Sub
        Public Sub OnFinish()
            RaisePreviewFinish()
            SyncLock LockState
                If State > LoadState.Loading Then Exit Sub
                State = LoadState.Finished
            End SyncLock
        End Sub
        Public Sub OnFileFail(File As NetFile)
            '将下载源的错误加入主错误列表
            For Each Source In File.Sources
                If Not IsNothing(Source.Ex) Then File.Ex.Add(Source.Ex)
            Next
            OnFail(File.Ex)
        End Sub
        Public Sub OnFail(ExList As List(Of Exception))
            SyncLock LockState
                If State > LoadState.Loading Then Exit Sub
                If ExList Is Nothing OrElse Not ExList.Any() Then ExList = New List(Of Exception) From {New Exception("未知错误！")}
                '寻找第一个不是 404 的下载源
                Dim UsefulExs = ExList.Where(Function(e) Not e.Message.Contains("(404)")).ToList
                [Error] = If(UsefulExs.Any, UsefulExs(0), ExList(0))
                '获取实际失败的文件
                For Each File In Files
                    If File.State = NetState.Error Then
                        [Error] = New Exception("文件下载失败：" & File.LocalPath & vbCrLf & Join(
                            File.Sources.Select(Function(s) If(s.Ex Is Nothing, s.Url, s.Ex.Message & "（" & s.Url & "）")), vbCrLf), [Error])
                        Exit For
                    End If
                Next
                '在设置 Error 对象后再更改为失败，避免 WaitForExit 无法捕获错误
                State = LoadState.Failed
            End SyncLock
            '中断所有文件
            For Each TaskFile In Files
                If TaskFile.State < NetState.Merge Then TaskFile.State = NetState.Error
            Next
            '在退出同步锁后再进行日志输出
            Dim ErrOutput As New List(Of String)
            For Each Ex As Exception In ExList
                ErrOutput.Add(GetExceptionDetail(Ex))
            Next
            Log("[Download] " & Join(ErrOutput.Distinct.ToArray, vbCrLf))
        End Sub
        Public Overrides Sub Abort()
            SyncLock LockState
                If State >= LoadState.Finished Then Exit Sub
                State = LoadState.Aborted
            End SyncLock
            Log("[Download] " & Name & " 已取消！")
            '中断所有文件
            For Each TaskFile In Files
                TaskFile.Abort(Me)
            Next
        End Sub

    End Class

    Public NetManager As New NetManagerClass
    ''' <summary>
    ''' 下载文件管理。
    ''' </summary>
    Public Class NetManagerClass

#Region "属性"

        ''' <summary>
        ''' 需要下载的文件。为“本地地址 - 文件对象”键值对。
        ''' </summary>
        Public Files As New Dictionary(Of String, NetFile)
        Public ReadOnly LockFiles As New Object

        ''' <summary>
        ''' 当前的所有下载任务。
        ''' </summary>
        Public Tasks As New SafeList(Of LoaderDownload)

        ''' <summary>
        ''' 已下载完成的大小。
        ''' </summary>
        Public Property DownloadDone As Long
            Get
                Return _DownloadDone
            End Get
            Set(value As Long)
                SyncLock LockDone
                    _DownloadDone = value
                End SyncLock
            End Set
        End Property
        Private _DownloadDone As Long = 0
        Private ReadOnly LockDone As New Object


        ''' <summary>
        ''' 尚未完成下载的文件数。
        ''' </summary>
        Public FileRemain As Integer = 0
        Public ReadOnly LockRemain As New Object

        ''' <summary>
        ''' 上次记速时的已下载大小。
        ''' </summary>
        Private SpeedLastDone As Long = 0
        ''' <summary>
        ''' 至多最近 30 次下载速度的记录，较新的在前面。
        ''' </summary>
        Private SpeedLast As New List(Of Long)
        '这些属性由 RefreshStat 刷新
        ''' <summary>
        ''' 当前的全局下载速度，单位为 Byte / 秒。
        ''' </summary>
        Public Speed As Long = 0

        Public ReadOnly Uuid As Integer = GetUuid()

#End Region

        ''' <summary>
        ''' 进度与下载速度由下载管理线程每隔约 0.1 秒刷新一次。
        ''' </summary>
        Private Sub RefreshStat()
            Try
                Dim DeltaTime As Long = GetTimeTick() - RefreshStatLast
                RefreshStatLast += DeltaTime
#Region "刷新整体速度"
                '计算瞬时速度
                Dim ActualSpeed As Double = Math.Max(0, (DownloadDone - SpeedLastDone) / (DeltaTime / 1000))
                SpeedLast.Insert(0, ActualSpeed)
                If SpeedLast.Count >= 31 Then SpeedLast.RemoveAt(30)
                SpeedLastDone = DownloadDone
                '计算用于显示的速度
                Dim SpeedSum As Long = 0, SpeedDiv As Long = 0, Weight = SpeedLast.Count
                For Each SpeedRecord In SpeedLast
                    SpeedSum += SpeedRecord * Weight
                    SpeedDiv += Weight
                    Weight -= 1
                Next
                Speed = If(SpeedDiv > 0, SpeedSum / SpeedDiv, 0)
                '计算新的速度下限
                Dim Limit As Long = 0
                If SpeedLast.Count >= 10 Then Limit = SpeedLast.Take(10).Average * 0.85 '取近 1 秒的平均速度的 85%
                If Limit > NetTaskSpeedLimitLow Then
                    NetTaskSpeedLimitLow = Limit
                    Log("[Download] " & "速度下限已提升到 " & GetString(Limit))
                End If
#End Region
#Region "刷新下载任务属性"
                For Each Task In Tasks
                    Task.RefreshStat()
                Next
#End Region
            Catch ex As Exception
                Log(ex, "刷新下载公开属性失败")
            End Try
        End Sub
        Private RefreshStatLast As Long

        ''' <summary>
        ''' 启动监控线程，用于新增下载线程。
        ''' </summary>
        Private Sub StartManager()
            If IsManagerStarted Then Exit Sub
            IsManagerStarted = True
            Dim ThreadStarter =
            Sub(Id As Integer) '0 或 1
                Try
                    While True
                        Thread.Sleep(20)
                        '获取文件列表
                        Dim AllFiles As List(Of NetFile)
                        SyncLock LockFiles
                            If Id = 0 AndAlso FileRemain = 0 AndAlso Files.Any() Then Files.Clear() '若已完成，则清空
                            AllFiles = Files.Values.ToList()
                        End SyncLock
                        Dim WaitingFiles As New List(Of NetFile)
                        Dim OngoingFiles As New List(Of NetFile)
                        For Each File As NetFile In AllFiles
                            If File.Uuid Mod 2 = Id Then Continue For
                            If File.State = NetState.WaitForDownload Then
                                WaitingFiles.Add(File)
                            ElseIf File.State < NetState.Merge Then
                                OngoingFiles.Add(File)
                            End If
                        Next
                        '为等待中的文件开始线程
                        For Each File As NetFile In WaitingFiles
                            If NetTaskThreadCount >= NetTaskThreadLimit Then Continue While '最大线程数检查
                            Dim NewThread = File.TryBeginThread()
                            If NewThread IsNot Nothing AndAlso NewThread.Source.Url.Contains("bmclapi") Then Thread.Sleep(70) '减少 BMCLAPI 请求频率，避免 Too Many Requests
                        Next
                        '为进行中的文件追加线程
                        If Speed >= NetTaskSpeedLimitLow Then Continue While '下载速度足够，无需新增
                        For Each File As NetFile In OngoingFiles
                            If NetTaskThreadCount >= NetTaskThreadLimit Then Continue While '最大线程数检查
                            '线程种类计数
                            Dim PreparingCount = 0, DownloadingCount = 0
                            If File.Threads IsNot Nothing Then
                                For Each Thread As NetThread In File.Threads.ToList
                                    If Thread.State < NetState.Download Then
                                        PreparingCount += 1
                                    ElseIf Thread.State = NetState.Download Then
                                        DownloadingCount += 1
                                    End If
                                Next
                            End If
                            '新增线程
                            If PreparingCount > DownloadingCount Then Continue For '准备中的线程已多于下载中的线程，不再新增
                            Dim NewThread = File.TryBeginThread()
                            If NewThread IsNot Nothing AndAlso NewThread.Source.Url.Contains("bmclapi") Then Thread.Sleep(70) '减少 BMCLAPI 请求频率，避免 Too Many Requests
                        Next
                    End While
                Catch ex As Exception
                    Log(ex, $"下载管理启动线程 {Id} 出错", LogLevel.Assert)
                End Try
            End Sub
            RunInNewThread(Sub() ThreadStarter(0), "NetManager ThreadStarter 0")
            RunInNewThread(Sub() ThreadStarter(1), "NetManager ThreadStarter 1")
            RunInNewThread(
            Sub()
                Try
                    Dim LastLoopTime As Long
                    NetTaskSpeedLimitLeftLast = GetTimeTick()
                    While True
                        Dim TimeNow = GetTimeTick()
                        LastLoopTime = TimeNow
                        '增加限速余量
                        If NetTaskSpeedLimitHigh > 0 Then NetTaskSpeedLimitLeft = NetTaskSpeedLimitHigh / 1000 * (TimeNow - NetTaskSpeedLimitLeftLast)
                        NetTaskSpeedLimitLeftLast = TimeNow
                        '刷新公开属性
                        RefreshStat()
                        '等待直至 80 ms
                        Do While GetTimeTick() - LastLoopTime < 80
                            Thread.Sleep(10)
                        Loop
                    End While
                Catch ex As Exception
                    Log(ex, "下载管理刷新线程出错", LogLevel.Assert)
                End Try
            End Sub, "NetManager StatRefresher")
        End Sub
        Private IsManagerStarted As Boolean = False

        'Public FileRemainList As New List(Of String)
        Private IsDownloadCacheCleared As Boolean = False
        ''' <summary>
        ''' 开始一个下载任务。
        ''' </summary>
        Public Sub Start(Task As LoaderDownload)
            StartManager()
            '清理缓存
            If Not IsDownloadCacheCleared Then
                Try
                    DeleteDirectory(PathTemp & "Download")
                Catch ex As Exception
                    Log(ex, "清理下载缓存失败")
                End Try
                IsDownloadCacheCleared = True
            End If
            Directory.CreateDirectory(PathTemp & "Download")
            '文件处理
            SyncLock LockFiles
                '添加每个文件
                For i = 0 To Task.Files.Count - 1
                    Dim File = Task.Files(i)
                    If Files.ContainsKey(File.LocalPath) Then
                        '已有该文件
                        If Files(File.LocalPath).State >= NetState.Finish Then
                            '该文件已经下载过一次，且下载完成
                            '将已下载的文件替换成当前文件，重新下载
                            File.Tasks.Add(Task)
                            Files(File.LocalPath) = File
                            SyncLock LockRemain
                                FileRemain += 1
                                If ModeDebug Then Log("[Download] " & File.LocalName & "：已替换列表，剩余文件 " & FileRemain)
                                'FileRemainList.Add(File.LocalPath)
                            End SyncLock
                        Else
                            '该文件正在下载中
                            '将当前文件替换成下载中的文件，即两个任务指向同一个文件
                            File = Files(File.LocalPath)
                            File.Tasks.Add(Task)
                        End If
                    Else
                        '没有该文件
                        File.Tasks.Add(Task)
                        Files.Add(File.LocalPath, File)
                        SyncLock LockRemain
                            FileRemain += 1
                            If ModeDebug Then Log("[Download] " & File.LocalName & "：已加入列表，剩余文件 " & FileRemain)
                            'FileRemainList.Add(File.LocalPath)
                        End SyncLock
                    End If
                    Task.Files(i) = File '回设
                Next
            End SyncLock
            Tasks.Add(Task)
        End Sub

    End Class

    ''' <summary>
    ''' 是否有正在进行中、需要在下载管理页面显示的下载任务？
    ''' </summary>
    Public Function HasDownloadingTask(Optional IgnoreCustomDownload As Boolean = False) As Boolean
        For Each Task In LoaderTaskbar.ToList()
            If (Task.Show AndAlso Task.State = LoadState.Loading) AndAlso
               (Not IgnoreCustomDownload OrElse Not Task.Name.ToString.Contains("自定义下载")) Then
                Return True
            End If
        Next
        Return False
    End Function

End Module
