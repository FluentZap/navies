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
	//Main Sub's go into Navi Main
	//Navi Resources is for any Navi specific Data
	//Host app calls Initialise then runs DoEvents
	//DXon if switched on changes rendering to a directx window rendering is then done in Draw_DX

	public partial class Navi_Main
	{

        //private NaviFormF NaviFormEvents;
        
        private NaviFormF NaviForm;                 
		private MenuForm MenuForm;

        private NaviFXF NaviGL;        
		
		//private NaviTrayIcon NaviTray;

        private HashSet<System.Windows.Forms.Keys> pressedkeys = new HashSet<System.Windows.Forms.Keys>();

        public IGraphicsContext GLC;
        //public IGraphicsContext GLC;
        
        //DirectX                
        //Device DXDevice;
		//PresentParameters DXPP;
		//Sprite DXSprite;
		public int GLNaviTexture;
        
        
		//Texture[] DXProjectileTexture;
		bool Init_GL;
			// = True
		bool GLOn;


		System.Drawing.Imaging.ImageAttributes NormalImage = new System.Drawing.Imaging.ImageAttributes();
		System.Drawing.Imaging.ImageAttributes BlueImage = new System.Drawing.Imaging.ImageAttributes();
		System.Drawing.Imaging.ImageAttributes RedImage = new System.Drawing.Imaging.ImageAttributes();

		System.Drawing.Imaging.ImageAttributes GreenImage = new System.Drawing.Imaging.ImageAttributes();
        private PointF Gravity = new PointF(0f, 0.5f);
        private PointF AirFriction = new PointF(0.01f, 0.01f);

        private PointF GroundFriction = new PointF(0.15f, 0f);
        private NetNavi_Type Host_Navi;

        private NetNavi_Type Client_Navi;

		bool Direct_Control = true;

		private double Physics_Timer;
		private double Physics_Rate;

		private long Program_Step;

        private class Projectiles_Type
		{
			public PointF Location;
			public PointF Speed;

			public int Life;
			public Projectiles_Type(Point Location, PointF Speed, int Life)
			{
				this.Location = Location;
				this.Speed = Speed;
				this.Life = Life;
			}

		}


        private HashSet<Projectiles_Type> Projectile_List = new HashSet<Projectiles_Type>();

        public Navi_Main(string Navi_Name, long NaviID)
		{
			Host_Navi = Navi_resources.Get_Data(Navi_Name, NaviID);
            

		}



        public void Initialise()
		{
			//Create and show forms
			NaviForm = new NaviFormF();            
            MenuForm = new MenuForm();
			NaviForm.Show();
			NaviForm.TopMost = true;
			NaviForm.Width = Convert.ToInt32(Host_Navi.GetSize().X);
			NaviForm.Height = Convert.ToInt32(Host_Navi.GetSize().Y);
			NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X);
			NaviForm.Top = Convert.ToInt32(Host_Navi.Location.Y);


            NaviForm.Paint += Navi_Redraw;
            NaviForm.KeyDown += NaviForm_KeyDown;
            NaviForm.KeyUp += NaviForm_KeyUp;
            NaviForm.GotFocus += NaviForm_GotFocus;
            NaviForm.LostFocus += NaviForm_LostFocus;
            NaviForm.Disposed += NaviForm_Disposed;            


            //NaviGL.KeyDown += NaviDX_KeyDown;
            //NaviGL.KeyUp += NaviDX_KeyUp;
            //NaviGL.LostFocus += NaviDX_LostFocus;
            //NaviGL.Disposed += NaviDX_Disposed;
		

			Host_Navi.Get_Compact_buffer();

			//Initialise_Network()

			//Initialise defaults
			//NaviTray = New NaviTrayIcon
			//NaviTray.Initialise(Host_Navi)
			Set_color_filters();

			Physics_Timer = DateTime.Now.TimeOfDay.TotalSeconds;
			
			Physics_Rate = 1f / 60f;
			Program_Step = 0;

			//Host_Navi.set_Animation(Animation_Name_Enum.None)

			Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize().Y;
			Host_Navi.Location.X = 1000;

			Host_Navi.Scale = 3;

		}


		public void DoEvents()
		{
			//Slowing program down
			System.Threading.Thread.Sleep(Convert.ToInt32(Physics_Rate * 1000));

			if (Physics_Timer <= DateTime.Now.TimeOfDay.TotalSeconds) {
				Handle_UI();
				Process_Navi_Commands();
				Update_Physics();
				Navi_resources.Set_Correct_Animation(ref Host_Navi);
				Host_Navi.Update_Sprite();
				//DoNetworkEvents()

				Physics_Timer = DateTime.Now.TimeOfDay.TotalSeconds + Physics_Rate;
				Program_Step += 1;
			}

			Draw_All();
		}


		public void Handle_UI()
		{
			if (pressedkeys.Contains(Keys.W)) {
				Host_Navi.Scale += 0.5f;
				Host_Navi.OldSprite = new Point(500, 500);
				if (Host_Navi.Scale < 0.5f)
					Host_Navi.Scale = 0.5f;
			}

			if (pressedkeys.Contains(Keys.S)) {
				Host_Navi.Scale -= 0.5f;
				Host_Navi.OldSprite = new Point(500, 500);
				if (Host_Navi.Scale < 0.5f)
					Host_Navi.Scale = 0.5f;
			}


			if (pressedkeys.Contains(Keys.A)) {
				Host_Navi.FaceLeft = true;
				Host_Navi.Running = true;
				if (pressedkeys.Contains(Keys.ShiftKey)){Host_Navi.Dashing = true;Host_Navi.HasDashed = true;}
				else
					Host_Navi.Dashing = false;
			}

			if (pressedkeys.Contains(Keys.D)) {
				Host_Navi.FaceLeft = false;
				Host_Navi.Running = true;
				if (pressedkeys.Contains(Keys.ShiftKey)){Host_Navi.Dashing = true;Host_Navi.HasDashed = true;}
				else
					Host_Navi.Dashing = false;
			}

			if (!pressedkeys.Contains(Keys.ShiftKey))
				Host_Navi.Dashing = false;

			if (!pressedkeys.Contains(Keys.D) && !pressedkeys.Contains(Keys.A)) {
				Host_Navi.Running = false;
			}


			if (pressedkeys.Contains(Keys.Space)) {
				if (Host_Navi.OnGround == true) {
					Host_Navi.Jumping = true;
					Host_Navi.HasJumped = true;
				}
			}

			if (pressedkeys.Contains(Keys.Tab))
				if (GLOn == false)
					GLOn = true;
				else
					NaviGL.Dispose();


			if (pressedkeys.Contains(Keys.OemQuestion)) {
				if (Host_Navi.FaceLeft == true) {
					Projectile_List.Add(new Projectiles_Type(new Point(Convert.ToInt32(Host_Navi.Navi_Location().Left), Convert.ToInt32(Host_Navi.Navi_Location().Top)), new PointF(-20, 0), 600));
				} else {
					Projectile_List.Add(new Projectiles_Type(new Point(Convert.ToInt32(Host_Navi.Navi_Location().Right), Convert.ToInt32(Host_Navi.Navi_Location().Top)), new PointF(20, 0), 600));
				}

				pressedkeys.Remove(Keys.OemQuestion);
			}


		}

		public void Draw_All()
		{
			if (GLOn == false) {
				if (!(Host_Navi.Sprite == Host_Navi.OldSprite) || !(Host_Navi.FaceLeft == Host_Navi.OldFaceLeft)) {
					NaviForm.Invalidate();
					Host_Navi.OldSprite = Host_Navi.Sprite;
					Host_Navi.OldFaceLeft = Host_Navi.FaceLeft;
				}
			}

			if (GLOn == true) {
                
                if (Init_GL == false)
                {
                    Start_GL();                
                }
                Draw_DX();
			}

		}
        
		public void Process_Navi_Commands()
		{
			if (Direct_Control == false) {
				Host_Navi.Running = false;
				if (Host_Navi.Navi_Location().Right <= Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox().Width) {
					Host_Navi.FaceLeft = false;
					Host_Navi.Running = true;
				}
				if (Host_Navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox().Width) {
					Host_Navi.FaceLeft = true;
				}
			}



			//Move Navies
			if (Host_Navi.OnGround == true) {
				if (Host_Navi.Running == true) {
					//Check for dashing
					if (Host_Navi.Dashing == true) {
						//Dashing
						if (Host_Navi.FaceLeft == true)
							Host_Navi.Speed.X -= Host_Navi.DashSpeed;
						else
							Host_Navi.Speed.X += Host_Navi.DashSpeed;
					} else {
						//Running
						if (Host_Navi.FaceLeft == true)
							Host_Navi.Speed.X -= Host_Navi.GroundSpeed;
						else
							Host_Navi.Speed.X += Host_Navi.GroundSpeed;
					}
				}
				//Jumping
				if (Host_Navi.Jumping == true && Host_Navi.HasJumped == true){Host_Navi.Speed.Y -= Host_Navi.Acrobatics;Host_Navi.HasJumped = false;}


			} else {
				//Air moving
				if (Host_Navi.Running == true) {
					if (Host_Navi.FaceLeft == true)
						Host_Navi.Speed.X -= Host_Navi.AirSpeed;
					else
						Host_Navi.Speed.X += Host_Navi.AirSpeed;
				}
			}


		}


		public void Update_Physics()
		{
			HashSet<Projectiles_Type> Item_Remove_List = new HashSet<Projectiles_Type>();
			foreach (Navi_Main.Projectiles_Type item in Projectile_List) {				
				item.Location.X += item.Speed.X;
				item.Location.Y += item.Speed.Y;
				item.Life -= 1;
				if (item.Life <= 0)
					Item_Remove_List.Add(item);
			}

			foreach (Navi_Main.Projectiles_Type item in Item_Remove_List) {				
				Projectile_List.Remove(item);
			}


			//Friction
			if (Host_Navi.OnGround == true) {
				Host_Navi.Speed.X -= Host_Navi.Speed.X * GroundFriction.X;
				Host_Navi.Speed.Y -= Host_Navi.Speed.Y * GroundFriction.Y;
			} else {
				Host_Navi.Speed.X -= Host_Navi.Speed.X * AirFriction.X;
				Host_Navi.Speed.Y -= Host_Navi.Speed.Y * AirFriction.Y;
			}

			//Gravity
			if (Host_Navi.OnGround == false)
				Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y;
			//Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y

			Host_Navi.Location.X = Host_Navi.Location.X + Host_Navi.Speed.X * Host_Navi.Scale;
			Host_Navi.Location.Y = Host_Navi.Location.Y + Host_Navi.Speed.Y * Host_Navi.Scale;

			//Bounds
			if (Host_Navi.FaceLeft == true) {
				if (Host_Navi.Navi_Location().Left <= Screen.PrimaryScreen.WorkingArea.Left){Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - (Host_Navi.GetSize().X - Host_Navi.GetHitBox().Right);Host_Navi.Speed.X = 0;}
			} else {
				if (Host_Navi.Navi_Location().Left <= Screen.PrimaryScreen.WorkingArea.Left){Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - Host_Navi.GetHitBox().Left;Host_Navi.Speed.X = 0;}
			}

			if (Host_Navi.FaceLeft == true) {
				if (Host_Navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right){Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - (Host_Navi.GetSize().X - Host_Navi.GetHitBox().Left);Host_Navi.Speed.X = 0;}
			} else {
				if (Host_Navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right){Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox().Right;Host_Navi.Speed.X = 0;}
			}

			if (Host_Navi.Navi_Location().Bottom > Screen.PrimaryScreen.WorkingArea.Bottom)
				Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetHitBox().Bottom;
			if (Host_Navi.Navi_Location().Bottom == Screen.PrimaryScreen.WorkingArea.Bottom){Host_Navi.OnGround = true;Host_Navi.Speed.Y = 0;}
			else
				Host_Navi.OnGround = false;

			//Update Location
			NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X);
			NaviForm.Top = Convert.ToInt32(Host_Navi.Location.Y);

			if (IsClient == true)
				NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X) + 32;
		}

		public void Navi_Redraw(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			NaviForm.Width = Convert.ToInt32(Host_Navi.GetSize().X);
			NaviForm.Height = Convert.ToInt32(Host_Navi.GetSize().Y);
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

			float xoff = 0;
			float yoff = 0;
			xoff = Convert.ToSingle(-0.5 + 0.5 * Host_Navi.SpriteSize.X / Host_Navi.GetSize().X) + Host_Navi.SpriteSize.X * Host_Navi.Sprite.X;
			yoff = Convert.ToSingle(-0.5 + 0.5 * Host_Navi.SpriteSize.Y / Host_Navi.GetSize().Y) + Host_Navi.SpriteSize.Y * Host_Navi.Sprite.Y;

			if (Host_Navi.FaceLeft == true) {
				e.Graphics.DrawImage(Host_Navi.SpriteSheet, new Rectangle(Convert.ToInt32(Host_Navi.GetSize().X), 0, Convert.ToInt32(-Host_Navi.GetSize().X - 1), Convert.ToInt32(Host_Navi.GetSize().Y)), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage);
			} else {
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

		public void Draw_DX()
		{            
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(Color.PaleVioletRed);



            int xoff = 0;
            int yoff = 0;
            xoff = Host_Navi.SpriteSize.X * Host_Navi.Sprite.X;
            yoff = Host_Navi.SpriteSize.Y * Host_Navi.Sprite.Y;




            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.BindTexture(TextureTarget.Texture2D, GLNaviTexture);

            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(Host_Navi.Location.X, Host_Navi.Location.Y);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(0.6f, -0.4f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(0.6f, 0.4f);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-0.6f, 0.4f);

            GL.End();












            
			//DXSprite.Draw(DXNaviTexture(0), New Rectangle(xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y), Vector3.Empty, New Vector3(Host_Navi.Location.X, Host_Navi.Location.Y, 0), Color.White)
            
			if (Host_Navi.FaceLeft == true) {
				//DXSprite.Transform = Matrix.Transformation2D(new Vector2(0, 0), 0, new Vector2(-Host_Navi.Scale, Host_Navi.Scale), new Vector2(0, 0), 0, new Vector2(Host_Navi.Location.X + (Host_Navi.SpriteSize.X * Host_Navi.Scale), Host_Navi.Location.Y));
				//DXSprite.Draw(DXNaviTexture[0], new Rectangle(xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y), Vector3.Empty, new Vector3(0, 0, 0), Color.White);
			} else {
				//DXSprite.Transform = Matrix.Transformation2D(new Vector2(0, 0), 0, new Vector2(Host_Navi.Scale, Host_Navi.Scale), new Vector2(0, 0), 0, new Vector2(Host_Navi.Location.X, Host_Navi.Location.Y));
				//DXSprite.Draw(DXNaviTexture[0], new Rectangle(xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y), Vector3.Empty, new Vector3(0, 0, 0), Color.White);
			}

			foreach (Navi_Main.Projectiles_Type item in Projectile_List) {				
				//DXSprite.Transform = Matrix.Transformation2D(new Vector2(0, 0), 0, new Vector2(3, 3), new Vector2(0, 0), 0, new Vector2(item.Location.X, item.Location.Y));
				//DXSprite.Draw(DXProjectileTexture[0], new Rectangle(0, 0, 8, 6), Vector3.Empty, new Vector3(0, 0, 0), Color.White);
			}

			//DXSprite.Transform = Matrix.Identity;
			//DXSprite.Draw(DXProjectileTexture[0], new Rectangle(0, 0, 8, 6), Vector3.Empty, new Vector3(0, 0, 0), Color.White);

			//DXSprite.End();

			//DXDevice.EndScene();            
            GraphicsContext.CurrentContext.SwapBuffers(); //Swaps Front and back buffers
		}

        /// <summary>
        ///  OpenGL Start Device
        /// </summary>
        public void Start_GL()
        {
            NaviGL = new NaviFXF();
            NaviGL.KeyDown += NaviDX_KeyDown;
            NaviGL.KeyUp += NaviDX_KeyUp;
            NaviGL.LostFocus += NaviDX_LostFocus;
            NaviGL.Disposed += NaviDX_Disposed;
            NaviGL.Show();
            NaviGL.Width = Screen.PrimaryScreen.WorkingArea.Width;
            NaviGL.Height = Screen.PrimaryScreen.WorkingArea.Height;            
            OpenTK.Platform.IWindowInfo wi = null;
            wi = OpenTK.Platform.Utilities.CreateWindowsWindowInfo(NaviGL.Handle);
            IGraphicsContext GLC = new GraphicsContext(GraphicsMode.Default, wi);            
            GLC.LoadAll();


            GL.ClearColor(Color.PaleVioletRed);
            GL.Enable(EnableCap.Texture2D);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.GenTextures(1, out GLNaviTexture);
            GL.BindTexture(TextureTarget.Texture2D, GLNaviTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);



            
            BitmapData data = Host_Navi.SpriteSheet.LockBits(new System.Drawing.Rectangle(0, 0, Host_Navi.SpriteSheet.Width, Host_Navi.SpriteSheet.Height), 
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            Host_Navi.SpriteSheet.UnlockBits(data);


            //GL.Viewport(0, 0, 1920, 1080);

            //GL.MatrixMode(MatrixMode.Projection);
            //GL.LoadIdentity();
            //GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);




            //NaviDX.Location = New Point(0, Screen.PrimaryScreen.Bounds.Height - NaviDX.Height)
            //NaviForm.Hide();
            
            //DXNaviTexture = new Texture[11];
            //My.Resources.Raven.MakeTransparent(Color.FromArgb(255, 0, 255, 0))
            //DXNaviTexture[0] = new Texture(DXDevice, Host_Navi.SpriteSheet, Usage.None, Pool.Managed);
            

            //DXProjectileTexture = new Texture[11];
            //DXProjectileTexture[0] = new Texture(DXDevice, Net_Navis.Resource1.Shot2, Usage.None, Pool.Managed);

            Init_GL = true;
        }


		private void NaviForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (pressedkeys.Contains(e.KeyCode)) {
			} else {
				pressedkeys.Add(e.KeyCode);
			}
		}

		private void NaviForm_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (pressedkeys.Contains(e.KeyCode)) {
				pressedkeys.Remove(e.KeyCode);
			}
		}

		private void NaviForm_GotFocus(object sender, System.EventArgs e)
		{
			Direct_Control = true;
		}

		private void NaviForm_LostFocus(object sender, System.EventArgs e)
		{
			if (GLOn == false)
				Direct_Control = false;
			pressedkeys.Clear();
		}

		private void NaviForm_Disposed(object sender, System.EventArgs e)
		{
			//NaviTray.tray.Visible = True
		}


		private void NaviDX_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (pressedkeys.Contains(e.KeyCode)) {
			} else {
				pressedkeys.Add(e.KeyCode);
			}
		}

		private void NaviDX_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (pressedkeys.Contains(e.KeyCode)) {
				pressedkeys.Remove(e.KeyCode);
			}
		}

		private void NaviDX_LostFocus(object sender, System.EventArgs e)
		{
			pressedkeys.Clear();
		}

		private void NaviDX_Disposed(object sender, System.EventArgs e)
		{
			GLOn = false;
			Init_GL = false;
            GL.DeleteTextures(1, ref GLNaviTexture);
			//DXDevice = null;
			//DXSprite = null;
			//DXNaviTexture = null;
			//DXPP = null;
			//NaviForm.Show();
		}



	}

}		