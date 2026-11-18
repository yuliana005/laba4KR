using System.Globalization;
using CateringLab.Data;
using CateringLab.Demo;
using CateringLab.Models;
using CateringLab.Repositories;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;
if (args.Contains("--demo")) { await DemoRunner.RunAsync(); return; }
await using (var db = new CateringDbContext()) { await db.Database.MigrateAsync(); }
Console.WriteLine("УЧЁТ ЗАКАЗОВ КАФЕ");
Console.WriteLine("Сначала добавьте блюда в меню, затем создайте заказ из этих блюд.");
Console.WriteLine("Данные сохраняются в " + Path.GetFullPath("catering.db"));
while (true)
{
    Console.WriteLine("\n1 — Меню блюд\n2 — Добавить блюдо\n3 — Изменить блюдо\n4 — Удалить блюдо\n5 — Создать заказ\n6 — Посмотреть заказы\n7 — Изменить статус заказа\n0 — Выход");
    Console.Write("Ваш выбор: ");
    var choice = Console.ReadLine();
    if (choice is null or "0") break;
    // Отдельный контекст на операцию не сохраняет неудачные изменения в следующей команде.
    await using var db = new CateringDbContext();
    IDishRepository repo = new DishRepository(db);
    try
    {
        switch (choice)
        {
            case "1": await ShowMenu(repo); break;
            case "2":
                var name = Read("Название блюда: ");
                var category = Read("Категория (например, Супы): ");
                var price = ReadPrice();
                var added = await repo.AddAsync(name, category, price);
                Console.WriteLine($"Блюдо №{added.Id} добавлено.");
                break;
            case "3":
                await ShowMenu(repo);
                var editId = ReadInt("Номер блюда: ");
                var dish = await repo.GetByIdAsync(editId);
                if (dish is null) { Console.WriteLine("Такого блюда нет."); break; }
                Console.WriteLine($"Сейчас: {dish.Name}, {dish.Category}, {Money(dish.PriceKopecks)}");
                var newName = Read("Новое название: ");
                var newCategory = Read("Новая категория: ");
                await repo.UpdateAsync(editId, newName, newCategory, ReadPrice());
                Console.WriteLine("Блюдо изменено. Цены в прошлых заказах сохранены.");
                break;
            case "4":
                await ShowMenu(repo);
                var deleteId = ReadInt("Номер блюда для удаления: ");
                if (Read("Удалить? Введите да: ").Equals("да", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine(await repo.DeleteAsync(deleteId) ? "Блюдо удалено." : "Такого блюда нет.");
                else Console.WriteLine("Удаление отменено.");
                break;
            case "5":
                if (!await db.Dishes.AnyAsync()) { Console.WriteLine("Сначала добавьте хотя бы одно блюдо: пункт 2."); break; }
                await ShowMenu(repo);
                var order = new Order();
                Console.WriteLine("Добавляйте позиции. Номер 0 завершает выбор.");
                while (true)
                {
                    var id = ReadInt("Номер блюда (0 — завершить): ", true);
                    if (id == 0) break;
                    var selected = await repo.GetByIdAsync(id);
                    if (selected is null) { Console.WriteLine("Такого блюда нет."); continue; }
                    var quantity = ReadInt("Количество порций: ");
                    var existing = order.Items.FirstOrDefault(x => x.DishId == id);
                    if (existing is null) order.Items.Add(new OrderItem { DishId = id, Quantity = quantity, UnitPriceKopecks = selected.PriceKopecks });
                    else existing.Quantity = checked(existing.Quantity + quantity);
                    Console.WriteLine($"Добавлено: {selected.Name} × {quantity}. Сумма заказа: {Money(Total(order))}");
                }
                if (order.Items.Count == 0) { Console.WriteLine("Пустой заказ отменён."); break; }
                if (!Read("Сохранить заказ? Введите да: ").Equals("да", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine("Заказ отменён."); break; }
                db.Orders.Add(order);
                await db.SaveChangesAsync();
                Console.WriteLine($"Заказ №{order.Id} сохранён. Сумма: {Money(Total(order))}");
                break;
            case "6":
                var orders = await db.Orders.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Dish).OrderByDescending(x => x.Id).ToListAsync();
                if (orders.Count == 0) Console.WriteLine("Заказов пока нет. Создайте заказ: пункт 5.");
                foreach (var o in orders)
                {
                    Console.WriteLine($"\nЗаказ №{o.Id} | {o.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm} | {Status(o.Status)}");
                    foreach (var item in o.Items) Console.WriteLine($"  {item.Dish.Name} × {item.Quantity} по {Money(item.UnitPriceKopecks)}");
                    Console.WriteLine($"  Итого: {Money(Total(o))}");
                }
                break;
            case "7":
                var orderId = ReadInt("Номер заказа: ");
                var target = await db.Orders.FindAsync(orderId);
                if (target is null) { Console.WriteLine("Такого заказа нет."); break; }
                Console.WriteLine("1 — Новый, 2 — Готовится, 3 — Выполнен, 4 — Отменён");
                var status = ReadInt("Новый статус: ");
                if (status > 4) { Console.WriteLine("Нужен номер от 1 до 4."); break; }
                target.Status = (OrderStatus)(status - 1);
                await db.SaveChangesAsync();
                Console.WriteLine("Статус изменён: " + Status(target.Status));
                break;
            default: Console.WriteLine("Выберите пункт от 0 до 7."); break;
        }
    }
    catch (EndOfStreamException) { break; }
    catch (ArgumentException ex) { Console.WriteLine("Не удалось сохранить: " + ex.Message); }
    catch (InvalidOperationException ex) { Console.WriteLine(ex.Message); }
    catch (OverflowException) { Console.WriteLine("Слишком большое число. Операция отменена."); }
    catch (DbUpdateException) { Console.WriteLine("Не удалось сохранить данные в БД. Проверьте значения и связи."); }
}
Console.WriteLine("Работа завершена. Сохранённые данные доступны при следующем запуске.");

static string Read(string prompt) { Console.Write(prompt); return Console.ReadLine()?.Trim() ?? throw new EndOfStreamException(); }
static int ReadInt(string prompt, bool allowZero = false)
{
    while (true)
    {
        if (int.TryParse(Read(prompt), out var value) && value >= (allowZero ? 0 : 1)) return value;
        Console.WriteLine(allowZero ? "Введите целое число от 0." : "Введите положительное целое число.");
    }
}
static long ReadPrice()
{
    while (true)
    {
        var input = Read("Цена в рублях (например, 250,50): ").Replace(',', '.');
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price)
            && price >= 0 && price <= 1000000 && decimal.Round(price, 2) == price) return (long)(price * 100);
        Console.WriteLine("Введите цену от 0 до 1 000 000 рублей, максимум 2 знака после запятой.");
    }
}
static string Money(long kopecks) => (kopecks / 100m).ToString("F2", CultureInfo.GetCultureInfo("ru-RU")) + " руб.";
static long Total(Order order) => order.Items.Sum(x => checked(x.UnitPriceKopecks * x.Quantity));
static string Status(OrderStatus status) => status switch { OrderStatus.New => "Новый", OrderStatus.Preparing => "Готовится", OrderStatus.Completed => "Выполнен", OrderStatus.Cancelled => "Отменён", _ => status.ToString() };
static async Task ShowMenu(IDishRepository repo)
{
    var dishes = await repo.GetAllAsync();
    if (dishes.Count == 0) Console.WriteLine("Меню пока пустое. Добавьте блюдо: пункт 2.");
    foreach (var d in dishes) Console.WriteLine($"№{d.Id} | {d.Name} | {d.Category} | {Money(d.PriceKopecks)}");
}

