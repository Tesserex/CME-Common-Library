using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;

namespace MegaMan
{
    public class BlockPatternInfo
    {
        public class BlockInfo
        {
            public PointF pos;
            public int on;
            public int off;
        }

        public string Entity { get; private set; }
        public int Length { get; private set; }
        public List<BlockInfo> Blocks { get; private set; }
        public int LeftBoundary { get; private set; }
        public int RightBoundary { get; private set; }

        public BlockPatternInfo(XElement xmlNode)
        {
            int left, right, length;

            XAttribute leftAttr = xmlNode.Attribute("left");
            if (leftAttr == null) throw new Exception("Blocks must specify a numeric left attribute");
            if (!int.TryParse(leftAttr.Value, out left)) throw new Exception("Blocks left attribute is not a valid number!");

            XAttribute rightAttr = xmlNode.Attribute("right");
            if (rightAttr == null) throw new Exception("Blocks must specify a numeric right attribute");
            if (!int.TryParse(rightAttr.Value, out right)) throw new Exception("Blocks right attribute is not a valid number!");

            XAttribute lenAttr = xmlNode.Attribute("length");
            if (lenAttr == null) throw new Exception("Blocks must specify a numeric length attribute");
            if (!int.TryParse(lenAttr.Value, out length)) throw new Exception("Blocks length attribute is not a valid number!");

            XAttribute nameAttr = xmlNode.Attribute("entity");
            if (nameAttr == null) throw new Exception("Blocks must specify an entity attribute.");
            Entity = nameAttr.Value;
            LeftBoundary = left;
            RightBoundary = right;
            Length = length;

            Blocks = new List<BlockInfo>();
            foreach (XElement block in xmlNode.Elements("Block"))
            {
                int x, y, on, off;

                XAttribute attr = block.Attribute("x");
                if (attr == null) throw new Exception("Block must specify a numeric x attribute");
                if (!int.TryParse(attr.Value, out x)) throw new Exception("Blocks x attribute is not a valid number!");

                attr = block.Attribute("y");
                if (attr == null) throw new Exception("Block must specify a numeric y attribute");
                if (!int.TryParse(attr.Value, out y)) throw new Exception("Blocks y attribute is not a valid number!");

                attr = block.Attribute("on");
                if (attr == null) throw new Exception("Block must specify a numeric on attribute");
                if (!int.TryParse(attr.Value, out on)) throw new Exception("Blocks on attribute is not a valid number!");

                attr = block.Attribute("off");
                if (attr == null) throw new Exception("Block must specify a numeric off attribute");
                if (!int.TryParse(attr.Value, out off)) throw new Exception("Blocks off attribute is not a valid number!");

                BlockInfo info = new BlockInfo();
                info.pos = new PointF(x, y);
                info.on = on;
                info.off = off;

                this.Blocks.Add(info);
            }
        }
    }
}
