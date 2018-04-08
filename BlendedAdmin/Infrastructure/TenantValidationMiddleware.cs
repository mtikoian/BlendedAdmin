﻿using BlendedAdmin.DomainModel.Tenants;
using BlendedAdmin.Infrastructure;
using BlendedAdmin.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BlendedAdmin
{
    public class TenantValidationMiddleware
    {
        private readonly RequestDelegate _next;

        private IServiceScopeFactory _serviceScopeFactory;
        private IOptions<HostingOptions> _hostingOptions;
        private IMemoryCache _memoryCache;
        private IUrlService _urlService;
        private ILogger<TenantValidationMiddleware> _logger { get; }

        public TenantValidationMiddleware(
            RequestDelegate next,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<HostingOptions> hostingOptions,
            IMemoryCache memoryCache,
            IUrlService urlService,
            ILogger<TenantValidationMiddleware> logger)
        {
            _next = next;

            _serviceScopeFactory = serviceScopeFactory;
            _hostingOptions = hostingOptions;
            _memoryCache = memoryCache;
            _urlService = urlService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_hostingOptions?.Value?.MultiTenants == false)
            {
                await this._next(context);
                return;
            }

            string tenantId = _urlService.GetTenant();
            if (tenantId == "x")
            {
                await this._next(context);
                return;
            }

            string tenantCacheKey = "tenant_" + tenantId;
            Tenant tenant = null;
            if (!_memoryCache.TryGetValue(tenantCacheKey, out tenant))
            {
                tenant = await GetTenant(tenantId);
                if (tenant == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Page not found");
                    return;
                }

                var cacheOptions = new MemoryCacheEntryOptions()
                        //.SetSlidingExpiration(TimeSpan.FromSeconds(60))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));

                _memoryCache.Set(tenantCacheKey, tenant, cacheOptions);
            }
            await this._next(context);
        }

        public async Task<Tenant> GetTenant(string tenantId)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var tenantRepository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
                var tenant = await tenantRepository.Get(tenantId);
                return tenant;
            }
        }
    }

    public static class TenantValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantValidationMiddleware>();
        }
    }
}
