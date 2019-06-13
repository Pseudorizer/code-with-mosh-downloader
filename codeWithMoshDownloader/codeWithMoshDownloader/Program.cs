using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using codeWithMoshDownloader.Models;

namespace codeWithMoshDownloader
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var isLecture = false;

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
                    case "-f":
                        arguments.Force = true;
                        break;
                    case "-c" when index + 1 < args.Length:
                        arguments.CookiesPath = args[index + 1];
                        break;
                    case "-q" when index + 1 < args.Length:
                        arguments.QualitySetting = args[index + 1];
                        break;
                    case "-Q":
                        arguments.CheckFormats = true;
                        break;
                    case "-s" when index + 1 < args.Length:
                        if (int.TryParse(args[index + 1], out int y))
                        {
                            arguments.StartingPosition = y <= 0 ? 0 : y - 1;
                        }
                        break;
                }
            }

            arguments.Url = new Uri(args.Last());

            if (Regex.IsMatch(arguments.Url.ToString(), @"\/courses\/\d+\/lectures\/\d+"))
            {
                isLecture = true;
            }

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

            if (isLecture)
            {
                await DownloadLecture(arguments.Url.AbsolutePath, courseHtml, client, arguments);
            }
            else
            {
                await DownloadPlaylist(courseHtml, client, arguments);
            }
        }

        private static async Task DownloadPlaylist(string playListHtml, SiteClient client, Arguments arguments)
        {
            var pageParser = new Parser();
            string courseName = pageParser.GetCourseName(playListHtml);
            List<LecturePage> playlistItems = pageParser.GetPlaylistItems(playListHtml);

            Console.WriteLine($"{courseName} - {playlistItems.Count} items");

            var downloader = new Downloader(client, courseName);
            await downloader.Download(playlistItems, arguments);
        }

        private static async Task DownloadLecture(string lecturePath, string pageHtml, SiteClient client, Arguments arguments)
        {
            var pageParser = new Parser();
            string courseName = pageParser.GetCourseName(pageHtml);
            string sectionName = pageParser.GetSectionNameFromLecture(pageHtml);

            var lecturePage = new LecturePage
            {
                SectionName = sectionName,
                Url = lecturePath
            };

            var downloader = new Downloader(client, courseName);
            await downloader.Download(lecturePage, arguments);
        }

        private static void Help()
        {
            throw new NotImplementedException();

            //-c cookies
            //-r rename
        }
    }
}
