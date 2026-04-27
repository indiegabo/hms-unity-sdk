using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HMSUnitySDK
{
    /// <summary>
    /// Responsible for locating all HSM services. <br />
    /// Use <see cref="Get{T}"/> to get an instance of a service.
    /// </summary>
    public class HMSLocator : MonoBehaviour
    {
        #region Static

        private static HMSLocator _instance;

        /// <summary>
        /// A dictionary of registered services, mapped by type.
        /// </summary>
        private static readonly Dictionary<Type, IHMSService> _services = new();

        /// <summary>
        /// Indicates whether the locator instance is available for service resolution.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        /// <summary>
        /// Gets an instance of the service type T.
        /// </summary>
        /// <typeparam name="T">The service type to get.</typeparam>
        /// <returns>An instance of the service.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the locator has not been initialized yet.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the service was not registered.
        /// </exception>
        public static T Get<T>() where T : class, IHMSService
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(HMSLocator)} is not initialized. " +
                    "Ensure HMSBootstrapper finished successfully before " +
                    "requesting services."
                );
            }

            return _instance.GetService<T>();
        }

        /// <summary>
        /// Tries to resolve a registered service without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The service type to get.</typeparam>
        /// <param name="service">Resolved service instance when available.</param>
        /// <returns>
        /// True when the locator is initialized and the service is registered.
        /// </returns>
        public static bool TryGet<T>(out T service) where T : class, IHMSService
        {
            return TryGet(out service, out _);
        }

        /// <summary>
        /// Tries to resolve a registered service without throwing exceptions and
        /// returns an explicit reason when it cannot be resolved.
        /// </summary>
        /// <typeparam name="T">The service type to get.</typeparam>
        /// <param name="service">Resolved service instance when available.</param>
        /// <param name="reason">Diagnostic reason when resolution fails.</param>
        /// <returns>
        /// True when the locator is initialized and the service is registered.
        /// </returns>
        public static bool TryGet<T>(out T service, out string reason)
            where T : class, IHMSService
        {
            service = null;

            if (_instance == null)
            {
                reason =
                    $"{nameof(HMSLocator)} is not initialized. " +
                    "HMSBootstrapper may have aborted before service " +
                    "registration.";
                return false;
            }

            if (!_services.TryGetValue(typeof(T), out var rawService))
            {
                string registered = _services.Count == 0
                    ? "none"
                    : string.Join(", ", _services.Keys.Select(type => type.Name));
                reason =
                    $"Service {typeof(T).Name} is not registered. " +
                    $"Registered services: {registered}.";
                return false;
            }

            service = rawService as T;
            if (service == null)
            {
                reason =
                    $"Service {typeof(T).Name} was found but could not be cast " +
                    "to the requested type.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Checks whether a service type is currently registered.
        /// </summary>
        /// <typeparam name="T">The service type to check.</typeparam>
        /// <returns>True when the service type is registered.</returns>
        public static bool IsServiceRegistered<T>() where T : class, IHMSService
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Resolves a service with bounded retries to tolerate short bootstrap
        /// races during runtime initialization.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="timeoutSeconds">Maximum time to wait for the service.</param>
        /// <param name="retryIntervalSeconds">Delay between resolution attempts.</param>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when timeout or retry interval are not positive.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown when the service is unavailable until timeout.
        /// </exception>
        public static async Awaitable<T> GetWithRetry<T>(
            float timeoutSeconds = 1.5f,
            float retryIntervalSeconds = 0.05f
        ) where T : class, IHMSService
        {
            if (timeoutSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    timeoutSeconds,
                    "Timeout must be greater than zero."
                );
            }

            if (retryIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(retryIntervalSeconds),
                    retryIntervalSeconds,
                    "Retry interval must be greater than zero."
                );
            }

            float startedAt = Time.realtimeSinceStartup;
            float nextAttemptAt = startedAt;
            string lastReason = "Service has not been resolved yet.";

            while (Time.realtimeSinceStartup - startedAt <= timeoutSeconds)
            {
                if (Time.realtimeSinceStartup >= nextAttemptAt)
                {
                    if (TryGet(out T service, out lastReason))
                    {
                        return service;
                    }

                    nextAttemptAt = Time.realtimeSinceStartup + retryIntervalSeconds;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"Timed out while resolving service {typeof(T).Name} after " +
                $"{timeoutSeconds:F2}s. Last reason: {lastReason}"
            );
        }

        #endregion

        #region Behaviour

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _services.Clear();
            _instance = null;
        }

        #endregion

        #region Locating

        /// <summary>
        /// Gets an instance of the service type T. If the service is not registered, throws an ArgumentException.
        /// </summary>
        /// <typeparam name="T">The service type to get.</typeparam>
        /// <returns>An instance of the service.</returns>
        /// /// <exception cref="ArgumentException">If the service is not registered.</exception>
        public T GetService<T>() where T : class, IHMSService
        {
            if (!_services.TryGetValue(typeof(T), out var service))
            {
                string registered = _services.Count == 0
                    ? "none"
                    : string.Join(", ", _services.Keys.Select(type => type.Name));
                throw new ArgumentException(
                    $"Service {typeof(T).Name} is not registered. " +
                    $"Registered services: {registered}."
                );
            }

            return service as T;
        }

        #endregion

        #region Registering


        /// <summary>
        /// Initializes the services by creating game objects and adding their respective components.
        /// The services are registered in the locator.
        /// </summary>
        /// <param name="runtimeInfo">The runtime information.</param>
        /// <param name="servicesContainer">The parent transform of the services objects.</param>
        /// <remarks>
        /// If the role is <see cref="HMSRuntimeRole.Server"/>, the connection service is initialized first.
        /// Then, all the other services are initialized by iterating over the types implementing <see cref="IHMSService"/>.
        /// This means that all the services will be able to find the connection service in the locator.
        /// For each service type, a game object is created and the service component is added.
        /// The service is then registered in the locator.
        /// </remarks>
        public void InitializeServices(HMSRuntimeInfo runtimeInfo, Transform servicesContainer)
        {
            _services.Clear();

            var hmsConfig = HMSConfig.Get();
            var services = new List<IHMSService>();

            IEnumerable<Type> childrenTypes = hmsConfig.GetAssemblies()
                 .SelectMany(assembly =>
                 {
                     try
                     {
                         return assembly.GetTypes();
                     }
                     catch
                     {
                         return Array.Empty<Type>();
                     }
                 })
                 .Where(t => t.IsClass
                     && !t.IsAbstract
                     && typeof(IHMSService).IsAssignableFrom(t)
                 )
                 .Distinct();

            // Iterate over the types and create a game object for each one
            // that is a valid service for the current runtime role
            foreach (Type childType in childrenTypes)
            {
                if (childType.GetCustomAttributes(typeof(HMSBuildRolesAttribute), true)
                    .FirstOrDefault() is not HMSBuildRolesAttribute serviceRole ||
                    serviceRole.Roles.Contains(runtimeInfo.Role)
                )
                {
                    // Create a new game object and add the service component
                    GameObject serviceObject = new();
                    var serviceComponent = serviceObject.AddComponent(childType) as IHMSService;

                    // Validate the service and destroy the object if it is invalid
                    if (!serviceComponent.ValidateService())
                    {
                        Destroy(serviceObject);
                        continue;
                    }

                    // Set up the game object and register the service
                    SetupServiceObject(serviceObject, servicesContainer.transform, serviceComponent);
                    RegisterService(serviceComponent);
                    services.Add(serviceComponent);
                }
            }

            foreach (IHMSService service in services)
            {
                service.InitializeService();
            }
        }

        private void RegisterService(IHMSService service)
        {
            var type = service.GetType();
            if (_services.ContainsKey(type))
            {
                return;
            }

            _services.Add(type, service);
        }

        /// <summary>
        /// Setups the game object containing the service.
        /// </summary>
        /// <param name="obj">The game object.</param>
        /// <param name="parent">The parent of the game object.</param>
        /// <param name="service">The service.</param>
        /// <remarks>
        /// Sets the game object name to the service's <see cref="IHMSService.ServiceObjectName"/> and sets the parent to the given parent.
        /// </remarks>
        private void SetupServiceObject(GameObject obj, Transform parent, IHMSService service)
        {
            obj.name = !string.IsNullOrEmpty(service.ServiceObjectName)
                ? service.ServiceObjectName
                : service.GetType().Name;

            obj.transform.SetParent(parent);
        }

        #endregion
    }
}