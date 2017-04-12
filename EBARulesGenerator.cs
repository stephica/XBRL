using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using System.Globalization;
using System.Text.RegularExpressions;
using NLog;

namespace SphinxRulesGenerator
{

    class ColumnIndicesEBA
    {
        public static int CheckID = 1;
        public static int CheckName = 2;
        public static int CheckRationale = 3;
        public static int CheckType = 4;
        public static int CheckClass = 5;
        public static int CheckFormula = 6;
    }

    class ColumnIndicesExtendedEBA
    {
        public static int CheckID = 1;
        public static int Severity = 8;
        public static int T1 = 9;
        public static int T2 = 10;
        public static int T3 = 11;
        public static int Rows = 16;
        public static int Columns = 17;
        public static int Sheets = 18;
        public static int CheckFormula = 19;
        public static int CheckClass = 13;
        public static int Tolerance = 14;
        public static int Uitvoeren = 16;
    }

    public class EBARulesGenerator
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        // Given a big string like this: {C 01.00, r300}+{C 01.00, r340, c010} <= {C 05.01 , r160, c010} + {C 05.01 , r160, c020}
        // identify all cell references like: C 01.00, r340, c010
        private Regex cellReferenceRegex = new Regex("(?<={).*?(?=})");
        private Regex ifStatementRegex = new Regex("if[(](?'conditionA'.*);(?'conditionB'.*);(?'conditionC'.*)[)]");
        private Regex sheetNameRegEx = new Regex(".+\\((?<sheetName>.+)\\)");
        private string[] manuallyImplementedChecks = { }; //{ "C1008", "DNB_0053" };  

        // the DNB rules published on 2016-03-31 is much more structured and better organized. So the processing logic has to be improved a bit.
        // the old logic: method "Process" is retained separately
        public void ProcessAdvancedVersion(string mappingInputTextFile, string validationRulesExcelFile,
            string checkType, string sphinxRulesOutputFile, string rulesWorksheetName)
        {
            int rowIndex = 3;
            string errorSeverity = "error";

            if (!File.Exists(mappingInputTextFile))
            {
                throw new ApplicationException("Mapping input file does not exist!");
            }

            using (SLDocument validationExcel = new SLDocument(validationRulesExcelFile))
            using (StreamWriter sWriter = new StreamWriter(sphinxRulesOutputFile))
            {
                Dictionary<string, string> mapping = GeneralHelper.ReadMapping(mappingInputTextFile);
                string checkID = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.CheckID);
                string errorMessage = string.Empty;
                while (!string.IsNullOrEmpty(checkID))
                {
                    if (!manuallyImplementedChecks.Contains(checkID))
                    {
                        errorSeverity = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.Severity);

                        //If Severity is Non-Blocking then change the message as Warning.
                        if (errorSeverity.ToLower().Contains("non-blocking"))
                        {
                            errorSeverity = "warning";
                        }
                        string rawFormula = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.CheckFormula).ToLower();
                        string checkClass = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.CheckClass);
                        string toleranceString = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.Tolerance);
                        decimal tolerance = 0;
                        if (!string.IsNullOrEmpty(toleranceString))
                        {
                            Decimal.TryParse(toleranceString, out tolerance);
                        }
                        string[] splitter = new string[] { "; Checked formula:" };

                        if (checkClass.ToLower().Contains("plausibility"))
                        {
                            errorSeverity = "warning";
                        }
                        if (checkID == "v4336_s")
                        {

                        }
                        if (checkID == "v1551_m" || checkID=="v1552_m")
                        {
                            List<string> formulae = ProcessFormulaAdvanced(validationExcel, mapping, checkID, rowIndex, tolerance);
                            if (formulae.Count() > 0)
                            {
                                int counter = 0;
                                errorMessage = string.Format("DNB check {0} failed", checkID);
                                var EBAFormula = string.Empty;
                                foreach (string formula in formulae)
                                {
                                    if (formula.Contains("[eba_as:x2]"))
                                    {
                                        EBAFormula = formula.Replace("[eba_as:x2]", "\"IFRS\"");
                                    }
                                    else
                                    {
                                        EBAFormula = formula;
                                    }
                                    counter++;
                                    sWriter.Write("@name(\"" + checkType + ": " + checkID + ((formulae.Count > 1) ? "_" + counter.ToString() : String.Empty) + "\")\r\n" +
                                        "@description(\"" + checkType + ": " + checkID + "\")\r\n" +
                                        "raise " + Helper.MakeValidName(checkType + "_" + checkID) + ((formulae.Count > 1) ? "_" + counter.ToString() : String.Empty) + " severity " + errorSeverity + "\r\n" +
                                         EBAFormula + "\r\n" +
                                        "message \"" + errorMessage + ". Formula used: " + rawFormula + "\"\r\n\r\n");
                                }
                            }
                        }
                    }

                    rowIndex++;
                    checkID = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.CheckID);
                }
            }
        }




        public static List<string> ProcessFormulaAdvanced(SLDocument validationExcel, Dictionary<string, string> mapping,
            string checkID, int rowIndex, decimal tolerance)
        {
            List<string> processedFormulae = new List<string>();
            List<string> expandedFormulae = new List<string>();
            List<string> finalProcessedFormulae = new List<string>();
            string processedFormula = string.Empty;
            string sheetName = string.Empty;

            string rawFormula = validationExcel.GetCellValueAsString(rowIndex, 19).ToLower();
            string nameT1 = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.T1).ToLower().Replace(" ", "");
            string nameT2 = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.T2).ToLower().Replace(" ", "");
            string nameT3 = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.T3).ToLower().Replace(" ", "");
            string rows = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.Rows).ToLower();
            string columns = validationExcel.GetCellValueAsString(rowIndex, 17).ToLower();
            string specialCaseFormula = string.Empty;
            if (rawFormula.Contains("is a row identifier"))
            {
                specialCaseFormula = rawFormula.Remove(rawFormula.IndexOf("is a row identifier"));
                specialCaseFormula = specialCaseFormula.Replace(",", ",r999,");
            }
            //if (rawFormula.Contains("if") && rawFormula.Contains("then"))
            //{

            //    Console.WriteLine(checkID);

            //}
            if (string.IsNullOrEmpty(specialCaseFormula))
            {
                if (!string.IsNullOrEmpty(validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.Sheets).ToLower()))
                {
                    sheetName = "s" + validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtendedEBA.Sheets).ToLower().Replace("(", "").Replace(")", "");
                }
                MatchCollection sheetColl = RegexHelper.sheetNameRegEx.Matches(nameT1);
                if (sheetColl != null && sheetColl.Count > 0)
                {
                    sheetName = "s" + sheetColl[0].Groups["sheetName"].Value;
                    nameT1 = nameT1.Replace(sheetColl[0].Groups["sheetName"].Value, "").Replace("(", "").Replace(")", "");
                }

                List<string> rowReferences = new List<string>();
                List<string> columnReferences = new List<string>();
                rowReferences.AddRange(GeneralHelper.ProcessRowReference(rows));
                columnReferences.AddRange(GeneralHelper.ProcessColumnReference(columns));

                if (!string.IsNullOrEmpty(rawFormula))
                {
                    rawFormula = DataHelper.GetProcessedformula(rawFormula);
                    expandedFormulae.AddRange(GeneralHelper.GetASbsoluteReference(rowReferences, columnReferences, rawFormula, nameT1, checkID, sheetName));
                    if (expandedFormulae.Count == 0)
                    {
                        if (!(rawFormula.Contains("and") || rawFormula.Contains("or") || rawFormula.Contains("not") || rawFormula.Contains("if") || rawFormula.Contains("/")))
                        {
                            if (rowReferences.Count == 1 && columnReferences.Count == 1 && rowReferences[0] == "" && columnReferences[0] == "")
                            {
                                string id = checkID;
                                string formula = GeneralHelper.GetInnerBracketsFormula(rawFormula, checkID);
                                if (!string.IsNullOrEmpty(formula))
                                {
                                    expandedFormulae.Add(formula);
                                }
                            }
                        }
                    }
                }
            }

            else
            {
                expandedFormulae.Add(specialCaseFormula);
            }

            foreach (string expandedFormula in expandedFormulae)
            {
                string finalFormula = expandedFormula;
                foreach (Match match in RegexHelper.cellReferenceRegex.Matches(finalFormula))
                {
                    string cellReference = "{" + match.Value + "}";
                    string cellReferenceTrimmed = cellReference.Replace(" ", "");
                    if (mapping.ContainsKey(cellReferenceTrimmed))
                    {
                        finalFormula = finalFormula.Replace(cellReference, mapping[cellReferenceTrimmed]);

                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(specialCaseFormula))
                        {
                            var val = specialCaseFormula.Substring(specialCaseFormula.IndexOf(", c"));
                            val = val.Replace(",", "").Replace("}", "");
                            string col = val.Replace("c", "");
                            int tempVal = 0;
                            Int32.TryParse(col, out tempVal); 
                            string sTempVal = string.Empty;
                            if(tempVal.ToString().Length==2)
                            {
                                tempVal = tempVal + 10;
                                sTempVal = "c0" + tempVal;
                            }
                            else if(tempVal.ToString().Length == 1)
                            {
                                tempVal = tempVal + 15;
                                sTempVal = "c00" + tempVal;
                            }
                            else
                            {
                                tempVal = tempVal + 10;
                                sTempVal = "c" + tempVal;
                            }
                            specialCaseFormula = specialCaseFormula.Replace(val.TrimStart().TrimEnd(), sTempVal);
                            string specialCaseFormulaTrimmed= specialCaseFormula.Replace(" ", "");
                            if (mapping.ContainsKey(specialCaseFormulaTrimmed))
                            {
                                finalFormula = finalFormula.Replace(cellReference, mapping[specialCaseFormulaTrimmed]);

                            }

                        }
                        //Console.WriteLine("No DPM ID found for Cell reference: " + cellReference + ". Check ID = " + checkID);
                    }

                }
                processedFormulae.AddRange(GeneralHelper.ProcessedFormulaWithDPMIds(finalFormula));

            }

            foreach (string formula in processedFormulae)
            {
                finalProcessedFormulae.AddRange(GeneralHelper.ProcessedFinalFormula(formula));
            }


            if (finalProcessedFormulae.Count > 0)
            {
                return finalProcessedFormulae;
            }

            return processedFormulae;
        }


        // handle IF statements

        // replace {} with DPM codes
        // % of replace with '/100 * '
        // % * to be replaced with '/100 * '   
        // sum( can be replaced with (
        // '>' replace with '> '
        // <> replace with !=
        private string ProcessFormula(string ebaFormula, Dictionary<string, string> mapping, string checkID)
        {
            string processedFormula = ebaFormula.ToLower();
            if (!string.IsNullOrEmpty(processedFormula))
            {
                processedFormula = processedFormula.
                    //Replace(">", "> ").
                    Replace(" = ", " == ").
                    Replace("^=", "!=").
                    Replace("<>", "!=").
                    Replace("sum(", "(").
                    Replace("% of", "/100 * ").
                    Replace("% * ", "/100 * ");

                foreach (Match match in cellReferenceRegex.Matches(processedFormula))
                {
                    string cellReference = "{" + match.Value + "}";
                    string cellReferenceTrimmed = "{" + match.Value.Replace(" ", "") + "}";
                    if (mapping.ContainsKey(cellReferenceTrimmed))
                    {
                        processedFormula = processedFormula.Replace(cellReference, mapping[cellReferenceTrimmed]);
                    }
                    else
                    {
                        Console.WriteLine("No DPM ID found for Cell reference: " + cellReferenceTrimmed + ". Check ID = " + checkID);
                    }
                }


                if (ifStatementRegex.Matches(processedFormula).Count > 0)
                {
                    foreach (Match match in ifStatementRegex.Matches(processedFormula))
                    {
                        processedFormula = "(" + match.Groups["conditionA"] + ") and (not (" + match.Groups["conditionB"] + "))";
                    }
                }
                else
                {
                    processedFormula = "not (" + processedFormula + ")";
                }
            }
            return processedFormula;
        }


    }
}




