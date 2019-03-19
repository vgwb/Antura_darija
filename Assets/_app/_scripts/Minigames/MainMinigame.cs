﻿using System.Collections;
using System.Collections.Generic;
using Antura.Database;

namespace Antura.Minigames
{
    public class MainMiniGame
    {
        public string MainId;
        public List<MiniGameInfo> variations;

        public string GetIconResourcePath()
        {
            return variations[0].data.GetIconResourcePath();
        }

        public MiniGameCode GetFirstVariationMiniGameCode()
        {
            return variations[0].data.Code;
        }
    }
}