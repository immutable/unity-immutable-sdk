using System.IO;

namespace Immutable.Passport.Editor
{
    public static class FileHelpers
    {
        /// <summary>
        /// Copies everything except .meta files in the source directory to the destination directory
        /// </summary> 
        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            // Checks if the destination directory exists
            DirectoryInfo destinationDir = new(destinationPath);
            if (!destinationDir.Exists)
            {
                Directory.CreateDirectory(destinationPath);
            }
            else
            {
                // If the directory exists, clear it
                ClearDirectory(destinationPath);
            }

            var dir = new DirectoryInfo(sourcePath);
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (FileInfo file in dir.GetFiles())
            {
                if (!file.Name.EndsWith(".meta"))
                {
                    string targetFilePath = Path.Combine(destinationPath, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newdestinationPath = Path.Combine(destinationPath, subDir.Name);
                CopyDirectory(subDir.FullName, newdestinationPath);
            }
        }

        /// <summary>
        /// Deletes everything in the given directory
        /// </summary> 
        public static void ClearDirectory(string directoryPath)
        {
            DirectoryInfo directory = new(directoryPath);
            foreach (FileInfo fileInfo in directory.EnumerateFiles())
            {
                fileInfo.Delete();
            }

            foreach (DirectoryInfo directoryInfo in directory.EnumerateDirectories())
            {
                directoryInfo.Delete(true);
            }
        }
    }
}