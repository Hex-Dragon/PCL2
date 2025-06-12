Imports PCL.Core.Model

Public Class PageSetupJava

    Private IsLoad As Boolean = False

    Private JavaPageLoader As New LoaderTask(Of Integer, List(Of Java))("JavaPageLoader", AddressOf Load_GetJavaList)
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PageLoaderInit(PanLoad, CardLoad, PanMain, Nothing, JavaPageLoader, AddressOf OnLoadFinished, AddressOf Load_Input)
    End Sub

    Private Function Load_Input()
        Return Javas.JavaList.Count
    End Function
    Private Sub Load_GetJavaList(loader As LoaderTask(Of Integer, List(Of Java)))
        Javas.ScanJava().GetAwaiter().GetResult()
        loader.Output = Javas.JavaList
    End Sub

    Private Sub OnLoadFinished()
        Dim ItemBuilder = Function(J As Java) As MyListItem
                              Dim Item As New MyListItem
                              Dim VersionTypeDesc = If(J.IsJre, "JRE", "JDK")
                              Dim VersionNameDesc = J.JavaMajorVersion.ToString()
                              Item.Title = $"{VersionTypeDesc} {VersionNameDesc}"

                              Item.Info = J.JavaFolder
                              Dim displayTags As New List(Of String)
                              Dim DisplayBits = If(J.Is64Bit, "64 Bit", "32 Bit")
                              displayTags.Add(DisplayBits)
                              Dim DisplayBrand = J.Brand.ToString()
                              displayTags.Add(DisplayBrand)
                              Item.Tags = displayTags

                              Item.Type = MyListItem.CheckType.RadioBox
                              AddHandler Item.Check, Sub(sender As Object, e As RouteEventArgs)
                                                         If J.IsEnabled Then
                                                             Setup.Set("LaunchArgumentJavaSelect", J.JavaExePath)
                                                         Else
                                                             Hint("未启动此 Java")
                                                             e.Handled = True
                                                         End If
                                                     End Sub
                              Dim BtnOpenFolder As New MyIconButton
                              BtnOpenFolder.Logo = Logo.IconButtonOpen
                              BtnOpenFolder.ToolTip = "打开"
                              AddHandler BtnOpenFolder.Click, Sub()
                                                                  OpenExplorer(J.JavaFolder)
                                                              End Sub
                              Dim BtnInfo As New MyIconButton
                              BtnInfo.Logo = Logo.IconButtonInfo
                              BtnInfo.ToolTip = "详细信息"
                              AddHandler BtnInfo.Click, Sub()
                                                            MyMsgBox($"类型: {VersionTypeDesc}" & vbCrLf &
                                                                     $"版本: {J.Version.ToString()}" & vbCrLf &
                                                                     $"架构: {J.JavaArch.ToString()} ({DisplayBits})" & vbCrLf &
                                                                     $"品排: {DisplayBrand}" & vbCrLf &
                                                                     $"位置: {J.JavaFolder}", "Java 信息")
                                                        End Sub
                              Dim BtnEnableSwitch As New MyIconButton


                              Item.Buttons = {BtnOpenFolder, BtnInfo, BtnEnableSwitch}

                              Dim UpdateEnableStyle = Sub(IsCurEnable As Boolean)
                                                          If IsCurEnable Then
                                                              Item.LabTitle.TextDecorations = Nothing
                                                              Item.LabTitle.Foreground = DynamicColors.Color1Brush
                                                              BtnEnableSwitch.Logo = Logo.IconButtonDisable
                                                              BtnEnableSwitch.ToolTip = "禁用此 Java"
                                                          Else
                                                              Item.LabTitle.TextDecorations = TextDecorations.Strikethrough
                                                              Item.LabTitle.Foreground = ColorGray4
                                                              BtnEnableSwitch.Logo = Logo.IconButtonEnable
                                                              BtnEnableSwitch.ToolTip = "启用此 Java"
                                                          End If
                                                      End Sub
                              AddHandler BtnEnableSwitch.Click, Sub()
                                                                    Try
                                                                        Dim target = Javas.JavaList.Where(Function(x) x.JavaExePath = J.JavaExePath).First()
                                                                        target.IsEnabled = Not target.IsEnabled
                                                                        UpdateEnableStyle(target.IsEnabled)
                                                                        JavaSetCache(Javas.GetCache())
                                                                    Catch ex As Exception
                                                                        Log(ex, "调整 Java 启用状态失败", LogLevel.Hint)
                                                                    End Try
                                                                End Sub
                              UpdateEnableStyle(J.IsEnabled)

                              Return Item
                          End Function
        PanContent.Children.Clear()
        Dim ItemAuto As New MyListItem With {
            .Type = MyListItem.CheckType.RadioBox,
            .Title = "自动选择",
            .Info = "Java 选择自动挡，依据游戏需要自动选择合适的 Java"
        }
        AddHandler ItemAuto.Check, Sub()
                                       Setup.Set("LaunchArgumentJavaSelect", "")
                                   End Sub
        PanContent.Children.Add(ItemAuto)
        Dim CurrentSetJava = Setup.Get("LaunchArgumentJavaSelect")
        For Each J In Javas.JavaList
            Dim item = ItemBuilder(J)
            PanContent.Children.Add(item)
            If J.JavaExePath = CurrentSetJava Then item.SetChecked(True, False, False)
        Next
        If String.IsNullOrEmpty(CurrentSetJava) Then ItemAuto.SetChecked(True, False, False)
    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As RouteEventArgs) Handles BtnRefresh.Click
        JavaPageLoader.Start(IsForceRestart:=True)
    End Sub

    Private Sub BtnAdd_Click(sender As Object, e As RouteEventArgs) Handles BtnAdd.Click
        Dim ret = SelectFile("Java 程序(*.exe)|*.exe", "选择 Java 程序")
        If String.IsNullOrEmpty(ret) OrElse Not File.Exists(ret) Then Return
        If JavaAddNew(ret) Then
            Hint("已添加 Java！", HintType.Finish)
        Else
            Hint("Java 可能已经存在，无法添加……")
        End If
        JavaPageLoader.Start()
    End Sub

End Class
