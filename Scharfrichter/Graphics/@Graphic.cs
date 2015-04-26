using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Scharfrichter.Codec.Graphics
{
    public class Graphic
    {
        // ALL graphics are expected to be in 32 bit non-premultiplied alpha format

        public byte[] Data;
        public int Height;
        public int Width;

        static public Graphic FromBitmap(Bitmap bitmap)
        {
            Graphic result = new Graphic();
            BitmapData bdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            result.Height = bdata.Height;
            result.Width = bdata.Width;
            result.Data = new byte[bdata.Width * bdata.Height];
            Marshal.Copy(bdata.Scan0, result.Data, 0, result.Data.Length);
            bitmap.UnlockBits(bdata);
            return result;
        }

        public Bitmap ToBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData bdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(Data, 0, bdata.Scan0, Data.Length);
            bitmap.UnlockBits(bdata);
            return bitmap;
        }

        public void Write(Stream target, ImageFormat format)
        {
            using (Bitmap bitmap = ToBitmap())
                bitmap.Save(target, format);
        }

        public void WriteFile(string filename, ImageFormat format)
        {
            using (Bitmap bitmap = ToBitmap())
                bitmap.Save(filename, format);
        }
    }
}
