using System;
using System.IO;
using System.Text;

namespace Pixsper.DmxDotNet.Serialization
{
	internal class SAcnBinaryReader : EndianBinaryReader
	{
		public SAcnBinaryReader(Stream stream)
			: base(EndianBitConverter.Big, stream, Encoding.UTF8)
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
