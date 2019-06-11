using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ByteSizeLib;
using codeWithMoshDownloader.Models;
using Newtonsoft.Json.Linq;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    public class WistiaDownloader //THIS CLASS IS FUCKING DISGUSTING, WHAT A POS
    /*
     * So much crap, the downloadlecture method should be split up in so many locations to be re usable
     * So much repetition in it it's horrible
     *
     */
    {
        private readonly Parser _lectureParser = new Parser();
        private readonly SiteClient _siteClient;
        private readonly string _courseName;
        private int _downloadCounter;
        private bool _rename;
        private bool _checkFormat;
        private Quality _quality;

        public WistiaDownloader(SiteClient siteClient, string courseName)
        {
            _siteClient = siteClient;
            _courseName = courseName.GetSafeFilename();
        }

        public async Task<bool> Download(List<LecturePage> lecturePageList, Arguments arguments)
        {
            if (lecturePageList == null || arguments == null || lecturePageList.Count == 0)
            {
                return false;
            }

            _rename = arguments.Rename;
            _quality = arguments.QualitySetting;
            _checkFormat = arguments.CheckFormats;

            int playlistTotal = lecturePageList.Count;
            var sectionCounter = 1;
            string currentSection = lecturePageList[0].SectionName;

            for (int index = arguments.StartingPosition; index < lecturePageList.Count; index++)
            {
                LecturePage lecturePage = lecturePageList[index];

                if (currentSection != lecturePage.SectionName)
                {
                    currentSection = lecturePage.SectionName;
                    sectionCounter++;
                }

                string sectionPath = Path.Combine(AppContext.BaseDirectory, _courseName,
                    $"{sectionCounter} - {lecturePage.SectionName}");

                if (!Directory.Exists(sectionPath))
                {
                    Directory.CreateDirectory(sectionPath);
                }

                Console.WriteLine($"\n[download] Downloading {index + 1} of {playlistTotal}");

                string lectureHtml = await _siteClient.Get(lecturePage.Url);
                Lecture lecture = _lectureParser.GetLectureLinks(lectureHtml);

                await DownloadLectureFilesNew(lecture, sectionPath);
            }

            return true;
        }

        public async Task Download(LecturePage lecturePage, Arguments arguments)
        {
            await Download(new List<LecturePage> { lecturePage }, arguments);
        }

        private async Task DownloadLectureFilesNew(Lecture lecture, string sectionPath)
        {
            if (lecture.WistiaId != null || lecture.WistiaId != "")
            {
                var t = await DownloadWistiaVideo(lecture.WistiaId, sectionPath);
            }
        }

        private async Task<bool> DownloadWistiaVideo(string wistiaId, string sectionPath)
        {
            string wistiaJson = await SimpleGet($"https://fast.wistia.net/embed/medias/{wistiaId}.json");


            JObject wistiaJObject = JObject.Parse(wistiaJson);

            if (_checkFormat)
            {
                DisplayFormats(wistiaJObject);
                return false;
            }

            var t = wistiaJObject.ToString();

            if (wistiaJObject?["media"] == null) return false;

            var downloadInfo = new DownloadInfo
            {
                FileName = wistiaJObject["media"]["name"].ToString()
            };

            Quality localQuality = _quality;

            switch (localQuality)
            {
                case Quality.Sd:
                    downloadInfo.Url = ParseUrlByQuality(wistiaJObject, "md_mp4_video", "540p");
                    break;
                case Quality.Hd:
                    downloadInfo.Url = ParseUrlByQuality(wistiaJObject, "hd_mp4_video", "720p");
                    break;
                case Quality.FullHd:
                    downloadInfo.Url = ParseUrlByQuality(wistiaJObject, "hd_mp4_video", "1080p");
                    break;
                case Quality.Original:
                    downloadInfo.Url = ParseUrlByQuality(wistiaJObject, "original", "Original file");
                    break;
            }

            return true;
        }

        private void DisplayFormats(JObject json)
        {
            var t = json.ToString();
            var assets = ParseAssets(json).ToList().AsReadOnly();
            Console.WriteLine("Format Name | Extension | Resolution | Other");

            int typeSpace = assets.Max(x => x.Type.Length) + 2;
            int extSpace = assets.Max(x => x.Extension.Length) + 2;
            int resolutionSpace = assets.Max(x => x.Resolution.Length) + 2;

            foreach (Format format in assets)
            {
                string formatString =
                        $"{format.Type}{' '.Repeat(typeSpace - format.Type.Length)} {format.Extension}{' '.Repeat(extSpace - format.Type.Length)} {format.Resolution}{' '.Repeat(resolutionSpace - format.Type.Length)} {format.Bitrate} {format.Container} Container {format.Codec} {format.Size}";

                Console.WriteLine(formatString);
            }
        }

        private static IEnumerable<Format> ParseAssets(JObject json)
        {
            JToken assets = json["media"]["assets"];

            List<Format> formatList = new List<Format>();

            foreach (JToken asset in assets)
            {
                var format = new Format
                {
                    Type = TryGetJsonKey(asset, "type", "?") + "-",
                    Codec = TryGetJsonKey(asset, "codec", "?"),
                    Bitrate = TryGetJsonKey(asset, "bitrate", "?") + "k",
                    Extension = TryGetJsonKey(asset, "ext", "?"),
                    Container = TryGetJsonKey(asset, "container", "?")
                };

                if (format.Extension == "jpg") //think of a better way
                {
                    continue;
                }

                string height = TryGetJsonKey(asset, "height", "?");
                string width = TryGetJsonKey(asset, "width", "?");
                format.Resolution = $"{width}x{height}";

                format.Codec += "@" + TryGetJsonKey(asset, "opt_vbitrate", "?") + "k";

                ByteSize sizeInBytes = ByteSize.FromBytes(TryGetJsonKey(asset, "size", 0D));
                double sizeRounded = Math.Round(sizeInBytes.LargestWholeNumberValue, 2);

                format.Size = sizeRounded + sizeInBytes.LargestWholeNumberSymbol;

                int typeCount = formatList.Count(x => x.Type.Substring(0, x.Type.Length - 1) == format.Type);
                format.Type += typeCount == 0 ? 0 : typeCount;

                formatList.Add(format);
            }

            return formatList;
        }

        private class Format
        {
            public string Type { get; set; }
            public string Codec { get; set; }
            public string Bitrate { get; set; }
            public string Resolution { get; set; }
            public string Extension { get; set; }
            public string Container { get; set; }
            public string Size { get; set; }
        }

        /*private async Task DownloadLectureFiles(Lecture lecture, string sectionPath) //should break up
        {
            if (lecture.WistiaId?["media"] != null)
            {
                var downloadInfo = new DownloadInfo
                {
                    FileName = lecture.WistiaId["media"]["name"].ToString()
                };

                List<int> qualitiesUsed = new List<int>
                {
                    (int)_quality
                };

                var attempts = 1;
                Quality originalQuality = _quality;

                while (downloadInfo.Url == null && attempts < 5)
                {
                    switch (_quality)
                    {
                        case Quality.Sd:
                            downloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "md_mp4_video", "540p");
                            break;
                        case Quality.Hd:
                            downloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "hd_mp4_video", "720p");
                            break;
                        case Quality.FullHd:
                            downloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "hd_mp4_video", "1080p");
                            break;
                        default:
                            downloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "original", "Original file");
                            break;
                    }

                    if (downloadInfo.Url == null)
                    {
                        Console.WriteLine("[download] Failed to find stream at chosen resolution, falling back to alternate stream");

                        if (!qualitiesUsed.Contains(1)) // there's probably a better way to do this but i cant be bothered, eventually this trash will be rewritten from the start anyway
                        {
                            _quality = (Quality) 1;
                            qualitiesUsed.Add(1);
                        }
                        else if (!qualitiesUsed.Contains(2))
                        {
                            _quality = (Quality) 2;
                            qualitiesUsed.Add(2);
                        }
                        else if (!qualitiesUsed.Contains(3))
                        {
                            _quality = (Quality) 3;
                            qualitiesUsed.Add(3);
                        }
                        else if (!qualitiesUsed.Contains(4))
                        {
                            _quality = (Quality)4;
                            qualitiesUsed.Add(4);
                        }
                    }

                    attempts++;
                }

                _quality = originalQuality;

                if (downloadInfo.Url == null)
                {
                    Console.WriteLine("[download] Failed to find valid stream");
                    return;
                }

                Console.WriteLine($"[download] Downloading {downloadInfo.FileName}");

                if (!Regex.IsMatch(downloadInfo.FileName, @"^\d+\s*-\s*"))
                {
                    downloadInfo.FileName = $"{_index + 1} - " + downloadInfo.FileName;
                }

                string saveLocation = Path.Combine(sectionPath, downloadInfo.FileName);

                var renameIndex = 1;

                while (File.Exists(saveLocation) && _rename)
                {
                    if (Regex.IsMatch(downloadInfo.FileName, @"^\(\d+\)"))
                    {
                        downloadInfo.FileName = Regex.Replace(downloadInfo.FileName, @"^\(\d+\)", $"({renameIndex})");
                    }
                    else
                    {
                        downloadInfo.FileName = $"({renameIndex}) " + downloadInfo.FileName;
                    }

                    saveLocation = Path
                        .Combine(sectionPath, downloadInfo.FileName);

                    renameIndex++;
                }

                if (File.Exists(saveLocation) && !_rename)
                {
                    Console.WriteLine("[download] File already exists");
                }
                else
                {
                    using (var videoWebClient = new WebClient())
                    {
                        videoWebClient.DownloadProgressChanged += DownloadProgressChangedHandler;
                        videoWebClient.DownloadFileCompleted += DownloadFileCompletedHandler;

                        await videoWebClient.DownloadFileTaskAsync(new Uri(downloadInfo.Url), saveLocation);
                    }
                }
            }

            foreach (Lecture.Extra lectureExtra in lecture.Extras)
            {
                string saveLocation = Path
                    .Combine(sectionPath, lectureExtra.FileName);

                Console.WriteLine($"[download] Downloading {lectureExtra.FileName}");

                var renameIndex = 1;

                while (File.Exists(saveLocation) && _rename)
                {
                    if (Regex.IsMatch(lectureExtra.FileName, @"^\(\d+\)"))
                    {
                        lectureExtra.FileName = Regex.Replace(lectureExtra.FileName, @"^\(\d+\)", $"({renameIndex})");
                    }
                    else
                    {
                        lectureExtra.FileName = $"({renameIndex}) " + lectureExtra.FileName;
                    }

                    saveLocation = Path
                        .Combine(sectionPath, lectureExtra.FileName);

                    renameIndex++;
                }

                if (File.Exists(saveLocation) && !_rename) // this doesn't feel right
                {
                    Console.WriteLine("[download] File already exists");
                }
                else
                {
                    using (var extraWebClient = new WebClient())
                    {
                        extraWebClient.DownloadFile(lectureExtra.Url, saveLocation);
                    }
                }
            }

            if (lecture.Text != null)
            {
                string saveLocation = Path
                    .Combine(sectionPath, lecture.Heading);

                var renameIndex = 1;

                while (File.Exists(saveLocation) && _rename)
                {
                    if (Regex.IsMatch(lecture.Heading, @"^\(\d+\)"))
                    {
                        lecture.Heading = Regex.Replace(lecture.Heading, @"^\(\d+\)", $"({renameIndex})");
                    }
                    else
                    {
                        lecture.Heading = $"({renameIndex}) " + lecture.Heading;
                    }

                    saveLocation = Path
                        .Combine(sectionPath, lecture.Heading);

                    renameIndex++;
                }

                if (File.Exists(saveLocation) && !_rename) // this doesn't feel right
                {
                    Console.WriteLine("[download] File already exists");
                }
                else
                {
                    File.Create(saveLocation).Close();
                    File.WriteAllText(saveLocation, lecture.Text);
                }
            }

            await Task.Delay(700);
        }*/

        private static string ParseUrlByQuality(JObject json, string type, string resolution)
        {
            return json["media"]["assets"]
                .Where(x => x["type"].ToString() == type)
                .Where(x => x["display_name"].ToString() == resolution)
                .Select(x => x["url"].ToString()).FirstOrDefault();
        }

        private void DownloadProgressChangedHandler(object sender, DownloadProgressChangedEventArgs args)
        {
            _downloadCounter++;

            if (_downloadCounter % 500 == 0)
            {
                string downloaded = ((args.BytesReceived / 1024f) / 1024f).ToString("#0.##");
                string total = ((args.TotalBytesToReceive / 1024f) / 1024f).ToString("#0.##");

                Console.Write($"\r[download] {downloaded}MB of {total}MB ({args.ProgressPercentage}%)");
            }
        }

        private static void DownloadFileCompletedHandler(object sender, AsyncCompletedEventArgs args)
        {
            int currentCursorPosition = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentCursorPosition);
            Console.Write("\r[download] Download complete\n");
        }
    }
}