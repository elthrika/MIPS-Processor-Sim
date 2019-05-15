using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MIPS_Processor
{
    public class ExecutableFile
    {

        public List<uint> Text;
        public byte[] TextBytes;
        public List<byte> Data;
        public byte[] DataBytes;
        public int DataStart;
        public int ProgramStart;

        public static void Write(string filename, List<uint> text, int programstart, List<byte> data, int datastart)
        {
            FileStream fs = File.OpenWrite(filename);

            byte[] textstr = Encoding.ASCII.GetBytes(".text");
            fs.Write(textstr, 0, 5);
            byte[] textlen = BitConverter.GetBytes(text.Count * 4);
            fs.Write(textlen, 0, 4);
            byte[] textstart = BitConverter.GetBytes(programstart);
            fs.Write(textstart, 0, 4);

            foreach (uint instr in text)
            {
                fs.Write(BitConverter.GetBytes(instr), 0, 4);
            }

            if(data != null && data.Count > 0)
            {
                byte[] datastr = Encoding.ASCII.GetBytes(".data");
                fs.Write(datastr, 0, 5);
                byte[] datalen = BitConverter.GetBytes(data.Count);
                fs.Write(datalen, 0, 4);
                byte[] datastartb = BitConverter.GetBytes(datastart);
                fs.Write(datastartb, 0, 4);
                foreach (byte d in data)
                {
                    fs.Write(new byte[1] { d }, 0, 1);
                }
            }

            fs.Close();
        }

        public static ExecutableFile Read(string filename)
        {
            FileStream fs = File.OpenRead(filename);

            byte[] strbytes = new byte[5];
            byte[] intbytes = new byte[4];

            List<uint> text = new List<uint>();
            List<byte> data = new List<byte>();
            int datastart = 0; int programstart = 0;

            fs.Read(strbytes, 0, 5);
            if (Encoding.ASCII.GetString(strbytes) != ".text")
                throw new Exception("File does not start with a .text segment");

            fs.Read(intbytes, 0, 4);
            int textlen = BitConverter.ToInt32(intbytes, 0);

            fs.Read(intbytes, 0, 4);
            programstart = BitConverter.ToInt32(intbytes, 0);

            for (int i = 0; i < textlen; i += 4)
            {
                fs.Read(intbytes, 0, 4);
                text.Add(BitConverter.ToUInt32(intbytes, 0));
            }

            fs.Read(strbytes, 0, 5);
            if (Encoding.ASCII.GetString(strbytes) == ".data")
            {
                fs.Read(intbytes, 0, 4);
                int datalen = BitConverter.ToInt32(intbytes, 0);
                fs.Read(intbytes, 0, 4);
                datastart = BitConverter.ToInt32(intbytes, 0);

                for (int i = 0; i < datalen; i++)
                {
                    data.Add((byte)fs.ReadByte());
                }
            }

            return new ExecutableFile(text, data, datastart, programstart);
        }

        private ExecutableFile(List<uint> text, List<byte> data, int datastart, int programstart)
        {
            Text = text;
            Data = data;

            TextBytes = new byte[Text.Count * 4];

            for(int i = 0; i < Text.Count; i++)
            {
                byte[] ibytes = BitConverter.GetBytes(Text[i]);
                TextBytes[i * 4]     = ibytes[0];
                TextBytes[i * 4 + 1] = ibytes[1];
                TextBytes[i * 4 + 2] = ibytes[2];
                TextBytes[i * 4 + 3] = ibytes[3];
            }

            DataBytes = Data.ToArray();
            DataStart = datastart;
            ProgramStart = programstart;
        }

    }
}
