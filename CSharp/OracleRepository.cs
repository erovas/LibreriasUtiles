// OracleRepository C# v1.0.0
// Copyright (c) 2021, Emanuel Rojas Vásquez
// https://github.com/erovas/LibreriasUtiles
// BSD 3-Clause License
namespace CSharp
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    // Requiere instalar Oracle DataBase para obtener el ensamblado (dll) y hay que agregar la referencia al proyecto
    using Oracle.DataAccess.Client;

    // Viene con .Net framework pero está deprecated
    // using System.Data.OracleClient;

    public abstract class OracleRepository<DTO> where DTO : class
    {
        private string _ConnectionString;
        private List<OracleParameter> _Parameters;

        protected string ConnectionString 
        { 
            get
            {
                return _ConnectionString;
            }
        }
        protected List<OracleParameter> Parameters
        {
            get
            {
                return _Parameters;
            }
        }


        protected string NewLine;

        protected string TableName;
        protected string SQLSelect;
        protected string SQLInsert;
        protected string SQLUpdate;
        protected string SQLDelete;

        #region CONSTRUCTORES

        // > Evitar que se pueda utilizar constructor por defecto, te va a obligar a llamar el otro constructor
        private OracleRepository()
        {
        }


        public OracleRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Invalid connection string");

            _ConnectionString = connectionString;

            _Parameters = new List<OracleParameter>();

            NewLine = Environment.NewLine;
        }

        #endregion

        #region IMPLEMENTAR

        public abstract IEnumerable<DTO> GetAll();
        public abstract DataTable GetAll_DataTable();
        public abstract DTO GetById(int Id);
        public abstract DataTable GetById_DataTable(int Id);
        public abstract int Add(ref DTO DTO_Object);
        public abstract bool Edit(ref DTO DTO_Object);
        public abstract bool Remove(int Id);

        #endregion

        #region PARA CONSULTAS

        protected object ExecuteScalar(string sql, bool isStoredProcedure = false)
        {
            try
            {
                // > Mediante "Using" está asegurada la liberación de la conexión
                using (OracleConnection connection = GetConnection())
                {
                    connection.Open();

                    // > Mediante "Using" está asegurada el Dispose() de command
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = sql;

                        if (isStoredProcedure)
                            command.CommandType = CommandType.StoredProcedure;
                        else
                            command.CommandType = CommandType.Text;

                        // > Insertar parametros si los hay
                        foreach (OracleParameter parameter in _Parameters)
                            command.Parameters.Add(parameter);

                        object result = command.ExecuteScalar();

                        _Parameters.Clear();

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _Parameters.Clear();
                throw ex;
            }
        }

        protected int ExecuteNoNQuery(string sql, bool isStoredProcedure = false)
        {
            try
            {
                // > Mediante "Using" está asegurada la liberación de la conexión
                using (OracleConnection connection = GetConnection())
                {
                    connection.Open();

                    // > Mediante "Using" está asegurada el Dispose() de command
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = sql;

                        if (isStoredProcedure)
                            command.CommandType = CommandType.StoredProcedure;
                        else
                            command.CommandType = CommandType.Text;

                        // > Insertar parametros si los hay
                        foreach (OracleParameter parameter in _Parameters)
                            command.Parameters.Add(parameter);

                        int result = command.ExecuteNonQuery();

                        _Parameters.Clear();

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _Parameters.Clear();
                throw ex;
            }
        }

        protected DataTable ExecuteReader(string sql, bool isStoredProcedure = false)
        {
            try
            {
                // > Mediante "Using" está asegurada la liberación de la conexión
                using (OracleConnection connection = GetConnection())
                {
                    connection.Open();

                    // > Mediante "Using" está asegurada el Dispose() de command
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = sql;

                        if (isStoredProcedure)
                            command.CommandType = CommandType.StoredProcedure;
                        else
                            command.CommandType = CommandType.Text;

                        // > Insertar parametros si los hay
                        foreach (OracleParameter parameter in _Parameters)
                            command.Parameters.Add(parameter);

                        OracleDataReader result = command.ExecuteReader();

                        _Parameters.Clear();

                        using (DataTable data_table = new DataTable())
                        {
                            data_table.Load(result);
                            result.Dispose();
                            return data_table;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Parameters.Clear();
                throw ex;
            }
        }

        protected DataSet ExecuteReaderDataSet(string sql, bool isStoredProcedure = false)
        {
            try
            {
                // > Mediante "Using" está asegurada la liberación de la conexión
                using (OracleConnection connection = GetConnection())
                {
                    connection.Open();

                    // > Mediante "Using" está asegurada el Dispose() de command
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = sql;

                        if (isStoredProcedure)
                            command.CommandType = CommandType.StoredProcedure;
                        else
                            command.CommandType = CommandType.Text;

                        // > Insertar parametros si los hay
                        foreach (OracleParameter parameter in _Parameters)
                            command.Parameters.Add(parameter);

                        OracleDataAdapter result = new OracleDataAdapter(command);

                        _Parameters.Clear();

                        using (DataSet data_set = new DataSet())
                        {
                            result.Fill(data_set);
                            result.Dispose();
                            return data_set;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Parameters.Clear();
                throw ex;
            }
        }

        protected List<int> ExecuteTransaction(ref List<string> sqlList, ref List<List<OracleParameter>> parametersList)
        {
            if (sqlList.Count != parametersList.Count)
                throw new Exception("sqlList must have the same count than parametersList");

            // > Mediante "Using" está asegurada la liberación de la conexión
            using (OracleConnection connection = GetConnection())
            {
                connection.Open();

                OracleTransaction transaction = connection.BeginTransaction();

                try
                {
                    // > Mediante "Using" está asegurada el Dispose() de command
                    using (OracleCommand command = new OracleCommand())
                    {
                        command.Connection = connection;
                        command.Transaction = transaction;
                        command.CommandType = CommandType.Text;

                        int i = 0;
                        List<OracleParameter> currentParameters = null;
                        List<int> result = new List<int>();

                        for (i = 0; i <= sqlList.Count - 1; i += 1)
                        {
                            command.CommandText = sqlList[i];
                            currentParameters = parametersList[i];

                            // > Insertar parametros si los hay
                            if (currentParameters != null)
                            {
                                foreach (OracleParameter parameter in parametersList[i])
                                    command.Parameters.Add(parameter);
                            }

                            result.Add(command.ExecuteNonQuery());

                            command.Parameters.Clear();
                        }

                        // > Hacer transacción
                        transaction.Commit();

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    string msg;
                    msg = "Commit Exception Type: {0}";
                    msg += NewLine;
                    msg += "Message: {1}";
                    msg = string.Format(msg, ex.GetType(), ex.Message);

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        // > This catch block will handle any errors that may have occurred
                        // > on the server that would cause the rollback to fail, such as
                        // > a closed connection.
                        msg += NewLine;
                        msg += "Rollback Exception Type: {0}";
                        msg += NewLine;
                        msg += "Message: {1}";
                        msg = string.Format(msg, ex2.GetType(), ex2.Message);
                    }

                    throw new Exception(msg);
                }
            }
        }

        #endregion

        #region PRIVADOS

        private OracleConnection GetConnection()
        {
            return new OracleConnection(_ConnectionString);
        }

        #endregion
    }
}