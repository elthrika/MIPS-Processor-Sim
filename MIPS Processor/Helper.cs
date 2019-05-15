using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIPS_Processor
{
    public class Helper
    {
        public class ProcessorState
        {
            public byte[] memory;
            public int[] registers;
            public bool compared = false;

            public ProcessorState(int[] pregisters, byte[] pmemory)
            {
                registers = new int[pregisters.Length];
                memory = new byte[pmemory.Length];

                Array.Copy(pmemory, memory, memory.Length);
                Array.Copy(pregisters, registers, registers.Length);
            }
        }
    }
}
