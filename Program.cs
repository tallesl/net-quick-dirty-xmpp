using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using System;
using static System.Text.Encoding;

class Program
{
    static string server = "localhost";

    static int port = 5222;

    static int timeout = 3000;

    static string id = Random("test");

    static string domain = "localhost";

    static string user = "usuario";

    static string resource = Random("resource");

    static string password = "churros";

    static string recipient = "usuario2";

    static string recipientresource = "pidgin";

    static string socks5filepath = "totransfer.txt";

    static string socks5filename = new FileInfo(socks5filepath).Name;

    static long socks5filesize = new FileInfo(socks5filepath).Length;

    static string socks5id = Random("socks5");

    static string socks5mimetype = "text/plain";

    static int socks5port = 6688;

    static string stream = $@"
        <stream:stream to='{domain}' xmlns='jabber:client' xmlns:stream='http://etherx.jabber.org/streams' version='1.0'>
    ";

    static string closestream = $@"</stream:stream>";

    static string getregister = $@"
        <iq type='get' id='{id}'>
            <query xmlns='jabber:iq:register'/>
        </iq>
    ";

    static string setregister = $@"
        <iq type='set' id='{id}' to='{domain}'>
            <query xmlns='jabber:iq:register'>
                <username>{user}</username>
                <password>{password}</password>
            </query>
        </iq>
    ";

    static string getauth = $@"
        <iq type='get' to='{domain}' id='{id}'>
            <query xmlns='jabber:iq:auth'/>
        </iq>
    ";

    static string setauth = $@"
        <iq type='set' id='{id}'>
            <query xmlns='jabber:iq:auth'>
                <username>{user}</username>
                <resource>{resource}</resource>
                <password>{password}</password>
            </query>
        </iq>
    ";

    static string ping = $@"
        <iq from='{user}@{domain}/{resource}' to='lit' id='c2s1' type='get'>
            <ping xmlns='urn:xmpp:ping'/>
        </iq>";

    static string message = $@"
        <message type='chat' to='{recipient}@{domain}' id='{id}'>
            <body>Test message.</body>
        </message>
    ";

    static string presence = $@"<presence/>";

    static string bytestreams1 = $@"
        <iq xml:lang='en' to='{recipient}@{domain}/{recipientresource}' from='{user}@{domain}/{resource}' type='set' id='{id}'>
            <si xmlns='http://jabber.org/protocol/si' id='{socks5id}' profile='http://jabber.org/protocol/si/profile/file-transfer'>
                <feature xmlns='http://jabber.org/protocol/feature-neg'>
                    <x xmlns='jabber:x:data' type='form'>
                        <field var='stream-method' type='list-single'>
                            <option>
                                <value>http://jabber.org/protocol/bytestreams</value>
                            </option>
                        </field>
                    </x>
                </feature>
                <file xmlns='http://jabber.org/protocol/si/profile/file-transfer' name='{socks5filename}' size='{socks5filesize}' mime-type='{socks5mimetype}' />
            </si>
        </iq>
    ";


    static string bytestreams2 = $@"
        <iq xml:lang='en' to='{recipient}@{domain}/{recipientresource}' from='{user}@{domain}/{resource}' type='set' id='{id}'>
            <query xmlns='http://jabber.org/protocol/bytestreams' sid='{socks5id}' mode='tcp'>
                <streamhost jid='{user}@{domain}/{resource}' host='localhost' port='{socks5port}' />
            </query>
        </iq>
    ";

    static void Main(string[] args)
    {
        PrintInfo();

        RunSocks5();

        using (var client = new TcpClient(server, port))
        using (var stream = client.GetStream())
        {
            stream.ReadTimeout = timeout;
            stream.WriteTimeout = timeout;

            for (;;)
            {
                PrintCommand();

                var comando = AskCommand();

                if (string.IsNullOrWhiteSpace(comando))
                {
                    PrintNothingToSend();
                }
                else
                {
                    string xml;

                    if (TryGetXml(comando, out xml))
                        WriteSocket(stream, xml);
                }

                PrintReceiving();

                var recebido = ReadSocket(stream);

                PrintReceived(recebido);
            }
        }
    }

    static string Random(string text) => $"{text}-{new Random().Next()}";

    static void PrintInfo()
    {
        Console.WriteLine($"Server: {server}");
        Console.WriteLine($"Port: {port}");
        Console.WriteLine();
        Console.WriteLine($"User: {user}@{domain}/{resource}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Recipient: {recipient}@{domain}");
        Console.WriteLine();
        Console.WriteLine($"File to transfer: {socks5filepath}");
        Console.WriteLine($"Size: {socks5filesize} bytes");
        Console.WriteLine($"Mime type: {socks5mimetype}");
        Console.WriteLine($"Port: {socks5port}");
        Console.WriteLine();
    }

    static void PrintCommand() => Console.Write("Command: ");

    static void PrintNothingToSend()
    {
        Console.WriteLine("Nothing to send.");
        Console.WriteLine();
    }

    static void PrintReceiving() => Console.Write("Receiving...");

    static void PrintReceived(string received)
    {
        received = TryFormatXml(received);

        Console.Write('\r');
        Console.WriteLine("Received:   ");
        Console.WriteLine();
        Console.WriteLine(received);
        Console.WriteLine();
    }

    static string AskCommand()
    {
        var command = Console.ReadLine().Trim();

        Console.WriteLine();

        return command;
    }

    static bool TryGetXml(string command, out string xml)
    {
        var field = typeof(Program).GetField(
            command, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (field == null)
        {
            Console.WriteLine($"Invalid \"{command}\" command.");

            xml = null;
            return false;
        }
        else
        {
            xml = (string)field.GetValue(null);
            return true;
        }
    }

    static void WriteSocket(Stream stream, string text)
    {
        var bytes = ASCII.GetBytes(text);

        stream.Write(bytes, 0, bytes.Length);
    }

    static string ReadSocket(Stream stream)
    {
        var bytes = new byte[1024];

        try
        {
            var read = stream.Read(bytes, 0, bytes.Length);
            return ASCII.GetString(bytes, 0, read);
        }
        catch (IOException e) when (e.Message.Contains("timed out"))
        {
            return "Timeout.";
        }
    }

    static void RunSocks5()
    {
        Task.Run(
            () =>
            {
                var server = new Socks5Server(socks5port);

                for (;;)
                {
                    AcceptSocks5(server);
                    WriteSocks5(server);
                }
            }
        ).ContinueWith(
            task => EndApplication(task.Exception.GetBaseException()),
            TaskContinuationOptions.OnlyOnFaulted
        );
    }

    static void AcceptSocks5(Socks5Server server)
    {
        var request = server.Accept();

        if (request.Destination is string)
            server.Reply(0, (string)request.Destination, request.Port);
        else if (request.Destination is System.Net.IPAddress)
            server.Reply(0, (System.Net.IPAddress)request.Destination, request.Port);
        else
            throw new InvalidOperationException($"Unexpected type: \"{request.Destination.GetType()}\".");
    }

    static void WriteSocks5(Socks5Server server) => File.OpenRead(socks5filepath).CopyTo(server.GetStream());

    static string TryFormatXml(string xml)
    {
        try
        {
            return XDocument.Parse(xml).ToString();
        }
        catch
        {
            return xml;
        }
    }

    static void EndApplication(Exception e)
    {
        Console.Error.WriteLine(e);
        Environment.Exit(1);
    }
}