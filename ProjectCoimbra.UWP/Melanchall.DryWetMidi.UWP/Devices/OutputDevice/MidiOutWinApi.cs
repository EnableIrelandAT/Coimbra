using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;

namespace Melanchall.DryWetMidi.Devices
{
    internal static class MidiOutWinApi
    {
        #region Types

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;
        }

        [Flags]
        public enum MIDICAPS : uint
        {
            MIDICAPS_VOLUME = 1,
            MIDICAPS_LRVOLUME = 2,
            MIDICAPS_CACHE = 4,
            MIDICAPS_STREAM = 8
        }

        #endregion

        private static IMidiOutPort s_midiOutPort;
        private static DeviceInformationCollection deviceInformationCollection;

        #region Methods

        private static DeviceInformationCollection GetDevices()
        {
            if (deviceInformationCollection == null)
            {
                string midiOutportQueryString = MidiOutPort.GetDeviceSelector();
                Task<DeviceInformationCollection> getDeviceInformationTask = DeviceInformation.FindAllAsync(midiOutportQueryString).AsTask();
                getDeviceInformationTask.Wait();
                deviceInformationCollection =  getDeviceInformationTask.Result;
            }

            return deviceInformationCollection;
        }

        public static uint midiOutGetDevCaps(IntPtr uDeviceID, ref MIDIOUTCAPS lpMidiOutCaps, uint cbMidiOutCaps)
        {
            DeviceInformation deviceInformation = GetDevices()[uDeviceID.ToInt32()];

            lpMidiOutCaps = new MIDIOUTCAPS();
            lpMidiOutCaps.szPname = deviceInformation.Name;
            lpMidiOutCaps.wMid = 1;
            lpMidiOutCaps.wChannelMask = ushort.MaxValue;
            
            lpMidiOutCaps.wTechnology = (ushort)OutputDeviceType.MidiPort;

            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutGetErrorText(uint mmrError, StringBuilder pszText, uint cchText)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutGetNumDevs()
        {
            return (uint)GetDevices().Count;
        }

        public static uint midiOutOpen(out IntPtr lphmo, int uDeviceID, MidiWinApi.MidiMessageCallback dwCallback, IntPtr dwInstance, uint dwFlags)
        {
            if (s_midiOutPort != null)
            {
                lphmo = new IntPtr(uDeviceID);
                return MidiWinApi.MMSYSERR_NOERROR;
            }

            try
            {
                s_midiOutPort = MidiOutPort.FromIdAsync(GetDevices()[uDeviceID].Id).AsTask().Result;

                lphmo = new IntPtr(uDeviceID);

                if (s_midiOutPort == null)
                {
                    return MidiWinApi.MMSYSERR_ERROR;
                }
            }
            catch (Exception)
            {
                lphmo = new IntPtr(uDeviceID);
                return MidiWinApi.MMSYSERR_ERROR;
            }

            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutClose(IntPtr hmo)
        {
            if (s_midiOutPort != null)
            {
                s_midiOutPort.Dispose();
            }

            s_midiOutPort = null;

            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutShortMsg(IntPtr hMidiOut, uint dwMsg)
        {
            MemoryStream outputStream = new MemoryStream();
            byte[] bytes = BitConverter.GetBytes(dwMsg);
            // Array.Reverse
            outputStream.Write(bytes, 0, bytes.Length);
            s_midiOutPort.SendBuffer(outputStream.GetWindowsRuntimeBuffer());
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutGetVolume(IntPtr hmo, ref uint lpdwVolume)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutSetVolume(IntPtr hmo, uint dwVolume)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutPrepareHeader(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutUnprepareHeader(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiOutLongMsg(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        #endregion
    }
}
