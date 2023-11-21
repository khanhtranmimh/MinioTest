using Microsoft.Extensions.Configuration;

using MinioTest.Dto;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinioTest.Service
{
    public static class QueryableEncryptionTutorial
    {
        public static async void RunExample()
        {
            var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);

            // start-setup-application-variables
            // KMS provider name should be one of the following: "aws", "gcp", "azure", "kmip" or "local"
            // The KMS you're using to store your Customer Master Key
            const string kmsProviderName = "local";

            //The database in MongoDB where your data encryption keys (DEKs) will be stored
            const string keyVaultDatabaseName = "encryption";

            // The collection in MongoDB where your DEKs will be stored
            const string keyVaultCollectionName = "__keyVault";

            //The namespace in MongoDB where your DEKs will be stored.
            //Set keyVaultNamespace to a new CollectionNamespace object
            //whose name is the values of the keyVaultDatabaseName
            //and keyVaultCollectionName variables, separated by a period.
            var keyVaultNamespace =
                CollectionNamespace.FromFullName($"{keyVaultDatabaseName}.{keyVaultCollectionName}");

            // The database in MongoDB where your encrypted data will be stored.
            const string encryptedDatabaseName = "medicalRecords";

            // The collection in MongoDB where your encrypted data will be stored.
            const string encryptedCollectionName = "patients";

            var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = appSettings["MongoDbUri"];
            // end-setup-application-variables

            var qeHelpers = new QueryableEncryptionHelpers(appSettings);
            var kmsProviderCredentials = qeHelpers.GetKmsProviderCredentials(kmsProviderName,
                generateNewLocalKey: true);

            // start-create-client
            var clientSettings = MongoClientSettings.FromConnectionString(uri);
            clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviderCredentials);
            var encryptedClient = new MongoClient(clientSettings);
            // end-create-client

            var keyDatabase = encryptedClient.GetDatabase(keyVaultDatabaseName);

            // Drop the collection in case you created it in a previous run of this application.
            keyDatabase.DropCollection(keyVaultCollectionName);

            // start-encrypted-fields-map
            var encryptedFields = new BsonDocument
            {
                {
                    "fields", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "keyId", BsonNull.Value },
                            { "path", "record.ssn" },
                            { "bsonType", "string" },
                            { "queries", new BsonDocument("queryType", "equality") }
                        },
                        new BsonDocument
                        {
                            { "keyId", BsonNull.Value },
                            { "path", "record.billing" },
                            { "bsonType", "object" }
                        }
                    }
                }
            };
            // end-encrypted-fields-map

            var patientDatabase = encryptedClient.GetDatabase(encryptedDatabaseName);
            patientDatabase.DropCollection(encryptedCollectionName);

            var clientEncryption = qeHelpers.GetClientEncryption(encryptedClient,
                keyVaultNamespace,
                kmsProviderCredentials);

            var customerMasterKeyCredentials = qeHelpers.GetCustomerMasterKeyCredentials(kmsProviderName);

            try
            {
                // start-create-encrypted-collection
                var createCollectionOptions = new CreateCollectionOptions<Patient>
                {
                    EncryptedFields = encryptedFields
                };

                clientEncryption.CreateEncryptedCollection(patientDatabase,
                    encryptedCollectionName,
                    createCollectionOptions,
                    kmsProviderName,
                    customerMasterKeyCredentials);
                // end-create-encrypted-collection
            }
            catch (Exception e)
            {
                throw new Exception("Unable to create encrypted collection due to the following error: " + e.Message);
            }

            // start-insert-document
            var patient = new Patient
            {
                Name = "Jon Doe",
                Id = new ObjectId(),
                Record = new PatientRecord
                {
                    Ssn = "987-65-4320",
                    Billing = new PatientBilling
                    {
                        CardType = "Visa",
                        CardNumber = 1111
                    }
                }
            };

            var encryptedCollection = encryptedClient.GetDatabase(encryptedDatabaseName).
                GetCollection<Patient>(encryptedCollectionName);

            encryptedCollection.InsertOne(patient);
            // end-insert-document

            // start-find-document
            var ssnFilter = Builders<Patient>.Filter.Eq("record.ssn", patient.Record.Ssn);
            var findResult = await encryptedCollection.Find(ssnFilter).ToCursorAsync();

            var data = findResult.FirstOrDefault().ToJson();
            // end-find-document
        }

        public static async void InvoiceExample()
        {
            var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);

            // start-setup-application-variables
            // KMS provider name should be one of the following: "aws", "gcp", "azure", "kmip" or "local"
            // The KMS you're using to store your Customer Master Key
            const string kmsProviderName = "local";

            //The database in MongoDB where your data encryption keys (DEKs) will be stored
            const string keyVaultDatabaseName = "encryption";

            // The collection in MongoDB where your DEKs will be stored
            const string keyVaultCollectionName = "__keyVault";

            //The namespace in MongoDB where your DEKs will be stored.
            //Set keyVaultNamespace to a new CollectionNamespace object
            //whose name is the values of the keyVaultDatabaseName
            //and keyVaultCollectionName variables, separated by a period.
            var keyVaultNamespace =
                CollectionNamespace.FromFullName($"{keyVaultDatabaseName}.{keyVaultCollectionName}");

            // The database in MongoDB where your encrypted data will be stored.
            const string encryptedDatabaseName = "invoice";

            // The collection in MongoDB where your encrypted data will be stored.
            const string encryptedCollectionName = "invoiceXml";

            const string invoiceSsnCollectionName = "invoiceSsn";

            var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = appSettings["MongoDbUri"];
            // end-setup-application-variables

            var qeHelpers = new QueryableEncryptionHelpers(appSettings);
            var kmsProviderCredentials = qeHelpers.GetKmsProviderCredentials(kmsProviderName,
                generateNewLocalKey: true);

            // start-create-client
            var clientSettings = MongoClientSettings.FromConnectionString(uri);
            clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviderCredentials);
            var encryptedClient = new MongoClient(clientSettings);
            // end-create-client

            var keyDatabase = encryptedClient.GetDatabase(keyVaultDatabaseName);

            // Drop the collection in case you created it in a previous run of this application.
            keyDatabase.DropCollection(keyVaultCollectionName);

            // start-encrypted-fields-map
            var encryptedFields = new BsonDocument
            {
                {
                    "fields", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "keyId", BsonNull.Value },
                            { "path", "ssn" },
                            { "bsonType", "string" },
                            { "queries", new BsonDocument("queryType", "equality") }
                        },
                        new BsonDocument
                        {
                            { "keyId", BsonNull.Value },
                            { "path", "xml" },
                            { "bsonType", "string" }
                        }
                    }
                }
            };
            // end-encrypted-fields-map

            var patientDatabase = encryptedClient.GetDatabase(encryptedDatabaseName);
            patientDatabase.DropCollection(encryptedCollectionName);

            var clientEncryption = qeHelpers.GetClientEncryption(encryptedClient,
                keyVaultNamespace,
                kmsProviderCredentials);

            var customerMasterKeyCredentials = qeHelpers.GetCustomerMasterKeyCredentials(kmsProviderName);

            try
            {
                // start-create-encrypted-collection
                var createCollectionOptions = new CreateCollectionOptions<InvoiceXml>
                {
                    EncryptedFields = encryptedFields
                };

                clientEncryption.CreateEncryptedCollection(patientDatabase,
                    encryptedCollectionName,
                    createCollectionOptions,
                    kmsProviderName,
                    customerMasterKeyCredentials);
                // end-create-encrypted-collection
            }
            catch (Exception e)
            {
                throw new Exception("Unable to create encrypted collection due to the following error: " + e.Message);
            }

            
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><HDon><DLHDon Id=\"Id-22884\"><TTChung><PBan>2.0.1</PBan><THDon>HÓA ĐƠN GIÁ TRỊ GIA TĂNG</THDon><KHMSHDon>1</KHMSHDon><KHHDon>K23TLL</KHHDon><SHDon>226</SHDon><NLap>2023-10-19</NLap><DVTTe>VND</DVTTe><TGia>1</TGia><HTTToan>TM/CK</HTTToan><MSTTCGP>0101352495</MSTTCGP></TTChung><NDHDon><NBan><Ten>VNPAY - KIỂM THỬ HĐĐT KHÔNG MÃ</Ten><MST>0102182292-998</MST><DChi>Tầng 8, Số 22, phố Láng Hạ, - Phường Láng Hạ - Quận Đống đa - Hà Nội.</DChi><SDThoai>012345678</SDThoai><DCTDTu>khanhtm@vnpay.vn</DCTDTu></NBan><NMua><Ten>DevTestHuyTangGiam</Ten><DChi>DevTestHuyTangGiam</DChi><MKHang>DevTestHuyTangGiam</MKHang><HVTNMHang>DevTestHuyTangGiam</HVTNMHang></NMua><DSHHDVu><HHDVu><TChat>1</TChat><STT>1</STT><MHHDVu>55ded9a9</MHHDVu><THHDVu>testKytheogio</THHDVu><DVTinh>Kg</DVTinh><SLuong>1</SLuong><DGia>0</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>0</ThTien><TSuat>0%</TSuat></HHDVu></DSHHDVu><TToan><TgTCThue>0</TgTCThue><TgTThue>0</TgTThue><TTCKTMai>0</TTCKTMai><TgTTTBSo>0</TgTTTBSo><TgTTTBChu>Không đồng</TgTTTBChu><THTTLTSuat><LTSuat><TSuat>0%</TSuat><ThTien>0</ThTien><TThue>0</TThue></LTSuat></THTTLTSuat></TToan><TTKhac><TTin><TTruong>Mã bảo mật</TTruong><KDLieu>string</KDLieu><DLieu>23b63e8b</DLieu></TTin></TTKhac></NDHDon></DLHDon><DLQRCode>00020101021202142657895426548904141568265489515426280010A00000077501100107001729520454995303704540105802VN5905VNPAY6005HANOI622801032260307CTYVNIS0706XYZ00199700010A00000077501130102182292998020110306K23TLL0403226050820231019060106304F223</DLQRCode><DSCKS><NBan><Signature Id=\"NBan-22884\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#Id-22884\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>OfuzGwxo3Cc2g79KHGrmJ3Dw8Fo=</DigestValue></Reference><Reference URI=\"#SigningTime-22884\" Type=\"https://vninvoice.vn/2021/xmldsig#SignatureProperties\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AmdU7cs+Z156chX2REos1Kxh4vI=</DigestValue></Reference></SignedInfo><SignatureValue>02j1UOlF3ssDbelB6NdkpUMKT8tC4qogXRPgPppzqt4XYWNbu0DnthX38roEASARF1pzcK635gNhXOb0aExkwpXn8I8m6cnjb9qZRBYPL6CGaj6XjpU69TkC/qtoyli1wZY8Lu/Nli0hPBjPXXgQPgtrtFLVJcA1aMUuhuGYZO0=</SignatureValue><KeyInfo><X509Data><X509SubjectName>CN=\"KHÁNH THDVTCDT TEST CTS HẾT HẠN ỐZỀ L=HA NOI S=HA NOI C=VN MST=0102182292-998\"</X509SubjectName><X509Certificate>MIIDszCCAyCgAwIBAgIQhbXut+xQi7ZN21koidFsGTAJBgUrDgMCHQUAMIGoMYGlMIGiBgNVBAMegZoASwBIAMEATgBIACAAVABIAEQAVgBUAEMARABUACAAVABFAFMAVAAgAEMAVABTACAASB6+AFQAIABIHqAATgAgHtAAWh7AACAATAA9AEgAQQAgAE4ATwBJACAAUwA9AEgAQQAgAE4ATwBJACAAQwA9AFYATgAgAE0AUwBUAD0AMAAxADAAMgAxADgAMgAyADkAMgAtADkAOQA4MB4XDTE3MDEwOTE3MDAwMFoXDTIzMTEwMTE3MDAwMFowgagxgaUwgaIGA1UEAx6BmgBLAEgAwQBOAEgAIABUAEgARABWAFQAQwBEAFQAIABUAEUAUwBUACAAQwBUAFMAIABIHr4AVAAgAEgeoABOACAe0ABaHsAAIABMAD0ASABBACAATgBPAEkAIABTAD0ASABBACAATgBPAEkAIABDAD0AVgBOACAATQBTAFQAPQAwADEAMAAyADEAOAAyADIAOQAyAC0AOQA5ADgwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBAOxVneHBZS717Nka1wrGBbupDhlmSdX4jf7gC0KmE03jCOm34hHd1TgTxp9o1+AAr5kWq/RB8HKuoo8LjT1ETKLTQ5CJFD52XcEU8kprEPWY7xakAMTQt7UpT57HMXVkbboxe1Fnj2+0knEBUgU7kjBjmEoMexAgxrqO1WL7B/3ZAgMBAAGjgeMwgeAwgd0GA1UdAQSB1TCB0oAQ+BGWDbGFZ64YhU7SeCgbGqGBqzCBqDGBpTCBogYDVQQDHoGaAEsASADBAE4ASAAgAFQASABEAFYAVABDAEQAVAAgAFQARQBTAFQAIABDAFQAUwAgAEgevgBUACAASB6gAE4AIB7QAFoewAAgAEwAPQBIAEEAIABOAE8ASQAgAFMAPQBIAEEAIABOAE8ASQAgAEMAPQBWAE4AIABNAFMAVAA9ADAAMQAwADIAMQA4ADIAMgA5ADIALQA5ADkAOIIQhbXut+xQi7ZN21koidFsGTAJBgUrDgMCHQUAA4GBAFRkKvRFUCFDLJmPmbbOA3Fl6gaQKbJytAd4ykt7qHGLVfPGRjdl3kHhw4EK7eEmdHIC7ae6+kMuecGpglRAvDDDqz4bi1kLTPOjcatoTsVcgPETcGUH/lxnPktJ5gfe6CR5+2iobKt1NqLtShzPIsHv0/QD//PxddpYcsivn63a</X509Certificate></X509Data></KeyInfo><Object Id=\"SigningTime-22884\"><SignatureProperties><SignatureProperty Target=\"signatureProperties\"><SigningTime>2023-10-19T11:11:51</SigningTime></SignatureProperty></SignatureProperties></Object></Signature></NBan></DSCKS></HDon>";

            // start-insert-document
            //var invoiceXml = new InvoiceXml
            //{
            //    Id = new ObjectId(),
            //    Ssn = ssn,
            //    ContentType = "application/xml",
            //    CreationTime = DateTime.Now,
            //    FileName = $"0102182292-998-1-K23TLL-{DateTime.Now.Ticks}.xml",
            //    Xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml))
            //};

            var encryptedCollection = encryptedClient.GetDatabase(encryptedDatabaseName).
                GetCollection<InvoiceXml>(encryptedCollectionName);

            var mongoClient = new MongoClient("mongodb://localhost:27888/?directConnection=true");
            var invoiceSsnCollection = mongoClient.GetDatabase(encryptedDatabaseName)
                .GetCollection<InvoiceSsn>(invoiceSsnCollectionName);

            for (int i = 0; i < 10; i++)
            {
                var ssn = Guid.NewGuid().ToString();

                var invoiceXml = new InvoiceXml
                {
                    Id = new ObjectId(),
                    Ssn = ssn,
                    ContentType = "application/xml",
                    CreationTime = DateTime.Now,
                    FileName = $"0102182292-998-1-K23TLL-{DateTime.Now.Ticks}.xml",
                    Xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml))
                };

                await encryptedCollection.InsertOneAsync(invoiceXml);

                await invoiceSsnCollection.InsertOneAsync(new InvoiceSsn { 
                    Id = new ObjectId(),
                    Ssn = ssn,
                    InvoiceXmlId = invoiceXml.Id
                });
            }
            // end-insert-document

            // start-find-document
            //var ssnFilter = Builders<InvoiceXml>.Filter.Eq("ssn", invoiceXml.Ssn);
            //var findResult = await encryptedCollection.Find(ssnFilter).ToCursorAsync();

            //var data = findResult.FirstOrDefault().ToJson();
            // end-find-document
        }

        public static async Task<string> Upload()
        {
            const string encryptedDatabaseName = "invoice";

            // The collection in MongoDB where your encrypted data will be stored.
            const string encryptedCollectionName = "invoiceXml";

            const string invoiceSsnCollectionName = "invoiceSsn";


            //The database in MongoDB where your data encryption keys (DEKs) will be stored
            const string keyVaultDatabaseName = "encryption";

            // The collection in MongoDB where your DEKs will be stored
            const string keyVaultCollectionName = "__keyVault";

            var keyVaultNamespace =
                CollectionNamespace.FromFullName($"{keyVaultDatabaseName}.{keyVaultCollectionName}");

            const string kmsProviderName = "local";

            var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = appSettings["MongoDbUri"];

            var qeHelpers = new QueryableEncryptionHelpers(appSettings);
            var kmsProviderCredentials = qeHelpers.GetKmsProviderCredentials(kmsProviderName,
                generateNewLocalKey: false);

            // start-create-client
            var clientSettings = MongoClientSettings.FromConnectionString(uri);
            clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviderCredentials);
            var encryptedClient = new MongoClient(clientSettings);
            // end-create-client

            var encryptedCollection = encryptedClient.GetDatabase(encryptedDatabaseName).
                GetCollection<InvoiceXml>(encryptedCollectionName);

            var mongoClient = new MongoClient("mongodb://localhost:27888/?directConnection=true");
            var invoiceSsnCollection = mongoClient.GetDatabase(encryptedDatabaseName)
                .GetCollection<InvoiceSsn>(invoiceSsnCollectionName);

            //var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><HDon><DLHDon Id=\"Id-23776\"><TTChung><PBan>2.0.1</PBan><THDon>HÓA ĐƠN GIÁ TRỊ GIA TĂNG</THDon><KHMSHDon>1</KHMSHDon><KHHDon>K23TKK</KHHDon><SHDon>1063</SHDon><NLap>2023-11-18</NLap><DVTTe>VND</DVTTe><TGia>1</TGia><HTTToan>TM/CK</HTTToan><MSTTCGP>0101352495</MSTTCGP></TTChung><NDHDon><NBan><Ten>VNPAY - KIỂM THỬ HĐĐT KHÔNG MÃ</Ten><MST>0102182292-998</MST><DChi>Tầng 8, Số 22, phố Láng Hạ, - Phường Láng Hạ - Quận Đống đa - Hà Nội.</DChi><SDThoai>012345678</SDThoai><DCTDTu>khanhtm@vnpay.vn</DCTDTu></NBan><NMua><Ten>k</Ten><DChi>k</DChi><MKHang>testcreate</MKHang><HVTNMHang>k</HVTNMHang></NMua><DSHHDVu><HHDVu><TChat>1</TChat><STT>1</STT><MHHDVu>hanghoa01</MHHDVu><THHDVu>hanghoa01</THHDVu><DVTinh>vnd</DVTinh><SLuong>1</SLuong><DGia>10000</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>10000</ThTien><TSuat>10%</TSuat></HHDVu><HHDVu><TChat>1</TChat><STT>2</STT><MHHDVu>dc4a7752</MHHDVu><THHDVu>t</THHDVu><DVTinh>t</DVTinh><SLuong>1</SLuong><DGia>10</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>10</ThTien><TSuat>0%</TSuat></HHDVu><HHDVu><TChat>1</TChat><STT>3</STT><MHHDVu>dc4a7752</MHHDVu><THHDVu>t</THHDVu><DVTinh>t</DVTinh><SLuong>1</SLuong><DGia>10</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>10</ThTien><TSuat>0%</TSuat></HHDVu><HHDVu><TChat>1</TChat><STT>4</STT><MHHDVu>dc4a7752</MHHDVu><THHDVu>t</THHDVu><DVTinh>t</DVTinh><SLuong>1</SLuong><DGia>10</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>10</ThTien><TSuat>0%</TSuat></HHDVu><HHDVu><TChat>1</TChat><STT>5</STT><MHHDVu>4cfebf7a</MHHDVu><THHDVu>aaa</THHDVu><DVTinh>Đĩa</DVTinh><SLuong>1</SLuong><DGia>20000</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>20000</ThTien><TSuat>0%</TSuat></HHDVu><HHDVu><TChat>1</TChat><STT>6</STT><MHHDVu>ceef225d</MHHDVu><THHDVu>aaaa</THHDVu><DVTinh>chi tiết 1 - sửa thay thế lần 1</DVTinh><SLuong>1</SLuong><DGia>0</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>0</ThTien><TSuat>0%</TSuat><TTKhac><TTin><TTruong>Ghi chú</TTruong><KDLieu>string</KDLieu><DLieu>a</DLieu></TTin></TTKhac></HHDVu></DSHHDVu><TToan><TgTCThue>30030</TgTCThue><TgTThue>1000</TgTThue><TTCKTMai>0</TTCKTMai><TgTTTBSo>31030</TgTTTBSo><TgTTTBChu>Ba mươi mốt nghìn không trăm ba mươi đồng</TgTTTBChu><THTTLTSuat><LTSuat><TSuat>10%</TSuat><ThTien>10000</ThTien><TThue>1000</TThue></LTSuat><LTSuat><TSuat>0%</TSuat><ThTien>20030</ThTien><TThue>0</TThue></LTSuat></THTTLTSuat></TToan><TTKhac><TTin><TTruong>Mã bảo mật</TTruong><KDLieu>string</KDLieu><DLieu>a8586ba1</DLieu></TTin></TTKhac></NDHDon></DLHDon><DLQRCode>00020101021202142657895426548904141568265489515426280010A000000775011001070017295204549953037045405310305802VN5905VNPAY6005HANOI6229010410630307CTYVNIS0706XYZ00199750010A00000077501130102182292998020110306K23TKK040410630508202311180605310306304EAD4</DLQRCode><DSCKS><NBan><Signature Id=\"NBan-23776\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#Id-23776\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>jkfxi3ilFHJ4fmUdASYO/BOaayM=</DigestValue></Reference><Reference URI=\"#SigningTime-23776\" Type=\"https://vninvoice.vn/2021/xmldsig#SignatureProperties\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>gQZIORSFcEEv+kIfo4jSO/X1ILM=</DigestValue></Reference></SignedInfo><SignatureValue>nUfmgzab23KvNQrtRhCtowMDKtzHujU+nkFaTA60DwT2aZSLvNvw3jQCTdXCMKwzpQ5h1UCieh1KS34APQcXRZ7rW2MbODKYNmjdrRHxAqOBkuLOTtg/t8NuP5byAkjLRu/HJ3exFeWFNyeATMqNcmjfufjryiB93RLDzsxO8vDC9WjzhDaeI4l1d2aQ5reaZ2cX/SFbmlU0oPMVLAFYaWy7GjfYF4hO6bRnBFPzwR9CHyNnrBdY18VEuhwg39R7pUsDNqJ52Gs7HxTkpD3+PAXeGmmukHEOrTzBynRTfXFtB6QxoZP2MCTMciTsquH4cAE+5lRsSb6glfA5JLjd3w==</SignatureValue><KeyInfo><X509Data><X509SubjectName>OID.0.9.2342.19200300.100.1.1=MST:0102182292-998, CN=CÔNG TY CỔ PHẦN GIẢI PHÁP THANH TOÁN VIỆT NAM KHÔNG MÃ CQT, L=Đống Đa, S=Hà Nội, C=VN</X509SubjectName><X509Certificate>MIIFiTCCBHGgAwIBAgIQVAEBB0AMD8uzzDAl7JFdQDANBgkqhkiG9w0BAQsFADB1MQswCQYDVQQGEwJWTjEoMCYGA1UECgwfQ29uZyB0eSBjbyBwaGFuIGNodSBreSBzbyBWSSBOQTEoMCYGA1UECwwfQ29uZyB0eSBjbyBwaGFuIGNodSBreSBzbyBWSSBOQTESMBAGA1UEAwwJU21hcnRTaWduMB4XDTIyMDIxNDA5MTkyNFoXDTI1MDIxNDA5MTkyNFowga0xCzAJBgNVBAYTAlZOMRIwEAYDVQQIDAlIw6AgTuG7mWkxFDASBgNVBAcMC8SQ4buRbmcgxJBhMVAwTgYDVQQDDEdDw5RORyBUWSBD4buUIFBI4bqmTiBHSeG6okkgUEjDgVAgVEhBTkggVE/DgU4gVknhu4ZUIE5BTSBLSMOUTkcgTcODIENRVDEiMCAGCgmSJomT8ixkAQEMEk1TVDowMTAyMTgyMjkyLTk5ODCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALVG20i0zLuiY4rDDyIkgJOxUjf8SLk0bYO7jPjhJgtPppbKM/BEg6H2sSqIhj7GSLGkOLeew9ZLh+7B2TKhfsVFDnRCDzyTAL01JifoZbem9yyEeEsBLDpNR9Ia5n4rUVzPapFf/A8vsqhEPIa43Zz5RhUPwddvgtx3CkfLkqGHJ2vW4NEA5KOhxwvUEkjWW5yOgdlqmAdy2YZ3Norx/eUyKpWFNF5SAVm9kY7VRYSJdUuEC8ffmwLepH73ktckvZpODiZ5CTnjunJ/nR3hK+Q678z+L6SDwxDOfW6RjeKhRP+SOK4U60LLGS2rxZXH/m5aEVzO2l8v5oZxsJQ4vuUCAwEAAaOCAdowggHWMHIGCCsGAQUFBwEBBGYwZDA1BggrBgEFBQcwAoYpaHR0cHM6Ly9zbWFydHNpZ24uY29tLnZuL3NtYXJ0c2lnbjI1Ni5jcnQwKwYIKwYBBQUHMAGGH2h0dHA6Ly9vY3NwMjU2LnNtYXJ0c2lnbi5jb20udm4wHQYDVR0OBBYEFKnjtfr8qHK4urZs2i5xDJeQ33rhMAwGA1UdEwEB/wQCMAAwHwYDVR0jBBgwFoAU0ApZUhzKisAJ0gQokuqT++NChh8wKAYIKwYBBQUHAQMEHDAaMBgGCCsGAQUFBwsBMAwGCisGAQQBge0DAQcwgZEGA1UdIASBiTCBhjCBgwYKKwYBBAGB7QMBBzB1MEoGCCsGAQUFBwICMD4ePABUAGgAaQBzACAAaQBzACAAYQBjAGMAcgBlAGQAaQB0AGUAZAAgAGMAZQByAHQAaQBmAGkAYwBhAHQAZTAnBggrBgEFBQcCARYbaHR0cDovL3NtYXJ0c2lnbi5jb20udm4vY3BzMC8GA1UdHwQoMCYwJKAioCCGHmh0dHA6Ly9jcmwyNTYuc21hcnRzaWduLmNvbS52bjAOBgNVHQ8BAf8EBAMCBsAwEwYDVR0lBAwwCgYIKwYBBQUHAwQwDQYJKoZIhvcNAQELBQADggEBAHzDUci7WWKfIpjx2dl4zT26meXjir4JkheZZsSw9z+ljx8/TTZ++E4M2b1f3VAyOVWEvnk97tq8A4E+9ueO9lG5Cw+E68oc0NdT6XP+zH/jU3wixnO4BirQKv3+AEsiVGUppNGk0vbz5bTRzL2WXIHAX3qJzXAjGhPq6ajIbhiPb4GaHtCpr0BeXwaSIeaoxPyEhWWGFQoeaZ5xjQ1pf1kZzT4gEyGVolvgc/AvtPJvBVYnVMCBCC2X1tdJCk5u7elNCOJm74Cl8K/0K9mxSDQ+Gz63yePQdfMQsop+1IC5kuvLslZRPrKqwJB3JVhrgFyGg/MTWKatnzgImEKc308=</X509Certificate></X509Data></KeyInfo><Object Id=\"SigningTime-23776\"><SignatureProperties><SignatureProperty Target=\"signatureProperties\"><SigningTime>2023-11-18T21:52:27</SigningTime></SignatureProperty></SignatureProperties></Object></Signature></NBan></DSCKS></HDon>";
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><HDon><DLHDon Id=\"Id-22884\"><TTChung><PBan>2.0.1</PBan><THDon>HÓA ĐƠN GIÁ TRỊ GIA TĂNG</THDon><KHMSHDon>1</KHMSHDon><KHHDon>K23TLL</KHHDon><SHDon>226</SHDon><NLap>2023-10-19</NLap><DVTTe>VND</DVTTe><TGia>1</TGia><HTTToan>TM/CK</HTTToan><MSTTCGP>0101352495</MSTTCGP></TTChung><NDHDon><NBan><Ten>VNPAY - KIỂM THỬ HĐĐT KHÔNG MÃ</Ten><MST>0102182292-998</MST><DChi>Tầng 8, Số 22, phố Láng Hạ, - Phường Láng Hạ - Quận Đống đa - Hà Nội.</DChi><SDThoai>012345678</SDThoai><DCTDTu>khanhtm@vnpay.vn</DCTDTu></NBan><NMua><Ten>DevTestHuyTangGiam</Ten><DChi>DevTestHuyTangGiam</DChi><MKHang>DevTestHuyTangGiam</MKHang><HVTNMHang>DevTestHuyTangGiam</HVTNMHang></NMua><DSHHDVu><HHDVu><TChat>1</TChat><STT>1</STT><MHHDVu>55ded9a9</MHHDVu><THHDVu>testKytheogio</THHDVu><DVTinh>Kg</DVTinh><SLuong>1</SLuong><DGia>0</DGia><TLCKhau>0</TLCKhau><STCKhau>0</STCKhau><ThTien>0</ThTien><TSuat>0%</TSuat></HHDVu></DSHHDVu><TToan><TgTCThue>0</TgTCThue><TgTThue>0</TgTThue><TTCKTMai>0</TTCKTMai><TgTTTBSo>0</TgTTTBSo><TgTTTBChu>Không đồng</TgTTTBChu><THTTLTSuat><LTSuat><TSuat>0%</TSuat><ThTien>0</ThTien><TThue>0</TThue></LTSuat></THTTLTSuat></TToan><TTKhac><TTin><TTruong>Mã bảo mật</TTruong><KDLieu>string</KDLieu><DLieu>23b63e8b</DLieu></TTin></TTKhac></NDHDon></DLHDon><DLQRCode>00020101021202142657895426548904141568265489515426280010A00000077501100107001729520454995303704540105802VN5905VNPAY6005HANOI622801032260307CTYVNIS0706XYZ00199700010A00000077501130102182292998020110306K23TLL0403226050820231019060106304F223</DLQRCode><DSCKS><NBan><Signature Id=\"NBan-22884\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#Id-22884\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>OfuzGwxo3Cc2g79KHGrmJ3Dw8Fo=</DigestValue></Reference><Reference URI=\"#SigningTime-22884\" Type=\"https://vninvoice.vn/2021/xmldsig#SignatureProperties\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>AmdU7cs+Z156chX2REos1Kxh4vI=</DigestValue></Reference></SignedInfo><SignatureValue>02j1UOlF3ssDbelB6NdkpUMKT8tC4qogXRPgPppzqt4XYWNbu0DnthX38roEASARF1pzcK635gNhXOb0aExkwpXn8I8m6cnjb9qZRBYPL6CGaj6XjpU69TkC/qtoyli1wZY8Lu/Nli0hPBjPXXgQPgtrtFLVJcA1aMUuhuGYZO0=</SignatureValue><KeyInfo><X509Data><X509SubjectName>CN=\"KHÁNH THDVTCDT TEST CTS HẾT HẠN ỐZỀ L=HA NOI S=HA NOI C=VN MST=0102182292-998\"</X509SubjectName><X509Certificate>MIIDszCCAyCgAwIBAgIQhbXut+xQi7ZN21koidFsGTAJBgUrDgMCHQUAMIGoMYGlMIGiBgNVBAMegZoASwBIAMEATgBIACAAVABIAEQAVgBUAEMARABUACAAVABFAFMAVAAgAEMAVABTACAASB6+AFQAIABIHqAATgAgHtAAWh7AACAATAA9AEgAQQAgAE4ATwBJACAAUwA9AEgAQQAgAE4ATwBJACAAQwA9AFYATgAgAE0AUwBUAD0AMAAxADAAMgAxADgAMgAyADkAMgAtADkAOQA4MB4XDTE3MDEwOTE3MDAwMFoXDTIzMTEwMTE3MDAwMFowgagxgaUwgaIGA1UEAx6BmgBLAEgAwQBOAEgAIABUAEgARABWAFQAQwBEAFQAIABUAEUAUwBUACAAQwBUAFMAIABIHr4AVAAgAEgeoABOACAe0ABaHsAAIABMAD0ASABBACAATgBPAEkAIABTAD0ASABBACAATgBPAEkAIABDAD0AVgBOACAATQBTAFQAPQAwADEAMAAyADEAOAAyADIAOQAyAC0AOQA5ADgwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBAOxVneHBZS717Nka1wrGBbupDhlmSdX4jf7gC0KmE03jCOm34hHd1TgTxp9o1+AAr5kWq/RB8HKuoo8LjT1ETKLTQ5CJFD52XcEU8kprEPWY7xakAMTQt7UpT57HMXVkbboxe1Fnj2+0knEBUgU7kjBjmEoMexAgxrqO1WL7B/3ZAgMBAAGjgeMwgeAwgd0GA1UdAQSB1TCB0oAQ+BGWDbGFZ64YhU7SeCgbGqGBqzCBqDGBpTCBogYDVQQDHoGaAEsASADBAE4ASAAgAFQASABEAFYAVABDAEQAVAAgAFQARQBTAFQAIABDAFQAUwAgAEgevgBUACAASB6gAE4AIB7QAFoewAAgAEwAPQBIAEEAIABOAE8ASQAgAFMAPQBIAEEAIABOAE8ASQAgAEMAPQBWAE4AIABNAFMAVAA9ADAAMQAwADIAMQA4ADIAMgA5ADIALQA5ADkAOIIQhbXut+xQi7ZN21koidFsGTAJBgUrDgMCHQUAA4GBAFRkKvRFUCFDLJmPmbbOA3Fl6gaQKbJytAd4ykt7qHGLVfPGRjdl3kHhw4EK7eEmdHIC7ae6+kMuecGpglRAvDDDqz4bi1kLTPOjcatoTsVcgPETcGUH/lxnPktJ5gfe6CR5+2iobKt1NqLtShzPIsHv0/QD//PxddpYcsivn63a</X509Certificate></X509Data></KeyInfo><Object Id=\"SigningTime-22884\"><SignatureProperties><SignatureProperty Target=\"signatureProperties\"><SigningTime>2023-10-19T11:11:51</SigningTime></SignatureProperty></SignatureProperties></Object></Signature></NBan></DSCKS></HDon>";

            for (int i = 0; i < 100000; i++)
            {
                var ssn = Guid.NewGuid().ToString();

                var invoiceXml = new InvoiceXml
                {
                    Id = new ObjectId(),
                    Ssn = ssn,
                    ContentType = "application/xml",
                    CreationTime = DateTime.Now,
                    FileName = $"0102182292-998-1-K23TKK-{DateTime.Now.Ticks}.xml",
                    Xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml))
                };

                await encryptedCollection.InsertOneAsync(invoiceXml);

                await invoiceSsnCollection.InsertOneAsync(new InvoiceSsn
                {
                    Id = new ObjectId(),
                    Ssn = ssn,
                    InvoiceXmlId = invoiceXml.Id
                });
            }

            return "Success";
        }

        public static async Task<string> Get(string ssn)
        {
            var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);

            // start-setup-application-variables
            // KMS provider name should be one of the following: "aws", "gcp", "azure", "kmip" or "local"
            // The KMS you're using to store your Customer Master Key
            const string kmsProviderName = "local";

            //The database in MongoDB where your data encryption keys (DEKs) will be stored
            const string keyVaultDatabaseName = "encryption";

            // The collection in MongoDB where your DEKs will be stored
            const string keyVaultCollectionName = "__keyVault";

            //The namespace in MongoDB where your DEKs will be stored.
            //Set keyVaultNamespace to a new CollectionNamespace object
            //whose name is the values of the keyVaultDatabaseName
            //and keyVaultCollectionName variables, separated by a period.
            var keyVaultNamespace =
                CollectionNamespace.FromFullName($"{keyVaultDatabaseName}.{keyVaultCollectionName}");

            // The database in MongoDB where your encrypted data will be stored.
            const string encryptedDatabaseName = "invoice";

            // The collection in MongoDB where your encrypted data will be stored.
            const string encryptedCollectionName = "invoiceXml";

            var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = appSettings["MongoDbUri"];
            // end-setup-application-variables

            var qeHelpers = new QueryableEncryptionHelpers(appSettings);
            var kmsProviderCredentials = qeHelpers.GetKmsProviderCredentials(kmsProviderName,
                generateNewLocalKey: false);

            // start-create-client
            var clientSettings = MongoClientSettings.FromConnectionString(uri);
            clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviderCredentials);
            var encryptedClient = new MongoClient(clientSettings);
            // end-create-client

            var keyDatabase = encryptedClient.GetDatabase(keyVaultDatabaseName);

            var patientDatabase = encryptedClient.GetDatabase(encryptedDatabaseName);

            var clientEncryption = qeHelpers.GetClientEncryption(encryptedClient,
                keyVaultNamespace,
                kmsProviderCredentials);

            var customerMasterKeyCredentials = qeHelpers.GetCustomerMasterKeyCredentials(kmsProviderName);

            var encryptedCollection = encryptedClient.GetDatabase(encryptedDatabaseName).
                GetCollection<InvoiceXml>(encryptedCollectionName);

            // start-find-document
            var ssnFilter = Builders<InvoiceXml>.Filter.Eq("ssn", ssn);
            var findResult = await encryptedCollection.Find(ssnFilter).ToCursorAsync();

            var data = findResult.FirstOrDefault().ToJson();
            return data;
            // end-find-document
        }

        public static async Task<object> UploadDocumentAsync(string xml)
        {
            const string encryptedDatabaseName = "invoice";

            // The collection in MongoDB where your encrypted data will be stored.
            const string encryptedCollectionName = "invoiceXml";

            const string invoiceSsnCollectionName = "invoiceSsn";


            //The database in MongoDB where your data encryption keys (DEKs) will be stored
            const string keyVaultDatabaseName = "encryption";

            // The collection in MongoDB where your DEKs will be stored
            const string keyVaultCollectionName = "__keyVault";

            var keyVaultNamespace =
                CollectionNamespace.FromFullName($"{keyVaultDatabaseName}.{keyVaultCollectionName}");

            const string kmsProviderName = "local";

            var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = appSettings["MongoDbUri"];

            var qeHelpers = new QueryableEncryptionHelpers(appSettings);
            var kmsProviderCredentials = qeHelpers.GetKmsProviderCredentials(kmsProviderName,
                generateNewLocalKey: false);

            // start-create-client
            var clientSettings = MongoClientSettings.FromConnectionString(uri);
            clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviderCredentials);
            var encryptedClient = new MongoClient(clientSettings);
            // end-create-client

            var encryptedCollection = encryptedClient.GetDatabase(encryptedDatabaseName).
                GetCollection<InvoiceXml>(encryptedCollectionName);

            var mongoClient = new MongoClient("mongodb://localhost:27888/?directConnection=true");
            var invoiceSsnCollection = mongoClient.GetDatabase(encryptedDatabaseName)
                .GetCollection<InvoiceSsn>(invoiceSsnCollectionName);
           
            var ssn = Guid.NewGuid().ToString();

            var invoiceXml = new InvoiceXml
            {
                Id = new ObjectId(),
                Ssn = ssn,
                ContentType = "application/xml",
                CreationTime = DateTime.Now,
                FileName = $"0102182292-998-1-K23TKK-{DateTime.Now.Ticks}.xml",
                Xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml))
            };

            await encryptedCollection.InsertOneAsync(invoiceXml);

            await invoiceSsnCollection.InsertOneAsync(new InvoiceSsn
            {
                Id = new ObjectId(),
                Ssn = ssn,
                InvoiceXmlId = invoiceXml.Id
            });

            return new
            {
                Ssn = ssn,
                InvoiceXmlId = invoiceXml.Id,
                FileName = invoiceXml.FileName,
                ContentType = "application/xml"
            };
        }
    }
}
