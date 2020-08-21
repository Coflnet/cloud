using System;
using System.Runtime.Serialization;

namespace Coflnet.Extentions.Oauth2 {

    /// <summary>
    /// Custom client settings
    /// </summary>
    [DataContract]
    public class OAuthClientSettings {
        [DataMember]
        private OAuthClient client;
        [DataMember]
        private int standardUserCalls;
        [DataMember]
        private int standardUserStorage;
        [DataMember]
        private int standardCallsUntilCaptcha;
        [DataMember]
        private bool allowSelfUserRegistration;
        [DataMember]
        private bool captchaRequiredForRegistration;
        [DataMember]
        private bool trackingEnabled;
        [DataMember]
        private UInt64 usageLeft;

        public void DecreaseUsage (ushort amount) {
            if (usageLeft < amount)
                throw new ClientLimitExeededException ();
            usageLeft -= amount;
        }
    }

}