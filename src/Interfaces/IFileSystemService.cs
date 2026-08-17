using System.Threading.Tasks;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces
{
    /// <summary>
    /// Abstracts the handful of file-system operations the export flow needs, so
    /// SolutionExportService / BrowseSolutionsViewModel can be unit tested with a
    /// fake implementation instead of touching the real disk.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Writes bytes to disk. net48's System.IO.File has no native async byte-write
        /// method (File.WriteAllBytesAsync is a .NET Core-and-later addition), so
        /// implementations wrap the synchronous call in Task.Run.
        /// </summary>
        Task WriteAllBytesAsync(string path, byte[] data);

        bool DirectoryExists(string path);
        bool FileExists(string path);

        /// <summary>File size in bytes. Caller must ensure the file exists first.</summary>
        long GetFileSize(string path);
    }
}
