using Coflnet;
using System;

    public class DummyCore : CoflnetCore
    {
        private int count =0;

        public static void Init(SourceReference deviceId)
        {
            // we are the resource now (needed for updating to it)
            ConfigController.DeviceId = deviceId;
            Instance.ReferenceManager = new ReferenceManager();
            Instance.ReferenceManager.coreInstance = Instance;
            Instance.Id = deviceId.FullServerId;
        }

        static DummyCore()
        {
            Instance = new DummyCore();
        }


        public override CommandController GetCommandController()
        {
            return new CommandController();
        }

        public override void SendCommand(MessageData data, long serverId = 0)
        {
            if(data.sId == default(SourceReference))
            data.sId = ConfigController.DeviceId;
            data.CoreInstance = this;
            if(count >= 50)
            {
                throw new Exception("we are in a loop");
            }
            count++;
            ReferenceManager.ExecuteForReference(data);
        }

        public override void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference))
        {
           return;
        }

        public override void SendCommand<C>(SourceReference receipient, byte[] data)
        {
            throw new System.NotImplementedException();
        }
    }

