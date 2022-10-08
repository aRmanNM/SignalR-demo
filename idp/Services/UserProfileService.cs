using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using idp.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace idp.Services
{
    public class UserProfileService : IProfileService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserClaimsPrincipalFactory<User> _claimsFactory;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            UserManager<User> userManager,
            IUserClaimsPrincipalFactory<User> claimsFactory,
            ILogger<UserProfileService> logger)
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _logger = logger;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subject = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(subject);
            var claimsPrincipal = await _claimsFactory.CreateAsync(user);
            var claimsList = claimsPrincipal.Claims.ToList();

            claimsList = claimsList
                .Where(c => context.RequestedClaimTypes
                .Contains(c.Type))
                .ToList();

            _logger.LogInformation("Default claims: {0}",
                JsonSerializer.Serialize(claimsList));

            claimsList.Add(new Claim(JwtClaimTypes.Name, user.UserName));

            var userRoles = await _userManager.GetRolesAsync(user);

            claimsList.Add(new Claim(JwtClaimTypes.Role, userRoles.First()));

            context.IssuedClaims = claimsList;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var subject = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(subject);
            context.IsActive = user != null;
        }
    }
}