// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;

namespace K9.IO.FileAccessors
{
    public class NFSFileAccessor : IFileAccessor
    {
        private const int Timeout = 10000;
        public enum ProtocolVersion
        {
            v2,
            v3,
            v4
        }

        private readonly NFSClient _client;
        private readonly string _filePath;
        private bool Connected => _client.IsConnected;
        private bool Mounted => _client.IsMounted;

        public NFSFileAccessor(ProtocolVersion version, string address, string share, string filePath)
        {
            // Figure out version
            NFSClient.NFSVersion protocol;
            switch (version)
            {
                case ProtocolVersion.v2:
                    protocol = NFSClient.NFSVersion.v2;
                    break;
                case ProtocolVersion.v4:
                    protocol = NFSClient.NFSVersion.v4;
                    break;
                default: // v3
                    protocol = NFSClient.NFSVersion.v3;
                    break;
            }

            // Create versioned client
            _client = new NFSClient(protocol);
            _client.Connect(IPAddress.Parse(address),0,0, Timeout, Encoding.UTF8, true, false);

            // DEBUG EXPORTED DEVICES
            List<string> devices = _client.GetExportedDevices();
            // foreach(string )
            // foreach (string device in _client.GetExportedDevices())
            // {
            //     Log.Write(device);
            // }
            //
            // _filePath = filePath;
            // if (!Connected)
            // {
            //     return;
            // }
            // _client.MountDevice(share);
        }

        /// <inheritdoc />
        public uint GetBlockSize()
        {
            return (uint)_client._blockSize;
        }

        /// <inheritdoc />
        public int GetReadBufferSize()
        {
            // Client sets the block size when connects based on device response, in our case we respond with 32KB
            return _client._blockSize;
        }

        /// <inheritdoc />
        public int GetWriteBufferSize()
        {
            // Client sets the block size when connects based on device response, in our case we respond with 32KB
            return _client._blockSize;
        }

        /// <inheritdoc />
        public bool ValidConnection()
        {
            return Connected && Mounted;
        }

        /// <inheritdoc />
        public Stream GetReader()
        {
            if (!Connected)
            {
                Log.WriteLine($"Unable to get reader for {_filePath} as NFS is not connected.", Core.LogCategory, Log.LogType.Error);
                return null;
            }
            if (!Mounted)
            {
                Log.WriteLine($"Unable to get reader for {_filePath} as NFS is not mounted.", Core.LogCategory, Log.LogType.Error);
                return null;
            }

            NFSAttributes attributes = _client.GetItemAttributes(_filePath);

            // Allocate that big block of memory
            Stream coalesceStream = new CoalesceStream(attributes.Size);

            // Read the file
            _client.Read(_filePath, ref coalesceStream);

            _client.UnMountDevice();
            _client.Disconnect();

            return coalesceStream;
        }

        /// <inheritdoc />
        public Stream GetWriter()
        {
            throw new System.NotImplementedException();
        }
    }
}