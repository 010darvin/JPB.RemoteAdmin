using System;
using System.IO;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class FileInfoMessage
    {
        public FileInfoMessage()
        {

        }

        public static FileInfoMessage FromLocalFile(string localPath)
        {
            FileInfoMessage fim = new FileInfoMessage();
            fim.Path = localPath;
            try
            {
                if (File.Exists(localPath))
                {
                    fim.Type = FileType.File;

                    var fileInfo = new FileInfo(localPath);
                    fim.LastModifyed = fileInfo.LastWriteTime;
                    fim.Size = fileInfo.Length;

                    using (var memstream = new MemoryStream())
                    {
                        var extractAssociatedIcon = System.Drawing.Icon.ExtractAssociatedIcon(localPath);
                        if (extractAssociatedIcon != null)
                            extractAssociatedIcon.Save(memstream);
                        fim.Icon = memstream.ToArray();
                    }
                }
                else if (Directory.Exists(localPath))
                {
                    fim.Type = FileType.Directory;
                    var fileInfo = new DirectoryInfo(localPath);
                    fim.LastModifyed = fileInfo.LastWriteTime;
                }

                fim.AccessAllowed = true;

            }
            catch (Exception e)
            {
                fim.Icon = null;
                fim.AccessAllowed = false;
            }

            return fim;
        }

        public string Path { get; set; }
        public DateTime LastModifyed { get; set; }
        public FileType Type { get; set; }
        public long Size { get; set; }

        public bool AccessAllowed { get; set; }

        public byte[] Icon { get; set; }

        public override string ToString()
        {
            return string.Format("{0} | {1} | {2}", LastModifyed, FormartType(Type), Path);
        }

        public static string FormartType(FileType type)
        {
            switch (type)
            {
                case FileType.Drive:
                    return "<DRIVE>";
                    break;
                case FileType.Directory:
                    return "<DIR> ";
                    break;
                case FileType.File:
                    return "<FILE>";
                    break;
            }
            return "<ERR>";
        }
    }

    public enum FileType
    {
        Invalid = 0,
        Directory,
        Drive,
        File
    }
}
