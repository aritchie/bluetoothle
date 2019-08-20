using System;
using System.IO;
using System.Reactive.Linq;

namespace Plugin.BluetoothLE
{
    public abstract class AbstractChannel : IChannel
    {
        public abstract Guid PeerUuid { get; }
        public abstract int Psm { get; }
        public abstract IInputStream InputStream { get; }
        public abstract IOutputStream OutputStream { get; }
        public virtual void Dispose()
        {
            InputStream?.Dispose();
            OutputStream?.Dispose();
        }
    }

    public class NullChannel : AbstractChannel
    {
        public override Guid PeerUuid => Guid.Empty;
        public override int Psm => 0x25;
        public override IInputStream InputStream => new NullInputOutputStream();
        public override IOutputStream OutputStream =>  new NullInputOutputStream();
      
    }


    internal class NullInputOutputStream : IInputStream, IOutputStream
    {
        public void Dispose()
        {
           
        }

        public bool IsDataAvailable => false;
        public void Open()
        {
            
        }

        public void Close()
        {
         
        }

        public bool CanRead => false;
        public bool CanWrite => false;
        public bool IsOpen => false;

        public int Read(byte[] buffer, int offset, int count) => 0;
        public IObservable<IStreamData> Read()
        {
            return Observable.Return(new StreamData(null, -1));
        }

        public void Flush()
        {
           
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            
        }

        public void Write(byte value)
        {
          
        }
    }

    public class StreamException : Exception
    {
        public StreamException(string message) : base(message)
        {
            
        }
        
        public StreamException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}