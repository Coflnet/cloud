using System;
using System.Collections.Concurrent;

namespace Coflnet
{
    public class CoflnetServices
    {
        public CoflnetCore Core { get; set; }

        private ConcurrentDictionary<Type, IService> Services = new ConcurrentDictionary<Type, IService>();

        public CoflnetServices(CoflnetCore core)
        {
            Core = core;
        }

        /// <summary>
        /// Gets an instance of the Service <see cref="T"/>. creates an instance if none exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>An instance of the service</returns>
        public T Get<T>() where T : IService
        {
            var type = typeof(T);
            if (!Services.TryGetValue(type, out IService value))
            {
                value = Services.GetOrAdd(type, t =>
                {
                    if (type.IsInterface)
                        throw new CoflnetException("unknown_service", $"There is no service with name `{type.Name}` was not found. Please register it.");
                    
                    var v = (T)Activator.CreateInstance(type);
                    v.Services = this;
                    return v;
                });
            }

            return (T)value;
        }

        /// <summary>
        /// Adds or overrides a service
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <param name="instance">The instance of <see cref="T"/> to add/override</param>
        public void AddOrOverride<T>(T instance) where T : IService
        {
            Services.AddOrUpdate(typeof(T), instance, (type,s)=>instance);
        }


        /// <summary>
        /// Extend a service
        /// </summary>
        /// <param name="extender">Function that receives the current instance of the service and returns the new/changed. 
        /// Receives <see cref="null"/> when the service type isn't registered yet</param>
        /// <typeparam name="T"></typeparam>
        public void Extend<T>(Func<T, T> extender) where T : CoflnetServiceBase
        {
            Services.AddOrUpdate(typeof(T),
                t => extender(null),
                (t, old) => extender(old as T));
        }
    }

    public interface IService
    {
        CoflnetServices Services { get; set; }
    }

    public class CoflnetServiceBase : IService
    {
        /// <summary>
        /// The <see cref="CoflnetServices"/> instance to communicate with other services
        /// </summary>
        /// <value></value>
        public virtual CoflnetServices Services { get; set; }
    }

}
