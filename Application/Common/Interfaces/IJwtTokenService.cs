using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    // Application.Common.Interfaces
    public interface IJwtTokenService
    {
        string GenerateToken(string userId, string email, string role, string userName = null);

    }


}
