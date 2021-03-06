using System;
using System.Linq;
using System.Text;
using TorrentUWP.BEncoding;

namespace TorrentUWP.BEncoding
{
    /// <summary>
    /// Class representing a BEncoded string
    /// </summary>
    public class BEncodedString : BEncodedValue, IComparable<BEncodedString>
    {
        public string Hex => BitConverter.ToString(TextBytes);

        /// <summary>
        /// The value of the BEncodedString
        /// </summary>
        public string Text
        {
            get { return Encoding.UTF8.GetString(TextBytes); }
            set { TextBytes = Encoding.UTF8.GetBytes(value); }
        }

        /// <summary>
        /// The underlying byte[] associated with this BEncodedString
        /// </summary>
        public byte[] TextBytes { get; private set; }

        public BEncodedString()
            : this(new byte[0])
        {
        }

        public BEncodedString(char[] value)
            : this(Encoding.UTF8.GetBytes(value))
        {
        }

        public BEncodedString(string value)
            : this(Encoding.UTF8.GetBytes(value))
        {
        }

        public BEncodedString(byte[] value)
        {
            TextBytes = value;
        }

        public static implicit operator BEncodedString(string value)
        {
            return new BEncodedString(value);
        }

        public static implicit operator BEncodedString(char[] value)
        {
            return new BEncodedString(value);
        }

        public static implicit operator BEncodedString(byte[] value)
        {
            return new BEncodedString(value);
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as BEncodedString);
        }

        public int CompareTo(BEncodedString other)
        {
            if (other == null)
                return 1;

            int difference;
            var length = TextBytes.Length > other.TextBytes.Length ? other.TextBytes.Length : TextBytes.Length;

            for (var i = 0; i < length; i++)
                if ((difference = TextBytes[i].CompareTo(other.TextBytes[i])) != 0)
                    return difference;

            if (TextBytes.Length == other.TextBytes.Length)
                return 0;

            return TextBytes.Length > other.TextBytes.Length ? 1 : -1;
        }

        /// <summary>
        /// Encodes the BEncodedString to a byte[] using the supplied Encoding
        /// </summary>
        /// <param name="buffer">The buffer to encode the string to</param>
        /// <param name="offset">The offset at which to save the data to</param>
        /// <returns>The number of bytes encoded</returns>
        public override int Encode(byte[] buffer, int offset)
        {
            var written = offset;
            written += AsciiHelper.WriteAscii(buffer, written, TextBytes.Length.ToString());
            written += AsciiHelper.WriteAscii(buffer, written, ":");
            written += AsciiHelper.Write(buffer, written, TextBytes);
            return written - offset;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            BEncodedString other;
            var s = obj as string;
            if (s != null)
                other = new BEncodedString(s);
            else if (obj is BEncodedString)
                other = (BEncodedString)obj;
            else
                return false;

            return Toolbox.ByteMatch(TextBytes, other.TextBytes);
        }

        public override int GetHashCode()
        {
            return TextBytes.Aggregate(0, (current, t) => current + t);
        }

        public override int LengthInBytes()
        {
            // The length is equal to the length-prefix + ':' + length of data
            var prefix = 1; // Account for ':'

            // Count the number of characters needed for the length prefix
            for (var i = TextBytes.Length; i != 0; i = i / 10)
                prefix += 1;

            if (TextBytes.Length == 0)
                prefix++;

            return prefix + TextBytes.Length;
        }


        public override string ToString()
        {
            return Encoding.UTF8.GetString(TextBytes);
        }

        /// <summary>
        /// Decodes a BEncodedString from the supplied StreamReader
        /// </summary>
        /// <param name="reader">The StreamReader containing the BEncodedString</param>
        internal override void DecodeInternal(RawReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            int letterCount;
            var length = string.Empty;

            while ((reader.PeekByte() != -1) && (reader.PeekByte() != ':')) // read in how many characters
                length += (char)reader.ReadByte(); // the string is

            if (reader.ReadByte() != ':') // remove the ':'
                throw new BEncodingException("Invalid data found. Aborting");

            if (!int.TryParse(length, out letterCount))
                throw new BEncodingException(
                    string.Format("Invalid BEncodedString. Length was '{0}' instead of a number", length));

            TextBytes = new byte[letterCount];
            if (reader.Read(TextBytes, 0, letterCount) != letterCount)
                throw new BEncodingException("Couldn't decode string");
        }
    }
}