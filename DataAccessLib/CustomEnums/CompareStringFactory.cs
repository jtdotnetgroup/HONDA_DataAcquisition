using System;
using System.Linq;
using System.Reflection;
using DataAccess.CustomAttributes;

namespace DataAccess.CustomEnums
{
    public class CompareStringFactory
    {
        public static string BuildCompareString(PropertyInfo propertyInfo,CompareEnum option)
        {
            var columnName = propertyInfo.Name;
            var tableColumnAttribute = propertyInfo.GetCustomAttributes(true).SingleOrDefault(p => p is TableColumnAttribute) as TableColumnAttribute;
            if (tableColumnAttribute != null)
            {
                columnName = tableColumnAttribute.ColumnName;
            }

            switch (option)
            {
                    case CompareEnum.Equal:
                    {
                        return $" {columnName} = @{propertyInfo.Name}";
                    }

                    case CompareEnum.GreaterThan:
                    {
                        return $"{columnName} > @{propertyInfo.Name}";
                    }

                    case CompareEnum.GreaterThanEqualTo:
                    {
                        return $"{columnName} >= @{propertyInfo.Name}";
                    }

                    case CompareEnum.In:
                    {
                        return $" {columnName} In @{propertyInfo.Name} ";
                    }

                    case CompareEnum.LessThan:
                    {
                        return $"{columnName} < @{propertyInfo.Name}";
                    }

                    case CompareEnum.LessThanEqualTo:
                    {
                        return $"{columnName} <= @{propertyInfo.Name}";
                    }

                    case CompareEnum.Like:
                    {
                        return $"{columnName} Like '%@{propertyInfo.Name}%'";
                    }

                    case CompareEnum.NotIn:
                    {
                        return $"{columnName} Not In @{propertyInfo.Name}";
                    }

                    case CompareEnum.NotLike:
                    {
                        return $"{columnName} Not Like @{propertyInfo.Name}";
                    }

                    case CompareEnum.IsNull:
                    {
                        return $"{columnName} Is Null";
                    }

                    default:
                    {
                        throw new ArgumentException($"不支持这种比较操作【{option.ToString()}】");
                    }
            }
        }
    }
}