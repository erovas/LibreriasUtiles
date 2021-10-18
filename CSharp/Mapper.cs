using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

// Mapper C# v1.0.0
// Copyright (c) 2021, Emanuel Rojas Vásquez
// https://github.com/erovas
// BSD 3-Clause License
namespace CSharp
{
    public class Mapper
    {
        private static BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static string NewLine = Environment.NewLine;

        /// <summary>
        /// Convert a DataTable to List of DTO's (POJO) 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static List<T> DataTableToListDTO<T>(DataTable dt) where T : new()
        {
            //DataTable NO valida
            if (dt == null || dt.Columns.Count == 0)
            {
                return null;
            }

            List<T> list_out = new List<T>();
            DataRowCollection rows = dt.Rows;

            //DataTable vacia
            if (rows.Count == 0)
            {
                return list_out;
            }


            string[] columnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();


            int i = 0;
            int j = 0;

            Type _Type = typeof(T);
            DataRow row;
            T dto;
            List<FieldInfo> fields = new List<FieldInfo>();
            FieldInfo field;
            object object_value;

            //Obtener todos los FieldInfo requeridos
            for (i = 0; i < columnNames.Length; i++)
            {
                field = _Type.GetField(columnNames[i], _flags);

                if (field != null)
                    fields.Add(field);
            }


            for (i = 0; i < rows.Count; i++)
            {
                row = rows[i];
                dto = new T();

                for (j = 0; j < fields.Count; j++)
                {
                    field = fields[j];

                    object_value = row[field.Name];

                    if (object_value == null || DBNull.Value == object_value)
                        continue;

                    try
                    {
                        field.SetValue(dto, object_value);
                    }
                    catch (Exception ex)
                    {
                        // > El tipo del valor devuelto por DDBB no coincida con el "field" de la entidad
                        ThrowException(object_value, field, ex);
                    }
                }

                list_out.Add(dto);
            }

            return list_out;
        }


        /// <summary>
        /// Convert a DataRow to DTO (POJO)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <returns></returns>
        private static T DataRowToDTO<T>(DataRow dr) where T : new()
        {
            // DataRow NO valida
            if (dr == null || dr.Table.Columns.Count == 0)
            {
                return default(T);
            }

            string[] columnNames = dr.Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
            T dto = new T();
            Type _Type = typeof(T);
            FieldInfo field;
            object object_value;

            for (int i = 0; i < columnNames.Length; i++)
            {
                field = _Type.GetField(columnNames[i], _flags);

                if (field == null)
                    continue;

                object_value = dr[field.Name];

                if (object_value == null || DBNull.Value == object_value)
                    continue;

                try
                {
                    field.SetValue(dto, object_value);
                }
                catch (Exception ex)
                {
                    // > El tipo del valor devuelto por DDBB no coincida con el "field" de la entidad
                    ThrowException(object_value, field, ex);
                }
            }

            return dto;

        }

        #region PRIVATE

        private static void ThrowException(object value, FieldInfo field, Exception ex)
        {
            string msj;

            msj = "Mapper Exception:";
            msj += NewLine;
            msj += "Target Type: \"{0}\"";
            msj += NewLine;
            msj += "Source Type: \"{1}\"";
            msj += NewLine;
            msj += "CANNOT BE CONVERTED";
            msj += NewLine + NewLine;
            msj += "Original Message:";
            msj += NewLine;
            msj += ex.Message;

            msj = String.Format(msj, value.GetType().FullName, field.FieldType.FullName);

            throw new Exception(msj);
        }

        #endregion

    }
}
