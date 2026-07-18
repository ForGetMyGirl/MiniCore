using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MiniCore.Model
{

    public interface IPresenter
    {
        void BindView(AUIBase uiBase);

        void UnbindView();
    }

}
