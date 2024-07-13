Public Module ModBedrock
    Public Sub LaunchBedrockUWP()
        If ShellAndGetOutput("powershell", "-Command ""Get-AppxPackage -Name ""Microsoft.MinecraftUWP""""") IsNot String.Empty Then
            LaunchUWP()
        End If
    End Sub
    Public Function LaunchUWP() As Result
        Return ShellAndGetExitCode("explorer.exe", "shell:AppsFolder\Microsoft.MinecraftUWP_8wekyb3d8bbwe!App")
    End Function
    Public Sub LaunchBedrockBeta()
        If ShellAndGetOutput("powershell", "-Command ""Get-AppxPackage -Name ""Microsoft.MinecraftWindowsBeta""""") IsNot String.Empty Then
            LaunchBeta()
        End If
    End Sub
    Public Function LaunchBeta() As Result
        Return ShellAndGetExitCode("explorer.exe", "shell:AppsFolder\Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe!App")
    End Function
End Module
