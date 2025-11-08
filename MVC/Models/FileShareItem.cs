namespace ABCRetailers.Models
{
    public class FileShareItem
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long? FileSize { get; set; }
    }
}
