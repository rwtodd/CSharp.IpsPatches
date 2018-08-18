using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace RWT.IpsLib {

public abstract class Patch {
	/// <summary>Apply the patch to a random-access file</summary>
	public abstract Task ApplyAsync(Stream os);

	/// <summary>Write out the patch in IPS format</summary>
	internal abstract Task WriteIpsFmtAsync(Stream os);
}


public class BytePatch : Patch {

	private readonly Int32 Offset;
	private readonly Byte[] Bytes;

	public BytePatch(Int32 offs, Byte[] bytes) {
		Offset = offs;
		Bytes = bytes;
	}

	public override String ToString() {
		return $"{Offset:X6}: Patch of length {Bytes.Length}";
	}

	public override Task ApplyAsync(Stream os) {
		os.Seek(Offset,SeekOrigin.Begin);
		return os.WriteAsync(Bytes,0,Bytes.Length);
	}
	internal override async Task WriteIpsFmtAsync(Stream os) {
		var obytes = BitConverter.GetBytes(Offset);
		var lbytes = BitConverter.GetBytes((UInt16)Bytes.Length);
		if(BitConverter.IsLittleEndian) {
			Array.Reverse(obytes);
			Array.Reverse(lbytes);
		}
		var buff = new Byte[5];
		Array.Copy(obytes,1, buff, 0, 3);
		Array.Copy(lbytes,0,buff,3,2);
		await os.WriteAsync(buff,0,5);
		await os.WriteAsync(Bytes,0,Bytes.Length);
	}
}

public class RLEPatch : Patch {

	private readonly Int32 Offset;
	private readonly UInt16 Len;
	private readonly Byte Value;

	public RLEPatch(Int32 offs, UInt16 len, Byte val) {
		Offset = offs;
		Len = len;
		Value = val;
	}

	public override String ToString() {
		return $"{Offset:X6}: Patch of length {Len}, Value {Value:X2}";
	}
	public override Task ApplyAsync(Stream os) {
		os.Seek(Offset,SeekOrigin.Begin);
		var buffer = Enumerable.Repeat(Value,Len).ToArray();
		return os.WriteAsync(buffer,0,Len);
	}

	internal override Task WriteIpsFmtAsync(Stream os) {
		var obytes = BitConverter.GetBytes(Offset);
		var lbytes = BitConverter.GetBytes(Len);
		if(BitConverter.IsLittleEndian) {
			Array.Reverse(obytes);
			Array.Reverse(lbytes);
		}

		// now format a buffer with the contents
		var buff = new Byte[8];
		Array.Copy(obytes, 1, buff, 0, 3);
		Array.Copy(lbytes,0,buff, 5, 2);
		buff[7] = Value;
		return os.WriteAsync(buff,0,8);
	}
}

} // end namespace
