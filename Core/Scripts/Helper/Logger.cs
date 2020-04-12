using System;

namespace Coflnet
{
    /// <summary>
    /// Central point for all Log Messages
    /// </summary>
    public class Logger : ILogger
    {
        /// <summary>
        /// This event is raised when the <see cref="Logger.Log(string)"/> is called
        /// </summary>
        public static event Action<string> OnLog;

        /// <summary>
        /// This event is raised when the <see cref="Logger.Error(string)"/> is called
        /// </summary>
        public static event Action<string> OnError; 
        /// <summary>
        /// An instance of the <see cref="Logger"/>
        /// </summary>
        /// <value></value>
        public static ILogger Instance {get;private set;}


        static Logger()
        {
            Instance = new Logger();
        }

        /// <summary>
        /// Logs a message.
        /// Is short for <see cref="Logger.Instance.Log(string)"/>
        /// </summary>
        /// <param name="message">The message that should be logged</param>
        public static void Log(string message) => Instance.Log(message);

        /// <summary>
        /// Loggs an object of any type, will attempt to serialize it if it is nested
        /// </summary>
        /// <param name="obj">The object to log</param>
        public static void Log(object obj)
        {
            if(obj.GetType().IsPrimitive)
                Log(obj.ToString());
            else 
                Log(MessagePack.MessagePackSerializer.SerializeToJson(obj));
        }

        /// <summary>
        /// Logs an error message.
        /// Is short for <see cref="Logger.Instance.Error(string)"/>
        /// </summary>
        /// <param name="message">The error message that should be logged</param>
        public static void Error(string message) => Instance.Error(message);

        void ILogger.Error(string message)
        {
            OnError?.Invoke(message);
        }

        void ILogger.Log(string message)
        {
            OnLog?.Invoke(message);
        }
    }

    public interface ILogger
    {
        void Log(string message);

        void Error(string message);
    }
}