using System.ComponentModel.DataAnnotations;

namespace MySalesTracker.Domain.Enums;
public enum Brand
{
    [Display(Name = "ТОТЕМ")]
    Totem = 1,

    [Display(Name = "Керамика")]
    Ceramics = 2,

    [Display(Name = "Гора")]
    Candles = 3
}
