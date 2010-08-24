////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Microsoft.SPOT.IO
{
    internal enum StorageEventType : byte
    {
        Invalid = 0,
        Insert = 1,
        Eject = 2,
    }

    internal class StorageEvent : BaseEvent
    {
        public StorageEventType EventType;
        public uint Handle;
        public DateTime Time;
    }

    internal class StorageEventProcessor : IEventProcessor
    {
        public BaseEvent ProcessEvent(uint data1, uint data2, DateTime time)
        {
            StorageEvent ev = new StorageEvent();
            ev.EventType = (StorageEventType)(data1 & 0xFF);
            ev.Handle = data2;
            ev.Time = time;

            return ev;
        }
    }

    internal class StorageEventListener : IEventListener
    {
        public void InitializeForEventSource()
        {
        }

        public bool OnEvent(BaseEvent ev)
        {
            if (ev is StorageEvent)
            {
                RemovableMedia.PostEvent((StorageEvent)ev);
            }

            return true;
        }
    }

    public sealed class VolumeInfo
    {
        public readonly String Name;

        public readonly String VolumeLabel;
        public readonly uint VolumeID;

        public readonly String FileSystem;
        public readonly uint FileSystemFlags;

        public readonly uint DeviceFlags;
        public readonly uint SerialNumber;

        public readonly long TotalFreeSpace;
        public readonly long TotalSize;

        internal uint VolumePtr;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern VolumeInfo(String volumeName);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern VolumeInfo(uint volumePtr);

        // This is used internally to create a VolumeInfo for removable volumes that have been ejected
        internal VolumeInfo(VolumeInfo ejectedVolumeInfo)
        {
            Name = ejectedVolumeInfo.Name;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void Refresh();

        public String RootDirectory
        {
            get { return "\\" + Name; }
        }

        public bool IsFormatted
        {
            get { return FileSystem != null && TotalSize > 0; }
        }

        public void Format(uint parameter)
        {
            Format(FileSystem, parameter, false);
        }

        public void Format(uint parameter, bool force)
        {
            Format(FileSystem, parameter, force);
        }

        public void Format(String fileSystem, uint parameter)
        {
            Format(fileSystem, parameter, false);
        }

        public void Format(String fileSystem, uint parameter, bool force)
        {
            String rootedNameSpace = "\\" + Name;

            bool restoreCD = FileSystemManager.CurrentDirectory == rootedNameSpace;

            if (FileSystemManager.IsInDirectory(FileSystemManager.CurrentDirectory, rootedNameSpace))
            {
                FileSystemManager.SetCurrentDirectory(NativeIO.FSRoot);
            }

            if (force)
            {
                FileSystemManager.ForceRemoveNameSpace(Name);
            }

            Object record = FileSystemManager.LockDirectory(rootedNameSpace);

            try
            {
                NativeIO.Format(Name, fileSystem, parameter);
                Refresh();
            }
            finally
            {
                FileSystemManager.UnlockDirectory(record);
            }

            if (restoreCD)
            {
                FileSystemManager.SetCurrentDirectory(rootedNameSpace);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern static VolumeInfo[] GetVolumes();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern static String[] GetFileSystems();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void FlushAll();
    }

    public static class RemovableMedia
    {
        public static event InsertEventHandler Insert;
        public static event EjectEventHandler Eject;

        private static ArrayList _volumes;

        static RemovableMedia()
        {
            try
            {
                Microsoft.SPOT.EventSink.AddEventProcessor(EventCategory.Storage, new StorageEventProcessor());
                Microsoft.SPOT.EventSink.AddEventListener(EventCategory.Storage, new StorageEventListener());

                _volumes = new ArrayList();

                MountRemovableVolumes();
            }
            catch
            {
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static void MountRemovableVolumes();

        internal static void PostEvent(StorageEvent ev)
        {
            /// We are using timer to process events instead of a separate message loop
            /// thread, to keep it light weight.

            Timer messagePseudoThread = new Timer(MessageHandler, ev, 10, Timeout.Infinite);
        }

        private static void MessageHandler(object args)
        {
            StorageEvent ev = args as StorageEvent;

            if (ev == null)
                return;

            if (ev.EventType == StorageEventType.Insert)
            {
                VolumeInfo volume = new VolumeInfo(ev.Handle);

                _volumes.Add(volume);

                if (Insert != null)
                {
                    MediaEventArgs mediaEventArgs = new MediaEventArgs(volume, ev.Time);

                    Insert(null, mediaEventArgs);
                }
            }
            else if (ev.EventType == StorageEventType.Eject)
            {
                VolumeInfo volumeInfo = RemoveVolume(ev.Handle);

                FileSystemManager.ForceRemoveNameSpace(volumeInfo.Name);

                if (Eject != null)
                {
                    MediaEventArgs mediaEventArgs = new MediaEventArgs(new VolumeInfo(volumeInfo), ev.Time);

                    Eject(null, mediaEventArgs);
                }
            }
        }

        private static VolumeInfo RemoveVolume(uint handle)
        {
            VolumeInfo volumeInfo;
            int count = _volumes.Count;

            for (int i = 0; i < count; i++)
            {
                volumeInfo = ((VolumeInfo)_volumes[i]);
                if (volumeInfo.VolumePtr == handle)
                {
                    _volumes.RemoveAt(i);
                    return volumeInfo;
                }
            }

            return null;
        }
    }

    //--//

    public class MediaEventArgs
    {
        public readonly DateTime Time;
        public readonly VolumeInfo Volume;

        public MediaEventArgs(VolumeInfo volume, DateTime time)
        {
            Time = time;
            Volume = volume;
        }
    }

    public delegate void InsertEventHandler(object sender, MediaEventArgs e);
    public delegate void EjectEventHandler(object sender, MediaEventArgs e);
}


