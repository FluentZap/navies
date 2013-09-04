'This module handles all the network interaction
Partial Class Navi_Main
    Private Net_Host As Net.Sockets.TcpListener
    Private Net_Client As Net.Sockets.TcpClient
    Private IsClient As Boolean

    Private Client_List As Dictionary(Of Integer, Client_Type) = New Dictionary(Of Integer, Client_Type)


    'Private Client_Member



    Class Client_Type
        Public ReSync As Boolean
        Public Socket As Net.Sockets.TcpClient

        Sub New(ByVal Socket As Net.Sockets.TcpClient)
            Me.Socket = Socket
            ReSync = True
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
                'Client.Value.Socket.SendBufferSize
                Resync(Client.Value.Socket, False)
            End If



        Next

    End Sub


    Sub Update_To_Host()

        Dim stream As New IO.MemoryStream
        '       stream = 

        'Net_Client.Client.Available=
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



    Sub Resync(ByVal Socket As Net.Sockets.TcpClient, ByVal IsClient As Boolean)
        If IsClient = False Then
            Dim s As New Runtime.Serialization.Formatters.Binary.BinaryFormatter
            Dim stream As New IO.MemoryStream()
            s.Serialize(stream, Host_Navi.Get_Compact)
            'Socket.Client.BeginSend(stream.ToArray, 0, stream.Length, Net.Sockets.SocketFlags.None, Nothing, Nothing)
            Socket.GetStream.BeginWrite(stream.ToArray, 0, stream.Length, Nothing, Nothing)

        End If


    End Sub


End Class
