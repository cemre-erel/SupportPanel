using Moq;
using SupportPanel.Interfaces;
using SupportPanel.Models;
using SupportPanel.Services;

namespace SupportPanel.Tests
{
    public class TicketServiceTests
    {
        private readonly Mock<ITicketRepository> _repository = new();
        private readonly Mock<ITicketHistoryService> _history = new();

        private TicketService CreateService() => new(_repository.Object, _history.Object);

        [Fact]
        public async Task CancelAsync_MusteriBekleniyorIptalEdilebilmeli()
        {
            var ticket = new Ticket { Id = 1, Status = TicketStatus.WaitingCustomer };
            _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(ticket);

            await CreateService().CancelAsync(1, 5);

            _repository.Verify(x => x.ChangeStatusAsync(1, TicketStatus.Cancelled), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_KapatilmisTalepIptalEdilememeli()
        {
            var ticket = new Ticket { Id = 1, Status = TicketStatus.Closed };
            _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(ticket);

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CancelAsync(1, 5));
            _repository.Verify(x => x.ChangeStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReopenAsync_AtananKapaliTalepIncelemedeAcilmali()
        {
            var ticket = new Ticket
            {
                Id = 1,
                Status = TicketStatus.Closed,
                AssignedUserId = 8,
                SlaStartedDate = DateTime.Now.AddHours(-2)
            };
            _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(ticket);

            await CreateService().ReopenAsync(1, 5);

            _repository.Verify(x => x.ChangeStatusAsync(1, TicketStatus.InReview), Times.Once);
            _history.Verify(x => x.AddAsync(It.Is<TicketHistory>(h =>
                h.Action == "Talep yeniden açıldı" && h.NewValue == TicketStatus.InReview)), Times.Once);
        }

        [Fact]
        public async Task ReopenAsync_FirmaIciKapatilanTalepYeniOlmalı()
        {
            var ticket = new Ticket { Id = 2, Status = TicketStatus.Closed };
            _repository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(ticket);

            await CreateService().ReopenAsync(2, 5);

            _repository.Verify(x => x.ChangeStatusAsync(2, TicketStatus.New), Times.Once);
        }

        [Theory]
        [InlineData(null, false, TicketStatus.New)]
        [InlineData(null, true, TicketStatus.SupportQueue)]
        [InlineData(7, true, TicketStatus.InReview)]
        public void GetReopenTarget_KapanisOncesiAkisaGoreDonmeli(int? assignedUserId, bool slaStarted, string expected)
        {
            DateTime? slaStartedDate = slaStarted ? DateTime.Now : null;
            Assert.Equal(expected, TicketStatus.GetReopenTarget(assignedUserId, slaStartedDate));
        }
    }
}
