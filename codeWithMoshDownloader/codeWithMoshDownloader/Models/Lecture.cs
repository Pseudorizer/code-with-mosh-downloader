using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace codeWithMoshDownloader.Models
{
    public class Lecture
    {
        public JObject VideoJson { get; set; }
        public List<Extra> Extras { get; } = new List<Extra>();

        public class Extra
        {
            public string Url { get; set; }
            public string FileName { get; set; }
        }
    }
}