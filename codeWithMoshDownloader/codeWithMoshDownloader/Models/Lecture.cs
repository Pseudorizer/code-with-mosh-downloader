using System.Collections.Generic;

namespace codeWithMoshDownloader.Models
{
    public class Lecture
    {
        public string WistiaId { get; set; }

        public GenericFile EmbeddedVideo { get; set; }

        public List<HtmlFile> HtmlFiles { get; } = new List<HtmlFile>();

        public List<GenericFile> Extras { get; } = new List<GenericFile>();
    }
}