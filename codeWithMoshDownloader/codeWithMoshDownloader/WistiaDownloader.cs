using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            bool videoDownloadResult;

            if (lecture.WistiaId == "")
            {
                videoDownloadResult = await DownloadFile(lecture.EmbeddedVideo, sectionPath);
            }
            else
            {
                videoDownloadResult = await DownloadWistiaVideo(lecture.WistiaId, sectionPath);
            }

            if (videoDownloadResult == false && lecture.WistiaId != "")
            {
                await DownloadFile(lecture.EmbeddedVideo, sectionPath);
            }

            foreach (LectureExtra lectureExtra in lecture.Extras)
            {
                await DownloadFile(lectureExtra, sectionPath);
            }

            foreach (TextArea lectureTextArea in lecture.TextAreas)
            {
                lectureTextArea.FileName = AddIndex(lectureTextArea.FileName, _currentItemIndex);

                string filePath = Path.Combine(sectionPath, lectureTextArea.FileName);

                File.Create(filePath).Close();
                
                File.WriteAllText(filePath, lectureTextArea.Html);
            }
        }

        // a good idea might be to check if the wistia is "" before calling this method, i could have an overload for just embedded stuff
        // and it would save a bit of time as it cuts out the get request and all the other checks and parsing
        private async Task<bool> DownloadWistiaVideo(string wistiaId, string sectionPath)
        {
            string wistiaJson = await SimpleGet($"https://fast.wistia.net/embed/medias/{wistiaId}.json");

            JObject wistiaJObject = JObject.Parse(wistiaJson);

            if (_checkFormat)
            {
                VideoStreamFormats.DisplayFormats(wistiaJObject);
                return false;
            }

            var downloadInfo = new WistiaDownloadInfo();

            if (TryGetJsonValueByJPath(wistiaJObject, "$.media.name", out string filename))
            {
                downloadInfo.FileName = filename;
            }

            if (VideoStreamFormats.TryGetFormat(wistiaJObject, _quality, out VideoFormat format))
            {
                downloadInfo.Url = format.Url;
                downloadInfo.FileSize = long.Parse(format.Size);
            }
            else if (_quality != "original" && VideoStreamFormats.TryGetFormat(wistiaJObject, "original", out VideoFormat originalFormat))
            {
                downloadInfo.Url = originalFormat.Url;
                downloadInfo.FileSize = long.Parse(originalFormat.Size);
            }
            else
            {
                Console.WriteLine("[download] video download failed, no valid links found");
                return false;
            }

            return await DownloadFile(downloadInfo, sectionPath);
        }

        private async Task<bool> DownloadFile(IDownload downloadInfo, string sectionPath)
        {
            if (downloadInfo.Url == null || downloadInfo.FileName == null)
            {
                Console.WriteLine("[download] download failed");
                return false;
            }

            downloadInfo.FileName = AddIndex(downloadInfo.FileName, _currentItemIndex);

            Console.WriteLine($"[download] downloading {downloadInfo.FileName}");

            string filePath = Path.Combine(sectionPath, downloadInfo.FileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine("[download] file already exists");
                return true;
            }

            return await DownloadClient(downloadInfo.Url, filePath);
        }

        private async Task<bool> DownloadFile(WistiaDownloadInfo wistiaDownloadInfo, string sectionPath) // these method names are terrible
        {
            wistiaDownloadInfo.FileName = AddIndex(wistiaDownloadInfo.FileName, _currentItemIndex);
            string filePath = Path.Combine(sectionPath, wistiaDownloadInfo.FileName);

            if (File.Exists(filePath))
            {
                long fileSize = new FileInfo(filePath).Length;

                if (fileSize == wistiaDownloadInfo.FileSize)
                {
                    Console.WriteLine("[download] file already exists");
                    return true;
                }
            }

            return await DownloadClient(wistiaDownloadInfo.Url, filePath);
        }

        private async Task<bool> DownloadClient(string url, string filepath)
        {
            using (var webClient = new WebClient())
            {
                var result = false;
                webClient.DownloadProgressChanged += DownloadProgressChangedHandler;

                webClient.DownloadFileCompleted += (sender, args) =>
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
                        Console.Write("\r[download] Download cancelled\n");
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
                var wistiaDownloadInfo = new WistiaDownloadInfo
                {
                    FileName = lecture.WistiaId["media"]["name"].ToString()
                };

                List<int> qualitiesUsed = new List<int>
                {
                    (int)_quality
                };

                var attempts = 1;
                Quality originalQuality = _quality;

                while (wistiaDownloadInfo.Url == null && attempts < 5)
                {
                    switch (_quality)
                    {
                        case Quality.Sd:
                            wistiaDownloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "md_mp4_video", "540p");
                            break;
                        case Quality.Hd:
                            wistiaDownloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "hd_mp4_video", "720p");
                            break;
                        case Quality.FullHd:
                            wistiaDownloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "hd_mp4_video", "1080p");
                            break;
                        default:
                            wistiaDownloadInfo.Url = ParseUrlByQuality(lecture.WistiaId, "original", "Original file");
                            break;
                    }

                    if (wistiaDownloadInfo.Url == null)
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

                if (wistiaDownloadInfo.Url == null)
                {
                    Console.WriteLine("[download] Failed to find valid stream");
                    return;
                }

                Console.WriteLine($"[download] Downloading {wistiaDownloadInfo.FileName}");

                if (!Regex.IsMatch(wistiaDownloadInfo.FileName, @"^\d+\s*-\s*"))
                {
                    wistiaDownloadInfo.FileName = $"{_index + 1} - " + wistiaDownloadInfo.FileName;
                }

                string saveLocation = Path.Combine(sectionPath, wistiaDownloadInfo.FileName);

                var renameIndex = 1;

                while (File.Exists(saveLocation) && _rename)
                {
                    if (Regex.IsMatch(wistiaDownloadInfo.FileName, @"^\(\d+\)"))
                    {
                        wistiaDownloadInfo.FileName = Regex.Replace(wistiaDownloadInfo.FileName, @"^\(\d+\)", $"({renameIndex})");
                    }
                    else
                    {
                        wistiaDownloadInfo.FileName = $"({renameIndex}) " + wistiaDownloadInfo.FileName;
                    }

                    saveLocation = Path
                        .Combine(sectionPath, wistiaDownloadInfo.FileName);

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

                        await videoWebClient.DownloadFileTaskAsync(new Uri(wistiaDownloadInfo.Url), saveLocation);
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
    }
}