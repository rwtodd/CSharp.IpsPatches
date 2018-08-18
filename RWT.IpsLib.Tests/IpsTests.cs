using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;


namespace RWT.IpsLib.Tests
{

    [TestClass]
	public class PatchTests
	{

		private static Byte[] KnownIps = new Byte[]
		{
			0x50, 0x41, 0x54, 0x43, 0x48,
			0x00, 0x00, 0x15, 0x00, 0x00, 0x00, 0x0E, 0x07,
			0x00, 0x00, 0xFE, 0x00, 0x04, 0x01, 0x02, 0x03, 0x04,
			0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0xFE,
			0x45, 0x4F, 0x46
		};

		[TestMethod]
		public async Task TestKnownIpsFile()
		{
			var ps = IpsFormat.ReadPatches(new MemoryStream(KnownIps));
			var ostr = new MemoryStream();
			await IpsFormat.WritePatchesAsync(ostr, ps);
			Assert.IsTrue(ostr.TryGetBuffer(out var content));
			Assert.IsTrue(content.SequenceEqual(KnownIps), "Re-Written IPS doesn't match!");

			// now try to create the known ips from scratch...
			var patches = new Patch[]
			{
				new RLEPatch(0x15, 0x0E, 0x07),
				new BytePatch(0xFE, new Byte[] { 1,2,3,4} ),
				new RLEPatch(256, 256, 254)
			};
			ostr.SetLength(0);
			await IpsFormat.WritePatchesAsync(ostr, patches);
			Assert.IsTrue(ostr.TryGetBuffer(out content));
			Assert.IsTrue(content.SequenceEqual(KnownIps), "Re-created IPS doesn't match!");
		}

		[TestMethod]
		public async Task TestApplyIpsFile()
		{
			var tgt = Enumerable.Repeat((byte)0x44, 2048).ToArray();
			var os = new MemoryStream(tgt);
			await IpsFormat.ForEachPatchAsync(new MemoryStream(KnownIps),
				async (p) =>
				{
					await p.ApplyAsync(os);
				}
			);
			await os.FlushAsync();

			// now check the results...
			Assert.IsTrue(check44(0, 21));
			for (int idx = 21; idx < 35; idx++)
			{
				Assert.AreEqual(0x07, tgt[idx]);
			}
			Assert.IsTrue(check44(35, 254));
			Assert.AreEqual(0x01, tgt[254]);
			Assert.AreEqual(0x02, tgt[255]);
			for (int idx = 256; idx < 512; idx++)
			{
				Assert.AreEqual(254, tgt[idx]);
			}
			Assert.IsTrue(check44(512, 2048));

			bool check44(int low, int high)
			{
				for (int i = low; i < high; i++)
				{
					if (tgt[i] != 0x44) return false;
				}
				return true;
			}
		}
	}

} // end namespace

