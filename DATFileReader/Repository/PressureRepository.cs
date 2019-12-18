using DataAccess;
using DataAccess.CustomEnums;
using DATFileReader.Model;

namespace DATFileReader.Repository
{
    public class PressureRepository:AbstractCRUDRepository<PressureRecord>
    {
        public PressureRepository(DBTypeEnums dbType=DBTypeEnums.MYSQL) : base(dbType)
        {
        }
    }
}