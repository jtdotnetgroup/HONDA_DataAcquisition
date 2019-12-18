using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DataAccess.CustomEnums;

namespace DataAccess
{
    public abstract class AbstractCRUDRepository<T> where T : class, new()
    {

        public DBTypeEnums DbType { get; set; }

        public AbstractCRUDRepository(DBTypeEnums dbType)
        {
            this.DbType = dbType;
        }

        public virtual List<T> Select(object where, Dictionary<string, CompareEnum> compares)
        {
            string sql = SqlBuilder.GetSelectSql<T>(where, compares);

            using (IDbConnection conn = DBConnectionFactory.GetConnection(DbType))
            {
                var result = conn.Query<T>(sql, where).ToList();
                return result;
            }
        }

        public virtual List<T> Select(string sql, object where)
        {
            using (IDbConnection conn = DBConnectionFactory.GetConnection(DbType))
            {
                var result = conn.Query<T>(sql, where).ToList();

                return result;
            }
        }

        public virtual T GetSingle(object where, Dictionary<string, CompareEnum> compares)
        {
            string sql = SqlBuilder.GetSelectSql<T>(where, compares);
            using (IDbConnection conn = DBConnectionFactory.GetConnection(DbType))
            {
                var result = conn.QuerySingleOrDefault<T>(sql, where);

                return result;
            }
        }

        public virtual T GetSingle(string sql, object where)
        {
            using (IDbConnection conn = DBConnectionFactory.GetConnection(DbType))
            {
                var result = conn.QuerySingleOrDefault<T>(sql, where);

                return result;
            }
        }

        public virtual bool Insert(T entity, IDbTransaction tran = null)
        {
            string sql = SqlBuilder.GetInsertSql(entity);
            IDbConnection conn = tran == null ? DBConnectionFactory.GetConnection(DbType) : tran.Connection;

            var result = tran == null ? conn.Execute(sql, entity) : conn.Execute(sql, entity, tran);
            if (tran == null)
            {
                conn.Close();
            }
            return result == 1;

        }

        public virtual bool InsertBulk(List<T> entities, IDbTransaction tran = null)
        {
            string sql = SqlBuilder.GetInsertBulkSql<T>();
            IDbConnection conn = tran == null ? DBConnectionFactory.GetConnection(DbType) : tran.Connection;

            var result = tran == null ? conn.Execute(sql, entities) : conn.Execute(sql, entities, tran);
            if (tran == null)
            {
                conn.Close();
            }
            return result == entities.Count;
        }

        public virtual bool Update(T entity, object where, IDbTransaction tran = null)
        {
            string sql = SqlBuilder.GetUpdateSql(entity, where);
            IDbConnection conn = tran == null ? DBConnectionFactory.GetConnection(DbType) : tran.Connection;

            var result = tran == null ? conn.Execute(sql, entity) : conn.Execute(sql, entity, tran);
            if (tran == null)
            {
                conn.Close();
            }
            return result == 1;

        }

        public virtual bool Delete<T>(object where, IDbTransaction tran = null)
        {
            string sql = SqlBuilder.GetDeleteSql<T>(where);

            IDbConnection conn = tran == null ? DBConnectionFactory.GetConnection(DbType) : tran.Connection;

            var result = tran == null ? conn.Execute(sql, where) : conn.Execute(sql, where, tran);
            if (tran == null)
            {
                conn.Close();
            }
            return result > 0;

        }

    }
}