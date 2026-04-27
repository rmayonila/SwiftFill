using System.Collections.Generic;
using SwiftFill.Models;

namespace SwiftFill.Models
{
    public class UserManagementViewModel
    {
        public ApplicationUser User { get; set; } = default!;
        public string Role { get; set; } = string.Empty;
        public List<string> Claims { get; set; } = new List<string>();
    }

    public class RegisterUserBindingModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Hub { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
