using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shimterface.Tests
{
    [TestClass]
    public class FileSystemExample
    {
        // Static filesystem-related methods as a "factory"
        public interface IFileSystem
        {
            [StaticShim(typeof(Directory))]
            IDirectoryInfo GetParent(string path);
        }

        // The bits we want from DirectoryInfo
        public interface IDirectoryInfo
        {
            bool Exists { get; }
            string FullName { get; }
            [TypeShim(typeof(DirectoryInfo))]
            IDirectoryInfo Parent { get; }
            [TypeShim(typeof(IEnumerable<FileInfo>))]
            IEnumerable<IFileInfo> EnumerateFiles();
            [TypeShim(typeof(FileInfo[]))]
            IFileInfo[] GetFiles();
            string ToString();
        }

        // The bits we want from FileInfo
        public interface IFileInfo
        {
            bool Exists { get; }
            string FullName { get; }
            string Name { get; }
            [TypeShim(typeof(DirectoryInfo))]
            IDirectoryInfo Directory { get; }
        }

        [TestMethod]
        public void Test_file_system_shims()
        {
            var assemblyFile = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // We'll need to use the filesystem shim factory
            var fileSystem = ShimBuilder.Create<IFileSystem>(); // In tests, this will be a mock

            // Make use of various shimmed methods
            IDirectoryInfo dir = fileSystem.GetParent(assemblyFile);
            IFileInfo[] files = dir.GetFiles();
            IFileInfo[] fileEnum = dir.EnumerateFiles().ToArray();

            // Check it returns what we expect
            Assert.IsTrue(dir.Exists);
            Assert.IsTrue(dir.Parent is IDirectoryInfo);
            Assert.AreEqual(Directory.GetParent(assemblyFile).FullName, dir.FullName);

            Assert.IsTrue(files.Length > 0);
            CollectionAssert.AreEqual(files.Select(f => f.FullName).OrderBy(f => f).ToArray(), fileEnum.Select(f => f.FullName).OrderBy(f => f).ToArray());
        }
    }
}
