using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceExplorer.Native.PE
{
    public class PEHeaderInfo
    {
        public IMAGE_DOS_HEADER DosHeader { get; private set; }

        public IMAGE_FILE_HEADER FileHeader { get; private set; }

        public bool Is32Bit => (FileHeader.Characteristics & 0x100) == 0x100;

        public IMAGE_OPTIONAL_HEADER32 OptionalHeader32 { get; private set; }

        public IMAGE_OPTIONAL_HEADER64 OptionalHeader64 { get; private set; }

        public IMAGE_SECTION_HEADER[] ImageSections { get; private set; }

        public IMAGE_DATA_DIRECTORY[] ImageTables { get; private set; }

        public static PEHeaderInfo ReadInfo(Stream stream)
        {
            stream.Position = 0;

            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                try
                {
                    PEHeaderInfo info = new PEHeaderInfo()
                    {
                        DosHeader = br.ReadStructure<IMAGE_DOS_HEADER>()
                    };
                    stream.Seek(info.DosHeader.Lfanew, SeekOrigin.Begin);
                    var fileHeaderSignature = br.ReadUInt32();
                    info.FileHeader = br.ReadStructure<IMAGE_FILE_HEADER>();

                    var imageTables = new List<IMAGE_DATA_DIRECTORY>();

                    if (info.Is32Bit)
                    {
                        var optHeader = br.ReadStructure<IMAGE_OPTIONAL_HEADER32>();
                        info.OptionalHeader32 = optHeader;
                        imageTables.AddRange(new IMAGE_DATA_DIRECTORY[]
                        {
                            optHeader.ExportTable,
                            optHeader.ImportTable,
                            optHeader.ResourceTable,
                            optHeader.ExceptionTable,
                            optHeader.CertificateTable,
                            optHeader.BaseRelocationTable,
                            optHeader.Debug,
                            optHeader.Architecture,
                            optHeader.GlobalPtr,
                            optHeader.TLSTable,
                            optHeader.LoadConfigTable,
                            optHeader.BoundImport,
                            optHeader.IAT,
                            optHeader.DelayImportDescriptor,
                            optHeader.CLRRuntimeHeader,
                            optHeader.Reserved
                        });
                    }
                    else
                    {
                        var optHeader = br.ReadStructure<IMAGE_OPTIONAL_HEADER64>();
                        info.OptionalHeader64 = optHeader;
                        imageTables.AddRange(new IMAGE_DATA_DIRECTORY[]
                        {
                            optHeader.ExportTable,
                            optHeader.ImportTable,
                            optHeader.ResourceTable,
                            optHeader.ExceptionTable,
                            optHeader.CertificateTable,
                            optHeader.BaseRelocationTable,
                            optHeader.Debug,
                            optHeader.Architecture,
                            optHeader.GlobalPtr,
                            optHeader.TLSTable,
                            optHeader.LoadConfigTable,
                            optHeader.BoundImport,
                            optHeader.IAT,
                            optHeader.DelayImportDescriptor,
                            optHeader.CLRRuntimeHeader,
                            optHeader.Reserved
                        });
                    }

                    info.ImageTables = imageTables/*.Where(x => x.Size != 0)*/.ToArray();
                    info.ImageSections = new IMAGE_SECTION_HEADER[info.FileHeader.NumberOfSections];

                    for (int i = 0; i < info.ImageSections.Length; i++)
                        info.ImageSections[i] = br.ReadStructure<IMAGE_SECTION_HEADER>();

                    return info;
                }
                catch { }
            }
            return null;
        }

        public IMAGE_SECTION_HEADER GetSectionForTable(IMAGE_DATA_DIRECTORY table)
        {
            return ImageSections.OrderBy(x => x.VirtualAddress).FirstOrDefault(x =>
                table.VirtualAddress >= x.VirtualAddress &&
                table.VirtualAddress < x.VirtualAddress + x.VirtualSize);
        }

        public IMAGE_SECTION_HEADER GetSectionForVirtualOffset(uint rva)
        {
            return ImageSections.OrderBy(x => x.VirtualAddress).FirstOrDefault(x =>
                rva >= x.VirtualAddress &&
                rva < x.VirtualAddress + x.VirtualSize);
        }
    }
}
