using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TLOULocalizationTool
{
    public partial class Form1 : Form
    {
        public static string[] SupportedExtensions = { ".subtitles", ".common", ".subtitles-systemic", ".web" };
        public static byte[] PlaceholderText = Encoding.UTF8.GetBytes("UNKNOWN STRING!!!\0");

        public Form1()
        {
            InitializeComponent();
            this.button1.Click += Button1_Click;
            this.button2.Click += Button2_Click;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileSelector = new OpenFileDialog())
            {
                fileSelector.Filter = "Localization files|*.subtitles;*.common;*.subtitles-systemic;*.web|All files|*.*";
                fileSelector.Title = "Select a localization file";

                if (fileSelector.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = fileSelector.FileName;
                    if (SupportedExtensions.Contains(Path.GetExtension(selectedPath)))
                    {
                        try
                        {
                            int version = DetermineVersion(selectedPath);
                            switch (version)
                            {
                                case 0: Export0(selectedPath); break;
                                case 1: Export1(selectedPath); break;
                                case 2: Export2(selectedPath); break;
                            }
                            MessageBox.Show("Export completed successfully!", "Success");
                        }
                        catch (Exception error)
                        {
                            MessageBox.Show($"Error during export: {error.Message}", "Error");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid file format!", "Error");
                    }
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileSelector = new OpenFileDialog())
            {
                fileSelector.Filter = "Text files|*.txt|All files|*.*";
                fileSelector.Title = "Select exported text file";

                if (fileSelector.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = fileSelector.FileName;
                    if (Path.GetExtension(selectedPath) == ".txt")
                    {
                        try
                        {
                            List<string> identifiers = new List<string>(File.ReadAllLines(Path.ChangeExtension(selectedPath, ".ids")));
                            List<string> texts = new List<string>(File.ReadAllLines(selectedPath));
                            int version = int.Parse(identifiers[0].Split('|')[0]);
                            switch (version)
                            {
                                case 0: Import0(selectedPath, identifiers, texts); break;
                                case 1: Import1(selectedPath, identifiers, texts); break;
                                case 2: Import2(selectedPath, identifiers, texts); break;
                            }
                            MessageBox.Show("Import completed successfully!", "Success");
                        }
                        catch (Exception error)
                        {
                            MessageBox.Show($"Error during import: {error.Message}", "Error");
                        }
                    }
                }
            }
        }

        private static int DetermineVersion(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(File.ReadAllBytes(filePath))))
            {
                byte endianIndicator = binaryReader.ReadByte();
                if (endianIndicator == 0)
                    return 0;
                binaryReader.BaseStream.Position = 16;
                return binaryReader.ReadInt32() == 0 ? 2 : 1;
            }
        }

        private static void Export2(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(File.ReadAllBytes(filePath))))
            {
                string exportDir = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace(".", "_") + "_exported");
                Directory.CreateDirectory(exportDir);

                List<long> textOffsets = new List<long>();
                List<string> textIds = new List<string> { "2|" + Path.GetExtension(filePath).Replace(".", "") };
                List<string> textEntries = new List<string>();

                int textCount = binaryReader.ReadInt32();
                for (int i = 0; i < textCount; i++)
                {
                    textIds.Add(binaryReader.ReadUInt64().ToString());
                    textOffsets.Add(binaryReader.ReadInt64());
                }
                long textTableStart = binaryReader.BaseStream.Position;
                for (int i = 0; i < textCount; i++)
                {
                    binaryReader.BaseStream.Position = textTableStart + textOffsets[i];
                    textEntries.Add(binaryReader.ReadNullTerminatedString());
                }

                string fileName = Path.GetFileName(filePath);
                File.WriteAllLines(Path.Combine(exportDir, fileName + ".txt"), textEntries);
                File.WriteAllLines(Path.Combine(exportDir, fileName + ".ids"), textIds);
            }
        }

        private static void Export1(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(File.ReadAllBytes(filePath))))
            {
                string exportDir = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace(".", "_") + "_exported");
                Directory.CreateDirectory(exportDir);

                List<int> textOffsets = new List<int>();
                List<string> textIds = new List<string> { "1|" + Path.GetExtension(filePath).Replace(".", "") };
                List<string> textEntries = new List<string>();

                int textCount = binaryReader.ReadInt32();
                for (int i = 0; i < textCount; i++)
                {
                    textIds.Add(binaryReader.ReadUInt32().ToString());
                    textOffsets.Add(binaryReader.ReadInt32());
                }
                long textTableStart = binaryReader.BaseStream.Position;
                for (int i = 0; i < textCount; i++)
                {
                    binaryReader.BaseStream.Position = textTableStart + textOffsets[i];
                    textEntries.Add(binaryReader.ReadNullTerminatedString());
                }

                string fileName = Path.GetFileName(filePath);
                File.WriteAllLines(Path.Combine(exportDir, fileName + ".txt"), textEntries);
                File.WriteAllLines(Path.Combine(exportDir, fileName + ".ids"), textIds);
            }
        }

        private static void Export0(string filePath)
        {
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(File.ReadAllBytes(filePath))))
            {
                string exportDir = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace(".", "_") + "_exported");
                Directory.CreateDirectory(exportDir);

                List<int> textOffsets = new List<int>();
                List<string> textIds = new List<string> { "0|" + Path.GetExtension(filePath).Replace(".", "") };
                List<string> textEntries = new List<string>();

                int textCount = binaryReader.ReadBEInt32();
                for (int i = 0; i < textCount; i++)
                {
                    textIds.Add(binaryReader.ReadBEUInt32().ToString());
                    textOffsets.Add(binaryReader.ReadBEInt32());
                }
                long textTableStart = binaryReader.BaseStream.Position;
                for (int i = 0; i < textCount; i++)
                {
                    binaryReader.BaseStream.Position = textTableStart + textOffsets[i];
                    textEntries.Add(binaryReader.ReadNullTerminatedString());
                }

                string fileName = Path.GetFileName(filePath);
                File.WriteAllLines(Path.Combine(exportDir, fileName + ".txt"), textEntries);
                File.WriteAllLines(Path.Combine(exportDir, fileName + ".ids"), textIds);
            }
        }

        private static void Import2(string textPath, List<string> identifiers, List<string> texts)
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
            {
                List<byte[]> encodedTexts = new List<byte[]>();
                identifiers.RemoveAt(0);
                int textCount = texts.Count;

                binaryWriter.Write(textCount);
                long currentOffset = PlaceholderText.Length;
                for (int i = 0; i < textCount; i++)
                {
                    encodedTexts.Add(Utilities.WriteNullTerminatedString(texts[i]));
                    binaryWriter.Write(Convert.ToUInt64(identifiers[i]));
                    binaryWriter.Write(currentOffset);
                    currentOffset += encodedTexts[i].Length;
                }
                binaryWriter.Write(PlaceholderText);
                foreach (var text in encodedTexts)
                    binaryWriter.Write(text);

                SaveImportedFile(textPath, binaryWriter);
            }
        }

        private static void Import1(string textPath, List<string> identifiers, List<string> texts)
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
            {
                List<byte[]> encodedTexts = new List<byte[]>();
                identifiers.RemoveAt(0);
                int textCount = texts.Count;

                binaryWriter.Write(textCount);
                long currentOffset = PlaceholderText.Length;
                for (int i = 0; i < textCount; i++)
                {
                    encodedTexts.Add(Utilities.WriteNullTerminatedString(texts[i]));
                    binaryWriter.Write(Convert.ToUInt32(identifiers[i]));
                    binaryWriter.Write((int)currentOffset);
                    currentOffset += encodedTexts[i].Length;
                }
                binaryWriter.Write(PlaceholderText);
                foreach (var text in encodedTexts)
                    binaryWriter.Write(text);

                SaveImportedFile(textPath, binaryWriter);
            }
        }

        private static void Import0(string textPath, List<string> identifiers, List<string> texts)
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
            {
                List<byte[]> encodedTexts = new List<byte[]>();
                identifiers.RemoveAt(0);
                int textCount = texts.Count;

                binaryWriter.WriteBE(textCount);
                long currentOffset = 0;
                for (int i = 0; i < textCount; i++)
                {
                    encodedTexts.Add(Utilities.WriteNullTerminatedString(texts[i]));
                    binaryWriter.WriteBE(Convert.ToUInt32(identifiers[i]));
                    binaryWriter.WriteBE((int)currentOffset);
                    currentOffset += encodedTexts[i].Length;
                }
                binaryWriter.Write(PlaceholderText);
                foreach (var text in encodedTexts)
                    binaryWriter.Write(text);

                SaveImportedFile(textPath, binaryWriter);
            }
        }

        private static void SaveImportedFile(string textPath, BinaryWriter binaryWriter)
        {
            string originalFileName = Path.GetFileNameWithoutExtension(textPath);
            string exportDir = Path.GetDirectoryName(textPath);
            string originalDir = Path.GetDirectoryName(exportDir);
            string outputPath = Path.Combine(originalDir, originalFileName + ".ready");
            File.WriteAllBytes(outputPath, binaryWriter.ToByteArray());
        }

        private void toolStripLabel1_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("This tool was built by AliZ — big thanks to NoobInCoding for the original script.", "Credits", MessageBoxButtons.OK);

        }
    }
}