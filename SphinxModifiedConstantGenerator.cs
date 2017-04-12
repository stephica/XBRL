using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SphinxRulesGenerator
{
    public class SphinxModifiedConstantGenerator
    {
        public static readonly string _currentDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        public void GetDta()
        {
            List<string> dpmIds = GetDPMIds("4.txt");
            int count = 0;
            //Get a List of all DPM_IDS  List<DPMIDS>
            //Read the constant file
            using (StreamReader sReader = new StreamReader(_currentDirectoryPath + "\\" + "ALG.CRDIV.Constants.txt"))
            using (StreamWriter sWriter = new StreamWriter(_currentDirectoryPath + "\\" + "Modified_ALG.CRDIV.Constants.txt"))
            {
                string line;
                string text = "eba_dim:OGR=none;unit=unit(xbrli:pure);]";
                while ((line = sReader.ReadLine()) != null)
                {
                    if (line.Contains("=eba_met:"))
                    {
                        var data = line.Remove(line.IndexOf("=eba_met:"));
                        data = data.Replace("constant ", "");
                        if (dpmIds.Contains(data))
                        {
                            if (!(line.Contains(text)))
                            {
                                line = line.Replace("]", text);
                                count++;
                            }
                        }
                    }

                    sWriter.WriteLine(line);
                }
                
            }
        }

        public List<string> GetDPMIds(string filename)
        {
            List<string> listofIds = new List<string>();
            using (StreamReader sReader = new StreamReader(_currentDirectoryPath + "\\" + filename))
            {
                string line;
                while ((line = sReader.ReadLine()) != null)
                {
                    if (line.Contains("="))
                    {
                        var data = line.Substring(line.IndexOf('=') + 1);
                        data = data.Replace("$", "");
                        listofIds.Add(data);
                    }
                }
            }
            return listofIds;
        }


    }

    //foreach List of constants
    //If any constant exist in List<DPMIDS>
    //change its definition
    //write it to the regenerated definition file 
}



