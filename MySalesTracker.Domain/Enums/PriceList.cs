using System.ComponentModel.DataAnnotations;

namespace MySalesTracker.Domain.Enums;

public enum PriceList
{
    [Display(Name = "BG")]
    BG = 0,

    [Display(Name = "EU")]
    EU = 1
}
