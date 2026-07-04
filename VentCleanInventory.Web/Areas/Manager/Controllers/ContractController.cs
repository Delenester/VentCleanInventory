using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xceed.Words.NET;
using Xceed.Document.NET;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = $"{AppUserRole.Manager},{AppUserRole.Client}")]
public class ContractController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Export(int id)
    {
        var req = await db.StockTransactions.AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.WorkObject)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (req == null) return NotFound();

        var clientOrg = req.Client;
        var contractNumber = req.ContractNumber ?? $"Д-{DateTime.Now:yyyyMMdd}-{req.Id}";
        var date = req.Date.ToString("dd.MM.yyyy");
        var cost = req.EstimatedCost ?? 0;
        var area = req.Area ?? 0;
        var address = req.WorkObject?.Address ?? "";
        var objectName = req.WorkObject?.Name ?? "";
        var serviceType = GetServiceTypeFromNote(req.Note);
        var description = GetDescriptionFromNote(req.Note);
        var startDate = req.PlannedStartDate?.ToString("dd.MM.yyyy") ?? "по согласованию";
        var endDate = req.PlannedEndDate?.ToString("dd.MM.yyyy") ?? "по согласованию";

        var clientOrgId = clientOrg?.Id;
        var clientUser = clientOrgId.HasValue
            ? await userManager.Users.AsNoTracking()
                .Where(u => u.OrganizationId == clientOrgId.Value)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync()
            : null;

        var clientBankAccount = clientUser?.BankAccount ?? "_________________";
        var clientBankName = clientUser?.BankName ?? "_________________";
        var clientPhone = clientUser?.PhoneNumber ?? "_________________";

        var doc = DocX.Create($"{contractNumber}.docx");

        doc.InsertParagraph("ДОГОВОР")
            .FontSize(18).Bold().Alignment = Alignment.center;
        doc.InsertParagraph("на выполнение работ по чистке и обслуживанию вентиляционных систем")
            .FontSize(14).Bold().Alignment = Alignment.center;
        doc.InsertParagraph($"№ {contractNumber}")
            .FontSize(12).Alignment = Alignment.center;
        doc.InsertParagraph();

        doc.InsertParagraph($"г. Минск")
            .FontSize(11).Alignment = Alignment.left;
        doc.InsertParagraph($"{date} г.")
            .FontSize(11).Alignment = Alignment.left;
        doc.InsertParagraph();

        doc.InsertParagraph(
            $"Общество с ограниченной ответственностью «VentClean» (далее — «Исполнитель»), УНП 123456789, " +
            $"ИНГ Белгазпромбанк, р/с BY12NBRB36009000000000000000, юр. адрес: г. Минск, ул. Примерная, 1, " +
            $"в лице Директора Иванова И.И., действующего на основании Устава, " +
            $"с одной стороны, и")
            .FontSize(11);

        doc.InsertParagraph(
            $"{clientOrg?.Name ?? "Заказчик"} (далее — «Заказчик»), УНП {clientOrg?.Unp ?? ""}, " +
            $"р/с {clientBankAccount}, " +
            $"{(string.IsNullOrWhiteSpace(clientBankName) ? "" : $"банк {clientBankName}, ")}" +
            $"тел. {clientPhone}, " +
            $"в лице руководителя, действующего на основании Устава, " +
            $"с другой стороны, заключили настоящий договор о нижеследующем:")
            .FontSize(11);

        // 1. ПРЕДМЕТ ДОГОВОРА
        doc.InsertParagraph();
        doc.InsertParagraph("1. ПРЕДМЕТ ДОГОВОРА").Bold().FontSize(13);
        doc.InsertParagraph(
            "1.1. Исполнитель обязуется выполнить работы по чистке, промывке и обслуживанию " +
            "вентиляционных систем и воздуховодов (далее — «Работы») на объекте Заказчика в " +
            "соответствии с условиями настоящего договора.")
            .FontSize(11);
        doc.InsertParagraph(
            $"1.2. Объект работ: «{objectName}» по адресу: {address}.")
            .FontSize(11);
        doc.InsertParagraph(
            $"1.3. Вид работ: {serviceType}.")
            .FontSize(11);
        doc.InsertParagraph(
            $"1.4. Описание работ: {description}.")
            .FontSize(11);
        doc.InsertParagraph(
            $"1.5. Площадь объекта: {area:N2} м².")
            .FontSize(11);

        // 2. ЦЕНА И ПОРЯДОК РАСЧЕТОВ
        doc.InsertParagraph();
        doc.InsertParagraph("2. ЦЕНА И ПОРЯДОК РАСЧЕТОВ").Bold().FontSize(13);
        doc.InsertParagraph(
            $"2.1. Стоимость Работ по настоящему договору составляет {cost:N2} (прописью: {NumberToWords(cost)}) рублей, " +
            "включая стоимость материалов и комплектующих, необходимых для выполнения работ.")
            .FontSize(11);
        doc.InsertParagraph(
            "2.2. Оплата производится в течение 5 (пяти) банковских дней с момента подписания " +
            "акта выполненных работ обеими сторонами.")
            .FontSize(11);
        doc.InsertParagraph(
            "2.3. Сумма договора является фиксированной и изменению не подлежит, " +
            "за исключением случаев, предусмотренных действующим законодательством.")
            .FontSize(11);

        // 3. СРОКИ ВЫПОЛНЕНИЯ РАБОТ
        doc.InsertParagraph();
        doc.InsertParagraph("3. СРОКИ ВЫПОЛНЕНИЯ РАБОТ").Bold().FontSize(13);
        doc.InsertParagraph(
            $"3.1. Начало работ: {startDate}.")
            .FontSize(11);
        doc.InsertParagraph(
            $"3.2. Окончание работ: {endDate}.")
            .FontSize(11);
        doc.InsertParagraph(
            "3.3. Сроки выполнения работ могут быть продлены по письменному соглашению сторон " +
            "при наличии обстоятельств, препятствующих своевременному выполнению.")
            .FontSize(11);

        // 4. ПРАВА И ОБЯЗАННОСТИ СТОРОН
        doc.InsertParagraph();
        doc.InsertParagraph("4. ПРАВА И ОБЯЗАННОСТИ СТОРОН").Bold().FontSize(13);
        doc.InsertParagraph(
            "4.1. Исполнитель имеет право:")
            .FontSize(11);
        doc.InsertParagraph(
            "  • получать от Заказчика всю необходимую информацию и документацию для выполнения Работ;")
            .FontSize(11);
        doc.InsertParagraph(
            "  • привлекать третьих лиц для выполнения Работ с согласия Заказчика.")
            .FontSize(11);
        doc.InsertParagraph(
            "4.2. Исполнитель обязуется:")
            .FontSize(11);
        doc.InsertParagraph(
            "  • выполнить работы в соответствии с техническими требованиями и нормативными документами;")
            .FontSize(11);
        doc.InsertParagraph(
            "  • обеспечить качество выполненных работ;")
            .FontSize(11);
        doc.InsertParagraph(
            "  • предоставить акт выполненных работ после завершения работ.")
            .FontSize(11);
        doc.InsertParagraph(
            "4.3. Заказчик имеет право:")
            .FontSize(11);
        doc.InsertParagraph(
            "  • проверять ход и качество выполнения Работ;")
            .FontSize(11);
        doc.InsertParagraph(
            "  • требовать устранения недостатков в установленные сроки.")
            .FontSize(11);
        doc.InsertParagraph(
            "4.4. Заказчик обязуется:")
            .FontSize(11);
        doc.InsertParagraph(
            "  • обеспечить доступ к объекту для выполнения работ;")
            .FontSize(11);
        doc.InsertParagraph(
            "  • оплатить выполненные работы в установленные сроки;")
            .FontSize(11);
        doc.InsertParagraph(
            "  • обеспечить безопасные условия для работы Исполнителя.")
            .FontSize(11);

        // 5. ОТВЕТСТВЕННОСТЬ СТОРОН
        doc.InsertParagraph();
        doc.InsertParagraph("5. ОТВЕТСТВЕННОСТЬ СТОРОН").Bold().FontSize(13);
        doc.InsertParagraph(
            "5.1. Исполнитель гарантирует качество выполненных работ в течение 12 (двенадцати) месяцев " +
            "с момента подписания акта выполненных работ.")
            .FontSize(11);
        doc.InsertParagraph(
            "5.2. В случае выявления недостатков, возникших по вине Исполнителя, " +
            "он обязуется устранить их за свой счёт в течение 10 (десяти) рабочих дней.")
            .FontSize(11);
        doc.InsertParagraph(
            "5.3. За нарушение сроков выполнения Работ Исполнитель уплачивает пеню в размере 0,1% " +
            "от стоимости Работ за каждый день просрочки, но не более 10% от суммы договора.")
            .FontSize(11);
        doc.InsertParagraph(
            "5.4. За нарушение сроков оплаты Заказчик уплачивает пеню в размере 0,1% " +
            "от неоплаченной суммы за каждый день просрочки.")
            .FontSize(11);

        // 6. ПОРЯДОК ИЗМЕНЕНИЯ И РАСТОРЖЕНИЯ ДОГОВОРА
        doc.InsertParagraph();
        doc.InsertParagraph("6. ПОРЯДОК ИЗМЕНЕНИЯ И РАСТОРЖЕНИЯ ДОГОВОРА").Bold().FontSize(13);
        doc.InsertParagraph(
            "6.1. Все изменения и дополнения к настоящему договору действительны при условии " +
            "их оформления в письменной форме и подписания уполномоченными представителями сторон.")
            .FontSize(11);
        doc.InsertParagraph(
            "6.2. Договор может быть расторгнут досрочно по письменному соглашению сторон " +
            "либо в одностороннем порядке в случае существенного нарушения условий договора " +
            "одной из сторон с письменным уведомлением не менее чем за 15 (пятнадцать) календарных дней.")
            .FontSize(11);
        doc.InsertParagraph(
            "6.3. В случае одностороннего отказа Заказчика от исполнения договора после начала работ " +
            "он обязан оплатить Исполнителю фактически выполненные работы.")
            .FontSize(11);

        // 7. ФОРС-МАЖОР
        doc.InsertParagraph();
        doc.InsertParagraph("7. ФОРС-МАЖОР").Bold().FontSize(13);
        doc.InsertParagraph(
            "7.1. Стороны освобождаются от ответственности за частичное или полное неисполнение " +
            "обязательств по настоящему договору, если это явилось следствием обстоятельств " +
            "непреодолимой силы (пожар, наводнение, землетрясение, военные действия, " +
            "изменение законодательства), возникших после заключения договора.")
            .FontSize(11);
        doc.InsertParagraph(
            "7.2. Сторона, ссылающаяся на форс-мажорные обстоятельства, обязана уведомить " +
            "другую сторону в письменной форме в течение 5 (пяти) календарных дней.")
            .FontSize(11);

        // 8. ПОРЯДОК РАЗРЕШЕНИЯ СПОРОВ
        doc.InsertParagraph();
        doc.InsertParagraph("8. ПОРЯДОК РАЗРЕШЕНИЯ СПОРОВ").Bold().FontSize(13);
        doc.InsertParagraph(
            "8.1. Все споры и разногласия, возникающие между сторонами, разрешаются путём переговоров.")
            .FontSize(11);
        doc.InsertParagraph(
            "8.2. В случае недостижения согласия спор передаётся на рассмотрение " +
            "экономического суда города Минска в соответствии с действующим законодательством.")
            .FontSize(11);

        // 9. ПРОЧИЕ УСЛОВИЯ
        doc.InsertParagraph();
        doc.InsertParagraph("9. ПРОЧИЕ УСЛОВИЯ").Bold().FontSize(13);
        doc.InsertParagraph(
            "9.1. Настоящий договор вступает в силу с момента его подписания обеими сторонами " +
            "и действует до полного исполнения сторонами своих обязательств.")
            .FontSize(11);
        doc.InsertParagraph(
            "9.2. Настоящий договор составлен в двух экземплярах, имеющих одинаковую юридическую силу, " +
            "по одному для каждой из сторон.")
            .FontSize(11);
        doc.InsertParagraph(
            "9.3. Во всём, что не предусмотрено настоящим договором, стороны руководствуются " +
            "действующим законодательством Республики Беларусь.")
            .FontSize(11);

        // 10. ЮРИДИЧЕСКИЕ АДРЕСА СТОРОН
        doc.InsertParagraph();
        doc.InsertParagraph("10. ЮРИДИЧЕСКИЕ АДРЕСА СТОРОН").Bold().FontSize(13);
        doc.InsertParagraph();

        var table = doc.AddTable(5, 2);
        table.Rows[0].Cells[0].Paragraphs[0].Append("ИСПОЛНИТЕЛЬ:").Bold();
        table.Rows[0].Cells[1].Paragraphs[0].Append("ЗАКАЗЧИК:").Bold();
        table.Rows[1].Cells[0].Paragraphs[0].Append("ООО «VentClean»");
        table.Rows[1].Cells[1].Paragraphs[0].Append(clientOrg?.Name ?? "");
        table.Rows[2].Cells[0].Paragraphs[0].Append("УНП: 123456789");
        table.Rows[2].Cells[1].Paragraphs[0].Append($"УНП: {clientOrg?.Unp ?? ""}");
        table.Rows[3].Cells[0].Paragraphs[0].Append("Юр. адрес: г. Минск, ул. Примерная, 1");
        table.Rows[3].Cells[1].Paragraphs[0].Append($"Юр. адрес: {clientOrg?.LegalAddress ?? ""}");
        table.Rows[4].Cells[0].Paragraphs[0].Append("Р/с: BY12NBRB36009000000000000000");
        table.Rows[4].Cells[1].Paragraphs[0].Append($"Р/с: {clientBankAccount}");
        doc.InsertTable(table);

        // 11. ПОДПИСИ СТОРОН
        doc.InsertParagraph();
        doc.InsertParagraph("11. ПОДПИСИ СТОРОН").Bold().FontSize(13);
        doc.InsertParagraph();

        var sigTable = doc.AddTable(3, 2);
        sigTable.Rows[0].Cells[0].Paragraphs[0].Append("ИСПОЛНИТЕЛЬ:").Bold();
        sigTable.Rows[0].Cells[1].Paragraphs[0].Append("ЗАКАЗЧИК:").Bold();
        sigTable.Rows[1].Cells[0].Paragraphs[0].Append("_________________ / Иванов И.И. /");
        sigTable.Rows[1].Cells[1].Paragraphs[0].Append($"_________________ / {clientOrg?.Name ?? ""} /");
        sigTable.Rows[2].Cells[0].Paragraphs[0].Append("М.П.");
        sigTable.Rows[2].Cells[1].Paragraphs[0].Append("М.П.");
        doc.InsertTable(sigTable);

        var stream = new MemoryStream();
        doc.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"Договор_{contractNumber}.docx");
    }

    private static string NumberToWords(decimal number)
    {
        if (number == 0) return "ноль рублей";
        var wholePart = (long)Math.Floor(number);
        var decimalPart = (long)Math.Round((number - wholePart) * 100);
        return $"{wholePart} руб. {decimalPart} коп.";
    }

    private static string GetServiceTypeFromNote(string? note)
    {
        if (string.IsNullOrEmpty(note)) return "";
        var idx = note.IndexOf(']');
        if (note.StartsWith("[") && idx > 0)
            return note[1..idx];
        return "";
    }

    private static string GetDescriptionFromNote(string? note)
    {
        if (string.IsNullOrEmpty(note)) return "";
        var idx = note.IndexOf(']');
        if (note.StartsWith("[") && idx > 0 && idx + 2 < note.Length)
            return note[(idx + 2)..];
        return note;
    }
}
