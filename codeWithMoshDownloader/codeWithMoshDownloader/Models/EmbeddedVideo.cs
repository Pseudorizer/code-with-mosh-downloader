namespace codeWithMoshDownloader.Models
{
    public class EmbeddedVideo : IDownload
    {
        public string FileName { get; set; }
        public string Url { get; set; }
    }
}