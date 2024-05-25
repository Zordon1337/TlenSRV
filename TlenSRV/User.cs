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
        public static Client LookupUser(string user,string digest,NetworkStream ns)
        {
            Client cl = users.Find(x => x.mail == user);
            if(cl.mail != null)
            {
                cl.ns = ns;
                
                return cl;
            } else
            {
                cl = new Client();
                cl.mail = user + "@tlen.pl";
                cl.roster = new List<Roster.Friend>();
                cl.ns = ns;
                cl.password = digest;
                return cl;
            }
        }
        public static bool PassCheck(Client user, string digest)
        {
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
