namespace devanewbot.Data.Models;

using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Role : IdentityRole<Guid>, IEntityTypeConfiguration<Role>, ICreateable
{
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
    }
}
