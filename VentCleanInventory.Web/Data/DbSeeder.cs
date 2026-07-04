using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IWebHostEnvironment env)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var role in AppUserRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var anyUsers = await userManager.Users.AnyAsync();
        if (anyUsers && await db.Nomenclatures.AnyAsync()) return;

        // ── Users ──
        var admin = await SeedUserAsync(userManager, "admin", "Admin2026", "Администратор", AppUserRole.Admin);
        var dispatcher = await SeedUserAsync(userManager, "dispatcher", "Disp2026", "Иван Петров", AppUserRole.Dispatcher);
        var master = await SeedUserAsync(userManager, "master", "Master2026", "Алексей Смирнов", AppUserRole.Master);
        var manager = await SeedUserAsync(userManager, "manager", "Manager2026", "Марина Иванова", AppUserRole.Manager);

        // ── Warehouses ──
        var centralWh = new Warehouse { Name = "Центральный склад", Type = WarehouseType.Central, Address = "г. Минск, ул. Заводская, 1", MasterUserId = master.Id };
        var mobileWh1 = new Warehouse { Name = "Карманный склад №1", Type = WarehouseType.Mobile, Address = "г. Минск, ул. Ленина, 15", MasterUserId = master.Id };
        db.Warehouses.AddRange(centralWh, mobileWh1);
        await db.SaveChangesAsync();

        // ── Organizations (clients & suppliers) ──
        var client1 = new Organization { Type = OrganizationType.Client, Unp = "123456789", Name = "ООО «Промвентиляция»", LegalAddress = "г. Минск, пр-т Независимости, 50", ContactInfo = "тел. +375291234567" };
        var client2 = new Organization { Type = OrganizationType.Client, Unp = "987654321", Name = "АО «ТеплоСтрой»", LegalAddress = "г. Минск, ул. Немига, 12", ContactInfo = "тел. +375297654321" };
        var supplier1 = new Organization { Type = OrganizationType.Supplier, Name = "ООО «ТехноВент»", Unp = "456123789", LegalAddress = "г. Минск, ул. Машиностроителей, 8", ContactInfo = "тел. +375331234567" };
        var supplier2 = new Organization { Type = OrganizationType.Supplier, Name = "ИП «ФильтрСервис»", Unp = "789321654", LegalAddress = "г. Минск, ул. Советская, 25", ContactInfo = "тел. +375291112233" };
        db.Organizations.AddRange(client1, client2, supplier1, supplier2);
        await db.SaveChangesAsync();

        // ── External users ──
        await SeedUserAsync(userManager, "client", "Client2026", "Пётр Иванович", AppUserRole.Client, organizationId: client1.Id);
        await SeedUserAsync(userManager, "supplier", "Supplier2026", "Сергей Петров", AppUserRole.Supplier, organizationId: supplier1.Id);

        // ── WorkObjects ──
        var wo1 = new WorkObject { Name = "Административное здание, ул. Ленина, 15", Address = "г. Минск, ул. Ленина, 15", VentSystemType = "Приточно-вытяжная", AccessDifficulty = "Средняя", Distance = "5 км" };
        var wo2 = new WorkObject { Name = "Производственный цех №3", Address = "г. Минск, ул. Заводская, 1", VentSystemType = "Промышленная вытяжка", AccessDifficulty = "Высокая (высота 8 м)", Distance = "12 км" };
        var wo3 = new WorkObject { Name = "Торговый центр «Гранд»", Address = "г. Минск, ул. Победителей, 100", VentSystemType = "Центральное кондиционирование", AccessDifficulty = "Низкая", Distance = "3 км" };
        db.WorkObjects.AddRange(wo1, wo2, wo3);
        await db.SaveChangesAsync();

        // ── Nomenclature ──
        var noms = new List<Nomenclature>
        {
            new() { Name = "Фильтр карманный F5 592x592x500", Unit = "шт.", IsEquipment = false },
            new() { Name = "Фильтр панельный G4 592x592x96", Unit = "шт.", IsEquipment = false },
            new() { Name = "Фильтр угольный", Unit = "шт.", IsEquipment = false },
            new() { Name = "Ремень клиновой SPZ 2000", Unit = "шт.", IsEquipment = false },
            new() { Name = "Подшипник SKF 6205-2Z", Unit = "шт.", IsEquipment = false },
            new() { Name = "Смазка для подшипников Mobilux EP 2 (1 кг)", Unit = "кг", IsEquipment = false },
            new() { Name = "Герметик силиконовый (туба 310 мл)", Unit = "шт.", IsEquipment = false },
            new() { Name = "Химия для промывки вентиляции (канистра 5 л)", Unit = "л", IsEquipment = false },
            new() { Name = "Двигатель вентилятора DKM 0.75 кВт", Unit = "шт.", IsEquipment = true },
            new() { Name = "Двигатель вентилятора DKM 1.5 кВт", Unit = "шт.", IsEquipment = true },
            new() { Name = "Вентилятор канальный VK 250", Unit = "шт.", IsEquipment = true },
            new() { Name = "Вентилятор канальный VK 315", Unit = "шт.", IsEquipment = true },
            new() { Name = "Калорифер водяной CWK 200x300", Unit = "шт.", IsEquipment = true },
            new() { Name = "Воздуховод оцинкованный 200x200 (1 м)", Unit = "шт.", IsEquipment = false },
            new() { Name = "Воздуховод оцинкованный d=250 (1 м)", Unit = "шт.", IsEquipment = false },
            new() { Name = "Решётка вентиляционная 200x200", Unit = "шт.", IsEquipment = false },
            new() { Name = "Лента алюминиевая (50 м)", Unit = "шт.", IsEquipment = false },
            new() { Name = "Утеплитель для воздуховодов (10 м²)", Unit = "м²", IsEquipment = false },
            new() { Name = "Датчик температуры", Unit = "шт.", IsEquipment = true },
            new() { Name = "Контроллер скорости вентилятора", Unit = "шт.", IsEquipment = true },
        };
        db.Nomenclatures.AddRange(noms);
        await db.SaveChangesAsync();

        // ── InventoryItems ──
        var items = new List<InventoryItem>();
        var rnd = new Random(42);
        foreach (var nom in noms)
        {
            int count = nom.IsEquipment ? 2 : 4;
            for (int i = 1; i <= count; i++)
            {
                var item = new InventoryItem
                {
                    NomenclatureId = nom.Id,
                    SerialNumber = nom.IsEquipment ? $"EQ-{nom.Id:D3}-{i:D4}" : null,
                    PurchaseDate = DateTime.Today.AddDays(-rnd.Next(30, 365)),
                    OrganizationId = i % 2 == 0 ? supplier1.Id : supplier2.Id,
                    PurchasePrice = rnd.Next(5, 500) + Math.Round((decimal)rnd.NextDouble(), 2),
                    ExpirationDate = nom.Name.Contains("Фильтр") ? DateTime.Today.AddDays(180) : null,
                };
                items.Add(item);
            }
        }
        db.InventoryItems.AddRange(items);
        await db.SaveChangesAsync();

        // ── StockBalances ──
        var balances = new List<StockBalance>();
        foreach (var item in items)
        {
            balances.Add(new StockBalance { WarehouseId = centralWh.Id, InventoryItemId = item.Id, Quantity = rnd.Next(5, 100) + Math.Round((decimal)rnd.NextDouble(), 1) });
            if (rnd.Next(3) == 0)
                balances.Add(new StockBalance { WarehouseId = mobileWh1.Id, InventoryItemId = item.Id, Quantity = rnd.Next(1, 10) + Math.Round((decimal)rnd.NextDouble(), 1) });
        }
        db.StockBalances.AddRange(balances);
        await db.SaveChangesAsync();

        // ── Requests (as StockTransaction with RequestStatusValue) ──
        var requestStatuses = new[] { RequestStatus.New, RequestStatus.Approved, RequestStatus.Completed };
        for (int i = 1; i <= 6; i++)
        {
            var isEven = i % 2 == 0;
            var reqItems = new List<TransactionItemDto>();
            for (int j = 0; j < rnd.Next(2, 5); j++)
            {
                var nom = noms[rnd.Next(noms.Count)];
                if (!reqItems.Any(ri => ri.NomenclatureId == nom.Id))
                    reqItems.Add(new TransactionItemDto { NomenclatureId = nom.Id, NomenclatureName = nom.Name, Quantity = rnd.Next(1, 20) });
            }
            var st = new StockTransaction
            {
                TransactionType = TransactionType.Issue,
                UserId = master.Id,
                WorkObjectId = new[] { wo1.Id, wo2.Id, wo3.Id }[rnd.Next(3)],
                ClientId = isEven ? client1.Id : client2.Id,
                SupplierId = isEven ? supplier1.Id : supplier2.Id,
                Date = DateTime.Today.AddDays(-rnd.Next(1, 60)),
                RequestStatusValue = requestStatuses[rnd.Next(3)],
                Note = $"Заявка #{i}",
                EstimatedCost = isEven ? (decimal)(100 + rnd.NextDouble() * 400) : null,
                ContractNumber = isEven ? $"Д-{DateTime.Now:yyyyMMdd}-{100 + i}" : null,
                AssignedMasterId = isEven ? master.Id : null,
            };
            st.SetItems(reqItems);
            db.StockTransactions.Add(st);
        }
        await db.SaveChangesAsync();

        // ── WorkLogs ──
        var workLogs = new List<WorkLog>();
        var zoneNames = new[] { "Цокольный этаж", "1 этаж — холл", "Кровля — венткамеры", "Цех — основная зона", "Торговый зал" };
        var objIds = new[] { wo1.Id, wo1.Id, wo1.Id, wo2.Id, wo3.Id };
        for (int i = 0; i < 5; i++)
        {
            var wl = new WorkLog
            {
                MasterUserId = master.Id,
                WorkObjectId = objIds[i],
                ZoneName = zoneNames[i],
                WorkDate = DateTime.Today.AddDays(-rnd.Next(1, 30)),
                Description = $"Плановое ТО вентиляции — {zoneNames[i]}",
                MaterialsUsed = $"{noms[rnd.Next(noms.Count)].Name} x{rnd.Next(1,5)}; {noms[rnd.Next(noms.Count)].Name} x{rnd.Next(1,3)}",
                Meters = rnd.Next(5, 30) + (decimal)rnd.NextDouble(),
                Grids = rnd.Next(0, 8),
            };
            workLogs.Add(wl);
        }
        db.WorkLogs.AddRange(workLogs);
        await db.SaveChangesAsync();

        // ── StockTransactions ──
        var txTypes = new[] { TransactionType.Receipt, TransactionType.Issue, TransactionType.Return, TransactionType.Transfer };
        var txns = new List<StockTransaction>();
        for (int i = 1; i <= 8; i++)
        {
            var txn = new StockTransaction
            {
                TransactionType = txTypes[rnd.Next(4)],
                Date = DateTime.Today.AddDays(-rnd.Next(1, 90)),
                UserId = dispatcher.Id,
                OrganizationId = rnd.Next(2) == 0 ? supplier1.Id : supplier2.Id,
                Note = $"Транзакция #{i}",
            };
            if (rnd.Next(2) == 0) { txn.FromWarehouseId = centralWh.Id; txn.ToWarehouseId = mobileWh1.Id; }
            else { txn.ToWarehouseId = centralWh.Id; }

            var txItems = new List<TransactionItemDto>();
            for (int j = 0; j < rnd.Next(1, 4); j++)
            {
                var item = items[rnd.Next(items.Count)];
                txItems.Add(new TransactionItemDto
                {
                    InventoryItemId = item.Id,
                    Quantity = rnd.Next(1, 15),
                    UnitPrice = item.PurchasePrice,
                });
            }
            txn.SetItems(txItems);
            txns.Add(txn);
        }
        db.StockTransactions.AddRange(txns);
        await db.SaveChangesAsync();

        var writeOffTx = txns.First(t => t.TransactionType == TransactionType.Issue || t.TransactionType == TransactionType.WriteOff);
        writeOffTx.ActNumber = "АС-2026-001";
        writeOffTx.ActDate = DateTime.Today.AddDays(-5);
        writeOffTx.ActCreatedByUserId = master.Id;
        writeOffTx.ActStatus = WriteOffActStatus.Approved;
        writeOffTx.WriteOffReason = WriteOffReason.Wear;
        await db.SaveChangesAsync();
    }

    static async Task<ApplicationUser> SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string login, string password, string fullName, string role,
        int? organizationId = null, string? email = null)
    {
        var user = await userManager.FindByNameAsync(login);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = login,
                Email = email ?? $"{login}@ventclean.local",
                FullName = fullName,
                IsApproved = true,
                AccountType = organizationId != null
                    ? (role == AppUserRole.Client ? AccountType.Client : AccountType.Supplier)
                    : AccountType.Internal,
                OrganizationId = organizationId,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create user '{login}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        user.IsApproved = true;
        user.OrganizationId = organizationId ?? user.OrganizationId;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }
}
