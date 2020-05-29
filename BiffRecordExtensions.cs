﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using b2xtranslator.Spreadsheet.XlsFileFormat;
using b2xtranslator.Spreadsheet.XlsFileFormat.Records;
using b2xtranslator.StructuredStorage.Reader;

namespace Macrome
{
    public static class BiffRecordExtensions
    {
        public static T AsRecordType<T>(this BiffRecord record) where T : BiffRecord
        {
            byte[] recordBytes = record.GetBytes();
            using (MemoryStream ms = new MemoryStream(recordBytes))
            {
                VirtualStreamReader vsr = new VirtualStreamReader(ms);
                RecordType id = (RecordType)vsr.ReadUInt16();
                ushort len = vsr.ReadUInt16();
                var typeConstructor = typeof(T).GetConstructor(new Type[]
                    {typeof(IStreamReader), typeof(RecordType), typeof(ushort)});

                if (typeConstructor == null)
                {
                    throw new ArgumentException(string.Format("Could not find appropriate constructor for type {0}", typeof(T).FullName));
                }

                return (T)typeConstructor.Invoke(new object[] {vsr, id, len});
            }
        }

        private static string HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            return result.ToString();
        }

        public static string ToHexDumpString(this BiffRecord record, int maxLength = 0x10)
        {
            string biffString = record.ToString();
            //Skip the 4 byte header
            byte[] bytes = record.GetBytes().Skip(4).ToArray();
            string hexDumpString = HexDump(bytes);

            if (record.Id == RecordType.Formula)
            {
                biffString = record.AsRecordType<Formula>().ToString();
            }
            else if (record.Id == RecordType.Dimensions)
            {
                biffString = record.AsRecordType<Dimensions>().ToString();
            }
            else if (record.Id == RecordType.Lbl)
            {
                biffString = record.AsRecordType<Lbl>().ToString();
            }


            if ((bytes.Length <= maxLength && bytes.Length > 0) ||
                record.Id == RecordType.Obj)
            {
                biffString += "\n" + hexDumpString;
            }

            return biffString;
        }


    }
}
