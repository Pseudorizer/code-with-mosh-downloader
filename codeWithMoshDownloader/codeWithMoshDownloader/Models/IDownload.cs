namespace codeWithMoshDownloader.Models
{
    internal interface IDownload
    {
        string Url { get; set; }
        string FileName { get; set; }
    }
}