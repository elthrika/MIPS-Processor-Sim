using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MIPS_Processor
{
    class Assembler
    {

        Dictionary<string, byte> regnameToReg;

        Dictionary<string, int> dataToAddr;
        Dictionary<string, int> labelToAddr;

        string[] lines;

        List<uint> text;
        List<byte> data;

        List<Tuple<int, string>> instructionIndicesToLabel;
        List<Tuple<int, string>> instructionIndicesForData;

        public Assembler(string file) //TODO: Extend to string[] for multiple files
        {
            lines = File.ReadAllLines(file);

            text = new List<uint>();
            data = new List<byte>();
            instructionIndicesToLabel = new List<Tuple<int, string>>();
            instructionIndicesForData = new List<Tuple<int, string>>();

            labelToAddr = new Dictionary<string, int>();
            dataToAddr = new Dictionary<string, int>();

            regnameToReg = new Dictionary<string, byte>()
            {
                { "$zero", 0 },
                { "$at",  1 },
                { "$v0",  2 },
                { "$v1",  3 },
                { "$a0",  4 },
                { "$a1",  5 },
                { "$a2",  6 },
                { "$a3",  7 },
                { "$t0",  8 },
                { "$t1",  9 },
                { "$t2", 10 },
                { "$t3", 11 },
                { "$t4", 12 },
                { "$t5", 13 },
                { "$t6", 14 },
                { "$t7", 15 },
                { "$s0", 16 },
                { "$s1", 17 },
                { "$s2", 18 },
                { "$s3", 19 },
                { "$s4", 20 },
                { "$s5", 21 },
                { "$s6", 22 },
                { "$s7", 23 },
                { "$t8", 24 },
                { "$t9", 25 },
                { "$k0", 26 },
                { "$k1", 27 },
                { "$gp", 28 },
                { "$sp", 29 },
                { "$fp", 30 },
                { "$ra", 31 }
            };
        }

        public void Start(int programstart, int datastart)
        {
            ReadDataSegment();
            ReadTextSegment();

            MakeAdressesForLabels(programstart);
            MakeAdressesForData(datastart);
        }

        private void ReadTextSegment()
        {
            int i;
            for(i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() == ".text") break;
            }
            i++;
            for(; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Length <= 0 || line.StartsWith("#")) continue;

                if (line.StartsWith("."))
                {
                    if(line == ".data") break;
                    
                }
                else if (line.EndsWith(":"))
                {
                    labelToAddr[line.Replace(":", "")] = text.Count * 4;
                }
                else
                {
                    //Console.WriteLine(text.Count * 4 + ": " + line);
                    GenerateForInstruction(line);
                }
            }
        }

        private void ReadDataSegment()
        {
            int l = 0;
            for(; l < lines.Length; l++)
            {
                if (lines[l].Trim() == ".data") break;
            }
            l++;
            for (; l < lines.Length; l++)
            {   
                DataItem di = ReadDataLine(lines[l].Trim());
                int datastart = data.Count;

                switch (di.datatype)
                {
                    case ".ascii":
                        byte[] str = MakeString(di.data, false);
                        data.AddRange(str);
                        break;
                    case ".asciiz":
                        byte[] cstr = MakeString(di.data);
                        data.AddRange(cstr);
                        break;
                    case ".byte":
                        string[] sbytes = di.data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        byte[] mbytes = new byte[sbytes.Length];
                        for (int i = 0; i < mbytes.Length; i++)
                        {
                            string s = sbytes[i].Trim();
                            mbytes[i] = s.StartsWith("0x") ? Convert.ToByte(s, 16) : Convert.ToByte(s);
                        }
                        data.AddRange(mbytes);
                        break;
                    case ".halfword":
                        string[] shws = di.data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        short[] mhws = new short[shws.Length];
                        byte[] mhwbs = new byte[mhws.Length * 2];
                        for (int i = 0; i < mhws.Length; i++)
                        {
                            string s = shws[i].Trim();
                            mhws[i] = s.StartsWith("0x") ? Convert.ToInt16(s, 16) : Convert.ToInt16(s);
                        }
                        for (int i = 0; i < mhws.Length; i++)
                        {
                            byte[] bs = BitConverter.GetBytes(mhws[i]);
                            Array.Copy(bs, 0, mhwbs, i * 2, 2);
                        }
                        data.AddRange(mhwbs);
                        break;
                    case ".word":
                        string[] sws = di.data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        int[] mws = new int[sws.Length];
                        byte[] mwbs = new byte[mws.Length * 4];
                        for (int i = 0; i < mws.Length; i++)
                        {
                            string s = sws[i].Trim();
                            mws[i] = s.StartsWith("0x") ? Convert.ToInt32(s, 16) : Convert.ToInt32(s);
                        }
                        for (int i = 0; i < mws.Length; i++)
                        {
                            byte[] bs = BitConverter.GetBytes(mws[i]);
                            Array.Copy(bs, 0, mwbs, i * 4, 4);
                        }
                        data.AddRange(mwbs);
                        break;
                    case ".space":
                        int space = di.data.StartsWith("0x") ? Convert.ToInt32(di.data, 16) : Convert.ToInt32(di.data);
                        data.AddRange(new byte[space]);
                        break;
                }

                dataToAddr.Add(di.label, datastart);

            }
        }

        private void MakeAdressesForLabels(int programstart)
        {
            foreach (Tuple<int, string> offset in instructionIndicesToLabel)
            {
                uint instr = text[offset.Item1 / 4];
                Instruction.Format f = Instruction.GetFormat(instr);

                if (f == Instruction.Format.I)
                {
                    short iimediate = (short)((labelToAddr[offset.Item2] - offset.Item1)/4);
                    instr = Instruction.SetIImmediate(iimediate, instr);
                }
                else if (f == Instruction.Format.J)
                {
                    int val = (programstart + labelToAddr[offset.Item2])/4;
                    instr = Instruction.SetJImmediate(val, instr);
                }
                else
                {
                    throw new Exception("WTF HAPPENED HERE???");
                }

                text[offset.Item1 / 4] = instr;
            }
        }

        private void MakeAdressesForData(int datastart)
        {
            foreach (Tuple<int, string> offset in instructionIndicesForData)
            {
                uint instr = text[offset.Item1 / 4];
                Instruction.Format f = Instruction.GetFormat(instr);

                string name = offset.Item2;
                int iofbracket, upper = -1, lower = -1;
                if ((iofbracket = name.IndexOf('[')) > -1){
                    int icolon = name.IndexOf(":");
                    upper = Convert.ToInt32(name.Substring(iofbracket + 1, icolon - iofbracket - 1));
                    lower = Convert.ToInt32(name.Substring(icolon + 1, name.Length - icolon - 2));
                    name = name.Substring(0, iofbracket);
                }

                if (f == Instruction.Format.I)
                {
                    int iimediate = datastart + dataToAddr[name];
                    if(iofbracket > 0)
                    {
                        //@HACK: not acctually using the real values
                        if(upper == 31)
                        {
                            iimediate = iimediate >> 16;
                        }
                        else if (upper == 15)
                        {
                            iimediate = iimediate & 0x0000FFFF;
                        }
                    }
                    instr = Instruction.SetIImmediate((short)iimediate, instr);
                }
                else
                {
                    throw new Exception("WTF HAPPENED HERE???");
                }

                text[offset.Item1 / 4] = instr;
            }

        }

        public void WriteToFile(string filename, int programstart, int datastart)
        {
            if(text.Count == 0)
            {
                throw new Exception("Start before you write!");
            }

            ExecutableFile.Write(filename, text, programstart, data, datastart);
        }
        
        private void GenerateForInstruction(string line)
        {
            string[] ins = ParseInstructionLine(line);

            uint instr = 0;
            string[] C;

            int offset = text.Count * 4;

            string mnemonic = ins[0];

            switch (mnemonic)
            {
                #region SWITCH_A
                case "add":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x20, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "addi":
                    instr = Instruction.SetOpCode(0x08, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(ins[3]), instr);
                    break;
                case "addiu":
                    instr = Instruction.SetOpCode(0x09, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(ins[3]), instr);
                    break;
                case "addu":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x21, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "and":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x24, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "andi":
                    instr = Instruction.SetOpCode(0x0C, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(ins[3]), instr);
                    break;
                #endregion
                #region SWITCH_B
                case "beq":
                    instr = Instruction.SetOpCode(0x04, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    int constant;
                    if(int.TryParse(ins[2], out constant))
                    {
                        // compare to constant
                        GenerateForInstruction("li $at,"+constant); //store constant in $at
                        instr = Instruction.SetTRegister(regnameToReg["$at"], instr);
                    }
                    else
                    {
                        instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    }
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[3]));
                    break;
                case "bgez":
                    instr = Instruction.SetOpCode(0x01, instr);
                    instr = Instruction.SetTRegister(0x01, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[2]));
                    break;
                case "bgezal":
                    instr = Instruction.SetOpCode(0x01, instr);
                    instr = Instruction.SetTRegister(0x11, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[2]));
                    break;
                case "bgtz":
                    instr = Instruction.SetOpCode(0x07, instr);
                    instr = Instruction.SetTRegister(0x00, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[2]));
                    break;
                case "blez":
                    instr = Instruction.SetOpCode(0x06, instr);
                    instr = Instruction.SetTRegister(0x00, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[2]));
                    break;
                case "bltz":
                    instr = Instruction.SetOpCode(0x01, instr);
                    instr = Instruction.SetTRegister(0x00, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[2]));
                    break;
                case "bltzal":
                    instr = Instruction.SetOpCode(0x01, instr);
                    instr = Instruction.SetTRegister(0x10, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[2]));
                    break;
                case "bne":
                    instr = Instruction.SetOpCode(0x05, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[3]));
                    break;
                case "break":
                    instr = Instruction.SetOpCode(0x00, instr);
                    instr = Instruction.SetFunct(0x0D, instr);
                    break;
                #endregion
                #region SWITCH_D
                case "div":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x1A, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    break;
                case "divu":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x1B, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    break;
                #endregion
                #region SWITCH_J
                case "j":
                    instr = Instruction.SetOpCode(0x02, instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[1]));
                    break;
                case "jal":
                    instr = Instruction.SetOpCode(0x03, instr);
                    instructionIndicesToLabel.Add(new Tuple<int, string>(offset, ins[1]));
                    break;
                case "jr":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x08, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    break;
                #endregion
                #region SWITCH_L
                case "lb":
                    instr = Instruction.SetOpCode(0x20, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "lbu":
                    instr = Instruction.SetOpCode(0x24, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "lh":
                    instr = Instruction.SetOpCode(0x21, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "lhu":
                    instr = Instruction.SetOpCode(0x25, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "lw":
                    instr = Instruction.SetOpCode(0x23, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "lui":
                    instr = Instruction.SetOpCode(0x0F, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    short lui_imm;
                    if (short.TryParse(ins[2], out lui_imm)) //numerical immediate
                    {
                        instr = Instruction.SetIImmediate(lui_imm, instr);
                    }
                    else //label from la instruction
                    {
                        instructionIndicesForData.Add(new Tuple<int, string>(offset, ins[2]));
                    }
                    break;
                #endregion
                #region SWITCH_M
                case "mfhi":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x10, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    break;
                case "mflo":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x12, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    break;
                case "mult":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x18, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    break;
                case "multu":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x19, instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    break;
                #endregion
                #region SWITCH_N
                case "noop":
                    break;
                case "nor":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x27, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                #endregion
                #region SWITCH_O
                case "or":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x25, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "ori":
                    instr = Instruction.SetOpCode(0x0D, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    short ori_imm;
                    if (short.TryParse(ins[3], out ori_imm)) //numerical immediate
                    {
                        instr = Instruction.SetIImmediate(ori_imm, instr);
                    }
                    else //label from la instruction
                    {
                        instructionIndicesForData.Add(new Tuple<int, string>(offset, ins[3]));
                    }
                    break;
                #endregion
                #region SWITCH_S
                case "sll":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x0, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetShiftAmount(Convert.ToByte(ins[3]), instr);
                    break;
                case "sllv":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x04, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[3]], instr);
                    break;
                case "slt":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x2A, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "slti":
                    instr = Instruction.SetOpCode(0x0A, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(ins[3]), instr);
                    break;
                case "sltiu":
                    instr = Instruction.SetOpCode(0x0B, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(ins[3]), instr);
                    break;
                case "sltu":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x2B, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "sra":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x03, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetShiftAmount(Convert.ToByte(ins[3]), instr);
                    break;
                case "srl":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x25, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetShiftAmount(Convert.ToByte(ins[3]), instr);
                    break;
                case "srlv":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x25, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[3]], instr);
                    break;
                case "sub":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x22, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "subu":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x23, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "sb":
                    instr = Instruction.SetOpCode(0x28, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "sh":
                    instr = Instruction.SetOpCode(0x29, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "sw":
                    instr = Instruction.SetOpCode(0x2B, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    C = ins[2].Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(C[0]), instr);
                    instr = Instruction.SetSRegister(regnameToReg[C[1]], instr);
                    break;
                case "syscall":
                    instr = Instruction.SetFunct(0x0C, instr);
                    break;
                #endregion
                #region SWITCH_X
                case "xor":
                    instr = Instruction.SetOpCode(0x0, instr);
                    instr = Instruction.SetFunct(0x26, instr);
                    instr = Instruction.SetDRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[3]], instr);
                    break;
                case "xori":
                    instr = Instruction.SetOpCode(0x0E, instr);
                    instr = Instruction.SetTRegister(regnameToReg[ins[1]], instr);
                    instr = Instruction.SetSRegister(regnameToReg[ins[2]], instr);
                    instr = Instruction.SetIImmediate(Convert.ToInt16(ins[3]), instr);
                    break;
                #endregion
                #region SWITCH_ADDITIONAL
                case "move":
                    GenerateForInstruction(string.Format("add {0},{1},$zero", ins[1], ins[2]));
                    return;
                case "clear":
                    GenerateForInstruction(string.Format("add {0},$zero,$zero", ins[1]));
                    return;
                case "not":
                    GenerateForInstruction(string.Format("nor {0},{1},$zero", ins[1], ins[2]));
                    return;
                case "la":
                    GenerateForInstruction(string.Format("lui {0},{1}[31:16]", ins[1], ins[2]));
                    GenerateForInstruction(string.Format("ori {0},{0},{1}[15:0]", ins[1], ins[2]));
                    return;
                case "li":
                    GenerateForInstruction(string.Format("lui {0},{1}", ins[1], (Convert.ToInt32(ins[2])>>16)));
                    GenerateForInstruction(string.Format("ori {0},{0},{1}", ins[1], ins[2]));
                    return;
                case "b":
                    GenerateForInstruction(string.Format("beq $zero,$zero,{0}", ins[1]));
                    return;
                case "bal":
                    GenerateForInstruction(string.Format("bgezal $zero,{0}", ins[1]));
                    return;
                case "bgt":
                    GenerateForInstruction(string.Format("slt $at,{0},{1}", ins[1], ins[2]));
                    GenerateForInstruction(string.Format("bne $at,$zero,{0}", ins[3]));
                    return;
                case "blt":
                    GenerateForInstruction(string.Format("slt $at,{0},{1}", ins[1], ins[2]));
                    GenerateForInstruction(string.Format("bne $at,$zero,{0}", ins[3]));
                    return;
                case "bge":
                    GenerateForInstruction(string.Format("slt $at,{0},{1}", ins[1], ins[2]));
                    GenerateForInstruction(string.Format("beq $at,$zero,{0}", ins[3]));
                    return;
                case "ble":
                    GenerateForInstruction(string.Format("slt $at,{0},{1}", ins[1], ins[2]));
                    GenerateForInstruction(string.Format("beq $at,$zero,{0}", ins[3]));
                    return;
                case "bgtu":
                    GenerateForInstruction(string.Format("sltu $at,{0},{1}", ins[1], ins[2]));
                    GenerateForInstruction(string.Format("bne $at,$zero,{0}", ins[3]));
                    return;
                case "beqz":
                    GenerateForInstruction(string.Format("beq {0},$zero,{1}", ins[1], ins[2]));
                    return;
                case "bnez":
                    GenerateForInstruction(string.Format("bne {0},$zero,{1}", ins[1], ins[2]));
                    return;
                case "mul":
                    GenerateForInstruction(string.Format("mult {0}, {1}", ins[2], ins[3]));
                    GenerateForInstruction(string.Format("mflo {0}", ins[1]));
                    return;
                case "quo":
                    GenerateForInstruction(string.Format("div {0}, {1}", ins[2], ins[3]));
                    GenerateForInstruction(string.Format("mflo {0}", ins[1]));
                    return;
                case "rem":
                    GenerateForInstruction(string.Format("div {0}, {1}", ins[2], ins[3]));
                    GenerateForInstruction(string.Format("mfhi {0}", ins[1]));
                    return;
                case "remu":
                    GenerateForInstruction(string.Format("divu {0}, {1}", ins[2], ins[3]));
                    GenerateForInstruction(string.Format("mfhi {0}", ins[1]));
                    return;
                case "ret":
                    GenerateForInstruction(string.Format("jr $ra"));
                    return;
                #endregion
                default:
                    Console.WriteLine("No switch for mnemonic: " + mnemonic);
                    break;
            }

            //Console.WriteLine("\t" + offset + " -> " + line);

            text.Add(instr);
        }

        private byte[] MakeString(string str, bool nullterminated = true)
        {
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                str = str.Remove(str.Length - 1, 1).Remove(0, 1);
            }
            byte[] bs = new byte[str.Length + 1];
            Array.Copy(System.Text.Encoding.ASCII.GetBytes(str), bs, str.Length);
            return bs;
        }

        public static string[] ParseInstructionLine(string line)
        {
            int pos = 0;
            List<string> ins = new List<string>();
            while(pos < line.Length)
            {
                string s = "";
                while(pos < line.Length && !char.IsWhiteSpace(line[pos]) && line[pos] != ',')
                {
                    s += line[pos];
                    pos++;
                }
                if (s.Length > 0 && !s.StartsWith("#"))
                    ins.Add(s);
                pos++;
            }
            return ins.ToArray();
        }

        private struct DataItem
        {
            public string label, datatype, data;

            public DataItem(string label, string datatype, string data)
            {
                this.label = label; this.datatype = datatype; this.data = data;
            }
        }

        private DataItem ReadDataLine(string line)
        {
            string labl, type, data;
            int n = ReadUntilChar(line, ':', 0, out labl);
            n = ReadUntilChar(line, '.', n + 1, out type);
            n = ReadUntilChar(line, ' ', n - 1, out type);
            n = ReadUntilChar(line, '\n', n, out data);

            return new DataItem(labl.Trim(), type.Trim(), data.Trim());
        }

        private int ReadUntilChar(string str, char c, int startidx, out string read)
        {
            read = "";
            int n = 0;

            for(int i = startidx; i < str.Length; i++)
            {
                if (str[i] == c) break;
                read += str[i];
                n++;
            }

            return n+startidx+1;
        }
    }
}
