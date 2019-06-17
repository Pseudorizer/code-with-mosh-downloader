using codeWithMoshDownloader.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    public class Downloader
    {
        private readonly SiteClient _siteClient;
        private readonly string _courseName;
        private int _downloadCounter;
        private bool _checkFormat;
        private bool _force;
        private bool _unZip;
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
            _unZip = arguments.UnZip;

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

                var parser = new Parser();

                string lectureHtml = await _siteClient.Get(lecturePage.Url);
                Lecture lecture = parser.GetLectureLinks(lectureHtml);

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

            foreach (GenericFile lectureExtra in lecture.Extras)
            {
                await DownloadFile(lectureExtra, sectionPath);
            }

            foreach (HtmlFile lectureTextArea in lecture.HtmlFiles)
            {
                lectureTextArea.FileName = AddIndex(lectureTextArea.FileName.GetSafeFilename(), _currentItemIndex);

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

        private async Task<bool> DownloadFile(GenericFile downloadInfo, string sectionPath)
        {
            if (downloadInfo.Url == null || downloadInfo.FileName == null)
            {
                Console.WriteLine("[download] Download failed");
                return false;
            }

            downloadInfo.FileName = AddIndex(downloadInfo.FileName, _currentItemIndex).GetSafeFilename();

            Console.WriteLine($"[download] Downloading {downloadInfo.FileName}");

            HttpResponseMessage fileHeadRequest = await SimpleHead(downloadInfo.Url);

            var downloadSize = fileHeadRequest.Content.Headers.ContentLength;

            string filePath = Path.Combine(sectionPath, downloadInfo.FileName);

            if (FileExists(filePath, downloadSize))
            {
                return true;
            }

            bool result = await DownloadClient(downloadInfo.Url, filePath);

            if (result)
            {
                result = VerifyDownload(filePath, downloadSize);
            }

            if (result && _unZip && Path.GetExtension(downloadInfo.FileName) == ".zip")
            {
                UnZipArchive(sectionPath, filePath);
            }

            return result;
        }

        private static bool VerifyDownload(string filePath, long? downloadSize)
        {
            long fileSize = new FileInfo(filePath).Length;

            if (downloadSize != fileSize)
            {
                Console.WriteLine("[download] Download failed\n");
                return false;
            }

            Console.Write("[download] Download complete\n");
            return true;
        }

        private bool FileExists(string filePath, long? downloadSize)
        {
            if (File.Exists(filePath) && !_force)
            {
                long fileSize = new FileInfo(filePath).Length;

                if (downloadSize == fileSize)
                {
                    Console.WriteLine("[download] File already exists");
                    return true;
                }
            }

            return false;
        }

        private void UnZipArchive(string sectionPath, string filePath)
        {
            Console.WriteLine("[download] Unzipping archive");

            string tempDirectory = Path.Combine(sectionPath, "temp");

            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            ZipFile.ExtractToDirectory(filePath, tempDirectory, true);

            foreach (string file in Directory.GetFiles(tempDirectory))
            {
                string filename = Path.GetFileName(file);
                string newFilename = Path.Combine(tempDirectory, AddIndex(filename, _currentItemIndex));

                if (!File.Exists(newFilename))
                {
                    File.Move(file, newFilename);
                }

                string newSectionPath = Path.Combine(sectionPath, AddIndex(filename, _currentItemIndex));

                if (!File.Exists(newSectionPath))
                {
                    File.Move(newFilename, newSectionPath);
                }
            }

            foreach (string directory in Directory.GetDirectories(tempDirectory))
            {
                string name = new DirectoryInfo(directory).Name;

                if (name == "__MACOSX")
                {
                    Directory.Delete(directory, true);
                    continue;
                }

                string newTempDirectoryName = Path.Combine(tempDirectory, AddIndex(name, _currentItemIndex));

                if (!Directory.Exists(newTempDirectoryName))
                {
                    Directory.Move(directory, newTempDirectoryName);
                }

                string newSectionPath = Path.Combine(sectionPath, AddIndex(name, _currentItemIndex));

                if (!Directory.Exists(newSectionPath))
                {
                    Directory.Move(newTempDirectoryName, newSectionPath);
                }
            }

            Directory.Delete(tempDirectory, true);

            File.Delete(filePath);
        }

        private async Task<bool> DownloadFile(WistiaDownloadInfo wistiaDownloadInfo, string sectionPath) // these method names are terrible
        {
            wistiaDownloadInfo.FileName = AddIndex(wistiaDownloadInfo.FileName, _currentItemIndex).GetSafeFilename();
            string filePath = Path.Combine(sectionPath, wistiaDownloadInfo.FileName);

            if (FileExists(filePath, wistiaDownloadInfo.FileSize))
            {
                return true;
            }

            Console.WriteLine($"[download] Downloading {wistiaDownloadInfo.FileName}");

            bool result = await DownloadClient(wistiaDownloadInfo.Url, filePath);

            if (result)
            {
                result = VerifyDownload(filePath, wistiaDownloadInfo.FileSize);
            }

            return result;
        }

        private async Task<bool> DownloadClient(string url, string filepath)
        {
            using (var webClient = new WebClient())
            {
                var result = false;
                webClient.DownloadProgressChanged += DownloadProgressChangedHandler;

                webClient.DownloadFileCompleted += (sender, args) =>
                {
                    ClearLine();

                    if (args.Error != null)
                    {
                        Console.Write("[download] Download failed\n");
                    }
                    else if (!args.Cancelled)
                    {
                        result = true;
                    }
                    else
                    {
                        Console.Write("[download] Download cancelled\n");
                    }
                };

                await webClient.DownloadFileTaskAsync(new Uri(url), filepath);

                return result;
            }
        }

        private void DownloadProgressChangedHandler(object sender, DownloadProgressChangedEventArgs args)
        {
            _downloadCounter++;

            if (_downloadCounter % 250 == 0)
            {
                string downloaded = ((args.BytesReceived / 1024f) / 1024f).ToString("#0.##");
                string total = ((args.TotalBytesToReceive / 1024f) / 1024f).ToString("#0.##");

                ClearLine();

                Console.Write($"[download] {downloaded}MB of {total}MB ({args.ProgressPercentage}%)");
            }
        }
    }
}