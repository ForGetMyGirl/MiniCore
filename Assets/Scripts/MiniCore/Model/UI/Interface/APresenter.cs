using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MiniCore.Model
{

    public abstract class APresenter<T> : IPresenter where T : AUIBase
    {
        public T View { get; set; }

        public virtual void BindView(AUIBase uiBase)
        {
            View = uiBase as T;
            OnBind();
        }

        public virtual void UnbindView()
        {
            View = null;
        }

        protected abstract void OnBind();
    }

}