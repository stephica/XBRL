using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SphinxRulesGenerator.Helpers
{
    public static class ClassHelpers
    {
        public static string Between(string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            int posB = value.LastIndexOf(b);
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        public static bool HasValue(string dpmID, List<string> listofMappings)
        {
            foreach (var item in listofMappings)
            {
                if (item.Equals(dpmID))
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }

            return false;
        }
    }
}
