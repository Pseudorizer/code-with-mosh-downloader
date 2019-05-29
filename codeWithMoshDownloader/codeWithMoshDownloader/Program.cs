using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using codeWithMoshDownloader.Models;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                Environment.Exit(1);
            }

            var arguments = new Arguments();

            for (var index = 0; index < args.Length; index++)
            {
                string arg = args[index].Trim();

                switch (arg)
                {
                    case "-r":
                        arguments.Rename = true;
                        break;
                    case "-c" when index + 1 < args.Length:
                        arguments.CookiesPath = args[index + 1];
                        break;
                }
            }

            arguments.Url = new Uri(args.Last());

            if (!File.Exists(arguments.CookiesPath))
            {
                Console.WriteLine("Error: Cookies file not found");
                Environment.Exit(1);
            }

            var cookieParser = new CookieParser(arguments.CookiesPath);

            Console.WriteLine("Parsing cookies");
            CookieCollection cookiesParsed = cookieParser.GetWebsiteCookies("codewithmosh.com");

            var client = new SiteClient(new Uri($"{arguments.Url.Scheme}://{arguments.Url.Host}"));
            client.SetCookies(cookiesParsed);

            Console.WriteLine("Grabbing course");
            string courseHtml = await client.Get(arguments.Url.AbsolutePath);

            var pageParser = new Parser();
            string courseName = pageParser.GetCourseName(courseHtml);
            List<Section> playlistItems = pageParser.GetPlaylistItems(courseHtml);

            int total = CountListOfLists(playlistItems);
            Console.WriteLine($"{courseName} - {total} items");

            var downloader = new Downloader(client, courseName);
            await downloader.DownloadPlaylist(playlistItems, arguments.Rename);
        }

        private static void Help()
        {
            throw new NotImplementedException();

            //-c cookies
            //-r rename
        }
    }
}
