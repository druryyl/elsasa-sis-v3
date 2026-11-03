namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Data.Common;
using System.IO;
using System.Text;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Elsasa.Sis.Students.Features.Admission.Documents.DownloadApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.Documents.UploadApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;
using Microsoft.AspNetCore.Http;

[Trait("Category", "UnitTest")]
public sealed class ApplicantDocumentHandlerUnitTests
{
    private readonly ISqlConnectionFactory _connectionFactory = new DummySqlConnectionFactory();
    private readonly IFileStorageService _fileStorageService = new DummyFileStorageService();
    private readonly ICurrentUser _currentUser = new DummyCurrentUser();

    [Fact]
    public async Task Upload_Should_Fail_When_ApplicationIdIsEmpty()
    {
        var handler = new UploadApplicantDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("test.pdf", 1024, "application/pdf");

        var result = await handler.HandleAsync(Guid.Empty, "birth_certificate", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.InvalidApplicationId", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Upload_Should_Fail_When_DocumentTypeIsEmpty(string? docType)
    {
        var handler = new UploadApplicantDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("test.pdf", 1024, "application/pdf");

        var result = await handler.HandleAsync(Guid.NewGuid(), docType, file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.DocumentTypeRequired", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_FileIsNull()
    {
        var handler = new UploadApplicantDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);

        var result = await handler.HandleAsync(Guid.NewGuid(), "birth_certificate", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.FileRequired", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_FileIsEmpty()
    {
        var handler = new UploadApplicantDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("test.pdf", 0, "application/pdf");

        var result = await handler.HandleAsync(Guid.NewGuid(), "birth_certificate", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.FileRequired", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_FileExceeds5MB()
    {
        var handler = new UploadApplicantDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("large.pdf", (5 * 1024 * 1024) + 1, "application/pdf");

        var result = await handler.HandleAsync(Guid.NewGuid(), "birth_certificate", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.FileTooLarge", result.Error.Code);
    }

    [Theory]
    [InlineData("test.exe")]
    [InlineData("test.bat")]
    [InlineData("test.docx")]
    [InlineData("test.txt")]
    [InlineData("test")]
    public async Task Upload_Should_Fail_When_FileExtensionNotWhitelisted(string fileName)
    {
        var handler = new UploadApplicantDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile(fileName, 1024, "application/octet-stream");

        var result = await handler.HandleAsync(Guid.NewGuid(), "birth_certificate", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.InvalidFileType", result.Error.Code);
    }

    [Fact]
    public async Task Download_Should_Fail_When_ApplicationIdOrDocIdEmpty()
    {
        var handler = new DownloadApplicantDocumentHandler(_connectionFactory, _fileStorageService);

        var result1 = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None);
        var result2 = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty, CancellationToken.None);

        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.Equal("Admission.DocumentNotFound", result1.Error.Code);
        Assert.Equal("Admission.DocumentNotFound", result2.Error.Code);
    }

    [Fact]
    public async Task Verify_Should_Fail_When_IdentifiersEmpty()
    {
        var handler = new VerifyApplicantDocumentHandler(_connectionFactory, _currentUser);
        var request = new VerifyApplicantDocumentRequest("verified", "Looks good");

        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.DocumentNotFound", result.Error.Code);
    }

    [Theory]
    [InlineData("invalid_status")]
    [InlineData("submitted")]
    [InlineData("pending")]
    public async Task Verify_Should_Fail_When_SubmissionStatusInvalid(string invalidStatus)
    {
        var handler = new VerifyApplicantDocumentHandler(_connectionFactory, _currentUser);
        var request = new VerifyApplicantDocumentRequest(invalidStatus, null);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Admission.InvalidSubmissionStatus", result.Error.Code);
    }

    private static FormFile CreateTestFormFile(string fileName, long length, string contentType)
    {
        var content = length > 0 ? new byte[length] : [];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class DummyFileStorageService : IFileStorageService
    {
        public Task<Result<StoredFileResult>> SaveFileAsync(Stream stream, string fileName, string contentType, string category, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<FileStreamResult>> OpenReadStreamAsync(string fileReference, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteFileAsync(string fileReference, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class DummyCurrentUser : ICurrentUser
    {
        public Guid? Id => Guid.NewGuid();
        public string? Username => "test-admin";
        public string? Email => "test@elsasa.test";
        public IReadOnlyList<string> Roles => ["Admin"];
        public bool IsAuthenticated => true;
    }
}
