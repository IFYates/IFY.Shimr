namespace Shimterface
{
	/// <summary>
	/// Interface for all shimmed types
	/// </summary>
	public interface IShim
    {
        /// <summary>
        /// Returns the original object that has been shimmed.
        /// </summary>
        object Unshim();
    }
    
	/// <summary>
	/// Interface for all shimmed types
	/// </summary>
	public interface IShim<T>
    {
        /// <summary>
        /// Returns the original object that has been shimmed.
        /// </summary>
        T Unshim();
    }
}
