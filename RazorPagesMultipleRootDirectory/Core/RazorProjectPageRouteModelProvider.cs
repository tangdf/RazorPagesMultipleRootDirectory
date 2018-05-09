using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RazorPagesMultipleRootDirectory.Core
{
    public class RazorProjectPageRouteModelProvider : IPageRouteModelProvider
    {
        private readonly RazorProject _project;
        private readonly RazorPagesOptions _pagesOptions;
        private readonly PageRouteModelFactory _routeModelFactory;
        private readonly ILogger<RazorProjectPageRouteModelProvider> _logger;

        public RazorProjectPageRouteModelProvider(
            RazorProject razorProject,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _project = razorProject;
            _pagesOptions = pagesOptionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<RazorProjectPageRouteModelProvider>();
            _routeModelFactory = new PageRouteModelFactory(_pagesOptions, _logger);
        }

        /// <remarks>
        /// Ordered to execute after <see cref="CompiledPageRouteModelProvider"/>.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(PageRouteModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(PageRouteModelProviderContext context)
        {
            // When RootDirectory and AreaRootDirectory overlap, e.g. RootDirectory = /, AreaRootDirectoryy = /Areas;
            // we need to ensure that the page is only route-able via the area route. By adding area routes first,
            // we'll ensure non area routes get skipped when it encounters an IsAlreadyRegistered check.

            if (_pagesOptions.AllowAreas)
            {
                AddAreaPageModels(context);
            }

            AddPageModels(context);
        }

        private void AddPageModels(PageRouteModelProviderContext context)
        {
            var normalizedAreaRootDirectory = Startup.OtherRootDirectory;
            if (!normalizedAreaRootDirectory.EndsWith("/", StringComparison.Ordinal))
            {
                normalizedAreaRootDirectory += "/";
            }

            foreach (var item in _project.EnumerateItems(Startup.OtherRootDirectory))
            {
                if (!IsRouteable(item))
                {
                    continue;
                }

                var relativePath = item.CombinedPath;
                if (context.RouteModels.Any(m => string.Equals(relativePath, m.RelativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    // A route for this file was already registered either by the CompiledPageRouteModel or as an area route.
                    // by this provider. Skip registering an additional entry.

                    // Note: We're comparing duplicates based on root-relative paths. This eliminates a page from being discovered
                    // by overlapping area and non-area routes where ViewEnginePath would be different.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                //if (_pagesOptions.AllowAreas && relativePath.StartsWith(normalizedAreaRootDirectory, StringComparison.OrdinalIgnoreCase))
                //{
                //    // Ignore Razor pages that are under the area root directory when AllowAreas is enabled. 
                //    // Conforming page paths will be added by AddAreaPageModels.
                //    //_logger.UnsupportedAreaPath(_pagesOptions, relativePath);
                //    continue;
                //}

                var routeModel = _routeModelFactory.CreateRouteModel(relativePath, routeTemplate);
                if (routeModel != null && context.RouteModels.Any(m => string.Equals(routeModel.ViewEnginePath, m.ViewEnginePath, StringComparison.OrdinalIgnoreCase))==false)
                {
                    context.RouteModels.Add(routeModel);
                }
            }
        }

        private void AddAreaPageModels(PageRouteModelProviderContext context)
        {
            foreach (var item in _project.EnumerateItems(_pagesOptions.AreaRootDirectory))
            {
                if (!IsRouteable(item))
                {
                    continue;
                }

                var relativePath = item.CombinedPath;
                if (context.RouteModels.Any(m => string.Equals(relativePath, m.RelativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    // A route for this file was already registered either by the CompiledPageRouteModel.
                    // Skip registering an additional entry.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                var routeModel = _routeModelFactory.CreateAreaRouteModel(relativePath, routeTemplate);
                if (routeModel != null)
                {
                    context.RouteModels.Add(routeModel);
                }
            }
        }

        private static bool IsRouteable(RazorProjectItem item)
        {
            // Pages like _ViewImports should not be routable.
            return !item.FileName.StartsWith("_", StringComparison.OrdinalIgnoreCase);
        }
    }
}
