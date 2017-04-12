using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SphinxRulesGenerator
{
    public class SphinxConstantsWriter
    {
        //eba_dim:CEG=eba_GA:NL;


        public void Process(string ruleIDCountryNameMappingTextFile, string countryCodeNameMappingTextFile, 
            string sphinxStandardConstantsXsrFile, string constantIdsTextFile, 
            string sphinxConstantsOutputXsrFile, string sphinxRulesOutputXsrFile)
        {
            List<string> constantsLines = new List<string>();
            Dictionary<string, string> countryCodeNameMapping = new Dictionary<string,string>();

            using (StreamReader scountryCodeNameMappingReader = new StreamReader(countryCodeNameMappingTextFile))
            {
                while (!scountryCodeNameMappingReader.EndOfStream)
                {
                    string[] mapping = scountryCodeNameMappingReader.ReadLine().Split('=');
                    countryCodeNameMapping[mapping[0]] = mapping[1];
                }
            }

            using (StreamReader sConstantsReader = new StreamReader(sphinxStandardConstantsXsrFile))
            {
                while (!sConstantsReader.EndOfStream)
                {
                    constantsLines.Add(sConstantsReader.ReadLine());
                }
            }

            using (StreamReader sReader = new StreamReader(constantIdsTextFile))
            using (StreamWriter sWriter = new StreamWriter(sphinxConstantsOutputXsrFile))
            { 
                string constantID;
                while (!sReader.EndOfStream)
                {
                    constantID = sReader.ReadLine();
                    string requiredConstantLine = string.Empty;
                    string constantExpression = "constant DPM_ID_" + constantID;

                    foreach (string constantLine in constantsLines)
                    {
                        if (constantLine.Contains(constantExpression))
                        {
                            requiredConstantLine = constantLine;
                            break;
                        }                        
                    }

                    if (!string.IsNullOrEmpty(requiredConstantLine))
                    {
                        foreach (string countryCode in countryCodeNameMapping.Keys)
                        {
                            string constantCountryExpression = constantExpression + "_" + countryCode;
                            string constantRuleLine = requiredConstantLine.Replace(constantExpression, constantCountryExpression).
                                Replace("]", " eba_dim:CEG=" + countryCode.Replace("eba_", "eba_GA:") + "; ]");
                            sWriter.WriteLine(constantRuleLine);
                        }
                    }
                }
            }

            /*
             
             @name("DNB-E4027: Reconciliation C09.01 and C09.02 to C09.03 - ARMENIA")
@description("If C09.01 or C09.02 is reported for a given country, a total exposure amount per country is expected in C09.03")
raise Consistency.ReconciliationC0901AndC0902ToC0903ARMENIA severity error
not (if $DPM_ID_85032 >= 0.1*$DPM_ID_85033 then if ({ c09.01, r_070-160, c080} + {c 09.02, r030-140, c110}) >0, $DPM_ID_42719 >0)
message "If C09.01 or C09.02 is reported for a given country, a total exposure amount per country is expected in C09.03"
                        
             
             */


            using (StreamReader sReader = new StreamReader(ruleIDCountryNameMappingTextFile))
            using (StreamWriter sWriter = new StreamWriter(sphinxRulesOutputXsrFile))
            {
                while (!sReader.EndOfStream)
                {
                    string[] splitted = sReader.ReadLine().Split('=');
                    string countryCode = "error";

                    KeyValuePair<string, string> countryMatch = countryCodeNameMapping.FirstOrDefault(x => x.Value.Equals(splitted[1]));
                    if (!string.IsNullOrEmpty(countryMatch.Key))
                    {
                        countryCode = countryMatch.Key;
                    }
                    else
                    {
                        Console.WriteLine("Could not find the code for the country: " + splitted[1]);
                    }

                    string rule = "@name(\"DNB-" + splitted[0] + ": Reconciliation C09.01 and C09.02 to C09.03 - " + splitted[1] + "\")\r\n"
                                + "@description(\"If C09.01 or C09.02 is reported for a given country, a total exposure amount per country is expected in C09.03\")\r\n"
                                + "raise " + Helper.MakeValidName("ReconciliationC0901AndC0902ToC0903" + splitted[1]) + " severity error\r\n"
                                + "(($DPM_ID_85032 >= (0.1 * $DPM_ID_85033) and ($DPM_ID_89013_" + countryCode + " + $DPM_ID_89012_" + countryCode
                                + " + $DPM_ID_89011_" + countryCode + " + $DPM_ID_89010_" + countryCode + " + $DPM_ID_85578_" + countryCode + " + $DPM_ID_85572_"
                                + countryCode + " + $DPM_ID_108836_" + countryCode + " + $DPM_ID_85587_" + countryCode + " + $DPM_ID_85576_" + countryCode + " + $DPM_ID_85581_"
                                + countryCode + " + $DPM_ID_85577_" + countryCode + " + $DPM_ID_85575_" + countryCode + " + $DPM_ID_85588_" + countryCode + " + $DPM_ID_85804_" 
                                + countryCode + " + $DPM_ID_85802_" + countryCode + " + $DPM_ID_85800_" + countryCode + " + $DPM_ID_85798_" + countryCode + " + $DPM_ID_85796_" 
                                + countryCode + " + $DPM_ID_85790_" + countryCode + " + $DPM_ID_85786_" + countryCode + " + $DPM_ID_85794_" + countryCode + " + $DPM_ID_85792_" 
                                + countryCode + " + $DPM_ID_85788_" + countryCode + " + $DPM_ID_85784_" + countryCode + " + $DPM_ID_85391_" + countryCode
                                + ") > 0)) and missing ($DPM_ID_42719_" + countryCode + ")\r\n"
                                + "message \"If C09.01 or C09.02 is reported for a given country, a total exposure amount per country is expected in C09.03. Country = "
                                + splitted[1] + "\"\r\n\r\n";
                    sWriter.WriteLine(rule);
                }
            }
        }
    }
}
