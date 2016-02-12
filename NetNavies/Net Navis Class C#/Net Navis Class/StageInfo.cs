using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net_Navis
{

    public class StageObject
    {
        Rectangle Bounds;
        GLItemTextureName Sprite;        
        bool Stretch;

       public StageObject(Rectangle bounds, GLItemTextureName sprite)
        {
            this.Bounds = bounds;
            this.Sprite = sprite;
        }

        public StageObject(Rectangle bounds, GLItemTextureName sprite, bool stretch)
        {
            Bounds = bounds;
            Sprite = sprite;            
            Stretch = stretch;
        }
    }


    public class StageBackground
    {
        float parallax;
        GLBGTextureName sprite;
        bool tiled;
        bool stretch;
        Rectangle bounds;

        public StageBackground(Rectangle bounds, GLBGTextureName sprite, float parallax)
        {
            this.bounds = bounds;
            this.sprite = sprite;
            this.parallax = parallax;
        }
        public StageBackground(Rectangle bounds, GLBGTextureName sprite, float parallax, bool tiled, bool stretch)
        {
            this.bounds = bounds;
            this.sprite = sprite;
            this.tiled = tiled;
            this.stretch = stretch;
            this.parallax = parallax;
        }


        public float Parallax { get { return this.parallax; } }
        public int SpriteInt { get { return (int)this.sprite; } }
        public GLBGTextureName Sprite { get { return this.sprite; } }
        public bool Tiled { get { return this.tiled; } }
        public bool Stretch { get { return this.stretch; } }
        public Rectangle Bounds { get { return this.bounds; } }        
    }


    public class StageCollisionTile
    {       
        public bool Active;
        public byte HeightLeft;
        public byte HeightRight;
        public bool DeathTile;

        public StageCollisionTile(bool deathTile = false)
        {
            HeightLeft = 16;
            HeightRight = 16;
            DeathTile = deathTile;            
        }

        public StageCollisionTile(byte heightLeft, byte heightRight, bool deathTile = false)
        {
            DeathTile = deathTile;
            HeightLeft = heightLeft;
            HeightRight = heightRight;
        }
    }


    public class Stage
    {
        public StageInfo info = new StageInfo();
        public HashSet<StageObject> Objects = new HashSet<StageObject>();
        public HashSet<StageBackground> BG = new HashSet<StageBackground>();
        public Dictionary<Point, StageCollisionTile> CollisionMap = new Dictionary<Point, StageCollisionTile>();
        public Point EntryPoint;
        public Size Bounds;
        public Color BackColor;
        public PointF Gravity;
        public PointF AirFriction;
        public PointF GroundFriction;



        
        public Stage(StageName Name = StageName.Lobby)
            {
                if (Name == StageName.Lobby) info.Load_Lobby(this);
                if (Name == StageName.Hyrule) info.Load_Hyrule(this);
            }           
        }


    public class StageInfo
    {


        private Size SpriteSize(GLBGTextureName sprite)
        {
            
            
            if (sprite == GLBGTextureName.LobbyBG1) return new Size(640, 600);
            if (sprite == GLBGTextureName.LobbyFG1) return new Size(832, 337);

            return new Size(0, 0);
        }





        public void Load_Lobby(Stage s)
        {

            s.BackColor = Color.White;
            s.Gravity = new PointF(0.0f, 0.5f);
            s.AirFriction = new PointF(0.01f, 0.01f);
            s.GroundFriction = new PointF(0.15f, 0f);
            
            s.Bounds = new Size(1664, 600);            
            s.EntryPoint = new Point(0, 0);
            //Add Background
            s.BG.Add(new StageBackground(new Rectangle(new Point(640 * 0, 0), SpriteSize(GLBGTextureName.LobbyBG1)), GLBGTextureName.LobbyBG1, 0.75f, true, false));
            s.BG.Add(new StageBackground(new Rectangle(new Point(640 * 1, 0), SpriteSize(GLBGTextureName.LobbyBG1)), GLBGTextureName.LobbyBG1, 0.75f, true, false));
            s.BG.Add(new StageBackground(new Rectangle(new Point(640 * 2, 0), SpriteSize(GLBGTextureName.LobbyBG1)), GLBGTextureName.LobbyBG1, 0.75f, true, false));
            //Add Forground
            s.BG.Add(new StageBackground(new Rectangle(new Point(0, 263), SpriteSize(GLBGTextureName.LobbyFG1)), GLBGTextureName.LobbyFG1, 1.0f, true, false));
            s.BG.Add(new StageBackground(new Rectangle(new Point(832, 263), SpriteSize(GLBGTextureName.LobbyFG1)), GLBGTextureName.LobbyFG1, 1.0f, true, false));

            for (int x = 0; x < 104; x++)
            {
                s.CollisionMap.Add(new Point(x, 36), new StageCollisionTile());
            }
            s.CollisionMap.Add(new Point(10, 35), new StageCollisionTile(0, 8));
            s.CollisionMap.Add(new Point(11, 35), new StageCollisionTile(8, 16));
            s.CollisionMap.Add(new Point(12, 35), new StageCollisionTile());
            s.CollisionMap.Add(new Point(13, 35), new StageCollisionTile());
            
            s.CollisionMap.Add(new Point(14, 34), new StageCollisionTile());
            s.CollisionMap.Add(new Point(15, 34), new StageCollisionTile());
            s.CollisionMap.Add(new Point(16, 34), new StageCollisionTile());
            s.CollisionMap.Add(new Point(17, 34), new StageCollisionTile());

            s.CollisionMap.Add(new Point(18, 32), new StageCollisionTile());
            s.CollisionMap.Add(new Point(19, 32), new StageCollisionTile());
            s.CollisionMap.Add(new Point(20, 32), new StageCollisionTile());
            s.CollisionMap.Add(new Point(21, 32), new StageCollisionTile());
        }




        public void Load_Hyrule(Stage s)
        {

            s.BackColor = Color.White;
            s.Gravity = new PointF(0.0f, 0.5f);
            s.AirFriction = new PointF(0.01f, 0.01f);
            s.GroundFriction = new PointF(0.15f, 0f);

            s.Bounds = new Size(1600, 1120);
            s.EntryPoint = new Point(500, 0);
            //Add Background
            s.BG.Add(new StageBackground(new Rectangle(new Point(0, 0), Net_Navis.Resource1.HyruleBG.Size), GLBGTextureName.HyruleBG, 0.75f, true, false));
            //Add Forground
            s.BG.Add(new StageBackground(new Rectangle(new Point(320, 160), Net_Navis.Resource1.HyruleFG.Size), GLBGTextureName.HyruleFG, 1.0f, true, false));


            //DeathMap
            for (int y = 0; y <= 70; y++)//left
                s.CollisionMap.Add(new Point(0, y), new StageCollisionTile(true));

            for (int y = 0; y <= 70; y++)//right
                s.CollisionMap.Add(new Point(99, y), new StageCollisionTile(true));

            for (int x = 1; x < 99; x++)//bottom
                s.CollisionMap.Add(new Point(x, 69), new StageCollisionTile(true));
            
            for (int x = 25; x < 47; x++)
                 s.CollisionMap.Add(new Point(x, 26), new StageCollisionTile());

            for (int x = 51; x < 69; x++)
                s.CollisionMap.Add(new Point(x, 26), new StageCollisionTile());

        }
    

    }

    public enum StageName
    {
        Lobby,
        Hyrule
    }

    
}

   


