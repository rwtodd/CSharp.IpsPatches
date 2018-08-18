using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RWT.IpsLib {


public static class IpsFormat {
	internal static async Task ReadFullyAsync(this Stream stream, byte[] buffer)
	{
		int offset = 0;
		int readBytes;
		do
		{
			readBytes = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
			offset += readBytes;
		} while (readBytes > 0 && offset < buffer.Length);

		if (offset < buffer.Length)
		{
			throw new EndOfStreamException();
		}
	}

	/// <summary> StartBytes == "PATCH"</summary>
	private static Byte[] StartBytes = new Byte[] { 80, 65, 84, 67, 72  };

	/// <summary> EOFBytes == "EOF"</summary>
	private static Byte[] EOFBytes = new Byte[] { 69, 79, 70 };

	/// <summary> returns an IEnumerable of the patches in a stream.
	/// This is a synchronous method, even though it waits on Async methods
	/// underneath.</summary>
	public static IEnumerable<Patch> ReadPatches(Stream ins) {
		// first check that the stream starts with "PATCH"...
		var start = new Byte[5];	
		ins.ReadFullyAsync(start).Wait();
		if(!StartBytes.SequenceEqual(start)) {
			throw new ArgumentException("Not an IPS file");
		}

		while(true) {
			var p = ReadPatchAsync(ins).Result;
			if(p == null) break;
			yield return p;
		} 
	}

	/// <summary>Performs an action on each Patch found in the given Stream.</summary>
	public static async Task ForEachPatchAsync(Stream ins, Action<Patch> ap) {
		var start = new Byte[5];	
		await ins.ReadFullyAsync(start);
		if(!StartBytes.SequenceEqual(start)) {
			throw new ArgumentException("Not an IPS file");
		}

		while(true) {
			var p = await ReadPatchAsync(ins);
			if(p == null) break;
			ap(p);
		}
	}

	/// <summary>Performs an async function on each Patch found in the given Stream.</summary>
	public static async Task ForEachPatchAsync(Stream ins, Func<Patch, Task> af) {
		var start = new Byte[5];	
		await ins.ReadFullyAsync(start);
		if(!StartBytes.SequenceEqual(start)) {
			throw new ArgumentException("Not an IPS file");
		}

		Task providedFunc = null;
		while(true) {
			var p = await ReadPatchAsync(ins);

			// give provided func as much time as possible before awaiting it.
			if(providedFunc != null) await providedFunc;
			if(p == null) break;

			providedFunc = af(p);
		}

	}

	private static async Task<Patch> ReadPatchAsync(Stream ins) {
		var offlen = new Byte[5];		
		try {
			await ins.ReadFullyAsync(offlen);
		} catch(EndOfStreamException) when( EOFBytes.SequenceEqual(new ArraySegment<Byte>(offlen,0,3)) ) {
			// normal EOF...
			return null;
		}

		var offsetBytes = new Byte[4];
		var lenBytes = new Byte[2];
		Array.Copy(offlen, 0, offsetBytes, 1, 3);
		Array.Copy(offlen, 3, lenBytes, 0, 2);
	
		if(BitConverter.IsLittleEndian) {
			Array.Reverse(offsetBytes);
			Array.Reverse(lenBytes);
		}

		Int32 offset = BitConverter.ToInt32(offsetBytes,0);
		UInt16 len = BitConverter.ToUInt16(lenBytes,0);

		// if the length is 0, we have an RLE patch...
		if(len == 0) {
			await ins.ReadFullyAsync(lenBytes);
			if(BitConverter.IsLittleEndian) { Array.Reverse(lenBytes); }

			len = BitConverter.ToUInt16(lenBytes,0);

			var valByte = new Byte[1];
			await ins.ReadFullyAsync(valByte);
			return new RLEPatch(offset, len, valByte[0]);
		} else {
			var valBytes = new Byte[len];
			await ins.ReadFullyAsync(valBytes);
			return new BytePatch(offset, valBytes);
		}
		
	}

	/// <summary>Writes an IPS-format patch sequence to the given Stream.</summary> 
	public static async Task WritePatchesAsync(Stream os, IEnumerable<Patch> ps) {
		await os.WriteAsync(StartBytes,0,5);
		foreach(var p in ps) {
			await p.WriteIpsFmtAsync(os);
		}
		await os.WriteAsync(EOFBytes,0,3);
	}
}

} // end namespace

