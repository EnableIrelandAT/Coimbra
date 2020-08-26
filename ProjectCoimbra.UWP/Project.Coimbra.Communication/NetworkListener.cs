// Licensed under the MIT License.

namespace Coimbra.Communication
{
    using System;
    using System.Threading.Tasks;
    using Coimbra.Model;
    using Newtonsoft.Json;
    using Windows.Networking.Sockets;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// Network Listener.
    /// </summary>
    public static class NetworkListener
    {
        private const string ServiceNameForListener = "123123";
        private static StreamSocketListener listener;

        /// <summary>
        /// MidiFileReceived.
        /// </summary>
        /// <param name="eventArgs">eventArgs.</param>
        public delegate void MidiFileReceived(MidiFileReceivedEventArguments eventArgs);

        /// <summary>
        /// PlayerInfoReceived.
        /// </summary>
        /// <param name="eventArgs">eventArgs.</param>
        public delegate void PlayerInfoReceived(PlayerInfoReceivedEventArguments eventArgs);

        /// <summary>
        /// PlayerReadyInfoReceived.
        /// </summary>
        /// <param name="eventArgs">eventArgs.</param>
        public delegate void PlayerReadyInfoReceived(PlayerInfoReceivedEventArguments eventArgs);

        /// <summary>
        /// PlayerInstrumentInfoReceived.
        /// </summary>
        /// <param name="eventArgs">eventArgs.</param>
        public delegate void PlayerInstrumentInfoReceived(PlayerInfoReceivedEventArguments eventArgs);

        /// <summary>
        /// StartTimeInfoReceived.
        /// </summary>
        /// <param name="eventArgs">eventArgs.</param>
        public delegate void StartTimeInfoReceived(StartTimeInfoReceivedEventArgs eventArgs);

        /// <summary>
        /// OnMidiFileReceived.
        /// </summary>
        public static event MidiFileReceived OnMidiFileReceived;

        /// <summary>
        /// OnPlayerInfoReceived.
        /// </summary>
        public static event PlayerInfoReceived OnPlayerInfoReceived;

        /// <summary>
        /// OnPlayerReadyInfoReceived.
        /// </summary>
        public static event PlayerReadyInfoReceived OnPlayerReadyInfoReceived;

        /// <summary>
        /// OnPlayerInstrumentInfoReceived.
        /// </summary>
        public static event PlayerInstrumentInfoReceived OnPlayerInstrumentInfoReceived;

        /// <summary>
        /// OnStartTimeInfoReceived.
        /// </summary>
        public static event StartTimeInfoReceived OnStartTimeInfoReceived;

        /// <summary>
        /// This is the click handler for the 'StartListener' button.
        /// </summary>
        public static async void StartListener()
        {
            listener = new StreamSocketListener();
            listener.ConnectionReceived += OnConnection;

            // If necessary, tweak the listener's control options before carrying out the bind operation.
            // These options will be automatically applied to the connected StreamSockets resulting from
            // incoming connections (i.e., those passed as arguments to the ConnectionReceived event handler).
            // Refer to the StreamSocketListenerControl class' MSDN documentation for the full list of control options.
            listener.Control.KeepAlive = false;

            // Start listen operation.
            var hostName = NetworkDataSender.GetAdapter();
            if (hostName != null)
            {
                await listener.BindEndpointAsync(hostName, ServiceNameForListener);
            }
        }

        /// <summary>
        /// Invoked once a connection is accepted by StreamSocketListener.
        /// </summary>
        /// <param name="sender">The listener that accepted the connection.</param>
        /// <param name="args">Parameters associated with the accepted connection.</param>
        private static async void OnConnection(
            StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var reader = new DataReader(args.Socket.InputStream);
            try
            {
                while (true)
                {
                    // First int is the DataType enum
                    _ = await reader.LoadAsync(sizeof(int));
                    switch ((DataType)reader.ReadInt32())
                    {
                        case DataType.Player:
                        case DataType.PlayerReady:
                        case DataType.PlayerInstrument:
                            _ = await ReadPlayerInfoAsync(reader).ConfigureAwait(true);
                            break;

                        case DataType.StartTime:
                            _ = await ReadStartTimeInfoAsync(reader).ConfigureAwait(true);
                            break;

                        case DataType.MidiFile:
                            _ = await ReadMidiFileAsync(reader).ConfigureAwait(true);
                            break;
                    }
                }
            }
            catch (Exception exception) when (SocketError.GetStatus(exception.HResult) != SocketErrorStatus.Unknown)
            {
            }
            finally
            {
                reader.Dispose();
            }
        }

        private static async Task<string> ReadStringFromStreamAsync(DataReader reader)
        {
            // Read first 4 bytes(length of the subsequent string).
            var sizeFieldCount = await reader.LoadAsync(sizeof(uint));
            if (sizeFieldCount != sizeof(uint))
            {
                // The underlying socket was closed before we were able to read the whole data.
                return null;
            }

            // Read the string.
            var stringLength = reader.ReadUInt32();
            var actualStringLength = await reader.LoadAsync(stringLength);
            if (stringLength != actualStringLength)
            {
                // The underlying socket was closed before we were able to read the whole data.
                return null;
            }

            return reader.ReadString(actualStringLength);
        }

        private static async Task<byte[]> ReadByteArrayAsync(DataReader reader)
        {
            _ = await reader.LoadAsync(sizeof(long));

            // Read first 4 bytes (length of the subsequent string).
            var sizeFieldCount = reader.ReadInt64();

            _ = await reader.LoadAsync((uint)sizeFieldCount);

            // Read the string.
            var bytes = new byte[sizeFieldCount];
            reader.ReadBytes(bytes);

            return bytes;
        }

        private static async Task<Player> ReadPlayerInfoAsync(DataReader reader)
        {
            var json = await ReadStringFromStreamAsync(reader).ConfigureAwait(true);
            var player = JsonConvert.DeserializeObject<Player>(json);

            MultiPlayerData.OtherPlayers[player.NickName] = player;

            var eventArgs = new PlayerInfoReceivedEventArguments(player);

            // Tracking number is available, raise the event.
            if (player.Instrument >= 0)
            {
                OnPlayerInstrumentInfoReceived?.Invoke(eventArgs);
            }
            else if (player.ReadyToStart)
            {
                OnPlayerReadyInfoReceived?.Invoke(eventArgs);
            }
            else
            {
                OnPlayerInfoReceived?.Invoke(eventArgs);
            }

            return player;
        }

        private static async Task<DateTime> ReadStartTimeInfoAsync(DataReader reader)
        {
            var json = await ReadStringFromStreamAsync(reader).ConfigureAwait(true);
            var startTime = JsonConvert.DeserializeObject<DateTime>(json);

            var eventArgs = new StartTimeInfoReceivedEventArgs(startTime);
            OnStartTimeInfoReceived?.Invoke(eventArgs);
            MultiPlayerData.StartTime = startTime;

            return startTime;
        }

        private static async Task<StorageFile> ReadMidiFileAsync(DataReader reader)
        {
            var fileName = await ReadStringFromStreamAsync(reader).ConfigureAwait(true);
            var midiFileBytes = await ReadByteArrayAsync(reader).ConfigureAwait(true);

            var storageFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(sampleFile, midiFileBytes);

            var eventArgs = new MidiFileReceivedEventArguments(sampleFile.Path);

            // Tracking number is available, raise the event.
            OnMidiFileReceived?.Invoke(eventArgs);

            return sampleFile;
        }
    }
}
