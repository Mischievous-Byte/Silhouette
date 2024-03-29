using MischievousByte.Masquerade;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

namespace MischievousByte.Silhouette
{
    public delegate void PoserDelegate<TInput>(in TInput input, ref BodyTree<Matrix4x4> tree);

    public static class PoserRegister
    {
        private static class MethodInfoLoader
        {

            public static bool TryCreateDelegate(MethodInfo info, out Delegate del)
            {
                del = null;
                ParameterInfo[] parameters = info.GetParameters();

                if (parameters.Length != 2)
                    return false;

                if (!parameters[0].IsIn)
                    return false;

                if (parameters[1].IsOut || parameters[1].IsIn || !parameters[1].ParameterType.IsByRef || parameters[1].ParameterType.GetElementType() != typeof(BodyTree<Matrix4x4>))
                    return false;

                del = Delegate.CreateDelegate(
                    typeof(PoserDelegate<>).MakeGenericType(
                        parameters[0].ParameterType.GetElementType() ?? parameters[0].ParameterType),
                    info);

                return true;
            }


        }
        private struct Entry
        {
            public Delegate action;
            public BodyNode target;
        }


        private static List<Entry> entries = new();


#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void OnLoad() { } //Empty method to call static constructor


        static PoserRegister()
        {
            FindFlaggedMethods();
        }

        private static void FindFlaggedMethods()
        {
            var pairs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods())
                .Where(method => method.IsStatic)
                .Select(method => (method, method.GetCustomAttribute<PoserAttribute>()))
                .Where(pair => pair.Item2 != null);

            foreach (var pair in pairs)
            {
                if (!MethodInfoLoader.TryCreateDelegate(pair.method, out var del))
                {
                    Debug.LogWarning($"{pair.method.Name} is flagged as generator, but doesn't match any delegate.");
                    continue;
                }

                if (entries.Where(e => e.action == del).Count() > 0)
                {
                    Debug.LogWarning($"{pair.method.Name} is already registered during discovery");
                    continue;
                }

                var entry = new Entry()
                {
                    action = del,
                    target = pair.Item2.Target
                };

                entries.Add(entry);

                
            }
        }


        public static GeneratorDelegate<TInput> Find<TInput>(BodyNode target) =>
            entries.Where(e => e.target == target && e.action is GeneratorDelegate<TInput>)
            .FirstOrDefault().action as GeneratorDelegate<TInput>;
    }
}
