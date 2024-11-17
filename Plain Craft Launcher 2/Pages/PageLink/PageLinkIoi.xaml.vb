Imports System.Net.Sockets

Public Class PageLinkIoi
    Public Const RequestVersion As Integer = 4
    Public Const IoiVersion As Integer = 10 '由于已关闭更新渠道，在提升 IoiVersion 时必须提升 RequestVersion
    Public Shared PathIoi As String = PathAppdata & "联机模块\IOI 联机模块.exe"

#Region "进程管理"

    Private Shared IoiId As String, IoiPassword As String
    Private Shared IoiProcess As Process = Nothing
    Private Shared IoiState As LoadState = LoadState.Waiting

    ''' <summary>
    ''' 若 Ioi 正在运行，则结束 Ioi 进程，同时初始化状态数据。返回是否关闭了对应进程。
    ''' </summary>
    Public Shared Function IoiStop(SleepWhenKilled As Boolean) As Boolean
        Return False
    End Function
    ''' <summary>
    ''' 启动 Ioi，并等待初始化完成后退出运行，同时更新 IoiId 与 IoiPassword。
    ''' 正常初始化返回 True，需要更新返回 False，其余情况抛出异常。
    ''' 若 Ioi 正在运行，则会先停止其运行。
    ''' </summary>
    Public Shared Function IoiStart() As Boolean
        Return False
    End Function

    'Ioi 日志
    Private Shared Sub IoiLogLine(Content As String)
    End Sub
    Private Shared LogLinesCount As Integer = 0
    Private Shared LastPortsId As String = "" '上一个收到 portssub 的 ID，用于记录端口

#End Region

#Region "时钟"

    'UI 线程刷新
    Private Shared UserListIdentifyCache As String = ""
    Private Shared RoomListIdentifyCache As String = ""
    Public Sub RefreshUi()
    End Sub

    '工作线程刷新
    Public Sub RefreshWorker()
    End Sub

#End Region

#Region "发送请求"

    ''' <summary>
    ''' 发送 Portsub 请求并等待获取控制台端口。进度将从 0 变化至 80%。
    ''' </summary>
    Private Shared Sub SendPortsubRequest(User As LinkUserIoi)
    End Sub

    ''' <summary>
    ''' 向控制台发送 Connect 请求。
    ''' </summary>
    Private Shared Sub SendConnectRequest(User As LinkUserIoi)
        Dim RawJson As New JObject()
        RawJson("version") = RequestVersion
        RawJson("name") = GetPlayerName()
        RawJson("id") = IoiId
        RawJson("type") = "connect"
        User.Send(RawJson)
    End Sub

    ''' <summary>
    ''' 向控制台发送 Update 请求。
    ''' </summary>
    Private Shared Sub SendUpdateRequest(User As LinkUserIoi, Stage As Integer, Optional Unique As Long = -1)
        If Unique = -1 Then Unique = GetTimeTick()
        Dim RawJson As New JObject
        RawJson("name") = GetPlayerName()
        RawJson("id") = IoiId
        RawJson("type") = "update"
        RawJson("stage") = Stage
        RawJson("unique") = Unique
        If Stage < 3 Then
            Dim Rooms As New JArray
            For Each Room In RoomListForMe
                Dim RoomObject As New JObject
                RoomObject("name") = Room.DisplayName
                RoomObject("port") = Room.Port
                Rooms.Add(RoomObject)
            Next
            RawJson("rooms") = Rooms
            User.PingPending(Unique) = Date.Now
        End If
        User.Send(RawJson)
    End Sub

    ''' <summary>
    ''' 尝试发送断开请求，并将其从用户列表中移除。
    ''' </summary>
    Private Shared Sub SendDisconnectRequest(User As LinkUserIoi, Optional Message As String = Nothing, Optional IsError As Boolean = False)
    End Sub

#End Region

#Region "左边栏操作"

    '刷新连接
    Private Shared Sub BtnListRefresh_Click(sender As MyIconButton, e As EventArgs)
    End Sub
    '断开连接
    Private Shared Sub BtnListDisconnect_Click(sender As MyIconButton, e As EventArgs)
    End Sub
    '复制联机码
    Public Shared Sub BtnLeftCopy_Click() Handles BtnLeftCopy.Click
        ClipboardSet(IoiId.Substring(4) & SecretEncrypt(GetPlayerName), False)
        Hint("已复制联机码！", HintType.Finish)
    End Sub

#End Region

#Region "玩家名"

    ''' <summary>
    ''' 获取当前的玩家名。
    ''' </summary>
    Public Shared Function GetPlayerName() As String
        '自动生成玩家名
        If AutogenPlayerName Is Nothing Then
            If IsPlayerNameValid(McLoginName) Then
                AutogenPlayerName = McLoginName()
            Else
                AutogenPlayerName = "玩家 " & CType(GetHash(If(UniqueAddress, "")) Mod 1048576, Integer).ToString("x5").ToUpper
            End If
        End If
        '获取玩家自定义的名称
        Dim CustomName As String = Setup.Get("LinkName").ToString.Trim()
        If CustomName <> "" Then
            If IsPlayerNameValid(CustomName) Then
                Return CustomName.Trim
            Else
                Hint("你所设置的玩家名存在异常，已被重置！", HintType.Critical)
                Setup.Set("LinkName", "")
            End If
        End If
        '使用自动生成的玩家名
        Return AutogenPlayerName
    End Function
    Private Shared AutogenPlayerName As String = Nothing '并非由玩家自定义，而是自动生成的玩家名
    ''' <summary>
    ''' 检查某个玩家名是否合法。
    ''' </summary>
    Private Shared Function IsPlayerNameValid(Name As String) As Boolean
        Return True
    End Function

#End Region

#Region "请求核心"

    ''' <summary>
    ''' 启动 Socket 监听核心。
    ''' </summary>
    Public Shared Sub StartSocketListener()
    End Sub

#End Region

#Region "用户核心"

    '用户基类
    Public MustInherit Class LinkUserBase
        Implements IDisposable

        '基础数据
        Public Uuid As Integer = GetUuid()
        Public Id As String
        Public DisplayName As String

        '请求管理
        Public Socket As Socket = Nothing
        Public Sub Send(Request As JObject)
        End Sub
        Public ListenerThread As Thread = Nothing
        Public Sub StartListener()
        End Sub
        Public Sub BindSocket(Socket As Socket)
            If Me.Socket IsNot Nothing Then Throw New Exception("该用户已经绑定了 Socket")
            Me.Socket = Socket
            StartListener()
        End Sub

        'Ping
        '0：与 Ping 计算无关，不回应
        '1：A to B，2：B to A，3：A to B
        Public PingPending As New Dictionary(Of Long, Date)
        Public PingRecord As New Queue(Of Integer)

        '心跳包
        Public LastSend As Date = Date.Now
        Public LastReceive As Date = Date.Now

        '类型转换
        Public Sub New(Id As String, DisplayName As String)
            Me.Id = Id
            Me.DisplayName = DisplayName
            Log("[IOI] 无通信包的新用户对象：" & ToString())
        End Sub
        Public Sub New(Id As String, DisplayName As String, Socket As Socket)
            Me.Id = Id
            Me.DisplayName = DisplayName
            Me.Socket = Socket
            Log("[IOI] 新用户对象：" & ToString())
            StartListener()
        End Sub
        Public Overrides Function ToString() As String
            Return DisplayName & " @ " & Id & " #" & Uuid
        End Function
        Public Shared Widening Operator CType(User As LinkUserBase) As String
            Return User.ToString
        End Operator

        '释放资源
        Public IsDisposed As Boolean = False
        Protected Overridable Sub Dispose(IsDisposing As Boolean)
            If Socket IsNot Nothing Then Socket.Dispose()
            If ListenerThread IsNot Nothing AndAlso ListenerThread.IsAlive Then ListenerThread.Interrupt()
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            If Not IsDisposed Then
                IsDisposed = True
                Dispose(True)
            End If
            GC.SuppressFinalize(Me)
        End Sub
    End Class

    '用户对象
    Public Shared UserList As New Dictionary(Of String, LinkUserIoi)
    Public Class LinkUserIoi
        Inherits LinkUserBase
        Public Sub New(Id As String, DisplayName As String, Socket As Socket)
            MyBase.New(Id, DisplayName, Socket)
        End Sub
        Public Sub New(Id As String, DisplayName As String)
            MyBase.New(Id, DisplayName)
        End Sub

        '基础数据
        Public Ports As New Dictionary(Of Integer, String)
        Public Rooms As New List(Of RoomEntry)

        '进度与 UI
        Public Progress As Double = 0
        Public RelativeThread As Thread = Nothing

        Public Function GetDescription() As String
            Return If(Progress < 1,
                "正在连接，" & Math.Round(Progress * 100) & "%",
                "已连接，" & If(Not PingRecord.Any(), "检查延迟中", Math.Round(PingRecord.Average) & "ms"))
        End Function
        Public Function ToListItem() As MyListItem
            Dim Item As New MyListItem With {
                .Title = DisplayName, .Height = 42, .Tag = Me, .Type = MyListItem.CheckType.None,
                .Logo = "pack://application:,,,/images/Blocks/Grass.png"}
            '绑定图标按钮
            Dim BtnRefresh As New MyIconButton With {.Logo = Logo.IconButtonRefresh, .LogoScale = 0.85, .ToolTip = "刷新", .Tag = Me}
            AddHandler BtnRefresh.Click, AddressOf BtnListRefresh_Click
            ToolTipService.SetPlacement(BtnRefresh, Primitives.PlacementMode.Bottom)
            ToolTipService.SetHorizontalOffset(BtnRefresh, -10)
            ToolTipService.SetVerticalOffset(BtnRefresh, 5)
            ToolTipService.SetInitialShowDelay(BtnRefresh, 200)
            Dim BtnClose As New MyIconButton With {.Logo = Logo.IconButtonCross, .LogoScale = 0.85, .ToolTip = "断开", .Tag = Me}
            AddHandler BtnClose.Click, AddressOf BtnListDisconnect_Click
            ToolTipService.SetPlacement(BtnClose, Primitives.PlacementMode.Bottom)
            ToolTipService.SetHorizontalOffset(BtnClose, -10)
            ToolTipService.SetVerticalOffset(BtnClose, 5)
            ToolTipService.SetInitialShowDelay(BtnClose, 200)
            Item.Buttons = {BtnRefresh, BtnClose}
            '刷新并返回
            RefreshUi(Item)
            Return Item
        End Function
        Public Sub RefreshUi(RelatedListItem As MyListItem)
            RelatedListItem.Title = DisplayName
            RelatedListItem.Info = GetDescription()
            RelatedListItem.Buttons(0).Visibility = If(Progress = 1, Visibility.Visible, Visibility.Collapsed)
        End Sub

        '释放
        Protected Overrides Sub Dispose(IsDisposing As Boolean)
            Log("[IOI] 用户资源释放（IOI, " & DisplayName & "）")
            If RelativeThread IsNot Nothing AndAlso RelativeThread.IsAlive Then RelativeThread.Interrupt()
            UserList.Remove(Id)
            MyBase.Dispose(IsDisposing)
        End Sub
    End Class

    '房间对象
    Private Shared RoomListForMe As New List(Of RoomEntry)

    Private Function GetRoomList() As List(Of RoomEntry)
        Dim RoomList As New List(Of RoomEntry)(RoomListForMe)
        For i = 0 To UserList.Count - 1
            If i > UserList.Count - 1 Then Exit For
            RoomList.AddRange(UserList.Values(i).Rooms)
        Next
        Return RoomList
    End Function
    Public Class RoomEntry

        '基础数据
        Public Port As Integer
        Public DisplayName As String
        Public User As LinkUserIoi = Nothing '若 IsOwner = True，则此项为 Nothing
        Public IsOwner As Boolean
        Public ReadOnly Property Ip As String
            Get
                If IsOwner Then
                    Return "localhost:" & Port
                Else
                    Return User.Ports(Port) & ":" & Port
                End If
            End Get
        End Property

        '类型转换
        Public Sub New(Port As Integer, DisplayName As String, Optional User As LinkUserIoi = Nothing)
            Me.IsOwner = User Is Nothing
            Me.User = User
            Me.DisplayName = DisplayName
            Me.Port = Port
        End Sub
        Public Overrides Function ToString() As String
            Return DisplayName & " - " & Port & " - " & IsOwner
        End Function
        Public Shared Widening Operator CType(Room As RoomEntry) As String
            Return Room.ToString
        End Operator
        Public Shared Function SelectPort(Room As RoomEntry) As Integer
            Return Room.Port
        End Function

        'UI
        Public Function GetDescription() As String
            If IsOwner Then
                Return "由我创建，端口 " & Port
            Else
                Return "由 " & User.DisplayName & " 创建，端口 " & Port
            End If
        End Function
        Public Function ToListItem() As MyListItem
            Dim Item As New MyListItem With {
                .Title = DisplayName, .Height = 42, .Info = GetDescription(), .Tag = Me,
                .Type = If(IsOwner, MyListItem.CheckType.None, MyListItem.CheckType.Clickable),
                .Logo = "pack://application:,,,/images/Blocks/" & If(IsOwner, "GrassPath", "Grass") & ".png"}
            If IsOwner Then
                '绑定图标按钮
                Dim BtnEdit As New MyIconButton With {.Logo = Logo.IconButtonEdit, .LogoScale = 1, .ToolTip = "修改名称", .Tag = Me}
                AddHandler BtnEdit.Click, AddressOf BtnRoomEdit_Click
                ToolTipService.SetPlacement(BtnEdit, Primitives.PlacementMode.Bottom)
                ToolTipService.SetHorizontalOffset(BtnEdit, -22)
                ToolTipService.SetVerticalOffset(BtnEdit, 5)
                ToolTipService.SetInitialShowDelay(BtnEdit, 200)
                Dim BtnClose As New MyIconButton With {.Logo = Logo.IconButtonCross, .LogoScale = 0.85, .ToolTip = "关闭", .Tag = Me}
                AddHandler BtnClose.Click, AddressOf BtnRoomClose_Click
                ToolTipService.SetPlacement(BtnClose, Primitives.PlacementMode.Bottom)
                ToolTipService.SetHorizontalOffset(BtnClose, -10)
                ToolTipService.SetVerticalOffset(BtnClose, 5)
                ToolTipService.SetInitialShowDelay(BtnClose, 200)
                Item.Buttons = {BtnEdit, BtnClose}
            Else
                '绑定点击事件
                AddHandler Item.Click, AddressOf BtnRoom_Click
            End If
            Return Item
        End Function
        Public Sub RefreshUi(RelatedListItem As MyListItem)
            RelatedListItem.Title = DisplayName
            RelatedListItem.Info = GetDescription()
        End Sub

    End Class

#End Region

    '正向与反向连接
    Public Shared Sub BtnLeftCreate_Click() Handles BtnLeftCreate.Click
    End Sub
    Private Shared Sub SendPortsubBack(User As LinkUserIoi, TargetVersion As Integer)
    End Sub

    '创建房间
    Private Sub LinkCreate() Handles BtnCreate.Click
    End Sub
    Private Shared Sub SendUpdateRequestToAllUsers()
        For i = 0 To UserList.Count - 1
            If i > UserList.Count - 1 Then Exit For
            Dim User = UserList.Values(i)
            If User.Progress < 1 Then Continue For
            Try
                SendUpdateRequest(User, 1) '不需要使用多线程，发送实际会瞬间完成
            Catch ex As Exception
                Log(ex, "发送全局刷新请求失败（" & User.DisplayName & "）")
            End Try
        Next
    End Sub
    '修改房间名称
    Private Shared Sub BtnRoomEdit_Click(sender As MyIconButton, e As EventArgs)
    End Sub
    '加入房间
    Private Shared Sub BtnRoom_Click(sender As MyListItem, e As EventArgs)
        Dim Room As RoomEntry = sender.Tag
        If MyMsgBox("请在多人游戏页面点击直接连接，输入 " & Room.Ip & " 以进入服务器！", "加入房间", "复制地址", "确定") = 1 Then
            ClipboardSet(Room.Ip)
        End If
    End Sub
    '关闭房间
    Private Shared Sub BtnRoomClose_Click(sender As MyIconButton, e As EventArgs)
    End Sub

    '获取数据包
    Public Shared Sub ReceiveJson(JsonData As JObject, Optional NewSocket As Socket = Nothing)
    End Sub
    ''' <summary>
    ''' 从用户列表中移除一位用户。提示信息视作该用户主动离开。
    ''' </summary>
    Public Shared Sub UserRemove(User As LinkUserIoi, ShowLeaveMessage As Boolean)
    End Sub

    Public Shared Sub ModuleStopManually()
    End Sub

End Class
