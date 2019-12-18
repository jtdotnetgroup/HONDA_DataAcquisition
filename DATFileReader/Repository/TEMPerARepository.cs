using DataAccess;
using DataAccess.CustomEnums;
using DATFileReader.Model;

namespace DATFileReader.Repository
{
    public class TEMPerARepository:AbstractCRUDRepository<TEMPerA>
    {
        public TEMPerARepository(DBTypeEnums dbType=DBTypeEnums.MYSQL) : base(dbType)
        {
        }
    }
}