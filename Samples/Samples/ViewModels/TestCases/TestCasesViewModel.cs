using System;
using System.Collections.Generic;
using Autofac;


namespace Samples.ViewModels.TestCases
{
    public class TestCasesViewModel : AbstractViewModel
    {
        public TestCasesViewModel(ILifetimeScope scope)
        {
            this.TestCases = new List<ITestCaseViewModel>
            {
                scope.Resolve<Test1ViewModel>()
            };
        }


        public IList<ITestCaseViewModel> TestCases { get; }
    }
}
