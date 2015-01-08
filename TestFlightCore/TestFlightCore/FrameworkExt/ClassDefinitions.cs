using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace KSPAlternateResourcePanel
{
    /// <summary>
    /// A Queue structure that has a numerical limit.
    /// When the limit is reached the oldest entry is discarded
    /// </summary>
    internal class LimitedQueue<T> : Queue<T>
    {
        private Int32 limit = -1;

        public Int32 Limit
        {
            get { return limit; }
            set
            {
                limit = value;
                if (limit > 0)
                {
                    //If more items in the queue than the limit then dequeue stuff
                    while (this.Count > this.limit)
                    {
                        this.Dequeue();
                    }
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="limit">How many items the queue can hold</param>
        public LimitedQueue(Int32 limit)
            : base(limit)
        {
            this.Limit = limit;
        }

        /// <summary>
        /// Add a new item to the queue. If this would exceed the limit then the oldest item is discarded
        /// </summary>
        /// <param name="item"></param>
        public new void Enqueue(T item)
        {
            if (limit > 0)
            {
                //Trim the queue down so there is room to add the next one
                while (this.Count >= this.Limit)
                {
                    this.Dequeue();
                }
            }
            base.Enqueue(item);
        }
    }
}