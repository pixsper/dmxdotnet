using System.IO;
using System.Linq;
using System.Text;

namespace Pixsper.DmxDotNet.Serialization
{
	internal class SAcnBinaryWriter : EndianBinaryWriter
	{
		public SAcnBinaryWriter(Stream stream)
			: base(EndianBitConverter.Big, stream, Encoding.UTF8)
		{
			
		}

		public override void Write(string value)
		{
			Write(Encoding.GetBytes(value + '\0'));
		}

		public void Write(string value, int fieldLength)
		{
			Write(Encoding.GetBytes(value + '\0'));
			if (value.Length + 1 < fieldLength)
				Write(Enumerable.Repeat((byte)0, fieldLength - (value.Length + 1)).ToArray());
		}
	}
}
