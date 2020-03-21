namespace Coflnet.Core.Crypto
{
    using System;
    using MessagePack;
    using unity.libsodium;

    /// <summary>
    /// Serializeable Signature for <see cref="MessageData"/>
    /// </summary>
    [MessagePackObject]
    [Union(1,typeof(LibsodiumSignature))]
	public abstract class SigningAlgorythm
	{
        /// <summary>
        /// Generates a new Signature
        /// </summary>
        /// <param name="data">the data to sign</param>
        /// <param name="keyPair">The <see cref="KeyPair"/> to sign the <see cref="data"/> with</param>
		public abstract byte[] GenerateSignature(byte[] data,KeyPair keyPair);

        /// <summary>
        /// Validates the signature on the <see cref="MessageData"/> 
        /// </summary>
        /// <param name="data">The data to validate</param>
        /// <param name="signature">The signature to validate for the data</param>
        /// <param name="publicKey">The public part of the <see cref="KeyPair"/> used to sign the <see cref="data"/></param>
        /// <returns></returns>
		public abstract bool ValidateSignature(byte[] data,byte[] signature,byte[] publicKey);


        /// <summary>
        /// Generates a new KeyPair needed for signing messages
        /// </summary>
        /// <returns></returns>
        public abstract SigningKeyPair GenerateKeyPair();

        /// <summary>
        /// The name of the Algorythm
        /// </summary>
        [IgnoreMember]
        public abstract string Name {get;}

	}

    /// <summary>
    /// The default signature algorythm implementation
    /// </summary>
    [MessagePackObject]
    public class LibsodiumSignature : SigningAlgorythm
    {
        /// <inheritdoc/>
        public override string Name => "EdDSA";

        /// <inheritdoc/>
        public override SigningKeyPair GenerateKeyPair()
        {
            SigningKeyPair keyPair = new SigningKeyPair(new byte[32],new byte[64]);


            if(NativeLibsodium.crypto_sign_keypair(keyPair.publicKey,keyPair.secretKey) != 0)
                throw new Exception("Could not create new KeyPair");

            keyPair.algorythm = this;
            return keyPair;
        }

        /// <inheritdoc/>
        public override byte[] GenerateSignature(byte[] data,KeyPair keyPair)
        {
            return LibsodiumEncryption.SignByteDetached(data,keyPair);
        }

        /// <inheritdoc/>
        public override bool ValidateSignature(byte[] data,byte[] signature, byte[] publicKey)
        {
            return LibsodiumEncryption.SignVertifyDetached(signature,data,publicKey);
        }

        
    }


    [MessagePackObject]
    public class Signature
    {
        [Key(0)]
        public SigningAlgorythm algorythm;
        [Key(1)]
        public byte[] content;


        public void GenerateSignature(byte[] signedContent,KeyPair keyPair)
        {
            content = algorythm.GenerateSignature(signedContent,keyPair);
        }

        public bool ValidateSignature(byte[] signedContent,byte[] publicKey)
        {
            return algorythm.ValidateSignature(signedContent,this.content,publicKey);
        }
        
    }
}