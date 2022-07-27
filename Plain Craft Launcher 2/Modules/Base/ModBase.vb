Imports System.IO.Compression
Imports System.Management
Imports System.Security.Cryptography
Imports System.Security.Principal
Imports System.Windows.Markup
Imports Newtonsoft.Json

Public Module ModBase

#Region "声明"

    '下列版本信息由更新器自动修改
    Public Const VersionBaseName As String = "2.3.1" '不含分支前缀的显示用版本名
    Public Const VersionStandardCode As String = "2.3.1." & VersionBranchCode '标准格式的四段式版本号
#If BETA Then
    Public Const VersionCode As Integer = 257 'Release
#Else
    Public Const VersionCode As Integer = 259 'Snapshot
#End If
    '自动生成的版本信息
    Public Const VersionDisplayName As String = VersionBranchName & " " & VersionBaseName
#If RELEASE Then
    Public Const VersionBranchName As String = "Snapshot"
    Public Const VersionBranchCode As String = "0"
#ElseIf BETA Then
    Public Const VersionBranchName As String = "Release"
    Public Const VersionBranchCode As String = "50"
#ElseIf DEBUG Then
    Public Const VersionBranchName As String = "Debug"
    Public Const VersionBranchCode As String = "100"
#End If

    ''' <summary>
    ''' 主窗口句柄。
    ''' </summary>
    Public Handle As IntPtr
    ''' <summary>
    ''' 程序的启动路径，以“\”结尾。
    ''' </summary>
    Public Path As String = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
    ''' <summary>
    ''' 包含程序名的完整路径。
    ''' </summary>
    Public PathWithName As String = Path & AppDomain.CurrentDomain.SetupInformation.ApplicationName
    ''' <summary>
    ''' 程序内嵌图片文件夹路径，以“/”结尾。
    ''' </summary>
    Public PathImage As String = "pack://application:,,,/images/"
    ''' <summary>
    ''' 当前程序的语言。
    ''' </summary>
    Public Lang As String = "zh_CN"
    ''' <summary>
    ''' 设置对象。
    ''' </summary>
    Public Setup As New ModSetup
    ''' <summary>
    ''' 程序的打开计时。
    ''' </summary>
    Public ApplicationStartTick As Long
    ''' <summary>
    ''' 程序打开时的时间。
    ''' </summary>
    Public ApplicationOpenTime As Date = Date.Now
    ''' <summary>
    ''' 设备唯一标识符。
    ''' </summary>
    Public UniqueAddress As String = SecretGetUniqueAddress()
    ''' <summary>
    ''' 程序是否已结束。
    ''' </summary>
    Public IsProgramEnded As Boolean = False
    ''' <summary>
    ''' 是否为 32 位系统。
    ''' </summary>
    Public Is32BitSystem As Boolean = Not Environment.Is64BitOperatingSystem
    ''' <summary>
    ''' 操作系统版本。Win10 为 10.0。
    ''' </summary>
    Public OsVersion As Version = Environment.OSVersion.Version
    ''' <summary>
    ''' 程序的缓存文件夹路径，以“\”结尾。
    ''' </summary>
    Public PathTemp As String = If(Setup.Get("SystemSystemCache") = "", IO.Path.GetTempPath() & "PCL\", Setup.Get("SystemSystemCache")).ToString.Replace("/", "\").TrimEnd("\") & "\"

#End Region

#Region "矢量图标"

    Public Class Logo
        ''' <summary>
        ''' 图标按钮，心（空心），1.1x
        ''' </summary>
        Public Const IconButtonLikeLine As String = "M512 896a42.666667 42.666667 0 0 1-30.293333-12.373333l-331.52-331.946667a224.426667 224.426667 0 0 1 0-315.733333 223.573333 223.573333 0 0 1 315.733333 0L512 282.026667l46.08-46.08a223.573333 223.573333 0 0 1 315.733333 0 224.426667 224.426667 0 0 1 0 315.733333l-331.52 331.946667A42.666667 42.666667 0 0 1 512 896zM308.053333 256a136.533333 136.533333 0 0 0-97.28 40.106667 138.24 138.24 0 0 0 0 194.986666L512 792.746667l301.226667-301.653334a138.24 138.24 0 0 0 0-194.986666 141.653333 141.653333 0 0 0-194.56 0l-76.373334 76.8a42.666667 42.666667 0 0 1-60.586666 0L405.333333 296.106667A136.533333 136.533333 0 0 0 308.053333 256z"
        ''' <summary>
        ''' 图标按钮，心（实心），1.1x
        ''' </summary>
        Public Const IconButtonLikeFill As String = "M700.856 155.543c-74.769 0-144.295 72.696-190.046 127.26-45.737-54.576-115.247-127.26-190.056-127.26-134.79 0-244.443 105.78-244.443 235.799 0 77.57 39.278 131.988 70.845 175.713C238.908 694.053 469.62 852.094 479.39 858.757c9.41 6.414 20.424 9.629 31.401 9.629 11.006 0 21.998-3.215 31.398-9.63 9.782-6.662 240.514-164.703 332.238-291.701 31.587-43.724 70.874-98.143 70.874-175.713-0.001-130.02-109.656-235.8-244.445-235.8z m0 0"
        ''' <summary>
        ''' 图标按钮，垃圾桶，1.1x
        ''' </summary>
        Public Const IconButtonDelete As String = "M520.192 0C408.43 0 317.44 82.87 313.563 186.734H52.736c-29.038 0-52.663 21.943-52.663 49.079s23.625 49.152 52.663 49.152h58.075v550.473c0 103.35 75.118 187.757 167.717 187.757h472.43c92.599 0 167.716-83.894 167.716-187.757V285.477h52.59c29.038 0 52.59-21.943 52.663-49.08-0.073-27.135-23.625-49.151-52.663-49.151H726.235C723.237 83.017 631.955 0 520.192 0zM404.846 177.957c3.803-50.03 50.176-89.015 107.447-89.015 57.197 0 103.57 38.985 106.788 89.015H404.92zM284.379 933.669c-33.353 0-69.997-39.351-69.997-95.525v-549.01H833.39v549.522c0 56.247-36.645 95.525-69.998 95.525H284.379v-0.512z M357.23 800.695a48.274 48.274 0 0 0 47.616-49.006V471.7a48.274 48.274 0 0 0-47.543-49.08 48.274 48.274 0 0 0-47.69 49.006V751.69c0 27.282 20.846 49.006 47.617 49.006z m166.62 0a48.274 48.274 0 0 0 47.688-49.006V471.7a48.274 48.274 0 0 0-47.689-49.08 48.274 48.274 0 0 0-47.543 49.006V751.69c0 27.282 21.431 49.006 47.543 49.006z m142.92 0a48.274 48.274 0 0 0 47.543-49.006V471.7a48.274 48.274 0 0 0-47.543-49.08 48.274 48.274 0 0 0-47.616 49.006V751.69c0 27.282 20.773 49.006 47.543 49.006z"
        ''' <summary>
        ''' 图标按钮，笔，1x
        ''' </summary>
        Public Const IconButtonEdit As String = "M732.64 64.32C688.576 21.216 613.696 21.216 569.6 64.32L120.128 499.52c-17.6 12.896-26.432 30.144-30.848 51.68L32 870.048c0 25.856 8.8 56 26.432 73.248 17.632 17.216 17.632 48.704 88.64 48.704h13.248l326.08-56c22.016-4.32 39.68-12.928 52.864-30.176l449.472-435.2c22.048-21.536 35.264-47.36 35.264-77.536 0-30.176-13.216-56-35.264-77.568l-256.096-251.2zM139.712 903.776l56-326.912 311.04-295.136 267.104 269.44-310.976 295.168-323.168 57.44zM844.576 467.84l-273.984-260.672 61.856-59.84c8.832-8.512 26.528-8.512 39.776 0l234.24 226.496c4.384 4.288 8.832 12.8 8.832 17.088s-4.416 8.544-8.864 12.8l-61.856 64.128z"
        ''' <summary>
        ''' 图标按钮，齿轮，1.1x
        ''' </summary>
        Public Const IconButtonSetup As String = "M651.946667 1001.813333c-22.186667 0-42.666667-10.24-61.44-27.306666-23.893333-23.893333-49.493333-35.84-75.093334-35.84-29.013333 0-56.32 11.946667-73.386666 30.72v3.413333c-17.066667 17.066667-42.666667 27.306667-66.56 27.306667h-6.826667c-6.826667 0-11.946667-1.706667-15.36-1.706667l-6.826667-1.706667c-64.853333-20.48-121.173333-54.613333-168.96-98.986666-29.013333-23.893333-37.546667-63.146667-25.6-95.573334 8.533333-23.893333 5.12-51.2-10.24-75.093333-15.36-27.306667-34.133333-40.96-59.733333-47.786667h-1.706667l-5.12-1.706666c-35.84-8.533333-61.44-34.133333-66.56-69.973334C1.706667 575.146667 0 537.6 0 512c0-32.426667 3.413333-63.146667 8.533333-93.866667v-6.826666l3.413334-8.533334c10.24-23.893333 23.893333-40.96 44.373333-51.2 5.12-3.413333 11.946667-6.826667 20.48-8.533333 27.306667-8.533333 51.2-25.6 63.146667-44.373333 13.653333-23.893333 17.066667-52.906667 10.24-81.92-11.946667-34.133333 0-71.68 30.72-93.866667 44.373333-37.546667 97.28-68.266667 158.72-93.866667l3.413333-1.706666c44.373333-13.653333 75.093333 3.413333 92.16 20.48 23.893333 23.893333 49.493333 35.84 75.093333 35.84 30.72 0 56.32-10.24 71.68-30.72l3.413334-3.413334c27.306667-27.306667 63.146667-35.84 93.866666-22.186666 63.146667 22.186667 117.76 54.613333 165.546667 97.28 29.013333 23.893333 37.546667 63.146667 25.6 95.573333-8.533333 23.893333-5.12 51.2 10.24 75.093333 15.36 27.306667 34.133333 40.96 59.733333 47.786667h1.706667l5.12 1.706667c35.84 8.533333 61.44 34.133333 66.56 71.68 6.826667 30.72 10.24 63.146667 11.946667 93.866666v3.413334c0 32.426667-3.413333 63.146667-8.533334 93.866666v6.826667l-3.413333 8.533333c-10.24 23.893333-23.893333 40.96-44.373333 51.2-5.12 3.413333-11.946667 6.826667-20.48 8.533334-27.306667 8.533333-51.2 25.6-63.146667 46.08-13.653333 23.893333-17.066667 52.906667-10.24 81.92 11.946667 35.84-1.706667 75.093333-30.72 95.573333-44.373333 35.84-95.573333 66.56-157.013333 92.16-15.36 3.413333-27.306667 3.413333-35.84 3.413333z m3.413333-83.626666z m1.706667 0zM517.12 853.333333c47.786667 0 93.866667 20.48 134.826667 59.733334 1.706667 1.706667 3.413333 1.706667 3.413333 3.413333 52.906667-22.186667 97.28-49.493333 136.533333-80.213333l1.706667-1.706667v-3.413333c-13.653333-52.906667-8.533333-104.106667 17.066667-148.48 23.893333-39.253333 64.853333-69.973333 114.346666-85.333334 1.706667 0 3.413333-1.706667 6.826667-6.826666 5.12-25.6 8.533333-51.2 8.533333-78.506667-1.706667-29.013333-3.413333-56.32-10.24-81.92v-5.12h-1.706666c-51.2-11.946667-90.453333-39.253333-119.466667-87.04-27.306667-44.373333-34.133333-100.693333-17.066667-148.48l-1.706666-1.706667h-3.413334c-39.253333-35.84-85.333333-63.146667-136.533333-80.213333H648.533333s-1.706667 1.706667-3.413333 1.706667c-32.426667 39.253333-80.213333 59.733333-136.533333 59.733333-47.786667 0-93.866667-20.48-134.826667-59.733333l-1.706667-1.706667h-1.706666c-54.613333 22.186667-98.986667 49.493333-136.533334 80.213333l-1.706666 1.706667v3.413333c13.653333 52.906667 8.533333 104.106667-17.066667 148.48-23.893333 39.253333-64.853333 69.973333-114.346667 85.333334-1.706667 0-3.413333 1.706667-6.826666 6.826666-6.826667 25.6-8.533333 51.2-8.533334 78.506667 0 30.72 3.413333 58.026667 6.826667 76.8l1.706667 5.12h1.706666c51.2 11.946667 90.453333 39.253333 119.466667 87.04 27.306667 44.373333 34.133333 100.693333 17.066667 148.48l1.706666 1.706667 1.706667 1.706666c37.546667 35.84 83.626667 63.146667 134.826667 80.213334 1.706667 0 3.413333 0 3.413333 1.706666h1.706667s1.706667 0 5.12-1.706666c34.133333-37.546667 81.92-59.733333 136.533333-59.733334z m-6.826667-146.773333c-110.933333 0-199.68-85.333333-199.68-196.266667 0-109.226667 87.04-196.266667 199.68-196.266666s199.68 85.333333 199.68 196.266666c-1.706667 109.226667-88.746667 196.266667-199.68 196.266667z m0-307.2c-63.146667 0-114.346667 49.493333-114.346666 110.933333 0 63.146667 49.493333 110.933333 114.346666 110.933334 30.72 0 59.733333-11.946667 80.213334-32.426667 20.48-20.48 32.426667-49.493333 32.426666-78.506667 0-63.146667-49.493333-110.933333-112.64-110.933333z"
        ''' <summary>
        ''' 图标按钮，重置，0.9x
        ''' </summary>
        Public Const IconButtonReset As String = "M667.6817627 313.65283203l-45.28564454 55.76660156L858.06933594 391.27124023 787.61950684 165.93066406l-56.01379395 69.01611328A354.47387695 354.47387695 0 0 0 520.89892578 165.93066406C324.87536621 165.93066406 165.93066406 324.43041992 165.93066406 519.91015625c0 195.52917481 158.94470215 353.97949219 354.96826172 353.97949219a355.06713867 355.06713867 0 0 0 331.73217774-227.66418458 50.52612305 50.52612305 0 0 0-29.21813966-65.25878905 50.77331543 50.77331543 0 0 0-65.50598144 29.16870117A253.61938477 253.61938477 0 0 1 520.94836426 772.78796387c-140.05920411 0-253.61938477-113.21411133-253.61938477-252.87780762 0-139.61425781 113.56018067-252.82836914 253.61938477-252.82836914 53.59130859 0 104.46350098 16.61132813 146.73339843 46.57104492"
        ''' <summary>
        ''' 图标按钮，刷新，0.85x
        ''' </summary>
        Public Const IconButtonRefresh As String = "M875.52 148.48C783.36 56.32 655.36 0 512 0 291.84 0 107.52 138.24 30.72 332.8l122.88 46.08C204.8 230.4 348.16 128 512 128c107.52 0 199.68 40.96 271.36 112.64L640 384h384V0L875.52 148.48zM512 896c-107.52 0-199.68-40.96-271.36-112.64L384 640H0v384l148.48-148.48C240.64 967.68 368.64 1024 512 1024c220.16 0 404.48-138.24 481.28-332.8L870.4 645.12C819.2 793.6 675.84 896 512 896z"
        ''' <summary>
        ''' 图标按钮，软盘，1x
        ''' </summary>
        Public Const IconButtonSave As String = "M819.392 0L1024 202.752v652.16a168.96 168.96 0 0 1-168.832 168.768h-104.192a47.296 47.296 0 0 1-10.752 0H283.776a47.232 47.232 0 0 1-10.752 0H168.832A168.96 168.96 0 0 1 0 854.912V168.768A168.96 168.96 0 0 1 168.832 0h650.56z m110.208 854.912V242.112l-149.12-147.776H168.896c-41.088 0-74.432 33.408-74.432 74.432v686.144c0 41.024 33.344 74.432 74.432 74.432h62.4v-190.528c0-33.408 27.136-60.544 60.544-60.544h440.448c33.408 0 60.544 27.136 60.544 60.544v190.528h62.4c41.088 0 74.432-33.408 74.432-74.432z m-604.032 74.432h372.864v-156.736H325.568v156.736z m403.52-596.48a47.168 47.168 0 1 1 0 94.336H287.872a47.168 47.168 0 1 1 0-94.336h441.216z m0-153.728a47.168 47.168 0 1 1 0 94.4H287.872a47.168 47.168 0 1 1 0-94.4h441.216z"
        ''' <summary>
        ''' 图标按钮，信息，1.05x
        ''' </summary>
        Public Const IconButtonInfo As String = "M512 917.333333c223.861333 0 405.333333-181.472 405.333333-405.333333S735.861333 106.666667 512 106.666667 106.666667 288.138667 106.666667 512s181.472 405.333333 405.333333 405.333333z m0 106.666667C229.226667 1024 0 794.773333 0 512S229.226667 0 512 0s512 229.226667 512 512-229.226667 512-512 512z m-32-597.333333h64a21.333333 21.333333 0 0 1 21.333333 21.333333v320a21.333333 21.333333 0 0 1-21.333333 21.333333h-64a21.333333 21.333333 0 0 1-21.333333-21.333333V448a21.333333 21.333333 0 0 1 21.333333-21.333333z m0-192h64a21.333333 21.333333 0 0 1 21.333333 21.333333v64a21.333333 21.333333 0 0 1-21.333333 21.333333h-64a21.333333 21.333333 0 0 1-21.333333-21.333333v-64a21.333333 21.333333 0 0 1 21.333333-21.333333z"
        ''' <summary>
        ''' 图标按钮，列表，1x
        ''' </summary>
        Public Const IconButtonList As String = "M384 128h640v128H384zM160 192m-96 0a96 96 0 1 0 192 0 96 96 0 1 0-192 0ZM384 448h640v128H384zM160 512m-96 0a96 96 0 1 0 192 0 96 96 0 1 0-192 0ZM384 768h640v128H384zM160 832m-96 0a96 96 0 1 0 192 0 96 96 0 1 0-192 0Z"
        ''' <summary>
        ''' 图标按钮，文件夹，1.15x
        ''' </summary>
        Public Const IconButtonOpen As String = "M889.018182 418.909091H884.363636V316.509091a93.090909 93.090909 0 0 0-99.607272-89.832727h-302.545455l-93.090909-76.334546A46.545455 46.545455 0 0 0 358.865455 139.636364H146.152727A93.090909 93.090909 0 0 0 46.545455 229.469091V837.818182a46.545455 46.545455 0 0 0 46.545454 46.545454 46.545455 46.545455 0 0 0 16.756364-3.258181 109.381818 109.381818 0 0 0 25.134545 3.258181h586.472727a85.178182 85.178182 0 0 0 87.04-63.301818l163.374546-302.545454a46.545455 46.545455 0 0 0 5.585454-21.876364A82.385455 82.385455 0 0 0 889.018182 418.909091z m-744.727273-186.181818h198.283636l93.09091 76.334545a46.545455 46.545455 0 0 0 29.323636 10.705455h319.301818a12.101818 12.101818 0 0 1 6.516364 0V418.909091H302.545455a85.178182 85.178182 0 0 0-87.04 63.301818L139.636364 622.778182V232.727273a19.549091 19.549091 0 0 1 6.516363 0z m578.094546 552.029091a27.461818 27.461818 0 0 0-2.792728 6.516363H154.530909l147.083636-272.290909a27.461818 27.461818 0 0 0 2.792728-6.981818h565.061818z"
        ''' <summary>
        ''' 图标按钮，名片，1.1x
        ''' </summary>
        Public Const IconButtonCard As String = "M834.5 684.1c-31.2-70.4-98.9-120.9-179.1-127.3 63.5-8.5 112.6-63 112.6-128.8 0-71.8-58.2-130-130-130s-130 58.2-130 130c0 65.9 49 120.3 112.6 128.8-80.2 6.4-148 57-179.1 127.3-8.7 19.7 6 42 27.6 42 12.1 0 22.7-7.5 27.7-18.5 24.3-53.9 78.5-91.5 141.3-91.5s117 37.6 141.3 91.5c5 11.1 15.6 18.5 27.7 18.5 21.4 0 36.1-22.3 27.4-42zM567.9 427.9c0-38.6 31.4-70 70-70s70 31.4 70 70-31.4 70-70 70-70-31.4-70-70zM460.3 347.9H216.9c-16.6 0-30 13.4-30 30s13.4 30 30 30h243.3c16.6 0 30-13.4 30-30 0.1-16.5-13.4-30-29.9-30zM367.4 459.6H216.9c-16.6 0-30 13.4-30 30s13.4 30 30 30h150.4c16.6 0 30-13.4 30-30 0.1-16.6-13.4-30-29.9-30zM297.4 571.2H217c-16.6 0-30 13.4-30 30s13.4 30 30 30h80.4c16.6 0 30-13.4 30-30 0-16.5-13.5-30-30-30zM900 236v552H124V236h776m0-60H124c-33.1 0-60 26.9-60 60v552c0 33.1 26.9 60 60 60h776c33.1 0 60-26.9 60-60V236c0-33.1-26.9-60-60-60z"
        ''' <summary>
        ''' 图标按钮，×，0.85x
        ''' </summary>
        Public Const IconButtonCross As String = "F1 M 26.9166,22.1667L 37.9999,33.25L 49.0832,22.1668L 53.8332,26.9168L 42.7499,38L 53.8332,49.0834L 49.0833,53.8334L 37.9999,42.75L 26.9166,53.8334L 22.1666,49.0833L 33.25,38L 22.1667,26.9167L 26.9166,22.1667 Z"
        ''' <summary>
        ''' 图标按钮，Mojang，1.1x
        ''' </summary>
        Public Const IconButtonMojang As String = "M9.183,18.967c-0.109-2.239,1.336-5.119,3.92-4.96c3.657-0.027,7.319-0.044,10.977,0.005	c3.712,0.214,6.596,2.759,9.652,4.533c0.181-1.697-0.197-3.717,1.237-4.982c1.899,2.091,3.143,4.894,5.677,6.334	c1.577,0.805,2.973-0.668,4.221-1.44c1.55,2.305,2.108,5.075,2.622,7.752c0.657,3.438,0.947,6.925,1.111,10.413	c-1.734-0.733-3.355-1.708-4.971-2.671c-1.396-4.933-5.349-8.656-9.723-11.059c-3.46-1.817-7.752-2.185-11.338-0.52	c-3.761,1.593-6.285,5.666-5.891,9.75c0.121,4.577,3.17,8.765,7.391,10.456c7.746,3.285,16.407,2.234,24.521,1.243	c0.454,2.315-1.511,4.527-3.695,4.889c-10.577,0.038-21.148-0.011-31.725,0.022c-2.217,0.279-4.079-1.879-3.964-4.008	C9.167,36.141,9.216,27.556,9.183,18.967z M40.114,8.09c0.294-0.801,0.872-1.417,1.542-1.924c1.379,2.589,2.742,5.482,2.311,8.491	c-0.332,2.54-4.42,2.507-5.052,0.163C38.349,12.527,39.209,10.178,40.114,8.09z M0,53 l0.1,0 z"
        ''' <summary>
        ''' 图标按钮，用户，0.95x
        ''' </summary>
        Public Const IconButtonUser As String = "M660.338 528.065c63.61-46.825 105.131-121.964 105.131-206.83 0-141.7-115.29-256.987-256.997-256.987-141.706 0-256.998 115.288-256.998 256.987 0 85.901 42.52 161.887 107.456 208.562-152.1 59.92-260.185 207.961-260.185 381.077 0 21.276 17.253 38.53 38.53 38.53 21.278 0 38.53-17.254 38.53-38.53 0-183.426 149.232-332.671 332.667-332.671 1.589 0 3.113-0.207 4.694-0.244 0.8 0.056 1.553 0.244 2.362 0.244 183.434 0 332.664 149.245 332.664 332.671 0 21.276 17.255 38.53 38.533 38.53 21.277 0 38.53-17.254 38.53-38.53 0-174.885-110.354-324.13-264.917-382.809z m-331.803-206.83c0-99.22 80.72-179.927 179.935-179.927s179.937 80.708 179.937 179.927c0 99.203-80.721 179.91-179.937 179.91s-179.935-80.708-179.935-179.91z"
        ''' <summary>
        ''' 图标，音符，1x
        ''' </summary>
        Public Const IconMusic As String = "M348.293565 716.53287V254.797913c0-41.672348 28.004174-78.358261 68.919652-90.37913L815.994435 40.826435c62.775652-18.610087 125.907478 26.579478 125.907478 89.933913v539.158261c8.013913 42.25113-8.94887 89.177043-47.014956 127.109565a232.848696 232.848696 0 0 1-170.785392 65.758609c-61.885217-2.938435-111.081739-33.435826-129.113043-80.050087-18.031304-46.614261-2.137043-102.177391 41.672348-145.853218a232.848696 232.848696 0 0 1 170.785391-65.80313c21.014261 1.024 40.514783 5.164522 57.878261 12.065391V233.338435c0-12.109913-10.551652-20.034783-20.569044-20.034783a24.620522 24.620522 0 0 0-5.787826 0.934957L439.785739 338.18713a19.545043 19.545043 0 0 0-14.825739 19.144348v438.984348H423.846957c11.53113 43.987478-5.164522 94.208-45.412174 134.322087a232.848696 232.848696 0 0 1-170.785392 65.758609c-61.885217-2.938435-111.081739-33.435826-129.113043-80.050087-18.031304-46.614261-2.137043-102.177391 41.672348-145.853218a232.848696 232.848696 0 0 1 170.785391-65.80313c20.791652 1.024 40.069565 5.075478 57.299478 11.842783z"
        ''' <summary>
        ''' 图标，播放，0.8x
        ''' </summary>
        Public Const IconPlay As String = "M803.904 463.936a55.168 55.168 0 0 1 0 96.128l-463.616 264.448C302.848 845.888 256 819.136 256 776.448V247.616c0-42.752 46.848-69.44 84.288-48.064l463.616 264.384z"
    End Class

#End Region

#Region "自定义类"

    ''' <summary>
    ''' 支持小数与常见类型隐式转换的颜色。
    ''' </summary>
    Public Class MyColor

        Public A As Double = 255
        Public R As Double = 0
        Public G As Double = 0
        Public B As Double = 0

        '类型转换
        Public Shared Widening Operator CType(str As String) As MyColor
            Return New MyColor(str)
        End Operator
        Public Shared Widening Operator CType(col As Color) As MyColor
            Return New MyColor(col)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Color
            Return Color.FromArgb(MathByte(conv.A), MathByte(conv.R), MathByte(conv.G), MathByte(conv.B))
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As System.Drawing.Color
            Return System.Drawing.Color.FromArgb(MathByte(conv.A), MathByte(conv.R), MathByte(conv.G), MathByte(conv.B))
        End Operator
        Public Shared Widening Operator CType(bru As SolidColorBrush) As MyColor
            Return New MyColor(bru.Color)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As SolidColorBrush
            Return New SolidColorBrush(Color.FromArgb(MathByte(conv.A), MathByte(conv.R), MathByte(conv.G), MathByte(conv.B)))
        End Operator
        Public Shared Widening Operator CType(bru As Brush) As MyColor
            Return New MyColor(bru)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Brush
            Return New SolidColorBrush(Color.FromArgb(MathByte(conv.A), MathByte(conv.R), MathByte(conv.G), MathByte(conv.B)))
        End Operator

        '颜色运算
        Public Shared Operator +(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A + b.A, .B = a.B + b.B, .G = a.G + b.G, .R = a.R + b.R}
        End Operator
        Public Shared Operator -(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A - b.A, .B = a.B - b.B, .G = a.G - b.G, .R = a.R - b.R}
        End Operator
        Public Shared Operator *(a As MyColor, b As Double) As MyColor
            Return New MyColor With {.A = a.A * b, .B = a.B * b, .G = a.G * b, .R = a.R * b}
        End Operator
        Public Shared Operator /(a As MyColor, b As Double) As MyColor
            Return New MyColor With {.A = a.A / b, .B = a.B / b, .G = a.G / b, .R = a.R / b}
        End Operator
        Public Shared Operator =(a As MyColor, b As MyColor) As Boolean
            If IsNothing(a) AndAlso IsNothing(b) Then Return True
            If IsNothing(a) OrElse IsNothing(b) Then Return False
            Return a.A = b.A AndAlso a.R = b.R AndAlso a.G = b.G AndAlso a.B = b.B
        End Operator
        Public Shared Operator <>(a As MyColor, b As MyColor) As Boolean
            If IsNothing(a) AndAlso IsNothing(b) Then Return False
            If IsNothing(a) OrElse IsNothing(b) Then Return True
            Return Not (a.A = b.A AndAlso a.R = b.R AndAlso a.G = b.G AndAlso a.B = b.B)
        End Operator

        '构造函数
        Public Sub New()
        End Sub
        Public Sub New(col As Color)
            Me.A = col.A
            Me.R = col.R
            Me.G = col.G
            Me.B = col.B
        End Sub
        Public Sub New(HexString As String)
            Dim StringColor As Media.Color = ColorConverter.ConvertFromString(HexString)
            A = StringColor.A
            R = StringColor.R
            G = StringColor.G
            B = StringColor.B
        End Sub
        Public Sub New(newA As Double, col As MyColor)
            Me.A = newA
            Me.R = col.R
            Me.G = col.G
            Me.B = col.B
        End Sub
        Public Sub New(newR As Double, newG As Double, newB As Double)
            Me.A = 255
            Me.R = newR
            Me.G = newG
            Me.B = newB
        End Sub
        Public Sub New(newA As Double, newR As Double, newG As Double, newB As Double)
            Me.A = newA
            Me.R = newR
            Me.G = newG
            Me.B = newB
        End Sub
        Public Sub New(brush As Brush)
            Dim Color As Color = CType(brush, SolidColorBrush).Color
            A = Color.A
            R = Color.R
            G = Color.G
            B = Color.B
        End Sub
        Public Sub New(brush As SolidColorBrush)
            Dim Color As Color = brush.Color
            A = Color.A
            R = Color.R
            G = Color.G
            B = Color.B
        End Sub
        Public Sub New(obj As Object)
            If obj Is Nothing Then
                A = 255 : R = 255 : G = 255 : B = 255
            Else
                If TypeOf obj Is SolidColorBrush Then
                    '避免反复获取 Color 对象造成性能下降
                    Dim Color As Color = CType(obj, SolidColorBrush).Color
                    A = Color.A
                    R = Color.R
                    G = Color.G
                    B = Color.B
                Else
                    A = obj.A
                    R = obj.R
                    G = obj.G
                    B = obj.B
                End If
            End If
        End Sub

        'HSL
        Public Function Hue(v1 As Double, v2 As Double, vH As Double) As Double
            If vH < 0 Then vH += 1
            If vH > 1 Then vH -= 1
            If vH < 0.16667 Then Return v1 + (v2 - v1) * 6 * vH
            If vH < 0.5 Then Return v2
            If vH < 0.66667 Then Return v1 + (v2 - v1) * (4 - vH * 6)
            Return v1
        End Function
        Public Function FromHSL(sH As Double, sS As Double, sL As Double) As MyColor
            If sS = 0 Then
                R = sL * 2.55
                G = R
                B = R
            Else
                Dim H = sH / 360
                Dim S = sS / 100
                Dim L = sL / 100
                S = If(L < 0.5, S * L + L, S * (1.0 - L) + L)
                L = 2 * L - S
                R = 255 * Hue(L, S, H + 1 / 3)
                G = 255 * Hue(L, S, H)
                B = 255 * Hue(L, S, H - 1 / 3)
            End If
            A = 255
            Return Me
        End Function
        Public Function FromHSL2(sH As Double, sS As Double, sL As Double) As MyColor
            If sS = 0 Then
                R = sL * 2.55 : G = R : B = R
            Else
                '初始化
                sH = (sH + 3600000) Mod 360
                Dim cent As Double() = {
                    +0.1, -0.06, -0.3, '0, 30, 60
                    -0.19, -0.15, -0.24, '90, 120, 150
                    -0.32, -0.09, +0.18, '180, 210, 240
                    +0.05, -0.12, -0.02, '270, 300, 330
                    +0.1, -0.06} '最后两位与前两位一致，加是变亮，减是变暗
                '计算色调对应的亮度片区
                Dim center As Double = sH / 30.0
                Dim intCenter As Integer = Math.Floor(center) '亮度片区编号
                center = 50 - (
                     (1 - center + intCenter) * cent(intCenter) + (center - intCenter) * cent(intCenter + 1)
                    ) * sS
                'center = 50 + (cent(intCenter) + (center - intCenter) * (cent(intCenter + 1) - cent(intCenter))) * sS
                sL = If(sL < center, sL / center, 1 + (sL - center) / (100 - center)) * 50
                FromHSL(sH, sS, sL)
            End If
            A = 255
            Return Me
        End Function

        Public Overrides Function ToString() As String
            Return "(" & A & "," & R & "," & G & "," & B & ")"
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me = obj
        End Function

    End Class

    ''' <summary>
    ''' 支持负数与浮点数的矩形。
    ''' </summary>
    Public Class MyRect

        '属性
        Public Property Width As Double = 0
        Public Property Height As Double = 0
        Public Property Left As Double = 0
        Public Property Top As Double = 0

        '构造函数
        Public Sub New()
        End Sub
        Public Sub New(left As Double, top As Double, width As Double, height As Double)
            Me.Left = left
            Me.Top = top
            Me.Width = width
            Me.Height = height
        End Sub

    End Class

    ''' <summary>
    ''' 模块加载状态枚举。
    ''' </summary>
    Public Enum LoadState
        Waiting
        Loading
        Finished
        Failed
        Aborted
    End Enum

    ''' <summary>
    ''' 执行返回值。
    ''' </summary>
    Public Enum Result
        ''' <summary>
        ''' 执行成功，或进程被中断。
        ''' </summary>
        Aborted = -1
        ''' <summary>
        ''' 执行成功。
        ''' </summary>
        Success = 0
        ''' <summary>
        ''' 执行失败。
        ''' </summary>
        Fail
        ''' <summary>
        ''' 执行时出现未经处理的异常。
        ''' </summary>
        Exception
        ''' <summary>
        ''' 执行超时。
        ''' </summary>
        Timeout
        ''' <summary>
        ''' 取消执行。可能是由于不满足执行的前置条件。
        ''' </summary>
        Cancel
    End Enum

    ''' <summary>
    ''' 可以使用 Equals 和等号的 List。
    ''' </summary>
    Public Class EqualableList(Of T)
        Inherits List(Of T)
        Public Overrides Function Equals(obj As Object) As Boolean
            If TryCast(obj, List(Of T)) Is Nothing Then
                '类型不同
                Return False
            Else
                '类型相同
                Dim objList As List(Of T) = obj
                If objList.Count <> Count Then Return False
                For i = 0 To objList.Count - 1
                    If Not objList(i).Equals(Me(i)) Then Return False
                Next
                Return True
            End If
        End Function
        Public Shared Operator =(left As EqualableList(Of T), right As EqualableList(Of T)) As Boolean
            Return EqualityComparer(Of EqualableList(Of T)).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As EqualableList(Of T), right As EqualableList(Of T)) As Boolean
            Return Not left = right
        End Operator
    End Class

#End Region

#Region "数学"

    ''' <summary>
    ''' 计算二阶贝塞尔曲线。
    ''' </summary>
    Public Function MathBezier(x As Double, x1 As Double, y1 As Double, x2 As Double, y2 As Double, Optional acc As Double = 0.01) As Double
        If x <= 0 OrElse Double.IsNaN(x) Then Return 0
        If x >= 1 Then Return 1
        Dim a, b
        a = x
        Do
            b = 3 * a * ((0.33333333 + x1 - x2) * a * a + (x2 - 2 * x1) * a + x1)
            a += (x - b) * 0.5
        Loop Until Math.Abs(b - x) < acc '精度
        Return 3 * a * ((0.33333333 + y1 - y2) * a * a + (y2 - 2 * y1) * a + y1)
    End Function

    ''' <summary>
    ''' 将一个数字限制为 0~255 的 Byte 值。
    ''' </summary>
    Public Function MathByte(d As Double) As Byte
        If d < 0 Then d = 0
        If d > 255 Then d = 255
        Return Math.Round(d)
    End Function

    ''' <summary>
    ''' 提供 MyColor 类型支持的 Math.Round。
    ''' </summary>
    Public Function MathRound(col As MyColor, Optional w As Integer = 0) As MyColor
        Return New MyColor With {.A = Math.Round(col.A, w), .R = Math.Round(col.R, w), .G = Math.Round(col.G, w), .B = Math.Round(col.B, w)}
    End Function

    ''' <summary>
    ''' 获取两数间的百分比。小数点精确到 6 位。
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function MathPercent(ValueA As Double, ValueB As Double, Percent As Double) As Double
        Return Math.Round(ValueA * (1 - Percent) + ValueB * Percent, 6) '解决 Double 计算错误
    End Function
    ''' <summary>
    ''' 获取两颜色间的百分比，根据 RGB 计算。小数点精确到 6 位。
    ''' </summary>
    Public Function MathPercent(ValueA As MyColor, ValueB As MyColor, Percent As Double) As MyColor
        Return MathRound(ValueA * (1 - Percent) + ValueB * Percent, 6) '解决Double计算错误
    End Function

    ''' <summary>
    ''' 将数值限定在某个范围内。
    ''' </summary>
    Public Function MathRange(value As Double, min As Double, max As Double) As Double
        Return Math.Max(min, Math.Min(max, value))
    End Function

    ''' <summary>
    ''' 符号函数。
    ''' </summary>
    Public Function MathSgn(Value As Double) As Integer
        If Value = 0 Then
            Return 0
        ElseIf Value > 0 Then
            Return 1
        Else
            Return -1
        End If
    End Function

#End Region

#Region "文件"

    '注册表
    ''' <summary>
    ''' 重命名一个注册表子键。不可用于包含子键的子键。
    ''' </summary>
    Public Sub RegRename(parentKey As Microsoft.Win32.RegistryKey, subKeyName As String, newSubKeyName As String)
        If parentKey.GetSubKeyNames().Contains(newSubKeyName) Then parentKey.DeleteSubKeyTree(newSubKeyName, False)
        Dim SourceKey As Microsoft.Win32.RegistryKey = parentKey.OpenSubKey(subKeyName)
        If IsNothing(SourceKey) Then Exit Sub '没有目标项
        Dim NewKey As Microsoft.Win32.RegistryKey = parentKey.CreateSubKey(newSubKeyName)
        If SourceKey.GetSubKeyNames().Length > 0 Then Throw New NotSupportedException("不支持对包含子键的子键进行重命名：" & SourceKey.GetSubKeyNames()(0) & "。")
        For Each valueName As String In SourceKey.GetValueNames()
            Dim objValue As Object = SourceKey.GetValue(valueName)
            Dim valKind As Microsoft.Win32.RegistryValueKind = SourceKey.GetValueKind(valueName)
            NewKey.SetValue(valueName, objValue, valKind)
        Next
        parentKey.DeleteSubKeyTree(subKeyName, False)
    End Sub
    ''' <summary>
    ''' 读取程序所属注册表。
    ''' </summary>
    Public Function ReadReg(Key As String, Optional DefaultValue As String = "") As String
        Try
            Dim parentKey As Microsoft.Win32.RegistryKey, softKey As Microsoft.Win32.RegistryKey
            parentKey = My.Computer.Registry.CurrentUser
            softKey = parentKey.OpenSubKey("Software\PCL", True)
            If softKey Is Nothing Then
                ReadReg = DefaultValue '不存在则返回默认值
            Else
                Dim readValue As New Text.StringBuilder
                readValue.AppendLine(softKey.GetValue(Key))
                Dim value = readValue.ToString.Replace(vbCrLf, "") '去除莫名的回车
                Return If(value = "", DefaultValue, value) '错误则返回默认值
            End If
        Catch ex As Exception
            Log(ex, "读取注册表出错：" & Key, LogLevel.Hint)
            Return DefaultValue
        End Try
    End Function
    ''' <summary>
    ''' 写入程序所属注册表。
    ''' </summary>
    Public Sub WriteReg(Key As String, Value As String, Optional ShowException As Boolean = False)
        Try
            Dim parentKey As Microsoft.Win32.RegistryKey, softKey As Microsoft.Win32.RegistryKey
            parentKey = My.Computer.Registry.CurrentUser
            softKey = parentKey.OpenSubKey("Software\PCL", True)
            If softKey Is Nothing Then softKey = parentKey.CreateSubKey("Software\PCL") '如果不存在就创建  
            softKey.SetValue(Key, Value)
        Catch ex As Exception
            Log(ex, "写入注册表出错：" & Key, If(ShowException, LogLevel.Hint, LogLevel.Developer))
            If ShowException Then Throw '如果显示错误就丢一个
        End Try
    End Sub

    'Ini 文件
    Private Class IniCache
        Public FileContent As String = ""
        Public KVCache As New Dictionary(Of String, String)
    End Class
    Private ReadOnly IniCacheList As New Dictionary(Of String, IniCache)
    ''' <summary>
    ''' 清除某 Ini 文件的运行时缓存。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    Public Sub IniClearCache(FileName As String)
        '还原文件路径
        If Not FileName.Contains(":\") Then FileName = Path & "PCL\" & FileName & ".ini"
        '检索缓存
        If IniCacheList.ContainsKey(FileName) Then IniCacheList.Remove(FileName)
    End Sub
    ''' <summary>
    ''' 获取 Ini 文件内容，这可能会使用到缓存。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    Private Function IniGetContent(FileName As String) As IniCache
        Try
            '还原文件路径
            If Not FileName.Contains(":\") Then FileName = Path & "PCL\" & FileName & ".ini"
            '检索缓存
            If IniCacheList.ContainsKey(FileName) Then
                '返回缓存中的信息
                Dim Cache As New IniCache
                IniCacheList.TryGetValue(FileName, Cache)
                Return Cache '防止ByRef导致缓存变更
            Else
                Dim Ini As IniCache
                If File.Exists(FileName) Then
                    '返回文件信息并且记入缓存
                    Dim Cache As String = (vbCrLf & ReadFile(FileName) & vbCrLf).Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr, vbCrLf).Replace(vbCrLf & vbCrLf, vbCrLf).Replace(vbNullChar, "")
                    Ini = New IniCache With {.FileContent = Cache}
                Else
                    '返回空文件信息
                    Ini = New IniCache With {.FileContent = ""}
                End If
                If Not IniCacheList.ContainsKey(FileName) Then IniCacheList.Add(FileName, Ini) '避免多线程异步执行此方法时造成多次添加项
                Return Ini
            End If
        Catch ex As Exception
            Log(ex, "读取文件失败：" & FileName, LogLevel.Hint)
            Return New IniCache With {.FileContent = ""}
        End Try
    End Function
    ''' <summary>
    ''' 读取Ini文件，这可能会使用到缓存。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    ''' <param name="Key">键。</param>
    ''' <param name="DefaultValue">没有找到键时返回的默认值。</param>
    Public Function ReadIni(FileName As String, Key As String, Optional DefaultValue As String = "") As String
        Try
            '获取目前文件
            Dim NowIni As IniCache = IniGetContent(FileName)
            If IsNothing(NowIni) Then Return DefaultValue
            '使用缓存
            If NowIni.KVCache.ContainsKey(Key) Then Return If(NowIni.KVCache(Key), DefaultValue)
            '没有缓存，读取文件
            If NowIni.FileContent.Contains(vbCrLf & Key & ":") Then
                Dim Ret As String = Mid(NowIni.FileContent, NowIni.FileContent.IndexOf(vbCrLf & Key & ":") + 3)
                Ret = If(Ret.Contains(vbCrLf), Mid(Ret, 1, Ret.IndexOf(vbCrLf)), Ret).Replace(Key & ":", "")
                NowIni.KVCache.Add(Key, If(Ret = vbLf, "", Ret))
                Return If(Ret = vbLf, "", Ret)
            Else
                Return DefaultValue
            End If
        Catch ex As Exception
            '读取失败
            Return DefaultValue
        End Try
    End Function
    ''' <summary>
    ''' 写入ini文件，这会更新缓存。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    ''' <param name="Key">键。</param>
    ''' <param name="Value">值。</param>
    ''' <remarks></remarks>
    Public Sub WriteIni(FileName As String, Key As String, Value As String)
        Try

            '还原文件路径
            If Not FileName.Contains(":\") Then FileName = Path & "PCL\" & FileName & ".ini"
            If IsNothing(Value) Then Value = ""
            Value = Value.Replace(vbCrLf, "")
            '创建文件夹
            If Not Directory.Exists(GetPathFromFullPath(FileName)) Then Directory.CreateDirectory(GetPathFromFullPath(FileName))
            '获取目前文件
            Dim NowFile As String = IniGetContent(FileName).FileContent
            If Not NowFile.EndsWith(vbCrLf) Then NowFile += vbCrLf
            '如果值一样就不处理
            If NowFile.Contains(vbCrLf & Key & ":" & Value & vbCrLf) Then Exit Sub
            '处理文件
            Dim FindResult As String = ReadIni(FileName, Key)
            If FindResult = "" AndAlso Not NowFile.Contains(vbCrLf & Key & ":") Then
                '不存在这个键
                NowFile = NowFile & vbCrLf & Key & ":" & Value & vbCrLf
            Else
                '存在这个键
                NowFile = NowFile.Replace(vbCrLf & Key & ":" & FindResult & vbCrLf, vbCrLf & Key & ":" & Value & vbCrLf)
            End If
            WriteFile(FileName, NowFile)
            '刷新目前缓存
            IniCacheList(FileName).FileContent = NowFile
            IniCacheList(FileName).KVCache.Remove(Key)
            IniCacheList(FileName).KVCache.Add(Key, Value)

        Catch ex As Exception
            Log(ex, "写入文件失败：" & FileName & "/" & Key & ": " & Value)
        End Try
    End Sub

    '路径处理
    ''' <summary>
    ''' 从文件路径或者 Url 获取不包含文件名的路径，或获取文件夹的父文件夹路径。不包含路径将会抛出异常。
    ''' </summary>
    Public Function GetPathFromFullPath(FilePath As String) As String
        If Not (FilePath.Contains("\") OrElse FilePath.Contains("/")) Then Throw New Exception("不包含路径：" & FilePath)
        If FilePath.EndsWith("\") OrElse FilePath.EndsWith("/") Then
            '是文件夹路径
            Dim IsRight As Boolean = FilePath.EndsWith("\")
            FilePath = Left(FilePath, Len(FilePath) - 1)
            GetPathFromFullPath = Left(FilePath, FilePath.LastIndexOfAny({"\", "/"})) & If(IsRight, "\", "/")
            If GetPathFromFullPath = "" Then Throw New Exception("不包含路径：" & FilePath)
        Else
            '是文件路径
            GetPathFromFullPath = Left(FilePath, FilePath.LastIndexOfAny({"\", "/"}) + 1)
            If GetPathFromFullPath = "" Then Throw New Exception("不包含路径：" & FilePath)
        End If
    End Function
    ''' <summary>
    ''' 从文件路径或者 Url 获取不包含路径的文件名。不包含文件名将会抛出异常。
    ''' </summary>
    Public Function GetFileNameFromPath(FilePath As String) As String
        If FilePath.EndsWith("\") OrElse FilePath.EndsWith("/") Then Throw New Exception("不包含文件名：" & FilePath)
        If Not (FilePath.Contains("\") OrElse FilePath.Contains("/")) Then Return FilePath
        If FilePath.Contains("?") Then FilePath = Left(FilePath, FilePath.LastIndexOf("?")) '去掉网络参数后的 ?
        GetFileNameFromPath = Mid(FilePath, FilePath.LastIndexOfAny({"\", "/"}) + 2)
        If GetFileNameFromPath = "" Then Throw New Exception("不包含文件名：" & FilePath)
    End Function
    ''' <summary>
    ''' 从文件路径或者 Url 获取不包含路径与扩展名的文件名。不包含文件名将会抛出异常。
    ''' </summary>
    Public Function GetFileNameWithoutExtentionFromPath(FilePath As String) As String
        Dim Name As String = GetFileNameFromPath(FilePath)
        If Name.Contains(".") Then
            Return Name.Substring(0, Name.LastIndexOf("."))
        Else
            Return Name
        End If
    End Function
    ''' <summary>
    ''' 从文件夹路径获取文件夹名。
    ''' </summary>
    Public Function GetFolderNameFromPath(FolderPath As String) As String
        If FolderPath.EndsWith(":\") OrElse FolderPath.EndsWith(":\\") Then Return FolderPath.Substring(0, 1)
        If FolderPath.EndsWith("\") OrElse FolderPath.EndsWith("/") Then FolderPath = Left(FolderPath, FolderPath.Length - 1)
        Return GetFileNameFromPath(FolderPath)
    End Function

    '读取、写入文件
    ''' <summary>
    ''' 读取文件，如果失败则返回空字符串。
    ''' </summary>
    ''' <param name="FilePath">文件完整或相对路径。</param>
    Public Function ReadFile(FilePath As String) As String
        Try
            '还原文件路径
            If Not FilePath.Contains(":\") Then FilePath = Path & FilePath
            If File.Exists(FilePath) Then
                ReadFile = DecodeBytes(File.ReadAllBytes(FilePath))
            Else
                Log("[System] 欲读取的文件不存在，已返回空字符串：" & FilePath)
                Return ""
            End If
        Catch ex As Exception
            Log(ex, "读取文件出错：" & FilePath)
            Return ""
        End Try
    End Function
    ''' <summary>
    ''' 读取流中的所有文本。
    ''' </summary>
    Public Function ReadFile(Stream As Stream, Optional Encoding As Encoding = Nothing) As String
        Try
            Dim srcBuf As Byte() = New Byte(16384) {}
            Dim DataCount As Integer = Stream.Read(srcBuf, 0, 16384)
            Dim Result As New List(Of Byte)
            While DataCount > 0
                If DataCount > 0 Then Result.AddRange(srcBuf.ToList.GetRange(0, DataCount))
                DataCount = Stream.Read(srcBuf, 0, 16384)
            End While
            Dim Bts = Result.ToArray
            Return If(Encoding, GetEncoding(Bts)).GetString(Bts)
        Catch ex As Exception
            Log(ex, "读取流出错")
            Return ""
        End Try
    End Function
    ''' <summary>
    ''' 写入文件。
    ''' </summary>
    ''' <param name="FilePath">文件完整或相对路径。</param>
    ''' <param name="Text">文件内容。</param>
    ''' <param name="Append">是否将文件内容追加到当前文件，而不是覆盖它。</param>
    Public Function WriteFile(FilePath As String, Text As String, Optional Append As Boolean = False, Optional Encoding As Encoding = Nothing) As Boolean
        Try
            '还原文件路径
            If Not FilePath.Contains(":\") Then FilePath = Path & FilePath
            '确保目录存在
            Directory.CreateDirectory(GetPathFromFullPath(FilePath))
            '写入文件
            If File.Exists(FilePath) Then
                '如果文件存在，刷新目前文件
                Using writer As New StreamWriter(FilePath, Append, If(Encoding, GetEncoding(FilePath)))
                    writer.Write(Text)
                    writer.Flush()
                    writer.Close()
                End Using
            Else
                '如果文件不存在，则新建并写入
                File.WriteAllText(FilePath, Text, If(Encoding, New UTF8Encoding(False)))
            End If
            Return True
        Catch ex As Exception
            Log(ex, "写入文件时出错：" & FilePath)
            Return False
        End Try
    End Function
    ''' <summary>
    ''' 写入文件。
    ''' </summary>
    ''' <param name="FilePath">文件完整或相对路径。</param>
    ''' <param name="Content">文件内容。</param>
    ''' <param name="Append">是否将文件内容追加到当前文件，而不是覆盖它。</param>
    Public Function WriteFile(FilePath As String, Content As Byte(), Optional Append As Boolean = False) As Boolean
        Try
            '还原文件路径
            If Not FilePath.Contains(":\") Then FilePath = Path & FilePath
            '确保目录存在
            Directory.CreateDirectory(GetPathFromFullPath(FilePath))
            '写入文件
            File.WriteAllBytes(FilePath, Content)
            'File.Exists 的写法只会写入 System.Byte[]（无法进行转码，Write 不支持写入 Byte[]）
            'If File.Exists(FilePath) Then
            '    '如果文件存在，刷新目前文件
            '    Using writer As New StreamWriter(FilePath, Append, GetEncoding(FilePath))
            '        writer.Write(Content)
            '        writer.Flush()
            '        writer.Close()
            '    End Using
            'Else
            '    '如果文件不存在，则新建并写入
            '     File.WriteAllBytes(FilePath, Content)
            'End If
            Return True
        Catch ex As Exception
            Log(ex, "写入文件时出错：" & FilePath)
            Return False
        End Try
    End Function
    ''' <summary>
    ''' 将流写入文件。
    ''' </summary>
    ''' <param name="FilePath">文件完整或相对路径。</param>
    Public Function WriteFile(FilePath As String, Stream As Stream) As Boolean
        Try
            '还原文件路径
            If Not FilePath.Contains(":\") Then FilePath = Path & FilePath
            '确保目录存在
            Directory.CreateDirectory(GetPathFromFullPath(FilePath))
            '读取流
            Using fs As New FileStream(FilePath, FileMode.Create, FileAccess.Write)
                Dim srcBuf As Byte() = New Byte(16384) {}
                Dim DataCount As Integer = Stream.Read(srcBuf, 0, 16384)
                While DataCount > 0
                    If DataCount > 0 Then fs.Write(srcBuf, 0, DataCount)
                    DataCount = Stream.Read(srcBuf, 0, 16384)
                End While
                fs.Close()
            End Using
            Return True
        Catch ex As Exception
            Log(ex, "保存流出错")
            Return False
        End Try
    End Function

    '文件编码
    ''' <summary>
    ''' 获取文件编码。
    ''' </summary>
    ''' <param name="FilePath">文件完整或相对路径。</param>
    Public Function GetEncoding(FilePath As String) As Encoding
        '还原文件路径
        If Not FilePath.Contains(":\") Then FilePath = Path & FilePath
        '获取编码
        GetEncoding = GetEncoding(File.ReadAllBytes(FilePath))
    End Function
    ''' <summary>
    ''' 获取 Bytes 的编码。
    ''' </summary>
    Public Function GetEncoding(Bytes As Byte()) As Encoding
        Dim Length As Integer = Bytes.Count
        If Length <= 2 Then Return New UTF8Encoding(False) '不带 BOM 的 UTF8
        '根据 BOM 判断编码
        If Bytes(0) >= &HEF Then
            '有 BOM 类型
            If Bytes(0) = &HEF AndAlso Bytes(1) = &HBB Then
                Return New UTF8Encoding(True) '带 BOM 的 UTF8
            ElseIf Bytes(0) = &HFE AndAlso Bytes(1) = &HFF Then
                Return Encoding.BigEndianUnicode
            ElseIf Bytes(0) = &HFF AndAlso Bytes(1) = &HFE Then
                Return Encoding.Unicode
            Else
                Return Encoding.GetEncoding("GB18030")
            End If
        End If
        '无 BOM 文件：GB18030（ANSI）或 UTF8
        Dim UTF8 = Encoding.UTF8.GetString(Bytes)
        Dim ErrorChar As Char = Encoding.UTF8.GetString({239, 191, 189}).ToCharArray()(0)
        If UTF8.Contains(ErrorChar) Then
            Return Encoding.GetEncoding("GB18030")
        Else
            Return New UTF8Encoding(False) '不带 BOM 的 UTF8
        End If
    End Function
    ''' <summary>
    ''' 解码 Bytes。
    ''' </summary>
    Public Function DecodeBytes(Bytes As Byte()) As String
        Dim Length As Integer = Bytes.Length
        If Length < 3 Then Return Encoding.UTF8.GetString(Bytes)
        '根据 BOM 判断编码
        If Bytes(0) >= &HEF Then
            '有 BOM 类型
            If Bytes(0) = &HEF AndAlso Bytes(1) = &HBB Then
                Return Encoding.UTF8.GetString(Bytes, 3, Length - 3)
            ElseIf Bytes(0) = &HFE AndAlso Bytes(1) = &HFF Then
                Return Encoding.BigEndianUnicode.GetString(Bytes, 3, Length - 3)
            ElseIf Bytes(0) = &HFF AndAlso Bytes(1) = &HFE Then
                Return Encoding.Unicode.GetString(Bytes, 3, Length - 3)
            Else
                Return Encoding.GetEncoding("GB18030").GetString(Bytes, 3, Length - 3)
            End If
        End If
        '无 BOM 文件：GB18030（ANSI）或 UTF8
        Dim UTF8 = Encoding.UTF8.GetString(Bytes)
        Dim ErrorChar As Char = Encoding.UTF8.GetString({239, 191, 189}).ToCharArray()(0)
        If UTF8.Contains(ErrorChar) Then
            Return Encoding.GetEncoding("GB18030").GetString(Bytes)
        Else
            Return UTF8
        End If
    End Function

    '对话框
    ''' <summary>
    ''' 弹出保存对话框并且要求保存位置，返回用户输入的完整路径。
    ''' </summary>
    ''' <param name="FileFilter">要求的格式。如：“常用图片文件(*.png;*.jpg)|*.png;*.jpg”。</param>
    ''' <param name="Title">弹窗的标题。</param>
    ''' <param name="FileName">默认的文件名。</param>
    Public Function SelectAs(Title As String, FileName As String, Optional FileFilter As String = Nothing, Optional DefaultDir As String = Nothing) As String
        Using fileDialog As New Forms.SaveFileDialog
            fileDialog.AddExtension = True
            fileDialog.AutoUpgradeEnabled = True
            fileDialog.Title = Title
            fileDialog.FileName = FileName
            If FileFilter IsNot Nothing Then fileDialog.Filter = FileFilter
            If DefaultDir IsNot Nothing Then fileDialog.InitialDirectory = DefaultDir
            fileDialog.ShowDialog()
            SelectAs = If(fileDialog.FileName.Contains(":\"), fileDialog.FileName, "")
            Log("[UI] 选择文件返回：" & SelectAs)
        End Using
    End Function
    ''' <summary>
    ''' 弹出选取文件对话框并且要求选取文件。
    ''' </summary>
    ''' <param name="FileFilter">要求的格式。如：“常用图片文件(*.png;*.jpg)|*.png;*.jpg”。</param>
    ''' <param name="Title">弹窗的标题。</param>
    Public Function SelectFile(FileFilter As String, Title As String) As String
        Using fileDialog As New Forms.OpenFileDialog
            fileDialog.AddExtension = True
            fileDialog.AutoUpgradeEnabled = True
            fileDialog.CheckFileExists = True
            fileDialog.Filter = FileFilter
            fileDialog.Multiselect = False
            fileDialog.Title = Title
            fileDialog.ValidateNames = True
            fileDialog.ShowDialog()
            SelectFile = fileDialog.FileName
            Log("[UI] 选择文件返回：" & SelectFile)
        End Using
    End Function
    ''' <summary>
    ''' 弹出选取文件夹对话框并且要求选取文件夹。如果没有选择就返回空字符串。
    ''' </summary>
    Public Function SelectFolder(Optional Title As String = "选择文件夹") As String
        Dim folderDialog As New Ookii.Dialogs.Wpf.VistaFolderBrowserDialog With {.ShowNewFolderButton = True, .RootFolder = Environment.SpecialFolder.Desktop, .Description = Title, .UseDescriptionForTitle = True}
        folderDialog.ShowDialog()
        SelectFolder = If(String.IsNullOrEmpty(folderDialog.SelectedPath), "", folderDialog.SelectedPath & If(folderDialog.SelectedPath.EndsWith("\"), "", "\"))
        Log("[UI] 选择文件夹返回：" & SelectFolder)
    End Function

    '文件校验
    ''' <summary>
    ''' 检查是否拥有某一文件夹的 I/O 权限。如果文件夹不存在，会返回 False。
    ''' </summary>
    Public Function CheckPermission(Path As String) As Boolean
        Try
            If Path = "" Then Return False
            If Not Path.EndsWith("\") Then Path += "\"
            If Path.EndsWith(":\System Volume Information\") OrElse Path.EndsWith(":\$RECYCLE.BIN\") Then Return False
            If Not Directory.Exists(Path) Then Return False
            Dim FileName As String = "CheckPermission" & GetUuid()
            If File.Exists(Path & FileName) Then File.Delete(Path & FileName)
            File.Create(Path & FileName).Dispose()
            File.Delete(Path & FileName)
            Return True
        Catch ex As Exception
            Log(ex, "没有对文件夹 " & Path & " 的权限，请尝试以管理员权限运行 PCL")
            Return False
        End Try
    End Function
    ''' <summary>
    ''' 检查是否拥有某一文件夹的 I/O 权限。如果出错，则抛出异常。
    ''' </summary>
    Public Sub CheckPermissionWithException(Path As String)
        If String.IsNullOrWhiteSpace(Path) Then Throw New ArgumentNullException("文件夹名不能为空！")
        If Not Path.EndsWith("\") Then Path += "\"
        If Not Directory.Exists(Path) Then Throw New DirectoryNotFoundException("文件夹不存在！")
        If File.Exists(Path & "CheckPermission") Then File.Delete(Path & "CheckPermission")
        File.Create(Path & "CheckPermission").Dispose()
        File.Delete(Path & "CheckPermission")
    End Sub
    ''' <summary>
    ''' 获取文件 MD5，若失败则返回空字符串。
    ''' </summary>
    Public Function GetAuthMD5(FilePath As String) As String
        Dim Retry As Boolean = False
Re:
        Try
            '获取 MD5
            Dim Result As New StringBuilder()
            Dim File As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim MD5 As MD5 = New MD5CryptoServiceProvider()
            Dim Retval As Byte() = MD5.ComputeHash(File)
            File.Close()
            For i = 0 To Retval.Length - 1
                Result.Append(Retval(i).ToString("x2"))
            Next
            Return Result.ToString
        Catch ex As Exception
            If Not Retry Then
                Retry = True
                Log(ex, "获取文件 MD5 可重试失败：" & FilePath, LogLevel.Normal)
                Thread.Sleep(RandomInteger(200, 500))
                GoTo Re
            Else
                Log(ex, "获取文件 MD5 失败：" & FilePath)
                Return ""
            End If
        End Try
    End Function
    ''' <summary>
    ''' 获取文件 SHA256，若失败则返回空字符串。
    ''' </summary>
    Public Function GetAuthSHA256(FilePath As String) As String
        Dim Retry As Boolean = False
Re:
        Try
            ''检测该文件是否在下载中，若在下载则放弃检测
            'If IgnoreOnDownloading AndAlso NetManage.Files.ContainsKey(FilePath) AndAlso NetManage.Files(FilePath).State <= NetState.Merge Then Return ""
            '获取 SHA256
            Dim file As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim sha1 As SHA256 = New SHA256CryptoServiceProvider()
            Dim retval As Byte() = sha1.ComputeHash(file)
            file.Close()
            Dim Result As New StringBuilder()
            For i As Integer = 0 To retval.Length - 1
                Result.Append(retval(i).ToString("x2"))
            Next
            Return Result.ToString
        Catch ex As Exception
            If Not Retry Then
                Retry = True
                Log(ex, "获取文件 SHA256 可重试失败：" & FilePath, LogLevel.Normal)
                Thread.Sleep(RandomInteger(200, 500))
                GoTo Re
            Else
                Log(ex, "获取文件 SHA256 失败：" & FilePath)
                Return ""
            End If
        End Try
    End Function
    ''' <summary>
    ''' 获取文件 SHA1，若失败则返回空字符串。
    ''' </summary>
    Public Function GetAuthSHA1(FilePath As String) As String
        Dim Retry As Boolean = False
Re:
        Try
            ''检测该文件是否在下载中，若在下载则放弃检测
            'If IgnoreOnDownloading AndAlso NetManage.Files.ContainsKey(FilePath) AndAlso NetManage.Files(FilePath).State <= NetState.Merge Then Return ""
            '获取 SHA1
            Dim file As New FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim sha1 As SHA1 = New SHA1CryptoServiceProvider()
            Dim retval As Byte() = sha1.ComputeHash(file)
            file.Close()
            Dim Result As New StringBuilder()
            For i As Integer = 0 To retval.Length - 1
                Result.Append(retval(i).ToString("x2"))
            Next
            Return Result.ToString
        Catch ex As Exception
            If Not Retry Then
                Retry = True
                Log(ex, "获取文件 SHA1 可重试失败：" & FilePath, LogLevel.Normal)
                Thread.Sleep(RandomInteger(200, 500))
                GoTo Re
            Else
                Log(ex, "获取文件 SHA1 失败：" & FilePath)
                Return ""
            End If
        End Try
    End Function
    ''' <summary>
    ''' 获取流 SHA1，若失败则返回空字符串。
    ''' </summary>
    Public Function GetAuthSHA1(Stream As Stream) As String
        Try
            Dim sha1 As SHA1 = New SHA1CryptoServiceProvider()
            Dim retval As Byte() = sha1.ComputeHash(Stream)
            Dim Result As New StringBuilder()
            For i As Integer = 0 To retval.Length - 1
                Result.Append(retval(i).ToString("x2"))
            Next
            Return Result.ToString
        Catch ex As Exception
            Log(ex, "获取流 SHA1 失败")
            Return ""
        End Try
    End Function
    ''' <summary>
    ''' 文件的校验规则。
    ''' </summary>
    Public Class FileChecker
        ''' <summary>
        ''' 文件的准确大小。
        ''' </summary>
        Public ActualSize As Long = -1
        ''' <summary>
        ''' 文件的最小大小。
        ''' </summary>
        Public MinSize As Long = -1
        ''' <summary>
        ''' 文件的 MD5、SHA1 或 SHA256。会根据输入字符串的长度自动判断种类。
        ''' </summary>
        Public Hash As String = Nothing
        ''' <summary>
        ''' 是否可以使用已经存在的文件。
        ''' </summary>
        Public CanUseExistsFile As Boolean = True
        ''' <summary>
        ''' 是否为 Json 文件。
        ''' </summary>
        Public IsJson As Boolean = False
        Public Sub New(Optional MinSize As Long = -1, Optional ActualSize As Long = -1, Optional Hash As String = Nothing, Optional CanUseExistsFile As Boolean = True, Optional IsJson As Boolean = False)
            Me.ActualSize = ActualSize
            Me.MinSize = MinSize
            Me.Hash = Hash
            Me.CanUseExistsFile = CanUseExistsFile
            Me.IsJson = IsJson
        End Sub
        ''' <summary>
        ''' 检查文件。若成功则返回 Nothing，失败则返回错误的描述文本，描述文本不以句号结尾。不会抛出错误。
        ''' </summary>
        Public Function Check(LocalPath As String) As String
            Try
                Dim Info As New FileInfo(LocalPath)
                If Not Info.Exists Then Return "文件不存在：" & LocalPath
                Dim FileSize As Long = Info.Length
                If ActualSize >= 0 AndAlso ActualSize <> FileSize Then Return "文件大小应为 " & ActualSize & " B，实际为 " & FileSize & " B"
                If MinSize >= 0 AndAlso MinSize > FileSize Then Return "文件大小应大于 " & MinSize & " B，实际为 " & FileSize & " B"
                If Not String.IsNullOrEmpty(Hash) Then
                    If Hash.Length < 35 Then 'MD5
                        If Hash <> GetAuthMD5(LocalPath) Then Return "文件 MD5 应为 " & Hash & "，实际为 " & GetAuthMD5(LocalPath)
                    ElseIf Hash.Length = 64 Then 'SHA256
                        If Hash <> GetAuthSHA256(LocalPath) Then Return "文件 SHA256 应为 " & Hash & "，实际为 " & GetAuthSHA256(LocalPath)
                    Else 'SHA1
                        If Hash <> GetAuthSHA1(LocalPath) Then Return "文件 SHA1 应为 " & Hash & "，实际为 " & GetAuthSHA1(LocalPath)
                    End If
                End If
                If IsJson Then
                    Dim Content As String = ReadFile(LocalPath)
                    If Content = "" Then Throw New Exception("读取到的文件为空")
                    Try
                        GetJson(Content)
                    Catch ex As Exception
                        Throw New Exception("不是有效的 Json 文件", ex)
                    End Try
                End If
                Return Nothing
            Catch ex As Exception
                Log(ex, "检查文件出错")
                Return GetString(ex)
            End Try
        End Function
    End Class

    ''' <summary>
    ''' 尝试根据后缀名判断文件种类并解压文件，支持 gz 与 zip，会尝试将 jar 以 zip 方式解压。
    ''' 会尝试创建，但不会清空目标文件夹。
    ''' 成功返回 True，并非压缩文件或失败返回 False。
    ''' </summary>
    Public Function ExtractFile(CompressFilePath As String, DestDirectory As String, Optional Encode As Encoding = Nothing) As Boolean
        Try
            Directory.CreateDirectory(DestDirectory)
            If CompressFilePath.EndsWith(".zip") OrElse CompressFilePath.EndsWith(".jar") Then
                '以 zip 方式解压
                ZipFile.ExtractToDirectory(CompressFilePath, DestDirectory, If(Encode, Encoding.Default))
                Return True
            ElseIf CompressFilePath.EndsWith(".gz") Then
                '以 gz 方式解压
                Dim stream As New GZipStream(New FileStream(CompressFilePath, FileMode.Open, FileAccess.ReadWrite), CompressionMode.Decompress)
                Dim decompressedFile As New FileStream(DestDirectory & GetFileNameFromPath(CompressFilePath).ToLower.Replace(".tar", "").Replace(".gz", ""), FileMode.OpenOrCreate, FileAccess.Write)
                Dim data As Integer = stream.ReadByte()
                While data <> -1
                    decompressedFile.WriteByte(data)
                    data = stream.ReadByte()
                End While
                decompressedFile.Close()
                stream.Close()
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Log(ex, "尝试解压文件失败")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 删除文件夹。失败会抛出异常。
    ''' </summary>
    Public Sub DeleteDirectory(Path As String, Optional IgnoreIssue As Boolean = False)
        If Not Directory.Exists(Path) Then Exit Sub
        Dim Temp As String()
        Temp = Directory.GetFiles(Path)
        For Each str As String In Temp
            Try
                File.Delete(str)
            Catch ex As Exception
                If IgnoreIssue Then
                    Log(ex, "删除单个文件可忽略地失败")
                Else
                    Throw
                End If
            End Try
        Next
        Temp = Directory.GetDirectories(Path)
        For Each str As String In Temp
            DeleteDirectory(str)
        Next
        Try
            Directory.Delete(Path, True)
        Catch ex As Exception
            If IgnoreIssue Then
                Log(ex, "删除单个文件夹可忽略地失败")
            Else
                Throw
            End If
        End Try
        '这个也不靠谱，目录不是空的警告
        'My.Computer.FileSystem.DeleteDirectory(Path, FileIO.DeleteDirectoryOption.DeleteAllContents)
    End Sub
    ''' <summary>
    ''' 遍历文件夹中的所有文件。
    ''' </summary>
    Public Function EnumerateFiles(Directory As String) As List(Of FileInfo)
        Dim Dir As New DirectoryInfo(Directory)
        Dim ResultList As New List(Of FileInfo)
        '添加文件
        ResultList.AddRange(Dir.EnumerateFiles)
        '添加文件夹中的文件
        For Each SubDir In Dir.EnumerateDirectories
            ResultList.AddRange(EnumerateFiles(SubDir.FullName))
        Next
        Return ResultList
    End Function

#End Region

#Region "文本"
    Public vbLQ As Char = Convert.ToChar(8220)
    Public vbRQ As Char = Convert.ToChar(8221)

    ''' <summary>
    ''' 提取 Exception 摘要。
    ''' </summary>
    ''' <param name="IsLine">输出是否强制为单行，单行模型下不会输出调用堆栈。</param>
    Public Function GetString(Ex As Exception, Optional IsLine As Boolean = True, Optional ShowAllTrace As Boolean = False) As String
        If Ex Is Nothing Then Return "无可用错误信息！"
        Dim Desc As New List(Of String)

        '自动错误判断
        Dim RealEx = Ex
        Do Until RealEx.InnerException Is Nothing
            RealEx = RealEx.InnerException
        Loop
        If TypeOf RealEx Is TypeLoadException OrElse TypeOf RealEx Is MissingMethodException OrElse TypeOf RealEx Is NotImplementedException OrElse TypeOf RealEx Is TypeInitializationException Then
            Desc.Add("系统环境存在问题，请尝试重装 .Net Framework 4.6.2 后再试")
        ElseIf TypeOf RealEx Is UnauthorizedAccessException Then
            Desc.Add("PCL2 权限不足，请尝试右键 PCL2，选择以管理员身份运行")
        ElseIf TypeOf RealEx Is OutOfMemoryException Then
            Desc.Add("电脑运行内存不足，PCL2 无法继续运行")
        End If

        If IsLine Then
            '无内部错误的快速处理
            If Ex.InnerException Is Nothing Then
                If Desc.Count = 0 Then
                    Return Ex.Message.Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr & vbCr, vbCr).Replace(vbCr, " ")
                Else
                    Return Desc.First
                End If
            End If
            '构造输出信息
            Do Until Ex Is Nothing
                Desc.Add(Ex.Message.Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr & vbCr, vbCr).Replace(vbCr, " "))
                Ex = Ex.InnerException
            Loop
            Desc.Reverse() '让最深层错误在最左边
            GetString = Join(Desc, " → ")
        Else
            Dim Stack As New List(Of String)
            '逐级追加描述与堆栈
            Do Until Ex Is Nothing
                Desc.Add(Ex.Message.Replace(vbCrLf, vbCr).Replace(vbLf, vbCr).Replace(vbCr & vbCr, vbCr).Replace(vbCr, vbCrLf))
                If Ex.StackTrace IsNot Nothing Then
                    For Each St As String In Ex.StackTrace.Split(vbCrLf)
                        If ShowAllTrace OrElse St.ToLower.Contains("pcl") Then Stack.Add(St.Replace(vbCr, String.Empty).Replace(vbLf, String.Empty))
                    Next
                End If
                Ex = Ex.InnerException
            Loop
            '构造输出信息
            Dim TypeName As String = RealEx.GetType.FullName
            GetString = Join(Desc, vbCrLf & "Caused By: ") & If(Stack.Count > 0, vbCrLf & Join(Stack, vbCrLf), "") & If(TypeName = "System.Exception", "", vbCrLf & "错误类型：" & TypeName)
        End If
    End Function
    ''' <summary>
    ''' 返回一个枚举对应的字符串。
    ''' </summary>
    ''' <param name="EnumData">一个已经实例化的枚举类型。</param>
    Public Function GetStringFromEnum(EnumData As [Enum]) As String
        Return [Enum].GetName(EnumData.GetType, EnumData)
    End Function
    ''' <summary>
    ''' 将文件大小转化为适合的文本形式，如“1.28 M”。
    ''' </summary>
    ''' <param name="FileSize">以字节为单位的大小表示。</param>
    Public Function GetString(FileSize As Long) As String
        If FileSize < 1000 Then
            'B 级
            Return FileSize & " B"
        ElseIf FileSize < 1024 * 1000 Then
            'K 级
            Dim RoundResult As String = Math.Round(FileSize / 1024)
            Return Math.Round(FileSize / 1024, CInt(MathRange(3 - RoundResult.Length, 0, 2))) & " K"
        ElseIf FileSize < 1024 * 1024 * 1000 Then
            'M 级
            Dim RoundResult As String = Math.Round(FileSize / 1024 / 1024)
            Return Math.Round(FileSize / 1024 / 1024, CInt(MathRange(3 - RoundResult.Length, 0, 2))) & " M"
        Else
            'G 级
            Dim RoundResult As String = Math.Round(FileSize / 1024 / 1024 / 1024)
            Return Math.Round(FileSize / 1024 / 1024 / 1024, CInt(MathRange(3 - RoundResult.Length, 0, 2))) & " G"
        End If
    End Function

    ''' <summary>
    ''' 获取 Json 对象。
    ''' </summary>
    Public Function GetJson(Data As String)
        Try
            Return JsonConvert.DeserializeObject(Data, New JsonSerializerSettings With {.DateTimeZoneHandling = DateTimeZoneHandling.Local})
        Catch ex As Exception
            Throw New Exception("格式化 Json 对象失败：" & If(If(Data, "").Length > 10000, Data.Substring(0, 100) & "...", Data))
        End Try
    End Function

    ''' <summary>
    ''' 将字符串统一至某个长度，过短则以 Code 将其右侧填充，过长则截取靠左的指定长度。
    ''' </summary>
    Public Function StrFill(Str As String, Code As String, Length As Byte) As String
        If Str.Length > Length Then Return Mid(Str, 1, Length)
        Return Mid(Str.PadRight(Length, Code), Str.Length + 1) & Str
    End Function
    ''' <summary>
    ''' 将一个小数显示为固定的小数点后位数形式，将向零取整。
    ''' 如 12 保留 2 位则输出 12.00，而 95.678 保留 2 位则输出 95.67。
    ''' </summary>
    Public Function StrFillNum(Num As Double, Length As Integer) As String
        Num = Math.Round(Num, Length, MidpointRounding.AwayFromZero)
        StrFillNum = Num
        If Not StrFillNum.Contains(".") Then Return (StrFillNum & ".").PadRight(StrFillNum.Length + 1 + Length, "0")
        Return StrFillNum.PadRight(StrFillNum.Split(".")(0).Length + 1 + Length, "0")
    End Function
    ''' <summary>
    ''' 移除字符串首尾的标点符号、回车，以及括号中、冒号后的补充说明内容。
    ''' </summary>
    Public Function StrTrim(Str As String, Optional RemoveQuote As Boolean = True)
        If RemoveQuote Then Str = Str.Split("（")(0).Split("：")(0).Split("(")(0).Split(":")(0)
        Return Str.Trim(".", "。", "！", " ", "!", "?", "？", vbCr, vbLf)
    End Function
    ''' <summary>
    ''' 连接字符串。
    ''' </summary>
    Public Function Join(List As ICollection, Split As String) As String
        Dim Builder As New StringBuilder
        Dim LastCount As Integer = List.Count - 1
        For i = 0 To LastCount
            If List(i) IsNot Nothing Then Builder.Append(List(i))
            If i <> LastCount Then Builder.Append(Split)
        Next
        Return Builder.ToString
    End Function

    ''' <summary>
    ''' 获取字符串哈希值。
    ''' </summary>
    Public Function GetHash(Str As String) As ULong
        GetHash = 5381
        For i = 0 To Str.Length - 1
            GetHash = (GetHash << 5) Xor GetHash Xor CType(AscW(Str(i)), ULong)
        Next
        Return GetHash Xor &HA98F501BC684032FUL
    End Function
    ''' <summary>
    ''' 获取字符串 MD5。
    ''' </summary>
    Public Function GetStringMD5(Str As String) As String
        Dim md5Hasher As New MD5CryptoServiceProvider
        Dim hashedDataBytes As Byte()
        hashedDataBytes = md5Hasher.ComputeHash(Encoding.GetEncoding("gb2312").GetBytes(Str))
        Dim tmp As New StringBuilder()
        For Each i As Byte In hashedDataBytes
            tmp.Append(i.ToString("x2"))
        Next
        Return tmp.ToString()
    End Function

    ''' <summary>
    ''' 输入 And 字符不会报错的 Val。
    ''' </summary>
    Public Function Val(Str As Object) As Double
        Return If(TypeOf Str Is String AndAlso Str = "&", 0, Conversion.Val(Str))
    End Function

    '转义
    ''' <summary>
    ''' 为字符串进行 XML 转义。
    ''' </summary>
    Public Function EscapeXML(Str As String) As String
        Return Str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("""", "&quot;")
    End Function

    '正则
    ''' <summary>
    ''' 搜索字符串中的所有正则匹配项。
    ''' </summary>
    Public Function RegexSearch(str As String, regex As String, Optional options As RegularExpressions.RegexOptions = RegularExpressions.RegexOptions.None) As List(Of String)
        Try
            RegexSearch = New List(Of String)
            Dim RegexSearchRes = New RegularExpressions.Regex(regex, options).Matches(str)
            If RegexSearchRes Is Nothing Then Return RegexSearch
            For Each item As RegularExpressions.Match In RegexSearchRes
                RegexSearch.Add(item.Value)
            Next
        Catch ex As Exception
            Log(ex, "正则匹配全部项出错")
            Return New List(Of String)
        End Try
    End Function
    ''' <summary>
    ''' 获取字符串中的第一个正则匹配项，若无匹配则返回 Nothing。
    ''' </summary>
    Public Function RegexSeek(str As String, regex As String, Optional options As RegularExpressions.RegexOptions = RegularExpressions.RegexOptions.None) As String
        Try
            Dim Result = RegularExpressions.Regex.Match(str, regex, options).Value
            Return If(Result = "", Nothing, Result)
        Catch ex As Exception
            Log(ex, "正则匹配第一项出错")
            Return Nothing
        End Try
    End Function
    ''' <summary>
    ''' 检查字符串是否匹配某正则模式。
    ''' </summary>
    Public Function RegexCheck(str As String, regex As String, Optional options As RegularExpressions.RegexOptions = RegularExpressions.RegexOptions.None) As Boolean
        Try
            Return RegularExpressions.Regex.IsMatch(str, regex, options)
        Catch ex As Exception
            Log(ex, "正则检查出错")
            Return False
        End Try
    End Function
    ''' <summary>
    ''' 进行正则替换，会抛出错误。
    ''' </summary>
    Public Function RegexReplace(Input As String, Replacement As String, Regex As String, Optional options As RegularExpressions.RegexOptions = RegularExpressions.RegexOptions.None) As String
        Return RegularExpressions.Regex.Replace(Input, Regex, Replacement, options)
    End Function

#End Region

#Region "搜索"

    ''' <summary>
    ''' 获取搜索文本的相似度。
    ''' </summary>
    ''' <param name="Source">被搜索的长内容。</param>
    ''' <param name="Query">用户输入的搜索文本。</param>
    Private Function SearchSimilarity(Source As String, Query As String) As Double
        Dim qp As Integer = 0, lenSum As Double = 0
        Source = Source.ToLower.Replace(" ", "")
        Query = Query.ToLower.Replace(" ", "")
        Dim sourceLength As Integer = Source.Length, queryLength As Integer = Query.Length '用于计算最后因数的长度缓存
        Do While qp < Query.Length
            '对 qp 作为开始位置计算
            Dim sp As Integer = 0, lenMax As Integer = 0, spMax As Integer = 0
            '查找以 qp 为头的最大子串
            Do While sp < Source.Length
                '对每个 sp 作为开始位置计算最大子串
                Dim len As Integer = 0
                Do While (qp + len) < Query.Length AndAlso (sp + len) < Source.Length AndAlso Source(sp + len) = Query(qp + len)
                    len += 1
                Loop
                '存储 len
                If len > lenMax Then
                    lenMax = len
                    spMax = sp
                End If
                '根据结果增加 sp
                sp += Math.Max(1, len)
            Loop
            If lenMax > 0 Then
                Source = Source.Substring(0, spMax) & If(Source.Count > spMax + lenMax, Source.Substring(spMax + lenMax), String.Empty) '将源中的对应字段替换空
                '存储 lenSum
                Dim IncWeight = (Math.Pow(1.4, 3 + lenMax) - 3.6) '根据长度加成
                IncWeight *= (1 + 0.3 * Math.Max(0, 3 - Math.Abs(qp - spMax))) '根据位置加成
                lenSum += IncWeight
            End If
            '根据结果增加 qp
            qp += Math.Max(1, lenMax)
        Loop
        '计算结果：重复字段量 × 源长度影响比例
        Return (lenSum / queryLength) * (3 / Math.Pow(sourceLength + 15, 0.5)) * If(queryLength <= 2, 3 - queryLength, 1)
    End Function
    ''' <summary>
    ''' 获取多段文本加权后的相似度。
    ''' </summary>
    Private Function SearchSimilarityWeighted(Source As List(Of KeyValuePair(Of String, Double)), Query As String) As Double
        Dim TotalWeight As Double = 0
        Dim Sum As Double = 0
        For Each Pair In Source
            Sum += SearchSimilarity(Pair.Key, Query) * Pair.Value
            TotalWeight += Pair.Value
        Next
        Return Sum / TotalWeight
    End Function
    ''' <summary>
    ''' 用于搜索的项目。
    ''' </summary>
    Public Class SearchEntry(Of T)
        ''' <summary>
        ''' 该项目对应的源数据。
        ''' </summary>
        Public Item As T
        ''' <summary>
        ''' 该项目用于搜索的源。
        ''' </summary>
        Public SearchSource As List(Of KeyValuePair(Of String, Double))
        ''' <summary>
        ''' 相似度。
        ''' </summary>
        Public Similarity As Double
        ''' <summary>
        ''' 是否完全匹配。
        ''' </summary>
        Public AbsoluteRight As Boolean
    End Class
    ''' <summary>
    ''' 进行多段文本加权搜索，获取相似度较高的数项结果。
    ''' </summary>
    ''' <param name="MaxBlurCount">返回的最大模糊结果数。</param>
    ''' <param name="MinBlurSimilarity">返回结果要求的最低相似度。</param>
    Public Function Search(Of T)(Entries As List(Of SearchEntry(Of T)), Query As String, Optional MaxBlurCount As Integer = 5, Optional MinBlurSimilarity As Double = 0.1) As List(Of SearchEntry(Of T))
        '初始化
        Dim ResultList As New List(Of SearchEntry(Of T))
        If Entries.Count = 0 Then Return ResultList
        '进行搜索，获取相似信息
        For Each Entry In Entries
            Entry.Similarity = SearchSimilarityWeighted(Entry.SearchSource, Query)
            Entry.AbsoluteRight = False
            For Each Pair In Entry.SearchSource
                If Pair.Key.ToLower.Replace(" ", "").Contains(Query.ToLower.Replace(" ", "")) Then Entry.AbsoluteRight = True
            Next
        Next
        '按照相似度进行排序
        Entries = Sort(Entries, Function(Left As SearchEntry(Of T), Right As SearchEntry(Of T)) As Boolean
                                    If Left.AbsoluteRight Xor Right.AbsoluteRight Then
                                        Return Left.AbsoluteRight
                                    Else
                                        Return Left.Similarity > Right.Similarity
                                    End If
                                End Function)
        '返回结果
        Dim BlurCount As Integer = 0
        For Each Entry In Entries
            If Entry.AbsoluteRight Then
                ResultList.Add(Entry) '完全匹配，直接加入
            Else
                If Entry.Similarity < MinBlurSimilarity OrElse BlurCount = MaxBlurCount Then Exit For '模糊结果边界条件
                ResultList.Add(Entry)
                BlurCount += 1 '模糊结果计数
            End If
        Next
        Return ResultList
    End Function

#End Region

#Region "系统"

    ''' <summary>
    ''' 当前程序是否拥有管理员权限。
    ''' </summary>
    Public Function IsAdmin() As Boolean
        Dim id As WindowsIdentity = WindowsIdentity.GetCurrent()
        Dim principal As New WindowsPrincipal(id)
        Return principal.IsInRole(WindowsBuiltInRole.Administrator)
    End Function
    ''' <summary>
    ''' 以管理员权限重新运行当前程序。返回是否成功。
    ''' </summary>
    Public Function RerunAsAdmin(Argument As String) As Boolean
        Try
            Process.Start(New ProcessStartInfo(PathWithName) With {.Verb = "runas", .Arguments = Argument})
            Return True
        Catch ex As Exception
            Log(ex, "以管理员权限运行程序失败")
            Return False
        End Try
    End Function

    Private Uuid As Integer = 1
    Private UuidLock As Object
    ''' <summary>
    ''' 获取一个全程序内不会重复的数字（伪 Uuid）。
    ''' </summary>
    Public Function GetUuid() As Integer
        If UuidLock Is Nothing Then UuidLock = New Object
        SyncLock UuidLock
            Uuid += 1
            Return Uuid
        End SyncLock
    End Function

    ''' <summary>
    ''' 将元素与 List 的混合体拆分为元素组。
    ''' </summary>
    Public Function GetFullList(Of T)(data As IList) As List(Of T)
        GetFullList = New List(Of T)
        For i = 0 To data.Count - 1
            If TypeOf data(i) Is ICollection Then
                GetFullList.AddRange(data(i))
            Else
                GetFullList.Add(data(i))
            End If
        Next i
    End Function
    ''' <summary>
    ''' 数组去重。
    ''' </summary>
    Public Function ArrayNoDouble(Of T)(Arr As T(), Optional IsEqual As CompareThreadStart = Nothing) As T()
        Dim ResultArray As New List(Of T)
        For i = 0 To UBound(Arr)
            For ii = i + 1 To UBound(Arr)
                If IsEqual Is Nothing Then
                    If Arr(i).Equals(Arr(ii)) Then GoTo NextElement
                Else
                    If IsEqual(Arr(i), Arr(ii)) Then GoTo NextElement
                End If
            Next
            ResultArray.Add(Arr(i))
NextElement:
        Next i
        Return ResultArray.ToArray
    End Function
    ''' <summary>
    ''' 数组去重。
    ''' </summary>
    Public Function ArrayNoDouble(Of T)(Arr As List(Of T), Optional IsEqual As CompareThreadStart = Nothing) As List(Of T)
        Dim ResultArray As New List(Of T)
        For i = 0 To Arr.Count - 1
            For ii = i + 1 To Arr.Count - 1
                If IsEqual Is Nothing Then
                    If Arr(i).Equals(Arr(ii)) Then GoTo NextElement
                Else
                    If IsEqual(Arr(i), Arr(ii)) Then GoTo NextElement
                End If
            Next
            ResultArray.Add(Arr(i))
NextElement:
        Next i
        Return ResultArray
    End Function
    ''' <summary>
    ''' 若 Key 不存在于辞典，则加入辞典。
    ''' 若 Key 已存在于辞典，则更新 Value。
    ''' </summary>
    Public Sub DictionaryAdd(Of TKey, TValue)(ByRef Dict As Dictionary(Of TKey, TValue), Key As TKey, Value As TValue)
        If Dict.ContainsKey(Key) Then
            Dict(Key) = Value
        Else
            Dict.Add(Key, Value)
        End If
    End Sub

    ''' <summary>
    ''' 获取格式类似于“11:08:52.037”的当前时间的字符串。
    ''' </summary>
    Public Function GetTimeNow() As String
        Return Date.Now.ToString("HH:mm:ss.fff")
    End Function
    ''' <summary>
    ''' 获取系统运行时间（毫秒），保证为正 Long 且大于 1，但可能突变减小。
    ''' </summary>
    Public Function GetTimeTick() As Long
        Return My.Computer.Clock.TickCount + 2147483651L
    End Function
    ''' <summary>
    ''' 将时间间隔转换为类似“5 分 10 秒前”的易于阅读的形式。
    ''' </summary>
    Public Function GetTimeSpanString(Span As TimeSpan) As String
        Dim EndFix = If(Span.TotalMilliseconds > 0, "后", "前")
        If Span.TotalMilliseconds < 0 Then Span = -Span
        Dim TotalMonthes = Math.Floor(Span.Days / 30)
        If TotalMonthes >= 61 Then
            '5+ 年，“5 年”
            GetTimeSpanString = Math.Floor(TotalMonthes / 12) & " 年"
        ElseIf TotalMonthes >= 12 Then
            '12~60 月，“1 年 2 个月”
            GetTimeSpanString = Math.Floor(TotalMonthes / 12) & " 年" & If((TotalMonthes Mod 12) > 0, " " & (TotalMonthes Mod 12) & " 个月", "")
        ElseIf TotalMonthes >= 4 Then
            '4~11 月，“5 月”
            GetTimeSpanString = TotalMonthes & " 月"
        ElseIf TotalMonthes >= 1 Then
            '1~4 月，“2 月 13 天”
            GetTimeSpanString = TotalMonthes & " 月" & If((Span.Days Mod 30) > 0, " " & (Span.Days Mod 30) & " 天", "")
        ElseIf Span.TotalDays >= 4 Then
            '4~30 天，“23 天”
            GetTimeSpanString = Span.Days & " 天"
        ElseIf Span.TotalDays >= 1 Then
            '1~3 天，“2 天 20 小时”
            GetTimeSpanString = Span.Days & " 天" & If(Span.Hours > 0, " " & Span.Hours & " 小时", "")
        ElseIf Span.TotalHours >= 10 Then
            '10 小时 ~ 1 天，“15 小时”
            GetTimeSpanString = Span.Hours & " 小时"
        ElseIf Span.TotalHours >= 1 Then
            '1~10 小时，“1 小时 20 分钟”
            GetTimeSpanString = Span.Hours & " 小时" & If(Span.Minutes > 0, " " & Span.Minutes & " 分钟", "")
        ElseIf Span.TotalMinutes >= 10 Then
            '10 分钟 ~ 1 小时，“49 分钟”
            GetTimeSpanString = Span.Minutes & " 分钟"
        ElseIf Span.TotalMinutes >= 1 Then
            '1~10 分钟，“9 分 23 秒”
            GetTimeSpanString = Span.Minutes & " 分" & If(Span.Seconds > 0, " " & Span.Seconds & " 秒", "")
        ElseIf Span.TotalSeconds >= 1 Then
            '1 秒 ~ 1 分钟，“23 秒”
            GetTimeSpanString = Span.Seconds & " 秒"
        Else
            '不到 1 秒
            GetTimeSpanString = "1 秒"
        End If
        GetTimeSpanString += EndFix
    End Function
    ''' <summary>
    ''' 获取十进制 Unix 时间戳。
    ''' </summary>
    Public Function GetUnixTimestamp() As Long
        Return (Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000
    End Function

    ''' <summary>
    ''' 用于储存 RaiseByMouse 的 EventArgs。
    ''' </summary>
    Public NotInheritable Class RouteEventArgs
        Inherits EventArgs
        Public RaiseByMouse As Boolean
        Public Handled As Boolean = False
        Public Sub New(Optional RaiseByMouse As Boolean = False)
            Me.RaiseByMouse = RaiseByMouse
        End Sub
    End Class

    ''' <summary>
    ''' 前台运行文件并返回返回值。
    ''' </summary>
    ''' <param name="FileName">文件名。可以为“notepad”等缩写。</param>
    ''' <param name="Arguments">运行参数。</param>
    ''' <param name="WaitForExit">是否等待该程序结束。默认为 False。</param>
    ''' <param name="Timeout">等待该程序结束的最长时间（毫秒）。超时会返回 Result.Timeout。</param>
    Public Function ShellAndGetExitCode(FileName As String, Optional Arguments As String = "", Optional WaitForExit As Boolean = False, Optional Timeout As Integer = 1000000) As Result
        Try
            Using Program As New Process
                Program.StartInfo.Arguments = Arguments
                Program.StartInfo.FileName = FileName
                Log("[System] 执行外部命令并等待返回码：" & FileName & " " & Arguments)
                Program.Start()
                If WaitForExit Then
                    If Program.WaitForExit(Timeout) Then
                        Return Program.ExitCode
                    Else
                        Return Result.Timeout
                    End If
                Else
                    Return Result.Success
                End If
            End Using
        Catch ex As Exception
            Log(ex, "执行命令失败：" & FileName, LogLevel.Msgbox)
            Return Result.Fail
        End Try
    End Function
    ''' <summary>
    ''' 静默运行文件并返回输出流字符串。执行失败会抛出异常。
    ''' </summary>
    ''' <param name="FileName">文件名。可以为“notepad”等缩写。</param>
    ''' <param name="Arguments">运行参数。</param>
    ''' <param name="Timeout">等待该程序结束的最长时间（毫秒）。超时会抛出错误。</param>
    Public Function ShellAndGetOutput(FileName As String, Optional Arguments As String = "", Optional Timeout As Integer = 1000000, Optional WorkingDirectory As String = Nothing) As String
        Dim Info = New ProcessStartInfo With {
            .Arguments = Arguments,
            .FileName = FileName,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardError = True,
            .RedirectStandardOutput = True,
            .WorkingDirectory = If(WorkingDirectory, Path.TrimEnd("\"c))
        }
        If WorkingDirectory IsNot Nothing Then
            If Info.EnvironmentVariables.ContainsKey("appdata") Then
                Info.EnvironmentVariables("appdata") = WorkingDirectory
            Else
                Info.EnvironmentVariables.Add("appdata", WorkingDirectory)
            End If
        End If
        Log("[System] 执行外部命令并等待返回结果：" & FileName & " " & Arguments)
        Using Program As New Process() With {.StartInfo = Info}
            Program.Start()
            Program.WaitForExit(Timeout)
            Return Program.StandardOutput.ReadToEnd & Program.StandardError.ReadToEnd
            '    Dim Result As New StringBuilder()
            '    AddHandler process.OutputDataReceived, Function(sender As Object, e As DataReceivedEventArgs)
            '                                               Try
            '                                                   If e.Data IsNot Nothing Then Result.AppendLine(e.Data)
            '                                               Finally
            '                                               End Try
            '                                               Return Nothing
            '                                           End Function
            '    AddHandler process.ErrorDataReceived, Function(sender As Object, e As DataReceivedEventArgs)
            '                                              Try
            '                                                  If e.Data IsNot Nothing Then Result.AppendLine(e.Data)
            '                                              Finally
            '                                              End Try
            '                                              Return Nothing
            '                                          End Function
            '    process.Start()
            '    process.BeginOutputReadLine()
            '    process.BeginErrorReadLine()
            '    If process.WaitForExit(Timeout) Then
            '        process.Close()
            '        Return Result.ToString
            '    Else
            '        process.Close()
            '        Throw New TimeoutException("程序执行超时，已有返回值：" & Result.ToString)
            '    End If
        End Using
    End Function

    ''' <summary>
    ''' 在新的工作线程中执行代码。
    ''' </summary>
    Public Function RunInNewThread(Action As Action, Name As String, Optional Priority As ThreadPriority = ThreadPriority.Normal) As Thread
        Dim th As New Thread(Sub()
                                 Try
                                     Action()
                                 Catch ex As ThreadInterruptedException
                                     Log(Name & "：线程已中止")
                                 Catch ex As Exception
                                     Log(ex, Name & "：线程执行失败", LogLevel.Feedback)
                                 End Try
                             End Sub) With {.Name = Name, .Priority = Priority}
        th.Start()
        Return th
    End Function
    ''' <summary>
    ''' 确保在 UI 线程中执行代码。
    ''' 如果当前并非 UI 线程，则会阻断当前线程，直至 UI 线程执行完毕。
    ''' 为防止线程互锁，请仅在开始加载动画、从 UI 获取输入时使用！
    ''' </summary>
    Public Sub RunInUiWait(Action As Action)
        If RunInUi() Then
            Action()
        Else
            Application.Current.Dispatcher.Invoke(Action)
        End If
    End Sub
    ''' <summary>
    ''' 确保在 UI 线程中执行代码，代码按触发顺序执行。
    ''' 如果当前并非 UI 线程，也不阻断当前线程的执行。
    ''' </summary>
    Public Sub RunInUi(Action As Action, Optional ForceWaitUntilLoaded As Boolean = False)
        If ForceWaitUntilLoaded Then
            Application.Current.Dispatcher.InvokeAsync(Action, Threading.DispatcherPriority.Loaded)
        ElseIf RunInUi() Then
            Action()
        Else
            Application.Current.Dispatcher.InvokeAsync(Action)
        End If
    End Sub
    ''' <summary>
    ''' 确保在工作线程中执行代码。
    ''' </summary>
    Public Sub RunInThread(Action As Action)
        If RunInUi() Then
            RunInNewThread(Action, "Runtime Invoke " & GetUuid() & "#")
        Else
            Action()
        End If
    End Sub

    ''' <summary>
    ''' 按照既定的函数进行选择排序。
    ''' </summary>
    ''' <param name="SortRule">传入两个对象，若第一个对象应该排在前面，则返回 True。</param>
    Public Function Sort(Of T)(List As IList(Of T), SortRule As CompareThreadStart) As List(Of T)
        Dim NewList As New List(Of T)
        While List.Count > 0
            Dim Highest = List(0)
            For i = 1 To List.Count - 1
                If SortRule(List(i), Highest) Then Highest = List(i)
            Next
            List.Remove(Highest)
            NewList.Add(Highest)
        End While
        Return NewList
    End Function
    Public Delegate Function CompareThreadStart(Left, Right) As Boolean

    ''' <summary>
    ''' 获取程序启动参数。
    ''' </summary>
    ''' <param name="Name">参数名。</param>
    ''' <param name="DefaultValue">默认值。</param>
    Public Function GetProgramArgument(Name As String, Optional DefaultValue As Object = "")
        Dim AllArguments() As String = Command.Split(" ")
        For i = 0 To AllArguments.Length - 1
            If AllArguments(i) = "-" & Name Then
                If AllArguments.Length = i + 1 OrElse AllArguments(i + 1).StartsWith("-") Then Return True
                Return AllArguments(i + 1)
            End If
        Next
        Return DefaultValue
    End Function

    ''' <summary>
    ''' 时间戳转化为日期。
    ''' </summary>
    Public Function GetDate(timeStamp As Integer) As Date
        Dim dtStart As Date = TimeZone.CurrentTimeZone.ToLocalTime(New Date(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        Dim lTime As Long = (CLng(timeStamp) * 10000000)
        Return dtStart.Add(New TimeSpan(lTime))
    End Function
    ''' <summary>
    ''' 将 UTC 时间转化为当前时区的时间。
    ''' </summary>
    Public Function GetLocalTime(UtcDate As Date) As Date
        Return DateTime.SpecifyKind(UtcDate, DateTimeKind.Utc).ToLocalTime
    End Function

    ''' <summary>
    ''' 打开网页。
    ''' </summary>
    Public Sub OpenWebsite(Url As String)
        Try
            Log("[System] 正在打开网页：" & Url)
            Process.Start(Url)
        Catch ex As Exception
            Log(ex, "无法打开网页（" & Url & "）")
            ClipboardSet(Url, False)
            MyMsgBox("可能由于浏览器未正确配置，PCL2 无法为你打开网页。" & vbCrLf & "网址已经复制到剪贴板，若有需要可以手动粘贴访问。", "无法打开网页")
        End Try
    End Sub
    ''' <summary>
    ''' 打开 explorer。
    ''' </summary>
    Public Sub OpenExplorer(Argument As String)
        Try
            Process.Start("explorer.exe", Argument)
        Catch ex As Exception
            Log(ex, "打开资源管理器失败，请尝试关闭安全软件（如 360 安全卫士）", LogLevel.Msgbox)
        End Try
    End Sub

    ''' <summary>
    ''' 设置剪贴板。将在另一线程运行，且不会抛出异常。
    ''' </summary>
    Public Sub ClipboardSet(Text As String, Optional ShowSuccessHint As Boolean = True)
        RunInThread(Sub()
                        Dim RetryCount As Integer = 0
Retry:
                        Try
                            RunInUi(Sub()
                                        My.Computer.Clipboard.Clear()
                                        My.Computer.Clipboard.SetText(Text)
                                    End Sub)
                        Catch ex As Exception
                            RetryCount += 1
                            If RetryCount <= 5 Then
                                Thread.Sleep(20)
                                GoTo Retry
                            Else
                                Log(ex, "可能由于剪贴板被其他程序占用，文本复制失败", LogLevel.Hint)
                            End If
                        End Try
                        If ShowSuccessHint Then Hint("已成功复制！", HintType.Finish)
                    End Sub)
    End Sub

    ''' <summary>
    ''' 以 Byte() 形式获取程序中的资源。
    ''' </summary>
    Public Function GetResources(ResourceName As String) As Byte()
        Log("[System] 获取资源：" & ResourceName)
        Dim Raw As Byte() = My.Resources.ResourceManager.GetObject(ResourceName)
        Return Raw
    End Function

#End Region

#Region "UI"

    '边距改变
    ''' <summary>
    ''' 相对增减控件的左边距。
    ''' </summary>
    Public Sub DeltaLeft(control As FrameworkElement, newValue As Double)
        '安全性检查
        DebugAssert(Not Double.IsNaN(newValue))
        DebugAssert(Not Double.IsInfinity(newValue))

        If TypeOf control Is Window Then
            '窗口改变
            CType(control, Window).Left += newValue
        Else
            '根据 HorizontalAlignment 改变数值
            Select Case control.HorizontalAlignment
                Case HorizontalAlignment.Left, HorizontalAlignment.Stretch
                    control.Margin = New Thickness(control.Margin.Left + newValue, control.Margin.Top, control.Margin.Right, control.Margin.Bottom)
                Case HorizontalAlignment.Right
                    control.Margin = New Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right - newValue, control.Margin.Bottom)
                    'control.Margin = New Thickness(control.Margin.Left, control.Margin.Top, CType(control.Parent, Object).ActualWidth - control.ActualWidth - newValue, control.Margin.Bottom)
                Case Else
                    DebugAssert(False)
            End Select
        End If
    End Sub
    ''' <summary>
    ''' 设置控件的左边距。（仅针对置左控件）
    ''' </summary>
    Public Sub SetLeft(control As FrameworkElement, newValue As Double)
        DebugAssert(control.HorizontalAlignment = HorizontalAlignment.Left)
        control.Margin = New Thickness(newValue, control.Margin.Top, control.Margin.Right, control.Margin.Bottom)
    End Sub
    ''' <summary>
    ''' 相对增减控件的上边距。
    ''' </summary>
    Public Sub DeltaTop(control As FrameworkElement, newValue As Double)
        '安全性检查
        DebugAssert(Not Double.IsNaN(newValue))
        DebugAssert(Not Double.IsInfinity(newValue))

        If TypeOf control Is Window Then
            '窗口改变
            CType(control, Window).Top += newValue
        Else
            '根据 VerticalAlignment 改变数值
            Select Case control.VerticalAlignment
                Case VerticalAlignment.Top
                    control.Margin = New Thickness(control.Margin.Left, control.Margin.Top + newValue, control.Margin.Right, control.Margin.Bottom)
                Case VerticalAlignment.Bottom
                    control.Margin = New Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom - newValue)
                    'control.Margin = New Thickness(control.Margin.Left, control.Margin.Top, CType(control.Parent, Object).ActualWidth - control.ActualWidth - newValue, control.Margin.Bottom)
                Case Else
                    DebugAssert(False)
            End Select
        End If

        'If Double.IsNaN(newValue) OrElse Double.IsInfinity(newValue) Then Exit Sub '安全性检查
        'Select Case control.VerticalAlignment
        '  Case VerticalAlignment.Top, VerticalAlignment.Stretch, VerticalAlignment.Center
        '      control.Margin = New Thickness(control.Margin.Left, newValue, control.Margin.Right, control.Margin.Bottom)
        '  Case VerticalAlignment.Bottom
        '      control.Margin = New Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, -newValue)
        '      'control.Margin = New Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, CType(control.Parent, Object).ActualHeight - control.ActualHeight - newValue)
        'End Select
    End Sub
    ''' <summary>
    ''' 设置控件的顶边距。（仅针对置上控件）
    ''' </summary>
    Public Sub SetTop(control As FrameworkElement, newValue As Double)
        DebugAssert(control.VerticalAlignment = VerticalAlignment.Top)
        control.Margin = New Thickness(control.Margin.Left, newValue, control.Margin.Right, control.Margin.Bottom)
    End Sub

    'DPI 转换
    Public ReadOnly DPI As Integer = System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiX
    ''' <summary>
    ''' 将经过 DPI 缩放的 WPF 尺寸转化为实际的像素尺寸。
    ''' </summary>
    Public Function GetPixelSize(WPFSize As Double) As Double
        Return WPFSize / 96 * DPI
    End Function
    ''' <summary>
    ''' 将实际的像素尺寸转化为经过 DPI 缩放的 WPF 尺寸。
    ''' </summary>
    Public Function GetWPFSize(PixelSize As Double) As Double
        Return PixelSize * 96 / DPI
    End Function

    'UI 截图
    ''' <summary>
    ''' 将某个控件的呈现转换为图片。
    ''' </summary>
    Public Function ControlBrush(UI As FrameworkElement) As ImageBrush
        Dim Width = UI.ActualWidth, Height = UI.ActualHeight
        If Width < 1 OrElse Height < 1 Then Return New ImageBrush
        Dim bmp As New RenderTargetBitmap(GetPixelSize(Width), GetPixelSize(Height), DPI, DPI, PixelFormats.Pbgra32)
        bmp.Render(UI)
        Return New ImageBrush(bmp)
    End Function
    ''' <summary>
    ''' 将某个控件的模拟呈现转换为图片。
    ''' </summary>
    Public Function ControlBrush(UI As FrameworkElement, Width As Double, Height As Double, Optional Left As Double = 0, Optional Top As Double = 0) As ImageBrush
        UI.Measure(New Size(Width, Height))
        UI.Arrange(New Rect(0, 0, Width, Height))
        Dim bmp As New RenderTargetBitmap(GetPixelSize(Width), GetPixelSize(Height), DPI, DPI, PixelFormats.Default)
        bmp.Render(UI)
        If Not (Left = 0 AndAlso Top = 0) Then UI.Arrange(New Rect(Left, Top, Width, Height))
        Return New ImageBrush(bmp)
    End Function
    ''' <summary>
    ''' 将 UI 内容固定为图片并进行 Clear。
    ''' </summary>
    Public Sub ControlFreeze(UI As Panel)
        UI.Background = ControlBrush(UI)
        UI.Children.Clear()
    End Sub
    ''' <summary>
    ''' 将 UI 内容固定为图片并进行 Clear。
    ''' </summary>
    Public Sub ControlFreeze(UI As Border)
        UI.Background = ControlBrush(UI)
        UI.Child = Nothing
    End Sub

    ''' <summary>
    ''' 将 XML 转换为对应 UI 对象。
    ''' </summary>
    Public Function GetObjectFromXML(Str As XElement)
        Return GetObjectFromXML(Str.ToString)
    End Function
    ''' <summary>
    ''' 将 XML 转换为对应 UI 对象。
    ''' </summary>
    Public Function GetObjectFromXML(Str As String)
        Using Stream As New MemoryStream
            Using Writer As New StreamWriter(Stream)
                Writer.Write(Str)
                Writer.Flush()
                Stream.Position = 0
                Return XamlReader.Load(Stream)
            End Using
        End Using
    End Function

    Private ReadOnly UiThreadId As Integer = Thread.CurrentThread.ManagedThreadId
    ''' <summary>
    ''' 当前线程是否为主线程。
    ''' </summary>
    Public Function RunInUi() As Boolean
        Return Thread.CurrentThread.ManagedThreadId = UiThreadId
    End Function

#End Region

#Region "Debug"

    Public ModeDebug As Boolean = False

    'Log
    Public Enum LogLevel
        ''' <summary>
        ''' 不提示，只记录日志。
        ''' </summary>
        Normal = 0
        ''' <summary>
        ''' 只提示开发者。
        ''' </summary>
        Developer = 1
        ''' <summary>
        ''' 只提示开发者与调试模式用户。
        ''' </summary>
        Debug = 2
        ''' <summary>
        ''' 弹出提示所有用户。
        ''' </summary>
        Hint = 3
        ''' <summary>
        ''' 弹窗，不要求反馈。
        ''' </summary>
        Msgbox = 4
        ''' <summary>
        ''' 弹窗，要求反馈。
        ''' </summary>
        Feedback = 5
        ''' <summary>
        ''' 弹窗，结束程序。
        ''' </summary>
        Assert = 6
    End Enum
    ''' <summary>
    ''' 记录程序是否递交了错误提示，以确保运行测试正确。
    ''' </summary>
    Public IsErrorTriggered As Boolean = False
    Private LogList As New StringBuilder
    Private LogWritter As StreamWriter
    Public Sub LogStart()
        RunInNewThread(Sub()
                           Dim IsInitSuccess As Boolean = True
                           Try
                               For i = 4 To 1
                                   If File.Exists(Path & "PCL\Log" & i & ".txt") Then
                                       If File.Exists(Path & "PCL\Log" & (i + 1) & ".txt") Then File.Delete(Path & "PCL\Log" & (i + 1) & ".txt")
                                       File.Copy(Path & "PCL\Log" & i & ".txt", Path & "PCL\Log" & (i + 1) & ".txt")
                                   End If
                               Next
                               File.Create(Path & "PCL\Log1.txt").Dispose()
                           Catch ex As IOException
                               IsInitSuccess = False
                               Hint("可能同时开启了多个 PCL2，程序可能会出现未知问题！", HintType.Critical)
                               Log(ex, "日志初始化失败（疑似文件占用问题）")
                           Catch ex As Exception
                               IsInitSuccess = False
                               Log(ex, "日志初始化失败", LogLevel.Hint)
                           End Try
                           Try
                               LogWritter = New StreamWriter(Path & "PCL\Log1.txt", True) With {.AutoFlush = True}
                           Catch ex As Exception
                               LogWritter = Nothing
                               Log(ex, "日志写入失败", LogLevel.Hint)
                           End Try
                           While True
                               If IsInitSuccess Then
                                   LogFlush()
                               Else
                                   LogList = New StringBuilder '清空 LogList 避免内存爆炸
                               End If
                               Thread.Sleep(50)
                           End While
                       End Sub, "Log Writer", ThreadPriority.Lowest)
    End Sub
    Private ReadOnly LogFlushLock As New Object '防止外部调用 LogFlush 时同时输出多次日志
    Public Sub LogFlush()
        On Error Resume Next
        If LogWritter Is Nothing Then Exit Sub
        Dim Log As String = Nothing
        SyncLock LogFlushLock
            If LogList.Length > 0 Then
                Dim LogListCache As StringBuilder
                LogListCache = LogList
                LogList = New StringBuilder
                Log = LogListCache.ToString
            End If
        End SyncLock
        If Log IsNot Nothing Then
            LogWritter.Write(Log)
        End If
    End Sub

    Private ReadOnly LogListLock As New Object '防止日志乱码，只在调试模式下启用
    ''' <summary>
    ''' 输出 Log。
    ''' </summary>
    ''' <param name="Title">如果要求弹窗，指定弹窗的标题。</param>
    Public Sub Log(Text As String, Optional Level As LogLevel = LogLevel.Normal, Optional Title As String = "出现错误")
        On Error Resume Next
        '放在最后会导致无法显示极端错误下的弹窗（如无法写入日志文件）
        '处理错误会导致再次调用 Log() 导致无限循环

        '输出日志
        Dim AppendText As String = "[" & GetTimeNow() & "] " & Text & vbCrLf '减轻同步锁占用
        If ModeDebug Then
            SyncLock LogListLock
                LogList.Append(AppendText)
            End SyncLock
        Else
            LogList.Append(AppendText)
        End If
#If DEBUG Then
        Console.Write(AppendText)
#End If
        If IsProgramEnded OrElse Level = LogLevel.Normal Then Exit Sub

        '去除前缀
        Text = RegexReplace(Text, "", "\[[^\]]+?\] ")

        '输出提示
        Select Case Level
#If DEBUG Then
            Case LogLevel.Developer
                Hint("[开发者模式] " & Text, HintType.Info, False)
            Case LogLevel.Debug
                Hint("[调试模式] " & Text, HintType.Info, False)
#Else
            Case LogLevel.Developer
            Case LogLevel.Debug
                If ModeDebug Then Hint("[调试模式] " & Text, HintType.Info, False)
#End If
            Case LogLevel.Hint
                IsErrorTriggered = True
                Hint(Text, HintType.Critical, False)
            Case LogLevel.Msgbox
                IsErrorTriggered = True
                MyMsgBox(Text, Title)
            Case LogLevel.Feedback
                IsErrorTriggered = True
                If MyMsgBox(Text & vbCrLf & vbCrLf & "是否反馈此问题？如果不反馈，这个问题可能永远无法得到解决！", Title, "反馈", "取消") = 1 Then Feedback("exlog", False, True)
            Case LogLevel.Assert
                IsErrorTriggered = True
                Dim Time As Long = GetTimeTick()
                If MsgBox(Text & vbCrLf & vbCrLf & "是否反馈此问题？如果不反馈，这个问题可能永远无法得到解决！", MsgBoxStyle.Critical + MsgBoxStyle.YesNo, Title) = MsgBoxResult.Yes Then Feedback("exlog", False, True)
                If GetTimeTick() - Time < 1500 Then
                    '弹窗无法保留
                    Log("[System] PCL 已崩溃：" & vbCrLf & Text)
                    FormMain.EndProgramForce(Result.Exception)
                Else
                    FormMain.EndProgramForce(Result.Fail)
                End If
        End Select

    End Sub
    ''' <summary>
    ''' 输出错误信息。
    ''' </summary>
    ''' <param name="Desc">错误描述。会在处理时在末尾加入冒号。</param>
    Public Sub Log(Ex As Exception, Desc As String, Optional Level As LogLevel = LogLevel.Debug, Optional Title As String = "出现错误")
        On Error Resume Next
        If TypeOf Ex Is ThreadInterruptedException Then Exit Sub
        IsErrorTriggered = True

        '获取错误信息
        Dim ExFull As String = Desc & "：" & GetString(Ex, False)

        '输出日志
        Dim AppendText As String = "[" & GetTimeNow() & "] " & Desc & "：" & GetString(Ex, False, True) & vbCrLf '减轻同步锁占用
        If ModeDebug Then
            SyncLock LogListLock
                LogList.Append(AppendText)
            End SyncLock
        Else
            LogList.Append(AppendText)
        End If
#If DEBUG Then
        Console.Write(AppendText)
#End If
        If IsProgramEnded Then Exit Sub

        '输出提示
        Select Case Level
            Case LogLevel.Normal
#If DEBUG Then
            Case LogLevel.Developer
                Dim ExLine As String = Desc & "：" & GetString(Ex, True)
                Hint("[开发者模式] " & ExLine, HintType.Info, False)
            Case LogLevel.Debug
                Dim ExLine As String = Desc & "：" & GetString(Ex, True)
                Hint("[调试模式] " & ExLine, HintType.Info, False)
#Else
            Case LogLevel.Developer
            Case LogLevel.Debug
                Dim ExLine As String = Desc & "：" & GetString(Ex, True)
                If ModeDebug Then Hint("[调试模式] " & ExLine, HintType.Info, False)
#End If
            Case LogLevel.Hint
                Dim ExLine As String = Desc & "：" & GetString(Ex, True)
                Hint(ExLine, HintType.Critical, False)
            Case LogLevel.Msgbox
                MyMsgBox(ExFull, Title)
            Case LogLevel.Feedback
                If MyMsgBox(ExFull & vbCrLf & vbCrLf & "是否反馈此问题？如果不反馈，这个问题可能永远无法得到解决！", Title, "反馈", "取消") = 1 Then Feedback("exlog", False, True)
            Case LogLevel.Assert
                Dim Time As Long = GetTimeTick()
                If MsgBox(ExFull & vbCrLf & vbCrLf & "是否反馈此问题？如果不反馈，这个问题可能永远无法得到解决！", MsgBoxStyle.Critical + MsgBoxStyle.YesNo, Title) = MsgBoxResult.Yes Then Feedback("exlog", False, True)
                If GetTimeTick() - Time < 1500 Then
                    '弹窗无法保留
                    Log("[System] PCL 已崩溃：" & vbCrLf & ExFull)
                    FormMain.EndProgramForce(Result.Exception)
                Else
                    FormMain.EndProgramForce(Result.Fail)
                End If
        End Select

    End Sub

    '反馈
    Public Sub Feedback(Source As String, Optional ShowMsgbox As Boolean = True, Optional ForceOpenLog As Boolean = False)
        On Error Resume Next
        FeedbackInfo()
        If ForceOpenLog OrElse (ShowMsgbox AndAlso MyMsgBox("若你在汇报一个 Bug，请点击 打开文件夹 按钮，并上传 Log(1~5).txt 中包含错误信息的文件。" & vbCrLf & "游戏崩溃一般与启动器无关，请不要因为游戏崩溃而提交反馈。", "反馈提交提醒", "打开文件夹", "不需要") = 1) Then
            OpenExplorer("""" & Path & "PCL\""")
        End If
        OpenWebsite("https://jinshuju.net/f/rP4b6E?x_field_1=" & Source)
        If Setup.Get("HintFeedback") = "" Then Setup.Set("HintFeedback", "/") '确保反馈报告会被加载
    End Sub
    ''' <summary>
    ''' 在日志中输出系统诊断信息。
    ''' </summary>
    Public Sub FeedbackInfo()
        On Error Resume Next
        Log("[System] 诊断信息：" & vbCrLf &
            "操作系统：" & My.Computer.Info.OSFullName & vbCrLf &
            "剩余内存：" & Int(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024) & " M / " & Int(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024) & " M" & vbCrLf &
            "DPI：" & DPI & "（" & Math.Round(DPI / 96, 2) * 100 & "%）" & vbCrLf &
            "文件位置：" & Path)
    End Sub

    '断言
    Public Sub DebugAssert(Exp As Boolean)
        If Not Exp Then Throw New Exception("断言命中")
    End Sub

#End Region

#Region "随机"

    Private ReadOnly Random As New Random

    ''' <summary>
    ''' 随机选择其一。
    ''' </summary>
    Public Function RandomOne(Of T)(objects As ICollection(Of T)) As T
        Return objects(RandomInteger(0, objects.Count - 1))
    End Function

    ''' <summary>
    ''' 取随机整数。
    ''' </summary>
    Public Function RandomInteger(min As Integer, max As Integer) As Integer
        Return Math.Floor((max - min + 1) * Random.NextDouble) + min
    End Function

    ''' <summary>
    ''' 将数组随机打乱。
    ''' </summary>
    Public Function RandomChaos(Of T)(array As IList(Of T)) As IList(Of T)
        RandomChaos = New List(Of T)
        Do While array.Count > 0
            Dim i As Integer = RandomInteger(0, array.Count - 1)
            RandomChaos.Add(array(i))
            array.RemoveAt(i)
        Loop
    End Function

#End Region

End Module