using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System.ComponentModel;
using System.Globalization;
using tyre_reporting_app_api.Interfaces;
using tyre_reporting_app_api.Models;
using System.Text.Json;

namespace tyre_reporting_app_api.Services
{
    public class StorageInteractor : IStorageInteractor
    {
        private readonly string _connectionString;
        private readonly BlobContainerClient _blobContainerClient;
        private const string ContainerName = "t-r-app";
        private const string JobsDirectory = "jobs/";
        private const string InitFileName = "init.json";
        private const string JobDescriptionsFileName = "jobDescriptions.json";

        public StorageInteractor(IConfiguration configuration)
        {
            _connectionString = configuration?.GetConnectionString("AzureBlobStorage") ?? throw new NullReferenceException();
            var blobServiceClient = new BlobServiceClient(_connectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        }

        public async Task<bool> CreateContainer(string regNumber, string user, DateTime date)
        {
            string localPath = Path.Combine(JobsDirectory, GetJobFolderName(regNumber, date));
            Directory.CreateDirectory(localPath);

            string fileName = "init.json";
            string localFilePath = Path.Combine(localPath, fileName);
            var initJobInfo = new MetadataDto(user);
            var initJobInfoJson = JsonSerializer.Serialize(initJobInfo);
            await File.WriteAllTextAsync(localFilePath, initJobInfoJson);


            var blobClient = _blobContainerClient.GetBlobClient(localFilePath);
            var response = await blobClient.UploadAsync(localFilePath, overwrite: true);

            return response.Value.LastModified > DateTime.UtcNow.AddMinutes(-5);
        }

        public async Task<bool> InsertJob(string regNumber, DateTime date, SaveJobDto saveJobDto)
        {
            string localPath = Path.Combine(JobsDirectory, GetJobFolderName(regNumber, date));

            foreach (var tyreChange in saveJobDto.TyreChanges)
            {
                string changePath = Path.Combine(localPath, tyreChange.TyrePosition);
                Directory.CreateDirectory(changePath);

                var preFileExtension = Path.GetExtension(tyreChange.PreImage.FileName);
                string preImagePath = Path.Combine(changePath, $"preImage{preFileExtension}");
                await HandleImage(preImagePath, tyreChange.PreImage);

                var postFileExtension = Path.GetExtension(tyreChange.PostImage.FileName);
                string postImagePath = Path.Combine(changePath, $"postImage{postFileExtension}");
                await HandleImage(postImagePath, tyreChange.PostImage);
            }

            // Save job descriptions as json file
            await HandleJobDescriptions(saveJobDto, localPath);

            return true;
        }

        private async Task HandleJobDescriptions(SaveJobDto saveJobDto, string localPath)
        {
            string descriptionsPath = Path.Combine(localPath, JobDescriptionsFileName);
            var descriptionsJson = JsonSerializer.Serialize(saveJobDto.JobDescriptions);
            await File.WriteAllTextAsync(descriptionsPath, descriptionsJson);
            var descBlobClient = _blobContainerClient.GetBlobClient(descriptionsPath);
            await descBlobClient.UploadAsync(descriptionsPath, overwrite: true);
        }

        public async Task<Dictionary<string, List<DateTime>>> ListJobs()
        {
            var jobs = new Dictionary<string, List<DateTime>>();
            var resultSegment = _blobContainerClient.GetBlobsByHierarchyAsync(prefix: JobsDirectory, delimiter: "/")
                .AsPages();

            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobHierarchyItem> blobPage in resultSegment)
            {
                // A hierarchical listing may return both virtual directories and blobs.
                foreach (BlobHierarchyItem blobhierarchyItem in blobPage.Values)
                {
                    var blobItemName = blobhierarchyItem.Prefix;
                    var jobData = GetJobData(blobItemName.Replace(JobsDirectory, "").TrimEnd('/'));

                    // Add to jobs dictionary, if key exists, add to values list
                    if (jobs.TryGetValue(jobData.Key, out List<DateTime>? value))
                    {
                        value.Add(jobData.Value);
                    }
                    else
                    {
                        jobs[jobData.Key] = [jobData.Value];
                    }
                }
            }

            return jobs;
        }
        
        public async Task<JobReviewDto> GetJobDetails(string regNumber, DateTime date)
        {
            var jobPrefix = $"{JobsDirectory}{GetJobFolderName(regNumber, date)}/";
            var blobs = _blobContainerClient.GetBlobsAsync(prefix: jobPrefix);
            var jobReview = new JobReviewDto
            {
                JobDescriptions = [],
                TyreChanges = []
            };

            await foreach(var blob in blobs)
            {
                // check if the blob.Name contains tyre position from Constants.TyrePositions
                foreach (var tyrePosition in Constants.TyrePositions)
                {
                    if (blob.Name.Contains($"/{tyrePosition}/"))
                    {
                        var tyreChange = jobReview.TyreChanges.FirstOrDefault(tc => tc.TyrePosition == tyrePosition);

                        if (tyreChange == null)
                        {
                            tyreChange = new TyreChangeViewDto
                            {
                                TyrePosition = tyrePosition
                            };
                            jobReview.TyreChanges.Add(tyreChange);
                        }

                        var blobClient = _blobContainerClient.GetBlobClient(blob.Name);

                        var sasBuilder = new BlobSasBuilder()
                        {
                            BlobContainerName = blobClient.BlobContainerName,
                            BlobName = blobClient.Name,
                            Resource = "b", // 'b' for blob
                            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5),
                            
                        };
                        sasBuilder.SetPermissions(BlobSasPermissions.Read);

                        if (blob.Name.Contains("preImage"))
                        {
                            tyreChange.PreImageUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
                        }
                        else if (blob.Name.Contains("postImage"))
                        {
                            tyreChange.PostImageUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
                        }
                    }
                }

                if(blob.Name.EndsWith(JobDescriptionsFileName))
                {
                    var descriptionsBlobClient = _blobContainerClient.GetBlobClient($"{jobPrefix}{JobDescriptionsFileName}");
                    var descriptionsJson = await descriptionsBlobClient.DownloadContentAsync();
                    var jobDescriptions = JsonSerializer.Deserialize<List<string>>(descriptionsJson.Value.Content.ToString());

                    jobReview.JobDescriptions = jobDescriptions ?? [];
                }

                if(blob.Name.EndsWith(InitFileName))
                {
                    var initBlobClient = _blobContainerClient.GetBlobClient($"{jobPrefix}{InitFileName}");
                    var initJson = await initBlobClient.DownloadContentAsync();
                    var initData = JsonSerializer.Deserialize<MetadataDto>(initJson.Value.Content.ToString());
                    if (initData != null)
                    {
                        jobReview.User = initData.User;
                    }
                }
            }

            return jobReview;
        }

        private async Task HandleImage(string imagePath, IFormFile image)
        {
            using var imageStream = new FileStream(imagePath, FileMode.Create);
            await image.CopyToAsync(imageStream);

            var preBlobClient = _blobContainerClient.GetBlobClient(imagePath);
            var preRes = await preBlobClient.UploadAsync(imagePath);
        }

        private static string GetJobFolderName(string regNumber, DateTime date)
        {
            return $"{regNumber}-{date:yyyyMMdd}";
        }

        private static KeyValuePair<string, DateTime> GetJobData(string jobFolderName)
        {
            var parts = jobFolderName.Split('-');
            var regNumber = parts[0];
            var dateTime = DateTime.ParseExact(parts[1], "yyyyMMdd", CultureInfo.InvariantCulture);
            return new KeyValuePair<string, DateTime>(regNumber, dateTime);
        }
    }
}
