using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Melanchall.DryWetMidi.Devices
{
    internal static class MidiInWinApi
    {
        #region Types

        [StructLayout(LayoutKind.Sequential)]
        internal struct MIDIINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwSupport;
        }

        #endregion

        #region Methods

        public static uint midiInGetDevCaps(IntPtr uDeviceID, ref MIDIINCAPS caps, uint cbMidiInCaps)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInGetErrorText(uint wError, StringBuilder lpText, uint cchText)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInGetNumDevs()
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInOpen(out IntPtr lphMidiIn, int uDeviceID, MidiWinApi.MidiMessageCallback dwCallback, IntPtr dwInstance, uint dwFlags)
        {
            lphMidiIn = new IntPtr(0);
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInClose(IntPtr hMidiIn)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInStart(IntPtr hMidiIn)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInStop(IntPtr hMidiIn)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInReset(IntPtr hMidiIn)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInPrepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int cbMidiInHdr)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInUnprepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int cbMidiInHdr)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        public static uint midiInAddBuffer(IntPtr hMidiIn, IntPtr lpMidiInHdr, int cbMidiInHdr)
        {
            return MidiWinApi.MMSYSERR_NOERROR;
        }

        #endregion
    }
}
