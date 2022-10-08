using MessagePack;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using server.Hubs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(10);
    hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
    hubOptions.MaximumParallelInvocationsPerClient = 2;

    hubOptions.EnableDetailedErrors = true; // only use in development environment

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://localhost:5000";
    options.ClientId = "signalR-server";
    options.ClientSecret = "secret";
    options.ResponseType = "token";
    options.CallbackPath = "/signin-oidc";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false; // only for development

    options.Scope.Add("role"); //Add this
})
.AddJwtBearer(options =>
{
    options.Authority = "http://localhost:5000";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false
    };

    options.RequireHttpsMetadata = false; // only for development
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/samplehub"))
            {
                // attempt to get a token from a query string used by websocket
                var accessToken = context.Request.Query["access_token"];

                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = context.Request
                        .Headers["Authorization"]
                        .ToString()
                        .Replace("Bearer ", "");
                }

                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OnlyAdmin", policy =>
    {
        policy.RequireClaim(JwtClaimTypes.Role, new string[] { "Admin" });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyGet",
        builder => builder.AllowAnyOrigin()
            .WithMethods("GET")
            .AllowAnyHeader());

    options.AddPolicy("AllowSampleDomains",
        builder => builder.WithOrigins("https://something.com", "https://something-else.com")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAnyGet")
    .UseCors("AllowSampleDomains");

app.UseAuthentication();
app.UseAuthorization();

// app.MapControllers();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<SampleHub>("/samplehub", options =>
    {
        options.Transports =
            HttpTransportType.WebSockets;
        // HttpTransportType.WebSockets | HttpTransportType.LongPolling;

        options.CloseOnAuthenticationExpiration = true;
        options.MinimumProtocolVersion = 0;

        // ...

        Console.WriteLine(
            $"Authorization data items: {options.AuthorizationData.Count}");
    });
});

app.Run();
