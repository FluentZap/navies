Public Class NetNavi_Type
    'Runtime Varables
    Public Location As PointF
    Public Speed As PointF    
    Public Scale As Single = 1
    Public Sprite As Point
    Public OldSprite As Point
    Public OldFaceLeft As Boolean
    Public Health As Integer
    Public Energy As Integer


    'Statistics
    Public NaviID As Long    
    Public Navi_Name As String
    Public HitBox As Rectangle
    Public SpriteSheet As System.Drawing.Bitmap
    Public Icon As System.Drawing.Icon
    Public SpriteSize As Point
    Public HealthMax As Integer
    Public EnergyMax As Integer
    Public Weight As Integer
    Public GroundSpeed As Single
    Public AirSpeed As Single
    Public DashSpeed As Single
    Public Acrobatics As Integer

    'Sprite Control
    Public OnGround As Boolean
    Public FaceLeft As Boolean
    Public Running As Boolean
    Public Jumping As Boolean
    Public HasJumped As Boolean
    Public Shooting As Boolean
    Public WallGrabing As Boolean
    Public Dashing As Boolean
    Public HasDashed As Boolean
    Public Current_Animation As Animation_Name_Enum = Animation_Name_Enum.None
    Public Ani_Current As Animation
    Public Ani_Index As Integer
    Public Ani_Step As Integer


    Function Navi_Location() As RectangleF
        If FaceLeft = True Then            
            Return New RectangleF(Location.X + (GetSize.X - GetHitBox.Right), Location.Y + GetHitBox.Top, GetHitBox.Width, GetHitBox.Height)
        Else            
            Return New RectangleF(Location.X + GetHitBox.Left, Location.Y + GetHitBox.Top, GetHitBox.Width, GetHitBox.Height)
        End If
    End Function

    Sub Update_Sprite()
        'Set correct animation


        'Progress Animation
        If Ani_Index > Ani_Current.Frame.Count - 1 Then
            If Ani_Current.RepeatFrame > -1 Then
                Ani_Index = Ani_Current.RepeatFrame : Ani_Step = 0
            Else
                Sprite = Ani_Current.Frame(Ani_Current.Hold_Index).Sprite
                Ani_Current.Finished = True
            End If
        End If
        If Ani_Index <= Ani_Current.Frame.Count - 1 Then
            Sprite = Ani_Current.Frame(Ani_Index).Sprite
            Ani_Step += 1
            If Ani_Step >= Ani_Current.Frame(Ani_Index).Duration Then Ani_Index += 1 : Ani_Step = 0
        End If
    End Sub


    Sub set_Animation(ByVal Animation As Animation_Name_Enum)
        Ani_Index = 0
        Ani_Step = 0
        Ani_Current = Get_Animation(Animation)
        Current_Animation = Animation
    End Sub

    Function GetHitBox() As Rectangle
        Return New Rectangle(CInt(HitBox.X * Scale), CInt(HitBox.Y * Scale), CInt(HitBox.Width * Scale), CInt(HitBox.Height * Scale))
    End Function

    Function GetSize() As PointF
        Return New PointF(SpriteSize.X * Scale, SpriteSize.Y * Scale)
    End Function





    Function Get_Compact_buffer() As Byte()
        Dim index As Integer = 0
        Dim b(65) As Byte
        BitConverter.GetBytes(NaviID).CopyTo(b, index) : index += 8

        BitConverter.GetBytes(Location.X).CopyTo(b, index) : index += 4
        BitConverter.GetBytes(Location.Y).CopyTo(b, index) : index += 4

        BitConverter.GetBytes(Speed.X).CopyTo(b, index) : index += 4
        BitConverter.GetBytes(Speed.Y).CopyTo(b, index) : index += 4

        BitConverter.GetBytes(SpriteSize.X).CopyTo(b, index) : index += 4
        BitConverter.GetBytes(SpriteSize.Y).CopyTo(b, index) : index += 4

        BitConverter.GetBytes(Scale).CopyTo(b, index) : index += 4

        BitConverter.GetBytes(Sprite.X).CopyTo(b, index) : index += 4
        BitConverter.GetBytes(Sprite.Y).CopyTo(b, index) : index += 4

        BitConverter.GetBytes(Health).CopyTo(b, index) : index += 4
        BitConverter.GetBytes(Energy).CopyTo(b, index) : index += 4

        BitConverter.GetBytes(OnGround).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(FaceLeft).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(Running).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(Jumping).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(HasJumped).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(Shooting).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(WallGrabing).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(Dashing).CopyTo(b, index) : index += 1
        BitConverter.GetBytes(HasDashed).CopyTo(b, index) : index += 1
        Return b

    End Function


    Sub Set_Compact_buffer(ByVal b() As Byte)
        Dim index As Integer = 5
        NaviID = BitConverter.ToInt64(b, index) : index += 8
        Location.X = BitConverter.ToSingle(b, index) : index += 4
        Location.Y = BitConverter.ToSingle(b, index) : index += 4
        Speed.X = BitConverter.ToSingle(b, index) : index += 4
        Speed.Y = BitConverter.ToSingle(b, index) : index += 4

        SpriteSize.X = BitConverter.ToInt32(b, index) : index += 4
        SpriteSize.Y = BitConverter.ToInt32(b, index) : index += 4
        Scale = BitConverter.ToSingle(b, index) : index += 4
        Sprite.X = BitConverter.ToInt32(b, index) : index += 4
        Sprite.Y = BitConverter.ToInt32(b, index) : index += 4
        Health = BitConverter.ToInt32(b, index) : index += 4
        Energy = BitConverter.ToInt32(b, index) : index += 4

        OnGround = BitConverter.ToBoolean(b, index) : index += 1
        FaceLeft = BitConverter.ToBoolean(b, index) : index += 1
        Running = BitConverter.ToBoolean(b, index) : index += 1
        Jumping = BitConverter.ToBoolean(b, index) : index += 1
        HasJumped = BitConverter.ToBoolean(b, index) : index += 1
        Shooting = BitConverter.ToBoolean(b, index) : index += 1
        WallGrabing = BitConverter.ToBoolean(b, index) : index += 1
        Dashing = BitConverter.ToBoolean(b, index) : index += 1
        HasDashed = BitConverter.ToBoolean(b, index) : index += 1
    End Sub




End Class