using SphinxRulesGenerator.hELPERS;
using SphinxRulesGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxRulesGenerator
{
    class Program
    {
        static void ShowUsage()
        {
            Console.WriteLine("Invalid arguments!\r\n" +
                "Usages:\r\n" +
                "SphinxRulesGenerator.exe mapping dpm_id_mapping_template_excel.xlsx MappingOutput.txt\r\n" +
                "SphinxRulesGenerator.exe rules MappingInput.txt ValidationRules.xlsx check_type SphinxRulesOutput.xsr \"DNB ... checks\" (Rules worksheet name) \r\n" +
                "SphinxRulesGenerator.exe constants ruleIDCountryNameMappingTextFile countryCodeNameMappingTextFile sphinxStandardConstantsXsrFile constantIdsTextFile sphinxConstantsOutputXsrFile sphinxRulesOutputXsrFile\r\n");
            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {

            switch (args[0])
            {
                case "mapping":
                    if (args.Length < 3)
                    {
                        ShowUsage();
                    }
                    DpmIdMappingGenerator mappingGenerator = new DpmIdMappingGenerator();
                    mappingGenerator.Process(args[1], args[2]);
                    break;
                case "rules":
                    if (args.Length < 6)
                    {
                        ShowUsage();
                    }
                    string checkType = "DNB Consistency Plausibility check";
                    if (args[2].ToString().StartsWith("EBA"))
                    {
                        EBARulesGenerator ebWriter = new EBARulesGenerator();
                        ebWriter.ProcessAdvancedVersion(args[1], args[2], checkType, args[4], args[5]);
                    }
                    else
                    {
                        SphinxRulesWriter spWriter = new SphinxRulesWriter();
                        spWriter.ProcessAdvancedVersion(args[1], args[2], checkType, args[4], args[5]);
                    }
                    break;
                case "constants":
                    //constants -m "temp.text"
                    if (args != null && args[1] == "-m")
                    {
                        List<string> allDPMIDS = FaultConstantGenerator.AllDPMIDsfromExcel();
                        List<Constants> modifiedconstants = FaultConstantGenerator.GetModifiedConstantDefinitions(allDPMIDS);
                        FaultConstantGenerator.WriteAllModifiedConstants(modifiedconstants, args[2]);
                        ProcessStartInfo StartInformation = new ProcessStartInfo();
                        StartInformation.FileName = Path.Combine(FaultConstantGenerator._rootFilePath, "ProcessedFiles");
                        Process process = Process.Start(StartInformation);
                        //process.EnableRaisingEvents = true;
                    }
                    //constants -m "temp.text"
                    else if (args != null && args[1] == "-a")
                    {
                        List<string> allDPMIDS = FaultConstantGenerator.AllDPMIDsfromExcel();
                        List<Constants> modifiedconstants = FaultConstantGenerator.GetModifiedConstantDefinitions(allDPMIDS);
                        FaultConstantGenerator.WriteAllNewlyAddedConstants(modifiedconstants, args[2]);
                        ProcessStartInfo StartInformation = new ProcessStartInfo();
                        StartInformation.FileName = Path.Combine(FaultConstantGenerator._rootFilePath, "ProcessedFiles");
                        Process process = Process.Start(StartInformation);
                        //process.EnableRaisingEvents = true;
                    }
                    else
                    {
                        SphinxConstantsWriter srWriter = new SphinxConstantsWriter();
                        srWriter.Process(args[1], args[2], args[3], args[4], args[5], args[6]);
                    }

                    break;

                default:
                    ShowUsage();
                    break;
            }
            Console.WriteLine("Task Completed...");
            Console.ReadKey();
        }




        static void GetFormulawithSheet()
        {
            List<int> Sheets = new List<int>();
            List<string> list = new List<string>();
            for (Int32 i = 001; i <= 017; i++)
            {
                Sheets.Add(i);
            }
            string val1 = "c07.00.a,r030,c200";
            string val2 = "c07.00.a,r030,c150";
            foreach (int i in Sheets)
            {
                list.Add("{" + val1 + "," + "s" + i + "}" + "<=" + "{" + val2 + "," + "s" + i + "}");
            }
            string filePath = AppDomain.CurrentDomain.BaseDirectory;
            using (System.IO.StreamWriter filetoWritwe = new System.IO.StreamWriter(filePath + "temp.txt"))
            {
                foreach (string listItem in list)
                {
                    filetoWritwe.WriteLine(listItem);

                }
            }
        }
    }
}
