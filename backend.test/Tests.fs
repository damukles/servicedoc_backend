module Tests

open Xunit
open Db.Models
open FSharp.Control.Tasks

let db =
    { connectionString = "mongodb://localhost"
    ; database = "servicedoc"
    }
    |> Db.Service.getClient

let getAllServices () =
    task {
        let! data = Db.Service.getAll<Service> db
        Assert.NotNull(data)
        Assert.True(data.Count > 1)
        return data
    }

let getService (id : string) =
    task {
        let! data = Db.Service.getById<Service> db id
        Assert.NotNull(data)
        Assert.True(id.Equals(data.Id))
        return data
    }

let addService (service : Service)=
    task {
        let! data = Db.Service.add<Service> db service
        Assert.NotNull(data.Id)
        return data
    }

let updateService (service : Service) =
    task {
        let! data = Db.Service.update<Service> db service.Id service
        Assert.NotNull(data.Description)
        return data
    }

let deleteService (service : Service) =
    task {
        let! result = Db.Service.delete<Service> db service.Id
        Assert.True(result.IsAcknowledged && result.DeletedCount > int64(0))
        return result
    }

[<Fact>]
let ``Full CRUD Test`` () =
    task {
        let! _ = getAllServices ()
        let! addedService = addService { Id = null; Name = "Test"; HostedOn = "Test"; Description = null }
        let! gotService = getService addedService.Id
        let! updatedService = updateService { gotService with Description = "TestUpdate" }
        let! _ = deleteService updatedService
        ()
    }