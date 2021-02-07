using Coflnet;
using System;

    public class DummyCore : CoflnetCore
    {
        private int count =0;

        public static void Init(EntityId deviceId)
        {
            // we are the resource now (needed for updating to it)
            ConfigController.DeviceId = deviceId;
            Instance.EntityManager = new EntityManager();
            Instance.EntityManager.coreInstance = Instance;
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

        public override void SendCommand(CommandData data, long serverId = 0)
        {
            if(data.SenderId == default(EntityId))
            data.SenderId = ConfigController.DeviceId;
            data.CoreInstance = this;
            if(count >= 50)
            {
                throw new Exception("we are in a loop");
            }
            count++;
            EntityManager.ExecuteForReference(data);
        }

        public override void SendCommand<C, T>(EntityId receipient, T data, EntityId sender = default(EntityId), long id = 0)
        {
           return;
        }
    }

