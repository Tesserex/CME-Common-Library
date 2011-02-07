using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Xml.Linq;
using System.IO;

namespace MegaMan
{
    public enum JoinType : int {
        Horizontal = 1,
        Vertical = 2
    }

    public enum JoinDirection : int {
        Both = 1,
        // <summary>
        // The player can only cross the join left to right, or top to bottom
        // </summary>
        ForwardOnly = 2,
        // <summary>
        // The player can only cross the join right to left, or bottom to top
        // </summary>
        BackwardOnly = 3
    }

    public class Join {
        public JoinType type;
        public string screenOne, screenTwo;
        // <summary>
        // The number of tiles from the top (if vertical) or left (if horizontal) of screenOne at which the join begins.
        // </summary>
        public int offsetOne;
        // <summary>
        // The number of tiles from the top (if vertical) or left (if horizontal) of screenTwo at which the join begins.
        // </summary>
        public int offsetTwo;
        // <summary>
        // The size extent of the join, in tiles.
        // </summary>
        public int Size;
        // <summary>
        // Whether the join allows the player to cross only one way or in either direction.
        // </summary>
        public JoinDirection direction;
        // <summary>
        // Whether this join has a boss-style door over it.
        // </summary>
        public bool bossDoor;
        public string bossEntityName;
    }

    public class Map {
        private Dictionary<string, Point> continuePoints;
        public IDictionary<string, Point> ContinuePoints { get { return continuePoints; } }

        private string name;
        private FilePath path;
        private FilePath musicIntroPath, musicLoopPath;
        private FilePath tilePath;

        #region Properties
        public Dictionary<string, Screen> Screens { get; private set; }
        public List<Join> Joins { get; private set; }
        public string StartScreen { get; set; }
        public int PlayerStartX { get; set; }
        public int PlayerStartY { get; set; }

        public Tileset Tileset { get; private set; }

        public string Name { get { return name; } set { name = value; Dirty = true; } }

        /// <summary>
        /// Gets or sets the absolute file path to the directory where this stage is stored
        /// </summary>
        public FilePath StagePath
        {
            get { return path; }
            set {
                path = value;
                Dirty = true;
            }
        }

        public FilePath MusicIntroPath { get { return musicIntroPath; } set { musicIntroPath = value; Dirty = true; } }
        public FilePath MusicLoopPath { get { return musicLoopPath; } set { musicLoopPath = value; Dirty = true; } }
        public int MusicNsfTrack { get; private set; }
        
        private bool dirty;
        public bool Dirty
        {
            get
            {
                if (dirty == true) 
                    return true;

                foreach (Screen screen in Screens.Values)
                    if (screen.Dirty) 
                        return true;

                return false;
            }
            set
            {
                if (dirty == value) return;
                dirty = value;
                if (DirtyChanged != null) DirtyChanged(dirty);
            }
        }
        #endregion Properties

        public event Action<bool> DirtyChanged;

        public Map() 
        {
            Screens = new Dictionary<string, Screen>();
            Joins = new List<Join>();
            continuePoints = new Dictionary<string, Point>();
        }

        public Map(FilePath path) : this() 
        {
            LoadMapXml(path);
        }

        public void LoadMapXml(FilePath path) 
        {
            StagePath = path;

            var mapXml = XElement.Load(Path.Combine(StagePath.Absolute, "map.xml"));
            Name = Path.GetFileNameWithoutExtension(StagePath.Absolute);

            string tilePathRel = mapXml.Attribute("tiles").Value;
            tilePath = FilePath.FromRelative(tilePathRel, StagePath.Absolute);

            Tileset = new Tileset(tilePath.Absolute);

            PlayerStartX = 3;
            PlayerStartY = 3;

            LoadMusicXml(mapXml);
            LoadScreenXml(mapXml);

            XElement start = mapXml.Element("Start");
            if (start != null) 
            {
                StartScreen = start.Attribute("screen").Value;
                PlayerStartX = Int32.Parse(start.Attribute("x").Value);
                PlayerStartY = Int32.Parse(start.Attribute("y").Value);
            }

            foreach (XElement contPoint in mapXml.Elements("Continue")) 
            {
                string screen = contPoint.Attribute("screen").Value;
                int x = int.Parse(contPoint.Attribute("x").Value);
                int y = int.Parse(contPoint.Attribute("y").Value);
                continuePoints.Add(screen, new Point(x, y));
            }

            foreach (XElement join in mapXml.Elements("Join")) 
            {
                string t = join.Attribute("type").Value;
                JoinType type;
                if (t.ToLower() == "horizontal") type = JoinType.Horizontal;
                else if (t.ToLower() == "vertical") type = JoinType.Vertical;
                else throw new Exception("map.xml file contains invalid join type.");

                string s1 = join.Attribute("s1").Value;
                string s2 = join.Attribute("s2").Value;
                int offset1 = Int32.Parse(join.Attribute("offset1").Value);
                int offset2 = Int32.Parse(join.Attribute("offset2").Value);
                int size = Int32.Parse(join.Attribute("size").Value);

                JoinDirection direction;
                XAttribute dirAttr = join.Attribute("direction");
                if (dirAttr == null || dirAttr.Value.ToUpper() == "BOTH") direction = JoinDirection.Both;
                else if (dirAttr.Value.ToUpper() == "FORWARD") direction = JoinDirection.ForwardOnly;
                else if (dirAttr.Value.ToUpper() == "BACKWARD") direction = JoinDirection.BackwardOnly;
                else throw new Exception("map.xml file contains invalid join direction.");

                string bosstile = null;
                XAttribute bossAttr = join.Attribute("bossdoor");
                bool bossdoor = (bossAttr != null);
                if (bossdoor) bosstile = bossAttr.Value;

                Join j = new Join();
                j.direction = direction;
                j.screenOne = s1;
                j.screenTwo = s2;
                j.offsetOne = offset1;
                j.offsetTwo = offset2;
                j.type = type;
                j.Size = size;
                j.bossDoor = bossdoor;
                j.bossEntityName = bosstile;

                Joins.Add(j);
            }

            Dirty = false;
        }

        /* *
         * LoadMusicXml - Load xml data for music
         * */
        public void LoadMusicXml(XElement mapXml) 
        {
            var music = mapXml.Element("Music");
            if (music != null) 
            {
                var intro = music.Element("Intro");
                var loop = music.Element("Loop");
                MusicIntroPath = (intro != null) ? FilePath.FromRelative(intro.Value, StagePath.BasePath) : null;
                MusicLoopPath = (loop != null) ? FilePath.FromRelative(loop.Value, StagePath.BasePath) : null;

                XAttribute nsfAttr = music.Attribute("nsftrack");
                if (nsfAttr != null)
                {
                    MusicNsfTrack = int.Parse(nsfAttr.Value);
                }
            }
        }

        /* *
         * LoadScreenXml - Load xml data for screens
         * */
        public void LoadScreenXml(XElement mapXml) 
        {
            foreach (XElement screen in mapXml.Elements("Screen"))
            {
                string id = screen.Attribute("id").Value;
                Screen s = new Screen(Path.Combine(StagePath.Absolute, id + ".scn"), this);
                Screens.Add(id, s);
                if (Screens.Count == 1)
                {
                    StartScreen = StartScreen ?? id;
                }
                foreach (XElement entity in screen.Elements("Entity"))
                {
                    string enemyname = entity.Attribute("name").Value;
                    string state = "Start";
                    XAttribute stateAttr = entity.Attribute("state");
                    if (stateAttr != null) state = stateAttr.Value;
                    int enemyX = Int32.Parse(entity.Attribute("x").Value);
                    int enemyY = Int32.Parse(entity.Attribute("y").Value);
                    EnemyCopyInfo info = new EnemyCopyInfo();
                    info.enemy = enemyname;
                    info.state = state;
                    info.screenX = enemyX;
                    info.screenY = enemyY;
                    info.boss = false;
                    XAttribute palAttr = entity.Attribute("pallete");
                    if (palAttr != null) info.pallete = palAttr.Value;
                    else info.pallete = "Default";
                    s.AddEnemy(info);
                }
                foreach (XElement teleport in screen.Elements("Teleport"))
                {
                    TeleportInfo info;
                    info.From = new Point(int.Parse(teleport.Attribute("from_x").Value), int.Parse(teleport.Attribute("from_y").Value));
                    info.To = new Point(int.Parse(teleport.Attribute("to_x").Value), int.Parse(teleport.Attribute("to_y").Value));
                    info.TargetScreen = teleport.Attribute("to_screen").Value;
                    s.AddTeleport(info);
                }
                foreach (XElement blocks in screen.Elements("Blocks"))
                {
                    BlockPatternInfo pattern = new BlockPatternInfo(blocks);
                    s.AddBlockPattern(pattern);
                }

                XElement screenmusic = screen.Element("Music");
                if (screenmusic != null)
                {
                    XElement intro = screenmusic.Element("Intro");
                    XElement loop = screenmusic.Element("Loop");
                    s.MusicIntroPath = (intro != null) ? FilePath.FromRelative(intro.Value, StagePath.BasePath) : null;
                    s.MusicLoopPath = (loop != null) ? FilePath.FromRelative(loop.Value, StagePath.BasePath) : null;

                    XAttribute nsfAttr = screenmusic.Attribute("nsftrack");
                    if (nsfAttr != null)
                    {
                        s.MusicNsfTrack = int.Parse(nsfAttr.Value);
                    }
                }

                foreach (XElement entity in screen.Elements("Boss"))
                {
                    string enemyname = entity.Attribute("name").Value;
                    string state = "Start";
                    XAttribute stateAttr = entity.Attribute("state");
                    if (stateAttr != null) state = stateAttr.Value;
                    int enemyX = Int32.Parse(entity.Attribute("x").Value);
                    int enemyY = Int32.Parse(entity.Attribute("y").Value);
                    EnemyCopyInfo info = new EnemyCopyInfo();
                    info.enemy = enemyname;
                    info.state = state;
                    info.screenX = enemyX;
                    info.screenY = enemyY;
                    info.boss = true;
                    s.AddEnemy(info);
                }

                s.Dirty = false;
            }
        }

        public void RenameScreen(Screen screen, string name)
        {
            this.Screens.Remove(screen.Name);
            screen.Name = name;
            this.Screens.Add(name, screen);
        }

        public void RenameScreen(string oldName, string newName)
        {
            RenameScreen(this.Screens[oldName], newName);
        }

        /// <summary>
        /// Changes the tileset by specifying an absolute path to the new tileset XML file.
        /// </summary>
        /// <param name="path">If it's not absolute, I'll make it so.</param>
        public void ChangeTileset(string path)
        {
            tilePath = FilePath.FromAbsolute(path, StagePath.Absolute);
            Tileset = new Tileset(tilePath.Absolute);
            
            foreach (Screen s in Screens.Values) s.Tileset = Tileset;
        }

        public void Clear() 
        {
            Screens.Clear();
            Joins.Clear();
            Tileset = null;
            Dirty = true;
        }

        public void Save() { if (StagePath != null) Save(StagePath.Absolute); }

        /// <summary>
        /// Saves this stage to the specified directory.
        /// </summary>
        /// <param name="directory">An absolute path to the directory to save to.</param>
        public void Save(string directory)
        {
            StagePath = FilePath.FromAbsolute(directory, StagePath.BasePath);
            this.Name = Path.GetFileNameWithoutExtension(directory);

            XmlTextWriter writer = new XmlTextWriter(Path.Combine(StagePath.Absolute, "map.xml"), null);
            writer.Formatting = Formatting.Indented;
            writer.IndentChar = '\t';
            writer.Indentation = 1;

            writer.WriteStartElement("Map");
            writer.WriteAttributeString("name", Name);

            writer.WriteAttributeString("tiles", tilePath.Relative);

            if (this.MusicIntroPath != null || this.MusicLoopPath != null || this.MusicNsfTrack > 0)
            {
                writer.WriteStartElement("Music");
                if (MusicNsfTrack > 0) writer.WriteAttributeString("nsftrack", MusicNsfTrack.ToString());
                if (MusicIntroPath != null && !string.IsNullOrEmpty(MusicIntroPath.Relative)) writer.WriteElementString("Intro", MusicIntroPath.Relative);
                if (MusicLoopPath != null && !string.IsNullOrEmpty(MusicLoopPath.Relative)) writer.WriteElementString("Loop", MusicLoopPath.Relative);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("Start");
            writer.WriteAttributeString("screen", StartScreen);
            writer.WriteAttributeString("x", PlayerStartX.ToString());
            writer.WriteAttributeString("y", PlayerStartY.ToString());
            writer.WriteEndElement();

            foreach (KeyValuePair<string, Point> pair in continuePoints)
            {
                writer.WriteStartElement("Continue");
                writer.WriteAttributeString("screen", pair.Key);
                writer.WriteAttributeString("x", pair.Value.X.ToString());
                writer.WriteAttributeString("y", pair.Value.Y.ToString());
                writer.WriteEndElement();
            }

            foreach (string id in Screens.Keys)
            {
                writer.WriteStartElement("Screen");
                writer.WriteAttributeString("id", id);

                if (Screens[id].MusicIntroPath != null || Screens[id].MusicLoopPath != null || Screens[id].MusicNsfTrack > 0)
                {
                    writer.WriteStartElement("Music");
                    if (Screens[id].MusicNsfTrack > 0) writer.WriteAttributeString("nsftrack", Screens[id].MusicNsfTrack.ToString());
                    if (Screens[id].MusicIntroPath != null && !string.IsNullOrEmpty(Screens[id].MusicIntroPath.Relative)) writer.WriteElementString("Intro", Screens[id].MusicIntroPath.Relative);
                    if (Screens[id].MusicLoopPath != null && !string.IsNullOrEmpty(Screens[id].MusicLoopPath.Relative)) writer.WriteElementString("Loop", Screens[id].MusicLoopPath.Relative);
                    writer.WriteEndElement();
                }

                foreach (EnemyCopyInfo info in Screens[id].EnemyInfo)
                {
                    writer.WriteStartElement("Entity");
                    writer.WriteAttributeString("name", info.enemy);
                    if (info.state != "Start") writer.WriteAttributeString("state", info.state);
                    writer.WriteAttributeString("x", info.screenX.ToString());
                    writer.WriteAttributeString("y", info.screenY.ToString());
                    writer.WriteEndElement();
                }

                foreach (BlockPatternInfo pattern in Screens[id].BlockPatternInfo)
                {
                    writer.WriteStartElement("Blocks");
                    writer.WriteAttributeString("left", pattern.LeftBoundary.ToString());
                    writer.WriteAttributeString("right", pattern.RightBoundary.ToString());
                    writer.WriteAttributeString("length", pattern.Length.ToString());
                    writer.WriteAttributeString("entity", pattern.Entity);

                    foreach (BlockPatternInfo.BlockInfo block in pattern.Blocks)
                    {
                        writer.WriteStartElement("Block");
                        writer.WriteAttributeString("x", block.pos.X.ToString());
                        writer.WriteAttributeString("y", block.pos.Y.ToString());
                        writer.WriteAttributeString("on", block.on.ToString());
                        writer.WriteAttributeString("off", block.off.ToString());
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                foreach (TeleportInfo teleport in Screens[id].Teleports) 
                {
                    writer.WriteStartElement("Teleport");
                    writer.WriteAttributeString("from_x", teleport.From.X.ToString());
                    writer.WriteAttributeString("from_y", teleport.From.Y.ToString());
                    writer.WriteAttributeString("screen", teleport.TargetScreen);
                    writer.WriteAttributeString("to_x", teleport.To.X.ToString());
                    writer.WriteAttributeString("to_y", teleport.To.Y.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            foreach (Join join in Joins)
            {
                writer.WriteStartElement("Join");
                writer.WriteAttributeString("type", (join.type == JoinType.Horizontal) ? "horizontal" : "vertical");

                writer.WriteAttributeString("s1", join.screenOne);
                writer.WriteAttributeString("s2", join.screenTwo);
                writer.WriteAttributeString("offset1", join.offsetOne.ToString());
                writer.WriteAttributeString("offset2", join.offsetTwo.ToString());
                writer.WriteAttributeString("size", join.Size.ToString());
                switch (join.direction)
                {
                    case JoinDirection.Both: writer.WriteAttributeString("direction", "both"); break;
                    case JoinDirection.ForwardOnly: writer.WriteAttributeString("direction", "forward"); break;
                    case JoinDirection.BackwardOnly: writer.WriteAttributeString("direction", "backward"); break;
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Close();

            foreach (string i in Screens.Keys) {
                Screens[i].Save(directory + "\\" + i.ToString() + ".scn");
            }

            Dirty = false;
        }

        // this doesn't work for files on different drives
        // also right now relativeTo should not have a trailing slash.
        internal static string PathToRelative(string path, string relativeTo)
        {
            if (System.IO.Path.HasExtension(relativeTo))
            {
                relativeTo = System.IO.Path.GetDirectoryName(relativeTo);
            }
            path = System.IO.Path.GetFullPath(path);

            // split into directories
            string[] pathdirs = path.Split(System.IO.Path.DirectorySeparatorChar);
            string[] reldirs = relativeTo.Split(System.IO.Path.DirectorySeparatorChar);

            int length = Math.Min(pathdirs.Length, reldirs.Length);
            StringBuilder relativePath = new StringBuilder();

            // find where the paths differ
            int forkpoint = 0;
            while (forkpoint < length && pathdirs[forkpoint] == reldirs[forkpoint]) forkpoint++;

            // go back by the number of directories in the relativeTo path
            int dirs = reldirs.Length - forkpoint;
            for (int i = 0; i < dirs; i++) relativePath.Append("..").Append(System.IO.Path.DirectorySeparatorChar);

            // append file path from that directory
            for (int i = forkpoint; i < pathdirs.Length - 1; i++) relativePath.Append(pathdirs[i]).Append(System.IO.Path.DirectorySeparatorChar);
            // append file, without directory separator
            relativePath.Append(pathdirs[pathdirs.Length - 1]);

            return relativePath.ToString();
        }
    }
}
