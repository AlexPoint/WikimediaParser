using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Idioms;
using WikitionaryParser.Src.Phrases;

namespace Test
{
    class Program
    {
        private static readonly string PathToProject = Environment.CurrentDirectory + "\\..\\..\\";
        private static readonly string PathToSerializedIdioms = PathToProject + "Data/idioms.nbin";

        static void Main(string[] args)
        {
            /*var idiomParser = new EnglishIdiomsParser();
            var idiom = idiomParser.ParseIdiomPage("/wiki/fucking_hell");
            
            SerializeIdioms(new List<Idiom>(){idiom}, PathToSerializedIdioms);

            var results = DeserializeIdioms(PathToSerializedIdioms);
            foreach (var result in results)
            {
                result.Print();
                Console.WriteLine("=====");
            }*/

            var parser = new WikitionaryParser.Src.WikitionaryParser();
            var idioms = parser.ParseAllEnglishIdioms();

            SerializeIdioms(idioms, PathToSerializedIdioms);

            Console.WriteLine("{0} idioms parsed:", idioms.Count);
            foreach (var proverb in idioms)
            {
                proverb.Print();
                Console.WriteLine("-------------");
            }
            Console.WriteLine("======= END ========");
            Console.ReadKey();
        }

        private static void SerializeIdioms(List<Idiom> idioms, string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Create))
            {
                var bin = new BinaryFormatter();
                bin.Serialize(stream, idioms);
            }
        }

        private static List<Idiom> DeserializeIdioms(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var bin = new BinaryFormatter();
                var idioms = (List<Idiom>)bin.Deserialize(stream);
                return idioms;
            }
        }
    }
}
