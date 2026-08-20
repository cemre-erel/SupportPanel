using System.ComponentModel.DataAnnotations;

namespace SupportPanel.Models
{
    public enum SlaCalculationMethod
    {
        [Display(Name = "7x24")]
        AroundTheClock = 0,

        [Display(Name = "Çalışma Saatleri")]
        WorkingHours = 1
    }
}
