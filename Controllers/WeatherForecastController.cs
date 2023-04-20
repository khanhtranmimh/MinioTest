using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Minio;
using Minio.Exceptions;

using System;
using System.IO;
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
    }
}
