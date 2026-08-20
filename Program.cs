using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SupportPanel.Constants;
using SupportPanel.Data;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;
namespace SupportPanel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
            });
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
                options.ValueLengthLimit = 25 * 1024 * 1024;
            });
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.AccessDeniedPath = "/Auth/AccessDenied";

                    options.Cookie.Name = "SupportPanel.Auth";

                    options.ExpireTimeSpan = TimeSpan.FromHours(8);

                    options.SlidingExpiration = true;

                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!int.TryParse(userIdValue, out var userId))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await db.Users
                            .AsNoTracking()
                            .IgnoreQueryFilters()
                            .Include(item => item.Tenant)
                            .Include(item => item.Role)
                            .FirstOrDefaultAsync(item => item.Id == userId);

                        var roleClaim = context.Principal?.FindFirstValue(ClaimTypes.Role);
                        var tenantClaim = context.Principal?.FindFirstValue("TenantId");
                        var currentTenantClaim = user?.TenantId?.ToString();
                        var identityChanged = user != null &&
                            (!string.Equals(roleClaim, user.Role?.Name, StringComparison.Ordinal) ||
                             !string.Equals(tenantClaim, currentTenantClaim, StringComparison.Ordinal));

                        if (user == null || !user.IsActive || user.Tenant?.IsActive == false || identityChanged)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    };
                });
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<ITenantRepository, TenantRepository>();
            builder.Services.AddScoped<ITenantService, TenantService>();

            builder.Services.AddScoped<ISlaLevelRepository, SlaLevelRepository>();
            builder.Services.AddScoped<ISlaLevelService, SlaLevelService>();

            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<ITicketService, TicketService>();

            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IProductService, ProductService>();

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddScoped<ITenantProductRepository, TenantProductRepository>();
            builder.Services.AddScoped<ITenantProductService, TenantProductService>();

            builder.Services.AddScoped<IUserProductRepository, UserProductRepository>();
            builder.Services.AddScoped<IUserProductService, UserProductService>();

            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IRoleService, RoleService>();

            builder.Services.AddScoped<ITicketCommentRepository, TicketCommentRepository>();
            builder.Services.AddScoped<ITicketCommentService, TicketCommentService>();

            builder.Services.AddScoped<ITicketAttachmentRepository, TicketAttachmentRepository>();
            builder.Services.AddScoped<ITicketAttachmentService, TicketAttachmentService>();

            builder.Services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
            builder.Services.AddScoped<ITicketHistoryService, TicketHistoryService>();

            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

            builder.Services.AddScoped<ISlaPauseRepository, SlaPauseRepository>();
            builder.Services.AddScoped<ISlaPauseService, SlaPauseService>();

            builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                if (app.Environment.IsDevelopment())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.MigrateAsync();
                }

                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                var slaLevelService = scope.ServiceProvider.GetRequiredService<ISlaLevelService>();

                await SeedInitialAdminAsync(app.Configuration, scope.ServiceProvider);

                var tenants = await tenantService.GetAllAsync();

                foreach (var tenant in tenants)
                {
                    await slaLevelService.EnsureDefaultLevelsAsync(tenant.Id);
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.Use(async (context, next) =>
            {
                try
                {
                    // Talep ekleri yalnızca yetkili indirme işlemi üzerinden sunulur.
                    if (context.Request.Path.StartsWithSegments("/uploads"))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    if (context.Request.Method == HttpMethods.Post &&
                        (context.Request.Path.StartsWithSegments("/Ticket/Create") ||
                         context.Request.Path.StartsWithSegments("/Ticket/UploadAttachment")) &&
                        context.Request.ContentLength.HasValue &&
                        context.Request.ContentLength.Value > 25 * 1024 * 1024)
                    {
                        context.Response.Redirect("/Ticket/Create?dosya=1");
                        return;
                    }

                    await next();
                }
                catch (Microsoft.AspNetCore.Http.BadHttpRequestException)
                {
                    if (context.Request.Method == HttpMethods.Post &&
                        (context.Request.Path.StartsWithSegments("/Ticket/Create") ||
                         context.Request.Path.StartsWithSegments("/Ticket/UploadAttachment")))
                    {
                        context.Response.Redirect("/Ticket/Create?dosya=1");
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                        await context.Response.WriteAsync("İstek çok büyük.");
                    }
                }
            });

            app.UseAuthentication();
            app.UseAuthorization();
           
            app.Use(async (context, next) =>
            {
                var user = context.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();

                    // 1. Kullanıcının SystemAdmin olup olmadığını role kontrolünden alıyoruz
                    dbContext.IsSystemAdmin = user.IsInRole(RoleNames.SystemAdmin);

                    // 2. Cookie içindeki "TenantId" claim'ini okuyoruz
                    var tenantClaim = user.FindFirst("TenantId")?.Value;

                    if (int.TryParse(tenantClaim, out var tenantId))
                    {
                        dbContext.CurrentTenantId = tenantId;
                    }
                }

                await next();
            });

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }

        /// <summary>
        /// Boş bir veritabanına tek bir SystemAdmin kullanıcısı ekler.
        /// Veritabanında en az bir kullanıcı varsa hiçbir şey yapmaz; mevcut şifrelere asla dokunmaz.
        /// </summary>
        private static async Task SeedInitialAdminAsync(
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<AppDbContext>();
            var roleService = serviceProvider.GetRequiredService<IRoleService>();
            var userService = serviceProvider.GetRequiredService<IUserService>();

            var hasAnyUser = await db.Users.IgnoreQueryFilters().AnyAsync();
            if (hasAnyUser)
            {
                return;
            }

            var password = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Veritabanında hiç kullanıcı yok, ancak ilk yönetici şifresi tanımlanmamış. " +
                    "Şifreyi ayarlayıp uygulamayı yeniden başlatın:\r\n" +
                    "    dotnet user-secrets set \"SeedAdmin:Password\" \"<guclu-bir-sifre>\"\r\n" +
                    "Alternatif olarak SeedAdmin__Password ortam değişkenini kullanabilirsiniz.");
            }

            var adminRole = (await roleService.GetAllAsync())
                .FirstOrDefault(r => r.Name == RoleNames.SystemAdmin);

            if (adminRole == null)
            {
                throw new InvalidOperationException(
                    $"'{RoleNames.SystemAdmin}' rolü bulunamadı. Migration'ların uygulandığından emin olun " +
                    "(dotnet ef database update).");
            }

            var email = configuration["SeedAdmin:Email"] ?? "admin@localhost";

            await userService.AddAsync(new User
            {
                FirstName = "Sistem",
                LastName = "Yöneticisi",
                Username = configuration["SeedAdmin:Username"] ?? "admin",
                Email = email,
                PasswordHash = password,
                IsActive = true,
                TenantId = null,
                RoleId = adminRole.Id
            });
        }
    }
}
