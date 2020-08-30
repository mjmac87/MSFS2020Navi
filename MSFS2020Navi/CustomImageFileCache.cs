using MapControl.Caching;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSFS2020Navi
{
    public class CustomImageFileCache : ImageFileCache
    {
        private const string ExpiresTag = "EXPIRES:";
        private readonly string rootDirectory;

        public CustomImageFileCache(string directory) : base(directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("The parameter directory must not be null or empty.");
            }

            rootDirectory = directory;
        }

        public Task Clean()
        {
            return Task.Factory.StartNew(() => CleanRootDirectory(), TaskCreationOptions.LongRunning);
        }

        private async Task CleanRootDirectory()
        {
            var deletedFileCount = 0;

            foreach (var dir in new DirectoryInfo(rootDirectory).EnumerateDirectories())
            {
                deletedFileCount += await CleanDirectory(dir).ConfigureAwait(false);
            }

            Debug.WriteLine("ImageFileCache: Cleaned {0} files in {1}", deletedFileCount, rootDirectory);
        }

        private static async Task<int> CleanDirectory(DirectoryInfo directory)
        {
            var deletedFileCount = 0;

            foreach (var dir in directory.EnumerateDirectories())
            {
                deletedFileCount += await CleanDirectory(dir).ConfigureAwait(false);
            }

            foreach (var file in directory.EnumerateFiles())
            {
                try
                {
                    if (await ReadExpirationAsync(file).ConfigureAwait(false) < DateTime.UtcNow)
                    {
                        file.Delete();
                        deletedFileCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ImageFileCache: Failed cleaning {0}: {1}", file.FullName, ex.Message);
                }
            }

            if (!directory.EnumerateFileSystemInfos().Any())
            {
                try
                {
                    directory.Delete();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ImageFileCache: Failed cleaning {0}: {1}", directory.FullName, ex.Message);
                }
            }

            return deletedFileCount;
        }

        private static async Task<DateTime> ReadExpirationAsync(FileInfo file)
        {
            DateTime expiration = DateTime.MaxValue;

            if (file.Length > 16)
            {
                var buffer = new byte[16];

                using (var stream = file.OpenRead())
                {
                    stream.Seek(-16, SeekOrigin.End);

                    if (await stream.ReadAsync(buffer, 0, 16).ConfigureAwait(false) == 16 &&
                        Encoding.ASCII.GetString(buffer, 0, 8) == ExpiresTag)
                    {
                        expiration = new DateTime(BitConverter.ToInt64(buffer, 8), DateTimeKind.Utc);
                    }
                }
            }

            return expiration;
        }
    }
}
