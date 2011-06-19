﻿using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan
{
    public class BossInfo
    {
        public int Slot { get; set; }
        public string Name { get; set; }
        public FilePath PortraitPath { get; set; }
        public string Stage { get; set; }
    }

    public class StageSelect
    {
        private List<BossInfo> bosses = new List<BossInfo>();

        public IEnumerable<BossInfo> Bosses
        {
            get { return bosses; }
        }

        public void AddBoss(BossInfo boss)
        {
            this.bosses.Add(boss);
        }

        public Sprite BossFrame { get; set; }

        public FilePath Background { get; set; }

        public MusicInfo Music { get; set; }

        public SoundInfo ChangeSound { get; set; }

        public int BossSpacingHorizontal { get; set; }

        public int BossSpacingVertical { get; set; }

        public int BossOffset { get; set; }

        public StageSelect(XElement stageSelectNode, string baseDir)
        {
            BossSpacingHorizontal = 24;
            BossSpacingVertical = 16;

            XElement frameNode = stageSelectNode.Element("BossFrame");
            if (frameNode != null)
            {
                XElement bossSprite = frameNode.Element("Sprite");
                if (bossSprite != null) this.BossFrame = Sprite.FromXml(bossSprite, baseDir);
            }

            XElement bgNode = stageSelectNode.Element("Background");
            if (bgNode != null) this.Background = FilePath.FromRelative(bgNode.Value, baseDir);

            XElement musicNode = stageSelectNode.Element("Music");
            if (musicNode != null)
            {
                this.Music = MusicInfo.FromXml(musicNode, baseDir);
            }

            XElement soundNode = stageSelectNode.Element("ChangeSound");
            if (soundNode != null)
            {
                this.ChangeSound = SoundInfo.FromXml(soundNode, baseDir);
            }

            foreach (XElement bossNode in stageSelectNode.Elements("Boss"))
            {
                XAttribute slotAttr = bossNode.Attribute("slot");
                int slot = -1;
                if (slotAttr != null) int.TryParse(slotAttr.Value, out slot);

                BossInfo info = new BossInfo();
                info.Slot = slot;
                var bossNameAttr = bossNode.Attribute("name");
                if (bossNameAttr != null) info.Name = bossNameAttr.Value;
                var portrait = bossNode.Attribute("portrait");
                if (portrait != null) info.PortraitPath = FilePath.FromRelative(portrait.Value, baseDir);
                info.Stage = bossNode.RequireAttribute("stage").Value;
                bosses.Add(info);
            }

            XElement spacingNode = stageSelectNode.Element("Spacing");
            if (spacingNode != null)
            {
                int x;
                if (spacingNode.TryInteger("x", out x))
                {
                    BossSpacingHorizontal = x;
                }

                int y;
                if (spacingNode.TryInteger("y", out y))
                {
                    BossSpacingVertical = y;
                }

                int off;
                if (spacingNode.TryInteger("offset", out off))
                {
                    BossOffset = off;
                }
            }
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("StageSelect");

            if (Music != null) Music.Save(writer);

            if (ChangeSound != null)
            {
                ChangeSound.Save(writer);
            }

            if (this.Background != null) writer.WriteElementString("Background", this.Background.Relative);

            if (this.BossFrame != null)
            {
                writer.WriteStartElement("BossFrame");
                this.BossFrame.WriteTo(writer);
                writer.WriteEndElement(); // BossFrame
            }

            writer.WriteStartElement("Spacing");
            writer.WriteAttributeString("x", this.BossSpacingHorizontal.ToString());
            writer.WriteAttributeString("y", this.BossSpacingVertical.ToString());
            writer.WriteAttributeString("offset", this.BossOffset.ToString());
            writer.WriteEndElement();

            foreach (BossInfo boss in this.bosses)
            {
                writer.WriteStartElement("Boss");
                if (boss.Slot >= 0) writer.WriteAttributeString("slot", boss.Slot.ToString());
                if (!string.IsNullOrEmpty(boss.Name)) writer.WriteAttributeString("name", boss.Name);
                if (boss.PortraitPath != null && !string.IsNullOrEmpty(boss.PortraitPath.Relative)) writer.WriteAttributeString("portrait", boss.PortraitPath.Relative);
                if (!string.IsNullOrEmpty(boss.Stage)) writer.WriteAttributeString("stage", boss.Stage);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // StageSelect
        }
    }
}
