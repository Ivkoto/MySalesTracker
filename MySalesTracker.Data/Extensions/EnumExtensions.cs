using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MySalesTracker.Data.Extensions;

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
