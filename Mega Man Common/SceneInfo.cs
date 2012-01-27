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
                info.KeyFrames.Add(KeyFrameInfo.FromXml(keyNode, basePath));
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
        public List<SceneCommandInfo> Commands { get; private set; }

        public static KeyFrameInfo FromXml(XElement node, string basePath)
        {
            var info = new KeyFrameInfo();

            info.Frame = node.GetInteger("frame");

            bool fade = false;
            node.TryBool("fade", out fade);
            info.Fade = fade;

            info.Commands = SceneCommandInfo.Load(node, basePath);

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
}
