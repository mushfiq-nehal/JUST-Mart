using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(IFormFile file, string containerName = "product-images");
        Task DeleteImageAsync(string imageUrl, string containerName = "product-images");
        Task<List<string>> UploadMultipleImagesAsync(IEnumerable<IFormFile> files, string containerName = "product-images");
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string containerName = "product-images")
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                // Get or create container
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";

                // Get blob client
                var blobClient = containerClient.GetBlobClient(fileName);

                // Upload file
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    });
                }

                // Return the URL
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload image: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> UploadMultipleImagesAsync(IEnumerable<IFormFile> files, string containerName = "product-images")
        {
            var imageUrls = new List<string>();

            if (files == null || !files.Any())
                return imageUrls;

            foreach (var file in files)
            {
                var url = await UploadImageAsync(file, containerName);
                if (!string.IsNullOrEmpty(url))
                {
                    imageUrls.Add(url);
                }
            }

            return imageUrls;
        }

        public async Task DeleteImageAsync(string imageUrl, string containerName = "product-images")
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            try
            {
                // Extract blob name from URL
                var uri = new Uri(imageUrl);
                var blobName = Path.GetFileName(uri.LocalPath);

                // Get container and blob client
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Delete blob
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - deletion failure shouldn't break the flow
                Console.WriteLine($"Failed to delete image: {ex.Message}");
            }
        }
    }
}
