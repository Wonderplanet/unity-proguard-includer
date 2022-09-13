using System.Collections.Generic;
using UnityEngine;

namespace Wonderplanet.AndroidTools.ProguardIncluder
{
    public class ProguardFileList : ScriptableObject
    {
        public List<TextAsset> ProguardFilesToInclude = new List<TextAsset>();
    }
}
