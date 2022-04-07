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

            _filePath = filePath;

            if (!Connected)
            {
                Log.WriteLine($"Unable to connect to NFS at {address}.", Core.LogCategory, Log.LogType.Error);

                return;
            }

            foreach (string device in _client.GetExportedDevices())
            {
                string[] split = device.Split('/', 3);
                if (split[2] == share)
                {
                    _client.MountDevice(device);
                    break;
                }
            }

            if (!_client.IsMounted)
            {
                Log.WriteLine($"Unable to get reader for {_filePath} as NFS is not mounted.", Core.LogCategory,
                    Log.LogType.Error);
            }
        }

        ~NFSFileAccessor()
        {
            if (_client == null)
            {
                return;
            }

            if (_client.IsMounted)
            {
                _client.UnMountDevice();
            }

            if (_client.IsConnected)
            {
                _client.Disconnect();
            }
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
            if (!ValidConnection())
            {
                return null;
            }
            if (!_client.FileExists(_filePath))
            {
                Log.WriteLine($"Unable to find {_filePath} on NFS.", Core.LogCategory,
                    Log.LogType.Error);
                return null;
            }

            NFSAttributes attributes = _client.GetItemAttributes(_filePath);

            // Allocate that big block of memory
            Stream coalesceStream = new CoalesceStream(attributes.Size);

            // Read the file
            _client.Read(_filePath, ref coalesceStream);

            return coalesceStream;
        }

        /// <inheritdoc />
        public Stream GetWriter()
        {
            throw new System.NotImplementedException();
        }
    }
}