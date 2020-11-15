using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Coflnet.Server
{
	public class LeaderBoardController : IRegisterCommands
	{
		public static LeaderBoardController Instance;
		Dictionary<EntityId, Leaderboard> leaderboards = new Dictionary<EntityId, Leaderboard> ();

		static LeaderBoardController ()
		{
			Instance = new LeaderBoardController ();
		}

		public void RegisterCommands (CommandController controller)
		{
			controller.RegisterCommand<GetCurrentScore> ();
		}

		public class GetCurrentScore : DefaultServerCommand
		{

			public override void Execute (CommandData data)
			{
				Reference<CoflnetUser> user = new Reference<CoflnetUser> (new EntityId ());
				int currentScore = LeaderBoardController.Instance.leaderboards[data.GetAs<EntityId> ()].scores[user].score;
				SendBack (data, currentScore);
			}

			public override string Slug
			{
				get
				{

					return "getLeaderbaordScore";
				}
			}

		}

	}

}