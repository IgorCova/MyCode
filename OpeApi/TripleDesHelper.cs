using Api.Classes;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Api.Tools {
  /// <summary>
  /// TripleDES encryption and decryption 
  ///
  /// Author: Igor Cova
  /// Date created: October 19, 2018
  /// </summary>
  public class TripleDESHelper {
    private readonly byte[] _key;
    private readonly byte[] _vector;

    public TripleDESHelper() {
      TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
      _key = tdes.Key;
      _vector = tdes.IV;
    }

    public TripleDESHelper(DESParameters des) {
      _key = Convert.FromBase64String(des.Key);
      _vector = Convert.FromBase64String(des.Vector);
    }

    public string GetParameters() {
      DESParameters prms = new DESParameters() {
        Key = Convert.ToBase64String(_key),
        Vector = Convert.ToBase64String(_vector)
      };

      return JsonConvert.SerializeObject(prms);
    }

    public string Encrypt(string text) {
      if (_key == null) {
        throw new Exception("_key is null");
      }
      if (_vector == null) {
        throw new Exception("_vector is null");
      }

      return Convert.ToBase64String(Encrypt(text, _key, _vector));
    }

    public string Decrypt(string cipherText) {
      if (_key == null) {
        throw new Exception("_key is null");
      }
      if (_vector == null) {
        throw new Exception("_vector is null");
      }

      return Decrypt(cipherText, _key, _vector);
    }

    public static byte[] Encrypt(string plainText, byte[] Key, byte[] IV) {
      byte[] encrypted;
      // Create a new TripleDESCryptoServiceProvider.  
      using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider()) {
        // Create encryptor  
        ICryptoTransform encryptor = tdes.CreateEncryptor(Key, IV);
        // Create MemoryStream  
        using (MemoryStream ms = new MemoryStream()) {
          // Create crypto stream using the CryptoStream class. This class is the key to encryption  
          // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream  
          // to encrypt  
          using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
            // Create StreamWriter and write data to a stream  
            using (StreamWriter sw = new StreamWriter(cs))
              sw.Write(plainText);
            encrypted = ms.ToArray();
          }
        }
      }
      // Return encrypted data  
      return encrypted;
    }

    public static string Decrypt(string cipherText, byte[] Key, byte[] IV) {
      byte[] decrypted = Convert.FromBase64String(cipherText);
      string plaintext = null;
      // Create TripleDESCryptoServiceProvider  
      using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider()) {
        // Create a decryptor  
        ICryptoTransform decryptor = tdes.CreateDecryptor(Key, IV);
        // Create the streams used for decryption.  
        using (MemoryStream ms = new MemoryStream(decrypted)) {
          // Create crypto stream  
          using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
            // Read crypto stream  
            using (StreamReader reader = new StreamReader(cs))
              plaintext = reader.ReadToEnd();
          }
        }
      }
      return plaintext;
    }

    private static void EncryptFile(String inName, String outName, byte[] desKey, byte[] desIV) {
      //Create the file streams to handle the input and output files.  
      FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read);
      FileStream fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write);
      fout.SetLength(0);
      //Create variables to help with read and write.  
      byte[] bin = new byte[100]; //This is intermediate storage for the encryption.  
      long rdlen = 0; //This is the total number of bytes written.  
      long totlen = fin.Length; //This is the total length of the input file.  
      int len; //This is the number of bytes to be written at a time.  
      DES des = new DESCryptoServiceProvider();
      CryptoStream encStream = new CryptoStream(fout, des.CreateEncryptor(desKey, desIV), CryptoStreamMode.Write);
      Console.WriteLine("Encrypting...");
      //Read from the input file, then encrypt and write to the output file.  
      while (rdlen < totlen) {
        len = fin.Read(bin, 0, 100);
        encStream.Write(bin, 0, len);
        rdlen = rdlen + len;
        Console.WriteLine("{0} bytes processed", rdlen);
      }
      encStream.Close();
      fout.Close();
      fin.Close();
    }
  }
}