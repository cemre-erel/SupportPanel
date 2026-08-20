using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SupportPanel.Constants;
using SupportPanel.Controllers;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;
using SupportPanel.ViewModels;
using System.Security.Claims;
using Xunit;

namespace SupportPanel.Tests
{
    public class TicketControllerTests
    {
        private readonly Mock<ITicketService> _ticketService = new();
        private readonly Mock<ITenantService> _tenantService = new();
        private readonly Mock<IProductService> _productService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<IUserService> _userService = new();
        private readonly Mock<ITicketCommentService> _ticketCommentService = new();
        private readonly Mock<ITicketHistoryService> _ticketHistoryService = new();
        private readonly Mock<ITicketAttachmentService> _ticketAttachmentService = new();
        private readonly Mock<ISlaLevelService> _slaLevelService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<IUserProductService> _userProductService = new();
        private readonly Mock<ITenantProductService> _tenantProductService = new();
        private readonly Mock<ISlaPauseService> _slaPauseService = new();

        private TicketController CreateController(
            int userId,
            int tenantId,
            string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("TenantId", tenantId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            var controller = new TicketController(
                _ticketService.Object,
                _tenantService.Object,
                _productService.Object,
                _categoryService.Object,
                _userService.Object,
                _ticketCommentService.Object,
                _ticketHistoryService.Object,
                _ticketAttachmentService.Object,
                _slaLevelService.Object,
                _notificationService.Object,
                _userProductService.Object,
                _tenantProductService.Object,
                _slaPauseService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public async Task AddComment_DestekCevabiysa_IlkMudahaleTarihiAtanmali()
        {
            // Arrange
            const int userId = 10;
            const int tenantId = 1;
            const int ticketId = 25;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                AssignedUserId = userId,
                CreatedByUserId = 99,
                Title = "Test talebi",
                Status = TicketStatus.InReview,
                FirstResponseDate = null
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.SupportSpecialist);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "Talebiniz inceleniyor."
            };

            // Act
            var result = await controller.AddComment(model);

            // Assert
            Assert.NotNull(ticket.FirstResponseDate);

            _ticketService.Verify(
                x => x.UpdateAsync(ticket),
                Times.Once);

            _ticketHistoryService.Verify(
                x => x.AddAsync(
                    It.Is<TicketHistory>(h =>
                        h.TicketId == ticketId &&
                        h.Action == "İlk müdahale yapıldı")),
                Times.Once);

            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task AddComment_MusteriCevabiysa_TalepInReviewDurumunaDonmeli()
        {
            // Arrange
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 30;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                AssignedUserId = 50,
                Title = "Müşteri bekleniyor testi",
                Status = TicketStatus.WaitingCustomer
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.CompanyUser);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "İstenen bilgileri iletiyorum."
            };

            // Act
            var result = await controller.AddComment(model);

            // Assert
            Assert.Equal(TicketStatus.InReview, ticket.Status);

            _slaPauseService.Verify(
                x => x.StopPauseAsync(ticketId),
                Times.Once);

            _ticketService.Verify(
                x => x.UpdateAsync(ticket),
                Times.Once);

            _ticketHistoryService.Verify(
                x => x.AddAsync(
                    It.Is<TicketHistory>(h =>
                        h.TicketId == ticketId &&
                        h.OldValue == TicketStatus.WaitingCustomer &&
                        h.NewValue == TicketStatus.InReview)),
                Times.Once);

            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task AddComment_MusteriCevabi_IlkMudahaleSayilmamali()
        {
            // Arrange
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 40;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Title = "Müşteri cevabı testi",
                Status = TicketStatus.InReview,
                FirstResponseDate = null
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.CompanyUser);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "Ek bilgi iletiyorum."
            };

            // Act
            await controller.AddComment(model);

            // Assert
            Assert.Null(ticket.FirstResponseDate);

            _ticketHistoryService.Verify(
                x => x.AddAsync(
                    It.Is<TicketHistory>(h =>
                        h.Action == "İlk müdahale yapıldı")),
                Times.Never);
                }
        [Fact]
        public async Task AddComment_IlkMudahaleZatenVarsa_TarihDegismemeli()
        {
            const int userId = 10;
            const int tenantId = 1;
            const int ticketId = 50;

            var existingDate = new DateTime(2026, 8, 17, 10, 30, 0);

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                AssignedUserId = userId,
                CreatedByUserId = 99,
                Title = "İkinci cevap testi",
                Status = TicketStatus.InReview,
                FirstResponseDate = existingDate
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.SupportSpecialist);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "İkinci destek mesajı."
            };

            await controller.AddComment(model);

            Assert.Equal(existingDate, ticket.FirstResponseDate);

            _ticketHistoryService.Verify(
                x => x.AddAsync(
                    It.Is<TicketHistory>(h =>
                        h.Action == "İlk müdahale yapıldı")),
                Times.Never);
        }

        [Fact]
        public async Task AddComment_IptalEdilenTalebeMesajEklenmemeli()
        {
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 60;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Title = "İptal edilmiş talep",
                Status = TicketStatus.Cancelled
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.CompanyUser);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "Yeni mesaj"
            };

            var result = await controller.AddComment(model);

            _ticketCommentService.Verify(
                x => x.AddAsync(It.IsAny<TicketComment>()),
                Times.Never);

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Details", redirect.ActionName);
        }

        [Fact]
        public async Task AddComment_CokKisaMesajEklenmemeli()
        {
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 70;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Title = "Kısa mesaj testi",
                Status = TicketStatus.InReview
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.CompanyUser);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "a"
            };

            await controller.AddComment(model);

            _ticketCommentService.Verify(
                x => x.AddAsync(It.IsAny<TicketComment>()),
                Times.Never);
        }

        [Fact]
        public async Task AddComment_DestekUzmaniKendisineAtanmamisTalebeYorumYapamamali()
        {
            const int userId = 10;
            const int tenantId = 1;
            const int ticketId = 80;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                AssignedUserId = 999,
                CreatedByUserId = 20,
                ProductId = 5,
                Title = "Yetki testi",
                Status = TicketStatus.InReview
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _userProductService
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<UserProduct>
                {
            new UserProduct
            {
                UserId = userId,
                ProductId = 5,
                IsActive = true,
                IsSupportSpecialist = true
            }
                });

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.SupportSpecialist);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "Bu mesaj eklenmemeli."
            };

            var result = await controller.AddComment(model);

            Assert.IsType<ForbidResult>(result);

            _ticketCommentService.Verify(
                x => x.AddAsync(It.IsAny<TicketComment>()),
                Times.Never);
        }
        [Fact]
        public async Task AddComment_KapatilanTalebeMesajEklenmemeli()
        {
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 90;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Title = "Kapatılmış talep",
                Status = TicketStatus.Closed
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.CompanyUser);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "Yeni mesaj"
            };

            var result = await controller.AddComment(model);

            _ticketCommentService.Verify(
                x => x.AddAsync(It.IsAny<TicketComment>()),
                Times.Never);

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Details", redirect.ActionName);
        }

        [Fact]
        public async Task AddComment_MusteriWaitingCustomerDegilse_DurumDegismemeli()
        {
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 100;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                AssignedUserId = 50,
                Title = "Durum değişmeme testi",
                Status = TicketStatus.InReview
            };

            _ticketService
                .Setup(x => x.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var controller = CreateController(
                userId,
                tenantId,
                RoleNames.CompanyUser);

            var model = new AddCommentViewModel
            {
                TicketId = ticketId,
                Message = "Yeni bilgi gönderiyorum."
            };

            await controller.AddComment(model);

            Assert.Equal(TicketStatus.InReview, ticket.Status);

            _slaPauseService.Verify(
                x => x.StopPauseAsync(ticketId),
                Times.Never);
        }

        [Fact]
        public async Task Cancel_CozulduDurumundakiTalepIptalEdilebilmeli()
        {
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 110;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Title = "Çözülmüş talep",
                Status = TicketStatus.Resolved
            };

            _ticketService.Setup(x => x.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
            _userService.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            var controller = CreateController(userId, tenantId, RoleNames.CompanyUser);
            var result = await controller.Cancel(ticketId);

            _ticketService.Verify(x => x.CancelAsync(ticketId, userId), Times.Once);
            Assert.Equal("Details", Assert.IsType<RedirectToActionResult>(result).ActionName);
        }

        [Fact]
        public async Task Reopen_KapatilanTalepYenidenAcilmali()
        {
            const int userId = 20;
            const int tenantId = 1;
            const int ticketId = 120;

            var ticket = new Ticket
            {
                Id = ticketId,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Title = "Kapatılmış talep",
                Status = TicketStatus.Closed,
                AssignedUserId = 10
            };

            _ticketService.Setup(x => x.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
            _ticketService.Setup(x => x.ReopenAsync(ticketId, userId)).Callback(() =>
            {
                ticket.Status = TicketStatus.InReview;
            }).Returns(Task.CompletedTask);

            var controller = CreateController(userId, tenantId, RoleNames.CompanyUser);
            var result = await controller.Reopen(ticketId);

            _ticketService.Verify(x => x.ReopenAsync(ticketId, userId), Times.Once);
            Assert.Equal("Details", Assert.IsType<RedirectToActionResult>(result).ActionName);
        }
    }
}