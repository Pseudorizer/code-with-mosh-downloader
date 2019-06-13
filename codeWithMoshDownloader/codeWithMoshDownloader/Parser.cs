using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using codeWithMoshDownloader.Models;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
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

            lecture.TextContentList.AddRange(GetTextAreas());

            HtmlNodeCollection attachments =
                _htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'lecture-attachment')]");

            foreach (HtmlNode attachmentNode in attachments.Where(x => !x.HasClass("lecture-attachment-type-video")))
            {
                lecture.Extras.AddRange(GetEmbeddedExtras(attachmentNode, lecture));

                lecture.Extras.AddRange(GetLectureExtras(attachmentNode, lecture));

                lecture.TextContentList.Add(GetLectureQuiz(attachmentNode));
            }

            return lecture;
        }

        private IEnumerable<LectureExtra> GetEmbeddedExtras(HtmlNode attachmentNode, Lecture lecture)
        {
            HtmlNodeCollection embedNodes;

            if (TryGetNodes(attachmentNode, "./div[@class='row attachment-pdf-embed']/div/div[@class='wrapper']/div", out HtmlNodeCollection nodeCollection))
            {
                embedNodes = nodeCollection;
            }
            else
            {
                yield break;
            }

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
            HtmlNodeCollection downloadNodes;

            if (TryGetNodes(attachmentNode, ".//a[contains(@class, 'download')]", out HtmlNodeCollection nodeCollection))
            {
                downloadNodes = nodeCollection;
            }
            else
            {
                yield break;
            }

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

        private static Quiz GetLectureQuiz(HtmlNode attachmentNode)
        {
            HtmlNode quizNode;

            if (TryGetNode(attachmentNode, "./div", out HtmlNode nodes))
            {
                quizNode = nodes;
            }
            else
            {
                return new Quiz();
            }

            JObject answersJson = JObject.Parse(HttpUtility.HtmlDecode(quizNode.Attributes["data-data"].Value));
            JObject questionsJson = JObject.Parse(HttpUtility.HtmlDecode(quizNode.Attributes["data-schema"].Value));

            ReadOnlyCollection<string> answers = answersJson["answerKey"]
                .Children().Children().Children()
                .Select(x => x.Value<string>())
                .ToList().AsReadOnly();

            var questionProperties = questionsJson["properties"].Children().Children();

            List<QuizQuestion> quizQuestions = new List<QuizQuestion>();

            foreach (JToken questionProperty in questionProperties)
            {
                var quizQuestion = new QuizQuestion
                {
                    Title = questionProperty["title"].Value<string>()
                };

                quizQuestion.PotentialAnswers.AddRange(questionProperty["enum"].Select(x => x.Value<string>()));

                quizQuestions.Add(quizQuestion);
            }

            return new Quiz
            {
                FileName = "Quiz.html",
                Html = BuildQuizHtml(quizQuestions, answers)
            };
        }

        private static string BuildQuizHtml(IReadOnlyList<QuizQuestion> quizQuestions, IReadOnlyList<string> answers)
        {
            var html =
                "<style>\r\n    .spoiler {\r\n        color: black;\r\n        background-color: black;\r\n    }\r\n\r\n    .spoiler:hover {\r\n        background-color: white;\r\n    }\r\n</style>\n\n";

            for (var i = 0; i < quizQuestions.Count; i++)
            {
                QuizQuestion quizQuestion = quizQuestions[i];
                string quizAnswer = answers[i];

                html += $"<h3>{quizQuestion.Title}</h3>\n";

                foreach (string potentialAnswer in quizQuestion.PotentialAnswers)
                {
                    html += $"<p>{potentialAnswer}</p>\n";
                }

                html += $"\n<br><p>Answer</p>\n<p class=\"spoiler\">{quizAnswer}</p>\n<br>";
            }

            return html;
        }

        private class QuizQuestion
        {
            public string Title { get; set; }
            public List<string> PotentialAnswers { get; } = new List<string>();
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