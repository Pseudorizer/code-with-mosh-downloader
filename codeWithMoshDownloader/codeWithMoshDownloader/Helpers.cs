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
            using (HttpResponseMessage response = await HttpClient.GetAsync(url))
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static string GetSafeFilename(this string filename)
        {
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

        public static T GetJsonValue<T>(JToken json, string key, T defaultReturn = default)
        {
            return json[key] != null ? json[key].Value<T>() : defaultReturn;
        }

        public static bool TryGetJsonValue<T>(JToken json, string key, out T value)
        {
            value = default;

            if (json[key] != null)
            {
                value = json[key].Value<T>();
            }

            return json[key] != null;
        }

        public static bool TryGetJsonValueByJPath<T>(JToken json, string jPath, out T value)
        {
            value = default;

            if (json.SelectToken(jPath) != null)
            {
                value = json.SelectToken(jPath).Value<T>();
            }

            return json.SelectToken(jPath) != null;
        }

        public static string AddIndex(string filename, int index)
        {
            if (!Regex.IsMatch(filename, @"^\d+\s*-\s*"))
            {
                filename = $"{index} - " + filename;
            }

            filename = Regex.Replace(filename, @"^(\d+)-\s", "$1 - ");

            return filename;
        }

        public static void ClearLine()
        {
            int currentCursorPosition = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentCursorPosition);
        }
    }
}