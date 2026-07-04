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
        doc.MarginLeft = 70f;
        doc.MarginRight = 50f;
        doc.MarginTop = 50f;
        doc.MarginBottom = 50f;
        var fs = 11f;

        // ════════════════════════════════════════════════
        //  HEADER
        // ════════════════════════════════════════════════
        doc.InsertParagraph("ДОГОВОР")
            .Font("Times New Roman").FontSize(16).Bold().Alignment = Alignment.center;
        doc.InsertParagraph("на выполнение работ по чистке и обслуживанию вентиляционных систем")
            .Font("Times New Roman").FontSize(13).Alignment = Alignment.center;
        doc.InsertParagraph($"№ {contractNumber}")
            .Font("Times New Roman").FontSize(13).Alignment = Alignment.center;
        doc.InsertParagraph();

        // City / Date
        var line = doc.InsertParagraph();
        line.Append("г. ").Font("Times New Roman").FontSize(fs);
        line.Append("Минск").Font("Times New Roman").FontSize(fs);
        line.Append($"\t\t\"{req.Date:dd}\" {req.Date:MMMM} {req.Date:yyyy} г.").Font("Times New Roman").FontSize(fs);
        line.SpacingAfter(12);

        // ════════════════════════════════════════════════
        //  PARTIES
        // ════════════════════════════════════════════════
        doc.InsertParagraph(
            "Общество с ограниченной ответственностью «VentClean» (УНП 123456789), " +
            "именуемое в дальнейшем «Исполнитель», в лице Директора Иванова И.И., " +
            "действующего на основании Устава, с одной стороны, и")
            .Font("Times New Roman").FontSize(fs).SpacingAfter(4);
        doc.InsertParagraph(
            $"{clientOrg?.Name ?? "Заказчик"} (УНП {clientOrg?.Unp ?? ""}), " +
            "именуемое в дальнейшем «Заказчик», в лице руководителя, действующего на основании Устава, " +
            "с другой стороны, заключили настоящий договор о нижеследующем:")
            .Font("Times New Roman").FontSize(fs).SpacingAfter(12);

        // ════════════════════════════════════════════════
        //  1. ПРЕДМЕТ ДОГОВОРА
        // ════════════════════════════════════════════════
        doc.InsertParagraph("1. ПРЕДМЕТ ДОГОВОРА").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "1.1. Исполнитель обязуется по заданию Заказчика выполнить работы по чистке, промывке " +
            "и обслуживанию вентиляционных систем и воздуховодов (далее — Работы), а Заказчик обязуется " +
            "принять результат Работ и оплатить его.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            $"1.2. Объект Работ: «{objectName}», расположенный по адресу: {address}.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph($"1.3. Вид Работ: {serviceType}.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph($"1.4. Площадь обслуживаемых поверхностей: {area:N2} м².")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  2. ЦЕНА ДОГОВОРА И ПОРЯДОК РАСЧЁТОВ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("2. ЦЕНА ДОГОВОРА И ПОРЯДОК РАСЧЁТОВ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            $"2.1. Стоимость Работ составляет {cost:N2} ({NumberToWords(cost)}) рублей.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "2.2. Оплата производится в течение 5 (пяти) банковских дней с даты подписания " +
            "сторонами акта сдачи-приёмки выполненных Работ.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "2.3. Стоимость Работ является фиксированной и изменению не подлежит.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  3. СРОКИ ВЫПОЛНЕНИЯ РАБОТ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("3. СРОКИ ВЫПОЛНЕНИЯ РАБОТ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph($"3.1. Начало Работ: {startDate}.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph($"3.2. Окончание Работ: {endDate}.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  4. ПРАВА И ОБЯЗАННОСТИ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("4. ПРАВА И ОБЯЗАННОСТИ СТОРОН").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "4.1. Исполнитель обязуется:")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - выполнить Работы качественно, в объёме и в сроки, предусмотренные настоящим договором;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - обеспечить соблюдение требований техники безопасности и пожарной безопасности " +
            "при выполнении Работ;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - предоставить Заказчику акт сдачи-приёмки выполненных Работ по завершении Работ;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - устранить выявленные недостатки за свой счёт в согласованные сроки.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.2. Заказчик обязуется:")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - обеспечить доступ Исполнителя на объект для выполнения Работ;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - принять выполненные Работы и подписать акт сдачи-приёмки;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  - оплатить Работы в порядке и сроки, предусмотренные настоящим договором.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.3. Заказчик вправе осуществлять контроль за ходом и качеством выполнения Работ " +
            "без вмешательства в оперативно-хозяйственную деятельность Исполнителя.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  5. ОТВЕТСТВЕННОСТЬ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("5. ОТВЕТСТВЕННОСТЬ СТОРОН").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "5.1. Исполнитель гарантирует качество выполненных Работ в течение 12 (двенадцати) месяцев " +
            "с даты подписания акта сдачи-приёмки.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.2. В случае обнаружения недостатков в гарантийный период Исполнитель обязан устранить " +
            "их за свой счёт в течение 10 (десяти) рабочих дней.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.3. За нарушение сроков выполнения Работ Исполнитель уплачивает Заказчику пеню " +
            "в размере 0,1% от стоимости невыполненных Работ за каждый день просрочки, " +
            "но не более 10% от стоимости Работ.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.4. За нарушение сроков оплаты Заказчик уплачивает Исполнителю пеню в размере 0,1% " +
            "от неоплаченной суммы за каждый день просрочки.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  6. ИЗМЕНЕНИЕ И РАСТОРЖЕНИЕ ДОГОВОРА
        // ════════════════════════════════════════════════
        doc.InsertParagraph("6. ИЗМЕНЕНИЕ И РАСТОРЖЕНИЕ ДОГОВОРА").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "6.1. Все изменения и дополнения к настоящему договору действительны, если совершены " +
            "в письменной форме и подписаны уполномоченными представителями Сторон.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.2. Договор может быть расторгнут досрочно по письменному соглашению Сторон " +
            "либо в одностороннем порядке с письменным уведомлением не менее чем за 15 " +
            "(пятнадцать) календарных дней.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  7. ФОРС-МАЖОР
        // ════════════════════════════════════════════════
        doc.InsertParagraph("7. ФОРС-МАЖОР").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "7.1. Стороны освобождаются от ответственности за полное или частичное неисполнение " +
            "обязательств, если это вызвано обстоятельствами непреодолимой силы, возникшими " +
            "после заключения договора.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "7.2. Сторона, ссылающаяся на форс-мажорные обстоятельства, обязана письменно уведомить " +
            "другую Сторону в течение 5 (пяти) календарных дней.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  8. РАЗРЕШЕНИЕ СПОРОВ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("8. РАЗРЕШЕНИЕ СПОРОВ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "8.1. Все споры и разногласия разрешаются путём переговоров Сторон.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "8.2. При недостижении согласия спор передаётся на рассмотрение в Экономический суд " +
            "г. Минска.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  9. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("9. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "9.1. Настоящий договор вступает в силу с даты его подписания Сторонами и действует " +
            "до полного исполнения Сторонами своих обязательств.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "9.2. Договор составлен в двух экземплярах, имеющих одинаковую юридическую силу, " +
            "по одному для каждой из Сторон.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "9.3. Во всём, что не предусмотрено настоящим договором, Стороны руководствуются " +
            "действующим законодательством Республики Беларусь.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  10. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("10. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН")
            .Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14).SpacingAfter(8);

        var tt = doc.AddTable(7, 2);
        tt.Design = TableDesign.TableGrid;
        tt.Alignment = Alignment.center;
        tt.SetWidths(new float[] { 280f, 280f });
        void T(int r, int c, string v) =>
            tt.Rows[r].Cells[c].Paragraphs[0].Append(v).Font("Times New Roman").FontSize(10);
        T(0, 0, "ИСПОЛНИТЕЛЬ:\nООО «VentClean»");
        T(0, 1, "ЗАКАЗЧИК:\n" + (clientOrg?.Name ?? ""));
        T(1, 0, "УНП: 123456789");
        T(1, 1, "УНП: " + (clientOrg?.Unp ?? ""));
        T(2, 0, "Юр. адрес: 220000, г. Минск, ул. Примерная, д. 1");
        T(2, 1, "Юр. адрес: " + (clientOrg?.LegalAddress ?? ""));
        T(3, 0, "Р/с: BY12NBRB36009000000000000000");
        T(3, 1, "Р/с: " + clientBankAccount);
        T(4, 0, "Банк: «Белгазпромбанк» ОАО");
        T(4, 1, "Банк: " + (string.IsNullOrWhiteSpace(clientBankName) ? "—" : clientBankName));
        T(5, 0, "Тел.: +375 29 111-22-33");
        T(5, 1, "Тел.: " + clientPhone);
        T(6, 0, "E-mail: info@ventclean.by");
        T(6, 1, "E-mail: " + (clientUser?.Email ?? ""));
        doc.InsertTable(tt);

        // ════════════════════════════════════════════════
        //  11. ПОДПИСИ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("11. ПОДПИСИ СТОРОН")
            .Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14).SpacingAfter(6);

        var st = doc.AddTable(3, 2);
        st.Design = TableDesign.TableGrid;
        st.Alignment = Alignment.center;
        st.SetWidths(new float[] { 280f, 280f });
        st.Rows[0].Cells[0].Paragraphs[0].Append("ИСПОЛНИТЕЛЬ").Bold().Font("Times New Roman").FontSize(10);
        st.Rows[0].Cells[0].Paragraphs[0].Alignment = Alignment.center;
        st.Rows[0].Cells[1].Paragraphs[0].Append("ЗАКАЗЧИК").Bold().Font("Times New Roman").FontSize(10);
        st.Rows[0].Cells[1].Paragraphs[0].Alignment = Alignment.center;
        st.Rows[1].Cells[0].Paragraphs[0].Append("_________ / Иванов И.И. /").Font("Times New Roman").FontSize(10);
        st.Rows[1].Cells[1].Paragraphs[0].Append("_________ / _____________ /").Font("Times New Roman").FontSize(10);
        st.Rows[2].Cells[0].Paragraphs[0].Append("М.П.").Font("Times New Roman").FontSize(10);
        st.Rows[2].Cells[0].Paragraphs[0].Alignment = Alignment.center;
        st.Rows[2].Cells[1].Paragraphs[0].Append("М.П.").Font("Times New Roman").FontSize(10);
        st.Rows[2].Cells[1].Paragraphs[0].Alignment = Alignment.center;
        doc.InsertTable(st);

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
