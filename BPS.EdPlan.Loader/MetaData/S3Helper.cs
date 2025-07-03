using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;
using System.Threading.Tasks;

public class S3Helper
{
    private readonly IAmazonS3 _s3Client;

    public S3Helper(string accessKey, string secretKey, string region)
    {
        _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
    }

    public async Task ListAndDownloadFilesAsync(string bucketName, string prefix, string downloadDirectory)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = prefix
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

        foreach (var s3Object in listResponse.S3Objects)
        {
            string destPath = Path.Combine(downloadDirectory, Path.GetFileName(s3Object.Key));
            var getRequest = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = s3Object.Key
            };

            using (var getResponse = await _s3Client.GetObjectAsync(getRequest))
            using (var responseStream = getResponse.ResponseStream)
            using (var fileStream = File.Create(destPath))
            {
                await responseStream.CopyToAsync(fileStream);
            }
        }
    }
}
