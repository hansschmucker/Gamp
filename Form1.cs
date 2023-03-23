using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using ZstdSharp;

namespace GameGearMicroCFWPackager {
    public partial class MainUI : Form {
        public MainUI() {
            InitializeComponent();
            RomBanks.NodeMouseClick += (sender, args) => RomBanks.SelectedNode = args.Node;
        }

        private void RomBanks_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else if (e.Data.GetDataPresent(typeof(TreeNode)))
                e.Effect = DragDropEffects.Move;
        }

        /***
         * Allows switching two nodes by switching the files
         */
        private void RomBanks_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(typeof(TreeNode))) {
                var originNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                var targetNode = RomBanks.SelectedNode;

                if(originNode!= targetNode && targetNode.Parent != null) {
                    var origTag = (ROM)originNode.Tag;
                    var targetTag = (ROM)targetNode.Tag;
                    byte[] origBytes = null;
                    byte[] targetBytes = null;
                    string origName = "";
                    string targetName = "";
                    if (origTag.Exists) {
                        origBytes= origTag.Bytes;
                        origName = origTag.Name;
                    }
                    if (targetTag.Exists) {
                        targetBytes = targetTag.Bytes;
                        targetName = targetTag.Name;
                    }

                    if (origBytes != null) {
                        File.WriteAllBytes(ROM.BuildPath(origName,targetTag.BankNum,targetTag.RomNum), origBytes);
                        File.Delete(origTag.RomPath);
                    }
                    if (targetBytes != null) {
                        File.WriteAllBytes(ROM.BuildPath(targetName, origTag.BankNum, origTag.RomNum), targetBytes);
                        File.Delete(targetTag.RomPath);
                    }

                }
                ReLoad();
                return;
            }


            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            for (var  i = 0; i < files.Length; i++) {
                var type = Path.GetExtension(files[i]).ToLower();
                if (type == ".gg") {
                    if (RomBanks.SelectedNode == null || RomBanks.SelectedNode.Parent == null)
                        continue;
                    var rom = ((ROM)RomBanks.SelectedNode.Tag);
                    var destFile = ROM.BuildPath(Path.GetFileNameWithoutExtension(files[i]).Trim(),rom.BankNum,rom.RomNum);
                    if (rom.Exists && new FileInfo(destFile).FullName.ToLower() == new FileInfo(rom.RomPath).FullName.ToLower())
                        continue;
                    if (rom.Exists)
                        File.Delete(rom.RomPath);

                    var destDir = Path.GetDirectoryName(destFile);
                    if(!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                    File.Copy(files[i], destFile);
                    if(RomBanks.SelectedNode.NextNode!=null)
                        RomBanks.SelectedNode = RomBanks.SelectedNode.NextNode;
                } else if (type == ".png") {
                    if (!Directory.Exists("Set\\ArtWork\\"))
                        Directory.CreateDirectory("Set\\ArtWork\\");
                    File.Copy(files[i],"Set\\ArtWork\\" + Path.GetFileNameWithoutExtension(files[i]).Trim() + ".png",true);
                }
            }
            ReLoad();
        }

        private void RomBanks_DragOver(object sender, DragEventArgs e) {
            TreeNode targetNode = RomBanks.GetNodeAt(RomBanks.PointToClient(new Point(e.X, e.Y)));
            if(targetNode != null && targetNode!=RomBanks.SelectedNode) 
                RomBanks.SelectedNode=targetNode;
        }

        private static Bitmap ExtendImage(string source,string dest,int maxSize, Action<Graphics> drawer) {
            var bitmap = new Bitmap(Bitmap.FromFile(source));
            var canvas = Graphics.FromImage(bitmap);
            canvas.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            drawer(canvas);
            var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            if (stream.Length > maxSize) {
                stream.Dispose();
                stream = new MemoryStream();
                var nbmp = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format16bppRgb555);
                bitmap.Dispose();
                (bitmap = nbmp).Save(stream, ImageFormat.Png);
            }
            if (stream.Length > maxSize) {
                stream.Dispose();
                stream = new MemoryStream();
                var nbmp = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format8bppIndexed);
                bitmap.Dispose();
                (bitmap = nbmp).Save(stream, ImageFormat.Png);
            }
            if (stream.Length > maxSize) {
                stream.Dispose();
                stream = new MemoryStream();
                var nbmp = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format1bppIndexed);
                bitmap.Dispose();
                (bitmap = nbmp).Save(stream, ImageFormat.Png);
            }
            var destDir = Path.GetDirectoryName(dest);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            using (FileStream file = new FileStream(dest, FileMode.Create, System.IO.FileAccess.Write)) {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(file);
                file.Close();
            }
            stream.Close();
            stream.Dispose();
            canvas.Dispose();
            var r=new Bitmap(bitmap);
            bitmap.Dispose();
            return r;
        }

        void BuildArtwork() {
            var banks = RomBanks.Nodes;
            for (var i = 0; i < banks.Count; i++) {
                if (!Directory.Exists("Set\\Banks\\Bank" + (string)banks[i].Tag))
                    Directory.CreateDirectory("Set\\Banks\\Bank" + (string)banks[i].Tag);

                var bank = banks[i].Nodes;
                if(Png01.Image!=null)
                    Png01.Image.Dispose();
                if (Png02.Image != null)
                    Png02.Image.Dispose();
                if (Png03.Image != null)
                    Png03.Image.Dispose();
                if (Png04.Image != null)
                    Png04.Image.Dispose();

                Png01.Image = ExtendImage("Set\\Common\\01.png", "Set\\Banks\\Bank" + (string)banks[i].Tag + "\\01.png", 45535, canvas => {
                    StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    for (var j = 0; j < bank.Count; j++) {
                        var rom = (ROM)bank[j].Tag;
                        canvas.DrawString(rom.Name, TitleViewFontDialog.Font, new SolidBrush(ColorTranslator.FromHtml("#000000")), new Rectangle(2, 156 + 28 * j, 236, 20), stringFormat);
                        canvas.DrawString(rom.Name, TitleViewFontDialog.Font, new SolidBrush(TitleViewFontColor.Color), new Rectangle(1, 155 + 28 * j, 236, 20), stringFormat);
                    }
                    canvas.DrawString("Settings", TitleViewFontDialog.Font, new SolidBrush(ColorTranslator.FromHtml("#000000")), new Rectangle(2, 156 + 28 * 6, 236, 20), stringFormat);
                    canvas.DrawString("Settings", TitleViewFontDialog.Font, new SolidBrush(TitleViewFontColor.Color), new Rectangle(1, 155 + 28 * 6, 236, 20), stringFormat);
                });

                Png02.Image = ExtendImage("Set\\Common\\02.png", "Set\\Banks\\Bank" + (string)banks[i].Tag + "\\02.png", 6220, canvas => {
                    for (var j = 0; j < bank.Count; j++) {
                        var rom = (ROM)bank[j].Tag;
                        if (!String.IsNullOrEmpty(rom.ImagePath) && File.Exists(rom.ImagePath)) {
                            var img = Bitmap.FromFile(rom.ImagePath);
                            canvas.DrawImage(img, 1 + 22 * j, 1, 12, 60);
                        }
                    }
                });

                Png03.Image = ExtendImage("Set\\Common\\03.png", "Set\\Banks\\Bank" + (string)banks[i].Tag + "\\03.png", 149556, canvas => {
                    StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    for (var j = 0; j < bank.Count; j++) {
                        for (var k = 0; k < banks.Count; k++) {
                            var rom = (ROM)banks[k].Nodes[j].Tag;
                            canvas.DrawString(rom.Name, GridViewFontDialog.Font, new SolidBrush(ColorTranslator.FromHtml("#000000")), new Rectangle(29 + j * 240, 15 + 16 * k, 209, 16), stringFormat);
                            canvas.DrawString(rom.Name, GridViewFontDialog.Font, new SolidBrush(k==i? GridViewFontColor.Color : GridViewFontColorBG.Color), new Rectangle(29 + j * 240, 15 + 16 * k, 209, 16), stringFormat);
                        }
                    }
                });

                Png04.Image = ExtendImage("Set\\Common\\04.png", "Set\\Banks\\Bank" + (string)banks[i].Tag + "\\04.png", 33966, canvas => {
                    StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    for (var j = 0; j < bank.Count; j++) {
                        var rom = (ROM)bank[j].Tag;
                        if (!String.IsNullOrEmpty(rom.ImagePath) && File.Exists(rom.ImagePath)) {
                            var img = Bitmap.FromFile(rom.ImagePath);
                            canvas.DrawImage(img, 2 + 59 * j, 2, 51, 58);
                        }
                    }
                });
            }
        }

        void BuildSets() {
            var banks = RomBanks.Nodes;
            for (var i = 0; i < banks.Count; i++) {
                if (!Directory.Exists("Set\\Banks\\Bank" + (string)banks[i].Tag))
                    Directory.CreateDirectory("Set\\Banks\\Bank" + (string)banks[i].Tag);
                var bank = banks[i].Nodes;
                var bankBytes = new byte[524288 * 6];
                //build ROMS
                for (var j = 0; j < bank.Count; j++) {
                    var rom = (ROM)bank[j].Tag;
                    if (!rom.Exists)
                        continue;
                    var romBytes = rom.Bytes;
                    var compressor = new ZstdSharp.Compressor(9);
                    var compressed = compressor.Wrap(romBytes).ToArray();
                    if (compressed.Length > 524288)
                        throw new Exception("Sorry, rom " + rom.Name + " Exceeded the 512kb compressed limit.");
                    //Pad to 524288
                    Array.Copy(compressed, 0, bankBytes, j * 524288, compressed.Length);
                };
                File.WriteAllBytes("Set\\Banks\\ROM0" + (string)banks[i].Tag, bankBytes);

                var buffer = new byte[235277];
                {
                    var img = File.ReadAllBytes("Set\\Banks\\Bank" + (string)banks[i].Tag + "\\01.png");
                    if (img.Length > 45535)
                        throw new Exception("Sorry, image " + (string)banks[i].Tag + "\\01.png Exceeded the 45535b limit.");
                    Array.Copy(img, 0, buffer, 0, img.Length);
                }
                {
                    var img = File.ReadAllBytes("Set\\Banks\\Bank" + (string)banks[i].Tag + "\\02.png");
                    if (img.Length > 45535)
                        throw new Exception("Sorry, image " + (string)banks[i].Tag + "\\02.png Exceeded the 6220b limit.");
                    Array.Copy(img, 0, buffer, 45535, img.Length);
                }
                {
                    var img = File.ReadAllBytes("Set\\Banks\\Bank" + (string)banks[i].Tag + "\\03.png");
                    if (img.Length > 45535)
                        throw new Exception("Sorry, image " + (string)banks[i].Tag + "\\03.png Exceeded the 149556b limit.");
                    Array.Copy(img, 0, buffer, 45535 + 6220, img.Length);
                }
                {
                    var img = File.ReadAllBytes("Set\\Banks\\Bank" + (string)banks[i].Tag + "\\04.png");
                    if (img.Length > 33966)
                        throw new Exception("Sorry, image " + (string)banks[i].Tag + "\\03.png Exceeded the 33966b limit.");
                    Array.Copy(img, 0, buffer, 45535 + 6220 + 149556, img.Length);
                }
                File.WriteAllBytes("Set\\Banks\\IMG0" + (string)banks[i].Tag, buffer);

            }
        }


        private void BtnDoCompileBanks_Click(object sender, EventArgs e) {
            try {
                BuildArtwork();
                BuildSets();
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, "Error");
            }
        }

        private void BtnDoGenArt_Click(object sender, EventArgs e) {
            try {
                BuildArtwork();
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, "Error");
            }
        }

        private void BtnDoBuild_Click(object sender, EventArgs e) {
            try {
                BuildSets();
            }catch(Exception ex) {
                MessageBox.Show(this, ex.Message, "Error");
            }
        }

        private void ReLoad() {
            if (File.Exists("Set\\Font.json")) {
                var fonts = JSON.Deserialize<Dictionary<string, object>>(File.ReadAllText("Set\\Font.json", Encoding.UTF8));
                GridViewFontDialog.Font = (Font)FontSerializer.ConvertFromString((string)fonts["ListFont"]);
                TitleViewFontDialog.Font = (Font)FontSerializer.ConvertFromString((string)fonts["TitleFont"]);
                GridViewFontColorBG.Color = ColorTranslator.FromHtml((string)fonts["ListColor"]);
                GridViewFontColor.Color = ColorTranslator.FromHtml((string)fonts["ListActiveColor"]);
                TitleViewFontColor.Color = ColorTranslator.FromHtml((string)fonts["TitleColor"]);
            }

            ApplyFont();

            var banks = RomBanks.Nodes;
            for (var i = 0; i < banks.Count; i++) {
                var bank = banks[i].Nodes;
                for (var j = 0; j < bank.Count; j++) {
                    var rom = (ROM)bank[j].Tag;
                    if (!Directory.Exists("Set\\Banks\\Bank" + rom.BankNum))
                        Directory.CreateDirectory("Set\\Banks\\Bank" + rom.BankNum);
                    var romFiles = Directory.GetFiles("Set\\Banks\\Bank" + rom.BankNum + "\\", rom.RomNum + "*.gg");

                    if (romFiles.Length > 0) {
                        rom.RomPath = romFiles[0];
                        bank[j].Text = "ROM " + rom.RomNum + " : " + rom.Name;
                    } else {
                        bank[j].Text = "ROM " + rom.RomNum;
                    }
                    if (!Directory.Exists("Set\\ArtWork\\"))
                        Directory.CreateDirectory("Set\\ArtWork\\");
                    var imgFiles = Directory.GetFiles("Set\\ArtWork\\", rom.Name + ".png");
                    if (imgFiles.Length > 0) {
                        rom.ImagePath = imgFiles[0];
                    }

                    if (!rom.Exists)
                        bank[j].ForeColor = Color.DarkRed;
                    else if (!rom.ImageExists)
                        bank[j].ForeColor = Color.DarkOrange;
                    else
                        bank[j].ForeColor = SystemColors.ControlText;
                }
            }
        }

        private void MainUI_Load(object sender, EventArgs e) {
            var banks = RomBanks.Nodes;
            for (var i = 0; i < banks.Count; i++) {
                banks[i].Tag = (i + 1).ToString();
                var bank = banks[i].Nodes;
                for (var j = 0; j < bank.Count; j++) {
                    var rom = (ROM)bank[j].Tag;
                    rom.BankNum = i + 1;
                    rom.RomNum = j + 1;
                }
            }
            ReLoad();
        }

        private void RomBanks_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
            var num = "";
            if (e.Node.Parent == null)
                num = (string)e.Node.Tag;
            else {
                var rom = (ROM)e.Node.Tag;
                num = rom.BankNum.ToString();
            }
            {
                if (File.Exists("Set\\Banks\\Bank" + num + "\\01.png")) {
                    using(var b= System.Drawing.Image.FromFile("Set\\Banks\\Bank" + num + "\\01.png"))
                    Png01.Image = new Bitmap(b);
                } else
                    Png01.Image = null;
                if (File.Exists("Set\\Banks\\Bank" + num + "\\02.png")) {
                    using (var b = System.Drawing.Image.FromFile("Set\\Banks\\Bank" + num + "\\02.png"))
                        Png02.Image = new Bitmap(b);
                } else
                    Png02.Image = null;
                if (File.Exists("Set\\Banks\\Bank" + num + "\\03.png")) {
                    using (var b = System.Drawing.Image.FromFile("Set\\Banks\\Bank" + num + "\\03.png"))
                        Png03.Image = new Bitmap(b);
                } else
                    Png03.Image = null;
                if (File.Exists("Set\\Banks\\Bank" + num + "\\04.png")) {
                    using (var b = System.Drawing.Image.FromFile("Set\\Banks\\Bank" + num + "\\04.png"))
                        Png04.Image = new Bitmap(b);
                } else
                    Png04.Image = null;
            }
        }

        private void RomBanks_ItemDrag(object sender, ItemDragEventArgs e) {
            if (e.Button == MouseButtons.Left && ((TreeNode)e.Item).Parent!=null) {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void RomContextMenu_Opening(object sender, CancelEventArgs e) {
            if (RomBanks.SelectedNode.Parent == null)
                e.Cancel = true;
            else {
                var rom = (ROM)RomBanks.SelectedNode.Tag;
                if (!rom.Exists)
                    e.Cancel = true;
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
            var rom = (ROM)RomBanks.SelectedNode.Tag;
            if (!rom.Exists)
                return;
            var reName=Microsoft.VisualBasic.Interaction.InputBox("Enter a valid filename as new title.","Name your Game!",rom.Name);
            if(String.IsNullOrWhiteSpace(reName))
                return;

            try {
                rom.Rename(reName);
                if (rom.ImageExists) {
                    var yesNo = MessageBox.Show(this, "Do you want to rename the artwork as well?", "Art!!!", MessageBoxButtons.YesNo);
                    if (yesNo == DialogResult.Yes) {
                        rom.RenameImage(reName);
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show(this, "Could not move file:\r\n"+ex.Message, "Error", MessageBoxButtons.OK);
            }
            ReLoad();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            var rom = (ROM)RomBanks.SelectedNode.Tag;
            if (!rom.Exists)
                return;

            var yesNo=MessageBox.Show(this, "This will delete the GG File from your HDD.", "Are you certain?",MessageBoxButtons.YesNo);
            if(yesNo == DialogResult.Yes) {
                try {
                    File.Delete(rom.RomPath);
                    rom.RomPath = "";
                    if (rom.ImageExists) {
                        var yesNoArt = MessageBox.Show(this, "Do you want to delete the artwork as well?", "Art!!!", MessageBoxButtons.YesNo);
                        if (yesNoArt == DialogResult.Yes) {
                            File.Delete(rom.ImagePath);
                            rom.ImagePath = "";
                        }
                    }
                } catch(Exception ex) {
                    MessageBox.Show(this, "Could not delete file:\r\n" + ex.Message, "Error", MessageBoxButtons.OK);
                }

                ReLoad();
            }
        }

        private List<string[]> UploadPaths=new List<string[]>() {
            new string[]{ "ROM01","Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "IMG01", "Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "ROM02","Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "IMG02", "Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "ROM03","Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "IMG03", "Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "ROM04","Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "IMG04", "Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "ROM05","Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "IMG05", "Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "ROM06","Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "IMG06", "Set\\Banks\\","/mnt/UDISK" },
            new string[]{ "ROM07","Set\\Banks\\","/mnt/UDISK/system" },
            new string[]{ "IMG07", "Set\\Banks\\","/mnt/UDISK/system" },

        };

        private void BtnDoUpload_Click(object sender, EventArgs e) {
            MessageBox.Show(this, "Please use your favourite CFW to open the server.", "Connect", MessageBoxButtons.OK);
            try {
                using (var client = new SftpClient("169.254.215.100", 22, "root", "")) {
                    client.OperationTimeout = TimeSpan.FromSeconds(8);

                    client.Connect();
                    client.BufferSize = 4 * 1024;
                    foreach (var Path in UploadPaths) {
                        client.ChangeDirectory(Path[2]);
                        using (var fileStream = new FileStream(Path[1] + Path[0], FileMode.Open)) {
                            client.UploadFile(fileStream, Path[0]);
                        }
                    }
                    client.Disconnect();
                }
                MessageBox.Show(this, "All files uploaded fine.", "Done", MessageBoxButtons.OK);
            } catch(Exception ex) {
                MessageBox.Show(this, "Upload failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK);

            }
        }

        private JavaScriptSerializer JSON = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
        private FontConverter FontSerializer = new FontConverter();
        private void ApplyFont() {
            TitleTextPreview.Font = new Font(TitleViewFontDialog.Font.Name, 10.0f);
            TitleTextPreview.ForeColor = TitleViewFontColor.Color;
            TitleTextPreview.BackColor = TitleTextPreview.BackColor;

            ListTextPreview.Font = new Font(GridViewFontDialog.Font.Name, 10.0f);
            ListTextPreview.ForeColor = GridViewFontColor.Color;

            ListTextPreviewBG.Font = new Font(GridViewFontDialog.Font.Name, 10.0f);
            ListTextPreviewBG.ForeColor = GridViewFontColorBG.Color;

            ListTextPreviewBG.BackColor = ListTextPreviewBG.BackColor;
            ListTextPreview.BackColor = ListTextPreviewBG.BackColor;

            File.WriteAllText("Set\\Font.json",JSON.Serialize(new Dictionary<string, string>() {
                {"ListFont",FontSerializer.ConvertToString(GridViewFontDialog.Font)},
                {"ListColor",ColorTranslator.ToHtml(GridViewFontColorBG.Color)},
                {"ListActiveColor",ColorTranslator.ToHtml(GridViewFontColor.Color)},
                {"TitleFont",FontSerializer.ConvertToString(TitleViewFontDialog.Font)},
                {"TitleColor",ColorTranslator.ToHtml(TitleViewFontColor.Color)}
            }),Encoding.UTF8);
        }
        private void DoSelectGridFont_Click(object sender, EventArgs e) {
            GridViewFontDialog.ShowDialog(this);
            ApplyFont();
        }

        private void BtnListColor_Click(object sender, EventArgs e) {
            GridViewFontColor.ShowDialog(this);
            ApplyFont();
        }

        private void BtnTitleFont_Click(object sender, EventArgs e) {
            TitleViewFontDialog.ShowDialog(this);
            ApplyFont();
        }

        private void BtnTitleColor_Click(object sender, EventArgs e) {
            TitleViewFontColor.ShowDialog(this);
            ApplyFont();
        }

        private void BtnListColorBG_Click(object sender, EventArgs e) {
            GridViewFontColorBG.ShowDialog(this);
            ApplyFont();
        }

    }

    public class ROM {
        public string RomPath;
        public string ImagePath;
        public int BankNum;
        public int RomNum;

        public bool Exists {
            get => !String.IsNullOrWhiteSpace(RomPath) && File.Exists(RomPath.Trim());
        }

        public bool ImageExists {
            get => !String.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath.Trim());
        }
        public string Name {
            get => Exists ? Path.GetFileNameWithoutExtension(RomPath).Substring(1).Trim(): "";
        }

        public static string BuildPath(string name,int bankNum,int romNum) {
            return "Set\\Banks\\Bank" + bankNum + "\\" + romNum + " " + name.Trim() + ".gg";
        }

        public static string BuildImagePath(string name) {
            return "Set\\ArtWork\\" + name.Trim() + ".png";
        }

        public void Rename(string newName) {
            if(!Exists)
                return;
            if(newName==Name) return;

            var newPath=BuildPath(newName, BankNum, RomNum);
            File.Move(RomPath, newPath);
            RomPath=newPath;
        }

        public void RenameImage(string newName) {
            if (!ImageExists)
                return;
            var newPath = BuildImagePath(newName);
            if(newPath==ImagePath)
                return;

            File.Move(ImagePath, newPath);
            ImagePath = newPath;
        }

        public byte[] Bytes { get { return Exists?File.ReadAllBytes(RomPath):Array.Empty<byte>(); } }
    }
}
