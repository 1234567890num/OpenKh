﻿using OpenKh.Game.Infrastructure;

namespace OpenKh.Game.Menu
{
    public interface IMenuManager
    {
        AnimatedSequenceFactory SequenceFactory { get; }
        InputManager InputManager { get; }

        void PushSubMenuDescription(ushort messageId);
        void PopSubMenuDescription();
        void SetElementDescription(ushort messageId);
    }
}
