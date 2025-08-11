using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models;

public class TokenResult
{
    public string Token { get; set; }
    public bool IsPetRegistered { get; set; }
    public bool IsProfileUpdated { get; set; }
    public string UserName { get; set; }
}
