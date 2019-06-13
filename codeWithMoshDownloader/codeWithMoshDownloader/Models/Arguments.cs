using System;

namespace codeWithMoshDownloader.Models
{
    public class Arguments
    {
        public bool Force { get; set; }

        public bool CheckFormats { get; set; }

        public string CookiesPath { get; set; }

        public int StartingPosition { get; set; } = 0;

        public Uri Url { get; set; }

        public string QualitySetting { get; set; } = "original";
    }
}