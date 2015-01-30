using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryParser.Src.Phrases
{
    /// <summary>
    /// A class containing an entity's definition and a collection of examples for this entity
    /// </summary>
    [Serializable]
    public class DefinitionAndExamples
    {
        // Properties -------------

        public string Definition { get; set; }
        public List<string> Examples { get; set; }
        public List<string> Quotes { get; set; }


        // Methods ----------------

        /// <summary>
        /// Prints this entity in the console
        /// </summary>
        public void Print()
        {
            Console.WriteLine(Definition);
            foreach (var example in Examples.Union(Quotes))
            {
                Console.WriteLine("--> " + example);
            }
        }
    }
}
