﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Melanchall.DryWetMidi.Common;

namespace Melanchall.DryWetMidi.Core
{
    /// <summary>
    /// Represents a MIDI file.
    /// </summary>
    /// <remarks>
    /// <para>An instance of <see cref="MidiFile"/> can be obtained via one of <c>Read</c>
    /// (<see cref="Read(string, ReadingSettings)"/> or <see cref="Read(Stream, ReadingSettings)"/>)
    /// static methods or via constructor which allows to create a MIDI file from scratch.</para>
    /// <para>Content of MIDI file available via <see cref="Chunks"/> property which contains instances of
    /// following chunk classes (derived from <see cref="MidiChunk"/>):</para>
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="TrackChunk"/></description>
    /// </item>
    /// <item>
    /// <description><see cref="UnknownChunk"/></description>
    /// </item>
    /// <item>
    /// <description>Any of the types specified by <see cref="ReadingSettings.CustomChunkTypes"/> property of the
    /// <see cref="ReadingSettings"/> that was used to read the file</description>
    /// </item>
    /// </list>
    /// <para>To save MIDI data to file on disk or to stream use appropriate <c>Write</c> method
    /// (<see cref="Write(string, bool, MidiFileFormat, WritingSettings)"/> or
    /// <see cref="Write(Stream, MidiFileFormat, WritingSettings)"/>).</para>
    /// <para>
    /// See <see href="https://www.midi.org/specifications-old/category/smf-specifications"/> for detailed MIDI file specification.
    /// </para>
    /// </remarks>
    /// <seealso cref="ReadingSettings"/>
    /// <seealso cref="WritingSettings"/>
    /// <seealso cref="MidiChunk"/>
    /// <seealso cref="MidiEvent"/>
    /// <seealso cref="Interaction"/>
    public sealed class MidiFile
    {
        #region Constants

        private const string RiffChunkId = "RIFF";
        private const int RmidPreambleSize = 12; // RMID_size (4) + 'RMID' (4) + 'data' (4)

        #endregion

        #region Fields

        internal ushort? _originalFormat;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MidiFile"/>.
        /// </summary>
        public MidiFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MidiFile"/> with the specified chunks.
        /// </summary>
        /// <param name="chunks">Chunks to add to the file.</param>
        /// <remarks>
        /// <para>Note that the library doesn't provide class for MIDI file header chunk
        /// so it cannot be added into the collection. Use <see cref="OriginalFormat"/> and <see cref="TimeDivision"/>
        /// properties instead. Header chunk with appropriate information will be written to a file automatically
        /// on <c>Write</c> method.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="chunks"/> is <c>null</c>.</exception>
        public MidiFile(IEnumerable<MidiChunk> chunks)
        {
            ThrowIfArgument.IsNull(nameof(chunks), chunks);

            Chunks.AddRange(chunks);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MidiFile"/> with the specified chunks.
        /// </summary>
        /// <param name="chunks">Chunks to add to the file.</param>
        /// <remarks>
        /// <para>Note that the library doesn't provide class for MIDI file header chunk
        /// so it cannot be added into the collection. Use <see cref="OriginalFormat"/> and <see cref="TimeDivision"/>
        /// properties instead. Header chunk with appropriate information will be written to a file automatically
        /// on <c>Write</c> method.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="chunks"/> is <c>null</c>.</exception>
        public MidiFile(params MidiChunk[] chunks)
            : this(chunks as IEnumerable<MidiChunk>)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a time division of a MIDI file.
        /// </summary>
        /// <remarks>
        /// <para>Time division specifies the meaning of the delta-times of MIDI events within <see cref="TrackChunk"/>.
        /// There are two types of the time division: ticks per quarter note and SMPTE. The first type represented by
        /// <see cref="TicksPerQuarterNoteTimeDivision"/> class and the second one represented by
        /// <see cref="SmpteTimeDivision"/> class.</para>
        /// </remarks>
        public TimeDivision TimeDivision { get; set; } = new TicksPerQuarterNoteTimeDivision();

        /// <summary>
        /// Gets collection of chunks of a MIDI file.
        /// </summary>
        /// <remarks>
        /// <para> MIDI files are made up of chunks. Сollection returned by this property may contain chunks
        /// of the following types:</para>
        /// <list type="bullet">
        /// <item>
        /// <description><see cref="TrackChunk"/></description>
        /// </item>
        /// <item>
        /// <description><see cref="UnknownChunk"/></description>
        /// </item>
        /// <item>
        /// <description>Custom chunk types</description>
        /// </item>
        /// </list>
        /// <para>You cannot create instance of the <see cref="UnknownChunk"/>. It will be created by the
        /// library on reading unknown chunk (neither track chunk nor custom one).</para>
        /// </remarks>
        public ChunksCollection Chunks { get; } = new ChunksCollection();

        /// <summary>
        /// Gets original format of the file was read or <c>null</c> if the current <see cref="MidiFile"/>
        /// was created by constructor.
        /// </summary>
        /// <exception cref="UnknownFileFormatException">File format is unknown.</exception>
        /// <exception cref="InvalidOperationException">Unable to get original format of the file. It means
        /// the current <see cref="MidiFile"/> was created via constructor rather than via <c>Read</c> method.</exception>
        public MidiFileFormat OriginalFormat
        {
            get
            {
                if (_originalFormat == null)
                    throw new InvalidOperationException("Unable to get original format of the file.");

                var formatValue = _originalFormat.Value;
                if (!Enum.IsDefined(typeof(MidiFileFormat), formatValue))
                    throw new UnknownFileFormatException(formatValue);

                return (MidiFileFormat)formatValue;
            }
            internal set
            {
                _originalFormat = (ushort)value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reads a MIDI file specified by its full path.
        /// </summary>
        /// <param name="filePath">Path to the file to read.</param>
        /// <param name="settings">Settings according to which the file must be read. Specify <c>null</c> to use
        /// default settings.</param>
        /// <returns>An instance of the <see cref="MidiFile"/> representing a MIDI file.</returns>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is a zero-length string,
        /// contains only white space, or contains one or more invalid characters as defined by
        /// <see cref="Path.InvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c>.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined
        /// maximum length. For example, on Windows-based platforms, paths must be less than 248 characters,
        /// and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example,
        /// it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while reading the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="filePath"/> is in an invalid format.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// One of the following errors occured:
        /// <list type="bullet">
        /// <item>
        /// <description>This operation is not supported on the current platform.</description>
        /// </item>
        /// <item>
        /// <description><paramref name="filePath"/> specified a directory.</description>
        /// </item>
        /// <item>
        /// <description>The caller does not have the required permission.</description>
        /// </item>
        /// </list>
        /// </exception>
        /// <exception cref="NoHeaderChunkException">There is no header chunk in a file and that should be treated as error
        /// according to the <see cref="ReadingSettings.NoHeaderChunkPolicy"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidChunkSizeException">Actual header or track chunk's size differs from the one declared
        /// in its header and that should be treated as error according to the <see cref="ReadingSettings.InvalidChunkSizePolicy"/>
        /// of the <paramref name="settings"/>.</exception>
        /// <exception cref="UnknownChunkException">Chunk to be read has unknown ID and that
        /// should be treated as error accordng to the <see cref="ReadingSettings.UnknownChunkIdPolicy"/> of the
        /// <paramref name="settings"/>.</exception>
        /// <exception cref="UnexpectedTrackChunksCountException">Actual track chunks
        /// count differs from the expected one (declared in the file header) and that should be treated as error according to
        /// the <see cref="ReadingSettings.UnexpectedTrackChunksCountPolicy"/> of the specified <paramref name="settings"/>.</exception>
        /// <exception cref="UnknownFileFormatException">The header chunk of the file specifies unknown file format and
        /// that should be treated as error according to the <see cref="ReadingSettings.UnknownFileFormatPolicy"/> of
        /// the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidChannelEventParameterValueException">Value of a channel event's parameter
        /// just read is invalid (is out of [0; 127] range) and that should be treated as error according to the
        /// <see cref="ReadingSettings.InvalidChannelEventParameterValuePolicy"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidMetaEventParameterValueException">Value of a meta event's parameter
        /// just read is invalid and that should be treated as error according to the
        /// <see cref="ReadingSettings.InvalidMetaEventParameterValuePolicy"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="UnknownChannelEventException">Reader has encountered an unknown channel event and that
        /// should be treated as error according to the <see cref="ReadingSettings.UnknownChannelEventPolicy"/> of
        /// the <paramref name="settings"/>.</exception>
        /// <exception cref="NotEnoughBytesException">MIDI file data cannot be read since the reader's underlying stream doesn't
        /// have enough bytes and that should be treated as error according to the <see cref="ReadingSettings.NotEnoughBytesPolicy"/>
        /// of the <paramref name="settings"/>.</exception>
        /// <exception cref="UnexpectedRunningStatusException">Unexpected running status is encountered.</exception>
        /// <exception cref="MissedEndOfTrackEventException">Track chunk doesn't end with <c>End Of Track</c> event and that
        /// should be treated as error accordng to the <see cref="ReadingSettings.MissedEndOfTrackPolicy"/> of
        /// the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ReaderSettings.Buffer"/> of <paramref name="settings"/>
        /// is <c>null</c> in case of <see cref="ReaderSettings.BufferingPolicy"/> set to
        /// <see cref="BufferingPolicy.UseCustomBuffer"/>.</exception>
        public static MidiFile Read(string filePath, ReadingSettings settings = null)
        {
            using (var fileStream = FileUtilities.OpenFileForRead(filePath))
            {
                return Read(fileStream, settings);
            }
        }

        /// <summary>
        /// Writes the MIDI file to location specified by full path.
        /// </summary>
        /// <param name="filePath">Full path of the file to write to.</param>
        /// <param name="overwriteFile">If <c>true</c> and file specified by <paramref name="filePath"/> already
        /// exists it will be overwritten; if <c>false</c> and the file exists exception will be thrown.</param>
        /// <param name="format">MIDI file format to write in.</param>
        /// <param name="settings">Settings according to which the file must be written. Specify <c>null</c> to use
        /// default settings.</param>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is a zero-length string,
        /// contains only white space, or contains one or more invalid characters as defined by
        /// <see cref="Path.InvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="format"/> specified an invalid value.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined
        /// maximum length. For example, on Windows-based platforms, paths must be less than 248 characters,
        /// and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example,
        /// it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while writing the file.</exception>
        /// <exception cref="NotSupportedException"><paramref name="filePath"/> is in an invalid format.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// One of the following errors occured:
        /// <list type="bullet">
        /// <item>
        /// <description>This operation is not supported on the current platform.</description>
        /// </item>
        /// <item>
        /// <description><paramref name="filePath"/> specified a directory.</description>
        /// </item>
        /// <item>
        /// <description>The caller does not have the required permission.</description>
        /// </item>
        /// </list>
        /// </exception>
        /// <exception cref="InvalidOperationException">Time division is <c>null</c>.</exception>
        /// <exception cref="TooManyTrackChunksException">Count of track chunks presented in the file
        /// exceeds maximum value allowed for MIDI file.</exception>
        public void Write(string filePath, bool overwriteFile = false, MidiFileFormat format = MidiFileFormat.MultiTrack, WritingSettings settings = null)
        {
            ThrowIfArgument.IsInvalidEnumValue(nameof(format), format);

            using (var fileStream = FileUtilities.OpenFileForWrite(filePath, overwriteFile))
            {
                Write(fileStream, format, settings);
            }
        }

        /// <summary>
        /// Reads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">Stream to read file from.</param>
        /// <param name="settings">Settings according to which the file must be read. Specify <c>null</c> to use
        /// default settings.</param>
        /// <returns>An instance of the <see cref="MidiFile"/> representing a MIDI file was read from the stream.</returns>
        /// <remarks>
        /// Stream must be readable, seekable and be able to provide its position and length via <see cref="Stream.Position"/>
        /// and <see cref="Stream.Length"/> properties.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// One of the following errors occured:
        /// <list type="bullet">
        /// <item>
        /// <description><paramref name="stream"/> doesn't support reading.</description>
        /// </item>
        /// <item>
        /// <description><paramref name="stream"/> is already read.</description>
        /// </item>
        /// </list>
        /// </exception>
        /// <exception cref="IOException">An I/O error occurred while reading the file.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="stream"/> is disposed.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// One of the following errors occured:
        /// <list type="bullet">
        /// <item>
        /// <description>This operation is not supported on the current platform.</description>
        /// </item>
        /// <item>
        /// <description>The caller does not have the required permission.</description>
        /// </item>
        /// </list>
        /// </exception>
        /// <exception cref="NoHeaderChunkException">There is no header chunk in a file and that should be treated as error
        /// according to the <see cref="ReadingSettings.NoHeaderChunkPolicy"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidChunkSizeException">Actual header or track chunk's size differs from the one declared
        /// in its header and that should be treated as error according to the <see cref="ReadingSettings.InvalidChunkSizePolicy"/>
        /// of the <paramref name="settings"/>.</exception>
        /// <exception cref="UnknownChunkException">Chunk to be read has unknown ID and that
        /// should be treated as error accordng to the <see cref="ReadingSettings.UnknownChunkIdPolicy"/> of the
        /// <paramref name="settings"/>.</exception>
        /// <exception cref="UnexpectedTrackChunksCountException">Actual track chunks
        /// count differs from the expected one (declared in the file header) and that should be treated as error according to
        /// the <see cref="ReadingSettings.UnexpectedTrackChunksCountPolicy"/> of the specified <paramref name="settings"/>.</exception>
        /// <exception cref="UnknownFileFormatException">The header chunk of the file specifies unknown file format and
        /// that should be treated as error according to the <see cref="ReadingSettings.UnknownFileFormatPolicy"/> of
        /// the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidChannelEventParameterValueException">Value of a channel event's parameter
        /// just read is invalid (is out of [0; 127] range) and that should be treated as error according to the
        /// <see cref="ReadingSettings.InvalidChannelEventParameterValuePolicy"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidMetaEventParameterValueException">Value of a meta event's parameter
        /// just read is invalid and that should be treated as error according to the
        /// <see cref="ReadingSettings.InvalidMetaEventParameterValuePolicy"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="UnknownChannelEventException">Reader has encountered an unknown channel event and that
        /// should be treated as error according to the <see cref="ReadingSettings.UnknownChannelEventPolicy"/> of
        /// the <paramref name="settings"/>.</exception>
        /// <exception cref="NotEnoughBytesException">MIDI file data cannot be read since the reader's underlying stream doesn't
        /// have enough bytes and that should be treated as error according to the <see cref="ReadingSettings.NotEnoughBytesPolicy"/>
        /// of the <paramref name="settings"/>.</exception>
        /// <exception cref="UnexpectedRunningStatusException">Unexpected running status is encountered.</exception>
        /// <exception cref="MissedEndOfTrackEventException">Track chunk doesn't end with <c>End Of Track</c> event and that
        /// should be treated as error accordng to the <see cref="ReadingSettings.MissedEndOfTrackPolicy"/> of
        /// the <paramref name="settings"/>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ReaderSettings.Buffer"/> of <paramref name="settings"/>
        /// is <c>null</c> in case of <see cref="ReaderSettings.BufferingPolicy"/> set to
        /// <see cref="BufferingPolicy.UseCustomBuffer"/>.</exception>
        public static MidiFile Read(Stream stream, ReadingSettings settings = null)
        {
            ThrowIfArgument.IsNull(nameof(stream), stream);

            if (!stream.CanRead)
                throw new ArgumentException("Stream doesn't support reading.", nameof(stream));

            //

            if (settings == null)
                settings = new ReadingSettings();

            if (settings.ReaderSettings == null)
                settings.ReaderSettings = new ReaderSettings();

            settings.PrepareReadingHandlers();

            var useReadingHandlers = settings.UseReadingHandlers;
            var fileReadingHandlers = settings.FileReadingHandlers;
            var trackChunkReadingHandlers = settings.TrackChunkReadingHandlers;

            //

            if (useReadingHandlers)
            {
                foreach (var handler in fileReadingHandlers)
                {
                    handler.OnStartFileReading();
                }
            }

            var file = new MidiFile();

            int? expectedTrackChunksCount = null;
            int actualTrackChunksCount = 0;
            bool headerChunkIsRead = false;

            //

            try
            {
                using (var reader = new MidiReader(stream, settings.ReaderSettings))
                {
                    if (reader.EndReached)
                        throw new ArgumentException("Stream is already read.", nameof(stream));

                    // Read RIFF header

                    long? smfEndPosition = null;

                    var chunkId = reader.ReadString(RiffChunkId.Length);
                    if (chunkId == RiffChunkId)
                    {
                        reader.Position += RmidPreambleSize;
                        var smfSize = reader.ReadDword();
                        smfEndPosition = reader.Position + smfSize;
                    }
                    else
                        reader.Position -= chunkId.Length;

                    // Read SMF

                    while (!reader.EndReached && (smfEndPosition == null || reader.Position < smfEndPosition))
                    {
                        // Read chunk

                        var chunk = ReadChunk(reader, settings, actualTrackChunksCount, expectedTrackChunksCount, trackChunkReadingHandlers);
                        if (chunk == null)
                            continue;

                        // Process header chunk

                        var headerChunk = chunk as HeaderChunk;
                        if (headerChunk != null)
                        {
                            if (!headerChunkIsRead)
                            {
                                expectedTrackChunksCount = headerChunk.TracksNumber;
                                file.TimeDivision = headerChunk.TimeDivision;
                                file._originalFormat = headerChunk.FileFormat;

                                if (useReadingHandlers)
                                {
                                    foreach (var handler in fileReadingHandlers)
                                    {
                                        handler.OnFinishHeaderChunkReading(headerChunk.TimeDivision);
                                    }
                                }
                            }

                            headerChunkIsRead = true;
                            continue;
                        }

                        // Process track chunk

                        if (chunk is TrackChunk)
                            actualTrackChunksCount++;

                        // Add chunk to chunks collection of the file

                        file.Chunks.Add(chunk);
                    }

                    if (expectedTrackChunksCount != null && actualTrackChunksCount != expectedTrackChunksCount)
                        ReactOnUnexpectedTrackChunksCount(settings.UnexpectedTrackChunksCountPolicy, actualTrackChunksCount, expectedTrackChunksCount.Value);
                }

                // Process header chunks count

                if (!headerChunkIsRead)
                {
                    file.TimeDivision = null;

                    if (settings.NoHeaderChunkPolicy == NoHeaderChunkPolicy.Abort)
                        throw new NoHeaderChunkException();
                }
            }
            catch (NotEnoughBytesException ex)
            {
                ReactOnNotEnoughBytes(settings.NotEnoughBytesPolicy, ex);
            }
            catch (EndOfStreamException ex)
            {
                ReactOnNotEnoughBytes(settings.NotEnoughBytesPolicy, ex);
            }

            //

            if (useReadingHandlers)
            {
                foreach (var handler in fileReadingHandlers)
                {
                    handler.OnFinishFileReading(file);
                }
            }

            return file;
        }

        /// <summary>
        /// Writes current <see cref="MidiFile"/> to the stream.
        /// </summary>
        /// <param name="stream">Stream to write file's data to.</param>
        /// <param name="format">Format of the file to be written.</param>
        /// <param name="settings">Settings according to which the file must be written. Specify <c>null</c> to use
        /// default settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> doesn't support writing.</exception>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="format"/> specified an invalid value.</exception>
        /// <exception cref="InvalidOperationException">Time division is <c>null</c>.</exception>
        /// <exception cref="IOException">An I/O error occurred while writing to the stream.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="stream"/> is disposed.</exception>
        /// <exception cref="TooManyTrackChunksException">Count of track chunks presented in the file
        /// exceeds maximum value allowed for MIDI file.</exception>
        public void Write(Stream stream, MidiFileFormat format = MidiFileFormat.MultiTrack, WritingSettings settings = null)
        {
            ThrowIfArgument.IsNull(nameof(stream), stream);
            ThrowIfArgument.IsInvalidEnumValue(nameof(format), format);

            if (TimeDivision == null)
                throw new InvalidOperationException("Time division is null.");

            if (!stream.CanWrite)
                throw new ArgumentException("Stream doesn't support writing.", nameof(stream));

            //

            if (settings == null)
                settings = new WritingSettings();

            if (settings.WriterSettings == null)
                settings.WriterSettings = new WriterSettings();

            using (var writer = new MidiWriter(stream, settings.WriterSettings))
            {
                var chunksConverter = ChunksConverterFactory.GetConverter(format);
                var chunks = chunksConverter.Convert(Chunks);

                if (settings.WriteHeaderChunk)
                {
                    var trackChunksCount = chunks.Count(c => c is TrackChunk);
                    if (trackChunksCount > ushort.MaxValue)
                        throw new TooManyTrackChunksException(trackChunksCount);

                    var headerChunk = new HeaderChunk
                    {
                        FileFormat = (ushort)format,
                        TimeDivision = TimeDivision,
                        TracksNumber = (ushort)trackChunksCount
                    };
                    headerChunk.Write(writer, settings);
                }

                foreach (var chunk in chunks)
                {
                    if (chunk is UnknownChunk && settings.DeleteUnknownChunks)
                        continue;

                    chunk.Write(writer, settings);
                }
            }
        }

        /// <summary>
        /// Clones the current <see cref="MidiFile"/> creating a copy of it.
        /// </summary>
        /// <returns>Copy of the current <see cref="MidiFile"/>.</returns>
        public MidiFile Clone()
        {
            var result = new MidiFile(Chunks.Select(c => c.Clone()))
            {
                TimeDivision = TimeDivision.Clone()
            };
            result._originalFormat = _originalFormat;

            return result;
        }

        /// <summary>
        /// Determines whether two specified <see cref="MidiFile"/> objects have the same content.
        /// </summary>
        /// <param name="midiFile1">The first file to compare, or <c>null</c>.</param>
        /// <param name="midiFile2">The second file to compare, or <c>null</c>.</param>
        /// <returns><c>true</c> if the <paramref name="midiFile1"/> is equal to the <paramref name="midiFile2"/>;
        /// otherwise, <c>false</c>.</returns>
        public static bool Equals(MidiFile midiFile1, MidiFile midiFile2)
        {
            string message;
            return Equals(midiFile1, midiFile2, out message);
        }

        /// <summary>
        /// Determines whether two specified <see cref="MidiFile"/> objects have the same content.
        /// </summary>
        /// <param name="midiFile1">The first file to compare, or <c>null</c>.</param>
        /// <param name="midiFile2">The second file to compare, or <c>null</c>.</param>
        /// <param name="message">Message containing information about what exactly is different in
        /// <paramref name="midiFile1"/> and <paramref name="midiFile2"/>.</param>
        /// <returns><c>true</c> if the <paramref name="midiFile1"/> is equal to the <paramref name="midiFile2"/>;
        /// otherwise, <c>false</c>.</returns>
        public static bool Equals(MidiFile midiFile1, MidiFile midiFile2, out string message)
        {
            return Equals(midiFile1, midiFile2, null, out message);
        }

        /// <summary>
        /// Determines whether two specified <see cref="MidiFile"/> objects have the same content.
        /// </summary>
        /// <param name="midiFile1">The first file to compare, or <c>null</c>.</param>
        /// <param name="midiFile2">The second file to compare, or <c>null</c>.</param>
        /// <param name="settings">Settings according to which files should be compared.</param>
        /// <returns><c>true</c> if the <paramref name="midiFile1"/> is equal to the <paramref name="midiFile2"/>;
        /// otherwise, <c>false</c>.</returns>
        public static bool Equals(MidiFile midiFile1, MidiFile midiFile2, MidiFileEqualityCheckSettings settings)
        {
            string message;
            return Equals(midiFile1, midiFile2, settings, out message);
        }

        /// <summary>
        /// Determines whether two specified <see cref="MidiFile"/> objects have the same content using
        /// the specified comparison settings.
        /// </summary>
        /// <param name="midiFile1">The first file to compare, or <c>null</c>.</param>
        /// <param name="midiFile2">The second file to compare, or <c>null</c>.</param>
        /// <param name="settings">Settings according to which files should be compared.</param>
        /// <param name="message">Message containing information about what exactly is different in
        /// <paramref name="midiFile1"/> and <paramref name="midiFile2"/>.</param>
        /// <returns><c>true</c> if the <paramref name="midiFile1"/> is equal to the <paramref name="midiFile2"/>;
        /// otherwise, <c>false</c>.</returns>
        public static bool Equals(MidiFile midiFile1, MidiFile midiFile2, MidiFileEqualityCheckSettings settings, out string message)
        {
            return MidiFileEquality.Equals(midiFile1, midiFile2, settings ?? new MidiFileEqualityCheckSettings(), out message);
        }

        private static MidiChunk ReadChunk(MidiReader reader, ReadingSettings settings, int actualTrackChunksCount, int? expectedTrackChunksCount, ICollection<ReadingHandler> trackChunkReadingHandlers)
        {
            MidiChunk chunk = null;

            try
            {
                var chunkId = reader.ReadString(MidiChunk.IdLength);
                if (chunkId.Length < MidiChunk.IdLength)
                {
                    switch (settings.NotEnoughBytesPolicy)
                    {
                        case NotEnoughBytesPolicy.Abort:
                            throw new NotEnoughBytesException("Chunk ID cannot be read since the reader's underlying stream doesn't have enough bytes.",
                                                              MidiChunk.IdLength,
                                                              chunkId.Length);
                        case NotEnoughBytesPolicy.Ignore:
                            return null;
                    }
                }

                //

                switch (chunkId)
                {
                    case HeaderChunk.Id:
                        chunk = new HeaderChunk();
                        break;
                    case TrackChunk.Id:
                        if (settings.UseReadingHandlers)
                        {
                            foreach (var handler in trackChunkReadingHandlers)
                            {
                                handler.OnStartTrackChunkReading();
                            }
                        }

                        chunk = new TrackChunk();
                        break;
                    default:
                        chunk = TryCreateChunk(chunkId, settings.CustomChunkTypes);
                        break;
                }

                //

                if (chunk == null)
                {
                    switch (settings.UnknownChunkIdPolicy)
                    {
                        case UnknownChunkIdPolicy.ReadAsUnknownChunk:
                            chunk = new UnknownChunk(chunkId);
                            break;

                        case UnknownChunkIdPolicy.Skip:
                            var size = reader.ReadDword();
                            reader.Position += size;
                            return null;

                        case UnknownChunkIdPolicy.Abort:
                            throw new UnknownChunkException(chunkId);
                    }
                }

                //

                if (chunk is TrackChunk && expectedTrackChunksCount != null && actualTrackChunksCount >= expectedTrackChunksCount)
                {
                    ReactOnUnexpectedTrackChunksCount(settings.UnexpectedTrackChunksCountPolicy, actualTrackChunksCount, expectedTrackChunksCount.Value);

                    switch (settings.ExtraTrackChunkPolicy)
                    {
                        case ExtraTrackChunkPolicy.Read:
                            break;

                        case ExtraTrackChunkPolicy.Skip:
                            var size = reader.ReadDword();
                            reader.Position += size;
                            return null;
                    }
                }

                //

                chunk?.Read(reader, settings);

                if (settings.UseReadingHandlers && chunkId == TrackChunk.Id)
                {
                    foreach (var handler in trackChunkReadingHandlers)
                    {
                        handler.OnFinishTrackChunkReading((TrackChunk)chunk);
                    }
                }
            }
            catch (NotEnoughBytesException ex)
            {
                ReactOnNotEnoughBytes(settings.NotEnoughBytesPolicy, ex);
            }
            catch (EndOfStreamException ex)
            {
                ReactOnNotEnoughBytes(settings.NotEnoughBytesPolicy, ex);
            }

            return chunk;
        }

        /// <summary>
        /// Does nothing if difference between expected track chunks count and the actual one should not
        /// be treated as error; or throws the <see cref="UnexpectedTrackChunksCountException"/> if this
        /// difference is unallowable.
        /// </summary>
        /// <param name="policy">The policy according to which the method should operate.</param>
        /// <param name="actualTrackChunksCount">Actual count of track chunks.</param>
        /// <param name="expectedTrackChunksCount">Expected count of track chunks.</param>
        /// <exception cref="UnexpectedTrackChunksCountException">Difference between expected track chunks
        /// count and the actual one is unallowable due to the <paramref name="policy"/>.</exception>
        private static void ReactOnUnexpectedTrackChunksCount(UnexpectedTrackChunksCountPolicy policy, int actualTrackChunksCount, int expectedTrackChunksCount)
        {
            switch (policy)
            {
                case UnexpectedTrackChunksCountPolicy.Ignore:
                    break;

                case UnexpectedTrackChunksCountPolicy.Abort:
                    throw new UnexpectedTrackChunksCountException(actualTrackChunksCount, expectedTrackChunksCount);
            }
        }

        /// <summary>
        /// Does nothing if lack of bytes in the reader's underlying stream needed to read a value should
        /// not be treated as error; or throws the <see cref="NotEnoughBytesException"/> if this is
        /// unallowable.
        /// </summary>
        /// <param name="policy">The policy according to which the method should operate.</param>
        /// <param name="exception">Initial exception.</param>
        /// <exception cref="NotEnoughBytesException">Lack of bytes in the reader's underlying stream needed to
        /// read a value is unallowable due to the <paramref name="policy"/>.</exception>
        private static void ReactOnNotEnoughBytes(NotEnoughBytesPolicy policy, Exception exception)
        {
            if (policy == NotEnoughBytesPolicy.Abort)
                throw new NotEnoughBytesException("MIDI file cannot be read since the reader's underlying stream doesn't have enough bytes.", exception);
        }

        /// <summary>
        /// Tries to create an instance of a chunk type that has specified ID.
        /// </summary>
        /// <param name="chunkId">ID of the chunk that need to be created.</param>
        /// <param name="chunksTypes">Collection of the chunks types to search for the one with
        /// <paramref name="chunkId"/> ID.</param>
        /// <returns>An instance of the chunk type with the specified ID or <c>null</c> if <paramref name="chunksTypes"/>
        /// doesn't contain chunk type with it.</returns>
        private static MidiChunk TryCreateChunk(string chunkId, ChunkTypesCollection chunksTypes)
        {
            Type type = null;
            return chunksTypes?.TryGetType(chunkId, out type) == true && IsChunkType(type)
                ? (MidiChunk)Activator.CreateInstance(type)
                : null;
        }

        /// <summary>
        /// Checks if a type represents a MIDI-file chunk.
        /// </summary>
        /// <param name="type">Type to check whether it represents a chunk or not.</param>
        /// <returns>True if passed type represents a MIDI-file chunk; <c>false</c> - otherwise.</returns>
        /// <remarks>
        /// Type represents a chunk if it is derived from the <see cref="MidiChunk"/> class and has
        /// parameterless constructor.
        /// </remarks>
        private static bool IsChunkType(Type type)
        {
            return type != null &&
                   type.IsSubclassOf(typeof(MidiChunk)) &&
                   type.GetConstructor(Type.EmptyTypes) != null;
        }

        #endregion
    }
}
