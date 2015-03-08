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
        public int GLProjectileTexture;
        public int GLBackground;
        public bool Init_GL;
        public bool GLOn;


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
                Draw_GL();
                ScreenScroll.X = Screen.PrimaryScreen.WorkingArea.Width / 2 - (int)Host_Navi.Location.X - (int)Host_Navi.GetSize().X / 2;
                ScreenScroll.Y = Screen.PrimaryScreen.WorkingArea.Height / 2 - (int)Host_Navi.Location.Y - (int)Host_Navi.GetSize().Y / 2;
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
            GL.ClearColor(Color.Black);
            GL.Color4(1f, 1f, 1f, 1f);
            Draw_Background_GL();

            //Draw host navi
            Draw_Navi_GL(Host_Navi);

            //Draw all client navis
            foreach (NetNavi_Type navi in Client_Navi.Values) Draw_Navi_GL(navi);
            GL.LoadIdentity();

            //Draw Projectiles
            GL.BindTexture(TextureTarget.Texture2D, GLProjectileTexture);
            Point S = ScreenScroll;            
            foreach (Navi_Main.Projectiles_Type item in Projectile_List)
            {
                GL.Color4(1f, 1f, 1f, 1f);
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0f, 0f); GL.Vertex2(item.Location.X + S.X, item.Location.Y + S.Y);
                GL.TexCoord2(1f, 0f); GL.Vertex2(item.Location.X + S.X + 8 * item.Scale, item.Location.Y + S.Y);
                GL.TexCoord2(1f, 1f); GL.Vertex2(item.Location.X + S.X + 8 * item.Scale, item.Location.Y + S.Y + 6 * item.Scale);
                GL.TexCoord2(0f, 1f); GL.Vertex2(item.Location.X + S.X, item.Location.Y + S.Y + 6 * item.Scale);
                GL.End();
            }
            NaviGL.glControl1.SwapBuffers();
        }
        public void Draw_Navi_GL(NetNavi_Type Navi)
        {
            Point S = ScreenScroll;
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.Scale(1f / (Navi.SpriteSheet.Width / Navi.SpriteSize.X),
                     1f / (Navi.SpriteSheet.Height / Navi.SpriteSize.Y), 1);
            GL.Translate(Navi.Sprite.X, Navi.Sprite.Y, 0);
            GL.BindTexture(TextureTarget.Texture2D, GLNaviTexture[Navi.GLSpriteSheetName]);
            if (Navi.FaceLeft == true)
            {
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(1f, 0f); GL.Vertex2(Navi.Location.X + S.X, Navi.Location.Y + S.Y);
                GL.TexCoord2(0f, 0f); GL.Vertex2(Navi.Location.X + S.X + Navi.SpriteSize.X * Navi.Scale, Navi.Location.Y + S.Y);
                GL.TexCoord2(0f, 1f); GL.Vertex2(Navi.Location.X + S.X + Navi.SpriteSize.X * Navi.Scale, Navi.Location.Y + S.Y + Navi.SpriteSize.Y * Navi.Scale);
                GL.TexCoord2(1f, 1f); GL.Vertex2(Navi.Location.X + S.X, Navi.Location.Y + S.Y + Navi.SpriteSize.Y * Navi.Scale);
                GL.End();
            }
            else
            {
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0f, 0f); GL.Vertex2(Navi.Location.X + S.X, Navi.Location.Y + S.Y);
                GL.TexCoord2(1f, 0f); GL.Vertex2(Navi.Location.X + S.X + Navi.SpriteSize.X * Navi.Scale, Navi.Location.Y + S.Y);
                GL.TexCoord2(1f, 1f); GL.Vertex2(Navi.Location.X + S.X + Navi.SpriteSize.X * Navi.Scale, Navi.Location.Y + S.Y + Navi.SpriteSize.Y * Navi.Scale);
                GL.TexCoord2(0f, 1f); GL.Vertex2(Navi.Location.X + S.X, Navi.Location.Y + S.Y + Navi.SpriteSize.Y * Navi.Scale);
                GL.End();
            }
            GL.LoadIdentity();
        }


        public void Draw_Background_GL()
        {
            Point S = ScreenScroll;
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.BindTexture(TextureTarget.Texture2D, GLBackground);

            for (int x = 0; x <= 9; x++)
            {
                Point pos = new Point();
                int Scale = 6;
                pos.X = x * 256 * Scale + S.X;
                pos.Y = 0 + S.Y;
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0f, 0f); GL.Vertex2(pos.X, pos.Y);
                GL.TexCoord2(1f, 0f); GL.Vertex2(pos.X + 256 * Scale, pos.Y);
                GL.TexCoord2(1f, 1f); GL.Vertex2(pos.X + 256 * Scale, pos.Y + 256 * Scale);
                GL.TexCoord2(0f, 1f); GL.Vertex2(pos.X, pos.Y + 256 * Scale);
                GL.End();            
            }
            
            
            GL.LoadIdentity();
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
            GL.Viewport(0, 0, Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
            GL.Ortho(0, Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height, 0, -1, 1);
            Load_Sprite_Sheets();            
            NaviForm.Hide();
            Init_GL = true;            
        }

        public void Load_Sprite_Sheets()
        {
            //Load Navi Sprite Sheet            
            GLNaviTexture.Add(0, load_sprite(Resource1.Raven));
            GLNaviTexture.Add(1, load_sprite(Resource1.Vex));
            GLNaviTexture.Add(2, load_sprite(Resource1.Barnabus));
            GLNaviTexture.Add(3, load_sprite(Resource1.Rebelpullsheet));

            //Load projectiles
            GLProjectileTexture = load_sprite(Net_Navis.Resource1.Shot2);

            //Load backgrounds            
            GLBackground = load_sprite(Net_Navis.Resource1.BG1);
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
}
