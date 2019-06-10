using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace codeWithMoshDownloader.Models
{
    public class Lecture
    {
        public string WistiaId { get; set; }

        public EmbeddedVideo EmbeddedVideo { get; set; }

        public List<TextArea> TextAreas { get; } = new List<TextArea>();

        public List<LectureExtra> Extras { get; } = new List<LectureExtra>();
    }
}