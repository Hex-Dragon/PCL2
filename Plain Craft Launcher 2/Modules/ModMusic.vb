Public Module ModMusic

#Region "播放列表"

    ''' <summary>
    ''' 接下来要播放的音乐文件路径。未初始化时为 Nothing。
    ''' </summary>
    Public MusicToplayList As List(Of String) = Nothing
    ''' <summary>
    ''' 全部音乐文件路径。未初始化时为 Nothing。
    ''' </summary>
    Public MusicAllList As List(Of String) = Nothing
    ''' <summary>
    ''' 初始化音乐播放列表。
    ''' </summary>
    ''' <param name="ForceReload">强制全部重新载入列表。</param>
    ''' <param name="IgnoreFirst">在重载列表时避免让某项成为第一项。</param>
    Private Sub MusicListInit(ForceReload As Boolean, Optional IgnoreFirst As String = "")
        If ForceReload Then MusicAllList = Nothing
        Try
            '初始化全部可用音乐列表
            If MusicAllList Is Nothing Then
                MusicAllList = New List(Of String)
                Directory.CreateDirectory(Path & "PCL\Musics\")
                For Each File In My.Computer.FileSystem.GetFiles(Path & "PCL\Musics\", FileIO.SearchOption.SearchAllSubDirectories, "*.*")
                    '文件夹可能会被加入 .ini 文件夹配置文件、一些乱七八糟的 .jpg 文件啥的
                    Dim Extend As String = File.Split(".").Last.ToLower
                    If Not (Extend = "ini" OrElse Extend = "jpg" OrElse Extend = "txt" OrElse Extend = "cfg" OrElse Extend = "png") Then
                        MusicAllList.Add(File)
                    End If
                Next
            End If
            '打乱顺序播放
            MusicToplayList = RandomChaos(New List(Of String)(MusicAllList))
            If Not IgnoreFirst = "" AndAlso Not MusicToplayList.Count = 0 AndAlso MusicToplayList(0) = IgnoreFirst Then
                '若需要避免成为第一项的为第一项，则将它放在最后
                MusicToplayList.RemoveAt(0)
                MusicToplayList.Add(IgnoreFirst)
            End If
        Catch ex As Exception
            Log(ex, "初始化音乐列表失败", LogLevel.Feedback)
        End Try
    End Sub
    ''' <summary>
    ''' 获取下一首播放的音乐路径并将其从列表中移除。
    ''' </summary>
    Private Function DequeueNextMusicAddress() As String
        '初始化，确保存在音乐
        If MusicAllList Is Nothing Then MusicListInit(False)
        If MusicAllList.Count = 0 Then Throw New Exception("在没有音乐时尝试获取音乐路径")
        '出列下一个音乐，如果出列结束则生成新列表
        DequeueNextMusicAddress = MusicToplayList(0)
        MusicToplayList.RemoveAt(0)
        If MusicToplayList.Count = 0 Then MusicListInit(False, DequeueNextMusicAddress)
    End Function

#End Region

#Region "UI 控制"

    ''' <summary>
    ''' 刷新背景音乐按钮 UI 与设置页 UI。
    ''' </summary>
    Private Sub MusicRefreshUI()
        RunInUi(Sub()
                    Try

                        If MusicAllList.Count = 0 Then
                            '无背景音乐
                            FrmMain.BtnExtraMusic.Show = False
                        Else
                            '有背景音乐
                            FrmMain.BtnExtraMusic.Show = True

                            Dim ToolTipText As String = "正在播放：" & GetFileNameWithoutExtentionFromPath(MusicCurrent)
                            If MusicState = MusicStates.Pause Then
                                FrmMain.BtnExtraMusic.Logo = Logo.IconPlay
                                FrmMain.BtnExtraMusic.LogoScale = 0.8
                                If MusicAllList.Count > 1 Then ToolTipText += vbCrLf & "左键播放，右键播放下一曲。"
                            Else
                                FrmMain.BtnExtraMusic.Logo = Logo.IconMusic
                                FrmMain.BtnExtraMusic.LogoScale = 1
                                If MusicAllList.Count > 1 Then ToolTipText += vbCrLf & "左键暂停，右键播放下一曲。"
                            End If
                            FrmMain.BtnExtraMusic.ToolTip = ToolTipText
                            ToolTipService.SetVerticalOffset(FrmMain.BtnExtraMusic, If(ToolTipText.Contains(vbLf), 10, 16))
                        End If
                        If FrmSetupUI IsNot Nothing Then FrmSetupUI.MusicRefreshUI()

                    Catch ex As Exception
                        Log(ex, "刷新背景音乐 UI 失败", LogLevel.Feedback)
                    End Try
                End Sub)
    End Sub

    ''' <summary>
    ''' 让音乐在暂停、播放间切换，并显示提示文本。
    ''' </summary>
    Public Sub MusicControlPause()
        If MusicNAudio Is Nothing Then
            Hint("音乐播放尚未开始！", HintType.Critical)
        Else
            Select Case MusicState
                Case MusicStates.Pause
                    MusicResume()
                Case MusicStates.Play
                    MusicPause()
                Case Else
                    Hint("音乐目前为停止状态！", HintType.Critical)
            End Select
        End If
    End Sub

    ''' <summary>
    ''' 播放下一曲，并显示提示文本。
    ''' </summary>
    Public Sub MusicControlNext()
        If MusicAllList.Count = 1 Then
            Hint("播放列表中仅有一首歌曲！")
        Else
            Dim Address As String = DequeueNextMusicAddress()
            MusicStartPlay(Address)
            Hint("正在播放：" & GetFileNameFromPath(Address), HintType.Finish)
            MusicRefreshUI()
        End If
    End Sub

#End Region

#Region "主状态控制"

    ''' <summary>
    ''' 获取当前的音乐播放状态。
    ''' </summary>
    Public ReadOnly Property MusicState As MusicStates
        Get
            If MusicNAudio Is Nothing Then Return MusicStates.Stop
            Select Case MusicNAudio.PlaybackState
                Case 0 'NAudio.Wave.PlaybackState.Stopped
                    Return MusicStates.Stop
                Case 2 'NAudio.Wave.PlaybackState.Paused
                    Return MusicStates.Pause
                Case Else
                    Return MusicStates.Play
            End Select
        End Get
    End Property
    Public Enum MusicStates
        [Stop]
        Play
        Pause
    End Enum

    '重载与开始

    ''' <summary>
    ''' 重载播放列表并尝试开始播放背景音乐。
    ''' </summary>
    ''' <param name="ShowHint">是否显示刷新提示。</param>
    Public Sub MusicRefreshPlay(ShowHint As Boolean, Optional IsFirstLoad As Boolean = False)
        Try

            MusicListInit(True)
            If MusicAllList.Count = 0 Then
                If MusicNAudio Is Nothing Then
                    If ShowHint Then Hint("未检测到可用的背景音乐！", HintType.Critical)
                Else
                    MusicNAudio = Nothing
                    If ShowHint Then Hint("背景音乐已清除！", HintType.Finish)
                End If
            Else
                Dim Address As String = DequeueNextMusicAddress()
                Try
                    MusicStartPlay(Address, IsFirstLoad)
                    If ShowHint Then Hint("背景音乐已刷新：" & GetFileNameFromPath(Address), HintType.Finish, False)
                Catch
                End Try
            End If
            MusicRefreshUI()

        Catch ex As Exception
            Log(ex, "刷新背景音乐播放失败", LogLevel.Feedback)
        End Try
    End Sub
    ''' <summary>
    ''' 开始播放音乐。
    ''' </summary>
    Private Sub MusicStartPlay(Address As String, Optional IsFirstLoad As Boolean = False)
        Log("[Music] 播放开始：" & Address)
        RunInNewThread(Sub() MusicLoop(Address, IsFirstLoad), "Music", ThreadPriority.BelowNormal)
    End Sub

    '播放与暂停

    ''' <summary>
    ''' 暂停音乐播放，返回是否成功切换了状态。
    ''' </summary>
    Public Function MusicPause() As Boolean
        If MusicState = MusicStates.Play Then
            RunInThread(Sub()
                            MusicNAudio.Pause()
                            MusicRefreshUI()
                            Log("[Music] 已暂停播放")
                        End Sub)
            Return True
        Else
            Return False
        End If
    End Function
    ''' <summary>
    ''' 继续音乐播放，返回是否成功切换了状态。
    ''' </summary>
    Public Function MusicResume() As Boolean
        If MusicState = MusicStates.Pause Then
            RunInThread(Sub()
                            MusicNAudio.Play()
                            MusicRefreshUI()
                            Log("[Music] 已恢复播放")
                        End Sub)
            Return True
        Else
            Return False
        End If
    End Function

#End Region

    ''' <summary>
    ''' 当前正在播放的 NAudio.Wave.WaveOut。
    ''' </summary>
    Public MusicNAudio = Nothing
    ''' <summary>
    ''' 当前播放的音乐地址。
    ''' </summary>
    Private MusicCurrent As String = ""

    ''' <summary>
    ''' 在 MusicUuid 不变的前提下，持续播放某地址的音乐，且在播放结束后随机播放下一曲。
    ''' </summary>
    Private Sub MusicLoop(Address As String, Optional IsFirstLoad As Boolean = False)
        MusicCurrent = Address
        Dim CurrentWave As NAudio.Wave.WaveOut = Nothing
        Dim Reader As NAudio.Wave.WaveStream = Nothing
        Try
            '开始播放
            CurrentWave = New NAudio.Wave.WaveOut()
            MusicNAudio = CurrentWave
            Reader = New NAudio.Wave.AudioFileReader(Address)
            CurrentWave.Init(Reader)
            CurrentWave.Play()
            '第一次打开的暂停
            If IsFirstLoad AndAlso Not Setup.Get("UiMusicAuto") Then CurrentWave.Pause()
            MusicRefreshUI()
            '停止条件：播放完毕或变化
            Dim PreviousVolume = 0
            While CurrentWave.Equals(MusicNAudio) AndAlso Not CurrentWave.PlaybackState = NAudio.Wave.PlaybackState.Stopped
                If Setup.Get("UiMusicVolume") <> PreviousVolume Then
                    '更新音量
                    PreviousVolume = Setup.Get("UiMusicVolume")
                    CurrentWave.Volume = PreviousVolume / 1000
                End If
                Thread.Sleep(50)
            End While
            '当前音乐已播放结束，继续下一曲
            If CurrentWave.PlaybackState = NAudio.Wave.PlaybackState.Stopped AndAlso MusicAllList.Count > 0 Then MusicStartPlay(DequeueNextMusicAddress)
        Catch ex As Exception
            If ex.Message.Contains("Got a frame at sample rate") OrElse ex.Message.Contains("does not support changes to") Then
                Hint("播放音乐失败（" & GetFileNameFromPath(Address) & "）：PCL2 不支持播放音频属性在中途发生变化的音乐", HintType.Critical)
            ElseIf Not (Address.ToLower.EndsWith(".wav") OrElse Address.ToLower.EndsWith(".mp3") OrElse Address.ToLower.EndsWith(".flac")) Then
                Hint("播放音乐失败（" & GetFileNameFromPath(Address) & "）：PCL2 可能不支持此音乐格式，请将格式转换为 .wav、.mp3 或 .flac 后再试", HintType.Critical)
            Else
                Log(ex, "播放音乐失败（" & GetFileNameFromPath(Address) & "）", LogLevel.Hint)
            End If
            Log(ex, "播放音乐失败（" & Address & "）", LogLevel.Developer)
            If MusicAllList.Count > 1 Then
                Thread.Sleep(1000)
                MusicStartPlay(DequeueNextMusicAddress)
            End If
        Finally
            If CurrentWave IsNot Nothing Then CurrentWave.Dispose()
            If Reader IsNot Nothing Then Reader.Dispose()
            MusicRefreshUI()
        End Try
    End Sub

End Module
