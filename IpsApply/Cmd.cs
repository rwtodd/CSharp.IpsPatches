using System;
using System.IO;
using Args = RWT.ArgParse;
using RWT.IpsLib;

namespace RWT.IpsApply
{

    class Cmd
    {

        public static void Main(String[] args)
        {
            String ipsFile = null, srcFile = null, resultFile = null;

            var ap = new Args.ArgParser(
                new Args.ExistingFileArg("-ips", "<file> the IPS change file (REQUIRED)")
                {
                    Command = (fn) => ipsFile = fn,
                    Required = true
                },
                new Args.ExistingFileArg("-src", "<file> the original source (REQUIRED)")
                {
                    Command = (fn) => srcFile = fn,
                    Required = true
                },
                new Args.StrArg("-dest", "<file> the destination output file (REQUIRED)")
                {
                    Command = (fn) => resultFile = fn,
                    Required = true
                },
                new Args.HelpArg("-help")
                {
                    Command = (opts) =>
                    {
                        Console.Error.WriteLine("Usage: IpsApply <options>");
                        Console.Error.WriteLine();
                        opts(Console.Error);
                        Environment.Exit(1);
                    }
                })
            {
                ExtrasRange = (0, 0)
            };

            try
            {
                ap.Parse(args);
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
            catch (Args.ArgParseException ape)
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
