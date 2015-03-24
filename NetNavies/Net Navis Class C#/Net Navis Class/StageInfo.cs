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

    public class Stage
    {
        public StageInfo info = new StageInfo();
        public HashSet<StageObject> Objects = new HashSet<StageObject>();
        public HashSet<StageBackground> BG = new HashSet<StageBackground>();
        public Point EntryPoint;
        public Size Bounds;
        public Color BackColor;
        public PointF Gravity;
        public PointF AirFriction;
        public PointF GroundFriction;



        
        public Stage(StageName Name = StageName.Lobby)
            {
                if (Name == StageName.Lobby) info.Load_Lobby(this);
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
            s.Bounds = new Size(1000, 337);
            s.Bounds = new Size(1664, 600);
            s.EntryPoint = new Point(0, 0);
            //Add Background
            s.BG.Add(new StageBackground(new Rectangle(new Point(640 * 0, 0), SpriteSize(GLBGTextureName.LobbyBG1)), GLBGTextureName.LobbyBG1, 0.75f, true, false));
            s.BG.Add(new StageBackground(new Rectangle(new Point(640 * 1, 0), SpriteSize(GLBGTextureName.LobbyBG1)), GLBGTextureName.LobbyBG1, 0.75f, true, false));
            s.BG.Add(new StageBackground(new Rectangle(new Point(640 * 2, 0), SpriteSize(GLBGTextureName.LobbyBG1)), GLBGTextureName.LobbyBG1, 0.75f, true, false));
            //Add Forground
            s.BG.Add(new StageBackground(new Rectangle(new Point(0, 263), SpriteSize(GLBGTextureName.LobbyFG1)), GLBGTextureName.LobbyFG1, 1.0f, true, false));
            s.BG.Add(new StageBackground(new Rectangle(new Point(832, 263), SpriteSize(GLBGTextureName.LobbyFG1)), GLBGTextureName.LobbyFG1, 1.0f, true, false));



        }
    

    }

    public enum StageName
    {
        Lobby
    }

    
}

   


