using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CheckYourEligibility.WebApp.Controllers
{
    public class BaseController : Controller
    {
        private readonly IAudit _audit;

        public BaseController(IAudit audit)
        {
            _audit = audit;
        }

        protected async Task Audit(Domain.Enums.AuditType type, string id)
        {
            if (HttpContext != null)
            {
                var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
                var host = HttpContext.Request.Host;
                var path = HttpContext.Request.Path;
                var method = HttpContext.Request.Method;
                var auth = HttpContext.Request.Headers.Authorization;

                await _audit.AuditAdd(new AuditData { Type = type, typeId = id, url = $"{host}{path}", method = method, source = remoteIpAddress.ToString(), authentication = auth });
            }
        }

    }
 }
