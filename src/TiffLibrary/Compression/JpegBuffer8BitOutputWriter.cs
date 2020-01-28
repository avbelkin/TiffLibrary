﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JpegLibrary;
using TiffLibrary.Utils;

namespace TiffLibrary.Compression
{
    internal sealed class JpegBuffer8BitOutputWriter : JpegBlockOutputWriter
    {
        private int _width;
        private int _skippedScanlines;
        private int _height;
        private int _componentCount;
        private Memory<byte> _output;

        public JpegBuffer8BitOutputWriter(int width, int skippedScanlines, int height, int componentsCount, Memory<byte> output)
        {
            if (output.Length < (width * height * componentsCount))
            {
                throw new ArgumentException("Destination buffer is too small.");
            }

            _width = width;
            _skippedScanlines = skippedScanlines / 8 * 8; // Align to block
            _height = height;
            _componentCount = componentsCount;
            _output = output;
        }

        public override void WriteBlock(ref short blockRef, int componentIndex, int x, int y)
        {
            int componentCount = _componentCount;
            int width = _width;

            if (x >= width || y >= _height)
            {
                return;
            }
            if ((y + 8) <= _skippedScanlines)
            {
                // No need to decode region before the fist requested scanline.
                return;
            }

            int writeWidth = Math.Min(width - x, 8);
            int writeHeight = Math.Min(_height - y, 8);

            ref byte destinationRef = ref MemoryMarshal.GetReference(_output.Span);
            destinationRef = ref Unsafe.Add(ref destinationRef, y * width * componentCount + x * componentCount + componentIndex);

            for (int destY = 0; destY < writeHeight; destY++)
            {
                ref byte destinationRowRef = ref Unsafe.Add(ref destinationRef, destY * width * componentCount);
                for (int destX = 0; destX < writeWidth; destX++)
                {
                    Unsafe.Add(ref destinationRowRef, destX * componentCount) = TiffMathHelper.ClampTo8Bit(Unsafe.Add(ref blockRef, destX));
                }
                blockRef = ref Unsafe.Add(ref blockRef, 8);
            }
        }
    }
}
