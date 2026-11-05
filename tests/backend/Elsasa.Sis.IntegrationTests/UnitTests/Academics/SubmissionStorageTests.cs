namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.IO;
using System.Text;
using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Features.Assignments.UploadSubmissionFile;
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Shared.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Unit tests for SubmissionStorageOptions, SubmissionStorageService, and UploadSubmissionFileHandler (P5-S13, TD-07).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class SubmissionStorageTests
{
    private sealed class MockCurrentUser(
        Guid? id,
        string username = "testuser",
        string email = "test@example.com",
        Guid? studentId = null,
        IReadOnlyList<string>? roles = null) : ICurrentUser
    {
        public Guid? Id => id;
        public string? Username => username;
        public string? Email => email;
        public Guid? StudentId => studentId;
        public IReadOnlyList<string> Roles => roles ?? ["Student"];
        public bool IsAuthenticated => true;
    }

    [Fact]
    public void SubmissionStorageOptions_Should_HaveDefaultValues()
    {
        // Arrange & Act
        var options = new SubmissionStorageOptions();

        // Assert
        options.StorageRootPath.Should().Be("App_Data/Submissions");
        options.MaxFileSizeBytes.Should().Be(25 * 1024 * 1024);
        SubmissionStorageOptions.SectionName.Should().Be("Academics:Submissions");
    }

    [Fact]
    public async Task SubmissionStorageService_Should_SaveFile_And_ReturnRelativePath()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "ElsasaTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = Options.Create(new SubmissionStorageOptions { StorageRootPath = tempFolder });
            var service = new SubmissionStorageService(options);

            var studentId = Guid.NewGuid();
            var contentBytes = Encoding.UTF8.GetBytes("Test PDF coursework content");
            using var stream = new MemoryStream(contentBytes);
            var formFile = new FormFile(stream, 0, contentBytes.Length, "file", "assignment1.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            // Act
            var relativePath = await service.SaveSubmissionFileAsync(formFile, studentId, "assignment1_test.pdf");

            // Assert
            relativePath.Should().NotBeNullOrWhiteSpace();
            relativePath.Should().Contain(studentId.ToString());
            relativePath.Should().EndWith("assignment1_test.pdf");

            var resolvedPath = service.ResolvePhysicalPath(relativePath);
            resolvedPath.Should().NotBeNull();
            File.Exists(resolvedPath!).Should().BeTrue();
            (await File.ReadAllTextAsync(resolvedPath!)).Should().Be("Test PDF coursework content");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }

    [Fact]
    public void SubmissionStorageService_ResolvePhysicalPath_Should_ReturnNull_When_PathContainsTraversal()
    {
        // Arrange
        var service = new SubmissionStorageService();

        // Act
        var resolved = service.ResolvePhysicalPath("../../etc/passwd");

        // Assert
        resolved.Should().BeNull();
    }

    [Fact]
    public void SubmissionStorageService_ResolvePhysicalPath_Should_ReturnNull_When_ReferenceIsEmpty()
    {
        // Arrange
        var service = new SubmissionStorageService();

        // Act
        var resolved = service.ResolvePhysicalPath("   ");

        // Assert
        resolved.Should().BeNull();
    }

    [Fact]
    public async Task UploadSubmissionFileHandler_Should_Fail_When_FileIsNull()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var currentUser = new MockCurrentUser(studentId, studentId: studentId);
        var handler = new UploadSubmissionFileHandler(currentUser);

        // Act
        var result = await handler.HandleAsync(null, studentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.SubmissionFileRequired");
    }

    [Fact]
    public async Task UploadSubmissionFileHandler_Should_Fail_When_FileExceedsLimit()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var currentUser = new MockCurrentUser(studentId, studentId: studentId);
        var options = Options.Create(new SubmissionStorageOptions { MaxFileSizeBytes = 10 });
        var handler = new UploadSubmissionFileHandler(currentUser, options: options);

        var contentBytes = Encoding.UTF8.GetBytes("More than ten bytes content");
        using var stream = new MemoryStream(contentBytes);
        var formFile = new FormFile(stream, 0, contentBytes.Length, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        // Act
        var result = await handler.HandleAsync(formFile, studentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.SubmissionFileTooLarge");
    }

    [Fact]
    public async Task UploadSubmissionFileHandler_Should_Fail_When_FileTypeNotAllowed()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var currentUser = new MockCurrentUser(studentId, studentId: studentId);
        var handler = new UploadSubmissionFileHandler(currentUser);

        var contentBytes = Encoding.UTF8.GetBytes("Executable content");
        using var stream = new MemoryStream(contentBytes);
        var formFile = new FormFile(stream, 0, contentBytes.Length, "file", "malicious.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/x-msdownload"
        };

        // Act
        var result = await handler.HandleAsync(formFile, studentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.InvalidSubmissionFileType");
    }

    [Fact]
    public async Task UploadSubmissionFileHandler_Should_Fail_When_StudentAttemptsToUploadForAnotherStudent()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var anotherStudentId = Guid.NewGuid();
        var currentUser = new MockCurrentUser(studentId, studentId: studentId, roles: ["Student"]);
        var handler = new UploadSubmissionFileHandler(currentUser);

        var contentBytes = Encoding.UTF8.GetBytes("PDF content");
        using var stream = new MemoryStream(contentBytes);
        var formFile = new FormFile(stream, 0, contentBytes.Length, "file", "homework.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        // Act
        var result = await handler.HandleAsync(formFile, anotherStudentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.UnauthorizedStudentUpload");
    }

    [Fact]
    public async Task UploadSubmissionFileHandler_Should_Succeed_When_FileIsValid()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "ElsasaTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = Options.Create(new SubmissionStorageOptions { StorageRootPath = tempFolder });
            var storageService = new SubmissionStorageService(options);

            var studentId = Guid.NewGuid();
            var currentUser = new MockCurrentUser(studentId, studentId: studentId);
            var handler = new UploadSubmissionFileHandler(currentUser, storageService, options);

            var contentBytes = Encoding.UTF8.GetBytes("Valid PDF content");
            using var stream = new MemoryStream(contentBytes);
            var formFile = new FormFile(stream, 0, contentBytes.Length, "file", "essay.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            // Act
            var result = await handler.HandleAsync(formFile, studentId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.StoragePath.Should().Contain(studentId.ToString());
            result.Value.StoragePath.Should().EndWith(".pdf");
            result.Value.ContentType.Should().Be("application/pdf");
            result.Value.FileSizeBytes.Should().Be(contentBytes.Length);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }

    [Fact]
    public void DependencyInjection_AddAcademicsDomain_Should_RegisterStorageServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Academics:Submissions:StorageRootPath"] = "Custom/Submissions/Path",
            ["Academics:Submissions:MaxFileSizeBytes"] = "10485760"
        });
        var configuration = configBuilder.Build();

        // Act
        services.AddAcademicsDomain(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var storageService = provider.GetService<ISubmissionStorageService>();
        storageService.Should().NotBeNull();
        storageService.Should().BeOfType<SubmissionStorageService>();

        var options = provider.GetService<IOptions<SubmissionStorageOptions>>();
        options.Should().NotBeNull();
        options!.Value.StorageRootPath.Should().Be("Custom/Submissions/Path");
        options.Value.MaxFileSizeBytes.Should().Be(10485760);
    }
}
