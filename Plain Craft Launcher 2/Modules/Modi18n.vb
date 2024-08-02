Module Modi18n
    ''' <summary>
    ''' 获取语言
    ''' </summary>
    ''' <param name="Key">键值</param>
    ''' <param name="Param">字段中对应要展示的内容</param>
    ''' <returns></returns>
    Public Function GetLang(Key As String, ParamArray Param As String()) As String
        Try
            Return String.Format(Application.Current.FindResource(Key), Param)
        Catch ex As Exception
            Log(ex, "[Lang] 获取语言资源失败：" & Key, LogLevel.Hint)
            Return Key
        End Try
    End Function

    ''' <summary>
    ''' 通过中文得到其它语言，应付一些 i18n 适配较苦难的场景
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
                Log("[Lang] GetLangByWord:没有找到 " & Word & " 的对应翻译")
                Return Word
        End Select
    End Function

    ''' <summary>
    ''' 获取当前语言的时间表达方式
    ''' </summary>
    ''' <param name="Time"></param>
    ''' <returns></returns>
    Public Function GetLocalTimeFormat(Time As DateTime) As String
        Select Case Lang
            Case "zh_CN", "zh_HK", "zh_TW"
                Return Time.ToString("yyyy'/'MM'/'dd HH':'mm")
            Case Else 'en_US
                Return Time.ToString("HH:mm MM/dd/yyyy")
        End Select
    End Function

End Module
