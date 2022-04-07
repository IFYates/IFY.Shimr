namespace IFY.Shimr
{
    /// <summary>
    /// Specify how the <see cref="ShimProxyAttribute"/> member is expected to behave.
    /// </summary>
    public enum ProxyBehaviour
    {
        /// <summary>
        /// Will override existing members, otherwise adds.
        /// </summary>
        Default,
        /// <summary>
        /// Shimmed type must provide the member to be overriden.
        /// </summary>
        Override,
        /// <summary>
        /// Shim must be defining a member not in the shimmed type.
        /// </summary>
        Add,
    }
}
