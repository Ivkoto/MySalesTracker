using System.ComponentModel.DataAnnotations;

namespace MySalesTracker.Domain.Enums;
public enum PaymentMethod
{
    [Display(Name = "Кеш")]
    Cash = 1,

    [Display(Name = "POS")]
    Card = 2,

    [Display(Name = "Револют Лидия")]
    RevolutLidia = 3,

    [Display(Name = "Револют Ивайло")]
    RevolutIvaylo = 4
}
