using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace JPB.RemoteAdmin.Server.Native
{


    static class Extensions
    {
        public static DisposableImageData LockBitsDisposable(this Bitmap bitmap, Rectangle rect, ImageLockMode flags, PixelFormat format)
        {
            return new DisposableImageData(bitmap, rect, flags, format);
        }

        public class DisposableImageData : IDisposable
        {
            private readonly Bitmap _bitmap;
            private readonly BitmapData _data;

            internal DisposableImageData(Bitmap bitmap, Rectangle rect, ImageLockMode flags, PixelFormat format)
            {
                _bitmap = bitmap;
                _data = bitmap.LockBits(rect, flags, format);
            }

            public void Dispose()
            {
                _bitmap.UnlockBits(_data);
            }

            public IntPtr Scan0
            {
                get { return _data.Scan0; }
            }

            public int Stride
            {
                get { return _data.Stride; }
            }

            public int Width
            {
                get { return _data.Width; }
            }

            public int Height
            {
                get { return _data.Height; }
            }

            public PixelFormat PixelFormat
            {
                get { return _data.PixelFormat; }
            }

            public int Reserved
            {
                get { return _data.Reserved; }
            }
        }
    }

    ///
    /// This class shall keep all the functionality for capturing
    /// the desktop.
    ///
    public class CaptureScreen
    {
        #region Public Class Functions
        public static Bitmap GetDesktopImage()
        {
            //In size variable we shall keep the size of the screen.
            Size size;

            //Variable to keep the handle to bitmap.
            IntPtr hBitmap;

            //Here we get the handle to the desktop device context.
            IntPtr hDC = PlatformInvokeUser32.GetDC
                (PlatformInvokeUser32.GetDesktopWindow());

            //Here we make a compatible device context in memory for screen
            //device context.
            IntPtr hMemDC = PlatformInvokeGdi32.CreateCompatibleDC(hDC);

            //We pass SM_CXSCREEN constant to GetSystemMetrics to get the
            //X coordinates of the screen.
            size.cx = PlatformInvokeUser32.GetSystemMetrics
                (PlatformInvokeUser32.SM_CXSCREEN);

            //We pass SM_CYSCREEN constant to GetSystemMetrics to get the
            //Y coordinates of the screen.
            size.cy = PlatformInvokeUser32.GetSystemMetrics
                (PlatformInvokeUser32.SM_CYSCREEN);

            //We create a compatible bitmap of the screen size and using
            //the screen device context.
            hBitmap = PlatformInvokeGdi32.CreateCompatibleBitmap
                (hDC, size.cx, size.cy);

            //As hBitmap is IntPtr, we cannot check it against null.
            //For this purpose, IntPtr.Zero is used.
            if (hBitmap != IntPtr.Zero)
            {
                //Here we select the compatible bitmap in the memeory device
                //context and keep the refrence to the old bitmap.
                IntPtr hOld = (IntPtr)PlatformInvokeGdi32.SelectObject
                    (hMemDC, hBitmap);
                //We copy the Bitmap to the memory device context.
                PlatformInvokeGdi32.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC,
                    0, 0, PlatformInvokeGdi32.SRCCOPY);
                //We select the old bitmap back to the memory device context.
                PlatformInvokeGdi32.SelectObject(hMemDC, hOld);
                //We delete the memory device context.
                PlatformInvokeGdi32.DeleteDC(hMemDC);
                //We release the screen device context.
                PlatformInvokeUser32.ReleaseDC(PlatformInvokeUser32.
                    GetDesktopWindow(), hDC);
                //Image is created by Image bitmap handle and stored in
                //local variable.
                Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                //Release the memory to avoid memory leaks.
                PlatformInvokeGdi32.DeleteObject(hBitmap);
                //This statement runs the garbage collector manually.
                GC.Collect();
                //Return the bitmap 
                return bmp;
            }
            //If hBitmap is null, retun null.
            return null;
        }
        #endregion
    }
}