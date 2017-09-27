module Backend.WebApp

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

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message


let webApp : (HttpFunc -> HttpContext -> HttpFuncResult) =
    choose [
        GET >=>
            choose [
                route "/"       >=> text "Hi, I am an API"
                route "/api"    >=> text "Coming soon.."
            ]
        setStatusCode 404 >=> text "Not Found"
    ]    