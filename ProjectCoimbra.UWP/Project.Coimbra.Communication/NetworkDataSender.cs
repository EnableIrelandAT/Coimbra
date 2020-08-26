// Licensed under the MIT License.

namespace Coimbra.Communication
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Coimbra.Model;
    using Newtonsoft.Json;
    using Windows.Networking;
    using Windows.Networking.Connectivity;
    using Windows.Networking.Sockets;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// Network Data Sender.
    /// </summary>
    public static class NetworkDataSender
    {
        private const string ServiceNameForListener = "123123";

        private static readonly List<StreamSocket> Sockets = new List<StreamSocket>();
        private static readonly List<DataWriter> Writers = new List<DataWriter>();

        /// <summary>
        /// NoIpAddressFound.
        /// </summary>
        public delegate void NoIpAddressFound();

        /// <summary>
        /// OnNoIpAddressFound.
        /// </summary>
        public static event NoIpAddressFound OnNoIpAddressFound;

        /// <summary>
        /// ConnectToAllServersAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task ConnectToAllServersAsync()
        {
            var addressLastPartValues = Enumerable.Range(0, 256);

            var socketCollection = new ConcurrentBag<StreamSocket>();

            var socketTasks = addressLastPartValues.Select(async (addressLastPart) =>
            {
                var socket = await ConnectToTheServerAsync(GetNetworkIdentifier() + addressLastPart.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(true);
                if (socket != null)
                {
                    socketCollection.Add(socket);
                }

                return true;
            });

            _ = await Task.WhenAll(socketTasks).ConfigureAwait(false);

            foreach (var socket in socketCollection)
            {
                Sockets.Add(socket);
            }

            if (Sockets.Count == 0)
            {
                OnNoIpAddressFound?.Invoke();
            }
        }

        /// <summary>
        /// Populates the NetworkAdapter list.
        /// </summary>
        /// <returns>Host Name.</returns>
        public static HostName GetAdapter()
        {
            foreach (var localHostInfo in NetworkInformation.GetHostNames())
            {
                if (localHostInfo.IPInformation != null && localHostInfo.Type == HostNameType.Ipv4 && IPAddress.TryParse(localHostInfo.CanonicalName, out _) && localHostInfo.CanonicalName.StartsWith(GetNetworkIdentifier(), StringComparison.Ordinal))
                {
                    return localHostInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// SendTextAsync.
        /// </summary>
        /// <param name="stringToSend">stringToSend.</param>
        /// <param name="type">type.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SendTextAsync(string stringToSend, DataType type)
        {
            foreach (var writer in Writers)
            {
                // Write first the length of the string as UINT32 value followed up by the string.
                // Writing data to the writer will just store data in memory.
                writer.WriteInt32((int)type);
                writer.WriteUInt32(writer.MeasureString(stringToSend));
                _ = writer.WriteString(stringToSend);

                // Write the locally buffered data to the network.
                _ = await writer.StoreAsync();
            }
        }

        /// <summary>
        /// SendBytes.
        /// </summary>
        /// <param name="bytesToSend">bytesToSend.</param>
        /// <param name="type">type.</param>
        /// <param name="fileName">fileName.</param>
        public static async void SendBytes(byte[] bytesToSend, DataType type, string fileName = null)
        {
            foreach (var writer in Writers)
            {
                writer.WriteInt32((int)type);

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    writer.WriteUInt32(writer.MeasureString(fileName));
                    _ = writer.WriteString(fileName);
                }

                // Write first the length of the string as UINT32 value followed up by the string.
                // Writing data to the writer will just store data in memory.
                writer.WriteInt64(bytesToSend.LongLength);
                writer.WriteBytes(bytesToSend);

                // Write the locally buffered data to the network.
                _ = await writer.StoreAsync();
            }
        }

        /// <summary>
        /// SendSelectedSong.
        /// </summary>
        /// <param name="midiFile">midiFile.</param>
        /// <param name="fileName">fileName.</param>
        public static async void SendSelectedSong(StorageFile midiFile, string fileName)
        {
            UserData.IsMultiplayerConductor = true;

            var fileStream = await midiFile.OpenStreamForReadAsync().ConfigureAwait(true);
            var bytes = new byte[(int)fileStream.Length];
            _ = fileStream.Read(bytes, 0, (int)fileStream.Length);
            SendBytes(bytes, DataType.MidiFile, fileName);
        }

        /// <summary>
        /// SendPlayerInfo.
        /// </summary>
        public static async void SendPlayerInfo()
        {
            var json = JsonConvert.SerializeObject(new Player { NickName = UserData.NickName });
            if (!string.IsNullOrWhiteSpace(json))
            {
                await SendTextAsync(json, DataType.Player).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// SendPlayerReadyInfo.
        /// </summary>
        public static async void SendPlayerReadyInfo()
        {
            var json = JsonConvert.SerializeObject(new Player { NickName = UserData.NickName, ReadyToStart = true });
            if (!string.IsNullOrWhiteSpace(json))
            {
                await SendTextAsync(json, DataType.PlayerReady).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// SendPlayerInstrumentInfo.
        /// </summary>
        /// <param name="instrument">instrument.</param>
        public static async void SendPlayerInstrumentInfo(int instrument)
        {
            var json = JsonConvert.SerializeObject(new Player { NickName = UserData.NickName, ReadyToStart = true, Instrument = instrument });
            if (!string.IsNullOrWhiteSpace(json))
            {
                await SendTextAsync(json, DataType.PlayerReady).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// SendStartTimeInfo.
        /// </summary>
        /// <param name="startTime">startTime.</param>
        public static async void SendStartTimeInfo(DateTime startTime)
        {
            var json = JsonConvert.SerializeObject(startTime);
            if (!string.IsNullOrWhiteSpace(json))
            {
                await SendTextAsync(json, DataType.StartTime).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// ConnectToTheServerAsync.
        /// </summary>
        /// <param name="playerIpAddress">playerIpAddress.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task<StreamSocket> ConnectToTheServerAsync(string playerIpAddress)
        {
            var socket = new StreamSocket();

            // If necessary, tweak the socket's control options before carrying out the connect operation.
            // Refer to the StreamSocketControl class' MSDN documentation for the full list of control options.
            socket.Control.KeepAlive = false;

            // Save the socket, so subsequent steps can use it.
            try
            {
                // Connect to the server (by default, the listener we created in the previous step).
                var hostName = new HostName(playerIpAddress);

                #pragma warning disable CA1508 // Avoid dead conditional code
                using (var cts = new CancellationTokenSource())
                #pragma warning restore CA1508 // Avoid dead conditional code
                {
                    cts.CancelAfter(2000); // cancel after 2 seconds

                    await socket.ConnectAsync(hostName, ServiceNameForListener)
                        .AsTask(cts.Token).ConfigureAwait(false);
                }

                Writers.Add(new DataWriter(socket.OutputStream));

                return socket;
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                return null;
            }
            catch (Exception exception) when (Windows.Networking.Sockets.SocketError.GetStatus(exception.HResult) != SocketErrorStatus.Unknown)
            {
                socket.Dispose();
                return null;
            }
            catch (FileNotFoundException)
            {
                socket.Dispose();
                return null;
            }
        }

        private static string GetNetworkIdentifier(HostNameType hostNameType = HostNameType.Ipv4)
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null)
            {
                return null;
            }

            var hostname =
                NetworkInformation.GetHostNames()
                    .FirstOrDefault(
                        hn =>
                            hn.Type == hostNameType
                            && hn.IPInformation?.NetworkAdapter != null
                            && hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId);

            // the ip address
            return hostname?.CanonicalName.Substring(0, hostname.CanonicalName.LastIndexOf('.') + 1);
        }
    }
}
