using System;
using System.Runtime.InteropServices;

namespace OpenSlideNET
{
    internal class OpenSlideImageSafeHandle : SafeHandle
    {
        public OpenSlideImageSafeHandle(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {

        }

        public OpenSlideImageSafeHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            var h = handle;
            if (h != IntPtr.Zero)
            {
                OpenSlideInterop.Close(h);
            }
            return true;
        }
    }
}
