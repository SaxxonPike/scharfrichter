using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Encryption
{
    public static class BemaniPCEout
    {
        private const int DefaultKey = 55;
        private const int DefaultCounter = 56;
        private const int DefaultLast = 57;
        private const int OneBillion = 1000000000;

        private static byte[] Key = {
            0x55, 0x37, 0x9F, 0xCC, 0xE3, 0xA7, 0x7D, 0x99,
            0xDD, 0xAA, 0xBB, 0xCF, 0xFC, 0x67, 0x43, 0x17
        };

        private static void Common(Stream source, Stream target, int length, Func<int[], int, byte> process)
        {
            BinaryReader reader = new BinaryReader(source);
            BinaryWriter writer = new BinaryWriter(target);
            int[] state = new int[DefaultLast];
            int index = 0;

            while (length > 0)
            {
                int quotient = index / 10;
                int remainder = index % 10;
                index++;

                if (remainder == 0)
                    UpdateState(state, Key[quotient % 10]);

                int data = process(state, reader.ReadByte());
                writer.Write((byte)(data & 0xFF));

                length--;
            }
        }

        public static void Decrypt(Stream source, Stream target, int length)
        {
            Common(source, target, length, ExecuteDecrypt);
        }

        public static void Encrypt(Stream source, Stream target, int length)
        {
            Common(source, target, length, ExecuteEncrypt);
        }

        private static byte ExecuteDecrypt(int[] state, int data)
        {
            return (byte)((data - ProduceKey(state)) & 0xFF);
        }

        private static byte ExecuteEncrypt(int[] state, int data)
        {
            return (byte)((data + ProduceKey(state)) & 0xFF);
        }

        private static void Mix(int[] state)
        {
            for (int i = 1; i < 25; i++)
            {
                int buffer = state[i];
                buffer -= state[i + 31];
                if (buffer < 0)
                    buffer += OneBillion;
                state[i] = buffer;
            }

            for (int i = 25; i <= DefaultKey; i++)
            {
                int buffer = state[i];
                buffer -= state[i - 24];
                if (buffer < 0)
                    buffer += OneBillion;
                state[i] = buffer;
            }
        }

        private static byte ProduceKey(int[] state)
        {
            int counter;
            counter = state[DefaultCounter] + 1;
            state[DefaultCounter] = counter;

            if (counter > 0x37)
            {
                Mix(state);
                counter = 1;
            }

            state[DefaultCounter] = counter;
            return (byte)state[counter];
        }

        private static void UpdateState(int[] state, byte data)
        {
            int sourceData;
            int buffer = 1;
            sourceData = data;
            state[DefaultKey] = sourceData;

            for (int i = 0x15; i <= 0x46E; i += 0x15)
            {
                sourceData -= buffer;
                state[i % 0x37] = buffer;
                buffer = sourceData;
                if (buffer < 0)
                    buffer += OneBillion;
                sourceData = state[i % 0x37];
            }

            for (int i = 0; i < 3; i++)
                Mix(state);
            state[DefaultCounter] = 0x37;
        }
    }
}
