Imports System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock

Module Modi18n
    ''' <summary>
    ''' 获取语言
    ''' </summary>
    ''' <param name="Key">键值</param>
    ''' <param name="Param">字段中对应要展示的内容</param>
    ''' <returns></returns>
    Public Function GetLang(Key As String, ParamArray Param As String()) As String
        Try
            If String.IsNullOrWhiteSpace(Key) Then Throw New Exception("Key 值未提供;No key value provided")
            Return String.Format(Application.Current.FindResource(Key), Param)
        Catch ex As FormatException
            Log(ex, $"[Lang] 格式化文本失败：{Key};传入参数：{Param.Join(",")}", LogLevel.Hint)
            Return Application.Current.FindResource(Key)
        Catch ex As ResourceReferenceKeyNotFoundException
            Log(ex, $"[Lang] 找不到对应的语言资源：{Key}")
            Return Key
        Catch ex As Exception
            Log(ex, $"[Lang] 获取语言资源失败：{Key}（{ex.Message}）", LogLevel.Hint)
            Return Key
        End Try
    End Function

    ''' <summary>
    ''' 通过中文得到其它语言，应付一些 i18n 适配较困难的场景
    ''' </summary>
    ''' <param name="Word"></param>
    ''' <returns></returns>
    Public Function GetLangByWord(Word As String) As String
        If Lang = "zh_CN" Then Return Word
        Select Case Word
            Case "正式版"
                Return GetLang("LangDownloadRelease")
            Case "预览版", "快照版本"
                Return GetLang("LangDownloadBeta")
            Case "远古版", "远古版本"
                Return GetLang("LangDownloadAncientVersion")
            Case "愚人节版"
                Return GetLang("LangDownloadAprilFool")
            Case "未知版本"
                Return GetLang("LangDownloadUnknown")
            Case Else
                Log("[Lang] 没有找到词语""" & Word & """的对应翻译")
                Return Word
        End Select
    End Function

    ''' <summary>
    ''' 获取当前语言的时间表达方式
    ''' </summary>
    ''' <param name="Time">时间</param>
    ''' <returns></returns>
    Public Function GetLocalTimeFormat(Time As DateTime) As String
        Select Case Lang
            Case "zh_CN", "zh_HK", "zh_TW", "lzh"
                Return Time.ToString("yyyy'/'MM'/'dd HH':'mm")
            Case Else 'en_US
                Return Time.ToString("HH:mm MM/dd/yyyy")
        End Select
    End Function

    ''' <summary>
    ''' 切换应用程序的字体
    ''' </summary>
    Public Sub SwitchApplicationFont(Font As FontFamily)
        Try
            Application.Current.Resources("LaunchFontFamily") = Font
        Catch ex As Exception
            Log(ex, "[Lang] 切换字体失败，这可能导致界面显示异常", LogLevel.Msgbox)
        End Try
    End Sub
End Module
