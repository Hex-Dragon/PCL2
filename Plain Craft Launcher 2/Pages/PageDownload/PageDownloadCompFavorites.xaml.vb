Public Class PageDownloadCompFavorites

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

    End Sub

End Class
