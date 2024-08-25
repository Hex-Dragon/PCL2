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
    ''' <param name="Word">中文文本</param>
    ''' <returns></returns>
    Public Function GetLangByWord(Word As String) As String
        If Lang = "zh_CN" Then Return Word '语言设置为中文时不需要处理，直接返回以节约处理时间
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
            Case "zh_CN", "zh_HK", "zh_TW", "lzh", "zh_MEME", "ja_JP", "ko_KR" '2024/08/16 11:47
                Return Time.ToString("yyyy'/'MM'/'dd HH':'mm")
            Case "en_GB", "es_ES", "fr_FR", "ru_RU" '11:47 16/08/2024
                Return Time.ToString("HH':'mm dd'/'MM'/'yyyy")
            Case Else 'en_US 11:47 08/16/2024
                Return Time.ToString("HH':'mm MM'/'dd'/'yyyy")
        End Select
    End Function

    ''' <summary>
    ''' 切换 PCL 界面的字体
    ''' </summary>
    ''' <param name="Font">字体</param>
    Public Sub SwitchApplicationFont(Font As FontFamily)
        Try
            Application.Current.Resources("LaunchFontFamily") = Font
        Catch ex As Exception
            Log(ex, "[Lang] 切换字体失败，这可能导致界面显示异常", LogLevel.Msgbox)
        End Try
    End Sub

    ''' <summary>
    ''' 地区是否为中国大陆
    ''' 君子协议
    ''' </summary>
    ''' <returns></returns>
    Public Function IsLocationZH() As Boolean
        Dim IsZH As Boolean = Globalization.CultureInfo.CurrentCulture.Name.Equals("zh-CN") '语言检测
        IsZH = IsZH And Globalization.CultureInfo.CurrentUICulture.Name.Equals("zh-CN") '语言检测
        IsZH = IsZH And TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Equals(New TimeSpan(8, 0, 0)) '时区检测
        Return IsZH
    End Function

    ''' <summary>
    ''' 获取当前系统的默认语言
    ''' </summary>
    ''' <returns>返回类似于 zh_CN 这样形式的文本</returns>
    Public Function GetDefaultLang() As String
        Select Case Globalization.CultureInfo.CurrentCulture.Name
            Case "en-GB", "en-NZ", "en-AU"
                Return "en_GB"
            Case "es-ES", "es-MX", "es-UY", "es-VE", "es-AR", "es_EC", "	es_CL"
                Return "es_ES"
            Case "fr-FR", "fr-CA"
                Return "fr_FR"
            Case "ja-JP"
                Return "ja_JP"
            Case "ko-KR", "ko-KP"
                Return "ko_KR"
            Case "ru-RU"
                Return "ru_RU"
            Case "zh-CN", "zh-SG", "zh-Hans"
                Return "zh_CN"
            Case "zh-HK", "zh-MO"
                Return "zh_HK"
            Case "zh-TW", "zh-Hant"
                Return "zh_TW"
            Case Else
                Return "en_US"
        End Select
    End Function

    ''' <summary>
    ''' 格式化本地化的数字描述
    ''' </summary>
    ''' <param name="Num">数量</param>
    ''' <returns>11 Million、2 万等这样的表示</returns>
    Public Function GetLocationNum(Num As Int32) As String
        Select Case Lang
            Case "zh_CN", "zh_HK", "zh_TW", "lzh", "zh_MEME", "ja_JP", "ko_KR"
                Return If(Num > 1000000000000, Math.Round(Num / 1000000000000, 2) & " " & GetLang("LangModCompModDigit3"), '兆
                If(Num > 100000000, Math.Round(Num / 100000000, 2) & " " & GetLang("LangModCompModDigit2"), '亿
                If(Num > 100000, Math.Round(Num / 10000, 0) & " " & GetLang("LangModCompModDigit1"), Num.ToString("N0")))) '万
            Case Else 'en_US, en_GB, fr_FR etc.
                Return If(Num > 1000000000, Math.Round(Num / 1000000000, 2) & GetLang("LangModCompModDigit3"), 'Billion
                If(Num > 1000000, Math.Round(Num / 1000000, 2) & GetLang("LangModCompModDigit2"), 'Million
                If(Num > 10000, Math.Round(Num / 1000, 0) & GetLang("LangModCompModDigit1"), Num))) 'Thousand(K)
        End Select
    End Function

    ''' <summary>
    ''' 根据当前数量判断使用单数或复数形式，提供的键名需有对应加P的复数形式于语言文件
    ''' </summary>
    ''' <param name="Count">数量</param>
    ''' <param name="Key">调用的键名</param>
    ''' <returns></returns>
    Public Function IsPlural(Count As Int32, Key As String) As String
        If Count <= 1 Then
            Return GetLang(Key)
        Else
            Return GetLang(Key & "P")
        End If
    End Function

End Module
