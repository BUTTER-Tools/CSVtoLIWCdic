using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PluginContracts;
using OutputHelperLib;
using System.Text;
using System.IO;
using System.Linq;
using TSOutputWriter;


namespace CSVtoLIWCdic
{
    public class CSVtoLIWCdic : LinearPlugin
    {


        public string[] InputType { get; } = { "CSV File" };
        public string OutputType { get; } = "LIWC Dictionary File";

        public Dictionary<int, string> OutputHeaderData { get; set; } = new Dictionary<int, string>() { { 0, "TokenizedText" } };
        public bool InheritHeader { get; } = false;

        public string StatusToReport { get; set; } = "";

        #region Plugin Details and Info

        public string PluginName { get; } = "CSV to LIWC Dictionary";
        public string PluginType { get; } = "Dictionary Tools";
        public string PluginVersion { get; } = "1.0.2";
        public string PluginAuthor { get; } = "Ryan L. Boyd (ryan@ryanboyd.io)";
        public string PluginDescription { get; } = "Takes a CSV file and converts it into a LIWC-formatted dictionary file.";
        public string PluginTutorial { get; } = "Coming Soon";
        public bool TopLevel { get; } = true;


        public Icon GetPluginIcon
        {
            get
            {
                return Properties.Resources.icon;
            }
        }

        #endregion


        private string SelectedEncoding { get; set; } = "utf-8";
        private string IncomingTextLocation { get; set; } = "";
        private string OutputLocation { get; set; } = "";
        private string Delimiter { get; set; } = ",";
        private string Quote { get; set; } = "\"";
        private string[] header { get; set; }
        private bool ConvertToLower { get; set; } = true;
        private string CSVStyle { get; set; } = "Table";

        public void ChangeSettings()
        {

            using (var form = new SettingsForm_CSVtoLIWCdic(IncomingTextLocation, OutputLocation, SelectedEncoding, Delimiter, Quote, ConvertToLower, CSVStyle))
            {


                form.Icon = Properties.Resources.icon;
                form.Text = PluginName;


                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    SelectedEncoding = form.SelectedEncoding;
                    IncomingTextLocation = form.InputFileName;
                    OutputLocation = form.OutputFileName;
                    Delimiter = form.Delimiter;
                    Quote = form.Quote;
                    ConvertToLower = form.ConvertLower;
                    CSVStyle = form.CSVStyle;

                }
            }

        }





        public Payload RunPlugin(Payload Input, int ThreadsAvailable)
        {


            uint WordsProcessed = 0;
            List<string> WordList = new List<string>();
            Dictionary<int, string> CategoryNumNameMap = new Dictionary<int, string>();
            Dictionary<string, List<int>> WordCategoryMap = new Dictionary<string, List<int>>();


            TimeSpan reportPeriod = TimeSpan.FromMinutes(0.01);
            using (new System.Threading.Timer(
                        _ => SetUpdate(WordsProcessed),
                             null, reportPeriod, reportPeriod))
            {


                if (CSVStyle == "Poster") { 
                    #region Poster Style CSV Reading
                    //read in all of the basic dictionary data from the CSV file
                    using (var stream = File.OpenRead(IncomingTextLocation))
                    using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
                    {
                        var data = CsvParser.ParseHeadAndTail(reader, Delimiter[0], Quote[0]);

                        //populate the header names and categories. might not end up being necessary
                        for (int i = 0; i < header.Length; i++)
                        {
                            if (!String.IsNullOrWhiteSpace(header[i].Trim()))
                            {
                                CategoryNumNameMap.Add(i + 1, header[i].Trim());
                            }
                        
                        }

                        var lines = data.Item2;

                        foreach (var line in lines)
                        {
                            try
                            {
                                for (int i = 0; i < line.Count(); i++)
                                {

                                    //we only want to add the word if we've actually got a corresponding
                                    //header to go with the column that the word is in.
                                    if (CategoryNumNameMap.ContainsKey(i+1)) { 

                                        string word = line[i].Trim();

                                        if (ConvertToLower) word = word.ToLower();

                                        if (string.IsNullOrWhiteSpace(word)) continue;
                                    

                                        if (WordCategoryMap.ContainsKey(word))
                                        {
                                            if (WordCategoryMap[word].Contains(i + 1)) continue;
                                            WordCategoryMap[word].Add(i + 1);
                                        }
                                        else
                                        {
                                            WordCategoryMap.Add(word, new List<int>() { i + 1 });
                                            WordList.Add(word);
                                        }
                                        WordsProcessed++;

                                    }
                                }
                                
                            }
                            catch
                            {

                            }


                        }

                    }
                    #endregion
                }
                else
                {
                    #region Table Style CSV Reading
                    //read in all of the basic dictionary data from the CSV file
                    using (var stream = File.OpenRead(IncomingTextLocation))
                    using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
                    {
                        var data = CsvParser.ParseHeadAndTail(reader, Delimiter[0], Quote[0]);

                        //populate the header names and categories. might not end up being necessary
                        for (int i = 0; i < header.Length; i++)
                        {
                            if (i > 0 && !String.IsNullOrWhiteSpace(header[i].Trim()))
                            {
                                CategoryNumNameMap.Add(i, header[i].Trim());
                            }

                        }

                        var lines = data.Item2;

                        foreach (var line in lines)
                        {
                            try
                            {

                                string word = line[0].Trim();
                                if (ConvertToLower) word = word.ToLower();
                                if (string.IsNullOrWhiteSpace(word)) continue;

                                for (int i = 1; i < line.Count(); i++)
                                {

                                    //we only want to add the word if we've actually got a corresponding
                                    //header to go with the column that the word is in.
                                    if (CategoryNumNameMap.ContainsKey(i) && !String.IsNullOrWhiteSpace(line[i]))
                                    {
                                        
                                        if (WordCategoryMap.ContainsKey(word))
                                        {
                                            if (WordCategoryMap[word].Contains(i)) continue;
                                            WordCategoryMap[word].Add(i);
                                        }
                                        else
                                        {
                                            WordCategoryMap.Add(word, new List<int>() { i });
                                            WordList.Add(word);
                                        }
                                        WordsProcessed++;

                                    }
                                }

                            }
                            catch
                            {

                            }


                        }

                    }
                    #endregion
                }







                WordList.Sort();

                using (ThreadsafeOutputWriter OutputWriter = new ThreadsafeOutputWriter(OutputLocation,
                    Encoding.GetEncoding(SelectedEncoding.ToString()), FileMode.Create))
                {

                    OutputWriter.WriteString("%");

                    //write the header
                    for (int i = 0; i < CategoryNumNameMap.Count(); i++)
                    {
                        string rowToWrite = (i + 1).ToString() + "\t" + CategoryNumNameMap[i+1];
                        OutputWriter.WriteString(rowToWrite);
                    }

                    OutputWriter.WriteString("%");

                    //write the dictionary body
                    for (int i = 0; i < WordList.Count(); i++)
                    {
                        WordCategoryMap[WordList[i]].Sort();
                        string[] categoryArray = WordCategoryMap[WordList[i]].Select(x => x.ToString()).ToArray();
                        string rowToWrite = WordList[i] + "\t" + String.Join("\t", categoryArray);
                        OutputWriter.WriteString(rowToWrite);
                    }

                }

            }


            

            return (new Payload());



        }


        //not used
        public Payload RunPlugin(Payload Input)
        {
            return new Payload();
        }


        public void Initialize()
        {
            try
            {
                using (var stream = File.OpenRead(IncomingTextLocation))
                using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
                {
                    var data = CsvParser.ParseHeadAndTail(reader, Delimiter[0], Quote[0]);

                    header = data.Item1.ToArray();

                }
            }
            catch
            {
                MessageBox.Show("There was a problem opening your dictionary CSV file. Is it currently open in another application?", "Dictionary CSV Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        public bool InspectSettings()
        {
            if (string.IsNullOrEmpty(IncomingTextLocation) || string.IsNullOrEmpty(OutputLocation))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Payload FinishUp(Payload Input)
        {
            return (Input);
        }


        #region Import/Export Settings
        public void ImportSettings(Dictionary<string, string> SettingsDict)
        {
            SelectedEncoding = SettingsDict["SelectedEncoding"];
            IncomingTextLocation = SettingsDict["IncomingTextLocation"];
            OutputLocation = SettingsDict["OutputLocation"];
            Delimiter = SettingsDict["Delimiter"];
            Quote = SettingsDict["Quote"];
            ConvertToLower = Boolean.Parse(SettingsDict["ConvertToLower"]);
            CSVStyle = SettingsDict["CSVStyle"];

        }


        public Dictionary<string, string> ExportSettings(bool suppressWarnings)
        {
            Dictionary<string, string> SettingsDict = new Dictionary<string, string>();
            SettingsDict.Add("SelectedEncoding", SelectedEncoding);
            SettingsDict.Add("IncomingTextLocation", IncomingTextLocation);
            SettingsDict.Add("OutputLocation", OutputLocation);
            SettingsDict.Add("Delimiter", Delimiter);
            SettingsDict.Add("Quote", Quote);
            SettingsDict.Add("ConvertToLower", ConvertToLower.ToString());
            SettingsDict.Add("CSVStyle", CSVStyle);
            return (SettingsDict);
        }
        #endregion




        private void SetUpdate(uint WordsProcessed)
        {
            StatusToReport = "Processed: " + WordsProcessed.ToString() + " words across " + header.Length.ToString() + " categories";
        }


    }
}
