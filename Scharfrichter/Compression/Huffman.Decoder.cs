using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Compression
{
    // this is a port of huffman.c
    // from the MAME source code originally written by Aaron Giles
    // more information:
    // https://github.com/mamedev/mame/blob/master/src/lib/util/huffman.c
    // https://github.com/mamedev/mame/blob/master/src/lib/util/huffman.h
    
    public class Huffman
    {
        public class Node
        {
            public UInt32 bits;
            public UInt32 count;
            public byte numBits;
            public Node parent;
            public UInt32 weight;
        }

        private Func<int, int, UInt16> MakeLookup = ((int code, int bits) => { return (UInt16)((code << 5) | (bits & 0x1F)); });

        private List<UInt32> dataHisto = new List<UInt32>();
        private Node[] huffNode;
        private UInt16[] lookup;
        private byte maxBits;
        private UInt32 numCodes;
        private byte prevData;
        private int rleRemaining;

        public Huffman(int newNumCodes, int newMaxBits, UInt16[] newLookup, UInt32[] newHisto, Node[] newNodes)
        {
            numCodes = (UInt32)newNumCodes;
            maxBits = (byte)newMaxBits;
            prevData = 0;
            rleRemaining = 0;
            if (newLookup != null)
                lookup = newLookup;
            else
                lookup = new UInt16[1 << maxBits];
            if (newHisto != null)
                dataHisto.AddRange(newHisto);
            if (newNodes != null)
                huffNode = newNodes;
            else
                huffNode = new Node[numCodes];
        }

        private void AssignCanonicalCodes()
        {
            // build up a histogram of bit lengths
            UInt32[] bitHisto = new UInt32[33];
            for (int curCode = 0; curCode < numCodes; curCode++)
            {
                Node node = huffNode[curCode];
                if (node.numBits > maxBits)
                    throw new Exception("Canonical code error- internal inconsistency.");
                if (node.numBits <= 32)
                    bitHisto[node.numBits]++;
            }

            // for each code length, determine the starting code number
            UInt32 curStart = 0;
            for (int codeLen = 32; codeLen > 0; codeLen--)
            {
                UInt32 nextStart = (curStart + bitHisto[codeLen]) >> 1;
                if (codeLen != 1 && (nextStart * 2) != (curStart + bitHisto[codeLen]))
                    throw new Exception("Canonical code error- internal inconsistency.");
                bitHisto[codeLen] = curStart;
                curStart = nextStart;
            }

            // now assign canonical codes
            for (int curCode = 0; curCode < numCodes; curCode++)
            {
                Node node = huffNode[curCode];
                if (node.numBits > 0)
                    node.bits = bitHisto[node.numBits]++;
            }
        }

        private void BuildLookupTable()
        {
            // iterate over all codes
            for (int curCode = 0; curCode < numCodes; curCode++)
            {
                // process all nodes which have non-zero bits
                Node node = huffNode[curCode];
                if (node.numBits > 0)
                {
                    // set up the entry
                    UInt16 value = MakeLookup(curCode, node.numBits);

                    // fill all matching entries
                    int shift = maxBits - node.numBits;
                    uint dest = node.bits << shift;
                    uint destEnd = ((node.bits + 1) << shift) - 1;
                    while (dest <= destEnd)
                    {
                        lookup[(int)dest] = value;
                        dest++;
                    }
                }
            }
        }

        public void ImportTreeRLE(BinaryReaderEx reader)
        {

            // bits per entry depends on the maxbits
            int numBits;
            if (maxBits >= 16)
                numBits = 5;
            else if (maxBits >= 8)
                numBits = 4;
            else
                numBits = 3;

            // loop until we read all the nodes
            int curNode;
            for (curNode = 0; curNode < numCodes; )
            {
                // a non-one value is just raw
                byte nodeBits = (byte)reader.ReadBits(numBits);
                if (nodeBits == 1)
                    huffNode[curNode++].numBits = nodeBits;

                // a one value is an escape code
                else
                {
                    // a double 1 is just a single 1
                    nodeBits = (byte)reader.ReadBits(numBits);
                    if (nodeBits == 1)
                        huffNode[curNode++].numBits = nodeBits;

                    // otherwise, we need one for value for the repeat count
                    else
                    {
                        int repcount = (int)reader.ReadBits(numBits) + 3;
                        while (repcount-- > 0)
                        {
                            if (huffNode[curNode] == null)
                            {
                                huffNode[curNode] = new Node();
                            }
                            huffNode[curNode++].numBits = nodeBits;
                        }
                    }
                }
            }

            // make sure we ended up with the right number
            if (curNode != numCodes)
                throw new Exception("Huffman tree import error- nodes != codes");

            // assign canonical codes for all nodes based on their code lengths
            AssignCanonicalCodes();

            // build the lookup table
            BuildLookupTable();
        }

    }
}
