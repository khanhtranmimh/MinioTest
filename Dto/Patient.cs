// start-patientusing MongoDB.Bson;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MinioTest.Dto
{
    [BsonIgnoreExtraElements]
    public class Patient
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public PatientRecord Record { get; set; }
    }
}
