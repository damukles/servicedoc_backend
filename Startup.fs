namespace Backend

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe.Middleware
open WebApp

type Startup private() =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member this.ConfigureServices(services: IServiceCollection) =
        ()

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        // if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore
        // app.Run(fun context -> context.Response.WriteAsync("Hello World!"))
        app.UseGiraffeErrorHandler WebApp.errorHandler
        app.UseGiraffe WebApp.webApp
        ()

    member val Configuration : IConfiguration = null with get, set