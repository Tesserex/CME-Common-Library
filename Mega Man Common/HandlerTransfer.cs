using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan.Common
{
    public enum HandlerType
    {
        Stage,
        Scene,
        StageSelect
    }

    public class HandlerTransfer
    {
        public HandlerType Type;
        public string Name;

        public static HandlerTransfer FromXml(XElement node)
        {
            HandlerTransfer transfer = new HandlerTransfer();

            switch (node.RequireAttribute("type").Value.ToLower())
            {
                case "stage":
                    transfer.Type = HandlerType.Stage;
                    break;

                case "stageselect":
                    transfer.Type = HandlerType.StageSelect;
                    break;

                case "scene":
                    transfer.Type = HandlerType.Scene;
                    break;
            }

            transfer.Name = node.RequireAttribute("name").Value;

            return transfer;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Next");

            writer.WriteAttributeString("type", Enum.GetName(typeof(HandlerType), Type));
            writer.WriteAttributeString("name", Name);

            writer.WriteEndElement();
        }
    }
}
