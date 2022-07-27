Public Class MyTextBox
    Inherits TextBox

    '自定义属性

    Public Property HasAnimation As Boolean = True
    Public Property ShowValidateResult As Boolean = True

    '额外控件初始化

    Private _labWrong As TextBlock = Nothing
    Private ReadOnly Property labWrong As TextBlock
        Get
            If Template Is Nothing Then Return Nothing
            If _labWrong Is Nothing Then _labWrong = Template.FindName("labWrong", Me)
            Return _labWrong
        End Get
    End Property
    Private _labHint As TextBlock = Nothing
    Private ReadOnly Property labHint As TextBlock
        Get
            If Template Is Nothing Then Return Nothing
            If _labHint Is Nothing Then _labHint = Template.FindName("labHint", Me)
            Return _labHint
        End Get
    End Property
    Public Overrides Sub OnApplyTemplate()
        MyBase.OnApplyTemplate()
        If HintText = "" OrElse labHint.Text <> "" Then Exit Sub
        labHint.Text = If(Text = "", HintText, "")
    End Sub

    '事件

    Public Uuid As Integer = GetUuid()
    Public Shared Event ValidateChanged(sender As Object, e As EventArgs)
    Public ChangedEventList As New List(Of RoutedEventHandler)
    Public Custom Event ValidatedTextChanged As RoutedEventHandler
        AddHandler(value As RoutedEventHandler)
            ChangedEventList.Add(value)
        End AddHandler
        RemoveHandler(value As RoutedEventHandler)
            ChangedEventList.Remove(value)
        End RemoveHandler
        RaiseEvent(sender As Object, e As TextChangedEventArgs)
            For Each handler As RoutedEventHandler In ChangedEventList
                If Not IsNothing(handler) Then handler.Invoke(sender, e)
            Next
        End RaiseEvent
    End Event

    '输入验证

    ''' <summary>
    ''' 输入验证结果。若为空字符串则无错误，否则为第一个错误原因。
    ''' </summary>
    Public Property ValidateResult As String
        Get
            Return GetValue(ValidateResultProperty)
        End Get
        Set(value As String)
            SetValue(ValidateResultProperty, value)
        End Set
    End Property
    Public Shared ReadOnly ValidateResultProperty As DependencyProperty = DependencyProperty.Register("ValidateResult", GetType(String), GetType(MyTextBox), New PropertyMetadata(""))
    ''' <summary>
    ''' 输入验证的规则。
    ''' </summary>
    Public Property ValidateRules As ObjectModel.Collection(Of Validate)
        Get
            Return _ValidateRules
        End Get
        Set(Value As ObjectModel.Collection(Of Validate))
            _ValidateRules = Value
            Validate()
        End Set
    End Property
    Private _ValidateRules As New ObjectModel.Collection(Of Validate)
    ''' <summary>
    ''' 进行输入验证。
    ''' </summary>
    Public Sub Validate() Handles Me.Loaded
        '执行输入验证
        ValidateResult = ""
        For Each ValidateRule As Validate In ValidateRules
            ValidateResult = ValidateRule.Validate(Text)
            If IsNothing(ValidateResult) OrElse Not ValidateResult = "" Then Exit For
        Next
        Dim NewResult As Integer = If(ValidateResult = "", 0, 1)
        '根据结果改变样式
        If ShownValidateResult <> NewResult Then
            If IsLoaded AndAlso labWrong IsNot Nothing Then
                ChangeValidateResult(ValidateResult = "", True)
            Else
                RunInNewThread(Sub()
                                   Thread.Sleep(30)
                                   RunInUi(Sub() ChangeValidateResult(NewResult, False))
                               End Sub, "DelayedValidate Change")
            End If
        End If
        '更新错误信息
        If ShowValidateResult AndAlso Not ValidateResult = "" Then
            If IsLoaded AndAlso labWrong IsNot Nothing Then
                labWrong.Text = ValidateResult
            Else
                RunInNewThread(Sub()
                                   Dim IsFinished As Boolean = False
                                   Do Until IsFinished
                                       Thread.Sleep(20)
                                       RunInUiWait(Sub()
                                                       If labWrong IsNot Nothing Then
                                                           labWrong.Text = ValidateResult
                                                           IsFinished = True
                                                       End If
                                                       If Not IsLoaded Then IsFinished = True
                                                   End Sub)
                                   Loop
                               End Sub, "DelayedValidate Text")
            End If
        End If
    End Sub
    Private ShownValidateResult As Integer = -1
    Private Sub ChangeValidateResult(NewResult As Boolean, IsLoaded As Boolean)
        If IsLoaded AndAlso AniControlEnabled = 0 AndAlso IsTextChanged AndAlso labWrong IsNot Nothing Then
            If NewResult Then
                '变为正确
                ShownValidateResult = 0
                AniStart({
                         AaOpacity(labWrong, -labWrong.Opacity, 150),
                         AaHeight(labWrong, -labWrong.Height, 150,, New AniEaseOutFluent),
                         AaCode(Sub() labWrong.Visibility = Visibility.Collapsed,, True)
                    }, "MyTextBox Validate " & Uuid)
            ElseIf ShowValidateResult Then
                '变为错误
                ShownValidateResult = 1
                labWrong.Visibility = Visibility.Visible
                AniStart({
                         AaOpacity(labWrong, 1 - labWrong.Opacity, 170),
                         AaHeight(labWrong, 21 - labWrong.Height, 300,, New AniEaseOutBack(AniEasePower.Weak))
                    }, "MyTextBox Validate " & Uuid)
            Else
                '变为错误，但不显示文本
                ShownValidateResult = 2
            End If
        Else
            ShownValidateResult = 3
            'AniStop("MyTextBox Validate " & Uuid)
            'If NewResult Then
            '    '变为正确
            '    labWrong.Opacity = 0
            '    labWrong.Height = 0
            '    labWrong.Visibility = Visibility.Collapsed
            'Else
            '    '变为错误
            '    labWrong.Visibility = Visibility.Visible
            '    labWrong.Text = ValidateResult
            '    labWrong.Opacity = 1
            '    labWrong.Height = 20.5
            'End If
            'Dim OriginalEnabled As Integer = AniControlEnabled
            'AniControlEnabled += 1
            'RefreshColor()
            'AniControlEnabled = OriginalEnabled
        End If
        RefreshColor()
        RaiseEvent ValidateChanged(Me, New EventArgs)
    End Sub

    '提示文本

    ''' <summary>
    ''' 是否已经由用户输入过文本，若尚未输入过，则不显示输入检查的失败。
    ''' </summary>
    Private IsTextChanged As Boolean = False
    Private _HintText As String = ""
    Public Property HintText As String
        Get
            Return _HintText
        End Get
        Set(value As String)
            _HintText = value
            If labHint IsNot Nothing Then labHint.Text = If(Text = "", HintText, "")
        End Set
    End Property
    Private Sub MyTextBox_TextChanged(sender As MyTextBox, e As TextChangedEventArgs) Handles Me.TextChanged
        Try
            '改变提示文本
            If labHint IsNot Nothing Then labHint.Text = If(Text = "", HintText, "")
            '改变输入记录
            IsTextChanged = IsLoaded
            '进行输入验证
            Validate()
            If Not ValidateResult = "" Then Exit Sub
            '改变文本
            RaiseEvent ValidatedTextChanged(sender, e)
        Catch ex As Exception
            Log(ex, "进行输入验证时出错", LogLevel.Assert)
        End Try
    End Sub

    '颜色

    Private Sub RefreshColor() Handles Me.IsEnabledChanged, Me.MouseEnter, Me.MouseLeave, Me.GotFocus, Me.LostFocus
        Try
            '手动关闭动画
            If Not HasAnimation Then Exit Sub
            '不对 ComboBox 从属进行动画
            If TemplatedParent IsNot Nothing AndAlso TypeOf TemplatedParent Is MyComboBox Then Exit Sub
            '判断当前颜色
            Dim ForeColorName As String, BackColorName As String
            Dim AnimationTime As Integer
            If IsEnabled Then
                If ValidateResult = "" OrElse Not IsTextChanged Then
                    If IsFocused Then
                        ForeColorName = "ColorBrush4"
                        BackColorName = "ColorBrush9"
                        AnimationTime = 60
                    ElseIf IsMouseOver Then
                        ForeColorName = "ColorBrush3"
                        BackColorName = "ColorBrushHalfWhite"
                        AnimationTime = 100
                    Else
                        ForeColorName = "ColorBrush1"
                        BackColorName = "ColorBrushHalfWhite"
                        AnimationTime = 200
                    End If
                Else
                    ForeColorName = "ColorBrushRedLight"
                    BackColorName = "ColorBrushRedBack"
                    AnimationTime = 200
                    'If IsFocused OrElse IsMouseOver Then
                    '    ForeColorName = "ColorBrushRedLight"
                    '    BackColorName = "ColorBrushRedBack"
                    '    AnimationTime = 100
                    'Else
                    '    ForeColorName = "ColorBrushRedDark"
                    '    BackColorName = "ColorBrushHalfWhite"
                    '    AnimationTime = 200
                    'End If
                End If
            Else
                ForeColorName = "ColorBrushGray4"
                BackColorName = "ColorBrushHalfWhite"
                AnimationTime = 200
            End If
            '触发颜色动画
            If IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
                '有动画
                AniStart({
                         AaColor(Me, BorderBrushProperty, ForeColorName, AnimationTime)，
                         AaColor(Me, BackgroundProperty, BackColorName, AnimationTime)
                     }, "MyTextBox Color " & Uuid)
            Else
                '无动画
                AniStop("MyTextBox Color " & Uuid)
                SetResourceReference(BorderBrushProperty, ForeColorName)
                SetResourceReference(BackgroundProperty, BackColorName)
            End If

        Catch ex As Exception
            Log(ex, "文本框颜色改变出错")
        End Try
    End Sub
    Private Sub RefreshTextColor() Handles Me.IsEnabledChanged
        Dim NewColor As MyColor = If(IsEnabled, ColorGray1, ColorGray4)
        If CType(Foreground, SolidColorBrush).Color.R = NewColor.R Then Exit Sub
        If IsLoaded AndAlso AniControlEnabled = 0 AndAlso Not Text = "" Then
            '有动画
            AniStart({AaColor(Me, ForegroundProperty, If(IsEnabled, "ColorBrushGray1", "ColorBrushGray4"), 200)}, "MyTextBox TextColor " & Uuid)
        Else
            '无动画
            AniStop("MyTextBox TextColor " & Uuid)
            Foreground = NewColor
        End If
    End Sub

End Class
