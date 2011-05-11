﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace MegaMan
{
    public class MusicInfo
    {
        public FilePath IntroPath { get; private set; }
        public FilePath LoopPath { get; private set; }
        public int NsfTrack { get; private set; }
        public AudioType Type { get; private set; }

        public static MusicInfo FromXml(XElement musicNode, string basePath)
        {
            MusicInfo music = new MusicInfo();

            XAttribute pathattr = musicNode.Attribute("path");
            var introNode = musicNode.Element("Intro");
            var loopNode = musicNode.Element("Loop");

            XAttribute trackAttr = musicNode.Attribute("track");

            if (introNode != null || loopNode != null)
            {
                music.Type = AudioType.Wav;
                if (introNode != null) music.IntroPath = FilePath.FromRelative(introNode.Value, basePath);
                if (loopNode != null) music.LoopPath = FilePath.FromRelative(loopNode.Value, basePath);
            }
            else if (trackAttr != null)
            {
                music.Type = AudioType.NSF;

                int track;
                if (!trackAttr.Value.TryParse(out track) || track <= 0) throw new GameXmlException(trackAttr, "Sound track attribute must be an integer greater than zero.");
                music.NsfTrack = track;
            }
            else
            {
                music.Type = AudioType.Unknown;
            }

            return music;
        }

        public void Save(XmlTextWriter writer)
        {
            if (this.Type == AudioType.Unknown) return;

            writer.WriteStartElement("Music");
            if (this.Type == AudioType.Wav)
            {
                if (this.IntroPath != null) writer.WriteElementString("Intro", this.IntroPath.Relative);
                if (this.LoopPath != null) writer.WriteElementString("Loop", this.LoopPath.Relative);
            }
            else
            {
                writer.WriteAttributeString("track", this.NsfTrack.ToString());
            }
            writer.WriteEndElement();
        }
    }
}