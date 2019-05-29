using System;

namespace codeWithMoshDownloader.Models
{
    public class Arguments
    {
        public bool Rename { get; set; }

        public string CookiesPath { get; set; }

        public Uri Url { get; set; }
    }
}