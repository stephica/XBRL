namespace SphinxRulesGenerator
{
    using System.Text.RegularExpressions;
    public static class RegexHelper
    {
        public static Regex cellReferenceRegex = new Regex("(?<={).*?(?=})");
        public static Regex ifStatementRegex = new Regex("if[(](?'conditionA'.*);(?'conditionB'.*);(?'conditionC'.*)[)]");
        public static Regex sheetNameRegEx = new Regex(".+\\((?<sheetName>.+)\\)");
        public static Regex mathematicalOperators = new Regex((@"[\/\+\-\*]"));
        public static Regex Pattern_CheckBetweenBrackets=new Regex(@"(?<=\(\{)(.*?)(?=\}\))");
        public static Regex operatorwithCommaRow = new Regex(@",\s\(\r\,");
        public static Regex Pattern_7 = new Regex(@"\),\sc");
    }
}
