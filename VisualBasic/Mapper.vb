Imports System.Reflection

' Mapper VB v1.0.0
' Copyright (c) 2021, Emanuel Rojas Vásquez
' https://github.com/erovas/LibreriasUtiles
' BSD 3-Clause License
Public Class Mapper

    Private Shared _flags As BindingFlags = BindingFlags.NonPublic Or BindingFlags.Instance
    Private Shared NewLine As String = Environment.NewLine

    ''' <summary>
    ''' Convert a DataTable to List of DTO's (POJO) 
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="data_table"></param>
    ''' <returns></returns>
    Public Shared Function DataTableToListDTO(Of T As New)(ByRef data_table As DataTable) As List(Of T)

        '> DataTable NO valida
        If IsNothing(data_table) OrElse data_table.Columns.Count = 0 Then
            Return Nothing
        End If

        Dim list_out As List(Of T) = New List(Of T)
        Dim rows As DataRowCollection = data_table.Rows

        '> DataTable vacia
        If rows.Count = 0 Then
            Return list_out
        End If

        Dim columnNames As String() = data_table.Columns.Cast(Of DataColumn)().Select(Function(x) x.ColumnName).ToArray()
        Dim fields As List(Of FieldInfo) = New List(Of FieldInfo)
        Dim _Type As Type = GetType(T)

        Dim i As Integer = 0
        Dim j As Integer = 0

        Dim row As DataRow
        Dim dto As T
        Dim field As FieldInfo
        Dim value As Object

        '> Obtener los FieldInfo requeridos
        For i = 1 To columnNames.Length - 1 Step 1
            field = _Type.GetField(columnNames(i), _flags)

            If Not IsNothing(field) Then
                fields.Add(field)
            End If
        Next

        For i = 0 To rows.Count - 1 Step 1

            row = rows(i)
            dto = New T()

            For j = 0 To fields.Count - 1 Step 1
                field = fields(i)
                value = row(field.Name)

                If IsNothing(value) OrElse IsDBNull(value) Then
                    Continue For
                End If

                Try
                    field.SetValue(dto, value)
                Catch ex As Exception
                    '> El tipo del valor devuelto por DDBB no coincida con el "field" de la entidad
                    ThrowException(value, field, ex)
                End Try

            Next

            list_out.Add(dto)

        Next

        Return list_out

    End Function


    ''' <summary>
    ''' Convert a DataRow to DTO (POJO)
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="data_row"></param>
    ''' <returns></returns>
    Public Shared Function DataRowToDTO(Of T As New)(ByRef data_row As DataRow) As T

        '> DataRow NO valida
        If IsNothing(data_row) OrElse data_row.Table.Columns.Count = 0 Then
            Return Nothing
        End If

        Dim columnNames As String() = data_row.Table.Columns.Cast(Of DataColumn)().[Select](Function(x) x.ColumnName).ToArray()
        Dim _Type As Type = GetType(T)
        Dim dto As T = New T()
        Dim i As Integer = 0

        Dim field As FieldInfo
        Dim value As Object

        For i = 0 To columnNames.Length - 1 Step 1

            field = _Type.GetField(columnNames(i), _flags)

            If IsNothing(field) Then
                Continue For
            End If

            value = data_row(field.Name)

            If IsNothing(value) OrElse IsDBNull(value) Then
                Continue For
            End If

            Try
                field.SetValue(dto, value)
            Catch ex As Exception
                '> El tipo del valor devuelto por DDBB no coincida con el "field" de la entidad
                ThrowException(value, field, ex)
            End Try
        Next

        Return dto

    End Function


#Region "PRIVATE"

    Private Shared Sub ThrowException(ByRef value As Object, ByRef field As FieldInfo, ByRef ex As Exception)

        Dim msj As String

        msj = "Mapper Exception:"
        msj += NewLine
        msj += "Target Type: ""{0}"""
        msj += NewLine
        msj += "Source Type: ""{1}"""
        msj += NewLine
        msj += "CANNOT BE CONVERTED"
        msj += NewLine + NewLine
        msj += "Original Message:"
        msj += NewLine
        msj += ex.Message

        msj = String.Format(msj, value.GetType().FullName, field.FieldType.FullName)

        Throw New Exception(msj)

    End Sub

#End Region

End Class
