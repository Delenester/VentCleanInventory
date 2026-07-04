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

        // City / Date (two-column table without borders)
        var headerTable = doc.AddTable(1, 2);
        headerTable.Design = TableDesign.None;
        headerTable.Alignment = Alignment.center;
        headerTable.SetWidths(new float[] { 280f, 280f });
        headerTable.Rows[0].Cells[0].Paragraphs[0]
            .Append("г. Минск").Font("Times New Roman").FontSize(fs);
        var monthName = RussianMonths[req.Date.Month - 1];
        headerTable.Rows[0].Cells[1].Paragraphs[0]
            .Append($"\"{req.Date:dd}\" {monthName} {req.Date:yyyy} г.").Font("Times New Roman").FontSize(fs);
        headerTable.Rows[0].Cells[1].Paragraphs[0].Alignment = Alignment.right;
        doc.InsertTable(headerTable);

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
            "  4.1.1. Выполнить Работы качественно, в объёме и в сроки, предусмотренные настоящим договором;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.2. Обеспечить соблюдение требований техники безопасности, пожарной безопасности и " +
            "охраны труда при выполнении Работ;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.3. Предоставить Заказчику акт сдачи-приёмки выполненных Работ по завершении Работ;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.4. Использовать при выполнении Работ исправное оборудование, инструмент и " +
            "качественные материалы, соответствующие требованиям технических нормативных правовых актов;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.5. Устранить выявленные недостатки за свой счёт в согласованные сроки;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.6. Не разглашать конфиденциальную информацию о деятельности Заказчика, " +
            "ставшую известной в ходе выполнения Работ;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.7. Обеспечить сохранность имущества Заказчика, находящегося на объекте;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.1.8. По окончании Работ передать Заказчику исполнительную документацию " +
            "(при её наличии).")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.2. Заказчик обязуется:")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.2.1. Обеспечить беспрепятственный доступ Исполнителя на объект для выполнения Работ, " +
            "в том числе к вентиляционному оборудованию;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.2.2. Принять выполненные Работы по акту сдачи-приёмки в порядке, предусмотренном " +
            "настоящим договором;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.2.3. Оплатить Работы в порядке и сроки, предусмотренные настоящим договором;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.2.4. Предоставить Исполнителю необходимую документацию и информацию, " +
            "относящуюся к объекту Работ (паспорта вентсистем, схемы, акты предыдущих обследований);")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.2.5. Назначить уполномоченного представителя для контроля за ходом Работ " +
            "и подписания акта сдачи-приёмки;")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "  4.2.6. Обеспечить отключение вентиляционного оборудования на время выполнения Работ " +
            "(при необходимости).")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.3. Заказчик вправе осуществлять контроль за ходом и качеством выполнения Работ " +
            "без вмешательства в оперативно-хозяйственную деятельность Исполнителя.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "4.4. Исполнитель вправе привлекать третьих лиц для выполнения Работ с письменного " +
            "согласия Заказчика, оставаясь ответственным перед Заказчиком за результат Работ.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  5. ПОРЯДОК СДАЧИ-ПРИЁМКИ РАБОТ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("5. ПОРЯДОК СДАЧИ-ПРИЁМКИ РАБОТ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "5.1. По завершении Работ Исполнитель направляет Заказчику подписанный со своей стороны " +
            "акт сдачи-приёмки выполненных Работ в двух экземплярах.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.2. Заказчик в течение 5 (пяти) рабочих дней со дня получения акта сдачи-приёмки " +
            "обязан подписать его и направить один экземпляр Исполнителю либо представить " +
            "мотивированный отказ от подписания.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.3. В случае мотивированного отказа Заказчика Стороны составляют двусторонний акт " +
            "с перечнем необходимых доработок и сроков их устранения.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "5.4. В случае неподписания Заказчиком акта сдачи-приёмки в установленный срок " +
            "и непредставления мотивированного отказа, Работы считаются принятыми Заказчиком " +
            "без замечаний.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  6. ГАРАНТИИ И ОТВЕТСТВЕННОСТЬ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("6. ГАРАНТИИ И ОТВЕТСТВЕННОСТЬ СТОРОН").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "6.1. Исполнитель гарантирует качество выполненных Работ в течение 12 (двенадцати) месяцев " +
            "с даты подписания акта сдачи-приёмки.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.2. Гарантия не распространяется на недостатки, возникшие вследствие: " +
            "а) нарушения Заказчиком правил эксплуатации вентиляционного оборудования; " +
            "б) внесения изменений в конструкцию вентсистем без согласования с Исполнителем; " +
            "в) действий третьих лиц; г) обстоятельств непреодолимой силы.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.3. В случае обнаружения недостатков в гарантийный период Исполнитель обязан устранить " +
            "их за свой счёт в течение 10 (десяти) рабочих дней с даты получения письменной " +
            "претензии от Заказчика.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.4. За нарушение срока начала или окончания Работ по вине Исполнителя он уплачивает " +
            "Заказчику пеню в размере 0,1% от стоимости невыполненных Работ за каждый день " +
            "просрочки, но не более 10% от стоимости Работ.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.5. За нарушение сроков оплаты Заказчик уплачивает Исполнителю пеню в размере 0,1% " +
            "от неоплаченной суммы за каждый день просрочки.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "6.6. Уплата пени не освобождает Стороны от исполнения обязательств в натуре.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  7. ИЗМЕНЕНИЕ И РАСТОРЖЕНИЕ ДОГОВОРА
        // ════════════════════════════════════════════════
        doc.InsertParagraph("7. ИЗМЕНЕНИЕ И РАСТОРЖЕНИЕ ДОГОВОРА").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "7.1. Все изменения и дополнения к настоящему договору действительны, если совершены " +
            "в письменной форме и подписаны уполномоченными представителями Сторон.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "7.2. Договор может быть расторгнут досрочно по письменному соглашению Сторон " +
            "либо в одностороннем порядке с письменным уведомлением не менее чем за 15 " +
            "(пятнадцать) календарных дней.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "7.3. Заказчик вправе отказаться от исполнения договора в одностороннем порядке " +
            "в случае существенного нарушения Исполнителем сроков выполнения Работ (более 15 дней).")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "7.4. Исполнитель вправе отказаться от исполнения договора в одностороннем порядке " +
            "в случае нарушения Заказчиком сроков оплаты более чем на 30 (тридцать) календарных дней.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  8. ФОРС-МАЖОР
        // ════════════════════════════════════════════════
        doc.InsertParagraph("8. ФОРС-МАЖОР").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "8.1. Стороны освобождаются от ответственности за полное или частичное неисполнение " +
            "обязательств, если это вызвано обстоятельствами непреодолимой силы (пожар, наводнение, " +
            "землетрясение, военные действия, эпидемии, решения государственных органов), возникшими " +
            "после заключения договора и которые Стороны не могли ни предвидеть, ни предотвратить.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "8.2. Сторона, ссылающаяся на форс-мажорные обстоятельства, обязана письменно уведомить " +
            "другую Сторону в течение 5 (пяти) календарных дней с момента их наступления. " +
            "Неуведомление лишает Сторону права ссылаться на эти обстоятельства.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "8.3. Если форс-мажорные обстоятельства продолжаются более 30 (тридцати) календарных дней, " +
            "любая из Сторон вправе расторгнуть договор в одностороннем порядке.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  9. КОНФИДЕНЦИАЛЬНОСТЬ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("9. КОНФИДЕНЦИАЛЬНОСТЬ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "9.1. Стороны обязуются сохранять конфиденциальность информации, полученной в ходе " +
            "исполнения настоящего договора, и не раскрывать её третьим лицам без письменного " +
            "согласия другой Стороны, за исключением случаев, предусмотренных законодательством.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "9.2. Обязательства по конфиденциальности действуют в течение 3 (трёх) лет после " +
            "окончания срока действия настоящего договора.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  10. РАЗРЕШЕНИЕ СПОРОВ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("10. РАЗРЕШЕНИЕ СПОРОВ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "10.1. Все споры и разногласия, возникающие из настоящего договора или в связи с ним, " +
            "разрешаются путём переговоров Сторон с соблюдением обязательного претензионного порядка.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "10.2. Сторона, получившая претензию, обязана рассмотреть её и направить ответ " +
            "в течение 10 (десяти) календарных дней с даты получения.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "10.3. При недостижении согласия спор передаётся на рассмотрение в Экономический суд " +
            "г. Минска в соответствии с законодательством Республики Беларусь.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  11. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ
        // ════════════════════════════════════════════════
        doc.InsertParagraph("11. ЗАКЛЮЧИТЕЛЬНЫЕ ПОЛОЖЕНИЯ").Bold().Font("Times New Roman").FontSize(13).SpacingBefore(14);
        doc.InsertParagraph(
            "11.1. Настоящий договор вступает в силу с даты его подписания Сторонами и действует " +
            "до полного исполнения Сторонами своих обязательств.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "11.2. Договор составлен в двух экземплярах, имеющих одинаковую юридическую силу, " +
            "по одному для каждой из Сторон.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "11.3. Во всём, что не предусмотрено настоящим договором, Стороны руководствуются " +
            "действующим законодательством Республики Беларусь.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "11.4. Стороны обязуются письменно уведомлять друг друга об изменении своих " +
            "юридических адресов, банковских реквизитов и других существенных данных " +
            "в течение 5 (пяти) рабочих дней.")
            .Font("Times New Roman").FontSize(fs);
        doc.InsertParagraph(
            "11.5. Электронная переписка Сторон, включая переписку по электронной почте, " +
            "признаётся Сторонами в качестве письменной формы, если она позволяет " +
            "достоверно установить отправителя.")
            .Font("Times New Roman").FontSize(fs);

        // ════════════════════════════════════════════════
        //  10/12. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН

        // ════════════════════════════════════════════════
        //  12. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("12. ЮРИДИЧЕСКИЕ АДРЕСА И РЕКВИЗИТЫ СТОРОН")
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
        //  13. ПОДПИСИ СТОРОН
        // ════════════════════════════════════════════════
        doc.InsertParagraph("13. ПОДПИСИ СТОРОН")
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

    [HttpGet]
    public async Task<IActionResult> ExportAct(int requestId)
    {
        var req = await db.StockTransactions.AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.WorkObject)
            .FirstOrDefaultAsync(t => t.Id == requestId);
        if (req == null) return NotFound();

        if (req.RequestStatusValue != RequestStatus.Completed)
            return BadRequest("Акт можно сформировать только после завершения работ.");

        var workLog = req.WorkLogId.HasValue
            ? await db.WorkLogs.AsNoTracking().FirstOrDefaultAsync(w => w.Id == req.WorkLogId.Value)
            : null;

        var clientOrg = req.Client;
        var contractNumber = req.ContractNumber ?? "";
        var date = DateTime.UtcNow;
        var cost = req.EstimatedCost ?? 0;
        var serviceType = GetServiceTypeFromNote(req.Note);
        var description = GetDescriptionFromNote(req.Note);
        var address = req.WorkObject?.Address ?? "";
        var objectName = req.WorkObject?.Name ?? "";

        var clientOrgId = clientOrg?.Id;
        var clientUser = clientOrgId.HasValue
            ? await userManager.Users.AsNoTracking()
                .Where(u => u.OrganizationId == clientOrgId.Value)
                .OrderBy(u => u.Id).FirstOrDefaultAsync()
            : null;
        var clientFullName = clientUser?.FullName ?? "_____________";
        var masterId = req.AssignedMasterId;
        var master = !string.IsNullOrWhiteSpace(masterId)
            ? await userManager.FindByIdAsync(masterId)
            : null;
        var masterName = master?.FullName ?? "_____________";

        var doc = DocX.Create($"Act-{contractNumber}.docx");
        doc.MarginLeft = 70f;
        doc.MarginRight = 50f;
        doc.MarginTop = 50f;
        doc.MarginBottom = 50f;
        var fs = 11f;

        doc.InsertParagraph("АКТ СДАЧИ-ПРИЁМКИ ВЫПОЛНЕННЫХ РАБОТ")
            .Font("Times New Roman").FontSize(16).Bold().Alignment = Alignment.center;
        if (!string.IsNullOrWhiteSpace(contractNumber))
            doc.InsertParagraph($"к договору № {contractNumber}")
                .Font("Times New Roman").FontSize(13).Alignment = Alignment.center;
        doc.InsertParagraph();

        var monthName = RussianMonths[date.Month - 1];
        var hl = doc.AddTable(1, 2);
        hl.Design = TableDesign.None;
        hl.Alignment = Alignment.center;
        hl.SetWidths(new float[] { 280f, 280f });
        hl.Rows[0].Cells[0].Paragraphs[0].Append("г. Минск").Font("Times New Roman").FontSize(fs);
        hl.Rows[0].Cells[1].Paragraphs[0].Append($"\"{date:dd}\" {monthName} {date:yyyy} г.").Font("Times New Roman").FontSize(fs);
        hl.Rows[0].Cells[1].Paragraphs[0].Alignment = Alignment.right;
        doc.InsertTable(hl);

        doc.InsertParagraph(
            "Мы, нижеподписавшиеся, представитель Исполнителя — ООО «VentClean» в лице " +
            "Директора Иванова И.И., действующего на основании Устава, " +
            "и представитель Заказчика — в лице руководителя, " +
            $"составили настоящий акт о том, что Исполнителем выполнены работы по договору № {contractNumber} " +
            $"на объекте: «{objectName}» ({address}).")
            .Font("Times New Roman").FontSize(fs).SpacingAfter(8);

        doc.InsertParagraph("Выполнены следующие работы:").Font("Times New Roman").FontSize(fs).SpacingAfter(6);

        var hasDucts = workLog?.Meters.HasValue == true && workLog.Meters.Value > 0;
        var hasGrids = workLog?.Grids.HasValue == true && workLog.Grids.Value > 0;
        var dataRows = 1 + (hasDucts ? 1 : 0) + (hasGrids ? 1 : 0);
        var totalRows = 1 + dataRows + 1; // header + data + total

        var table = doc.AddTable(totalRows, 5);
        table.Design = TableDesign.TableGrid;
        table.Alignment = Alignment.center;
        table.SetWidths(new float[] { 30f, 200f, 60f, 60f, 60f });

        string[] headers = { "№", "Наименование работ", "Ед.", "Кол-во", "Сумма" };
        for (int i = 0; i < headers.Length; i++)
        {
            table.Rows[0].Cells[i].Paragraphs[0].Append(headers[i]).Bold().Font("Times New Roman").FontSize(9);
            table.Rows[0].Cells[i].Paragraphs[0].Alignment = Alignment.center;
        }

        void FillRow(int row, string num, string name, string unit, string qty, string sum)
        {
            table.Rows[row].Cells[0].Paragraphs[0].Append(num).Font("Times New Roman").FontSize(fs);
            table.Rows[row].Cells[0].Paragraphs[0].Alignment = Alignment.center;
            table.Rows[row].Cells[1].Paragraphs[0].Append(name).Font("Times New Roman").FontSize(fs);
            table.Rows[row].Cells[2].Paragraphs[0].Append(unit).Font("Times New Roman").FontSize(fs);
            table.Rows[row].Cells[2].Paragraphs[0].Alignment = Alignment.center;
            table.Rows[row].Cells[3].Paragraphs[0].Append(qty).Font("Times New Roman").FontSize(fs);
            table.Rows[row].Cells[3].Paragraphs[0].Alignment = Alignment.center;
            table.Rows[row].Cells[4].Paragraphs[0].Append(sum).Font("Times New Roman").FontSize(fs);
            table.Rows[row].Cells[4].Paragraphs[0].Alignment = Alignment.center;
        }

        int r = 1;
        FillRow(r++, "1", $"{serviceType} — {description}", "м²", req.Area?.ToString("N1") ?? "—", "");

        if (hasDucts)
            FillRow(r++, "2", "Монтаж/обслуживание воздуховодов", "пог. м", workLog!.Meters!.Value.ToString("N1"), "");
        if (hasGrids)
            FillRow(r++, hasDucts ? "3" : "2", "Установка/замена решёток", "шт.", workLog!.Grids!.Value.ToString(), "");

        FillRow(r, "", "ИТОГО:", "", "", cost.ToString("N2"));
        table.Rows[r].Cells[1].Paragraphs[0].Bold();
        table.Rows[r].Cells[4].Paragraphs[0].Bold();

        doc.InsertTable(table);

        doc.InsertParagraph(
            $"Всего стоимость выполненных работ составляет: {cost:N2} ({NumberToWords(cost)}) " +
            "без НДС (Исполнитель не является плательщиком НДС).")
            .Font("Times New Roman").FontSize(fs).SpacingBefore(8);

        doc.InsertParagraph(
            "Работы выполнены в полном объёме, в установленные сроки и с надлежащим качеством. " +
            "Стороны претензий друг к другу не имеют.")
            .Font("Times New Roman").FontSize(fs).SpacingBefore(8);

        doc.InsertParagraph(
            "Настоящий акт составлен в двух экземплярах, имеющих одинаковую юридическую силу, " +
            "по одному для каждой из Сторон.")
            .Font("Times New Roman").FontSize(fs).SpacingBefore(4);

        doc.InsertParagraph().SpacingBefore(12);

        // Signatures table
        var st = doc.AddTable(3, 2);
        st.Design = TableDesign.TableGrid;
        st.Alignment = Alignment.center;
        st.SetWidths(new float[] { 280f, 280f });
        st.Rows[0].Cells[0].Paragraphs[0].Append("ИСПОЛНИТЕЛЬ").Bold().Font("Times New Roman").FontSize(10);
        st.Rows[0].Cells[0].Paragraphs[0].Alignment = Alignment.center;
        st.Rows[0].Cells[1].Paragraphs[0].Append("ЗАКАЗЧИК").Bold().Font("Times New Roman").FontSize(10);
        st.Rows[0].Cells[1].Paragraphs[0].Alignment = Alignment.center;
        st.Rows[1].Cells[0].Paragraphs[0].Append("_________ / Иванов И.И. /").Font("Times New Roman").FontSize(10);
        st.Rows[1].Cells[1].Paragraphs[0].Append($"_________ / {clientFullName} /").Font("Times New Roman").FontSize(10);
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
            $"Акт_{contractNumber}.docx");
    }

    private static readonly string[] RussianMonths =
        ["января", "февраля", "марта", "апреля", "мая", "июня",
         "июля", "августа", "сентября", "октября", "ноября", "декабря"];

    private static string RubleWord(long n)
    {
        n %= 100;
        if (n is >= 11 and <= 19) return "рублей";
        n %= 10;
        if (n == 1) return "рубль";
        if (n is >= 2 and <= 4) return "рубля";
        return "рублей";
    }

    private static string KopeckWord(long n)
    {
        n %= 100;
        if (n is >= 11 and <= 19) return "копеек";
        n %= 10;
        if (n == 1) return "копейка";
        if (n is >= 2 and <= 4) return "копейки";
        return "копеек";
    }

    private static string NumberToWords(decimal number)
    {
        if (number == 0) return "ноль рублей 00 копеек";
        var wholePart = (long)Math.Floor(number);
        var decimalPart = (long)Math.Round((number - wholePart) * 100);
        return $"{NumberToWordsRus(wholePart)} {RubleWord(wholePart)} {decimalPart:D2} {KopeckWord(decimalPart)}";
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
