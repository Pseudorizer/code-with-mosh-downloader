using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace codeWithMoshDownloader.Models
{
    public class Lecture
    {
        public string WistiaId { get; set; }

        public EmbeddedVideo EmbeddedVideo { get; set; }

        public List<IText> TextContentList { get; } = new List<IText>();

        public List<LectureExtra> Extras { get; } = new List<LectureExtra>();
    }
}