using System;


namespace Plugin.BluetoothLE.Internals
{
    public class DefaultBleMediator : IBleInvoker
    {
        public bool ExecuteOnMainThread { get; set; }
        public TimeSpan? OperationPause { get; set; }
        public bool SynchronizeActions { get; set; }
        public bool ShouldWaitForCompletion { get; set; }


        public IObservable<T> Invoke<T>(Action triggerAction, IObservable<T> observable)
        {
            throw new NotImplementedException();
        }
    }
}
/*
IDisposable sub = null;
                var pastGate = false;
                var cancel = false;
                Log.Debug("Device", "Lock - at the gate");

                this.reset.WaitOne();

                if (cancel)
                {
                    Log.Debug("Device", "Lock - past the gate, but was cancelled");
                }
                else
                {
                    pastGate = true;
                    Log.Debug("Device", "Lock - past the gate");

                    if (CrossBleAdapter.AndroidOperationPause != null)
                        System.Threading.Thread.Sleep(CrossBleAdapter.AndroidOperationPause.Value);

                    sub = inner.Subscribe(
                        ob.OnNext,
                        ex =>
                        {
                            Log.Debug("Device", "Task errored - releasing lock");
                            pastGate = false;
                            this.reset.Set();
                            ob.OnError(ex);
                        },
                        () =>
                        {
                            Log.Debug("Device", "Task completed - releasing lock");
                            pastGate = false;
                            this.reset.Set();
                            ob.OnCompleted();
                        }
                    );
                }

                return () =>
                {
                    cancel = true;
                    sub?.Dispose();

                    if (pastGate)
                    {
                        Log.Debug("Device", "Cleanup releasing lock");
                        reset.Set();
                    }
                };
 */
