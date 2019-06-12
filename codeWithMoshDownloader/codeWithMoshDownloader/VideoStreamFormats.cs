using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ByteSizeLib;
using codeWithMoshDownloader.Models;
using Newtonsoft.Json.Linq;
using static codeWithMoshDownloader.Helpers;

namespace codeWithMoshDownloader
{
    public static class VideoStreamFormats
    {
        public static void DisplayFormats(JObject json)
        {
            ReadOnlyCollection<VideoFormat> assets = ParseAssets(json).ToList().AsReadOnly();

            int typeSpace = assets.Max(x => x.Type.Length) + 2;
            int extSpace = assets.Max(x => x.Extension.Length) + 2;
            int resolutionSpace = assets.Max(x => x.Resolution.Length) + 2;
            int bitrateSpace = assets.Max(x => x.Bitrate.Length) + 2;
            int containerSpace = assets.Max(x => x.Container.Length) + 2; // because of the container text
            int codecSpace = assets.Max(x => x.Codec.Length) + 2;

            foreach (VideoFormat format in assets.OrderBy(x => x.Type))
            {
                string formatString = // ty youtube-dl for the videoFormat!
                    AddSpaces(format.Type, typeSpace) +
                    AddSpaces(format.Extension, extSpace) +
                    AddSpaces(format.Resolution, resolutionSpace) +
                    AddSpaces(format.Bitrate, bitrateSpace) +
                    AddSpaces(format.Container, containerSpace) +
                    AddSpaces(format.Codec, codecSpace) +
                    format.Size;
                Console.WriteLine(formatString);
            }
        }

        public static bool TryGetFormat(JObject json, string quality, out VideoFormat videoFormat)
        {
            if (json["media"]?["assets"] == null)
            {
                videoFormat = null;
                return false;
            }

            JToken correctFormatBlock = json["media"]["assets"]
                .FirstOrDefault(x => x["type"].Value<string>() == quality);

            if (correctFormatBlock == null)
            {
                videoFormat = null;
                return false;
            }

            videoFormat = new VideoFormat
            {
                Url = GetJsonValue<string>(correctFormatBlock, "url"),
                Size = GetJsonValue<string>(correctFormatBlock, "size")
            };

            return true;
        }

        private static string AddSpaces(string value, int max)
        {
            int spacesNeeded = max - value.Length;
            return value += new string(' ', spacesNeeded);
        }

        private static IEnumerable<VideoFormat> ParseAssets(JObject json)
        {
            JToken assets = json["media"]["assets"];

            var formatList = new List<VideoFormat>();

            var titles = new VideoFormat
            {
                Type = "VideoFormat Name",
                Extension = "Extension",
                Resolution = "Resolution",
                Bitrate = "Bitrate",
                Codec = "Codec",
                Container = "Container",
                Size = "Size"
            };

            formatList.Add(titles);

            foreach (JToken asset in assets)
            {
                var format = new VideoFormat
                {
                    Type = GetJsonValue(asset, "type", "?") + "-",
                    Codec = GetJsonValue(asset, "codec", "?"),
                    Bitrate = GetJsonValue(asset, "bitrate", "?") + "k",
                    Extension = GetJsonValue(asset, "ext", "?"),
                    Container = GetJsonValue(asset, "container", "?") + " Container"
                };

                if (format.Extension == "jpg") //think of a better way
                {
                    continue;
                }

                string height = GetJsonValue(asset, "height", "?");
                string width = GetJsonValue(asset, "width", "?");
                format.Resolution = $"{width}x{height}";

                format.Codec += "@" + GetJsonValue(asset, "opt_vbitrate", "?") + "k";

                ByteSize sizeInBytes = ByteSize.FromBytes(GetJsonValue(asset, "size", 0D));
                double sizeRounded = Math.Round(sizeInBytes.LargestWholeNumberValue, 2);

                format.Size = sizeRounded + sizeInBytes.LargestWholeNumberSymbol;

                int typeCount = formatList.Count(x => x.Type.Substring(0, x.Type.Length - 1) == format.Type);
                format.Type += typeCount == 0 ? 0 : typeCount;

                formatList.Add(format);
            }

            return formatList;
        }
    }
}