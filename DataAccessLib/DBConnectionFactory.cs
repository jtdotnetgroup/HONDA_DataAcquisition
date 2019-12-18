using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DataAccess.CustomEnums;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace DataAccess
{
    public class DBConnectionFactory
    {
        public static IDbConnection GetConnection(DBTypeEnums dbType)
        {
            var conStr = AppDomain.CurrentDomain.GetData("DefaultDB");
            if (conStr == null)
            {
                string connectioName = ConfigurationManager.AppSettings["ConnectionName"];
                conStr = ConfigurationManager.ConnectionStrings[connectioName].ConnectionString;
                AppDomain.CurrentDomain.SetData("DefaultDB", conStr);
            }

            switch (dbType)
            {
                case DBTypeEnums.MYSQL:
                {
                    return new MySqlConnection(conStr.ToString());
                }

                case DBTypeEnums.SQLSERVER:
                {
                    return new SqlConnection(conStr.ToString());
                }

                case DBTypeEnums.ORACLE:
                {
                    return new OracleConnection(conStr.ToString());
                }

                default:
                {
                    throw new ArgumentException("不支持的数据库类型");
                }
            }
        }
    }
}