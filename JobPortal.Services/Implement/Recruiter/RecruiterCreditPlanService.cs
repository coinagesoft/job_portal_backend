// File: JobPortal.Services/Implement/Recruiter/RecruiterCreditPlanService.cs

using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using JobPortal.Services.Implement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobPortal.Services.Implement.Public;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterCreditPlanService : IRecruiterCreditPlanService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecruiterCreditPlanService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IRecruiterInvoiceService _invoiceService;
        private readonly PayPalPaymentService _payPal;

        private const string GatewayRazorpay = "Razorpay";
        private const string GatewayPayPal = "PayPal";

        private string RazorpayKeyId =>
            _configuration["Razorpay:KeyId"]
            ?? throw new InvalidOperationException("Razorpay:KeyId missing in appsettings");

        private string RazorpayKeySecret =>
            _configuration["Razorpay:KeySecret"]
            ?? throw new InvalidOperationException("Razorpay:KeySecret missing in appsettings");

        public RecruiterCreditPlanService(
            AppDbContext context,
            ILogger<RecruiterCreditPlanService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IRecruiterInvoiceService invoiceService,
            PayPalPaymentService payPal)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Razorpay");
            _invoiceService = invoiceService;
            _payPal = payPal;
        }

        // ─────────────────────────────────────────────────────────────
        // 1. List active plans
        // ─────────────────────────────────────────────────────────────
        public async Task<List<CreditPlanResponseDto>> GetActivePlansAsync(string? region = null)
        {
            var query = _context.CreditPlans
                .Where(p => p.IsActive && p.Price > 0);

            if (!string.IsNullOrWhiteSpace(region))
            {
                query = query.Where(p => p.Region == region);
            }

            return await query
                .OrderBy(p => p.Price)
                .Select(p => new CreditPlanResponseDto
                {
                    PlanId = p.PlanId,
                    PlanName = p.PlanName,
                    Credits = p.Credits,
                    Price = p.Price,
                    ValidityMonths = p.ValidityMonths,
                    Region = p.Region,
                    Bonus = p.Bonus,
                    IsActive = p.IsActive
                })
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // 2. Create Razorpay order  (NO UserId from the caller)
        //    We resolve UserId from EmployerProfile internally.
        // ─────────────────────────────────────────────────────────────
        public async Task<CreatePlanOrderResponseDto> CreatePlanOrderAsync(
            Guid employerId,
            CreatePlanOrderRequestDto request)
        {
            // Resolve employer + get UserId in one query
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
                return Fail<CreatePlanOrderResponseDto>("Employer profile not found.");

            var plan = await _context.CreditPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlanId == request.PlanId && p.IsActive);

            if (plan == null)
                return Fail<CreatePlanOrderResponseDto>("Plan not found or is no longer active.");

            // The free plan is granted automatically once at registration
            // (see SubmitRegistrationAsync) and isn't meant to be
            // re-purchased. Block it here too, not just by hiding it from
            // GetActivePlansAsync, so it can't be bought via a direct API
            // call with its planId.
            if (plan.Price <= 0)
                return Fail<CreatePlanOrderResponseDto>(
                    "This plan is included automatically with your account and can't be purchased again.");

            int amountPaise = (int)(plan.Price * 100);   // ₹ → paise
            int gstAmountPaise = (int)Math.Round(amountPaise * 0.18m, MidpointRounding.AwayFromZero);
            int totalAmountPaise = amountPaise + gstAmountPaise;

            // ── Call Razorpay Orders API ──────────────────────────────
            string razorpayOrderId;
            try
            {
                razorpayOrderId = await CallRazorpayCreateOrderAsync(totalAmountPaise, plan.PlanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Razorpay order creation failed for PlanId={PlanId}", plan.PlanId);
                return Fail<CreatePlanOrderResponseDto>("Payment gateway error. Please try again.");
            }

            // ── Persist a pending PaymentTransaction ──────────────────
            //    UserId comes from employer.UserId — caller never passes it
            var txn = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = employer.UserId,   // ← resolved from DB
                EmployerId = employerId,
                TransactionType = "CreditPlanPurchase",
                PackType = plan.PlanName,
                CreditQuantity = plan.Credits,
                ValidityMonths = (byte)Math.Min(plan.ValidityMonths, 255),
                AmountPaise = amountPaise,
                GstAmountPaise = gstAmountPaise,
                TotalAmountPaise = totalAmountPaise,
                PaymentMethod = "Razorpay",
                RazorpayOrderId = razorpayOrderId,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(txn);
            await _context.SaveChangesAsync();

            return new CreatePlanOrderResponseDto
            {
                Success = true,
                Message = "Order created successfully.",
                RazorpayOrderId = razorpayOrderId,
                AmountPaise = totalAmountPaise,   // GST-inclusive — this is what the Razorpay SDK actually charges
                BaseAmountPaise = amountPaise,
                GstAmountPaise = gstAmountPaise,
                Currency = "INR",
                RazorpayKeyId = RazorpayKeyId,
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = plan.Credits,
                ValidityMonths = plan.ValidityMonths,
                TransactionId = txn.TransactionId
            };
        }

        // ─────────────────────────────────────────────────────────────
        // 3. Verify Razorpay signature → credit wallet → record purchase
        // ─────────────────────────────────────────────────────────────
        public async Task<VerifyPlanPaymentResponseDto> VerifyPlanPaymentAsync(
        Guid employerId,
        VerifyPlanPaymentRequestDto request)
        {
            // See SubmitRegistrationAsync in RecruiterRegistrationService.cs
            // for why this needs the execution-strategy wrapper: Program.cs
            // enables EnableRetryOnFailure, and a retrying strategy can't
            // work with a plain BeginTransactionAsync() unless it owns the
            // whole thing via ExecuteAsync.
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await LockTransactionAsync(request.TransactionId);

                    var txn = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t =>
                            t.TransactionId == request.TransactionId &&
                            t.EmployerId == employerId &&
                            t.PaymentMethod == GatewayRazorpay &&
                            t.PaymentStatus == "Pending");

                    if (txn == null)
                    {
                        return Fail<VerifyPlanPaymentResponseDto>(
                            "Transaction not found or already processed.");
                    }

                    if (!VerifySignature(
                            request.RazorpayOrderId ?? string.Empty,
                            request.RazorpayPaymentId ?? string.Empty,
                            request.RazorpaySignature ?? string.Empty))
                    {
                        txn.PaymentStatus = "Failed";

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();

                        _logger.LogWarning(
                            "Invalid Razorpay signature. Txn={TxnId}",
                            txn.TransactionId);

                        return Fail<VerifyPlanPaymentResponseDto>(
                            "Payment verification failed.");
                    }

                    var alreadyUsed = await _context.PaymentTransactions
                        .AnyAsync(t =>
                            t.TransactionId != txn.TransactionId &&
                            t.RazorpayPaymentId == request.RazorpayPaymentId &&
                            t.PaymentStatus == "Completed");

                    if (alreadyUsed)
                    {
                        return Fail<VerifyPlanPaymentResponseDto>(
                            "This payment has already been applied.");
                    }

                    txn.RazorpayOrderId = request.RazorpayOrderId;
                    txn.RazorpayPaymentId = request.RazorpayPaymentId;

                    var outcome = await ApplyCompletedPurchaseAsync(employerId, txn);

                    if (outcome == null)
                    {
                        return Fail<VerifyPlanPaymentResponseDto>(
                            "Associated credit plan not found.");
                    }

                    await dbTransaction.CommitAsync();

                    await TryEmailInvoiceAsync(outcome.InvoiceId, employerId);

                    return new VerifyPlanPaymentResponseDto
                    {
                        Success = true,
                        Message = $"Payment successful! {outcome.Credits} credits added to your wallet.",
                        NewCreditBalance = outcome.NewBalance,
                        PurchaseId = outcome.PurchaseId
                    };
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();

                    _logger.LogError(
                        ex,
                        "VerifyPlanPaymentAsync failed for EmployerId={EmployerId}",
                        employerId);

                    return new VerifyPlanPaymentResponseDto
                    {
                        Success = false,
                        Message = "Payment verification failed. Please contact support if the amount was deducted."
                    };
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 4. PayPal — create order (outside India only)
        // ─────────────────────────────────────────────────────────────
        public async Task<CreatePlanOrderResponseDto> CreatePayPalPlanOrderAsync(
            Guid employerId,
            CreatePlanOrderRequestDto request,
            string visitorRegion)
        {
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
                return Fail<CreatePlanOrderResponseDto>("Employer profile not found.");

            var plan = await _context.CreditPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlanId == request.PlanId && p.IsActive);

            if (plan == null)
                return Fail<CreatePlanOrderResponseDto>("Plan not found or is no longer active.");

            if (plan.Price <= 0)
                return Fail<CreatePlanOrderResponseDto>(
                    "This plan is included automatically with your account and can't be purchased again.");

            // Region-wise pricing: the plan must belong to the region the server
            // detected for this caller, and India plans are Razorpay-only.
            if (string.IsNullOrWhiteSpace(visitorRegion) ||
                !string.Equals(plan.Region, visitorRegion, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(plan.Region, "in", StringComparison.OrdinalIgnoreCase))
            {
                return Fail<CreatePlanOrderResponseDto>(
                    "This plan isn't available for your region.");
            }

            var currency = VisitorCountryService.CurrencyForRegion(plan.Region);

            if (!VisitorCountryService.IsPayPalCurrency(currency))
            {
                _logger.LogError(
                    "Plan {PlanId} (region {Region}) is priced in {Currency}, which PayPal can't settle.",
                    plan.PlanId, plan.Region, currency);

                return Fail<CreatePlanOrderResponseDto>(
                    "This plan can't be paid with PayPal. Please contact support.");
            }

            // Minor units (cents / pence). No GST outside India.
            int amountMinor = (int)Math.Round(plan.Price * 100m, MidpointRounding.AwayFromZero);
            var transactionId = Guid.NewGuid();

            PayPalOrderResult order;
            try
            {
                // reference_id / custom_id = our transaction id; checked again at capture time.
                order = await _payPal.CreateOrderAsync(
                    amountMinor / 100m,
                    currency,
                    transactionId.ToString("N"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayPal order creation failed for PlanId={PlanId}", plan.PlanId);
                return Fail<CreatePlanOrderResponseDto>("Payment gateway error. Please try again.");
            }

            _context.PaymentTransactions.Add(new PaymentTransaction
            {
                TransactionId = transactionId,
                UserId = employer.UserId,
                EmployerId = employerId,
                TransactionType = "CreditPlanPurchase",
                PackType = plan.PlanName,
                CreditQuantity = plan.Credits,
                ValidityMonths = (byte)Math.Min(plan.ValidityMonths, 255),
                AmountPaise = amountMinor,       // column name is legacy: it holds minor units of the plan currency
                GstAmountPaise = 0,
                TotalAmountPaise = amountMinor,
                PaymentMethod = GatewayPayPal,
                PayPalOrderId = order.OrderId,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new CreatePlanOrderResponseDto
            {
                Success = true,
                Message = "Order created successfully.",
                PaymentGateway = GatewayPayPal,
                PayPalOrderId = order.OrderId,
                PayPalApprovalUrl = order.ApprovalUrl,
                PayPalClientId = _configuration["PayPal:ClientId"],
                AmountPaise = amountMinor,
                BaseAmountPaise = amountMinor,
                GstAmountPaise = 0,
                Currency = currency,
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = plan.Credits,
                ValidityMonths = plan.ValidityMonths,
                TransactionId = transactionId
            };
        }

        // ─────────────────────────────────────────────────────────────
        // 5. PayPal — capture, verify against our own record, then credit
        // ─────────────────────────────────────────────────────────────
        public async Task<VerifyPlanPaymentResponseDto> VerifyPayPalPlanPaymentAsync(
            Guid employerId,
            VerifyPlanPaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.PayPalOrderId))
                return Fail<VerifyPlanPaymentResponseDto>("PayPalOrderId is required.");

            // The order id must belong to a transaction WE created for THIS employer.
            var txn = await _context.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TransactionId == request.TransactionId &&
                    t.EmployerId == employerId &&
                    t.PaymentMethod == GatewayPayPal &&
                    t.PayPalOrderId == request.PayPalOrderId);

            if (txn == null)
                return Fail<VerifyPlanPaymentResponseDto>("Transaction not found.");

            if (txn.PaymentStatus == "Completed")
                return await AlreadyCompletedAsync(employerId);

            if (txn.PaymentStatus != "Pending")
                return Fail<VerifyPlanPaymentResponseDto>("Transaction not found or already processed.");

            // Capture OUTSIDE the db transaction so we never hold a lock during an HTTP call.
            PayPalCaptureResult capture;
            try
            {
                capture = await _payPal.CaptureOrderAsync(request.PayPalOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PayPal capture failed. Txn={TxnId}", txn.TransactionId);
                return Fail<VerifyPlanPaymentResponseDto>(
                    "We couldn't confirm your PayPal payment. If money was deducted, contact support with your transaction reference.");
            }

            var plan = await FindPlanForTransactionAsync(txn);
            var expectedCurrency = plan == null
                ? null
                : VisitorCountryService.CurrencyForRegion(plan.Region);

            var problem = ValidateCapture(txn, capture, expectedCurrency);

            if (problem != null)
            {
                _logger.LogCritical(
                    "PayPal capture REJECTED ({Problem}). Txn={TxnId} Order={OrderId} Capture={CaptureId} Status={Status}/{CaptureStatus}. " +
                    "If the capture completed, the buyer may need a manual refund.",
                    problem, txn.TransactionId, capture.OrderId, capture.CaptureId, capture.Status, capture.CaptureStatus);

                await MarkTransactionFailedAsync(txn.TransactionId);

                return Fail<VerifyPlanPaymentResponseDto>(
                    "Payment verification failed. Please contact support if the amount was deducted.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await LockTransactionAsync(txn.TransactionId);

                    var tracked = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t =>
                            t.TransactionId == txn.TransactionId &&
                            t.PaymentStatus == "Pending");

                    // A concurrent request (double click / webhook) already applied it.
                    if (tracked == null)
                    {
                        await dbTransaction.RollbackAsync();
                        return await AlreadyCompletedAsync(employerId);
                    }

                    var captureUsed = await _context.PaymentTransactions
                        .AnyAsync(t =>
                            t.TransactionId != tracked.TransactionId &&
                            t.PayPalCaptureId == capture.CaptureId &&
                            t.PaymentStatus == "Completed");

                    if (captureUsed)
                    {
                        return Fail<VerifyPlanPaymentResponseDto>(
                            "This payment has already been applied.");
                    }

                    tracked.PayPalCaptureId = capture.CaptureId;

                    var outcome = await ApplyCompletedPurchaseAsync(employerId, tracked);

                    if (outcome == null)
                    {
                        return Fail<VerifyPlanPaymentResponseDto>(
                            "Associated credit plan not found.");
                    }

                    await dbTransaction.CommitAsync();

                    await TryEmailInvoiceAsync(outcome.InvoiceId, employerId);

                    return new VerifyPlanPaymentResponseDto
                    {
                        Success = true,
                        Message = $"Payment successful! {outcome.Credits} credits added to your wallet.",
                        NewCreditBalance = outcome.NewBalance,
                        PurchaseId = outcome.PurchaseId
                    };
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();

                    _logger.LogError(
                        ex,
                        "VerifyPayPalPlanPaymentAsync failed for EmployerId={EmployerId} Txn={TxnId}",
                        employerId, txn.TransactionId);

                    return new VerifyPlanPaymentResponseDto
                    {
                        Success = false,
                        Message = "Payment verification failed. Please contact support if the amount was deducted."
                    };
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 6. PayPal webhook backstop
        // ─────────────────────────────────────────────────────────────
        public async Task<PayPalWebhookResult> ReconcilePayPalPaymentAsync(
            Guid transactionId,
            string paypalOrderId)
        {
            var txn = await _context.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TransactionId == transactionId &&
                    t.PaymentMethod == GatewayPayPal);

            // Not one of ours, or the order id doesn't belong to that transaction.
            if (txn == null ||
                txn.EmployerId == null ||
                !string.Equals(txn.PayPalOrderId, paypalOrderId, StringComparison.Ordinal))
            {
                return PayPalWebhookResult.Ignored;
            }

            if (txn.PaymentStatus == "Completed")
                return PayPalWebhookResult.Completed;

            // The browser flow may have marked it Failed (e.g. capture was still PENDING review
            // when the buyer returned) while PayPal later completed it. Re-open it so the same
            // verification rules decide — the capture is still checked against our own record.
            if (txn.PaymentStatus == "Failed")
            {
                var reopened = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.PaymentStatus == "Failed");

                if (reopened != null)
                {
                    reopened.PaymentStatus = "Pending";
                    await _context.SaveChangesAsync();
                }
            }
            else if (txn.PaymentStatus != "Pending")
            {
                return PayPalWebhookResult.Ignored;
            }

            // Same capture + validation + credit path as the browser flow (capture of an
            // already-captured order falls back to reading its state).
            var result = await VerifyPayPalPlanPaymentAsync(
                txn.EmployerId.Value,
                new VerifyPlanPaymentRequestDto
                {
                    TransactionId = txn.TransactionId,
                    PayPalOrderId = paypalOrderId,
                    PaymentGateway = GatewayPayPal
                });

            if (result.Success)
                return PayPalWebhookResult.Completed;

            // Distinguish "PayPal/DB hiccup" (retry) from "verification said no" (don't retry forever).
            var after = await _context.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.TransactionId == transactionId)
                .Select(t => t.PaymentStatus)
                .FirstOrDefaultAsync();

            return after == "Failed"
                ? PayPalWebhookResult.Rejected
                : PayPalWebhookResult.Retry;
        }

        public async Task MarkPayPalTransactionFailedAsync(Guid transactionId)
        {
            var row = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.TransactionId == transactionId &&
                    t.PaymentMethod == GatewayPayPal &&
                    t.PaymentStatus == "Pending");

            if (row == null) return;

            row.PaymentStatus = "Failed";
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // Shared purchase logic (Razorpay + PayPal)
        // Must be called inside an open db transaction; the caller commits.
        // ─────────────────────────────────────────────────────────────
        private sealed record PurchaseOutcome(int Credits, int NewBalance, Guid PurchaseId, Guid InvoiceId);

        private async Task<PurchaseOutcome?> ApplyCompletedPurchaseAsync(
            Guid employerId,
            PaymentTransaction txn)
        {
            var plan = await FindPlanForTransactionAsync(txn);

            if (plan == null)
            {
                _logger.LogError("Credit Plan not found. PackType={PackType}", txn.PackType);
                return null;
            }

            // What the buyer was actually charged for is what we deliver — taken from
            // the stored transaction, not re-read from a plan an admin may have edited.
            int credits = txn.CreditQuantity ?? plan.Credits;
            int months = txn.ValidityMonths.HasValue ? (int)txn.ValidityMonths.Value : plan.ValidityMonths;

            txn.PaymentStatus = "Completed";
            txn.CreditsAddedAt = DateTime.UtcNow;

            var wallet = await _context.CreditWallets
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (wallet == null)
            {
                wallet = new CreditWallet
                {
                    Wallet_Id = Guid.NewGuid(),
                    EmployerId = employerId,
                    CreditBalance = credits,
                    PackageName = plan.PlanName,
                    SharedWallet = true,
                    PackExpiresAt = DateTime.UtcNow.AddMonths(months),
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CreditWallets.Add(wallet);
            }
            else
            {
                wallet.CreditBalance = (wallet.CreditBalance) + credits;
                wallet.PackageName = plan.PlanName;

                var baseDate =
                    wallet.PackExpiresAt.HasValue &&
                    wallet.PackExpiresAt.Value > DateTime.UtcNow
                        ? wallet.PackExpiresAt.Value
                        : DateTime.UtcNow;

                wallet.PackExpiresAt = baseDate.AddMonths(months);
                wallet.UpdatedAt = DateTime.UtcNow;
            }

            // AssignedBy = owner's login identity (UserId), not the EmployerId.
            var ownerUserId = await _context.EmployerProfiles
                .Where(e => e.EmployerId == employerId)
                .Select(e => e.UserId)
                .FirstOrDefaultAsync();

            var purchase = new EmployerPlanPurchase
            {
                EmployerCreditPlanId = Guid.NewGuid(),
                EmployerId = employerId,
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = credits,
                Price = txn.AmountPaise / 100m,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(months),
                IsActive = true,
                AssignedBy = ownerUserId
            };

            _context.EmployerPlanPurchase.Add(purchase);

            var invoiceNumber = await GenerateInvoiceNumberAsync();

            // Invoice amount columns are int (whole currency units). Razorpay keeps its
            // existing truncating behaviour; PayPal rounds so 49.99 USD isn't stored as 49.
            bool isPayPal = txn.PaymentMethod == GatewayPayPal;
            int Whole(int minor) => isPayPal
                ? (int)Math.Round(minor / 100m, MidpointRounding.AwayFromZero)
                : minor / 100;

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                TransactionId = txn.TransactionId,
                UserId = txn.UserId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                InvoiceAmount = Whole(txn.AmountPaise),
                InvoiceGst = Whole(txn.GstAmountPaise),
                InvoiceTotal = Whole(txn.TotalAmountPaise),
                InvoiceS3Url = null, // PDF generated on demand — see RecruiterInvoiceService.DownloadInvoicePdfAsync
                CreatedAt = DateTime.UtcNow,

                // Assigning the navigation populates the FK shadow column
                // (see the note on the original implementation of this method).
                PaymentTransaction = txn
            };

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Employer {EmployerId} purchased plan {PlanName} via {Gateway}. Credits={Credits}",
                employerId, plan.PlanName, txn.PaymentMethod, credits);

            return new PurchaseOutcome(
                credits,
                wallet.CreditBalance,
                purchase.EmployerCreditPlanId,
                invoice.InvoiceId);
        }

        // The transaction has no PlanId column, so match on what we stored. PlanName
        // alone is ambiguous now that the same plan name exists in several regions.
        private async Task<CreditPlan?> FindPlanForTransactionAsync(PaymentTransaction txn)
        {
            var candidates = await _context.CreditPlans
                .AsNoTracking()
                .Where(p => p.PlanName == txn.PackType && p.IsActive)
                .ToListAsync();

            var paidPrice = txn.PaymentMethod == GatewayPayPal
                ? txn.AmountPaise / 100m
                : (decimal?)null; // Razorpay amounts include GST; matched by credits below

            return candidates.FirstOrDefault(p =>
                       p.Credits == txn.CreditQuantity &&
                       (paidPrice == null || p.Price == paidPrice))
                   ?? candidates.FirstOrDefault(p => p.Credits == txn.CreditQuantity)
                   ?? candidates.FirstOrDefault();
        }

        // Returns null when the capture is acceptable, otherwise a short reason.
        private static string? ValidateCapture(
            PaymentTransaction txn,
            PayPalCaptureResult capture,
            string? expectedCurrency)
        {
            if (!capture.IsCompleted)
                return $"not completed ({capture.Status}/{capture.CaptureStatus})";

            if (string.IsNullOrWhiteSpace(capture.CaptureId))
                return "missing capture id";

            if (!string.Equals(capture.CustomId, txn.TransactionId.ToString("N"), StringComparison.Ordinal))
                return "custom_id does not match our transaction";

            if (string.IsNullOrWhiteSpace(expectedCurrency))
                return "could not determine expected currency";

            if (!string.Equals(capture.CapturedCurrency, expectedCurrency, StringComparison.OrdinalIgnoreCase))
                return $"currency mismatch (got {capture.CapturedCurrency}, expected {expectedCurrency})";

            if (capture.CapturedAmount is null ||
                Math.Round(capture.CapturedAmount.Value * 100m) != txn.TotalAmountPaise)
                return $"amount mismatch (got {capture.CapturedAmount}, expected {txn.TotalAmountPaise / 100m})";

            return null;
        }

        private async Task<VerifyPlanPaymentResponseDto> AlreadyCompletedAsync(Guid employerId)
        {
            var balance = await _context.CreditWallets
                .AsNoTracking()
                .Where(w => w.EmployerId == employerId)
                .Select(w => w.CreditBalance)
                .FirstOrDefaultAsync();

            return new VerifyPlanPaymentResponseDto
            {
                Success = true,
                Message = "Payment already confirmed. Your credits are in your wallet.",
                NewCreditBalance = balance
            };
        }

        private async Task MarkTransactionFailedAsync(Guid transactionId)
        {
            var row = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.PaymentStatus == "Pending");

            if (row == null) return;

            row.PaymentStatus = "Failed";
            await _context.SaveChangesAsync();
        }

        // Serialises concurrent verifies of the same transaction (double click, webhook race).
        // Transaction-scoped: released automatically on commit/rollback.
        private Task LockTransactionAsync(Guid transactionId)
        {
            var key = transactionId.ToString("N");
            return _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({key}))");
        }

        private async Task TryEmailInvoiceAsync(Guid invoiceId, Guid employerId)
        {
            // Best-effort: a failed send must never undo a successful payment.
            try
            {
                await _invoiceService.EmailInvoiceAsync(invoiceId, employerId);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(
                    emailEx,
                    "Could not email invoice {InvoiceId} for employer {EmployerId}.",
                    invoiceId, employerId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Legacy admin-assign  (unchanged)
        // ─────────────────────────────────────────────────────────────
        public async Task<CommonResponseDto> BuyPlanAsync(Guid employerId, Guid planId)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
                return new CommonResponseDto { Success = false, Message = "Employer not found." };

            var plan = await _context.CreditPlans
                .FirstOrDefaultAsync(x => x.PlanId == planId && x.IsActive);

            if (plan == null)
                return new CommonResponseDto { Success = false, Message = "Plan not found." };

            var wallet = await _context.CreditWallets
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (wallet == null)
            {
                wallet = new CreditWallet
                {
                    Wallet_Id = Guid.NewGuid(),
                    EmployerId = employerId,
                    CreditBalance = plan.Credits,
                    PackageName = plan.PlanName,
                    PackExpiresAt = DateTime.UtcNow.AddMonths(plan.ValidityMonths),
                    SharedWallet = true,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CreditWallets.Add(wallet);
            }
            else
            {
                wallet.CreditBalance += plan.Credits;
                wallet.PackageName = plan.PlanName;

                var baseDate = wallet.PackExpiresAt > DateTime.UtcNow
                    ? wallet.PackExpiresAt.Value : DateTime.UtcNow;

                wallet.PackExpiresAt = baseDate.AddMonths(plan.ValidityMonths);
                wallet.UpdatedAt = DateTime.UtcNow;
            }

            // AssignedBy should be the owner's login identity (UserId), not
            // the EmployerId — see the matching fix in VerifyPlanPaymentAsync.
            var ownerUserId = await _context.EmployerProfiles
                .Where(e => e.EmployerId == employerId)
                .Select(e => e.UserId)
                .FirstOrDefaultAsync();

            _context.EmployerPlanPurchase.Add(new EmployerPlanPurchase
            {
                EmployerCreditPlanId = Guid.NewGuid(),
                EmployerId = employerId,
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = plan.Credits,
                Price = plan.Price,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(plan.ValidityMonths),
                IsActive = true,
                AssignedBy = ownerUserId
            });

            await _context.SaveChangesAsync();
            return new CommonResponseDto { Success = true, Message = "Plan purchased successfully." };
        }

        // ─────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────
        private async Task<string> CallRazorpayCreateOrderAsync(int amountPaise, Guid planId)
        {
            string credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{RazorpayKeyId}:{RazorpayKeySecret}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
            req.Headers.TryAddWithoutValidation("Authorization", $"Basic {credentials}");

            req.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    amount = amountPaise,
                    currency = "INR",
                    receipt = $"PLAN-{planId.ToString("N")[..12]}",
                    payment_capture = 1
                }),
                Encoding.UTF8,
                "application/json");

            var res = await _httpClient.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Razorpay error {res.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("id").GetString()
                ?? throw new Exception("Razorpay response missing 'id'.");
        }

        private bool VerifySignature(string orderId, string paymentId, string signature)
        {
            var payload = Encoding.UTF8.GetBytes($"{orderId}|{paymentId}");
            var key = Encoding.UTF8.GetBytes(RazorpayKeySecret);

            using var hmac = new HMACSHA256(key);
            var computed = BitConverter.ToString(hmac.ComputeHash(payload))
                                         .Replace("-", "")
                                         .ToLowerInvariant();

            return computed == signature;
        }

        // tiny helper so we don't repeat { Success=false, Message=... }
        private static T Fail<T>(string message) where T : new()
        {
            dynamic obj = new T();
            obj.Success = false;
            obj.Message = message;
            return obj;
        }

        // Generates a sequential, per-month invoice number, e.g. INV-202607-0001.
        //
        // IMPORTANT: this is called from inside VerifyPlanPaymentAsync's open
        // db transaction.
        //
        // Two things have to both be true for this to be safe:
        //
        // 1. NO RACING — two payment verifications for the same month
        //    running at the same time (double-submitted pay button, two
        //    sub-users checking out together, a retried webhook, etc.)
        //    must not compute the same "next number". We take a Postgres
        //    advisory *transaction* lock keyed on the month prefix before
        //    reading anything. The lock is scoped to the current db
        //    transaction and released automatically on commit/rollback, so
        //    a second concurrent call for the same month simply waits here
        //    until the first one has committed, instead of racing it.
        //
        // 2. NO GAP-REUSE — even with no concurrency at all, basing the
        //    next number on COUNT(*) of this month's rows is wrong: COUNT
        //    only equals "the highest number issued so far" if every row
        //    for the month is still present with none missing. If a row
        //    is ever missing for any reason (a manually deleted test
        //    invoice, cleanup scripts, an old bug that partially
        //    committed before this fix existed, etc.), COUNT undercounts
        //    and the next purchase regenerates a number that's already
        //    sitting in the table — a guaranteed,100%-reproducible
        //    duplicate-key error on every subsequent attempt, with zero
        //    concurrency involved. We use MAX(existing suffix) + 1
        //    instead, which always resumes above the highest number ever
        //    actually issued this month — a missing row just leaves a
        //    gap in the sequence, it never gets reused.
        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({prefix}))");

            var suffixesThisMonth = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .Select(i => i.InvoiceNumber.Substring(prefix.Length))
                .ToListAsync();

            int maxNumber = suffixesThisMonth
                .Select(s => int.TryParse(s, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix}{(maxNumber + 1):D4}";
        }
    }
}