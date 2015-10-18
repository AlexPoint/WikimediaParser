using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public WikiPage ReadNext()
        {
            while (_reader.ReadToFollowing("page"))
            {
                var foundTitle = _reader.ReadToDescendant("title");
                if (foundTitle)
                {
                    var title = _reader.ReadInnerXml();
                    var foundRevision = _reader.ReadToNextSibling("revision");
                    if (foundRevision)
                    {
                        //var revision = _reader.ReadInnerXml();
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
                else
                {
                    Console.WriteLine("Couldn't find title in node");
                }
            }

            return null;
        }
    }
}
