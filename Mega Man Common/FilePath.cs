using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MegaMan
{
    public class FilePath
    {
        private string basepath;
        private string relative;
        private string absolute;

        public string BasePath
        {
            get { return basepath; }
            set
            {
                basepath = Path.GetFullPath(value);
                FindRelative();
            }
        }

        public string Absolute
        {
            get { return absolute; }
            set
            {
                absolute = Path.GetFullPath(value);
                FindRelative();
            }
        }

        public string Relative
        {
            get { return relative; }
        }

        public static FilePath FromAbsolute(string absolute, string basepath)
        {
            return new FilePath(absolute, basepath);
        }

        public static FilePath FromRelative(string relative, string basepath)
        {
            FilePath fp = new FilePath();
            fp.basepath = Path.GetFullPath(basepath);
            fp.relative = relative;
            fp.absolute = Path.Combine(basepath, relative);
            return fp;
        }

        private FilePath()
        {
        }

        private FilePath(string absolute, string basepath)
        {
            this.basepath = Path.GetFullPath(basepath);
            Absolute = absolute;
        }

        private void FindRelative()
        {
            if (string.IsNullOrEmpty(absolute)) return;
            if (string.IsNullOrEmpty(basepath)) return;

            if (System.IO.Path.HasExtension(basepath))
            {
                basepath = System.IO.Path.GetDirectoryName(basepath);
            }

            // split into directories
            string[] pathdirs = absolute.Split(System.IO.Path.DirectorySeparatorChar);
            string[] reldirs = basepath.Split(System.IO.Path.DirectorySeparatorChar);

            int length = Math.Min(pathdirs.Length, reldirs.Length);
            StringBuilder relativePath = new StringBuilder();

            // find where the paths differ
            int forkpoint = 0;
            while (forkpoint < length && pathdirs[forkpoint] == reldirs[forkpoint]) forkpoint++;

            // go back by the number of directories in the relativeTo path
            int dirs = reldirs.Length - forkpoint;
            for (int i = 0; i < dirs; i++) relativePath.Append("..").Append(System.IO.Path.DirectorySeparatorChar);

            // append file path from that directory
            for (int i = forkpoint; i < pathdirs.Length - 1; i++) relativePath.Append(pathdirs[i]).Append(System.IO.Path.DirectorySeparatorChar);
            // append file, without directory separator
            relativePath.Append(pathdirs[pathdirs.Length - 1]);

            relative = relativePath.ToString();
        }
    }
}
