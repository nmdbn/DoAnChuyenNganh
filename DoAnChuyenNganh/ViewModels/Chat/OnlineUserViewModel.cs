namespace DoAnChuyenNganh.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for tracking online users
    /// </summary>
    public class OnlineUserViewModel
    {
        public int UserId { get; set; }
        
        public string Username { get; set; } = null!;
        
        public string FirstName { get; set; } = null!;
        
        public string LastName { get; set; } = null!;
        
        public string? AvatarUrl { get; set; }
        
        public DateTime ConnectedAt { get; set; }
        
        public DateTime LastActivityAt { get; set; }
        
        // Helper properties
        public string FullName => $"{FirstName} {LastName}";
        
        public string Initials
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                {
                    return $"{FirstName[0]}{LastName[0]}".ToUpper();
                }
                return Username.Substring(0, Math.Min(2, Username.Length)).ToUpper();
            }
        }
    }
    
    /// <summary>
    /// ViewModel for online users count
    /// </summary>
    public class OnlineUsersCountViewModel
    {
        public int TotalOnlineUsers { get; set; }
        
        public List<OnlineUserViewModel> OnlineUsers { get; set; } = new List<OnlineUserViewModel>();
    }
}

