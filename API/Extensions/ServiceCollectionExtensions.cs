using System.Data;
using API.Settings;
using Application.Application.Interfaces;
using Application.Application.UseCases.Account.Authenticate;
using Application.Application.UseCases.Account.Authorize;
using Application.Application.UseCases.Account.ConfirmEmail;
using Application.Application.UseCases.Account.Create;
using Application.Application.UseCases.Account.RefreshAccessToken;
using Application.Infrastructure.Interfaces.JwtProvider;
using Application.Infrastructure.Interfaces.PasswordHasher;
using Application.Infrastructure.Interfaces.Repositories;
using Infrastructure.Hasher;
using Infrastructure.JwtProvider;
using Infrastructure.Repositories.Implementations;
using Infrastructure.Settings;
using MassTransit;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbConnection(this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbSettings = configuration.GetSection("ConnectionStrings")
            .Get<DbConnectionSettings>();
        services.AddScoped<IDbConnection>(_ => 
            new NpgsqlConnection(dbSettings!.ConnectionString));

        return services;
    }

    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<CreateAccountRequest, CreateAccountResponse>, 
            CreateAccountUseCase>();
        services.AddScoped<IUseCase<AuthenticateAccountRequest, AuthenticateAccountResponse>,
            AuthenticateAccountUseCase>();
        services.AddScoped<IUseCase<AuthorizeAccountRequest, AuthorizeAccountResponse>,
            AuthorizeAccountUseCase>();
        services.AddScoped<IUseCase<ConfirmAccountEmailRequest, ConfirmAccountEmailResponse>,
            ConfirmAccountEmailUseCase>();
        services.AddScoped<IUseCase<RefreshAccountAccessTokenRequest, RefreshAccountAccessTokenResponse>,
            RefreshAccountAccessTokenUseCase>();
        
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddTransient<IAccountRepository, AccountRepository>();
        
        return services;
    }

    public static IServiceCollection AddPasswordHasher(this IServiceCollection services)
    {
        services.AddScoped<IHasher, Hasher>();
        
        return services;
    }

    public static IServiceCollection AddJwtProvider(this IServiceCollection services)
    {
        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
    
    public static IServiceCollection ConfigureSerilog(this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("MongoSettings").Get<MongoSettings>();
        
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
            .WriteTo.MongoDBBson(settings!.ConnectionString, 
            "logs",
            LogEventLevel.Verbose, 
            512,
            TimeSpan.FromSeconds(20))
            .CreateLogger();
        services.AddSerilog();
        
        return services;
    }

    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.UsingInMemory();

            x.AddRider(rider =>
            {
                // rider.AddConsumer<KafkaMessageConsumer>();

                rider.UsingKafka((context, k) =>
                {
                    k.Host("localhost:9092");

                    // k.TopicEndpoint<KafkaMessage>("topic-name", "consumer-group-name", e =>
                    // {
                    //     e.ConfigureConsumer<KafkaMessageConsumer>(context);
                    // });
                });
            });
        });

        return services;
    }
    
    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder.AddPrometheusExporter();

                builder.AddMeter("Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel");
                builder.AddView("http.server.request.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries =
                        [
                            0, 0.005, 0.01, 0.025, 0.05,
                            0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10
                        ]
                    });
            })
            .WithTracing(builder =>
            {
                builder
                    .AddGrpcClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("Identity"))
                    .AddSource("Identity")
                    // .AddSource("MassTransit") 
                    .AddJaegerExporter();
            });;
        
        return services;
    }
}