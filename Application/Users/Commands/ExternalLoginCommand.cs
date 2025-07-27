using Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Users.Commands
{
    public class ExternalLoginCommand : IRequest<ApiResponse<TokenResult>>
    {
        public string Provider { get; set; }
        public string IdToken { get; set; }
    }
}
