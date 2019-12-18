using DataAccess;
using DataAccess.CustomEnums;
using DATFileReader.Model;

namespace DATFileReader.Repository
{
    public class VerInfoRepository:AbstractCRUDRepository<VerInfo>
    {
        public VerInfoRepository(DBTypeEnums dbType=DBTypeEnums.MYSQL) : base(dbType)
        {
        }
    }
}