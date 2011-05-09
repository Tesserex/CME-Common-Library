using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

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

        private void Load(string path)
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
    }
}
