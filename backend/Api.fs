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

let getAll<'T> (dbConfig : DbConfig) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! result = Db.Service.getAll<'T> dbConfig
            return! json result next ctx
        }

let getById<'T> (dbConfig : DbConfig) (id : string)  =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! result = Db.Service.getById<'T> dbConfig id
            return! json result next ctx
        }

let add<'T> (dbConfig : DbConfig) (model : 'T) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbModel = Db.Service.add<'T> dbConfig model
            return! json dbModel next ctx
        }

let update<'T> (dbConfig : DbConfig) (id : string) (model : 'T) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! dbModel = Db.Service.update<'T> dbConfig id model
            return! json dbModel next ctx
        }

let deleteService (dbConfig : DbConfig) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! cons = Db.Service.getAll<Connection> dbConfig
            if (cons |> Seq.exists (fun x -> x.From.Equals(id) || x.To.Equals(id))) then
                let! _ = Db.Service.delete<Service> dbConfig id
                return! next ctx
            else
                return! badRequest "This service has connections. Delete those first." next ctx
        }

let deleteConnection (dbConfig : DbConfig) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! _ = Db.Service.delete<Connection> dbConfig id
            return! next ctx
        }

let router (dbConfig : DbConfig) : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                route "/"                       >=> text "Hi, I am an Api.."
                route "/api/services"           >=> getAll<Service> dbConfig
                route "/api/connections"        >=> getAll<Connection> dbConfig
                routef "/api/services/%s"       <| fun (id : string) -> getById<Service> dbConfig id
                routef "/api/connections/%s"    <| fun (id : string) -> getById<Connection> dbConfig id
            ]
        POST >=>
            choose [
                route "/api/services"           >=> (requireValid<Service>      <| add<Service> dbConfig)
                route "/api/connections"        >=> (requireValid<Connection>   <| add<Connection> dbConfig)
            ]
        PUT >=>
            choose [
                routef "/api/services/%s"       (requireValid<Service> << update<Service> dbConfig)
                routef "/api/connections/%s"    (requireValid<Connection> << update<Connection> dbConfig)
            ]
        DELETE >=>
            choose [
                routef "/api/services/%s"       <| deleteService dbConfig
                routef "/api/connections/%s"    <| deleteConnection dbConfig
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    