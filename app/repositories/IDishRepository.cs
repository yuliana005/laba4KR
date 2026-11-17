using CateringLab.Models;
namespace CateringLab.Repositories;
public interface IDishRepository
{
    Task<Dish> AddAsync(string name, string category, long priceKopecks);
    Task<List<Dish>> GetAllAsync();
    Task<Dish?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, string name, string category, long priceKopecks);
    Task<bool> DeleteAsync(int id);
}
