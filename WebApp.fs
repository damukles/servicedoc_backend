module WebApp

open System
open System.IO
open System.Text
open System.Security.Claims
open System.Collections.Generic
open System.Threading
open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Db.Models

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let getDbConfig (dbConfig : DbConfig option) =
    (match dbConfig with
        | Some config ->
            String.Join(" - ", [ config.connectionString ; config.database ])
        | None ->
            "No Config could be found."
    )
        |> text

let webApp (dbConfig : DbConfig option) : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                route "/"               >=> text "Hi, I am an API"
                route "/api"            >=> text "Coming soon.."
                route "/api/dbconfig"   >=> getDbConfig dbConfig
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    