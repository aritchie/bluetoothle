using System;
using System.Windows.Input;


namespace Samples.ViewModels.TestCases
{
    public interface ITestCaseViewModel
    {
        // TODO: when action occurs IObservable<string> WhenOutput();
        string Name { get; }
        ICommand Run { get; }
    }
}
