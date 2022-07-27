Public Class PageDownloadInstall

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(LoadMinecraft, PanLoad, PanBack, Nothing, DlClientListLoader, AddressOf LoadMinecraft_OnFinish)
    End Sub

    Private IsLoad As Boolean = False
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
        DlOptiFineListLoader.Start()
        DlLiteLoaderListLoader.Start()
        DlFabricListLoader.Start()

        '重载预览
        TextSelectName.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(PathMcFolder & "versions")}
        TextSelectName.Validate()
        SelectReload()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

        McDownloadForgeRecommendedRefresh()

        LoadOptiFine.State = DlOptiFineListLoader
        LoadLiteLoader.State = DlLiteLoaderListLoader
        LoadFabric.State = DlFabricListLoader
        LoadFabricApi.State = DlFabricApiLoader
        LoadOptiFabric.State = DlOptiFabricLoader
    End Sub

#Region "页面切换"

    '页面切换动画
    Private IsInSelectPage As Boolean = False
    Private IsFirstLoaded As Boolean = False
    Private Sub EnterSelectPage()
        If IsInSelectPage Then Exit Sub
        IsInSelectPage = True

        IsSelectNameEdited = False
        PanSelect.Visibility = Visibility.Visible
        PanSelect.IsHitTestVisible = True
        PanMinecraft.IsHitTestVisible = False
        PanBack.IsHitTestVisible = False
        PanBack.ScrollToHome()

        CardMinecraft.IsSwaped = True
        CardOptiFine.IsSwaped = True
        CardLiteLoader.IsSwaped = True
        CardForge.IsSwaped = True
        CardFabric.IsSwaped = True
        CardFabricApi.IsSwaped = True
        CardOptiFabric.IsSwaped = True

        If Not Setup.Get("HintInstallBack") Then
            Setup.Set("HintInstallBack", True)
            Hint("点击 Minecraft 项即可返回游戏主版本选择页面！")
        End If

        '如果在选择页面按了刷新键，选择页的东西可能会由于动画被隐藏，但不会由于加载结束而再次显示，因此这里需要手动恢复
        For Each Card In GetAllAnimControls(PanSelect)
            Card.Opacity = 1
            Card.RenderTransform = New TranslateTransform
        Next

        '启动 Forge 加载
        If SelectedMinecraftId.StartsWith("1.") Then
            Dim ForgeLoader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion " & SelectedMinecraftId, AddressOf DlForgeVersionMain)
            LoadForge.State = ForgeLoader
            ForgeLoader.Start(SelectedMinecraftId)
        End If

        '启动 Fabric API、OptiFabric 加载
        DlFabricApiLoader.Start()
        DlOptiFabricLoader.Start()

        AniStart({
            AaOpacity(PanMinecraft, -PanMinecraft.Opacity, 100, 10),
            AaTranslateX(PanMinecraft, -50 - CType(PanMinecraft.RenderTransform, TranslateTransform).X, 110, 10),
            AaCode(Sub()
                       PanBack.ScrollToHome()
                       TextSelectName.Validate()
                       OptiFine_Loaded()
                       LiteLoader_Loaded()
                       Forge_Loaded()
                       Fabric_Loaded()
                       FabricApi_Loaded()
                       OptiFabric_Loaded()
                       SelectReload()
                   End Sub, After:=True),
            AaOpacity(PanSelect, 1 - PanSelect.Opacity, 250, 150),
            AaTranslateX(PanSelect, -CType(PanSelect.RenderTransform, TranslateTransform).X, 500, 150, Ease:=New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(Sub()
                       PanMinecraft.Visibility = Visibility.Collapsed
                       PanBack.IsHitTestVisible = True
                       '初始化 Binding
                       If IsFirstLoaded Then Exit Sub
                       IsFirstLoaded = True
                       BtnOptiFineClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardOptiFine.MainTextBlock, .Mode = BindingMode.OneWay})
                       BtnLiteLoaderClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardLiteLoader.MainTextBlock, .Mode = BindingMode.OneWay})
                       BtnForgeClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardForge.MainTextBlock, .Mode = BindingMode.OneWay})
                       BtnFabricClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardFabric.MainTextBlock, .Mode = BindingMode.OneWay})
                       BtnFabricApiClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardFabricApi.MainTextBlock, .Mode = BindingMode.OneWay})
                       BtnOptiFabricClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardOptiFabric.MainTextBlock, .Mode = BindingMode.OneWay})
                   End Sub,, True)
        }, "FrmDownloadInstall SelectPageSwitch", True)
    End Sub
    Public Sub ExitSelectPage()
        If Not IsInSelectPage Then Exit Sub
        IsInSelectPage = False

        SelectClear() '清除已选择项
        PanMinecraft.Visibility = Visibility.Visible
        PanSelect.IsHitTestVisible = False
        PanMinecraft.IsHitTestVisible = True
        PanBack.IsHitTestVisible = False
        PanBack.ScrollToHome()

        AniStart({
            AaOpacity(PanSelect, -PanSelect.Opacity, 90, 10),
            AaTranslateX(PanSelect, 50 - CType(PanSelect.RenderTransform, TranslateTransform).X, 100, 10),
            AaCode(Sub() PanBack.ScrollToHome(), After:=True),
            AaOpacity(PanMinecraft, 1 - PanMinecraft.Opacity, 150, 100),
            AaTranslateX(PanMinecraft, -CType(PanMinecraft.RenderTransform, TranslateTransform).X, 400, 100, Ease:=New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(Sub()
                       PanSelect.Visibility = Visibility.Collapsed
                       PanBack.IsHitTestVisible = True
                   End Sub,, True)
        }, "FrmDownloadInstall SelectPageSwitch")
    End Sub

    '页面切换触发
    Public Sub MinecraftSelected(sender As MyListItem, e As Object)
        SelectedMinecraftId = sender.Title
        SelectedMinecraftJsonUrl = sender.Tag("url").ToString
        SelectedMinecraftIcon = sender.Logo
        EnterSelectPage()
    End Sub
    Private Sub CardMinecraft_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardMinecraft.PreviewSwap
        ExitSelectPage()
        e.Handled = True
    End Sub

#End Region

#Region "选择"

    'Minecraft
    Private SelectedMinecraftId As String
    Private SelectedMinecraftJsonUrl As String
    Private SelectedMinecraftIcon As String

    'OptiFine
    Private SelectedOptiFine As DlOptiFineListEntry = Nothing
    Private Sub SetOptiFineInfoShow(IsShow As String)
        If PanOptiFineInfo.Tag = IsShow Then Exit Sub
        PanOptiFineInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanOptiFineInfo, -CType(PanOptiFineInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanOptiFineInfo, 1 - PanOptiFineInfo.Opacity, 100, 90)
            }, "SetOptiFineInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanOptiFineInfo, 6 - CType(PanOptiFineInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanOptiFineInfo, -PanOptiFineInfo.Opacity, 100)
            }, "SetOptiFineInfoShow")
        End If
    End Sub

    'LiteLoader
    Private SelectedLiteLoader As DlLiteLoaderListEntry = Nothing
    Private Sub SetLiteLoaderInfoShow(IsShow As String)
        If PanLiteLoaderInfo.Tag = IsShow Then Exit Sub
        PanLiteLoaderInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanLiteLoaderInfo, -CType(PanLiteLoaderInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanLiteLoaderInfo, 1 - PanLiteLoaderInfo.Opacity, 100, 90)
            }, "SetLiteLoaderInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanLiteLoaderInfo, 6 - CType(PanLiteLoaderInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanLiteLoaderInfo, -PanLiteLoaderInfo.Opacity, 100)
            }, "SetLiteLoaderInfoShow")
        End If
    End Sub

    'Forge
    Private SelectedForge As DlForgeVersionEntry = Nothing
    Private Sub SetForgeInfoShow(IsShow As String)
        If PanForgeInfo.Tag = IsShow Then Exit Sub
        PanForgeInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanForgeInfo, -CType(PanForgeInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanForgeInfo, 1 - PanForgeInfo.Opacity, 100, 90)
            }, "SetForgeInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanForgeInfo, 6 - CType(PanForgeInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanForgeInfo, -PanForgeInfo.Opacity, 100)
            }, "SetForgeInfoShow")
        End If
    End Sub

    'Fabric
    Private SelectedFabric As String = Nothing
    Private Sub SetFabricInfoShow(IsShow As String)
        If PanFabricInfo.Tag = IsShow Then Exit Sub
        PanFabricInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanFabricInfo, -CType(PanFabricInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanFabricInfo, 1 - PanFabricInfo.Opacity, 100, 90)
            }, "SetFabricInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanFabricInfo, 6 - CType(PanFabricInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanFabricInfo, -PanFabricInfo.Opacity, 100)
            }, "SetFabricInfoShow")
        End If
    End Sub

    'FabricApi
    Private SelectedFabricApi As DlCfFile = Nothing
    Private Sub SetFabricApiInfoShow(IsShow As String)
        If PanFabricApiInfo.Tag = IsShow Then Exit Sub
        PanFabricApiInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanFabricApiInfo, -CType(PanFabricApiInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanFabricApiInfo, 1 - PanFabricApiInfo.Opacity, 100, 90)
            }, "SetFabricApiInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanFabricApiInfo, 6 - CType(PanFabricApiInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanFabricApiInfo, -PanFabricApiInfo.Opacity, 100)
            }, "SetFabricApiInfoShow")
        End If
    End Sub

    'OptiFabric
    Private SelectedOptiFabric As DlCfFile = Nothing
    Private Sub SetOptiFabricInfoShow(IsShow As String)
        If PanOptiFabricInfo.Tag = IsShow Then Exit Sub
        PanOptiFabricInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanOptiFabricInfo, -CType(PanOptiFabricInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanOptiFabricInfo, 1 - PanOptiFabricInfo.Opacity, 100, 90)
            }, "SetOptiFabricInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanOptiFabricInfo, 6 - CType(PanOptiFabricInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanOptiFabricInfo, -PanOptiFabricInfo.Opacity, 100)
            }, "SetOptiFabricInfoShow")
        End If
    End Sub

    ''' <summary>
    ''' 重载已选择的项目的显示。
    ''' </summary>
    Private Sub SelectReload() Handles CardOptiFine.Swap, LoadOptiFine.StateChanged, CardForge.Swap, LoadForge.StateChanged, CardFabric.Swap, LoadFabric.StateChanged, CardFabricApi.Swap, LoadFabricApi.StateChanged, CardOptiFabric.Swap, LoadOptiFabric.StateChanged, CardLiteLoader.Swap, LoadLiteLoader.StateChanged
        If SelectedMinecraftId Is Nothing Then Exit Sub
        '主预览
        SelectNameUpdate()
        ItemSelect.Title = TextSelectName.Text
        ItemSelect.Info = GetSelectInfo()
        ItemSelect.Logo = GetSelectLogo()
        'Minecraft
        LabMinecraft.Text = SelectedMinecraftId
        ImgMinecraft.Source = New MyBitmap(SelectedMinecraftIcon)
        'OptiFine
        Dim OptiFineError As String = LoadOptiFineGetError()
        CardOptiFine.MainSwap.Visibility = If(OptiFineError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If OptiFineError IsNot Nothing Then CardOptiFine.IsSwaped = True '例如在同时展开卡片时选择了不兼容项则强制折叠
        SetOptiFineInfoShow(CardOptiFine.IsSwaped)
        If SelectedOptiFine Is Nothing Then
            BtnOptiFineClear.Visibility = Visibility.Collapsed
            ImgOptiFine.Visibility = Visibility.Collapsed
            LabOptiFine.Text = If(OptiFineError, "点击选择")
            LabOptiFine.Foreground = ColorGray4
        Else
            BtnOptiFineClear.Visibility = Visibility.Visible
            ImgOptiFine.Visibility = Visibility.Visible
            LabOptiFine.Text = SelectedOptiFine.NameDisplay.Replace(SelectedMinecraftId & " ", "")
            LabOptiFine.Foreground = ColorGray1
        End If
        'LiteLoader
        Dim LiteLoaderError As String = LoadLiteLoaderGetError()
        CardLiteLoader.MainSwap.Visibility = If(LiteLoaderError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If LiteLoaderError IsNot Nothing Then CardLiteLoader.IsSwaped = True '例如在同时展开卡片时选择了不兼容项则强制折叠
        SetLiteLoaderInfoShow(CardLiteLoader.IsSwaped)
        If SelectedLiteLoader Is Nothing Then
            BtnLiteLoaderClear.Visibility = Visibility.Collapsed
            ImgLiteLoader.Visibility = Visibility.Collapsed
            LabLiteLoader.Text = If(LiteLoaderError, "点击选择")
            LabLiteLoader.Foreground = ColorGray4
        Else
            BtnLiteLoaderClear.Visibility = Visibility.Visible
            ImgLiteLoader.Visibility = Visibility.Visible
            LabLiteLoader.Text = SelectedLiteLoader.Inherit
            LabLiteLoader.Foreground = ColorGray1
        End If
        'Forge
        Dim ForgeError As String = LoadForgeGetError()
        CardForge.MainSwap.Visibility = If(ForgeError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If ForgeError IsNot Nothing Then CardForge.IsSwaped = True
        SetForgeInfoShow(CardForge.IsSwaped)
        If SelectedForge Is Nothing Then
            BtnForgeClear.Visibility = Visibility.Collapsed
            ImgForge.Visibility = Visibility.Collapsed
            LabForge.Text = If(ForgeError, "点击选择")
            LabForge.Foreground = ColorGray4
        Else
            BtnForgeClear.Visibility = Visibility.Visible
            ImgForge.Visibility = Visibility.Visible
            LabForge.Text = SelectedForge.Version
            LabForge.Foreground = ColorGray1
        End If
        'Fabric
        Dim FabricError As String = LoadFabricGetError()
        CardFabric.MainSwap.Visibility = If(FabricError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If FabricError IsNot Nothing Then CardFabric.IsSwaped = True
        SetFabricInfoShow(CardFabric.IsSwaped)
        If SelectedFabric Is Nothing Then
            BtnFabricClear.Visibility = Visibility.Collapsed
            ImgFabric.Visibility = Visibility.Collapsed
            LabFabric.Text = If(FabricError, "点击选择")
            LabFabric.Foreground = ColorGray4
        Else
            BtnFabricClear.Visibility = Visibility.Visible
            ImgFabric.Visibility = Visibility.Visible
            LabFabric.Text = SelectedFabric.Replace("+build", "")
            LabFabric.Foreground = ColorGray1
        End If
        'FabricApi
        Dim FabricApiError As String = LoadFabricApiGetError()
        CardFabricApi.MainSwap.Visibility = If(FabricApiError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If FabricApiError IsNot Nothing OrElse SelectedFabric Is Nothing Then CardFabricApi.IsSwaped = True
        SetFabricApiInfoShow(CardFabricApi.IsSwaped)
        If SelectedFabricApi Is Nothing Then
            BtnFabricApiClear.Visibility = Visibility.Collapsed
            ImgFabricApi.Visibility = Visibility.Collapsed
            LabFabricApi.Text = If(FabricApiError, "点击选择")
            LabFabricApi.Foreground = ColorGray4
        Else
            BtnFabricApiClear.Visibility = Visibility.Visible
            ImgFabricApi.Visibility = Visibility.Visible
            LabFabricApi.Text = SelectedFabricApi.DisplayName.Split("]")(1).Replace("Fabric API ", "").Replace(" build ", ".").Split("+").First.Trim
            LabFabricApi.Foreground = ColorGray1
        End If
        'OptiFabric
        Dim OptiFabricError As String = LoadOptiFabricGetError()
        CardOptiFabric.MainSwap.Visibility = If(OptiFabricError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If OptiFabricError IsNot Nothing OrElse SelectedFabric Is Nothing Then CardOptiFabric.IsSwaped = True
        SetOptiFabricInfoShow(CardOptiFabric.IsSwaped)
        If SelectedOptiFabric Is Nothing Then
            BtnOptiFabricClear.Visibility = Visibility.Collapsed
            ImgOptiFabric.Visibility = Visibility.Collapsed
            LabOptiFabric.Text = If(OptiFabricError, "点击选择")
            LabOptiFabric.Foreground = ColorGray4
        Else
            BtnOptiFabricClear.Visibility = Visibility.Visible
            ImgOptiFabric.Visibility = Visibility.Visible
            LabOptiFabric.Text = SelectedOptiFabric.DisplayName.ToLower.Replace("optifabric-", "").Replace(".jar", "").Trim.TrimStart("v")
            LabOptiFabric.Foreground = ColorGray1
        End If
        '主警告
        HintFabricAPI.Visibility = If(SelectedFabric IsNot Nothing AndAlso SelectedFabricApi Is Nothing, Visibility.Visible, Visibility.Collapsed)
        HintOptiFabric.Visibility = If(SelectedFabric IsNot Nothing AndAlso SelectedOptiFine IsNot Nothing AndAlso SelectedOptiFabric Is Nothing, Visibility.Visible, Visibility.Collapsed)
    End Sub
    ''' <summary>
    ''' 清空已选择的项目。
    ''' </summary>
    Private Sub SelectClear()
        SelectedMinecraftId = Nothing
        SelectedMinecraftJsonUrl = Nothing
        SelectedMinecraftIcon = Nothing
        SelectedOptiFine = Nothing
        SelectedLiteLoader = Nothing
        SelectedForge = Nothing
        SelectedFabric = Nothing
        SelectedFabricApi = Nothing
        SelectedOptiFabric = Nothing
    End Sub

    '显示信息获取
    ''' <summary>
    ''' 获取默认版本名。
    ''' </summary>
    Private Function GetSelectName() As String
        Dim Name As String = SelectedMinecraftId
        If SelectedFabric IsNot Nothing Then
            Name += "-Fabric " & SelectedFabric.Replace("+build", "")
        End If
        If SelectedForge IsNot Nothing Then
            Name += "-Forge_" & SelectedForge.Version
        End If
        If SelectedLiteLoader IsNot Nothing Then
            Name += "-LiteLoader"
        End If
        If SelectedOptiFine IsNot Nothing Then
            Name += "-OptiFine_" & SelectedOptiFine.NameDisplay.Replace(SelectedMinecraftId & " ", "").Replace(" ", "_")
        End If
        Return Name
    End Function
    ''' <summary>
    ''' 获取版本描述信息。
    ''' </summary>
    Private Function GetSelectInfo() As String
        Dim Info As String = ""
        If SelectedFabric IsNot Nothing Then
            Info += ", Fabric " & SelectedFabric.Replace("+build", "")
        End If
        If SelectedForge IsNot Nothing Then
            Info += ", Forge " & SelectedForge.Version
        End If
        If SelectedLiteLoader IsNot Nothing Then
            Info += ", LiteLoader"
        End If
        If SelectedOptiFine IsNot Nothing Then
            Info += ", OptiFine " & SelectedOptiFine.NameDisplay.Replace(SelectedMinecraftId & " ", "")
        End If
        If Info = "" Then Info = ", 无附加安装"
        Return Info.TrimStart(", ".ToCharArray())
    End Function
    ''' <summary>
    ''' 获取版本图标。
    ''' </summary>
    Private Function GetSelectLogo() As String
        If SelectedFabric IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Fabric.png"
        ElseIf SelectedForge IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Anvil.png"
        ElseIf SelectedLiteLoader IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Egg.png"
        ElseIf SelectedOptiFine IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/GrassPath.png"
        Else
            Return SelectedMinecraftIcon
        End If
    End Function

    '版本名处理
    Private IsSelectNameEdited As Boolean = False
    Private IsSelectNameChanging As Boolean = False
    Private Sub SelectNameUpdate()
        If IsSelectNameEdited OrElse IsSelectNameChanging Then Exit Sub
        IsSelectNameChanging = True
        TextSelectName.Text = GetSelectName()
        IsSelectNameChanging = False
    End Sub
    Private Sub TextSelectName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextSelectName.TextChanged
        If IsSelectNameChanging Then Exit Sub
        IsSelectNameEdited = True
        SelectReload()
    End Sub
    Private Sub TextSelectName_ValidateChanged(sender As Object, e As EventArgs) Handles TextSelectName.ValidateChanged
        BtnSelectStart.IsEnabled = TextSelectName.ValidateResult = ""
    End Sub

#End Region

#Region "加载器"

    '结果数据化
    Private Sub LoadMinecraft_OnFinish()
        ExitSelectPage() '返回
        Try
            Dim Dict As New Dictionary(Of String, List(Of JObject)) From {
                {"正式版", New List(Of JObject)}, {"预览版", New List(Of JObject)}, {"远古版", New List(Of JObject)}, {"愚人节版", New List(Of JObject)}
            }
            Dim Versions As JArray = DlClientListLoader.Output.Value("versions")
            For Each Version As JObject In Versions
                '确定分类
                Dim Type As String = Version("type")
                Select Case Type
                    Case "release"
                        Type = "正式版"
                    Case "snapshot"
                        Type = "预览版"
                        'Mojang 误分类
                        If Version("id").ToString.StartsWith("1.") AndAlso
                            Not Version("id").ToString.ToLower.Contains("combat") AndAlso
                            Not Version("id").ToString.ToLower.Contains("rc") AndAlso
                            Not Version("id").ToString.ToLower.Contains("experimental") AndAlso
                            Not Version("id").ToString.ToLower.Contains("pre") Then
                            Type = "正式版"
                            Version("type") = "release"
                        End If
                        '愚人节版本
                        Select Case Version("id").ToString.ToLower
                            Case "20w14infinite", "20w14∞"
                                Type = "愚人节版"
                                Version("id") = "20w14∞"
                                Version("type") = "special"
                                Version.Add("lore", GetMcFoolName(Version("id")))
                            Case "3d shareware v1.34", "1.rv-pre1", "15w14a", "2.0", "22w13oneblockatatime"
                                Type = "愚人节版"
                                Version("type") = "special"
                                Version.Add("lore", GetMcFoolName(Version("id")))
                        End Select
                    Case "special"
                        '已被处理的愚人节版
                        Type = "愚人节版"
                    Case Else
                        Type = "远古版"
                End Select
                '加入辞典
                Dict(Type).Add(Version)
            Next
            '排序
            For i = 0 To Dict.Keys.Count - 1
                Dict(Dict.Keys(i)) = Sort(Dict.Values(i), Function(Left As JObject, Right As JObject) As Boolean
                                                              Return Left("releaseTime").Value(Of Date) > Right("releaseTime").Value(Of Date)
                                                          End Function)
            Next
            '清空当前
            PanMinecraft.Children.Clear()
            '添加最新版本
            Dim CardInfo As New MyCard With {.Title = "最新版本", .Margin = New Thickness(0, 15, 0, 15), .SwapType = 2}
            Dim TopestVersions As New List(Of JObject)
            Dim Release As JObject = Dict("正式版")(0).DeepClone()
            Release("lore") = "最新正式版，发布于 " & Release("releaseTime").ToString()
            TopestVersions.Add(Release)
            If Dict("正式版")(0)("releaseTime").Value(Of Date) < Dict("预览版")(0)("releaseTime").Value(Of Date) Then
                Dim Snapshot As JObject = Dict("预览版")(0).DeepClone()
                Snapshot("lore") = "最新预览版，发布于 " & Snapshot("releaseTime").ToString()
                TopestVersions.Add(Snapshot)
            End If
            Dim PanInfo As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = TopestVersions}
            MyCard.StackInstall(PanInfo, 7)
            CardInfo.Children.Add(PanInfo)
            PanMinecraft.Children.Insert(0, CardInfo)
            '添加其他版本
            For Each Pair As KeyValuePair(Of String, List(Of JObject)) In Dict
                If Pair.Value.Count = 0 Then Continue For
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Pair.Key & " (" & Pair.Value.Count & ")", .Margin = New Thickness(0, 0, 0, 15), .SwapType = 7}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.IsSwaped = True
                PanMinecraft.Children.Add(NewCard)
            Next
        Catch ex As Exception
            Log(ex, "可视化安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

#End Region

#Region "OptiFine 列表"

    ''' <summary>
    ''' 获取 OptiFine 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadOptiFineGetError() As String
        'If Not SelectedMinecraftId.Contains("1.") Then Return "没有可用版本"
        If LoadOptiFine.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadOptiFine.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadOptiFine.State, Object).Error.Message
        For Each Version As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
            If Version.NameDisplay.StartsWith(SelectedMinecraftId & " ") Then
                'If SelectedFabric IsNot Nothing Then Return "与 Fabric 不兼容"
                If SelectedForge IsNot Nothing AndAlso VersionSortInteger(SelectedMinecraftId, "1.13") >= 0 AndAlso VersionSortInteger("1.14.3", SelectedMinecraftId) >= 0 Then Return "与 Forge 不兼容"
                Return Nothing
            End If
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardOptiFine_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFine.PreviewSwap
        If LoadOptiFineGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 OptiFine 版本列表。
    ''' </summary>
    Private Sub OptiFine_Loaded() Handles LoadOptiFine.StateChanged
        Try
            If DlOptiFineListLoader.State <> LoadState.Finished Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of DlOptiFineListEntry)
            For Each Version As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
                If Version.NameDisplay.StartsWith(SelectedMinecraftId & " ") Then Versions.Add(Version)
            Next
            If Versions.Count = 0 Then Exit Sub
            '排序
            Versions = Sort(Versions, Function(Left As DlOptiFineListEntry, Right As DlOptiFineListEntry) As Boolean
                                          Return VersionSortBoolean(Left.NameDisplay, Right.NameDisplay)
                                      End Function)
            '可视化
            PanOptiFine.Children.Clear()
            For Each Version In Versions
                PanOptiFine.Children.Add(OptiFineDownloadListItem(Version, AddressOf OptiFine_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 OptiFine 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub OptiFine_Selected(sender As MyListItem, e As EventArgs)
        SelectedOptiFine = sender.Tag
        OptiFabric_Loaded()
        CardOptiFine.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub OptiFine_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnOptiFineClear.MouseLeftButtonUp
        SelectedOptiFine = Nothing
        SelectedOptiFabric = Nothing
        CardOptiFine.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "LiteLoader 列表"

    ''' <summary>
    ''' 获取 LiteLoader 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadLiteLoaderGetError() As String
        If Not SelectedMinecraftId.Contains("1.") OrElse Val(SelectedMinecraftId.Split(".")(1)) > 12 Then Return "没有可用版本"
        If LoadLiteLoader.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadLiteLoader.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadLiteLoader.State, Object).Error.Message
        For Each Version As DlLiteLoaderListEntry In DlLiteLoaderListLoader.Output.Value
            If Version.Inherit = SelectedMinecraftId Then Return Nothing
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardLiteLoader_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardLiteLoader.PreviewSwap
        If LoadLiteLoaderGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 LiteLoader 版本列表。
    ''' </summary>
    Private Sub LiteLoader_Loaded() Handles LoadLiteLoader.StateChanged
        Try
            If DlLiteLoaderListLoader.State <> LoadState.Finished Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Version As DlLiteLoaderListEntry In DlLiteLoaderListLoader.Output.Value
                If Version.Inherit = SelectedMinecraftId Then Versions.Add(Version)
            Next
            If Versions.Count = 0 Then Exit Sub
            '可视化
            PanLiteLoader.Children.Clear()
            For Each Version In Versions
                PanLiteLoader.Children.Add(LiteLoaderDownloadListItem(Version, AddressOf LiteLoader_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 LiteLoader 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub LiteLoader_Selected(sender As MyListItem, e As EventArgs)
        SelectedLiteLoader = sender.Tag
        CardLiteLoader.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub LiteLoader_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnLiteLoaderClear.MouseLeftButtonUp
        SelectedLiteLoader = Nothing
        CardLiteLoader.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "Forge 列表"

    ''' <summary>
    ''' 获取 Forge 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadForgeGetError() As String
        If Not SelectedMinecraftId.StartsWith("1.") Then Return "没有可用版本"
        If Not LoadForge.State.IsLoader Then Return "正在获取版本列表……"
        Dim Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)) = LoadForge.State
        If SelectedMinecraftId <> Loader.Input Then Return "正在获取版本列表……"
        If Loader.State = LoadState.Loading Then Return "正在获取版本列表……"
        If Loader.State = LoadState.Failed Then
            Dim ErrorMessage As String = Loader.Error.Message
            If ErrorMessage.Contains("没有可用版本") Then
                Return "没有可用版本"
            Else
                Return "获取版本列表失败：" & ErrorMessage
            End If
        End If
        If Loader.State <> LoadState.Finished Then Return "获取版本列表失败：未知错误，状态为 " & GetStringFromEnum(Loader.State)
        For Each Version In Loader.Output
            If Version.Category = "universal" OrElse Version.Category = "client" Then Continue For '跳过无法自动安装的版本
            If SelectedFabric IsNot Nothing Then Return "与 Fabric 不兼容"
            If SelectedOptiFine IsNot Nothing AndAlso VersionSortInteger(SelectedMinecraftId, "1.13") >= 0 AndAlso VersionSortInteger("1.14.3", SelectedMinecraftId) >= 0 Then Return "与 OptiFine 不兼容"
            Return Nothing
        Next
        Return "该版本不支持自动安装"
    End Function

    '限制展开
    Private Sub CardForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardForge.PreviewSwap
        If LoadForgeGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 Forge 版本列表。
    ''' </summary>
    Private Sub Forge_Loaded() Handles LoadForge.StateChanged
        Try
            If Not LoadForge.State.IsLoader Then Exit Sub
            Dim Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)) = LoadForge.State
            If SelectedMinecraftId <> Loader.Input Then Exit Sub
            If Loader.State <> LoadState.Finished Then Exit Sub
            '可视化
            Dim Versions As New List(Of DlForgeVersionEntry)
            Versions.AddRange(Loader.Output) '复制数组，以免 Output 在实例化后变空
            If Loader.Output.Count = 0 Then Exit Sub
            PanForge.Children.Clear()
            Versions = Sort(Versions, Function(Left As DlForgeVersionEntry, Right As DlForgeVersionEntry) As Boolean
                                          Return New Version(Left.Version) > New Version(Right.Version)
                                      End Function)
            ForgeDownloadListItemPreload(PanForge, Versions, AddressOf Forge_Selected, False)
            For Each Version In Versions
                If Version.Category = "universal" OrElse Version.Category = "client" Then Continue For '跳过无法自动安装的版本
                PanForge.Children.Add(ForgeDownloadListItem(Version, AddressOf Forge_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Forge 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub Forge_Selected(sender As MyListItem, e As EventArgs)
        SelectedForge = sender.Tag
        CardForge.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub Forge_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnForgeClear.MouseLeftButtonUp
        SelectedForge = Nothing
        CardForge.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "Fabric 列表"

    ''' <summary>
    ''' 获取 Fabric 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadFabricGetError() As String
        If LoadFabric.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadFabric.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadFabric.State, Object).Error.Message
        For Each Version As JObject In DlFabricListLoader.Output.Value("game")
            If Version("version").ToString = SelectedMinecraftId Then
                If SelectedForge IsNot Nothing Then Return "与 Forge 不兼容"
                'If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
                Return Nothing
            End If
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabric.PreviewSwap
        If LoadFabricGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 Fabric 版本列表。
    ''' </summary>
    Private Sub Fabric_Loaded() Handles LoadFabric.StateChanged
        Try
            If DlFabricListLoader.State <> LoadState.Finished Then Exit Sub
            '获取版本列表
            Dim Versions As JArray = DlFabricListLoader.Output.Value("loader")
            If Versions.Count = 0 Then Exit Sub
            '可视化
            PanFabric.Children.Clear()
            For Each Version In Versions
                PanFabric.Children.Add(FabricDownloadListItem(Version, AddressOf Fabric_Selected))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Fabric 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub Fabric_Selected(sender As MyListItem, e As EventArgs)
        SelectedFabric = sender.Tag("version").ToString
        FabricApi_Loaded()
        OptiFabric_Loaded()
        CardFabric.IsSwaped = True
        SelectReload()
        If Not Setup.Get("HintInstallFabricApi") Then
            Setup.Set("HintInstallFabricApi", True)
            Hint("安装 Fabric 时通常还需要安装 Fabric API，在选择 Fabric 后就会显示其安装选项！")
        End If
    End Sub
    Private Sub Fabric_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnFabricClear.MouseLeftButtonUp
        SelectedFabric = Nothing
        SelectedFabricApi = Nothing
        SelectedOptiFabric = Nothing
        CardFabric.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "Fabric API 列表"

    ''' <summary>
    ''' 从显示名判断该 API 是否与某版本适配。
    ''' </summary>
    Public Shared Function IsSuitableFabricApi(DisplayName As String, MinecraftVersion As String) As Boolean
        Try
            If DisplayName Is Nothing OrElse MinecraftVersion Is Nothing Then Return False
            DisplayName = DisplayName.ToLower : MinecraftVersion = MinecraftVersion.ToLower
            If DisplayName.StartsWith("[" & MinecraftVersion & "]") Then Return True
            If Not DisplayName.Contains("/") OrElse Not DisplayName.Contains("]") Then Return False
            '直接的判断（例如 1.18.1/22w03a）
            For Each Part As String In DisplayName.Split("]")(0).TrimStart("[").Split("/")
                If Part = MinecraftVersion Then Return True
            Next
            '将版本名分割语素（例如 1.16.4/5）
            Dim Lefts = RegexSearch(DisplayName.Split("]")(0), "[a-z/]+|[0-9/]+")
            Dim Rights = RegexSearch(MinecraftVersion.Split("]")(0), "[a-z/]+|[0-9/]+")
            '对每段进行判断
            Dim i As Integer = 0
            While True
                '两边均缺失，感觉是一个东西
                If Lefts.Count - 1 < i AndAlso Rights.Count - 1 < i Then Return True
                '确定两边是否一致
                Dim LeftValue As String = If(Lefts.Count - 1 < i, "-1", Lefts(i))
                Dim RightValue As String = If(Rights.Count - 1 < i, "-1", Rights(i))
                If Not LeftValue.Contains("/") Then
                    If LeftValue <> RightValue Then Return False
                Else
                    '左边存在斜杠
                    If Not LeftValue.Contains(RightValue) Then Return False
                End If
                i += 1
            End While
            Return True
        Catch ex As Exception
            Log(ex, "判断 Fabric API 版本适配性出错（" & DisplayName & ", " & MinecraftVersion & "）")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 获取 FabricApi 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadFabricApiGetError() As String
        If SelectedFabric Is Nothing Then Return "需要安装 Fabric"
        If LoadFabricApi.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadFabricApi.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadFabricApi.State, Object).Error.Message
        For Each Version In DlFabricApiLoader.Output
            If IsSuitableFabricApi(Version.DisplayName, SelectedMinecraftId) Then Return Nothing
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardFabricApi_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabricApi.PreviewSwap
        If LoadFabricApiGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 FabricApi 版本列表。
    ''' </summary>
    Private Sub FabricApi_Loaded() Handles LoadFabricApi.StateChanged
        Try
            If DlFabricApiLoader.State <> LoadState.Finished Then Exit Sub
            If SelectedMinecraftId Is Nothing OrElse SelectedFabric Is Nothing Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of DlCfFile)
            For Each Version In DlFabricApiLoader.Output
                If IsSuitableFabricApi(Version.DisplayName, SelectedMinecraftId) Then
                    If Not Version.DisplayName.StartsWith("[") Then
                        Log("[Download] 已特判修改 Fabric API 显示名：" & Version.DisplayName, LogLevel.Debug)
                        Version.DisplayName = "[" & SelectedMinecraftId & "] " & Version.DisplayName
                    End If
                    Versions.Add(Version)
                End If
            Next
            If Versions.Count = 0 Then Exit Sub
            '排序
            Versions = Sort(Versions, Function(Left As DlCfFile, Right As DlCfFile) As Boolean
                                          Return Left.Date > Right.Date
                                      End Function)
            '可视化
            PanFabricApi.Children.Clear()
            For Each Version In Versions
                If Not IsSuitableFabricApi(Version.DisplayName, SelectedMinecraftId) Then Continue For
                PanFabricApi.Children.Add(FabricApiDownloadListItem(Version, AddressOf FabricApi_Selected))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Fabric API 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub FabricApi_Selected(sender As MyListItem, e As EventArgs)
        SelectedFabricApi = sender.Tag
        CardFabricApi.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub FabricApi_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnFabricApiClear.MouseLeftButtonUp
        SelectedFabricApi = Nothing
        CardFabricApi.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "OptiFabric 列表"

    ''' <summary>
    ''' 从显示名判断该 Mod 是否与某版本适配。
    ''' </summary>
    Private Function IsSuitableOptiFabric(ModFile As DlCfFile, MinecraftVersion As String) As Boolean
        Try
            If MinecraftVersion Is Nothing Then Return False
            Return ModFile.GameVersion.Contains(MinecraftVersion)
        Catch ex As Exception
            Log(ex, "判断 OptiFabric 版本适配性出错（" & MinecraftVersion & "）")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 获取 OptiFabric 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadOptiFabricGetError() As String
        If SelectedFabric Is Nothing AndAlso SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine 与 Fabric"
        If SelectedFabric Is Nothing Then Return "需要安装 Fabric"
        If SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine"
        If LoadOptiFabric.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadOptiFabric.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadOptiFabric.State, Object).Error.Message
        For Each Version In DlOptiFabricLoader.Output
            If IsSuitableOptiFabric(Version, SelectedMinecraftId) Then Return Nothing
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardOptiFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFabric.PreviewSwap
        If LoadOptiFabricGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 OptiFabric 版本列表。
    ''' </summary>
    Private Sub OptiFabric_Loaded() Handles LoadOptiFabric.StateChanged
        Try
            If DlOptiFabricLoader.State <> LoadState.Finished Then Exit Sub
            If SelectedMinecraftId Is Nothing OrElse SelectedFabric Is Nothing OrElse SelectedOptiFine Is Nothing Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of DlCfFile)
            For Each Version In DlOptiFabricLoader.Output
                If IsSuitableOptiFabric(Version, SelectedMinecraftId) Then Versions.Add(Version)
            Next
            If Versions.Count = 0 Then Exit Sub
            '排序
            Versions = Sort(Versions, Function(Left As DlCfFile, Right As DlCfFile) As Boolean
                                          Return Left.Date > Right.Date
                                      End Function)
            '可视化
            PanOptiFabric.Children.Clear()
            For Each Version In Versions
                If Not IsSuitableOptiFabric(Version, SelectedMinecraftId) Then Continue For
                PanOptiFabric.Children.Add(OptiFabricDownloadListItem(Version, AddressOf OptiFabric_Selected))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Fabric API 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub OptiFabric_Selected(sender As MyListItem, e As EventArgs)
        SelectedOptiFabric = sender.Tag
        CardOptiFabric.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub OptiFabric_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnOptiFabricClear.MouseLeftButtonUp
        SelectedOptiFabric = Nothing
        CardOptiFabric.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "安装"

    Private Sub BtnSelectStart_Click(sender As Object, e As EventArgs) Handles BtnSelectStart.Click
        '确认版本隔离
        If (SelectedForge IsNot Nothing OrElse SelectedFabric IsNot Nothing) AndAlso
           (Setup.Get("LaunchArgumentIndie") = 0 OrElse Setup.Get("LaunchArgumentIndie") = 2) Then
            If MyMsgBox("你尚未开启版本隔离，这会导致多个 MC 共用同一个 Mod 文件夹。" & vbCrLf &
                        "因此在切换 MC 版本时，MC 会因为读取到与当前版本不符的 Mod 而崩溃。" & vbCrLf &
                        "PCL2 推荐你在开始下载前，在 设置 → 版本隔离 中开启版本隔离选项！", "版本隔离提示", "取消下载", "继续") = 1 Then
                Exit Sub
            End If
        End If
        '提交安装申请
        Dim Request As New McInstallRequest With {
            .TargetVersionName = TextSelectName.Text,
            .MinecraftJson = SelectedMinecraftJsonUrl,
            .MinecraftName = SelectedMinecraftId,
            .OptiFineEntry = SelectedOptiFine,
            .ForgeEntry = SelectedForge,
            .FabricVersion = SelectedFabric,
            .FabricApi = SelectedFabricApi,
            .OptiFabric = SelectedOptiFabric,
            .LiteLoaderEntry = SelectedLiteLoader
        }
        If Not McInstall(Request) Then Exit Sub
        '返回，这样在再次进入安装页面时这个版本就会显示文件夹已重复
        ExitSelectPage()
    End Sub

#End Region

End Class
