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
        public List<DefinitionAndExamples> DefinitionsAndExamples { get; set; }
        public List<string> Synonyms { get; set; }
        public List<string> Categories { get; set; }
        public string PartOfSpeech { get; set; }


        public void Print()
        {
            Console.WriteLine(Name + " (" + PartOfSpeech + " - " + string.Join("|", Categories) + ")");
            Console.WriteLine("Defs:");
            foreach (var definitionAndExample in DefinitionsAndExamples)
            {
                Console.WriteLine(definitionAndExample.Definition);
                foreach (var example in definitionAndExample.Examples.Union(definitionAndExample.Quotes))
                {
                    Console.WriteLine("--> " + example);
                }
                Console.WriteLine("---");
            }
            Console.WriteLine("-------------");
        }
    }
}
