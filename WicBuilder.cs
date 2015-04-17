// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ImageResizer.Plugins.Wic.InteropServices.ComTypes;
using System.Drawing;
using ImageResizer.Plugins.Wic.InteropServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Globalization;
using Xunit;

namespace ImageResizer.Plugins.WicBuilder {

    public class WicFail{

        public WicFail()
        {
        }

        private byte[] CreateJpeg() //Not used
        {
            var ms = new MemoryStream();
            using (var bit = new Bitmap(300, 300))
            {
                bit.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }

        public void ExerciseWic(string path)
        {

            byte[] data = File.ReadAllBytes(path); 

            long lData = data.Length;

            var ms = new MemoryStream();

            DecodeResizeAndEncode(data, lData, 150, 150, ms);


            GC.KeepAlive(data);
        }

        public void DecodeResizeAndEncode(byte[] fileBytes, long lData, uint width, uint height, Stream writeTo)
        {

            //A list of COM objects to destroy
            List<object> com = new List<object>();
            try {
                //Create the factory
                IWICComponentFactory factory  = (IWICComponentFactory)new WICImagingFactory();
                com.Add(factory);

                //Wrap the byte[] with a IWICStream instance
                var streamWrapper = factory.CreateStream();
                streamWrapper.InitializeFromMemory(fileBytes, (uint)lData);
                com.Add(streamWrapper);

                var decoder = factory.CreateDecoderFromStream(streamWrapper, null,
                                                              WICDecodeOptions.WICDecodeMetadataCacheOnLoad);
                com.Add(decoder);

                IWICBitmapFrameDecode frame = decoder.GetFrame(0);
                com.Add(frame);

                
                var scaler = factory.CreateBitmapScaler();
                scaler.Initialize(frame, width, height, WICBitmapInterpolationMode.WICBitmapInterpolationModeFant);
                com.Add(scaler);
                

                var outputStream = new MemoryIStream();


                EncodeToStream(factory, scaler,  outputStream);

                outputStream.WriteTo(writeTo);

            } finally {
                //Manually cleanup all the com reference counts, aggressively
                while (com.Count > 0) {
                    Marshal.ReleaseComObject(com[com.Count - 1]); //In reverse order, so no item is ever deleted out from under another.
                    com.RemoveAt(com.Count - 1);
                }
            }
        }

   

        public void EncodeToStream(IWICComponentFactory factory, IWICBitmapSource data, IStream outputStream)
        {
            //A list of COM objects to destroy
            List<object> com = new List<object>();
            try
            {

                //Find the GUID of the destination format
                Guid guidEncoder = Consts.GUID_ContainerFormatJpeg;

                //Find out the data's pixel format
                Guid pFormat = Guid.Empty;
                data.GetPixelFormat(out pFormat);

                //Create the encoder
                var encoder = factory.CreateEncoder(guidEncoder, null);
                com.Add(encoder);
                //And initialize it
                encoder.Initialize(outputStream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);

                // Create the output frame and property bag
                IWICBitmapFrameEncode outputFrame;
                var propertyBagArray = new IPropertyBag2[1];
                encoder.CreateNewFrame(out outputFrame, propertyBagArray); //An array is used instead of an out parameter... I have no idea why
                com.Add(outputFrame);
                //The property bag is a COM object...
                var propBag = propertyBagArray[0];
                com.Add(propBag);

                //Adjust encoder settings if it's a jpegs
                if (guidEncoder.Equals(Consts.GUID_ContainerFormatJpeg))
                {
                     //Configure encoder settings (see http://msdn.microsoft.com/en-us/library/windows/desktop/ee719871(v=vs.85).aspx#encoderoptions)
                    var qualityOption = new PROPBAG2[1];
                    qualityOption[0].pstrName = "ImageQuality";
                    qualityOption[0].vt = VarEnum.VT_R4;
                    object qualityValue = ((float)90) / 100;

                    propBag.Write(1, qualityOption, new object[] { qualityValue  });

                }

                //Apply the property bag
                outputFrame.Initialize(propBag);


                //Get size
                uint fw, fh;
                data.GetSize(out fw, out fh);

                //Set destination frame size
                outputFrame.SetSize(fw, fh);

                // Write the data to the output frame
                outputFrame.WriteSource(data, null);
                outputFrame.Commit();
                encoder.Commit();


            }
            finally
            {
                //Manually cleanup all the com reference counts, aggressively
                while (com.Count > 0)
                {
                    Marshal.ReleaseComObject(com[com.Count - 1]); //In reverse order, so no item is ever deleted out from under another.
                    com.RemoveAt(com.Count - 1);
                }
            }
        }

    }
}
