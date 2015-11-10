using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace NightBitsMGSTPP_Resolution_Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private byte[] _replace;
        private bool _hasTest = false;
        private Dictionary<string, byte[]> _resolutionsDictionary = new Dictionary<string, byte[]>();

        public MainWindow()
        {
            InitializeComponent();
            FillInKnownResolutions();
            FillInComboBox();
            GetAllValidResolutions();
        }

        private void FillInComboBox()
        {
            foreach (var resolution in _resolutionsDictionary.Keys)
            {
                comboBox.Items.Add(resolution);
            }
            comboBox.SelectedIndex = 0;
        }

        private void FillInKnownResolutions()
        {
            _resolutionsDictionary["1024X768 (4:3)"] = new byte[] { 0xAB, 0xAA, 0xAA, 0x3F };
            _resolutionsDictionary["1280X1024 (5:4)"] = new byte[] { 0x00, 0x00, 0x0A, 0x3F };
            _resolutionsDictionary["1366X768 (16:9)"] = new byte[] { 0xAB, 0xAA, 0xE3, 0x3F };
            _resolutionsDictionary["1440X900 (16:10)"] = new byte[] { 0xCD, 0xCC, 0xCC, 0x3F };
            _resolutionsDictionary["1680X1050 (16:10)"] = new byte[] { 0xCD, 0xCC, 0xCC, 0x3F };
            _resolutionsDictionary["1920X1080 (16:9)"] = new byte[] { 0x39, 0x8E, 0xE3, 0x3F };
            _resolutionsDictionary["2560X1080 (21:9)"] = new byte[] { 0x26, 0xB4, 0x17, 0x40 };
           _resolutionsDictionary["3440X1440 (21:9)"] = new byte[] { 0x8E, 0xE3, 0x18, 0x40 };
           _resolutionsDictionary["5040x1050 (48:10)"] = new byte[] { 0x9A, 0x99, 0x99, 0x40 };
           _resolutionsDictionary["4800X900 (48:9)"] = new byte[] { 0xBA, 0xAA, 0xAA, 0x40 };
        }

        private void GetAllValidResolutions()
        {
            var scope = new ManagementScope();

            var query = new ObjectQuery("SELECT * FROM CIM_VideoControllerResolution");

            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                var results = searcher.Get();

                //previous resolution for checking when new resolution is found
                string prevReso = string.Empty;
                //current resolution
                string currReso = string.Empty;

                foreach (var result in results)
                {
                    currReso = result["HorizontalResolution"] + "x" + result["VerticalResolution"];

                    if (currReso != prevReso)
                    {
                        comboBox.Items.Add(currReso);
                        prevReso = currReso;
                    }
                }
            }
        }

        private static string ConvertToHex(byte[] bytes)
        {
            var builder = new StringBuilder();

            var hexCharacters = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

            for (var i = 0; i < bytes.Length; i++)
            {
                int firstValue = (bytes[i] >> 4) & 0x0F;
                int secondValue = bytes[i] & 0x0F;

                char firstCharacter = hexCharacters[firstValue];
                char secondCharacter = hexCharacters[secondValue];

                builder.Append("0x");
                builder.Append(firstCharacter);
                builder.Append(secondCharacter);
                builder.Append(' ');
            }

            return builder.ToString().Trim(' ');
        }

        private void PatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasTest)
            {
                uint test = 3440 + 1440;
                MessageBox.Show(ConvertToHex(Encoding.UTF8.GetBytes(test.ToString())));
            }
            else
            {
                if (comboBox.Text.Length > 0)
                {
                    PatchFile();
                }
                else
                {
                    MessageBox.Show("Please select a resolution!");
                }
            }
        }

        private void PatchFile()
        {
            var startupFolder = @"I:\Steam\SteamApps\common\MGS_TPP";

            var dialog = new OpenFileDialog
            {
                Title = "Select file to patch",
                DefaultExt = ".exe",
                Filter = "Exe Files (*.exe) | *.exe"
            };

            // Set to my own resolution
            if (Directory.Exists(startupFolder))
            {
                dialog.InitialDirectory = startupFolder;
                _replace = _resolutionsDictionary["2560X1080 (21:9)"]; 
            }
            else
            {
                _replace = _resolutionsDictionary[comboBox.Text];
            }

            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var filename = dialog.FileName;
                CreateBackup(filename);

                if (PatchTheFile(filename))
                {
                    MessageBox.Show("The file was patched successfully");
                }
                else
                {
                    MessageBox.Show("The file failed to patch!");
                }
                MessageBox.Show("Patcher created by NightBits");
            }
        }

        public string Converts(double d)
        {
            float f = (float)d;
            byte[] convertToBytes = new byte[32];
            string hex = "";
            convertToBytes = BitConverter.GetBytes(f);
            for (int i = (convertToBytes.Count(x => x != 0)) - 1; i >= 0; i--)
            {
                hex = hex + "," + convertToBytes[i].ToString("X");
            }

            return hex;
        }

        private void CreateBackup(string filename)
        {
            if (File.Exists(filename + ".backup"))
            {
                File.Delete(filename + ".backup");
            }
            File.Copy(filename, filename + ".backup");
        }

        private static readonly byte[] PatchFind = { 0x39, 0x8E, 0xE3, 0x3F };
        private static readonly byte[] PatchReplace = { 0x26, 0xb4, 0x17, 0x40 };

        private static bool DetectPatch(byte[] sequence, int position)
        {
            if (position + PatchFind.Length > sequence.Length) return false;
            for (int p = 0; p < PatchFind.Length; p++)
            {
                if (PatchFind[p] != sequence[position + p]) return false;
            }
            return true;
        }

        private bool PatchTheFile(string filename)
        {
            try
            {
                // Ensure target directory exists.
                var targetDirectory = Path.GetDirectoryName(filename);
                if (targetDirectory == null) return false;
                Directory.CreateDirectory(targetDirectory);

                // Read file bytes.
                byte[] fileContent = File.ReadAllBytes(filename);

                // Detect and patch file.
                for (int p = 0; p < fileContent.Length; p++)
                {
                    if (!DetectPatch(fileContent, p)) continue;

                    for (int w = 0; w < PatchFind.Length; w++)
                    {
                        fileContent[p + w] = PatchReplace[w];
                    }
                }

                // Save it to another location.
                File.WriteAllBytes(filename, fileContent);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
