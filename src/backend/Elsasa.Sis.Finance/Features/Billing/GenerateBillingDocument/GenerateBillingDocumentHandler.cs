namespace Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;

using Dapper;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Finance.Features.Billing.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler executing point-in-time billing document (bill) compilation and frozen itemization (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class GenerateBillingDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    IStudentAccountService studentAccountService,
    ICurrentUser currentUser,
    ILogger<GenerateBillingDocumentHandler> logger)
{
    static GenerateBillingDocumentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkStudentExistsSql = """
        SELECT 1
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectSpecificObligationsSql = """
        SELECT 
            id AS Id,
            obligation_number AS ObligationNumber,
            description AS Description,
            original_amount AS OriginalAmount,
            due_date AS DueDate,
            status AS Status
        FROM finance.financial_obligations
        WHERE student_id = @StudentId
          AND id IN @ObligationIds;
        """;

    private const string _selectAllOpenObligationsSql = """
        SELECT 
            id AS Id,
            obligation_number AS ObligationNumber,
            description AS Description,
            original_amount AS OriginalAmount,
            due_date AS DueDate,
            status AS Status
        FROM finance.financial_obligations
        WHERE student_id = @StudentId
          AND status = 'assessed'
        ORDER BY due_date ASC, created_at ASC;
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
            'bill',
            @RecipientName,
            @RecipientRelationship,
            @RecipientContact,
            NULL,
            NULL,
            @IssueDate,
            @PaymentDueDate,
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

    private const string _updateObligationsStatusSql = """
        UPDATE finance.financial_obligations
        SET status = 'issued',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id IN @ObligationIds
          AND status = 'assessed';
        """;

    public async Task<Result<GenerateBillingDocumentResponse>> HandleAsync(
        GenerateBillingDocumentRequest request,
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
            return Result.Failure<GenerateBillingDocumentResponse>(
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

        // 3. Resolve primary recipient contact per guarantor hierarchy (GAP-005, TD-05)
        var recipient = await BillingRecipientResolver.ResolveAsync(
            request.StudentId,
            connection,
            transaction,
            cancellationToken);

        var recipientName = recipient?.Name ?? "Student";
        var recipientRelationship = recipient?.Relationship ?? "Self";
        var recipientContact = string.IsNullOrWhiteSpace(recipient?.Contact) ? "-" : recipient.Contact;

        // 4. Query and validate obligations to include
        List<ObligationCandidateRow> obligations;

        if (request.ObligationIds is { Count: > 0 })
        {
            var distinctIds = request.ObligationIds.Distinct().ToList();
            var queried = (await connection.QueryAsync<ObligationCandidateRow>(new CommandDefinition(
                _selectSpecificObligationsSql,
                new { request.StudentId, ObligationIds = distinctIds },
                transaction: transaction,
                cancellationToken: cancellationToken))).ToList();

            if (queried.Count != distinctIds.Count)
            {
                return Result.Failure<GenerateBillingDocumentResponse>(
                    DomainError.NotFound(
                        "Finance.Billing.ObligationsNotFound",
                        "One or more specified obligations were not found for this student."));
            }

            var invalidStatus = queried.FirstOrDefault(o => o.Status != "assessed");
            if (invalidStatus is not null)
            {
                return Result.Failure<GenerateBillingDocumentResponse>(
                    DomainError.Conflict(
                        "Finance.Billing.InvalidObligationStatus",
                        $"Obligation '{invalidStatus.ObligationNumber}' has status '{invalidStatus.Status}'. Only open obligations in 'assessed' status can be compiled into a bill."));
            }

            obligations = queried;
        }
        else
        {
            obligations = (await connection.QueryAsync<ObligationCandidateRow>(new CommandDefinition(
                _selectAllOpenObligationsSql,
                new { request.StudentId },
                transaction: transaction,
                cancellationToken: cancellationToken))).ToList();

            if (obligations.Count == 0)
            {
                return Result.Failure<GenerateBillingDocumentResponse>(
                    DomainError.Conflict(
                        "Finance.Billing.NoOpenObligations",
                        $"Student with ID '{request.StudentId}' has no open obligations to bill."));
            }
        }

        // 5. Calculate total billed amount and payment due date
        var totalBilledAmount = obligations.Sum(o => o.OriginalAmount);
        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var paymentDueDate = request.PaymentDueDate ?? obligations.Max(o => o.DueDate);
        var cleanNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var now = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();

        // 6. Generate sequential document number (BIL-{Year}-{Sequence:D5})
        var documentNumber = await BillingDocumentNumberGenerator.GenerateAsync(
            "bill",
            connection,
            transaction,
            cancellationToken);

        // 7. Insert header into finance.billing_documents
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
                IssueDate = issueDate,
                PaymentDueDate = paymentDueDate,
                TotalBilledAmount = totalBilledAmount,
                Notes = cleanNotes,
                CreatedAt = now,
                CreatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 8. Freeze itemized line items into finance.billing_document_items
        var lineItems = new List<GenerateBillingDocumentItemResponse>(obligations.Count);
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

            lineItems.Add(new GenerateBillingDocumentItemResponse(
                itemId,
                sequence,
                "obligation",
                "financial_obligations",
                obligation.Id,
                obligation.Description,
                obligation.OriginalAmount,
                "IDR"));
        }

        // 9. Update referenced obligations status to 'issued'
        var obligationIds = obligations.Select(o => o.Id).ToList();
        await connection.ExecuteAsync(new CommandDefinition(
            _updateObligationsStatusSql,
            new
            {
                ObligationIds = obligationIds,
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 10. Commit transaction atomically
        transaction.Commit();

        logger.LogInformation(
            "Compiled billing document {DocumentNumber} ({DocumentId}) with {LineCount} frozen items totaling {TotalAmount} IDR for student {StudentId}.",
            documentNumber,
            documentId,
            lineItems.Count,
            totalBilledAmount,
            request.StudentId);

        var response = new GenerateBillingDocumentResponse(
            documentId,
            documentNumber,
            account.Id,
            request.StudentId,
            "bill",
            recipientName,
            recipientRelationship,
            recipientContact,
            null,
            null,
            issueDate,
            paymentDueDate,
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

    private sealed record ObligationCandidateRow(
        Guid Id,
        string ObligationNumber,
        string Description,
        long OriginalAmount,
        DateOnly DueDate,
        string Status);
}
