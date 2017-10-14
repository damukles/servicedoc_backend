module Db.Service

open MongoDB.Driver
open MongoDB.Bson
open Models
open Giraffe.Tasks

// --- TRY TO UNIFY WITH GENERICS --- //

let connect (dbConfig : DbConfig) =
    MongoClient(dbConfig.connectionString).GetDatabase(dbConfig.database)

let servicesCollection (dbConfig : DbConfig) =
    (connect dbConfig)
        .GetCollection<Service> "Services"

let connectionsCollection (dbConfig : DbConfig) =
    (connect dbConfig)
        .GetCollection<Connection> "Connections"
    
let getServices (dbConfig : DbConfig) =
    task {
        let! services = (servicesCollection dbConfig)
                            .Find(fun _ -> true).ToListAsync()
        return services
    }

let getService (dbConfig : DbConfig) (id : string) =
    task {
        let! service = (servicesCollection dbConfig)
                            .Find(fun x -> id.Equals(x.Id)).SingleOrDefaultAsync()
        return service
    }

let getConnections (dbConfig : DbConfig) =
    task {
        let! connections = (connectionsCollection dbConfig)
                            .Find(fun _ -> true).ToListAsync()
        return connections
    }

let getConnection (dbConfig : DbConfig) (id : string) =
    task {
        let! connection = (connectionsCollection dbConfig)
                            .Find(fun x -> id.Equals(x.Id)).ToListAsync()
        return connection
    }

let addService (dbConfig : DbConfig) (service : Service) =
    task {
        let! _ = (servicesCollection dbConfig)
                            .InsertOneAsync(service)
        return service
    }

let addConnection (dbConfig : DbConfig) (connection : Connection) =
    task {
        let! _ = (connectionsCollection dbConfig)
                            .InsertOneAsync(connection)
        return connection
    }

let filterById id =
    // WTF, that needs to be better!
    FilterDefinition.op_Implicit(sprintf """{ "_id": ObjectId("%s") }""" id)

let updateService (dbConfig : DbConfig) (id : string) (service : Service) =
    task {
        let options =
            let o = new FindOneAndReplaceOptions<Service, Service>()
            o.ReturnDocument <- ReturnDocument.After
            o
        let dbService = (servicesCollection dbConfig)
                            .FindOneAndReplaceAsync(filterById id, service, options)
        return! dbService
    }

let updateConnection (dbConfig : DbConfig) (id : string) (connection : Connection) =
    task {
        let options =
            let o = new FindOneAndReplaceOptions<Connection, Connection>()
            o.ReturnDocument <- ReturnDocument.After
            o
        let dbConnection = (connectionsCollection dbConfig)
                                .FindOneAndReplaceAsync(filterById id, connection, options)
        return! dbConnection
    }

let deleteService (dbConfig : DbConfig) (id : string) =
    task {
        let! result = (servicesCollection dbConfig)
                            .DeleteOneAsync(fun x -> x.Id.Equals(id))
        return result
    }

let deleteConnection (dbConfig : DbConfig) (id : string) =
    task {
        let! result = (connectionsCollection dbConfig)
                            .DeleteOneAsync(fun x -> x.Id.Equals(id))
        return result
    }