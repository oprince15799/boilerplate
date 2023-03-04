using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Helpers
{
    public static class TypeHelper
    {
        public static bool IsAssignableToGenericType(this Type targetType, Type genericType) => genericType.IsAssignableFromGenericType(targetType);

        public static bool IsAssignableFromGenericType(this Type genericType, Type targetType)
        {
            var interfaceTypes = targetType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == genericType)
                return true;

            var baseType = targetType.BaseType;
            if (baseType == null) return false;

            return baseType.IsAssignableFromGenericType(genericType);
        }
    }
}