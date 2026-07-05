using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xceed.Words.NET;
using Xceed.Document.NET;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = $"{AppUserRole.Dispatcher},{AppUserRole.Supplier},{AppUserRole.Admin},{AppUserRole.Manager}")]
public class SupplyContractController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Export(int id)
    {
        var req = await db.SupplyRequests.AsNoTracking()
            .Include(r => r.Organization)
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (req is null) return NotFound();
        if (req.Status is SupplyRequestStatus.New or SupplyRequestStatus.Cancelled)
            return BadRequest("Договор можно сформировать только после отправки запроса поставщику.");

        var supplier = req.Organization;
        var docNumber = req.Number;
        var date = req.CreatedAt;

        // Get supplier user details for bank info
        var supplierUser = await userManager.Users.AsNoTracking()
            .Where(u => u.OrganizationId == supplier.Id)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        var doc = DocX.Create($"Договор_{docNumber}.docx");
        doc.MarginLeft = 70f;
        doc.MarginRight = 50f;
        doc.MarginTop = 50f;
        doc.MarginBottom = 50f;
        var fs = 11f;

        doc.InsertParagraph("ДОГОВОР ПОСТАВКИ")
            .Font("Times New Roman").FontSize(16).Bold().Alignment = Alignment.center;
        doc.InsertParagraph($"№ {docNumber}")
            .Font("Times New Roman").FontSize(13).Alignment = Alignment.center;
        doc.InsertParagraph();

        var monthName = RussianMonths[date.Month - 1];
        var hl = doc.AddTable(1, 2);
        hl.Design = TableDesign.None;
        hl.Alignment = Alignment.center;
        hl.SetWidths(new float[] { 280f, 280f });
        hl.Rows[0].Cells[0].Paragraphs[0]
            .Append("г. Минск").Font("Times New Roman").FontSize(fs);
        hl.Rows[0].Cells[1].Paragraphs[0]
            .Append($"\"{date:dd}\" {monthName} {date:yyyy} г.").Font("Times New Roman").FontSize(fs);
        hl.Rows[0].Cells[1].Paragraphs[0].Alignment = Alignment.right;
        doc.InsertTable(hl);

        doc.InsertParagraph(
            "Общество с ограниченной ответственностью «VentClean» (УНП 123456789), " +
            "именуемое в дальнейшем «Покупатель», в лице Директора Иванова И.И., " +
            "действующего на основании Устава, с одной стороны, и")
            .Font("Times New Roman").FontSize(fs).SpacingAfter(4);
        doc.InsertParagraph(
            $"{supplier?.Name ?? "Поставщик"} (УНП {supplier?.Unp ?? ""}), " +
            "именуемое в дальнейшем «Поставщик», в лице руководителя, действующего на основании Устава, " +
            "с другой стороны, заключили настоящий договор о нижеследующем:")
            .Font("Times New Roman").FontSize(fs).SpacingAfter(12);

        doc.InsertParagraph("1. ПРЕДМЕТ ДОГОВОРА").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "1.1. Поставщик обязуется передать в собственность Покупателю, а Покупатель обязуется " +
            "принять и оплатить товар (далее — Товар) в соответствии с условиями настоящего договора.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "1.2. Наименование, количество, цена и общая стоимость Товара указываются в спецификации " +
            "(Приложение № 1), являющейся неотъемлемой частью настоящего договора.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "1.3. Поставка Товара осуществляется отдельными партиями на основании заявок Покупателя.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("2. ЦЕНА И ПОРЯДОК РАСЧЁТОВ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        var totalCost = req.Items.Sum(i => i.Quantity);
        doc.InsertParagraph(
            "2.1. Цена на Товар устанавливается в белорусских рублях и указывается в спецификации.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "2.2. Общая сумма настоящего договора определяется как сумма стоимости всех поставленных " +
            "партий Товара за период действия договора.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "2.3. Оплата производится в течение 10 (десяти) банковских дней с даты поставки Товара " +
            "на склад Покупателя на основании товарной накладной.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "2.4. Цена Товара является фиксированной и изменению не подлежит без письменного " +
            "соглашения Сторон.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("3. КАЧЕСТВО И КОМПЛЕКТНОСТЬ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "3.1. Качество Товара должно соответствовать требованиям технических нормативных правовых " +
            "актов, действующих на территории Республики Беларусь.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "3.2. Поставщик гарантирует, что Товар является новым, не был в употреблении, " +
            "не имеет дефектов, связанных с материалами или качеством изготовления.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "3.3. На Товар предоставляется гарантия изготовителя. Срок гарантии указывается " +
            "в гарантийных талонах или технической документации на Товар.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("4. ПОРЯДОК ПОСТАВКИ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "4.1. Поставка Товара осуществляется на склад Покупателя по адресу: " +
            "г. Минск, ул. Заводская, 1 (центральный склад).")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.2. Срок поставки — в течение 5 (пяти) рабочих дней с даты подтверждения " +
            "заявки Поставщиком.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.3. Право собственности на Товар переходит к Покупателю с момента передачи " +
            "Товара на складе Покупателя и подписания товарной накладной.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.4. Риск случайной гибели или повреждения Товара несёт Поставщик до момента " +
            "передачи Товара Покупателю.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("5. ОТВЕТСТВЕННОСТЬ СТОРОН").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "5.1. За нарушение сроков поставки Поставщик уплачивает Покупателю пеню в размере " +
            "0,1% от стоимости непоставленного Товара за каждый день просрочки, но не более 10%.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.2. За нарушение сроков оплаты Покупатель уплачивает Поставщику пеню в размере " +
            "0,1% от неоплаченной суммы за каждый день просрочки.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.3. Уплата пени не освобождает Стороны от исполнения обязательств в натуре.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("6. ФОРС-МАЖОР").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "6.1. Стороны освобождаются от ответственности за неисполнение обязательств, " +
            "вызванное обстоятельствами непреодолимой силы (пожар, наводнение, военные действия, " +
            "эпидемии, решения государственных органов).")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.2. Сторона, ссылающаяся на форс-мажор, обязана письменно уведомить другую Сторону " +
            "в течение 5 (пяти) календарных дней.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("7. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "7.1. Настоящий договор вступает в силу с даты его подписания и действует " +
            "до 31 декабря текущего календарного года.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "7.2. Все споры разрешаются путём переговоров, а при недостижении согласия — " +
            "в Экономическом суде г. Минска.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "7.3. Договор составлен в двух экземплярах, имеющих одинаковую юридическую силу.")
            .Font("Times New Roman").FontSize(fs);

        doc.InsertParagraph("Приложение № 1").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(16);
        doc.InsertParagraph("СПЕЦИФИКАЦИЯ").Bold().Font("Times New Roman").FontSize(13).Alignment = Alignment.center;
        doc.InsertParagraph($"к договору поставки № {docNumber}")
            .Font("Times New Roman").FontSize(fs).Alignment = Alignment.center;
        doc.InsertParagraph();

        var headerRow = 1 + req.Items.Count + 1;
        var tt = doc.AddTable(headerRow, 5);
        tt.Design = TableDesign.TableGrid;
        tt.Alignment = Alignment.center;
        tt.SetWidths(new float[] { 30f, 210f, 60f, 60f, 90f });

        string[] th = { "№", "Наименование", "Ед.", "Кол-во", "Цена" };
        for (int i = 0; i < th.Length; i++)
        {
            tt.Rows[0].Cells[i].Paragraphs[0].Append(th[i]).Bold().Font("Times New Roman").FontSize(10);
            tt.Rows[0].Cells[i].Paragraphs[0].Alignment = Alignment.center;
        }

        int ri = 1;
        foreach (var item in req.Items)
        {
            tt.Rows[ri].Cells[0].Paragraphs[0].Append(ri.ToString()).Font("Times New Roman").FontSize(fs);
            tt.Rows[ri].Cells[0].Paragraphs[0].Alignment = Alignment.center;
            tt.Rows[ri].Cells[1].Paragraphs[0].Append(item.Nomenclature?.Name ?? "").Font("Times New Roman").FontSize(fs);
            tt.Rows[ri].Cells[2].Paragraphs[0].Append(item.Nomenclature?.Unit ?? "").Font("Times New Roman").FontSize(fs);
            tt.Rows[ri].Cells[2].Paragraphs[0].Alignment = Alignment.center;
            tt.Rows[ri].Cells[3].Paragraphs[0].Append(item.Quantity.ToString("N1")).Font("Times New Roman").FontSize(fs);
            tt.Rows[ri].Cells[3].Paragraphs[0].Alignment = Alignment.center;
            tt.Rows[ri].Cells[4].Paragraphs[0].Append(item.UnitPrice?.ToString("N2") ?? "—").Font("Times New Roman").FontSize(fs);
            tt.Rows[ri].Cells[4].Paragraphs[0].Alignment = Alignment.center;
            ri++;
        }

        tt.Rows[ri].Cells[3].Paragraphs[0].Append($"Итого: {req.Items.Sum(i => i.Quantity):N1}").Bold().Font("Times New Roman").FontSize(fs);
        doc.InsertTable(tt);

        doc.InsertParagraph().SpacingBefore(12);

        var addr = doc.AddTable(7, 2);
        addr.Design = TableDesign.TableGrid;
        addr.Alignment = Alignment.center;
        addr.SetWidths(new float[] { 280f, 280f });
        void A(int r, int c, string v) =>
            addr.Rows[r].Cells[c].Paragraphs[0].Append(v).Font("Times New Roman").FontSize(10);
        A(0, 0, "ПОКУПАТЕЛЬ:\nООО «VentClean»");
        A(0, 1, "ПОСТАВЩИК:\n" + (supplier?.Name ?? ""));
        A(1, 0, "УНП: 123456789");
        A(1, 1, "УНП: " + (supplier?.Unp ?? ""));
        A(2, 0, "Юр. адрес: 220000, г. Минск, ул. Примерная, д. 1");
        A(2, 1, "Юр. адрес: " + (supplier?.LegalAddress ?? ""));
        A(3, 0, "Р/с: BY12NBRB36009000000000000000");
        A(3, 1, "Р/с: " + (supplierUser?.BankAccount ?? "_________________"));
        A(4, 0, "Банк: «Белгазпромбанк» ОАО");
        A(4, 1, "Банк: " + (string.IsNullOrWhiteSpace(supplierUser?.BankName) ? "_________________" : supplierUser.BankName));
        A(5, 0, "Тел.: +375 29 111-22-33");
        A(5, 1, "Тел.: " + (supplierUser?.PhoneNumber ?? "_________________"));
        A(6, 0, "E-mail: info@ventclean.by");
        A(6, 1, "E-mail: " + (supplierUser?.Email ?? "_________________"));
        doc.InsertTable(addr);

        doc.InsertParagraph("ПОДПИСИ СТОРОН")
            .Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14).SpacingAfter(6);

        var st = doc.AddTable(3, 2);
        st.Design = TableDesign.TableGrid;
        st.Alignment = Alignment.center;
        st.SetWidths(new float[] { 280f, 280f });
        st.Rows[0].Cells[0].Paragraphs[0].Append("ПОКУПАТЕЛЬ").Bold().Font("Times New Roman").FontSize(10);
        st.Rows[0].Cells[0].Paragraphs[0].Alignment = Alignment.center;
        st.Rows[0].Cells[1].Paragraphs[0].Append("ПОСТАВЩИК").Bold().Font("Times New Roman").FontSize(10);
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

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"Договор_{docNumber}.docx");
    }

    private static readonly string[] RussianMonths =
        ["января", "февраля", "марта", "апреля", "мая", "июня",
         "июля", "августа", "сентября", "октября", "ноября", "декабря"];
}
