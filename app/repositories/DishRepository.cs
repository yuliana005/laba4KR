using CateringLab.Data;
using CateringLab.Models;
using Microsoft.EntityFrameworkCore;
namespace CateringLab.Repositories;
public class DishRepository(CateringDbContext db) : IDishRepository
{
    private static void Validate(string name, string category, long price)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150) throw new ArgumentException("Некорректное название.");
        if (string.IsNullOrWhiteSpace(category) || category.Trim().Length > 80) throw new ArgumentException("Некорректная категория.");
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
    }
    public Task<List<Dish>> GetAllAsync() => db.Dishes.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    public Task<Dish?> GetByIdAsync(int id) => db.Dishes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    public async Task<Dish> AddAsync(string name, string category, long priceKopecks)
    {
        Validate(name, category, priceKopecks);
        var dish = new Dish { Name = name.Trim(), Category = category.Trim(), PriceKopecks = priceKopecks };
        db.Dishes.Add(dish);
        await db.SaveChangesAsync();
        return dish;
    }
    public async Task<bool> UpdateAsync(int id, string name, string category, long priceKopecks)
    {
        Validate(name, category, priceKopecks);
        var dish = await db.Dishes.FindAsync(id);
        if (dish is null) return false;
        dish.Name = name.Trim(); dish.Category = category.Trim(); dish.PriceKopecks = priceKopecks;
        await db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var dish = await db.Dishes.FindAsync(id);
        if (dish is null) return false;
        if (await db.OrderItems.AnyAsync(x => x.DishId == id)) throw new InvalidOperationException("Блюдо используется в заказах.");
        db.Dishes.Remove(dish);
        await db.SaveChangesAsync();
        return true;
    }
}
