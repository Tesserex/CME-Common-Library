using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Graphics.Color;

namespace MegaMan
{
    public struct EnemyCopyInfo
    {
        public string enemy;
        public string state;
        public float screenX;
        public float screenY;
        public string pallete;
        public bool boss;
    }

    public struct TeleportInfo
    {
        public Point From;
        public Point To;
        public string TargetScreen;
    }
    
    public class Screen
    {
        private int[][] tiles;
        private bool dirty;
        private string name;

        private FilePath musicIntroPath, musicLoopPath;
        private int musicNsfTrack;

        public FilePath MusicIntroPath { get { return musicIntroPath; } set { musicIntroPath = value; Dirty = true; } }
        public FilePath MusicLoopPath { get { return musicLoopPath; } set { musicLoopPath = value; Dirty = true; } }
        public int MusicNsfTrack { get { return musicNsfTrack; } set { musicNsfTrack = value; Dirty = true; } }

        public Map Map { get; private set; }

        public List<EnemyCopyInfo> EnemyInfo { get; private set; }
        public List<BlockPatternInfo> BlockPatternInfo { get; private set; }
        public List<TeleportInfo> Teleports { get; private set; }
        
        #region Properties
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                this.Dirty = true;
            }
        }
        public int Width { get { return tiles[0].Length; } }
        public int Height { get { return tiles.GetLength(0); } }
        public int PixelWidth { get { return tiles[0].Length * Tileset.TileSize; } }
        public int PixelHeight { get { return tiles.GetLength(0) * Tileset.TileSize; } }
        public Tileset Tileset { get; set; }

        public bool IsBossRoom { get; private set; }

        public bool Dirty 
        { 
            get 
            { 
                return dirty; 
            } 
            
            set 
            { 
                dirty = value; 
                Map.Dirty = value; 
            } 
        }

        #endregion Properties

        public Screen(int width, int height, Map parent)
        {
            this.Map = parent;

            EnemyInfo = new List<EnemyCopyInfo>();
            BlockPatternInfo = new List<BlockPatternInfo>();
            Teleports = new List<TeleportInfo>();
            Tileset = parent.Tileset;

            Resize(width, height);
        }

        public Screen(string filepath, Map parent)
        {
            this.Map = parent;
            Tileset = parent.Tileset;

            Name = System.IO.Path.GetFileNameWithoutExtension(filepath);

            string[] lines = File.ReadAllLines(filepath);
            string[] firstline = lines[0].Split(' ');
            int width = Int32.Parse(firstline[0]);
            int height = Int32.Parse(firstline[1]);
            tiles = new int[height][];
            for (int y = 0; y < height; y++)
            {
                tiles[y] = new int[width];
                string[] line = lines[y + 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < width; x++)
                {
                    int id = Int32.Parse(line[x]);
                    tiles[y][x] = id;
                }
            }

            EnemyInfo = new List<EnemyCopyInfo>();
            BlockPatternInfo = new List<BlockPatternInfo>();
            Teleports = new List<TeleportInfo>();
        }
        
        public void Resize(int width, int height) 
        {
            var newTiles = CreateNewTiles(width, height);

            if (this.tiles != null)
                CopyOldTiles(width, height, newTiles);

            this.tiles = newTiles;

            this.Dirty = true;
        }

        public void CopyOldTiles(int width, int height, int[][] newTiles) 
        {
            // Copy over old tiles
            int minWidth = (width < tiles[0].Length) ? width : tiles[0].Length;
            int minHeight = (height < tiles.Length) ? height : tiles.Length;

            for (int j = 0; j < minHeight; j++) 
                for (int i = 0; i < minWidth; i++) 
                    newTiles[j][i] = tiles[j][i];
        }

        public int[][] CreateNewTiles(int width, int height) 
        {
            var newTiles = new int[height][];
            for (int y = 0; y < height; y++) 
            {
                newTiles[y] = new int[width];
                for (int x = 0; x < width; x++) 
                    newTiles[y][x] = 0;
            }
            return newTiles;
        }

        public Tile TileAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return Tileset[tiles[y][x]];
        }
        
        public int? TileIndexAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return tiles[y][x];
        }

        public void AddEnemy(EnemyCopyInfo enemy)
        {
            EnemyInfo.Add(enemy);
            Dirty = true;
        }

        public void AddBlockPattern(BlockPatternInfo info)
        {
            BlockPatternInfo.Add(info);
            Dirty = true;
        }

        public void AddTeleport(TeleportInfo info)
        {
            Teleports.Add(info);
            Dirty = true;
        }

        public void ChangeTile(int x, int y, int tile)
        {
            if (y < 0 || y >= Height || x < 0 || x >= Width) 
                return;

            if (tile < 0 || tile >= Tileset.Count) 
                throw new ArgumentException("Tile is not within tileset range");

            tiles[y][x] = tile;

            Dirty = true;
        }

        public void Draw(Graphics g, float off_x, float off_y, int width, int height)
        {
            Draw(g, off_x, off_y, width, height, (img) => { return img; });
        }

        public void Draw(Graphics g, float off_x, float off_y, int width, int height, Func<Image, Image> transform)
        {
            if (Tileset == null) 
                throw new InvalidOperationException("Screen has no tileset to draw with.");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float xpos = x * Tileset.TileSize + off_x;
                    float ypos = y * Tileset.TileSize + off_y;

                    if (xpos + Tileset.TileSize < 0 || ypos + Tileset.TileSize < 0) continue;
                    if (xpos > width || ypos > height) continue;
                    Tileset[tiles[y][x]].Draw(g, xpos, ypos, transform);
                }
            }
        }
                
        public void DrawXna(SpriteBatch batch, XnaColor color, float off_x, float off_y, int width, int height)
        {
            if (Tileset == null) 
                throw new InvalidOperationException("Screen has no tileset to draw with.");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float xpos = x * Tileset.TileSize + off_x;
                    float ypos = y * Tileset.TileSize + off_y;

                    if (xpos + Tileset.TileSize < 0 || ypos + Tileset.TileSize < 0) continue;
                    if (xpos > width || ypos > height) continue;
                    Tileset[tiles[y][x]].Draw(batch, color, xpos, ypos);
                }
            }
        }

        public void Save(string filepath)
        {
            using (FileStream f = File.Open(filepath, FileMode.Create))
            {
                using (StreamWriter s = new StreamWriter(f))
                {
                    s.WriteLine(Width.ToString() + " " + Height.ToString());

                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            s.Write(tiles[y][x].ToString() + " ");
                        }
                        s.Write('\n');
                    }
                }
                f.Close();
            }
            Dirty = false;
        }
    }
}
