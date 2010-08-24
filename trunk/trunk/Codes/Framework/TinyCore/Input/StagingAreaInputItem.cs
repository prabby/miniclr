////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using Microsoft.SPOT;

namespace Microsoft.SPOT.Input
{
    /// <summary>
    ///     This class encapsulates an input event while it is being
    ///     processed by the input manager.
    /// </summary>
    /// <remarks>
    ///     This class just provides the dictionary-based storage for
    ///     all of the listeners of the various input manager events.
    /// </remarks>
    public class StagingAreaInputItem
    {

        internal StagingAreaInputItem(bool isMarker, InputEventArgs input, StagingAreaInputItem promote)
        {
            IsMarker = isMarker;
            Input = input;

            if (promote != null && promote._keys != null)
            {
                // REFACTOR -- need a hashtable!

                _keys = (ArrayList)promote._keys.Clone();
                _values = (ArrayList)promote._values.Clone();
            }
        }

        /// <summary>
        ///     Returns the input event.
        /// </summary>
        public readonly InputEventArgs Input;

        /// <summary>
        ///     Provides storage for arbitrary data needed during the
        ///     processing of this input event.
        /// </summary>
        /// <param name="key">
        ///     An arbitrary key for the data.  This cannot be null.
        /// </param>
        /// <returns>
        ///     The data previously set for this key, or null.
        /// </returns>
        public object GetData(object key)
        {
            if (_keys == null)
            {
                return null;
            }
            else
            {
                int idx = _keys.IndexOf(key);
                if (idx < 0)
                {
                    return null;
                }

                return _values[idx];
            }
        }

        /// <summary>
        ///     Provides storage for arbitrary data needed during the
        ///     processing of this input event.
        /// </summary>
        /// <param name="key">
        ///     An arbitrary key for the data.  This cannot be null.
        /// </param>
        /// <param name="value">
        ///     The data to set for this key.  This can be null.
        /// </param>
        public void SetData(object key, object value)
        {
            if (_keys == null)
            {
                _keys = new ArrayList();
                _values = new ArrayList();
            }

            int idx = _keys.IndexOf(key);
            if (idx < 0)
            {
                _keys.Add(key);
                _values.Add(value);
            }
            else
            {
                _keys[idx] = value;
            }
        }

        /// <summary>
        /// Indicates this is a marker.
        /// </summary>
        public readonly bool IsMarker;

        private ArrayList _keys;
        private ArrayList _values;
    }
}


