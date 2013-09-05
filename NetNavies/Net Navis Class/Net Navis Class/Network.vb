'This module handles all the network interaction


'Packet Layout
'All packets
'4 byte Program step
'1 byte Packet type

'Full sync

'CommandSend



Partial Class Navi_Main


    Public Enum Packet_Type As Byte
        FullSync = 0
        CommandSend = 1
    End Enum


    Private Net_Host As Net.Sockets.TcpListener
    Private Net_Client As Net.Sockets.TcpClient
    Private IsClient As Boolean

    Private Client_List As Dictionary(Of Integer, Client_Type) = New Dictionary(Of Integer, Client_Type)


    Class Client_Type
        Public ReSync As Boolean
        Public Socket As Net.Sockets.TcpClient
        Public Client_Navi As NetNavi_Type

        Sub New(ByVal Socket As Net.Sockets.TcpClient)
            Me.Socket = Socket
            ReSync = True
            Client_Navi = New NetNavi_Type
        End Sub

    End Class


    Function Initialise_Network() As Boolean
        'Check for host if none start host
        Try
            Connect_As_Client()
            Console.WriteLine("Connected")
            Return True
        Catch
            Try
                Host()
                Console.WriteLine("Hosted")
            Catch ex As Exception
                Return False
            End Try
            Return True
        End Try
        Return False
    End Function


    Private Sub Connect_As_Client()
        Dim ip As Net.IPAddress = Net.IPAddress.Parse("127.0.0.1")
        Net_Client = New Net.Sockets.TcpClient
        Net_Client.Connect(ip, 52525)
        Console.WriteLine("Connecting")
        IsClient = True
    End Sub


    Private Sub Host()
        Dim ip As Net.IPAddress = Net.IPAddress.Parse("127.0.0.1")
        Net_Host = New Net.Sockets.TcpListener(ip, 52525)
        Net_Host.Start()
        Console.WriteLine("Hosting")
        IsClient = False
    End Sub

    Sub CheckForConnections()
        Do Until Net_Host.Pending = False

            Dim ID As Integer
            For ID = 0 To 1000
                If Not Client_List.ContainsKey(ID) Then Exit For
                If ID = 1000 Then Exit Sub
            Next
            Client_List.Add(ID, New Client_Type(Net_Host.AcceptTcpClient))
        Loop
    End Sub


    Sub Handle_Clients()

        For Each Client In Client_List
            If Client.Value.ReSync = True Then                
                ServerResync(Client.Value.Socket)
                'Client.Value.ReSync = False
            End If



        Next

    End Sub


    Sub Update_To_Host()
        If Net_Client.Client.Available > 0 Then
            Dim b(71) As Byte
            Net_Client.GetStream.Read(b, 0, 71)
            Host_Navi.Set_Compact_buffer(b)
        End If

    End Sub


    Sub DoNetworkEvents()
        If IsClient = False Then
            CheckForConnections()
            Handle_Clients()
        End If

        If IsClient = True Then
            Update_To_Host()
        End If

    End Sub



    Sub ServerResync(ByVal Socket As Net.Sockets.TcpClient)        
        Dim Buffer(71) As Byte
        'Convert Data
        BitConverter.GetBytes(Program_Step).CopyTo(Buffer, 0)
        Buffer(4) = Packet_Type.FullSync
        Host_Navi.Get_Compact_buffer.CopyTo(Buffer, 5) '65 bytes
        'Send Data
        'Socket.GetStream.BeginWrite(Buffer, 0, Buffer.Length, Nothing, Nothing)

        Socket.GetStream.Write(Buffer, 0, Buffer.Length - 1)
    End Sub


End Class
