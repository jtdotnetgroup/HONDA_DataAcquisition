using System;

namespace DataAccess.CustomAttributes
{
    public class TableNameAttribute:Attribute
    {
        public string TableName { get; set; }
        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}