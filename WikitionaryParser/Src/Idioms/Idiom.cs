using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Phrases;

namespace WikitionaryParser.Src.Idioms
{
    public class Idiom: WikitionaryEntity
    {
        public string Name { get; set; }
        public List<string> Synonyms { get; set; }
        public List<string> Categories { get; set; }
        public List<Usage> Usages { get; set; }


        public void Print()
        {
            Console.WriteLine(Name + " (" + string.Join(" | ", Usages.Select(u => u.PartOfSpeech)) + " - " + string.Join("|", Categories) + ")");
            Console.WriteLine("Usages:");
            foreach (var usage in Usages)
            {
                Console.WriteLine(usage.PartOfSpeech);
                foreach (var definitionAndExample in usage.DefinitionsAndExamples)
                {
                    Console.WriteLine(definitionAndExample.Definition);
                    foreach (var example in definitionAndExample.Examples.Union(definitionAndExample.Quotes))
                    {
                        Console.WriteLine("--> " + example);
                    }
                    Console.WriteLine("--");
                }
                Console.WriteLine("---");
            }
            Console.WriteLine("-------------");
        }
    }
}
