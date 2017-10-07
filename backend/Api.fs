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

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

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

// NO NEED TO MAP A FULL PIPELINE FUNCTION
let mapValid (func : 'a -> HttpFunc -> HttpContext -> HttpFuncResult) validState : (HttpFunc -> HttpContext -> HttpFuncResult) =
        match validState with
        | Valid model ->
            func model
        | Invalid results ->
            badRequest results

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

let addService (dbConfig : DbConfig) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! service = ctx.BindJson<Service>()
            let result = validateModel service
                            |> mapValid
                                (fun (service : Service) (next : HttpFunc) (ctx : HttpContext)  ->
                                    task {
                                        let! dbService = Db.Service.addService dbConfig service
                                        return! json dbService next ctx
                                    }
                                )
            return! result next ctx
        }

let addConnection (dbConfig : DbConfig) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! connection = ctx.BindJson<Connection>()
            let result = validateModel connection
                            |> mapValid
                                (fun (connection : Connection) (next : HttpFunc) (ctx : HttpContext)  ->
                                    task {
                                        let! dbConnection = Db.Service.addConnection dbConfig connection
                                        return! json dbConnection next ctx
                                    }
                                )
            return! result next ctx
        }

let updateService (dbConfig : DbConfig) (id : string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! service = ctx.BindJson<Service>()
            let result = validateModel service
                            |> mapValid
                                (fun (service : Service) (next : HttpFunc) (ctx : HttpContext)  ->
                                    task {
                                        let! dbService = Db.Service.updateService dbConfig id service
                                        return! json dbService next ctx
                                    }
                                )
            return! result next ctx                                  
        }

let updateConnection (dbConfig : DbConfig) (id : string) =
        fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! connection = ctx.BindJson<Connection>()
            let result = validateModel connection                            
                            |> mapValid
                                (fun (connection : Connection) (next : HttpFunc) (ctx : HttpContext)  ->
                                    task {
                                        let! dbConnection = Db.Service.updateConnection dbConfig id connection
                                        return! json dbConnection next ctx
                                    }
                                )
            return! result next ctx
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
                route "/api/services"           >=> addService dbConfig
                route "/api/connections"        >=> addConnection dbConfig
            ]
        PUT >=>
            choose [
                routef "/api/services/%s"       <| fun (id : string) -> updateService dbConfig id
                routef "/api/connections/%s"    <| fun (id : string) -> updateConnection dbConfig id
            ]
        DELETE >=>
            choose [
                routef "/api/services/%s"       <| fun (id : string) -> deleteService dbConfig id
                routef "/api/connections/%s"    <| fun (id : string) -> deleteConnection dbConfig id
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    