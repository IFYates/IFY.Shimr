namespace Shimterface
{
    /// <summary>
    /// Interface for all shimmed types
    /// </summary>
    public interface IShim
    {
        /// <summary>
        /// Return the original object that has been shimmed.
        /// </summary>
        object Unshim();
    }
}
