using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class Downloader
    {
        private readonly Parser _lectureParser = new Parser();
        private readonly SiteClient _siteClient;
        private readonly string _courseName;
        private int _downloadCounter;
        private bool _checkFormat;
        private bool _force;
        private string _quality;
        private int _currentItemIndex = 1;

        public Downloader(SiteClient siteClient, string courseName)
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
            _force = arguments.Force;
            _checkFormat = arguments.CheckFormats;

            ReadOnlyCollection<string> sectionList = lecturePageList
                .GroupBy(x => x.SectionName)
                .Select(x => x.First().SectionName)
                .ToList().AsReadOnly();

            ReadOnlyCollection<ReadOnlyCollection<LecturePage>> sectionsGrouped = lecturePageList
                .GroupBy(x => x.SectionName)
                .Select(x => x.ToList().AsReadOnly())
                .ToList().AsReadOnly();

            string currentSection = string.Empty;

            for (int index = arguments.StartingPosition; index < lecturePageList.Count; index++)
            {
                LecturePage lecturePage = lecturePageList[index];

                int sectionIndex = sectionList.IndexOf(lecturePage.SectionName) + 1;

                _currentItemIndex = sectionsGrouped
                    .Select(x => x.IndexOf(lecturePage))
                    .First(x => x != -1) + 1;

                if (currentSection != lecturePage.SectionName && currentSection != "")
                {
                    currentSection = lecturePage.SectionName;
                    _currentItemIndex = 1;
                }
                else
                {
                    currentSection = lecturePage.SectionName;
                }

                string sectionPath = Path.Combine(AppContext.BaseDirectory, _courseName, AddIndex(lecturePage.SectionName, sectionIndex));

                if (!Directory.Exists(sectionPath))
                {
                    Directory.CreateDirectory(sectionPath);
                }

                Console.WriteLine($"\n[download] Downloading {index + 1} of {lecturePageList.Count}");

                string lectureHtml = await _siteClient.Get(lecturePage.Url);
                Lecture lecture = _lectureParser.GetLectureLinks(lectureHtml);

                await DownloadLecture(lecture, sectionPath);
                _currentItemIndex++;
            }

            return true;
        }

        public async Task Download(LecturePage lecturePage, Arguments arguments)
        {
            await Download(new List<LecturePage> { lecturePage }, arguments);
        }

        private async Task DownloadLecture(Lecture lecture, string sectionPath)
        {
            var videoDownloadResult = false;

            if (lecture.WistiaId == "" && lecture.EmbeddedVideo != null)
            {
                videoDownloadResult = await DownloadFile(lecture.EmbeddedVideo, sectionPath);
            }
            else if (lecture.WistiaId != "")
            {
                videoDownloadResult = await DownloadWistiaVideo(lecture.WistiaId, sectionPath);
            }

            if (_checkFormat)
            {
                return;
            }

            if (videoDownloadResult == false && lecture.WistiaId != "" && lecture.EmbeddedVideo != null)
            {
                await DownloadFile(lecture.EmbeddedVideo, sectionPath);
            }

            foreach (LectureExtra lectureExtra in lecture.Extras)
            {
                await DownloadFile(lectureExtra, sectionPath);
            }

            foreach (IText lectureTextArea in lecture.TextContentList)
            {
                lectureTextArea.FileName = AddIndex(lectureTextArea.FileName, _currentItemIndex);

                string filePath = Path.Combine(sectionPath, lectureTextArea.FileName);

                File.Create(filePath).Close();
                
                File.WriteAllText(filePath, lectureTextArea.Html);
            }
        }

        private async Task<bool> DownloadWistiaVideo(string wistiaId, string sectionPath)
        {
            string wistiaJson = await SimpleGet($"https://fast.wistia.net/embed/medias/{wistiaId}.json");

            JObject wistiaJObject = JObject.Parse(wistiaJson);

            if (_checkFormat)
            {
                VideoStreamFormats.DisplayFormats(wistiaJObject);
                return true;
            }

            var downloadInfo = new WistiaDownloadInfo();

            if (TryGetJsonValueByJPath(wistiaJObject, "$.media.name", out string filename))
            {
                downloadInfo.FileName = filename;
            }

            bool usingResolution = Regex.IsMatch(_quality, @"\d+x\d+");

            if (!usingResolution && VideoStreamFormats.TryGetFormat(wistiaJObject, _quality, out VideoFormat format))
            {
                downloadInfo.Url = format.Url;
                downloadInfo.FileSize = long.Parse(format.Size);
            }
            else if (usingResolution && VideoStreamFormats.TryGetFormatByResolution(wistiaJObject, _quality, out VideoFormat formatByResolution))
            {
                downloadInfo.Url = formatByResolution.Url;
                downloadInfo.FileSize = long.Parse(formatByResolution.Size);
            }
            else if (_quality != "original" && VideoStreamFormats.TryGetFormat(wistiaJObject, "original", out VideoFormat originalFormat))
            {
                downloadInfo.Url = originalFormat.Url;
                downloadInfo.FileSize = long.Parse(originalFormat.Size);
            }
            else
            {
                Console.WriteLine("[download] Video download failed, no valid links found");
                return false;
            }

            return await DownloadFile(downloadInfo, sectionPath);
        }

        private async Task<bool> DownloadFile(IDownload downloadInfo, string sectionPath)
        {
            if (downloadInfo.Url == null || downloadInfo.FileName == null)
            {
                Console.WriteLine("[download] Download failed");
                return false;
            }

            downloadInfo.FileName = AddIndex(downloadInfo.FileName, _currentItemIndex).GetSafeFilename();

            Console.WriteLine($"[download] Downloading {downloadInfo.FileName}");

            string filePath = Path.Combine(sectionPath, downloadInfo.FileName);

            if (File.Exists(filePath) && !_force)
            {

                Console.WriteLine("[download] File already exists");
                return true;
            }

            return await DownloadClient(downloadInfo.Url, filePath);
        }

        private async Task<bool> DownloadFile(WistiaDownloadInfo wistiaDownloadInfo, string sectionPath) // these method names are terrible
        {
            wistiaDownloadInfo.FileName = AddIndex(wistiaDownloadInfo.FileName, _currentItemIndex).GetSafeFilename();
            string filePath = Path.Combine(sectionPath, wistiaDownloadInfo.FileName);

            if (File.Exists(filePath) && !_force)
            {
                long fileSize = new FileInfo(filePath).Length;

                if (fileSize == wistiaDownloadInfo.FileSize)
                {
                    Console.WriteLine("[download] File already exists");
                    return true;
                }
            }

            Console.WriteLine($"[download] Downloading {wistiaDownloadInfo.FileName}");

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