using CateringLab.Data;
using CateringLab.Models;
using CateringLab.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CateringLab.Demo;
public static class DemoRunner { public static async Task RunAsync() {
Console.OutputEncoding = System.Text.Encoding.UTF8;
await using var db = new CateringDbContext();
await db.Database.MigrateAsync();
Console.WriteLine("Миграции применены. База: " + Path.GetFullPath("catering.db"));
// Откат демонстрационных данных позволяет безопасно повторять запуск.
await using var transaction = await db.Database.BeginTransactionAsync();
IDishRepository repo = new DishRepository(db);
var dish = await repo.AddAsync("Борщ", "Супы", 25000);
Console.WriteLine($"CREATE: #{dish.Id} {dish.Name}, {dish.PriceKopecks / 100m:F2} руб.");
if (await repo.GetByIdAsync(dish.Id) is null) throw new Exception("Ошибка чтения.");
Console.WriteLine($"READ: блюд в базе — {(await repo.GetAllAsync()).Count}");
await repo.UpdateAsync(dish.Id, "Борщ со сметаной", "Супы", 28000);
if ((await repo.GetByIdAsync(dish.Id))?.PriceKopecks != 28000) throw new Exception("Ошибка обновления.");
Console.WriteLine("UPDATE: цена изменена на 280,00 руб.");
await repo.DeleteAsync(dish.Id);
if (await repo.GetByIdAsync(dish.Id) is not null) throw new Exception("Ошибка удаления.");
Console.WriteLine("DELETE: блюдо удалено.");
var menuDish = await repo.AddAsync("Плов", "Горячие блюда", 35000);
var order = new Order { Items = [new OrderItem { DishId = menuDish.Id, Quantity = 2, UnitPriceKopecks = menuDish.PriceKopecks }] };
db.Orders.Add(order);
await db.SaveChangesAsync();
Console.WriteLine($"Заказ #{order.Id}: 2 порции плова, сумма {order.Items.Sum(x => x.Quantity * x.UnitPriceKopecks) / 100m:F2} руб.");
try { await repo.DeleteAsync(menuDish.Id); throw new Exception("Защита удаления не сработала."); }
catch (InvalidOperationException) { Console.WriteLine("Проверка: удаление блюда из заказа запрещено."); }
await transaction.RollbackAsync();
Console.WriteLine("Демонстрация завершена, тестовые данные отменены. Схема БД сохранена.");

} }

