using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

// Đặt file này trong thư mục /Tests của bạn
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Tạo một user giả đã được xác thực
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, "1"), // Giả lập GetAdminId()
            new Claim(ClaimTypes.Email, "testadmin@example.com"),
            new Claim(ClaimTypes.Role, "ADMIN") // ✨ Quan trọng: Cung cấp Role "ADMIN"
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        // Trả về thành công
        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}