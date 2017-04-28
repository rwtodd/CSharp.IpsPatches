# IPS Patches

IPS is a file format for binary patches, commonly used to patch
game ROMs.  The format is described [at zophar.net](http://www.zerosoft.zophar.net/ips.php).

Often when you run across a patch file, people want you to install some freeware patcher, 
which is hard to trust.  Better to write my own patch program than risk getting some kind
of crypto virus :)

The main program here applies a patch file to a target, but I also made a library with
IPS-writing capability.  This way, I can easily write programs that create patches
as well.

