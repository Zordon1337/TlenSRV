using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TlenSRV
{
    internal class Roster
    {
        public enum SubTypes
        {
            none,
            both,
            remove,
            to
        }

        public struct Friend
        {
            public string jid;
            public SubTypes subtype;
            public string name;
            public string group;
        }
        public static string GetHeader()
        {
            return ("<iq type=\"result\" id=\"GetRoster\"><query xmlns=\"jabber:iq:roster\">");
        }
        public static string GetUser(Friend user) {
            return (
                $"<item jid=\"{user.jid}\" name=\"{user.name}\" subscription=\"{user.subtype}\"><group>{user.group}</group></item>");
        }
        public static void SendRoster(List<Friend> roster, NetworkStream ns)
        {
            string message = "";
            message += GetHeader();
            foreach(var user in roster)
            {
                message += GetUser(user);
            }
            message += GetBottom();
            Program.WriteReply(message,ns);
        }
        public static string GetBottom()
        {
            return ("</query></iq>");
        }
    }
}
