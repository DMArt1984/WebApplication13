using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Data
{
    public class InvitationTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public InvitationTokenProvider(IDataProtectionProvider dataProtectionProvider, IOptions<InvitationTokenProviderOptions> options, ILogger<DataProtectorTokenProvider<TUser>> logger) : base(dataProtectionProvider, options, logger)
        {
            
        }
    }

    public class InvitationTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        
    }
}
