using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.wikimodel.wem.xhtml;
using java.io;

namespace WikiParser
{
    public class Converter
    {
        public static string MediaWikiToXhtml(string markup)
        {
            string retVal = null;

            using (XhtmlPrinter printer = new XhtmlPrinter())
            {
                Reader rdr = new java.io.StringReader(markup);

                // org.wikimodel.wem.WikiPrinter wp = new org.wikimodel.wem.WikiPrinter ();
                // var listener = new org.wikimodel.wem.xwiki.XWikiSerializer(wp);

                // var listener = new org.wikimodel.wem.xwiki.XWikiSerializer(printer);
                org.wikimodel.wem.IWemListener listener = new PrintListener(printer);

                org.wikimodel.wem.mediawiki.MediaWikiParser mep =
                    new org.wikimodel.wem.mediawiki.MediaWikiParser();
                mep.parse(rdr, listener);
                retVal = printer.Text;

                rdr.close();
                rdr = null;
                listener = null;
                mep = null;
            } // End Using printer

            return retVal;
        }

    }
}
