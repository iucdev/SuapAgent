namespace suap.miniagent.S7.Types
{
    public class Class {
        /// <summary>
        /// Gets the size of the struct in bytes.
        /// </summary>
        /// <param name="structType">the type of the struct</param>
        /// <returns>the number of bytes</returns>
        public static int GetClassSize(Type classType) {
            double numBytes = 0.0;
            var properties = classType.GetProperties();
            foreach (var property in properties) {
                numBytes += CalculateBytes(property.PropertyType.Name);
            }
            return (int)numBytes;
        }

        /// <summary>
        /// Given a property name, it returns the number of bytes that is composed
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static double CalculateBytes(string propertyType) {
            double numBytes = 0;
            switch (propertyType) {
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
                case "Float":
                case "Single":
                    numBytes = Math.Ceiling(numBytes);
                    if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                        numBytes++;
                    numBytes += 4;
                    break;
                case "Double":
                case "Int64":
                case "UInt64":
                    numBytes = Math.Ceiling(numBytes);
                    if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                        numBytes++;
                    numBytes += 8;
                    break;
                case "DatetimeS7":
                    numBytes = Math.Ceiling(numBytes);
                    if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                        numBytes++;
                    numBytes += 12;
                    break;
            }
            return numBytes;
        }

        /// <summary>
        /// Creates a struct of a specified type by an array of bytes.
        /// </summary>
        /// <param name="sourceClass"></param>
        /// <param name="classType">The struct type</param>
        /// <param name="bytes">The array of bytes</param>
        /// <returns>The object depending on the struct type or null if fails(array-length != struct-length</returns>
        public static void FromBytes(object sourceClass, Type classType, byte[] bytes) {
            if (bytes == null)
                return;

            if (bytes.Length < GetClassSize(classType))
                return;

            // and decode it
            int bytePos = 0;
            int bitPos = 0;
            int byteNum = 0;
            double numBytes = 0;
            var properties = sourceClass.GetType().GetProperties();
            foreach (var property in properties) {
                switch (property.PropertyType.Name) {
                    case "Boolean":
                        // get the value
                        bytePos = (int)Math.Floor(numBytes);
                        bitPos = (int)((numBytes - bytePos) / 0.125);
                        property.SetValue(sourceClass, (bytes[bytePos] & (int)Math.Pow(2, bitPos)) != 0);
                        numBytes += 0.125;
                        break;
                    case "Byte":
                        numBytes = Math.Ceiling(numBytes);
                        property.SetValue(sourceClass, bytes[(int)numBytes]);
                        numBytes++;
                        break;
                    case "Int16":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        byteNum = (int)numBytes;
                        property.SetValue(sourceClass, BitConverter.ToInt16(new[]
                        {
                            bytes[byteNum + 1],
                            bytes[byteNum + 0]
                        }, 0));
                        numBytes += 2;
                        break;
                    case "UInt16":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;

                        byteNum = (int)numBytes;
                        property.SetValue(sourceClass, BitConverter.ToUInt16(new[]
                        {
                            bytes[byteNum + 1],
                            bytes[byteNum + 0]
                        }, 0));
                        numBytes += 2;
                        break;
                    case "Int32":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        byteNum = (int)numBytes;
                        property.SetValue(sourceClass, BitConverter.ToInt32(new[]
                        {
                                bytes[byteNum + 3],
                                bytes[byteNum + 2],
                                bytes[byteNum + 1],
                                bytes[byteNum + 0]
                        }, 0));
                        numBytes += 4;
                        break;
                    case "UInt32":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        byteNum = (int)numBytes;
                        property.SetValue(sourceClass, BitConverter.ToUInt32(new[]
                           {
                                bytes[byteNum + 3],
                                bytes[byteNum + 2],
                                bytes[byteNum + 1],
                                bytes[byteNum + 0]
                            }, 0));
                        numBytes += 4;
                        break;
                    case "UInt64":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        property.SetValue(sourceClass, BitConverter.ToUInt64(new[]
                        {
                            bytes[(int) numBytes + 7],
                            bytes[(int) numBytes + 6],
                            bytes[(int) numBytes + 5],
                            bytes[(int) numBytes + 4],
                            bytes[(int) numBytes + 3],
                            bytes[(int) numBytes + 2],
                            bytes[(int) numBytes + 1],
                            bytes[(int) numBytes + 0]
                        }, 0));
                        numBytes += 8;
                        break;
                    case "Int64":
                        numBytes = Math.Ceiling(numBytes);
                        if ((numBytes / 2 - Math.Floor(numBytes / 2.0)) > 0)
                            numBytes++;
                        // hier auswerten
                        property.SetValue(sourceClass, BitConverter.ToInt64(new[]
                        {
                            bytes[(int) numBytes + 7],
                            bytes[(int) numBytes + 6],
                            bytes[(int) numBytes + 5],
                            bytes[(int) numBytes + 4],
                            bytes[(int) numBytes + 3],
                            bytes[(int) numBytes + 2],
                            bytes[(int) numBytes + 1],
                            bytes[(int) numBytes + 0]
                        }, 0));
                        numBytes += 8;
                        break;
                    case "Single":
                    case "Float":
                        byteNum = (int)numBytes;
                        var value = BitConverter.ToSingle(new[]
                        {
                            bytes[byteNum + 3],
                            bytes[byteNum + 2],
                            bytes[byteNum + 1],
                            bytes[byteNum + 0]
                        }, 0);

                        property.SetValue(sourceClass, value);
                        numBytes += 4;
                        break;
                    case "Double":
                        byteNum = (int)numBytes;
                        var sourceBytes = new[]
                        {
                            bytes[byteNum + 7],
                            bytes[byteNum + 6],
                            bytes[byteNum + 5],
                            bytes[byteNum + 4],
                            bytes[byteNum + 3],
                            bytes[byteNum + 2],
                            bytes[byteNum + 1],
                            bytes[byteNum + 0]
                        };
                        property.SetValue(sourceClass, BitConverter.ToDouble(sourceBytes, 0));
                        numBytes += 8;
                        break;
                    case "DatetimeS7":
                        byteNum = (int)numBytes;
                        property.SetValue(sourceClass, Struct.FromBytes(property.PropertyType,
                                                                        bytes.Skip(byteNum).Take(12).ToArray()));
                        numBytes += 12;
                        break;
                }
            }
        }

        public static object FromBytesToType(string typeName, byte[] bytes, ref float position, ref byte bitPos, bool isDirectOrder = true) {
            if (bytes == null)
                return null;
            int bytePos = 0;
            byte[] sourceBytes = null;
            switch (typeName) {
                case "Boolean":
                    bytePos = (int)Math.Floor(position);
                    bitPos = (byte)((position - bytePos) / 0.125);
                    bool b = (bytes[bytePos] & (int)Math.Pow(2, bitPos)) == 1;
                    ;
                    position += 0.125f;
                    return b;
                case "Byte":
                    bytePos = (int)position;
                    var byteValue = bytes[bytePos];
                    position += 1;
                    return byteValue;
                case "Int16":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }

                    Int16 int16Value = BitConverter.ToInt16(sourceBytes, 0);
                    position += 2;
                    return int16Value;
                case "UInt16":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;


                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    UInt16 uint16Value = BitConverter.ToUInt16(sourceBytes, 0);
                    position += 2;
                    return uint16Value;
                case "Int32":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    Int32 int32Value = BitConverter.ToInt32(sourceBytes, 0);
                    position += 4;
                    return int32Value;
                case "UInt32":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;


                    if (isDirectOrder) {
                        sourceBytes = new[]
                         {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    UInt32 uint32Value = BitConverter.ToUInt32(sourceBytes, 0);
                    position += 4;
                    return uint32Value;
                    break;
                case "UInt64":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;


                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    UInt64 uint64Value = BitConverter.ToUInt64(sourceBytes, 0);
                    position += 8;
                    return uint64Value;
                    break;
                case "Int64":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;
                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    Int64 int64Value = BitConverter.ToInt64(sourceBytes, 0);
                    position += 8;
                    return int64Value;
                    break;
                case "Single":
                case "Float":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    Single singleValue = BitConverter.ToSingle(sourceBytes, 0);
                    position += 4;
                    return singleValue;
                    break;
                case "Double":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    var doubleValue = BitConverter.ToDouble(sourceBytes, 0);
                    position += 8;
                    return doubleValue;
                    break;
                case "DatetimeS7":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;
                    var datetimeS7Value = Struct.FromBytes(typeof(DatetimeS7), bytes.Skip(bytePos).Take(12).ToArray());
                    position += 12;
                    return datetimeS7Value;
                    break;
            }
            return null;
        }


        public static object FromBytesToType(string typeName, byte[] bytes, float position, bool isDirectOrder = true) {
            int bytePos = 0;
            byte[] sourceBytes = null;
            switch (typeName) {
                case "Boolean":
                    bytePos = (int)Math.Floor(position);
                    var bitPos = (byte)((position - bytePos) / 0.125);
                    bool b = (bytes[bytePos] & (int)Math.Pow(2, bitPos)) == 1;
                    return b;
                case "Byte":
                    bytePos = (int)position;
                    var byteValue = bytes[bytePos];
                    return byteValue;
                case "Int16":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }

                    Int16 int16Value = BitConverter.ToInt16(sourceBytes, 0);
                    return int16Value;
                case "UInt16":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;


                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    UInt16 uint16Value = BitConverter.ToUInt16(sourceBytes, 0);
                    return uint16Value;
                case "Int32":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    Int32 int32Value = BitConverter.ToInt32(sourceBytes, 0);
                    return int32Value;
                case "UInt32":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;


                    if (isDirectOrder) {
                        sourceBytes = new[]
                         {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    UInt32 uint32Value = BitConverter.ToUInt32(sourceBytes, 0);
                    return uint32Value;
                case "UInt64":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;


                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    UInt64 uint64Value = BitConverter.ToUInt64(sourceBytes, 0);
                    return uint64Value;
                case "Int64":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;
                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    Int64 int64Value = BitConverter.ToInt64(sourceBytes, 0);
                    return int64Value;
                case "Single":
                case "Float":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    Single singleValue = BitConverter.ToSingle(sourceBytes, 0);
                    return singleValue;
                case "Double":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    var doubleValue = BitConverter.ToDouble(sourceBytes, 0);
                    return doubleValue;
                case "DatetimeS7":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;
                    var datetimeS7Value = Struct.FromBytes(typeof(DatetimeS7), bytes.Skip(bytePos).Take(12).ToArray());
                    return datetimeS7Value;

                case "Datetime":
                    bytePos = (int)Math.Floor(position);
                    bytePos = (position - bytePos) <= 0 ? bytePos : ++bytePos;

                    if (isDirectOrder) {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 0],
                            bytes[bytePos + 1],
                            bytes[bytePos + 2],
                            bytes[bytePos + 3],
                            bytes[bytePos + 4],
                            bytes[bytePos + 5],
                            bytes[bytePos + 6],
                            bytes[bytePos + 7]
                        };
                    } else {
                        sourceBytes = new[]
                        {
                            bytes[bytePos + 7],
                            bytes[bytePos + 6],
                            bytes[bytePos + 5],
                            bytes[bytePos + 4],
                            bytes[bytePos + 3],
                            bytes[bytePos + 2],
                            bytes[bytePos + 1],
                            bytes[bytePos + 0]
                        };
                    }
                    var date = (new DateTime(1970, 1, 1)).AddMilliseconds(BitConverter.ToDouble(sourceBytes, 0));
                    return date;


                default: throw new NotImplementedException();
            };
        }
    }
}
