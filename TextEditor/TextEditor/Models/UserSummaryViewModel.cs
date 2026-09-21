namespace TextEditor.Models
{
  
    /// Read-only snapshot of a user shown in the Admin panel table.
    public class UserSummaryViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public int DocumentCount { get; init; }
        public IList<string> Roles { get; init; } = new List<string>();
        public bool IsLockedOut { get; init; }

        
        public string RolesBadge =>
            Roles.Count > 0 ? string.Join(", ", Roles) : "User";

        public string CreatedAtFormatted =>
            CreatedAt.ToString("yyyy-MM-dd");

        public string LockStatus =>
            IsLockedOut ? "Locked" : "Active";

        public string LockStatusBadgeClass =>
            IsLockedOut ? "badge bg-danger" : "badge bg-success";
    }

   
    /// Wraps the list for the Admin/Index view and carries any flash messages.
  
    public class AdminUsersViewModel
    {
        public IReadOnlyList<UserSummaryViewModel> Users { get; init; }
            = new List<UserSummaryViewModel>();

        public string? StatusMessage { get; set; }
    }
}
