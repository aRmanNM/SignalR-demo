using MessagePack;
using Microsoft.AspNetCore.Http.Connections;
using server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(10);
    hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
    hubOptions.MaximumParallelInvocationsPerClient = 2;

    hubOptions.EnableDetailedErrors = false; // only use in development environment

    if (hubOptions?.SupportedProtocols is not null)
    {
        foreach (var protocol in hubOptions.SupportedProtocols)
            Console.WriteLine($"SignalR supports {protocol} protocol");
    }
})
.AddJsonProtocol(options =>
{
    // json options here
})
.AddMessagePackProtocol(options =>
{
    // https://learn.microsoft.com/en-us/aspnet/core/signalr/messagepackhubprotocol?view=aspnetcore-6.0#messagepack-considerations)
    // https://github.com/neuecc/MessagePack-CSharp

    options.SerializerOptions = MessagePackSerializerOptions.Standard
        .WithSecurity(MessagePackSecurity.UntrustedData)
        .WithCompression(MessagePackCompression.Lz4Block)
        .WithAllowAssemblyVersionMismatch(true)
        .WithOldSpec()
        .WithOmitAssemblyVersion(true);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// app.MapControllers();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<SampleHub>("/samplehub", options =>
    {
        options.Transports =
            HttpTransportType.WebSockets | HttpTransportType.LongPolling;

        options.CloseOnAuthenticationExpiration = true;
        options.MinimumProtocolVersion = 0;

        // ...

        Console.WriteLine(
            $"Authorization data items: {options.AuthorizationData.Count}");
    });
});

app.Run();
