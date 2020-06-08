using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace imageEncryptor
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("=[ Welcome to Image Encryptor by IKTeam v0.1 ]=");
                Console.WriteLine();
                Console.WriteLine("=[1]= - Encrypt an image.");
                Console.WriteLine("=[2]= - Decrypt an image.");
                Console.WriteLine();
                Console.Write("(1 or 2): ");
                string answ = Console.ReadLine();
                Console.Clear();
                if (answ == "1")
                {
                    Console.Write("==> Path to an image (ex. image.png or C:/image.png). Shouldn't have transparency: ");
                    string path = Console.ReadLine();
                    Console.Write("==> Password (necessary to decrypt. Leave empty to none.): ");
                    string password = Console.ReadLine();
                    if (password.Length == 0)
                        password = "D3faultPassw0rd";
                    Console.Write("==> Contents to be encrypted (only text for now.): ");
                    string content = Console.ReadLine();
                    string ToBeEncrypted = "IKT" + content;
                    byte[] Encrypted = EncryptText(ToBeEncrypted, password);
                    List<byte> EncList = Encrypted.ToList();
                    Console.WriteLine("[+] Started processing! [+]");
                    if (File.Exists(path))
                    {
                        Bitmap b = new Bitmap(path);
                        int W = b.Width;
                        int H = b.Height;
                        if (W % 4 == 0)
                        {
                            Console.WriteLine("[!!!] Encrypted image content WILL differ from old image. Press ENTER to acknowledge.");
                            Console.ReadLine();
                        }
                        if (Encrypted.Length > H * (W % 4))
                        {
                            Console.WriteLine("[!!!] Text to encode is too big to fit inside an image. Program will be stopped.");
                            Console.WriteLine("Length specified (ENCRYPTED data): " + Encrypted.Length + ". Should not exceed: " + H * (W % 4));
                            Console.ReadLine();
                            break;
                        }
                        Color[,] colormap = new Color[W, H]; //It just feels better to work with, IMO.
                        for (int y = 0; y < H; y++)
                        {
                            for (int x = 0; x < W; x++)
                            {
                                if (W % 4 == 0 && x == W - 1)
                                {

                                }
                                else
                                {
                                    Color pre = b.GetPixel(x, y);
                                    var newColor = Color.FromArgb(255, pre.R, pre.G, pre.B);
                                    colormap[x, y] = pre;
                                }
                            }
                        }
                        if (W % 4 == 0)
                        {
                            W = colormap.GetLength(0);
                            H = colormap.GetLength(1);
                        }
                        //Now, the nasty part begins.
                        using (FileStream s = new FileStream("new.png", FileMode.Create))
                        {
                            //First, write initial header
                            Baker.First(s);
                            //Now, we should write BMP size header
                            int PixelAmount = W * H;
                            int BMPSize = PixelAmount * 3 + //1 pixel = 3 bytes.
                                H*(W%4) + //Total length of padding
                                54; //54 bytes is the length of the whole header part.
                            Baker.Second(s, BMPSize);
                            //Application specific header. It will be...
                            Baker.Third(s, "IKTM"); //IKTM
                            //Static offset of pixel data.
                            Baker.Fourth(s);
                            //DIB header, fully handled by one function (it's mostly static.)
                            Baker.DIB(s, W, H);
                            //Now, we've got ourselves a nice base file for hiding stuff.
                            int AmountOfBytesPerWidth = W % 4; //Shows how much we can hide in every line.
                            int CurrentOffset = 0; //Where we stopped reading from encrypted
                            for (int y = 0; y < H; y++)
                            {
                                for (int x = 0; x < W; x++)
                                {
                                    Color clr = colormap[x, y];
                                    byte R = BitConverter.GetBytes((byte)clr.R)[0];
                                    byte G = BitConverter.GetBytes((byte)clr.G)[0];
                                    byte B = BitConverter.GetBytes((byte)clr.B)[0]; 
                                    s.Write(new byte[3] { B, G, R }, 0, 3); //We just copy pixels
                                }
                                //And once the line has ended (y = 0, x = 255, for example...)
                                if (CurrentOffset <= EncList.Count)
                                {
                                    byte[] WriteB = default(byte[]);
                                    if (CurrentOffset + AmountOfBytesPerWidth <= EncList.Count)
                                    {
                                        WriteB = EncList.GetRange(CurrentOffset, AmountOfBytesPerWidth).ToArray();
                                    }
                                    else
                                    {
                                        WriteB = EncList.GetRange(CurrentOffset, EncList.Count - CurrentOffset).ToArray();
                                    }
                                    s.Write(WriteB, 0, WriteB.Length); //We write encrypted data as a padding.
                                    CurrentOffset += WriteB.Length;
                                }
                                else
                                {
                                    byte[] Padding = new byte[AmountOfBytesPerWidth];
                                    s.Write(Padding, 0, Padding.Length);
                                }
                            }
                            //Yeah. That's it. I think?
                            s.Flush();
                            //Hehe.
                            Console.WriteLine("[+++] Done encrypting. New file saved as new.png. [+++]");
                        }
                        Console.WriteLine("Press ENTER to continue...");
                        Console.ReadLine();
                        Console.Clear();
                        continue;
                    }
                }
                else if (answ == "2")
                {

                }
            }
        }

        public static byte[] EncryptText(string text, string key)
        {
            byte[] encrypted;
            byte[] Key = Encoding.UTF8.GetBytes(key);
            List<byte> k = Key.ToList();
            while (k.Count % 16 != 0)
            {
                k.Add(0x00);
            }
            Key = k.ToArray();
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.Mode = CipherMode.ECB; //we use ECB
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(text);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

    }
    public class Baker {
        private static byte[] IDField = { 0x42, 0x4D }; //"BM"
        private static byte[] DataOffset = { 0x36, 0x00, 0x00, 0x00 }; //54.
        private static byte[] DIBNumber = { 0x28, 0x00, 0x00, 0x00 }; //40.
        private static byte[] DIB_MagicBytes_1 = {
            0x01, 0x00,//plane amount
            0x18, 0x00,//Bits per pixel.
            0x00, 0x00, 0x00, 0x00 //zero compression
        };
        private static byte[] DIB_MagicBytes_2 =
        {
            0x13, 0x0B, 0x00, 0x00, //Printing details =======
            0x13, 0x0B, 0x00, 0x00, //========================
            0x00, 0x00, 0x00, 0x00, //colors amount
            0x00, 0x00, 0x00, 0x00 // all colors are important
        };

        /// <summary>
        /// Adds "BM" header to the file.
        /// </summary>
        /// <param name="s"></param>
        public static void First(FileStream s)
        {
            s.Write(IDField, 0, IDField.Length);
        }
        /// <summary>
        /// Adds BMP size header to the file.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="BMPSize"></param>
        public static void Second(FileStream s, int BMPSize)
        {
            byte[] Bytes = BitConverter.GetBytes(BMPSize);
            s.Write(Bytes, 0, Bytes.Length);
        }
        /// <summary>
        /// Adds program-specific unused value to the file.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="header"></param>
        public static void Third(FileStream s, string header)
        {
            Third(s, Encoding.UTF8.GetBytes(header));
        }
        public static void Third(FileStream s, byte[] header)
        {
            if (header.Length == 4) {
                s.Write(header, 0, header.Length);
            } else
            {
                throw new ArgumentException("Header length should be 4 bytes.");
            }
        }
        /// <summary>
        /// Writes offset to pixel array to the file.
        /// </summary>
        /// <param name="s"></param>
        public static void Fourth(FileStream s)
        {
            s.Write(DataOffset, 0, DataOffset.Length);
        }
        /// <summary>
        /// Manages DIB header creation
        /// </summary>
        /// <param name="s"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void DIB(FileStream s, int w, int h)
        {
            s.Write(DIBNumber, 0, DIBNumber.Length);
            byte[] WBytes = BitConverter.GetBytes(w);
            byte[] HBytes = BitConverter.GetBytes(h);
            s.Write(WBytes, 0, WBytes.Length);
            s.Write(HBytes, 0, HBytes.Length);
            s.Write(DIB_MagicBytes_1, 0, DIB_MagicBytes_1.Length);
            byte[] RawBitmapSize = BitConverter.GetBytes(w * h * 4);
            s.Write(RawBitmapSize, 0, RawBitmapSize.Length);
            s.Write(DIB_MagicBytes_2, 0, DIB_MagicBytes_2.Length);
        }
    }
}
