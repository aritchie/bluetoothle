using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ReactiveUI;


namespace Samples.ViewModels
{
    public abstract class AbstractViewModel : ReactiveObject, IViewModel
    {
        public virtual void Init(object args)
        {
        }


        public virtual void OnActivate()
        {
        }


        public virtual void OnDeactivate()
        {
        }
    }
}
