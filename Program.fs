module Program

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe.Middleware
open Db.Models

// module Program =
let exitCode = 0

let mutable DbConfig : DbConfig option = None

let configureConfiguration (ctx : WebHostBuilderContext) (configBuilder : IConfigurationBuilder) =
    let config = configBuilder
                    .SetBasePath(ctx.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json")
                    .Build()
    DbConfig <- Some { connectionString =  config.GetSection("DbConfig:ConnectionString").Value
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
