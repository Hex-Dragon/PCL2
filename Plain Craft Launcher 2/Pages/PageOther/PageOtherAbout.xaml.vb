Public Class PageOtherAbout

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

        ItemAboutPcl.Info = ItemAboutPcl.Info.Replace("%VERSION%", VersionBaseName).Replace("%VERSIONCODE%", VersionCode).Replace("%BRANCH%", VersionBranchName).Replace("%COMMIT_HASH%", CommitHashShort).Replace("%UPSTREAM_VERSION%", UpstreamVersion)

    End Sub

    Private Sub BtnAboutBmclapi_Click(sender As Object, e As EventArgs) Handles BtnAboutBmclapi.Click
        OpenWebsite("https://afdian.com/a/bangbang93")
    End Sub
    Private Sub BtnAboutWiki_Click(sender As Object, e As EventArgs) Handles BtnAboutWiki.Click
        OpenWebsite("https://www.mcmod.cn")
    End Sub

    Private Sub ImgPCLCommunity_Click(sender As Object, e As MouseButtonEventArgs) Handles ImgPCLCommunity.MouseLeftButtonDown
        AniStart({
                 AaRotateTransform(sender, 360)})
    End Sub

    '彩蛋
    Private ClickCount As Integer = 0
    Private Sub ImgPCLLogo_Click(sender As Object, e As MouseButtonEventArgs) Handles ImgPCLLogo.MouseLeftButtonDown
        If ClickCount < 200 Then
            ClickCount += 1
            Select Case ClickCount
                Case 5
                    Hint("点这个很好玩么……")
                Case 15
                    Hint("还点？")
                Case 25
                    Select Case MyMsgBox("你现在是不是超无聊的？", "咕咕咕？", Button1:="是的", Button2:="并不是")
                        Case 2
                            Hint("那你还点啥……真是搞不懂。")
                    End Select
                Case 50
                    Hint("嗯，加油吧，嗯……")
                Case 75
                    Hint("隐藏主题 混乱黄 已……嗯不对，这是 PCL2 社区版，应该没有这玩意……")
                Case 100
                    Hint("你咋还这么无聊啊？")
                Case 130
                    Hint("后面什么都没有了哦！")
                Case 150
                    Select Case MyMsgBox("你真的不累么？", "温馨提示", "累死了", "真的不累")
                        Case 1
                            Hint("那你就别点了喂……后面真的真的真的什么都没有了！")
                        Case 2
                            Select Case MyMsgBox("你真的真的不累么？", "超温馨的温馨提示", "累死了", "真的真的不累")
                                Case 1
                                    Hint("那你就别点了喂……后面真的真的真的什么都没有了！")
                                Case 2
                                    Select Case MyMsgBox("你真的真的真的不累么？", "超超超温馨的温馨提示", "累死了", "真的真的真的不累")
                                        Case 1
                                            Hint("那你就别点了喂……后面真的真的真的什么都没有了！")
                                        Case 2
                                            Hint("好吧……不过后面是真的啥也没了，不用点了真的。")
                                    End Select
                            End Select
                    End Select
                Case 200
                    Hint("还点，还点就不让你点了……")
                    ImgPCLLogo.IsHitTestVisible = False
                    Exit Sub
            End Select
            Dim rand = New Random()
            Dim mx = rand.Next(-1, 1)
            If mx = 0 Then mx = 1
            Dim my = rand.Next(-1, 1)
            If my = 0 Then my = 1
            AniStart({
                 AaTranslateX(sender, mx, Time:=0),
                 AaTranslateY(sender, my, Time:=0),
                 AaTranslateX(sender, -mx, Time:=0, Delay:=100),
                 AaTranslateY(sender, -my, Time:=0, Delay:=100)
                 })
        End If
    End Sub

End Class
