using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Shimterface.Tests
{
    // Specific issues around these structures
    [TestClass]
	public class DbCommandTests
	{
		public interface ITestDbCommand : IDbCommand
		{
            DbTransaction DbTransaction { get; set; } // From DbCommand
        }

        [TestMethod]
        public void Can_shim_DbCommand()
        {
            var obj = new SqlCommand();

            var shim = obj.Shim<ITestDbCommand>();
        }
	}
}
