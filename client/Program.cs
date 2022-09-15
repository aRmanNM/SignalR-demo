﻿using System.Reflection;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string url = "http://localhost:5218/samplehub";

        var hubConnection = new HubConnectionBuilder().WithUrl(url).Build();

        hubConnection.On<string>("ReceiveMessage",
            message => Console.WriteLine("SampleHub message is: {0}", message));


        try
        {
            await hubConnection.StartAsync();

            var running = true;

            while (running)
            {
                var message = string.Empty;
                var groupName = string.Empty;

                AnsiConsole.Write(
                    new FigletText(GetAppName())
                        .LeftAligned());

                Console.WriteLine("Specify action:");
                Console.WriteLine("0 - broadcast to all");
                Console.WriteLine("1 - send to others");
                Console.WriteLine("2 - send to self");
                Console.WriteLine("3 - send to individual");
                Console.WriteLine("4 - send to a group");
                Console.WriteLine("5 - add user to a group");
                Console.WriteLine("6 - remove user from a group");
                Console.WriteLine("exit - Exit the program");

                var action = Console.ReadLine();

                if (action != "5" && action != "6")
                {
                    Console.WriteLine("Please specify the message:");
                    message = Console.ReadLine();
                }

                if (action == "4" || action == "5" || action == "6")
                {
                    Console.WriteLine("Please specify the message:");
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
                        hubConnection.SendAsync("SendToGroup", groupName, message).Wait();
                        break;
                    case "5":
                        hubConnection.SendAsync("AddUserToGroup", groupName).Wait();
                        break;
                    case "6":
                        hubConnection.SendAsync("RemoveUserFromGroup", groupName).Wait();
                        break;
                    case "exit":
                        running = false;
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

    private static string GetAppName()
    {
        return Assembly.GetCallingAssembly().GetName().Name ?? string.Empty;
    }
}