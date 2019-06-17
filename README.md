# code-with-mosh-downloader-csharp

### A Downloader for [https://codewithmosh.com/](https://codewithmosh.com/)

![Preview](https://i.imgur.com/5mteEaK.png)

#### Why Use This Over Youtube-dl?

The problem with Youtube-dl in this case is that at the current time of writing it does no organisation of the files, so they'll just be dumped into one big folder. Whereas my tool will sort these into the correct folder and index everything correctly.

Another big reason to use this is that it grabs lecture attachments which youtube-dl doesn't. This includes embedded attachments like PDF's. It will also store text only lectures and quizzes to html files for your viewing.

#### Credit to Youtube-dl for the output formatting style

![Formats](https://i.imgur.com/zGFc4n1.png)

##### Usage: `dotnet codeWithMoshDownloader.dll [args] URL`

Example: `dotnet codeWithMoshDownloader.dll -c cookies.txt -q 1280x720 https://codewithmosh.com/courses/enrolled/228831`

`-c [VAL]` path to cookies.txt - this is required, you cannot use the tool using a username/password, there are many extensions that can get your cookies for you

`-f` force overwriting of existing files

`-q [VAL]` quality setting, can take either a format code from -Q or a resolution like 1280x720, default is original

`-Q` will print all formats for each lecture, similar to youtube-dl's -F

`-s [VAL]` sets the starting position for a playlist

`-z` auto extract zip archives

The URL can be to either a playlist or individual lecture

#### [Requires .Net Core 2.2+](https://dotnet.microsoft.com/download)

### Dependencies

* HtmlAgilityPack
* NewtonsoftJson
* ByteSize

### Note to anyone concerned, this tool does NOT break any protection methods employed by the host site, this tool REQUIRES an account WITH paid access to any content being requested. The host website ALREADY allows downloading of ANY video on said platform, this tool merely automates that process.
