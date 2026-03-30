using System;
using System.IO;
using System.IntelHex;


namespace Example
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if false
            #region ATtiny2313 dump example
            ushort[] flash = IntelHex.ReadWords(@"TestData\t2313_flash.hex", 1024);
            IntelHex.SaveWords(@"TestData\f_2313.hex", flash);

            byte[] eeprom = IntelHex.ReadBytes(@"TestData\t2313_eep.hex", 256);
            IntelHex.SaveBytes(@"TestData\e_2313.hex", eeprom);
            #endregion
#endif

#if false
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

            IntelHex.SaveBytes(@"TestData\test.hex", dump[0], 0x00000000, false);
            IntelHex.SaveBytes(@"TestData\test.hex", dump[3], 0x00F00000, false);
            IntelHex.SaveBytes(@"TestData\test.hex", dump[2], 0x00300000, false);
            IntelHex.SaveBytes(@"TestData\test.hex", dump[1], 0x00200000, true);
            #endregion
#endif

#if false
            #region PIC12F1840 dump exaple + SaveBytesReduced example
            if (File.Exists(@"TestData\boot_rb.hex"))
            {
                File.Delete(@"TestData\boot_rb.hex");
            }
            
            byte[] program = IntelHex.ReadBytes(@"TestData\boot.hex", 4096*2); // 14bit-word dump of flash read as byte array
            IntelHex.SaveBytesReduced(@"TestData\boot_br.hex", program, 0);

            byte[] prog = IntelHex.ReadBytes(@"TestData\boot_br.hex", 4096*2);

            for (int i = 0; i < prog.Length; i++)
            {
                if (prog[i] != program[i])
                {
                    Console.WriteLine($"{i:X4}: {prog[i]:X2} != {program[i]:X2}");
                    Console.WriteLine("test failed!");

                    break;
                }
            }
            #endregion
#endif

#if true
            #region PIC12F1840 dump exaple + SaveWordsReduced example
            if (File.Exists(@"TestData\boot_wr.hex"))
            {
                File.Delete(@"TestData\boot_wr.hex");
            }

            ushort[] programw = IntelHex.ReadWords(@"TestData\boot.hex", 4096, 0x3FFF);
            IntelHex.SaveWordsReduced(@"TestData\boot_wr.hex", programw, 0, true, 0x3FFF);

            ushort[] progw = IntelHex.ReadWords(@"TestData\boot_wr.hex", 4096, 0x3FFF);

            for (int i = 0; i < progw.Length; i++)
            {
                if (progw[i] != programw[i])
                {
                    Console.WriteLine($"{i:X4}: {progw[i]:X4} != {programw[i]:X4}");
                    Console.WriteLine("test failed!");

                    break;
                }
            }
            #endregion
#endif
            Console.WriteLine("\r\ndone.");
            Console.ReadKey();
        }
    }
}