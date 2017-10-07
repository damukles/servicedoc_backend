module Tests

open System
open Xunit
open Api
open Db.Models
open Giraffe.Tasks
open MongoDB.Driver

// --- BUILD A NICE CHAIN OF TESTS STARTING WITH ADD AND ENDING WITH DELETE --- //

let dbConfig =
    { connectionString = "mongodb://localhost"
    ; database = "servicedoc"
    }

let mutable services : Service List = []

[<Fact>]
let ``Get all services`` () =
    task {
        let! data = Db.Service.getServices dbConfig
        services <- List.ofSeq data
        Assert.NotNull(data)
        Assert.True(data.Count > 1)
    }

[<Fact>]
let ``Get service`` () =
    task {
        let id = "59d91aa2b5553255080b41cf"
        let! data = Db.Service.getService dbConfig id
        Assert.NotNull(data)
        Assert.True(id.Equals(data.Id))
    }

[<Fact>]
let ``Add service`` () =
    task {
        let service = { Id = null; Name = "Test"; HostedOn = "Test"; Description = null }
        let! data = Db.Service.addService dbConfig service
        Assert.NotNull(data.Id)
    }

[<Fact>]
let ``Update service`` () =
    task {
        let myService = { Id = "59d91aa2b5553255080b41cf"; Name = "Test"; HostedOn = "Test"; Description = null }
        let service = { myService with Description = "Updated" }
        let! data = Db.Service.updateService dbConfig myService.Id service
        Assert.NotNull(data.Description)
    }

[<Fact>]
let ``Delete service`` () =
    task {
        let! result = Db.Service.deleteService dbConfig "59d91aa2b5553255080b41cf"
        Assert.True(result.IsAcknowledged && result.DeletedCount > int64(0))
    }