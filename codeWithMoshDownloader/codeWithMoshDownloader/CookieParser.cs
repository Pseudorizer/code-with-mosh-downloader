using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace codeWithMoshDownloader
{
    public class CookieParser
    {
        private readonly string _cookiesPath;

        public CookieParser(string cookiesPath)
        {
            _cookiesPath = cookiesPath;
        }

        public CookieCollection GetWebsiteCookies(string website)
        {
            var cookies = new CookieCollection();

            string cookieContent = File.ReadAllText(_cookiesPath);

            MatchCollection cookieMatches = Regex.Matches(cookieContent,
                $@"\.?{website.EscapeRegexCharacters()}\s\w+\s\/\w*\/?\s+\w+\s+\d+\s([^\s]+)\s([^\n]+)");

            foreach (Match cookieMatch in cookieMatches)
            {
                cookies.Add(new Cookie(cookieMatch.Groups[1].Captures[0].Value, cookieMatch.Groups[2].Captures[0].Value.Replace("\r", "")) { Domain = website });
            }

            return cookies;
        }
    }
}