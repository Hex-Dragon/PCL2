Public Class PageOtherVote

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherFeedback_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True


    End Sub
    Private Sub Vote_Click(sender As Object, e As MouseButtonEventArgs)
        PageOtherLeft.TryVote()
    End Sub
End Class
