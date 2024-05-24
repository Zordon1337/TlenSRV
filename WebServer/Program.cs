using System.Net;

HttpListener http = new HttpListener();
http.Prefixes.Add("http://idi.tlen.pl:80/");
http.Start();
string server = "localhost";
string port = "25555";
while(true)
{
    var ctx = http.GetContext();
    var writer = new StreamWriter(ctx.Response.OutputStream);
    writer.Write($"<t s=\"{server}\" p=\"{port}\" v=\"59\" c=\"0\" i=\"83.5.30.88\">59</t>");
    writer.Flush();
    writer.Close();
    ctx.Response.Close();
}