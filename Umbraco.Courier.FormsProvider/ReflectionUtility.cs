﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Umbraco.Courier.FormsProvider
{
    internal static class ReflectionUtility
    {
        private static Func<TInstance, TValue> GetPropertyGetter<TInstance, TValue>(PropertyInfo property)
        {
            var type = typeof(TInstance);

            var getMethod = property.GetMethod;
            if (getMethod == null)
                throw new InvalidOperationException($"Property {type}.{property.Name} : {property.PropertyType} does not have a getter.");

            var exprThis = Expression.Parameter(type, "this");
            var exprCall = Expression.Call(exprThis, getMethod);
            var expr = Expression.Lambda<Func<TInstance, TValue>>(exprCall, exprThis);
            return expr.Compile();
        }

        private static Action<TInstance, TValue> GetPropertySetter<TInstance, TValue>(PropertyInfo property)
        {
            var type = typeof(TInstance);

            var setMethod = property.SetMethod;
            if (setMethod == null)
                throw new InvalidOperationException($"Property {type}.{property.Name} : {property.PropertyType} does not have a setter.");

            var exprThis = Expression.Parameter(type, "this");
            var exprArg0 = Expression.Parameter(typeof(TValue), "value");
            var exprCall = Expression.Call(exprThis, setMethod, exprArg0);
            var expr = Expression.Lambda<Action<TInstance, TValue>>(exprCall, exprThis, exprArg0);
            return expr.Compile();
        }

        public static Func<TInstance, TValue> GetPropertyGetter<TInstance, TValue>(string propertyName)
        {
            var type = typeof(TInstance);
            var type0 = typeof(TValue);
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || property.PropertyType != type0)
                throw new InvalidOperationException($"Could not get property {type}.{propertyName} : {type0}.");
            return GetPropertyGetter<TInstance, TValue>(property);
        }

        public static Action<TInstance, TValue> GetPropertySetter<TInstance, TValue>(string propertyName)
        {
            var type = typeof(TInstance);
            var type0 = typeof(TValue);
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || property.PropertyType != type0)
                throw new InvalidOperationException($"Could not get property {type}.{propertyName} : {type0}.");
            return GetPropertySetter<TInstance, TValue>(property);
        }

        public static void GetPropertyGetterSetter<TInstance, TValue>(string propertyName, out Func<TInstance, TValue> getter, out Action<TInstance, TValue> setter)
        {
            var type = typeof(TInstance);
            var type0 = typeof(TValue);
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || property.PropertyType != type0)
                throw new InvalidOperationException($"Could not get property {type}.{propertyName} : {type0}.");
            getter = GetPropertyGetter<TInstance, TValue>(property);
            setter = GetPropertySetter<TInstance, TValue>(property);
        }

        public static Func<TInstance> GetCtor<TInstance>()
        {
            var type = typeof(TInstance);

            // get the constructor infos
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);

            if (ctor == null)
                throw new InvalidOperationException($"Could not find constructor {type}.ctor().");

            var exprNew = Expression.New(ctor);
            var expr = Expression.Lambda<Func<TInstance>>(exprNew);
            return expr.Compile();
        }

        public static Func<TArg0, TInstance> GetCtor<TInstance, TArg0>()
        {
            var type = typeof(TInstance);
            var type0 = typeof(TArg0);

            // get the constructor infos
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, new[] { type0 }, null);

            if (ctor == null)
                throw new InvalidOperationException($"Could not find constructor {type}.ctor({type0}).");

            var exprArgs = ctor.GetParameters().Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();
            // ReSharper disable once CoVariantArrayConversion
            var exprNew = Expression.New(ctor, exprArgs);
            var expr = Expression.Lambda<Func<TArg0, TInstance>>(exprNew, exprArgs);
            return expr.Compile();
        }

        public static Func<TArg0, TArg1, TInstance> GetCtor<TInstance, TArg0, TArg1>()
        {
            var type = typeof(TInstance);
            var type0 = typeof(TArg0);
            var type1 = typeof(TArg1);

            // get the constructor infos
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, new[] { type0, type1 }, null);

            if (ctor == null)
                throw new InvalidOperationException($"Could not find constructor {type}.ctor({type0}, {type1}).");

            var exprArgs = ctor.GetParameters().Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();
            // ReSharper disable once CoVariantArrayConversion
            var exprNew = Expression.New(ctor, exprArgs);
            var expr = Expression.Lambda<Func<TArg0, TArg1, TInstance>>(exprNew, exprArgs);
            return expr.Compile();
        }

        public static TMethod GetMethod<TMethod>(MethodInfo method)
        {
            var type = method.DeclaringType;

            GetMethodParms<TMethod>(out var parameterTypes, out var returnType);
            return GetStaticMethod<TMethod>(method, method.Name, type, parameterTypes, returnType);
        }

        public static TMethod GetMethod<TInstance, TMethod>(MethodInfo method)
        {
            var type = method.DeclaringType;

            GetMethodParms<TInstance, TMethod>(out var parameterTypes, out var returnType);
            return GetMethod<TMethod>(method, method.Name, type, parameterTypes, returnType);
        }

        public static TMethod GetMethod<TInstance, TMethod>(string methodName)
        {
            var type = typeof(TInstance);

            GetMethodParms<TInstance, TMethod>(out var parameterTypes, out var returnType);

            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, parameterTypes, null);

            return GetMethod<TMethod>(method, methodName, type, parameterTypes, returnType);
        }

        private static void GetMethodParms<TMethod>(out Type[] parameterTypes, out Type returnType)
        {
            var typeM = typeof(TMethod);
            var typeList = new List<Type>();
            returnType = typeof(void);

            if (!typeof(MulticastDelegate).IsAssignableFrom(typeM) || typeM == typeof(MulticastDelegate))
                throw new InvalidOperationException("Invalid TMethod, must be a Func or Action.");

            var typeName = typeM.FullName;
            if (typeName.StartsWith("System.Func`"))
            {
                var i = 0;
                while (i < typeM.GenericTypeArguments.Length - 1)
                    typeList.Add(typeM.GenericTypeArguments[i++]);
                returnType = typeM.GenericTypeArguments[i];
            }
            else if (typeName.StartsWith("System.Action`"))
            {
                var i = 0;
                while (i < typeM.GenericTypeArguments.Length)
                    typeList.Add(typeM.GenericTypeArguments[i++]);
            }
            else if (typeName == "System.Action")
            {
                // no args
            }
            else
                throw new InvalidOperationException(typeName);

            parameterTypes = typeList.ToArray();
        }

        private static void GetMethodParms<TInstance, TMethod>(out Type[] parameterTypes, out Type returnType)
        {
            var type = typeof(TInstance);

            var typeM = typeof(TMethod);
            var typeList = new List<Type>();
            returnType = typeof(void);

            if (!typeof(MulticastDelegate).IsAssignableFrom(typeM) || typeM == typeof(MulticastDelegate))
                throw new InvalidOperationException("Invalid TMethod, must be a Func or Action.");

            var typeName = typeM.FullName;
            if (!typeM.IsGenericType)
                throw new InvalidOperationException(typeName);
            if (typeM.GenericTypeArguments[0] != type)
                throw new InvalidOperationException("Invalid TMethod, the first generic argument must be TInstance.");
            if (typeName.StartsWith("System.Func`"))
            {
                var i = 1;
                while (i < typeM.GenericTypeArguments.Length - 1)
                    typeList.Add(typeM.GenericTypeArguments[i++]);
                returnType = typeM.GenericTypeArguments[i];
            }
            else if (typeName.StartsWith("System.Action`"))
            {
                var i = 1;
                while (i < typeM.GenericTypeArguments.Length)
                    typeList.Add(typeM.GenericTypeArguments[i++]);
            }
            else
                throw new InvalidOperationException(typeName);

            parameterTypes = typeList.ToArray();
        }

        private static TMethod GetStaticMethod<TMethod>(MethodInfo method, string methodName, Type type, Type[] parameterTypes, Type returnType)
        {
            if (method == null || method.ReturnType != returnType)
                throw new InvalidOperationException($"Could not find static method {type}.{methodName}({string.Join(",", parameterTypes.Select(x => x.ToString()))}) : {returnType}");

            var e = new List<ParameterExpression>();
            foreach (var p in method.GetParameters())
                e.Add(Expression.Parameter(p.ParameterType, p.Name));
            var exprCallArgs = e.ToArray();
            var exprLambdaArgs = exprCallArgs;

            // ReSharper disable once CoVariantArrayConversion
            var exprCall = Expression.Call(method, exprCallArgs);
            var expr = Expression.Lambda<TMethod>(exprCall, exprLambdaArgs);
            return expr.Compile();
        }

        private static TMethod GetMethod<TMethod>(MethodInfo method, string methodName, Type type, Type[] parameterTypes, Type returnType)
        {
            if (method == null || method.ReturnType != returnType)
                throw new InvalidOperationException($"Could not find method {type}.{methodName}({string.Join(",", parameterTypes.Select(x => x.ToString()))}) : {returnType}");

            var e = new List<ParameterExpression>();
            foreach (var p in method.GetParameters())
                e.Add(Expression.Parameter(p.ParameterType, p.Name));
            var exprCallArgs = e.ToArray();

            var exprThis = Expression.Parameter(type, "this");
            e.Insert(0, exprThis);
            var exprLambdaArgs = e.ToArray();

            // ReSharper disable once CoVariantArrayConversion
            var exprCall = Expression.Call(exprThis, method, exprCallArgs);
            var expr = Expression.Lambda<TMethod>(exprCall, exprLambdaArgs);
            return expr.Compile();
        }

        public static object GetStaticProperty(this Type type, string propertyName, Func<IEnumerable<PropertyInfo>, PropertyInfo> filter = null)
        {
            var propertyInfo = GetPropertyInfo(type, propertyName, filter);
            if (propertyInfo == null)
                throw new ArgumentOutOfRangeException(nameof(propertyName),
                    $"Couldn't find property {propertyName} in type {type.FullName}");
            return propertyInfo.GetValue(null, null);
        }

        public static object CallStaticMethod(this Type type, string methodName, params object[] parameters)
        {
            var methodInfo = GetMethodInfo(type, methodName);
            if (methodInfo == null)
                throw new ArgumentOutOfRangeException(nameof(methodName),
                    $"Couldn't find method {methodName} in type {type.FullName}");
            return methodInfo.Invoke(null, parameters);
        }

        public static object CallMethod(this object obj, string methodName, params object[] parameters)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type type = obj.GetType();
            var methodInfo = GetMethodInfo(type, methodName);
            if (methodInfo == null)
                throw new ArgumentOutOfRangeException(nameof(methodName),
                    $"Couldn't find method {methodName} in type {type.FullName}");
            return methodInfo.Invoke(obj, parameters);
        }

        public static object CallMethod(this object obj, string methodName, Func<IEnumerable<MethodInfo>, MethodInfo> filter = null, params object[] parameters)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type type = obj.GetType();
            var methodInfo = GetMethodInfo(type, methodName, filter);
            if (methodInfo == null)
                throw new ArgumentOutOfRangeException(nameof(methodName),
                    $"Couldn't find method {methodName} in type {type.FullName}");
            return methodInfo.Invoke(obj, parameters);
        }

        private static MethodInfo GetMethodInfo(Type type, string methodName, Func<IEnumerable<MethodInfo>, MethodInfo> filter = null)
        {
            MethodInfo methodInfo;
            do
            {
                try
                {
                    methodInfo = type.GetMethod(methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                catch (AmbiguousMatchException)
                {
                    if (filter == null) throw;

                    methodInfo = filter(
                        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                            .Where(x => x.Name == methodName));
                }
                type = type.BaseType;
            }
            while (methodInfo == null && type != null);
            return methodInfo;
        }

        private static PropertyInfo GetPropertyInfo(Type type, string propertyName, Func<IEnumerable<PropertyInfo>, PropertyInfo> filter = null)
        {
            PropertyInfo propInfo;
            do
            {
                try
                {
                    propInfo = type.GetProperty(propertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                catch (AmbiguousMatchException)
                {
                    if (filter == null) throw;

                    propInfo = filter(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .Where(x => x.Name == propertyName));
                }
                type = type.BaseType;
            }
            while (propInfo == null && type != null);
            return propInfo;
        }

        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException(nameof(propertyName),
                    $"Couldn't find property {propertyName} in type {objType.FullName}");
            return propInfo.GetValue(obj, null);
        }

        public static object GetPropertyValue(this object obj, string propertyName, IDictionary<string, PropertyInfo> propCache)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type objType = obj.GetType();
            PropertyInfo propInfo;
            if (propCache.ContainsKey(propertyName))
            {
                propInfo = propCache[propertyName];
            }
            else
            {
                propInfo = GetPropertyInfo(objType, propertyName);
                if (propInfo == null)
                    throw new ArgumentOutOfRangeException(nameof(propertyName),
                        $"Couldn't find property {propertyName} in type {objType.FullName}");

                propCache[propertyName] = propInfo;
            }
            return propInfo.GetValue(obj, null);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object val)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException(nameof(propertyName),
                    $"Couldn't find property {propertyName} in type {objType.FullName}");
            propInfo.SetValue(obj, val, null);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object val, IDictionary<string, PropertyInfo> propCache)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (propCache == null) throw new ArgumentNullException(nameof(propCache));

            Type objType = obj.GetType();
            PropertyInfo propInfo;
            if (propCache.ContainsKey(propertyName))
            {
                propInfo = propCache[propertyName];
            }
            else
            {
                propInfo = GetPropertyInfo(objType, propertyName);
                if (propInfo == null)
                    throw new ArgumentOutOfRangeException(nameof(propertyName),
                        $"Couldn't find property {propertyName} in type {objType.FullName}");

                propCache[propertyName] = propInfo;
            }

            propInfo.SetValue(obj, val, null);
        }
    }
}