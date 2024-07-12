using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IFY.Shimr.Examples;

[TestClass]
public class FileSystemExample
{
    // Static filesystem-related methods as a "factory"
    public interface IFileSystem
    {
        [StaticShim(typeof(Directory))]
        IDirectoryInfo GetParent(string path);
    }

    /// <summary>
    /// The bits we want from <see cref="DirectoryInfo"/>.
    /// </summary>
    public interface IDirectoryInfo
    {
        bool Exists { get; }
        string FullName { get; } // From DirectoryInfo parent class
        IDirectoryInfo Parent { get; }
        IEnumerable<IFileInfo> EnumerateFiles(); // Autoshim enumerable
        IFileInfo[] GetFiles();
        string ToString();
    }

    /// <summary>
    /// The bits we want from <see cref="FileInfo"/>.
    /// </summary>
    public interface IFileInfo
    {
        bool Exists { get; }
        string FullName { get; }
        string Name { get; }
        IDirectoryInfo Directory { get; }
    }

    [TestMethod]
    public void Test_file_system_shims()
    {
        var assemblyFile = System.Reflection.Assembly.GetExecutingAssembly().Location;

        // We'll need to use the filesystem shim factory
        IFileSystem fileSystem = ShimBuilder.Create<IFileSystem>(); // In tests, this will be a mock

        // Make use of various shimmed methods
        IDirectoryInfo dir = fileSystem.GetParent(assemblyFile);
        IFileInfo[] files = dir.GetFiles();
        IFileInfo[] fileEnum = dir.EnumerateFiles().ToArray();

        // Check it returns what we expect
        Assert.IsTrue(dir.Exists);
        Assert.IsInstanceOfType<IDirectoryInfo>(dir.Parent);
        Assert.AreEqual(Directory.GetParent(assemblyFile)!.FullName, dir.FullName);

        Assert.IsTrue(files.Length > 0);
        CollectionAssert.AreEqual(files.Select(f => f.FullName).OrderBy(f => f).ToArray(), fileEnum.Select(f => f.FullName).OrderBy(f => f).ToArray());
    }
}
