using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

public struct OrderGroupHeadNode
{
    public string FileName;
    public short Index;
}
public struct GroupHead
{
    public byte[] Pre;
    public short NodeSize;
    public GroupHeadNode[] Node;
    
    public void SetPreHeader(byte[] nPreHeader) { Array.Copy(nPreHeader, 0, Pre, 0, 20); }
    public void SetNodeContent(byte[] nHeader, short index, int start, int size)
    {
        Array.Clear(Node[index].Content, 0, Node[index].Content.Length);
        Array.Copy(nHeader, start, Node[index].Content, 0, size);
    }

}
public struct GroupHeadNode
{
    public byte[] Content;
    public string FileName;
    public short Status;
    public int BlockSize;
    public short ImageCount;

}


namespace GEditor
{
    public partial class MainForm : Form
    {

        public class GroupInput
        {
            public GroupInput(byte[] nContent)
            {
                byte[] TmpName = new byte[10];
                Array.Copy(nContent, 12 , TmpName, 0, 10);

                string tmpChildName = System.Text.Encoding.UTF8.GetString(TmpName).Replace("\0", string.Empty);
                Console.WriteLine("Content {0} ", tmpChildName);
                ConName = tmpChildName;
                BlockSize = System.BitConverter.ToInt32(nContent, 4);
            }
            public GroupInput() { ConName = null; BlockSize = 0; }
            public void SetName(string nName) { ConName = nName;}
            public string ConName;
            public int BlockSize;
        }
        public class GroupImageInput
        {
            public GroupImageInput(int index, byte[] nContent) {


                Console.WriteLine("GroupImageInput Index {0} Pos {2} Length: {1} Remind {3} ", index,nContent.Length, GSizeHead + 144 + (index * GSizeChild), index, nContent.Length - (GSizeHead + 144 + (index * GSizeChild)));

                byte[] TmpName = new byte[100];
                Array.Copy(nContent, (GSizeHead + 144 + (index * GSizeChild)), TmpName, 0, 100); 
                string tmpChildName = System.Text.Encoding.UTF8.GetString(TmpName).Replace("\0", string.Empty);
                ConName = tmpChildName;
                ImgPosX = (int)(System.BitConverter.ToSingle(nContent, GSizeHead + 0 + (index * GSizeChild)));
                ImgPosY = (int)(System.BitConverter.ToSingle(nContent, GSizeHead + 4 + (index * GSizeChild)));
                ImgAngle = (short)System.BitConverter.ToSingle(nContent, GSizeHead + 72 + (index * GSizeChild));
                ImgScaleX = System.BitConverter.ToSingle(nContent, GSizeHead + 24 + (index * GSizeChild));
                ImgScaleY = System.BitConverter.ToSingle(nContent, GSizeHead + 28 + (index * GSizeChild));
                ImgWidth = (int)(System.BitConverter.ToInt32(nContent, GSizeHead + 356 + (index * GSizeChild)));
                ImgHeight = (int)(System.BitConverter.ToInt32(nContent, GSizeHead + 360 + (index * GSizeChild)));
                if (GIfMode && GInterface.ContainsKey(tmpChildName))
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(GInterface[tmpChildName]));
                    ImgWidthCal = image.Width;
                    ImgHeightCal = image.Height;
                }
                else
                {
                    ImgWidthCal = -1;
                    ImgHeightCal = -1;
                }
            }
            public GroupImageInput() { ConName = null; ImgPosX = 0; ImgPosY = 0; ImgAngle = 0; ImgScaleX = 0; ImgScaleY = 0; ImgWidth = 0; ImgHeight = 0; ImgWidthCal = 0; ImgHeightCal = 0; }
            public int GetImageCalWidth() { return ImgWidthCal; }
            public int GetImageCalHeight() { return ImgHeightCal; }
            public string ConName;
            public int ImgPosX;
            public int ImgPosY;
            public short ImgAngle;
            public float ImgScaleX;
            public float ImgScaleY;
            public int ImgWidth;
            public int ImgHeight;
            public int ImgWidthCal;
            public int ImgHeightCal;

        }
        public class GroupButtonInput
        {
            public GroupButtonInput(int index, byte[] nContent)
            {

                byte[] TmpName = new byte[100];
                Array.Copy(nContent, (GSizeHead + 244 + (index * GSizeChild)), TmpName, 0, 100);
                string tmpChildName = System.Text.Encoding.UTF8.GetString(TmpName).Replace("\0", string.Empty);
                ConName = tmpChildName;
                ConPosX = (int)(System.BitConverter.ToSingle(nContent, GSizeHead + 36 + (index * GSizeChild)));
                ConPosY = (int)(System.BitConverter.ToSingle(nContent, GSizeHead + 40 + (index * GSizeChild)));
                MaxPosX = (int)(System.BitConverter.ToSingle(nContent, GSizeHead + 48 + (index * GSizeChild)));
                MaxPosY = (int)(System.BitConverter.ToSingle(nContent, GSizeHead + 52 + (index * GSizeChild)));
                ImgWidth = MaxPosX - ConPosX;
                ImgHeight = MaxPosY - ConPosY;
                if (GIfMode && GInterface.ContainsKey(tmpChildName))
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(GInterface[tmpChildName]));
                    ImgWidthCal = image.Width;
                    ImgHeightCal = image.Height;
                } else
                {
                    ImgWidthCal = -1;
                    ImgHeightCal = -1;
                }
            }
            public GroupButtonInput() { ConName = null; ConPosX = 0; ConPosY = 0; }
            // public GroupButtonInput(string nName, int nPosX, int nPosY) { ConName = nName; ConPosX = nPosX; ConPosY = nPosY; }
            public int GetImageCalWidth() { return ImgWidthCal; }
            public int GetImageCalHeight() { return ImgHeightCal; }

            public string ConName;
            public int ConPosX;
            public int ConPosY;
            public int MaxPosX;
            public int MaxPosY;
            public int ImgWidth;
            public int ImgHeight;
            public int ImgWidthCal;
            public int ImgHeightCal;
        }

        public static int EditorGridSize = 50;
        public static int EditorGridSubSize = EditorGridSize / 5;
        public static GroupHead GHead;
        public static short GSizeHead = 400;
        public static short GSizeZipChild = 24;
        public static short GSizeChild = 364;
        public static string GTitle = "Group.tex Editor";
        public static GroupInput GInput;
        public static GroupImageInput GInputImage;
        public static GroupButtonInput GInputButton;
        public static Dictionary<string, byte[]> GInterface;
        public static AutoCompleteStringCollection GInterfaceListFile;
        public static bool GIfMode;
        public static short GIfLastRenderNum;
        public static short GSelectParentNode;
        public static short GSelectSubNode;
        public static bool GIfCacheEdit;
        public static bool GIfLastSelect;
        public static Color GColorSelectHilightImage = Color.Green;
        public static Color GColorBackGroundPanel = Color.Maroon;

        public static bool GRenderCrossHair;
        public static int GRenderCrossHairPosX;
        public static int GRenderCrossHairPosY;
        public static bool GTextboxEventStart;
        public MainForm()
        {
            InitializeComponent();
            this.Width = 1280;
            this.Height = 820;
            PanelRenderArea.Width = 900;
            PanelRenderArea.Height = 650;

            LoadXml();
            LoadCheckPanel();


        }
        public void LoadXml()
        {
            if (File.Exists("config.xml"))
            {
#pragma warning disable CS0618
                XmlDataDocument xml = new XmlDataDocument();
#pragma warning restore CS0618
                FileStream fs = new FileStream("config.xml", FileMode.Open, FileAccess.Read);
                xml.Load(fs);
                XmlNodeList elemList = xml.GetElementsByTagName("saveinfo");
                string sOpgrouppath = elemList[0].Attributes["grouppath"].Value;
                string sOpinterfacepath = elemList[0].Attributes["interfacepath"].Value;
                string sOpCursorCross = elemList[0].Attributes["cursorcross"].Value;


                InputPathGroupTex.Text = sOpgrouppath;
                InputPathInterfaceTex.Text = sOpinterfacepath;
                GRenderCrossHair = Convert.ToBoolean(sOpCursorCross);



                fs.Close();
            }
        }

        public void SaveXml()
        {
            //Debug.WriteLine("SaveXml");
            if (File.Exists("config.xml"))
            {
                try
                {
                    File.Delete("config.xml");

                }
                catch
                {
                    //Debug.WriteLine("Failed write xml");
                }
            }

            if (String.IsNullOrEmpty(InputPathGroupTex.Text) == false)
            {
                XmlTextWriter writer = new XmlTextWriter("config.xml", System.Text.Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.WriteStartElement("saveinfo");
                writer.WriteAttributeString("grouppath", InputPathGroupTex.Text);
                writer.WriteAttributeString("interfacepath", (String.IsNullOrEmpty(InputPathInterfaceTex.Text) ? "" : InputPathInterfaceTex.Text));
                writer.WriteAttributeString("cursorcross", (GRenderCrossHair ? "true" : "false"));

                
                writer.WriteEndElement();
                writer.Flush();
                writer.Close();
            }

        }

        public void LoadCheckPanel()
        {

            CheckTexFileExist(true, InputPathGroupTex.Text);
            CheckTexFileExist(false, InputPathInterfaceTex.Text);

            GIfMode = false;
            LoadPanelArea.Visible = true;
            StartPanelArea.Visible = false;
            SubLoadPanelArea.Location = new Point(this.ClientSize.Width / 2 - SubLoadPanelArea.Size.Width / 2, this.ClientSize.Height / 2 - SubLoadPanelArea.Size.Height / 2);
        }

        public void StartEditor()
        {
            SaveXml();
            LoadPanelArea.Visible = false;
            StartPanelArea.Visible = true;
            GHead.Pre = new byte[20];
            GIfLastRenderNum = 0;
            TreeViewGroupTex.SelectedImageIndex = 4;
            LoadEditor(InputPathGroupTex.Text);
            GIfMode = LoadInterface(InputPathInterfaceTex.Text);
            GSelectParentNode = -1;
            GSelectSubNode = -1;
            bool nGifMode = GIfMode;
            GIfCacheEdit = false;
            GIfLastSelect = false;
            PanelRenderArea.BackColor = GColorBackGroundPanel;
            ToolViewInputCursorCross.Checked = GRenderCrossHair;




            ButtonAutoCalculateRect.Visible = nGifMode;
            ButtonAutoCalculateSize.Visible = nGifMode;
            TextboxImageInputName.AutoCompleteMode = (nGifMode ? AutoCompleteMode.SuggestAppend : AutoCompleteMode.None);
            TextboxImageInputName.AutoCompleteSource = (nGifMode ? AutoCompleteSource.CustomSource : AutoCompleteSource.None);
            TextboxImageInputName.AutoCompleteCustomSource = GInterfaceListFile;
            TextboxButtonInputName.AutoCompleteMode = (nGifMode ? AutoCompleteMode.SuggestAppend : AutoCompleteMode.None);
            TextboxButtonInputName.AutoCompleteSource = (nGifMode ? AutoCompleteSource.CustomSource : AutoCompleteSource.None);
            TextboxButtonInputName.AutoCompleteCustomSource = GInterfaceListFile;
            GTextboxEventStart = false;



            DrawGrid();


        }

        public void CheckValidate(bool found = false)
        {
            if (found)
            {
                ButtonStartEditor.Enabled = true;
            } else
            {
                ButtonStartEditor.Enabled = false;

            }
        }

        private void BrowseGroupTex_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                InputPathGroupTex.Text = openFileDialog.FileName;
                CheckTexFileExist(true, InputPathGroupTex.Text);
            }
        }
        private void BrowseInterfaceTex_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                InputPathInterfaceTex.Text = openFileDialog.FileName;
                CheckTexFileExist(false, InputPathInterfaceTex.Text);
            }
        }
        public void CheckTexFileExist(bool IsGroup, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                if (IsGroup)
                {
                    labelGroupTexInput.ForeColor = Color.White;
                    InputPathGroupTex.ForeColor = Color.White;
                    CheckValidate();
                }
                else
                {
                    labelInterfaceTexInput.ForeColor = Color.White;
                    InputPathInterfaceTex.ForeColor = Color.White;
                }

            }
            else
            {
                bool found = false;
                if (File.Exists(path))
                    found = true;

                if (IsGroup)
                {
                    labelGroupTexInput.ForeColor = (found ? Color.Green : Color.Red);
                    InputPathGroupTex.ForeColor = (found ? Color.Green : Color.Red);
                    CheckValidate(found);
                }
                else
                {
                    labelInterfaceTexInput.ForeColor = (found ? Color.Green : Color.Red);
                    InputPathInterfaceTex.ForeColor = (found ? Color.Green : Color.Red);
                }

            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartEditor();
        }



        public void DrawGrid()
        {


        }

      

        public bool LoadInterface(string nPath)
        {
            if (string.IsNullOrEmpty(nPath)) { return false; }
                

                // Load File================================================================
                byte[] GB = File.ReadAllBytes(nPath);
            if (GB != null && GB.Length > 0) { Statusbar.Text = "Loading Interface.tex plase wait."; }
            else { Statusbar.Text = "Error can't read Interface.tex file."; return false; }
            // Init ====================================================================

            byte[] tmpByte = { GB[12], GB[13], GB[14], GB[15] };
            short nInterfaceSize = BitConverter.ToInt16(tmpByte, 0);

            GInterface = new Dictionary<string, byte[]>(nInterfaceSize);
            GInterfaceListFile = new AutoCompleteStringCollection();
            int nPos = 20;
            byte[] BlockSize = new byte[4];
            byte[] Name = new byte[10];
            int nBlockSizeNum = 0;
            //nInterfaceSize
            for (short x = 0; x < nInterfaceSize; x++)
            {
                nBlockSizeNum = System.BitConverter.ToInt32(GB, nPos + 4);

                //Console.WriteLine("Size Index: {0} / {1}", nBlockSizeNum , nInterfaceSize);

                Array.Copy(GB, nPos + 12, Name, 0, 10);
                string tmpStrName = System.Text.Encoding.UTF8.GetString(Name).Replace("\0", string.Empty);

                byte[] tmpContent = new byte[nBlockSizeNum];
                Array.Copy(GB, nPos+GSizeZipChild, tmpContent, 0, nBlockSizeNum);
                GInterface[tmpStrName] = tmpContent;
                GInterfaceListFile.Add(tmpStrName);

                Statusbar.Text = "Loading Interface.tex (" + (x + 1) + "/" + nInterfaceSize + ")";
                nPos += nBlockSizeNum + GSizeZipChild;
            }



            Statusbar.Text = "Ready";
            return true;
        }
        public bool LoadEditor(string nPath)
        {
            // Load File================================================================
            byte[] GB = File.ReadAllBytes(nPath);
            if (GB != null && GB.Length > 0) { Statusbar.Text = "Loading Group.tex plase wait."; }
            else { Statusbar.Text = "Error can't read Group.tex file."; return false; }

            // Init ====================================================================
            GHead.SetPreHeader(GB);
            byte[] tmpByte = { GB[12], GB[13], GB[14], GB[15] };
            GHead.NodeSize = BitConverter.ToInt16(tmpByte, 0);

            if (GHead.Node == null) { GHead.Node = new GroupHeadNode[GHead.NodeSize]; }
            else { Array.Clear(GHead.Node, 0, GHead.Node.Length); Array.Resize<GroupHeadNode>(ref GHead.Node, GHead.NodeSize); }

            TreeViewGroupTex.BeginUpdate();
            TreeViewGroupTex.Nodes.Clear();
            int nPos = 20;

            byte[] ImageCount = new byte[4];
            byte[] BlockSize = new byte[4];
            byte[] Name = new byte[10];
            byte[] TmpName = new byte[100];
            byte[] BlockType = new byte[4];
            string PrevName = null;

            
            // LOOP =====================================================================
            for (short x = 0; x < GHead.NodeSize; x++)
            {

                // Get Block size
                Array.Copy(GB, (nPos + 4), BlockSize, 0, 4);
                GHead.Node[x].BlockSize = BitConverter.ToInt32(BlockSize, 0);

                // Set Content in box (zip header + content)
                if (GHead.Node[x].Content == null) { GHead.Node[x].Content = new byte[GHead.Node[x].BlockSize + GSizeZipChild]; }
                else { Array.Resize<byte>(ref GHead.Node[x].Content, GHead.Node[x].BlockSize + GSizeZipChild); }
                GHead.SetNodeContent(GB, x, nPos, GHead.Node[x].BlockSize + GSizeZipChild);

                Array.Copy(GHead.Node[x].Content, (12), Name, 0, 10);
                Array.Copy(GHead.Node[x].Content, (392), ImageCount, 0, 4);


                // Set Global
                GHead.Node[x].ImageCount = BitConverter.ToInt16(ImageCount, 0);






                //ByteArrayToFile(x.ToString(), GHead.Node[x].Content);
                string tmpStrName = System.Text.Encoding.UTF8.GetString(Name).Replace("\0", string.Empty);
                GHead.Node[x].FileName = tmpStrName;
                GHead.Node[x].Status = 0;

                if (String.IsNullOrEmpty(PrevName) == false && PrevName == tmpStrName)
                {
                    TreeViewGroupTex.Nodes.Add(x.ToString(), tmpStrName + " (*duplicate name)");
                    TreeViewGroupTex.Nodes[x.ToString()].ImageIndex = 1;
                    TreeViewGroupTex.Nodes[x.ToString()].ForeColor = Color.Red;

                }
                else
                {
                    TreeViewGroupTex.Nodes.Add(x.ToString(), tmpStrName);
                }
                PrevName = tmpStrName;



                for (short y = 0; y < GHead.Node[x].ImageCount; y++)
                {



                    Array.Copy(GHead.Node[x].Content, (GSizeHead + 344 + (y * GSizeChild)), BlockType, 0, 4);
                    int nBlockType = 0;
                    string nBlockTypeName = null;
                    if (BitConverter.ToInt32(BlockType, 0) == 3)
                    {
                        Array.Copy(GHead.Node[x].Content, (GSizeHead + 244 + (y * GSizeChild)), TmpName, 0, 100);  // Target for Control
                        nBlockType = 3;
                        nBlockTypeName = "B";

                    }
                    else
                    {
                        nBlockTypeName = "I";
                        nBlockType = 2;
                        Array.Copy(GHead.Node[x].Content, (GSizeHead + 144 + (y * GSizeChild)), TmpName, 0, 100); // Imag name from Image
                    }

                    // Clear junk byte
                    for (short z = 1; z < 100; z++)
                    {
                        if(TmpName[z] == 0)
                        {
                            byte[] zerobytes = new byte[100 - (z)];
                            Buffer.BlockCopy(zerobytes, 0, GHead.Node[x].Content, GSizeHead + (BitConverter.ToInt32(BlockType, 0) == 3 ? 244 : 144) + (z) + (y * GSizeChild), 100 - (z));
                            Buffer.BlockCopy(zerobytes, 0, TmpName, z , 100 - (z));

                            //Console.WriteLine(x + "/" + y + "/" + z + "\r\n");
                            break;
                        }                        
                    }

                    string tmpChildName = System.Text.Encoding.UTF8.GetString(TmpName).Replace("\0", string.Empty);
                    if (tmpChildName.Length == 0) { tmpChildName = "*(empty)"; }

                    TreeViewGroupTex.Nodes[x].Nodes.Add(nBlockTypeName + y.ToString(), tmpChildName, nBlockType);

                    
                }
                
                nPos += GHead.Node[x].BlockSize + GSizeZipChild;
            }
            TreeViewGroupTex.EndUpdate();
            

            /*
            Debug.Write("Final Byte : " + GB.Length + " \n");
            int nsize = GHead.Pre.Length;
            byte[] tmpWrite = new byte[GB.Length];
            GHead.Pre.CopyTo(tmpWrite, 0);
            int PosX = GHead.Pre.Length;
            for (short x = 0; x < GHead.NodeSize; x++)
            {

                GHead.Node[x].Content.CopyTo(tmpWrite, PosX);


                nsize += GHead.Node[x].Content.Length;
                PosX += GHead.Node[x].Content.Length;
            }

            Debug.Write("Final 2 Byte : " + nsize + " \n");
            */


            Statusbar.Text = "Ready.";
            this.Text = GTitle + " [" + nPath + "]";
            return true;
            /*
            try
            {
                using (var fs = new FileStream("C:\\DreamINC\\Group.tex.sav", FileMode.Create, FileAccess.Write))
                {
                    fs.Write(tmpWrite, 0, tmpWrite.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }*/


            ///  MessageBox.Show("BYte " + GroupFileBinary[15] +"-"+ GroupFileBinary[14] + "-" + GroupFileBinary[13] + "-" + GroupFileBinary[12]+"  XX");



        }
        public void eventInterfacePictureClick(object sender, EventArgs e)
        {
            Debug.WriteLine("Click");
        }
        public void eventInterfacePictureMove(object sender, EventArgs e)
        {
            Debug.WriteLine("Move");
        }
        public void eventInterfacePictureEnter(object sender, EventArgs e)
        {
            Debug.WriteLine("Enter");
        }
        public void eventInterfacePictureLeave(object sender, EventArgs e)
        {
            Debug.WriteLine("Leave");
        }
        public void eventInterfacePictureHover(object sender, EventArgs e)
        {
            Debug.WriteLine("Hover");
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

            //Debug.WriteLine("W" + PanelRenderArea.Width);
            int nLineXnum = PanelRenderArea.Width / EditorGridSize;
            int nLineYnum = PanelRenderArea.Height / EditorGridSize;
            Point PanelLoc = PanelRenderArea.PointToScreen(Point.Empty);
            int PosX = PanelLoc.X;
            int PosY = PanelLoc.Y;
            base.OnPaint(e);
            using (Graphics g = e.Graphics)
            {
                // top
                var p = new Pen(Color.Gray, 1);
                var p2 = new Pen(Color.Red, 1);
                for (int x = 0; x <= nLineXnum; x++)
                {

                    var px1 = new Point(40 + (x * EditorGridSize), 0);
                    var px2 = new Point(40 + (x * EditorGridSize), 20);
                    g.DrawLine(p, px1, px2);


                    if (x < nLineXnum)
                    {
                        for (int xs = 0; xs < 4; xs++)
                        {
                            var pxs1 = new Point(40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize), 0);
                            var pxs2 = new Point(40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize), 6);
                            g.DrawLine(p, pxs1, pxs2);
                        }
                    }
                }

                // bot
                for (int x = 0; x <= nLineXnum; x++)
                {
                    var px1 = new Point(40 + (x * EditorGridSize), SplitMainEditor.Height - 20);
                    var px2 = new Point(40 + (x * EditorGridSize), SplitMainEditor.Height);
                    g.DrawLine(p, px1, px2);

                    if (x < nLineXnum)
                    {
                        for (int xs = 0; xs < 4; xs++)
                        {
                            var pxs1 = new Point(40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize), SplitMainEditor.Height - 11);
                            var pxs2 = new Point(40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize), SplitMainEditor.Height);
                            g.DrawLine(p, pxs1, pxs2);
                        }
                    }
                }

                // left
                for (int x = 0; x <= nLineYnum; x++)
                {
                    var px1 = new Point(0, 40 + (x * EditorGridSize));
                    var px2 = new Point(20, 40 + (x * EditorGridSize));
                    g.DrawLine(p, px1, px2);

                    if (x < nLineYnum)
                    {
                        for (int xs = 0; xs < 4; xs++)
                        {
                            var pxs1 = new Point(0, 40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize));
                            var pxs2 = new Point(6, 40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize));
                            g.DrawLine(p, pxs1, pxs2);
                        }
                    }

                }



                // right
                for (int x = 0; x <= nLineYnum; x++)
                {
                    var px1 = new Point(PanelRenderArea.Width + 58, 40 + (x * EditorGridSize));
                    var px2 = new Point(PanelRenderArea.Width + 80, 40 + (x * EditorGridSize));
                    g.DrawLine(p, px1, px2);

                    if (x < nLineYnum)
                    {
                        for (int xs = 0; xs < 4; xs++)
                        {
                            var pxs1 = new Point(PanelRenderArea.Width + 70, 40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize));
                            var pxs2 = new Point(PanelRenderArea.Width + 80, 40 + (x * EditorGridSize) + ((xs + 1) * EditorGridSubSize));
                            g.DrawLine(p, pxs1, pxs2);
                        }
                    }

                }

            }
        }

        private void TreeViewGroupTex_AfterSelect(object sender, TreeViewEventArgs e)
        {
            GTextboxEventStart = false;
            AlertImageFileNotFound.Visible = false;
            AlertButtonFileNotFound.Visible = false;
            short index = 0;
            short subindex = -1;
            if (e.Node.Name[0] == 'I')
            {
                index = Convert.ToInt16(e.Node.Parent.Name);
                try
                {
                    subindex = Int16.Parse(Regex.Match(e.Node.Name, @"\d+").Value);
                }
                catch (FormatException error)
                {
                    Console.WriteLine(error.Message);
                }

                GSelectParentNode = index;
                GSelectSubNode = subindex;
                Console.WriteLine("GSelectParentNode: {0} GSelectSubNode {1}", GSelectParentNode, GSelectSubNode);

                Console.WriteLine("NodeName ED: {0}", e.Node.Name);
                //Console.WriteLine("Node Index: {0}", e.Node.Parent.Name);
                //GHead.Node[index].Content
                GInputImage = new GroupImageInput(subindex,GHead.Node[index].Content);

                TextboxImageInputName.Text = GInputImage.ConName;
                TextboxImageInputPosX.Text = GInputImage.ImgPosX.ToString();
                TextboxImageInputPosY.Text = GInputImage.ImgPosY.ToString();

                //TextboxImageInputAngle.Text = GInputImage.ImgAngle.ToString();
                TextboxImageInputScaleX.Text = GInputImage.ImgScaleX.ToString();
                TextboxImageInputScaleY.Text = GInputImage.ImgScaleY.ToString();
                TextboxImageInputWidth.Text = GInputImage.ImgWidth.ToString();
                TextboxImageInputHeight.Text = GInputImage.ImgHeight.ToString();

                if(GIfMode)
                {

                    if (!GInterface.ContainsKey(TextboxImageInputName.Text))
                    {
                        AlertImageFileNotFound.Visible = true;
                    }
                }

                PanelInputButton.Visible = false;
                PanelInputImage.Visible = true;
                PanelInputNode.Visible = false;
            }
            else if (e.Node.Name[0] == 'B')
            {
                index = Convert.ToInt16(e.Node.Parent.Name);
                try
                {
                    subindex = Int16.Parse(Regex.Match(e.Node.Name, @"\d+").Value);
                }
                catch (FormatException error)
                {
                    Console.WriteLine(error.Message);
                }
                GSelectParentNode = index;
                GSelectSubNode = subindex;

                GInputButton = new GroupButtonInput(subindex, GHead.Node[index].Content);
                TextboxButtonInputName.Text = GInputButton.ConName;
                TextboxButtonInputPosX.Text = GInputButton.ConPosX.ToString();
                TextboxButtonInputPosY.Text = GInputButton.ConPosY.ToString();
                TextboxButtonInputWidth.Text = GInputButton.ImgWidth.ToString();
                TextboxButtonInputHeight.Text = GInputButton.ImgHeight.ToString();

                if (GIfMode)
                {
                    if (!GInterface.ContainsKey(TextboxButtonInputName.Text))
                    {
                        AlertButtonFileNotFound.Visible = true;
                    }
                    if (TextboxButtonInputWidth.Text != GInputButton.ImgWidthCal.ToString())
                    {
                        Console.WriteLine("Width mismatch" + GInputButton.ImgWidthCal.ToString());
                    }
                    if (TextboxButtonInputHeight.Text != GInputButton.ImgHeightCal.ToString())
                    {
                        Console.WriteLine("Height mismatch" + GInputButton.ImgHeightCal.ToString());
                    }
                }

                PanelInputButton.Visible = true;
                PanelInputImage.Visible = false;
                PanelInputNode.Visible = false;
            }
            else
            {

                index = Convert.ToInt16(e.Node.Name);
                GSelectParentNode = index;
                GSelectSubNode = -1;
                GInput = new GroupInput( GHead.Node[index].Content);
                TextboxGroupInputName.Text = GInput.ConName;
                LabelGroupByteSize.Text = GInput.BlockSize.ToString();
                Console.WriteLine("GInput.ConName: {0} ", GInput.ConName);

                PanelInputButton.Visible = false;
                PanelInputImage.Visible = false;
                PanelInputNode.Visible = true;
                //TreeViewGroupTex.CollapseAll();
                //TreeViewGroupTex.SelectedNode = TreeViewGroupTex.Nodes[GSelectParentNode];

                TreeViewGroupTex.Nodes[GSelectParentNode].Expand();
                //TreeViewGroupTex.Focus();
                //
            }
            RenderGroupInterface(index,subindex);
            Console.WriteLine("GSelectParentNode: {0} GSelectSubNode {1}", GSelectParentNode, GSelectSubNode);
            GTextboxEventStart = true;
            GIfLastSelect = true;

        }

        public static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }

        public void RenderGroupInterface(short x , short sub)
        {
            Console.WriteLine("GIfMode: {0} {1}", GIfMode, GIfLastSelect);
            if (!GIfMode) { return; }
            if (GIfLastSelect == true || GIfCacheEdit == true)
            {
                GIfCacheEdit = false;
                GIfLastSelect = false;
                byte[] BlockType = new byte[4];
                byte[] TmpName = new byte[100];
                int PosX = 0;
                int PosY = 0;
                int gWidth = 0;
                int gHeight = 0;
                int PicWidth = 0;
                int PicHeight = 0;
                float ScaleX = 1;
                float ScaleY = 1;

                DrawPictureBoxArea.Image = null;
                Image DrawImg = new Bitmap(900, 650);
                using (var graphics = Graphics.FromImage(DrawImg))
                {

                    for (short y = 0; y < GHead.Node[x].ImageCount; y++)
                    {
                        Array.Copy(GHead.Node[x].Content, (GSizeHead + 344 + (y * GSizeChild)), BlockType, 0, 4);
                        bool IsButton = (BitConverter.ToInt32(BlockType, 0) == 3 ? true : false);
                        if (IsButton)
                        {
                            Array.Copy(GHead.Node[x].Content, (GSizeHead + 244 + (y * GSizeChild)), TmpName, 0, 100);  // Target for Control
                            PosX = (int)(System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 36 + (y * GSizeChild)));
                            PosY = (int)(System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 40 + (y * GSizeChild)));
                            gWidth = (int)(System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 48 + (y * GSizeChild))) - PosX;
                            gHeight = (int)(System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 52 + (y * GSizeChild))) - PosY;
                        }
                        else
                        {
                            Array.Copy(GHead.Node[x].Content, (GSizeHead + 144 + (y * GSizeChild)), TmpName, 0, 100); // Imag name from Image
                            PosX = (int)(System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 0 + (y * GSizeChild)));
                            PosY = (int)(System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 4 + (y * GSizeChild)));
                            ScaleX = (System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 24 + (y * GSizeChild)));
                            ScaleY = (System.BitConverter.ToSingle(GHead.Node[x].Content, GSizeHead + 28 + (y * GSizeChild)));
                            gWidth = (int)(System.BitConverter.ToInt32(GHead.Node[x].Content, GSizeHead + 356 + (y * GSizeChild)));
                            gHeight = (int)(System.BitConverter.ToInt32(GHead.Node[x].Content, GSizeHead + 360 + (y * GSizeChild)));


                        }


                        bool nSpace = false;
                        for (int i = 0; i < TmpName.Length; i++)
                        {
                            if (TmpName[i] == 0x00 || nSpace == true)
                            {
                                nSpace = true;
                                TmpName[i] = 0x00;
                            }
                        }
                        string tmpChildName = System.Text.Encoding.UTF8.GetString(TmpName).Replace("\0", string.Empty);
                        
                        System.Drawing.Image tmpImg = null;
                        ColorMatrix matrix = new ColorMatrix();


                        if (GInterface.ContainsKey(tmpChildName)) {
                            tmpImg = ByteToImage(GInterface[tmpChildName]);
                            PicWidth = Convert.ToInt32((double)(gWidth * ScaleX));
                            PicHeight = Convert.ToInt32((double)(gHeight * ScaleY));

                        } else {
                            tmpImg = new Bitmap(1, 1);
                            PicWidth = 1;
                            PicHeight = 1;
                        }

                        //Console.WriteLine("Interface Pos " + PosX +" "+ PosY);
                        /*var picture = new PictureBox
                        {
                            Name = "Pic"+y,
                            Size = new Size(PicWidth, PicHeight),
                            Location = new Point(PosX, PosY),
                            Image = null,
                            BackColor = Color.Transparent,

                        };*/




                        if (sub > 0 && sub == y)
                        {
                            const int borderSize = 1;

                            using (Brush border = new SolidBrush(GColorSelectHilightImage))
                            {
                                graphics.FillRectangle(border, PosX - borderSize, PosY - borderSize, PicWidth + borderSize, PicHeight + borderSize);
                            }

                        }
                        if (ScaleX > 2 && ScaleY <= 1)
                        {
                            for (int ix = 0; ix < (int)ScaleX; ix++)
                            {
                                graphics.DrawImage(tmpImg, new Rectangle(PosX + ix, PosY, gWidth, gHeight));
                            }
                        }
                        else if(ScaleX <= 1 && ScaleY > 2)
                        {
                            for (int iy = 0; iy < (int)ScaleY; iy++)
                            {
                                graphics.DrawImage(tmpImg, new Rectangle(PosX, PosY + iy, gWidth, gHeight));
                            }
                        }
                        else
                        {
                            graphics.DrawImage(tmpImg, new Rectangle(PosX, PosY, PicWidth, PicHeight));
                        }
                    }
                
                }

                DrawPictureBoxArea.Image = DrawImg;
                GIfLastRenderNum = GHead.Node[x].ImageCount;
            }
        }
        
        
        // Validate function
        private void KeypressFloatBox(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }
        private void KeypressIntBox(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) )
            {
                e.Handled = true;
            }
        }

        private void ToolBackToMain_Click(object sender, EventArgs e)
        {
            LoadPanelArea.Visible = true;
            StartPanelArea.Visible = false;
            GInterface.Clear();
            SaveXml();
        }

        private void ToolViewGroupDescription_CheckedChanged(object sender, EventArgs e)
        {
            if (ToolViewGroupDescription.Checked)
            {
                IconHelpDesc.Visible = true;
            }
            else
            {

                IconHelpDesc.Visible = false;
            }
        }

        private void ToolViewInputDescription_CheckedChanged(object sender, EventArgs e)
        {
            if (ToolViewInputDescription.Checked)
            {
                LabelHelp01.Visible = true;
                LabelHelp02.Visible = true;
                LabelHelp04.Visible = true;
                LabelHelp05.Visible = true;
                LabelHelp08.Visible = true;
                LabelHelp09.Visible = true;
                LabelHelp10.Visible = true;
                LabelHelp11.Visible = true;
                LabelHelp12.Visible = true;
            }
            else
            {                
                LabelHelp01.Visible = false;
                LabelHelp02.Visible = false;
                LabelHelp04.Visible = false;
                LabelHelp05.Visible = false;
                LabelHelp08.Visible = false;
                LabelHelp09.Visible = false;
                LabelHelp10.Visible = false;
                LabelHelp11.Visible = false;
                LabelHelp12.Visible = false;
            }
            
        }
        public bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }
        }

        private void TextboxGroupInputName_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return;}
            TreeViewGroupTex.Nodes[GSelectParentNode].Text = TextboxGroupInputName.Text;
            byte[] bytes = Encoding.ASCII.GetBytes(TextboxGroupInputName.Text.PadRight(10, '\0'));
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, 12, 10);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, 168, 10);
            GHead.Node[GSelectParentNode].FileName = TextboxGroupInputName.Text;
        }



        

        private void ButtonAutoCalculateRect_Click(object sender, EventArgs e)
        {
            if(GSelectSubNode >= 0 && GIfMode)
            {
                TextboxImageInputWidth.Text = GInputImage.GetImageCalWidth().ToString();
                TextboxImageInputHeight.Text = GInputImage.GetImageCalHeight().ToString();
                TextboxImageInputWidth_TextChanged(sender, e);
                TextboxImageInputHeight_TextChanged(sender, e);
            }
        }

        private void AlertImageFileNotFound_MouseHover(object sender, EventArgs e)
        {
            ToolTip tt = new ToolTip();
            tt.SetToolTip(this.AlertImageFileNotFound, "Image file not found on interface.tex");

        }

        private void AlertButtonFileNotFound_MouseHover(object sender, EventArgs e)
        {
            ToolTip tt = new ToolTip();
            tt.SetToolTip(this.AlertButtonFileNotFound, "Image file not found on interface.tex");

        }

        // Image Edit
        private void TextboxImageInputName_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            if (TextboxImageInputName.Text.Length > 0 && GIfMode)
            {
                if (GInterface.ContainsKey(TextboxImageInputName.Text))
                {   
                    AlertImageFileNotFound.Visible = false;
                } else
                {
                    AlertImageFileNotFound.Visible = true;
                }
            }

            TreeViewGroupTex.Nodes[GSelectParentNode].Nodes[GSelectSubNode].Text = TextboxImageInputName.Text;
            byte[] bytes = Encoding.ASCII.GetBytes(TextboxImageInputName.Text.PadRight(100, '\0'));
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, (GSizeHead + 144 + (GSelectSubNode * GSizeChild)), 100);
        }

        private void TextboxImageInputPosX_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxImageInputPosX_TextChanged");
            GIfCacheEdit = true;
            float nTmp = (TextboxImageInputPosX.Text.Length > 0 ? (float)Convert.ToDouble(TextboxImageInputPosX.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 0 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void TextboxImageInputPosY_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }

            Console.WriteLine("TextboxImageInputPosY_TextChanged");
            GIfCacheEdit = true;
            float nTmp = (TextboxImageInputPosY.Text.Length > 0 ? (float)Convert.ToDouble(TextboxImageInputPosY.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 4 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }
        private void TextboxImageInputWidth_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxImageInputWidth_TextChanged");
            GIfCacheEdit = true;
            int nTmp = (TextboxImageInputWidth.Text.Length > 0 ? Convert.ToInt32(TextboxImageInputWidth.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 356 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void TextboxImageInputHeight_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxImageInputHeight_TextChanged");
            GIfCacheEdit = true;
            int nTmp = (TextboxImageInputHeight.Text.Length > 0 ? Convert.ToInt32(TextboxImageInputHeight.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 360 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void TextboxImageInputScaleX_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxImageInputScaleX_TextChanged");
            GIfCacheEdit = true;
            float nTmp = (TextboxImageInputScaleX.Text.Length > 0 ? (float)Convert.ToDouble(TextboxImageInputScaleX.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 24 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void TextboxImageInputScaleY_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxImageInputScaleY_TextChanged");
            GIfCacheEdit = true;
            float nTmp = (TextboxImageInputScaleY.Text.Length > 0 ? (float)Convert.ToDouble(TextboxImageInputScaleY.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 28 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        // Button Edit
        private void TextboxButtonInputName_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            if (TextboxButtonInputName.Text.Length > 0 && GIfMode)
            {
                if (GInterface.ContainsKey(TextboxButtonInputName.Text))
                {
                    AlertButtonFileNotFound.Visible = false;
                }
                else
                {
                    AlertButtonFileNotFound.Visible = true;
                }
            }

            TreeViewGroupTex.Nodes[GSelectParentNode].Nodes[GSelectSubNode].Text = TextboxButtonInputName.Text;
            byte[] bytes = Encoding.ASCII.GetBytes(TextboxButtonInputName.Text.PadRight(100, '\0'));
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, (GSizeHead + 244 + (GSelectSubNode * GSizeChild)), 100);
        }

        private void TextboxButtonInputPosX_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxButtonInputPosX_TextChanged");
            GIfCacheEdit = true;
            float nTmp = (TextboxButtonInputPosX.Text.Length > 0 ? (float)Convert.ToDouble(TextboxButtonInputPosX.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 36 + (GSelectSubNode * GSizeChild), 4);

            float nTmpSub = nTmp + (TextboxButtonInputWidth.Text.Length > 0 ? (float)Convert.ToDouble(TextboxButtonInputWidth.Text) : 0);
            byte[] bytesSub = BitConverter.GetBytes(nTmpSub);
            Buffer.BlockCopy(bytesSub, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 48 + (GSelectSubNode * GSizeChild), 4);
        }

        private void TextboxButtonInputPosY_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxButtonInputPosY_TextChanged");
            GIfCacheEdit = true;
            float nTmp = (TextboxButtonInputPosY.Text.Length > 0 ? (float)Convert.ToDouble(TextboxButtonInputPosY.Text) : 0);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 40 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);

            float nTmpSub = nTmp + (TextboxButtonInputHeight.Text.Length > 0 ? (float)Convert.ToDouble(TextboxButtonInputHeight.Text) : 0);
            byte[] bytesSub = BitConverter.GetBytes(nTmpSub);
            Buffer.BlockCopy(bytesSub, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 52 + (GSelectSubNode * GSizeChild), 4);
        }

        private void TextboxButtonInputWidth_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxButtonInputWidth_TextChanged");
            GIfCacheEdit = true;
            float nTmpPosX = (TextboxButtonInputPosX.Text.Length > 0 ? (float)Convert.ToDouble(TextboxButtonInputPosX.Text) : 0);
            float nTmp = (TextboxButtonInputWidth.Text.Length > 0 ? ((float)(Convert.ToDouble(TextboxButtonInputWidth.Text) + nTmpPosX)) : nTmpPosX + 1);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 48 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void TextboxButtonInputHeight_TextChanged(object sender, EventArgs e)
        {
            if (GTextboxEventStart == false) { return; }
            Console.WriteLine("TextboxButtonInputHeight_TextChanged");
            GIfCacheEdit = true;
            float nTmpPosY = (TextboxButtonInputPosY.Text.Length > 0 ? (float)Convert.ToDouble(TextboxButtonInputPosY.Text) : 0);
            float nTmp = (TextboxButtonInputHeight.Text.Length > 0 ? ((float)(Convert.ToDouble(TextboxButtonInputHeight.Text) + nTmpPosY)) : nTmpPosY + 1);
            byte[] bytes = BitConverter.GetBytes(nTmp);
            Buffer.BlockCopy(bytes, 0, GHead.Node[GSelectParentNode].Content, GSizeHead + 52 + (GSelectSubNode * GSizeChild), 4);
            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void ButtonAutoCalculateSize_Click(object sender, EventArgs e)
        {
            if (GSelectSubNode >= 0 && GIfMode)
            {
                TextboxButtonInputWidth.Text = GInputButton.GetImageCalWidth().ToString();
                TextboxButtonInputHeight.Text = GInputButton.GetImageCalHeight().ToString();
                TextboxButtonInputWidth_TextChanged(sender, e);
                TextboxButtonInputHeight_TextChanged(sender, e);
            }
        }

        private void ToolBackToMain_Click_1(object sender, EventArgs e)
        {
            LoadCheckPanel();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SaveXml();
            System.Windows.Forms.Application.Exit();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileAsDialog = new SaveFileDialog();
            saveFileAsDialog.Filter = "Tex|*.tex";
            saveFileAsDialog.Title = "Save group.tex File";

            saveFileAsDialog.ShowDialog();

            if (saveFileAsDialog.FileName != "")
            {
                ProcessSaveData(saveFileAsDialog.FileName);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

            ProcessSaveData(InputPathGroupTex.Text);
        }

        private void ProcessSaveData(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {

                int Size = GHead.Pre.Length;
                
                Array.Sort<GroupHeadNode>(GHead.Node, (x, y) => x.FileName.CompareTo(y.FileName));
                for (short x = 0; x < GHead.NodeSize; x++)
                {
                    Size += GHead.Node[x].Content.Length;
                }

                byte[] tmpWrite = new byte[Size];
                
                byte[] bytesTotalSize = BitConverter.GetBytes(Size);
                Buffer.BlockCopy(bytesTotalSize, 0, GHead.Pre, 8, 4);

                GHead.Pre.CopyTo(tmpWrite, 0);
                int PosX = GHead.Pre.Length;
                for (short x = 0; x < GHead.NodeSize; x++)
                {
                    GHead.Node[x].Content.CopyTo(tmpWrite, PosX);
                    PosX += GHead.Node[x].Content.Length;
                }

                try
                {
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(tmpWrite, 0, tmpWrite.Length);

                        Console.WriteLine("Success");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception caught in process: {0}", ex);
                }

            }
        }

        private void DrawPictureBoxArea_MouseMove(object sender, MouseEventArgs e)
        {
            LebelPosX.Text = "X: "+e.X.ToString();
            LabelPosY.Text = "Y: "+e.Y.ToString();

            if (GRenderCrossHair)
            {
                Region r = new Region();
                r.Union(new Rectangle(0, GRenderCrossHairPosY, DrawPictureBoxArea.Width, 1));
                r.Union(new Rectangle(GRenderCrossHairPosX, 0, 1, DrawPictureBoxArea.Height));
                DrawPictureBoxArea.Invalidate(r);
                DrawPictureBoxArea.Update();
                Graphics g = Graphics.FromHwnd(DrawPictureBoxArea.Handle);
                g.DrawLine(Pens.Black, 0, e.Y, DrawPictureBoxArea.Width, e.Y);
                g.DrawLine(Pens.Black, e.X, 0, e.X, DrawPictureBoxArea.Height);
                //to draw the circle

                GRenderCrossHairPosX = e.X;
                GRenderCrossHairPosY = e.Y;
            }            
        }

        private void DrawPictureBoxArea_MouseLeave(object sender, EventArgs e)
        {
            if (GRenderCrossHair)
            {
                DrawPictureBoxArea.Invalidate();
                GRenderCrossHairPosX = 0;
                GRenderCrossHairPosY = 0;
            }
            LebelPosX.Text = "";
            LabelPosY.Text = "";
        }


        private void ButtonGroupAddNewButton_Click(object sender, EventArgs e)
        {

            byte[] newButton = new byte[GSizeChild];
            // Name
            newButton[244] = 0x62;
            newButton[245] = 0x6C;
            newButton[246] = 0x61;
            newButton[247] = 0x6E;
            newButton[248] = 0x6B;

            // Type = 13
            newButton[344] = 0x03;
            UpsizeChild(newButton, "B", 3);
        }
        private void ButtonGroupAddNewImage_Click(object sender, EventArgs e)
        {
            byte[] newImg = new byte[GSizeChild];
            // Scale
            newImg[26] = 0x80;
            newImg[27] = 0x3F;
            newImg[30] = 0x80;
            newImg[31] = 0x3F;
            newImg[34] = 0x80;
            newImg[35] = 0x3F;

            // Scale Percent
            newImg[78] = 0x80;
            newImg[79] = 0x3F;

            // Name
            newImg[144] = 0x62;
            newImg[145] = 0x6C;
            newImg[146] = 0x61;
            newImg[147] = 0x6E;
            newImg[148] = 0x6B;

            // Type = 1 
            newImg[344] = 0x01;
            UpsizeChild(newImg, "I", 2);
        }
        
        private void UpsizeChild(byte[] NewByte, string nType, int nNum)
        {
            Array.Resize<byte>(ref GHead.Node[GSelectParentNode].Content, GHead.Node[GSelectParentNode].BlockSize + GSizeZipChild + GSizeChild);
            Buffer.BlockCopy(NewByte, 0, GHead.Node[GSelectParentNode].Content, GHead.Node[GSelectParentNode].BlockSize + GSizeZipChild, GSizeChild);
            GHead.Node[GSelectParentNode].BlockSize = GHead.Node[GSelectParentNode].BlockSize + GSizeChild;


            TreeViewGroupTex.Nodes[GSelectParentNode].Nodes.Add(nType + (GHead.Node[GSelectParentNode].ImageCount.ToString()), "blank", nNum);
            GHead.Node[GSelectParentNode].ImageCount = (short)(GHead.Node[GSelectParentNode].ImageCount + 1);

            int TmpImageCount = GHead.Node[GSelectParentNode].ImageCount;
            byte[] bytesImgCount = BitConverter.GetBytes(TmpImageCount);
            Buffer.BlockCopy(bytesImgCount, 0, GHead.Node[GSelectParentNode].Content, 392, 4);

            int TmpBlockSize = GHead.Node[GSelectParentNode].BlockSize;
            byte[] bytesBlockSize = BitConverter.GetBytes(TmpBlockSize);
            Buffer.BlockCopy(bytesBlockSize, 0, GHead.Node[GSelectParentNode].Content, 4, 4);
        }

        private void RemoveChild()
        {


            

            int NewSize = GHead.Node[GSelectParentNode].Content.Length - GSizeChild;            
            byte[] newByte = new byte[NewSize];            


            // Src
            // SrcOffeset
            // Dst
            // DstOffset
            // Length
            Buffer.BlockCopy(
                GHead.Node[GSelectParentNode].Content,
                0,                                                  // 0
                newByte, 
                0,                                                  // 0
                GSizeHead + (GSelectSubNode  * GSizeChild)           // 400 + 364 / 1128
                );

            Console.WriteLine("Src Offset {0} DstOffset {1} Size {2} Img {3} ", 0, 0, GSizeHead + ((GSelectSubNode) * GSizeChild) , GHead.Node[GSelectParentNode].ImageCount);
            Console.WriteLine("Src Offset {0} DstOffset {1} Size {2} Img {3}", GSizeHead + ((GSelectSubNode + 1) * GSizeChild), GSizeHead + (GSelectSubNode * GSizeChild), NewSize - (GSizeHead + (GSelectSubNode * GSizeChild)), GHead.Node[GSelectParentNode].ImageCount - 1);

            if(GSelectSubNode < GHead.Node[GSelectParentNode].ImageCount - 1 ) // Not last
            {
                //(GHead.Node[GSelectParentNode].ImageCount - 1 - GSelectSubNode)
                Buffer.BlockCopy(
                    GHead.Node[GSelectParentNode].Content,
                    GSizeHead + ((GSelectSubNode + 1) * GSizeChild),                   // 764
                    newByte,
                    GSizeHead + ((GSelectSubNode) * GSizeChild),                      // 400
                     ((GHead.Node[GSelectParentNode].ImageCount -1)  - GSelectSubNode) * GSizeChild     // 1820
                    );
            }


            bool nDeleted = false;
            byte[] BlockType = new byte[4];
            string nType = null;
            for (short x = GSelectSubNode; x < GHead.Node[GSelectParentNode].ImageCount; x++)
            {
                Console.WriteLine("LOOP {0} Select {1} ",x , GSelectSubNode);

               Array.Copy(GHead.Node[GSelectParentNode].Content, (GSizeHead + 344 + (x * GSizeChild)), BlockType, 0, 4);
                nType = (BitConverter.ToInt32(BlockType, 0) == 3 ? "B" : "I");
                if (x == GSelectSubNode && nDeleted == false) { TreeViewGroupTex.Nodes[GSelectParentNode].Nodes[nType + x.ToString()].Remove(); nDeleted = true; }
                else { int tmpInt = x - 1; TreeViewGroupTex.Nodes[GSelectParentNode].Nodes[nType + x.ToString()].Name = nType + tmpInt.ToString(); }
            }

            GHead.Node[GSelectParentNode].BlockSize = GHead.Node[GSelectParentNode].BlockSize - GSizeChild;
            GHead.Node[GSelectParentNode].ImageCount = (short)(GHead.Node[GSelectParentNode].ImageCount - 1);

            Console.WriteLine("Node Size Before {0}", GHead.Node[GSelectParentNode].Content.Length);

            Array.Resize<byte>(ref GHead.Node[GSelectParentNode].Content, NewSize);
            GHead.SetNodeContent(newByte, GSelectParentNode, 0, NewSize);
            Console.WriteLine("Node Size After {0}", GHead.Node[GSelectParentNode].Content.Length);

            int TmpImageCount = GHead.Node[GSelectParentNode].ImageCount;
            byte[] bytesImgCount = BitConverter.GetBytes(TmpImageCount);
            Buffer.BlockCopy(bytesImgCount, 0, GHead.Node[GSelectParentNode].Content, 392, 4);

            int TmpBlockSize = GHead.Node[GSelectParentNode].BlockSize;
            byte[] bytesBlockSize = BitConverter.GetBytes(TmpBlockSize);
            Buffer.BlockCopy(bytesBlockSize, 0, GHead.Node[GSelectParentNode].Content, 4, 4);

            RenderGroupInterface(GSelectParentNode, GSelectSubNode);
        }

        private void ButtonGroupClone_Click(object sender, EventArgs e)
        {
            ++GHead.NodeSize;
            Array.Resize<GroupHeadNode>(ref GHead.Node, GHead.NodeSize);
            GHead.Node[GHead.NodeSize].Status = 2;
            TreeViewGroupTex.Nodes.Add(GHead.NodeSize.ToString(), GHead.Node[GSelectParentNode].FileName);
        }

        private void ButtonGroupDelete_Click(object sender, EventArgs e)
        {
            GHead.Node[GSelectParentNode].Status = 1;
            TreeViewGroupTex.Nodes[GSelectParentNode].Remove();
        }

        private void ButtonButtonDelete_Click(object sender, EventArgs e) { RemoveChild(); }      
        private void ButtonImageDelete_Click(object sender, EventArgs e) { RemoveChild(); }

        private void ToolViewInputCursorCross_Click(object sender, EventArgs e)
        {
            if (ToolViewInputCursorCross.Checked)
            {
                GRenderCrossHair = true;
            }
            else
            {
                GRenderCrossHair = false;
            }
            
        }
    }
}

