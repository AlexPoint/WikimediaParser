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
            var idioms = parser.ParseAllEnglishIdioms();

            Console.WriteLine("{0} idioms parsed:", idioms.Count);
            foreach (var proverb in idioms)
            {
                proverb.Print();
                Console.WriteLine("-------------");
            }
            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }
    }
}
