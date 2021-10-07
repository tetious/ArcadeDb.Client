using System.Reflection;

namespace ArcadeDb.Client.Extras;

public static class TypeExtensions
{
    public static bool IsInitOnly(this PropertyInfo propertyInfo)
    {
        var setMethod = propertyInfo.SetMethod;
        if (setMethod == null) return false;
        var isExternalInitType = typeof(System.Runtime.CompilerServices.IsExternalInit);
        return setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(isExternalInitType);
    }
}
