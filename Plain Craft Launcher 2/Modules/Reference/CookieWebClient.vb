Imports System.Net

Public Class CookieWebClient
    Inherits WebClient

    Public Sub New(container As CookieContainer, Headers As Dictionary(Of String, String))
        Me.New(container)
        For Each keyVal In Headers
            Me.Headers(keyVal.Key) = keyVal.Value
        Next
    End Sub

    Public Sub New()
        Me.New(New CookieContainer())
    End Sub

    Public Sub New(container As CookieContainer)
        Me.container = container
    End Sub

    Private Shadows ReadOnly container As New CookieContainer()

    ''' <summary>
    ''' 以毫秒为单位的超时。
    ''' </summary>
    Public Timeout As Integer = 600000

    Protected Overrides Function GetWebRequest(ByVal address As Uri) As WebRequest
        Dim r As WebRequest = MyBase.GetWebRequest(address)
        Dim request = TryCast(r, HttpWebRequest)

        If request IsNot Nothing Then
            request.CookieContainer = container
            request.Timeout = Timeout
        End If

        Return r
    End Function

    Protected Overrides Function GetWebResponse(ByVal request As WebRequest, ByVal result As IAsyncResult) As WebResponse
        Dim response As WebResponse = MyBase.GetWebResponse(request, result)
        ReadCookies(response)
        Return response
    End Function

    Protected Overrides Function GetWebResponse(ByVal request As WebRequest) As WebResponse
        Dim response As WebResponse = MyBase.GetWebResponse(request)
        ReadCookies(response)
        Return response
    End Function

    Private Sub ReadCookies(ByVal r As WebResponse)
        Dim response = TryCast(r, HttpWebResponse)
        If response IsNot Nothing Then
            Dim cookies As CookieCollection = response.Cookies
            container.Add(cookies)
        End If
    End Sub
End Class
