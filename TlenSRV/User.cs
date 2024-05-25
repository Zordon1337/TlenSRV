using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Program;

namespace TlenSRV
{
    internal class User
    {
        public static Client LookupUser(string user, string digest, NetworkStream ns)
        {
            foreach (var existingUser in users)
            {
                if (existingUser.mail == user + "@tlen.pl")
                {
                    Console.WriteLine($"{existingUser.mail} {user}@tlen.pl");
                    Client newusr = new Client(); // Create a new Client object
                    newusr.mail = existingUser.mail;
                    newusr.password = existingUser.password;
                    newusr.roster = existingUser.roster;
                    newusr.ns = ns; // Assign the new NetworkStream
                    return newusr;
                }
            }
            // If the user is not found, create a new Client object
            Client cl = new Client();
            cl.mail = user + "@tlen.pl";
            cl.roster = new List<Roster.Friend>();
            cl.ns = ns;
            cl.password = digest;
            Console.WriteLine("new user " + cl.mail);
            Program.users.Add(cl);
            foreach (var user1 in Program.users)
            {
                Console.WriteLine(user1.mail + $" {user1.password}");
            }
            return cl;
        }


        public static bool PassCheck(Client user, string digest)
        {
            Console.WriteLine($"{user.password} == {digest}");
            return user.password == digest;
        }

        public static void SendLoginFail(NetworkStream ns)
        {
            WriteReply("<iq type=\"got-password-incorrect\" id=\"456C287C\"/>", ns);
            ns.Close();
        }
        public static void SendLoginSuccess(NetworkStream ns)
        {
            WriteReply("<iq type=\"result\" id=\"456C287C\"/>", ns);
        }
    }
}
