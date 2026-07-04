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
        doc.MarginLeft = 60f;
        doc.MarginRight = 60f;
        doc.MarginTop = 50f;
        doc.MarginBottom = 50f;

        // ── Title ──
        doc.InsertParagraph("ДОГОВОР")
            .FontSize(16).Bold().Alignment = Alignment.center;
        doc.InsertParagraph("на выполнение работ по чистке и обслуживанию вентиляционных систем")
            .FontSize(13).Alignment = Alignment.center;
        doc.InsertParagraph();
        doc.InsertParagraph($"№ {contractNumber}")
            .FontSize(12).Alignment = Alignment.center;
        doc.InsertParagraph();

        // ── City / Date line ──
        var dtLine = doc.InsertParagraph();
        dtLine.Append("г. ").FontSize(11);
        dtLine.Append("Минск").FontSize(11).UnderlineStyle(Xceed.Document.NET.UnderlineStyle.singleLine);
        dtLine.Append($"\t\t{date} г.").FontSize(11);
        dtLine.SpacingAfter(10);

        // ── Parties block ──
        doc.InsertParagraph(
            "Общество с ограниченной ответственностью «VentClean», именуемое в дальнейшем «Исполнитель», " +
            "в лице Директора Иванова И.И., действующего на основании Устава, с одной стороны, и")
            .FontSize(11).IndentationFirstLine = 1.27f;

        var p = doc.InsertParagraph();
        p.Append(clientOrg?.Name ?? "Заказчик").FontSize(11).Bold();
        p.Append(", именуемое в дальнейшем «Заказчик», в лице руководителя, действующего на основании Устава, с другой стороны, заключили настоящий договор о нижеследующем:").FontSize(11);
        p.IndentationFirstLine = 1.27f;
        p.SpacingAfter(8);

        // ── 1. ПРЕДМЕТ ДОГОВОРА ──
        doc.InsertParagraph("1. ПРЕДМЕТ ДОГОВОРА").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "1.1. Исполнитель обязуется по заданию Заказчика выполнить работы по чистке, промывке " +
            "и обслуживанию вентиляционных систем и воздуховодов (далее — Работы), а Заказчик обязуется " +
            "принять результат Работ и оплатить его.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        var p12 = doc.InsertParagraph();
        p12.Append("1.2. Объект Работ: «").FontSize(11);
        p12.Append(objectName).FontSize(11).UnderlineStyle(Xceed.Document.NET.UnderlineStyle.singleLine);
        p12.Append("» по адресу: ").FontSize(11);
        p12.Append(address).FontSize(11).UnderlineStyle(Xceed.Document.NET.UnderlineStyle.singleLine);
        p12.Append(".").FontSize(11);
        p12.IndentationFirstLine = 1.27f;
        doc.InsertParagraph($"1.3. Вид Работ: {serviceType}.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph($"1.4. Характеристики объекта: площадь обслуживаемых поверхностей — {area:N2} м².")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 2. ЦЕНА ДОГОВОРА И ПОРЯДОК РАСЧЁТОВ ──
        doc.InsertParagraph("2. ЦЕНА ДОГОВОРА И ПОРЯДОК РАСЧЁТОВ").Bold().FontSize(12).SpacingBefore(12);
        var p21 = doc.InsertParagraph();
        p21.Append("2.1. Стоимость Работ составляет ").FontSize(11);
        p21.Append($"{cost:N2}").FontSize(11).Bold();
        p21.Append($" ({NumberToWords(cost)}) рублей.").FontSize(11);
        p21.IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "2.2. Оплата производится в течение 5 (пяти) банковских дней с даты подписания " +
            "сторонами акта сдачи-приёмки выполненных Работ.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "2.3. Стоимость Работ является фиксированной и изменению не подлежит.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "2.4. Датой оплаты считается дата поступления денежных средств на расчётный счёт Исполнителя.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 3. СРОКИ ВЫПОЛНЕНИЯ РАБОТ ──
        doc.InsertParagraph("3. СРОКИ ВЫПОЛНЕНИЯ РАБОТ").Bold().FontSize(12).SpacingBefore(12);
        var p31 = doc.InsertParagraph();
        p31.Append("3.1. Начало Работ: ").FontSize(11);
        p31.Append(startDate).FontSize(11).UnderlineStyle(Xceed.Document.NET.UnderlineStyle.singleLine);
        p31.Append(".").FontSize(11);
        p31.IndentationFirstLine = 1.27f;
        var p32 = doc.InsertParagraph();
        p32.Append("3.2. Окончание Работ: ").FontSize(11);
        p32.Append(endDate).FontSize(11).UnderlineStyle(Xceed.Document.NET.UnderlineStyle.singleLine);
        p32.Append(".").FontSize(11);
        p32.IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "3.3. Досрочное выполнение Работ допускается по согласованию Сторон.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 4. ПРАВА И ОБЯЗАННОСТИ СТОРОН ──
        doc.InsertParagraph("4. ПРАВА И ОБЯЗАННОСТИ СТОРОН").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "4.1. Исполнитель обязуется:")
            .FontSize(11);
        doc.InsertParagraph(
            "  — выполнить Работы качественно, в объёме и в сроки, предусмотренные настоящим договором;")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "  — обеспечить соблюдение требований техники безопасности и пожарной безопасности " +
            "при выполнении Работ;")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "  — предоставить Заказчику акт сдачи-приёмки выполненных Работ по завершении Работ;")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "  — устранить выявленные недостатки за свой счёт в согласованные сроки.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "4.2. Заказчик обязуется:")
            .FontSize(11);
        doc.InsertParagraph(
            "  — обеспечить доступ Исполнителя на объект для выполнения Работ;")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "  — принять выполненные Работы и подписать акт сдачи-приёмки;")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "  — оплатить Работы в порядке и сроки, предусмотренные настоящим договором.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "4.3. Исполнитель вправе не приступать к Работам, а начатые Работы приостановить " +
            "в случае неисполнения Заказчиком своих обязательств по настоящему договору.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "4.4. Заказчик вправе осуществлять контроль за ходом и качеством выполнения Работ " +
            "без вмешательства в оперативно-хозяйственную деятельность Исполнителя.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 5. ОТВЕТСТВЕННОСТЬ СТОРОН ──
        doc.InsertParagraph("5. ОТВЕТСТВЕННОСТЬ СТОРОН").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "5.1. Исполнитель гарантирует качество выполненных Работ в течение 12 (двенадцати) месяцев " +
            "с даты подписания акта сдачи-приёмки выполненных Работ.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "5.2. В случае обнаружения недостатков в гарантийный период Исполнитель обязан устранить " +
            "их за свой счёт в течение 10 (десяти) рабочих дней.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "5.3. За нарушение сроков выполнения Работ Исполнитель уплачивает Заказчику пеню " +
            "в размере 0,1% от стоимости невыполненных Работ за каждый день просрочки, " +
            "но не более 10% от стоимости Работ.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "5.4. За нарушение сроков оплаты Заказчик уплачивает Исполнителю пеню в размере 0,1% " +
            "от неоплаченной суммы за каждый день просрочки.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 6. ИЗМЕНЕНИЕ И РАСТОРЖЕНИЕ ДОГОВОРА ──
        doc.InsertParagraph("6. ИЗМЕНЕНИЕ И РАСТОРЖЕНИЕ ДОГОВОРА").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "6.1. Все изменения и дополнения к настоящему договору действительны, если совершены " +
            "в письменной форме и подписаны уполномоченными представителями Сторон.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "6.2. Договор может быть расторгнут досрочно по письменному соглашению Сторон " +
            "либо в одностороннем порядке с письменным уведомлением другой Стороны " +
            "не менее чем за 15 (пятнадцать) календарных дней.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "6.3. В случае одностороннего отказа Заказчика от договора после начала Работ " +
            "Заказчик оплачивает Исполнителю стоимость фактически выполненных Работ.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 7. ФОРС-МАЖОР ──
        doc.InsertParagraph("7. ФОРС-МАЖОР").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "7.1. Стороны освобождаются от ответственности за полное или частичное неисполнение " +
            "обязательств по настоящему договору, если это вызвано обстоятельствами непреодолимой силы, " +
            "возникшими после заключения договора.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "7.2. Сторона, ссылающаяся на форс-мажорные обстоятельства, обязана письменно уведомить " +
            "другую Сторону в течение 5 (пяти) календарных дней с момента их возникновения.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 8. РАЗРЕШЕНИЕ СПОРОВ ──
        doc.InsertParagraph("8. РАЗРЕШЕНИЕ СПОРОВ").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "8.1. Все споры и разногласия разрешаются путём переговоров Сторон.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "8.2. При недостижении согласия спор передаётся на рассмотрение в Экономический суд " +
            "г. Минска в порядке, установленном законодательством Республики Беларусь.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 9. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ ──
        doc.InsertParagraph("9. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ").Bold().FontSize(12).SpacingBefore(12);
        doc.InsertParagraph(
            "9.1. Настоящий договор вступает в силу с даты его подписания Сторонами и действует " +
            "до полного исполнения Сторонами своих обязательств.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "9.2. Договор составлен в двух экземплярах, имеющих одинаковую юридическую силу, " +
            "по одному для каждой из Сторон.")
            .FontSize(11).IndentationFirstLine = 1.27f;
        doc.InsertParagraph(
            "9.3. Во всём, что не предусмотрено настоящим договором, Стороны руководствуются " +
            "действующим законодательством Республики Беларусь.")
            .FontSize(11).IndentationFirstLine = 1.27f;

        // ── 10. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН ──
        doc.InsertParagraph("10. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН")
            .Bold().FontSize(12).SpacingBefore(12).SpacingAfter(6);

        var addrTable = doc.AddTable(7, 2);
        addrTable.Design = TableDesign.TableGrid;
        addrTable.Alignment = Alignment.center;
        addrTable.SetWidths(new float[] { 300f, 300f });

        void WriteCell(int row, int col, string label, string value)
        {
            var cell = addrTable.Rows[row].Cells[col];
            cell.Paragraphs[0].Append(label).Bold().FontSize(10);
            cell.Paragraphs[0].Append("\n" + value).FontSize(10);
        }

        WriteCell(0, 0, "ИСПОЛНИТЕЛЬ:", "ООО «VentClean»");
        WriteCell(0, 1, "ЗАКАЗЧИК:", clientOrg?.Name ?? "");
        WriteCell(1, 0, "УНП:", "123456789");
        WriteCell(1, 1, "УНП:", clientOrg?.Unp ?? "");
        WriteCell(2, 0, "Юр. адрес:", "220000, г. Минск, ул. Примерная, д. 1");
        WriteCell(2, 1, "Юр. адрес:", clientOrg?.LegalAddress ?? "");
        WriteCell(3, 0, "Р/с:", "BY12NBRB36009000000000000000");
        WriteCell(3, 1, "Р/с:", clientBankAccount);
        WriteCell(4, 0, "Банк:", "«Белгазпромбанк» ОАО");
        WriteCell(4, 1, "Банк:", clientBankName);
        WriteCell(5, 0, "Тел.:", "+375 29 111-22-33");
        WriteCell(5, 1, "Тел.:", clientPhone);
        WriteCell(6, 0, "E-mail:", "info@ventclean.by");
        WriteCell(6, 1, "E-mail:", clientUser?.Email ?? "");
        doc.InsertTable(addrTable);

        // ── 11. ПОДПИСИ СТОРОН ──
        doc.InsertParagraph("11. ПОДПИСИ СТОРОН")
            .Bold().FontSize(12).SpacingBefore(14).SpacingAfter(6);

        var sigTable = doc.AddTable(4, 2);
        sigTable.Design = TableDesign.TableGrid;
        sigTable.Alignment = Alignment.center;
        sigTable.SetWidths(new float[] { 300f, 300f });
        void SigCell(int row, int col, string text, bool bold = false, bool center = false)
        {
            var p2 = sigTable.Rows[row].Cells[col].Paragraphs[0];
            p2.Append(text).FontSize(10);
            if (bold) p2.Bold();
            if (center) p2.Alignment = Alignment.center;
        }
        SigCell(0, 0, "ИСПОЛНИТЕЛЬ", bold: true, center: true);
        SigCell(0, 1, "ЗАКАЗЧИК", bold: true, center: true);
        SigCell(1, 0, "Директор");
        SigCell(1, 1, "Руководитель");
        SigCell(2, 0, "_________ / Иванов И.И. /");
        SigCell(2, 1, "_________ / _____________ /");
        SigCell(3, 0, "М.П.", center: true);
        SigCell(3, 1, "М.П.", center: true);
        doc.InsertTable(sigTable);

        // ── Footer ──
        doc.InsertParagraph("Договор составлен в двух экземплярах, имеющих одинаковую юридическую силу.")
            .FontSize(10).Italic().Alignment = Alignment.center;

        var stream = new MemoryStream();
        doc.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"Договор_{contractNumber}.docx");
    }

    private static string NumberToWords(decimal number)
    {
        if (number == 0) return "ноль рублей 00 копеек";
        var wholePart = (long)Math.Floor(number);
        var decimalPart = (long)Math.Round((number - wholePart) * 100);
        return $"{NumberToWordsRus(wholePart)} рублей {decimalPart:D2} копеек";
    }

    private static string NumberToWordsRus(long number)
    {
        if (number == 0) return "ноль";
        var ones = new[] { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять",
            "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
            "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
        var tens = new[] { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят",
            "семьдесят", "восемьдесят", "девяносто" };
        var hundreds = new[] { "", "сто", "двести", "триста", "четыреста", "пятьсот",
            "шестьсот", "семьсот", "восемьсот", "девятьсот" };
        var thousands = new[] { "", "одна", "две" };

        var result = "";
        if (number >= 1000)
        {
            var t = number / 1000;
            if (t == 1) result += "одна тысяча ";
            else if (t == 2) result += "две тысячи ";
            else
            {
                result += NumberToWordsRus(t) + " ";
                result = result.Replace("один ", "одна ").Replace("два ", "две ");
                result += (t % 10 == 1 ? "тысяча " : t % 10 >= 2 && t % 10 <= 4 ? "тысячи " : "тысяч ");
            }
            number %= 1000;
        }
        if (number >= 100)
        {
            result += hundreds[number / 100] + " ";
            number %= 100;
        }
        if (number >= 20)
        {
            result += tens[number / 10] + " ";
            number %= 10;
        }
        if (number > 0)
            result += ones[number] + " ";
        return result.Trim();
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
