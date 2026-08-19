// File: JobPortal.Services/Implement/Recruiter/RecruiterCreditPlanService.cs

using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterCreditPlanService : IRecruiterCreditPlanService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecruiterCreditPlanService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IRecruiterInvoiceService _invoiceService;

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
            IRecruiterInvoiceService invoiceService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Razorpay");
            _invoiceService = invoiceService;
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
                    //--------------------------------------------------------
                    // Load pending transaction
                    //--------------------------------------------------------

                    var txn = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t =>
                            t.TransactionId == request.TransactionId &&
                            t.EmployerId == employerId &&
                            t.PaymentStatus == "Pending");

                    if (txn == null)
                    {
                        return Fail<VerifyPlanPaymentResponseDto>(
                            "Transaction not found or already processed.");
                    }

                    //--------------------------------------------------------
                    // Verify Razorpay Signature
                    //--------------------------------------------------------

                    if (!VerifySignature(
                            request.RazorpayOrderId,
                            request.RazorpayPaymentId,
                            request.RazorpaySignature))
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

                    //--------------------------------------------------------
                    // Prevent duplicate payment usage
                    //--------------------------------------------------------

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

                    //--------------------------------------------------------
                    // Load Credit Plan
                    //--------------------------------------------------------

                    var plan = await _context.CreditPlans
                        .FirstOrDefaultAsync(p =>
                            p.PlanName == txn.PackType &&
                            p.IsActive);

                    if (plan == null)
                    {
                        _logger.LogError(
                            "Credit Plan not found. PackType={PackType}",
                            txn.PackType);

                        return Fail<VerifyPlanPaymentResponseDto>(
                            "Associated credit plan not found.");
                    }

                    //--------------------------------------------------------
                    // Complete Transaction
                    //--------------------------------------------------------

                    txn.RazorpayOrderId = request.RazorpayOrderId;
                    txn.RazorpayPaymentId = request.RazorpayPaymentId;
                    txn.PaymentStatus = "Completed";
                    txn.CreditsAddedAt = DateTime.UtcNow;

                    //--------------------------------------------------------
                    // Wallet
                    //--------------------------------------------------------

                    var wallet = await _context.CreditWallets
                        .FirstOrDefaultAsync(x =>
                            x.EmployerId == employerId);

                    if (wallet == null)
                    {
                        wallet = new CreditWallet
                        {
                            Wallet_Id = Guid.NewGuid(),
                            EmployerId = employerId,
                            CreditBalance = plan.Credits,
                            PackageName = plan.PlanName,
                            SharedWallet = true,
                            PackExpiresAt = DateTime.UtcNow.AddMonths(plan.ValidityMonths),
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.CreditWallets.Add(wallet);
                    }
                    else
                    {
                        wallet.CreditBalance =
                            (wallet.CreditBalance) + plan.Credits;

                        wallet.PackageName = plan.PlanName;

                        var baseDate =
                            wallet.PackExpiresAt.HasValue &&
                            wallet.PackExpiresAt.Value > DateTime.UtcNow
                                ? wallet.PackExpiresAt.Value
                                : DateTime.UtcNow;

                        wallet.PackExpiresAt =
                            baseDate.AddMonths(plan.ValidityMonths);

                        wallet.UpdatedAt = DateTime.UtcNow;
                    }

                    //--------------------------------------------------------
                    // Purchase History
                    //--------------------------------------------------------

                    // AssignedBy should be the owner's login identity (UserId) —
                    // that's what transaction-history name resolution matches
                    // against — not the EmployerId itself, which can never
                    // equal a User's UserId.
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
                        Credits = plan.Credits,
                        Price = plan.Price,
                        AssignedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMonths(plan.ValidityMonths),
                        IsActive = true,
                        AssignedBy = ownerUserId
                    };

                    _context.EmployerPlanPurchase.Add(purchase);

                    //--------------------------------------------------------
                    // Invoice (GST-compliant billing record)
                    //--------------------------------------------------------

                    var invoiceNumber = await GenerateInvoiceNumberAsync();

                    var invoice = new Invoice
                    {
                        InvoiceId = Guid.NewGuid(),
                        TransactionId = txn.TransactionId,
                        UserId = txn.UserId,
                        InvoiceNumber = invoiceNumber,
                        InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        InvoiceAmount = txn.AmountPaise / 100,
                        InvoiceGst = txn.GstAmountPaise / 100,
                        InvoiceTotal = txn.TotalAmountPaise / 100,
                        InvoiceS3Url = null, // PDF is generated on demand — see RecruiterInvoiceService.DownloadInvoicePdfAsync
                        CreatedAt = DateTime.UtcNow,

                        // NOTE: the DB's actual FK constraint is on a separate shadow
                        // column (PaymentTransactionTransactionId) that EF created
                        // because the "PaymentTransaction" nav property doesn't match
                        // the "TransactionId" FK property by convention. Setting the
                        // scalar TransactionId above does NOT populate that shadow
                        // column. Assigning the navigation here — txn is already
                        // tracked by this same DbContext — lets EF resolve the
                        // shadow FK from it automatically at SaveChanges time.
                        PaymentTransaction = txn
                    };

                    _context.Invoices.Add(invoice);

                    //--------------------------------------------------------
                    // Save
                    //--------------------------------------------------------

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation(
                        "Employer {EmployerId} purchased plan {PlanName}. Credits={Credits}",
                        employerId,
                        plan.PlanName,
                        plan.Credits);

                    // ── Email the invoice right away so the employer has a
                    // copy in their inbox without needing to visit the
                    // Invoices page. Best-effort: a failed send (bad SMTP
                    // config, no contact email on file, etc.) should never
                    // undo a successful payment, so this is swallowed and
                    // just logged — the invoice is still downloadable from
                    // the Invoices page regardless. ────────────────────────
                    try
                    {
                        await _invoiceService.EmailInvoiceAsync(invoice.InvoiceId, employerId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(
                            emailEx,
                            "Could not email invoice {InvoiceId} for employer {EmployerId}.",
                            invoice.InvoiceId,
                            employerId);
                    }

                    return new VerifyPlanPaymentResponseDto
                    {
                        Success = true,
                        Message = $"Payment successful! {plan.Credits} credits added to your wallet.",
                        NewCreditBalance = wallet.CreditBalance,
                        PurchaseId = purchase.EmployerCreditPlanId
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
        // db transaction. "COUNT existing rows, then INSERT count+1" is not
        // atomic on its own — if two payment verifications for the same
        // month run at the same time (double-submitted pay button, two
        // sub-users checking out together, a retried webhook, etc.) both
        // transactions can COUNT the same value before either has committed,
        // so both try to insert the same InvoiceNumber and the second one
        // fails with a "duplicate key value violates unique constraint
        // IX_invoices_InvoiceNumber" error.
        //
        // Fix: take a Postgres advisory *transaction* lock keyed on the
        // month prefix before counting. The lock is scoped to the current
        // db transaction and is released automatically on commit/rollback,
        // so a second concurrent call for the same month simply waits here
        // until the first one has committed (and therefore sees the
        // up-to-date count) instead of racing it.
        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({prefix}))");

            var countThisMonth = await _context.Invoices
                .CountAsync(i => i.InvoiceNumber.StartsWith(prefix));

            return $"{prefix}{(countThisMonth + 1):D4}";
        }
    }
}