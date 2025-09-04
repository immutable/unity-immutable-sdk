#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using Unity.Collections;
using VoltRpc.IO;
using VoltRpc.Types;

using VoltstroStudios.UnityWebBrowser.Shared.Events;

#if !UWB_DOCS
using VoltstroStudios.NativeArraySpanExtensions;
#endif

namespace VoltstroStudios.UnityWebBrowser.Core
{
    internal class PixelsEventTypeReader : TypeReadWriter<PixelsEvent>
    {
        private NativeArray<byte> pixelData;

        public PixelsEventTypeReader(NativeArray<byte> textureData)
        {
            SetPixelDataArray(textureData);
        }

        public void SetPixelDataArray(NativeArray<byte> array)
        {
            pixelData = array;
        }

        public override void Write(BufferedWriter writer, PixelsEvent value)
        {
            throw new NotImplementedException();
        }

        public override PixelsEvent Read(BufferedReader reader)
        {
#if !UWB_DOCS
            //Read the size first
            int size = reader.ReadInt();
            if (size <= 0)
                return default;

            ReadOnlySpan<byte> data = reader.ReadBytesSpanSlice(size);
            if (!pixelData.IsCreated || pixelData.Length != size)
                return default;

            pixelData.CopyFrom(data);

#endif
            return default;
        }
    }
}

#endif