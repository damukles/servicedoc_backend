module Api

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Db.Models
open FSharp.Collections

// The default error handler
let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// End the Http Pipeline calling this instead of next
let finish =
    Some >> System.Threading.Tasks.Task.FromResult

let badRequest responseMessage =
    clearResponse >=> setStatusCode 400 >=> text responseMessage

type ModelState<'e,'a> =
    | Valid of 'a
    | Invalid of string

let validateModel model = 
    let mutable results = List<ValidationResult>()
    if Validator.TryValidateObject(model, (ValidationContext(model, null, null)), results) then
        Valid model
    else
        results
            |> Seq.map (fun (x : ValidationResult) -> x.ErrorMessage)
            |> String.concat ", "
            |> Invalid

// HttpHandler that requires a valid model and passes it to the next function
let requireValid<'T> (modelHandler : 'T -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJson<'T>()
            match validateModel model with
                    | Valid model ->
                        return! modelHandler model next ctx
                    | Invalid results ->
                        return! badRequest results finish ctx
        }

let getServices (dbConfig : DbConfig) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! services = Db.Service.getServices dbConfig
            return! json services next ctx
        }

let getService (dbConfig : DbConfig) (id : string)  =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! services = Db.Service.getService dbConfig id
            return! json services next ctx
        }

let getConnections (dbConfig : DbConfig) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! services = Db.Service.getConnections dbConfig
            return! json services next ctx
        }

let getConnection (dbConfig : DbConfig) (id : string)  =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! services = Db.Service.getConnection dbConfig id
            return! json services next ctx
        }

let addService (dbConfig : DbConfig) (service : Service) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbService = Db.Service.addService dbConfig service
            return! json dbService next ctx
        }

let addConnection (dbConfig : DbConfig) (connection : Connection) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbConnection = Db.Service.addConnection dbConfig connection
            return! json dbConnection next ctx
        }


let updateService (dbConfig : DbConfig) (id : string) (service : Service) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbService = Db.Service.updateService dbConfig id service
            return! json dbService next ctx
        }


let updateConnection (dbConfig : DbConfig) (id : string) (connection : Connection) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbConnection = Db.Service.updateConnection dbConfig id connection
            return! json dbConnection next ctx
        }

let deleteService (dbConfig : DbConfig) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! cons = Db.Service.getConnections dbConfig
            if (cons |> Seq.exists (fun x -> x.From.Equals(id) || x.To.Equals(id))) then
                let! _ = Db.Service.deleteService dbConfig id
                return! next ctx
            else
                return! badRequest "This service has connections. Delete those first." next ctx
        }

let deleteConnection (dbConfig : DbConfig) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! _ = Db.Service.deleteConnection dbConfig id
            return! next ctx
        }

let router (dbConfig : DbConfig) : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                route "/"                       >=> text "Hi, I am an Api.."
                route "/api/services"           >=> getServices dbConfig
                route "/api/connections"        >=> getConnections dbConfig
                routef "/api/services/%s"       <| fun (id : string) -> getService dbConfig id
                routef "/api/connections/%s"    <| fun (id : string) -> getConnection dbConfig id
            ]
        POST >=>
            choose [
                route "/api/services"           >=> (requireValid<Service>      <| addService dbConfig)
                route "/api/connections"        >=> (requireValid<Connection>   <| addConnection dbConfig)
            ]
        PUT >=>
            choose [
                routef "/api/services/%s"       <| fun (id : string) -> requireValid<Service>    <| updateService dbConfig id
                routef "/api/connections/%s"    <| fun (id : string) -> requireValid<Connection> <| updateConnection dbConfig id
            ]
        DELETE >=>
            choose [
                routef "/api/services/%s"       <| fun (id : string) -> deleteService dbConfig id
                routef "/api/connections/%s"    <| fun (id : string) -> deleteConnection dbConfig id
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    