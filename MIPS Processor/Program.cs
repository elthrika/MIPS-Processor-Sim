using System;


namespace MIPS_Processor
{
    class Program
    {
        static void Main(string[] args)
        {

            string infile = args[0];
            string outfile = infile.Replace(".txt", ".bin");
            int datastart = args.Length > 2 ?  int.Parse(args[1]) : 0x0FFF;
            int programstart = args.Length > 3 ? int.Parse(args[2]) : 0x0040;

            Assembler asm = new Assembler(infile);
            asm.Start(programstart, datastart);
            asm.WriteToFile(outfile, programstart, datastart);

            Processor proc = new Processor(outfile);
            proc.Start();

            Console.Read();

        }

    }
}
