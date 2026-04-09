using System;
using System.IO;

namespace Projektsoftware.Models
{
    public class CustomerDocument
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string Description { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public byte[] FileData { get; set; }

        public string FileSizeFormatted => FileSize < 1024 * 1024
            ? $"{FileSize / 1024.0:F1} KB"
            : $"{FileSize / (1024.0 * 1024):F1} MB";

        public bool HasFileData => FileData != null && FileData.Length > 0;
        public bool FileExists => (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath)) || HasFileData;
    }
}
