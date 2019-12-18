using DataAccess.CustomAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAccess.CustomEnums;

namespace DataAccess
{

    public partial class SqlBuilder
    {
        /// <summary>
        /// 获取实体映身的数据表名称，如实体类添加了 DataAccess.CustomAttributes.TableNameAttribute则返回TableName值，否则返回类名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetTableName<T>()
        {
            var t = typeof(T);

            var tableAttr = t.GetCustomAttributes(true).SingleOrDefault(p => p is TableNameAttribute) as TableNameAttribute;

            if (tableAttr == null)
            {
                return t.Name;
            }

            return tableAttr.TableName;
        }

        /// <summary>
        /// 根据条件对象获取Where语句，条件与条件之间的逻辑为与即用AND连接
        /// </summary>
        /// <param name="where">条件对象</param>
        /// <param name="compareOptions">条件对象属性所对应的比较操作字典,如果为空，则所有条件用 = 比较</param>
        /// <returns></returns>
        public static string GetWhereString(object where,Dictionary<string,CompareEnum> compareOptions=null)
        {
            if (where == null)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();

            var t = where.GetType();

            var pis = t.GetProperties();

            foreach (var pi in pis)
            {
                var value = pi.GetValue(where, null);
                if (value == null)
                {
                    continue;
                }

                CompareEnum compare = CompareEnum.Equal;

                if (compareOptions != null&&compareOptions.ContainsKey(pi.Name))
                {
                    compare = compareOptions[pi.Name];
                }

                var compareStr = CompareStringFactory.BuildCompareString(pi, compare);

                builder.Append($" AND {compareStr} ");

            }

            return builder.ToString();


        }

        /// <summary>
        /// 根据对像获取字段赋值语句，返回的结果是一个字典集合，方便拼接或用逗号分隔
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Dictionary<string,string> GetFieldNames<T>(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentException("参数不能为null");
            }

            var t = entity.GetType();
            var pis = t.GetProperties();

            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var pi in pis)
            {
                var value = pi.GetValue(entity, null);

                if (value == null)
                {
                    continue;
                }

                var fieldName = pi.Name;

                var columnAttribute =
                    pi.GetCustomAttributes(true)
                        .SingleOrDefault(p => p is TableColumnAttribute) as TableColumnAttribute;
                if (columnAttribute != null)
                {
                    fieldName = columnAttribute.ColumnName;
                }

                result.Add(pi.Name,fieldName);
            }

            return result;
        }

        public static Dictionary<string, string> GetFieldNames<T>()
        {
            var t = typeof(T);
            var pis = t.GetProperties();

            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var pi in pis)
            {
                var fieldName = pi.Name;

                var columnAttribute =
                    pi.GetCustomAttributes(true)
                        .SingleOrDefault(p => p is TableColumnAttribute) as TableColumnAttribute;
                if (columnAttribute != null)
                {
                    fieldName = columnAttribute.ColumnName;
                }

                result.Add(pi.Name, fieldName);
            }

            return result;
        }
    }
}