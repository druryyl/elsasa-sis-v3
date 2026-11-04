namespace Elsasa.Sis.Finance.Features.Billing.GenerateStatement;

using Dapper;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Finance.Features.Billing.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler executing periodic financial statement compilation with frozen line items (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class GenerateStatementHandler(
    ISqlConnectionFactory connectionFactory,
    IStudentAccountService studentAccountService,
    ICurrentUser currentUser,
    ILogger<GenerateStatementHandler> logger)
{
    static GenerateStatementHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkStudentExistsSql = """
        SELECT 1
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectPeriodObligationsSql = """
        SELECT 
            id AS Id,
            obligation_number AS ObligationNumber,
            description AS Description,
            original_amount AS OriginalAmount,
            due_date AS DueDate,
            status AS Status
        FROM finance.financial_obligations
        WHERE student_id = @StudentId
          AND issue_date >= @BillingPeriodStart
          AND issue_date <= @BillingPeriodEnd
          AND status != 'cancelled'
        ORDER BY issue_date ASC, created_at ASC;
        """;

    private const string _insertBillingDocumentSql = """
        INSERT INTO finance.billing_documents (
            id,
            document_number,
            student_account_id,
            student_id,
            document_type,
            recipient_name,
            recipient_relationship,
            recipient_contact,
            billing_period_start,
            billing_period_end,
            issue_date,
            payment_due_date,
            total_billed_amount,
            currency_code,
            delivery_channel,
            delivery_status,
            sent_at,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @DocumentNumber,
            @StudentAccountId,
            @StudentId,
            'statement',
            @RecipientName,
            @RecipientRelationship,
            @RecipientContact,
            @BillingPeriodStart,
            @BillingPeriodEnd,
            @IssueDate,
            NULL,
            @TotalBilledAmount,
            'IDR',
            'portal',
            'issued',
            NULL,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _insertBillingItemSql = """
        INSERT INTO finance.billing_document_items (
            id,
            billing_document_id,
            line_sequence,
            item_type,
            reference_entity_type,
            reference_entity_id,
            description,
            amount,
            currency_code,
            created_at
        ) VALUES (
            @Id,
            @BillingDocumentId,
            @LineSequence,
            'obligation',
            'financial_obligations',
            @ReferenceEntityId,
            @Description,
            @Amount,
            'IDR',
            @CreatedAt
        );
        """;

    public async Task<Result<GenerateStatementResponse>> HandleAsync(
        GenerateStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        // 1. Verify student exists
        var studentExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkStudentExistsSql,
            new { request.StudentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (!studentExists)
        {
            return Result.Failure<GenerateStatementResponse>(
                DomainError.NotFound(
                    "Finance.Billing.StudentNotFound",
                    $"Student with ID '{request.StudentId}' was not found."));
        }

        var auditUser = currentUser.Username ?? "SYSTEM_OPERATIONAL";

        // 2. Resolve or provision student financial account
        var account = await studentAccountService.GetOrCreateAccountAsync(
            request.StudentId,
            connection,
            transaction,
            createdBy: auditUser,
            cancellationToken: cancellationToken);

        // 3. Resolve primary recipient per guarantor hierarchy
        var recipient = await BillingRecipientResolver.ResolveAsync(
            request.StudentId,
            connection,
            transaction,
            cancellationToken);

        var recipientName = recipient?.Name ?? "Student";
        var recipientRelationship = recipient?.Relationship ?? "Self";
        var recipientContact = string.IsNullOrWhiteSpace(recipient?.Contact) ? "-" : recipient.Contact;

        // 4. Query obligations within the specified billing period
        var obligations = (await connection.QueryAsync<StatementObligationRow>(new CommandDefinition(
            _selectPeriodObligationsSql,
            new
            {
                request.StudentId,
                request.BillingPeriodStart,
                request.BillingPeriodEnd
            },
            transaction: transaction,
            cancellationToken: cancellationToken))).ToList();

        if (obligations.Count == 0)
        {
            return Result.Failure<GenerateStatementResponse>(
                DomainError.Conflict(
                    "Finance.Billing.NoStatementActivity",
                    $"No active financial obligations found for student with ID '{request.StudentId}' in the period from {request.BillingPeriodStart:yyyy-MM-dd} to {request.BillingPeriodEnd:yyyy-MM-dd}."));
        }

        // 5. Compute summary metrics and document number
        var totalBilledAmount = obligations.Sum(o => o.OriginalAmount);
        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var cleanNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var now = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();

        var documentNumber = await BillingDocumentNumberGenerator.GenerateAsync(
            "statement",
            connection,
            transaction,
            cancellationToken);

        // 6. Insert statement header into finance.billing_documents
        await connection.ExecuteAsync(new CommandDefinition(
            _insertBillingDocumentSql,
            new
            {
                Id = documentId,
                DocumentNumber = documentNumber,
                StudentAccountId = account.Id,
                StudentId = request.StudentId,
                RecipientName = recipientName,
                RecipientRelationship = recipientRelationship,
                RecipientContact = recipientContact,
                request.BillingPeriodStart,
                request.BillingPeriodEnd,
                IssueDate = issueDate,
                TotalBilledAmount = totalBilledAmount,
                Notes = cleanNotes,
                CreatedAt = now,
                CreatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 7. Freeze line items into finance.billing_document_items
        var lineItems = new List<GenerateStatementItemResponse>(obligations.Count);
        for (var i = 0; i < obligations.Count; i++)
        {
            var obligation = obligations[i];
            var itemId = Guid.NewGuid();
            var sequence = i + 1;

            await connection.ExecuteAsync(new CommandDefinition(
                _insertBillingItemSql,
                new
                {
                    Id = itemId,
                    BillingDocumentId = documentId,
                    LineSequence = sequence,
                    ReferenceEntityId = obligation.Id,
                    Description = obligation.Description,
                    Amount = obligation.OriginalAmount,
                    CreatedAt = now
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            lineItems.Add(new GenerateStatementItemResponse(
                itemId,
                sequence,
                "obligation",
                "financial_obligations",
                obligation.Id,
                obligation.Description,
                obligation.OriginalAmount,
                "IDR"));
        }

        // 8. Commit transaction atomically
        transaction.Commit();

        logger.LogInformation(
            "Compiled financial statement {DocumentNumber} ({DocumentId}) for period {Start} to {End} with {LineCount} items totaling {TotalAmount} IDR for student {StudentId}.",
            documentNumber,
            documentId,
            request.BillingPeriodStart,
            request.BillingPeriodEnd,
            lineItems.Count,
            totalBilledAmount,
            request.StudentId);

        var response = new GenerateStatementResponse(
            documentId,
            documentNumber,
            account.Id,
            request.StudentId,
            "statement",
            recipientName,
            recipientRelationship,
            recipientContact,
            request.BillingPeriodStart,
            request.BillingPeriodEnd,
            issueDate,
            null,
            totalBilledAmount,
            "IDR",
            "portal",
            "issued",
            null,
            cleanNotes,
            lineItems,
            now,
            auditUser);

        return Result.Success(response);
    }

    private sealed record StatementObligationRow(
        Guid Id,
        string ObligationNumber,
        string Description,
        long OriginalAmount,
        DateOnly DueDate,
        string Status);
}
