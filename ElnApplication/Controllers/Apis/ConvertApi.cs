using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace ElnApplication.Controllers.Apis
{
    public class ConvertApi
    {
        /// <summary>
        /// 데이터테이블을 제네릭리스트로 변환
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<T> ConvertToList<T>(DataTable table)
        {
            List<T> rList = new List<T>();
            foreach(DataRow row in table.Rows)
            {
                T obj = ConvertToObject<T>(row);
                rList.Add(obj);
            }
            return rList;
        }

        /// <summary>
        /// 데이터테이블의 각 로우를 제네릭오브젝트로 변환
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static T ConvertToObject<T>(DataRow row)
        {
            Type type = typeof(T);
            T obj = Activator.CreateInstance<T>();
            foreach(DataColumn column in row.Table.Columns)
            {
                foreach(PropertyInfo prop in type.GetProperties())
                {
                    if(column.ColumnName == prop.Name)
                    {
                        prop.SetValue(obj, row[column.ColumnName] == DBNull.Value ? null : row[column.ColumnName]);
                        break;
                    }
                }
            }
            return obj;
        }
    }
}
