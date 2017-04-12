using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SphinxRulesGenerator
{
    public struct DpmIdMappingEntry
    {
        public string CellReference;
        public string DpmVariableName;
    }

    public static class Helper
    {
        private static TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

        public static string MakeValidName(string checkName)
        {
            string tempString = textInfo.ToTitleCase(checkName).Replace(" ", string.Empty);
            StringBuilder sb = new StringBuilder();
            foreach (char c in tempString)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
