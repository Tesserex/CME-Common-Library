using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;

namespace MegaMan
{
    public class PauseScreen
    {
        private List<WeaponInfo> weapons;

        public IEnumerable<WeaponInfo> Weapons
        {
            get { return weapons; }
        }

        public FilePath Background { get; set; }

        public SoundInfo ChangeSound { get; set; }

        public SoundInfo PauseSound { get; set; }

        public Point LivesPosition { get; set; }

        public PauseScreen(XElement reader, string basePath)
        {
            weapons = new List<WeaponInfo>();

            ChangeSound = SoundInfo.FromXml(reader.Element("ChangeSound"), basePath);
            PauseSound = SoundInfo.FromXml(reader.Element("PauseSound"), basePath);

            Background = FilePath.FromRelative(reader.Element("Background").Value, basePath);

            foreach (XElement weapon in reader.Elements("Weapon"))
                weapons.Add(WeaponInfo.FromXml(weapon, basePath));

            XElement livesNode = reader.Element("Lives");
            if (livesNode != null)
            {
                LivesPosition = new Point(livesNode.GetInteger("x"), livesNode.GetInteger("y"));
            }
        }
    }
}
