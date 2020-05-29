using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestBot
{
    static class Extentsions
    {
        public static string ToFamilyFriendlyString(this string input, List<string> words)
        {
            foreach (string fWord in words)
            {
                //  Replace the word with *'s (but keep it the same length)
                string strReplace = "";
                for (int i = 0; i < fWord.Length; i++)
                {
                    strReplace += "#";
                }
                input = Regex.Replace(input.ToString(), fWord, strReplace, RegexOptions.IgnoreCase);
            }
            return input.ToString();
        }
    }
}
