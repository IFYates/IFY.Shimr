namespace Shimterface.Standard
{
	class TestObject1
	{
		public string Field1;
	}

	class TestObject2
	{
		private readonly TestObject1 _inst;

		public TestObject2(TestObject1 inst)
		{
			_inst = inst;
		}

		public string WrapField
		{
			get { return _inst.Field1; }
			set { _inst.Field1 = value; }
		}
	}
}
