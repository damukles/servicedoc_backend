module Db.Service

open MongoDB.Driver
open MongoDB.Bson
open Models
open Giraffe.Tasks

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