using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Drawing;

namespace MegaMan
{
    public class MeterInfo
    {
        public enum Orientation : byte
        {
            Horizontal,
            Vertical
        }

        public PointF Position { get; set; }
        public FilePath Background { get; set; }
        public FilePath TickImage { get; set; }
        public Orientation Orient { get; set; }
        public Point TickOffset { get; set; }
        public SoundInfo Sound { get; set; }

        public static MeterInfo FromXml(XElement meterNode, string basePath)
        {
            MeterInfo meter = new MeterInfo();
            meter.Position = new PointF(meterNode.GetFloat("x"), meterNode.GetFloat("y"));

            XAttribute imageAttr = meterNode.RequireAttribute("image");
            meter.TickImage = FilePath.FromRelative(imageAttr.Value, basePath);

            XAttribute backAttr = meterNode.Attribute("background");
            if (backAttr != null)
            {
                meter.Background = FilePath.FromRelative(backAttr.Value, basePath);
            }

            bool horiz = false;
            XAttribute dirAttr = meterNode.Attribute("orientation");
            if (dirAttr != null)
            {
                horiz = (dirAttr.Value == "horizontal");
            }
            meter.Orient = horiz? Orientation.Horizontal : Orientation.Vertical;

            int x = 0; int y = 0;
            meterNode.TryInteger("tickX", out x);
            meterNode.TryInteger("tickY", out y);

            meter.TickOffset = new Point(x, y);

            XElement soundNode = meterNode.Element("Sound");
            if (soundNode != null) meter.Sound = SoundInfo.FromXml(soundNode, basePath);

            return meter;
        }
    }
}
