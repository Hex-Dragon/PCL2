Public Module ModLog

#Region "枚举"
    Private Enum GameLogLevel
        Debug
        Info
        Warn
        [Error]
        Fatal
    End Enum
#End Region

#Region "日志处理方法"

    ''' <summary>
    ''' 将流中的文本逐行迭代。可能阻塞。
    ''' </summary>
    Private Iterator Function EnumLines(stream As StreamReader) As IEnumerable(Of String)
        Using stream
            Do
                Dim s = stream.ReadLine()
                If s IsNot Nothing Then Yield s
            Loop
        End Using
    End Function
    Private Function GetLevel(line As String, lastLevel As GameLogLevel) As GameLogLevel
        Dim GetColorBrush As Func(Of String, SolidColorBrush) = Function(name) CType(Application.Current.Resources(name), SolidColorBrush)
        Dim Starting As String = line.Split(": ")(0)
        If Starting.ContainsF("FATAL") Then Return GameLogLevel.Fatal
        If Starting.ContainsF("ERROR") Then Return GameLogLevel.Error
        If Starting.ContainsF("WARN") Then Return GameLogLevel.Warn
        If Starting.ContainsF("INFO") Then Return GameLogLevel.Info
        If Starting.ContainsF("DEBUG") Then Return GameLogLevel.Debug
        If line.StartsWithF("Exception in thread """) Then Return GameLogLevel.Error
        Return lastLevel
    End Function
    Private Function GetColor(level As GameLogLevel) As SolidColorBrush
        Dim GetColorBrush As Func(Of String, SolidColorBrush) = Function(name) CType(Application.Current.Resources(name), SolidColorBrush)
        Select Case level
            Case GameLogLevel.Debug
                Return GetColorBrush("ColorBrushDebug")
            Case GameLogLevel.Info
                Return New SolidColorBrush(If(IsDarkMode, New MyColor(255, 255, 255), New MyColor(0, 0, 0)))
            Case GameLogLevel.Warn
                Return GetColorBrush("ColorBrushWarn")
            Case GameLogLevel.Error
                Return GetColorBrush("ColorBrushError")
            Case GameLogLevel.Fatal
                Return GetColorBrush("ColorBrushFatal")
        End Select
        Return Nothing
    End Function
#End Region

#Region "事件"
    Public Class LogOutputEventArgs
        Inherits EventArgs
        Public LogText As String
        Public Color As SolidColorBrush
        Public Sub New(LogText As String, Color As SolidColorBrush)
            Me.LogText = LogText
            Me.Color = Color
        End Sub
    End Class
#End Region

#Region "实时日志类"
    Public Class McGameLog
        ''' <summary>
        ''' 游戏版本。
        ''' </summary>
        Public Version As McVersion
        ''' <summary>
        ''' 游戏进程。
        ''' </summary>
        Public Process As Process
        ''' <summary>
        ''' 运行线程。
        ''' </summary>
        Private RunThread As Thread
        ''' <summary>
        ''' 是否已停止。
        ''' </summary>
        Public Stopped As Boolean = False
        '''' <summary>
        '''' HTML 格式的正文。
        '''' </summary>
        'Private HtmlBody As String = ""
        '''' <summary>
        '''' HTML 替换文本。替换 [BODY]。
        '''' </summary>
        'Private Const HtmlPattern As String = "<!DOCTYPE html><html lang=""en""><head><title>PCL Real-time Log</title><meta charset=""utf-8""><meta name=""viewport""content=""width=device-width, initial-scale=1.0""><style>body{font-family:""Consolas"",""Courier New"",monospace;margin:0;padding:0;display:flex}.line-numbers{background-color:#f4f4f4;padding:10px;border-right:1px solid#ccc;text-align:right;-webkit-user-select:none;user-select:none}.line-numbers span{display:block;line-height:1.2}.content{padding:10px;line-height:1.2}</style></head><body><div class=""line-numbers""id=""lineNumbers""></div><div class=""content"">[BODY]</div><script>document.addEventListener(""DOMContentLoaded"",()=>{function addLineNumbers(){const content=document.querySelector("".content"");const lines=content.querySelectorAll(""div"");const lineNumbers=document.getElementById(""lineNumbers"");lines.forEach((line,index)=>{const lineNumber=document.createElement(""span"");lineNumber.textContent=index+1;lineNumbers.appendChild(lineNumber)})}addLineNumbers()});</script></body></html>"
        Public CountFatal As UInteger = 0
        Public CountError As UInteger = 0
        Public CountWarn As UInteger = 0
        Public CountInfo As UInteger = 0
        Public CountDebug As UInteger = 0
        Public Sub New(Version As McVersion, Process As Process)
            Me.Version = Version
            Me.Process = Process
            RunThread = New Thread(Sub()
                                       Dim level As GameLogLevel = GameLogLevel.Info
                                       For Each line In EnumLines(Me.Process.StandardOutput)
                                           Print(line, level)
                                           If Me.Process.HasExited Then
                                               Print($"游戏已退出，返回值：{Me.Process.ExitCode}", GameLogLevel.Info)
                                               Return
                                           End If
                                           If Stopped Then Return
                                           Thread.Sleep(1)
                                       Next
                                   End Sub)
            RunThread.Start()
        End Sub
        ''' <summary>
        ''' 输出一行日志，并统计日志行数。
        ''' </summary>
        Private Sub Print(line As String, ByRef level As GameLogLevel)
            level = If(line.StartsWithF("	at ") _
                OrElse line.StartsWithF("Caused by: ") _
                OrElse line.StartsWithF("	... "), '例如 “	... 4 more”
                level, GetLevel(line, level))
            Dim color = GetColor(level)
            'HtmlBody &= $"<div style=""color:{color}"">{line}</div>"
            'RunInUi(Sub() Paragraph.Inlines.Add(New Run(line) With {.Foreground = color}))
            Select Case level
                Case GameLogLevel.Debug
                    CountDebug += 1
                Case GameLogLevel.Info
                    CountInfo += 1
                Case GameLogLevel.Warn
                    CountWarn += 1
                Case GameLogLevel.Error
                    CountError += 1
                Case GameLogLevel.Fatal
                    CountFatal += 1
            End Select
            RaiseEvent LogOutput(Me, New LogOutputEventArgs(line, color))
        End Sub
        'Public ReadOnly Property HtmlFormat As String
        '    Get
        '        Return HtmlPattern.Replace("[BODY]", HtmlBody)
        '    End Get
        'End Property
        ''' <summary>
        ''' 有新的日志输出，日志计数器发生改变时触发。
        ''' </summary>
        Public Event LogOutput(sender As McGameLog, e As LogOutputEventArgs)
    End Class
#End Region

End Module
