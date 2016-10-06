using ResourceExplorer.Native.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ResourceExplorer.Native
{
    public static class PEHelper
    {
        public static bool VerifyPEModule(string filename, out bool isManaged, out bool is64Bit)
        {
            isManaged = false;
            is64Bit = false;
            using (Stream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if (fileStream.Length < 64)
                        return false;

                    //PE Header starts @ 0x3C (60). Its a 4 byte header.
                    fileStream.Position = 0x3C;
                    uint peHeaderPointer = binaryReader.ReadUInt32();
                    if (peHeaderPointer == 0)
                        peHeaderPointer = 0x80;

                    // Ensure there is at least enough room for the following structures:
                    //     24 byte PE Signature & Header
                    //     28 byte Standard Fields         (24 bytes for PE32+)
                    //     68 byte NT Fields               (88 bytes for PE32+)
                    // >= 128 byte Data Dictionary Table
                    if (peHeaderPointer > fileStream.Length - 256)
                    {
                        return false;
                    }


                    // Check the PE signature.  Should equal 'PE\0\0'.
                    fileStream.Position = peHeaderPointer;
                    uint peHeaderSignature = binaryReader.ReadUInt32();
                    if (peHeaderSignature != 0x00004550)
                    {
                        return false;
                    }

                    // skip over the PEHeader fields
                    fileStream.Position += 20;

                    const ushort PE32 = 0x10b;
                    const ushort PE32Plus = 0x20b;

                    // Read PE magic number from Standard Fields to determine format.
                    var peFormat = binaryReader.ReadUInt16();
                    if (peFormat != PE32 && peFormat != PE32Plus)
                    {
                        return false;
                    }

                    is64Bit = peFormat == PE32Plus;

                    // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
                    // When this is non-zero then the file contains CLI data otherwise not.
                    ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
                    fileStream.Position = dataDictionaryStart;

                    uint cliHeaderRva = binaryReader.ReadUInt32();
                    isManaged = cliHeaderRva != 0;

                    return true;
                }
            }
        }

        public static bool IsManagedAssembly(string fileName)
        {
            using (Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if (fileStream.Length < 64)
                        return false;

                    //PE Header starts @ 0x3C (60). Its a 4 byte header.
                    fileStream.Position = 0x3C;
                    uint peHeaderPointer = binaryReader.ReadUInt32();
                    if (peHeaderPointer == 0)
                        peHeaderPointer = 0x80;

                    // Ensure there is at least enough room for the following structures:
                    //     24 byte PE Signature & Header
                    //     28 byte Standard Fields         (24 bytes for PE32+)
                    //     68 byte NT Fields               (88 bytes for PE32+)
                    // >= 128 byte Data Dictionary Table
                    if (peHeaderPointer > fileStream.Length - 256)
                    {
                        return false;
                    }


                    // Check the PE signature.  Should equal 'PE\0\0'.
                    fileStream.Position = peHeaderPointer;
                    uint peHeaderSignature = binaryReader.ReadUInt32();
                    if (peHeaderSignature != 0x00004550)
                    {
                        return false;
                    }

                    // skip over the PEHeader fields
                    fileStream.Position += 20;

                    const ushort PE32 = 0x10b;
                    const ushort PE32Plus = 0x20b;

                    // Read PE magic number from Standard Fields to determine format.
                    var peFormat = binaryReader.ReadUInt16();
                    if (peFormat != PE32 && peFormat != PE32Plus)
                    {
                        return false;
                    }

                    // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
                    // When this is non-zero then the file contains CLI data otherwise not.
                    ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
                    fileStream.Position = dataDictionaryStart;

                    uint cliHeaderRva = binaryReader.ReadUInt32();
                    if (cliHeaderRva == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        public static IMAGE_FILE_HEADER GetImageFileHeader(Stream peStream)
        {

            peStream.Seek(0, SeekOrigin.Begin);
            var dosHeader = peStream.ReadStructure<IMAGE_DOS_HEADER>();
            peStream.Seek(dosHeader.Lfanew + 4, SeekOrigin.Begin);
            return peStream.ReadStructure<IMAGE_FILE_HEADER>();
        }

        private static bool IsPE64Bit(Stream peStream)
        {
            var binRead = new BinaryReader(peStream);
            var peFormat = binRead.ReadUInt16();
            peStream.Seek(-2, SeekOrigin.Current);
            return peFormat == 0x20b;
        }

        public static IMAGE_SECTION_HEADER[] GetImageSections(Stream peStream)
        {
            var fileHeader = GetImageFileHeader(peStream);

            peStream.Seek(IsPE64Bit(peStream) ? 240 : 224, SeekOrigin.Current);

            var sections = new IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];

            for (int i = 0; i < fileHeader.NumberOfSections; i++)
                sections[i] = peStream.ReadStructure<IMAGE_SECTION_HEADER>();

            return sections;
        }

        public static IMAGE_DATA_DIRECTORY[] GetImageDirectories(Stream peStream)
        {
            var fileHeader = GetImageFileHeader(peStream);
            peStream.Seek(IsPE64Bit(peStream) ? 0x70 : 0x60, SeekOrigin.Current);

            var directories = new IMAGE_DATA_DIRECTORY[16];

            for (int i = 0; i < 16; i++)
                directories[i] = peStream.ReadStructure<IMAGE_DATA_DIRECTORY>();

            return directories;
        }
    }
}
