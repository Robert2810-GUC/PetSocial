using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users.Commands;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Admin.Commands
{
    internal class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, ApiResponse<TokenResult>>
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AdminLoginCommandHandler(
            UserManager<IdentityUser> userManager,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<ApiResponse<TokenResult>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
        {
            if (request is null) return ApiResponse<TokenResult>.Fail("Wrong Call..", 400);
            if (string.IsNullOrWhiteSpace(request.Email))
                return ApiResponse<TokenResult>.Fail("Email is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<TokenResult>.Fail("Password is required.", 400);

            IdentityUser identityUser = null;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                identityUser = await _userManager.FindByEmailAsync(request.Email);               
            }
            
            if (identityUser == null )
                return ApiResponse<TokenResult>.Fail("User not found.", 404);


            // Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, request.Password);
            if (!isPasswordValid)
                return ApiResponse<TokenResult>.Fail("Invalid credentials.", 401);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var role = roles.FirstOrDefault() ?? "User";

            // Generate JWT
            var token = _jwtTokenService.GenerateToken(identityUser.Id, identityUser.Email ?? string.Empty, role, identityUser.UserName!);
            var tokenResult = new TokenResult
            {
                Token = token,
                IsPetRegistered = false,
                IsProfileUpdated = false,
                UserName = identityUser.UserName!
            };

            return ApiResponse<TokenResult>.Success(tokenResult, "Login successful!", 200);
        }
    }
}
