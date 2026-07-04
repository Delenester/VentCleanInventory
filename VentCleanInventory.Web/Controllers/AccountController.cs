using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Models.Account;

namespace VentCleanInventory.Web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHomeByRole();
        }
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

     [HttpPost]
     [AllowAnonymous]
     [ValidateAntiForgeryToken]
     [EnableRateLimiting("login")]
     public async Task<IActionResult> Login(LoginViewModel model)
     {
         if (!ModelState.IsValid)
         {
             return View(model);
         }

         var user = await userManager.FindByNameAsync(model.Login);
         if (user != null && !user.IsApproved)
         {
             ModelState.AddModelError(string.Empty, "Ваша учётная запись ожидает одобрения администратора.");
             return View(model);
         }

          var result = await signInManager.PasswordSignInAsync(
              userName: model.Login,
              password: model.Password.Trim(),
              isPersistent: model.RememberMe,
              lockoutOnFailure: true);

         if (result.Succeeded)
         {
             logger.LogInformation("Пользователь вошел в систему: {Login}", model.Login);
             return RedirectToHomeByRole();
         }

         if (result.IsLockedOut)
         {
             ModelState.AddModelError(string.Empty, "Учётная запись заблокирована на 5 минут из-за слишком большого количества неудачных попыток входа.");
             return View(model);
         }

         ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
         return View(model);
     }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.AccountType != AccountType.Client && model.AccountType != AccountType.Supplier)
        {
            ModelState.AddModelError(nameof(model.AccountType), "Самостоятельная регистрация доступна только для клиентов и поставщиков. Работникам учётную запись выдаёт администратор.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким email уже зарегистрирован.");
            return View(model);
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            int? organizationId = null;
            string role;
            var orgType = model.AccountType == AccountType.Client
                ? OrganizationType.Client
                : OrganizationType.Supplier;

            if (await db.Organizations.AnyAsync(o => o.Type == orgType && o.Unp == model.Unp))
            {
                ModelState.AddModelError(nameof(model.Unp), "Организация с таким УНП уже зарегистрирована.");
                return View(model);
            }

            var org = new Organization
            {
                Type = orgType,
                Unp = model.Unp.Trim(),
                Name = model.OrganizationName.Trim(),
                LegalAddress = model.LegalAddress.Trim(),
                ContactInfo = model.ContactInfo?.Trim(),
            };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
            organizationId = org.Id;
            role = model.AccountType == AccountType.Client ? AppUserRole.Client : AppUserRole.Supplier;

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName.Trim(),
                IsApproved = true,
                AccountType = model.AccountType,
                OrganizationId = organizationId,
                ContractNumber = $"Д-{DateTime.Now:yyyyMMdd}-{org.Id:D4}",
                PhoneContact = model.PhoneContact?.Trim(),
                BankAccount = model.BankAccount?.Trim(),
                BankName = model.BankName?.Trim(),
                BIK = model.BIK?.Trim(),
            };

            var result = await userManager.CreateAsync(user, model.Password.Trim());
            if (!result.Succeeded)
            {
                await tx.RollbackAsync();
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, TranslateIdentityError(error.Code));
                }
                return View(model);
            }

            await userManager.AddToRoleAsync(user, role);
            await tx.CommitAsync();

            if (role == AppUserRole.Client)
            {
                try
                {
                    var managers = await userManager.GetUsersInRoleAsync(AppUserRole.Manager);
                    foreach (var m in managers)
                    {
                        db.Notifications.Add(new Data.Entities.Notification
                        {
                            UserId = m.Id,
                            Title = "Новая регистрация клиента",
                            Message = $"Зарегистрировался клиент: {user.FullName} ({user.Email}), организация: {model.OrganizationName}",
                            Link = "/Admin/Users",
                        });
                    }
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Не удалось создать уведомления для руководителей при регистрации клиента {Email}", model.Email);
                }
            }

            logger.LogInformation("Регистрация: {Email}, тип {Type}", model.Email, model.AccountType);

            await signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToHomeByRole();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterPending() => View();

    private string TranslateIdentityError(string errorCode)
    {
        return errorCode switch
        {
            "PasswordTooShort" => "Пароль слишком короткий. Минимальная длина: 8 символов.",
            "PasswordRequiresNonAlphanumeric" => "Пароль должен содержать хотя бы один специальный символ.",
            "PasswordRequiresDigit" => "Пароль должен содержать хотя бы одну цифру.",
            "PasswordRequiresUpper" => "Пароль должен содержать хотя бы одну заглавную букву.",
            "PasswordRequiresLower" => "Пароль должен содержать хотя бы одну строчную букву.",
            "DuplicateUserName" => "Пользователь с таким email уже существует.",
            "DuplicateEmail" => "Этот email уже используется.",
            "InvalidToken" => "Неверный или просроченный токен.",
            _ => $"Ошибка регистрации: {errorCode}"
        };
    }

    private IActionResult RedirectToHomeByRole()
    {
        if (User.IsInRole(AppUserRole.Admin))
            return RedirectToAction("Index", "AdminHome", new { Area = "Admin" });
        if (User.IsInRole(AppUserRole.Dispatcher))
            return RedirectToAction("Index", "DispatcherHome", new { Area = "Dispatcher" });
        if (User.IsInRole(AppUserRole.Master))
            return RedirectToAction("Index", "MasterHome", new { Area = "Master" });
        if (User.IsInRole(AppUserRole.Manager))
            return RedirectToAction("Index", "ManagerHome", new { Area = "Manager" });
        if (User.IsInRole(AppUserRole.Client))
            return RedirectToAction("Index", "ClientHome", new { Area = "Client" });
        if (User.IsInRole(AppUserRole.Supplier))
            return RedirectToAction("Index", "SupplierHome", new { Area = "Supplier" });

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out: {Login}", userName);
        return RedirectToAction(nameof(Login), "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
