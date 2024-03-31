using MischievousByte.Masquerade;
using MischievousByte.Silhouette.BuiltIn;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MischievousByte.Silhouette
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SkeletonGeneratorAttribute : Attribute { }

    public delegate void SkeletonGeneratorDelegate<TInput>(in TInput input, out BodyTree<Matrix4x4> tree);

    public static class SkeletonGeneratorRegister
    {
        private static List<Delegate> delegates = new();


#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void OnLoad() { } //Empty method to call static constructor


        static SkeletonGeneratorRegister()
        {
            FindFlaggedMethods();

            BodyMeasurements m = new BodyMeasurements()
            {
                height = 1.76f,
                wingspan = 1.8f
            };

            Find<BodyMeasurements>()(in m, out var tree);
        }

        private static void FindFlaggedMethods()
        {
            var pairs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods())
                .Where(method => method.IsStatic)
                .Select(method => (method, method.GetCustomAttribute<SkeletonGeneratorAttribute>()))
                .Where(pair => pair.Item2 != null);

            foreach (var pair in pairs)
            {
                if (!TryCreateDelegate(pair.method, out var del))
                {
                    Debug.LogWarning($"{pair.method.Name} is flagged as generator, but doesn't match any delegate.");
                    continue;
                }

                if (delegates.Where(x => x == del).Count() > 0)
                {
                    Debug.LogWarning($"{pair.method.Name} is already registered during discovery");
                    continue;
                }

                delegates.Add(del);
            }
        }

        private static bool TryCreateDelegate(MethodInfo info, out Delegate del)
        {
            del = null;
            ParameterInfo[] parameters = info.GetParameters();

            if (parameters.Length != 2)
                return false;

            if (!parameters[0].IsIn)
                return false;

            if (!parameters[1].IsOut || parameters[1].ParameterType != typeof(BodyTree<Matrix4x4>).MakeByRefType())
                return false;

            del = Delegate.CreateDelegate(
                typeof(SkeletonGeneratorDelegate<>).MakeGenericType(
                    parameters[0].ParameterType.GetElementType() ?? parameters[0].ParameterType),
                info);

            return true;
        }

        public static SkeletonGeneratorDelegate<TInput> Find<TInput>() =>
            delegates.Where(e => e is SkeletonGeneratorDelegate<TInput>)
            .FirstOrDefault() as SkeletonGeneratorDelegate<TInput>;
    }
}
