using System;
using System.Collections.Generic;

namespace ScratchDL.GUI
{
    public class UILock : IDisposable
    {
        public readonly Guid ID = Guid.NewGuid();

        readonly HashSet<Guid> _sourceSet;

        public UILock(HashSet<Guid> sourceSet)
        {
            _sourceSet = sourceSet;
            sourceSet.Add(ID);
        }

        public void Dispose()
        {
            _sourceSet.Remove(ID);
        }
    }
}
