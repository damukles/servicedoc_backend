module Program

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe.Middleware
open Db.Models

let exitCode = 0

// store a mutable DbConfig option instead of using DI
let mutable DbConfig : DbConfig = { connectionString = null; database = null }

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
    app.UseGiraffeErrorHandler WebApp.errorHandler
    app.UseGiraffe (WebApp.webApp DbConfig)
    ()

let BuildWebHost args =
    WebHost
        .CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(configureConfiguration)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()


[<EntryPoint>]
let main args =
    BuildWebHost(args).Run()

    exitCode
