using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SmartDeviceProject2
{
    public class _Surface : IDisposable
    {
        //[DllImport("DirectDrawWrapperUnmanaged.dll")]
        //private unsafe static extern uint Blt(void* destSurface, ref tagRECT dest, void* sourceSurface, ref tagRECT src, uint flags, void* fx);


        //[DllImport("DirectDrawWrapperUnmanaged.dll")]
        //private unsafe static extern uint Blt(void* destSurface, void* dest, void* sourceSurface, void* src, uint flags, void* fx);


        //private DirectDrawGraphics _directDraw;
        private DDSURFACEDESC _desc;
        internal IDDrawSurface _surface;
        internal unsafe IntPtr _surfacePtr;
        private bool _isDisposed = false;

        internal _Surface(/*DirectDrawGraphics draw, */IDDrawSurface surface)
        {
            //_directDraw = draw;
            _surface = surface;
            _surfacePtr = Marshal.GetComInterfaceForObject(surface, Type.GetTypeFromCLSID(new Guid("0b0e83e4-f37f-11d2-8b15-00c04f689292")));
            DDSURFACEDESC desc = new DDSURFACEDESC();
            desc.dwSize = (uint)Marshal.SizeOf(desc);
            _surface.GetSurfaceDesc(ref desc);
            _desc = desc;
        }
        ~_Surface()
        {
            OnDispose();
        }

        public void Fill(ref Color color)
        {

            uint colorValue = ColorUtil.GetColor(ref color, ref _desc.ddpfPixelFormat);
            Fill(colorValue);


        }
        public void Fill(uint color)
        {
            uint retval=0;
            DDBLTFX fx = new DDBLTFX();
            fx.dwSize = DDBLTFX.Size;
            fx.dwFillColor = color;
            
            unsafe
            {
                // TODO: retval = _surface.Blt(null, null, null, BltFlags.DDBLT_COLORFILL, (void*)&fx);
                //retval = Blt(_surfacePtr.ToPointer(), (void*)0, (void*)0, (void*)0, (uint)BltFlags.DDBLT_COLORFILL, &fx);
            }

            if (retval != 0)
            {
                //throw ExceptionUtil.Create(retval);
                throw new COMException(((ErrorCodes)retval).ToString());
            }
        }
        public void SetColorKey(Color color,ColorKeyFlags flags)
        {
            DDCOLORKEY colorKey = new DDCOLORKEY();
            DDPIXELFORMAT format = _desc.ddpfPixelFormat;
            Color colorValue = color;


            colorKey.dwColorSpaceHighValue = ColorUtil.GetColor(ref colorValue, ref format);
            colorKey.dwColorSpaceLowValue = colorKey.dwColorSpaceHighValue;

            _surface.SetColorKey(flags, ref colorKey);
        }
        public void Draw(ref tagRECT destRectangle, _Surface source, ref tagRECT sourceRectangle, 
            BltFlags flags)
        {
            tagRECT destREC = destRectangle;
            tagRECT srcREC = sourceRectangle;
            unsafe
            {

                //TODO: _surface.Blt((void*)&destREC, source._surface,(void*)&srcREC, flags, (void*)0);
                
               // Blt(_surfacePtr.ToPointer(), ref destRectangle, source._surfacePtr.ToPointer(), ref sourceRectangle, (uint)flags, (void*)0);

                //Blt(_surfacePtr.ToPointer(), (void*)&destREC, source._surfacePtr.ToPointer(), (void*)&srcREC, (uint)flags, (void*)0);
            }
        }
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            OnDispose();
            GC.SuppressFinalize(this);
        }
        private void OnDispose()
        {
            if (_surface != null)
            {
                Marshal.FinalReleaseComObject(_surface);
                _surface = null;
            }
        }
        private uint GetColorValue(Color color)
        {
            DDPIXELFORMAT format = _desc.ddpfPixelFormat;
            Color colorValue = color;
            return ColorUtil.GetColor(ref colorValue, ref format);
        }
        internal void LoadImage(Bitmap image)
        {

            IntPtr hBitmap = image.GetHbitmap();
            IntPtr hdcImage, hdc = IntPtr.Zero;
            //_surface.Restore();
            
            // Create the device context for image and select the image.
            hdcImage = _GDI.CreateCompatibleDC(IntPtr.Zero);
            _GDI.SelectObject(hdcImage, hBitmap);

            // Get the device context for surface.
            _surface.GetDC(ref hdc);
            // Blt the image on to the surface.
            _GDI.BitBlt(hdc, 0, 0, image.Width, image.Height, hdcImage, 0, 0, TernaryRasterOperations.SRCCOPY);
            // Release surface hdc.
            _surface.ReleaseDC(hdc);
            // Delete the image hdc.
            _GDI.DeleteDC(hdcImage);
        }

        public int Width
        {
            get
            {
                return (int)_desc.dwWidth;
            }
        }
        public int Height
        {
            get
            {
                return (int)_desc.dwHeight;
            }
        }

    }




    internal static class ColorUtil
    {
        public static uint GetColor(ref Color color, ref DDPIXELFORMAT dDPIXELFORMAT)
        {
            // Just 565 for now.
            byte red = (byte)((color.R / 0xFF) * 0x1F);
            byte green = (byte)((color.G / 0xFF) * 0x3F);
            byte blue = (byte)((color.B / 0xFF) * 0x1F);

            return (uint)(
                (red & 0x1F) << 0xb |
                (green & 0x3F) << 0x5 |
                (blue & 0x1F)
            );

        }


        private static int GetMask(byte value, byte maxSize, int start, int end)
        {
            throw new NotImplementedException();
        }
        private static unsafe void GetMaskSize(int mask, out int start, out int end)
        {
            int tempMask, count = sizeof(int);
            bool started = false;

            start = end = 0;

            for (int i = 0; i < count; i++)
            {
                tempMask = 1 << i;

                if ((mask & tempMask) == tempMask)
                {
                    if (!started)
                    {
                        end = start = i;
                    }
                    else
                    {
                        end = i;
                    }
                }
            }
        }
    }

}
