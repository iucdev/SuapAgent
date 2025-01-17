﻿using System;
using System.Linq;
using System.Globalization;

namespace suap.miniagent.S7.Types
{
    public static class Struct
    {
        /// <summary>
        /// Gets the size of the struct in bytes.
        /// </summary>
        /// <param name="structType">the type of the struct</param>
        /// <returns>the number of bytes</returns>
        public static int GetStructSize(Type structType)
        {
            double numBytes = 0.0;

            System.Reflection.FieldInfo[] infos = structType.GetFields();
            foreach (System.Reflection.FieldInfo info in infos)
            {
                switch (info.FieldType.Name)
                {
                    case "Boolean":
                        numBytes += 0.125;
                        break;
                    case "Byte":
                        numBytes = Math.Ceiling(numBytes);
                        numBytes++;
                        break;
                    case "Int16":
                    case "UInt16":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        numBytes += 2;
                        break;
                    case "Int32":
                    case "UInt32":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        numBytes += 4;
                        break;
                    case "Float": 
                    case "Single": 
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        numBytes += 4;
                        break;
                    case "Double":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        numBytes += 8;
                        break;
                }
            }
            return (int)numBytes;
        }

        /// <summary>
        /// Creates a struct of a specified type by an array of bytes.
        /// </summary>
        /// <param name="structType">The struct type</param>
        /// <param name="bytes">The array of bytes</param>
        /// <returns>The object depending on the struct type or null if fails(array-length != struct-length</returns>
        public static object FromBytes(Type structType, byte[] bytes)
        {
            if (bytes == null)
                return null;

            if (bytes.Length != GetStructSize(structType))
                return null;

            // and decode it
            int bytePos = 0;
            int bitPos = 0;
            double numBytes = 0.0;
            object structValue = Activator.CreateInstance(structType);

            System.Reflection.FieldInfo[] infos = structValue.GetType().GetFields();
            foreach (System.Reflection.FieldInfo info in infos)
            {
                switch (info.FieldType.Name)
                {
                    case "Boolean":
                        // get the value
                        bytePos = (int)Math.Floor(numBytes);
                        bitPos = (int)((numBytes - (double)bytePos) / 0.125);
                        if ((bytes[bytePos] & (int)Math.Pow(2, bitPos)) != 0)
                            info.SetValue(structValue, true);
                        else
                            info.SetValue(structValue, false);
                        numBytes += 0.125;
                        break;
                    case "Byte":
                        numBytes = Math.Ceiling(numBytes);
                        info.SetValue(structValue, (byte)(bytes[(int)numBytes]));
                        numBytes++;
                        break;
                    case "Int16":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        ushort source = Word.FromBytes(bytes[(int)numBytes + 1], bytes[(int)numBytes]);
                        info.SetValue(structValue, source.ConvertToShort());
                        numBytes += 2;
                        break;
                    case "UInt16":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        info.SetValue(structValue, Word.FromBytes(bytes[(int)numBytes + 1],
                                                                          bytes[(int)numBytes]));
                        numBytes += 2;
                        break;
                    case "Int32":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        uint sourceUInt = DWord.FromBytes(bytes[(int)numBytes + 3],
                                                                           bytes[(int)numBytes + 2],
                                                                           bytes[(int)numBytes + 1],
                                                                           bytes[(int)numBytes + 0]);                        
                        info.SetValue(structValue, sourceUInt.ConvertToInt());                       
                        numBytes += 4;
                        break;
                    case "UInt32":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        info.SetValue(structValue, DWord.FromBytes(bytes[(int)numBytes + 3],
                                                                           bytes[(int)numBytes + 2],
                                                                           bytes[(int)numBytes + 1],
                                                                           bytes[(int)numBytes + 0]));
                        numBytes += 4;
                        break;
                    case "Float":
                    case "Single":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        info.SetValue(structValue, Double.FromByteArray(new byte[] { bytes[(int)numBytes],
                                                                           bytes[(int)numBytes + 1],
                                                                           bytes[(int)numBytes + 2],
                                                                           bytes[(int)numBytes + 3] }));
                        numBytes += 4;
                        break;                    
                    case "Double":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        info.SetValue(structValue, Double.FromByteArray(new byte[] { bytes[(int)numBytes],
                                                                           bytes[(int)numBytes + 1],
                                                                           bytes[(int)numBytes + 2],
                                                                           bytes[(int)numBytes + 3] }));
                        numBytes += 4;
                        break;
                }
            }
            return structValue;
        }

        /// <summary>
        /// Creates a byte array depending on the struct type.
        /// </summary>
        /// <param name="structValue">The struct object</param>
        /// <returns>A byte array or null if fails.</returns>
        public static byte[] ToBytes(object structValue)
        {
            Type type = structValue.GetType();

            int size = Struct.GetStructSize(type);
            byte[] bytes = new byte[size];
            byte[] bytes2 = null;

            int bytePos = 0;
            int bitPos = 0;
            double numBytes = 0.0;

            System.Reflection.FieldInfo[] infos = type.GetFields();
            foreach (System.Reflection.FieldInfo info in infos)
            {
                bytes2 = null;
                switch (info.FieldType.Name)
                {
                    case "Boolean":
                        // get the value
                        bytePos = (int)Math.Floor(numBytes);
                        bitPos = (int)((numBytes - (double)bytePos) / 0.125);
                        if ((bool)info.GetValue(structValue))
                            bytes[bytePos] |= (byte)Math.Pow(2, bitPos);            // is true
                        else
                            bytes[bytePos] &= (byte)(~(byte)Math.Pow(2, bitPos));   // is false
                        numBytes += 0.125;
                        break;
                    case "Byte":
                        numBytes = (int)Math.Ceiling(numBytes);
                        bytePos = (int)numBytes;
                        bytes[bytePos] = (byte)info.GetValue(structValue);
                        numBytes++;
                        break;
                    case "Int16":
                        bytes2 = Int.ToByteArray((Int16)info.GetValue(structValue));
                        break;
                    case "UInt16":
                        bytes2 = Word.ToByteArray((UInt16)info.GetValue(structValue));
                        break;
                    case "Int32":
                        bytes2 = DInt.ToByteArray((Int32)info.GetValue(structValue));
                        break;
                    case "UInt32":
                        bytes2 = DWord.ToByteArray((UInt32)info.GetValue(structValue));
                        break;
                    case "Float":
                    case "Single":
                        bytes2 = Float.ToByteArray((float)info.GetValue(structValue));
                        break;
                    case "Double":
                        bytes2 = Double.ToByteArray((double)info.GetValue(structValue));
                        break;
                }
                if (bytes2 != null)
                {
                    // add them
                    numBytes = Math.Ceiling(numBytes);
                    if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                        numBytes++;
                    bytePos = (int)numBytes;
                    for (int bCnt=0; bCnt<bytes2.Length; bCnt++)
                        bytes[bytePos + bCnt] = bytes2[bCnt];
                    numBytes += bytes2.Length;
                }
            }
            return bytes;
        }
    }
}