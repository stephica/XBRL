using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using System.Text.RegularExpressions;

namespace SphinxRulesGenerator
{


    public class DpmIdMappingGenerator
    {
        public static readonly string _currentDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        List<DpmIdMappingEntry> dpmIdMappingEntry = new List<DpmIdMappingEntry>();

        // For each worksheet
        //  Find the first non-empty cell in Column D
        //  Calculate the top right diagonal column
        //  For each row/column combination: store the mapping info in a list
        // Finally dump all that in the output file

        public void Process(string dpmIdExcelInputFile, string outputMappingTextFile)
        {
           
            Regex hasOnlyDigits = new Regex(@"^\$DPM_ID_\[0-9]");
            if (!File.Exists(dpmIdExcelInputFile))
            {
                throw new ApplicationException("Input Excel file does not exist!");
            }

            using (SLDocument inputExcel = new SLDocument(dpmIdExcelInputFile))
            {
                foreach (string sheetName in inputExcel.GetSheetNames(true))
                {
                    string tempSheetName = sheetName.ToLower();
                    if (tempSheetName[0] != 'c' && tempSheetName[0] != 'f' && tempSheetName[0] != 'p')
                    {
                        continue;
                    }

                    const int columnIndexOfFirstColumnIdentifer = 4;
                    const int rowIdentifierColumn = 3;
                    int rowIndexOfFirstRowIdentifer = 1;
                    int emptyRowIdentifierCount = 0;

                    if (inputExcel.SelectWorksheet(sheetName))
                    {
                        string rowIdentifier = string.Empty;
                        string columnIdentifier = string.Empty;
                        int rowIndex = 1;
                        while (string.IsNullOrEmpty(rowIdentifier))
                        {
                            rowIdentifier = inputExcel.GetCellValueAsString(rowIndex, rowIdentifierColumn);
                            rowIndex++;
                        }

                        rowIndex = rowIndex - 1;
                        rowIndexOfFirstRowIdentifer = rowIndex;

                        //while (!string.IsNullOrEmpty(rowIdentifier))
                        while (emptyRowIdentifierCount < 25)
                        {
                            if (string.IsNullOrEmpty(rowIdentifier))
                            {
                                emptyRowIdentifierCount++;
                            }
                            else
                            {
                                emptyRowIdentifierCount = 0;
                            }

                            int columnIndex = columnIndexOfFirstColumnIdentifer;
                            int j = 1;
                            while (string.IsNullOrEmpty(columnIdentifier) && (rowIndexOfFirstRowIdentifer - j) > 0)
                            {
                                columnIdentifier = inputExcel.GetCellValueAsString((rowIndexOfFirstRowIdentifer - j), columnIndex);
                                j++;
                            }
                            j--;
                            while (!string.IsNullOrEmpty(columnIdentifier))
                            {
                                string dpmId = GetDPMID(inputExcel.GetCellValueAsString(rowIndex, columnIndex));
                                Match match = hasOnlyDigits.Match(dpmId);
                                if (!string.IsNullOrEmpty(dpmId))
                                {
                                    string cellReferencePrefix = string.Empty;
                                    string adjustedSheetName = sheetName;
                                    if (sheetName.Contains("(") && sheetName.Contains(")"))
                                    {
                                        adjustedSheetName = sheetName.Split('(')[0];
                                        cellReferencePrefix = ",s" + sheetName.Split('(')[1].Replace(")", "");
                                    }

                                    // {C 05.01, r211, c060}
                                    string cellReference = "{" + adjustedSheetName + ",r" + rowIdentifier + ",c" + columnIdentifier + cellReferencePrefix + "}";
                                    cellReference = cellReference.Replace(" ", string.Empty).ToLower();

                                    dpmIdMappingEntry.Add(new DpmIdMappingEntry()
                                    {
                                        CellReference = cellReference,
                                        DpmVariableName = "$DPM_ID_" + dpmId
                                    });
                                }
                                columnIndex++;
                                columnIdentifier = inputExcel.GetCellValueAsString((rowIndexOfFirstRowIdentifer - j), columnIndex);
                            }
                            rowIndex++;
                            rowIdentifier = inputExcel.GetCellValueAsString(rowIndex, rowIdentifierColumn);
                        }
                    }
                }
            }

            if (dpmIdMappingEntry.Count > 0)
            {
                using (StreamWriter sWriter = new StreamWriter(outputMappingTextFile))
                {
                    foreach (DpmIdMappingEntry entry in dpmIdMappingEntry)
                    {
                        sWriter.WriteLine(entry.CellReference + "=" + entry.DpmVariableName);
                        string adjustedCellReference = entry.CellReference
                            .Replace("{c07.01.a", "{c07.01")
                            .Replace("{c07.01.b", "{c07.01")
                            .Replace("{c07.01.c", "{c07.01")
                            .Replace("{c08.01.a", "{c08.01")
                            .Replace("{c08.01.b", "{c08.01")
                            ;
                        if (!adjustedCellReference.Equals(entry.CellReference))
                        {
                            sWriter.WriteLine(adjustedCellReference + "=" + entry.DpmVariableName);

                        }
                    }
                }
            }

            GeneratePureMappings();
        }


        public void GeneratePureMappings()
        {
            List<string> listofNotUsedConstants = new List<string>();
            using (StreamReader reader = new StreamReader(_currentDirectoryPath + "Mapping.txt"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!(line.Contains("$DPM_ID_â‚¬Â£$") || line.Contains("$DPM_ID_ %") || line.Contains("$DPM_ID_€£$")|| line.Contains("$DPM_ID_(")|| line.Contains("$DPM_ID_%")|| line.Contains("$DPM_ID_#")|| line.Contains("$DPM_ID_<")))
                    {
                        listofNotUsedConstants.Add(line);
                    }
                }
                reader.Close();
            }

            using (System.IO.StreamWriter filetoWritwe = new System.IO.StreamWriter(_currentDirectoryPath + "Mapping.txt"))
            {
                foreach (string listItem in listofNotUsedConstants)
                {
                    filetoWritwe.WriteLine(listItem);

                }
            }
        }

        public string GetDPMID(string dpmID)
        {
            if (dpmID.Contains("_x000D"))
            {
                dpmID = dpmID.Remove(dpmID.IndexOf("_x000D"));
            }
            return dpmID;
        }
    }
}
