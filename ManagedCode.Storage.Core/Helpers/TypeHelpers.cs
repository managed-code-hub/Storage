﻿using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ManagedCode.Storage.Core.Helpers;

public static class TypeHelpers
{
    public static Type GetImplementationType<TAbstraction, TImplementation, TOptions>()
        where TAbstraction : IStorage
    {
        var typeSignature = typeof(TAbstraction).Name;
        var an = new AssemblyName(typeSignature);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ManagedCodeCModule");
        var tb = moduleBuilder.DefineType(typeSignature,
            TypeAttributes.Public |
            TypeAttributes.Class |
            TypeAttributes.AutoClass |
            TypeAttributes.AnsiClass |
            TypeAttributes.BeforeFieldInit |
            TypeAttributes.AutoLayout,
            null);

        tb.SetParent(typeof(TImplementation));
        tb.AddInterfaceImplementation(typeof(TAbstraction));

        var newConstructor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
            new[] { typeof(TOptions) });

        var baseConstructors = typeof(TImplementation).GetConstructors(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        var emitter = newConstructor.GetILGenerator();
        emitter.Emit(OpCodes.Nop);

        // Load `this` and call base constructor with arguments
        emitter.Emit(OpCodes.Ldarg_0);
        emitter.Emit(OpCodes.Ldarg, 1);
        emitter.Emit(OpCodes.Call, baseConstructors[0]);

        emitter.Emit(OpCodes.Ret);

        return tb.CreateType();
    }
}