using System.Collections.Generic;

namespace Coflnet.Server
{
    /// <summary>
    /// Scores for friends of a specific user
    /// may need to be sorted
    /// </summary>
    public class FriendLeaderBoard {
		public List<LeaderboardScore> scores;

		public FriendLeaderBoard (List<LeaderboardScore> scores) {
			this.scores = scores;
		}

		public FriendLeaderBoard (int friendCount) {
			this.scores = new List<LeaderboardScore> (friendCount);
		}

		public void AddScoreForFriend (LeaderboardScore score) {
			scores.Add (score);
		}
	}

}