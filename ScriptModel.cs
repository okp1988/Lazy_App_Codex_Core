using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lazy_App_Codex_Core
{
    public class ScriptModel
    {
        public int Duration { get; set; }        // 0 = unlimited, >0 = loop count
        public int Interval_Min { get; set; }    // 秒
        public int Interval_Max { get; set; }    // 秒

        // Config 改为数组 (List) 对应 JSON 中的 []
        public List<StepAction> Config { get; set; } = new List<StepAction>();
    }

    public class StepAction
    {
        public string Act { get; set; } = "";   // e.g. leftclick, leftdrag, rightclick
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int? PosX2 { get; set; } // FOR DRAGGING
        public int? PosY2 { get; set; } // FOR DRAGGING
        public int ScrX { get; set; }
        public int ScrY { get; set; }
        public int? ScrX2 { get; set; } // FOR DRAGGING
        public int? ScrY2 { get; set; } // FOR DRAGGING
        public int RandX { get; set; }
        public int RandY { get; set; }
        public int Sleep_Min { get; set; }
        public int Sleep_Max { get; set; }
        public string Offset { get; set; } = ""; // optional x or y; applies on left click only
    }
}
