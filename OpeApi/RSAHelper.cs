using Api.Classes;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Api.Tools {
  /// <summary>
  /// RSA encryption and decryption using OpenSSL public key encryption / private key decryption
  ///
  /// Public and private keys, please use openssl to generate ssh-keygen -t rsa command generated public key private key is not acceptable
  ///
  /// Author: Igor Cova
  /// Date created: October 17, 2018
  /// </summary>
  public class RSAHelper : IDisposable {
    internal bool disposed = false;
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
      if (disposed)
        return;

      if (disposing) {
        _privateKeyRsaProvider.Dispose();
        _publicKeyRsaProvider.Dispose();
      }

      disposed = true;
    }

    private readonly RSA _privateKeyRsaProvider;
    private readonly RSA _publicKeyRsaProvider;
    private readonly HashAlgorithmName _hashAlgorithmName;
    private readonly Encoding _encoding;
    private readonly RSAParameters _publicKeyInfo;
    private readonly RSAParameters _privateKeyInfo;

    /// <summary>
    /// RSAHelper
    /// </summary>
    /// <param name="rsaType">Encryption algorithm type RSA SHA1; RSA2 SHA256 key length is at least 2048</param>
    /// <param name="encoding">Encoding type</param>
    /// <param name="privateKey">Private key</param>
    /// <param name="publicKey">Public key</param>
    public RSAHelper(RSAType rsaType, Encoding encoding, string privateKey, string publicKey = null) {
      _encoding = encoding;
      if (!string.IsNullOrEmpty(privateKey)) {
        _privateKeyRsaProvider = CreateRsaProviderFromPrivateKey(privateKey, out _privateKeyInfo);
      }

      if (!string.IsNullOrEmpty(publicKey)) {
        _publicKeyRsaProvider = CreateRsaProviderFromPublicKey(publicKey, out _publicKeyInfo);
      }

      _hashAlgorithmName = rsaType == RSAType.RSA ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
    }

    #region Sign with private key
    /// <summary>
    /// Sign with private key
    /// </summary>
    /// <param name="data">Raw data</param>
    /// <returns></returns>
    public string Sign(string data) {
      byte[] dataBytes = _encoding.GetBytes(data);
      byte[] signatureBytes = _privateKeyRsaProvider.SignData(dataBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

      return Convert.ToBase64String(signatureBytes);
    }
    #endregion Sign with private key 

    #region Use public key to verify signature
    /// <summary>
    /// Use public key to verify signature
    /// </summary>
    /// <param name="data">Raw data</param>
    /// <param name="sign">Signature</param>
    /// <returns></returns>
    public bool Verify(string data, string sign) {
      byte[] dataBytes = _encoding.GetBytes(data);
      byte[] signBytes = Convert.FromBase64String(sign);

      bool verify = _publicKeyRsaProvider.VerifyData(dataBytes, signBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

      return verify;
    }
    #endregion  Use public key to verify signature

    #region Decryption
    public string Decrypt(string cipherText) {
      if (_privateKeyRsaProvider == null) {
        throw new Exception("_privateKeyRsaProvider is null");
      }

      string decrypted = string.Empty;
      try {
        decrypted = Encoding.UTF8.GetString(_privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.Pkcs1));
      } catch (Exception e) {
        Console.Out.WriteLine($"Decryption error: {e.Message}");
        throw new ApiException(CodeStatus.Warning, "Decryption error");
      }

      return decrypted;
    }
    #endregion Decryption

    #region Encryption
    public string Encrypt(string text) {
      if (_publicKeyRsaProvider == null) {
        throw new Exception("_publicKeyRsaProvider is null");
      }
      return Convert.ToBase64String(_publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
    }
    #endregion Encryption

    #region Create an RSA instance with a private key
    public RSA CreateRsaProviderFromPrivateKey(string privateKey, out RSAParameters rsaKeyInfo) {
      byte[] privateKeyBits = Convert.FromBase64String(privateKey);

      RSA rsa = RSA.Create();
      rsaKeyInfo = new RSAParameters();

      using (BinaryReader binr = new BinaryReader(new MemoryStream(privateKeyBits))) {
        byte bt = 0;
        ushort twobytes = 0;
        twobytes = binr.ReadUInt16();
        if (twobytes == 0x8130)
          binr.ReadByte();
        else if (twobytes == 0x8230)
          binr.ReadInt16();
        else
          throw new Exception("Unexpected value read binr.ReadUInt16()");

        twobytes = binr.ReadUInt16();
        if (twobytes != 0x0102)
          throw new Exception("Unexpected version");

        bt = binr.ReadByte();
        if (bt != 0x00)
          throw new Exception("Unexpected value read binr.ReadByte()");

        rsaKeyInfo.Modulus = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.Exponent = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.D = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.P = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.Q = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.DP = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.DQ = binr.ReadBytes(GetIntegerSize(binr));
        rsaKeyInfo.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
      }

      rsa.ImportParameters(rsaKeyInfo);

      //  _publicKeyInfo = rsaParameters;
      return rsa;
    }
    #endregion Create an RSA instance with a private key

    #region Create an RSA instance using a public key
    public RSA CreateRsaProviderFromPublicKey(string publicKeyString, out RSAParameters rsaKeyInfo) {
      rsaKeyInfo = new RSAParameters();
      // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
      byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
      byte[] seq = new byte[15];

      byte[] x509Key = Convert.FromBase64String(publicKeyString);

      // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
      using (MemoryStream mem = new MemoryStream(x509Key)) {
        using (BinaryReader binr = new BinaryReader(mem))  //wrap Memory Stream with BinaryReader for easy reading
        {
          byte bt = 0;
          ushort twobytes = 0;

          twobytes = binr.ReadUInt16();
          if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
            binr.ReadByte();    //advance 1 byte
          else if (twobytes == 0x8230)
            binr.ReadInt16();   //advance 2 bytes
          else
            return null;

          seq = binr.ReadBytes(15);       //read the Sequence OID
          if (!CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
            return null;

          twobytes = binr.ReadUInt16();
          if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
            binr.ReadByte();    //advance 1 byte
          else if (twobytes == 0x8203)
            binr.ReadInt16();   //advance 2 bytes
          else
            return null;

          bt = binr.ReadByte();
          if (bt != 0x00)     //expect null byte next
            return null;

          twobytes = binr.ReadUInt16();
          if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
            binr.ReadByte();    //advance 1 byte
          else if (twobytes == 0x8230)
            binr.ReadInt16();   //advance 2 bytes
          else
            return null;

          twobytes = binr.ReadUInt16();
          byte lowbyte = 0x00;
          byte highbyte = 0x00;

          if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
            lowbyte = binr.ReadByte();  // read next bytes which is bytes in modulus
          else if (twobytes == 0x8202) {
            highbyte = binr.ReadByte(); //advance 2 bytes
            lowbyte = binr.ReadByte();
          } else
            return null;
          byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
          int modsize = BitConverter.ToInt32(modint, 0);

          int firstbyte = binr.PeekChar();
          if (firstbyte == 0x00) {   //if first byte (highest order) of modulus is zero, don't include it
            binr.ReadByte();    //skip this null byte
            modsize -= 1;   //reduce modulus buffer size by 1
          }

          byte[] modulus = binr.ReadBytes(modsize);   //read the modulus bytes

          if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
            return null;
          int expbytes = binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
          byte[] exponent = binr.ReadBytes(expbytes);

          // ------- create RSACryptoServiceProvider instance and initialize with public key -----
          RSA rsa = RSA.Create();
          rsaKeyInfo.Modulus = modulus;
          rsaKeyInfo.Exponent = exponent;

          rsa.ImportParameters(rsaKeyInfo);

          return rsa;
        }

      }
    }
    #endregion Create an RSA instance using a public key

    #region Import key algorithm
    private int GetIntegerSize(BinaryReader binr) {
      byte bt = 0;
      int count = 0;
      bt = binr.ReadByte();
      if (bt != 0x02)
        return 0;
      bt = binr.ReadByte();

      if (bt == 0x81)
        count = binr.ReadByte();
      else
      if (bt == 0x82) {
        byte highbyte = binr.ReadByte();
        byte lowbyte = binr.ReadByte();
        byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
        count = BitConverter.ToInt32(modint, 0);
      } else {
        count = bt;
      }

      while (binr.ReadByte() == 0x00) {
        count -= 1;
      }
      binr.BaseStream.Seek(-1, SeekOrigin.Current);
      return count;
    }

    private bool CompareBytearrays(byte[] a, byte[] b) {
      if (a.Length != b.Length)
        return false;
      int i = 0;
      foreach (byte c in a) {
        if (c != b[i])
          return false;
        i++;
      }
      return true;
    }
    #endregion Import key algorithm
  }

  /// <summary>
  /// RSA algorithm type
  /// </summary>
  public enum RSAType {
    /// <summary>
    /// SHA1
    /// </summary>
    RSA = 0,
    /// <summary>
    /// RSA2 The key length is at least 2048
    /// SHA256
    /// </summary>
    RSA2
  }
}