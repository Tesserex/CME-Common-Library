using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan.Common
{
    public class SceneInfo
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public bool CanSkip { get; set; }

        public Dictionary<string, Sprite> Sprites { get; private set; }
        public List<KeyFrameInfo> KeyFrames { get; private set; }
        public HandlerTransfer NextHandler { get; private set; }

        public SceneInfo()
        {
            Sprites = new Dictionary<string, Sprite>();
            KeyFrames = new List<KeyFrameInfo>();
        }

        public static SceneInfo FromXml(XElement node, string basePath)
        {
            var info = new SceneInfo();
            info.Name = node.RequireAttribute("name").Value;
            info.Duration = node.GetInteger("duration");

            bool canSkip = false;
            node.TryBool("canskip", out canSkip);
            info.CanSkip = canSkip;

            foreach (var spriteNode in node.Elements("Sprite"))
            {
                var sprite = Sprite.FromXml(spriteNode, basePath);
                info.Sprites.Add(sprite.Name, sprite);
            }

            foreach (var keyNode in node.Elements("Keyframe"))
            {
                info.KeyFrames.Add(KeyFrameInfo.FromXml(keyNode));
            }

            var transferNode = node.Element("Next");
            if (transferNode != null)
            {
                info.NextHandler = HandlerTransfer.FromXml(transferNode);
            }

            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Scene");

            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("duration", Duration.ToString());
            writer.WriteAttributeString("canskip", CanSkip.ToString());

            foreach (var sprite in Sprites.Values)
            {
                sprite.WriteTo(writer);
            }

            foreach (var keyframe in KeyFrames)
            {
                keyframe.Save(writer);
            }

            if (NextHandler != null)
            {
                NextHandler.Save(writer);
            }

            writer.WriteEndElement();
        }
    }

    public class KeyFrameInfo
    {
        public int Frame { get; set; }
        public bool Fade { get; set; }
        public List<IKeyFrameCommandInfo> Commands { get; private set; }

        public KeyFrameInfo()
        {
            Commands = new List<IKeyFrameCommandInfo>();
        }

        public static KeyFrameInfo FromXml(XElement node)
        {
            var info = new KeyFrameInfo();

            info.Frame = node.GetInteger("frame");

            bool fade = false;
            node.TryBool("fade", out fade);
            info.Fade = fade;

            foreach (var cmdNode in node.Elements())
            {
                switch (cmdNode.Name.LocalName)
                {
                    case "PlayMusic":
                        info.Commands.Add(KeyFramePlayCommandInfo.FromXml(cmdNode));
                        break;

                    case "Sprite":
                        info.Commands.Add(KeyFrameSpriteCommandInfo.FromXml(cmdNode));
                        break;

                    case "Remove":
                        info.Commands.Add(KeyFrameRemoveCommandInfo.FromXml(cmdNode));
                        break;

                    case "Entity":
                        info.Commands.Add(KeyFrameEntityCommandInfo.FromXml(cmdNode));
                        break;

                    case "Text":
                        info.Commands.Add(KeyFrameTextCommandInfo.FromXml(cmdNode));
                        break;

                    case "Fill":
                        info.Commands.Add(KeyFrameFillCommandInfo.FromXml(cmdNode));
                        break;

                    case "FillMove":
                        info.Commands.Add(KeyFrameFillMoveCommandInfo.FromXml(cmdNode));
                        break;
                }
            }

            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Keyframe");

            foreach (var command in Commands)
            {
                command.Save(writer);
            }

            writer.WriteEndElement();
        }
    }

    public enum KeyFrameCommands
    {
        PlayMusic,
        Sprite,
        Remove,
        Entity,
        Text,
        Fill,
        FillMove
    }

    public interface IKeyFrameCommandInfo
    {
        KeyFrameCommands Type { get; }
        void Save(XmlTextWriter writer);
    }

    public class KeyFramePlayCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.PlayMusic; } }
        public int Track { get; set; }

        public static KeyFramePlayCommandInfo FromXml(XElement node)
        {
            var info = new KeyFramePlayCommandInfo();
            info.Track = node.GetInteger("track");
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("PlayMusic");
            writer.WriteAttributeString("track", Track.ToString());
            writer.WriteEndElement();
        }
    }

    public class KeyFrameSpriteCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Sprite; } }
        public string Name { get; set; }
        public string Sprite { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public static KeyFrameSpriteCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameSpriteCommandInfo();
            var nameAttr = node.Attribute("name");
            if (nameAttr != null) info.Name = nameAttr.Value;
            info.Sprite = node.RequireAttribute("sprite").Value;
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("PlayMusic");
            if (!string.IsNullOrEmpty(Name)) writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("sprite", Sprite);
            writer.WriteEndElement();
        }
    }

    public class KeyFrameRemoveCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Remove; } }
        public string Name { get; set; }

        public static KeyFrameRemoveCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameRemoveCommandInfo();
            info.Name = node.RequireAttribute("name").Value;
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("PlayMusic");
            writer.WriteAttributeString("name", Name);
            writer.WriteEndElement();
        }
    }

    public class KeyFrameEntityCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Entity; } }
        public string Name { get; set; }
        public string Entity { get; set; }
        public string State { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public static KeyFrameEntityCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameEntityCommandInfo();
            info.Entity = node.RequireAttribute("entity").Value;
            var nameAttr = node.Attribute("name");
            if (nameAttr != null) info.Name = nameAttr.Value;
            var stateAttr = node.Attribute("state");
            if (stateAttr != null) info.State = stateAttr.Value;
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Entity");
            if (!string.IsNullOrEmpty(Name)) writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("entity", Entity);
            if (!string.IsNullOrEmpty(State)) writer.WriteAttributeString("state", State);
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteEndElement();
        }
    }

    public class KeyFrameTextCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Text; } }
        public string Name { get; set; }
        public string Content { get; set; }
        public int? Speed { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public static KeyFrameTextCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameTextCommandInfo();
            info.Content = node.RequireAttribute("content").Value;
            var nameAttr = node.Attribute("name");
            if (nameAttr != null) info.Name = nameAttr.Value;
            int speed;
            if (node.TryInteger("speed", out speed)) info.Speed = speed;
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Text");
            if (!string.IsNullOrEmpty(Name)) writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("content", Content);
            if (Speed != null) writer.WriteAttributeString("speed", Speed.Value.ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteEndElement();
        }
    }

    public class KeyFrameFillCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Fill; } }
        public string Name { get; set; }
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Layer { get; set; }

        public static KeyFrameFillCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameFillCommandInfo();
            var nameAttr = node.Attribute("name");
            if (nameAttr != null) info.Name = nameAttr.Value;
            var colorAttr = node.RequireAttribute("color");
            var color = colorAttr.Value;
            var split = color.Split(',');
            info.Red = byte.Parse(split[0]);
            info.Green = byte.Parse(split[1]);
            info.Blue = byte.Parse(split[2]);
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");
            info.Width = node.GetInteger("width");
            info.Height = node.GetInteger("height");
            int layer = 1;
            if (node.TryInteger("layer", out layer)) info.Layer = layer;
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Fill");
            if (!string.IsNullOrEmpty(Name)) writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("color", Red.ToString() + "," + Green.ToString() + "," + Blue.ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());
            writer.WriteAttributeString("layer", Layer.ToString());
            writer.WriteEndElement();
        }
    }

    public class KeyFrameFillMoveCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.FillMove; } }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Duration { get; set; }

        public static KeyFrameFillMoveCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameFillMoveCommandInfo();
            info.Name = node.RequireAttribute("name").Value;
            info.Duration = node.GetInteger("duration");
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");
            info.Width = node.GetInteger("width");
            info.Height = node.GetInteger("height");
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Fill");
            if (!string.IsNullOrEmpty(Name)) writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());
            writer.WriteAttributeString("duration", Duration.ToString());
            writer.WriteEndElement();
        }
    }
}
