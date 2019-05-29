using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using codeWithMoshDownloader.Models;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    public class Parser
    {
        public List<Section> GetPlaylistItems(string pageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContent);

            HtmlNodeCollection sections = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'course-section')]");

            List<Section> sectionList = new List<Section>();

            foreach (HtmlNode section in sections)
            {
                var sectionObj = new Section();

                sectionObj.SectionName = section.SelectSingleNode(".//div[contains(@class, 'section-title')]").ChildNodes
                    .Where(x => x.Name == "#text" && x.InnerText.Trim().Length > 0)
                    .Select(y => y.InnerText.Trim()).FirstOrDefault()
                    ?.Split(" (")[0].Trim().GetSafeFilename(); //replace with regex...

                foreach (HtmlNode listItem in section.SelectNodes(".//li[contains(@class, 'section-item')]"))
                {
                    string url = listItem.SelectSingleNode(".//a[contains(@class, 'item')]").Attributes["href"].Value;
                    sectionObj.UrlList.Add(url);
                }

                sectionList.Add(sectionObj);
            }

            return sectionList;
        }

        public async Task<Lecture> GetLectureLinks(string pageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContent);

            var lecture = new Lecture();

            HtmlNode idNode =
                htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'attachment-wistia-player')]");

            if (idNode != null)
            {
                string id = idNode.Attributes["data-wistia-id"].Value;

                Console.WriteLine($"[Wistia] {id}: Grabbing JSON");

                lecture.VideoJson = JObject.Parse(await SimpleGet($"https://fast.wistia.net/embed/medias/{id}.json"));
            }

            HtmlNodeCollection attachments =
                htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'lecture-attachment')]");

            if (attachments.Count == 1 && idNode != null) return lecture;

            foreach (HtmlNode attachmentNode in attachments.Where(x => !x.HasClass("lecture-attachment-type-video")))
            {
                var extra = new Lecture.Extra();

                HtmlNode downloadNode = attachmentNode.SelectSingleNode(".//a[contains(@class, 'download')]");

                if (downloadNode == null) continue;

                extra.Url = downloadNode.Attributes["href"].Value;
                extra.FileName = downloadNode.Attributes["data-x-origin-download-name"].Value.GetSafeFilename();

                lecture.Extras.Add(extra);
            }

            return lecture;
        }

        public string GetCourseName(string pageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContent);

            return htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'course-sidebar')]//h2").InnerText;
        }

        public string GetSectionNameFromLecture(string pageContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContent);

            string lectureName = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'lecture-content')]//h2[@class='section-title']")
                .InnerText.Replace("&nbsp;", "").Trim();

            IEnumerable<HtmlNode> lectureListNode = htmlDocument.DocumentNode.SelectNodes("//li[contains(@class, 'section-item')]");
            // NO NO WTF COURSE-SECTION -> ALL LI ELEMENTS -> CHECK ALL LECTURE-NAMES -> IF MATCH TAKE COURSE-SECTION NODE AND EXTRACT SECTION NAME
            HtmlNode q = (from node in lectureListNode
                from descendant in node.Descendants()
                where descendant.Name == "#text"
                where descendant.InnerText.Split(" (")[0].Trim() == lectureName
                select descendant).FirstOrDefault();

            var e = q.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode;

            //var t = lectureName.SelectSingleNode(".//div[@class='section-title]").InnerText;
            return "";
        }
    }
}