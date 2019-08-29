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
            var bin = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
			var shim = bin.Directory.Shim<IDirectoryInfo>();
			var files = shim.GetFiles();
			var fileEnum = shim.EnumerateFiles().ToArray();

			Assert.IsTrue(shim.Exists);
			Assert.IsTrue(shim.Parent is IDirectoryInfo);
			Assert.AreEqual(bin.Directory.Parent.Name, shim.Parent.Name);

			Assert.IsTrue(files.Length > 0);
			CollectionAssert.AreEqual(files.Select(f => f.FullName).OrderBy(f => f).ToArray(), fileEnum.Select(f => f.FullName).OrderBy(f => f).ToArray());
		}
	}
}
