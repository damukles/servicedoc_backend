module Db.Models

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes;

type DbConfig =
    { connectionString : string
      database : string }

type Service =
    { [<BsonId>]
      [<BsonRepresentation(BsonType.ObjectId)>]
      Id : string
      Name : string
      HostedOn : string
      Description : string
    }

type Connection =
    { [<BsonId>]
      [<BsonRepresentation(BsonType.ObjectId)>]
      Id : string
      Name : string
      From : string
      To : string
      ConnectionDetails : string
      Authentication : string
      Description : string
    }