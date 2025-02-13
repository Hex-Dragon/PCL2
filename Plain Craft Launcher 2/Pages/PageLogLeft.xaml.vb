Public Class PageLogLeft
    Public ShownLogs As New List(Of KeyValuePair(Of Integer, McGameLog))
    Public CurrentUuid As Integer
    Public CurrentLog As McGameLog
    Private Sub PageLogLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        LogListUI()
    End Sub
    Private Sub LogListUI()
        Try
            FrmLogLeft.PanList.Children.Clear()
            For Each item In ShownLogs
                Dim Uuid As Integer = item.Key
                Dim Version As McVersion = item.Value.Version
                Dim Proc As Process = item.Value.Process
                ''TODO：修改 Logo
                'Dim ContMenu As ContextMenu = GetObjectFromXML(
                '                <ContextMenu xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:PCL;assembly=Plain Craft Launcher 2">
                '                    <local:MyMenuItem x:Name="Remove" Header="移除" Padding="0,0,0,2" Icon="F1 M 26.9166,22.1667L 37.9999,33.25L 49.0832,22.1668L 53.8332,26.9168L 42.7499,38L 53.8332,49.0834L 49.0833,53.8334L 37.9999,42.75L 26.9166,53.8334L 22.1666,49.0833L 33.25,38L 22.1667,26.9167L 26.9166,22.1667 Z "/>
                '                </ContextMenu>
                ')
                ''注册事件
                'CType(ContMenu.FindName("Remove"), MyMenuItem).AddHandler(
                '    MyMenuItem.ClickEvent, New RoutedEventHandler(AddressOf FrmLogLeft.Remove_Click))
                Dim NewItem As New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.RadioBox, .MinPaddingRight = 30, .Title = Version.Name, .Info = Version.Version.ToString, .Height = 40, .Tag = Uuid}
                AddHandler NewItem.Changed, AddressOf FrmLogLeft.Version_Change
                'Dim KillButton As New MyIconButton With {.Logo = Logo.IconButtonCross, .LogoScale = 0.85}
                Dim RemoveButton As New MyIconButton With {.Logo = Logo.IconButtonDelete, .LogoScale = 1.1}
                'AddHandler KillButton.Click, AddressOf FrmLogLeft.Kill_Click
                AddHandler RemoveButton.Click, AddressOf FrmLogLeft.Remove_Click
                NewItem.Buttons = {RemoveButton}
                FrmLogLeft.PanList.Children.Add(NewItem)
            Next
        Catch ex As Exception
            Log(ex, "构建游戏实时日志 UI 出错", LogLevel.Feedback)
        End Try
    End Sub
    Public Sub AddProcess(version As McVersion, proc As Process)
        Dim uuid As Integer = GetUuid()
        ShownLogs.Add(New KeyValuePair(Of Integer, McGameLog)(uuid, New McGameLog(version, proc)))
        SelectChange(uuid)
        RunInUi(AddressOf LogListUI)
        FrmMain.BtnExtraLog.ShowRefresh()
    End Sub
    Public Sub SelectChange(Uuid As Integer)
        If Uuid = -1 Then
            CurrentUuid = -1
        End If
        For Each item In ShownLogs
            If item.Key = Uuid Then
                CurrentUuid = Uuid
                CurrentLog = item.Value
            End If
        Next
    End Sub
    Public Sub RemoveItem(Uuid As Integer)
        For i = 0 To ShownLogs.Count - 1
            Dim item = ShownLogs(i)
            If item.Key = Uuid Then
                ShownLogs.RemoveAt(i)
                If CurrentUuid = item.Key Then
                    If ShownLogs.Count = 0 Then
                        '没有可以显示的了
                        CurrentUuid = -1
                        CurrentLog = Nothing
                    Else
                        SelectChange(ShownLogs({{i, ShownLogs.Count - 1}.Min, 0}.Max).Key)
                    End If
                End If
                LogListUI()
                Return
            End If
        Next
    End Sub
    'Public Sub Kill_Click(sender As Object, e As RoutedEventArgs)
    '    Dim Uuid As Integer = (CType(CType(sender, MyIconButton).Parent, MyListItem).Tag)
    '    For Each item In ShownLogs
    '        If item.Key = Uuid Then
    '            item.Value.proc.Kill()
    '        End If
    '    Next
    'End Sub
    Public Sub Remove_Click(sender As Object, e As RoutedEventArgs)
        RemoveItem(CType(CType(sender, MyIconButton).Parent, MyListItem).Tag)
    End Sub

    '点击选项
    Public Sub Version_Change(sender As Object, e As RouteEventArgs)
        SelectChange(CType(sender, MyListItem).Tag)
    End Sub
End Class
