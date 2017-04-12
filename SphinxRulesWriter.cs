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
    class ColumnIndices
    {
        public static int CheckID = 1;
        public static int CheckName = 2;
        public static int CheckRationale = 3;
        public static int CheckType = 4;
        public static int CheckClass = 5;
        public static int CheckFormula = 6;
    }

    class ColumnIndicesExtended
    {
        public static int CheckID = 1;
        public static int T1 = 2;
        public static int T2 = 3;
        public static int T3 = 4;
        public static int Rows = 9;
        public static int Columns = 10;
        public static int Sheets = 11;
        public static int CheckFormula = 12;
        public static int CheckClass = 13;
        public static int Tolerance = 14;
        public static int Uitvoeren = 16;
    }

    //@name("DNB-C27: CET1 deduction if AT1 deductions exceed AT1 capital")
    //@description("Rationale: If the AT1 deductions exceed the amount of AT1 instruments, this line should be lower than zero; Checked formula: {C 01.00, r440, c010} = -{C 01.00, r740, c010}")
    //raise CET1DeductionsWithRespectToAT1 severity warning
    //$DPM_ID_33407 != (-($DPM_ID_33408))  
    //message "CET1 deduction if AT1 deductions exceed AT1 capital: If the AT1 deductions exceed the amount of AT1 instruments, this line should be lower than zero."


    public class SphinxRulesWriter
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
            int rowIndex = 2;

            if (!File.Exists(mappingInputTextFile))
            {
                throw new ApplicationException("Mapping input file does not exist!");
            }
            if (!File.Exists(validationRulesExcelFile))
            {
                throw new ApplicationException("Validation rules file does not exist!");
            }

            using (SLDocument validationExcel = new SLDocument(validationRulesExcelFile, "DNBConsistency Plausibility"))
            using (StreamWriter sWriter = new StreamWriter(sphinxRulesOutputFile))
            {
                Dictionary<string, string> mapping = GeneralHelper.ReadMapping(mappingInputTextFile);
                string checkID = validationExcel.GetCellValueAsString(2,1);
                string errorMessage = string.Empty;
                while (!string.IsNullOrEmpty(checkID))
                {
                    if (!manuallyImplementedChecks.Contains(checkID))
                    {
                        string errorSeverity = "error";
                        string rawFormula = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.CheckFormula).ToLower();
                        string checkClass = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.CheckClass);
                        string toleranceString = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.Tolerance);
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
                        if (checkID == "DNB_0022")
                        {

                        }
                        List<string> formulae = ProcessFormulaAdvanced(validationExcel, mapping, checkID, rowIndex, tolerance);
                        if (formulae.Count() > 0)
                        {
                            int counter = 0;
                            errorMessage = string.Format("DNB check {0} failed", checkID);
                            foreach (string formula in formulae)
                            {
                                counter++;
                                sWriter.Write("@name(\"" + checkType + ": " + checkID + ((formulae.Count > 1) ? "_" + counter.ToString() : String.Empty) + "\")\r\n" +
                                    "@description(\"" + checkType + ": " + checkID + "\")\r\n" +
                                    "raise " + Helper.MakeValidName(checkType + "_" + checkID) + ((formulae.Count > 1) ? "_" + counter.ToString() : String.Empty) + " severity " + errorSeverity + "\r\n" +
                                     formula + "\r\n" +
                                    "message \"" + errorMessage + ". Formula used: " + rawFormula + "\"\r\n\r\n");
                            }
                        }


                    }

                    rowIndex++;
                    checkID = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.CheckID);
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

            string rawFormula = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.CheckFormula).ToLower();
            string nameT1 = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.T1).ToLower().Replace(" ", "");
            string nameT2 = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.T2).ToLower().Replace(" ", "");
            string nameT3 = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.T3).ToLower().Replace(" ", "");
            string rows = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.Rows).ToLower();
            string columns = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.Columns).ToLower();
            string uitvoeren = validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.Uitvoeren).ToLower();
            //if (!(DataHelper.HasExecuteFormula(uitvoeren)))
            //{
            //    //Console.WriteLine(checkID);
            //    return new List<string>();
            //}
            if (rawFormula.Contains("if") && rawFormula.Contains("then"))
            {

                Console.WriteLine(checkID);

            }
            if (!string.IsNullOrEmpty(validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.Sheets).ToLower()))
            {
                sheetName = "s" + validationExcel.GetCellValueAsString(rowIndex, ColumnIndicesExtended.Sheets).ToLower().Replace("(", "").Replace(")", "");
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

                //if (expandedFormulae.Count == 1)
                //{
                //    if (!(expandedFormulae[0].Contains("max") || expandedFormulae[0].Contains("min")))
                //    {
                //        Match match = RegexHelper.Pattern_7.Match(expandedFormulae[0]);
                //        if (match.Success)
                //        {
                //            string formula = GeneralHelper.GetInnerBracketsFormula(rawFormula, checkID);
                //            if (!string.IsNullOrEmpty(formula))
                //            {
                //                expandedFormulae.Add(formula);
                //            }
                //        }
                //    }

                //}

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

                            //Console.WriteLine("No DPM ID found for Cell reference: " + cellReference + ". Check ID = " + checkID);
                        }

                    }
                    processedFormulae.AddRange(GeneralHelper.ProcessedFormulaWithDPMIds(finalFormula));

                }

                foreach (string formula in processedFormulae)
                {
                    finalProcessedFormulae.AddRange(GeneralHelper.ProcessedFinalFormula(formula));
                }
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

        /*
        public void Process(string mappingInputTextFile, string validationRulesExcelFile, string checkTypeFilterString, string sphinxRulesOutputFile)
        {
            int rowIndex = 2;

            if (!File.Exists(mappingInputTextFile))
            {
                throw new ApplicationException("Mapping input file does not exist!");
            }
            if (!File.Exists(validationRulesExcelFile))
            {
                throw new ApplicationException("Validation rules file does not exist!");
            }

            using (SLDocument validationExcel = new SLDocument(validationRulesExcelFile, "Checks - overview"))
            using (StreamWriter sWriter = new StreamWriter(sphinxRulesOutputFile))
            {
                Dictionary<string, string> mapping = ReadMapping(mappingInputTextFile);
                string checkID = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckID);
                while (!string.IsNullOrEmpty(checkID))
                {
                    if (!manuallyImplementedChecks.Contains(checkID))
                    {
                        string checkType = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckType);

                        if (string.IsNullOrEmpty(checkTypeFilterString) || checkTypeFilterString.Equals(checkType))
                        {
                            string errorSeverity = "error";
                            string checkName = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckName);
                            string checkRationale = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckRationale);
                            string checkClass = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckClass);
                            string checkFormula = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckFormula);
                            string[] splitter = new string[] { "; Checked formula:" };

                            if (checkClass.ToLower().Contains("plausibility"))
                            {
                                errorSeverity = "warning";
                            }

                            sWriter.Write("@name(\"DNB-" + checkID + ": " + checkName + "\")\r\n" +
                               "@description(\"" + checkRationale + "\")\r\n" +
                               "raise " + Helper.MakeValidName(checkName) + checkID + " severity " + errorSeverity + "\r\n" +
                               ProcessFormula(checkFormula, mapping, checkID) + "\r\n" +
                               "message \"" + checkRationale + " Formula used: " + checkFormula + "\"\r\n\r\n");
                        }
                    }
                    rowIndex++;
                    checkID = validationExcel.GetCellValueAsString(rowIndex, ColumnIndices.CheckID);
                }
            }
        }
        */
    }
}


