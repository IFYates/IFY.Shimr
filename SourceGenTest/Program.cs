using IFY.Shimr;

Console.WriteLine("TEST");

namespace IFY.Shimr.Tests
{
    public class CodeGenTests
    {
        private readonly static Dictionary<(Type, Type), Type> _types = new Dictionary<(Type, Type), Type>();

        public static string Register(Type c, Type a, Type b)
        {
            _types[(a, b)] = c;
            return _types.Count + " > " + a.FullName + ", " + b.FullName + " = " + c.FullName;
        }

        [Shimr(typeof(string))]
        public interface IToString
        {
            string ToString();
        }

        public void Can_always_shim_null()
        {
            var t = Type.GetType("Shimr.Auto.StringShimr");
            var s = (IToString)Activator.CreateInstance(t, "test");
            var x = s.ToString();
            var t2 = _types[(typeof(IToString), typeof(string))];
        }
    }
}