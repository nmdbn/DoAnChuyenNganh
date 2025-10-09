using Microsoft.AspNetCore.SignalR;

namespace DoAnChuyenNganh.Providers
{
    /// <summary>
    /// Custom SignalR User ID Provider that reads user ID from session
    /// This is required for SignalR to identify users for targeted message delivery
    /// </summary>
    public class SessionUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Get the HTTP context from the SignalR connection
            var httpContext = connection.GetHttpContext();
            
            if (httpContext == null)
                return null;

            // Get the user ID from session
            var userId = httpContext.Session.GetInt32("UserId");
            
            // Return the user ID as a string (SignalR requires string)
            return userId?.ToString();
        }
    }
}

