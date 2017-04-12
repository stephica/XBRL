using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SphinxRulesGenerator.hELPERS
{
    public class CreateConstants
    {
        public void Create()
        {

            StringBuilder sb = new StringBuilder();
            //string dpmID = "149080";
            string contextRef = string.Empty;
            List<string> listofDPMIds = new List<string>();
            listofDPMIds.Add("140716");
            listofDPMIds.Add("140718");
            listofDPMIds.Add("140720");
            listofDPMIds.Add("140722");
            listofDPMIds.Add("149426");
            listofDPMIds.Add("140728");
            listofDPMIds.Add("140719");
            listofDPMIds.Add("140721");
            listofDPMIds.Add("140723");
            listofDPMIds.Add("140725");
            listofDPMIds.Add("140727");
            listofDPMIds.Add("140713");
            listofDPMIds.Add("140708");
            listofDPMIds.Add("140709");
            listofDPMIds.Add("140710");
            listofDPMIds.Add("140712");
            listofDPMIds.Add("140717");
            listofDPMIds.Add("140714");
            listofDPMIds.Add("140715");
            listofDPMIds.Add("140711");

            //using (StreamReader reader = new StreamReader(@"C:\Users\ABasu\Documents\SphinxRulesGenerator\bin\Debug\Mapping.txt"))
            //{
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        if (line.StartsWith("{c66.00.w"))
            //        {
            //            string[] splitChar = line.Split('=');
            //            string val = splitChar[1];
            //            val = val.Replace("$DPM_ID_", "");
            //            listofDPMIds.Add(val);
            //        }
            //    }
            //    reader.Close();
            //}

            var listofDPM = listofDPMIds;
            XmlDocument doc = new XmlDocument();
            doc.Load(@"C:\Users\ABasu\Documents\SphinxRulesGenerator\Helpers\test.xml");
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("xbrli", "http://purl.org/dc/elements/1.1/");
            List<string> listofResult = new List<string>();
            foreach (string dpmID in listofDPMIds)
            {
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.InnerText == dpmID)
                    {
                        contextRef = node.Attributes["contextRef"].Value;
                    }
                }
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.Attributes["id"] != null)
                    {
                        if (node.Attributes["id"].Value == contextRef)
                        {
                            XmlNodeList list = node.ChildNodes;
                            foreach (XmlNode item in list)
                            {
                                if (item.Name == "xbrli:scenario")
                                {
                                    XmlNode nodeL = item;
                                    using (System.IO.StreamWriter filetoWritwe = new System.IO.StreamWriter(@"C:\Users\ABasu\Documents\SphinxRulesGenerator\hELPERS\1.txt"))
                                    {
                                        foreach (XmlNode nodee in nodeL)
                                        {
                                            filetoWritwe.WriteLine(nodee.OuterXml);
                                        }

                                    }
                                    using (StreamReader reader = new StreamReader(@"C:\Users\ABasu\Documents\SphinxRulesGenerator\hELPERS\1.txt"))
                                    {
                                        string line;
                                        string tempVal = string.Empty;
                                        while ((line = reader.ReadLine()) != null)
                                        {
                                            int index = line.IndexOf("eba_dim:");
                                            string val = line.Remove(0, index - 1);
                                            val = val.Replace(">", "=").Replace("xmlns:xbrldi=\"http://xbrl.org/2006/xbrldi", "");
                                            val = val.Remove(val.IndexOf("</"));
                                            val = val.Replace("\"", "").Replace(" = ", "=");
                                            sb.AppendFormat("{0};", val.Replace(" =", "="));
                                            tempVal = string.Format("constant DPM_ID_{0}=eba_met:mi353[ {1} ]", dpmID, sb.ToString());

                                        }
                                        listofResult.Add(tempVal);
                                        reader.Close();
                                    }
                                }
                            }
                        }
                    }

                }
            }

            foreach (var item in listofResult)
            {
                Console.WriteLine(item);
            }
            //using (System.IO.StreamWriter filetoWritwe = new System.IO.StreamWriter(@"C:\Users\ABasu\Documents\SphinxRulesGenerator\Helpers\3.txt"))
            //{
            //    foreach (string listItem in listofDPMIds)
            //    {
            //        filetoWritwe.WriteLine(string.Format("constant DPM_ID_{0}=eba_met:mi76[ eba_dim:BAS=eba_BA:x14;eba_dim:CCA=eba_CA:x1;eba_dim:CUS=eba_CU:AFN;eba_dim:MCY=eba_MC:x146; ]", listItem));

            //    }
            //}

            Console.ReadKey();
        }
    }
}
