using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Minio;
using Minio.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MinioTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IConfiguration _configuration;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// GridFS
        /// </summary>
        [HttpGet("gridfs")]
        public void CreateBucket()
        {
            var mongoClient = new MongoClient("connection_string");
            IMongoDatabase database = mongoClient.GetDatabase("database_name");
            var options = new GridFSBucketOptions
            {
                BucketName = "your_bucket_name",
                ChunkSizeBytes = 255 * 1024 //255 MB is the default value
            };
            GridFSBucket bucket = new(database, options);
        }

        /// <summary>
        /// upload file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task UploadAsync(IFormFile file)
        {
            var mongoClient = new MongoClient("connection_string");
            IMongoDatabase database = mongoClient.GetDatabase("database_name");
            GridFSBucket bucket = new GridFSBucket(database);
            var type = file.ContentType.ToString();
            var fileName = file.FileName;

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument { { "FileName", fileName }, { "Type", type } }
            };

            using var stream = await bucket.OpenUploadStreamAsync(fileName, options); // Open the output stream
            var id = stream.Id; // Unique Id of the file
            file.CopyTo(stream); // Copy the contents to the stream
            await stream.CloseAsync();
        }

        //Method 1

        public async Task<byte[]> GetFileByNameAsync(string fileName)
        {
            var mongoClient = new MongoClient("connection_string");
            IMongoDatabase database = mongoClient.GetDatabase("database_name");
            GridFSBucket bucket = new GridFSBucket(database);
            return await bucket.DownloadAsBytesByNameAsync(fileName);
        }


        //Method 2

        public async Task<byte[]> GetFileByIdAsync(string fileName)
        {
            var mongoClient = new MongoClient("connection_string");
            IMongoDatabase database = mongoClient.GetDatabase("database_name");
            GridFSBucket bucket = new GridFSBucket(database);
            var fileInfo = await FindFile(fileName);
            return await bucket.DownloadAsBytesAsync(fileInfo.Id);
        }

        private async Task<GridFSFileInfo> FindFile(string fileName)
        {
            var mongoClient = new MongoClient("connection_string");
            IMongoDatabase database = mongoClient.GetDatabase("database_name");
            GridFSBucket bucket = new GridFSBucket(database);
            var options = new GridFSFindOptions
            {
                Limit = 1
            };
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, fileName);
            using var cursor = await bucket.FindAsync(filter, options);
            return (await cursor.ToListAsync()).FirstOrDefault();
        }

        #region MINIO By PASS SSL
        [HttpGet("minio")]
        public async Task<string> Minio()
        {
            var endpoint = _configuration["Minio:Endpoint"];
            var accessKey = _configuration["Minio:AccessKey"];
            var secretKey = _configuration["Minio:SecretKey"];

            try
            {
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    return true;
                };

                // Pass the handler to httpclient(from you are calling api)
                HttpClient client = new HttpClient(clientHandler);

                var minio = new MinioClient()
                                    .WithEndpoint(endpoint)
                                    .WithCredentials(accessKey, secretKey)
                                    .WithSSL(true)
                                    .WithHttpClient(client)
                                    .Build();
                await UploadFileAsync(minio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return "success";
        }


        private async Task UploadFileAsync(MinioClient minio)
        {
            var bucketName = _configuration["Minio:BucketName"];
            //var location = "us-east-1";
            var objectName = $@"{DateTime.Now.Ticks}.xml";
            var filePath = Path.GetFullPath(_configuration["File:Path"]);
            var contentType = "application/xml";

            try
            {
                // Upload a file to bucket.
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithFileName(filePath)
                    .WithContentType(contentType);
                await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }
            catch (MinioException e)
            {
                _logger.LogError(e.InnerException, e.Message);
            }
        }
        #endregion
    }
}
