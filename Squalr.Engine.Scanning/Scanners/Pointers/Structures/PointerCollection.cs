﻿namespace Squalr.Engine.Scanning.Scanners.Pointers.Structures
{
    using Squalr.Engine.DataTypes;
    using Squalr.Engine.Memory;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A class to contain the discovered pointers from a pointer scan.
    /// </summary>
    public class PointerCollection : IEnumerable<Pointer>
    {
        /// <summary>
        /// The number of discovered pointers.
        /// </summary>
        private UInt64 count;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointerCollection" /> class.
        /// </summary>
        public PointerCollection()
        {
            this.PointerRoots = new ConcurrentBag<PointerRoot>();
            this.count = 0;
        }

        /// <summary>
        /// Gets the discovered pointer roots.
        /// </summary>
        public ConcurrentBag<PointerRoot> PointerRoots { get; private set; }

        /// <summary>
        /// Gets the number of discovered pointers.
        /// </summary>
        public UInt64 Count
        {
            get
            {
                return this.count;
            }
        }

        /// <summary>
        /// Gets the pointers between the specified indicies.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>The pointers between the specified indicies.</returns>
        public IEnumerable<Pointer> GetPointers(UInt64 startIndex, UInt64 endIndex)
        {
            IEnumerator<Pointer> enumerator = this.EnumeratePointers(startIndex, endIndex);

            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null)
                {
                    yield return enumerator.Current;
                }
            }
        }

        /// <summary>
        /// Sorts the pointer roots by base address.
        /// </summary>
        public void Sort()
        {
        }

        /// <summary>
        /// Updates the number of total pointers.
        /// </summary>
        public void BuildCount()
        {
            this.count = this.CountLeaves();
        }

        /// <summary>
        /// Gets the enumerator for the discovered pointers.
        /// </summary>
        /// <returns>The enumerator for the discovered pointers.</returns>
        public IEnumerator<Pointer> GetEnumerator()
        {
            return this.EnumeratePointers();
        }

        /// <summary>
        /// Gets the enumerator for the discovered pointers.
        /// </summary>
        /// <param name="startIndex">The start index at which to return non-null values.</param>
        /// <param name="endIndex">The end index at which to return non-null values.</param>
        /// <returns>The enumerator for the discovered pointers.</returns>
        private IEnumerator<Pointer> EnumeratePointers(UInt64? startIndex = null, UInt64? endIndex = null)
        {
            PointerIndicies indicies = new PointerIndicies(startIndex, endIndex);

            foreach (PointerRoot root in this.PointerRoots)
            {
                foreach (PointerBranch branch in root)
                {
                    Stack<Int32> offsets = new Stack<Int32>();

                    foreach (Pointer pointer in this.EnumerateBranches(root.BaseAddress, offsets, branch, indicies))
                    {
                        yield return pointer;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the pointers of the specified pointer branch.
        /// </summary>
        /// <param name="baseAddress">The current base address.</param>
        /// <param name="offsets">The offsets leading to this branch.</param>
        /// <param name="branch">The current branch.</param>
        /// <param name="pointerIndicies">The indicies at which to return non-null values.</param>
        /// <returns>The full pointer path to the branch.</returns>
        private IEnumerable<Pointer> EnumerateBranches(UInt64 baseAddress, Stack<Int32> offsets, PointerBranch branch, PointerIndicies pointerIndicies)
        {
            offsets.Push(branch.Offset);

            // End index reached
            if (pointerIndicies.Finished)
            {
                yield break;
            }

            if (branch.Branches.Count() <= 0)
            {
                Pointer pointer;

                // Only create pointer items when in the range of the selection indicies. This is an optimization to prevent creating unneeded objects.
                if (pointerIndicies.IterateNext())
                {
                    String moduleName;
                    UInt64 address = AddressResolver.GetInstance().AddressToModule(baseAddress, out moduleName);
                    pointer = new Pointer(address, DataType.Int32, offsets.ToArray().Reverse());
                }
                else
                {
                    pointer = null;
                }

                yield return pointer;
            }
            else
            {
                foreach (PointerBranch childBranch in branch)
                {
                    foreach (Pointer pointer in this.EnumerateBranches(baseAddress, offsets, childBranch, pointerIndicies))
                    {
                        yield return pointer;
                    }
                }
            }

            offsets.Pop();
        }

        /// <summary>
        /// Counts the number of leaves for this pointer tree.
        /// </summary>
        /// <returns>The number of leaves on the pointer tree.</returns>
        private UInt64 CountLeaves()
        {
            UInt64 count = 0;

            foreach (PointerRoot root in this.PointerRoots)
            {
                foreach (PointerBranch branch in root)
                {
                    this.CountLeaves(ref count, branch);
                }
            }

            return count;
        }

        /// <summary>
        /// Helper function to count the number of leaves on a pointer tree branch.
        /// </summary>
        /// <param name="count">The current number of leaves.</param>
        /// <param name="branch">The current pointer tree branch.</param>
        private void CountLeaves(ref UInt64 count, PointerBranch branch)
        {
            if (branch.Branches.Count() <= 0)
            {
                count = count + 1;
            }
            else
            {
                foreach (PointerBranch childBranch in branch)
                {
                    this.CountLeaves(ref count, childBranch);
                }
            }
        }

        /// <summary>
        /// Gets the enumerator for the discovered pointers.
        /// </summary>
        /// <returns>The enumerator for the discovered pointers.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Defines a range of indicies over which to collect pointers.
        /// </summary>
        private class PointerIndicies
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PointerIndicies" /> class.
            /// </summary>
            /// <param name="startIndex">The start index.</param>
            /// <param name="endIndex">The end index.</param>
            public PointerIndicies(UInt64? startIndex, UInt64? endIndex)
            {
                // Ensure valid indicies
                if (startIndex != null)
                {
                    if (endIndex == null || endIndex < startIndex)
                    {
                        endIndex = startIndex;
                    }
                }

                this.StartIndex = startIndex;
                this.EndIndex = endIndex;

                this.HasIndicies = this.StartIndex != null;
            }

            /// <summary>
            /// Gets the start index.
            /// </summary>
            public UInt64? StartIndex { get; private set; }

            /// <summary>
            /// Gets the end index.
            /// </summary>
            public UInt64? EndIndex { get; private set; }

            /// <summary>
            /// Gets a value indicating whether there are any indicies in this structure.
            /// </summary>
            public Boolean HasIndicies { get; private set; }

            /// <summary>
            /// Gets a value indicating whether there are indicies left to iterate.
            /// </summary>
            public Boolean Finished
            {
                get
                {
                    return this.HasIndicies && this.EndIndex <= 0;
                }
            }

            /// <summary>
            /// Attempts to iterate to the next valid indicies.
            /// </summary>
            /// <returns>Returns true if the next index is within the original start and end index.</returns>
            public Boolean IterateNext()
            {
                if (!this.HasIndicies)
                {
                    return true;
                }

                Boolean result = this.StartIndex == 0 && !this.Finished;

                if (this.StartIndex > 0)
                {
                    this.StartIndex--;
                }

                if (this.EndIndex > 0)
                {
                    this.EndIndex--;
                }

                return result;
            }
        }
    }
    //// End class
}
//// End namespace