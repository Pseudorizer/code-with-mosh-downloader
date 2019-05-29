using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codeWithMoshDownloader
{
    public class SiteClient
    {
        private readonly CookieContainer _cookieContainer;
        private readonly HttpClient _httpClient;

        public SiteClient(Uri baseUri)
        {
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = _cookieContainer};
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = baseUri
            };

            _httpClient.DefaultRequestHeaders.Add("Host", baseUri.Host); //should move this out of constructor
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:59.0) Gecko/20100101 Firefox/59.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-us,en;q=0.5");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _httpClient.DefaultRequestHeaders.Add("Connection", "close");
        }

        public void SetCookies(CookieCollection cookies)
        {
            _cookieContainer.Add(cookies);
        }

        public async Task<string> Get(string url)
        {
            string html;

            using (HttpResponseMessage getResponseMessage = await _httpClient.GetAsync(url))
            {
                Stream getResponseStream = await getResponseMessage.Content.ReadAsStreamAsync();
                html = await DecompressGZipStream(getResponseStream);
            }

            return html;
        }

        private static async Task<string> DecompressGZipStream(Stream stream)
        {
            using (var gZipStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (var streamReader = new StreamReader(gZipStream, Encoding.UTF8))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
        }
    }
}