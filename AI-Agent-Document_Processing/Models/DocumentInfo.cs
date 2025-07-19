namespace AI_Agent_Document_Processing.Models
{
    public class DocumentInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}
