using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RWT.IpsPatch;

namespace RWT.IpsApply
{

	class Cmd
	{

		public static void Main(String[] args)
		{

			if (args.Length != 3)
			{
				Console.Error.WriteLine("Usage: IpsApply <ipsfile> <srcfile> <resultfile>");
				Environment.Exit(1);
			}
			var ipsFile = args[0];
			var srcFile = args[1];
			var resultFile = args[2];
			Console.WriteLine($"Patching <{srcFile}> with <{ipsFile}> to produce <{resultFile}>");

			try
			{
				int count = 1;
				File.Copy(srcFile, resultFile);
				using (FileStream ips = File.OpenRead(ipsFile),
					res = File.OpenWrite(resultFile))
				{
					IpsFormat.ForEachPatchAsync(ips,
						(p) =>
						{
							Console.WriteLine($"Patch {count}: {p}");
							++count;
							return p.ApplyAsync(res);
						}).Wait();
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}
		}

	}

} // end namespace
