Imports System.Collections.ObjectModel
Imports System.ComponentModel

Public Class MyDlEntry
    Inherits MyCard

    ''' <summary>
    ''' 卡片中每一条子下载任务的数据模型，将Loader作为唯一标识符
    ''' </summary>
    Public Class MyDlTaskEntry
        Implements INotifyPropertyChanged

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub New(Loader As LoaderBase)
            Me.Loader = Loader
            _LoaderState = Loader.State
            _Progress = Loader.Progress
            _Descreption = Loader.Name
        End Sub

        ''' <summary>
        ''' 检查值有无改变以及通知前端
        ''' </summary>
        Public Sub SyncValuesToUI()
            If (Not Loader.State = _LoaderState) Then
                _LoaderState = Loader.State
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("LoaderState"))
            End If
            If (Not Loader.Progress = _Progress) Then
                _Progress = Loader.Progress
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("PercentStr"))
            End If
        End Sub

        Public Loader As LoaderBase

        Private _LoaderState As LoadState
        Public ReadOnly Property LoaderState As LoadState
            Get
                Return _LoaderState
            End Get
        End Property

        Private _Progress As Double
        Public ReadOnly Property PercentStr As String
            Get
                Return Math.Floor(_Progress * 100) & "%"
            End Get
        End Property

        Private _Descreption As String
        Public ReadOnly Property Descreption As String
            Get
                Return _Descreption
            End Get
        End Property
    End Class

    ''' <summary>
    ''' TaskListBox的数据源
    ''' </summary>
    Public ReadOnly Property TaskEntries As New ObservableCollection(Of MyDlTaskEntry)

    Public Loader As LoaderBase

    ''' <summary>
    ''' 获取所有子下载任务
    ''' </summary>
    Private ReadOnly Property SubDlTasks As List(Of LoaderBase)
        Get
            Return CType(Loader, Object).GetLoaderList()
        End Get
    End Property

    Private _Failed As Boolean
    ''' <summary>
    ''' 是否已经失败，值发生改变时切换显示内容
    ''' </summary>
    Private Property Failed
        Get
            Return _Failed
        End Get
        Set(value)
            If value = _Failed Then Exit Property
            _Failed = value
            If value Then
                ExceptionHint.Text = GetExceptionDetail(Loader.Error)
                ExceptionHint.Visibility = Visibility.Visible
                TaskListBox.Visibility = Visibility.Collapsed
            Else '应该不会进到这个case里
                ExceptionHint.Visibility = Visibility.Collapsed
                TaskListBox.Visibility = Visibility.Visible
            End If
        End Set
    End Property

    Public Sub New(Loader As LoaderBase)
        InitializeComponent()
        Me.Loader = Loader
        Title = Loader.Name
        RefreshSubTasks()
    End Sub

    ''' <summary>
    ''' 同步前端状态（是否已失败、初次调用时添加子任务显示条目、刷新子任务显示条目的信息）
    ''' </summary>
    Public Sub RefreshSubTasks()
        If Failed Then Exit Sub
        Try
            If Loader.State = LoadState.Failed Then
                Failed = True
            Else
                For Each DlTask As LoaderBase In SubDlTasks
                    Dim TaskEntry = TaskEntries.FirstOrDefault(Function(t) t.Loader Is DlTask)
                    If TaskEntry Is Nothing Then '除了第一次调用之外不会进入这个case，因为LoaderCombo的子加载任务不会增加
                        TaskEntries.Add(New MyDlTaskEntry(DlTask))
                    Else
                        TaskEntry.SyncValuesToUI()
                    End If
                Next
            End If
        Catch ex As Exception
            Log(ex, "刷新下载管理显示失败", LogLevel.Feedback)
        End Try
    End Sub

    ''' <summary>
    ''' 点击失败提示卡片之后复制错误信息到剪贴板
    ''' </summary>
    Private Sub CopyExceptionDetail(sender As MyHint, e As EventArgs) Handles ExceptionHint.MouseLeftButtonDown
        ClipboardSet(sender.Text, False)
        Hint("已复制错误详情！", HintType.Finish)
    End Sub

    ''' <summary>
    ''' 点击取消按钮之后播放关闭动画、中止Loader
    ''' </summary>
    Private Sub Cancel(sender As MyIconButton, e As EventArgs) Handles BtnCancel.Click
        AniDispose(sender, False)
        AniDispose(Me, True, Sub() FrmSpeedRight?.TryReturnToHome())
        RunInThread(Sub() Loader.Abort())
        LoaderTaskbar.Remove(Loader)
    End Sub

End Class
