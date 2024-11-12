﻿Public Class PageDownloadOptiFine

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanMain, CardTip, DlOptiFineListLoader, AddressOf Load_OnFinish)
    End Sub
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

    Private Sub Load_OnFinish()
        '结果数据化
        Try
            '归类
            Dim Dict As New Dictionary(Of String, List(Of DlOptiFineListEntry))
            Dict.Add("快照版本", New List(Of DlOptiFineListEntry))
            For VersionCode As Integer = 50 To 0 Step -1
                Dict.Add("1." & VersionCode, New List(Of DlOptiFineListEntry))
            Next
            For Each Version As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
                If Version.Inherit.StartsWith("1.") Then
                    Dim MainVersion As String = "1." & Version.NameDisplay.Split(".")(1).Split(" ")(0)
                    If Dict.ContainsKey(MainVersion) Then
                        Dict(MainVersion).Add(Version)
                    Else
                        Dict("快照版本").Add(Version)
                    End If
                Else
                    Dict("快照版本").Add(Version)
                End If
            Next
            '清空当前
            PanMain.Children.Clear()
            '转化为 UI
            For Each Pair As KeyValuePair(Of String, List(Of DlOptiFineListEntry)) In Dict
                If Not Pair.Value.Any() Then Continue For
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Pair.Key & " (" & Pair.Value.Count & ")", .Margin = New Thickness(0, 0, 0, 15), .SwapType = 3}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.IsSwaped = True
                PanMain.Children.Add(NewCard)
            Next
        Catch ex As Exception
            Log(ex, "可视化 OptiFine 版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub BtnWeb_Click(sender As Object, e As EventArgs) Handles BtnWeb.Click
        OpenWebsite("https://www.optifine.net/")
    End Sub

End Class
