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

        public Dictionary<string, Sprite> Sprites { get; private set; }
        public List<KeyFrameInfo> KeyFrames { get; private set; }

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

            foreach (var spriteNode in node.Elements("Sprite"))
            {
                var sprite = Sprite.FromXml(spriteNode, basePath);
                info.Sprites.Add(sprite.Name, sprite);
            }

            foreach (var keyNode in node.Elements("Keyframe"))
            {
                info.KeyFrames.Add(KeyFrameInfo.FromXml(keyNode));
            }

            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Scene");

            foreach (var sprite in Sprites.Values)
            {
                sprite.WriteTo(writer);
            }

            foreach (var keyframe in KeyFrames)
            {
                keyframe.Save(writer);
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

                    case "Add":
                        info.Commands.Add(KeyFrameAddCommandInfo.FromXml(cmdNode));
                        break;

                    case "Remove":
                        info.Commands.Add(KeyFrameRemoveCommandInfo.FromXml(cmdNode));
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
        Add,
        Remove
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

    public class KeyFrameAddCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Add; } }
        public string Name { get; set; }
        public string Sprite { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public static KeyFrameAddCommandInfo FromXml(XElement node)
        {
            var info = new KeyFrameAddCommandInfo();
            info.Name = node.RequireAttribute("name").Value;
            info.Sprite = node.RequireAttribute("sprite").Value;
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");
            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("PlayMusic");
            writer.WriteAttributeString("name", Name);
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
}
