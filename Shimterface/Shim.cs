namespace Shimterface
{
	/// <summary>
	/// Base class for all generated shim types.
	/// </summary>
	/// <typeparam name="T">The concrete type that was shimmed.</typeparam>
	public abstract class Shim<T> : IShim<T>, IShim
	{
        protected readonly T _inst;

        public Shim(T inst)
        {
            _inst = inst;
        }

		object IShim.Unshim() => _inst;
        
        T IShim<T>.Unshim() => _inst;

		// No effect unless you cast the shim Shim<T>
        public static explicit operator T(Shim<T> s) => s._inst;
	}
}
