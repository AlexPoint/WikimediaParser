using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikitionaryDumpParser.Src
{
    public static class StringHelpers
    {
        public static bool IsFirstLetterLower(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return char.IsLower(input[0]);
        }

        public static bool IsFirstLetterUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return char.IsUpper(input[0]);
        }

        public static string LowerCaseFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToLower(input[0]) + input.Substring(1);
        }

        public static string UpperCaseFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
