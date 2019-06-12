using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

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
            return String.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
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

        public static T TryGetJsonValue<T>(JToken json, string key, T defaultReturn = default)
        {
            return json[key] != null ? json[key].Value<T>() : defaultReturn;
        }

        public static string AddIndex(string filename, int index)
        {
            if (!Regex.IsMatch(filename, @"^\d+\s*-\s*"))
            {
                filename += $"{index} - ";
            }

            return filename;
        }
    }
}