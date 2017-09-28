module WebApp

open System
open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Db.Models

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// let noDbConfigError =
//     clearResponse >=> setStatusCode 500 >=> text "Database Config not found or corrupted."

// let withDbConfig (dbConfig : DbConfig option) (func : DbConfig -> HttpFunc -> HttpContext -> HttpFuncResult) =
//     match dbConfig with
//         | Some config ->
//             func config
//         | None ->
//             noDbConfigError

let getDbConfig (dbConfig : DbConfig) =
    text <| String.Join(" - ", [ dbConfig.connectionString ; dbConfig.database ])

let getServices (dbConfig : DbConfig) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! services = Db.Service.getServices dbConfig
            return! json services next ctx
        }

let webApp (dbConfig : DbConfig) : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                route "/"               >=> text "Hi, I am an API"
                route "/api/dbconfig"   >=> getDbConfig dbConfig
                route "/api/services"   >=> getServices dbConfig
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    