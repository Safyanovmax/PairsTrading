using AppCore.Models.Whitebit;
using AppCore.Models.Whitebit.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IWhitebitAuthenticator
    {
        HttpRequestMessage GetAuthenticatedRequest(BaseWhitebitRequest data);
    }
}