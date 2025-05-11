Public Class PageLoginOffline
    Private Sub BtnBack_Click(sender As Object, e As EventArgs) Handles BtnBack.Click
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
    End Sub
    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        If TextUserName.ValidateResult <> "" Then
            MyMsgBox(TextUserName.ValidateResult, "用户名输入错误", "确认")
            Return
        End If
        If CreateCustomUuid.Checked AndAlso TextUserUuid.ValidateResult <> "" Then
            MyMsgBox(TextUserUuid.ValidateResult, "UUID输入错误", "确认")
            Return
        End If
        Dim UserUuid As String = Nothing
        If CreateStandardUuid.Checked Then
            UserUuid = GetOfflineUuid(TextUserName.Text)
        ElseIf CreateOfficialUuid.Checked Then
            UserUuid = GetOfflineUuid(TextUserName.Text, IsLegacy := True)
        Else
            UserUuid = TextUserUuid.Text
        End If
        Dim NewProfile = New McProfile With {
            .Type = McLoginType.Legacy,
            .Uuid = UserUuid,
            .Username = TextUserName.Text,
            .Desc = ""}
        ProfileList.Add(NewProfile)
        SaveProfile()
        Hint("档案新建成功！", HintType.Finish)
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
    End Sub
End Class
