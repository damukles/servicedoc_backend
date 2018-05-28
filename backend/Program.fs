module Program

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Giraffe.Middleware
open Db.Models

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Warning
    builder.AddFilter(filter)
        .AddConsole()
        .AddDebug()
        |> ignore

let configureServices (config : IConfiguration) (services : IServiceCollection)  =
    services
        .AddCors(
            fun options ->
            options.AddPolicy("CorsPolicy",
                fun builder ->
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        |> ignore
            )
        )
        .Configure<DbConfig>(fun options -> config.Bind("DbConfig", options))
        |> ignore
    ()

let configureApp(app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    if (env.IsDevelopment()) then app.UseCors("CorsPolicy") |> ignore
    app.UseGiraffeErrorHandler Api.errorHandler
    let dbClient =
        app.ApplicationServices.GetRequiredService<IOptions<DbConfig>>().Value
            |> Db.Service.getClient
    app.UseGiraffe (Api.router dbClient)
    ()

[<EntryPoint>]
let main _ =
    let config =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build()
    WebHostBuilder()
        .UseKestrel()
        .ConfigureLogging(configureLogging)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices config)
        .Build()
        .Run()
    0
