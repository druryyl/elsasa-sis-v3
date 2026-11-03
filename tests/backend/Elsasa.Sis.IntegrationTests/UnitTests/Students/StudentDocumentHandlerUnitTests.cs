namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Data.Common;
using System.IO;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Elsasa.Sis.Students.Features.Records.Documents.DownloadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;
using Microsoft.AspNetCore.Http;

[Trait("Category", "UnitTest")]
public sealed class StudentDocumentHandlerUnitTests
{
    private readonly ISqlConnectionFactory _connectionFactory = new DummySqlConnectionFactory();
    private readonly IFileStorageService _fileStorageService = new DummyFileStorageService();
    private readonly ICurrentUser _currentUser = new DummyCurrentUser();

    [Fact]
    public async Task Upload_Should_Fail_When_StudentIdIsEmpty()
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("transcript.pdf", 1024, "application/pdf");

        var result = await handler.HandleAsync(Guid.Empty, "transcript", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.InvalidStudentId", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Upload_Should_Fail_When_DocumentTypeIsEmpty(string? docType)
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("transcript.pdf", 1024, "application/pdf");

        var result = await handler.HandleAsync(Guid.NewGuid(), docType, file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.DocumentTypeRequired", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_DocumentTypeTooLong()
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("transcript.pdf", 1024, "application/pdf");
        var longType = new string('A', 101);

        var result = await handler.HandleAsync(Guid.NewGuid(), longType, file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.DocumentTypeTooLong", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_FileIsNull()
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);

        var result = await handler.HandleAsync(Guid.NewGuid(), "transcript", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.FileRequired", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_FileIsEmpty()
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("transcript.pdf", 0, "application/pdf");

        var result = await handler.HandleAsync(Guid.NewGuid(), "transcript", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.FileRequired", result.Error.Code);
    }

    [Fact]
    public async Task Upload_Should_Fail_When_FileExceeds5MB()
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile("large.pdf", (5 * 1024 * 1024) + 1, "application/pdf");

        var result = await handler.HandleAsync(Guid.NewGuid(), "transcript", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.FileTooLarge", result.Error.Code);
    }

    [Theory]
    [InlineData("test.exe")]
    [InlineData("test.bat")]
    [InlineData("test.docx")]
    [InlineData("test.txt")]
    [InlineData("test")]
    public async Task Upload_Should_Fail_When_FileExtensionNotWhitelisted(string fileName)
    {
        var handler = new UploadStudentDocumentHandler(_connectionFactory, _fileStorageService, _currentUser);
        var file = CreateTestFormFile(fileName, 1024, "application/octet-stream");

        var result = await handler.HandleAsync(Guid.NewGuid(), "transcript", file, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.InvalidFileType", result.Error.Code);
    }

    [Fact]
    public async Task Download_Should_Fail_When_StudentIdOrDocIdEmpty()
    {
        var handler = new DownloadStudentDocumentHandler(_connectionFactory, _fileStorageService);

        var result1 = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None);
        var result2 = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty, CancellationToken.None);

        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.Equal("Students.DocumentNotFound", result1.Error.Code);
        Assert.Equal("Students.DocumentNotFound", result2.Error.Code);
    }

    [Fact]
    public async Task Verify_Should_Fail_When_IdentifiersEmpty()
    {
        var handler = new VerifyStudentDocumentHandler(_connectionFactory, _currentUser);
        var request = new VerifyStudentDocumentRequest("verified", "Looks verified");

        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.DocumentNotFound", result.Error.Code);
    }

    [Theory]
    [InlineData("invalid_status")]
    [InlineData("submitted")]
    [InlineData("pending")]
    public async Task Verify_Should_Fail_When_SubmissionStatusInvalid(string invalidStatus)
    {
        var handler = new VerifyStudentDocumentHandler(_connectionFactory, _currentUser);
        var request = new VerifyStudentDocumentRequest(invalidStatus, null);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Students.InvalidSubmissionStatus", result.Error.Code);
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
