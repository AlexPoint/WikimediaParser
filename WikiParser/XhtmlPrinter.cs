using System;
using System.Text;
using org.wikimodel.wem;

namespace WikiParser
{
    public class XhtmlPrinter : IWikiPrinter, IDisposable
    {
        protected bool Disposed = false;
        protected StringBuilder Sb = new StringBuilder();


        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here. 
                if (Sb != null)
                {
                    Sb.Clear();
                    Sb = null;
                }
            }

            // Free any unmanaged objects here. 
            Disposed = true;
        }


        public void print(string str)
        {
            Sb.Append(str);
        }


        public void println(string str)
        {
            Sb.Append(str);
            Sb.Append("\n");
        }


        public string Text
        {
            get
            {
                return Sb.ToString();
            }

        }

    }
}
