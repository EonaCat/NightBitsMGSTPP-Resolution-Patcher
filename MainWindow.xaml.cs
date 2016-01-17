using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using static NightBitsMGSTPP_Resolution_Patcher.ResolutionDetector;

namespace NightBitsMGSTPP_Resolution_Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private byte[] _replace;
        private Dictionary<string, byte[]> _resolutionsDictionary = new Dictionary<string, byte[]>();

        public MainWindow()
        {
            InitializeComponent();
            GetAllValidResolutions();
            FillInKnownResolutions();
            FillInComboBox();
        }

        private byte[] GetBytesArrayFromResolution(int resolutionX, int resolutionY)
        {
            var resultX = resolutionX.ToString("X4");
            var resultY = resolutionY.ToString("X4");
            byte[] resultXInBytes = StringToByteArray(resultX);
            byte[] resultYInBytes = StringToByteArray(resultY);
            return resultXInBytes.Concat(resultYInBytes).ToArray();
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
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
            _resolutionsDictionary["1024X768 (4:3)"] = GetBytesArrayFromResolution(1024, 768);
            _resolutionsDictionary["1280X1024 (5:4)"] = GetBytesArrayFromResolution(1280, 1024);
            _resolutionsDictionary["1366X768 (16:9)"] = GetBytesArrayFromResolution(1366, 768);
            _resolutionsDictionary["1440X900 (16:10)"] = GetBytesArrayFromResolution(1440, 900);
            _resolutionsDictionary["1680X1050 (16:10)"] = GetBytesArrayFromResolution(1680, 1050);
            _resolutionsDictionary["1600X1200 (16:10)"] = GetBytesArrayFromResolution(1600, 1200);
            _resolutionsDictionary["1920X1080 (16:9)"] = GetBytesArrayFromResolution(1920, 1080);
            _resolutionsDictionary["2560X1080 (21:9)"] = GetBytesArrayFromResolution(2560, 1080);
            _resolutionsDictionary["3440X1440 (21:9)"] = GetBytesArrayFromResolution(3440, 1440);
            _resolutionsDictionary["5040x1050 (48:10)"] = GetBytesArrayFromResolution(5040, 1050);
            _resolutionsDictionary["4800X900 (48:9)"] = GetBytesArrayFromResolution(4800, 900);
            _resolutionsDictionary["7860X1080 (48:9)"] = GetBytesArrayFromResolution(7860, 1080);
            _resolutionsDictionary["7860X2160 (48:9)"] = GetBytesArrayFromResolution(7860, 2160);
        }

        /* // Is not used anymore
        private void OldKnownResolutions()
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
            _resolutionsDictionary["7860X1080 (48:9)"] = new byte[] { 0xBA, 0xAA, 0xAA, 0x40 };
        }
        */

        // Doesnt work yet
        private void GetAllValidResolutions()
        {
            DEVMODE vDevMode = new DEVMODE();
            int i = 0;
            while (EnumDisplaySettings(null, i, ref vDevMode))
            {
                Console.WriteLine("Width:{0} Height:{1} Color:{2} Frequency:{3}",
                                        vDevMode.dmPelsWidth,
                                        vDevMode.dmPelsHeight,
                                        1 << vDevMode.dmBitsPerPel, vDevMode.dmDisplayFrequency
                                    );
                i++;
            }

            var scope = new ManagementScope();

            var query = new ObjectQuery("SELECT * FROM CIM_VideoControllerResolution");

            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                var results = searcher.Get();

                //previous resolution for checking when new resolution is found
                string previousResolution = string.Empty;
                //current resolution
                string currentResolution = string.Empty;

                foreach (var result in results)
                {
                    currentResolution = result["HorizontalResolution"] + "x" + result["VerticalResolution"];

                    if (currentResolution != previousResolution)
                    {
                        comboBox.Items.Add(currentResolution);
                        previousResolution = currentResolution;
                    }
                }
            }
        }

        private void PatchButton_Click(object sender, RoutedEventArgs e)
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
                Exception error;

                if (PatchTheFile(filename, out error))
                {
                    MessageBox.Show("The file was patched successfully");
                }
                else
                {
                    if (error != null)
                    {
                        MessageBox.Show($"The file failed to patch! => '{error.Message}'");
                    }
                    else
                    {
                        MessageBox.Show($"The file failed to patch! => 'Could not find original hexValues, are you sure you are trying to patch an UNPATCHED executable?'");
                    }
                }
                MessageBox.Show("Patcher created by NightBits");
            }
        }

        private void CreateBackup(string filename)
        {
            string dirPath = Path.GetDirectoryName(filename);
            string fileName = Path.GetFileName(filename);
            string backupExtension = ".backup";
            string[] files = Directory.GetFiles(dirPath);
            int count = files.Count(file => { return file.Contains(fileName + backupExtension); });
            string newFileName = (count == 0) ? filename + backupExtension : $"{fileName} ({count + 1}){backupExtension}";
            File.Copy(filename, newFileName + ".backup");
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

        private bool PatchTheFile(string filename, out Exception error)
        {
            bool foundValueForPatching = false;
            error = null;

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

                    foundValueForPatching = true;
                    for (int w = 0; w < PatchFind.Length; w++)
                    {
                        fileContent[p + w] = PatchReplace[w];
                    }
                }

                if (foundValueForPatching)
                {
                    // Save it to another location.
                    File.WriteAllBytes(filename, fileContent);
                }

                return foundValueForPatching;
            }
            catch (Exception exception)
            {
                error = exception;
                return false;
            }
        }
    }
}
