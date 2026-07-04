# VentClean

Система автоматизации учёта вентиляционных работ, складского учёта и взаимодействия с клиентами.

## Роли

| Роль | Описание |
|---|---|
| Администратор | Справочники, пользователи, бэкап |
| Руководитель | Одобрение заявок, отчёты, клиенты |
| Диспетчер | Склад, приёмка, выдача, списание |
| Мастер | Выполнение работ, чек-листы, брак |
| Клиент | Создание заявок, договоры |
| Поставщик | Просмотр поставок |

## Технологии

- ASP.NET Core 8 (MVC, Areas)
- SQL Server LocalDB (2 БД: Identity + Domain)
- DocX (Word-договоры)
- Bootstrap 5 + Bootstrap Icons
- Rate limiting, HTTPS

## Запуск

```bash
dotnet run --project VentCleanInventory.Web
```

- HTTP: `http://localhost:5053`
- HTTPS: `https://localhost:7018`

БД создаются и заполняются автоматически при первом запуске.

## Структура

```
VentCleanInventory.Web/
├── Areas/
│   ├── Admin/          # Администрирование
│   ├── Manager/        # Руководитель
│   ├── Dispatcher/     # Диспетчер
│   ├── Master/         # Мастер
│   ├── Client/         # Клиент
│   └── Supplier/       # Поставщик
├── Data/               # EF Core, сущности, сидер
├── Services/           # StockService, WriteOffActService
├── Models/             # ViewModel'и
└── Views/              # Общие представления
```

## Лицензия

MIT
