using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.ComponentModel;
using System.Globalization;
using tyre_reporting_app_api.Interfaces;
using tyre_reporting_app_api.Models;

namespace tyre_reporting_app_api.Services
{
    public class StorageInteractor : IStorageInteractor
    {
        private readonly string _connectionString;
        private readonly BlobContainerClient _blobContainerClient;
        private const string ContainerName = "t-r-app";
        private const string JobsDirectory = "jobs/";

        public StorageInteractor(IConfiguration configuration)
        {
            _connectionString = configuration?.GetConnectionString("AzureBlobStorage") ?? throw new NullReferenceException();
            var blobServiceClient = new BlobServiceClient(_connectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        }

        public async Task<bool> CreateContainer(string regNumber, DateTime date)
        {
            string localPath = Path.Combine(JobsDirectory, GetJobFolderName(regNumber, date));
            Directory.CreateDirectory(localPath);

            string fileName = "init.txt";
            string localFilePath = Path.Combine(localPath, fileName);
            await File.WriteAllTextAsync(localFilePath, "Init job info.");


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

            return true;
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
