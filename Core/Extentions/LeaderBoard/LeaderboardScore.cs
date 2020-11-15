namespace Coflnet.Server
{
    public class LeaderboardScore {
		public int score;
		public Reference<CoflnetUser> user;
		public LeaderboardPercentage percentage;

		public LeaderboardScore (Reference<CoflnetUser> user) {
			this.user = user;
		}
	}

}