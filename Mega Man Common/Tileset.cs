using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace MegaMan
{
    public class Tileset : List<Tile>
    {
        private Dictionary<string, TileProperties> properties;
        public IDictionary<string, TileProperties> Properties { get { return properties; } }

        public Image Sheet { get; private set; }
        public string SheetPath { get; set; }
        public string FilePath { get; set; }

        public int TileSize { get; private set; }

        public event Action TileAdded;

        // ************
        // Constructors
        // ************

        // Creates a new Tileset from the given image with tiles of the specified size.
        public Tileset(Image sheet, int tilesize)
        {
            this.TileSize = tilesize;
            this.Sheet = sheet;
            this.properties = new Dictionary<string, TileProperties>();
        }

        // Load the tileset xml along with the tileset images.
        // rootPath should be absolute. tilesetPath should be relative to rootPath
        // Ex: rootPath = "C:\MegaMan\MyCoolGame\", tilesetPath = "level1\level1.xml"
        // Will load: "C:\MegaMan\MyCoolGame\level1\level1.xml" as the full path
        public Tileset(string rootPath, string tilesetPath) 
        {
            LoadTilesetXml(rootPath, tilesetPath);
        }

        // Use this to specify the tileset path, assuming root path is current directory
        public Tileset(string path) 
        {
            LoadTilesetXml(Directory.GetCurrentDirectory(), path);
        }


        // LoadTilesetXml - Load the tileset xml along with the tileset images.
        // rootPath should be absolute. 
        // tilesetPath should be relative to rootPath
        // 
        // Ex: 
        //      rootPath = "C:\MegaMan\MyCoolGame\"
        //      tilesetPath = "level1\level1.xml"
        //      
        // Will load: "C:\MegaMan\MyCoolGame\level1\level1.xml" as the full path
        public void LoadTilesetXml(string rootPath, string tilesetPath) 
        {
            this.properties = new Dictionary<string, TileProperties>();

            FilePath = Path.Combine(rootPath, tilesetPath);

            var doc = XDocument.Load(FilePath);
            var reader = doc.Element("Tileset");
            if (reader == null)
                throw new Exception("The specified tileset definition file does not contain a Tileset tag.");

            var stageDirectory = Directory.GetParent(tilesetPath).Name;
            var tilesheetPath = Path.Combine(Path.Combine(Path.Combine(rootPath, "stages"), stageDirectory), reader.Attribute("tilesheet").Value);
            SheetPath = Path.Combine(rootPath, tilesheetPath);

            try 
            {
                Sheet = Bitmap.FromFile(SheetPath);
            } 
            catch (FileNotFoundException err) 
            {
                throw new Exception("A tile image file was not found at the location specified by the tileset definition: " + SheetPath, err);
            }

            int size;
            if (!int.TryParse(reader.Attribute("tilesize").Value, out size)) 
                throw new Exception("The tileset definition does not contain a valid tilesize attribute.");
            TileSize = size;

            this.properties["Default"] = TileProperties.Default;
            var propParent = reader.Element("TileProperties");
            if (propParent != null) {
                foreach (XElement propNode in propParent.Elements("Properties")) {
                    var prop = new TileProperties(propNode);
                    this.properties[prop.Name] = prop;
                }
            }

            LoadTilesFromXml(reader);
        }

        public void LoadTilesFromXml(XElement reader) 
        {
            foreach (XElement tileNode in reader.Elements("Tile")) 
            {
                int id = int.Parse(tileNode.Attribute("id").Value);
                string name = tileNode.Attribute("name").Value;
                var sprite = Sprite.Empty;

                var spriteNode = tileNode.Element("Sprite");
                if (spriteNode != null) 
                    sprite = Sprite.FromXml(spriteNode, Sheet);

                Tile tile = new Tile(id, sprite);

                string propName = "Default";
                XAttribute propAttr = tileNode.Attribute("properties");
                if (propAttr != null) 
                    if (this.properties.ContainsKey(propAttr.Value)) 
                        propName = propAttr.Value;

                tile.Properties = this.properties[propName];

                tile.Sprite.Play();
                base.Add(tile);
            }
        }


        public void SetTextures(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            Microsoft.Xna.Framework.Graphics.Texture2D tex = Microsoft.Xna.Framework.Graphics.Texture2D.FromFile(device, this.SheetPath);
            foreach (Tile tile in this)
            {
                tile.Sprite.SetTexture(tex);
            }
        }

        // Do not use! Use AddTile instead!
        public new void Add(Tile tile) 
        { 
            throw new NotSupportedException("Don't use this function!"); 
        }

        public void AddTile()
        {
            Sprite sprite = new Sprite(this.TileSize, this.TileSize);
            sprite.sheet = this.Sheet;
            base.Add(new Tile(this.Count, sprite));
            if (TileAdded != null) TileAdded();
        }

        public void Save()
        {
            if (FilePath != null) 
                Save(FilePath);
        }

        public void Save(string path)
        {
            XmlTextWriter writer = new XmlTextWriter(path, null);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 1;
            writer.IndentChar = '\t';

            writer.WriteStartElement("Tileset");
            writer.WriteAttributeString("tilesheet", SheetPath);
            writer.WriteAttributeString("tilesize", TileSize.ToString());

            writer.WriteStartElement("TileProperties");
            foreach (TileProperties properties in this.properties.Values)
            {
                if (properties.Name == "Default" && properties == TileProperties.Default) 
                    continue;
                properties.Save(writer);
            }
            writer.WriteEndElement();

            foreach (Tile tile in this)
            {
                writer.WriteStartElement("Tile");
                writer.WriteAttributeString("id", tile.Id.ToString());
                writer.WriteAttributeString("name", tile.Name);
                writer.WriteAttributeString("properties", tile.Properties.Name);

                writer.WriteStartElement("Sprite");
                writer.WriteAttributeString("width", TileSize.ToString());
                writer.WriteAttributeString("height", TileSize.ToString());

                writer.WriteStartElement("Hotspot");
                writer.WriteAttributeString("x", "0");
                writer.WriteAttributeString("y", "0");
                writer.WriteEndElement();

                foreach (SpriteFrame frame in tile.Sprite)
                {
                    writer.WriteStartElement("Frame");
                    writer.WriteAttributeString("x", frame.SheetLocation.X.ToString());
                    writer.WriteAttributeString("y", frame.SheetLocation.Y.ToString());
                    writer.WriteAttributeString("duration", frame.Duration.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();   // end Sprite

                writer.WriteEndElement();   // end Tile
            }
            writer.WriteEndElement();

            writer.Close();
        }
    }
}
