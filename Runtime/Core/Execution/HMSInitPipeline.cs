using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HMSUnitySDK
{
    /// <summary>
    /// Centralized execution pipeline for methods annotated with
    /// <see cref="HMSInitAttribute"/>.
    /// </summary>
    public static class HMSInitPipeline
    {
        private static bool _bootstrapCompleted;
        private static List<HMSInitMethodDescriptor> _cachedMethods;
        private static readonly HashSet<string> _executedMethods = new();

        /// <summary>
        /// Resets internal state for a fresh bootstrap cycle.
        /// </summary>
        public static void ResetForBootstrap()
        {
            _bootstrapCompleted = false;
            _cachedMethods = null;
            _executedMethods.Clear();
        }

        /// <summary>
        /// Marks HMS bootstrap as complete so HMSInit methods become eligible.
        /// </summary>
        public static void MarkBootstrapCompleted()
        {
            _bootstrapCompleted = true;
        }

        /// <summary>
        /// Runtime hook for HMSInit methods that run before scene load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunBeforeSceneLoad()
        {
            Execute(RuntimeInitializeLoadType.BeforeSceneLoad);
        }

        /// <summary>
        /// Runtime hook for HMSInit methods that run after scene load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RunAfterSceneLoad()
        {
            Execute(RuntimeInitializeLoadType.AfterSceneLoad);
        }

        /// <summary>
        /// Executes HMSInit methods for a given runtime stage.
        /// </summary>
        /// <param name="stage">Runtime stage to execute.</param>
        private static void Execute(RuntimeInitializeLoadType stage)
        {
            if (!_bootstrapCompleted)
            {
                HMSLogger.LogWarning(
                    $"{nameof(HMSInitPipeline)} skipped at {stage} because " +
                    "HMS bootstrap did not complete successfully."
                );
                return;
            }

            foreach (var descriptor in GetMethodsForStage(stage))
            {
                if (_executedMethods.Contains(descriptor.MethodKey))
                {
                    continue;
                }

                try
                {
                    descriptor.Method.Invoke(null, null);
                    _executedMethods.Add(descriptor.MethodKey);
                    HMSLogger.Log(
                        $"{nameof(HMSInitPipeline)} executed {descriptor.MethodKey}."
                    );
                }
                catch (TargetInvocationException exception)
                {
                    HMSLogger.LogException(exception.InnerException ?? exception);
                    HMSLogger.LogError(
                        $"{nameof(HMSInitPipeline)} failed while executing " +
                        $"{descriptor.MethodKey}."
                    );
                }
                catch (Exception exception)
                {
                    HMSLogger.LogException(exception);
                    HMSLogger.LogError(
                        $"{nameof(HMSInitPipeline)} failed while executing " +
                        $"{descriptor.MethodKey}."
                    );
                }
            }
        }

        /// <summary>
        /// Gets all cached HMSInit descriptors that match a runtime stage.
        /// </summary>
        /// <param name="stage">Runtime stage to filter.</param>
        /// <returns>Ordered list of descriptors for the given stage.</returns>
        private static IEnumerable<HMSInitMethodDescriptor> GetMethodsForStage(
            RuntimeInitializeLoadType stage
        )
        {
            if (_cachedMethods == null)
            {
                _cachedMethods = DiscoverInitMethods();
            }

            return _cachedMethods.Where(descriptor => descriptor.LoadType == stage);
        }

        /// <summary>
        /// Discovers all methods annotated with <see cref="HMSInitAttribute"/>.
        /// </summary>
        /// <returns>Deterministically ordered method descriptors.</returns>
        private static List<HMSInitMethodDescriptor> DiscoverInitMethods()
        {
            var config = HMSConfig.Get();
            if (config == null)
            {
                HMSLogger.LogError(
                    $"{nameof(HMSInitPipeline)} could not load HMSConfig. " +
                    "No HMSInit methods will run."
                );
                return new List<HMSInitMethodDescriptor>();
            }

            var methods = new List<HMSInitMethodDescriptor>();

            foreach (var assembly in config.GetAssemblies())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (type == null)
                    {
                        continue;
                    }

                    foreach (var method in type.GetMethods(
                                 BindingFlags.Static |
                                 BindingFlags.Public |
                                 BindingFlags.NonPublic
                             ))
                    {
                        if (method.GetCustomAttribute<HMSInitAttribute>()
                            is not HMSInitAttribute attribute)
                        {
                            continue;
                        }

                        if (!ValidateMethodSignature(method))
                        {
                            continue;
                        }

                        methods.Add(new HMSInitMethodDescriptor(method, attribute));
                    }
                }
            }

            return methods
                .OrderBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.MethodKey, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Validates that an HMSInit method can be invoked safely by reflection.
        /// </summary>
        /// <param name="method">Method metadata to validate.</param>
        /// <returns>True when signature is supported.</returns>
        private static bool ValidateMethodSignature(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                HMSLogger.LogError(
                    $"{nameof(HMSInitAttribute)} requires static methods. " +
                    $"Invalid method: {ResolveMethodKey(method)}"
                );
                return false;
            }

            if (method.GetParameters().Length != 0)
            {
                HMSLogger.LogError(
                    $"{nameof(HMSInitAttribute)} methods must not have parameters. " +
                    $"Invalid method: {ResolveMethodKey(method)}"
                );
                return false;
            }

            if (method.ReturnType != typeof(void))
            {
                HMSLogger.LogError(
                    $"{nameof(HMSInitAttribute)} methods must return void. " +
                    $"Invalid method: {ResolveMethodKey(method)}"
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves loadable types for an assembly and tolerates partial load
        /// failures.
        /// </summary>
        /// <param name="assembly">Assembly to inspect.</param>
        /// <returns>Loadable types contained in the assembly.</returns>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// Builds a stable identifier for a reflected method.
        /// </summary>
        /// <param name="method">Method metadata.</param>
        /// <returns>Fully qualified method identifier.</returns>
        private static string ResolveMethodKey(MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName}.{method.Name}";
        }

        /// <summary>
        /// Data transfer object describing one HMSInit method entry.
        /// </summary>
        private sealed class HMSInitMethodDescriptor
        {
            /// <summary>
            /// Initializes a new descriptor from reflection metadata and the
            /// resolved attribute.
            /// </summary>
            /// <param name="method">Reflected method metadata.</param>
            /// <param name="attribute">Resolved attribute values.</param>
            public HMSInitMethodDescriptor(MethodInfo method, HMSInitAttribute attribute)
            {
                Method = method;
                MethodKey = ResolveMethodKey(method);
                LoadType = attribute.LoadType;
                Order = attribute.Order;
            }

            /// <summary>
            /// Reflected method metadata.
            /// </summary>
            public MethodInfo Method { get; }

            /// <summary>
            /// Fully qualified method identifier.
            /// </summary>
            public string MethodKey { get; }

            /// <summary>
            /// Runtime stage where the method should execute.
            /// </summary>
            public RuntimeInitializeLoadType LoadType { get; }

            /// <summary>
            /// Ordering hint where lower values execute first.
            /// </summary>
            public int Order { get; }
        }
    }
}