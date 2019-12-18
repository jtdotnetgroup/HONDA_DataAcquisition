using System;

namespace DataAccess.CustomAttributes
{
    public class TableColumnAttribute:Attribute
    {
        public string ColumnName { get;  }

        public TableColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }

    }
}