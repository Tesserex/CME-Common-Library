using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan
{
    public class BossInfo
    {
        public int Slot { get; set; }
        public string Name { get; set; }
        public FilePath PortraitPath { get; set; }
        public string Stage { get; set; }
    }

    public class StageInfo
    {
        public string Name { get; set; }
        public FilePath StagePath { get; set; }
    }

    public class Project
    {
        #region Game XML File Stuff

        private List<StageInfo> stages = new List<StageInfo>();
        private List<BossInfo> bosses = new List<BossInfo>(8);
        private List<string> includeFiles = new List<string>();

        public IEnumerable<StageInfo> Stages
        {
            get { return stages; }
        }

        public void AddStage(StageInfo stage)
        {
            this.stages.Add(stage);
        }

        public IEnumerable<BossInfo> Bosses
        {
            get { return bosses; }
        }

        public IEnumerable<string> Includes
        {
            get { return includeFiles; }
        }

        public string Name
        {
            get;
            set;
        }

        public string Author
        {
            get;
            set;
        }

        public string GameFile { get; private set; }
        public string BaseDir { get; private set; }

        public int ScreenWidth
        {
            get;
            set;
        }
        public int ScreenHeight
        {
            get;
            set;
        }

        public Sprite BossFrameSprite
        {
            get;
            set;
        }

        public FilePath MusicNSF { get; set; }
        public FilePath EffectsNSF { get; set; }

        public FilePath StageSelectIntro
        {
            get;
            set;
        }

        public FilePath StageSelectLoop
        {
            get;
            set;
        }

        public FilePath StageSelectBackground
        {
            get;
            set;
        }

        public FilePath StageSelectChangeSound
        {
            get;
            set;
        }

        public int BossSpacingHorizontal
        {
            get;
            set;
        }

        public int BossSpacingVertical
        {
            get;
            set;
        }

        public int BossOffset
        {
            get;
            set;
        }

        public FilePath PauseScreenBackground
        {
            get;
            set;
        }

        public SoundInfo PauseChangeSound
        {
            get;
            set;
        }

        public SoundInfo PauseSound
        {
            get;
            set;
        }

        public Point PauseLivesPosition { get; set; }

        #endregion

        public Project()
        {
            // sensible defaults where possible
            ScreenWidth = 256;
            ScreenHeight = 224;
            BossSpacingHorizontal = 24;
            BossSpacingVertical = 16;

            for (int i = 0; i < 8; i++)
            {
                var boss = new BossInfo();
                boss.Slot = -1;
                bosses.Add(boss);
            }
        }

        public void Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("The project file does not exist: " + path);

            GameFile = path;
            BaseDir = Path.GetDirectoryName(path);
            XElement reader = XElement.Load(path);

            XAttribute nameAttr = reader.Attribute("name");
            if (nameAttr != null) this.Name = nameAttr.Value;

            XAttribute authAttr = reader.Attribute("author");
            if (authAttr != null) this.Author = authAttr.Value;

            XElement sizeNode = reader.Element("Size");
            if (sizeNode != null)
            {
                int across, down;
                if (int.TryParse(sizeNode.Attribute("x").Value, out across))
                {
                    ScreenWidth = across;
                }
                else ScreenWidth = 0;

                if (int.TryParse(sizeNode.Attribute("y").Value, out down))
                {
                    ScreenHeight = down;
                }
                else ScreenHeight = 0;
            }

            XElement nsfNode = reader.Element("NSF");
            if (nsfNode != null) LoadNSFInfo(nsfNode);

            XElement stagesNode = reader.Element("Stages");
            if (stagesNode != null)
            {
                foreach (XElement stageNode in stagesNode.Elements("Stage"))
                {
                    var info = new StageInfo();
                    info.Name = stageNode.RequireAttribute("name").Value;
                    info.StagePath = FilePath.FromRelative(stageNode.RequireAttribute("path").Value, this.BaseDir);
                    stages.Add(info);
                }
            }

            XElement stageSelectNode = reader.Element("StageSelect");
            if (stageSelectNode != null)
            {
                XElement musicNode = stageSelectNode.Element("Music");
                if (musicNode != null)
                {
                    var introNode = musicNode.Element("Intro");
                    var loopNode = musicNode.Element("Loop");
                    if (introNode != null) this.StageSelectIntro = FilePath.FromRelative(introNode.Value, this.BaseDir);
                    if (loopNode != null) this.StageSelectLoop = FilePath.FromRelative(loopNode.Value, this.BaseDir);
                }

                XElement soundNode = stageSelectNode.Element("ChangeSound");
                if (soundNode != null)
                {
                    this.PauseChangeSound = SoundInfo.FromXml(soundNode, this.BaseDir);
                }

                XElement bgNode = stageSelectNode.Element("Background");
                if (bgNode != null) this.StageSelectBackground = FilePath.FromRelative(bgNode.Value, this.BaseDir);

                XElement bossFrame = stageSelectNode.Element("BossFrame");
                if (bossFrame != null)
                {
                    XElement bossSprite = bossFrame.Element("Sprite");
                    if (bossSprite != null) this.BossFrameSprite = Sprite.FromXml(bossSprite, this.BaseDir);
                }

                XElement spacingNode = stageSelectNode.Element("Spacing");
                if (spacingNode != null)
                {
                    int x = BossSpacingHorizontal;
                    if (spacingNode.TryInteger("x", out x))
                    {
                        BossSpacingHorizontal = x;
                    }

                    int y = BossSpacingVertical;
                    if (spacingNode.TryInteger("y", out y))
                    {
                        BossSpacingVertical = y;
                    }

                    int off = BossOffset;
                    if (spacingNode.TryInteger("offset", out off))
                    {
                        BossOffset = off;
                    }
                }

                int bossIndex = 0;
                foreach (XElement bossNode in stageSelectNode.Elements("Boss"))
                {
                    XAttribute slotAttr = bossNode.Attribute("slot");
                    int slot = -1;
                    if (slotAttr != null) int.TryParse(slotAttr.Value, out slot);

                    BossInfo info = this.bosses[bossIndex];
                    info.Slot = slot;
                    var bossNameAttr = bossNode.Attribute("name");
                    if (bossNameAttr != null) info.Name = nameAttr.Value;
                    var portrait = bossNode.Attribute("portrait");
                    if (portrait != null) info.PortraitPath = FilePath.FromRelative(portrait.Value, this.BaseDir);
                    info.Stage = bossNode.RequireAttribute("stage").Value;
                    bossIndex++;
                }
            }

            XElement pauseNode = reader.Element("PauseScreen");

            foreach (XElement entityNode in reader.Elements("Entities"))
            {
                if (!string.IsNullOrEmpty(entityNode.Value.Trim())) includeFiles.Add(entityNode.Value);
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(this.GameFile)) return;

            XmlTextWriter writer = new XmlTextWriter(this.GameFile, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 1;
            writer.IndentChar = '\t';

            writer.WriteStartElement("Game");
            if (!string.IsNullOrEmpty(this.Name)) writer.WriteAttributeString("name", this.Name);
            if (!string.IsNullOrEmpty(this.Author)) writer.WriteAttributeString("author", this.Author);
            writer.WriteAttributeString("version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

            writer.WriteStartElement("Size");
            writer.WriteAttributeString("x", this.ScreenWidth.ToString());
            writer.WriteAttributeString("y", this.ScreenHeight.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("Stages");
            foreach (var info in stages)
            {
                writer.WriteStartElement("Stage");
                writer.WriteAttributeString("name", info.Name);
                writer.WriteAttributeString("path", info.StagePath.Relative);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // Stages

            writer.WriteStartElement("StageSelect");

            writer.WriteStartElement("Music");
            if (this.StageSelectIntro != null) writer.WriteElementString("Intro", this.StageSelectIntro.Relative);
            if (this.StageSelectLoop != null) writer.WriteElementString("Loop", this.StageSelectLoop.Relative);
            writer.WriteEndElement();

            if (this.StageSelectChangeSound != null)
            {
                writer.WriteStartElement("ChangeSound");
                writer.WriteAttributeString("path", this.StageSelectChangeSound.Relative);
                writer.WriteEndElement(); // ChangeSound
            }
            if (this.StageSelectBackground != null) writer.WriteElementString("Background", this.StageSelectBackground.Relative);

            if (this.BossFrameSprite != null)
            {
                writer.WriteStartElement("BossFrame");
                this.BossFrameSprite.WriteTo(writer);
                writer.WriteEndElement(); // BossFrame
            }

            writer.WriteStartElement("Spacing");
            writer.WriteAttributeString("x", this.BossSpacingHorizontal.ToString());
            writer.WriteAttributeString("y", this.BossSpacingVertical.ToString());
            writer.WriteAttributeString("offset", this.BossOffset.ToString());
            writer.WriteEndElement();

            foreach (BossInfo boss in this.bosses)
            {
                writer.WriteStartElement("Boss");
                if (boss.Slot >= 0) writer.WriteAttributeString("slot", boss.Slot.ToString());
                if (!string.IsNullOrEmpty(boss.Name)) writer.WriteAttributeString("name", boss.Name);
                if (boss.PortraitPath != null && !string.IsNullOrEmpty(boss.PortraitPath.Relative)) writer.WriteAttributeString("portrait", boss.PortraitPath.Relative);
                if (!string.IsNullOrEmpty(boss.Stage)) writer.WriteAttributeString("stage", boss.Stage);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // StageSelect

            foreach (string entityFile in this.includeFiles)
            {
                writer.WriteElementString("Include", entityFile);
            }

            writer.WriteEndElement(); // Game

            writer.Close();
        }

        private void LoadNSFInfo(XElement nsfNode)
        {
            XElement musicNode = nsfNode.Element("Music");
            if (musicNode != null)
            {
                MusicNSF = FilePath.FromRelative(musicNode.Value, this.BaseDir);
            }

            XElement sfxNode = nsfNode.Element("SFX");
            if (sfxNode != null)
            {
                EffectsNSF = FilePath.FromRelative(sfxNode.Value, this.BaseDir);
            }
        }
    }
}
