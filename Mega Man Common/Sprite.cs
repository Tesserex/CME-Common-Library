using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using DrawPoint     = System.Drawing.Point;
using DrawRectangle = System.Drawing.Rectangle;

using XnaRectangle  = Microsoft.Xna.Framework.Rectangle;
using XnaColor      = Microsoft.Xna.Framework.Graphics.Color;

namespace MegaMan
{
    /// <summary>
    /// Represents a 2D rectangular image sprite, which can be animated.
    /// </summary>
    public class Sprite : ICollection<SpriteFrame>
    {
        private List<SpriteFrame> frames;
        private int currentFrame;
        private int lastFrameTime;

        // XNA stuff
        private Texture2D texture;

        internal Image sheet;

        /// <summary>
        /// Gets or sets the direction in which to play the sprite animation.
        /// </summary>
        public AnimationDirection AnimDirection { get; set; }

        /// <summary>
        /// Gets or sets the animation style.
        /// </summary>
        public AnimationStyle AnimStyle { get; set; }

        /// <summary>
        /// Gets or sets the point representing the drawing offset for the sprite.
        /// </summary>
        public DrawPoint HotSpot { get; set; }

        /// <summary>
        /// Gets a rectangle representing the box surrounding the sprite.
        /// </summary>
        public RectangleF BoundBox { get; protected set; }

        /// <summary>
        /// Gets the height of the sprite.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Gets the width of the sprite.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the number of frames in the sprite animation.
        /// </summary>
        public int Count { get { return frames.Count; } }

        public int CurrentFrame { get { return this.currentFrame; } set { this.currentFrame = value; } }
        
        public int FrameTime { get { return this.lastFrameTime; } set { this.lastFrameTime = value; } }

        public string Name { get; private set; }

        /// <summary>
        /// Gets whether or not the sprite animation is currently playing.
        /// </summary>
        public bool Playing { get; private set; }

        public bool HorizontalFlip { get; set; }

        public bool VerticalFlip { get; set; }

        public bool Visible { get; set; }

        public int Layer { get; private set; }

        /// <summary>
        /// If this is true, it means the sprite sheet is backwards - it's facing left instead of right,
        /// so we have to flip all drawing of this sprite to match proper orientation rules.
        /// </summary>
        public bool Reversed { get; set; }

        public event Action Stopped;

        /// <summary>
        /// Creates a new Sprite object with the given width and height, and no frames.
        /// </summary>
        public Sprite(int width, int height)
        {
            this.Height = height;
            this.Width = width;
            frames = new List<SpriteFrame>();

            this.currentFrame = 0;
            this.lastFrameTime = 0;
            this.HotSpot = new DrawPoint(0, 0);
            this.BoundBox = new RectangleF(0, 0, Width, Height);
            this.Playing = false;
            this.Visible = true;
            this.AnimDirection = AnimationDirection.Forward;
            this.AnimStyle = AnimationStyle.Repeat;
            this.sheet = null;
        }

        public Sprite(Sprite copy)
        {
            this.Height = copy.Height;
            this.Width = copy.Width;
            this.tickable = copy.tickable;
            this.frames = copy.frames;
            this.currentFrame = 0;
            this.lastFrameTime = 0;
            this.HotSpot = new DrawPoint(copy.HotSpot.X, copy.HotSpot.Y);
            this.BoundBox = new RectangleF(0, 0, copy.Width, copy.Height);
            this.Playing = false;
            this.Visible = true;
            this.AnimDirection = copy.AnimDirection;
            this.AnimStyle = copy.AnimStyle;
            this.Layer = copy.Layer;
            this.texture = copy.texture;
            this.Reversed = copy.Reversed;
        }

        public void SetTexture(GraphicsDevice device, string sheet)
        {
            texture = Texture2D.FromFile(device, sheet);
        }

        public void SetTexture(Texture2D texture)
        {
            this.texture = texture;
        }

        public SpriteFrame this[int index]
        {
            get { return frames[index]; }
        }

        /// <summary>
        /// Adds a frame with no image or duration
        /// </summary>
        public void AddFrame()
        {
            frames.Add(new SpriteFrame(this, this.sheet, 0, DrawRectangle.Empty));
            CheckTickable();
        }

        /// <summary>
        /// Adds a frame to the collection from a given Image.
        /// </summary>
        /// <param name="tilesheet">The image from which to retreive the frame.</param>
        /// <param name="x">The x-coordinate, on the tilesheet, of the top-left corner of the frame image.</param>
        /// <param name="y">The y-coordinate, on the tilesheet, of the top-left corner of the frame image.</param>
        /// <param name="duration">The duration of the frame, in game ticks.</param>
        public void AddFrame(Image tilesheet, int x, int y, int duration)
        {
            this.frames.Add(new SpriteFrame(this, tilesheet, duration, new DrawRectangle(x, y, this.Width, this.Height)));
            CheckTickable();
        }

        /// <summary>
        /// Advances the sprite animation by one tick. This should be the default if Update is called once per game step.
        /// </summary>
        public void Update() { Update(1); }

        /// <summary>
        /// Advances the sprite animation, if it is currently playing.
        /// </summary>
        /// <param name="ticks">The number of steps, or ticks, to advance the animation.</param>
        public void Update(int ticks)
        {
            if (!Playing || !tickable) return;

            this.lastFrameTime += ticks;
            int neededTime = frames[currentFrame].Duration;

            if (this.lastFrameTime >= neededTime)
            {
                this.lastFrameTime -= neededTime;
                this.TickFrame();
                while (this.frames[currentFrame].Duration == 0) this.TickFrame();
            }
        }

        /// <summary>
        /// Draws the sprite on the specified Graphics surface at the specified position. Remember that the HotSpot is used as a position offset.
        /// </summary>
        /// <param name="graphics">The graphics surface on which to draw the sprite.</param>
        /// <param name="posX">The x-coordinate at which to draw the sprite.</param>
        /// <param name="posY">The y-coordinate at which to draw the sprite.</param>
        public void Draw(Graphics graphics, float positionX, float positionY) 
        {
            Draw(graphics, positionX, positionY, (img) => { return img; });
        }

        public void Draw(Graphics graphics, float positionX, float positionY, Func<Image, Image> transform)
        {
            if (!Visible || frames.Count == 0) return;
            if (this.frames[currentFrame].Image == null)
            {
                graphics.FillRectangle(Brushes.Black, positionX, positionY, this.Width, this.Height);
                return;
            }

            bool horiz = this.HorizontalFlip;
            if (this.Reversed) 
                horiz = !horiz;
            this.frames[currentFrame].Draw(graphics, positionX - this.HotSpot.X, positionY - this.HotSpot.Y, horiz, this.VerticalFlip, transform);
        }

        public void DrawXna(SpriteBatch batch, XnaColor color, float positionX, float positionY)
        {
            if (!Visible || frames.Count == 0 || batch == null || this.texture == null) return;

            SpriteEffects effect = SpriteEffects.None;
            if (HorizontalFlip ^ this.Reversed) effect = SpriteEffects.FlipHorizontally;
            if (VerticalFlip) effect |= SpriteEffects.FlipVertically;

            int hx = (HorizontalFlip ^ this.Reversed) ? this.Width - this.HotSpot.X : this.HotSpot.X;
            int hy = VerticalFlip ? this.Height - this.HotSpot.Y : this.HotSpot.Y;

            batch.Draw(this.texture,
                new XnaRectangle((int)(positionX),
                    (int)(positionY), this.Width, this.Height),
                new XnaRectangle(this[currentFrame].SheetLocation.X, this[currentFrame].SheetLocation.Y, this[currentFrame].SheetLocation.Width, this[currentFrame].SheetLocation.Height),
                color, 0,
                new Vector2(hx, hy), effect, 0);
        }

        private bool tickable;

        internal void CheckTickable()
        {
            tickable = false;
            if (frames.Count <= 1) 
                return;
            else foreach (SpriteFrame frame in frames)
            {
                if (frame.Duration > 0)
                {
                    tickable = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Begins playing the animation from the beginning.
        /// </summary>
        public void Play()
        {
            this.Playing = true;
            this.Reset();
        }

        /// <summary>
        /// Stops and resets the animation.
        /// </summary>
        public void Stop()
        {
            this.Playing = false;
            this.Reset();
            if (Stopped != null) Stopped();
        }

        /// <summary>
        /// Resumes playing the animation from the current frame.
        /// </summary>
        public void Resume() { this.Playing = true; }

        /// <summary>
        /// Pauses the animation at the current frame.
        /// </summary>
        public void Pause() { this.Playing = false; }

        /// <summary>
        /// Restarts the animation to the first frame or the last, based on the value of AnimDirection.
        /// </summary>
        public void Reset()
        {
            if (AnimDirection == AnimationDirection.Forward) currentFrame = 0;
            else currentFrame = Count - 1;

            lastFrameTime = 0;
        }

        private void TickFrame()
        {
            switch (this.AnimDirection)
            {
                case AnimationDirection.Forward:
                    this.currentFrame++;
                    break;
                case AnimationDirection.Backward:
                    this.currentFrame--;
                    break;
            }

            if (this.currentFrame >= this.Count)
            {
                switch (this.AnimStyle)
                {
                    case AnimationStyle.PlayOnce:
                        this.Stop();
                        break;
                    case AnimationStyle.Repeat:
                        this.currentFrame = 0;
                        break;
                    case AnimationStyle.Bounce:
                        this.currentFrame -= 2;
                        this.AnimDirection = AnimationDirection.Backward;
                        break;
                }
            }
            else if (this.currentFrame < 0)
            {
                switch (this.AnimStyle)
                {
                    case AnimationStyle.PlayOnce:
                        this.Stop();
                        break;
                    case AnimationStyle.Repeat:
                        this.currentFrame = this.Count - 1;
                        break;
                    case AnimationStyle.Bounce:
                        this.currentFrame = 1;
                        this.AnimDirection = AnimationDirection.Forward;
                        break;
                }
            }
        }

        public static readonly Sprite Empty = new Sprite(0, 0);

        public static Sprite FromXml(string path)
        {
            XmlTextReader reader = new XmlTextReader(path);
            if (!reader.ReadToFollowing("Sprite")) return Sprite.Empty; // should alert the user

            string dir = System.IO.Path.GetDirectoryName(path);
            return FromXml(ref reader, Image.FromFile(System.IO.Path.Combine(dir, reader.GetAttribute("tilesheet"))));
        }

        public static Sprite FromXml(ref XmlTextReader reader, Image tilesheet)
        {
            int width = Int32.Parse(reader.GetAttribute("width"));
            int height = Int32.Parse(reader.GetAttribute("height"));

            Sprite sprite = new Sprite(width, height);
            sprite.sheet = tilesheet;

            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && (reader.Name == "Sprite" || reader.Name == "State") ))
            {
                switch (reader.Name)
                {
                    case "Hotspot":
                        int hx = Int32.Parse(reader.GetAttribute("x"));
                        int hy = Int32.Parse(reader.GetAttribute("y"));
                        sprite.HotSpot = new DrawPoint(hx, hy);
                        break;

                    case "AnimStyle":
                        string style = reader.ReadString();
                        switch (style)
                        {
                            case "Bounce": sprite.AnimStyle = AnimationStyle.Bounce; break;
                            case "PlayOnce": sprite.AnimStyle = AnimationStyle.PlayOnce; break;
                        }
                        break;

                    case "Frame":
                        int duration = Int32.Parse(reader.GetAttribute("duration"));
                        int x = Int32.Parse(reader.GetAttribute("x"));
                        int y = Int32.Parse(reader.GetAttribute("y"));
                        sprite.AddFrame(tilesheet, x, y, duration);
                        break;
                }
            }

            return sprite;
        }

        public static Sprite FromXml(XElement element, string basePath)
        {
            XAttribute tileattr = element.Attribute("tilesheet");
            if (tileattr == null) throw new ArgumentException("Sprite element does not specify a tilesheet!");
            Sprite sprite = null;

            Image tilesheet = Image.FromFile(System.IO.Path.Combine(basePath, tileattr.Value));
            sprite = FromXml(element, tilesheet);
            return sprite; 
        }

        public static Sprite FromXml(XElement element, Image tilesheet)
        {
            int width = Int32.Parse(element.Attribute("width").Value);
            int height = Int32.Parse(element.Attribute("height").Value);

            Sprite sprite = new Sprite(width, height);
            sprite.sheet = tilesheet;

            XAttribute nameAttr = element.Attribute("name");
            if (nameAttr != null) sprite.Name = nameAttr.Value;

            XAttribute revAttr = element.Attribute("reversed");
            if (revAttr != null)
            {
                bool r = false;
                if (bool.TryParse(revAttr.Value, out r)) sprite.Reversed = r;
            }

            XElement hotspot = element.Element("Hotspot");
            if (hotspot != null)
            {
                int hx = Int32.Parse(hotspot.Attribute("x").Value);
                int hy = Int32.Parse(hotspot.Attribute("y").Value);
                sprite.HotSpot = new DrawPoint(hx, hy);
            }

            XAttribute layerAttr = element.Attribute("layer");
            if (layerAttr != null)
            {
                int layer;
                if (int.TryParse(layerAttr.Value, out layer)) sprite.Layer = layer;
            }

            XElement stylenode = element.Element("AnimStyle");
            if (stylenode != null)
            {
                string style = stylenode.Value;
                switch (style)
                {
                    case "Bounce": sprite.AnimStyle = AnimationStyle.Bounce; break;
                    case "PlayOnce": sprite.AnimStyle = AnimationStyle.PlayOnce; break;
                }
            }

            foreach (XElement frame in element.Elements("Frame"))
            {
                int duration = Int32.Parse(frame.Attribute("duration").Value);
                int x = Int32.Parse(frame.Attribute("x").Value);
                int y = Int32.Parse(frame.Attribute("y").Value);
                sprite.AddFrame(tilesheet, x, y, duration);
            }

            return sprite;
        }

        public void WriteTo(XmlTextWriter writer)
        {
            writer.WriteStartElement("Sprite");
            writer.WriteAttributeString("width", this.Width.ToString());
            writer.WriteAttributeString("height", this.Height.ToString());

            writer.WriteStartElement("Hotspot");
            writer.WriteAttributeString("x", this.HotSpot.X.ToString());
            writer.WriteAttributeString("y", this.HotSpot.Y.ToString());
            writer.WriteEndElement();

            foreach (SpriteFrame frame in this.frames)
            {
                writer.WriteStartElement("Frame");
                writer.WriteAttributeString("x", frame.SheetLocation.X.ToString());
                writer.WriteAttributeString("y", frame.SheetLocation.Y.ToString());
                writer.WriteAttributeString("duration", frame.Duration.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();   // end Sprite
        }

        #region ICollection<SpriteFrame> Members

        public void Add(SpriteFrame item)
        {
            this.frames.Add(item);
            CheckTickable();
        }

        public void Clear()
        {
            this.frames.Clear();
            tickable = false;
        }

        public bool Contains(SpriteFrame item)
        {
            return this.frames.Contains(item);
        }

        public void CopyTo(SpriteFrame[] array, int arrayIndex)
        {
            this.frames.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(SpriteFrame item)
        {
            return this.frames.Remove(item);
        }

        #endregion

        #region IEnumerable<SpriteFrame> Members

        public IEnumerator<SpriteFrame> GetEnumerator()
        {
            return this.frames.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.frames.GetEnumerator();
        }

        #endregion
    }

    public class SpriteFrame
    {
        private Sprite sprite;
        private Image cutTile;
        private int duration;

        /// <summary>
        /// Gets or sets the image for this frame.
        /// </summary>
        public Image Image { get; private set; }
        public Image CutTile { get { return cutTile; } }
        public DrawRectangle SheetLocation { get; private set; }

        /// <summary>
        /// Gets or sets the number of ticks that this image should be displayed.
        /// </summary>
        public int Duration
        {
            get
            {
                return duration;
            }
            set
            {
                duration = value;
                sprite.CheckTickable();
            }
        }

        internal SpriteFrame(Sprite spr, Image img, int duration, DrawRectangle sheetRect)
        {
            this.sprite = spr;
            this.Image = img;
            this.Duration = duration;
            this.SheetLocation = sheetRect;

            CutoutTile();
        }

        public void SetSheetPosition(DrawRectangle rect)
        {
            SheetLocation = rect;
            CutoutTile();
        }

        private void CutoutTile()
        {
            if (this.Image == null || this.SheetLocation == DrawRectangle.Empty) return;
            if (this.cutTile != null) this.cutTile.Dispose();
            this.cutTile = new Bitmap(SheetLocation.Width, SheetLocation.Height);
            using (Graphics g = Graphics.FromImage(cutTile))
            {
                g.DrawImage(Image, new DrawRectangle(0, 0, SheetLocation.Width, SheetLocation.Height), SheetLocation.X, SheetLocation.Y, SheetLocation.Width, SheetLocation.Height, GraphicsUnit.Pixel);
            }
        }

        public void Draw(Graphics g, float positionX, float positionY, bool hflip, bool vflip) 
        {
            Draw(g, positionX, positionY, hflip, vflip, (img) => { return img; });
        }

        public void Draw(Graphics g, float positionX, float positionY, bool hflip, bool vflip, Func<Image,Image> transform) 
        {
            int trueX, trueY;
            if (hflip)
            {
                this.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                trueX = (int)(Image.Width - SheetLocation.Right);
            }
            else trueX = SheetLocation.Left;

            if (vflip) 
            {
                this.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                trueY = (int)(Image.Height - SheetLocation.Bottom);
            }
            else 
                trueY = SheetLocation.Top;

            if (this.cutTile == null) 
                g.FillRectangle(Brushes.Black, positionX, positionY, this.SheetLocation.Width, this.SheetLocation.Height);
            else
                g.DrawImage(transform(this.cutTile), positionX, positionY, cutTile.Width, cutTile.Height);

            if (hflip) 
                this.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

            if (vflip) 
                this.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            
        }
    }

    /// <summary>
    /// Specifies in which direction an animation playback should sweep.
    /// </summary>
    public enum AnimationDirection : int
    {
        Forward = 1,
        Backward = 2
    }

    /// <summary>
    /// Describes how an animation is played.
    /// </summary>
    public enum AnimationStyle : int
    {
        /// <summary>
        /// The animation is played until the last frame, and then stops.
        /// </summary>
        PlayOnce = 1,
        /// <summary>
        /// The animation will repeat from the beginning indefinitely.
        /// </summary>
        Repeat = 2,
        /// <summary>
        /// The animation will play forward and backward indefinitely.
        /// </summary>
        Bounce = 3
    }
}
