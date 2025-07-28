using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Gets an instance of the service type T. Throws an exception if the service is not registered.
        /// </summary>
        /// <typeparam name="T">The service type to get.</typeparam>
        /// <returns>An instance of the service.</returns>
        /// <exception cref="ArgumentException">If the service is not registered.</exception>
        public static T Get<T>() where T : class, IHMSService => _instance.GetService<T>();

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

        private void OnDestroy() => _instance = null;

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
                throw new ArgumentException($"Service {typeof(T).Name} is not registered.");
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
            var hmsConfig = HMSConfig.Get();
            var services = new List<IHMSService>();
            var serviceBaseType = typeof(IHMSService);

            // Get all types that are a subclass of IHMSService and are not abstract
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