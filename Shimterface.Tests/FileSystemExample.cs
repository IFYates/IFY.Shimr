using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;

namespace Shimterface.Tests
{
	[TestClass]
	public class FileSystemExample
	{
		public interface IFileInfo
		{
			bool Exists { get; }
			string FullName { get; }
			string Name { get; }
			[TypeShim(typeof(DirectoryInfo))]
			IDirectoryInfo Directory { get; }
		}
		public interface IDirectoryInfo
		{
			bool Exists { get; }
			string Name { get; }
			[TypeShim(typeof(DirectoryInfo))]
			IDirectoryInfo Parent { get; }
			[TypeShim(typeof(IEnumerable<FileInfo>))]
			IEnumerable<IFileInfo> EnumerateFiles();
			[TypeShim(typeof(FileInfo[]))]
			IFileInfo[] GetFiles();
			string ToString();
		}

		[TestMethod]
		public void Test_file_system_shims()
		{
			var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			var di = new DirectoryInfo(path).Shim<IDirectoryInfo>();
			var files = di.GetFiles();
			var fileEnum = di.EnumerateFiles().ToArray();

			Assert.IsTrue(di.Exists);
			Assert.AreEqual("Debug", di.Name);
			Assert.IsTrue(di.Parent is IDirectoryInfo);
			Assert.AreEqual("bin", di.Parent.Name);

			Assert.IsTrue(files.Length > 0);
			CollectionAssert.AreEqual(files.Select(f => f.FullName).OrderBy(f => f).ToArray(), fileEnum.Select(f => f.FullName).OrderBy(f => f).ToArray());
		}
	}
}
