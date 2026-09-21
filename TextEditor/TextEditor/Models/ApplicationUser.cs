using Microsoft.AspNetCore.Identity;

namespace TextEditor.Models
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string User = "User";

        /// All defined roles, in descending privilege order.
        public static readonly string[] All = { Admin, Moderator, User };
    }

    public class ApplicationUser : IdentityUser
    {
       
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

      
        public string? DisplayName { get; set; }

        public virtual ICollection<Doc> Docs { get; set; } = new List<Doc>();

        public string FriendlyName =>
            !string.IsNullOrWhiteSpace(DisplayName)
                ? DisplayName
                : (Email?.Split('@')[0] ?? UserName ?? Id);

        public string AccountAge
        {
            get
            {
                var span = DateTime.UtcNow - CreatedAt;
                return span.TotalDays >= 365
                    ? $"{(int)(span.TotalDays / 365)} year(s) ago"
                    : span.TotalDays >= 1
                        ? $"{(int)span.TotalDays} day(s) ago"
                        : "today";
            }
        }
    }

    public class RegularUser : ApplicationUser
    {
        public static RegularUser From(ApplicationUser user)
        {
            var u = new RegularUser();
            CopyFields(user, u);
            return u;
        }

        protected static void CopyFields(ApplicationUser src, ApplicationUser dst)
        {
            dst.Id = src.Id;
            dst.UserName = src.UserName;
            dst.NormalizedUserName = src.NormalizedUserName;
            dst.Email = src.Email;
            dst.NormalizedEmail = src.NormalizedEmail;
            dst.PasswordHash = src.PasswordHash;
            dst.SecurityStamp = src.SecurityStamp;
            dst.ConcurrencyStamp = src.ConcurrencyStamp;
            dst.CreatedAt = src.CreatedAt;
            dst.DisplayName = src.DisplayName;
        }

        public virtual bool CanEditDoc(Doc doc, string currentUserId) =>
            doc.UserId == currentUserId;

        public virtual bool CanDeleteDoc(Doc doc, string currentUserId) =>
            doc.UserId == currentUserId;

        public virtual bool CanViewAllUsers() => false;
    }

    public class ModeratorUser : RegularUser
    {
        public static new ModeratorUser From(ApplicationUser user)
        {
            var u = new ModeratorUser();
            CopyFields(user, u);
            return u;
        }

        public override bool CanEditDoc(Doc doc, string currentUserId) => true;
        public override bool CanDeleteDoc(Doc doc, string currentUserId) => true;
        public override bool CanViewAllUsers() => false;
    }

    public class AdminUser : ModeratorUser
    {
        public static new AdminUser From(ApplicationUser user)
        {
            var u = new AdminUser();
            CopyFields(user, u);
            return u;
        }

        public override bool CanViewAllUsers() => true;
        public bool CanManageRoles() => true;
        public bool CanLockAccount() => true;
    }
}
