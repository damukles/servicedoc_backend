module Api

open Db.Models
open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open MongoDB.Driver

// The default error handler
let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// End the Http Pipeline calling this instead of next
let finish =
    Some >> Threading.Tasks.Task.FromResult

let badRequest responseMessage =
    clearResponse >=> setStatusCode 400 >=> text responseMessage

type ModelState<'e,'a> =
    | Valid of 'a
    | Invalid of ValidationResult seq

let validateModel model = 
    let mutable results = List<ValidationResult>()
    if Validator.TryValidateObject(model, (ValidationContext(model, null, null)), results) then
        Valid model
    else
        Invalid results

// HttpHandler that requires a valid model and passes it to the next function
let requireValid<'T> (modelHandler : 'T -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJson<'T>()
            match validateModel model with
                | Valid model ->
                    return! modelHandler model next ctx
                | Invalid results ->
                    let strResult =
                        results
                            |> Seq.map (fun (x : ValidationResult) -> x.ErrorMessage)
                            |> String.concat ", "
                    return! badRequest strResult finish ctx
        }

let getAll<'T> (dbClient : IMongoDatabase) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! result = Db.Service.getAll<'T> dbClient
            return! json result next ctx
        }

let getById<'T> (dbClient : IMongoDatabase) (id : string)  =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! result = Db.Service.getById<'T> dbClient id
            return! json result next ctx
        }

let add<'T> (dbClient : IMongoDatabase) (model : 'T) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbModel = Db.Service.add<'T> dbClient model
            return! json dbModel next ctx
        }

let update<'T> (dbClient : IMongoDatabase) (id : string) (model : 'T) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbModel = Db.Service.update<'T> dbClient id model
            return! json dbModel next ctx
        }

let deleteService (dbClient : IMongoDatabase) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! cons = Db.Service.getAll<Connection> dbClient
            if (cons |> Seq.exists (fun x -> x.From.Equals(id) || x.To.Equals(id))) then
                let! _ = Db.Service.delete<Service> dbClient id
                return! next ctx
            else
                return! badRequest "This service has connections. Delete those first." next ctx
        }

let deleteConnection (dbClient : IMongoDatabase) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! _ = Db.Service.delete<Connection> dbClient id
            return! next ctx
        }

let router (dbClient : IMongoDatabase) : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                route "/"                       >=> text "Hi, I am an Api.."
                route "/api/services"           >=> getAll<Service> dbClient
                route "/api/connections"        >=> getAll<Connection> dbClient
                routef "/api/services/%s"       <| getById<Service> dbClient
                routef "/api/connections/%s"    <| getById<Connection> dbClient
            ]
        POST >=>
            choose [
                route "/api/services"           >=> (requireValid<Service>      <| add<Service> dbClient)
                route "/api/connections"        >=> (requireValid<Connection>   <| add<Connection> dbClient)
            ]
        PUT >=>
            choose [
                routef "/api/services/%s"       (requireValid<Service> << update<Service> dbClient)
                routef "/api/connections/%s"    (requireValid<Connection> << update<Connection> dbClient)
            ]
        DELETE >=>
            choose [
                routef "/api/services/%s"       <| deleteService dbClient
                routef "/api/connections/%s"    <| deleteConnection dbClient
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    