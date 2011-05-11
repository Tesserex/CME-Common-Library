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
    public class StageInfo
    {
        public string Name { get; set; }
        public FilePath StagePath { get; set; }
    }

    public class Project
    {
        #region Game XML File Stuff

        private List<StageInfo> stages = new List<StageInfo>();
        
        private List<string> includeFiles = new List<string>();

        public IEnumerable<StageInfo> Stages
        {
            get { return stages; }
        }

        public void AddStage(StageInfo stage)
        {
            this.stages.Add(stage);
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

        public FilePath MusicNSF { get; set; }
        public FilePath EffectsNSF { get; set; }

        public StageSelect StageSelect { get; set; }

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
                this.StageSelect = new StageSelect(stageSelectNode, this.BaseDir);
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

            if (this.StageSelect != null) this.StageSelect.Save(writer);

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
