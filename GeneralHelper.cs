using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SphinxRulesGenerator
{
    public class GeneralHelper
    {
        enum TypeOperations
        {
            Equals,
            GreaterThan,
            GreaterThanEquals,
            LessThan,
            LessThanEquals
        };
        private Regex equalReferenceCheckwithPlus = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s?==\s\+\w*\$DPM_ID_\w*)");
        private Regex equalReferenceCheck = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s?==\s\w*\$DPM_ID_\w*)");
        private Regex equalReferenceCheckwithStartingbrackets = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s?==\s\(\w*)");
        private Regex greaterthanReferenceCheck = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s\>\s\w*\$DPM_ID_\w*)");
        private Regex lessthanReferenceCheck = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s\<\s\w*\$DPM_ID_\w*)");
        private Regex greaterthanEqualsReferenceCheck = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s\>\=\s\w*\$DPM_ID_\w*)");
        private Regex lessthanEqualsReferenceCheck = new Regex(@"(?<TM>\w*\$DPM_ID_\w*\s\<\=\s\w*\$DPM_ID_\w*)");
        private Regex specialCharacter = new Regex(@"([>\?\==])+");
        public static Dictionary<string, string> ReadMapping(string mappingInputTextFile)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            using (StreamReader sReader = new StreamReader(mappingInputTextFile))
            {
                while (!sReader.EndOfStream)
                {
                    string mappingLine = sReader.ReadLine();
                    string[] splitted = mappingLine.Split('=');
                    if (!mapping.ContainsKey(splitted[0]))
                    {
                        mapping[splitted[0]] = splitted[1];
                    }
                }
            }
            return mapping;
        }

        public static Dictionary<int, string> ReadMappingFromXsr(string mappingInputTextFile)
        {
            Dictionary<int, string> mapping = new Dictionary<int, string>();
            using (StreamReader sReader = new StreamReader(mappingInputTextFile))
            {
                int count = 1;
                while (!sReader.EndOfStream)
                {
                    string mappingLine = sReader.ReadLine();

                    if (mappingLine.Contains("$DPM_ID"))
                    {
                        int[] indexes = AllIndexesOf(mappingLine, "$DPM_", false);
                        for (int index = 0; index < indexes.Count(); index++)
                        {
                            string val = mappingLine.Substring(indexes[index]);
                            int subindex = val.IndexOf(" ");
                            if (subindex == -1)
                            {
                                subindex = val.IndexOf(")");
                            }
                            if (subindex == -1)
                            {
                                if (index == count - 1)
                                {
                                    val = mappingLine.Substring(indexes[index]);
                                }
                            }
                            else
                            {

                                val = val.Remove(subindex);
                            }
                            var matchVal = mapping.GroupBy(x => x.Value).Where(x => x.Count() > 1);
                            if (matchVal.Count() == 0)
                            {
                                mapping.Add(count, val);
                                count++;
                            }
                        }
                    }
                }
            }
            return mapping;
        }

        public static int[] AllIndexesOf(string str, string substr, bool ignoreCase = false)
        {
            if (string.IsNullOrWhiteSpace(str) ||
                string.IsNullOrWhiteSpace(substr))
            {
                throw new ArgumentException("String or substring is not specified.");
            }

            var indexes = new List<int>();
            int index = 0;

            while ((index = str.IndexOf(substr, index, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) != -1)
            {
                indexes.Add(index++);
            }

            return indexes.ToArray();
        }
        private string GetRawFormula(TypeOperations type)
        {
            string rawFormula = string.Empty;
            switch (type)
            {
                case TypeOperations.Equals:
                    rawFormula = "(({0}) - ({1}))::abs < 1000";
                    break;
                case TypeOperations.GreaterThan:
                    rawFormula = "(({0}) - ({1})) > -1000";
                    break;
                case TypeOperations.LessThan:
                    rawFormula = "(({0}) - ({1})) < 1000";
                    break;
                case TypeOperations.GreaterThanEquals:
                    rawFormula = "(({0}) - ({1})) >= -1000";
                    break;
                case TypeOperations.LessThanEquals:
                    rawFormula = "(({0}) - ({1})) <= 1000";
                    break;
            }

            return rawFormula;
        }

        public string GetSpecialOperands(string inputFormula)
        {
            string finalFormula = inputFormula;
            if (inputFormula.Contains("\t"))
            {
                finalFormula = inputFormula.Replace("\t", "");
            }
            MatchCollection matchEqualReference = equalReferenceCheck.Matches(finalFormula);
            if (matchEqualReference.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchEqualReference, TypeOperations.Equals);
            }

            MatchCollection matchEqualReferencewithPlus = equalReferenceCheckwithPlus.Matches(finalFormula);
            if (matchEqualReferencewithPlus.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchEqualReferencewithPlus, TypeOperations.Equals);
            }
            MatchCollection matchequalReferenceCheckwithStartingbrackets = equalReferenceCheckwithStartingbrackets.Matches(finalFormula);
            if (matchequalReferenceCheckwithStartingbrackets.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchequalReferenceCheckwithStartingbrackets, TypeOperations.Equals);
            }

            MatchCollection matchGreaterThanReference = greaterthanReferenceCheck.Matches(finalFormula);
            if (matchGreaterThanReference.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchGreaterThanReference, TypeOperations.GreaterThan);
            }

            MatchCollection matchLessThanReference = lessthanReferenceCheck.Matches(finalFormula);
            if (matchLessThanReference.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchLessThanReference, TypeOperations.LessThan);
            }

            MatchCollection matchGreaterThanEqualsReference = greaterthanEqualsReferenceCheck.Matches(finalFormula);
            if (matchGreaterThanEqualsReference.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchGreaterThanEqualsReference, TypeOperations.GreaterThanEquals);
            }
            MatchCollection matchLessThanEqualsReference = lessthanEqualsReferenceCheck.Matches(finalFormula);
            if (matchLessThanEqualsReference.Count > 0)
            {
                finalFormula = GetTranslatedFormula(finalFormula, matchLessThanEqualsReference, TypeOperations.LessThanEquals);
            }
            return finalFormula;

        }

        private string GetTranslatedFormula(string inputFormula, MatchCollection matchReference, TypeOperations Type)
        {
            string rawFormula = string.Empty;
            string[] operands = new string[0];
            MatchCollection match = specialCharacter.Matches(inputFormula);
            if (matchReference.Count > 0)
            {
                foreach (var item in matchReference)
                {
                    if (matchReference.Count == 1)
                    {
                        if (Type == TypeOperations.Equals)
                        {
                            operands = GetOperands(match, item.ToString(), inputFormula, TypeOperations.Equals);
                        }
                        else if (Type == TypeOperations.GreaterThan)
                        {
                            operands = GetOperands(match, item.ToString(), inputFormula, TypeOperations.GreaterThan);
                        }
                        else if (Type == TypeOperations.LessThan)
                        {
                            operands = GetOperands(match, item.ToString(), inputFormula, TypeOperations.LessThan);
                        }
                        else if (Type == TypeOperations.GreaterThanEquals)
                        {
                            operands = GetOperands(match, item.ToString(), inputFormula, TypeOperations.GreaterThanEquals);
                        }
                        else if (Type == TypeOperations.LessThanEquals)
                        {
                            operands = GetOperands(match, item.ToString(), inputFormula, TypeOperations.LessThanEquals);
                        }
                        else
                        {

                        }
                        rawFormula = GetRawFormula(Type);
                        string replaceFormula = string.Format(rawFormula, operands[0], operands[1]);
                        if (match.Count > 1)
                        {
                            inputFormula = inputFormula.Replace(item.ToString(), replaceFormula);
                        }
                        else
                        {
                            inputFormula = replaceFormula;
                        }

                    }
                    else
                    {
                        operands = item.ToString().Split(new string[] { " == " }, StringSplitOptions.None);
                        rawFormula = GetRawFormula(Type);
                        string replaceFormula = string.Format(rawFormula, operands[0], operands[1]);
                        inputFormula = inputFormula.Replace(item.ToString(), replaceFormula);
                    }
                }
            }
            return inputFormula;
        }

        private Dictionary<string, string> GetOperators()
        {
            Dictionary<string, string> dictOperators = new Dictionary<string, string>();
            dictOperators.Add(TypeOperations.Equals.ToString(), " == ");
            dictOperators.Add(TypeOperations.GreaterThan.ToString(), " > ");
            dictOperators.Add(TypeOperations.GreaterThanEquals.ToString(), " >= ");
            dictOperators.Add(TypeOperations.LessThan.ToString(), " < ");
            dictOperators.Add(TypeOperations.LessThanEquals.ToString(), " <= ");
            return dictOperators;
        }

        private string[] GetOperands(MatchCollection match, string item, string inputFormula, TypeOperations Type)
        {
            Dictionary<string, string> dictOperators = GetOperators();
            string[] operands = new string[0];
            if (match.Count > 1)
            {
                operands = item.ToString().Split(new string[] { dictOperators[Type.ToString()] }, StringSplitOptions.None);
            }
            else
            {
                operands = inputFormula.ToString().Split(new string[] { dictOperators[Type.ToString()] }, StringSplitOptions.None);
            }

            return operands;
        }

        public static string GetMidOperator(string rawFormula)
        {
            string midOperator = string.Empty;
            if (rawFormula.Contains("=="))
            {

                midOperator = " == ";
            }
            else if (rawFormula.Contains("<="))
            {

                midOperator = " <= ";
            }
            else if (rawFormula.Contains(">="))
            {

                midOperator = " >= ";
            }
            return midOperator;
        }
        public static string GetInnerBracketsFormula(string rawFormula, string checkId)
        {
            string leftFacts = string.Empty;
            string rightFacts = string.Empty;
            string tempRightFacts = string.Empty;
            string tempLeftFacts = string.Empty;
            string restFacts = string.Empty;
            string finalformula = string.Empty;
            if (string.IsNullOrEmpty(rawFormula))
            {
                return string.Empty;
            }
            string[] splitFactors = new string[10];
            string midOperator = string.Empty;
            midOperator = GetMidOperator(rawFormula);
            splitFactors = Regex.Split(rawFormula, midOperator); 
            if (splitFactors.Length == 2)
            {
                leftFacts = splitFactors[0].Replace("({", "").Replace("})", "");
                if (splitFactors[1].Contains("abs("))
                {
                    splitFactors[1] = splitFactors[1].Replace("abs(", "");
                }
               // MatchCollection matchRightF = RegexHelper.Pattern_CheckBetweenBrackets.Matches(splitFactors[1]);
                MatchCollection splitRightOperators = RegexHelper.mathematicalOperators.Matches(splitFactors[1]);
                if (splitRightOperators.Count>0)
                {
                    foreach (var rightOperator in splitRightOperators)
                    {
                        if (!string.IsNullOrEmpty(tempRightFacts))
                        {
                            tempRightFacts = tempRightFacts + "+" + GetRightFacts(rightOperator.ToString(), checkId);
                        }
                        else
                        {
                            tempRightFacts = tempRightFacts + GetRightFacts(rightOperator.ToString(), checkId);
                        }

                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(tempRightFacts))
                    {
                        tempRightFacts = tempRightFacts + "+" + GetRightFacts(splitFactors[1], checkId);
                    }
                    else
                    {
                        tempRightFacts = tempRightFacts + GetRightFacts(splitFactors[1], checkId);
                    }
                }


                string[] splitLeftOperators = splitFactors[0].Split('+');
                if (splitLeftOperators.Count() > 0)
                {
                    foreach (var leftOperator in splitLeftOperators)
                    {
                        if (!string.IsNullOrEmpty(tempLeftFacts))
                        {
                            tempLeftFacts = tempLeftFacts + "+" + GetRightFacts(leftOperator, checkId);
                        }
                        else
                        {
                            tempLeftFacts = tempLeftFacts + GetRightFacts(leftOperator, checkId);
                        }

                    }
                }

                else
                {
                    tempLeftFacts = tempLeftFacts + GetRightFacts(leftFacts, checkId);
                }

            }



            if (!(string.IsNullOrEmpty(tempLeftFacts) && string.IsNullOrEmpty(tempRightFacts)))
            {
                finalformula = tempLeftFacts + midOperator + tempRightFacts;
            }
            else
            {
                finalformula = leftFacts + midOperator + rightFacts;
            }

            return finalformula;
        }



        public static string GetRightFacts(string rightFacts, string checkid)
        {
            rightFacts = rightFacts.Replace("x", "");
            Regex operatorwithCommaRow = new Regex(@"(?<TM>\w*\s?,\s\w*r)");
            Regex pattern_1 = new Regex(@"\((.*?)\)");
            Regex pattern_2 = new Regex((@"r\d+-\d\d\d"));
            Regex pattern_3 = new Regex((@"c\d+-\d\d\d"));
            Regex pattern_operation = new Regex((@"[\/\+\-\*]"));
            Regex pattern_5 = new Regex(@"}\s[\/\+\-\*]\s{[a-z]");
            Regex pattern_6 = new Regex(@"^-*[0-9,\.]+$");
            Regex Pattern_7 = new Regex(@"\),\sc");
            Regex Pattern_8 = new Regex(@",\sc");
            Regex Pattern_9 = new Regex(@",\s\(c");
            Regex Pattern_CheckBetweenBrackets = new Regex(@"(?<=\(\{)(.*?)(?=\}\))");
            Regex PatternStartingwithDigit = new Regex(@"^?\d*\.?\d*[\/\+\-\*]\(");
            string tempRightFacts = string.Empty;
            string plainFinalformula = string.Empty;
            string formula = string.Empty;
            List<string> Rows = new List<string>();
            List<string> Columns = new List<string>();
            string finalFormula = string.Empty;
            string operators = string.Empty;
            Match matchOperator = PatternStartingwithDigit.Match(rightFacts);
            operators = matchOperator.Value;
            MatchCollection matchColFacts = Pattern_CheckBetweenBrackets.Matches(rightFacts);
            if (matchColFacts.Count > 1)
            {

                foreach (var match in matchColFacts)
                {
                    string operation = string.Empty;
                    if (operators.Length > 0)
                    {
                        operation = rightFacts.Replace(match.ToString(), "").Replace(operators, "");
                    }
                    else
                    {
                        operation = rightFacts.Replace(match.ToString(), "");
                    }

                    Match matchoperation = pattern_operation.Match(operation.Replace(" ", ""));
                    if (finalFormula.Length > 0)
                    {
                        finalFormula = finalFormula + matchoperation + "(" + ProcessedFormula(match.ToString(), checkid) + ")";
                    }
                    else
                    {
                        finalFormula = "(" + ProcessedFormula(match.ToString(), checkid) + ")";
                    }
                }
                if (operators.Length > 0)
                {
                    finalFormula = "(" + operators + finalFormula + ")";
                }
                return finalFormula;
            }

            finalFormula = ProcessedFormula(rightFacts, checkid);
            return finalFormula;


        }

        public static string ProcessedFormula(string rightFacts, string checkId)
        {
            if(checkId=="DNB_C135" || checkId == "DNB_C136")
            {
                return "";
            }
            Regex operatorwithCommaRow = new Regex(@"(?<TM>\w*\s?,\s\w*r)");
            Regex pattern_1 = new Regex(@"\((.*?)\)");
            Regex pattern_2 = new Regex((@"r\d+-\d\d\d"));
            Regex pattern_3 = new Regex((@"c\d+-\d\d\d"));
            Regex pattern_4 = new Regex((@"[\/\+\-\*][a-z]"));
            Regex pattern_5 = new Regex(@"}\s[\/\+\-\*]\s{[a-z]");
            Regex pattern_6 = new Regex(@"^-*[0-9,\.]+$");
            Regex Pattern_7 = new Regex(@"\),\sc");
            Regex Pattern_8 = new Regex(@",\sc");
            Regex Pattern_9 = new Regex(@",\s\(c");
            Regex pattern_OnlyRow = new Regex(@"r\d\d\d");
            Regex Pattern_CheckBetweenBrackets = new Regex(@"(?<=\(\{)(.*?)(?=\}\))");
            string tempRightFacts = string.Empty;
            string plainFinalformula = string.Empty;
            string formula = string.Empty;
            List<string> Rows = new List<string>();
            List<string> Columns = new List<string>();
            string templateId = string.Empty;
            string row = string.Empty;
            string col = string.Empty;
            string rest = string.Empty;
            rightFacts = rightFacts.Replace("({", "").Replace("})", "");
            MatchCollection splitIndexes = pattern_1.Matches(rightFacts);
            if (splitIndexes.Count == 0)
            {
                Match matchPlainFormula = pattern_5.Match(rightFacts);
                if (matchPlainFormula.Success)
                {
                    plainFinalformula = rightFacts;
                }
                else
                {
                    rightFacts = rightFacts.Replace("{", "").Replace("}", "");
                    string[] splitChars = rightFacts.Split(',');
                    if (splitChars.Count() >= 3)
                    {
                        row = splitChars[1];
                        col = splitChars[2];
                    }
                }
            }

            foreach (var index in splitIndexes)
            {
                if (index.ToString().Contains("r") && index.ToString().Contains("c"))
                {
                    MatchCollection matches = pattern_2.Matches(index.ToString());
                    if (matches.Count > 0)
                    {
                        foreach (var match in matches)
                        {
                            if (!string.IsNullOrEmpty(row))
                            {
                                row = row + "," + match;
                            }
                            else
                            {
                                row = row + match;
                            }

                        }
                    }
                    MatchCollection matcheCol = pattern_3.Matches(index.ToString());
                    if (matcheCol.Count > 0)
                    {
                        foreach (var match in matcheCol)
                        {
                            if (!string.IsNullOrEmpty(col))
                            {
                                col = col + "," + match;
                            }
                            else
                            {
                                col = col + match;
                            }

                        }
                    }
                    MatchCollection matchRow = pattern_OnlyRow.Matches(index.ToString());
                    foreach (var item in matchRow)
                    {
                        Rows.Add(item.ToString());
                    }
                }
                else
                {
                    if (index.ToString().Contains("r"))
                    {
                        row = index.ToString();
                    }
                    else
                    {
                        col = index.ToString();
                    }
                }
            }

            //string[] splitIndexes = rightFacts.Split('(');
            Match hasNumericFact = pattern_6.Match(rightFacts.TrimStart().TrimEnd());
            if (hasNumericFact.Success)
            {
                formula = rightFacts;
                return formula;
            }
            else
            {
                Match match = pattern_5.Match(rightFacts);
                if (match.Success)
                {
                    formula = rightFacts;
                }
                else
                {
                    templateId = rightFacts.Remove(rightFacts.IndexOf(',')).Replace("{", "").Replace("}", "");
                }
            }

            string[] tempRowIndex = new string[10];
            string[] tempColIndex = new string[10];
            if (!string.IsNullOrEmpty(row))
            {
                row = row.Replace("(", "").Replace(")", "");
                tempRowIndex = Regex.Split(row, ",");
                foreach (var rowIndex in tempRowIndex)
                {
                    if (rowIndex.Contains("-"))
                    {
                        var tmpitem = rowIndex.Replace("r", "");
                        string[] rangeSplit = tmpitem.Split('-');
                        int lowerRange = Convert.ToInt32(rangeSplit[0]);
                        int upperRange = Convert.ToInt32(rangeSplit[1]);
                        for (int i = lowerRange; i <= upperRange; i = (i + 10))
                        {
                            Rows.Add(string.Format("r{0}", i));
                        }
                    }
                    else
                    {
                        Rows.Add(rowIndex);
                    }
                }
                Match matchRow = Pattern_8.Match(rightFacts);
                if (matchRow.Success)
                {
                    //string mCol = rightFacts.Substring(matchColl.Index + 2);
                    //Columns.Add(mCol);
                }

            }
            else
            {
                if (rightFacts.Contains("{"))
                {
                    if (string.IsNullOrEmpty(formula))
                    {
                        row = rightFacts.Replace(templateId, "").Replace(col, "").Replace(",", "").Replace("()", "");
                        Rows.Add(row);
                    }
                }
            }
            if (!string.IsNullOrEmpty(col))
            {
                col = col.Replace("(", "").Replace(")", "");
                tempColIndex = Regex.Split(col, ",");
                foreach (var colIndex in tempColIndex)
                {
                    if (colIndex.Contains("-"))
                    {
                        var tmpitem = colIndex.Replace("c", "");
                        string[] rangeSplit = tmpitem.Split('-');
                        int lowerRange = Convert.ToInt32(rangeSplit[0]);
                        int upperRange = Convert.ToInt32(rangeSplit[1]);
                        for (int i = lowerRange; i <= upperRange; i = (i + 10))
                        {
                            Columns.Add(string.Format("c{0}", i));
                        }
                    }
                    else
                    {
                        if (colIndex.Length > 0)
                        {
                            Columns.Add(colIndex);
                        }
                    }

                }
            }
            else
            {
                if (rightFacts.Contains("{"))
                {
                    if (string.IsNullOrEmpty(formula))
                    {
                        col = rightFacts.Replace(templateId, "").Replace(row, "").Replace(",", "").Replace("()", "");
                        Columns.Add(col);
                    }
                }
            }


            if (Rows.Count() == 0)
            {
                string[] splitRow = rightFacts.Split(',');
                if (splitRow.Length > 3)
                {
                    Match matchCol = Pattern_9.Match(rightFacts);
                    if (matchCol.Success)
                    {
                        string tmpC = rightFacts.Remove(matchCol.Index);
                        tmpC = tmpC.Replace(templateId, "").Replace(", ", "");
                        Rows.Add(tmpC);
                    }
                }
                else
                {
                    Rows.Add(splitRow[1]);
                }

            }
            if (Columns.Count() == 0)
            {
                string[] splitCol = rightFacts.Split(',');
                if (splitCol.Length > 3)
                {
                    Match matchCol = Pattern_7.Match(rightFacts);
                    string tmpC = rightFacts.Substring(matchCol.Index + 2);
                    Columns.Add(tmpC);
                }
                else
                {
                    Match match = pattern_5.Match(rightFacts);
                    if (!(match.Success))
                    {
                        Columns.Add(splitCol[2]);
                    }
                }
            }
            tempRightFacts = GetFinalFormula(Rows, Columns, templateId);


            if (!string.IsNullOrEmpty(tempRightFacts))
            {
                return tempRightFacts;
            }
            else
            {
                if (string.IsNullOrEmpty(formula))
                {
                    return rightFacts;
                }
                else
                {
                    Match match = pattern_5.Match(rightFacts);
                    string operators = rightFacts.Substring(match.Index + 1);
                    string finalformula = string.Empty;
                    operators = operators.Remove(operators.IndexOf("{"));
                    if (!string.IsNullOrEmpty(operators))
                    {
                        string[] splitChars = Regex.Split(rightFacts, operators);

                        foreach (var item in splitChars)
                        {
                            string tempVar = item.Replace("}", "").Replace("(", "");
                            string tempFormula = string.Empty;
                            Match matchN = pattern_4.Match(item);
                            if (matchN.Success)
                            {
                                string val = item.Remove(matchN.Index + 1);
                                val = val.Replace("(", "");
                                tempVar = tempVar.Replace(val, "");

                                tempFormula = "(" + val + ")" + "*" + "{" + tempVar + "}"; //string.Format("({0}){{1}})", val, finalformula);
                            }
                            else
                            {
                                tempFormula = item;
                            }

                            if (!string.IsNullOrEmpty(finalformula))
                            {
                                finalformula = finalformula + operators + tempFormula;
                            }
                            else
                            {
                                finalformula = tempFormula;
                            }

                        }
                        return finalformula;
                    }
                    return rightFacts;
                }
            }
        }


        string ExtractString(string s, string tag)
        {
            // You should check for errors in real-world code, omitted for brevity
            var startTag = "<" + tag + ">";
            int startIndex = s.IndexOf(startTag) + startTag.Length;
            int endIndex = s.IndexOf("</" + tag + ">", startIndex);
            return s.Substring(startIndex, endIndex - startIndex);
        }

        public static string GetFinalFormula(List<string> listofRows, List<string> listofColumns, string templateId)
        {
            string finalFormula = string.Empty;
            foreach (var row in listofRows)
            {
                foreach (var col in listofColumns)
                {
                    if (finalFormula.Length > 0)
                    {
                        finalFormula = finalFormula + "+" + "{" + templateId + ", " + row + ", " + col + "}";
                    }
                    else
                    {
                        finalFormula = "{" + templateId + ", " + row + ", " + col + "}";
                    }

                }
            }

            return finalFormula;
        }


        public static List<string> ProcessRowReference(string rows)
        {
            List<string> rowReferences = new List<string>();
            if (!string.IsNullOrEmpty(rows))
            {
                rows = rows.Replace("(", "").Replace(")", "").Replace(" ", "");
                if (rows.Contains(";") || rows.Contains("-"))
                {
                    string[] rowsSplit = rows.Split(';');
                    foreach (string row in rowsSplit)
                    {
                        if (row.Contains("-"))
                        {
                            string[] rangeSplit = row.Split('-');
                            int lowerRange = Convert.ToInt32(rangeSplit[0]);
                            int upperRange = Convert.ToInt32(rangeSplit[1]);
                            for (int i = lowerRange; i <= upperRange; i = (i + 10))
                            {
                                rowReferences.Add("r" + i.ToString().PadLeft(rangeSplit[0].Length, '0'));
                            }
                        }
                        else
                        {
                            rowReferences.Add("r" + row);
                        }
                    }
                }
                else
                {
                    rowReferences.Add("r" + rows);
                }
            }
            // dummy references
            if (rowReferences.Count == 0)
            {
                rowReferences.Add(string.Empty);
            }
            return rowReferences;
        }

        public static List<string> ProcessColumnReference(string columns)
        {
            List<string> columnReferences = new List<string>();
            if (!string.IsNullOrEmpty(columns))
            {
                columns = columns.Replace("(", "").Replace(")", "").Replace(" ", "");
                if (columns.Contains(";") || columns.Contains("-"))
                {
                    string[] columnsSplit = columns.Split(';');
                    foreach (string column in columnsSplit)
                    {
                        if (column.Contains("-"))
                        {
                            int lowerRange, upperRange = 0;
                            string[] rangeSplit = column.Split('-');
                            Int32.TryParse(rangeSplit[0], out lowerRange);
                            Int32.TryParse(rangeSplit[1], out upperRange); 
                            for (int i = lowerRange; i <= upperRange; i = (i + 10))
                            {
                                columnReferences.Add("c" + i.ToString().PadLeft(rangeSplit[0].Length, '0'));
                            }
                        }
                        else
                        {
                            columnReferences.Add("c" + column);
                        }
                    }
                }
                else
                {
                    columnReferences.Add("c" + columns);
                }
            }
            // dummy references
            if (columnReferences.Count == 0)
            {
                columnReferences.Add(string.Empty);
            }
            return columnReferences;
        }

        public static List<string> GetASbsoluteReference(List<string> rowReferences, List<string> columnReferences, string rawFormula, string nameT1, string checkID, string sheetName)
        {
            string processedFormula = string.Empty;
            List<string> expandedFormulae = new List<string>();
            foreach (string rowReference in rowReferences)
            {
                foreach (string columnReference in columnReferences)
                {
                    processedFormula = rawFormula;

                    foreach (string rawCellReference in RegexHelper.cellReferenceRegex.Matches(rawFormula).OfType<Match>().Select(m => m.Groups[0].Value).Distinct())
                    {
                        //string rawCellReference = match.Value;
                        string absoluteCellReference = rawCellReference.Replace(" ", "");
                        string[] cellReferenceParts = absoluteCellReference.Split(',');

                        if (cellReferenceParts.Length == 1)
                        {
                            if (cellReferenceParts[0][0] == 'c')
                            {
                                if (nameT1.Equals(cellReferenceParts[0]))
                                {
                                    string id = checkID;
                                    absoluteCellReference = nameT1 + "," + rowReference + "," + columnReference;
                                }
                                else
                                {
                                    absoluteCellReference = nameT1 + "," + rowReference + "," + cellReferenceParts[0];
                                }
                                if (!string.IsNullOrEmpty(sheetName))
                                {
                                    absoluteCellReference += "," + sheetName;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                            else if (cellReferenceParts[0][0] == 'r')
                            {
                                absoluteCellReference = nameT1 + "," + cellReferenceParts[0] + "," + columnReference;
                                if (!string.IsNullOrEmpty(sheetName))
                                {
                                    absoluteCellReference += "," + sheetName;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                            if (cellReferenceParts[0][0] == 'f')
                            {
                                if (!string.IsNullOrEmpty(columnReference) && !string.IsNullOrEmpty(rowReference))
                                {
                                    absoluteCellReference = nameT1 + "," + rowReference + "," + columnReference;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                        }
                        if (cellReferenceParts.Length == 2)
                        {
                            // template & row present
                            if (cellReferenceParts[1][0] == 'r')
                            {
                                absoluteCellReference = cellReferenceParts[0] + "," + cellReferenceParts[1] + "," + columnReference;
                                if (!string.IsNullOrEmpty(sheetName))
                                {
                                    absoluteCellReference += "," + sheetName;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                            else if (cellReferenceParts[0][0] != 'r' && cellReferenceParts[1][0] == 'c')
                            {
                                absoluteCellReference = cellReferenceParts[0] + "," + rowReference + "," + cellReferenceParts[1];
                                if (!string.IsNullOrEmpty(sheetName))
                                {
                                    absoluteCellReference += "," + sheetName;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                            else if (cellReferenceParts[0][0] == 'r' && cellReferenceParts[1][0] == 'c')
                            {
                                absoluteCellReference = nameT1 + "," + cellReferenceParts[0] + "," + cellReferenceParts[1];
                                if (!string.IsNullOrEmpty(sheetName))
                                {
                                    absoluteCellReference += "," + sheetName;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                            else if (cellReferenceParts[0][0] == 'c')
                            {
                                absoluteCellReference = cellReferenceParts[0] + "," + cellReferenceParts[1] + "," + columnReference;
                                if (!string.IsNullOrEmpty(sheetName))
                                {
                                    absoluteCellReference += "," + sheetName;
                                }
                                processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                            }
                        }

                        if (cellReferenceParts.Length == 3)
                        {
                            if (!string.IsNullOrEmpty(sheetName))
                            {
                                // template & row present
                                if (cellReferenceParts[1][0] == 'r')
                                {
                                    absoluteCellReference = cellReferenceParts[0] + "," + cellReferenceParts[1] + "," + cellReferenceParts[2];
                                    if (!string.IsNullOrEmpty(sheetName))
                                    {
                                        absoluteCellReference += "," + sheetName;
                                    }
                                    processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                                }
                                else if (cellReferenceParts[0][0] != 'r' && cellReferenceParts[1][0] == 'c')
                                {
                                    absoluteCellReference = cellReferenceParts[0] + "," + rowReference + "," + cellReferenceParts[1];
                                    if (!string.IsNullOrEmpty(sheetName))
                                    {
                                        absoluteCellReference += "," + sheetName;
                                    }
                                    processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                                }
                                else if (cellReferenceParts[0][0] == 'r' && cellReferenceParts[1][0] == 'c')
                                {
                                    absoluteCellReference = nameT1 + "," + cellReferenceParts[0] + "," + cellReferenceParts[1];
                                    if (!string.IsNullOrEmpty(sheetName))
                                    {
                                        absoluteCellReference += "," + sheetName;
                                    }
                                    processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                                }
                                else if (cellReferenceParts[0][0] == 'c')
                                {
                                    absoluteCellReference = cellReferenceParts[0] + "," + cellReferenceParts[1] + "," + columnReference;
                                    if (!string.IsNullOrEmpty(sheetName))
                                    {
                                        absoluteCellReference += "," + sheetName;
                                    }
                                    processedFormula = processedFormula.Replace(rawCellReference, absoluteCellReference);
                                }
                            }
                        }
                    }
                    expandedFormulae.Add(processedFormula);
                }
            }
            return expandedFormulae;

        }

        public static List<string> ProcessedFormulaWithDPMIds(string finalFormula)
        {
            string tempFinalFormula = finalFormula;
            List<string> processedFormulae = new List<string>();

            if (tempFinalFormula.ToLower().Contains("or") || tempFinalFormula.ToLower().Contains("then"))
            {
                //finalFormula = finalFormula.Replace("if", "");
                finalFormula = finalFormula.Replace("else", "or");
                finalFormula = finalFormula.Replace("then", "  ");
                finalFormula = finalFormula.Replace("%", " /100");
                tempFinalFormula = finalFormula;
                GeneralHelper helper = new GeneralHelper();
                string specialOperandFormula = helper.GetSpecialOperands(tempFinalFormula);
                if (!string.IsNullOrEmpty(specialOperandFormula))
                {
                    finalFormula = specialOperandFormula;
                } 

                if(finalFormula.Contains("if"))
                {
                    finalFormula = "not (" + finalFormula + " else true"+ ")";
                }
            }
            else
            {

                if (RegexHelper.ifStatementRegex.Matches(finalFormula).Count > 0)
                {
                    foreach (Match match in RegexHelper.ifStatementRegex.Matches(finalFormula))
                    {
                        finalFormula = "(" + match.Groups["conditionA"] + ") and (not (" + match.Groups["conditionB"] + "))";
                    }
                }
                else
                {
                    GeneralHelper helper = new GeneralHelper();
                    string specialOperandFormula = helper.GetSpecialOperands(finalFormula);
                    if (string.IsNullOrEmpty(specialOperandFormula))
                    {
                        finalFormula = "not (" + tempFinalFormula + ")";
                    }
                    else
                    {
                        finalFormula = "not (" + specialOperandFormula + ")";
                    }
                }
            }


            processedFormulae.Add(finalFormula);
            return processedFormulae;
        }

        public static List<string> ProcessedFinalFormula(string formula)
        {
            List<string> finalProcessedFormulae = new List<string>();
            string tempFormula = string.Empty;
            if (formula.Contains("=0"))
            {
                tempFormula = formula.Replace("=0", "==0");

            }
            if (tempFormula.Contains("!==0"))
            {
                tempFormula = tempFormula.Replace("!==0", "!=0");
            }
            if (tempFormula.Contains("<==0"))
            {
                tempFormula = tempFormula.Replace("<==0", "<=0");
            }
            if (!string.IsNullOrEmpty(tempFormula))
            {
                finalProcessedFormulae.Add(tempFormula);
            }
            else
            {
                finalProcessedFormulae.Add(formula);
            }

            return finalProcessedFormulae;
        }

        //End
    }

}
