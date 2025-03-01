﻿#nullable enable
using System;
using System.Linq;
using System.IO;
using U8Xml;
using U8Xml.Unsafes;
using System.Text;
using System.Collections.Generic;

namespace UnitTest
{
    internal static class TestCases
    {
        public static IEnumerable<Func<XmlObject>> GetTestCases(ReadOnlySpan<byte> xml)
        {
            var xmlBytes = xml.ToArray();
            return new Func<XmlObject>[]
            {
                // from ReadOnlySpan<byte>
                () => XmlParser.Parse(xmlBytes.ToArray()),
                // from string
                () => XmlParser.Parse(Encoding.UTF8.GetString(xmlBytes.ToArray())),
                // from ReadOnlySpan<char>
                () => XmlParser.Parse(Encoding.UTF8.GetString(xmlBytes.ToArray()).AsSpan()),
                // from Stream
                () => XmlParser.Parse(new MemoryStream(xmlBytes.ToArray())),
                // from Stream, fileSizeHint
                () =>
                {
                    var ms = new MemoryStream(xmlBytes.ToArray());
                    return XmlParser.Parse(ms, (int)ms.Length);
                },
                // from Stream, Encoding
                ReEncoding(xmlBytes.ToArray(), Encoding.UTF8),
                ReEncoding(xmlBytes.ToArray(), Encoding.Unicode),
                ReEncoding(xmlBytes.ToArray(), Encoding.BigEndianUnicode),
                ReEncoding(xmlBytes.ToArray(), Encoding.UTF32),
            };
        }

        public static IEnumerable<Func<XmlObjectUnsafe>> GetUnsafeTestCases(ReadOnlySpan<byte> xml)
        {
            var xmlBytes = xml.ToArray();
            return new Func<XmlObjectUnsafe>[]
            {
                // from ReadOnlySpan<byte>
                () => XmlParserUnsafe.ParseUnsafe(xmlBytes.ToArray()),
                // from Stream
                () => XmlParserUnsafe.ParseUnsafe(new MemoryStream(xmlBytes.ToArray())),
                // from Stream, fileSizeHint
                () =>
                {
                    var ms = new MemoryStream(xmlBytes.ToArray());
                    return XmlParserUnsafe.ParseUnsafe(ms, (int)ms.Length);
                },
            };
        }

        private static Func<XmlObject> ReEncoding(ReadOnlySpan<byte> xml, Encoding encoding)
        {
            var bytes = Encoding.Convert(Encoding.UTF8, encoding, xml.ToArray());
            var ms = new MemoryStream(bytes);
            return () => XmlParser.Parse(ms, encoding);
        }
    }
}
