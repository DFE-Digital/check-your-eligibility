using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace CheckYourEligibility.WebApp.Controllers
{
    public class BaseController : Controller
    {
        private readonly IAudit _audit;

        public BaseController(IAudit audit)
        {
            _audit = audit;
        }

        /// <summary>
        /// get and create audit item
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected async Task<string> AuditAdd(Domain.Enums.AuditType type, string id)
        {
            var data = AuditDataGet(type, id);
            if (data == null) { return string.Empty; }
            return await _audit.AuditAdd(data);
        }

        /// <summary>
        /// get audit item
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected AuditData? AuditDataGet(Domain.Enums.AuditType type, string id)
        {
            if (HttpContext != null)
            {
                var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
                var host = HttpContext.Request.Host;
                var path = HttpContext.Request.Path;
                var method = HttpContext.Request.Method;
                var auth = HttpContext.Request.Headers.Authorization;
                return new AuditData { Type = type, typeId = id, url = $"{host}{path}", method = method, source = remoteIpAddress.ToString(), authentication = auth };
            }
            return new AuditData { Type = type, typeId = id, url = "Unknown", method = "Unknown", source = "Unknown", authentication = "Unknown" };
        }
    }
 }
