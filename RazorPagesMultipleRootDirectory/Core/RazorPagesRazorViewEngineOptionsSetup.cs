using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

namespace RazorPagesMultipleRootDirectory.Core
{
    public class RazorPagesRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
    {
        public void Configure(RazorViewEngineOptions options)
        {
            var defaultPageSearchPath = CombinePath(Startup.OtherRootDirectory, "{1}/{0}" + RazorViewEngine.ViewExtension);
            options.PageViewLocationFormats.Add(defaultPageSearchPath);
        }

        private static string CombinePath(string path1, string path2)
        {
            if (path1.EndsWith("/", StringComparison.Ordinal) || path2.StartsWith("/", StringComparison.Ordinal))
            {
                return path1 + path2;
            }
            else if (path1.EndsWith("/", StringComparison.Ordinal) && path2.StartsWith("/", StringComparison.Ordinal))
            {
                return path1 + path2.Substring(1);
            }

            return path1 + "/" + path2;
        }
    }
}
