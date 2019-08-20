using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using System.Reactive.Subjects;

namespace Plugin.BluetoothLE
{
    public partial class L2CapChannel : AbstractChannel
    {
        public L2CapChannel(CBL2CapChannel channel)
        {
            InputStream = new BleInputStream(channel.InputStream);
            OutputStream = new BleOutputStream(channel.OutputStream);
            Psm = channel.Psm;
            PeerUuid = channel.Peer.Identifier.ToGuid();
        }

        public override Guid PeerUuid { get; }
        public override int Psm { get; }
        public override IInputStream InputStream { get; }
        public override IOutputStream OutputStream { get; }
    }


    internal class BleInputStream : NSObject, IInputStream
    {
        private NSInputStream _inputStream;
        private Subject<IStreamData> _streamSubject;
        private static int MAX_READ_BYTES_SIZE = 1024;

        public BleInputStream(NSInputStream inputStream)
        {
            _inputStream = inputStream;
            _streamSubject = new Subject<IStreamData>();
            _inputStream.WeakDelegate = this;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentException("Offset must be greater than or equal to 0");
            }

            if (offset == 0)
            {
                return (int) _inputStream.Read(buffer, (nuint)count);
            }

            return (int) _inputStream.Read(buffer, offset, (nuint)count);
        }

        public IObservable<IStreamData> Read()
        {
            return _streamSubject;
        }

        public bool CanRead => true;
        public bool CanWrite => false;
        public bool IsOpen { get; private set; }

        public bool IsDataAvailable => _inputStream.HasBytesAvailable();
        public void Open()
        {
            _inputStream?.Open();
        }

        public void Close()
        {
            _inputStream?.Close();
            IsOpen = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inputStream?.Close();
                _inputStream?.Dispose();
                _inputStream = null;
                _streamSubject?.Dispose();
                _streamSubject = null;
                IsOpen = false;
            }
        }

        //bind to the Objective-C selector mapView:didSelectAnnotationView:
        [Export("stream:handleEvent:")]
        public void HandleStreamEvent (NSStream stream, NSStreamEvent streamEvent)
        {
            switch (streamEvent)
            {
                case NSStreamEvent.ErrorOccurred:
                    _streamSubject.OnError(new StreamException("Failed to read stream"));
                    break;
                case NSStreamEvent.EndEncountered:
                    _streamSubject.OnNext(new StreamData(null, -1));
                    _streamSubject.OnCompleted();
                    break; 
                case NSStreamEvent.OpenCompleted:
                    IsOpen = true;
                    break;
                case NSStreamEvent.HasBytesAvailable:
                    var buffer = new byte[MAX_READ_BYTES_SIZE];
                    var len = (stream as NSInputStream).Read(buffer, (nuint) MAX_READ_BYTES_SIZE);
                    if (len > 0)
                    {
                        _streamSubject.OnNext(new StreamData(buffer, (int) len));
                    }
                    break;
            }
        }
    }

    internal class BleOutputStream : NSObject, IOutputStream
    {
        private NSOutputStream _outputStream;
        private readonly List<byte> _buffer;

        public BleOutputStream(NSOutputStream outputStream)
        {
            _outputStream = outputStream;
            _outputStream.WeakDelegate = this;
            _buffer = new List<byte>();
        }

        public void Flush()
        {
            Write(_buffer.ToArray(),0,_buffer.Count);
            _buffer.Clear();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentException("Offset must be greater than or equal to 0");
            }
            
            var bytesSent = 0;
            var bytesToBeSent = new byte[buffer.Length - offset];
            var length = count;
            buffer.CopyTo(bytesToBeSent, offset);

            Console.WriteLine($"Start writing to stream - length: {buffer.LongLength}"); 
            
            while (length > 0)
            {       
                if (_outputStream.HasSpaceAvailable()) 
                {   
                    var bytesWritten = _outputStream.Write(buffer, (uint)count);
                    if (bytesWritten == -1)
                    {
                        Console.WriteLine($"{buffer.LongLength} bytes failed to write to stream");
                        break;
                    }

                    if (bytesWritten > 0)
                    {
                        Console.WriteLine($"Bytes written to stream: {bytesWritten}");
                        length -= (int) bytesWritten;
                        if (0 == length)
                            break;

                        var temp = new List<byte>();
                        for (var i = bytesWritten; i < bytesToBeSent.Length; i++)
                        {
                            temp.Add(bytesToBeSent[i]);
                        }
                        bytesToBeSent = temp.ToArray();
                    }
                }
                else 
                {
                    Console.WriteLine("No more space left in output stream");
                }
            }
            
        }

        public void Write(byte value)
        {
            _buffer.Add(value);
        }

        public bool CanRead => false;
        public bool CanWrite => true;
        public bool IsOpen { get; private set; }

        public bool IsDataAvailable => _outputStream.HasSpaceAvailable();
        public void Open()
        {
            _outputStream?.Open();
        }

        public void Close()
        {
            _outputStream?.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outputStream?.Close();
                _outputStream?.Dispose();
                _outputStream = null;
            }
        }

        //bind to the Objective-C selector mapView:didSelectAnnotationView:
        [Export("stream:handleEvent:")]
        public void HandleStreamEvent (NSStream stream, NSStreamEvent streamEvent)
        {
            switch (streamEvent)
            {
                case NSStreamEvent.ErrorOccurred:
                     break;
                case NSStreamEvent.EndEncountered:
                     break; 
                case NSStreamEvent.OpenCompleted:
                    IsOpen = true;
                    break;
                case NSStreamEvent.HasSpaceAvailable:
                    break;
            }
        }
    }
    
    
}