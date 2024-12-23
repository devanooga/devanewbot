namespace devanewbot.Entities;

using Microsoft.EntityFrameworkCore;

public class DevanewbotContext(DbContextOptions<DevanewbotContext> options) : DbContext(options)
{
    public required DbSet<ChannelBan> ChannelBans { get; set; }
}