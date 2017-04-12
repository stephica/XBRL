using SphinxRulesGenerator.Helpers;
using SphinxRulesGenerator.Models;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SphinxRulesGenerator
{
    public class FaultConstantGenerator
    {
        public static readonly string _currentDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string _rootFilePath = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.FullName;
        public static List<string> GetListofNotUsedConstants()
        {
            string mappingFilePath = _currentDirectoryPath + "SphinxRulesOutput_Consistency.xsr";
            Dictionary<int, string> dictGetUsedConstants = new Dictionary<int, string>();
            if (File.Exists(mappingFilePath))
            {

                dictGetUsedConstants = GeneralHelper.ReadMappingFromXsr(mappingFilePath);
            }
            if (dictGetUsedConstants.Count <= 0)
            {
                return new List<string>();
            }

            List<string> listofMappings = new List<string>();
            listofMappings = dictGetUsedConstants.Values.ToList();


            List<string> listofNotUsedConstants = new List<string>();
            using (StreamReader reader = new StreamReader(_currentDirectoryPath + "ALG.CRDIV.Constants.xsr"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("constant DPM_ID"))
                    {
                        string val = "$" + ClassHelpers.Between(line, "constant ", "=eba_met");
                        if (!ClassHelpers.HasValue(val, listofMappings))
                        {
                            line = "//" + line;
                            listofNotUsedConstants.Add(line);
                        }
                        else
                        {
                            listofNotUsedConstants.Add(line);
                        }
                    }
                }
                reader.Close();
            }
            return listofNotUsedConstants;
        }

        public static void WriteAllNotUsedConstants(List<string> listofNotUsedConstants)
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory;
            using (System.IO.StreamWriter filetoWritwe = new System.IO.StreamWriter(_currentDirectoryPath + "temp.txt"))
            {
                foreach (string listItem in listofNotUsedConstants)
                {
                    filetoWritwe.WriteLine(listItem);

                }
            }
        }
        public static List<string> AllDPMIDsfromExcel()
        {
            List<string> dpmIDs = new List<string>();
            if (File.Exists(_rootFilePath + "//Resources//DPMIDS.xlsx"))
            {
                using (SLDocument inputExcel = new SLDocument(_rootFilePath + "//Resources//DPMIDS.xlsx"))
                {
                    for (int i = 2; i <= 121; i++)
                    {
                        string dpmId = inputExcel.GetCellValueAsString(i, 1);
                        if (!string.IsNullOrEmpty(dpmId))
                        {
                            dpmIDs.Add("DPM_ID_" + dpmId);
                        }
                    }
                }
            }

            return dpmIDs;
        }

        public static void RemoveAllExistingDpmIdDefinitions(List<string> dpmIDs)
        {
            List<string> listofAllConstants = new List<string>();
            using (StreamReader reader = new StreamReader(_currentDirectoryPath + "ALG.CRDIV.Constants.xsr"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("constant DPM_ID"))
                    {
                        string val = ClassHelpers.Between(line, "constant ", "=eba_met");
                        if (!ClassHelpers.HasValue(val, dpmIDs))
                        {
                            line = "";
                            listofAllConstants.Add(line);
                        }
                        else
                        {
                            listofAllConstants.Add(line);
                        }
                    }
                }
                reader.Close();
            }
        }

        public static List<Constants> GetModifiedConstantDefinitions(List<string> dpmIds)
        {
            List<Constants> allconstsants = new List<Constants>();
            List<Constants> constsants = new List<Constants>();
            if (File.Exists(_rootFilePath + "//Resources//Constants.xlsx"))
            {
                using (SLDocument inputExcel = new SLDocument(_rootFilePath + "//Resources//Constants.xlsx"))
                {
                    for (int i = 2; i <= 57407; i++)
                    {
                        string definition = inputExcel.GetCellValueAsString(i, 6);
                        string dpmID = ClassHelpers.Between(definition, "constant ", "=eba_met");
                        allconstsants.Add(new Constants
                        {
                            Definitions = definition,
                            DPMID = dpmID
                        });
                    }


                }

                foreach (var item in dpmIds)
                {
                    var aaaa = allconstsants.Where(x => x.DPMID == item).FirstOrDefault();
                    if (aaaa != null)
                    {

                    }
                    constsants.Add(allconstsants.Where(x => x.DPMID == item).FirstOrDefault());
                }
            }

            return constsants;
        }

        public static void WriteAllModifiedConstants(List<Constants> modifiedDefinitions,string fileName)
        {
            //Get All Constants from Existing files
            List<string> listofAllConstants = new List<string>();
            using (StreamReader reader = new StreamReader(_currentDirectoryPath + "ALG.CRDIV.Constants.xsr"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    listofAllConstants.Add(line);
                }
                reader.Close();
            }

            //constant DPM_ID_10006=eba_met:md106[ eba_dim:ALO=eba_IM:x1;eba_dim:APL=eba_PL:x4;eba_dim:BAS=eba_BA:x6;eba_dim:MCY=eba_MC:x143;unit=unit(iso4217:EUR);]

            string fullFilePath = Path.Combine(_rootFilePath, "Resources", fileName);
            using (System.IO.StreamWriter filetoWritwe = new StreamWriter(File.Open(fullFilePath, System.IO.FileMode.Append)))
            {
                foreach (string listItem in listofAllConstants)
                {
                    if (listItem.StartsWith("constant DPM_ID"))
                    {
                        string constantID = ClassHelpers.Between(listItem, "constant ", "=eba_met");
                        var val = modifiedDefinitions.Where(x => x.DPMID.Equals(constantID));
                        if (val.Count() != 0)
                        {
                            string modifiedDefinition = modifiedDefinitions.Where(x => x.DPMID.Equals(constantID)).FirstOrDefault().Definitions;
                            filetoWritwe.WriteLine(modifiedDefinition);
                        }
                        else
                        {
                            filetoWritwe.WriteLine(listItem);
                        }

                    }
                    else if (listItem.StartsWith("macro DPM_ID"))
                    {
                        string constantID = ClassHelpers.Between(listItem, "macro ", "() eba_met");
                        var val = modifiedDefinitions.Where(x => x.DPMID.Equals(constantID));
                        if (val.Count() != 0)
                        {
                            string modifiedDefinition = modifiedDefinitions.Where(x => x.DPMID.Equals(constantID)).FirstOrDefault().Definitions;
                            string existingDefinition = ClassHelpers.Between(listItem.Replace("; ]", ";]"), "[", ";]");
                            modifiedDefinition = ClassHelpers.Between(modifiedDefinition, "[", ";]");
                            string finalItem = listItem.Replace(existingDefinition, modifiedDefinition);
                            filetoWritwe.WriteLine(finalItem);
                        }
                        else
                        {
                            filetoWritwe.WriteLine(listItem);
                        }
                    }

                    else
                    {
                        filetoWritwe.WriteLine(listItem);
                    }
                }

            }
        }


    }
}
