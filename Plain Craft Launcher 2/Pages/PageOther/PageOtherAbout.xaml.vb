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

End Class
