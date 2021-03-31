using System;
using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Shimterface.Shims
{
    public interface IILGenerator
    {
        Label BeginExceptionBlock();
        void BeginFinallyBlock();
        Label DefineLabel();
		void Emit(OpCode opcode);
		void Emit(OpCode opcode, int arg);
		void Emit(OpCode opcode, ConstructorInfo constructor);
		void Emit(OpCode opcode, FieldInfo field);
		void Emit(OpCode opcode, Label label);
		void Emit(OpCode opcode, MethodInfo method);
		void Emit(OpCode opcode, Type type);
        void EndExceptionBlock();
		void MarkLabel(Label loc);

		public void Box(Type type) => Emit(OpCodes.Box, type);
		public void Call(ConstructorInfo constructor) => Emit(OpCodes.Call, constructor);
		public void Call(MethodInfo method) => Emit(OpCodes.Call, method);
		public void EmitArg(int arg) => Emit(OpCodes.Ldarg, arg);
		public void EmitThis() => Emit(OpCodes.Ldarg_0);
		public void GetFalse() => Emit(OpCodes.Ldc_I4_0);
		public void GetField(FieldInfo field) => Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
		public void GetTrue() => Emit(OpCodes.Ldc_I4_1);
		public void New(ConstructorInfo constructor) => Emit(OpCodes.Newobj, constructor);
		public void Return() => Emit(OpCodes.Ret);
		public void SetField(FieldInfo field) => Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
		public void Throw() => Emit(OpCodes.Throw);
    }
}
