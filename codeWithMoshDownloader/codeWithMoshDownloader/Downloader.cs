using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using codeWithMoshDownloader.Models;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    public class Downloader
    {
        private readonly Parser _lectureParser = new Parser();
        private readonly SiteClient _siteClient;
        private readonly string _courseName;
        private int _downloadCounter;

        public Downloader(SiteClient siteClient, string courseName)
        {
            _siteClient = siteClient;
            _courseName = courseName.GetSafeFilename();
        }

        public async Task DownloadPlaylist(List<Section> sectionsList, bool rename)
        {
            int playlistTotal = CountListOfLists(sectionsList);
            var playlistDownloadCounter = 1;

            foreach (Section section in sectionsList)
            {
                string sectionPath = Path.Combine(AppContext.BaseDirectory, _courseName, section.SectionName);

                if (!Directory.Exists(sectionPath))
                {
                    Directory.CreateDirectory(sectionPath);
                }

                for (var index = 0; index < section.UrlList.Count; index++)
                {
                    Console.WriteLine($"\n[download] Downloading {playlistDownloadCounter} of {playlistTotal}");

                    string lectureUrl = section.UrlList[index];
                    string lectureHtml = await _siteClient.Get(lectureUrl);
                    Lecture lecture = await _lectureParser.GetLectureLinks(lectureHtml);

                    await DownloadLecture(lecture, section.SectionName, index, rename);
                    playlistDownloadCounter++;
                }
            }
        }

        public async Task DownloadLecture(Lecture lecture, string sectionName, int index, bool rename)
        {
            if (lecture.VideoJson?["media"] != null)
            {
                var downloadInfo = new DownloadInfo
                {
                    Url = lecture.VideoJson["media"]["assets"][0]["url"].ToString(),
                    FileName = lecture.VideoJson["media"]["name"].ToString().GetSafeFilename()
                };

                Console.WriteLine($"[download] Downloading {downloadInfo.FileName}");

                if (!Regex.IsMatch(downloadInfo.FileName, @"^\d+\s*-\s*"))
                {
                    downloadInfo.FileName = $"{index + 1} - " + downloadInfo.FileName;
                }

                string saveLocation = Path.Combine(AppContext.BaseDirectory, _courseName, sectionName,
                    downloadInfo.FileName);

                var renameIndex = 1;

                while (File.Exists(saveLocation) && rename)
                {
                    if (Regex.IsMatch(downloadInfo.FileName, @"^\(\d+\)"))
                    {
                        downloadInfo.FileName = Regex.Replace(downloadInfo.FileName, @"^\(\d+\)", $"({renameIndex})");
                    }
                    else
                    {
                        downloadInfo.FileName = $"({renameIndex}) " + downloadInfo.FileName;
                    }

                    Console.WriteLine($"renaming to {downloadInfo.FileName}");

                    saveLocation = Path
                        .Combine(AppContext.BaseDirectory, _courseName, sectionName, downloadInfo.FileName);

                    renameIndex++;
                }

                if (File.Exists(saveLocation) && !rename)
                {
                    Console.WriteLine("[download] File already exists");
                }
                else
                {
                    using (var videoWebClient = new WebClient())
                    {
                        videoWebClient.DownloadProgressChanged += DownloadProgressChangedHandler;

                        await videoWebClient.DownloadFileTaskAsync(new Uri(downloadInfo.Url), saveLocation);
                    }
                }
            }

            foreach (Lecture.Extra lectureExtra in lecture.Extras)
            {
                string saveLocation = Path
                    .Combine(AppContext.BaseDirectory, _courseName, sectionName, lectureExtra.FileName);

                Console.WriteLine($"[download] Downloading {lectureExtra.FileName}");

                var renameIndex = 1;

                while (File.Exists(saveLocation) && rename)
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
                        .Combine(AppContext.BaseDirectory, _courseName, sectionName, lectureExtra.FileName);

                    renameIndex++;
                }

                if (File.Exists(saveLocation) && !rename) // this doesn't feel right
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

            await Task.Delay(700);
        }

        private void DownloadProgressChangedHandler(object sender, DownloadProgressChangedEventArgs args)
        {
            _downloadCounter++;

            if (_downloadCounter % 500 == 0)
            {
                string downloaded = ((args.BytesReceived / 1024f) / 1024f).ToString("#0.##");
                string total = ((args.TotalBytesToReceive / 1024f) / 1024f).ToString("#0.##");

                Console.Write($"[download] {downloaded}MB of {total}MB ({args.ProgressPercentage}%)\n");
            }
        }
    }
}