using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;

namespace WikitionaryDumpParser.Src
{
    /// <summary>
    /// A downloader for WikiMedia dump files.
    /// The dumps are downloaded locally in the AppData folder.
    /// </summary>
    public class DumpDownloader
    {
        private const string RootUrl = "https://dumps.wikimedia.org";

        private readonly string _localDownloadDirectory;

        public DumpDownloader(string localDownloadDirectory)
        {
            _localDownloadDirectory = localDownloadDirectory;
        }

        public List<string> DownloadLatestFiles(string wikimedia, string languageCode)
        {
            var latestDirUrl = string.Format("{0}/{1}{2}/latest/", RootUrl, languageCode, wikimedia);
            var html = new WebClient().DownloadString(latestDirUrl);
            
            // list the files to download in this directory
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.Descendants("a")
                .Where(n => n.HasAttributes && n.Attributes.Contains("href"))
                .Select(n => n.Attributes["href"].Value)
                .Where(link => Regex.IsMatch(link, "latest-pages-meta-current\\d+") && Path.GetExtension(link) == ".bz2")
                .ToList();

            // Download those files if they're not already on disk
            var downloadedFiles = links
                .Select(l => DownloadIfNotPresent(string.Format("{0}/{1}", latestDirUrl, l), Path.GetFileName(l)))
                .ToList();
            return downloadedFiles;
        }

        /// <summary>
        /// Downloads a dump file locally in the AppData folder (keep the same filename) if the file
        /// hasn't been downloaded already (dump file are huge, so we optimize by checking if the file already exists).
        /// A filename generally contains the language/wikimedia/version/type of file.
        /// Ex: enwiki-latest-page.sql.gz
        /// </summary>
        /// <param name="fileName">The name of the file to download</param>
        /// <returns>The local path of the downloaded file (null is download failed)</returns>
        public string DownloadFile(string fileName)
        {
            // Full file names contains wikimedia/language/date infos
            // Ex: enwiki-latest-page.sql.gz 
            var parts = fileName.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                var languageCode = parts.First().Substring(0, 2);
                var wikimedia = parts.First().Substring(2);
                var dateVersion = parts[1];

                return DownloadFile(wikimedia, languageCode, fileName, dateVersion);
            }
            else
            {
                Console.WriteLine("Couldn't extract required info (for download) from file name '{0}'", fileName);
                return null;
            }
        }

        private string DownloadIfNotPresent(string downloadFileUrl, string downloadedFileName)
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(_localDownloadDirectory))
            {
                Directory.CreateDirectory(_localDownloadDirectory);
            }

            // Check if the local copy of the file already exists
            var localFilePath = _localDownloadDirectory + downloadedFileName;
            if (File.Exists(localFilePath))
            {
                // Check file size (if
                var remoteFileSize = GetByteSize(downloadFileUrl);
                if (GetFileSize(localFilePath) == remoteFileSize)
                {
                    // The file already exists and has the correct checsum -> we don't download it
                    return localFilePath;
                }
                else
                {
                    // Otherwise we delete the file (wrong checksum of we couldn't find the checksum)
                    File.Delete(localFilePath);
                }
            }

            // We download the file
            //var fileUrl = string.Format("{0}/{1}", relevantVersionPageUrl, fileName);
            Console.WriteLine("Start download of file at {0}", downloadFileUrl);
            using (var client = new WebClient())
            {
                var lastProgressLogged = 0;
                client.DownloadProgressChanged += (sender, args) =>
                {
                    if (args.ProgressPercentage % 10 == 0 & args.ProgressPercentage > lastProgressLogged)
                    {
                        lastProgressLogged = args.ProgressPercentage;
                        Console.WriteLine("{0}%", args.ProgressPercentage);
                    }
                };
                var task = client.DownloadFileTaskAsync(downloadFileUrl, localFilePath);
                task.Wait();
            }
            Console.WriteLine("End of download of file {0}", downloadedFileName);

            return localFilePath;
        }

        private string DownloadFile(string wikimedia, string languageCode, string fileName, string dateVersion)
        {
            // Retrieve url of page with all versions
            var versionsPageUrl = GetVersionsPageUrl(wikimedia, languageCode);
            // Get the url of the relevant page
            var relevantVersionPageUrl = string.Format("{0}/{1}", versionsPageUrl, dateVersion);
            
            return DownloadIfNotPresent(relevantVersionPageUrl, fileName);

            //return localFilePath;
        }

        private static long GetFileSize(string localFilePath)
        {
            var info = new FileInfo(localFilePath);
            return info.Length;
        }

        private string GetMd5CheckSum(string localFilePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(localFilePath))
                {
                    var bytes = md5.ComputeHash(stream);
                    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
                }
            }
        }

        private static long GetByteSize(string md5Url)
        {
            //var md5Url = string.Format("{0}/{1}", versionPageUrl, fileName);
            
            var req = WebRequest.Create(md5Url);
            req.Method = "HEAD";
            using (var resp = req.GetResponse())
            {
                long contentLength;
                if (long.TryParse(resp.Headers.Get("Content-Length"), out contentLength))
                {
                    return contentLength;
                }
            }

            return -1;
        }

        private string GetMd5CheckSum(string versionPageUrl, string wikimedia, string languageCode, string dateExtension, string fileName)
        {
            // Check md5 sum
            var md5Url = string.Format("{0}/{1}{2}-{3}-md5sums.txt", versionPageUrl, languageCode, wikimedia, dateExtension);
            using (var client = new WebClient())
            {
                var md5Checksums = client.DownloadString(md5Url);
                using (var reader = new StringReader(md5Checksums))
                {
                    var line = reader.ReadLine();
                    while (line != null)
                    {
                        var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var fileNameParts = fileName.Split('-');
                        if (parts.Length == 2 && fileNameParts.All(p => p =="latest" || parts.Last().Contains(p)))
                        {
                            var md5CheckSum = parts.First();
                            return md5CheckSum;
                        }
                        line = reader.ReadLine();
                    }
                }
            }

            Console.WriteLine("Unable to find checksum for file {0} at date {1}", fileName, dateExtension);
            return "";
        }

        private static string GetVersionsPageUrl(string wikimedia, string languageCode)
        {
            return string.Format("{0}/{1}{2}", RootUrl, languageCode, wikimedia);
        }
    }

}
