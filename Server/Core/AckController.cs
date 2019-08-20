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

        public override MessageData ExecuteWithReturn(MessageData data)
        {
            data.CoreInstance.ReferenceManager.ExecuteForReference(data.GetAs<MessageData>(),data.sId);

			var hash = data.CoreInstance.ReferenceManager.GetResource<Referenceable>(data.GetAs<MessageData>().rId).GetHashCode();
			// success :)
			data.SerializeAndSet(hash);
			return data;
        } 
    }
}