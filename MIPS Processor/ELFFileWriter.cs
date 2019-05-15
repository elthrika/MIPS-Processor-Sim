using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIPS_Processor
{
    class ELFWriter
    {

        //int e_phoff;
        int e_shoff;

        public ELFWriter()
        {

        }

        private void WriteELFHeader(FileStream fs)
        {
            fs.Seek(0, SeekOrigin.Begin); //Goto start of file

            fs.WriteByte(0x7F); fs.WriteByte(0x45); fs.WriteByte(0x4C); fs.WriteByte(0x46); //magic number .ELF
            fs.WriteByte(0x01); //32-bit
            fs.WriteByte(0x01); //little-endian
            fs.WriteByte(0x01); //original ELF version
            fs.WriteByte(0x00); //Target-OS - blank
            fs.Write(new byte[8], 0, 8); //8bytes padding
            fs.WriteByte(0x02); //executable
            fs.WriteByte(0x08); fs.WriteByte(0x00); //MIPS-Instruction set
            fs.WriteByte(0x01); fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); //original ELF version #2
            fs.WriteByte(0x40); fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); //memory address of entry point
            fs.WriteByte(0x34); fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); //offset to start of program header table
            fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); //offset to start of section header table -- write later
            fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); fs.WriteByte(0x00); //e_flags -- ignore
            fs.WriteByte(0x34); fs.WriteByte(0x00); //size of this header
            fs.WriteByte(0x04); fs.WriteByte(0x00); //size of a program header table entry 
            fs.WriteByte(0x08); fs.WriteByte(0x00); //number of entries in the program header table
            fs.WriteByte(0x00); fs.WriteByte(0x00); //size of a section header table entry
            fs.WriteByte(0x00); fs.WriteByte(0x00); //number of entries in the section header table
            fs.WriteByte(0x00); fs.WriteByte(0x00); //index of the section header table entry that contains the section names
        }

    }
}
