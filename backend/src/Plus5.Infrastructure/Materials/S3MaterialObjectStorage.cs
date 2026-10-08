using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Plus5.Application.Materials;

namespace Plus5.Infrastructure.Materials;

public sealed class S3MaterialObjectStorage : IMaterialObjectStorage, IDisposable
{
    private readonly MaterialStorageOptions options;
    private readonly AmazonS3Client client;

    public S3MaterialObjectStorage(IOptions<MaterialStorageOptions> configuredOptions)
    {
        options = configuredOptions.Value;
        client = new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                AuthenticationRegion = options.Region,
                ForcePathStyle = options.ForcePathStyle,
            });
    }

    public async Task PutQuarantineAsync(
        MaterialObjectWrite request,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        EnsureReference(request.Reference, options.CleanBucket);

        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.QuarantineBucket,
            Key = request.Reference.ObjectKey,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = request.MediaType,
            Metadata =
            {
                ["sha256"] = request.Sha256Checksum,
            },
        }, cancellationToken);
    }

    public async Task<Stream> OpenQuarantineReadAsync(
        MaterialObjectReference reference,
        CancellationToken cancellationToken)
    {
        EnsureReference(reference, options.CleanBucket);
        var response = await client.GetObjectAsync(
            options.QuarantineBucket,
            reference.ObjectKey,
            cancellationToken);
        return new ResponseOwnedStream(response);
    }

    public async Task PromoteCleanAsync(
        MaterialObjectReference reference,
        CancellationToken cancellationToken)
    {
        EnsureReference(reference, options.CleanBucket);
        await client.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = options.QuarantineBucket,
            SourceKey = reference.ObjectKey,
            DestinationBucket = options.CleanBucket,
            DestinationKey = reference.ObjectKey,
        }, cancellationToken);
        await client.DeleteObjectAsync(
            options.QuarantineBucket,
            reference.ObjectKey,
            cancellationToken);
    }

    public async Task CopyCleanAsync(
        MaterialObjectReference source,
        MaterialObjectReference destination,
        CancellationToken cancellationToken)
    {
        EnsureReference(source, options.CleanBucket);
        EnsureReference(destination, options.CleanBucket);
        await client.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = options.CleanBucket,
            SourceKey = source.ObjectKey,
            DestinationBucket = options.CleanBucket,
            DestinationKey = destination.ObjectKey,
        }, cancellationToken);
    }

    public async Task DeleteAsync(
        MaterialObjectReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        EnsureReference(reference, options.CleanBucket);
        await client.DeleteObjectAsync(options.QuarantineBucket, reference.ObjectKey, cancellationToken);
        await client.DeleteObjectAsync(options.CleanBucket, reference.ObjectKey, cancellationToken);
    }

    public Task<Uri> CreateReadAccessAsync(
        MaterialObjectReference reference,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        Plus5.Domain.Materials.MaterialFilePolicy.EnsureReadAccessLifetime(lifetime);
        EnsureReference(reference, options.CleanBucket);
        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.CleanBucket,
            Key = reference.ObjectKey,
            Expires = DateTime.UtcNow.Add(lifetime),
            Verb = HttpVerb.GET,
        });
        return Task.FromResult(new Uri(url, UriKind.Absolute));
    }

    public void Dispose() => client.Dispose();

    private void EnsureReference(MaterialObjectReference reference, string expectedBucket)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (!string.Equals(reference.StorageProvider, options.ProviderCode, StringComparison.Ordinal)
            || !string.Equals(reference.StorageContainer, expectedBucket, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Material object reference does not match the configured private storage boundary.");
        }
    }

    private sealed class ResponseOwnedStream(GetObjectResponse response) : Stream
    {
        private readonly Stream inner = response.ResponseStream;
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        protected override void Dispose(bool disposing)
        {
            if (disposing) response.Dispose();
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            response.Dispose();
            await base.DisposeAsync();
        }
    }
}
