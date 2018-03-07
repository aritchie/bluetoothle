using System;


namespace Plugin.BluetoothLE.Internals
{
    public class DefaultBleMediator : IBleMediator
    {
        /// <summary>
        /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public bool ExecuteOnMainThread { get; set; } = true;


        public TimeSpan? OperationPause { get; set; } = TimeSpan.FromMilliseconds(100);
        public bool SynchronizeActions { get; set; } = true;
        //public bool ShouldWaitForCompletion { get; set; }


        public IObservable<T> Invoke<T>(IDevice device, Action triggerAction, IObservable<T> observable)
        {
            throw new NotImplementedException();
        }
    }
}
/*

        public static bool IsMainThreadSuggested =>
            Build.VERSION.SdkInt < BuildVersionCodes.Kitkat ||
            Build.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase);


        public static bool PerformActionsOnMainThread { get; set; } = IsMainThreadSuggested;


         static TimeSpan? opPause;

        /// <summary>
        /// Time span to pause android operations
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static TimeSpan? OperationPause
        {
            get
            {
                if (opPause != null)
                    return opPause;

                if (Build.VERSION.SdkInt < BuildVersionCodes.N)
                    return TimeSpan.FromMilliseconds(100);

                return null;
            }
            set => opPause = value;
        }
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
