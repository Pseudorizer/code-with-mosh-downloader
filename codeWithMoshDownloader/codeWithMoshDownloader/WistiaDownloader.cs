using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private string _quality;
        private int _currentItemIndex = 1;

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

            var sectionCounter = 1;
            string currentSection = lecturePageList[0].SectionName;

            for (int index = arguments.StartingPosition; index < lecturePageList.Count; index++)
            {
                LecturePage lecturePage = lecturePageList[index];

                if (currentSection != lecturePage.SectionName)
                {
                    currentSection = lecturePage.SectionName;
                    sectionCounter++;
                    _currentItemIndex = 1;
                }

                string sectionPath = Path.Combine(AppContext.BaseDirectory, _courseName, AddIndex(lecturePage.SectionName, sectionCounter));

                if (!Directory.Exists(sectionPath))
                {
                    Directory.CreateDirectory(sectionPath);
                }

                Console.WriteLine($"\n[download] Downloading {index + 1} of {lecturePageList.Count}");

                string lectureHtml = await _siteClient.Get(lecturePage.Url);
                Lecture lecture = _lectureParser.GetLectureLinks(lectureHtml);

                await DownloadLectureFilesNew(lecture, sectionPath);
                _currentItemIndex++;
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
                var t = await DownloadWistiaVideo(lecture.WistiaId, lecture.EmbeddedVideo.Url, sectionPath);
            }
        }

        private async Task<bool> DownloadWistiaVideo(string wistiaId, string embeddedVideoUrl, string sectionPath)
        {
            string wistiaJson = await SimpleGet($"https://fast.wistia.net/embed/medias/{wistiaId}.json");


            JObject wistiaJObject = JObject.Parse(wistiaJson);

            if (_checkFormat)
            {
                VideoStreamFormats.DisplayFormats(wistiaJObject);
                return false;
            }

            var t = wistiaJObject.ToString();

            if (wistiaJObject["media"] == null) return false;

            var downloadInfo = new DownloadInfo
            {
                FileName = wistiaJObject["media"]["name"].Value<string>().Trim()
            };

            string qualityTemp = _quality;

            if (VideoStreamFormats.TryGetFormat(wistiaJObject, qualityTemp, out VideoFormat format))
            {
                downloadInfo.Url = format.Url;
                downloadInfo.FileSize = long.Parse(format.Size);
            }
            else if (qualityTemp != "original" && VideoStreamFormats.TryGetFormat(wistiaJObject, "original", out VideoFormat originalFormat))
            {
                downloadInfo.Url = originalFormat.Url;
                downloadInfo.FileSize = long.Parse(originalFormat.Size);
            }
            else if (embeddedVideoUrl != null)
            {
                downloadInfo.Url = embeddedVideoUrl;
            }
            else
            {
                Console.WriteLine("[download] video download failed, no valid links found");
                return false;
            }

            if (downloadInfo.FileSize == null)
            {
                return await DownloadFile(downloadInfo.Url, downloadInfo.FileName, sectionPath);
            }

            return await DownloadFile(downloadInfo, sectionPath);
        }

        private async Task<bool> DownloadFile(DownloadInfo downloadInfo, string sectionPath) // these method names are terrible
        {
            downloadInfo.FileName = AddIndex(downloadInfo.FileName, _currentItemIndex);
            string filePath = Path.Combine(sectionPath, downloadInfo.FileName);

            if (File.Exists(filePath) && !_rename)
            {
                long fileSize = new FileInfo(filePath).Length;

                if (fileSize == downloadInfo.FileSize)
                {
                    Console.WriteLine("[download] file already exists");
                    return true;
                }
            }

            return await DownloadClient(downloadInfo.Url, filePath);
        }

        private async Task<bool> DownloadFile(string url, string filename, string sectionPath)
        {
            filename = AddIndex(filename, _currentItemIndex);
            string filePath = Path.Combine(sectionPath, filename);

            if (File.Exists(filePath) && !_rename)
            {
                Console.WriteLine("[download] file already exists");
                return true;
            }

            return await DownloadClient(url, filePath);
        }

        private async Task<bool> DownloadClient(string url, string filepath)
        {
            using (var webClient = new WebClient())
            {
                var result = false;
                webClient.DownloadProgressChanged += DownloadProgressChangedHandler;

                webClient.DownloadDataCompleted += (sender, args) =>
                {
                    int currentCursorPosition = Console.CursorTop;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, currentCursorPosition);

                    if (args.Error != null)
                    {
                        Console.Write("\r[download] Download failed\n");
                    }
                    else if (!args.Cancelled)
                    {
                        result = true;
                        Console.Write("\r[download] Download complete\n");
                    }
                    else
                    {
                        Console.Write("\r[download] something else\n");
                    }
                };

                await webClient.DownloadFileTaskAsync(new Uri(url), filepath);

                return result;
            }
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