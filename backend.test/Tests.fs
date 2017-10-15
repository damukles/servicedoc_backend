module Tests

open System
open Xunit
open Api
open Db.Models
open Giraffe.Tasks
open MongoDB.Driver

let dbConfig =
    { connectionString = "mongodb://localhost"
    ; database = "servicedoc"
    }

let getAllServices () =
    task {
        let! data = Db.Service.getAll<Service> dbConfig
        Assert.NotNull(data)
        Assert.True(data.Count > 1)
        return data
    }

let getService (id : string) =
    task {
        let! data = Db.Service.getById<Service> dbConfig id
        Assert.NotNull(data)
        Assert.True(id.Equals(data.Id))
        return data
    }

let addService (service : Service)=
    task {
        let! data = Db.Service.add<Service> dbConfig service
        Assert.NotNull(data.Id)
        return data
    }

let updateService (service : Service) =
    task {
        let! data = Db.Service.update<Service> dbConfig service.Id service
        Assert.NotNull(data.Description)
        return data
    }

let deleteService (service : Service) =
    task {
        let! result = Db.Service.delete<Service> dbConfig service.Id
        Assert.True(result.IsAcknowledged && result.DeletedCount > int64(0))
        return result
    }

[<Fact>]
let ``Full CRUD Test`` () =
    task {
        let! allServices = getAllServices ()
        let! addedService = addService { Id = null; Name = "Test"; HostedOn = "Test"; Description = null }
        let! gotService = getService addedService.Id
        let! updatedService = updateService { gotService with Description = "TestUpdate" }
        let! deleteResult = deleteService updatedService
        ()
    }