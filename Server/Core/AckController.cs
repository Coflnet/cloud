using Coflnet;
using Coflnet.Core;
using System.Collections.Generic;
using System;

namespace Coflnet.Server
{
	public class AckController
	{
		public static AckController Instance;

		static AckController()
		{
			Instance = new AckController();
		}





		
	}

    public class SibblingUpdate : ReturnCommand
    {
        public override string Slug => "sibUpdate";

        public override CommandData ExecuteWithReturn(CommandData data)
        {
            data.CoreInstance.EntityManager.ExecuteForReference(data.GetAs<CommandData>(),data.SenderId);

			var hash = data.CoreInstance.EntityManager.GetEntity<Entity>(data.GetAs<CommandData>().Recipient).GetHashCode();
			// success :)
			data.SerializeAndSet(hash);
			return data;
        } 
    }
}