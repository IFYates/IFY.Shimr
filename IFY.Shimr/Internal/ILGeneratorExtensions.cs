using System.Reflection.Emit;

namespace Shimterface.Internal
{
    internal static class ILGeneratorExtensions
    {
        public static void Ldarg(this ILGenerator impl, byte num)
        {
            var op = num switch
            {
                0 => OpCodes.Ldarg_0,
                1 => OpCodes.Ldarg_1,
                2 => OpCodes.Ldarg_2,
                3 => OpCodes.Ldarg_3,
                _ => default
            };
            if (op.Value > 0)
            {
                impl.Emit(op);
            }
            else
            {
                impl.Emit(OpCodes.Ldarg_S, num);
            }
        }
        public static void Ldc_I4(this ILGenerator impl, int num)
        {
            var op = num switch
            {
                0 => OpCodes.Ldc_I4_0,
                1 => OpCodes.Ldc_I4_1,
                2 => OpCodes.Ldc_I4_2,
                3 => OpCodes.Ldc_I4_3,
                4 => OpCodes.Ldc_I4_4,
                5 => OpCodes.Ldc_I4_5,
                6 => OpCodes.Ldc_I4_6,
                7 => OpCodes.Ldc_I4_7,
                8 => OpCodes.Ldc_I4_8,
                _ => default
            };
            if (op.Value > 0)
            {
                impl.Emit(op);
            }
            else
            {
                impl.Emit(OpCodes.Ldc_I4, num);
            }
        }
        public static void Ldloc(this ILGenerator impl, int num)
        {
            var op = num switch
            {
                0 => OpCodes.Ldloc_0,
                1 => OpCodes.Ldloc_1,
                2 => OpCodes.Ldloc_2,
                3 => OpCodes.Ldloc_3,
                _ => default
            };
            if (op.Value > 0)
            {
                impl.Emit(op);
            }
            else
            {
                impl.Emit(OpCodes.Ldloc, num); // TODO: Might be able to use Ldloc_S
            }
        }
        public static void Stloc(this ILGenerator impl, int num)
        {
            var op = num switch
            {
                0 => OpCodes.Stloc_0,
                1 => OpCodes.Stloc_1,
                2 => OpCodes.Stloc_2,
                3 => OpCodes.Stloc_3,
                _ => default
            };
            if (op.Value > 0)
            {
                impl.Emit(op);
            }
            else
            {
                impl.Emit(OpCodes.Stloc, num); // TODO: Might be able to use Stloc_S
            }
        }
    }
}