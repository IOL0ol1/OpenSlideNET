using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenSlideNET
{
    public class OpenSlideImage : IDisposable
    {
        public static string LibraryVersion => OpenSlideInterop.GetVersion();

        private OpenSlideImageSafeHandle _handle;
        private FileInfo _fileInfo;

        internal OpenSlideImage(OpenSlideImageSafeHandle handle, FileInfo fileInfo)
        {
            Debug.Assert(!handle.IsInvalid);
            _handle = handle;
            _fileInfo = fileInfo;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public IntPtr Handle => _handle.DangerousGetHandle();

        public DateTime LastWriteTimeUtc => _fileInfo.LastWriteTimeUtc;

        public static OpenSlideImage Open(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            // Open file using OpenSlide
            var handle = OpenSlideInterop.Open(filename);
            if (handle.IsInvalid)
            {
                throw new OpenSlideUnsupportedFormatException();
            }
            if (!ThrowHelper.TryCheckError(handle, out string errMsg))
            {
                handle.Dispose();
                throw new OpenSlideException(errMsg);
            }
            return new OpenSlideImage(handle, fileInfo);
        }

        /// <summary>
        /// Return a string describing the format vendor of the specified file. This string is also accessible via the PROPERTY_NAME_VENDOR property.
        /// If the file is not recognized, return null.
        /// </summary>
        /// <param name="filename">the file to examine</param>
        /// <returns>the format vendor of the specified file.</returns>
        public static string DetectFormat(string filename)
        {
            return OpenSlideInterop.DetectVendor(filename);
        }

        /// <summary>
        /// The number of levels in the slide. Levels are numbered from 0 (highest resolution) to level_count - 1 (lowest resolution).
        /// </summary>
        public int LevelCount
        {
            get
            {
                EnsureNotDisposed();

                int result = OpenSlideInterop.GetLevelCount(_handle);
                if (result == -1)
                {
                    ThrowHelper.CheckAndThrowError(_handle);
                }
                return result;
            }
        }

        private ImageDimemsions? _dimemsionsCache = null;
        private void EnsureDimemsionsCached()
        {
            if (_dimemsionsCache == null)
            {
                OpenSlideInterop.GetLevel0Dimensions(_handle, out long w, out long h);
                if (w == -1 || h == -1)
                {
                    ThrowHelper.CheckAndThrowError(_handle);
                }
                _dimemsionsCache = new ImageDimemsions(w, h);
            }
        }
        /// <summary>
        /// A (width, height) tuple for level 0 of the slide.
        /// </summary>
        public ImageDimemsions Dimemsions
        {
            get
            {
                EnsureNotDisposed();
                EnsureDimemsionsCached();

                return _dimemsionsCache.Value;
            }
        }
        public long Width
        {
            get
            {
                EnsureNotDisposed();
                EnsureDimemsionsCached();

                return _dimemsionsCache.Value.Width;
            }
        }
        public long Height
        {
            get
            {
                EnsureNotDisposed();
                EnsureDimemsionsCached();

                return _dimemsionsCache.Value.Height;
            }
        }

        /// <summary>
        /// Get a (width, height) tuple for level k of the slide.
        /// </summary>
        /// <param name="level">the k level</param>
        /// <returns>A (width, height) tuple for level k of the slide.</returns>
        public ImageDimemsions GetLevelDimemsions(int level)
        {
            EnsureNotDisposed();

            OpenSlideInterop.GetLevelDimensions(_handle, level, out long w, out long h);
            if (w == -1 || h == -1)
            {
                ThrowHelper.CheckAndThrowError(_handle);
            }
            return new ImageDimemsions(w, h);
        }

        /// <summary>
        /// Get the downsample factor for level k of the slide.
        /// </summary>
        /// <param name="level">the k level</param>
        /// <returns>The downsample factor for level k of the slide.</returns>
        public double GetLevelDownsample(int level)
        {
            EnsureNotDisposed();

            double result = OpenSlideInterop.GetLevelDownsample(_handle, level);
            if (result == -1.0d)
            {
                ThrowHelper.CheckAndThrowError(_handle);
            }
            return result;
        }

        /// <summary>
        /// Metadata about the slide.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [IndexerName("Property")]
        public string this[string name]
        {
            get
            {
                EnsureNotDisposed();

                string value = OpenSlideInterop.GetPropertyValue(_handle, name);
                ThrowHelper.CheckAndThrowError(_handle);
                return value;
            }
        }
        public string Comment
        {
            get
            {
                return this[OpenSlideInterop.OpenSlidePropertyNameComment];
            }
        }

        public string Vendor
        {
            get
            {
                return this[OpenSlideInterop.OpenSlidePropertyNameVendor];
            }
        }

        public string QuickHash1
        {
            get
            {
                return this[OpenSlideInterop.OpenSlidePropertyNameQuickHash1];
            }
        }

        public string BackgroundColor
        {
            get
            {
                return this[OpenSlideInterop.OpenSlidePropertyNameBackgroundColor];
            }
        }

        public string ObjectiveColor
        {
            get
            {
                return this[OpenSlideInterop.OpenSlidePropertyNameObjectivePower];
            }
        }

        /// <summary>
        /// Get microns per pixel in the left to right direction.
        /// </summary>
        public double? MicronsPerPixelX
        {
            get
            {
                if (TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameMPPX, out string value) && double.TryParse(value, out double result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get microns per pixel in the top to bottom direction.
        /// </summary>
        public double? MicronsPerPixelY
        {
            get
            {
                if (TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameMPPY, out string value) && double.TryParse(value, out double result))
                {
                    return result;
                }
                return null;
            }
        }

        public long? BoundsX
        {
            get
            {
                if (TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsX, out string value) && long.TryParse(value, out long result))
                {
                    return result;
                }
                return null;
            }
        }

        public long? BoundsY
        {
            get
            {
                if (TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsY, out string value) && long.TryParse(value, out long result))
                {
                    return result;
                }
                return null;
            }
        }

        public long? BoundsWidth
        {
            get
            {
                if (TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsWidth, out string value) && long.TryParse(value, out long result))
                {
                    return result;
                }
                return null;
            }
        }

        public long? BoundsHeight
        {
            get
            {
                if (TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsHeight, out string value) && long.TryParse(value, out long result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the array of property names. 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> GetAllPropertyNames()
        {
            EnsureNotDisposed();

            string[] properties = OpenSlideInterop.GetPropertyNames(_handle);
            ThrowHelper.CheckAndThrowError(_handle);
            return properties;
        }

        public bool TryGetProperty(string name, out string value)
        {
            EnsureNotDisposed();

            value = OpenSlideInterop.GetPropertyValue(_handle, name);
            return value != null;
        }

        public IReadOnlyCollection<string> GetAllAssociatedImageNames()
        {
            EnsureNotDisposed();

            var associatedImages = OpenSlideInterop.GetAssociatedImageNames(_handle);
            ThrowHelper.CheckAndThrowError(_handle);
            return associatedImages;
        }

        public bool TryGetAssociatedImageDimemsions(string name, out ImageDimemsions dims)
        {
            EnsureNotDisposed();

            OpenSlideInterop.GetAssociatedImageDimensions(_handle, name, out long w, out long h);
            if (w != -1 && h != -1)
            {
                dims = new ImageDimemsions(w, h);
                return true;
            }

            dims = default;
            return false;
        }

        public unsafe byte[] ReadAssociatedImage(string name, out ImageDimemsions dimemsions)
        {
            EnsureNotDisposed();

            if (!TryGetAssociatedImageDimemsions(name, out dimemsions))
            {
                throw new KeyNotFoundException();
            }

            var data = new byte[dimemsions.Width * dimemsions.Height * 4];
            if (data.Length > 0)
            {
                fixed (void* pdata = &data[0])
                {
                    ReadAssociatedImageInternal(name, pdata);
                }
            }
            return data;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void DangerousReadAssociatedImage(string name, ref byte buffer)
        {
            EnsureNotDisposed();
            fixed (void* pdata = &buffer)
            {
                ReadAssociatedImageInternal(name, pdata);
            }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void DangerousReadAssociatedImage(string name, IntPtr buffer)
        {
            EnsureNotDisposed();
            ReadAssociatedImageInternal(name, (void*)buffer);
        }
        private unsafe void ReadAssociatedImageInternal(string name, void* pointer)
        {
            OpenSlideInterop.ReadAssociatedImage(_handle, name, pointer);
            ThrowHelper.CheckAndThrowError(_handle);
        }


        public unsafe byte[] ReadRegion(int level, long x, long y, long width, long height)
        {
            EnsureNotDisposed();

            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            var data = new byte[width * height * 4];
            if (data.Length > 0)
            {
                fixed (void* pdata = &data[0])
                {
                    ReadRegionInternal(level, x, y, width, height, pdata);
                }
            }
            return data;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void DangerousReadRegion(int level, long x, long y, long width, long height, ref byte buffer)
        {
            EnsureNotDisposed();

            fixed (void* pdata = &buffer)
            {
                ReadRegionInternal(level, x, y, width, height, pdata);
            }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void DangerousReadRegion(int level, long x, long y, long width, long height, IntPtr buffer)
        {
            EnsureNotDisposed();
            ReadRegionInternal(level, x, y, width, height, (void*)buffer);
        }
        private unsafe void ReadRegionInternal(int level, long x, long y, long width, long height, void* pointer)
        {
            OpenSlideInterop.ReadRegion(_handle, pointer, x, y, level, width, height);
            ThrowHelper.CheckAndThrowError(_handle);
        }

        public int GetBestLevelForDownsample(double downsample)
        {
            EnsureNotDisposed();

            return OpenSlideInterop.GetBestLevelForDownsample(_handle, downsample);
        }


        public readonly struct ImageDimemsions
        {
            private readonly long _width;
            private readonly long _height;

            public long Width => _width;
            public long Height => _height;

            public ImageDimemsions(long width, long height)
            {
                _width = width;
                _height = height;
            }

            public void Deconstruct(out long width, out long height)
            {
                width = _width;
                height = _height;
            }

            public static implicit operator (long Width, long Height) (ImageDimemsions dimemsions)
            {
                return (Width: dimemsions._width, Height: dimemsions._height);
            }

            public static explicit operator ImageDimemsions(ValueTuple<long, long> dimemsions)
            {
                return new ImageDimemsions(dimemsions.Item1, dimemsions.Item2);
            }
        }

        #region IDisposable Support

        public bool IsDisposed => _handle is null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EnsureNotDisposed()
        {
            if (_handle is null)
            {
                ThrowObjectDisposedException();
            }
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(typeof(OpenSlideImage).FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            Interlocked.Exchange(ref _handle, null)?.Dispose();
        }

        ~OpenSlideImage()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
