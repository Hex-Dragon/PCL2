Imports System.Text.RegularExpressions

Public Class SemVer
    Implements IComparable(Of SemVer)
    Implements IEquatable(Of SemVer)

    ' 正则表达式模式（含可选 v前缀）
    Private Const Pattern As String = "^v?(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)" &
        "(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?" &
        "(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$"

    Private Shared ReadOnly SemVerRegex As New Regex(Pattern, RegexOptions.Compiled Or RegexOptions.ExplicitCapture)

    Public ReadOnly Property Major As Integer
    Public ReadOnly Property Minor As Integer
    Public ReadOnly Property Patch As Integer
    Public ReadOnly Property Prerelease As String
    Public ReadOnly Property BuildMetadata As String

    Public Sub New(major As Integer, minor As Integer, patch As Integer, Optional prerelease As String = Nothing, Optional buildMetadata As String = Nothing)
        Me.Major = major
        Me.Minor = minor
        Me.Patch = patch
        Me.Prerelease = If(prerelease, String.Empty)
        Me.BuildMetadata = If(buildMetadata, String.Empty)
    End Sub

    Public Shared Function Parse(version As String) As SemVer
        If Not TryParse(version, Nothing) Then
            Throw New ArgumentException("Invalid semantic version format")
        End If

        Dim match = SemVerRegex.Match(version)
        Return CreateFromMatch(match)
    End Function

    Public Shared Function TryParse(version As String, ByRef result As SemVer) As Boolean
        If String.IsNullOrWhiteSpace(version) Then
            result = Nothing
            Return False
        End If

        Dim match = SemVerRegex.Match(version)
        If Not match.Success Then
            result = Nothing
            Return False
        End If

        result = CreateFromMatch(match)
        Return True
    End Function

    Private Shared Function CreateFromMatch(match As Match) As SemVer
        Dim major = Integer.Parse(match.Groups("major").Value)
        Dim minor = Integer.Parse(match.Groups("minor").Value)
        Dim patch = Integer.Parse(match.Groups("patch").Value)
        Dim prerelease = match.Groups("prerelease").Value
        Dim build = match.Groups("build").Value

        Return New SemVer(major, minor, patch, prerelease, build)
    End Function

    Public Function CompareTo(other As SemVer) As Integer Implements IComparable(Of SemVer).CompareTo
        If other Is Nothing Then Return 1

        Dim compare = Major.CompareTo(other.Major)
        If compare <> 0 Then Return compare

        compare = Minor.CompareTo(other.Minor)
        If compare <> 0 Then Return compare

        compare = Patch.CompareTo(other.Patch)
        If compare <> 0 Then Return compare

        Return ComparePrerelease(Prerelease, other.Prerelease)
    End Function

    Private Shared Function ComparePrerelease(a As String, b As String) As Integer
        If String.Equals(a, b, StringComparison.Ordinal) Then Return 0

        ' 正式版优先级高于预发布版
        If String.IsNullOrEmpty(a) Then Return 1
        If String.IsNullOrEmpty(b) Then Return -1

        Dim identifiersA = a.Split("."c)
        Dim identifiersB = b.Split("."c)

        For i = 0 To Math.Min(identifiersA.Length, identifiersB.Length) - 1
            Dim idA = identifiersA(i)
            Dim idB = identifiersB(i)

            Dim numA, numB As Integer
            Dim aIsNumeric = Integer.TryParse(idA, numA)
            Dim bIsNumeric = Integer.TryParse(idB, numB)

            Dim result As Integer

            If aIsNumeric AndAlso bIsNumeric Then
                result = numA.CompareTo(numB)
            ElseIf aIsNumeric OrElse bIsNumeric Then
                ' 数值标识符比非数值标识符优先级低
                Return If(aIsNumeric, -1, 1)
            Else
                result = String.Compare(idA, idB, StringComparison.Ordinal)
            End If

            If result <> 0 Then Return result
        Next

        Return identifiersA.Length.CompareTo(identifiersB.Length)
    End Function

    Public Overrides Function ToString() As String
        Dim version = $"{Major}.{Minor}.{Patch}"

        If Not String.IsNullOrEmpty(Prerelease) Then
            version &= $"-{Prerelease}"
        End If

        If Not String.IsNullOrEmpty(BuildMetadata) Then
            version &= $"+{BuildMetadata}"
        End If

        Return version
    End Function

    ' 实现相等性比较
    Public Overrides Function Equals(obj As Object) As Boolean
        Return Equals(TryCast(obj, SemVer))
    End Function

    Public Overloads Function Equals(other As SemVer) As Boolean Implements IEquatable(Of SemVer).Equals
        Return other IsNot Nothing AndAlso
               Major = other.Major AndAlso
               Minor = other.Minor AndAlso
               Patch = other.Patch AndAlso
               String.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal) AndAlso
               String.Equals(BuildMetadata, other.BuildMetadata, StringComparison.Ordinal)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hash = 17
        hash = hash * 23 + Major.GetHashCode()
        hash = hash * 23 + Minor.GetHashCode()
        hash = hash * 23 + Patch.GetHashCode()
        hash = hash * 23 + If(Prerelease, String.Empty).GetHashCode()
        hash = hash * 23 + If(BuildMetadata, String.Empty).GetHashCode()
        Return hash
    End Function

    ' 运算符重载
    Public Shared Operator =(left As SemVer, right As SemVer) As Boolean
        If left Is Nothing Then Return right Is Nothing
        Return left.Equals(right)
    End Operator

    Public Shared Operator <>(left As SemVer, right As SemVer) As Boolean
        Return Not (left = right)
    End Operator

    Public Shared Operator <(left As SemVer, right As SemVer) As Boolean
        If left Is Nothing Then Return right IsNot Nothing
        Return left.CompareTo(right) < 0
    End Operator

    Public Shared Operator >(left As SemVer, right As SemVer) As Boolean
        If left Is Nothing Then Return False
        Return left.CompareTo(right) > 0
    End Operator

    Public Shared Operator <=(left As SemVer, right As SemVer) As Boolean
        If left Is Nothing Then Return True
        Return left.CompareTo(right) <= 0
    End Operator

    Public Shared Operator >=(left As SemVer, right As SemVer) As Boolean
        If left Is Nothing Then Return right Is Nothing
        Return left.CompareTo(right) >= 0
    End Operator
End Class
