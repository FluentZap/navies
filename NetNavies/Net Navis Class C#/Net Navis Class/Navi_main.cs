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
using System.Threading;

namespace Net_Navis
{
    //Main Sub's go into Navi Main
    //Navi Resources is for any Navi specific Data
    //Host app calls Initialise then runs DoEvents
    //DXon if switched on changes rendering to a directx window rendering is then done in Draw_DX

    public partial class Navi_Main
    {
        public const int COMPACT_BUFFER_SIZE = 100;

        private NaviFormF NaviForm;
        private MenuForm MenuForm;
        private NaviFXF NaviGL;
        //private NaviTrayIcon NaviTray;
        private HashSet<System.Windows.Forms.Keys> pressedkeys = new HashSet<System.Windows.Forms.Keys>();
        private HashSet<System.Windows.Forms.Keys> prevPressedkeys = new HashSet<System.Windows.Forms.Keys>();
        System.Drawing.Imaging.ImageAttributes NormalImage = new System.Drawing.Imaging.ImageAttributes();
        System.Drawing.Imaging.ImageAttributes BlueImage = new System.Drawing.Imaging.ImageAttributes();
        System.Drawing.Imaging.ImageAttributes RedImage = new System.Drawing.Imaging.ImageAttributes();
        System.Drawing.Imaging.ImageAttributes GreenImage = new System.Drawing.Imaging.ImageAttributes();


        //private PointF Gravity = new PointF(0.0f, 0.5f);
        //private PointF AirFriction = new PointF(0.01f, 0.01f);
        //private PointF GroundFriction = new PointF(0.15f, 0f);
        private Size ScreenSize;
        private PointF ScreenScroll;
        private float ScreenZoom;
        private Stage stage;

        public NetNavi_Type Host_Navi;
        public Dictionary<String, NetNavi_Type> Client_Navi;
        private Navi_Network_TCP Net;


        bool Direct_Control = false;

        bool OnDesktop = true;

        private double Physics_Rate;
        private double Render_Rate;
        private bool Advance_Physics;


        private bool Show_CD = false;

        private PerformanceTimer Physics_Timer = new PerformanceTimer(60.0);

        private PerformanceTimer Render_Timer = new PerformanceTimer(60.0);

        //public ulong Program_Step;

        private class Projectiles_Type
        {
            public PointF Location;
            public String Owner;
            public PointF Speed;
            public double Scale;
            public int Life;
            public Color4 Color;
            public Projectiles_Type(Point Location, PointF Speed, int Life, double Scale, Color4 Color, string Owner)
            {
                this.Location = Location;
                this.Speed = Speed;
                this.Life = Life;
                this.Scale = Scale;
                this.Color = Color;
                this.Owner = Owner;
            }

        }


        private HashSet<Projectiles_Type> Projectile_List = new HashSet<Projectiles_Type>();

        public Navi_Main(int Navi_Name_ID, ulong NAVIEXEID)
        {
            Host_Navi = Navi_resources.Get_Data((Navi_Name_ID)Navi_Name_ID, NAVIEXEID);
        }

        public void Initialise()
        {
            //Create and show forms
            NaviForm = new NaviFormF();
            MenuForm = new MenuForm();
            NaviForm.TopMost = true;
            NaviForm.Width = Convert.ToInt32(Host_Navi.GetSize().X);
            NaviForm.Height = Convert.ToInt32(Host_Navi.GetSize().Y);
            NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X);
            NaviForm.Top = Convert.ToInt32(Host_Navi.Location.Y);


            NaviForm.Paint += Draw_Navi_GDI;
            NaviForm.KeyDown += NaviForm_KeyDown;
            NaviForm.KeyUp += NaviForm_KeyUp;
            NaviForm.GotFocus += NaviForm_GotFocus;
            NaviForm.LostFocus += NaviForm_LostFocus;
            NaviForm.Disposed += NaviForm_Disposed;
            //NaviFXF handels set in startGL
            Client_Navi = new Dictionary<String, NetNavi_Type>();

            //Initialise defaults
            //NaviTray = New NaviTrayIcon
            //NaviTray.Initialise(Host_Navi)
            Set_color_filters();
            Physics_Timer.Start();
            Physics_Rate = 1000 / 60.0;
            Render_Rate = 1000 / 60.0;
            Host_Navi.Program_Step = 0;
            //Host_Navi.set_Animation(Animation_Name_Enum.None);
            stage = new Stage(StageName.Lobby);
            ScreenZoom = 3.0f;

            Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize().Y;
            Host_Navi.Location.X = 0;
            Host_Navi.Scale = 2;
            NaviForm.Show();
            NaviForm.Visible = true;
            Net = new Navi_Network_TCP(this);

            //StartNetwork();
            
            //Thread t = new Thread(ConsoleInput);
            //t.IsBackground = true;
            //t.Start();
            
        }


        public bool DoEvents()
        {
            Handle_UI();
            Physics_Timer.Stop(); // doesn't actually stop the timer, just updates it
            if (Physics_Rate > Physics_Timer.ElapsedTime)
            Thread.Sleep((int)(Physics_Rate - Physics_Timer.ElapsedTime) + 1);            

            if (Physics_Timer.ElapsedTime > Physics_Rate)
            //if (Advance_Physics == true)
            {
                //Advance_Physics = false;
                if (!Net.NetworkHold)
                {
                    Random_Stuff();
                    Process_Navi_Commands();
                    Update_Physics();
                    Navi_resources.Set_Correct_Animation(ref Host_Navi);

                    Host_Navi.Update_Sprite();
                    Host_Navi.ShootCharge += 1;
                    Host_Navi.Program_Step++;
                    Physics_Timer.Start();
                }
                Net.NetworkHold = true;
                Net.DoNetworkEvents();
            }


            Render_Timer.Stop();
            if (Render_Timer.ElapsedTime > Render_Rate)
            {
                Draw_Navi();
                //Advance_Physics = true;
                Render_Timer.Start();
            }
            return Term;
        }

        public int nextaction;
        public int targetlocation;

        public void Random_Stuff()
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            nextaction--;
            if (nextaction <= 0)            
            {
                targetlocation = rnd.Next(Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox().Width);
                nextaction = rnd.Next(500, 1000);
            }            
        }
        public bool Term;



        public void Advance_Clients()
        {

            foreach (KeyValuePair<string, NetNavi_Type> navi in Client_Navi)
            {
                navi.Value.Process_Update();
                Process_Client_Commands(navi);
            }

        }


        public void Handle_UI()
        {

            if (pressedkeys.Contains(Keys.W))
            {
                //ScreenZoom += 0.0001f;
            }

            if (pressedkeys.Contains(Keys.S))
            {
                //ScreenZoom -= 0.0001f;
            }

            if (pressedkeys.Contains(Keys.A))
            {
                Host_Navi.FaceLeft = true;
                Host_Navi.Running = true;
                if (pressedkeys.Contains(Keys.ShiftKey)) { Host_Navi.Dashing = true; Host_Navi.HasDashed = true; }
                else
                    Host_Navi.Dashing = false;
            }

            if (pressedkeys.Contains(Keys.D))
            {
                Host_Navi.FaceLeft = false;
                Host_Navi.Running = true;
                if (pressedkeys.Contains(Keys.ShiftKey)) { Host_Navi.Dashing = true; Host_Navi.HasDashed = true; }
                else
                    Host_Navi.Dashing = false;
            }

            if (!pressedkeys.Contains(Keys.ShiftKey))
                Host_Navi.Dashing = false;

            if (!pressedkeys.Contains(Keys.D) && !pressedkeys.Contains(Keys.A))
            {
                Host_Navi.Running = false;
            }


            if (pressedkeys.Contains(Keys.Space))
            {
                if (Host_Navi.OnGround == true)
                {
                    Host_Navi.Jumping = true;
                    Host_Navi.HasJumped = true;
                }
            }

            if (pressedkeys.Contains(Keys.Tab))
            {
                pressedkeys.Remove(Keys.Tab);
                if (GLOn == false)
                {
                    GLOn = true;
                    OnDesktop = true;
                    Host_Navi.Location = stage.EntryPoint;
                    Host_Navi.Scale = 1F;
                }
                else
                {
                    OnDesktop = false;
                    NaviGL.Dispose();
                }
             }

            if (pressedkeys.Contains(Keys.OemQuestion))
                Host_Navi.Shooting = true;
            else
                Host_Navi.Shooting = false;
            
            //else if (pressedkeys.Contains(Keys.T) && !prevPressedkeys.Contains(Keys.T))
            //    StartNetwork("Jonny Flame");
            //else if (pressedkeys.Contains(Keys.Y) && !prevPressedkeys.Contains(Keys.Y))
            //    StartNetwork("Presto Pretzel", 11995);
            //else if (pressedkeys.Contains(Keys.U) && !prevPressedkeys.Contains(Keys.U))
            //    StartNetwork("Mechana Banana", 11996);
            //else if (pressedkeys.Contains(Keys.I) && !prevPressedkeys.Contains(Keys.I))
            //    StartNetwork("Rico Rico", 11997);
            //else if (pressedkeys.Contains(Keys.O) && !prevPressedkeys.Contains(Keys.O))
            //    StartNetwork("Chloe Lamb", 11998);
            //else if (pressedkeys.Contains(Keys.G) && !prevPressedkeys.Contains(Keys.G))
            //    ConnectToPeer("127.0.0.1", 11994);
            //else if (pressedkeys.Contains(Keys.H) && !prevPressedkeys.Contains(Keys.H))
            //    ConnectToPeer("127.0.0.1", 11995);
            //else if (pressedkeys.Contains(Keys.J) && !prevPressedkeys.Contains(Keys.J))
            //    ConnectToPeer("127.0.0.1", 11996);
            //else if (pressedkeys.Contains(Keys.K) && !prevPressedkeys.Contains(Keys.K))
            //    ConnectToPeer("127.0.0.1", 11997);
            //else if (pressedkeys.Contains(Keys.L) && !prevPressedkeys.Contains(Keys.L))
            //    ConnectToPeer("127.0.0.1", 11998);

            if (pressedkeys.Contains(Keys.Escape))
                Term = true;
                


            prevPressedkeys.Clear();
            foreach (System.Windows.Forms.Keys key in pressedkeys)
                prevPressedkeys.Add(key);
        }

        void ConsoleInput()
        {
            string[] command = { "" };

            while (command[0] != "exit" && command[0] != "quit")
            {
                command = Console.ReadLine().Split(' ');

                if (command.Length == 0)
                    continue;

                if (command[0] == "help")
                {
                    Console.WriteLine("\t\"start\"");
                    Console.WriteLine("\t\"stop\"");
                    Console.WriteLine("\t\"connect IP PORT\"");
                    Console.WriteLine("\t\"captain\"");
                    Console.WriteLine("\t\"peers\"");
                    Console.WriteLine("\t\"name\"");
                    Console.WriteLine("\t\"step\"");
                    Console.WriteLine("\t\"scd\"");
                }
                else if (command[0] == "connect")
                {
                    if (command[1] == "home")
                        command[1] = "127.0.0.1";
                    Net.ConnectToPeer(command[1], Convert.ToInt32(command[2]));
                }
                else if (command[0] == "start")
                    Net.StartNetwork();
                else if (command[0] == "stop")
                    Net.StopNetwork();
                else if (command[0] == "name")
                    Console.WriteLine(Net.name);
                else if (command[0] == "step")
                    Console.WriteLine(Host_Navi.Program_Step);
                else if (command[0] == "scd")
                    if (Show_CD)
                    { Show_CD = false; Console.WriteLine("Hideing collision detection"); }
                    else
                    { Show_CD = true; Console.WriteLine("Showing collision detection"); }
                else if (command[0] == "captain")
                {
                    if (Net.networkCaptain == null)
                        Console.WriteLine("me");
                    else
                        Console.WriteLine(Net.networkCaptain);
                }
                else if (command[0] == "peers")
                    foreach (string name in Net.peers.Keys)
                        Console.WriteLine(name);
            }
        }

        public void Process_Navi_Commands()
        {
            
            if (Direct_Control == false)
            {

                if (Math.Abs(targetlocation - Host_Navi.Navi_Location().Left) > 100)
                    Host_Navi.Running = true;
                else
                    Host_Navi.Running = false;

                if (Host_Navi.Navi_Location().Right <= targetlocation)
                {
                    Host_Navi.FaceLeft = false;                    
                }
                if (Host_Navi.Navi_Location().Left >= targetlocation)
                {
                    Host_Navi.FaceLeft = true;
                }
            }
            

            #region Movement
            //Move Navies
            if (Host_Navi.OnGround == true)
            {
                if (Host_Navi.Running == true)
                {
                    //Check for dashing
                    if (Host_Navi.Dashing == true)
                    {
                        //Dashing
                        if (Host_Navi.FaceLeft == true)
                            Host_Navi.Speed.X -= Host_Navi.DashSpeed;
                        else
                            Host_Navi.Speed.X += Host_Navi.DashSpeed;
                    }
                    else
                    {
                        //Running
                        if (Host_Navi.FaceLeft == true)
                            Host_Navi.Speed.X -= Host_Navi.GroundSpeed;
                        else
                            Host_Navi.Speed.X += Host_Navi.GroundSpeed;
                    }
                }
                //Jumping
                if (Host_Navi.Jumping == true && Host_Navi.HasJumped == true) { Host_Navi.Speed.Y -= Host_Navi.Acrobatics; Host_Navi.HasJumped = false; }


            }
            else
            {
                //Air moving
                if (Host_Navi.Running == true)
                {
                    if (Host_Navi.FaceLeft == true)
                        Host_Navi.Speed.X -= Host_Navi.AirSpeed;
                    else
                        Host_Navi.Speed.X += Host_Navi.AirSpeed;
                }
            }
            #endregion
            if (Host_Navi.Shooting == true)
                if (Host_Navi.ShootCharge > 20)
                {
                    Color4 color = new Color4();
                    Host_Navi.ShootCharge = 0;
                    Host_Navi.Shoot_Advance += 1;
                    if (Host_Navi.Shoot_Advance > 3) Host_Navi.Shoot_Advance = 1;
                    Host_Navi.Activated_Ability = Host_Navi.Shoot_Advance;

                    if (Host_Navi.Shoot_Advance == 1) { color.R = 255; color.G = 0; color.B = 0; color.A = 255; }
                    if (Host_Navi.Shoot_Advance == 2) { color.R = 0; color.G = 255; color.B = 0; color.A = 255; }
                    if (Host_Navi.Shoot_Advance == 3) { color.R = 0; color.G = 0; color.B = 255; color.A = 255; }

                    Point loc = new Point();
                    PointF Shoot_Point;
                    PointF Speed = new PointF();
                    Shoot_Point = Host_Navi.Get_Shoot_Point();
                    if (Host_Navi.FaceLeft)
                        loc.X = (int)Shoot_Point.X - (int)(8 * Host_Navi.Scale);
                    else
                        loc.X = (int)Shoot_Point.X;
                    loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * Host_Navi.Scale);
                    if (Host_Navi.FaceLeft) Speed.X = -10; else Speed.X = 10;
                    Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, Host_Navi.Scale, color, "Host"));
                }
        }

        public void Process_Client_Commands(KeyValuePair<string, NetNavi_Type> navi)
        {
            if (navi.Value.Activated_Ability == 1)
            {
                Point loc = new Point();
                PointF Shoot_Point;
                PointF Speed = new PointF();
                Shoot_Point = navi.Value.Get_Shoot_Point();
                if (navi.Value.FaceLeft)
                    loc.X = (int)Shoot_Point.X - (int)(8 * navi.Value.Scale);
                else
                    loc.X = (int)Shoot_Point.X;
                loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * navi.Value.Scale);
                if (navi.Value.FaceLeft) Speed.X = -10; else Speed.X = 10;
                Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, navi.Value.Scale, new Color4(255, 0, 0, 255), navi.Key));
            }

            if (navi.Value.Activated_Ability == 2)
            {
                Point loc = new Point();
                PointF Shoot_Point;
                PointF Speed = new PointF();
                Shoot_Point = navi.Value.Get_Shoot_Point();
                if (navi.Value.FaceLeft)
                    loc.X = (int)Shoot_Point.X - (int)(8 * navi.Value.Scale);
                else
                    loc.X = (int)Shoot_Point.X;
                loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * navi.Value.Scale);
                if (navi.Value.FaceLeft) Speed.X = -10; else Speed.X = 10;
                Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, navi.Value.Scale, new Color4(0, 255, 0, 255), navi.Key));
            }

            if (navi.Value.Activated_Ability == 3)
            {
                Point loc = new Point();
                PointF Shoot_Point;
                PointF Speed = new PointF();
                Shoot_Point = navi.Value.Get_Shoot_Point();
                if (navi.Value.FaceLeft)
                    loc.X = (int)Shoot_Point.X - (int)(8 * navi.Value.Scale);
                else
                    loc.X = (int)Shoot_Point.X;
                loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * navi.Value.Scale);
                if (navi.Value.FaceLeft) Speed.X = -10; else Speed.X = 10;
                Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, navi.Value.Scale, new Color4(0, 0, 255, 255), navi.Key));
            }

        }



        #region Physics update

        public void Update_Physics()
        {
            Update_Physics_Projectiles();
            Update_Physics_Navi(Host_Navi);
            //Update GDI Location
            if (!GLOn)
            {
                NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X);
                NaviForm.Top = Convert.ToInt32(Host_Navi.Location.Y);
            }
        }

        private void Update_Physics_Projectiles()
        {
            Projectiles_Type[] p = new Projectiles_Type[Projectile_List.Count]; Projectile_List.CopyTo(p);
            foreach (Navi_Main.Projectiles_Type item in p)
            {
                item.Location.X += item.Speed.X;
                item.Location.Y += item.Speed.Y;

                foreach (KeyValuePair<string, NetNavi_Type> navi in Client_Navi)
                    Update_Physics_Projectiles_Hit_Client(item, navi);
                Update_Physics_Projectiles_Hit_Host(item, Host_Navi);

                item.Life -= 1;
                if (item.Life <= 0)
                    Projectile_List.Remove(item);
            }
        }


        private void Update_Physics_Projectiles_Hit_Host(Projectiles_Type p, NetNavi_Type navi)
        {
            if (p.Owner != "Host")
            {
                Point point = new Point((int)p.Location.X, (int)p.Location.Y);
                if (navi.Navi_Location().Contains(point))
                {
                    if (p.Speed.X < 0)
                        navi.Speed.X -= 15;
                    else
                        navi.Speed.X += 15;

                    Projectile_List.Remove(p);
                }
            }
        }

        private void Update_Physics_Projectiles_Hit_Client(Projectiles_Type p, KeyValuePair<string, NetNavi_Type> navi)
        {
            if (p.Owner != navi.Key)
            {
                Point point = new Point((int)p.Location.X, (int)p.Location.Y);
                if (navi.Value.Navi_Location().Contains(point))
                {
                    if (p.Speed.X < 0)
                        navi.Value.Speed.X -= 15;
                    else
                        navi.Value.Speed.X += 15;

                    Projectile_List.Remove(p);
                }
            }
        }



        private void Update_Physics_Navi(NetNavi_Type navi)
        {
            //Friction
            if (navi.OnGround == true)
            {
                navi.Speed.X -= navi.Speed.X * stage.GroundFriction.X;
                navi.Speed.Y -= navi.Speed.Y * stage.GroundFriction.Y;
            }
            else
            {
                navi.Speed.X -= navi.Speed.X * stage.AirFriction.X;
                navi.Speed.Y -= navi.Speed.Y * stage.AirFriction.Y;
            }

            //Gravity
            //if (navi.OnGround == false)
            //{
            navi.Speed.Y = navi.Speed.Y + stage.Gravity.Y;
            navi.Speed.X = navi.Speed.X + stage.Gravity.X;
            //}
            //Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y

            //PointF vector = new PointF(navi.Speed.X * navi.Scale, navi.Speed.Y * navi.Scale);
            navi.StepMovement = new PointF(navi.Speed.X * navi.Scale, navi.Speed.Y * navi.Scale);

            float x, y;

            //1 by 1 movement
            do
            {
                if (navi.StepMovement.X > 0)
                    if (navi.StepMovement.X > 1) { navi.StepMovement.X--; x = 1; } else { navi.StepMovement.X = 0; x = navi.StepMovement.X; }
                else
                    if (navi.StepMovement.X < -1) { navi.StepMovement.X++; x = -1; } else { navi.StepMovement.X = 0; x = navi.StepMovement.X; }


                if (navi.StepMovement.Y > 0)
                    if (navi.StepMovement.Y > 1) { navi.StepMovement.Y--; y = 1; } else { navi.StepMovement.Y = 0; y = navi.StepMovement.Y; }
                else
                    if (navi.StepMovement.Y < -1) { navi.StepMovement.Y++; y = -1; } else { navi.StepMovement.Y = 0; y = navi.StepMovement.Y; }

                navi.Location.X += x;
                navi.Location.Y += y;

                if (!GLOn)
                    Update_Physics_GDI_Bounds(navi);
                else
                    Update_Physics_GL_Bounds(navi);

            } while (navi.StepMovement.X != 0 || navi.StepMovement.Y != 0);





            navi.Location_Last = navi.Location;
        }







        private void Update_Physics_GDI_Bounds(NetNavi_Type navi)
        {
            //Bounds
            if (navi.FaceLeft == true)
            {
                if (navi.Navi_Location().Left <= Screen.PrimaryScreen.WorkingArea.Left) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - (navi.GetSize().X - navi.GetHitBox().Right); navi.Speed.X = 0; }
            }
            else
            {
                if (navi.Navi_Location().Left <= Screen.PrimaryScreen.WorkingArea.Left) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - navi.GetHitBox().Left; navi.Speed.X = 0; }
            }

            if (navi.FaceLeft == true)
            {
                if (navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - (navi.GetSize().X - navi.GetHitBox().Left); navi.Speed.X = 0; }
            }
            else
            {
                if (navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - navi.GetHitBox().Right; navi.Speed.X = 0; }
            }

            if (navi.Navi_Location().Bottom > Screen.PrimaryScreen.WorkingArea.Bottom)
                navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - navi.GetHitBox().Bottom;
            if (navi.Navi_Location().Bottom == Screen.PrimaryScreen.WorkingArea.Bottom) { navi.OnGround = true; navi.Speed.Y = 0; }
            else
                navi.OnGround = false;
        }

        private void Update_Physics_GL_Bounds(NetNavi_Type navi)
        {
            bool OnGround = false;

            if (navi.Navi_Location().Left < 0)
            { navi.Set_LocationX(0); navi.Speed.X = 0; navi.StepMovement.X = 0; }

            if (navi.Navi_Location().Right > stage.Bounds.Width)
            { navi.Set_LocationX(stage.Bounds.Width - navi.Navi_Location().Width); navi.Speed.X = 0; navi.StepMovement.X = 0; }


            if (navi.Navi_Location().Bottom > stage.Bounds.Height)
                navi.Location.Y = stage.Bounds.Height - navi.GetHitBox().Bottom;
            if (navi.Navi_Location().Bottom == stage.Bounds.Height) { OnGround = true; navi.Speed.Y = 0; navi.StepMovement.Y = 0; }
            
            //Top bounds
            if (navi.Navi_Location().Top < 0) { navi.Location.Y = 0 - navi.GetHitBox().Top; navi.Speed.Y = 0; navi.StepMovement.Y = 0; }

            //CollisionMap
            foreach (StageCollisionTile tile in stage.CollisionMap.Values)
            {
                tile.Active = false;
            }

            Update_Physics_GL_Left(navi);
            Update_Physics_GL_Right(navi);

            Update_Physics_GL_Top(navi);
            Update_Physics_GL_Bottom(navi, OnGround);            
        }


        private void Update_Physics_GL_Bottom(NetNavi_Type navi, bool OnGround)
        {
            //Bottom            
            int Top = (int)(navi.Navi_Location().Top / 16);
            int Bottom = (int)(navi.Navi_Location().Bottom / 16);
            int Left = (int)(navi.Navi_Location().Left / 16);
            int Right = (int)(navi.Navi_Location().Right / 16);

            //always does hightmap on tile colleciton
            for (int y = Top; y <= Bottom; y++)
                for (int x = Left; x <= Right; x++)
                {
                    //int x;//, y;
                    //x = Right;// y = Bottom;
                    RectangleF rct = navi.Navi_Location();
                    if (stage.CollisionMap.ContainsKey(new Point(x, y)))
                    {
                        StageCollisionTile tile = stage.CollisionMap[new Point(x, y)];
                        tile.Active = true;

                        //Slope collision with a center tile
                        float point, pointL, pointR, ratioL, ratioR;
                        point = 16;
                        if (x != Left && x != Right)
                        {
                            if (tile.HeightLeft > tile.HeightRight) point = tile.HeightLeft; else point = tile.HeightRight;
                        }
                        else
                        //Slope collision with leftmost or rightmost tile
                        {
                            pointL = 16;
                            pointR = 16;

                            ratioR = (navi.Navi_Location().Right - x * 16) / 16;
                            ratioL = (navi.Navi_Location().Left - x * 16) / 16;
                            if (ratioL >= 0 && ratioL <= 1) pointL = tile.HeightLeft + (tile.HeightRight - tile.HeightLeft) * ratioL;
                            if (ratioR >= 0 && ratioR <= 1) pointR = tile.HeightLeft + (tile.HeightRight - tile.HeightLeft) * ratioR;
                            if (pointL < pointR) point = pointL; else point = pointR;
                        }

                        //set to ground
                        if (rct.Bottom > (y * 16) + 16 - point)
                        {
                            if (rct.Bottom - (y * 16) - (16 - point) <= 4)
                            {
                                if (navi.Navi_Location().Top < (y * 16 + (16 - point)))
                                {
                                    navi.Set_LocationY(y * 16 + (16 - point));
                                    navi.Speed.Y = 0;
                                    navi.StepMovement.Y = 0;
                                    OnGround = true;
                                }
                            }
                        }

                        if (navi.Navi_Location().Bottom + 1 >= (y * 16) + 16 - point)
                        {
                            OnGround = true;
                        }

                    }
                    navi.OnGround = OnGround;
                }
        }

        private void Update_Physics_GL_Top(NetNavi_Type navi)
        {
            //Top
            int Top = (int)(navi.Navi_Location().Top / 16);            
            int Left = (int)(navi.Navi_Location().Left / 16);
            int Right = (int)(navi.Navi_Location().Right / 16);

            //always does hightmap on tile colleciton            
                for (int x = Left; x <= Right; x++)
                {                    
                    RectangleF rct = navi.Navi_Location();
                    if (stage.CollisionMap.ContainsKey(new Point(x, Top)))
                    {
                        StageCollisionTile tile = stage.CollisionMap[new Point(x, Top)];
                        tile.Active = true;
                        
                        //set to Top
                        if (rct.Top - (Top * 16 + 16) >= -1)
                        {                            
                                navi.Set_LocationY(Top * 16 + 16 + navi.Navi_Location().Height);
                                navi.Speed.Y = 0;
                                navi.StepMovement.Y = 0;                                                            
                        }                    
                    }                    
                }
        }


        private void Update_Physics_GL_Left(NetNavi_Type navi)
        {
            //Top
            int Top = (int)((navi.Navi_Location().Top + 2) / 16);
            int Bottom = (int)((navi.Navi_Location().Bottom - 2) / 16);
            int Left = (int)(navi.Navi_Location().Left / 16);            

            //always does hightmap on tile colleciton    
            for (int y = Top; y <= Bottom; y++)
            {
                RectangleF rct = navi.Navi_Location();
                if (stage.CollisionMap.ContainsKey(new Point(Left, y)))
                {
                    StageCollisionTile tile = stage.CollisionMap[new Point(Left, y)];
                    tile.Active = true;

                    if (rct.Bottom - (y * 16 + 16 - tile.HeightRight) >= 4)
                    {
                        //set to Left
                        if (rct.Left - (Left * 16 + 16) >= -1)
                        {
                            navi.Set_LocationX(Left * 16 + 16);
                            navi.Speed.X = 0;
                            navi.StepMovement.X = 0;
                        }
                    }
                }
            }
        }

        private void Update_Physics_GL_Right(NetNavi_Type navi)
        {
            //Top
            int Top = (int)((navi.Navi_Location().Top + 2) / 16);
            int Bottom = (int)((navi.Navi_Location().Bottom - 2) / 16);
            int Right = (int)(navi.Navi_Location().Right / 16);

            //always does hightmap on tile colleciton    
            for (int y = Top; y <= Bottom; y++)
            {
                RectangleF rct = navi.Navi_Location();
                if (stage.CollisionMap.ContainsKey(new Point(Right, y)))
                {
                    StageCollisionTile tile = stage.CollisionMap[new Point(Right, y)];
                    tile.Active = true;

                    if (rct.Bottom - (y * 16 + 16 - tile.HeightLeft) >= 4)
                    {
                        //set to Left
                        if (rct.Right > (Right * 16))
                        {
                            navi.Set_LocationX(Right * 16 - navi.Navi_Location().Width);
                            navi.Speed.X = 0;
                            navi.StepMovement.X = 0;
                        }
                    }
                }
            }
        }

        #endregion

        #region Fourm Handelers

        private void NaviForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            pressedkeys.Add(e.KeyCode); // automatically checks for duplicates and won't add twice
        }

        private void NaviForm_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            pressedkeys.Remove(e.KeyCode); // returns false if KeyCode isn't in the hashset
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


        private void NaviGL_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (pressedkeys.Contains(e.KeyCode))
            {
            }
            else
            {
                pressedkeys.Add(e.KeyCode);
            }
        }

        private void NaviGL_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (pressedkeys.Contains(e.KeyCode))
            {
                pressedkeys.Remove(e.KeyCode);
            }
        }

        private void NaviGL_LostFocus(object sender, System.EventArgs e)
        {
            pressedkeys.Clear();
        }

        private void NaviGL_Disposed(object sender, System.EventArgs e)
        {
            GLOn = false;
            Init_GL = false;
            foreach (int id in GLNaviTexture.Values)
            {
                int ID = id;
                GL.DeleteTextures(1, ref ID);
            }

            foreach (int id in GLItemTexture.Values)
            {
                int ID = id;
                GL.DeleteTextures(1, ref ID);
            }


            GLNaviTexture.Clear();
            GLItemTexture.Clear();
            NaviForm.Show();
            //Reset Navi location
            Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize().Y;
            Host_Navi.Location.X = 1000;
            Host_Navi.Scale = 3;
        }
        #endregion
    }
}