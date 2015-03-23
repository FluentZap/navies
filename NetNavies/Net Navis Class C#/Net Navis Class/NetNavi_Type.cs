using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
namespace Net_Navis
{    
	public class NetNavi_Type
	{
        public NetNavi_Network_Type NetBuffer = new NetNavi_Network_Type();

		//Runtime Varables
        public bool Initialised;
		public PointF Location;
        public PointF Location_Last;
		public PointF Speed;
		public float Scale = 1;
		public Point Sprite;
		public Point OldSprite;
		public bool OldFaceLeft;
		public int Health;
		public int Energy;

		//Statistics
        public ulong NAVIEXEID;
        public Navi_Name_ID NaviID;
        public string Navi_Display_Name;
		public Rectangle HitBox;
		public System.Drawing.Bitmap SpriteSheet;
        public int GLSpriteSheetName;
		public System.Drawing.Icon Icon;
		public Point SpriteSize;
		public int HealthMax;
		public int EnergyMax;
		public int Weight;
		public float GroundSpeed;
		public float AirSpeed;
		public float DashSpeed;
        public Point ShootPoint;
        public int ShootCharge;
        public int Activated_Ability = -1;

        //Temp
        public int Shoot_Advance = 0;

		public int Acrobatics;
		//Sprite Control
		public bool OnGround;
		public bool FaceLeft;
		public bool Running;
		public bool Jumping;
		public bool HasJumped;
		public bool Shooting;        
        public bool WallGrabing;
		public bool Dashing;
		public bool HasDashed;
		public Animation_Name_Enum Current_Animation = Animation_Name_Enum.None;
		public Navi_resources.Animation Ani_Current;
		public int Ani_Index;
		public int Ani_Step;                


		public RectangleF Navi_Location()
		{
			if (FaceLeft == true) {
				return new RectangleF(Location.X + (GetSize().X - GetHitBox().Right), Location.Y + GetHitBox().Top, GetHitBox().Width, GetHitBox().Height);
			} else {
				return new RectangleF(Location.X + GetHitBox().Left, Location.Y + GetHitBox().Top, GetHitBox().Width, GetHitBox().Height);
			}
		}

		public void Update_Sprite()
		{
			//Set correct animation


			//Progress Animation
			if (Ani_Index > Ani_Current.Frame.Count() - 1) {
				if (Ani_Current.RepeatFrame > -1) {
					Ani_Index = Ani_Current.RepeatFrame;
					Ani_Step = 0;
				} else {
					Sprite = Ani_Current.Frame[Ani_Current.Hold_Index].Sprite;
					Ani_Current.Finished = true;
				}
			}
			if (Ani_Index <= Ani_Current.Frame.Count() - 1) {
				Sprite = Ani_Current.Frame[Ani_Index].Sprite;
				Ani_Step += 1;
				if (Ani_Step >= Ani_Current.Frame[Ani_Index].Duration){Ani_Index += 1;Ani_Step = 0;}
			}
		}


		public void set_Animation(Animation_Name_Enum Animation)
		{
			Ani_Index = 0;
			Ani_Step = 0;
			Ani_Current = Navi_resources.Get_Animation(Animation);
			Current_Animation = Animation;
		}

		public Rectangle GetHitBox()
		{
			return new Rectangle(Convert.ToInt32(HitBox.X * Scale), Convert.ToInt32(HitBox.Y * Scale), Convert.ToInt32(HitBox.Width * Scale), Convert.ToInt32(HitBox.Height * Scale));
		}

		public PointF GetSize()
		{
			return new PointF(SpriteSize.X * Scale, SpriteSize.Y * Scale);
		}

        public PointF Get_Shoot_Point()
        {
            if (FaceLeft)
                return new PointF(Location.X + (SpriteSize.X - ShootPoint.X) * Scale, Location.Y + ShootPoint.Y * Scale);
            else
                return new PointF(Location.X + ShootPoint.X * Scale, Location.Y + ShootPoint.Y * Scale);
            
        }

        public void Set_Navi_Network(byte[] b, int index = 0)
        { NetBuffer.Set(b, index); }

        public byte[] Get_Navi_Network()
        { return NetBuffer.Get(this); }

        public void Process_Update()
        {
            Location_Last = Location;
            NetBuffer.ProcessBuffer(this); 
        }
    }	




    public class NetNavi_Network_Type
    {        
        //Statistics
        public ulong NAVIEXEID;
        public Navi_Name_ID NaviID;
        //public string Navi_Display_Name;        

        //Runtime Varables
        PointF Location;
        PointF Speed;
        float Scale = 1;
        Point Sprite;
        int Health;
        int Energy;
        int Activated_Ability = -1;

        //Sprite Control
        bool OnGround;
        bool FaceLeft;
        bool Running;
        bool Jumping;
        bool HasJumped;
        bool Shooting;
        bool WallGrabing;
        bool Dashing;
        bool HasDashed;
        
        public void Set(byte[] b, int index = 0)
        {            
            NAVIEXEID = BitConverter.ToUInt64(b, index);                 index += 8;
            NaviID = (Navi_Name_ID)BitConverter.ToInt32(b, index);      index += 4;
            Location.X = BitConverter.ToSingle(b, index);               index += 4;
            Location.Y = BitConverter.ToSingle(b, index);               index += 4;
            Speed.X = BitConverter.ToSingle(b, index);                  index += 4;
            Speed.Y = BitConverter.ToSingle(b, index);                  index += 4;
            Scale = BitConverter.ToSingle(b, index);                    index += 4;
            Sprite.X = BitConverter.ToInt32(b, index);                  index += 4;
            Sprite.Y = BitConverter.ToInt32(b, index);                  index += 4;
            Health = BitConverter.ToInt32(b, index);                    index += 4;
            Energy = BitConverter.ToInt32(b, index);                    index += 4;
            Activated_Ability = BitConverter.ToInt32(b, index);         index += 4;
            OnGround = BitConverter.ToBoolean(b, index);                index += 1;
            FaceLeft = BitConverter.ToBoolean(b, index);                index += 1;
            Running = BitConverter.ToBoolean(b, index);                 index += 1;
            Jumping = BitConverter.ToBoolean(b, index);                 index += 1;
            HasJumped = BitConverter.ToBoolean(b, index);               index += 1;
            Shooting = BitConverter.ToBoolean(b, index);                index += 1;
            WallGrabing = BitConverter.ToBoolean(b, index);             index += 1;
            Dashing = BitConverter.ToBoolean(b, index);                 index += 1;
            HasDashed = BitConverter.ToBoolean(b, index);               index += 1;
        }

        public byte[] Get(NetNavi_Type navi)
        {
            byte[] b = new byte[Navi_Main.COMPACT_BUFFER_SIZE];
            int index = 0;
            BitConverter.GetBytes(navi.NAVIEXEID).CopyTo(b, index); index += 8;
            BitConverter.GetBytes((int)navi.NaviID).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Location.X).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Location.Y).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Speed.X).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Speed.Y).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Scale).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Sprite.X).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Sprite.Y).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Health).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Energy).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.Activated_Ability).CopyTo(b, index); index += 4;
            BitConverter.GetBytes(navi.OnGround).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.FaceLeft).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.Running).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.Jumping).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.HasJumped).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.Shooting).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.WallGrabing).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.Dashing).CopyTo(b, index); index += 1;
            BitConverter.GetBytes(navi.HasDashed).CopyTo(b, index); index += 1;
            return b;
        }

        public void ProcessBuffer(NetNavi_Type navi)
        {
            navi.NAVIEXEID = NAVIEXEID;
            navi.NaviID = NaviID;
            navi.Location = Location;
            navi.Speed = Speed;
            navi.Scale = Scale;
            navi.Sprite = Sprite;
            navi.Health = Health;
            navi.Energy = Energy;
            navi.Activated_Ability = Activated_Ability;
            navi.OnGround = OnGround;
            navi.FaceLeft = FaceLeft;
            navi.Running = Running;
            navi.Jumping = Jumping;
            navi.HasJumped = HasJumped;
            navi.Shooting = Shooting;
            navi.WallGrabing = WallGrabing;
            navi.Dashing = Dashing;
            navi.HasDashed = HasDashed;
        }
    }
}
