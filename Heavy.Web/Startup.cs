﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Heavy.Web.Data;
using Heavy.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Heavy.Web.Models;
using Heavy.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Heavy.Web.Filters;

namespace Heavy.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser,IdentityRole>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 1;
            }).AddDefaultUI(UIFramework.Bootstrap4)
              .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddDbContext<HeavyContext>(options =>
                {
                    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                });

            services.AddScoped<IAlbumService, AlbumEfService>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("仅限管理员", policy => policy.RequireRole("Administrators"));
                options.AddPolicy("编辑专辑", policy => policy.RequireClaim("Edit Albums"));
                options.AddPolicy("编辑专辑1", policy => policy.RequireAssertion(context =>
                {
                    return context.User.HasClaim(x => x.Type == "Edit Albums");
                }));
                options.AddPolicy("编辑专辑2", policy => policy.AddRequirements
                (//new EmailRequirement("@126.com"),
                new QualifiedUserRequirement()));
            });

            services.AddSingleton<IAuthorizationHandler, EmailHandler>();
            services.AddSingleton<IAuthorizationHandler, CanEditAlbumHandler>();
            services.AddSingleton<IAuthorizationHandler, AdministratorsHandler>();

            services.AddAntiforgery(options =>
            {
                options.FormFieldName = "AntiforgeryFieldname";
                options.HeaderName = "X-CSRF-TOKEN-HEADERNAME";
                options.SuppressXFrameOptionsHeader = false;
            });
            services.AddMvc(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                //options.Filters.Add(typeof(LogAsyncResourceFilter));
                options.Filters.Add<LogResourceFilter>();

                options.CacheProfiles.Add("Default", new CacheProfile
                {
                    Duration = 60
                });
            });

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = "localhost";
                options.InstanceName = "redis-for-albums";
            });

            services.AddResponseCompression();

            // add-migration InitialData -Context HeavyContext
            // update-database -Context HeavyContext
            // add-migration AddClaims -Context ApplicationDbContext
            // update-database -Context ApplicationDbContext
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseResponseCompression();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
