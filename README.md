# code-with-mosh-downloader-csharp

### A Downloader for [https://codewithmosh.com/](https://codewithmosh.com/)

![Preview](https://i.imgur.com/5mteEaK.png)

#### Why Use This Over Youtube-dl?

The problem with Youtube-dl in this case is that at the current time of writing it does no orginisation of the files, so there'll just be dumped into one big folder. Whereas my tool will sort these into the correct folder and index everything correctly.

Another big reason to use this is that it grabs lecture attacthments which youtube-dl doesn't. This includes embedded attatchments like PDF's. It will also store text only lectures to html files for your viewing.

#### Credit to Youtube-dl for the output formatting

##### Usage: `dotnet codeWithMoshDownloader.dll [-c -f -q [FORMATCODE/RESOLUTION] -Q -s [STARTING POSITION]] URL to playlist or lecture`

`-c` path to cookies.txt - this is required, you cannot use the tool using a username/password, there are many extensions that can do this for you

`-f` force overwriting of existing files

`-q` quality setting, can take either a format code from -Q or a resolution like 1280x720, default is original

`-Q` will print all formats for each lecture, similar to youtube-dl's -F

`-s` sets the starting position for a playlist

### Dependencies

* .Net Core 2.2+
* HtmlAgilityPack
* NewtonsoftJson
* ByteSize

### Note to anyone concerned, this tool does NOT break any protection methods employed by the host site, this tool REQUIRES an account WITH paid access to any content being requested. The host website ALREADY allows downloading of ANY video on said platform, this tool merely automates that process.
