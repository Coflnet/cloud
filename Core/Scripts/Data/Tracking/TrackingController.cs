using Coflnet;

namespace Coflnet.Core.Tracking
{
	/// <summary>
	/// Handles, Tracking, Analytics and Rate Limiting
	/// </summary>
	public class TrackingController  {
		/// <summary>
		/// An instance of <see cref="TrackingController"/> class.
		/// </summary>
		public static TrackingController Instance;
		static TrackingController () {
			Instance = new TrackingController ();
		}
	}

	
}