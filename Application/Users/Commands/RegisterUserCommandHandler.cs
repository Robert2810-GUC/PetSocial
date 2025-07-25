using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Application.Users.Commands
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, string>
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _config;
        private readonly IImageService _imageService;

        public RegisterUserCommandHandler(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IConfiguration config,
            IImageService imageService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _config = config;

            _imageService = imageService;
        }

        public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            string imageUrl = null;
            string imagePublicId = null;

            try
            {
                // 1. Upload image first (if image provided)
                if (request.Image != null)
                {
                    var uploadResult = await _imageService.UploadImageAsync(request.Image);
                    imageUrl = uploadResult.Url;
                    imagePublicId = uploadResult.PublicId;
                }

                // 2. Create Identity User
                var identityUser = new IdentityUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    PhoneNumber = request.Phone
                };
               var result = await _userManager.CreateAsync(identityUser, request.Password);
                if (!result.Succeeded)
                    throw new Exception("Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

                // 3. Add User profile
                var userProfile = new User
                {
                    IdentityId = identityUser.Id,
                    Name = request.Name,
                    Phone = request.Phone,
                    Email = request.Email,
                    Gender = request.Gender,
                    DOB = request.DOB,
                    ImagePath = imageUrl ?? "/images/default-user.jpg"
                };
                _dbContext.Users.Add(userProfile);
                await _dbContext.SaveChangesAsync(cancellationToken);


                // 4. Generate JWT
                var token = GenerateJwtToken(identityUser);
                await transaction.CommitAsync(cancellationToken);

                return token;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                // Delete uploaded image if user/profile creation failed after upload
                if (imagePublicId != null)
                {
                    await _imageService.DeleteImageAsync(imagePublicId);
                }
                throw;
            }
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
