using System;

namespace Plugin.BluetoothLE
{
    public interface IChannel : IDisposable
    {
        Guid PeerUuid { get; }
        int Psm { get; }
        IInputStream InputStream { get; }
        IOutputStream OutputStream { get; }

    }

    public interface IStream : IDisposable
    {
        bool IsDataAvailable { get; }
        void Open();
        void Close();
        bool CanRead { get; }
        bool CanWrite { get; }

        bool IsOpen { get; }
    }

    public interface IStreamData
    {
        int Length { get; }
        byte[] Data { get; }
    }

    public class StreamData : IStreamData
    {
        public StreamData(byte[] data, int length)
        {
            Length = length;
            Data = data;
        }
        
        public int Length { get; }
        public byte[] Data { get; }
    }
    
    public interface IInputStream : IStream
    {
        IObservable<IStreamData> Read();
     }


    public interface IOutputStream : IStream
    {
       void Flush();
 //      long Seek(long offset, SeekOrigin origin);
  //     void SetLength(long value);
       void Write(byte[] buffer, int offset, int count);
       void Write(byte value);

       //void WriteByte(byte value);

       //Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}