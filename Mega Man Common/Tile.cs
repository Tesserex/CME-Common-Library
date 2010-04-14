using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan
{
    public class TileProperties
    {
        public string Name { get; private set; }
        public bool Blocking { get; private set; }
        public bool Climbable { get; private set; }
        public bool Lethal { get; private set; }
        public float PushMultX { get; private set; }
        public float PushMultY { get; private set; }
        public float PushConstX { get; private set; }
        public float PushConstY { get; private set; }
        public float ResistMultX { get; private set; }
        public float ResistMultY { get; private set; }
        public float ResistConstX { get; private set; }
        public float ResistConstY { get; private set; }
        public float GravityMult { get; private set; }

        private static TileProperties def = new TileProperties();
        public static TileProperties Default { get { return def; } }
        private TileProperties()
        {
            this.Name = "Default";
            this.PushMultX = 1;
            this.PushMultY = 1;
            this.ResistMultX = 1;
            this.ResistMultY = 1;
            this.GravityMult = 1;
        }

        public TileProperties(XElement xmlNode) : this()
        {
            this.Name = "Default";
            foreach (XAttribute attr in xmlNode.Attributes())
            {
                bool b;
                float f;
                switch (attr.Name.LocalName)
                {
                    case "name":
                        this.Name = attr.Value;
                        break;

                    case "blocking":
                        if (!bool.TryParse(attr.Value, out b)) throw new Exception("Tile property blocking attribute was not a valid bool.");
                        Blocking = b;
                        break;

                    case "climbable":
                        if (!bool.TryParse(attr.Value, out b)) throw new Exception("Tile property climbable attribute was not a valid bool.");
                        Climbable = b;
                        break;

                    case "lethal":
                        if (!bool.TryParse(attr.Value, out b)) throw new Exception("Tile property lethal attribute was not a valid bool.");
                        Lethal = b;
                        break;

                    case "pushmultX":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property pushmultX attribute was not a valid number.");
                        PushMultX = f;
                        break;

                    case "pushmultY":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property pushmultY attribute was not a valid number.");
                        PushMultY = f;
                        break;

                    case "pushconstX":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property pushconstX attribute was not a valid number.");
                        PushConstX = f;
                        break;

                    case "pushconstY":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property pushconstY attribute was not a valid number.");
                        PushConstY = f;
                        break;

                    case "resistmultX":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property resistmultX attribute was not a valid number.");
                        ResistMultX = f;
                        break;

                    case "resistmultY":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property resistmultY attribute was not a valid number.");
                        ResistMultY = f;
                        break;

                    case "resistconstX":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property resistconstX attribute was not a valid number.");
                        ResistConstX = f;
                        break;

                    case "resistconstY":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property resistconstY attribute was not a valid number.");
                        ResistConstY = f;
                        break;

                    case "gravitymult":
                        if (!float.TryParse(attr.Value, out f)) throw new Exception("Tile property gravitymult attribute was not a valid number.");
                        GravityMult = f;
                        break;
                }
            }
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Properties");
            writer.WriteAttributeString("name", this.Name);
            if (this.Blocking) writer.WriteAttributeString("blocking", "true");
            if (this.Climbable) writer.WriteAttributeString("climbable", "true");
            if (this.Lethal) writer.WriteAttributeString("lethal", "true");
            if (this.GravityMult != 1) writer.WriteAttributeString("gravitymult", this.GravityMult.ToString());
            if (this.PushMultX != 1) writer.WriteAttributeString("pushmultX", this.PushMultX.ToString());
            if (this.PushMultY != 1) writer.WriteAttributeString("pushmultY", this.PushMultY.ToString());
            if (this.PushConstX != 0) writer.WriteAttributeString("pushconstX", this.PushConstX.ToString());
            if (this.PushConstY != 0) writer.WriteAttributeString("pushconstY", this.PushConstY.ToString());
            if (this.ResistMultX != 1) writer.WriteAttributeString("resistmultX", this.ResistMultX.ToString());
            if (this.ResistMultY != 1) writer.WriteAttributeString("resistmultY", this.ResistMultY.ToString());
            if (this.ResistConstX != 0) writer.WriteAttributeString("resistconstX", this.ResistConstX.ToString());
            if (this.ResistConstY != 0) writer.WriteAttributeString("resistconstY", this.ResistConstY.ToString());
            writer.WriteEndElement();
        }
    }

    public class Tile
    {
        public int Id { get; private set; }
        public string Name { get; set; }
        public Sprite Sprite { get; protected set; }
        public float Width { get { return Sprite.Width; } }
        public float Height { get { return Sprite.Height; } }
        public TileProperties Properties { get; set; }

        public Tile(int id, Sprite sprite)
        {
            Id = id;
            Sprite = sprite;
            if (Sprite.Count == 0) Sprite.AddFrame();
            Properties = TileProperties.Default;
        }

        public void Draw(Graphics g, float posX, float posY)
        {
            if (Sprite != null) Sprite.Draw(g, (int)posX, (int)posY);
        }

        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch, Microsoft.Xna.Framework.Graphics.Color color, float posX, float posY)
        {
            if (Sprite != null) Sprite.DrawXna(batch, color, (int)posX, (int)posY);
        }
    }
}
