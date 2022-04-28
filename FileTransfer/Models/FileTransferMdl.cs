namespace FileTransfer.Models
{
    public interface IFileTransferMdl
    {
        string DestFolderPath { get; set; }
        string SrcFolderPath { get; set; }

        IEnumerable<string> GetFileExtensions();
        IEnumerable<IGrouping<string, string>> GetFileGroups();
    }

    public class FileTransferMdl: IFileTransferMdl
    {
        IEnumerable<IGrouping<string, string>> _fileGroups;
        public string SrcFolderPath { get; set; }
        public string DestFolderPath { get; set; }

        public FileTransferMdl(string srcFolderPath, string destFolderPath)
        {
            SrcFolderPath = srcFolderPath;
            DestFolderPath = destFolderPath;
        }

        public IEnumerable<IGrouping<string, string>> GetFileGroups()
        {
            return _fileGroups ?? (_fileGroups = Directory.GetFiles(SrcFolderPath).GroupBy(x => Path.GetExtension(x)));
        }

        public IEnumerable<string> GetFileExtensions()
        {
            return GetFileGroups().Select(x=>x.Key);
        }
    }
}
