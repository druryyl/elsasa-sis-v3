namespace Elsasa.Sis.IntegrationTests.UnitTests.Storage;

using System.Text;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[Trait("Category", "UnitTest")]
public sealed class PhysicalFileStorageServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly PhysicalFileStorageService _service;

    public PhysicalFileStorageServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ElsasaStorageTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _service = new PhysicalFileStorageService(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore temporary cleanup errors on test tear-down
        }
    }

    [Fact]
    public async Task SaveFileAsync_Should_PersistFile_When_ValidPdfProvided()
    {
        // Arrange
        var content = "%PDF-1.4 sample content"u8.ToArray();
        using var stream = new MemoryStream(content);
        const string fileName = "sample_transcript.pdf";
        const string contentType = "application/pdf";
        const string category = "admission/test-applicant";

        // Act
        var result = await _service.SaveFileAsync(stream, fileName, contentType, category);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(fileName, result.Value.FileName);
        Assert.Equal(contentType, result.Value.ContentType);
        Assert.Equal(content.Length, result.Value.SizeBytes);
        Assert.StartsWith("storage/admission/test-applicant/", result.Value.FileReference, StringComparison.Ordinal);

        // Verify physical file was written and can be read back
        var openResult = await _service.OpenReadStreamAsync(result.Value.FileReference);
        Assert.True(openResult.IsSuccess);
        using var readStream = openResult.Value.Stream;
        using var memoryStream = new MemoryStream();
        await readStream.CopyToAsync(memoryStream);
        Assert.Equal(content, memoryStream.ToArray());
        Assert.Equal("application/pdf", openResult.Value.ContentType);
    }

    [Theory]
    [InlineData("avatar.jpg", "image/jpeg", ".jpg")]
    [InlineData("avatar.jpeg", "image/jpeg", ".jpeg")]
    [InlineData("badge.png", "image/png", ".png")]
    public async Task SaveFileAsync_Should_PersistFile_When_ValidImageProvided(
        string fileName,
        string contentType,
        string expectedExtension)
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("fake-image-binary-data");
        using var stream = new MemoryStream(content);

        // Act
        var result = await _service.SaveFileAsync(stream, fileName, contentType, "documents/images");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.EndsWith(expectedExtension, result.Value.FileReference, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(contentType, result.Value.ContentType);

        var readResult = await _service.OpenReadStreamAsync(result.Value.FileReference);
        Assert.True(readResult.IsSuccess);
        readResult.Value.Stream.Dispose();
    }

    [Theory]
    [InlineData("malicious.exe", "application/x-msdownload")]
    [InlineData("script.bat", "application/x-bat")]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("noextension", "application/pdf")]
    public async Task SaveFileAsync_Should_ReturnInvalidContentType_When_ExtensionIsNotWhitelisted(
        string fileName,
        string contentType)
    {
        // Arrange
        using var stream = new MemoryStream("malicious script"u8.ToArray());

        // Act
        var result = await _service.SaveFileAsync(stream, fileName, contentType, "admission");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.InvalidContentType", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task SaveFileAsync_Should_ReturnInvalidContentType_When_MimeTypeDoesNotMatchExtension()
    {
        // Arrange: valid extension .pdf, but mismatched MIME type image/png
        using var stream = new MemoryStream("pdf fake data"u8.ToArray());

        // Act
        var result = await _service.SaveFileAsync(stream, "document.pdf", "image/png", "admission");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.InvalidContentType", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task SaveFileAsync_Should_ReturnFileTooLarge_When_SeekableStreamExceeds5Mb()
    {
        // Arrange: 5 MB + 1 byte
        var largeContent = new byte[PhysicalFileStorageService.MaxFileSizeBytes + 1];
        using var stream = new MemoryStream(largeContent);

        // Act
        var result = await _service.SaveFileAsync(stream, "large_dossier.pdf", "application/pdf", "admission");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.FileTooLarge", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task SaveFileAsync_Should_ReturnFileTooLarge_When_NonSeekableStreamExceeds5Mb()
    {
        // Arrange: Custom stream that reports CanSeek = false
        var largeContent = new byte[PhysicalFileStorageService.MaxFileSizeBytes + 1024];
        using var underlying = new MemoryStream(largeContent);
        using var nonSeekable = new NonSeekableStreamWrapper(underlying);

        // Act
        var result = await _service.SaveFileAsync(nonSeekable, "large_stream.pdf", "application/pdf", "admission");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.FileTooLarge", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task SaveFileAsync_Should_PreventDirectoryTraversal_When_CategoryOrFileNameContainsTraversal()
    {
        // Arrange
        using var stream = new MemoryStream("data"u8.ToArray());

        // Act 1: Traversal in category
        var resultCategoryTraversal = await _service.SaveFileAsync(
            stream,
            "doc.pdf",
            "application/pdf",
            "admission/../../etc");

        // Assert 1
        Assert.False(resultCategoryTraversal.IsSuccess);
        Assert.Equal("Storage.InvalidCategory", resultCategoryTraversal.Error!.Code);

        // Act 2: Traversal in file name
        stream.Position = 0;
        var resultNameTraversal = await _service.SaveFileAsync(
            stream,
            "../../escape.pdf",
            "application/pdf",
            "admission");

        // Path.GetFileName strips separators, but if clean file name still contained traversal it would be caught.
        // If sanitized to "escape.pdf", it must store safely within the root path.
        Assert.True(resultNameTraversal.IsSuccess);
        Assert.False(resultNameTraversal.Value.FileReference.Contains("..", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OpenReadStreamAsync_Should_ReturnNotFound_When_FileDoesNotExist()
    {
        // Act
        var result = await _service.OpenReadStreamAsync("storage/documents/non_existent.pdf");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.FileNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Theory]
    [InlineData("../../windows/system32/cmd.exe")]
    [InlineData("storage/../../evil.pdf")]
    [InlineData("..\\..\\secret.pdf")]
    public async Task OpenReadStreamAsync_Should_PreventDirectoryTraversal_When_FileReferenceContainsTraversal(
        string maliciousReference)
    {
        // Act
        var result = await _service.OpenReadStreamAsync(maliciousReference);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.InvalidFileReference", result.Error.Code);
    }

    [Fact]
    public async Task DeleteFileAsync_Should_RemoveFile_When_FileExists()
    {
        // Arrange
        using var stream = new MemoryStream("temporary content"u8.ToArray());
        var saveResult = await _service.SaveFileAsync(stream, "temp.pdf", "application/pdf", "admission");
        Assert.True(saveResult.IsSuccess);

        // Act
        var deleteResult = await _service.DeleteFileAsync(saveResult.Value.FileReference);

        // Assert
        Assert.True(deleteResult.IsSuccess);

        // Subsequent open should return NotFound
        var openResult = await _service.OpenReadStreamAsync(saveResult.Value.FileReference);
        Assert.False(openResult.IsSuccess);
        Assert.Equal("Storage.FileNotFound", openResult.Error!.Code);
    }

    [Fact]
    public async Task DeleteFileAsync_Should_ReturnNotFound_When_FileDoesNotExist()
    {
        // Act
        var result = await _service.DeleteFileAsync("storage/admission/missing_file.pdf");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Storage.FileNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AddSharedStorage_Should_RegisterService_When_Configured()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:LocalRootPath"] = _tempDirectory
            })
            .Build();

        // Act
        services.AddSharedStorage(configuration);
        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IFileStorageService>();

        // Assert
        Assert.NotNull(storage);
        Assert.IsType<PhysicalFileStorageService>(storage);

        // Verify storage works via DI
        using var stream = new MemoryStream("di test data"u8.ToArray());
        var saveResult = await storage.SaveFileAsync(stream, "di_doc.pdf", "application/pdf", "di");
        Assert.True(saveResult.IsSuccess);
    }

    private sealed class NonSeekableStreamWrapper(Stream innerStream) : Stream
    {
        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            innerStream.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            innerStream.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
