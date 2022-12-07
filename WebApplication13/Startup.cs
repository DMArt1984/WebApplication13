using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using FactPortal.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using FactPortal.Services;
using FactPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using System.IO;
using SmartBreadcrumbs.Extensions;
using System.Reflection;

namespace FactPortal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Это нам не нужно т.к. строка подключения у нас динамическая
            //services.AddDbContext<ApplicationDbContext>();
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseNpgsql(
            //        Configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<InvitationTokenProvider<ApplicationUser>>("Invitation")
                .AddEntityFrameworkStores<ApplicationDbContext>(); //;

            // Возможно когда-то пригодится
            //services.AddTransient<IEmailSender, EmailSender>();
            //services.Configure<AuthMessageSenderOptions>(this.Configuration);

            //Для работы UserStore в API
            //services.AddScoped<IBlogContextProvider, BlogContextProvider>();

            //============================================================================

            // Контекст данных для бизнес процессов
            // вариант 1
            //services.AddDbContext<BusinessContext>(options => options.UseNpgsql(Configuration.GetConnectionString("business")));

            // Контекст данных для бизнес процессов
            // вариант 2
            //services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddDbContext<BusinessContext>((serviceProvider, options) =>
            //{
            //    var httpContext = serviceProvider.GetService<IHttpContextAccessor>().HttpContext;
            //    var httpRequest = httpContext.Request;
            //    var connection = GetConnection(httpRequest);
            //    options.UseSqlServer(connection);
            //});

            // Контекст данных для бизнес процессов
            // вариант 3
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDbContext<BusinessContext>();

            services.AddDbContext<ApplicationDbContext>();

            //===================================================================================

            // Крошки
            services.AddBreadcrumbs(GetType().Assembly, options =>
            {
                options.TagName = "nav";
                options.TagClasses = "";
                options.OlClasses = "breadcrumb";
                options.LiClasses = "breadcrumb-item";
                options.ActiveLiClasses = "breadcrumb-item active";
                options.SeparatorElement = "<li class=\"separator\">/</li>";
            });

            //===================================================================================

            services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
                options.JsonSerializerOptions.WriteIndented = true;
            });
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddDirectoryBrowser(); // Статические файлы

            services.AddSignalR(); // Обновление данных без перезагрузки страницы

            // Ограничение длины запроса...
            services.Configure<IISServerOptions>(options =>
           {
               options.MaxRequestBodySize = 4294967295; // int.MaxValue;
           });

            // Возможно пригодится позже
            //services.AddSession(options =>
            //{
            //    options.IdleTimeout = TimeSpan.FromMinutes(30);
            //    options.Cookie.HttpOnly = true;
            //    options.Cookie.IsEssential = true;
            //});


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //env.EnvironmentName = "Production"; // test
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error?code={0}");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // обработка ошибок HTTP
            //app.UseStatusCodePages();
            //app.UseStatusCodePages("text/html", "<div style='font-size:200%'><strong>Error</strong>. Status code : {0}</div>");
            app.UseStatusCodePagesWithRedirects("/Home/Error?code={0}");

            //---
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // >>>>>>>>>>>>>>>>>>>> requestPath
            var fileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "Files/Images"));
            var requestPath = "/MyImages";

            // Enable displaying browser links.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = requestPath
            });

            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = fileProvider,
                RequestPath = requestPath
            });
            // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapHub<ChatHub>("/chat");
            });

        }
    }
}
