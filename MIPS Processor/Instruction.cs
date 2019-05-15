namespace MIPS_Processor
{

    static class Instruction
    {
        #region DWORD_TO_PARTS

        public enum Format
        {
            R,
            I,
            J
        }

        public static Format GetFormat(uint instr)
        {
            byte opcode = GetOpCode(instr);
            if (opcode == 0) return Format.R;
            else if (opcode == 2 || opcode == 3) return Format.J;
            return Format.I;
        }

        public static byte GetOpCode(uint instr)
        {
            return (byte)(0x3F & instr >> 26);
        }

        public static byte GetFunct(uint instr)
        {
            return (byte)(0x3F & instr);
        }

        public static short GetIImmediate(uint instr)
        {
            return (short)(0xFFFF & instr);
        }

        public static int GetJImmediate(uint instr)
        {
            return ((int)instr << 6) >> 6;
        }

        public static byte GetSRegister(uint instr)
        {
            return (byte)(0x1F & instr >> 21);
        }

        public static byte GetTRegister(uint instr)
        {
            return (byte)(0x1F & instr >> 16);
        }

        public static byte GetDRegister(uint instr)
        {
            return (byte)(0x1F & instr >> 11);
        }

        public static byte GetShiftAmount(uint instr)
        {
            return (byte)(0x1F & instr >> 6);
        }

        public static string ToString(uint instr)
        {
            return "";
        }

        #endregion

        #region PARTS_TO_DWORD
        
        public static uint SetOpCode(byte opcode, uint instr)
        {
            return instr | (uint)(opcode << 26);
        }

        public static uint SetFunct(byte funct, uint instr)
        {
            return instr | (uint)(0x3F & funct);
        }

        public static uint SetIImmediate(short immediate, uint instr)
        {
            return instr | (ushort)immediate;
        }

        public static uint SetJImmediate(int immediate, uint instr)
        {
            return instr | (0x3FFFFFF & (uint)immediate);
        }

        public static uint SetSRegister(byte register, uint instr)
        {
            return instr | (uint)((0x1F & register) << 21);
        }

        public static uint SetTRegister(byte register, uint instr)
        {
            return instr | (uint)((0x1F & register) << 16);
        }

        public static uint SetDRegister(byte register, uint instr)
        {
            return instr | (uint)((0x1F & register) << 11);
        }

        public static uint SetShiftAmount(byte shamt, uint instr)
        {
            return instr | (uint)((0x1F & shamt) << 6);
        }


        #endregion
    }
}
