﻿using Fido2IdentityServer.Identity.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fido2IdentityServer.Identity
{
    public class AuthenticationContext : IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public AuthenticationContext() : base() { }
        public AuthenticationContext(DbContextOptions<AuthenticationContext> options) : base(options)
        {
        }

        public DbSet<FidoLogin> FidoLogins { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<PaymentAuthorization> PaymentAuthorizations { get; set; }
        public DbSet<UserCertificate> UserCertificates { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source=C:\Users\Mladjan\source\repos\Fido2\Fido2.Identity\users.db");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            var schemaName = "AUTHENTICATION";
            builder.HasDefaultSchema(schemaName);

            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<UserRole>().ToTable("UserRoles");
            builder.Entity<UserClaim>().ToTable("UserClaims");
            builder.Entity<UserLogin>().ToTable("UserLogins");
            builder.Entity<RoleClaim>().ToTable("RoleClaims");
            builder.Entity<UserToken>().ToTable("UserTokens");
            builder.Entity<FidoLogin>().ToTable("FidoLogins");
            builder.Entity<PaymentAuthorization>().ToTable("PaymentAuthorizations");
            builder.Entity<UserCertificate>().ToTable("UserCertificates");
      //      builder.Entity<FidoLogin>().OwnsOne(x => x.CredentialPublicKey);
            builder.Entity<FidoLogin>().HasOne(x => x.User).WithMany(x => x.FidoLogins).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PaymentAuthorization>().HasOne(x => x.Payment).WithMany(x => x.PaymentAuthorizations).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
