using MischievousByte.Masquerade;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MischievousByte.Silhouette
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SkeletonPoserAttribute : Attribute
    {
        public readonly BodyNode Target;

        public SkeletonPoserAttribute(BodyNode target) => Target = target;
    }

    public delegate void SkeletonPoserDelegate<TInput>(ref BodyTree<Matrix4x4> tree, in TInput input);
    public delegate void SkeletonPoserDelegate<TSettings, TInput>(ref BodyTree<Matrix4x4> tree, in TSettings settings, in TInput input);

    public static class SkeletonPoserRegister
    {
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


        static SkeletonPoserRegister()
        {
            FindFlaggedMethods();
        }

        private static void FindFlaggedMethods()
        {
            var pairs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods())
                .Where(method => method.IsStatic)
                .Select(method => (method, method.GetCustomAttribute<SkeletonPoserAttribute>()))
                .Where(pair => pair.Item2 != null);

            foreach (var pair in pairs)
            {
                if (!TryCreateDelegate(pair.method, out var del))
                {
                    Debug.LogWarning($"{pair.method.Name} is flagged as poser, but doesn't match any delegate.");
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


        private static bool TryCreateDelegate(MethodInfo info, out Delegate del)
        {
            bool HandleSimple(out Delegate del)
            {
                del = null;

                ParameterInfo[] parameters = info.GetParameters();

                if (parameters.Length != 2)
                    return false;

                if (!parameters[1].IsIn)
                    return false;

                if (parameters[0].IsOut || parameters[0].IsIn || !parameters[0].ParameterType.IsByRef || parameters[0].ParameterType.GetElementType() != typeof(BodyTree<Matrix4x4>))
                    return false;

                del = Delegate.CreateDelegate(
                    typeof(SkeletonPoserDelegate<>).MakeGenericType(
                        parameters[1].ParameterType.GetElementType() ?? parameters[1].ParameterType),
                    info);

                return true;
            }

            bool HandleSettings(out Delegate del)
            {
                del = null;

                ParameterInfo[] parameters = info.GetParameters();

                if (parameters.Length != 3)
                    return false;

                if (!parameters[1].IsIn || !parameters[2].IsIn)
                    return false;

                if (parameters[0].IsOut || parameters[0].IsIn || !parameters[0].ParameterType.IsByRef || parameters[0].ParameterType.GetElementType() != typeof(BodyTree<Matrix4x4>))
                    return false;

                del = Delegate.CreateDelegate(
                    typeof(SkeletonPoserDelegate<,>).MakeGenericType(
                        parameters[1].ParameterType.GetElementType() ?? parameters[1].ParameterType,
                        parameters[2].ParameterType.GetElementType() ?? parameters[2].ParameterType),
                    info);

                return true;
            }

            if (HandleSimple(out del)) return true;
            if (HandleSettings(out del)) return true;

            del = null;
            return false;
        }





        public static SkeletonPoserDelegate<TInput> Find<TInput>(BodyNode target) =>
            entries.Where(e => e.target == target && e.action is SkeletonPoserDelegate<TInput>)
            .FirstOrDefault().action as SkeletonPoserDelegate<TInput>;

        public static SkeletonPoserDelegate<TSettings, TInput> Find<TSettings, TInput>(BodyNode target) =>
            entries.Where(e => e.target == target && e.action is SkeletonPoserDelegate<TSettings, TInput>)
            .FirstOrDefault().action as SkeletonPoserDelegate<TSettings, TInput>;
    }
}
