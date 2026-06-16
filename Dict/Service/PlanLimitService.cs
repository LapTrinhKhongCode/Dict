using Dict.Data;
using Dict.Models;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    /// <summary>
    /// Kiểm tra quota và feature limit theo PersonalTier của user.
    /// FREE: OCR 20 lần/tháng, file 10MB
    /// PREMIUM: OCR không giới hạn, file 500MB
    /// </summary>
    public class PlanLimitService
    {
        private readonly ApplicationDbContext _db;

        public PlanLimitService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── Constants ────────────────────────────────────────────────────
        public const long FREE_MAX_FILE_BYTES    = 10L * 1024 * 1024;   // 10 MB
        public const long PREMIUM_MAX_FILE_BYTES = 500L * 1024 * 1024; // 500 MB
        public const int  FREE_OCR_PER_MONTH     = 20;

        // ── File size check (theo spec: check OwnerType của workspace) ──────
        public async Task<(bool allowed, string? error)> CheckFileSizeAsync(int userId, long fileSizeBytes, int? workspaceId = null)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found");

            long maxBytes;
            string tierLabel;

            if (workspaceId.HasValue)
            {
                // Lấy workspace để check OwnerType
                var ws = await _db.Workspaces
                    .Include(w => w.Organization)
                    .FirstOrDefaultAsync(w => w.Id == workspaceId.Value);

                if (ws?.OwnerType == "ORGANIZATION" && ws.Organization != null)
                {
                    // B2B: dùng Org.Plan
                    bool orgHasPremium = ws.Organization.OrgPlan == OrgPlan.TEAM || ws.Organization.OrgPlan == OrgPlan.ENTERPRISE;
                    maxBytes = orgHasPremium ? PREMIUM_MAX_FILE_BYTES : FREE_MAX_FILE_BYTES;
                    tierLabel = $"Org {ws.Organization.OrgPlan}";
                }
                else
                {
                    // PERSONAL: dùng PersonalTier của user
                    bool isPremium = user.PersonalTier == "PREMIUM" &&
                                     (user.PremiumExpiresAt == null || user.PremiumExpiresAt > DateTime.UtcNow);
                    maxBytes = isPremium ? PREMIUM_MAX_FILE_BYTES : FREE_MAX_FILE_BYTES;
                    tierLabel = isPremium ? "Premium" : "Free";
                }
            }
            else
            {
                // Không có workspace context → dùng PersonalTier
                bool isPremium = user.PersonalTier == "PREMIUM" &&
                                 (user.PremiumExpiresAt == null || user.PremiumExpiresAt > DateTime.UtcNow);
                maxBytes = isPremium ? PREMIUM_MAX_FILE_BYTES : FREE_MAX_FILE_BYTES;
                tierLabel = isPremium ? "Premium" : "Free";
            }

            if (fileSizeBytes > maxBytes)
            {
                var maxMb = maxBytes / 1024 / 1024;
                return (false, $"File quá lớn. Gói {tierLabel} chỉ hỗ trợ tối đa {maxMb}MB. " +
                               "Nâng cấp để upload file lớn hơn.");
            }
            return (true, null);
        }

        // ── OCR quota check ───────────────────────────────────────────────
        public async Task<(bool allowed, string? error)> CheckOcrQuotaAsync(int userId, int? workspaceId = null)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found");

            // Nếu có workspace context → check Org.Plan (B2B)
            if (workspaceId.HasValue)
            {
                var ws = await _db.Workspaces
                    .Include(w => w.Organization)
                    .FirstOrDefaultAsync(w => w.Id == workspaceId.Value);

                if (ws?.OwnerType == "ORGANIZATION" && ws.Organization != null)
                {
                    bool orgUnlimited = ws.Organization.OrgPlan == OrgPlan.TEAM || ws.Organization.OrgPlan == OrgPlan.ENTERPRISE;
                    if (orgUnlimited) return (true, null); // Org TEAM/ENTERPRISE: unlimited

                    // Org FREE: shared pool 20/tháng cho cả org
                    var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    var orgId = ws.Organization.Id;
                    var orgMemberIds = await _db.OrganizationMembers
                        .Where(m => m.OrganizationId == orgId)
                        .Select(m => (int)m.UserId).ToListAsync();
                    var orgOcrCount = await _db.OcrJobs
                        .CountAsync(j => j.UserId != null && orgMemberIds.Contains(j.UserId.Value) && j.CreatedAt >= startOfMonth);
                    if (orgOcrCount >= FREE_OCR_PER_MONTH)
                        return (false, $"Tổ chức đã dùng hết {FREE_OCR_PER_MONTH} lần OCR tháng này (Org Free). Nâng cấp lên Org Team để dùng không giới hạn.");
                    return (true, null);
                }
            }

            // PERSONAL workspace hoặc không có context → check cá nhân
            bool isPremium = user.PersonalTier == "PREMIUM" &&
                             (user.PremiumExpiresAt == null || user.PremiumExpiresAt > DateTime.UtcNow);
            if (isPremium) return (true, null);

            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var ocrCount = await _db.OcrJobs
                .CountAsync(j => j.UserId == userId && j.CreatedAt >= start);

            if (ocrCount >= FREE_OCR_PER_MONTH)
                return (false, $"Bạn đã dùng hết {FREE_OCR_PER_MONTH} lần OCR tháng này (gói Free). Nâng cấp Premium để OCR không giới hạn.");

            return (true, null);
        }

        // ── Current usage info ────────────────────────────────────────────
        public async Task<object> GetUsageInfoAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return new { };

            bool isPremium = user.PersonalTier == "PREMIUM" &&
                             (user.PremiumExpiresAt == null || user.PremiumExpiresAt > DateTime.UtcNow);

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var ocrCount = await _db.OcrJobs
                .CountAsync(j => j.UserId == userId && j.CreatedAt >= startOfMonth);

            return new
            {
                tier = user.PersonalTier,
                isPremium,
                premiumExpiresAt = user.PremiumExpiresAt,
                ocr = new
                {
                    used = ocrCount,
                    limit = isPremium ? (int?)null : FREE_OCR_PER_MONTH,
                    unlimited = isPremium
                },
                fileSize = new
                {
                    maxMb = (isPremium ? PREMIUM_MAX_FILE_BYTES : FREE_MAX_FILE_BYTES) / 1024 / 1024
                }
            };
        }
    }
}
