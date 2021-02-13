using Coflnet;

namespace Core.Extentions.KeyValue
{
    public class KeyValueExtension : IRegisterCommands
    {
        public void RegisterCommands(CommandController controller)
        {
            controller.RegisterCommand<CreateKeyValueStoreCommand>();
        }
    }
}