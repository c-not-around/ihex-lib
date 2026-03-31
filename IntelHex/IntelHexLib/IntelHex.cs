using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace System.IntelHex
{
    /// <summary>
    /// Record types defined in the IntelHex format.
    /// </summary>
    public enum IntelHexRecordType : byte
    {
        Data                   = 0,
        EndOfFile              = 1,
        ExtendedSegmentAddress = 2,
        StartSegmentAddress    = 3,
        ExtendedLinearAddress  = 4,
        StartLinearAddress     = 5
    };

    /// <summary>
    /// Represents IntelHex record.
    /// </summary>
    public struct IntelHexRecord
    {
        public IntelHexRecordType Type;
        public ushort             Address;
        public byte[]             Data;
    }

    /// <summary>
    /// Represents the parameters of a segment in a segmented address space.
    /// </summary>
    public struct IntelHexRegion
    {
        /// <summary>
        /// The offset of the segment relative to address zero of the segmented address space.
        /// </summary>
        public long Offset;
        /// <summary>
        /// The size of a segment relative to address zero of the segmented address space.
        /// </summary>
        public long Size;

        /// <summary>
        /// Initializes a new IntelHexRegion object using the given Offset and Size.
        /// </summary>
        /// <param name="Offset">The offset of the segment relative to address zero of the segmented address space.</param>
        /// <param name="Size">The size of a segment relative to address zero of the segmented address space.</param>
        public IntelHexRegion(long Offset, long Size)
        {
            this.Offset = Offset;
            this.Size   = Size;
        }
    }
    
    /// <summary>
    /// Defines the data type of the segment.
    /// </summary>
    public enum IntelHexSegmentType
    {
        Byte,
        Word
    }

    /// <summary>
    /// Represents the enhanced parameters of a segment in a segmented address space.
    /// </summary>
    public struct IntelHexSegmentInfo
    {
        /// <summary>
        /// Specifies the data type of the segment.
        /// </summary>
        public IntelHexSegmentType Type;
        /// <summary>
        /// The offset of the segment relative to address zero of the segmented address space.
        /// Set according to the data type defined by the value of the Type field.
        /// </summary>
        public long                Offset;
        /// <summary>
        /// The size of a segment relative to address zero of the segmented address space.
        /// Set according to the data type defined by the value of the Type field.
        /// </summary>
        public long                Size;
        /// <summary>
        /// Specifies a 16-bit value that will be used to fill the elements of the output array for which data is missing from the file.
        /// </summary>
        public ushort              Empty;

        /// <summary>
        /// Initializes a new IntelHexSegmentInfo object using the given Type, Offset, Size and Empty data unit value.
        /// </summary>
        /// <param name="Offset">The offset of the segment relative to address zero of the segmented address space.</param>
        /// <param name="Size">The size of a segment relative to address zero of the segmented address space.</param>
        /// <param name="Type">Segment data type.</param>
        /// <param name="Empty">Value for initial filling of the output data array.</param>
        public IntelHexSegmentInfo(long Offset, long Size, IntelHexSegmentType Type = IntelHexSegmentType.Word, ushort Empty = 0xFFFF)
        {
            this.Offset = Offset;
            this.Size   = Size;
            this.Type   = Type;
            this.Empty  = Empty;
        }
    }

    /// <summary>
    /// Represents an object containing the data segment of a single segment.
    /// </summary>
    public class IntelHexSegment
    {
        #region Fields
        private byte[]   _Data;
        private ushort[] _Words;
        #endregion

        #region Ctors
        internal IntelHexSegment(IntelHexSegmentInfo info)
        {
            byte l = (byte)(info.Empty >> 0);
            byte h = (byte)(info.Empty >> 8);

            if (info.Type == IntelHexSegmentType.Word)
            {
                Offset = info.Offset << 1;
                _Data  = new byte[info.Size << 1];

                for (int i = 0; i < _Data.Length;)
                {
                    _Data[i++] = l;
                    _Data[i++] = h;
                }
            }
            else
            {
                Offset = info.Offset;
                _Data  = new byte[info.Size];

                for (int i = 0; i < info.Size; i++)
                {
                    _Data[i] = l;
                }
            }

            _Words = null;
        }
        #endregion

        #region Properties
        public long Offset { get; private set; }

        public int ByteLength => _Data.Length;

        public int WordLength => _Data.Length >> 1;

        public byte[] Bytes => _Data;

        public ushort[] Words 
        { 
            get
            {
                if (_Words == null)
                {
                    _Words = new ushort[_Data.Length >> 1];

                    for (int b = 0, w = 0; b < _Data.Length; b += 2)
                    {
                        _Words[w++] = BitConverter.ToUInt16( _Data, b);
                    }
                }

                return _Words;
            }
        }
        #endregion

        #region Methods
        public byte GetByte(int index) => _Data[index];

        public ushort GetWord(int index) => Words[index];
        #endregion

        #region Internal
        internal bool IsBelong(long address)
        {
            long offset = address - Offset;
            return (-1 < offset && offset < _Data.LongLength);
        }

        internal void SetBytes(byte[] data, long offset)
        {
            Array.Copy(data, 0, _Data, offset, data.Length);
        }
        #endregion
    }
    
    /// <summary>
    /// Contains methods for working with files in the IntelHex format. 
    /// Reading and writing both individual IntelHex records and entire dumps.
    /// </summary>
    public static class IntelHex
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

        /// <summary>
        /// Decodes the string representation of an IntelHex Record.
        /// </summary>
        /// <param name="line">IntelHex Record Image String.</param>
        /// <returns>A IntelHexRecord object representing a recognized IntelHex record.</returns>
        /// <exception cref="Exception">If the input string does not conform to the IntelHex format, an appropriate exception is thrown.</exception>
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
        /// <summary>
        /// Generates a record with IntelHex data as a string.
        /// Helper function.
        /// </summary>
        /// <param name="address">Data offset in the generated record.</param>
        /// <param name="dump">Byte array - data source.</param>
        /// <param name="index">The index in the source array from which the data will be taken.</param>
        /// <param name="count">The number of bytes from which an IntelHex record must be formed.</param>
        /// <returns>The term representing the generated IntelHex record.</returns>
        /// <exception cref="Exception">The number of data bytes specified is more than 255.</exception>
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

        /// <summary>
        /// Generates a record with IntelHex data as a string.
        /// Helper function.
        /// </summary>
        /// <param name="address">Data offset in the generated record. The byte address is specified.</param>
        /// <param name="dump">Array of word - data source.</param>
        /// <param name="index">The index in the source array from which the data will be taken.</param>
        /// <param name="count">The number of words from which an IntelHex record must be formed.</param>
        /// <returns>The term representing the generated IntelHex record.</returns>
        /// <exception cref="Exception">The number of data bytes specified is more than 127.</exception>
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
                byte   l = (byte)(w & 0xFF);
                byte   h = (byte)(w >> 8);
                line    += $"{l:X2}{h:X2}";
                crc     += (byte)(l + h);
            }

            crc = (byte)((byte)0 - crc);

            return line + crc.ToString("X2");
        }

        /// <summary>
        /// Generates an IntelHex record of type "Extended Linear Address" as a string.
        /// Helper function.
        /// </summary>
        /// <param name="address">Extended Line Address Value (32-bit address high-order part).</param>
        /// <returns>The term representing the generated IntelHex record.</returns>
        public static string SaveAddress(ushort address)
        {
            byte ext = (byte)IntelHexRecordType.ExtendedLinearAddress;
            byte crc = (byte)((byte)0 - (0x02 + (address >> 8) + (address & 0xFF) + ext));

            return $":020000{ext:X2}{address:X4}{crc:X2}";
        }
        #endregion

        #region Read
        /// <summary>
        /// Reads a byte array of data from the specified file in ItelHex format.
        /// </summary>
        /// <param name="fname">Specifies the name of the file containing the data.</param>
        /// <param name="size">The maximum expected size of the data array.</param>
        /// <returns>Array of read data bytes.</returns>
        /// <exception cref="Exception"></exception>
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
                    int  count  = record.Data.Length;
                    long offset = address + record.Address;

                    if ((offset + count) <= size)
                    {
                        Array.Copy(record.Data, 0, result, offset, count);
                    }
                    else
                    {
                        throw new Exception($"\"{fname}\" - file read error: line{line} dump overflow: cant copy {count} bytes by offset {address}.");
                    }
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

        /// <summary>
        /// Reads an word array of data from the specified file in ItelHex format.
        /// </summary>
        /// <param name="fname">Specifies the name of the file containing the data.</param>
        /// <param name="size">The maximum expected size of the data array. Specified as a number of 16-bit words.</param>
        /// <param name="empty">Specifies a 16-bit value that will be used to fill the elements of the output array of words for which data is missing from the file.</param>
        /// <returns>An array of read data bytes as an array of 16-bit words.</returns>
        /// <exception cref="Exception"></exception>
        public static ushort[] ReadWords(string fname, long size, ushort empty = 0xFFFF)
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
                        long offset = (address + record.Address) / 2;

                        if ((offset + (count / 2)) <= size)
                        {
                            for (int i = 0; i < count; i += 2)
                            {
                                result[offset++] = (ushort)((record.Data[i+1] << 8) | record.Data[i+0]);
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

        /// <summary>
        /// Reads a complex dump containing segmented data.
        /// </summary>
        /// <param name="fname">Specifies the name of the file containing the data.</param>
        /// <param name="regions">Specifies a variable number of objects describing segments of the address space.</param>
        /// <returns>An array of data arrays, each representing data from one of the individual segments.</returns>
        /// <exception cref="Exception"></exception>
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

            long MinOffset = regions.Min(r => r.Offset);
            long MaxOffset = regions.Max(r => r.Offset + r.Size);

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
                    long a     = address + record.Address;
                    int  count = record.Data.Length;

                    if (a < MinOffset || (a + count) > MaxOffset)
                    {
                        throw new Exception($"Offset 0x{a:X8} does not belong to any of the specified regions.");
                    }

                    long offset = a - regions[region].Offset;
                    while (offset < 0 || offset > regions[region].Size)
                    {
                        if (++region == regions.Length)
                        {
                            region = 0;
                        }

                        offset = a - regions[region].Offset;
                    }

                    if ((offset + count) <= regions[region].Size)
                    {
                        Array.Copy(record.Data, 0, result[region], offset, count);
                    }
                    else
                    {
                        throw new Exception($"Region[{region}] overflow: cant copy {count} bytes by offset {offset} (0x{offset:X8}).");
                    }
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

        /// <summary>
        /// Reads a complex dump containing segmented data.
        /// </summary>
        /// <param name="fname">Specifies the name of the file containing the data.</param>
        /// <param name="segments">Specifies a variable number of objects describing segments of the address space.</param>
        /// <returns>List of IntelHexSegment type objects, each representing data from one of the individual segments.</returns>
        /// <exception cref="Exception"></exception>
        public static List<IntelHexSegment> ReadDump(string fname, params IntelHexSegmentInfo[] segments)
        {
            List<IntelHexSegment> dump = new List<IntelHexSegment>();
            for (int i = 0; i < segments.Length; i++)
            {
                dump.Add(new IntelHexSegment(segments[i]));
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

            long            address = 0;
            int             seg     = 0;
            IntelHexSegment segment = dump[seg];

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
                    long a = address + record.Address;

                    if (!segment.IsBelong(a))
                    {
                        seg = dump.FindIndex(s => s.IsBelong(a));

                        if (seg != -1)
                        {
                            segment = dump[seg];
                        }
                        else
                        {
                            throw new Exception($"\"{fname}\" - file read error. line{line} Offset 0x{a:X8} does not belong to any of the specified segments.");
                        }
                    }

                    int  count  = record.Data.Length;
                    long offset = a - segment.Offset;

                    if ((offset + count) <= segment.ByteLength)
                    {
                        segment.SetBytes(record.Data, offset);
                    }
                    else
                    {
                        throw new Exception($"\"{fname}\" - file read error. line{line} Segment[{seg}] overflow: cant copy {count} bytes by offset {offset} (0x{offset:X8}).");
                    }
                }
                else if (record.Type == IntelHexRecordType.ExtendedLinearAddress)
                {
                    if (record.Data.Length != 2)
                    {
                        throw new Exception($"\"{fname}\" - file read error. line{line} Incorrect ExtendedLinearAddress length={record.Data.Length}.");
                    }

                    address = (record.Data[0] << 24) | (record.Data[1] << 16);
                }
                else if (record.Type == IntelHexRecordType.EndOfFile)
                {
                    break;
                }
            }

            return dump;
        }
        #endregion

        #region Save
        /// <summary>
        /// Appends an array of bytes to a file in IntelHex format. 
        /// </summary>
        /// <param name="fname">File name. The file is appended to, not overwritten.</param>
        /// <param name="dump">An byte array to save. Saved entirely to a hex file.</param>
        /// <param name="offset">The initial offset of the bytes to be saved in the hex file.</param>
        /// <param name="end">Specifies that an end-of-hex file marker should be added after the data being saved.</param>
        /// <param name="width">Specifies the length of one hex data record.</param>
        /// <exception cref="Exception">The hex string width is set to more than 255 bytes.</exception>
        public static void SaveBytes(string fname, byte[] dump, long offset = 0, bool end = true, int width = 16)
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

        /// <summary>
        /// Appends an array of 16-bit words to a file in IntelHex format. 
        /// </summary>
        /// <param name="fname">File name. The file is appended to, not overwritten.</param>
        /// <param name="dump">An array of words to save. Saved entirely to a hex file.</param>
        /// <param name="offset">The initial offset of the words to be saved in the hex file. Specified as a "word address" - NOT a byte address.</param>
        /// <param name="end">Specifies that an end-of-hex file marker should be added after the data being saved.</param>
        /// <param name="width">Specifies the length of one hex data record. Specified in words. The default is 8 words, i.e., 16 bytes per line.</param>
        /// <exception cref="Exception">The hex string width is set to more than 127 words.</exception>
        public static void SaveWords(string fname, ushort[] dump, long offset = 0, bool end = true, int width = 8)
        {
            if (width > 127 || width < 0)
            {
                throw new Exception($"incorrect line width: {width}.");
            }

            offset <<= 1;

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

        /// <summary>
        /// Appends an array of bytes to a file in IntelHex format, excluding empty values. 
        /// </summary>
        /// <param name="fname">File name. The file is appended to, not overwritten.</param>
        /// <param name="dump">An byte array to save. Saved entirely to a hex file.</param>
        /// <param name="offset">The initial offset of the bytes to be saved in the hex file.</param>
        /// <param name="end">Specifies that an end-of-hex file marker should be added after the data being saved.</param>
        /// <param name="empty">Specifies the value of a byte that will be considered empty. Such bytes will be skipped when saving data to a hex file.</param>
        /// <param name="width">Specifies the length of one hex data record.</param>
        /// <exception cref="Exception">The hex string width is set to more than 255 bytes.</exception>
        public static void SaveBytesReduced(string fname, byte[] dump, long offset, bool end = true, byte empty = 0xFF, int width = 16)
        {
            if (width > 255 || width < 0)
            {
                throw new Exception($"incorrect line width: {width}.");
            }

            using (StreamWriter writer = File.AppendText(fname))
            {
                long index = 0;
                int  count = 0;

                byte   crc  = 0;
                string line = "";

                ushort hadr = 0; // (ushort)((offset >> 16) & 0xFFFF);

                while (index < dump.LongLength)
                {
                    byte data = dump[index];

                    if (data != empty)
                    {
                        if (count == 0)
                        {
                            ushort adrl = (ushort)(offset & 0xFFFF);
                            ushort adrh = (ushort)((offset >> 16) & 0xFFFF);

                            if (adrh != hadr && adrh != 0)
                            {
                                writer.WriteLine(SaveAddress(adrh));
                                hadr = adrh;
                            }

                            crc  = (byte)((adrl >> 8) + (adrl & 0xFF) + (byte)IntelHexRecordType.Data);
                            line = $"{adrl:X4}{(byte)IntelHexRecordType.Data:X2}";
                        }

                        crc  += data;
                        line += data.ToString("X2");

                        count++;
                    }

                    index++;
                    offset++;

                    if (count != 0)
                    {
                        bool last = index < dump.LongLength ? dump[index] == empty : true;

                        if (count == width || last || (offset & 0xFFFF) == 0x0000)
                        {
                            crc  = (byte)((byte)0 - (crc + count));
                            line = $":{count:X2}" + line + crc.ToString("X2");

                            writer.WriteLine(line);

                            count = 0;
                        }
                    }
                }

                if (end)
                {
                    writer.Write(":00000001FF");
                }
            }
        }

        /// <summary>
        /// Appends an array of 16-bit words to a file in IntelHex format, excluding empty values. 
        /// </summary>
        /// <param name="fname">File name. The file is appended to, not overwritten.</param>
        /// <param name="dump">An array of words to save. Saved entirely to a hex file.</param>
        /// <param name="offset">The initial offset of the words to be saved in the hex file. Specified as a "word address" - NOT a byte address.</param>
        /// <param name="end">Specifies that an end-of-hex file marker should be added after the data being saved.</param>
        /// <param name="empty">Specifies the value of a word that will be considered empty. Such words will be skipped when saving data to a hex file.</param>
        /// <param name="width">Specifies the length of one hex data record. Specified in words. The default is 8 words, i.e., 16 bytes per line.</param>
        /// <exception cref="Exception">The hex string width is set to more than 127 words.</exception>
        public static void SaveWordsReduced(string fname, ushort[] dump, long offset, bool end = true, ushort empty = 0xFFFF, int width = 8)
        {
            if (width > 127 || width < 0)
            {
                throw new Exception($"incorrect line width: {width}.");
            }

            offset <<= 1;
            width   *= 2;

            using (StreamWriter writer = File.AppendText(fname))
            {
                long index = 0;
                int  count = 0;

                byte   crc  = 0;
                string line = "";

                ushort hadr = 0; //(ushort)((offset >> 16) & 0xFFFF);

                while (index < dump.LongLength)
                {
                    ushort data = dump[index];

                    if (data != empty)
                    {
                        if (count == 0)
                        {
                            ushort adrl = (ushort)(offset & 0xFFFF);
                            ushort adrh = (ushort)((offset >> 16) & 0xFFFF);

                            if (adrh != hadr && adrh != 0)
                            {
                                writer.WriteLine(SaveAddress(adrh));
                                hadr = adrh;
                            }

                            crc  = (byte)((adrl >> 8) + (adrl & 0xFF) + (byte)IntelHexRecordType.Data);
                            line = $"{adrl:X4}{(byte)IntelHexRecordType.Data:X2}";
                        }

                        byte l = (byte)(data & 0xFF);
                        byte h = (byte)(data >> 8);

                        crc  += (byte)(l + h);
                        line += $"{l:X2}{h:X2}";

                        count += 2;
                    }

                    index++;
                    offset += 2;

                    if (count != 0)
                    {
                        bool last = index < dump.LongLength ? dump[index] == empty : true;

                        if (count == width || last || (offset & 0xFFFF) == 0x0000)
                        {
                            crc = (byte)((byte)0 - (crc + count));
                            line = $":{count:X2}" + line + crc.ToString("X2");

                            writer.WriteLine(line);

                            count = 0;
                        }
                    }
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