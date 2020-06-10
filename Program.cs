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
                Console.WriteLine("=[ Welcome to Image Encryptor by IKTeam v1.0 ]=");
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
                    Console.Write("==> Password (necessary to decrypt. Leave empty to none. No longer than 24): ");
                    string password = Console.ReadLine();
                    if (password.Length == 0)
                        password = "D3faultPassw0rd";
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
                        Color[,] colormap = null;
                        if (W % 4 == 0)
                        {
                            colormap = new Color[W + 1, H];
                        }
                        else
                        {
                            colormap = new Color[W, H]; //It just feels better to work with, IMO.
                        }
                        var InitW = b.Width;
                        var InitH = b.Height;
                        if (W % 4 == 0)
                        {
                            W = colormap.GetLength(0);
                            H = colormap.GetLength(1);
                        }
                        for (int y = 0; y < H; y++)
                        {
                            for (int x = 0; x < W; x++)
                            {
                                Color pre = Color.White;
                                if (InitW % 4 == 0 && x == W - 1)
                                {
                                    pre = b.GetPixel(x - 1, y); //We basically extend picture 1 pixel to right.
                                }
                                else
                                {
                                    pre = b.GetPixel(x, y); //Otherwise just copy it entirely to Color[,] 2D array
                                }
                                var newColor = Color.FromArgb(255, pre.R, pre.G, pre.B);
                                colormap[x, y] = pre;

                            }
                        }
                        Console.Write("==> Contents to be encrypted (only text for now.): ");
                        string content = Console.ReadLine();
                        string ToBeEncrypted = "IKT" + content;
                        byte[] Encrypted = Encrypt(Encoding.UTF8.GetBytes(ToBeEncrypted), Encoding.UTF8.GetBytes(password));
                        List<byte> EncList = Encrypted.ToList();
                        if (Encrypted.Length > (H * (W % 4)-1))
                        {
                            Console.WriteLine("[!!!] Text to encode is too big to fit inside an image. Program will be stopped.");
                            Console.WriteLine("Length specified (ENCRYPTED data): " + Encrypted.Length + ". Should not exceed: " + H * (W % 4));
                            Console.ReadLine();
                            break;
                        }
                        //Now, the nasty part begins.
                        using (FileStream s = new FileStream("new.bmp", FileMode.Create))
                        {
                            //First, write initial header
                            Baker.BMHeader(s);
                            //Now, we should write BMP size header
                            int PixelAmount = W * H;
                            int BMPSize = PixelAmount * 3 + //1 pixel = 3 bytes.
                                H * (W % 4) + //Total length of padding
                                54; //54 bytes is the length of the whole header part.
                            Baker.FileSizeHeader(s, BMPSize);
                            //Application specific header. It will be...
                            Baker.IKTMHeader(s, "IKTM"); //IKTM
                            //Static offset of pixel data.
                            Baker.PixelOffsetHeader(s);
                            //DIB header, fully handled by one function (it's mostly static.)
                            Baker.DIB(s, W, H);
                            //Now, we've got ourselves a nice base file for hiding stuff.
                            int AmountOfBytesPerWidth = W % 4; //Shows how much we can hide in every line.
                            int CurrentOffset = 0; //Where we stopped reading from encrypted
                            if (EncList[EncList.Count - 1] == 0x00)
                            {
                                while (EncList.Count < (H * AmountOfBytesPerWidth)) EncList.Add(0x01);
                            } else
                            {
                                while (EncList.Count < (H * AmountOfBytesPerWidth)) EncList.Add(0x00);
                            }
                            for (int y = H - 1; y >= 0; y--)
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

                                var WriteB = EncList.GetRange(CurrentOffset, AmountOfBytesPerWidth).ToArray();
                                s.Write(WriteB, 0, WriteB.Length); //We write encrypted data as a padding.
                                CurrentOffset += WriteB.Length;
                            }
                            //Yeah. That's it. I think?
                            s.Flush();
                            //Hehe.
                            Console.WriteLine("[+++] Done encrypting. New file saved as new.bmp. [+++]");
                        }
                    } else
                    {
                        Console.WriteLine("[---] Cannot find the file specified! [---]");
                    }
                    
                    
                }
                else if (answ == "2")
                {
                    Console.Write("===> Path to an encrypted image: ");
                    string path = Console.ReadLine();
                    if (File.Exists(path))
                    {
                        byte[] FileContents = File.ReadAllBytes(path);
                        int PictureLocation = IndexOf(FileContents, new byte[4] { 0x49, 0x4B, 0x54, 0x4D }); //"IKTM"
                        if (PictureLocation >= 6)
                        {
                            List<byte> Contents = FileContents.ToList();
                            byte[] FSBytes = Contents.GetRange(PictureLocation - 4, 4).ToArray();
                            int FileSize = BitConverter.ToInt32(FSBytes);
                            byte[] ImageContents = Contents.GetRange(PictureLocation - 6, FileSize).ToArray();
                            List<byte> ICList = ImageContents.ToList();
                            byte[] PPBytes = ICList.GetRange(10, 4).ToArray();
                            int PixelPointer = BitConverter.ToInt32(PPBytes);
                            byte[] RSBytes = ICList.GetRange(34, 4).ToArray();
                            int RawSize = BitConverter.ToInt32(RSBytes);
                            byte[] WBytes = ICList.GetRange(18,4).ToArray();
                            int Width = BitConverter.ToInt32(WBytes);

                            //Now let's do decoding by having all necessary info
                            int BytesPerLine = Width % 4;
                            byte[] BitmapData = ICList.GetRange(PixelPointer, RawSize).ToArray();
                            List<byte> BMPList = BitmapData.ToList();
                            List<byte> EncryptedBytes = new List<byte>();
                            for (int i = Width*3; i<RawSize; i+=Width*3)
                            {
                                EncryptedBytes.AddRange(BMPList.GetRange(i, BytesPerLine));
                                i += BytesPerLine;
                            }
                            Console.WriteLine("[+] Loaded successfully! [+]");
                            Console.WriteLine("[~] Payload size (encrypted): " + EncryptedBytes.Count);
                            Console.Write("===> Password (leave empty for none): ");
                            string password = Console.ReadLine();
                            if (password.Length == 0)
                                password = "D3faultPassw0rd";
                            byte trun = EncryptedBytes[EncryptedBytes.Count - 1];
                            byte[] tbd = Trim(EncryptedBytes.ToArray(), trun);
                            byte[] decrypted = Decrypt(tbd, Encoding.UTF8.GetBytes(password));
                            string finalResult = Encoding.UTF8.GetString(decrypted);
                            if (finalResult.StartsWith("IKT"))
                            {
                                Console.WriteLine("[+++] Successfully extracted text. [+++]");
                                Console.WriteLine("Text representation: " + finalResult.Substring(3));
                            } else
                            {
                                Console.WriteLine("[---] Couldn't confirm the password.");
                                Console.WriteLine("Extracted content length: "+decrypted.Length);
                                Console.WriteLine("Text representation: " + finalResult);
                                Console.WriteLine("[---] If text is corrupt, wrong password...");
                            }
                        } else
                        {
                            Console.WriteLine("[---] Image seems to be corrupted!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[---] Cannot find the file you've specified!");
                    }
                }
                Console.WriteLine("Press ENTER to continue...");
                Console.ReadLine();
                Console.Clear();
            }
        }
        public static int IndexOf(byte[] arrayToSearchThrough, byte[] patternToFind)
        {
            if (patternToFind.Length > arrayToSearchThrough.Length)
                return -1;
            for (int i = 0; i < arrayToSearchThrough.Length - patternToFind.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < patternToFind.Length; j++)
                {
                    if (arrayToSearchThrough[i + j] != patternToFind[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        public static byte[] Encrypt(byte[] toEncrypt, byte[] key)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            List<byte> K = new List<byte>();
            K = key.ToList();
            while (K.Count < 24)
            {
                K.Add(0x00);
            }
            key = K.ToArray();
            tdes.Key = key;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.Zeros;
            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncrypt, 0, toEncrypt.Length);
            tdes.Clear();
            return resultArray;
        }
        public static byte[] Decrypt(byte[] Encrypted, byte[] key)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            List<byte> K = new List<byte>();
            K = key.ToList();
            while (K.Count < 24)
            {
                K.Add(0x00);
            }
            key = K.ToArray();
            tdes.Key = key;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.Zeros;
            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
            tdes.Clear();
            return resultArray;
        }
        public static byte[] Trim(byte[] input, byte to_trim)
        {
            List<byte> inp = input.ToList();
            for (int i = inp.Count-1; i>0; i--)
            {
                if (inp[i] == to_trim)
                {
                    inp.RemoveAt(i);
                } else
                {
                    break;
                }
            }
            return inp.ToArray();
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
        public static void BMHeader(FileStream s)
        {
            s.Write(IDField, 0, IDField.Length);
        }
        /// <summary>
        /// Adds BMP size header to the file.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="BMPSize"></param>
        public static void FileSizeHeader(FileStream s, int BMPSize)
        {
            byte[] Bytes = BitConverter.GetBytes(BMPSize);
            s.Write(Bytes, 0, Bytes.Length);
        }
        /// <summary>
        /// Adds program-specific unused value to the file.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="header"></param>
        public static void IKTMHeader(FileStream s, string header)
        {
            IKTMHeader(s, Encoding.UTF8.GetBytes(header));
        }
        public static void IKTMHeader(FileStream s, byte[] header)
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
        public static void PixelOffsetHeader(FileStream s)
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
            int padding = w % 4;
            byte[] RawBitmapSize = BitConverter.GetBytes((w*3 + padding) * h);
            s.Write(RawBitmapSize, 0, RawBitmapSize.Length);
            s.Write(DIB_MagicBytes_2, 0, DIB_MagicBytes_2.Length);
        }
    }
}
