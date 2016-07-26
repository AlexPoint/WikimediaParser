using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.SharpZipLib.BZip2;

namespace WikitionaryDumpParser.Src
{
    public class XmlDumpFileReader
    {

        // Properties -------------------------------------

        private readonly XmlReader _reader;
        

        // Constructors -----------------------------------

        public XmlDumpFileReader(string filePath)
        {
            Stream fileStream = File.OpenRead(filePath);
            var decompressedStream = new BZip2InputStream(fileStream);
            _reader = XmlReader.Create(decompressedStream);
        }


        // Methods ----------------------------------------

        public WikiPage ReadNext(Predicate<string> pageFilterer)
        {
            while (_reader.ReadToFollowing("page"))
            {
                var foundTitle = _reader.ReadToDescendant("title");
                if (foundTitle)
                {
                    var title = _reader.ReadInnerXml();
                    if (!pageFilterer(title))
                    {
                        var foundRevision = _reader.ReadToNextSibling("revision");
                        if (foundRevision)
                        {
                            var foundText = _reader.ReadToDescendant("text");
                            if (foundText)
                            {
                                var text = _reader.ReadInnerXml();

                                return new WikiPage()
                                {
                                    Title = title,
                                    //Revision = revision,
                                    Text = text
                                };
                            }
                        } 
                    }
                }
                else
                {
                    Console.WriteLine("Couldn't find title in node");
                }
            }

            return null;
        }

        public static WikiPage GetPage(string title)
        {
            var url = string.Format("https://en.wikipedia.org/w/api.php?action=query&titles={0}&prop=revisions&rvprop=content&format=xml", title);
            var client = new WebClient
            {
                // Need to set encoding to UTF-8 for special characters such as Brønsted (article: Acid)
                Encoding = Encoding.UTF8
            };
            var xmlResponse = client.DownloadString(url);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlResponse);

            var revNode = xmlDoc.SelectSingleNode("//rev");
            if (revNode != null)
            {
                return new WikiPage()
                {
                    Title = title,
                    Text = revNode.InnerText
                };
            }

            return null;
        }
    }
}
