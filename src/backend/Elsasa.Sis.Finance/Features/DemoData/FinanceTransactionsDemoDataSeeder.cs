namespace Elsasa.Sis.Finance.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous seeder for the Finance domain transactions: student financial accounts,
/// obligations with due schedules, billing documents, payers, payments, and payment
/// allocations (ADR-002, ADR-004, TD-06, OQ-008).
///
/// Semester 1 (historical): 100% billed and 100% paid in full.
/// Semester 2 (active): ~60% paid in full, ~20% partial, ~10% overdue (Grace),
///                       ~10% unpaid live-hook (Jonathan INV-SEM2-0042), and one
///                       pastoral scholarship waiver record.
/// </summary>
public sealed class FinanceTransactionsDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : IFinanceTransactionsDemoDataSeeder
{
    private const string _createdBy = "demo-data-seeder";

    // ─────────────────────────────────────────────────────────────────────
    // SQL templates
    // ─────────────────────────────────────────────────────────────────────

    private const string _insertAccountSql = """
        INSERT INTO finance.student_financial_accounts (
            id, account_number, student_id, currency_code, account_status,
            opened_date, created_at, created_by
        ) VALUES (
            @Id, @AccountNumber, @StudentId, 'IDR', 'active',
            @OpenedDate, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertObligationSql = """
        INSERT INTO finance.financial_obligations (
            id, obligation_number, student_account_id, student_id,
            charge_rule_id, policy_id, description, charge_type,
            is_mandatory, original_amount, currency_code,
            issue_date, due_date, academic_period_id,
            academic_period_label, status, authorized_by,
            created_at, created_by
        ) VALUES (
            @Id, @ObligationNumber, @StudentAccountId, @StudentId,
            @ChargeRuleId, @PolicyId, @Description, @ChargeType,
            @IsMandatory, @OriginalAmount, 'IDR',
            @IssueDate, @DueDate, @AcademicPeriodId,
            @AcademicPeriodLabel, @Status, @AuthorizedBy,
            @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertDueScheduleSql = """
        INSERT INTO finance.obligation_due_schedules (
            id, obligation_id, installment_sequence, due_date,
            scheduled_amount, currency_code, status, created_at, created_by
        ) VALUES (
            @Id, @ObligationId, @InstallmentSequence, @DueDate,
            @ScheduledAmount, 'IDR', @Status, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertPayerSql = """
        INSERT INTO finance.payers (
            id, payer_number, payer_name, payer_type,
            phone_number, email, billing_address,
            created_at, created_by
        ) VALUES (
            @Id, @PayerNumber, @PayerName, 'parent_guardian',
            @PhoneNumber, @Email, @BillingAddress,
            @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertBillingDocumentSql = """
        INSERT INTO finance.billing_documents (
            id, document_number, student_account_id, student_id,
            document_type, recipient_name, recipient_relationship,
            recipient_contact, billing_period_start, billing_period_end,
            issue_date, payment_due_date, total_billed_amount,
            currency_code, delivery_channel, delivery_status, sent_at,
            notes, created_at, created_by
        ) VALUES (
            @Id, @DocumentNumber, @StudentAccountId, @StudentId,
            'bill', @RecipientName, @RecipientRelationship,
            @RecipientContact, @BillingPeriodStart, @BillingPeriodEnd,
            @IssueDate, @PaymentDueDate, @TotalBilledAmount,
            'IDR', 'portal', @DeliveryStatus, @SentAt,
            @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertBillingItemSql = """
        INSERT INTO finance.billing_document_items (
            id, billing_document_id, line_sequence, item_type,
            reference_entity_type, reference_entity_id,
            description, amount, currency_code, created_at
        ) VALUES (
            @Id, @BillingDocumentId, @LineSequence, @ItemType,
            'financial_obligations', @ReferenceEntityId,
            @Description, @Amount, 'IDR', @CreatedAt
        );
        """;

    private const string _insertPaymentSql = """
        INSERT INTO finance.payments (
            id, payment_number, student_account_id, student_id,
            payer_id, payment_amount, currency_code,
            payment_method, external_reference, payment_date,
            confirmation_status, confirmation_date, confirmed_by,
            receipt_number, receipt_issued_at,
            created_at, created_by
        ) VALUES (
            @Id, @PaymentNumber, @StudentAccountId, @StudentId,
            @PayerId, @PaymentAmount, 'IDR',
            @PaymentMethod, @ExternalReference, @PaymentDate,
            @ConfirmationStatus, @ConfirmationDate, @ConfirmedBy,
            @ReceiptNumber, @ReceiptIssuedAt,
            @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertAllocationSql = """
        INSERT INTO finance.payment_allocations (
            id, allocation_number, student_account_id,
            obligation_id, source_type, payment_id,
            allocated_amount, currency_code,
            allocation_date, allocated_by, status,
            created_at, created_by
        ) VALUES (
            @Id, @AllocationNumber, @StudentAccountId,
            @ObligationId, @SourceType, @PaymentId,
            @AllocatedAmount, 'IDR',
            @AllocationDate, @AllocatedBy, @Status,
            @CreatedAt, @CreatedBy
        );
        """;

    /// <inheritdoc />
    public async Task<FinanceTransactionsSeedResult> SeedFinanceTransactionsAsync(
        OperationsSeedResult operationsContext,
        FinancePolicySeedResult financePolicyContext,
        StudentsSeedResult studentsContext,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var students = studentsContext.EnrolledStudentIds;
        var personas = studentsContext.Personas;
        var historicalPeriodId = operationsContext.HistoricalPeriodId;
        var activePeriodId = operationsContext.ActivePeriodId;
        var policyId = financePolicyContext.PolicyId;
        var tuitionRuleId = financePolicyContext.TuitionChargeRuleId;
        var facilityRuleId = financePolicyContext.FacilityChargeRuleId;
        var paceMaterialRuleId = financePolicyContext.PaceMaterialChargeRuleId;

        // SPP monthly amount: IDR 1,500,000
        const long sppMonthlyAmount = 1_500_000L;
        // Annual facility fee: IDR 10,000,000
        const long facilityAnnualAmount = 10_000_000L;
        // PACE material fee per unit: IDR 75,000 × 10 units
        const int paceUnitsPerStudent = 10;
        const long paceTotalFee = 75_000L * paceUnitsPerStudent;

        // Dynamic date anchoring derived from operationsContext.BaseDate (ADR-002, TD-03)
        var baseDate = DateOnly.FromDateTime(operationsContext.BaseDate);
        var sem1Start = baseDate.AddDays(-180);
        var sem1End = baseDate.AddDays(-35);
        var sem2Start = baseDate.AddDays(-30);
        var sem2End = baseDate.AddDays(120);

        var sem1Label = $"Semester 1 TP {operationsContext.AcademicYearCode.Replace("AY-", "")}";
        var sem2Label = $"Semester 2 TP {operationsContext.AcademicYearCode.Replace("AY-", "")}";

        // ─── Flagship personas ──────────────────────────────────────────
        var jonathanId = personas.JonathanStudentId;
        var graceId = personas.GraceStudentId;
        var budiId = personas.BudiStudentId;

        // Deterministic ordering of all enrolled students
        var allStudentIds = students.ToList();
        allStudentIds.Sort();

        var totalStudents = allStudentIds.Count;

        // Determine cohort distribution
        var paidFullCount = (int)Math.Floor(totalStudents * 0.60);
        var partialCount = (int)Math.Floor(totalStudents * 0.20);
        var overdueCount = 1; // Grace only
        var unpaidCount = totalStudents - paidFullCount - partialCount - overdueCount;
        if (unpaidCount < 1) unpaidCount = 1;
        paidFullCount = totalStudents - partialCount - overdueCount - unpaidCount;

        // Select pastoral student (deterministic, not a persona)
        var pastoralStudentId = allStudentIds
            .FirstOrDefault(id => id != jonathanId && id != graceId && id != budiId);

        // Assign each student a state
        var studentStates = new Dictionary<Guid, string>(totalStudents);
        var idx = 0;
        foreach (var sid in allStudentIds)
        {
            if (sid == jonathanId) { studentStates[sid] = "unpaid"; continue; }
            if (sid == graceId)    { studentStates[sid] = "overdue"; continue; }
            if (sid == pastoralStudentId) { studentStates[sid] = "unpaid"; continue; }

            if (idx < paidFullCount) { studentStates[sid] = "paid_full"; }
            else if (idx < paidFullCount + partialCount) { studentStates[sid] = "partial"; }
            else { studentStates[sid] = "unpaid"; }
            idx++;
        }

        // ─── Create financial accounts ───────────────────────────────────
        var accountById = new Dictionary<Guid, Guid>(allStudentIds.Count);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var periodRows = (await connection.QueryAsync<(Guid Id, DateTime StartDate, DateTime EndDate)>(
                new CommandDefinition(
                    "SELECT id AS Id, start_date AS StartDate, end_date AS EndDate FROM operations.operational_periods WHERE id IN (@HistoricalPeriodId, @ActivePeriodId);",
                    new { HistoricalPeriodId = historicalPeriodId, ActivePeriodId = activePeriodId },
                    transaction: transaction,
                    cancellationToken: cancellationToken))).ToDictionary(p => p.Id);

            if (periodRows.TryGetValue(historicalPeriodId, out var hRow))
            {
                sem1Start = DateOnly.FromDateTime(hRow.StartDate);
                sem1End = DateOnly.FromDateTime(hRow.EndDate);
            }

            if (periodRows.TryGetValue(activePeriodId, out var aRow))
            {
                sem2Start = DateOnly.FromDateTime(aRow.StartDate);
                sem2End = DateOnly.FromDateTime(aRow.EndDate);
            }

            foreach (var sid in allStudentIds)
            {
                var accountId = Guid.NewGuid();
                var accountNumber = $"ACC-{baseDate:yyyy}-{accountId.ToString()[..8].ToUpperInvariant()}";
                accountById[sid] = accountId;

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertAccountSql,
                    new
                    {
                        Id = accountId,
                        AccountNumber = accountNumber,
                        StudentId = sid,
                        OpenedDate = sem1Start,
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }



            // ─── Generate obligations and due schedules ────────────────────
            var allObligations = new List<(Guid Id, Guid StudentId, Guid AccountId, string Description, long Amount, DateOnly DueDate, Guid PeriodId, string PeriodLabel, string Status)>();
            var allDueSchedules = new List<(Guid Id, Guid ObligationId, int InstallmentSequence, DateOnly DueDate, long ScheduledAmount, string Status)>();

            foreach (var sid in allStudentIds)
            {
                var accountId = accountById[sid];
                var isJonathan = sid == jonathanId;
                var isPastoral = sid == pastoralStudentId;

                // ── Semester 1 obligations (historical, fully settled) ──
                for (int month = 1; month <= 6; month++)
                {
                    var monthAnchor = sem1Start.AddMonths(month - 1);
                    var dueDate = new DateOnly(monthAnchor.Year, monthAnchor.Month, Math.Min(10, DateTime.DaysInMonth(monthAnchor.Year, monthAnchor.Month)));
                    var oblId = Guid.NewGuid();
                    allObligations.Add((oblId, sid, accountId, $"SPP Bulanan - {dueDate:MMMM yyyy}", sppMonthlyAmount, dueDate, historicalPeriodId, sem1Label, "settled"));

                    var schedId = Guid.NewGuid();
                    allDueSchedules.Add((schedId, oblId, month, dueDate, sppMonthlyAmount, "paid"));
                }

                // Facility fee (S1) - Annual fee for TP 2026/2027 assessed in Semester 1
                var facilityOblS1 = Guid.NewGuid();
                var facilityDueDateS1 = new DateOnly(sem1Start.Year, sem1Start.Month, Math.Min(14, DateTime.DaysInMonth(sem1Start.Year, sem1Start.Month)));
                allObligations.Add((facilityOblS1, sid, accountId, $"Uang Gedung Tahunan - TP {operationsContext.AcademicYearCode.Replace("AY-", "")}", facilityAnnualAmount, facilityDueDateS1, historicalPeriodId, sem1Label, "settled"));

                var facilitySchedS1 = Guid.NewGuid();
                allDueSchedules.Add((facilitySchedS1, facilityOblS1, 1, facilityDueDateS1, facilityAnnualAmount, "paid"));

                // PACE fee (S1)
                var paceOblS1 = Guid.NewGuid();
                var paceDueDateS1 = new DateOnly(sem1Start.Year, sem1Start.Month, Math.Min(10, DateTime.DaysInMonth(sem1Start.Year, sem1Start.Month)));
                allObligations.Add((paceOblS1, sid, accountId, $"Biaya Materi PACE - TP {operationsContext.AcademicYearCode.Replace("AY-", "")}", paceTotalFee, paceDueDateS1, historicalPeriodId, sem1Label, "settled"));

                var paceSchedS1 = Guid.NewGuid();
                allDueSchedules.Add((paceSchedS1, paceOblS1, 1, paceDueDateS1, paceTotalFee, "paid"));

                // ── Semester 2 obligations (active) ──
                if (isJonathan)
                {
                    // Jonathan Pratama: Live cashier demonstration hook (SCR-FIN-02, TD-06).
                    // Exactly 1 month SPP obligation (IDR 1,500,000) for unpaid invoice INV-SEM2-0042.
                    var monthAnchor = sem2Start;
                    var dueDate = new DateOnly(monthAnchor.Year, monthAnchor.Month, Math.Min(10, DateTime.DaysInMonth(monthAnchor.Year, monthAnchor.Month)));
                    var oblId = Guid.NewGuid();
                    allObligations.Add((oblId, sid, accountId, $"SPP Bulanan - {dueDate:MMMM yyyy}", sppMonthlyAmount, dueDate, activePeriodId, sem2Label, "assessed"));

                    var schedId = Guid.NewGuid();
                    allDueSchedules.Add((schedId, oblId, 1, dueDate, sppMonthlyAmount, "pending"));
                }
                else
                {
                    // For other students in S2: 5 months of SPP
                    for (int month = 1; month <= 5; month++)
                    {
                        var monthAnchor = sem2Start.AddMonths(month - 1);
                        var dueDate = new DateOnly(monthAnchor.Year, monthAnchor.Month, Math.Min(10, DateTime.DaysInMonth(monthAnchor.Year, monthAnchor.Month)));
                        var oblId = Guid.NewGuid();
                        allObligations.Add((oblId, sid, accountId, $"SPP Bulanan - {dueDate:MMMM yyyy}", sppMonthlyAmount, dueDate, activePeriodId, sem2Label, "assessed"));

                        var schedId = Guid.NewGuid();
                        allDueSchedules.Add((schedId, oblId, month, dueDate, sppMonthlyAmount, "pending"));
                    }

                    // PACE fee (S2)
                    var paceOblS2 = Guid.NewGuid();
                    var paceDueDateS2 = new DateOnly(sem2Start.Year, sem2Start.Month, Math.Min(10, DateTime.DaysInMonth(sem2Start.Year, sem2Start.Month)));
                    allObligations.Add((paceOblS2, sid, accountId, $"Biaya Materi PACE - TP {operationsContext.AcademicYearCode.Replace("AY-", "")}", paceTotalFee, paceDueDateS2, activePeriodId, sem2Label, "assessed"));

                    var paceSchedS2 = Guid.NewGuid();
                    allDueSchedules.Add((paceSchedS2, paceOblS2, 1, paceDueDateS2, paceTotalFee, "pending"));

                    // Pastoral scholarship waiver for S2 SPP
                    if (isPastoral)
                    {
                        for (int month = 1; month <= 5; month++)
                        {
                            var monthAnchor = sem2Start.AddMonths(month - 1);
                            var waiverDueDate = new DateOnly(monthAnchor.Year, monthAnchor.Month, Math.Min(10, DateTime.DaysInMonth(monthAnchor.Year, monthAnchor.Month)));
                            var waiverOblId = Guid.NewGuid();
                            allObligations.Add((waiverOblId, sid, accountId, $"Beasiswa Pastoral - Waiver SPP {waiverDueDate:MMMM yyyy}", -sppMonthlyAmount, waiverDueDate, activePeriodId, sem2Label, "assessed"));

                            var waiverSchedId = Guid.NewGuid();
                            allDueSchedules.Add((waiverSchedId, waiverOblId, month, waiverDueDate, -sppMonthlyAmount, "pending"));
                        }
                    }
                }
            }

            // ─── Insert obligations ────────────────────────────────────────
            foreach (var (oblId, sid, accountId, desc, amt, dueDate, periodId, periodLabel, status) in allObligations)
            {
                var ruleId = ResolveChargeRule(desc, status, tuitionRuleId, facilityRuleId, paceMaterialRuleId);
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertObligationSql,
                    new
                    {
                        Id = oblId,
                        ObligationNumber = $"OBL-{periodId.ToString()[..8].ToUpperInvariant()}-{oblId:N}",
                        StudentAccountId = accountId,
                        StudentId = sid,
                        ChargeRuleId = ruleId,
                        PolicyId = policyId,
                        Description = desc,
                        ChargeType = ResolveChargeType(desc),
                        IsMandatory = true,
                        OriginalAmount = amt,
                        IssueDate = dueDate,
                        DueDate = dueDate,
                        AcademicPeriodId = periodId,
                        AcademicPeriodLabel = periodLabel,
                        Status = status,
                        AuthorizedBy = _createdBy,
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // ─── Insert due schedules ───────────────────────────────────────
            foreach (var (schedId, oblId, seq, schedDueDate, schedAmt, schedStatus) in allDueSchedules)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertDueScheduleSql,
                    new
                    {
                        Id = schedId,
                        ObligationId = oblId,
                        InstallmentSequence = seq,
                        DueDate = schedDueDate,
                        ScheduledAmount = schedAmt,
                        Status = schedStatus,
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // ─── Insert payers ─────────────────────────────────────────────
            var payerById = new Dictionary<Guid, Guid>(allStudentIds.Count);
            var payerCounter = 1;
            foreach (var sid in allStudentIds)
            {
                var payerId = Guid.NewGuid();
                payerById[sid] = payerId;
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertPayerSql,
                    new
                    {
                        Id = payerId,
                        PayerNumber = PayerNumberGenerator.Format(payerCounter),
                        PayerName = $"Parent of {sid:D}",
                        PhoneNumber = "+6281234567890",
                        Email = $"parent_{payerCounter}@example.com",
                        BillingAddress = "Yogyakarta, Indonesia",
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
                payerCounter++;
            }

            // ─── Group obligations by period and student ───────────────────
            var s1ObligationsByStudent = allObligations
                .Where(o => o.PeriodId == historicalPeriodId)
                .GroupBy(o => o.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var s2ObligationsByStudent = allObligations
                .Where(o => o.PeriodId == activePeriodId)
                .GroupBy(o => o.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int invoiceCount = 0;
            Guid jonathanUnpaidInvoiceId = DemoDataConstants.JonathanUnpaidInvoiceId;
            int paymentCount = 0;
            int allocSeq = 1;

            // ─── S1 billing documents (historical, all paid) ───────────────
            foreach (var (sid, s1Obss) in s1ObligationsByStudent.OrderBy(kv => kv.Key))
            {
                var accountId = accountById[sid];
                var docId = Guid.NewGuid();
                var docNumber = $"BIL-{sem1Start:yyyy}-{invoiceCount + 1:D5}";
                invoiceCount++;
                var totalBilled = s1Obss.Sum(o => o.Amount);

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertBillingDocumentSql,
                    new
                    {
                        Id = docId,
                        DocumentNumber = docNumber,
                        StudentAccountId = accountId,
                        StudentId = sid,
                        BillingPeriodStart = sem1Start,
                        BillingPeriodEnd = sem1End,
                        IssueDate = sem1Start,
                        PaymentDueDate = sem1Start.AddDays(14),
                        TotalBilledAmount = totalBilled,
                        DeliveryStatus = "paid",
                        SentAt = (DateTimeOffset?)null,
                        Notes = "Semester 1 billing - historical closed period",
                        CreatedAt = now,
                        CreatedBy = _createdBy,
                        RecipientName = sid.ToString(),
                        RecipientRelationship = "Self",
                        RecipientContact = "-"
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                for (int i = 0; i < s1Obss.Count; i++)
                {
                    var obl = s1Obss[i];
                    var itemId = Guid.NewGuid();
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertBillingItemSql,
                        new
                        {
                            Id = itemId,
                            BillingDocumentId = docId,
                            LineSequence = i + 1,
                            ItemType = "obligation",
                            ReferenceEntityId = obl.Id,
                            Description = obl.Description,
                            Amount = obl.Amount,
                            CreatedAt = now
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }

                // Consolidated payment for S1
                var payId = Guid.NewGuid();
                paymentCount++;
                var payNumber = PaymentNumberGenerator.Format(paymentCount);
                var receiptNum = ReceiptNumberGenerator.Format(paymentCount);
                var method = SelectPaymentMethod(paymentCount);
                var s1PaymentDate = sem1End.AddDays(-7);
                var s1PaymentOffset = new DateTimeOffset(s1PaymentDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertPaymentSql,
                    new
                    {
                        Id = payId,
                        PaymentNumber = payNumber,
                        StudentAccountId = accountId,
                        StudentId = sid,
                        PayerId = payerById[sid],
                        PaymentAmount = totalBilled,
                        PaymentMethod = method,
                        ExternalReference = (string?)null,
                        PaymentDate = s1PaymentDate,
                        ConfirmationStatus = "confirmed",
                        ConfirmationDate = s1PaymentOffset,
                        ConfirmedBy = "cashier-demo",
                        ReceiptNumber = receiptNum,
                        ReceiptIssuedAt = s1PaymentOffset,
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                // Allocate to each S1 obligation
                foreach (var obl in s1Obss)
                {
                    var allocId = Guid.NewGuid();
                    var allocNumber = AllocationNumberGenerator.Format(++allocSeq);
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertAllocationSql,
                        new
                        {
                            Id = allocId,
                            AllocationNumber = allocNumber,
                            StudentAccountId = accountId,
                            ObligationId = obl.Id,
                            SourceType = "payment",
                            PaymentId = payId,
                            AllocatedAmount = obl.Amount,
                            AllocationDate = s1PaymentOffset,
                            AllocatedBy = "demo-data-seeder",
                            Status = "active",
                            CreatedAt = now,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // ─── S2 billing documents (active) ─────────────────────────────
            int s2InvoiceSeq = 1;
            foreach (var (sid, s2Obss) in s2ObligationsByStudent.OrderBy(kv => kv.Key))
            {
                var accountId = accountById[sid];
                var isJonathan = sid == jonathanId;
                var docId = isJonathan ? jonathanUnpaidInvoiceId : Guid.NewGuid();
                string docNumber;
                if (isJonathan)
                {
                    docNumber = "INV-SEM2-0042";
                }
                else
                {
                    if (s2InvoiceSeq == 42)
                    {
                        s2InvoiceSeq++;
                    }
                    docNumber = $"INV-SEM2-{s2InvoiceSeq:D4}";
                    s2InvoiceSeq++;
                }
                invoiceCount++;
                var totalBilled = s2Obss.Sum(o => o.Amount);

                var state = studentStates[sid];
                string deliveryStatus;
                long? paidAmount = null;
                DateOnly? paymentDate = null;
                string? confirmationStatus = null;

                if (state == "paid_full")
                {
                    deliveryStatus = "paid";
                    paidAmount = totalBilled;
                    paymentDate = baseDate.AddDays(-5);
                    confirmationStatus = "confirmed";
                }
                else if (state == "partial")
                {
                    deliveryStatus = "partial";
                    var partialPct = ((int)sid.GetHashCode()) % 31 + 40; // 40-70%
                    paidAmount = (long)(totalBilled * partialPct / 100);
                    paymentDate = baseDate.AddDays(-5);
                    confirmationStatus = "confirmed";
                }
                else if (state == "overdue")
                {
                    deliveryStatus = "overdue";
                }
                else // unpaid
                {
                    deliveryStatus = "unpaid";
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertBillingDocumentSql,
                    new
                    {
                        Id = docId,
                        DocumentNumber = docNumber,
                        StudentAccountId = accountId,
                        StudentId = sid,
                        BillingPeriodStart = sem2Start,
                        BillingPeriodEnd = sem2End,
                        IssueDate = sem2Start,
                        PaymentDueDate = sem2Start.AddDays(14),
                        TotalBilledAmount = totalBilled,
                        DeliveryStatus = deliveryStatus,
                        SentAt = (DateTimeOffset?)null,
                        Notes = state == "unpaid" ? "Live demonstration invoice - awaiting payment" : null,
                        CreatedAt = now,
                        CreatedBy = _createdBy,
                        RecipientName = sid.ToString(),
                        RecipientRelationship = "Self",
                        RecipientContact = "-"
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                for (int i = 0; i < s2Obss.Count; i++)
                {
                    var obl = s2Obss[i];
                    var itemId = Guid.NewGuid();
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertBillingItemSql,
                        new
                        {
                            Id = itemId,
                            BillingDocumentId = docId,
                            LineSequence = i + 1,
                            ItemType = "obligation",
                            ReferenceEntityId = obl.Id,
                            Description = obl.Description,
                            Amount = obl.Amount,
                            CreatedAt = now
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }

                // Create payment and allocation for paid_full and partial states
                if (paidAmount.HasValue && paidAmount.Value > 0 && paymentDate.HasValue)
                {
                    var payId = Guid.NewGuid();
                    paymentCount++;
                    var payNumber = PaymentNumberGenerator.Format(paymentCount);
                    var receiptNum = ReceiptNumberGenerator.Format(paymentCount);
                    var method = SelectPaymentMethod(paymentCount);
                    var s2PaymentOffset = new DateTimeOffset(paymentDate.Value.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertPaymentSql,
                        new
                        {
                            Id = payId,
                            PaymentNumber = payNumber,
                            StudentAccountId = accountId,
                            StudentId = sid,
                            PayerId = payerById[sid],
                            PaymentAmount = paidAmount.Value,
                            PaymentMethod = method,
                            ExternalReference = (string?)null,
                            PaymentDate = paymentDate.Value,
                            ConfirmationStatus = confirmationStatus,
                            ConfirmationDate = s2PaymentOffset,
                            ConfirmedBy = "cashier-demo",
                            ReceiptNumber = receiptNum,
                            ReceiptIssuedAt = s2PaymentOffset,
                            CreatedAt = now,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    // Allocate pro-rata to obligations
                    var sortedObss = s2Obss
                        .OrderBy(o => o.DueDate)
                        .ThenByDescending(o => o.Amount < 0 ? 1 : 0)
                        .ToList();

                    long remaining = paidAmount.Value;
                    foreach (var obl in sortedObss)
                    {
                        if (remaining <= 0) break;
                        var allocAmount = Math.Min(remaining, obl.Amount > 0 ? obl.Amount : -obl.Amount);
                        var allocId = Guid.NewGuid();
                        var allocNumber = AllocationNumberGenerator.Format(++allocSeq);
                        await connection.ExecuteAsync(new CommandDefinition(
                            _insertAllocationSql,
                            new
                            {
                                Id = allocId,
                                AllocationNumber = allocNumber,
                                StudentAccountId = accountId,
                                ObligationId = obl.Id,
                                SourceType = "payment",
                                PaymentId = payId,
                                AllocatedAmount = allocAmount,
                                AllocationDate = s2PaymentOffset,
                                AllocatedBy = "demo-data-seeder",
                                Status = "active",
                                CreatedAt = now,
                                CreatedBy = _createdBy
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                        remaining -= allocAmount;
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return new FinanceTransactionsSeedResult(
                AccountsCreated: allStudentIds.Count,
                InvoicesCreated: invoiceCount,
                PaymentsCreated: paymentCount,
                JonathanUnpaidInvoiceId: jonathanUnpaidInvoiceId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Guid? ResolveChargeRule(string description, string status, Guid tuitionRule, Guid facilityRule, Guid paceRule)
    {
        var isPastoral = description.Contains("Beasiswa");
        if (status == "settled")
        {
            if (description.StartsWith("SPP", StringComparison.Ordinal)) return tuitionRule;
            if (description.Contains("Uang Gedung")) return facilityRule;
            if (description.Contains("PACE")) return paceRule;
        }
        else
        {
            if (isPastoral) return tuitionRule;
            if (description.StartsWith("SPP", StringComparison.Ordinal)) return tuitionRule;
            if (description.Contains("Uang Gedung")) return facilityRule;
            if (description.Contains("PACE")) return paceRule;
        }
        return null;
    }

    private static string ResolveChargeType(string description)
    {
        if (description.StartsWith("SPP", StringComparison.Ordinal)) return "tuition";
        if (description.Contains("Uang Gedung")) return "enrollment_fee";
        if (description.Contains("PACE")) return "activity_fee";
        return "tuition";
    }

    private static string SelectPaymentMethod(int sequence)
    {
        return (sequence % 3) switch
        {
            0 => "bank_transfer",
            1 => "cash",
            _ => "qris"
        };
    }
}
