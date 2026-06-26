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
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Razorpay");
        }

        // ─────────────────────────────────────────────────────────────
        // 1. List active plans
        // ─────────────────────────────────────────────────────────────
        public async Task<List<CreditPlanResponseDto>> GetActivePlansAsync()
        {
            return await _context.CreditPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .Select(p => new CreditPlanResponseDto
                {
                    PlanId = p.PlanId,
                    PlanName = p.PlanName,
                    Credits = p.Credits,
                    Price = p.Price,
                    ValidityMonths = p.ValidityMonths,
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

            int amountPaise = (int)(plan.Price * 100);   // ₹ → paise

            // ── Call Razorpay Orders API ──────────────────────────────
            string razorpayOrderId;
            try
            {
                razorpayOrderId = await CallRazorpayCreateOrderAsync(amountPaise, plan.PlanId);
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
                GstAmountPaise = 0,
                TotalAmountPaise = amountPaise,
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
                AmountPaise = amountPaise,
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
            // Load the pending transaction that belongs to this employer
            var txn = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.TransactionId == request.TransactionId &&
                    t.EmployerId == employerId &&
                    t.PaymentStatus == "Pending");

            if (txn == null)
                return Fail<VerifyPlanPaymentResponseDto>("Transaction not found or already processed.");

            // ── Verify HMAC-SHA256 signature ──────────────────────────
            if (!VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
            {
                txn.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Invalid Razorpay signature — TxnId={TxnId} OrderId={OrderId}",
                    txn.TransactionId, request.RazorpayOrderId);

                return Fail<VerifyPlanPaymentResponseDto>("Payment verification failed. Invalid signature.");
            }

            // ── Double-spend guard ────────────────────────────────────
            bool alreadyUsed = await _context.PaymentTransactions
                .AnyAsync(t =>
                    t.RazorpayPaymentId == request.RazorpayPaymentId &&
                    t.PaymentStatus == "Completed");

            if (alreadyUsed)
                return Fail<VerifyPlanPaymentResponseDto>("This payment has already been applied.");

            // ── Look up the plan (PackType stores PlanName) ───────────
            var plan = await _context.CreditPlans
                .FirstOrDefaultAsync(p => p.PlanName == txn.PackType && p.IsActive);

            if (plan == null)
            {
                _logger.LogError(
                    "Plan '{PlanName}' not found during verify for TxnId={TxnId}",
                    txn.PackType, txn.TransactionId);
                return Fail<VerifyPlanPaymentResponseDto>("Associated plan not found. Contact support.");
            }

            // ── Mark transaction completed ────────────────────────────
            txn.RazorpayOrderId = request.RazorpayOrderId;
            txn.RazorpayPaymentId = request.RazorpayPaymentId;
            txn.PaymentStatus = "Completed";
            txn.CreditsAddedAt = DateTime.UtcNow;

            // ── Upsert credit wallet ───────────────────────────────────
            var wallet = await _context.CreditWallets
                .FirstOrDefaultAsync(w => w.EmployerId == employerId);

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
                    ? wallet.PackExpiresAt.Value
                    : DateTime.UtcNow;

                wallet.PackExpiresAt = baseDate.AddMonths(plan.ValidityMonths);
                wallet.UpdatedAt = DateTime.UtcNow;
            }

            // ── Record plan purchase ───────────────────────────────────
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
                AssignedBy = employerId
            };
            _context.EmployerPlanPurchase.Add(purchase);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Plan '{Plan}' purchased by EmployerId={EmpId}. Credits added: {Credits}",
                plan.PlanName, employerId, plan.Credits);

            return new VerifyPlanPaymentResponseDto
            {
                Success = true,
                Message = $"Payment successful! {plan.Credits} credits added to your wallet.",
                NewCreditBalance = wallet.CreditBalance,
                PurchaseId = purchase.EmployerCreditPlanId
            };
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
                AssignedBy = employerId
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
    }
}