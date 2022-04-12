using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ElnApplication.Controllers.Apis
{
    public class MsSqlDbConn
    {
        
        public static DataTable SelectToDataTable(string connectionString, string queryStr)
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using(SqlCommand command = new SqlCommand(queryStr, conn))
                {
                    using(SqlDataAdapter adapter = new SqlDataAdapter())
                    {
                        adapter.SelectCommand = command;
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        public static List<T> SelectToList<T>(string connectionString, string queryStr)
        {
            DataTable dataTable = SelectToDataTable(connectionString, queryStr);

            List<T> rList = ConvertApi.ConvertToList<T>(dataTable);

            return rList;
        }

        public static int Update<T>(string connectionString, string queryStr, T paramClass)
        {
            int cntQuery = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(queryStr, conn))
                {
                    if(paramClass != null)
                    {
                        AddParams(command, paramClass);
                    }
                    cntQuery = command.ExecuteNonQuery();
                }
            }

            return cntQuery;
        }

        private static void AddParams<T>(SqlCommand command, T paramClass)
        {
            HashSet<string> matchParam = new HashSet<string>();
            MatchCollection matches = Regex.Matches(command.CommandText, @"@[a-zA-Z][-_a-zA-Z0-9]*");
            matches.Cast<Match>().ToList()
                .ForEach(match => matchParam.Add(match.Value.Replace("@", "")));

            PropertyInfo[] properties = paramClass.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            if(properties.Length <= 0)
            {
                return;
            }

            foreach(string str in matchParam)
            {
                foreach(PropertyInfo prop in properties)
                {
                    if(prop.Name == str)
                    {
                        command.Parameters.AddWithValue(str, prop.GetValue(paramClass) ?? DBNull.Value);
                        break;
                    }
                }
            }
        }
    }
}
