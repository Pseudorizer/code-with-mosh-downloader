using System;
using System.Collections.Generic;
using System.Linq;
using codeWithMoshDownloader.Models;
using HtmlAgilityPack;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    public class Parser
    {
        private readonly HtmlDocument _htmlDocument = new HtmlDocument();

        public List<LecturePage> GetPlaylistItems(string pageContent)
        {
            _htmlDocument.LoadHtml(pageContent);

            HtmlNodeCollection sections = _htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'course-section')]");

            var lectureList = new List<LecturePage>();

            foreach (HtmlNode section in sections) // some crazy linq gets recommended here
            {
                string sectionName = section.SelectSingleNode(".//div[contains(@class, 'section-title')]").ChildNodes
                    .Where(x => x.Name == "#text" && x.InnerText.Trim().Length > 0)
                    .Select(y => y.InnerText.Trim()).FirstOrDefault()
                    ?.Split(" (")[0].Trim().GetSafeFilename(); //replace with regex...

                foreach (HtmlNode listItem in section.SelectNodes(".//li[contains(@class, 'section-item')]"))
                {
                    lectureList.Add(new LecturePage
                    {
                        SectionName = sectionName,
                        Url = listItem.SelectSingleNode(".//a[contains(@class, 'item')]").Attributes["href"]
                            .Value
                    });
                }
            }

            return lectureList;
        }

        public Lecture GetLectureLinks(string pageContent)
        {
            _htmlDocument.LoadHtml(pageContent);

            var lecture = new Lecture();

            if (TryGetNode(_htmlDocument, "//div[contains(@class, 'attachment-wistia-player')]", out HtmlNode idNode))
            {
                lecture.WistiaId = idNode.Attributes["data-wistia-id"].Value;
                Console.WriteLine($"[Wistia] {lecture.WistiaId}: Grabbing JSON");
            }

            if (TryGetNode(_htmlDocument, "//div[@class='video-options']//a", out HtmlNode embeddedVideoNode))
            {
                lecture.EmbeddedVideo = new EmbeddedVideo
                {
                    Url = embeddedVideoNode.Attributes["href"].Value,
                    FileName = embeddedVideoNode.Attributes["data-x-origin-download-name"].Value
                };
            }

            lecture.TextAreas.AddRange(GetTextAreas());

            HtmlNodeCollection attachments =
                _htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'lecture-attachment')]");

            foreach (HtmlNode attachmentNode in attachments.Where(x => !x.HasClass("lecture-attachment-type-video")))
            {
                lecture.Extras.AddRange(GetEmbeddedExtras(attachmentNode, lecture));

                lecture.Extras.AddRange(GetLectureExtras(attachmentNode, lecture));
            }

            return lecture;
        }

        private IEnumerable<LectureExtra> GetEmbeddedExtras(HtmlNode attachmentNode, Lecture lecture)
        {
            HtmlNodeCollection embedNodes = attachmentNode.SelectNodes(
                "./div[@class='row attachment-pdf-embed']/div/div[@class='wrapper']/div");

            foreach (HtmlNode embedNode in embedNodes)
            {
                var embedExtra = new LectureExtra();

                string id = embedNode.Attributes["data-pdfviewer-id"].Value;

                embedExtra.Url = "https://www.filepicker.io/api/file/" + id;

                if (TryGetNode(_htmlDocument, "//div[@class='row attachment-pdf-embed']/div/div[@class='label']",
                    out HtmlNode node))
                {
                    embedExtra.FileName = node.InnerText.Trim().GetSafeFilename();
                }

                if (lecture.Extras.Any(x => x.Url == embedExtra.Url)) continue;

                yield return embedExtra;
            }
        }

        private IEnumerable<TextArea> GetTextAreas()
        {
            if (!TryGetNodes(_htmlDocument, "//div[@class='lecture-text-container']",
                out HtmlNodeCollection textNodes)) yield break;

            foreach (HtmlNode textNode in textNodes)
            {
                if (!TryGetNode(_htmlDocument, "//h2[@id='lecture_heading']", out HtmlNode headerNode)) continue;

                var textArea = new TextArea
                {
                    FileName = headerNode.InnerText.Trim()
                                   .Replace("&nbsp;", "")
                                   .Trim()
                               + ".html",
                    Html = textNode.InnerHtml
                };

                yield return textArea;
            }
        }

        private static IEnumerable<LectureExtra> GetLectureExtras(HtmlNode attachmentNode, Lecture lecture)
        {
            HtmlNodeCollection downloadNodes = attachmentNode.SelectNodes(".//a[contains(@class, 'download')]");

            foreach (HtmlNode downloadNode in downloadNodes)
            {
                var extra = new LectureExtra
                {
                    Url = downloadNode.Attributes["href"].Value,
                    FileName = downloadNode.Attributes["data-x-origin-download-name"].Value.Trim().GetSafeFilename()
                };

                if (lecture.Extras.Any(x => x.Url == extra.Url)) continue;

                yield return extra;
            }
        }

        public string GetCourseName(string pageContent)
        {
            _htmlDocument.LoadHtml(pageContent);

            return _htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'course-sidebar')]//h2").InnerText;
        }

        public string GetSectionNameFromLecture(string pageContent)
        {
            _htmlDocument.LoadHtml(pageContent);

            string lectureName = _htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'lecture-content')]//h2[@class='section-title']")
                .InnerText
                .Replace("&nbsp;", "")
                .Trim();

            HtmlNodeCollection sectionNodeList = _htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'course-section')]");

            return SearchForSectionNameFromLecture(sectionNodeList, lectureName);
        }

        private static string SearchForSectionNameFromLecture(HtmlNodeCollection sectionNodeList, string lectureName)
        {
            foreach (HtmlNode sectionNode in sectionNodeList) // needs work, can be extracted
            {
                foreach (HtmlNode listItemNode in sectionNode.SelectNodes("./ul/li"))
                {
                    string title = listItemNode.SelectSingleNode("./a/div/span[@class='lecture-name']")
                        .InnerText
                        .Trim()
                        .Split("\n")[0];

                    if (title != lectureName) continue;

                    return sectionNode.SelectSingleNode("./div")
                        .InnerText
                        .Replace("&nbsp;", "")
                        .Split(" (")[0]
                        .Trim();
                }
            }

            return "Unknown";
        }
    }
}