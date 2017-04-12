using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SphinxRulesGenerator
{
    public class CreateExposureMacro
    {
        private Regex checkReference = new Regex(@"(?<TM>\w*\DPM_ID_\w*)");
        public static readonly string _currentDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        public List<string> GetListofDPMIDS()
        {

            List<string> listofMacros = new List<string>();
            var listofConstants=new List<string>();
            using (StreamReader reader = new StreamReader(_currentDirectoryPath + "Input_DNB_Rules.txt"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("not"))
                    {

                        MatchCollection matchGreaterThanReference = checkReference.Matches(line);
                        listofConstants = matchGreaterThanReference.Cast<Match>().Select(m => m.Value).ToList();
                        listofMacros.AddRange(listofConstants);
                    }
                   
                }
                reader.Close();
            }
            return listofMacros;
        }

        public void WriteAllMacros()
        {
            List<string> macrolist = new List<string>();
            List<string> macrolistNotFound = new List<string>();
            List<string> list = GetListofDPMIDS();
            string filePath = AppDomain.CurrentDomain.BaseDirectory; 
            
            using (StreamReader reader = new StreamReader(_currentDirectoryPath + "ALG.CRDIV.Constants.xsr"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("constant DPM_ID"))
                    {
                        Match matchGreaterThanReference = checkReference.Match(line);
                        bool val = list.Any(x => x== matchGreaterThanReference.Value);
                        if(val)
                        {
                            macrolist.Add(line);
                        }
                        else
                        {
                            macrolistNotFound.Add(matchGreaterThanReference.Value);
                        }

                    }

                }
               
            }



            using (System.IO.StreamWriter filetoWritwe = new System.IO.StreamWriter(_currentDirectoryPath + "tempMacro.txt"))
            {
                foreach (string listItem in macrolist)
                {
                    filetoWritwe.WriteLine(listItem);

                }
            }
             

            using (System.IO.StreamWriter filetoWritweN = new System.IO.StreamWriter(_currentDirectoryPath + "NotUsed.txt"))
            {
                foreach (string listItem in macrolistNotFound)
                {
                    filetoWritweN.WriteLine(listItem);

                }
            }
           
        }


        public static string Between(string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            int posB = value.LastIndexOf(b);
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        public static bool HasValue(string dpmID, List<string> listofMappings)
        {
            foreach (var item in listofMappings)
            {
                if (item.Equals(dpmID))
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }

            return false;
        }
    }
}
