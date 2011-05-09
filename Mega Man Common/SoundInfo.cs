using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MegaMan
{
    public enum SoundType : byte
    {
        Wav,
        NSF,
        Unknown
    }

    public class SoundInfo
    {
        public string Name { get; private set; }
        public FilePath Path { get; private set; }
        public int NsfTrack { get; private set; }
        public bool Loop { get; private set; }
        public float Volume { get; private set; }
        public byte Priority { get; private set; }
        public SoundType Type { get; private set; }

        public static SoundInfo FromXml(XElement soundNode, string basePath)
        {
            SoundInfo sound = new SoundInfo();
            sound.Name = soundNode.RequireAttribute("name").Value;

            bool loop = false;
            soundNode.TryBool("loop", out loop);
            sound.Loop = loop;

            float vol;
            if (!soundNode.TryFloat("volume", out vol)) vol = 1;
            sound.Volume = vol;

            XAttribute pathattr = soundNode.Attribute("path");
            XAttribute trackAttr = soundNode.Attribute("track");
            if (pathattr != null)
            {
                sound.Type = SoundType.Wav;
                sound.Path = FilePath.FromRelative(pathattr.Value, basePath);
            }
            else if (trackAttr != null)
            {
                sound.Type = SoundType.NSF;

                int track;
                if (!trackAttr.Value.TryParse(out track) || track <= 0) throw new GameXmlException(trackAttr, "Sound track attribute must be an integer greater than zero.");
                sound.NsfTrack = track;

                int priority;
                if (!soundNode.TryInteger("priority", out priority)) priority = 100;
                sound.Priority = (byte)priority;
            }
            else
            {
                sound.Type = SoundType.Unknown;
            }

            return sound;
        }
    }
}
