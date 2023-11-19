using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MinioTest.Dto
{
    [BsonIgnoreExtraElements]
    public class InvoiceXml
    {
        public ObjectId Id { get; set; }

        public string Ssn { get; set; }

        public string ContentType { get; set; }

        public DateTime CreationTime { get; set; }

        public string FileName { get; set; }

        public string Xml { get; set; }
    }
}
