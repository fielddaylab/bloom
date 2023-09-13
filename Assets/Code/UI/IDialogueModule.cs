using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.UI
{
    public interface IDialogueModule
    {
        public abstract void Activate(bool allowReactivate);

        public abstract void Deactivate();

        public abstract bool UsedBy(string charName);
    }
}