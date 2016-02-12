using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Reflection;

namespace Net_Navis
{
    partial class Navi_Main
    {
        public Dictionary<int, int> GLNaviTexture = new Dictionary<int, int>();
        public Dictionary<int, int> GLItemTexture = new Dictionary<int, int>();
        public Dictionary<int, int> GLBackground = new Dictionary<int, int>();

        public bool Init_GL;
        public bool GLOn;

        //public readonly Font TextFont = new Font(FontFamily.GenericSansSerif, 8);
        public Font font = new Font("Arial Black", 20, FontStyle.Regular);        
        

        public void Draw_Navi()
        {
            if (GLOn == false)
            {
                if (!(Host_Navi.Sprite == Host_Navi.OldSprite) || !(Host_Navi.FaceLeft == Host_Navi.OldFaceLeft))
                {
                    NaviForm.Invalidate();
                    Host_Navi.OldSprite = Host_Navi.Sprite;
                    Host_Navi.OldFaceLeft = Host_Navi.FaceLeft;
                }
            }

            if (GLOn == true)
            {

                if (Init_GL == false)
                {
                    Start_GL();
                }
                ScreenScroll.X = Host_Navi.Location.X + Host_Navi.GetSize().X / 2 - ScreenSize.Width / 2;
                ScreenScroll.Y = Host_Navi.Location.Y + Host_Navi.GetSize().Y / 2 - ScreenSize.Height / 2;

                SizeF screenBounds = new SizeF(0 - (ScreenSize.Width - (ScreenSize.Width / ScreenZoom)) / 2, 0 - (ScreenSize.Height - (ScreenSize.Height / ScreenZoom)) / 2);
                SizeF stageBounds = new SizeF(0 - (stage.Bounds.Width - (stage.Bounds.Width / ScreenZoom)) / 2, 0 - (stage.Bounds.Height - (stage.Bounds.Height / ScreenZoom))/2);

                if (ScreenScroll.X + ScreenSize.Width > stage.Bounds.Width - screenBounds.Width) ScreenScroll.X = stage.Bounds.Width - (ScreenSize.Width + screenBounds.Width);
                if (ScreenScroll.X < screenBounds.Width) ScreenScroll.X = screenBounds.Width;                               
                if (ScreenScroll.Y < screenBounds.Height) ScreenScroll.Y = screenBounds.Height;
                if (ScreenScroll.Y + ScreenSize.Height > stage.Bounds.Height - screenBounds.Height) ScreenScroll.Y = stage.Bounds.Height - (ScreenSize.Height + screenBounds.Height);
                Draw_GL();
            }

        }


        #region GDI+                
        public void Draw_Navi_GDI(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            NaviForm.Width = Convert.ToInt32(Host_Navi.GetSize().X);
            NaviForm.Height = Convert.ToInt32(Host_Navi.GetSize().Y);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            float xoff = 0;
            float yoff = 0;
            xoff = Convert.ToSingle(-0.5 + 0.5 * Host_Navi.SpriteSize.X / Host_Navi.GetSize().X) + Host_Navi.SpriteSize.X * Host_Navi.Sprite.X;
            yoff = Convert.ToSingle(-0.5 + 0.5 * Host_Navi.SpriteSize.Y / Host_Navi.GetSize().Y) + Host_Navi.SpriteSize.Y * Host_Navi.Sprite.Y;

            if (Host_Navi.FaceLeft == true)
            {
                e.Graphics.DrawImage(Host_Navi.SpriteSheet, new Rectangle(Convert.ToInt32(Host_Navi.GetSize().X), 0, Convert.ToInt32(-Host_Navi.GetSize().X - 1), Convert.ToInt32(Host_Navi.GetSize().Y)), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage);
            }
            else
            {
                e.Graphics.DrawImage(Host_Navi.SpriteSheet, new Rectangle(0, 0, Convert.ToInt32(Host_Navi.GetSize().X), Convert.ToInt32(Host_Navi.GetSize().Y)), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage);
            }


        }
        public void Set_color_filters()
        {
            float[][] NormalColorMatrixElements = {
				new float[] {1,0,0,0,0},
				new float[] {0,1,0,0,0},
				new float[] {0,0,1,0,0},
				new float[] {0,0,0,1,0},
				new float[] {0,0,0,0,1}
			};

            float[][] BlueColorMatrixElements = {
				new float[] {1,0,0,0,0},
				new float[] {0,1,0,0,0},
				new float[] {0,0,2,0,0},
				new float[] {0,0,0,1,0},
				new float[] {-0.2f,-0.2f,0,0,1}
			};

            NormalImage.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(NormalColorMatrixElements), System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
            BlueImage.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(BlueColorMatrixElements), System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

        }

        #endregion

        #region OpenGL

        public void Draw_GL()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(stage.BackColor);
            GL.Color4(1f, 1f, 1f, 1f);                        

            foreach (StageBackground BG in stage.BG)
            {
                Draw_Background_GL(BG);
            }
            //Draw host navi
            Draw_Navi_GL(Host_Navi);

            //Draw all client navis
            foreach (NetNavi_Type navi in Client_Navi.Values) Draw_Navi_GL(navi);
            

            Draw_Projectiles_GL(); 
            if (Show_CD == true)
                Draw_CollisionMap();

            NaviGL.glControl1.SwapBuffers();
        }

        public void Draw_Navi_GL(NetNavi_Type Navi)
        {
            PointF S = ScreenScroll;
            Reset_Matrix_GL();

            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.Scale(1f / (Navi.SpriteSheet.Width / Navi.SpriteSize.X),
                     1f / (Navi.SpriteSheet.Height / Navi.SpriteSize.Y), 1);
            GL.Translate(Navi.Sprite.X, Navi.Sprite.Y, 0);            
            GL.BindTexture(TextureTarget.Texture2D, GLNaviTexture[(int)Navi.GLSpriteSheetName]);
            
            RectangleF r = new RectangleF();
            r.X = ((Navi.Location_Last.X + Navi.Location.X) / 2f) - S.X;
            r.Y = ((Navi.Location_Last.Y + Navi.Location.Y) / 2f) - S.Y;
            r.Width = Navi.SpriteSize.X * Navi.Scale;
            r.Height = Navi.SpriteSize.Y * Navi.Scale;

            if (Navi.FaceLeft == true)
                Draw_Sprite(r, true);
            else
                Draw_Sprite(r);

        }


        public void Draw_CollisionMap()
        {
            RectangleF r;
            GL.Disable(EnableCap.Texture2D);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            Reset_Matrix_GL();
            GL.Color4(1f, 0f, 0f, 1f);
            foreach (KeyValuePair<Point,StageCollisionTile> tile in stage.CollisionMap)
            {
                if (tile.Value.Active)
                    GL.Color4(1f, 0f, 0f, 1f);
                else
                    GL.Color4(0f, 1f, 0f, 1f);

                r = new RectangleF(tile.Key.X * 16 - ScreenScroll.X, tile.Key.Y * 16 - ScreenScroll.Y, 16, 16);                
                GL.Begin(PrimitiveType.Quads);
                //Top left, Top right, Bottom right, Bottom left                
                GL.Vertex2(r.X, r.Y + 16 - tile.Value.HeightLeft);             
                GL.Vertex2(r.Right, r.Y + 16- tile.Value.HeightRight);
                GL.Vertex2(r.Right, r.Bottom);
                GL.Vertex2(r.X, r.Bottom);                                                               
                GL.End();
            }


            r = Host_Navi.Navi_Location();
            r.X -= ScreenScroll.X;
            r.Y -= ScreenScroll.Y;

            GL.Begin(PrimitiveType.Quads);
            //Top left, Top right, Bottom right, Bottom left                
            GL.Vertex2(r.X, r.Y);
            GL.Vertex2(r.Right, r.Y);
            GL.Vertex2(r.Right, r.Bottom);
            GL.Vertex2(r.X, r.Bottom);
            GL.End();

            GL.Enable(EnableCap.Texture2D);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        public void Draw_Background_GL(StageBackground BG)
        {
            Reset_Matrix_GL();
            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.BindTexture(TextureTarget.Texture2D, GLBackground[BG.SpriteInt]);
            PointF S = new PointF(ScreenScroll.X, ScreenScroll.Y);
            RectangleF r = new RectangleF(BG.Bounds.X - S.X * BG.Parallax, BG.Bounds.Y - S.Y * BG.Parallax, BG.Bounds.Width, BG.Bounds.Height);
            Draw_Sprite(r);            
        }

        public void Draw_Projectiles_GL()
        {
            Reset_Matrix_GL();
            //Draw Projectiles
            GL.BindTexture(TextureTarget.Texture2D, GLItemTexture[(int)GLItemTextureName.BasicShot]);
            PointF S = ScreenScroll;
            foreach (Navi_Main.Projectiles_Type item in Projectile_List)
            {
                GL.Color4(item.Color.R, item.Color.G, item.Color.B, item.Color.A);
                Draw_Sprite(new RectangleF(item.Location.X - S.X, item.Location.Y - S.Y, 8f * (float)item.Scale, 6f * (float)item.Scale));                
            }
        }

        public void Reset_Matrix_GL()
        {
            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Scale(ScreenZoom, ScreenZoom, 1);
        }

        public void Draw_Sprite(RectangleF r, bool flip = false)
        {            
            if (!flip)
            {
                GL.Begin(PrimitiveType.Quads);
                //Top left, Top right, Bottom right, Bottom left
                GL.TexCoord2(0f, 0f); GL.Vertex2(r.X, r.Y);
                GL.TexCoord2(1f, 0f); GL.Vertex2(r.Right, r.Y);
                GL.TexCoord2(1f, 1f); GL.Vertex2(r.Right, r.Bottom);
                GL.TexCoord2(0f, 1f); GL.Vertex2(r.X, r.Bottom);
                GL.End();
            }
            else
            {
                GL.Begin(PrimitiveType.Quads);
                //Top left, Top right, Bottom right, Bottom left
                GL.TexCoord2(1f, 0f); GL.Vertex2(r.X, r.Y);
                GL.TexCoord2(0f, 0f); GL.Vertex2(r.Right, r.Y);
                GL.TexCoord2(0f, 1f); GL.Vertex2(r.Right, r.Bottom);
                GL.TexCoord2(1f, 1f); GL.Vertex2(r.X, r.Bottom);
                GL.End();
            }
        }    

        /// <summary>
        ///  OpenGL Start Device
        /// </summary>
        public void Start_GL()
        {

            NaviGL = new NaviFXF();
            NaviGL.KeyDown += NaviGL_KeyDown;
            NaviGL.KeyUp += NaviGL_KeyUp;
            NaviGL.LostFocus += NaviGL_LostFocus;
            NaviGL.Disposed += NaviGL_Disposed;            
            NaviGL.Show();
            NaviGL.Location = new Point(0, 0);
            NaviGL.Width = Screen.PrimaryScreen.WorkingArea.Width;
            NaviGL.Height = Screen.PrimaryScreen.WorkingArea.Height;
            GLControl control = new GLControl(new GraphicsMode(32, 24, 8, 0), 3, 0, GraphicsContextFlags.Default);
            NaviGL.glControl1.Width = Screen.PrimaryScreen.WorkingArea.Width;
            NaviGL.glControl1.Height = Screen.PrimaryScreen.WorkingArea.Height;
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            ScreenSize.Width = Screen.PrimaryScreen.WorkingArea.Width;
            ScreenSize.Height = Screen.PrimaryScreen.WorkingArea.Height;
            GL.Viewport(0, 0, Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
            GL.Ortho(0, Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height, 0, -1, 1);
            Load_Sprite_Sheets();            

            
            NaviForm.Hide();
            Init_GL = true;
        }

        public void Load_Sprite_Sheets()
        {
            //Load Navi Sprite Sheet            
            GLNaviTexture.Add((int)GLNaviSpriteName.Raven, load_sprite(Resource1.Raven));
            GLNaviTexture.Add((int)GLNaviSpriteName.Vex, load_sprite(Resource1.Vex));
            GLNaviTexture.Add((int)GLNaviSpriteName.Barabus, load_sprite(Resource1.Barnabus));
            GLNaviTexture.Add((int)GLNaviSpriteName.Rebel, load_sprite(Resource1.Rebelpullsheet));
            GLNaviTexture.Add((int)GLNaviSpriteName.Junker, load_sprite(Resource1.Junker));
            GLNaviTexture.Add((int)GLNaviSpriteName.Zen, load_sprite(Resource1.Zen));

            //Load projectiles
            GLItemTexture[(int)GLItemTextureName.BasicShot] = load_sprite(Net_Navis.Resource1.Shot2);

            //Load backgrounds            
            GLBackground[(int)GLBGTextureName.LobbyBG1] = load_sprite(Net_Navis.Resource1.LobbyBG1);
            GLBackground[(int)GLBGTextureName.LobbyFG1] = load_sprite(Net_Navis.Resource1.LobbyFG1);

            GLBackground[(int)GLBGTextureName.HyruleFG] = load_sprite(Net_Navis.Resource1.HyruleFG);
            GLBackground[(int)GLBGTextureName.HyruleBG] = load_sprite(Net_Navis.Resource1.HyruleBG);
            //GLBackground = load_sprite(Net_Navis.Resource1.BG1);
        }
        //Load Sprite From Bitmap
        private int load_sprite(Bitmap bitmap)
        {
            int id;
            GL.GenTextures(1, out id);
            bitmap.MakeTransparent(Color.LimeGreen);
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);
            return id;
        }        

        #endregion

    }

    public enum GLNaviSpriteName
    {
        Junker,
        Raven,
        Vex,
        Rebel,
        Barabus,
        Zen
    }

    public enum GLItemTextureName
    {
        BasicShot,
        Portal1,        
    }



    public enum GLBGTextureName
    {        
        LobbyBG1,
        LobbyFG1,
        HyruleBG,
        HyruleFG
    }
}
