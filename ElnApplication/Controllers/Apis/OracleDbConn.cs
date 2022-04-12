using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ElnApplication.Controllers.Apis
{
    public class OracleDbConn
    {
        public static DataTable SelectToDataTable(string ConnectionString, string queryStr)
        {
            DataTable dataTable = new DataTable();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                using (OracleCommand command = new OracleCommand(queryStr, conn))
                {
                    using (OracleDataAdapter adapter = new OracleDataAdapter())
                    {
                        adapter.SelectCommand = command;
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        public static List<T> SelectToList<T>(string ConnectionString, string queryStr)
        {
            DataTable dataTable = SelectToDataTable(ConnectionString, queryStr);

            List<T> rList = ConvertApi.ConvertToList<T>(dataTable);

            return rList;
        }

        public static int Update<T>(string ConnectionString, string queryStr, T paramClass)
        {
            int cntQuery = 0;

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                using (OracleCommand command = new OracleCommand(queryStr, conn))
                {
                    if (paramClass != null)
                    {
                        AddParams(command, paramClass);
                    }
                    cntQuery = command.ExecuteNonQuery();
                }
            }

            return cntQuery;
        }

        private static void AddParams<T>(OracleCommand command, T paramClass)
        {
            HashSet<string> matchParam = new HashSet<string>();
            MatchCollection matches = Regex.Matches(command.CommandText, @":[a-zA-Z][-_a-zA-Z0-9]*");
            matches.Cast<Match>().ToList()
                .ForEach(match => matchParam.Add(match.Value.Replace(":", "")));

            PropertyInfo[] properties = paramClass.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (properties.Length <= 0)
            {
                return;
            }

            foreach (string str in matchParam)
            {
                foreach (PropertyInfo prop in properties)
                {
                    if (prop.Name == str)
                    {
                        command.Parameters.Add(new OracleParameter(str, prop.GetValue(paramClass) ?? DBNull.Value));
                        break;
                    }
                }
            }
        }
    }
}
