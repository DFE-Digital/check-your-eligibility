using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;
using CheckYourEligibility.SystemTests.Utilities.Models;
using Microsoft.Playwright;

namespace CheckYourEligibility.SystemTests.Utilities
{
    public static class CommonMethods 
    {         

        public static string ExtractStringAfterLastSlash(string url)
        {
            // Find the last occurrence of '/'
            int lastSlashIndex = url.LastIndexOf('/');

            // Check if '/' is found in the URL
            if (lastSlashIndex >= 0 && lastSlashIndex < url.Length - 1)
            {
                // Extract the string after the last '/'
                return url.Substring(lastSlashIndex + 1);
            }

            // Return an empty string or handle the case where no '/' is found
            return string.Empty;
        }
    }
}
