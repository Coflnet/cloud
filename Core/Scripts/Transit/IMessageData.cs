namespace Coflnet
{
    public interface ICommandData
    {
        byte[] message { get; set; }
        string Data { get; }
        dynamic DeSerialized { get; set; }
        CoflnetCore CoreInstance { get; set; }
        byte[] SignableContent { get; }

        void Deserialize<T>();
        bool Equals(object obj);
        T GetAs<T>();
        int GetHashCode();
        T GetTargetAs<T>() where T : Entity;
        void SendBack(CommandData data);
        byte[] Serialize<T>(T ob);
        CommandData SerializeAndSet<T>(T ob);
        void SetCommand<C>() where C : Command, new();
        void Sign(KeyPair singKeyPair);
        string ToString();
        bool ValidateSignature(byte[] publicKey);
    }
}


