using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Src
{
    public static class WordDictionary
    {
        private static readonly Dictionary<string, Word> dictionary = new Dictionary<string, Word>();

        public static Word GetOrCreate(string token)
        {
            if (!dictionary.ContainsKey(token))
            {
                dictionary.Add(token, new Word(){ Token = token});
            }

            return dictionary[token];
        }
    }

    public class Word : IComparable<Word>
    {
        public string Token { get; set; }
        public int CompareTo(Word other)
        {
            return String.Compare(this.Token, other.Token, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return this.Token ?? "";
        }
    }
}
