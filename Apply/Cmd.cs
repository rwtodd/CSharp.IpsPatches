using System;
using System.IO;
using RWT.IpsPatch;
using RWT.ArgParse;

namespace RWT.IpsApply
{

    class Cmd
	{

		public static void Main(String[] args)
		{
            String ipsFile = null, srcFile = null, resultFile = null;

            var ap = new ArgParser(
                new StrArg("-ips","<file> the IPS change file (REQUIRED)") {
                    Command = (fn) => ipsFile = fn,
                    Required = true
                },
                new StrArg("-src","<file> the original source (REQUIRED)")
                {
                    Command = (fn) => srcFile = fn,
                    Required = true
                },
                new StrArg("-dest","<file> the destination output file (REQUIRED)")
                {
                    Command = (fn) => resultFile = fn,
                    Required = true
                },
                new HelpArg("-help")
                {
                    Command = (opts) =>
                    {
                        Console.Error.WriteLine("Usage: IpsApply <options>");
                        Console.Error.WriteLine();
                        opts(Console.Error);
                        Environment.Exit(1);
                    }
                });

            try { 
                var extras = ap.Parse(args);

                if (extras.Count != 0)
                {
                    throw new ArgumentException("Unknown arguments given on the command line!");
                }
           

    			Console.WriteLine($"Patching <{srcFile}> with <{ipsFile}> to produce <{resultFile}>");

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
            catch (ArgParseException ape)
            {
                Console.Error.WriteLine(ape.Message);
                Console.Error.WriteLine();
                ap.ActivateSwitch("-help");
            }
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}
		}

	}

} // end namespace
