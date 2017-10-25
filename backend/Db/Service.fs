module Db.Service

open MongoDB.Driver
open Models
open Giraffe.Tasks


let connect (dbConfig : DbConfig) =
    MongoClient(dbConfig.connectionString).GetDatabase(dbConfig.database)

let collection<'T> (dbConfig : DbConfig) : IMongoCollection<'T> =
    (connect dbConfig)
        .GetCollection<'T> (typeof<'T>.Name + "s")

let filterById id =
    // WTF, that needs to be better!
    FilterDefinition.op_Implicit(sprintf """{ "_id": ObjectId("%s") }""" id)

// let inline filterById<'a when 'a : ( member Id : string)> id =
//     fun x -> id = (^a : (member Id : string) x)

let getAll<'T> (dbConfig :  DbConfig) =
    task {
        return! (collection<'T> dbConfig)
                    .Find(fun _ -> true)
                    .ToListAsync();
    }

let getById<'T> (dbConfig : DbConfig) (id : string) =
    task {
        return! (collection<'T> dbConfig)
                    .Find(filterById id)
                    .SingleOrDefaultAsync()
    }

let add<'T> (dbConfig : DbConfig) (model : 'T) =
    task {
        (collection<'T> dbConfig)
            .InsertOneAsync(model)
            |> ignore
        return model
    }

let update<'T> (dbConfig : DbConfig) (id : string) (model : 'T) =
    task {
        let options =
            let o = new FindOneAndReplaceOptions<'T, 'T>()
            o.ReturnDocument <- ReturnDocument.After
            o
        let dbService = (collection<'T> dbConfig)
                            .FindOneAndReplaceAsync(filterById id, model, options)
        return! dbService
    }

let delete<'T> (dbConfig : DbConfig) (id : string) =
    task {
        return! (collection<'T> dbConfig)
                    .DeleteOneAsync(filterById id)
    }
