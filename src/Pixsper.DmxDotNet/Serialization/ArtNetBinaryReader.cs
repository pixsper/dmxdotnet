using System;
using System.IO;
using System.Text;

namespace Pixsper.DmxDotNet.Serialization
{
	internal class ArtNetBinaryReader : EndianBinaryReader
	{
		public ArtNetBinaryReader(Stream stream)
			: base(EndianBitConverter.Little, stream, Encoding.UTF8)
		{
			
		}

		public override string ReadString()
		{
			var result = new StringBuilder(32);

			for (int i = 0; i < BaseStream.Length; ++i)
			{
				char c = Convert.ToChar(ReadByte());

				if (c == 0)
					break;

				result.Append(c);
			}

			return result.ToString();
		}
	}
}
