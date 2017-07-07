//
// Copyright (c) 2017, Bianco Veigel
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DiscUtils.Btrfs.Base;
using DiscUtils.Btrfs.Base.Items;
using DiscUtils.Streams;
using DiscUtils.Vfs;

namespace DiscUtils.Btrfs
{
    internal class File : IVfsFile
    {
        protected readonly DirEntry DirEntry;
        protected readonly Context Context;

        public File(DirEntry dirEntry, Context context)
        {
            DirEntry = dirEntry;
            Context = context;
        }

        public DateTime CreationTimeUtc
        {
            get { return DirEntry.CreationTimeUtc; }
            set { throw new NotImplementedException(); }
        }

        public FileAttributes FileAttributes
        {
            get { return DirEntry.FileAttributes; }
            set { throw new NotImplementedException(); }
        }

        public IBuffer FileContent
        {
            get
            {
                var extents = Context.FindKey<ExtentData>(DirEntry.TreeId, new Key { ItemType = ItemType.ExtentData, ObjectId = DirEntry.ObjectId });
                return BufferFromExtentList(new List<ExtentData>(extents));
            }
        }

        private IBuffer BufferFromExtentList(IList<ExtentData> extents)
        {
            var builderExtents = new List<BuilderExtent>(extents.Count);

            foreach (var extent in extents)
            {
                if (extent.Encryption)
                    throw new IOException("Extent encryption is not supported");
                if (extent.Compression != ExtentDataCompression.None)
                    throw new IOException("Extent compression is not supported");

                BuilderExtent builderExtent;
                switch (extent.Type)
                {
                    case ExtentDataType.Inline:
                        builderExtent = new BuilderBytesExtent((long)extent.Key.Offset, extent.InlineData);
                        break;
                    case ExtentDataType.Regular:
                        builderExtent = null;
                        break;
                    case ExtentDataType.PreAlloc:
                        builderExtent = null;
                        break;
                    default:
                        throw new IOException("invalid extent type");
                }
                builderExtents.Add(builderExtent);
            }

            return new StreamBuffer(new BuiltStream((long)DirEntry.FileSize, builderExtents), Ownership.Dispose);
        }

        public long FileLength
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime LastAccessTimeUtc
        {
            get { return DirEntry.LastAccessTimeUtc; }
            set { throw new NotImplementedException(); }
        }

        public DateTime LastWriteTimeUtc
        {
            get { return DirEntry.LastWriteTimeUtc; }
            set { throw new NotImplementedException(); }
        }
    }
}
