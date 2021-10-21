'> Requiere instalar Oracle DataBase para obtener el ensamblado (dll) y hay que agregar la referencia al proyecto
Imports Oracle.DataAccess.Client

'> Viene con .Net framework pero está deprecated
'Imports System.Data.OracleClient

' OracleRepository VB v1.0.0
' Copyright (c) 2021, Emanuel Rojas Vásquez
' https://github.com/erovas/LibreriasUtiles
' BSD 3-Clause License
Public MustInherit Class OracleRepository(Of DTO As Class)

    Protected ReadOnly Property ConnectionString() As String
    Protected ReadOnly Property Parameters() As List(Of OracleParameter)


    Protected NewLine As String

    Protected TableName As String
    Protected SQLSelect As String
    Protected SQLInsert As String
    Protected SQLUpdate As String
    Protected SQLDelete As String

#Region "CONSTRUCTORES"

    '> Evitar que se pueda utilizar constructor por defecto, te va a obligar a llamar el otro constructor
    Private Sub New()
    End Sub


    Public Sub New(connectionString As String)

        If String.IsNullOrWhiteSpace(connectionString) Then
            Throw New Exception("Invalid connection string")
        End If

        _ConnectionString = connectionString

        _Parameters = New List(Of OracleParameter)()

        NewLine = Environment.NewLine

    End Sub

#End Region

#Region "BASICO IMPLEMENTAR"

    Public MustOverride Function GetAll() As IEnumerable(Of DTO)
    Public MustOverride Function GetAll_DataTable() As DataTable
    Public MustOverride Function GetById(Id As Integer) As DTO
    Public MustOverride Function GetById_DataTable(Id As Integer) As DataTable
    Public MustOverride Function Add(ByRef DTO_Object As DTO) As Integer
    Public MustOverride Function Edit(ByRef DTO_Object As DTO) As Boolean
    Public MustOverride Function Remove(Id As Integer) As Boolean

#End Region

#Region "METODOS PARA CONSULTAS"

    Protected Function ExecuteScalar(sql As String, Optional isStoredProcedure As Boolean = False) As Object

        Try
            '> Mediante "Using" está asegurada la liberación de la conexión
            Using connection As OracleConnection = GetConnection()

                connection.Open()

                '> Mediante "Using" está asegurada el Dispose() de command
                Using command As OracleCommand = New OracleCommand()

                    command.Connection = connection
                    command.CommandText = sql

                    If isStoredProcedure Then
                        command.CommandType = CommandType.StoredProcedure
                    Else
                        command.CommandType = CommandType.Text
                    End If

                    '> Insertar parametros si los hay
                    For Each parameter As OracleParameter In _Parameters
                        command.Parameters.Add(parameter)
                    Next

                    Dim result As Object = command.ExecuteScalar()

                    _Parameters.Clear()

                    Return result

                End Using

            End Using

        Catch ex As Exception
            _Parameters.Clear()
            Throw ex
        End Try

    End Function

    Protected Function ExecuteNoNQuery(sql As String, Optional isStoredProcedure As Boolean = False) As Integer

        Try
            '> Mediante "Using" está asegurada la liberación de la conexión
            Using connection As OracleConnection = GetConnection()

                connection.Open()

                '> Mediante "Using" está asegurada el Dispose() de command
                Using command As OracleCommand = New OracleCommand()

                    command.Connection = connection
                    command.CommandText = sql

                    If isStoredProcedure Then
                        command.CommandType = CommandType.StoredProcedure
                    Else
                        command.CommandType = CommandType.Text
                    End If

                    '> Insertar parametros si los hay
                    For Each parameter As OracleParameter In _Parameters
                        command.Parameters.Add(parameter)
                    Next

                    Dim result As Integer = command.ExecuteNonQuery()

                    _Parameters.Clear()

                    Return result

                End Using

            End Using

        Catch ex As Exception
            _Parameters.Clear()
            Throw ex
        End Try

    End Function

    Protected Function ExecuteReader(sql As String, Optional isStoredProcedure As Boolean = False) As DataTable

        Try
            '> Mediante "Using" está asegurada la liberación de la conexión
            Using connection As OracleConnection = GetConnection()

                connection.Open()

                '> Mediante "Using" está asegurada el Dispose() de command
                Using command As OracleCommand = New OracleCommand()

                    command.Connection = connection
                    command.CommandText = sql

                    If isStoredProcedure Then
                        command.CommandType = CommandType.StoredProcedure
                    Else
                        command.CommandType = CommandType.Text
                    End If

                    '> Insertar parametros si los hay
                    For Each parameter As OracleParameter In _Parameters
                        command.Parameters.Add(parameter)
                    Next

                    Dim result As OracleDataReader = command.ExecuteReader()

                    _Parameters.Clear()

                    Using data_table As DataTable = New DataTable()
                        data_table.Load(result)
                        result.Dispose()
                        Return data_table
                    End Using

                End Using

            End Using

        Catch ex As Exception
            _Parameters.Clear()
            Throw ex
        End Try

    End Function

    Protected Function ExecuteReaderDataSet(sql As String, Optional isStoredProcedure As Boolean = False) As DataSet

        Try
            '> Mediante "Using" está asegurada la liberación de la conexión
            Using connection As OracleConnection = GetConnection()

                connection.Open()

                '> Mediante "Using" está asegurada el Dispose() de command
                Using command As OracleCommand = New OracleCommand()

                    command.Connection = connection
                    command.CommandText = sql

                    If isStoredProcedure Then
                        command.CommandType = CommandType.StoredProcedure
                    Else
                        command.CommandType = CommandType.Text
                    End If

                    '> Insertar parametros si los hay
                    For Each parameter As OracleParameter In _Parameters
                        command.Parameters.Add(parameter)
                    Next

                    Dim result As OracleDataAdapter = New OracleDataAdapter(command)

                    _Parameters.Clear()

                    Using data_set As DataSet = New DataSet()
                        result.Fill(data_set)
                        result.Dispose()
                        Return data_set
                    End Using

                End Using

            End Using

        Catch ex As Exception
            _Parameters.Clear()
            Throw ex
        End Try

    End Function

    Protected Function ExecuteTransaction(ByRef sqlList As List(Of String), ByRef parametersList As List(Of List(Of OracleParameter))) As List(Of Integer)

        If sqlList.Count <> parametersList.Count Then
            Throw New Exception("sqlList must have the same count than parametersList")
        End If

        '> Mediante "Using" está asegurada la liberación de la conexión
        Using connection As OracleConnection = GetConnection()

            connection.Open()

            Dim transaction As OracleTransaction = connection.BeginTransaction()

            Try
                '> Mediante "Using" está asegurada el Dispose() de command
                Using command As OracleCommand = New OracleCommand()

                    command.Connection = connection
                    command.Transaction = transaction
                    command.CommandType = CommandType.Text

                    Dim i As Integer = 0
                    Dim currentParameters As List(Of OracleParameter) = Nothing
                    Dim result As List(Of Integer) = New List(Of Integer)

                    For i = 0 To sqlList.Count - 1 Step 1

                        command.CommandText = sqlList(i)
                        currentParameters = parametersList(i)

                        '> Insertar parametros si los hay
                        If Not IsNothing(currentParameters) Then
                            For Each parameter As OracleParameter In parametersList(i)
                                command.Parameters.Add(parameter)
                            Next
                        End If

                        result.Add(command.ExecuteNonQuery())

                        command.Parameters.Clear()
                    Next

                    '> Hacer transacción
                    transaction.Commit()

                    Return result

                End Using

            Catch ex As Exception

                Dim msg As String
                msg = "Commit Exception Type: {0}"
                msg += NewLine
                msg += "Message: {1}"
                msg = String.Format(msg, ex.GetType(), ex.Message)

                Try
                    transaction.Rollback()
                Catch ex2 As Exception
                    '> This catch block will handle any errors that may have occurred
                    '> on the server that would cause the rollback to fail, such as
                    '> a closed connection.
                    msg += NewLine
                    msg += "Rollback Exception Type: {0}"
                    msg += NewLine
                    msg += "Message: {1}"
                    msg = String.Format(msg, ex2.GetType(), ex2.Message)
                End Try

                Throw New Exception(msg)

            End Try

        End Using

    End Function

#End Region

#Region "METODOS PRIVADOS"

    Private Function GetConnection() As OracleConnection
        Return New OracleConnection(_ConnectionString)
    End Function

#End Region

End Class