Public Class PageLoginOffline
    Private Sub BtnBack_Click(sender As Object, e As EventArgs) Handles BtnBack.Click
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
    End Sub
    Private Sub RadioCustomUuid_Checked() Handles RadioUuidCustom.Check, RadioUuidStandard.Check, RadioUuidLegacy.Check
        If RadioUuidCustom.Checked Then
            TextUuidTitle.Visibility = Visibility.Visible
            TextUuid.Visibility = Visibility.Visible
        Else
            TextUuidTitle.Visibility = Visibility.Collapsed
            TextUuid.Visibility = Visibility.Collapsed
        End If
    End Sub
    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        '玩家 ID 输入检查
        Dim Username As String = TextName.Text
        Dim UsernameValidateResult = New ValidateRegex("^[A-z0-9_]{3,16}$").Validate(Username)
        If UsernameValidateResult <> "" Then
            If MyMsgBox($"你输入的玩家 ID 不符合标准（3 - 16 位，只可以包含英文字母、数字与下划线），可能导致部分版本的游戏无法启动或发生错误。{vbCrLf}强烈建议使用规范的玩家 ID！{vbCrLf}如果你坚持，仍然可以继续创建档案。",
                     "玩家 ID 不符合规范", "继续", "取消", IsWarn:=True, ForceWait:=True) = 2 Then Exit Sub
        End If
        'UUID
        Dim UserUuid As String = Nothing
        If RadioUuidCustom.Checked Then
            '自定义输入检查
            Dim UuidInput As String = TextUuid.Text.Replace("-", "")
            Dim UuidValidateResult = New ValidateRegex("^[a-fA-F0-9]{32}$").Validate(UuidInput)
            If RadioUuidCustom.Checked AndAlso UuidValidateResult <> "" Then
                Hint("UUID 不符合要求：" & UuidValidateResult, HintType.Critical)
                Exit Sub
            End If
            UserUuid = UuidInput
        ElseIf RadioUuidLegacy.Checked Then
            UserUuid = GetOfflineUuid(Username, IsLegacy:=True)
        Else
            UserUuid = GetOfflineUuid(Username)
        End If
        '创建档案
        Dim NewProfile = New McProfile With {
            .Type = McLoginType.Legacy,
            .Uuid = UserUuid,
            .Username = Username,
            .Desc = ""}
        ProfileList.Add(NewProfile)
        SaveProfile()
        Hint("档案新建成功！", HintType.Finish)
        RunInUi(Sub() FrmLaunchLeft.RefreshPage(True))
    End Sub
End Class
