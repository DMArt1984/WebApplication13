﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FactPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FactPortal.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        private readonly IConfiguration Configuration;
        private readonly HttpContext _httpContext;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration, IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContext = httpContextAccessor?.HttpContext;
            Configuration = configuration;
        }

        public DbSet<Company> Company { get; set; } // Компания (и база данных)

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Динамическая строка подключения в этой версии пока не нужна!

                //if (!optionsBuilder.IsConfigured)
                //{
                //    var ConnectionName_Header = _httpContext.Request.Headers["db"].ToString();
                //    var ConnectionName_Cookie = _httpContext.Request.Cookies["connname"];

                //    if (String.IsNullOrWhiteSpace(ConnectionName_Header))
                //    {
                //        ConnectionName_Header = ConnectionName_Cookie;
                //        if (String.IsNullOrWhiteSpace(ConnectionName_Cookie))
                //            ConnectionName_Header = "default";
                //    }

                //    var ConnectionString = Configuration.GetConnectionString(ConnectionName_Header);
                //    if (String.IsNullOrWhiteSpace(ConnectionString))
                //        ConnectionString = Configuration.GetConnectionString("default");

                //    optionsBuilder.UseNpgsql(ConnectionString);
                //}

            // Будет так:
            string ConnectionString = Configuration.GetConnectionString("default");

                optionsBuilder.UseNpgsql(ConnectionString);
                //_httpContext.Response.Cookies.Append("optionBuilder", "", new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(1) });
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Company>().ToTable("Company");

            builder.HasDefaultSchema("Identity");
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "myUser");
                //entity.Ignore(x => x.NormalizedUserName); // нельзя удалять этот столбец
                entity.Ignore(c => c.AccessFailedCount);
                entity.Ignore(c => c.LockoutEnabled);
                entity.Ignore(c => c.LockoutEnd);
                entity.Ignore(c => c.TwoFactorEnabled);
            });
            builder.Entity<ApplicationUser>()
                    .Property(p => p.UserName)
                    .HasColumnName("Login");
            builder.Entity<ApplicationUser>()
                    .Property(p => p.NormalizedUserName)
                    .HasColumnName("NormalizedLogin");

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "myRole");
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("myUserRoles");
            });
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("myUserClaims");
            });

            builder.Ignore<IdentityUserLogin<string>>();
            //builder.Entity<IdentityUserLogin<string>>(entity =>
            //{
            //    entity.ToTable("myUserLogins");
            //});

            //builder.Ignore<IdentityRoleClaim<string>>();
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("myRoleClaims");
            });

            builder.Ignore<IdentityUserToken<string>>();
            //builder.Entity<IdentityUserToken<string>>(entity =>
            //{
            //    entity.ToTable("myUserTokens");
            //});
        }
    }
}
