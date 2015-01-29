using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Phrases;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new WikitionaryParser.Src.WikitionaryParser();
            var phrases = parser.ParseAllEnglishPhrases();

            Console.WriteLine("{0} phrases parsed", phrases.Count);
            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }
    }
}
