using CateringLab.Models;
using Microsoft.EntityFrameworkCore;
namespace CateringLab.Data;
public class CateringDbContext : DbContext
{
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=catering.db");
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Dish>(e => {
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Category).IsRequired().HasMaxLength(80);
            e.ToTable(t => t.HasCheckConstraint("CK_Dish_Price", "PriceKopecks >= 0"));
        });
        model.Entity<Order>().Property(x => x.Status).HasConversion<string>();
        model.Entity<OrderItem>(e => {
            e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Dish).WithMany(x => x.OrderItems).HasForeignKey(x => x.DishId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => {
                t.HasCheckConstraint("CK_Item_Quantity", "Quantity > 0");
                t.HasCheckConstraint("CK_Item_Price", "UnitPriceKopecks >= 0");
            });
        });
    }
}
