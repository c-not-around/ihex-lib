using System;
using System.IO;


namespace System.IntelHex
{
    public enum IntelHexRecordType : byte
    {
        Data                   = 0,
        EndOfFile              = 1,
        ExtendedSegmentAddress = 2,
        StartSegmentAddress    = 3,
        ExtendedLinearAddress  = 4,
        StartLinearAddress     = 5
    };

    public struct IntelHexRecord
    {
        public IntelHexRecordType Type;
        public ushort             Address;
        public byte[]             Data;
    }

    public struct IntelHexRegion
    {
        public long Offset;
        public long Size;
    }

    public class IntelHex
    {
        #region Parse
        private static int HexParse(string image)
        {
            try
            {
                return Convert.ToUInt16(image, 16);
            }
            catch
            {
                return -1;
            }
        }

        public static IntelHexRecord Parse(string line)
        {
            IntelHexRecord record = new IntelHexRecord();

            if (line.Length >= 1+2+4+2+2) // :,cnt,adr,type,crc
            {
                if (line[0] == ':')
                {
                    int count = HexParse(line.Substring(1, 2));

                    if (count != -1)
                    {
                        if (line.Length == 11+2*count)
                        {
                            int address = HexParse(line.Substring(3, 4));

                            if (address != -1)
                            {
                                record.Address = (ushort)address;

                                int type = HexParse(line.Substring(7, 2));

                                if (type != -1)
                                {
                                    if (type < (int)IntelHexRecordType.StartLinearAddress)
                                    {
                                        record.Type = (IntelHexRecordType)type;
                                        record.Data = new byte[count];

                                        byte sum = (byte)((byte)count + (byte)(address >> 8) + (byte)address + (byte)type);

                                        for (int i = 0; i < count; i++)
                                        {
                                            int data = HexParse(line.Substring(9+2*i, 2));

                                            if (data != -1)
                                            {
                                                record.Data[i] = (byte)data;
                                                sum           += (byte)data;
                                            }
                                            else
                                            {
                                                throw new Exception($"{9+2*i} - data parse error.");
                                            }
                                        }

                                        int crc = HexParse(line.Substring(line.Length-2, 2));

                                        if (crc != -1)
                                        {
                                            sum = (byte)((byte)0 - sum);

                                            if (sum != crc)
                                            {
                                                throw new Exception($"checksum did not match {crc} != {sum}.");
                                            }
                                        }
                                        else
                                        {
                                            throw new Exception($"{line.Length-2} - crc parse error.");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception($"7 - {type} is incorrect type value.");
                                    }
                                }
                                else
                                {
                                    throw new Exception("7 - type parse error.");
                                }
                            }
                            else
                            {
                                throw new Exception("3 - address parse error.");
                            }
                        }
                        else
                        {
                            throw new Exception($"1 - {count} is incorrect data length for line length {line.Length}.");
                        }
                    }
                    else
                    {
                        throw new Exception("1 - data count parse error.");
                    }
                }
                else
                {
                    throw new Exception("0 - start sequence is missing.");
                }
            }
            else
            {
                throw new Exception($"{line.Length} is incorrect length for intel-hex record.");
            }

            return record;
        }
        #endregion

        #region Save line
        public static string SaveDataBytes(ushort address, byte[] dump, long index, int count)
        {
            if (count > 255 || count < 0)
            {
                throw new Exception($"incorrect byte count: {count}");
            }

            string line = $":{count:X2}{address:X4}{(byte)IntelHexRecordType.Data:X2}";
            byte   crc  = (byte)(count + (address>>8) + (address&0xFF) + (byte)IntelHexRecordType.Data);

            while (count-- > 0)
            {
                line += dump[index].ToString("X2");
                crc  += dump[index++];
            }

            crc = (byte)((byte)0 - crc);

            return line + crc.ToString("X2");
        }

        public static string SaveDataWords(ushort address, ushort[] dump, long index, int count)
        {
            if (count > 127 || count < 0)
            {
                throw new Exception($"incorrect word count: {count}");
            }

            string line = $":{(2*count):X2}{address:X4}{(byte)IntelHexRecordType.Data:X2}";
            byte   crc  = (byte)(2 * count + (address >> 8) + (address & 0xFF) + (byte)IntelHexRecordType.Data);

            while (count-- > 0)
            {
                ushort w = dump[index++];
                line    += w.ToString("X4");
                crc     += (byte)((w >> 8) + (w & 0xFF));
            }

            crc = (byte)((byte)0 - crc);

            return line + crc.ToString("X2");
        }

        public static string SaveAddress(ushort address)
        {
            byte ext = (byte)IntelHexRecordType.ExtendedLinearAddress;
            byte crc = (byte)((byte)0 - (0x02 + (address >> 8) + (address & 0xFF) + ext));

            return $":020000{ext:X2}{address:X4}{crc:X2}";
        }
        #endregion

        #region Read
        public static byte[] ReadBytes(string fname, long size)
        {
            byte[] result = new byte[size];
            for (long i = 0; i < size; i++)
            {
                result[i] = 0xFF;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(fname);
            }
            catch (Exception ex)
            {
                throw new Exception($"\"{fname}\" - file read error: {ex.Message}");
            }

            long address = 0;
            for (int line = 0; line < lines.Length; line++)
            {
                IntelHexRecord record;
                try
                {
                    record = Parse(lines[line]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"\"{fname}\" - file read error: line{line} {ex.Message}");
                }

                if (record.Type == IntelHexRecordType.Data)
                {
                    int count = record.Data.Length;
                    if ((address + count) <= size)
                    {
                        Array.Copy(record.Data, 0, result, address, count);
                    }
                    else
                    {
                        throw new Exception($"\"{fname}\" - file read error: line{line} dump overflow: cant copy {count} bytes by offset {address}.");
                    }

                    address += count;
                }
                else if (record.Type == IntelHexRecordType.ExtendedLinearAddress)
                {
                    if (record.Data.Length != 2)
                    {
                        throw new Exception($"\"{fname}\" - file read error: line{line} incorrect ExtendedLinearAddress length={record.Data.Length}.");
                    }

                    address = (record.Data[0] << 24) | (record.Data[1] << 16);
                }
                else if (record.Type == IntelHexRecordType.EndOfFile)
                {
                    break;
                }
            }

            return result;
        }

        public static ushort[] ReadWods(string fname, long size, ushort empty = 0xFFFF)
        {
            ushort[] result = new ushort[size];
            for (long i = 0; i < size; i++)
            {
                result[i] = empty;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(fname);
            }
            catch (Exception ex)
            {
                throw new Exception($"\"{fname}\" - file read error: {ex.Message}");
            }

            long address = 0;
            for (int line = 0; line < lines.Length; line++)
            {
                IntelHexRecord record;
                try
                {
                    record = Parse(lines[line]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"\"{fname}\" - file read error: line{line} {ex.Message}");
                }

                if (record.Type == IntelHexRecordType.Data)
                {
                    int count = record.Data.Length;

                    if ((count & 1) == 0)
                    {
                        long offset = address / 2;

                        if ((offset + (count / 2)) <= size)
                        {
                            for (int i = 0; i < count; i += 2)
                            {
                                result[offset++] = (ushort)((record.Data[i+0] << 8) | record.Data[i+1]);
                            }
                        }
                        else
                        {
                            throw new Exception($"dump overflow: line{line} - cant copy {count} bytes by offset {offset}.");
                        }
                    }
                    else
                    {
                        throw new Exception($"line{line} - {count} incorrect count for word dump.");
                    }

                    address += count;
                }
                else if (record.Type == IntelHexRecordType.ExtendedLinearAddress)
                {
                    if (record.Data.Length != 2)
                    {
                        throw new Exception($"\"{fname}\" - file read error: line{line} incorrect ExtendedLinearAddress length={record.Data.Length}.");
                    }

                    address = (record.Data[0] << 24) | (record.Data[1] << 16);
                }
                else if (record.Type == IntelHexRecordType.EndOfFile)
                {
                    break;
                }
            }

            return result;
        }

        public static byte[][] ReadDump(string fname, params IntelHexRegion[] regions)
        {
            int N = regions.Length;
            byte[][] result = new byte[N][];
            for (int i = 0; i < N; i++)
            {
                result[i] = new byte[regions[i].Size];
                for (long j = 0; j < result[i].Length; j++)
                {
                    result[i][j] = 0xFF;
                }
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(fname);
            }
            catch (Exception ex)
            {
                throw new Exception($"\"{fname}\" - file read error: {ex.Message}");
            }

            int  region  = 0;
            long address = 0;
            for (int line = 0; line < lines.Length; line++)
            {
                IntelHexRecord record;
                try
                {
                    record = Parse(lines[line]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"\"{fname}\" - file read error: line{line} {ex.Message}");
                }

                if (record.Type == IntelHexRecordType.Data)
                {
                    long offset = address - regions[region].Offset;
                    while (offset < 0 || offset > regions[region].Size)
                    {
                        if (++region == regions.Length)
                        {
                            region = 0;
                        }

                        offset = address - regions[region].Offset;
                    }

                    int count = record.Data.Length;
                    if ((offset + count) <= regions[region].Size)
                    {
                        Array.Copy(record.Data, 0, result[region], offset, count);
                    }
                    else
                    {
                        throw new Exception($"Region[{region}] overflow: cant copy {count} bytes by offset {offset}.");
                    }

                    address += count;
                }
                else if (record.Type == IntelHexRecordType.ExtendedLinearAddress)
                {
                    if (record.Data.Length != 2)
                    {
                        throw new Exception($"\"{fname}\" - file read error: line{line} incorrect ExtendedLinearAddress length={record.Data.Length}.");
                    }

                    address = (record.Data[0] << 24) | (record.Data[1] << 16);
                }
                else if (record.Type == IntelHexRecordType.EndOfFile)
                {
                    break;
                }
            }

            return result;
        }
        #endregion

        #region Save
        public static void SaveBytes(string fname, byte[] dump, long offset = 0, int width = 16, bool end = true)
        {
            if (width > 255 || width < 0)
            {
                throw new Exception($"incorrect line width: {width}.");
            }

            using (StreamWriter writer = File.AppendText(fname))
            {
                long index = 0;
                while (index < dump.LongLength)
                {
                    if ((offset & 0xFFFF) == 0 && offset != 0)
                    {
                        writer.WriteLine(SaveAddress((ushort)(offset >> 16)));
                    }

                    int count = (int)Math.Min(((~offset) & 0xFFFF) + 1, Math.Min(width, dump.LongLength - index));

                    writer.WriteLine(SaveDataBytes((ushort)offset, dump, index, count));

                    index  += count;
                    offset += count;
                }

                if (end)
                {
                    writer.Write(":00000001FF");
                }
            }
        }

        public static void SaveWords(string fname, ushort[] dump, long offset = 0, int width = 8, bool end = true)
        {
            if (width > 127 || width < 0)
            {
                throw new Exception($"incorrect line width: {width}.");
            }

            using (StreamWriter writer = File.AppendText(fname))
            {
                long index = 0;
                while (index < dump.LongLength)
                {
                    if ((offset & 0xFFFF) == 0 && offset != 0)
                    {
                        writer.WriteLine(SaveAddress((ushort)(offset >> 16)));
                    }

                    int count = (int)Math.Min(((~offset) & 0xFFFF) + 1, 2 * Math.Min(width, dump.LongLength - index)) / 2;

                    writer.WriteLine(SaveDataWords((ushort)offset, dump, index, count));

                    index  += count;
                    offset += count * 2;
                }

                if (end)
                {
                    writer.Write(":00000001FF");
                }
            }
        }
        #endregion
    }
}