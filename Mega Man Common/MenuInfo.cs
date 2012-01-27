using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan.Common
{
    public class MenuInfo
    {
        public string Name { get; set; }
        public SoundInfo ChangeSound { get; set; }

        public Dictionary<string, Sprite> Sprites { get; private set; }
        public List<MenuStateInfo> States { get; private set; }

        public MenuInfo()
        {
            Sprites = new Dictionary<string, Sprite>();
            States = new List<MenuStateInfo>();
        }

        public static MenuInfo FromXml(XElement node, string basePath)
        {
            var info = new MenuInfo();
            info.Name = node.RequireAttribute("name").Value;

            foreach (var spriteNode in node.Elements("Sprite"))
            {
                var sprite = Sprite.FromXml(spriteNode, basePath);
                info.Sprites.Add(sprite.Name, sprite);
            }

            foreach (var keyNode in node.Elements("State"))
            {
                info.States.Add(MenuStateInfo.FromXml(keyNode, basePath));
            }

            XElement soundNode = node.Element("Sound");
            if (soundNode != null)
            {
                info.ChangeSound = SoundInfo.FromXml(soundNode, basePath);
            }

            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Menu");

            writer.WriteAttributeString("name", Name);

            foreach (var sprite in Sprites.Values)
            {
                sprite.WriteTo(writer);
            }

            if (ChangeSound != null)
            {
                ChangeSound.Save(writer);
            }

            foreach (var state in States)
            {
                state.Save(writer);
            }

            writer.WriteEndElement();
        }
    }

    public class MenuStateInfo
    {
        public string Name { get; set; }
        public bool Fade { get; set; }
        public List<SceneCommandInfo> Commands { get; private set; }

        public static MenuStateInfo FromXml(XElement node, string basePath)
        {
            var info = new MenuStateInfo();

            info.Name = node.RequireAttribute("name").Value;

            bool fade = false;
            node.TryBool("fade", out fade);
            info.Fade = fade;

            info.Commands = SceneCommandInfo.Load(node, basePath);

            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("State");
            writer.WriteAttributeString("name", Name);

            foreach (var command in Commands)
            {
                command.Save(writer);
            }

            writer.WriteEndElement();
        }
    }

    
}
