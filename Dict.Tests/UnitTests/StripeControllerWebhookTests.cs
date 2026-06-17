using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dict.Controllers;
using Dict.Service.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Stripe;
using Xunit;

namespace Dict.Tests.UnitTests
{
    public class StripeControllerWebhookTests
    {
        private readonly Mock<IStripeService> _stripeServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<StripeController>> _loggerMock;

        public StripeControllerWebhookTests()
        {
            _stripeServiceMock = new Mock<IStripeService>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<StripeController>>();
        }

        [Fact]
        public async Task Webhook_WhenStripeSignatureHeaderIsMissing_ShouldReturnBadRequest()
        {
            var controller = CreateController("{\"type\":\"checkout.session.completed\"}");

            var result = await controller.Webhook();

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Missing Stripe-Signature header");
            _stripeServiceMock.Verify(x => x.HandleWebhookAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Webhook_WhenStripeSignatureIsInvalid_ShouldReturnBadRequest()
        {
            const string payload = "{\"id\":\"evt_invalid\"}";
            const string signature = "t=1,v1=invalid";

            _stripeServiceMock
                .Setup(x => x.HandleWebhookAsync(payload, signature))
                .ThrowsAsync(new StripeException("invalid signature"));

            var controller = CreateController(payload, signature);

            var result = await controller.Webhook();

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Invalid signature");
            _stripeServiceMock.Verify(x => x.HandleWebhookAsync(payload, signature), Times.Once);
        }

        [Fact]
        public async Task Webhook_WhenSignatureIsPresentAndHandlerSucceeds_ShouldReturnOk()
        {
            const string payload = "{\"id\":\"evt_valid\"}";
            const string signature = "t=1,v1=valid";

            _stripeServiceMock
                .Setup(x => x.HandleWebhookAsync(payload, signature))
                .Returns(Task.CompletedTask);

            var controller = CreateController(payload, signature);

            var result = await controller.Webhook();

            result.Should().BeOfType<OkResult>();
            _stripeServiceMock.Verify(x => x.HandleWebhookAsync(payload, signature), Times.Once);
        }

        private StripeController CreateController(string payload, string? stripeSignature = null)
        {
            var controller = new StripeController(
                _stripeServiceMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            httpContext.Request.ContentLength = Encoding.UTF8.GetByteCount(payload);

            if (!string.IsNullOrEmpty(stripeSignature))
            {
                httpContext.Request.Headers["Stripe-Signature"] = stripeSignature;
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }
    }
}
