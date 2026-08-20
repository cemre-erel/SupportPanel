namespace SupportPanel.Models
{
    public class UserProduct
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public bool IsProductManager { get; set; }

        public bool IsSupportSpecialist { get; set; }

        // Navigation Properties
        public User? User { get; set; }

        public Product? Product { get; set; }
    }
}
