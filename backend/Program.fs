module Program

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe.Middleware
open Db.Models
open Microsoft.Extensions.Logging

let exitCode = 0

// store a mutable DbConfig instead of using DI
let mutable DbConfig : DbConfig = { connectionString = null; database = null }

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Warning
    builder.AddFilter(filter)
           .AddConsole()
           .AddDebug()
        |> ignore

let configureConfiguration (ctx : WebHostBuilderContext) (configBuilder : IConfigurationBuilder) =
    let config = configBuilder
                    .SetBasePath(ctx.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json")
                    .Build()
    DbConfig <- { connectionString =  config.GetSection("DbConfig:ConnectionString").Value
                ; database = config.GetSection("DbConfig:Database").Value
                }
    ()

let configureServices(services : IServiceCollection) =
    services.AddCors(
        fun options -> options.AddPolicy("CorsPolicy",
                              fun builder -> builder.AllowAnyOrigin()
                                                    .AllowAnyMethod()
                                                    .AllowAnyHeader()
                                                    .AllowCredentials()
                                                    |> ignore
        )
    ) |> ignore
    ()

let configureApp(app: IApplicationBuilder) =
    app.UseCors("CorsPolicy") |> ignore
    app.UseGiraffeErrorHandler Api.errorHandler
    app.UseGiraffe (Api.router DbConfig)
    ()

let BuildWebHost args =
    WebHostBuilder()
        // .CreateDefaultBuilder(args)
        .UseKestrel()
        .UseIISIntegration()
        .ConfigureAppConfiguration(configureConfiguration)
        .ConfigureLogging(configureLogging)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)


[<EntryPoint>]
let main args =
    BuildWebHost(args)
        .Build()
        .Run()

    exitCode
