using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TlenSRV;
using static TlenSRV.Roster;

class Program
{
    public struct Client
    {
        public string mail;
        public string password;
        public NetworkStream ns;
        public List<Friend> roster;
    }

    public static List<Client> users = new List<Client>();

    public static void WriteReply(string reply, NetworkStream ns)
    {
        byte[] msg = Encoding.ASCII.GetBytes(reply);
        ns.Write(msg, 0, msg.Length);
        //Console.WriteLine($"Sent: {reply}");
    }

    static void SendHello(Client cl)
    {
        WriteReply($"<message from=\"bot@tlen.pl\" type=\"chat\"><body>Witaj {cl.mail} na Testowym serwerze Tlen.pl</body></message>", cl.ns);
    }

    static void SendMessage(string receiver, string message, Client cl)
    {
        Client user = users.Find(x => x.mail == receiver);
        if (user.ns != null)
        {
            WriteReply($"<message from=\"{cl.mail}\" type=\"chat\"><body>{message}</body></message>", user.ns);
        }
    }

    static void Main(string[] args)
    {
        TcpListener server = null;
        try
        {
            int port = 25555;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
            Console.WriteLine("Serwer wystartował na porcie " + port);
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"SocketException: {e}");
        }
        finally
        {
            server.Stop();
        }

        Console.WriteLine("\nServer stopped. Press any key to exit...");
        Console.ReadKey();
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        byte[] bytes = new byte[1024];
        string data = null;
        NetworkStream stream = client.GetStream();
        Client cl = new Client { ns = stream };
        bool added = false;

        while (true)
        {
            try
            {
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(100);
                    continue;
                }

                int i = stream.Read(bytes, 0, bytes.Length);
                if (i == 0)
                {
                    continue;
                }
                data = Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine(data);
                cl = HandlePacket(data, cl);
                if (!added && !string.IsNullOrEmpty(cl.mail))
                {
                    users.Add(cl);
                    added = true;
                }

                Array.Clear(bytes, 0, bytes.Length);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Client disconnected: {ex.Message}");
                break;
            }
        }
        stream.Close();
        client.Close();
    }

    static Client HandlePacket(string packet, Client cl)
    {
        if (packet.Contains("</s>"))
        {
            HandleQuit(packet, cl);
            return cl;
        }

        string[] packets = packet.Split(new string[] { "</" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string packetPart in packets)
        {
            if (packetPart.Contains("<s "))
            {
                HandleStream(packetPart, cl);
            }
            else if (packetPart.Contains("<iq "))
            {
                cl = HandleIQ(packet, cl);
            }
            else if (packetPart.Contains("<presence"))
            {
                HandlePresence(packetPart, cl);
            }
            else if (packetPart.Contains("<message "))
            {
                HandleMessage(packetPart, cl);
            }
        }
        return cl;
    }

    private static void HandleQuit(string packetPart, Client cl)
    {
        foreach(var usr in users)
        {
            WriteReply($"<presence from=\"{cl.mail}\"><show>unavailable</show></presence>", usr.ns);
        }
        Console.WriteLine($"{cl.mail} wylogował się");
    }

    static void HandleStream(string packet, Client cl)
    {
        WriteReply("<s i=\"456C287C\" c=\"0\" s=\"0\">", cl.ns);
    }

    static Client HandleIQ(string packet, Client cl)
    {
        var id = packet.Split("id=\"")[1];
        var idfinal = id.Split("\"")[0];
        switch(idfinal)
        {
            /*
             * 
             * login?
             * 
             */
            case "456C287C":
                {
                    if (packet.Contains("<username>"))
                    {
                        var first = packet.Split("<username>")[1];
                        var username = first.Split("</username>")[0];
                        var first2 = packet.Split("<digest>")[1];
                        var digest = first2.Split("</digest>")[0];
                        cl = User.LookupUser(username, digest, cl.ns);
                        Console.WriteLine(digest);
                        if (User.PassCheck(cl,digest))
                        {
                            Console.WriteLine($"Użytkownik {cl.mail} właśnie się zalogował");
                            User.SendLoginSuccess(cl.ns);
                        } else {
                            Console.WriteLine($"Użytkownik {cl.mail} podał złe hasło");
                            User.SendLoginFail(cl.ns);
                        }
                        SendHello(cl);
                        foreach (var usr in users)
                        {
                            WriteReply($"<presence from=\"{usr.mail}\"><show>available</show></presence>", cl.ns);
                        }
                    }
                    break;
                }
                /*
                 * 
                 * nie musze chyba tlumaczyc
                 * 
                 */
            case "GetRoster":
                {
                    List<Friend> friends = new List<Friend>();
                    Friend friend = new Friend();
                    friend.jid = "admin@tlen.pl";
                    friend.name = "Admin";
                    friend.group = "Administatorzy";
                    friend.subtype = SubTypes.both;
                    friends.Add(friend);
                    Roster.SendRoster(friends, cl.ns); 
                    break;
                }
        }
        return cl;
    }

    static void HandlePresence(string packet, Client cl)
    {
        bool sent = false;
        if(!packet.Contains("<show>"))
        {
            var firstt = packet.Split("to=\"")[1];
            var secondt = firstt.Split("\"")[0];
            var type1 = packet.Split("type=\"")[1];
            var type2 = type1.Split("\"")[0];
            var cl2 = users.Find(x=>x.mail == secondt);
            if(type2 != "subscribed")
                WriteReply($"<presence from=\"{cl.mail}\" type=\"subscribed\"/>", cl2.ns);
        } else
        {
            foreach (var usr in users)
            {
                if (!packet.Contains("unavailable") && !packet.Contains("invisible"))
                {
                    var firstb = packet.Split("<show>")[1];
                    var secondb = firstb.Split("</show>")[0];
                    WriteReply($"<presence from=\"{cl.mail}\"><show>{secondb}</show></presence>", usr.ns);
                    if (!sent)
                        Console.WriteLine($"Presence {cl.mail}->{secondb}");
                    sent = true;
                }
                else
                {
                    WriteReply($"<presence from=\"{cl.mail}\"><show>unavailable</show></presence>", usr.ns);
                    if (!sent)
                        Console.WriteLine($"Presence {cl.mail}->unavailable");
                    sent = true;
                }

            }
        }
        
    }

    static void HandleMessage(string packet, Client cl)
    {
        if (!packet.Contains("<body>"))
            return;

        var firstb = packet.Split("<body>")[1];
        var secondb = firstb.Split("</body>")[0];
        var firstt = packet.Split("to=\"")[1];
        var secondt = firstt.Split("\"")[0];
        Console.WriteLine($"{cl.mail}->{secondt}: {secondb}");
        SendMessage(secondt, secondb, cl);
    }
}
