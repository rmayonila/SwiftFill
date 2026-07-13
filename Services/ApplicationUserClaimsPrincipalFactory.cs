using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SwiftFill.Models;
using System.Security.Claims;

namespace SwiftFill.Services
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
            _roleManager = roleManager;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var roles = await UserManager.GetRolesAsync(user);

            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var claim in roleClaims)
                    {
                        // To avoid duplicates if the claim is already on the user identity
                        if (!identity.HasClaim(claim.Type, claim.Value))
                        {
                            identity.AddClaim(claim);
                        }
                    }
                }
            }

            return identity;
        }
    }
}
