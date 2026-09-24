namespace devanewbot.Data.Models;

using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class User : IdentityUser<Guid>, IEntityTypeConfiguration<User>, ICreateable
{
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
    }
}
