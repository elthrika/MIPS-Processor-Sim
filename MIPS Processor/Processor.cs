using System;
using System.IO;
using System.Threading;
using Microsoft.Win32.SafeHandles;


namespace MIPS_Processor
{
    class Processor
    {
        int[] registers = new int[32];
        int hi, lo;
        uint pc;

        byte[] memory;

        Helper.ProcessorState savedState;

        bool running = true;

        int freq; //in KHz, -1 = unlimited

        public Processor(string binfile, uint memsize = 8192, uint pc = 0x0040, int freq = -1)
        {
            memory = new byte[memsize];

            this.pc = pc; // init Programcounter
            registers[29] = registers[28] = (int)memsize; //init Stackpointer and Basepointer

            ExecutableFile ex = ExecutableFile.Read(binfile);

            Array.Copy(ex.TextBytes, 0, memory, ex.ProgramStart, ex.TextBytes.Length);
            Array.Copy(ex.DataBytes, 0, memory, ex.DataStart, ex.DataBytes.Length);

            if (this.pc != ex.ProgramStart)
                throw new Exception("pc != Programstart");

            this.freq = freq;
        }

        public void Start()
        {
            if (freq < 0)
            {
                while (running)
                {
                    uint instr = BitConverter.ToUInt32(memory, (int)pc);
                    try
                    {
                        ExecuteInstruction(instr);
                    }
                    catch (OverflowException)
                    {
                        running = false;
                        Console.WriteLine("Error at 0x{0:X}, instruction overflowed", pc);
                    }
                }
            }
            else
            {
                while (running)
                {
                    var start = DateTime.Now;
                    try
                    {
                        ExecuteInstruction(BitConverter.ToUInt32(memory, (int)pc));
                    }
                    catch (OverflowException)
                    {
                        running = false;
                        Console.Error.WriteLine("Error at {{0}}, instruction overflowed", pc);
                    }
                    var end = (DateTime.Now - start).Ticks;
                    
                    //@TODO: sleep timer

                }
            }
        }

        public void SetProgramCounter(uint nPC)
        {
            pc = nPC;
        }

        void ExecuteInstruction(uint instr)
        {
            Instruction.Format format = Instruction.GetFormat(instr);
            switch (format)
            {
                case Instruction.Format.R:
                    ExecuteRInstruction(instr);
                    break;
                case Instruction.Format.I:
                    ExecuteIInstruction(instr);
                    break;
                case Instruction.Format.J:
                    ExecuteJInstruction(instr);
                    break;
            }
        }

        void ExecuteRInstruction(uint instr)
        {
            byte funct = Instruction.GetFunct(instr);

            byte sreg = Instruction.GetSRegister(instr);
            byte treg = Instruction.GetTRegister(instr);
            byte dreg = Instruction.GetDRegister(instr);

            pc += 4;

            switch (funct)
            {
                case 0x20: //add
                    checked
                    {
                        registers[dreg] = registers[sreg] + registers[treg];
                    }
                    break;
                case 0x21: //addu
                    registers[dreg] = registers[sreg] + registers[treg];
                    break;
                case 0x24: //and
                    registers[dreg] = registers[sreg] & registers[treg];
                    break;
                case 0x1A: //div
                    lo = registers[sreg] / registers[treg];
                    hi = registers[sreg] % registers[treg];
                    break;
                case 0x1B: //divu
                    lo = registers[sreg] / registers[treg];
                    hi = registers[sreg] % registers[treg];
                    break;
                case 0x08: //jr
                    pc = (uint)registers[sreg];
                    break;
                case 0x10: //mfhi
                    registers[dreg] = hi;
                    break;
                case 0x12: //mflo
                    registers[dreg] = lo;
                    break;
                case 0x18: //mult
                    lo = ((registers[sreg] * registers[treg]) << 32) >> 32;
                    hi = (registers[sreg] * registers[treg]) >> 32;
                    break;
                case 0x19: //multu
                    lo = ((registers[sreg] * registers[treg]) << 32) >> 32;
                    hi = (registers[sreg] * registers[treg]) >> 32;
                    break;
                case 0x27: //nor
                    registers[dreg] = ~(registers[sreg] | registers[treg]);
                    break;
                case 0x25: //or
                    registers[dreg] = registers[sreg] | registers[treg];
                    break;
                case 0x00: //sll
                    registers[dreg] = registers[treg] << Instruction.GetShiftAmount(instr);
                    break;
                case 0x04: //sllv
                    registers[dreg] = registers[treg] << registers[sreg];
                    break;
                case 0x2A: //slt
                    registers[dreg] = registers[sreg] < registers[treg] ? 1 : 0;
                    break;
                case 0x2B: //sltu
                    registers[dreg] = registers[sreg] < registers[treg] ? 1 : 0;
                    break;
                case 0x03: //sra
                    registers[dreg] = registers[treg] >> Instruction.GetShiftAmount(instr);
                    break;
                case 0x02: //srl
                    registers[dreg] = (int)((uint)registers[treg] >> Instruction.GetShiftAmount(instr));
                    break;
                case 0x06: //srlv
                    registers[dreg] = (int)((uint)registers[treg] >> registers[sreg]);
                    break;
                case 0x22: //sub
                    checked { registers[dreg] = registers[sreg] - registers[treg]; }
                    break;
                case 0x23: //subu
                    registers[dreg] = registers[sreg] - registers[treg];
                    break;
                case 0x0C: //syscall
                    ExecuteSyscall();
                    break;
                case 0x26: //xor
                    registers[dreg] = registers[sreg] ^ registers[treg];
                    break;
                case 0x0D: //break - debug
                    StartDebug();
                    break;
                default:
                    throw new Exception("No R Instruction with Funct: " + funct);
            }
        }

        void ExecuteIInstruction(uint instr)
        {
            byte opcode = Instruction.GetOpCode(instr);
            byte treg = Instruction.GetTRegister(instr);
            byte sreg = Instruction.GetSRegister(instr);
            short imm = Instruction.GetIImmediate(instr);

            pc += 4;

            switch (opcode)
            {
                case 0x08: //addi
                    checked { registers[treg] = registers[sreg] + imm; }
                    break;
                case 0x09: //addiu
                    registers[treg] = registers[sreg] + imm;
                    break;
                case 0x0C: //andi
                    registers[treg] = registers[sreg] & imm;
                    break;
                case 0x04: //beq
                    if (registers[sreg] == registers[treg])
                    {
                        pc -= 4;
                        pc += (uint)(imm << 2);
                    }
                    break;
                case 0x01: 
                    switch (treg)
                    {
                        case 0x01: //bgez
                            if(registers[sreg] >= 0)
                            {
                                pc -= 4;
                                pc += (uint)(imm << 2);
                            }
                            break;
                        case 0x11: //bgezal
                            if (registers[sreg] >= 0)
                            {
                                registers[31] = (int)(pc + 4);
                                pc -= 4;
                                pc += (uint)(imm << 2);
                            }
                            break;
                        case 0x00: //bltz
                            if (registers[sreg] < 0)
                            {
                                pc -= 4;
                                pc += (uint)(imm << 2);
                            }
                            break;
                        case 0x10: //bltzal
                            if (registers[sreg] < 0)
                            {
                                registers[31] = (int)(pc + 4);
                                pc -= 4;
                                pc += (uint)(imm << 2);
                            }
                            break;
                    }
                    break;
                case 0x07: //bgtz
                    if(registers[sreg] > 0)
                    {
                        pc -= 4;
                        pc += (uint)(imm << 2);
                    }
                    break;
                case 0x06: //blez
                    if (registers[sreg] <= 0)
                    {
                        pc -= 4;
                        pc += (uint)(imm << 2);
                    }
                    break;
                case 0x05: //bne
                    if (registers[sreg] != registers[treg])
                    {
                        pc -= 4;
                        pc += (uint)(imm << 2);
                    }
                    break;
                case 0x20: //lb
                    registers[treg] = memory[registers[sreg] + imm];
                    break;
                case 0x24: //lbu
                    registers[treg] = memory[registers[sreg] + imm];
                    break;
                case 0x21: //lh
                    registers[treg] = BitConverter.ToInt16(memory, registers[sreg] + imm);
                    break;
                case 0x25: //lhu
                    registers[treg] = BitConverter.ToUInt16(memory, registers[sreg] + imm);
                    break;
                case 0x23: //lw
                    registers[treg] = BitConverter.ToInt32(memory, registers[sreg] + imm);
                    break;
                case 0x0F: //lui
                    registers[treg] = imm << 16;
                    break;
                case 0x0D: //ori
                    registers[treg] = registers[sreg] | (ushort)imm;
                    break;
                case 0x28: //sb
                    memory[registers[sreg] + imm] = (byte)registers[treg];
                    break;
                case 0x29: //sh
                    int t1 = registers[treg];
                    memory[registers[sreg] + imm] = (byte)t1;
                    memory[registers[sreg] + imm + 1] = (byte)(t1 >> 8);
                    break;
                case 0x2B: //sw
                    int t = registers[treg];
                    memory[registers[sreg] + imm] = (byte)t;
                    memory[registers[sreg] + imm + 1] = (byte)(t >> 8);
                    memory[registers[sreg] + imm + 2] = (byte)(t >> 16);
                    memory[registers[sreg] + imm + 3] = (byte)(t >> 24);
                    break;
                case 0x0A: //slti
                    registers[treg] = registers[sreg] < imm ? 1 : 0;
                    break;
                case 0x0B: //sltiu
                    registers[treg] = registers[sreg] < imm ? 1 : 0;
                    break;
                case 0x0E: //xori
                    registers[treg] = registers[sreg] ^ (ushort)imm;
                    break;
                default:
                    throw new Exception("No I Instruction with OpCode: " + opcode);
            }
        }

        void ExecuteJInstruction(uint instr)
        {
            byte opcode = Instruction.GetOpCode(instr);
            int imm = Instruction.GetJImmediate(instr);
            uint oldpc = pc;

            switch (opcode)
            {
                case 0x02: //j
                    pc = (pc & 0xF0000000) | (uint)(imm << 2);
                    //Console.WriteLine("j {0}->{1}", oldpc, pc);
                    break;
                case 0x03: //jal
                    registers[31] = (int)pc + 4;
                    pc = (pc & 0xF0000000) | (uint)(imm << 2);
                    //Console.WriteLine("jal {0}->{1}", oldpc, pc);
                    break;
            }
        }

        void ExecuteSyscall()
        {
            switch (registers[2])
            {
                case 1: //write int
                    Console.Write(registers[4]);
                    break;
                case 2: //write float
                    break;
                case 3: //write double
                    break;
                case 4: //write null-terminated string
                    string outp = ReadStringFromMemory(registers[4]);
                    Console.Write(outp);
                    break;
                case 5: //read int
                    registers[2] = Convert.ToInt32(Console.ReadLine());
                    break;
                case 6: //read float
                    break;
                case 7: //read double
                    break;
                case 8: //read string
                    string inp = Console.ReadLine();
                    byte[] str = System.Text.Encoding.ASCII.GetBytes(inp.ToCharArray(), 0, registers[5]);
                    Array.Copy(str, 0, memory, registers[4], registers[5]); 
                    break;
                case 9: //sbrk --- have to write memory allocator for this
                    break;
                case 10: //exit
                    running = false;
                    break;
                case 11: //print character
                    Console.Write((char)registers[4]);
                    break;
                case 12: //read character
                    registers[2] = Console.Read();
                    break;
                case 13: //open file
                    string fname = ReadStringFromMemory(registers[4]);
                    registers[2] = File.Open(fname, (FileMode)registers[5], (FileAccess)registers[6]).SafeFileHandle.DangerousGetHandle().ToInt32();
                    break;
                case 14: //read from file
                    byte[] rbuf = new byte[registers[6]];
                    int nread = (new FileStream(new SafeFileHandle(new IntPtr(registers[4]), true), FileAccess.Read)).Read(rbuf, 0, registers[6]);
                    Array.Copy(rbuf, 0, memory, registers[5], registers[6]);
                    registers[2] = nread;
                    break;
                case 15: //write to file
                    byte[] wbuf = new byte[registers[6]];
                    Array.Copy(memory, registers[5], wbuf, 0, registers[6]);
                    (new FileStream(new SafeFileHandle(new IntPtr(registers[4]), true), FileAccess.Write)).Write(wbuf, 0, registers[6]);
                    registers[2] = registers[6];
                    break;
                case 16: //close file
                    (new FileStream(new SafeFileHandle(new IntPtr(registers[4]) ,true), FileAccess.Write)).Close();
                    break;
                case 17: //exit2 --- with value
                    running = false;
                    break;
                case 18:
                    SaveMemoryAndRegisters();
                    break;
                case 19:
                    CompareMemoryAndRegisters();
                    break;
                case 30:
                    long secs = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                    registers[4] = (int)secs;
                    registers[5] = (int)(secs >> 32);
                    break;
                case 31:
                    Console.Beep(registers[4], registers[5]);
                    break;
                case 32:
                    Thread.Sleep(registers[4]);
                    break;
                case 33:
                    Console.Beep(registers[4], registers[5]);
                    break;
                case 34:
                    Console.Write(Convert.ToString(registers[4], 16).PadLeft(8, '0'));
                    break;
                case 35:
                    Console.Write(Convert.ToString(registers[4], 2).PadLeft(32, '0'));
                    break;
                case 36:
                    Console.Write((uint)registers[4]);
                    break;
            }
        }

        void StartDebug()
        {
            Console.WriteLine();
            string cmd;
            do
            {
                Console.WriteLine("Debugging mode - pc: {0} - command: ", Convert.ToString(pc, 16));
                cmd = Console.ReadLine();

                switch (cmd)
                {
                    case "c":
                        uint instr = BitConverter.ToUInt32(memory, (int)pc);
                        ExecuteInstruction(instr);
                        break;
                    case "reg":
                        for(int i = 0; i < registers.Length; i++)
                        {
                            Console.WriteLine(("$"+i).PadRight(4, ' ') + "= " + registers[i]);
                        }
                        break;
                    case "memdump":
                        Console.Write("Address: "); int address = Convert.ToInt32(Console.ReadLine(), 16);
                        Console.Write("N Bytes: "); int nbytes = Convert.ToInt32(Console.ReadLine());
                        for(int j = 0; j < nbytes; j++)
                        {
                            Console.Write(Convert.ToString(memory[address + j], 16).PadLeft(2, '0') + " ");
                        }
                        Console.WriteLine();
                        break;
                }

            } while (cmd != "q");
        }

        void SaveMemoryAndRegisters()
        {
            if(savedState != null && !savedState.compared)
            {
                Console.WriteLine("WARNING: Overwriting savedState without comparing");
            }
            savedState = new Helper.ProcessorState(registers, memory);

        }

        void CompareMemoryAndRegisters()
        {
            if(savedState == null)
            {
                Console.WriteLine("Call to Compare without saved state");
                return;
            }

            Console.WriteLine("Comparing savedState to current State...");

            int nosamereg = 0;
            int nosamemem = 0;

            for (int i = 0; i < registers.Length; i++)
            {
                if (registers[i] != savedState.registers[i])
                    nosamereg++;
            }
            for (int i = 0; i < memory.Length; i++)
            {
                if (memory[i] != savedState.memory[i])
                    nosamemem++;
            }

            savedState.compared = true;

            Console.WriteLine("Finished comparing {0} changed registers, {1} changed memorylocations", nosamereg, nosamemem);
        }

        string ReadStringFromMemory(int address)
        {
            string outp = "";
            for (; memory[address] != 0; address++)
            {
                outp += (char)memory[address];
            }
            return outp;
        }
    }
}
