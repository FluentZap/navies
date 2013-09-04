Public Class NetNavi_Type
    'Runtime Varables
    Public Location As PointF
    Public Speed As PointF
    Public Size As PointF
    Public Scale As Double = 1
    Public Sprite As Point
    Public OldSprite As Point
    Public OldFaceLeft As Boolean
    Public Health As Integer
    Public Energy As Integer


    'Statistics
    Public NaviID
    Public Navi_Name As String
    Public HitBox As Rectangle
    Public SpriteSheet As System.Drawing.Bitmap
    Public SpriteSize As Point
    Public HealthMax As Integer
    Public EnergyMax As Integer
    Public Weight As Integer
    Public GroundSpeed As Double
    Public AirSpeed As Double
    Public DashSpeed As Double
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
        Return New Rectangle(HitBox.X * Scale, HitBox.Y * Scale, HitBox.Width * Scale, HitBox.Height * Scale)
    End Function

    Function GetSize() As PointF
        Return New PointF(Size.X * Scale, Size.Y * Scale)
    End Function





    Function Get_Compact() As NetNavi_Compact_Type

        Dim N As New NetNavi_Compact_Type
        N.Location = Location
        N.Speed = Speed
        N.Size = Size
        N.Scale = Scale
        N.Sprite = Sprite
        N.Health = Health
        N.Energy = Energy
        N.NaviID = NaviID
        N.Navi_Name = Navi_Name
        N.OnGround = OnGround
        N.FaceLeft = FaceLeft
        N.Running = Running
        N.Jumping = Jumping
        N.HasJumped = HasJumped
        N.Shooting = Shooting
        N.WallGrabing = WallGrabing
        N.Dashing = Dashing
        N.HasDashed = HasDashed
        Return N

    End Function







End Class


<Serializable()> _
Public Class NetNavi_Compact_Type

    'Runtime Varables
    Public Location As PointF
    Public Speed As PointF
    Public Size As PointF
    Public Scale As Double = 1
    Public Sprite As Point
    Public Health As Integer
    Public Energy As Integer
    'Statistics
    Public NaviID
    Public Navi_Name As String
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
End Class