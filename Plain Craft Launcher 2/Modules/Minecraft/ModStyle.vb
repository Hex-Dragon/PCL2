Module ModStyle
    ''' <summary>
    ''' Minecraft 文本格式化代码，用于显示不同颜色的文本
    ''' </summary>
    ''' <param name="text">要格式化的文本</param>
    ''' <param name="lab">控件</param>
    Public Sub SetColorfulTextLab(text As String, lab As TextBlock)
        If lab Is Nothing Then
            Log("[Style] SetColorfulTextLab: lab is null")
            Exit Sub
        End If
        lab.Inlines.Clear()
        '随机太难实现，先咕咕咕了
        Dim HasItalicProperty As Boolean = False '斜体
        Dim HasDeleteLineProperty As Boolean = False '删除线
        Dim HasStrickThroughProperty As Boolean = False '下划线
        Dim HasBlodProperty As Boolean = False '粗体

        Dim color As String = "#FFFFFF"
        Dim isColorCode As Boolean = False
        Dim curRun As Run = New Run()
        lab.Inlines.Add(curRun)
        For Each c As Char In text
            If c = "§" Then '下一字符是格式化代码
                isColorCode = True
                Continue For
            End If
            If isColorCode Then
                Select Case c
                    '颜色代码
                    Case "0"
                        color = "#000000"
                    Case "1"
                        color = "#0000AA"
                    Case "2"
                        color = "#00AA00"
                    Case "3"
                        color = "#00AAAA"
                    Case "4"
                        color = "#AA0000"
                    Case "5"
                        color = "#AA00AA"
                    Case "6"
                        color = "#FFAA00"
                    Case "7"
                        color = "#AAAAAA"
                    Case "8"
                        color = "#555555"
                    Case "9"
                        color = "#5555FF"
                    Case "a", "A"
                        color = "#55FF55"
                    Case "b", "B"
                        color = "#55FFFF"
                    Case "c", "C"
                        color = "#FF5555"
                    Case "d", "D"
                        color = "#FF55FF"
                    Case "e", "E"
                        color = "#FFFF55"
                    Case "f", "F"
                        color = "#FFFFFF"
                    '格式化代码
                    Case "l" '粗体
                        HasBlodProperty = True
                    Case "o" '斜体
                        HasItalicProperty = True
                    Case "n" '下划线
                        HasStrickThroughProperty = True
                    Case "m" '删除线
                        HasDeleteLineProperty = True
                    Case "r" '重置
                        color = "#FFFFFF"
                        HasBlodProperty = False
                        HasItalicProperty = False
                        HasStrickThroughProperty = False
                        HasDeleteLineProperty = False
                End Select
                If Not String.IsNullOrEmpty(curRun.Text) Then '遇到格式代码但是有文本，重开一个Run
                    curRun = New Run()
                    lab.Inlines.Add(curRun)
                End If
                curRun.Foreground = New SolidColorBrush(New MyColor(color))
                curRun.FontWeight = If(HasBlodProperty, FontWeights.Bold, FontWeights.Normal)
                curRun.FontStyle = If(HasItalicProperty, FontStyles.Italic, FontStyles.Normal)
                curRun.TextDecorations = If(HasStrickThroughProperty, TextDecorations.Strikethrough, Nothing)
                curRun.TextDecorations = If(HasDeleteLineProperty, TextDecorations.Underline, Nothing)
            End If
            If Not isColorCode Then curRun.Text += c
            If isColorCode Then isColorCode = False
        Next
    End Sub
End Module
