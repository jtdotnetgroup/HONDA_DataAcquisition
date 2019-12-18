namespace DataAccess.CustomEnums
{
    /// <summary>
    /// 比较操作
    /// </summary>
    public enum CompareEnum
    {
        /// <summary>
        /// 模糊查询
        /// </summary>
        Like,
        /// <summary>
        /// In比较，多值比较
        /// </summary>
        In,
        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan,
        /// <summary>
        /// 小于
        /// </summary>
        LessThan,
        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanEqualTo,
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanEqualTo,
        ///等于
        Equal,
        /// <summary>
        /// 不包含
        /// </summary>
        NotLike,
        /// <summary>
        /// 不属于
        /// </summary>
        NotIn,
        /// <summary>
        /// 空值
        /// </summary>
        IsNull
    }
}