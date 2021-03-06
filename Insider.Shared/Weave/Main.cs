﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Insider
{
    /// <summary>
    /// Static class meant to make editing modules
    /// easier.
    /// </summary>
    public static partial class Weave
    {
        /// <summary>
        /// Imported assemblies, classed by name.
        /// </summary>
        internal static Dictionary<string, Tuple<Assembly, AssemblyDefinition>> Assemblies;

        /// <summary>
        /// <see cref="Assembly"/> being modified.
        /// </summary>
        public static Assembly CurrentAssembly { get; internal set; }

        /// <summary>
        /// <see cref="AssemblyDefinition"/> of the <see cref="Assembly"/> being modified.
        /// </summary>
        public static AssemblyDefinition CurrentAssemblyDef { get; internal set; }

        /// <summary>
        /// Dictionary containing all settings set by the user.
        /// </summary>
        public static IReadOnlyDictionary<string, object> Settings { get; internal set; }


        /// <summary>
        /// Attempt to convert the given <see cref="TypeReference"/> to
        /// a <see cref="Type"/>.
        /// </summary>
        public static Type AsType(this TypeReference typeRef)
        {
            Type t = CurrentAssembly.GetType(typeRef.Namespace + "." + typeRef.Name);
            if (t == null && Assemblies.ContainsKey(typeRef.Module.Assembly.Name.Name))
                t = Assemblies[typeRef.Module.Assembly.Name.Name].Item1.GetType(typeRef.Namespace + '.' + typeRef.Name);
            if (t == null && Assemblies.ContainsKey(typeRef.Scope.Name))
                t = Assemblies[typeRef.Scope.Name].Item1.GetType(typeRef.Namespace + '.' + typeRef.Name);
            if (t == null)
                return null;

            if (typeRef is GenericInstanceType)
                t = t.MakeGenericType(((GenericInstanceType)typeRef).GenericArguments.Select(x => x.AsType()).ToArray());
            return t;
        }

        /// <summary>
        /// Attempt to convert the given <see cref="TypeReference"/> to
        /// a <see cref="TypeDefinition"/>.
        /// </summary>
        public static TypeDefinition AsTypeDefinition(this TypeReference typeRef)
        {
            try
            {
                TypeDefinition def = typeRef.Resolve();

                if (def != null)
                    return def;
            }
            catch (Exception) { }

            return Assemblies.ContainsKey(typeRef.Scope.Name)
                ? Assemblies[typeRef.Scope.Name].Item2.MainModule.GetType(typeRef.FullName)
                : null;
        }

        /// <summary>
        /// Attempt to convert the given <see cref="Type"/> to
        /// a <see cref="TypeDefinition"/>.
        /// </summary>
        public static TypeDefinition AsTypeDefinition(this Type type)
        {
            string assemblyName = type.GetTypeInfo().Assembly.GetName().Name;
            if (Assemblies.ContainsKey(assemblyName))
                return Assemblies[assemblyName].Item2.MainModule.GetType(type.FullName);
            return CurrentAssemblyDef.MainModule.GetType(type.FullName);
        }


        /// <summary>
        /// Return the first <see cref="TypeDefinition"/> that matches <typeparamref name="T"/>.
        /// </summary>
        public static TypeDefinition FindType<T>(this ModuleDefinition module)
        {
            return module.Types.First(x => x.Is(typeof(T), false));
        }

        /// <summary>
        /// Return the first <see cref="TypeDefinition"/> that matches <typeparamref name="T"/>.
        /// </summary>
        public static TypeDefinition FindType(this ModuleDefinition module, Type type)
        {
            return module.Types.First(x => x.Is(type, false));
        }


        /// <summary>
        /// Returns a <see cref="CustomAttributeArgument"/>'s <see cref="object"/>
        /// value.
        /// </summary>
        public static object GetValue(this CustomAttributeArgument arg)
        {
            if (arg.Value is CustomAttributeArgument[])
                return (arg.Value as CustomAttributeArgument[]).Select(GetValue).ToArray();

            object value = arg.Value;
            while (value is CustomAttributeArgument)
                value = ((CustomAttributeArgument)value).Value;
            return value;
        }


        /// <summary>
        /// Get the setting named <paramref name="key"/>, cast
        /// it to <typeparamref name="T"/> if possible,
        /// and return it.
        /// <para>
        /// If any of the above steps fails, <paramref name="defaultValue"/>
        /// will be returned.
        /// </para>
        /// </summary>
        public static T GetSetting<T>(string key, T defaultValue = default(T))
        {
            object value;
            if (Settings.TryGetValue(key, out value) && value is T)
                return (T)value;
            return defaultValue;
        }

        /// <summary>
        /// Returns whether or not a given method is marked external.
        /// </summary>
        public static bool IsExternal(this MethodDefinition method)
        {
            return method.RVA == 0;
        }
    }
}
