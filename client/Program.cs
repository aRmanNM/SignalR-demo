using IdentityModel.Client;
using MessagePack;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var header = args.Length >= 1 ? args[0] : "Client1";
        AnsiConsole.Write(
                    new FigletText(header)
                        .LeftAligned());

        bool authenticate = false;
        bool asAdmin = false;

        Console.Write("Do you want to authenticate? (y/n): ");
        authenticate = Console.ReadLine()?.ToLower() == "y" ? true : false;

        if (authenticate)
        {
            Console.Write("Authenticate as Admin (otherwise regular user)? (y/n): ");
            asAdmin = Console.ReadLine()?.ToLower() == "y" ? true : false;
        }

        string token = string.Empty;

        if (authenticate)
        {
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("http://localhost:5000");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                Console.WriteLine("Press any key to exit ...");
                Console.ReadKey();
                return;
            }

            var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "signalR-client",
                ClientSecret = "secret",
                UserName = asAdmin ? "admin1" : "user1",
                Password = "P@ssw0rd"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                Console.WriteLine("Press any key to exit ...");
                Console.ReadKey();
                return;
            }

            token = tokenResponse.AccessToken;
        }

        //
        // strart hub

        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5218/samplehub", HttpTransportType.WebSockets, options =>
            {
                if (!string.IsNullOrEmpty(token))
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
            })
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithSecurity(MessagePackSecurity.UntrustedData)
                    .WithCompression(MessagePackCompression.Lz4Block)
                    .WithAllowAssemblyVersionMismatch(true)
                    .WithOldSpec()
                    .WithOmitAssemblyVersion(true);
            })
            .Build();

        hubConnection.On<string>("ReceiveMessage",
            message => Console.WriteLine("\n ----- \nSampleHub message is: {0}\n ----- \n", message));

        await hubConnection.StartAsync();

        try
        {
            while (true)
            {
                var message = string.Empty;
                var groupName = string.Empty;

                Console.WriteLine("Specify action:");
                Console.WriteLine("0 - broadcast to all");
                Console.WriteLine("1 - send to others");
                Console.WriteLine("2 - send to self");
                Console.WriteLine("3 - send to individual");
                Console.WriteLine("4 - send to a group");
                Console.WriteLine("5 - add user to a group");
                Console.WriteLine("6 - remove user from a group");
                Console.WriteLine("7 - send to self (throws exception)");
                Console.WriteLine("8 - send to self (requires authenticated user)");
                Console.WriteLine("9 - send to self (requires authenticated user with admin role)");
                Console.WriteLine("exit - Exit the program");

                var action = Console.ReadLine();

                if (action == "exit")
                    break;

                if (action != "5" && action != "6" && action != "7")
                {
                    Console.WriteLine("Please specify the message:");
                    message = Console.ReadLine();
                }

                if (action == "4" || action == "5" || action == "6")
                {
                    Console.WriteLine("Please specify the group name:");
                    groupName = Console.ReadLine();
                }

                switch (action)
                {
                    case "0":
                        await hubConnection.SendAsync("BroadcastMessage", message);
                        break;
                    case "1":
                        await hubConnection.SendAsync("SendToOthers", message);
                        break;
                    case "2":
                        await hubConnection.SendAsync("SendToCaller", message);
                        break;
                    case "3":
                        Console.WriteLine("Specify connection id:");
                        var connectionId = Console.ReadLine();
                        await hubConnection.SendAsync("SendToIndividual", connectionId, message);
                        break;
                    case "4":
                        await hubConnection.SendAsync("SendToGroup", groupName, message);
                        break;
                    case "5":
                        await hubConnection.SendAsync("AddUserToGroup", groupName);
                        break;
                    case "6":
                        await hubConnection.SendAsync("RemoveUserFromGroup", groupName);
                        break;
                    case "7":
                        // server exceptions didn't received on client using SendAsync method!
                        await hubConnection.InvokeAsync("SendToCallerWithException");
                        break;
                    case "8":
                        await hubConnection.InvokeAsync("SendToCallerIfAuthenticated", message);
                        break;
                    case "9":
                        await hubConnection.InvokeAsync("SendToCallerIfAuthenticatedWithAdminRole", message);
                        break;
                    default:
                        Console.WriteLine("Invalid action specified");
                        break;
                }
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
            return;
        }

    }
}