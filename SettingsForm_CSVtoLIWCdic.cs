﻿using System.IO;
using System.Text;
using System.Windows.Forms;
using System;
using System.Drawing;

namespace CSVtoLIWCdic
{
    internal partial class SettingsForm_CSVtoLIWCdic : Form
    {


        #region Get and Set Options

        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
        public string SelectedEncoding { get; set; }
        public string Delimiter { get; set; }
        public string Quote { get; set; }
        public bool ConvertLower { get; set; }
        public string CSVStyle { get; set; }
        #endregion

        private Color[,] bgColors { get; set; } = new Color[2, 2] {
            { SystemColors.Control, SystemColors.Control },
            { SystemColors.Control, SystemColors.Control }
        };



        public SettingsForm_CSVtoLIWCdic(string InputFile, string OutputFile, string SelectedEncodingIncoming, string DelimiterIncoming, string QuoteIncoming, bool LowerCaseIncoming, string CSVStyleIncoming)
        {
            InitializeComponent();

            foreach (var encoding in Encoding.GetEncodings())
            {
                EncodingDropdown.Items.Add(encoding.Name);
            }

            try
            {
                EncodingDropdown.SelectedIndex = EncodingDropdown.FindStringExact(SelectedEncodingIncoming);
            }
            catch
            {
                EncodingDropdown.SelectedIndex = EncodingDropdown.FindStringExact(Encoding.Default.BodyName);
            }

            
            OutputFileTextbox.Text = OutputFile;
           
            SelectedFileTextbox.Text = InputFile;

            CSVDelimiterTextbox.Text = DelimiterIncoming;
            CSVQuoteTextbox.Text = QuoteIncoming;

            LowercaseCheckbox.Checked = LowerCaseIncoming;

            CSVStyle = CSVStyleIncoming;

            if (CSVStyleIncoming == "Poster")
            {
                PosterStyleRadioButton.Select();
            }
            else
            {
                TableStyleRadioButton.Select();
            }


        }






        private void ChooseInputFileButton_Click(object sender, System.EventArgs e)
        {

            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;
                dialog.Title = "Please choose the CSV file that you would like to read";
                dialog.FileName = "CSV Spreadsheet.csv";
                dialog.Filter = "Comma-Separated Values (CSV) File (*.csv)|*.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SelectedFileTextbox.Text = dialog.FileName;
                }
            }
        }





        private void OKButton_Click(object sender, System.EventArgs e)
        {
            this.SelectedEncoding = EncodingDropdown.SelectedItem.ToString();
            this.InputFileName = SelectedFileTextbox.Text;
            this.Delimiter = CSVDelimiterTextbox.Text;
            this.Quote = CSVQuoteTextbox.Text;
            this.OutputFileName = OutputFileTextbox.Text;
            this.ConvertLower = LowercaseCheckbox.Checked;
            this.DialogResult = DialogResult.OK;
        }


        private void ChooseOutputFileButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Please choose the output location for your LIWC dictionary file";
                dialog.FileName = "Dictionary.dic";
                dialog.Filter = "LIWC Dictionary File (*.dic)|*.dic";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (File.Exists(dialog.FileName.ToString()))
                        {
                            if (DialogResult.Yes == MessageBox.Show("BUTTER is about to overwrite your selected file. Are you ABSOLUTELY sure that you want to do this? All data currently contained in the selected file will immediately be deleted if you select \"Yes\".", "Overwrite File?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
                            {
                                using (var myFile = File.Create(dialog.FileName.ToString())) { }
                                OutputFileTextbox.Text = dialog.FileName.ToString();
                            }
                            else
                            {
                                OutputFileTextbox.Text = "";
                            }
                        }
                        else
                        {
                            using (var myFile = File.Create(dialog.FileName.ToString())) { }
                            OutputFileTextbox.Text = dialog.FileName.ToString();
                        }



                    }
                    catch
                    {
                        MessageBox.Show("BUTTER does not appear to be able to create this output file. Do you have write permissions for this file? Is the file already open in another program?", "Cannot Create File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        OutputFileTextbox.Text = "";
                    }
                }
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb != null)
            {
                if (rb == PosterStyleRadioButton)
                {
                    bgColors[0, 0] = System.Drawing.Color.PaleTurquoise;
                    bgColors[1, 0] = System.Drawing.Color.PaleTurquoise;
                    bgColors[0, 1] = System.Drawing.SystemColors.Control;
                    bgColors[1, 1] = System.Drawing.SystemColors.Control;
                    CSVStyle = "Poster";
                }
                else
                {
                    bgColors[0, 0] = System.Drawing.SystemColors.Control;
                    bgColors[1, 0] = System.Drawing.SystemColors.Control;
                    bgColors[0, 1] = System.Drawing.Color.PaleTurquoise;
                    bgColors[1, 1] = System.Drawing.Color.PaleTurquoise;
                    CSVStyle = "Table";
                }
                CSVStyleTableLayoutPanel.Refresh();
            }
        }

        private void TableLayoutPanel1_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            using (var b = new SolidBrush(bgColors[e.Column, e.Row]))
            {
                e.Graphics.FillRectangle(b, e.CellBounds);
            }
        }
    }
}
