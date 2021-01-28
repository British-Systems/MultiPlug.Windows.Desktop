using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MultiPlug.Windows.Desktop.Update
{
    internal static class Package
    {
        internal static string Extract(string theStartDir)
        {
            string FileNameWithoutExtension = Path.GetFileNameWithoutExtension(theStartDir);

            string RootDirectory = Path.Combine(Path.GetTempPath(), "multiplug", FileNameWithoutExtension);

            if (Directory.Exists(RootDirectory))
            {
                try
                {
                    Directory.Delete(RootDirectory, true);
                }
                catch (IOException)
                {
                }
            }

            if (!Directory.Exists(RootDirectory))
            {
                try
                {
                    Directory.CreateDirectory(RootDirectory);
                }
                catch (IOException)
                {
                }
            }

            try
            {
                ZipFile.ExtractToDirectory(theStartDir, RootDirectory);
            }
            catch
            {
                return string.Empty;
            }

            return RootDirectory;
        }

        internal static string GetPackageName(string theStartDir)
        {
            string Result = string.Empty;

            var result = Directory.GetFiles(theStartDir, "*.nuspec", SearchOption.TopDirectoryOnly);

            if (result.Any())
            {
                return Path.GetFileNameWithoutExtension(result[0]);
            }

            return Result;
        }


        internal static string GetHomeDirectory(string theStartDir)
        {
            string HomeDirectory = Desktop.Update.Directories.SearchForDirectory(theStartDir, "net472");

            if (HomeDirectory == string.Empty)
            {
                HomeDirectory = Desktop.Update.Directories.SearchForDirectory(theStartDir, "lib");
            }
            if (HomeDirectory == string.Empty)
            {
                HomeDirectory = theStartDir;
            }

            if (!Desktop.Update.Directories.DirectoryContainsDlls(HomeDirectory))
            {
                return string.Empty;
            }

            return HomeDirectory;
        }

        internal static bool DirectoryExistsOrCreate(SftpClient client, string remotePath)
        {
            try
            {

                if (!client.Exists(remotePath))
                {
                    client.CreateDirectory(remotePath);
                }
            }
            catch (Renci.SshNet.Common.SftpPermissionDeniedException)
            {
                return false;
            }

            return true;
        }

        internal static bool DirectoryDeleteCreate(SftpClient client, string remotePath)
        {
            try
            {
                client.DeleteDirectory(remotePath);
                client.CreateDirectory(remotePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static bool UploadDirectory(SftpClient client, string localPath, string remotePath)
        {
            try
            {

                Console.WriteLine("Uploading directory {0} to {1}", localPath, remotePath);

                IEnumerable<FileSystemInfo> infos =
                    new DirectoryInfo(localPath).EnumerateFileSystemInfos();
                foreach (FileSystemInfo info in infos)
                {
                    if (info.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        string subPath = remotePath + "/" + info.Name;
                        if (!client.Exists(subPath))
                        {
                            client.CreateDirectory(subPath);
                        }
                        UploadDirectory(client, info.FullName, remotePath + "/" + info.Name);
                    }
                    else
                    {
                        using (Stream fileStream = new FileStream(info.FullName, FileMode.Open))
                        {
                            Console.WriteLine(
                                "Uploading {0} ({1:N0} bytes)",
                                info.FullName, ((FileInfo)info).Length);

                            client.UploadFile(fileStream, remotePath + "/" + info.Name);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

    }
}
