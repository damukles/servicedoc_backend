module Db.Service

open MongoDB.Driver
open Models
open Giraffe.Tasks


let getClient (dbConfig : DbConfig) =
    MongoClient(dbConfig.connectionString).GetDatabase(dbConfig.database)

let collection<'T> (client : IMongoDatabase) : IMongoCollection<'T> =
    client
        .GetCollection<'T> (typeof<'T>.Name + "s")

let filterById id =
    // WTF, that needs to be better!
    FilterDefinition.op_Implicit(sprintf """{ "_id": ObjectId("%s") }""" id)

// let inline filterById<'a when 'a : ( member Id : string)> id =
//     fun x -> id = (^a : (member Id : string) x)

let getAll<'T> (client : IMongoDatabase) =
    task {
        return! (client |> collection<'T>)
                    .Find(fun _ -> true)
                    .ToListAsync();
    }

let getById<'T> (client : IMongoDatabase) (id : string) =
    task {
        return! (client |> collection<'T>)
                    .Find(filterById id)
                    .SingleOrDefaultAsync()
    }

let add<'T> (client : IMongoDatabase) (model : 'T) =
    task {
        (client |> collection<'T>)
            .InsertOneAsync(model)
            |> ignore
        return model
    }

let update<'T> (client : IMongoDatabase) (id : string) (model : 'T) =
    task {
        let options =
            let o = new FindOneAndReplaceOptions<'T, 'T>()
            o.ReturnDocument <- ReturnDocument.After
            o
        let dbService = (client |> collection<'T>)
                            .FindOneAndReplaceAsync(filterById id, model, options)
        return! dbService
    }

let delete<'T> (client : IMongoDatabase) (id : string) =
    task {
        return! (client |> collection<'T>)
                    .DeleteOneAsync(filterById id)
    }
