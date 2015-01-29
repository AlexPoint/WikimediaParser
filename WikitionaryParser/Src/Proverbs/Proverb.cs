using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikitionaryParser.Src.Phrases;

namespace WikitionaryParser.Src.Proverbs
{
    /// <summary>
    /// A proverb parsed on wikitionary
    /// </summary>
    public class Proverb: WikitionaryEntity
    {
        // Properties ---------------------

        public string Name { get; set; }
        public List<DefinitionAndExamples> DefinitionsAndExamples { get; set; }


        // Methods ------------------------

        /// <summary>
        /// Prints this entity in the console
        /// </summary>
        public void Print()
        {
            Console.WriteLine(Name);
            if (DefinitionsAndExamples.Any())
            {
                Console.WriteLine("Defs:");
                foreach (var definitionAndExamples in DefinitionsAndExamples)
                {
                    Console.WriteLine(definitionAndExamples.Definition);
                    foreach (var example in definitionAndExamples.Examples.Union(definitionAndExamples.Quotes))
                    {
                        Console.WriteLine("--> " + example);
                    }
                }
            }
            Console.WriteLine("------");
        }
    }
}
