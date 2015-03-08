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
		//Runtime Varables
		public PointF Location;
		public PointF Speed;
		public float Scale = 1;
		public Point Sprite;
		public Point OldSprite;
		public bool OldFaceLeft;
		public int Health;

		public int Energy;

		//Statistics
		public long NaviID;
		public string Navi_Name;
		public Rectangle HitBox;
		public System.Drawing.Bitmap SpriteSheet;
        public String GLSpriteSheetName;
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



		public byte[] Get_Compact_buffer()
		{
			int index = 0;
			byte[] b = new byte[70];
			BitConverter.GetBytes(NaviID).CopyTo(b, index);
			index += 8;

			BitConverter.GetBytes(Location.X).CopyTo(b, index);
			index += 4;
			BitConverter.GetBytes(Location.Y).CopyTo(b, index);
			index += 4;

			BitConverter.GetBytes(Speed.X).CopyTo(b, index);
			index += 4;
			BitConverter.GetBytes(Speed.Y).CopyTo(b, index);
			index += 4;

			BitConverter.GetBytes(SpriteSize.X).CopyTo(b, index);
			index += 4;
			BitConverter.GetBytes(SpriteSize.Y).CopyTo(b, index);
			index += 4;

			BitConverter.GetBytes(Scale).CopyTo(b, index);
			index += 4;

			BitConverter.GetBytes(Sprite.X).CopyTo(b, index);
			index += 4;
			BitConverter.GetBytes(Sprite.Y).CopyTo(b, index);
			index += 4;

			BitConverter.GetBytes(Health).CopyTo(b, index);
			index += 4;
			BitConverter.GetBytes(Energy).CopyTo(b, index);
			index += 4;
            

            BitConverter.GetBytes(Activated_Ability).CopyTo(b, index);
            index += 4;
            

			BitConverter.GetBytes(OnGround).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(FaceLeft).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(Running).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(Jumping).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(HasJumped).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(Shooting).CopyTo(b, index);
			index += 1;            
			BitConverter.GetBytes(WallGrabing).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(Dashing).CopyTo(b, index);
			index += 1;
			BitConverter.GetBytes(HasDashed).CopyTo(b, index);
			index += 1;
			return b;
		}


		public void Set_Compact_buffer(byte[] b)
		{
			int index = 5;
			NaviID = BitConverter.ToInt64(b, index);
			index += 8;
			Location.X = BitConverter.ToSingle(b, index);
			index += 4;
			Location.Y = BitConverter.ToSingle(b, index);
			index += 4;
			Speed.X = BitConverter.ToSingle(b, index);
			index += 4;
			Speed.Y = BitConverter.ToSingle(b, index);
			index += 4;

			SpriteSize.X = BitConverter.ToInt32(b, index);
			index += 4;
			SpriteSize.Y = BitConverter.ToInt32(b, index);
			index += 4;
			Scale = BitConverter.ToSingle(b, index);
			index += 4;
			Sprite.X = BitConverter.ToInt32(b, index);
			index += 4;
			Sprite.Y = BitConverter.ToInt32(b, index);
			index += 4;
			Health = BitConverter.ToInt32(b, index);
			index += 4;
			Energy = BitConverter.ToInt32(b, index);
			index += 4;

            Activated_Ability = BitConverter.ToInt32(b, index);
            index += 4;            

			OnGround = BitConverter.ToBoolean(b, index);
			index += 1;
			FaceLeft = BitConverter.ToBoolean(b, index);
			index += 1;
			Running = BitConverter.ToBoolean(b, index);
			index += 1;
			Jumping = BitConverter.ToBoolean(b, index);
			index += 1;
			HasJumped = BitConverter.ToBoolean(b, index);
			index += 1;
			Shooting = BitConverter.ToBoolean(b, index);
            index += 1;            
			WallGrabing = BitConverter.ToBoolean(b, index);
			index += 1;
			Dashing = BitConverter.ToBoolean(b, index);
			index += 1;
			HasDashed = BitConverter.ToBoolean(b, index);
			index += 1;
		}




	}
}
