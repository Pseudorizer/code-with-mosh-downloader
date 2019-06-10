using System;

namespace codeWithMoshDownloader.Models
{
    public class Arguments
    {
        public bool Rename { get; set; }

        public string CookiesPath { get; set; }

        public int StartingPosition { get; set; }

        public Uri Url { get; set; }

        public Quality QualitySetting { get; set; } = Quality.Hd;
    }
}