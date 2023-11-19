using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MinioTest.Dto
{
    [BsonIgnoreExtraElements]
    public class InvoiceSsn
    {
        public ObjectId Id { get; set; }

        public string Ssn { get; set; }

        public ObjectId InvoiceXmlId { get; set; }
    }
}
