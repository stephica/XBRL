using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SphinxRulesGenerator
{
    public static class DataHelper
    {
        public static string GetProcessedformula(string rawFormula)
        {
            rawFormula = rawFormula.
                    Replace(" = ", " == ").
                    Replace("^=", "!=").
                    Replace("<>", "!=").
                    Replace("xsum(", "(").
                    Replace("sum(", "(").
                    Replace("% of", "/100 * ").
                    Replace("% * ", "/100 * ");
            return rawFormula;
        }

        public static bool HasExecuteFormula(string uitvoeren)
        {
            if(!string.IsNullOrEmpty(uitvoeren))
            {
              return  uitvoeren == "ja" ? true : false;
            }
            return true;
        }
    }
}
