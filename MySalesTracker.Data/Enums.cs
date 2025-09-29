namespace MySalesTracker.Data;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

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

public enum Brand
{
    [Display(Name = "ТОТЕМ")]
    Totem = 1,

    [Display(Name = "Керамика")]
    Ceramics = 2,

    [Display(Name = "Гора")]
    Candles = 3
}

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString());
        if (member.Length > 0)
        {
            var display = member[0].GetCustomAttribute<DisplayAttribute>();
            if (display != null)
            {
                return display.GetName() ?? value.ToString();
            }
        }
        return value.ToString();
    }
}
