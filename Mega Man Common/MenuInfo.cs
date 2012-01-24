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
                info.States.Add(MenuStateInfo.FromXml(keyNode));
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
        public List<IKeyFrameCommandInfo> Commands { get; private set; }

        public MenuStateInfo()
        {
            Commands = new List<IKeyFrameCommandInfo>();
        }

        public static MenuStateInfo FromXml(XElement node)
        {
            var info = new MenuStateInfo();

            info.Name = node.RequireAttribute("name").Value;

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

                    case "Option":
                        info.Commands.Add(MenuOptionCommandInfo.FromXml(cmdNode));
                        break;
                }
            }

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

    public class MenuOptionCommandInfo : IKeyFrameCommandInfo
    {
        public KeyFrameCommands Type { get { return KeyFrameCommands.Option; } }

        public int X { get; set; }
        public int Y { get; set; }

        public HandlerTransfer NextHandler { get; private set; }

        public static MenuOptionCommandInfo FromXml(XElement node)
        {
            var info = new MenuOptionCommandInfo();
            info.X = node.GetInteger("x");
            info.Y = node.GetInteger("y");

            var nextNode = node.Element("Next");
            if (nextNode != null)
            {
                info.NextHandler = HandlerTransfer.FromXml(nextNode);
            }

            return info;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Option");
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteEndElement();
        }
    }
}
