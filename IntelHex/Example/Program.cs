using System;
using System.IntelHex;


namespace Example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            #region ATtiny2313 dump example
            ushort[] flash = IntelHex.ReadWods(@"TestData\t2313_flash.hex", 1024);
            IntelHex.SaveWords(@"TestData\f_2313.hex", flash);

            byte[] eeprom = IntelHex.ReadBytes(@"TestData\t2313_eep.hex", 256);
            IntelHex.SaveBytes(@"TestData\e_2313.hex", eeprom);
            #endregion

            #region PIC18F2550 dump example
            byte[][] dump = IntelHex.ReadDump(@"TestData\PK2V023200.hex", new IntelHexRegion[]
            {
                new IntelHexRegion { Offset=0x00000000, Size=32768 }, // Flash
                new IntelHexRegion { Offset=0x00200000, Size=8 },     // UserIds
                new IntelHexRegion { Offset=0x00300000, Size=14 },    // Config words
                new IntelHexRegion { Offset=0x00F00000, Size=256 }    // Eeprom
            });

            Console.WriteLine("UserIds:");
            for (int i = 0; i < dump[1].Length; i++)
            {
                Console.Write(dump[1][i].ToString("X2"));
            }

            Console.WriteLine("\r\nConfiguration:");
            for (int i = 0; i < dump[2].Length; i++)
            {
                Console.Write(dump[2][i].ToString("X2"));
            }

            IntelHex.SaveBytes(@"TestData\test.hex", dump[0], 0x00000000, 16, false);
            IntelHex.SaveBytes(@"TestData\test.hex", dump[3], 0x00F00000, 16, false);
            IntelHex.SaveBytes(@"TestData\test.hex", dump[2], 0x00300000, 16, false);
            IntelHex.SaveBytes(@"TestData\test.hex", dump[1], 0x00200000, 16, true);
            #endregion

            Console.WriteLine("\r\ndone.");
            Console.ReadKey();
        }
    }
}