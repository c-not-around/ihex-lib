using System;
using System.IO;
using System.Collections.Generic;
using System.IntelHex;


namespace Example
{
    internal class Program
    {
        static void CleanTestFile(string fname)
        {
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }
        }

        static void Main(string[] args)
        {
#if false
            #region ATtiny2313 dump example
            CleanTestFile(@"TestData\f_2313.hex");
            CleanTestFile(@"TestData\e_2313.hex");

            ushort[] flash = IntelHex.ReadWords(@"TestData\t2313_flash.hex", 1024);
            IntelHex.SaveWords(@"TestData\f_2313.hex", flash);

            byte[] eeprom = IntelHex.ReadBytes(@"TestData\t2313_eep.hex", 256);
            IntelHex.SaveBytes(@"TestData\e_2313.hex", eeprom);
            #endregion
#endif

#if false
            #region PIC18F2550 dump example
            CleanTestFile(@"TestData\test.hex");

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
            #region PIC12F1840 dump example + SaveBytesReduced example
            CleanTestFile(@"TestData\boot_rb.hex");
            
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

#if false
            #region PIC12F1840 dump example + SaveWordsReduced example
            CleanTestFile(@"TestData\boot_wr.hex");

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

#if false
            #region PIC18F2550 dump exaple + ReadDump Segments example
            CleanTestFile(@"TestData\testw.hex");

            List<IntelHexSegment> dump = IntelHex.ReadDump(@"TestData\PK2V023200.hex", new IntelHexSegmentInfo[]
            {
                new IntelHexSegmentInfo(0x00000000, 16384 ), // Flash
                new IntelHexSegmentInfo(0x00100000, 4     ), // UserIds
                new IntelHexSegmentInfo(0x00180000, 7     ), // Config words
                new IntelHexSegmentInfo(0x00780000, 128   )  // Eeprom
            });

            IntelHex.SaveWords(@"TestData\testw.hex", dump[0].Words, 0x00000000, false);
            IntelHex.SaveWords(@"TestData\testw.hex", dump[3].Words, 0x00780000, false);
            IntelHex.SaveWords(@"TestData\testw.hex", dump[2].Words, 0x00180000, false);
            IntelHex.SaveWords(@"TestData\testw.hex", dump[1].Words, 0x00100000, true);
            #endregion
#endif

#if true
            #region PIC12F1840 ReadDump Segments example + SaveWordsReduced example
            CleanTestFile(@"TestData\project_wr.hex");

            List<IntelHexSegment> dump = IntelHex.ReadDump(@"TestData\project.hex", new IntelHexSegmentInfo[]
            {
                new IntelHexSegmentInfo(0x0000, 4096, IntelHexSegmentType.Word, 0x3FFF), // Flash
                new IntelHexSegmentInfo(0x8000, 4,    IntelHexSegmentType.Word, 0x3FFF), // UserIds
                new IntelHexSegmentInfo(0x8007, 2,    IntelHexSegmentType.Word, 0x3FFF), // Config words
                new IntelHexSegmentInfo(0xF000, 256,  IntelHexSegmentType.Word, 0x3FFF)  // Eeprom
            });

            IntelHex.SaveWordsReduced(@"TestData\project_wr.hex", dump[0].Words, 0x0000, false, 0x3FFF);
            IntelHex.SaveWordsReduced(@"TestData\project_wr.hex", dump[1].Words, 0x8000, false, 0x3FFF);
            IntelHex.SaveWordsReduced(@"TestData\project_wr.hex", dump[2].Words, 0x8007, false, 0x3FFF);
            IntelHex.SaveWordsReduced(@"TestData\project_wr.hex", dump[3].Words, 0xF000, true,  0x3FFF);
            #endregion
#endif

            Console.WriteLine("\r\ndone.");
            Console.ReadKey();
        }
    }
}