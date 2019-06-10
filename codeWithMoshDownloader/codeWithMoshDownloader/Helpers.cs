using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.XPath;
using codeWithMoshDownloader.Models;
using HtmlAgilityPack;

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

        public static bool TryGetNode(HtmlDocument htmlDocument, string xPath, out HtmlNode node)
        {
            if (htmlDocument == null)
            {
                node = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(xPath))
            {
                node = null;
                return false;
            }

            node = htmlDocument.DocumentNode.SelectSingleNode(xPath);

            return node != null;
        }

        public static bool TryGetNode(HtmlNode htmlNode, string xPath, out HtmlNode node)
        {
            if (htmlNode == null)
            {
                node = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(xPath))
            {
                node = null;
                return false;
            }

            node = htmlNode.SelectSingleNode(xPath);

            return node != null;
        }

        public static bool TryGetNodes(HtmlDocument htmlDocument, string xPath, out HtmlNodeCollection node)
        {
            if (htmlDocument == null)
            {
                node = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(xPath))
            {
                node = null;
                return false;
            }

            node = htmlDocument.DocumentNode.SelectNodes(xPath);

            return node != null;
        }

        public static bool TryGetNodes(HtmlNode htmlNode, string xPath, out HtmlNodeCollection node)
        {
            if (htmlNode == null)
            {
                node = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(xPath))
            {
                node = null;
                return false;
            }

            node = htmlNode.SelectNodes(xPath);

            return node != null;
        }

        public static int CountListOfLists(List<LecturePage> list)
        {
            var total = 0;

            foreach (LecturePage i in list)
            {
                //total += i.UrlList.Count;
            }

            return total;
        }
    }
}