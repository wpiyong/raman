using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RamanMapping.Model
{
    public static class SPCHelper
    {
        static public bool OpenSPC(string filename, ref double[] wavelengths, ref double[] intensities,
                                    ref string xAxisLabel, ref string yAxisLabel, ref string notes)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open),
                    Encoding.ASCII))
                {
                    //read Main Header Block
                    SPCStructs.SPCHDR header = new SPCStructs.SPCHDR();
                    header.ftflgs = reader.ReadByte();
                    header.fversn = reader.ReadByte();
                    header.fexper = reader.ReadByte();
                    header.fexp = reader.ReadByte();
                    header.fnpts = reader.ReadUInt32();
                    header.ffirst = reader.ReadDouble();
                    header.flast = reader.ReadDouble();
                    header.fnsub = reader.ReadUInt32();
                    header.fxtype = reader.ReadByte();
                    header.fytype = reader.ReadByte();
                    header.fztype = reader.ReadByte();
                    header.fpost = reader.ReadByte();
                    header.fdate = reader.ReadUInt32();
                    header.fres = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(9));
                    header.fsource = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(9));
                    header.fpeakpt = reader.ReadUInt16();
                    reader.ReadBytes(32);//fspare
                    header.fcmnt = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(130));
                    header.fcatxt = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(30));
                    header.flogoff = reader.ReadUInt32();

                    xAxisLabel = Enum<SPC_XTypes>.Description((SPC_XTypes)header.fxtype);
                    yAxisLabel = Enum<SPC_YTypes>.Description((SPC_YTypes)header.fytype);

                    //skip rest of header
                    reader.ReadBytes(512 - 252);

                    if (header.fversn != 0x4B)
                    {
                        throw (new Exception("Invalid format"));
                    }

                    if ((header.ftflgs & 0x04) != 0)
                    {
                        throw (new Exception("Multifile format is not supported"));
                    }

                    if (header.fnpts <= 1)
                    {
                        throw (new Exception("Bad SPC file, not enough data points"));
                    }

                    //if filetype is XY then read X-axis data
                    if ((header.ftflgs & 0x80) != 0)
                    {
                        wavelengths = new double[header.fnpts];
                        intensities = new double[header.fnpts];

                        for (int i = 0; i < header.fnpts; i++)
                        {
                            wavelengths[i] = reader.ReadSingle();
                        }
                    }
                    else if (header.ftflgs == 0)
                    {
                        wavelengths = new double[header.fnpts];
                        intensities = new double[header.fnpts];

                        for (int i = 0; i < header.fnpts; i++)
                        {
                            wavelengths[i] = header.ffirst +
                                (i * ((header.flast - header.ffirst) / (header.fnpts - 1)));
                        }
                    }
                    else
                    {
                        throw (new Exception("Unsupported SPC file type"));
                    }


                    //read sub-header bytes
                    reader.ReadBytes(32);

                    //read intensities
                    if (header.fexp == 0x80)
                    {
                        for (int i = 0; i < header.fnpts; i++)
                        {
                            intensities[i] = reader.ReadSingle();
                        }
                    }
                    else
                    {
                        int intensity = 0;
                        int exp = ((header.ftflgs & 0x1) != 0) ? 16 : 32;
                        for (int i = 0; i < header.fnpts; i++)
                        {
                            if (exp == 16)
                                intensity = reader.ReadInt16();
                            else
                                intensity = reader.ReadInt32();

                            intensities[i] = Math.Pow(2, header.fexp) * intensity / Math.Pow(2, exp);
                        }
                    }

                    header.fcmnt = header.fcmnt.Replace("\0", "");
                    if (!String.IsNullOrEmpty(header.fcmnt) && header.fcmnt.Length > 0)
                    {
                        notes = header.fcmnt;
                    }

                    //64 byte log header block
                    /*
                    if (header.flogoff > 0)
                    {
                        reader.BaseStream.Seek(header.flogoff, SeekOrigin.Begin);
                        int logBlockSize = reader.ReadInt32();
                        reader.ReadInt32(); //don't care
                        int logTextOffset = reader.ReadInt32();
                        reader.BaseStream.Seek(header.flogoff + logTextOffset, SeekOrigin.Begin);
                        notes = System.Text.Encoding.ASCII.GetString(reader.ReadBytes((int)header.flogoff + logBlockSize - logTextOffset));
                    }
                    */

                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString(), "Could not open file");
                Console.WriteLine("SPCHelper: " + e.Message);
                return false;
            }

            return true;
        }


        static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", "description");
            // or return default(T);
        }

        static public bool OpenTXT(string filename, ref double[] wavelengths, ref double[] intensities,
                                    ref string xAxisLabel, ref string yAxisLabel)
        {
            try
            {
                var lineCount = File.ReadLines(filename).Count();
                using (StreamReader file = new StreamReader(filename))
                {
                    string line = file.ReadLine();
                    var values = line.Split(',');
                    int counter = 0;
                    double test;
                    if (double.TryParse(values[0], out test))
                    {
                        wavelengths = new double[lineCount];
                        intensities = new double[lineCount];
                        wavelengths[counter] = Convert.ToDouble(values[0]);
                        intensities[counter] = Convert.ToDouble(values[1]);
                        counter++;
                    }
                    else
                    {
                        wavelengths = new double[lineCount - 1];
                        intensities = new double[lineCount - 1];
                        xAxisLabel = values[0];
                        yAxisLabel = values[1];
                    }
                    while ((line = file.ReadLine()) != null)
                    {
                        values = line.Split(',');
                        wavelengths[counter] = Convert.ToDouble(values[0]);
                        intensities[counter] = Convert.ToDouble(values[1]);
                        counter++;
                    }
                }
            }
            catch
            {
                return OpenGemmoRamanTXT(filename, ref wavelengths, ref intensities,
                                    ref xAxisLabel, ref yAxisLabel);
            }
            return true;
        }

        static public bool OpenGemmoRamanTXT(string filename, ref double[] wavelengths, ref double[] intensities,
                                    ref string xAxisLabel, ref string yAxisLabel)
        {
            try
            {
                List<double> wl = new List<double>();
                List<double> counts = new List<double>();

                using (StreamReader file = new StreamReader(filename))
                {
                    string line = String.Empty;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line[0] == '#') continue;
                        var values = line.Split(',');
                        wl.Add(Convert.ToDouble(values[0]));
                        counts.Add(Convert.ToDouble(values[1]));
                    }
                }

                xAxisLabel = "Raman Shift (cm-1)";
                yAxisLabel = "Intensity";
                wavelengths = wl.ToArray();
                intensities = counts.ToArray();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Could not open file");
                Console.WriteLine("SPCHelper exception: " + ex.Message);
                return false;
            }
            return true;
        }

        static public bool OpenBwTekTXT(string filename, ref double[] wavelengths, ref double[] intensities,
                                    ref string xAxisLabel, ref string yAxisLabel)
        {
            try
            {
                using (StreamReader file = new StreamReader(filename))
                {
                    string line = file.ReadLine();
                    var values = line.Split(';');
                    if (!values[1].Contains("BWSpec"))
                        throw new Exception("Not a valid BWTek file");

                    while (values[0] != "Pixel")
                    {
                        line = file.ReadLine();
                        values = line.Split(';');
                    }

                    do
                    {
                        line = file.ReadLine();
                        values = line.Split(';');
                    } while (values[3].Trim().Length == 0);

                    List<double> wl = new List<double>();
                    List<double> i = new List<double>();

                    while (values[3].Trim().Length > 0)
                    {
                        wl.Add(Convert.ToDouble(values[3]));
                        i.Add(Convert.ToDouble(values[6]));

                        line = file.ReadLine();
                        if (line == null) break;

                        values = line.Split(';');
                    }

                    wavelengths = wl.ToArray();
                    intensities = i.ToArray();
                    xAxisLabel = "Raman Shift (cm-1)";
                    yAxisLabel = "Intensity";

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Could not open file");
                Console.WriteLine("SPCHelper exception: " + ex.Message);
                return false;
            }
            return true;
        }

        static public bool SaveToSPC(double[] wavelengths, double[] intensities, string filename,
                                        string xAxisLabel, string yAxisLabel, string notes = "")
        {
            try
            {
                SPCStructs.SPCHDR header = new SPCStructs.SPCHDR();
                // Single file, uneven X spacing
                header.ftflgs = SPCConstants.TXVALS;
                // First and last X values
                header.ffirst = Convert.ToSingle(wavelengths[0]);
                header.flast = Convert.ToSingle(wavelengths[wavelengths.Length - 1]);
                // Number of points
                header.fnpts = Convert.ToUInt32(wavelengths.Length);
                // SPC Version
                header.fversn = 0x4B;
                // Experiment type
                header.fexper = SPCConstants.SPCFLR;
                // This sets the Y data to 32-bit float
                header.fexp = 128;
                // Number of subfiles
                header.fnsub = 1;
                // X,Y,Z axis types
                header.fxtype = SPCConstants.XNMETR;
                header.fytype = SPCConstants.YCOUNT;
                // Date
                DateTime dt = DateTime.Now;
                header.fdate = (uint)dt.Year << 20;
                header.fdate |= (uint)dt.Month << 16;
                header.fdate |= (uint)dt.Day << 11;
                header.fdate |= (uint)dt.Hour << 6;
                header.fdate |= (uint)dt.Minute;

                if (notes.Length > 0)
                {
                    header.fcmnt = notes;
                }

                header.fxtype = (byte)GetValueFromDescription<SPC_XTypes>(xAxisLabel);
                header.fytype = (byte)GetValueFromDescription<SPC_YTypes>(yAxisLabel);

                SPCStructs.SUBHDR subHeader = new SPCStructs.SUBHDR();
                subHeader.subindx = 0;

                BinaryWriter binWriter = new BinaryWriter(File.Open(filename, FileMode.Create));
                binWriter.Write(StructToByteArray(header));
                foreach (double value in wavelengths)
                    binWriter.Write(Convert.ToSingle(value));
                binWriter.Write(StructToByteArray(subHeader));
                foreach (double value in intensities)
                    binWriter.Write(Convert.ToSingle(value));

                /*
                if (notes.Length > 0)
                {
                    while (binWriter.BaseStream.Position % 4 != 0)
                        binWriter.Write((byte)0);

                    //64 byte log header block
                    uint currentPosition = (uint)binWriter.BaseStream.Position;
                    binWriter.BaseStream.Seek(248, SeekOrigin.Begin);
                    binWriter.Write(currentPosition);//header.flogoff;
                    binWriter.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
                    int logsizd = System.Text.ASCIIEncoding.ASCII.GetByteCount(notes) + 64;
                    int logsizm = ((logsizd / 4096) + 1) * 4096;
                    binWriter.Write(logsizd);
                    binWriter.Write(logsizm);
                    binWriter.Write(64); //text offset
                    binWriter.Write(0); //binary log size
                    binWriter.Write(0); //size of disk area ???
                    byte[] fill = new byte[64 - 20];
                    binWriter.Write(fill);
                    byte[] bytes = Encoding.ASCII.GetBytes(notes);
                    binWriter.Write(bytes);
                }
                */

                binWriter.Close();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(new Action(() =>
                    System.Windows.MessageBox.Show(App.Current.MainWindow, ex.Message, "Save Failed")));
                return false;
            }

            return true;
        }

        static public bool SaveToTXT(double[] wavelengths, double[] intensities, string filename,
                                        string xAxisLabel, string yAxisLabel)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
                {
                    file.WriteLine(xAxisLabel + "," + yAxisLabel);
                    for (int i = 0; i < wavelengths.Length; i++)
                    {
                        file.WriteLine(wavelengths[i] + "," + intensities[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(new Action(() =>
                    System.Windows.MessageBox.Show(App.Current.MainWindow, ex.Message, "Save Failed")));
                return false;
            }

            return true;
        }

        private static byte[] StructToByteArray(object structure)
        {
            try
            {
                byte[] buffer = new byte[Marshal.SizeOf(structure)];
                GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(structure, h.AddrOfPinnedObject(), false);
                h.Free();
                return buffer;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SPCHelper exception: " + ex.Message);
                throw ex;
            }
        }
    }

    public static partial class SPCStructs
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public struct SPCHDR
        {
            // BYTE->unsigned char
            public byte ftflgs;
            // BYTE->unsigned char
            public byte fversn;
            // BYTE->unsigned char
            public byte fexper;
            // char
            public byte fexp;
            // DWORD->unsigned int
            public uint fnpts;
            // double
            public double ffirst;
            // double
            public double flast;
            // DWORD->unsigned int
            public uint fnsub;
            // BYTE->unsigned char
            public byte fxtype;
            // BYTE->unsigned char
            public byte fytype;
            // BYTE->unsigned char
            public byte fztype;
            // BYTE->unsigned char
            public byte fpost;
            // DWORD->unsigned int
            public uint fdate;
            // char[9]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 9)]
            public string fres;
            // char[9]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 9)]
            public string fsource;
            // WORD->unsigned short
            public ushort fpeakpt;
            // float[8]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R4)]
            public float[] fspare;
            // char[130]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 130)]
            public string fcmnt;
            // char[30]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 30)]
            public string fcatxt;
            // DWORD->unsigned int
            public uint flogoff;
            // DWORD->unsigned int
            public uint fmods;
            // BYTE->unsigned char
            public byte fprocs;
            // BYTE->unsigned char
            public byte flevel;
            // WORD->unsigned short
            public ushort fsampin;
            // float
            public float ffactor;
            // char[48]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 48)]
            public string fmethod;
            // float
            public float fzinc;
            // DWORD->unsigned int
            public uint fwplanes;
            // float
            public float fwinc;
            // BYTE->unsigned char
            public byte fwtype;
            // char[187]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 187)]
            public string freserv;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public struct SUBHDR
        {
            // BYTE->unsigned char
            public byte subflgs;
            // char
            public byte subexp;
            // WORD->unsigned short
            public ushort subindx;
            // float
            public float subtime;
            // float
            public float subnext;
            // float
            public float subnois;
            // DWORD->unsigned int
            public uint subnpts;
            // DWORD->unsigned int
            public uint subscan;
            // float
            public float subwlevel;
            // char[4]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 4)]
            public string subresv;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct SSFSTC
        {
            // DWORD->unsigned int
            public uint ssfposn;
            // DWORD->unsigned int
            public uint ssfsize;
            // float
            public float ssftime;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public struct LOGSTC
        {
            // DWORD->unsigned int
            public uint logsizd;
            // DWORD->unsigned int
            public uint logsizm;
            // DWORD->unsigned int
            public uint logtxto;
            // DWORD->unsigned int
            public uint logbins;
            // DWORD->unsigned int
            public uint logdsks;
            // char[44]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 44)]
            public string logspar;
        }
    }

    public static partial class SPCConstants
    {
        /* Possible settings for fxtype, fztype, fwtype. */
        /* XEV and XDIODE - XMETERS are new and not supported by all software. */
        public const byte XARB = 0; /* Arbitrary */
        public const byte XWAVEN = 1; /* Wavenumber (cm-1) */
        public const byte XUMETR = 2; /* Micrometers (um) */
        public const byte XNMETR = 3; /* Nanometers (nm) */
        public const byte XSECS = 4; /* Seconds */
        public const byte XMINUTS = 5; /* Minutes */
        public const byte XHERTZ = 6; /* Hertz (Hz) */
        public const byte XKHERTZ = 7; /* Kilohertz (KHz) */
        public const byte XMHERTZ = 8; /* Megahertz (MHz) */
        public const byte XMUNITS = 9; /* Mass (M/z) */
        public const byte XPPM = 10; /* Parts per million (PPM) */
        public const byte XDAYS = 11; /* Days */
        public const byte XYEARS = 12; /* Years */
        public const byte XRAMANS = 13; /* Raman Shift (cm-1) */
        public const byte XEV = 14; /* eV */
        public const byte ZTEXTL = 15; /* XYZ text labels in fcatxt (old 0x4D version only) */
        public const byte XDIODE = 16; /* Diode Number */
        public const byte XCHANL = 17; /* Channel */
        public const byte XDEGRS = 18; /* Degrees */
        public const byte XDEGRF = 19; /* Temperature (F) */
        public const byte XDEGRC = 20; /* Temperature (C) */
        public const byte XDEGRK = 21; /* Temperature (K) */
        public const byte XPOINT = 22; /* Data Points */
        public const byte XMSEC = 23; /* Milliseconds (mSec) */
        public const byte XUSEC = 24; /* Microseconds (uSec) */
        public const byte XNSEC = 25; /* Nanoseconds (nSec) */
        public const byte XGHERTZ = 26; /* Gigahertz (GHz) */
        public const byte XCM = 27; /* Centimeters (cm) */
        public const byte XMETERS = 28; /* Meters (m) */
        public const byte XMMETR = 29; /* Millimeters (mm) */
        public const byte XHOURS = 30; /*Hours */
        public const byte XDBLIGM = 255; /* Double interferogram (no display labels) */

        /* Possible settings for fytype. (The first 127 have positive peaks.) */
        /* YINTENS - YDEGRK and YEMISN are new and not supported by all software. */
        public const byte YARB = 0; /* Arbitrary Intensity */
        public const byte YIGRAM = 1; /* Interferogram */
        public const byte YABSRB = 2; /* Absorbance */
        public const byte YKMONK = 3; /* Kubelka-Monk */
        public const byte YCOUNT = 4; /* Counts */
        public const byte YVOLTS = 5; /* Volts */
        public const byte YDEGRS = 6; /* Degrees */
        public const byte YAMPS = 7; /* Milliamps */
        public const byte YMETERS = 8; /* Millimeters */
        public const byte YMVOLTS = 9; /* Millivolts */
        public const byte YLOGDR = 10; /* Log(1/R) */
        public const byte YPERCNT = 11; /* Percent */
        public const byte YINTENS = 12; /* Intensity */
        public const byte YRELINT = 13; /* Relative Intensity */
        public const byte YENERGY = 14; /* Energy */
        public const byte YDECBL = 16; /* Decibel */
        public const byte YDEGRF = 19; /* Temperature (F) */
        public const byte YDEGRC = 20; /* Temperature (C) */
        public const byte YDEGRK = 21; /* Temperature (K) */
        public const byte YINDRF = 22; /* Index of Refraction [N] */
        public const byte YEXTCF = 23; /* Extinction Coeff. [K] */
        public const byte YREAL = 24; /* Real */
        public const byte YIMAG = 25; /* Imaginary */
        public const byte YCMPLX = 26; /* Complex */
        public const byte YTRANS = 128; /* Transmission (ALL HIGHER MUST HAVE VALLEYS!) */
        public const byte YREFLEC = 129; /* Reflectance */
        public const byte YVALLEY = 130; /* Arbitrary or Single Beam with Valley Peaks */
        public const byte YEMISN = 131; /* Emission */

        /* Possible bit FTFLGS flag byte settings. */
        /* Note that TRANDM and TORDRD are mutually exclusive. */
        /* Code depends on TXVALS being the sign bit. TXYXYS must be 0 if TXVALS=0. */
        /* In old software without the fexper code, TCGRAM specifies a chromatogram. */
        public const byte TSPREC = 1; /* Single precision (16 bit) Y data if set. */
        public const byte TCGRAM = 2; /* Enables fexper in older software (CGM if fexper=0) */
        public const byte TMULTI = 4; /* Multiple traces format (set if more than one subfile) */
        public const byte TRANDM = 8; /* If TMULTI and TRANDM=1 then arbitrary time (Z) values */
        public const byte TORDRD = 16; /* If TMULTI abd TORDRD=1 then ordered but uneven subtimes */
        public const byte TALABS = 32; /* Set if should use fcatxt axis labels, not fxtype etc. */
        public const byte TXYXYS = 64; /* If TXVALS and multifile, then each subfile has own X's */
        public const byte TXVALS = 128; /* Floating X value array preceeds Y's (New format only) */
        /* FMODS spectral modifications flag setting conventions: */
        /* "A" (2^01) = Averaging (from multiple source traces) */
        /* "B" (2^02) = Baseline correction or offset functions */
        /* "C" (2^03) = Interferogram to spectrum Computation */
        /* "D" (2^04) = Derivative (or integrate) functions */
        /* "E" (2^06) = Resolution Enhancement functions (such as deconvolution) */
        /* "I" (2^09) = Interpolation functions */
        /* "N" (2^14) = Noise reduction smoothing */
        /* "O" (2^15) = Other functions (add, subtract, noise, etc.) */
        /* "S" (2^19) = Spectral Subtraction */
        /* "T" (2^20) = Truncation (only a portion of original X axis remains) */
        /* "W" (2^23) = When collected (date and time information) has been modified */
        /* "X" (2^24) = X units conversions or X shifting */
        /* "Y" (2^25) = Y units conversions (transmission->absorbance, etc.) */
        /* "Z" (2^26) = Zap functions (features removed or modified) */

        /* Instrument Technique fexper settings */
        /* In older software, the TCGRAM in ftflgs must be set if fexper is non-zero. */
        /* A general chromatogram is specified by a zero fexper when TCGRAM is set. */
        public const byte SPCGEN = 0; /* GeneralSPC (could be anything) */
        public const byte SPCGC = 1; /* Gas Chromatogram */
        public const byte SPCCGM = 2; /* General Chromatogram (same as SPCGEN with TCGRAM) */
        public const byte SPCHPLC = 3; /* HPLC Chromatogram */
        public const byte SPCFTIR = 4; /* FT-IR, FT-NIR, FT-Raman Spectrum or Igram (Can also be used for scanning IR.) */
        public const byte SPCNIR = 5; /* NIR Spectrum (Usually multi-spectral data sets for calibration.) */
        public const byte SPCUV = 7; /* UV-VIS Spectrum (Can be used for single scanning UV-VIS-NIR) */
        public const byte SPCXRY = 8; /* X-ray Diffraction Spectrum */
        public const byte SPCMS = 9; /* Mass Spectrum (Can be single, GC-MS, Continuum, Centroid or TOF.) */
        public const byte SPCNMR = 10; /* NMR Spectrum or FID */
        public const byte SPCRMN = 11; /* Raman Spectrum (Usually Diode Array, CCD, etc. use SPCFTIR for FT-Raman.) */
        public const byte SPCFLR = 12; /* Fluorescence Spectrum */
        public const byte SPCATM = 13; /* Atomic Spectrum */
        public const byte SPCDAD = 14; /* Chromatography Diode Array Spectra */
    }

    public enum SPC_YTypes
    {
        [Description("Arbitrary Intensity")]
        ArbitraryIntensity = 0,
        Interferogram,
        Absorbance,
        [Description("Kubelka-Monk")]
        KubelkaMonk,
        Counts,
        Volts,
        Degrees,
        Milliamps,
        Millimeters,
        Millivolts,
        [Description("Log(1/R)")]
        Log,
        Percent,
        Intensity,
        [Description("Relative Intensity")]
        RelativeIntensity,
        Energy,
        Decibel = 16,
        [Description("Temperature (F)")]
        F = 19,
        [Description("Temperature (C)")]
        C,
        [Description("Temperature (K)")]
        K,
        [Description("Index of Refraction [N]")]
        Refraction,
        [Description("Extinction Coeff. [K]")]
        Extinction,
        Real,
        Imaginary,
        Complex,
        [Description("Transmission (ALL HIGHER MUST HAVE VALLEYS!)")]
        Transmission = 128,
        Reflectance,
        [Description("Arbitrary or Single Beam with Valley Peaks")]
        SingleBeam,
        Emission
    }

    public enum SPC_XTypes
    {
        Arbitrary,
        [Description("Wavenumber (cm-1)")]
        wavenum,
        [Description("Micrometers (um)")]
        um,
        [Description("Wavelength (nm)")]
        nm,
        Seconds,
        Minutes,
        [Description("Hertz (Hz)")]
        hz,
        [Description("Kilohertz (KHz)")]
        khz,
        [Description("Megahertz (MHz)")]
        mhz,
        [Description("Mass (M/z)")]
        mmz,
        [Description("Parts per million (PPM)")]
        ppm,
        Days,
        Years,
        [Description("Raman Shift (cm-1)")]
        ramanshift,
        eV,
        [Description("XYZ text labels in fcatxt (old 0x4D version only)")]
        xyz,
        [Description("Diode Number")]
        diodenum,
        Channel,
        Degrees,
        [Description("Temperature (F)")]
        f,
        [Description("Temperature (C)")]
        c,
        [Description("Temperature (K)")]
        k,
        [Description("Data Points")]
        datapoints,
        [Description("Milliseconds (mSec)")]
        msec,
        [Description("Microseconds (uSec)")]
        usec,
        [Description("Nanoseconds (nSec)")]
        nsec,
        [Description("Gigahertz (GHz)")]
        ghz,
        [Description("Centimeters (cm)")]
        cm,
        [Description("Meters (m)")]
        m,
        [Description("Millimeters (mm)")]
        mm,
        Hours,
        [Description("Double interferogram (no display labels)")]
        Interferogram = 255
    }

    public static class Enum<T>
    {
        public static string Description(T value)
        {
            DescriptionAttribute[] da = (DescriptionAttribute[])(typeof(T).GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
            return da.Length > 0 ? da[0].Description : value.ToString();
        }
    }
}
