using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PowerThreadPool_Net20.Helpers
{
    ///// <summary>
    ///// Allows reusable uint Ids
    ///// 允许可重用的 uint id
    ///// </summary>
    //public class IdQueue
    //{
    //    private int m_currentId;
    //    private readonly LockFreeQueue<uint> m_freeIds;

    //    public IdQueue() {
    //        m_freeIds = new LockFreeQueue<uint>();
    //    }

    //    public uint NextId() {
    //        if (m_freeIds.Count > 0) {
    //            return m_freeIds.Dequeue();
    //        }
    //        else {
    //            return (uint)Interlocked.Increment(ref m_currentId);
    //        }
    //    }

    //    public void RecycleId(uint id) {
    //        m_freeIds.Enqueue(id);
    //    }

    //    public void Load(List<uint> usedIds) {
    //        uint maxId = 0;

    //        for (int i = 0; i < usedIds.Count; i++) {
    //            if (usedIds[i] > maxId)
    //                maxId = usedIds[i];
    //        }

    //        for (uint i = 0; i <= maxId; i++) {
    //            if (!usedIds.Contains(i)) {
    //                RecycleId(i);
    //            }
    //        }

    //        m_currentId = (int)maxId;
    //    }
    //}
    /// <summary>
    /// 可复用数字id管理器
    /// </summary>
    public class IdManager
    {
        // The next ID to try
        private int m_nextIdToTry = 0;

        // Stores whether each ID is free or not. Additionally, the object is also used as a lock for the IdManager.
        private List<bool> m_freeIds = new List<bool>();

        public int GetId() {
            lock (m_freeIds) {
                int availableId = m_nextIdToTry;
                while (availableId < m_freeIds.Count) {
                    if (m_freeIds[availableId]) { break; }
                    availableId++;
                }

                if (availableId == m_freeIds.Count) {
                    m_freeIds.Add(false);
                }
                else {
                    m_freeIds[availableId] = false;
                }

                m_nextIdToTry = availableId + 1;

                return availableId;
            }
        }

        // Return an ID to the pool
        public void ReturnId(int id) {
            lock (m_freeIds) {
                m_freeIds[id] = true;
                if (id < m_nextIdToTry) m_nextIdToTry = id;
            }
        }
        public void Load(List<int> usedIds) {
            int maxId = 0;

            foreach (var id in usedIds) {
                if (id > maxId)
                    maxId = id;
            }

            m_freeIds.Clear();
            for (int i = 0; i <= maxId; i++) {
                m_freeIds.Add(true);
            }

            foreach (var id in usedIds) {
                m_freeIds[id] = false;
            }

            m_nextIdToTry = 0;
            while (m_nextIdToTry < m_freeIds.Count && !m_freeIds[m_nextIdToTry]) {
                m_nextIdToTry++;
            }
        }
    }
}
