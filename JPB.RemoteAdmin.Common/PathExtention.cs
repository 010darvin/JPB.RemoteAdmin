using System.IO;

namespace JPB.RemoteAdmin.Common
{
    public static class PathExtention
    {
        public static string GetTempFolderName()
        {
            var tempFileName = Path.GetTempFileName();
            File.Delete(tempFileName);
            var remove = tempFileName.Remove(tempFileName.Length - 4);

            if (Directory.Exists(remove))
            {
                return MakeDirUnique(remove);
            }
            return remove;
        }

        public static string MakeFileUnique(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string fileExt = Path.GetExtension(path);

            for (int i = 1; ; ++i)
            {
                if (!File.Exists(path))
                    return path;

                path = Path.Combine(dir, fileName + " (" + i + ")" + fileExt);
            }
        }

        public static string MakeDirUnique(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string targetFolder = path.Substring(path.LastIndexOf(@"\") + 1);

            for (int i = 1; ; ++i)
            {
                if (!Directory.Exists(path))
                    return path;

                path = Path.Combine(dir, targetFolder + " (" + i + ")");
            }
        }
    }
}