module Db.Models

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes;
open System.ComponentModel.DataAnnotations;

[<CLIMutable>]
type DbConfig =
    {   
        connectionString : string
        database : string
    }

[<CLIMutable>]
type Service =
    {   
        [<BsonId>]
        [<BsonRepresentation(BsonType.ObjectId)>]
        Id : string
        [<Required>]
        Name : string
        [<Required>]
        HostedOn : string
        Description : string
    }

[<CLIMutable>]
type Connection =
    {
        [<BsonId>]
        [<BsonRepresentation(BsonType.ObjectId)>]
        Id : string
        [<Required>]
        Name : string
        [<Required>]
        From : string
        [<Required>]
        To : string
        [<Required>]
        ConnectionType : string
        ConnectionDetails : string
        Authentication : string
        Description : string
    }