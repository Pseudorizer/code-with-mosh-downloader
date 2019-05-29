using System.Collections.Generic;

namespace codeWithMoshDownloader.Models
{
    public class Section
    {
        public string SectionName { get; set; }
        public List<string> UrlList { get; } = new List<string>();
    }
}