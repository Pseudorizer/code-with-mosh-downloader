using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using codeWithMoshDownloader.Models;

namespace codeWithMoshDownloader
{
    internal static class Helpers
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static string EscapeRegexCharacters(this string stringToEscape)
        {
            return stringToEscape.Replace(".", "\\.");
        }

        public static async Task<string> SimpleGet(string url)
        {
            HttpResponseMessage response = await HttpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        public static string GetSafeFilename(this string filename)
        {
            var t = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public static int CountListOfLists(List<Section> list)
        {
            var total = 0;

            foreach (Section i in list)
            {
                total += i.UrlList.Count;
            }

            return total;
        }
    }
}