using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;



namespace SmartDeviceProject2
{
    class DirectDraw
    {
        bool fullscr;
        private Control _hostControl;
        private IDirectDraw _ddraw;
        IDDrawSurface surface_prim, surface_back;
        DDSURFACEDESC desc = new DDSURFACEDESC();
        IDDrawSurface surface_draw = null;

        public DirectDraw(Control control, bool fullscreen)
        {
            fullscr = fullscreen;

            uint result;
            // Init DirectDraw
            result = DirectDrawCreate(IntPtr.Zero, out _ddraw, IntPtr.Zero);
            if (result != 0) throw new COMException(((ErrorCodes)result).ToString());

            // Set the cooperative level.
            _hostControl = control;
            if (fullscr) result = _ddraw.SetCooperativeLevel(_hostControl.Handle, CooperativeFlags.Fullscreen);
            else         result = _ddraw.SetCooperativeLevel(_hostControl.Handle, CooperativeFlags.Normal);
            //else         result = _ddraw.SetCooperativeLevel(IntPtr.Zero, CooperativeFlags.Normal);            // handle is not necessary for normal mode
            
            if (result != 0) throw new COMException(((ErrorCodes)result).ToString());



            // create primary surface
            desc.dwSize = (uint)Marshal.SizeOf(typeof(DDSURFACEDESC));
            if (fullscr)
            {
                desc.dwFlags = SurfaceDescFlags.CAPS | SurfaceDescFlags.BACKBUFFERCOUNT;
                desc.ddsCaps.dwCaps = SurfaceCapsFlags.PRIMARYSURFACE | SurfaceCapsFlags.FLIP;
                desc.dwBackBufferCount = 1;
            }
            else
            {
                desc.dwFlags = SurfaceDescFlags.CAPS;
                desc.ddsCaps.dwCaps = SurfaceCapsFlags.PRIMARYSURFACE;
            }
            //MessageBox.Show("before createsurface");
            result = _ddraw.CreateSurface(ref desc, out surface_prim, IntPtr.Zero);
            if (result != 0) throw new COMException(((ErrorCodes)result).ToString());
            //MessageBox.Show("after createsurface");

            surface_prim.GetSurfaceDesc(ref desc);
            //MessageBox.Show(desc.dwWidth + ", " + desc.dwHeight);
            //MessageBox.Show(desc.lXPitch + ", " + desc.lPitch + "; " + desc.lpSurface);

            if (fullscr)
            {
                // Get the pointer to the back buffer
                result = surface_prim.EnumAttachedSurfaces(IntPtr.Zero, Marshal.GetFunctionPointerForDelegate(new GetBuffCallBack(getBackBuff)));
                if (result != 0) throw new COMException(((ErrorCodes)result).ToString());
            }
            else 
            {
                desc.dwFlags = SurfaceDescFlags.CAPS | SurfaceDescFlags.HEIGHT | SurfaceDescFlags.WIDTH;
                desc.ddsCaps.dwCaps = SurfaceCapsFlags.VIDEOMEMORY;
                //MessageBox.Show("before createbackbuff");
                result = _ddraw.CreateSurface(ref desc, out surface_back, IntPtr.Zero);
                //MessageBox.Show("return from createbackbuff");
                if (result != 0) throw new COMException(((ErrorCodes)result).ToString());
                //MessageBox.Show("after createbackbuff");

                surface_draw = surface_back;
            }
        }

        delegate void GetBuffCallBack(IDDrawSurface surface, DDSURFACEDESC desc, IntPtr dummy);
        void getBackBuff(IDDrawSurface surface, DDSURFACEDESC desc, IntPtr dummy)
        {
            surface_draw = surface;
        }

        public void resize()
        {
            if (!fullscr)
            {
                uint result;
                IDirectDrawClipper clipper = null;
                //MessageBox.Show("creating clipper");
                result = _ddraw.CreateClipper(0, out clipper, IntPtr.Zero);
                if (result != 0) throw new COMException(((ErrorCodes)result).ToString());

                //MessageBox.Show("setting clipper's hwnd");
                result = clipper.SetHWnd(0, _hostControl.Handle);
                if (result != 0) throw new COMException(((ErrorCodes)result).ToString());

                //MessageBox.Show("setting clipper to surface");
                result = surface_prim.SetClipper(clipper);
                if (result != 0) throw new COMException(((ErrorCodes)result).ToString());

                if (!fullscr)
                {
                    result = surface_back.SetClipper(clipper);
                    if (result != 0) throw new COMException(((ErrorCodes)result).ToString());
                }
            }
        }

        public unsafe void updateScreen()
        {
            if (fullscr)
            {
                surface_prim.Flip(IntPtr.Zero, 0);
            }
            else
            {
                _ddraw.WaitForVerticalBlank(WaitForVBlankFlags.WAITVB_BLOCKEND, IntPtr.Zero);

                surface_prim.Blt(null, surface_back, null, BltFlags.DDBLT_WAITVSYNC, null);
                //DDBLTFX* pbltfx = null;
                //DDBLTFX bltfx = new DDBLTFX();
                //bltfx.dwSize = (uint)Marshal.SizeOf(typeof(DDBLTFX));
                //bltfx.dwDDFX = DDBLTFX_NOTEARING;
                //surface_prim.Blt(ref *rect, surface_back, ref *rect, BltFlags.DDBLT_WAITVSYNC, ref bltfx);
                //tagRECT rect1 = new tagRECT(x1, y1, x2, y2);
                //surface_prim.Blt(ref rect1, surface_back, ref rect1, BltFlags.DDBLT_WAITNOTBUSY, ref bltfx);
            }

        }

        public unsafe void drawPxByPx(int x1, int y1, int x2, int y2, UInt16 color)
        {
            tagRECT *rect = null;
            surface_draw.Lock(ref *rect, ref desc, LockFlags.WAITNOTBUSY, IntPtr.Zero);

            if (x1 < 0 || x2 > desc.dwWidth) return;
            if (y1 < 0 || y2 > desc.dwHeight) return;

            //int x = 7, y = 7;
            for (int x = x1; x < x2; x++)
            {
                for (int y = y1; y < y2; y++)
                {
                    byte* pPixelOffset = (byte*)desc.lpSurface + x * desc.lXPitch + y * desc.lPitch;
                    *(UInt16*)pPixelOffset = color;
                }
            }

            surface_draw.Unlock(ref *rect);


            updateScreen();
        }

        #region import API

        [DllImport("ddraw.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern uint DirectDrawCreate(IntPtr lpGUID, out IDirectDraw lplpDD, IntPtr pUnkOuter);
        
        #endregion
    }

    #region Interfaces
    [Guid("9c59509a-39bd-11d1-8c4a-00c04fd930c5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirectDraw
    {
        uint CreateClipper(uint dwFlags, out IDirectDrawClipper lplpDDClipper, IntPtr pUnkOuter);
        uint CreatePalette(CreatePaletteFlags dwFlags, ref tagPALETTEENTRY lpDDColorArray, out IDirectDrawPalette lplpDDPalette, IntPtr pUnkOuter);
        uint CreateSurface(ref DDSURFACEDESC lpDDSurfaceDesc, out IDDrawSurface lplpDDSurface, IntPtr pUnkOuter);
        uint EnumDisplayModes(uint dwFlags, ref DDSURFACEDESC lpDDSurfaceDesc2, IntPtr lpContext, IntPtr lpEnumModesCallback);
        uint EnumSurfaces(uint dwFlags, ref DDSURFACEDESC lpDDSD2, IntPtr lpContext, IntPtr lpEnumSurfacesCallback);
        uint FlipToGDISurface();
        uint GetCaps(out DDCAPS halCaps, out DDCAPS helCaps);
        uint GetDisplayMode(out DDSURFACEDESC lpDDSurfaceDesc2);
        uint GetFourCCCodes(ref int lpNumCodes, IntPtr lpCodes);

        uint GetGDISurface(out IDDrawSurface lplpGDIDDSSurface4);
        uint GetMonitorFrequency(ref uint lpdwFrequency);
        uint GetScanLine(ref uint lpdwScanLine);
        uint GetVerticalBlankStatus(ref bool lpbIsInVB);
        uint RestoreDisplayMode();
        uint SetCooperativeLevel(IntPtr hWnd, CooperativeFlags flags);
        uint SetDisplayMode(uint dwWidth, uint dwHeight, uint dwBPP, uint dwRefreshRate, SetDisplayModeFlags dwFlags);
        uint WaitForVerticalBlank(WaitForVBlankFlags flags, IntPtr handle);

        uint GetAvailableVidMem(ref DDSCAPS lpDDSCaps2, ref uint lpdwTotal, ref uint lpdwFree);
        uint GetSurfaceFromDC(IntPtr hdc, out IDDrawSurface lpDDS4);
        uint RestoreAllSurfaces();
        uint TestCooperativeLevel();
        uint GetDeviceIdentifier(ref DDDEVICEIDENTIFIER lpDDDeviceIdentifier, uint dwFlags);
    }
    [Guid("0b0e83e4-f37f-11d2-8b15-00c04f689292"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IDDrawSurface
    {
        uint AddOverlayDirtyRect(ref tagRECT rec);
        uint Blt(void* lpDestRect, IDDrawSurface lpDDSrcSurface, void* lpSrcRect, BltFlags dwFlags, void* lpDDBltFx);
        //uint Blt(ref tagRECT lpDestRect, IDDrawSurface lpDDSrcSurface, ref tagRECT lpSrcRect, BltFlags dwFlags, ref DDBLTFX lpDDBltFx);
        uint EnumAttachedSurfaces(IntPtr lpContext, IntPtr lpEnumSurfacesCallback);
        uint EnumOverlayZOrders(uint dwFlags, IntPtr lpContext, IntPtr lpEnumModesCallback);
        uint Flip(IntPtr lpDDSurfaceTargetOverride, FlipFlags dwFlags);
        uint GetBltStatus(BltFlags dwFlags);
        uint GetCaps(ref DDSCAPS lpDDSCaps);
        uint GetClipper(out IDirectDrawClipper lplpDDClipper);
        uint GetColorKey(ColorKeyFlags dwFlags, ref DDCOLORKEY lpDDColorKey);
        uint GetDC(ref IntPtr lphDC);

        uint GetFlipStatus(FlipStatusFlags dwFlags);
        uint GetOverlayPosition(ref int lplX, ref int lplY);
        uint GetPalette(out IDirectDrawPalette lpDDPalette);
        uint GetPixelFormat(ref DDPIXELFORMAT lpDDPixelFormat);

        uint GetSurfaceDesc(ref DDSURFACEDESC lpDDSurfaceDesc);
        uint IsLost();
        uint Lock(ref tagRECT lpDestRect, ref DDSURFACEDESC lpDDSurfaceDesc, LockFlags dwFlags, IntPtr hEvent);
        uint ReleaseDC(IntPtr hDC);
        uint Restore();

        uint SetClipper(IDirectDrawClipper lpDDClipper);
        uint SetColorKey(ColorKeyFlags dwFlags, ref DDCOLORKEY lpDDColorKey);
        uint SetOverlayPosition(int lX, int lY);
        uint SetPalette(IDirectDrawPalette lpDDPalette);
        uint Unlock(ref tagRECT lpRect);

        uint UpdateOverlay(ref tagRECT lpSrcRect, IDDrawSurface lpDDDestSurface, ref tagRECT lpDestRect, UpdateOverlayFlags dwFlags, ref DDOVERLAYFX lpDDOverlayFx);
        uint UpdateOverlayZOrder(ZOrderUpdateFlags dwFlags, IDDrawSurface lpDDSReference);
        uint GetDDInterface(ref IntPtr lplpDD);
        uint AlphaBlt(ref tagRECT lpDestRect, IDDrawSurface lpDDSrcSurface, ref tagRECT lpSrcRect, AlphaBltFlags dwFlags, ref DDALPHABLTFX lpDDAlphaBltFX);
    }
    [Guid("6c14db85-a733-11ce-a521-0020af0be560"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirectDrawClipper
    {
        uint GetClipList(tagRECT lpRect, out RGNDATA lpClipList, ref ushort lpdwSize);
        uint GetHWnd(ref IntPtr lpHwnd);
        uint IsClipListChanged(out bool lpbChanged);
        uint SetClipList(ref RGNDATA lpClipList, uint dwFlags);
        uint SetHWnd(uint dwFlags, IntPtr hwnd);
    }
    [Guid("6c14db84-a733-11ce-a521-0020af0be560"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirectDrawPalette
    {
        uint GetCaps(ref PaletteCapsFlags dwFlags);
        uint GetEntries(uint dwFlags, uint dwBase, uint dwNumEntries, ref tagPALETTEENTRY lpEntries);
        uint SetEntries(uint dwFlags, uint dwStartingEntry, uint dwCount, ref tagPALETTEENTRY lpEntries);
    }
    [Guid("4b9f0ee0-0d7e-11d0-9b06-00a0c903a3b8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirectDrawColorControl
    {
        uint GetColorControls(ref DDCOLORCONTROL lpColorControl);
        uint SetColorControls(ref DDCOLORCONTROL lpColorControl);

    }
    [Guid("69c11c3e-b46b-11d1-ad7a-00c04fc29b4e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirectDrawGammaControl
    {
        uint GetGammaRamp(ref DDGAMMARAMP lpGammaRamp);
        uint SetGammaRamp(ref DDGAMMARAMP lpGammaRamp);
    }


    #endregion Interfaces

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    public struct DDCOLORKEY
    {
        public uint dwColorSpaceLowValue;
        public uint dwColorSpaceHighValue;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDCAPS
    {
        public uint dwSize;			        // size of the DDCAPS structure

        // Surface capabilities

        public uint dwVidMemTotal;          // total amount of video memory
        public uint dwVidMemFree;           // amount of free video memory
        public uint dwVidMemStride;         // video memory stride (0 if linear)

        public DDSCAPS ddsCaps;                // surface caps

        public uint dwNumFourCCCodes;       // number of four cc codes

        // Palette capabilities

        public uint dwPalCaps;              // palette capabilities

        // Hardware blitting capabilities

        public uint dwBltCaps;              // driver specific capabilities
        public uint dwCKeyCaps;		        // color key blitting capabilities
        public uint dwAlphaCaps;	        // alpha blitting capabilities
        public unsafe fixed uint dwRops[8];	// ROPS supported

        // Overlay capabilities

        public uint dwOverlayCaps;          // general overlay capabilities.

        public uint dwMaxVisibleOverlays;	// maximum number of visible overlays
        public uint dwCurrVisibleOverlays;	// current number of visible overlays

        public uint dwAlignBoundarySrc;	    // source rectangle alignment
        public uint dwAlignSizeSrc;		    // source rectangle byte size
        public uint dwAlignBoundaryDest;	// dest rectangle alignment
        public uint dwAlignSizeDest;	    // dest rectangle byte size

        public uint dwMinOverlayStretch;	// minimum overlay stretch factor multiplied by 1000, eg 1000 == 1.0, 1300 == 1.3
        public uint dwMaxOverlayStretch;	// maximum overlay stretch factor multiplied by 1000, eg 1000 == 1.0, 1300 == 1.3

        // Miscalenous capabilies
        public uint dwMiscCaps;

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDSURFACEDESC
    {
        public uint dwSize;
        public SurfaceDescFlags dwFlags;
        public uint dwHeight;
        public uint dwWidth;
        public int lPitch;
        public int lXPitch;
        public uint dwBackBufferCount;
        public uint dwRefreshRate;
        public IntPtr lpSurface;
        public DDCOLORKEY ddckCKDestOverlay;
        public DDCOLORKEY ddckCKDestBlt;
        public DDCOLORKEY ddckCKSrcOverlay;
        public DDCOLORKEY ddckCKSrcBlt;
        public DDPIXELFORMAT ddpfPixelFormat;
        public DDSCAPS ddsCaps;
        public uint dwSurfaceSize;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDPIXELFORMAT
    {
        public uint dwSize;
        public PixelFormatFlags dwFlags;
        public uint dwFourCC;
        public uint dwRGBBitCount;
        public uint dwRBitMask;
        public uint dwGBitMask;
        public uint dwBBitMask;
        public uint dwRGBAlphaBitMask;

        public uint dwYUVBitCount
        {
            get
            {
                return dwRGBBitCount;
            }
        }
        public uint dwAlphaBitDepth
        {
            get
            {
                return dwRGBBitCount;
            }
        }

        public uint dwYBitMask
        {
            get
            {
                return dwRBitMask;
            }
        }


        public uint dwUBitMask
        {
            get
            {
                return dwGBitMask;
            }
        }

        public uint dwVBitMask
        {
            get
            {
                return dwBBitMask;
            }
        }



    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDSCAPS
    {
        public SurfaceCapsFlags dwCaps;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDARGB
    {
        byte blue;
        byte green;
        byte red;
        byte alpha;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDBLTFX
    {
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(DDBLTFX));
        public uint dwSize;                 // size of structure
        public uint dwROP;                  // Win32 raster operations
        public uint dwFillColor;            // color in RGB or Palettized (Brush value for Win32 ROPs)
        public DDCOLORKEY ddckDestColorkey;		// DestColorkey override
        public DDCOLORKEY ddckSrcColorkey;		// SrcColorkey override

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DDALPHABLTFX
    {
        public uint dwSize;                 // size of structure
        public DDARGB ddargbScaleFactors;     // Constant scaling factors
        public uint dwFillColor;            // color in ARGB or Palettized
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct tagRECT
    {
        public tagRECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
        public int left;
        public int top;
        public int right;
        public int bottom;

        public static implicit operator tagRECT(Rectangle value)
        {
            tagRECT rect = new tagRECT();
            rect.bottom = value.Bottom;
            rect.right = value.Right;
            rect.top = value.Top;
            rect.left = value.Left;
            return rect;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct tagPALETTEENTRY
    {
        public byte peRed;
        public byte peGreen;
        public byte peBlue;
        public byte peFlags;
    }

    public unsafe struct DDDEVICEIDENTIFIER
    {
        /*
         * These elements are for presentation to the user only. They should not be used to identify particular
         * drivers, since this is unreliable and many different strings may be associated with the same
         * device, and the same driver from different vendors.
         */

        public fixed char szDriver[512];
        public fixed char szDescription[512];

        /*
         * This element is the version of the DirectDraw/3D driver. It is legal to do <, > comparisons
         * on the whole 64 bits. Caution should be exercised if you use this element to identify problematic
         * drivers. It is recommended that guidDeviceIdentifier is used for this purpose.
         *
         * This version has the form:
         *  wProduct = HIWORD(liDriverVersion.HighPart)
         *  wVersion = LOWORD(liDriverVersion.HighPart)
         *  wSubVersion = HIWORD(liDriverVersion.LowPart)
         *  wBuild = LOWORD(liDriverVersion.LowPart)
         */

        public ulong liDriverVersion;


        /*
         * These elements can be used to identify particular chipsets. Use with extreme caution. 
         *   dwVendorId     Identifies the manufacturer. May be zero if unknown.
         *   dwDeviceId     Identifies the type of chipset. May be zero if unknown.
         *   dwSubSysId     Identifies the subsystem, typically this means the particular board. May be zero if unknown.
         *   dwRevision     Identifies the revision level of the chipset. May be zero if unknown.
         */

        public uint dwVendorId;
        public uint dwDeviceId;
        public uint dwSubSysId;
        public uint dwRevision;

        /*
         * This element can be used to check changes in driver/chipset. This GUID is a unique identifier for the
         * driver/chipset pair. Use this element if you wish to track changes to the driver/chipset in order to
         * reprofile the graphics subsystem.
         * This element can also be used to identify particular problematic drivers.
         */

        public Guid guidDeviceIdentifier;

    }


    public struct DDOVERLAYFX
    {
        public uint dwSize;                  // size of structure

        public uint dwAlphaConstBitDepth;    // Bit depth used to specify alpha constant.
        public uint dwAlphaConst;            // Constant to use as alpha channel.

        public DDCOLORKEY dckDestColorkey;         // DestColorkey override
        public DDCOLORKEY dckSrcColorkey;          // DestColorkey override

    }

    public struct DDCOLORCONTROL
    {
        public uint dwSize;
        public ColorControlFlags dwFlags;
        public int lBrightness;
        public int lContrast;
        public int lHue;
        public int lSaturation;
        public int lSharpness;
        public int lGamma;
        public int lColorEnable;

    }
    public unsafe struct DDGAMMARAMP
    {
        public fixed ushort red[256];
        public fixed ushort green[256];
        public fixed ushort blue[256];

    }


    public struct RGNDATAHEADER
    {
        public uint dwSize;
        public uint iType;
        public uint nCount;
        public uint nRgnSize;
        public tagRECT rcBound;
    }

    public unsafe struct RGNDATA
    {
        public RGNDATAHEADER rdh;
        public fixed byte Buffer[1];
    }


    #endregion Structures

    #region Constants/Flags
    public enum CreatePaletteFlags
    {
        PRIMARYSURFACE = 0x00000010,
        ALPHA = 0x00000400
    }
    public enum SetDisplayModeFlags : uint
    {
        None = 0
    }
    public enum WaitForVBlankFlags : uint
    {
        WAITVB_BLOCKBEGIN = 0,
        WAITVB_BLOCKEND = 1
    }
    public enum CooperativeFlags : uint
    {
        Normal = 0,
        Fullscreen
    }
    [Flags()]
    public enum SurfaceCapsFlags : uint
    {
        ALPHA = 0x00000001,
        BACKBUFFER = 0x00000002,
        FLIP = 0x00000004,
        FRONTBUFFER = 0x00000008,
        OVERLAY = 0x00000010,
        PALETTE = 0x00000020,
        PRIMARYSURFACE = 0x00000040,
        SYSTEMMEMORY = 0x00000080,
        VIDEOMEMORY = 0x00000100,
        WRITEONLY = 0x00000200,
        READONLY = 0x00000800,
        NOTUSERLOCKABLE = 0x00002000,
        DYNAMIC = 0x00004000
    }
    public enum PixelFormatFlags : uint
    {
        DDPF_ALPHAPIXELS = 0x00000001,
        DDPF_ALPHA = 0x00000002,
        DDPF_FOURCC = 0x00000004,
        DDPF_PALETTEINDEXED = 0x00000020,
        DDPF_RGB = 0x00000040,
        DDPF_ALPHAPREMULT = 0x00008000
    }
    public enum SurfaceDescFlags : uint
    {
        CAPS = 0x00000001,
        HEIGHT = 0x00000002,
        WIDTH = 0x00000004,
        PITCH = 0x00000008,
        XPITCH = 0x00000010,
        BACKBUFFERCOUNT = 0x00000020,
        LPSURFACE = 0x00000800,
        PIXELFORMAT = 0x00001000,
        CKDESTOVERLAY = 0x00002000,
        CKDESTBLT = 0x00004000,
        CKSRCOVERLAY = 0x00008000,
        CKSRCBLT = 0x00010000,
        REFRESHRATE = 0x00040000,
        SURFACESIZE = 0x00080000
    }
    public enum BltFlags
    {
        DDBLT_COLORFILL = 0x00000400,
        DDBLT_KEYDEST = 0x00002000,
        DDBLT_KEYDESTOVERRIDE = 0x00004000,
        DDBLT_KEYSRC = 0x00008000,
        DDBLT_KEYSRCOVERRIDE = 0x00010000,
        DDBLT_ROP = 0x00020000,
        DDBLT_WAITNOTBUSY = 0x01000000,
        DDBLT_WAITVSYNC = 0x00000001

    }
    public enum ColorKeyFlags
    {
        COLORSPACE = 0x00000001,
        DESTBLT = 0x00000002,
        DESTOVERLAY = 0x00000004,
        SRCBLT = 0x00000008,
        SRCOVERLAY = 0x00000010
    }

    public enum FlipFlags
    {
        EVEN = 0x00000002,
        ODD = 0x00000004,
        INTERVAL1 = 0x01000000,
        INTERVAL2 = 0x02000000,
        INTERVAL4 = 0x04000000,
        WAITNOTBUSY = 0x00000008,
        WAITVSYNC = 0x00000001
    }


    public enum DirectDrawEnumFlag
    {
        ATTACHEDSECONDARYDEVICES = 0x00000001,
        DETACHEDSECONDARYDEVICES = 0x00000002
    }
    public enum ZOrderUpdateFlags
    {
        SENDTOFRONT = 0x00000000,
        SENDTOBACK = 0x00000001,
        MOVEFORWARD = 0x00000002,
        MOVEBACKWARD = 0x00000003,
        INSERTINFRONTOF = 0x00000004,
        INSERTINBACKOF = 0x00000005,
    }
    public enum ContinueEnumationFlags
    {
        Cancel,
        Ok
    }
    public enum BltStatusFlags
    {

        CANBLT = 0x00000001,
        ISBLTDONE = 0x00000002
    }
    public enum FlipStatusFlags
    {
        CANFLIP = 0x00000001,
        ISFLIPDONE = 0x00000002
    }
    public enum PaletteCapsFlags
    {
        PRIMARYSURFACE = 0x00000010,
        ALPHA = 0x00000400
    }
    public enum LockFlags
    {
        READONLY = 0x00000001,
        WRITEONLY = 0x00000002,
        DISCARD = 0x00000004,
        WAITNOTBUSY = 0x00000008
    }
    public enum UpdateOverlayFlags
    {
        ALPHADEST = 0x00000001,
        ALPHADESTNEG = 0x00000002,
        ALPHASRC = 0x00000004,
        ALPHASRCNEG = 0x00000008,
        ALPHACONSTOVERRIDE = 0x00000010,
        HIDE = 0x00000020,
        KEYDEST = 0x00000040,
        KEYDESTOVERRIDE = 0x00000080,
        KEYSRC = 0x00000100,
        KEYSRCOVERRIDE = 0x00000200,
        SHOW = 0x00000400,
        MIRRORLEFTRIGHT = 0x00001000,
        MIRRORUPDOWN = 0x00002000,
        WAITNOTBUSY = 0x00004000,
        WAITVSYNC = 0x00008000

    }
    public enum AlphaBltFlags
    {
        NOBLEND = 0x02000000,
        COLORFILL = 0x00100000,
        ALPHADESTNEG = 0x00000004,
        ALPHASRCNEG = 0x00000080,
        WAITNOTBUSY = 0x01000000,
        WAITVSYNC = 0x00000001
    }

    public enum ColorControlFlags
    {
        BRIGHTNESS = 0x00000001,
        CONTRAST = 0x00000002,
        HUE = 0x00000004,
        SATURATION = 0x00000008,
        SHARPNESS = 0x00000010,
        GAMMA = 0x00000020,
        COLORENABLE = 0x00000040
    }
    public enum ErrorCodes : uint
    {
        DD_OK = 0,
        DDERR_GENERIC = 0x80004005,
        INVALIDPARAMS = 0x80070057,
        UNSUPPORTED = 0x80004001,
        OUTOFMEMORY = 0x8007000E,
        CURRENTLYNOTAVAIL = 0x88760028,
        HEIGHTALIGN = 0x8876005a,
        INCOMPATIBLEPRIMARY = 0x8876005f,
        INVALIDCAPS = 0x88760064,
        INVALIDCLIPLIST = 0x8876006e,
        INVALIDMODE = 0x88760078,
        INVALIDOBJECT = 0x88760082,
        INVALIDPIXELFORMAT = 0x88760091,
        INVALIDRECT = 0x88760096,
        LOCKEDSURFACES = 0x887600a0,
        NOCLIPLIST = 0x887600cd,
        NOALPHAHW = 0x887600b4,
        NOCOLORCONVHW = 0x887600d2,
        NOCOOPERATIVELEVELSET = 0x887600d4,
        NOCOLORKEYHW = 0x887600d7,
        NOFLIPHW = 0x887600e6,
        NOTFOUND = 0x887600ff,
        NOOVERLAYHW = 0x88760104,
        OVERLAPPINGRECTS = 0x8876010e,
        NORASTEROPHW = 0x88760118,
        NOSTRETCHHW = 0x88760136,
        NOVSYNCHW = 0x8876014f,
        NOZOVERLAYHW = 0x8876015e,
        OUTOFCAPS = 0x88760168,
        OUTOFVIDEOMEMORY = 0x8876017c,
        PALETTEBUSY = 0x88760183,
        COLORKEYNOTSET = 0x88760190,
        SURFACEBUSY = 0x887601ae,
        CANTLOCKSURFACE = 0x887601b3,
        SURFACELOST = 0x887601c2,
        TOOBIGHEIGHT = 0x887601d6,
        TOOBIGSIZE = 0x887601e0,
        TOOBIGWIDTH = 0x887601ea,
        UNSUPPORTEDFORMAT = 0x88760218,
        VERTICALBLANKINPROGRESS = 0x88760219,
        WASSTILLDRAWING = 0x8876021c,
        DIRECTDRAWALREADYCREATED = 0x88760232,
        PRIMARYSURFACEALREADYEXISTS = 0x88760234,
        REGIONTOOSMALL = 0x88760236,
        CLIPPERISUSINGHWND = 0x88760237,
        NOCLIPPERATTACHED = 0x88760238,
        NOPALETTEATTACHED = 0x8876023c,
        NOPALETTEHW = 0x8876023d,
        NOBLTHW = 0x8876023f,
        OVERLAYNOTVISIBLE = 0x88760241,
        NOOVERLAYDEST = 0x88760242,
        INVALIDPOSITION = 0x88760243,
        NOTAOVERLAYSURFACE = 0x88760244,
        EXCLUSIVEMODEALREADYSET = 0x88760245,
        NOTFLIPPABLE = 0x88760246,
        NOTLOCKED = 0x88760248,
        CANTCREATEDC = 0x88760249,
        NODC = 0x8876024a,
        WRONGMODE = 0x8876024b,
        IMPLICITLYCREATED = 0x8876024c,
        NOTPALETTIZED = 0x8876024d,
        DCALREADYCREATED = 0x8876026c,
        MOREDATA = 0x887602b2,
        VIDEONOTACTIVE = 0x887602b7,
        DEVICEDOESNTOWNSURFACE = 0x887602bb
    }
    #endregion Constants/Flags

    #region Callbacks/Delegates
    internal delegate uint EnumSurfacesCallback(IDDrawSurface lpDDSurface, ref DDSURFACEDESC lpDDSurfaceDesc,
        IntPtr lpContent);
    internal delegate uint EnumModesCallback(ref DDSURFACEDESC desc, IntPtr lpContext);
    internal delegate uint DDEnumCallbackEx(IntPtr lpGUID, [MarshalAs(UnmanagedType.LPWStr)] string lpDriverDescription,
        [MarshalAs(UnmanagedType.LPWStr)] string lpDriverName, IntPtr lpContext, IntPtr hm);
    #endregion Callbacks/Delegates
}
