using System.IO;
using System.Threading.Tasks;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>Thin wrapper over System.IO so file access can be mocked in tests.</summary>
    public class FileSystemService : IFileSystemService
    {
        public async Task WriteAllBytesAsync(string path, byte[] data)
        {
            // net48's System.IO.File has no async byte-write overload
            // (File.WriteAllBytesAsync is .NET Core 3+ only), so the synchronous call
            // is pushed onto a thread-pool thread instead. This is what actually keeps
            // the UI thread free during the "Saving file..." stage.
            await Task.Run(() => File.WriteAllBytes(path, data)).ConfigureAwait(false);
        }

        public bool DirectoryExists(string path) => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        public bool FileExists(string path) => !string.IsNullOrEmpty(path) && File.Exists(path);

        public long GetFileSize(string path) => new FileInfo(path).Length;
    }
}
