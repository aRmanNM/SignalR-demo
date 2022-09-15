using System.Net.WebSockets;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string url = "ws://localhost:5218/samplehub";

        try
        {
            var ws = new ClientWebSocket();

            await ws.ConnectAsync(new Uri(url), CancellationToken.None);

            var handshake = new List<byte>(Encoding.UTF8.GetBytes(@"{""protocol"":""json"", ""version"":1}"))
            {
                0x1e
            };

            await ws.SendAsync(new ArraySegment<byte>(handshake.ToArray()), WebSocketMessageType.Text, true, CancellationToken.None);

            Console.WriteLine("WebSockets connection established");
            await ReceiveAsync(ws);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            throw;
        }
    }

    private static async Task ReceiveAsync(ClientWebSocket ws)
    {
        var buffer = new byte[4096];

        try
        {
            while (true)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }
                else
                {
                    Console.WriteLine(Encoding.Default.GetString(Decode(buffer)));
                    buffer = new byte[4096];
                }
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }
    }

    private static byte[] Decode(byte[] packet)
    {
        var i = packet.Length - 1;
        while (i >= 0 && packet[i] == 0)
        {
            --i;
        }

        var temp = new byte[i + 1];
        Array.Copy(packet, temp, i + 1);
        return temp;
    }
}