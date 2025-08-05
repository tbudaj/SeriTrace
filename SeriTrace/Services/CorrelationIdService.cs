using Microsoft.AspNetCore.Http;

namespace SeriTrace.Services
{
    /// <summary>
    /// Provides access to CorrelationId and request start time within the HTTP context.
    /// Allows retrieving these values from HttpContext.Items during a single request.
    /// </summary>
    public interface ICorrelationIdService
    {
        /// <summary>
        /// Retrieves the CorrelationId from HttpContext.Items.
        /// </summary>
        /// <returns>The CorrelationId, or null if not available.</returns>
        string? GetCorrelationId();

        /// <summary>
        /// Retrieves the request start timestamp from HttpContext.Items.
        /// </summary>
        /// <returns>The request start time, or null if not available.</returns>
        DateTime? GetRequestStartTime();
    }

    /// <summary>
    /// Implementation of the CorrelationId service using IHttpContextAccessor.
    /// Allows safe retrieval of CorrelationId and request start time within the request scope.
    /// </summary>
    public class CorrelationIdService : ICorrelationIdService
    {
        // Keys must match those used in CorrelationIdMiddleware
        private const string CorrelationIdContextKey = "CorrelationId";
        private const string RequestStartTimeKey = "RequestStartTime";

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates a new instance of CorrelationIdService.
        /// </summary>
        /// <param name="httpContextAccessor">Access to the current HTTP context.</param>
        public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Safely retrieves the CorrelationId from HttpContext.Items.
        /// </summary>
        public string? GetCorrelationId()
        {
            return _httpContextAccessor.HttpContext?.Items[CorrelationIdContextKey] as string;
        }

        /// <summary>
        /// Safely retrieves the request start timestamp from HttpContext.Items.
        /// </summary>
        public DateTime? GetRequestStartTime()
        {
            return _httpContextAccessor.HttpContext?.Items[RequestStartTimeKey] as DateTime?;
        }
    }
}
