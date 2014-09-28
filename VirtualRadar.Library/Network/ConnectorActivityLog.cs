﻿// Copyright © 2014 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Network;

namespace VirtualRadar.Library.Network
{
    /// <summary>
    /// The default implementation of <see cref="IConnectorActivityLog"/>.
    /// </summary>
    public class ConnectorActivityLog : IConnectorActivityLog
    {
        /// <summary>
        /// The largest number of activities recorded by the log.
        /// </summary>
        public static readonly int MaximumActivities = 250;

        /// <summary>
        /// The lock that protects the list from multithreaded access.
        /// </summary>
        private SpinLock _SpinLock = new SpinLock();

        /// <summary>
        /// The list of activities recorded by the log.
        /// </summary>
        private LinkedList<ConnectorActivityEvent> _Activities = new LinkedList<ConnectorActivityEvent>();

        private static readonly IConnectorActivityLog _Singleton = new ConnectorActivityLog();
        /// <summary>
        /// See interface docs.
        /// </summary>
        public IConnectorActivityLog Singleton { get { return _Singleton; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler<EventArgs<ConnectorActivityEvent>> ActivityRecorded;

        /// <summary>
        /// Raises <see cref="ActivityRecorded"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivityRecorded(EventArgs<ConnectorActivityEvent> args)
        {
            if(ActivityRecorded != null) ActivityRecorded(this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="connector"></param>
        public void RecordConnectorCreated(IConnector connector)
        {
            connector.ActivityRecorded += Connector_ActivityRecorded;
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="connector"></param>
        public void RecordConnectorDestroyed(IConnector connector)
        {
            connector.ActivityRecorded -= Connector_ActivityRecorded;
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <returns></returns>
        public ConnectorActivityEvent[] GetActivityHistory()
        {
            using(_SpinLock.AcquireLock()) {
                return _Activities.ToArray();
            }
        }

        /// <summary>
        /// Called when a connector indicates that an activity has been recorded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Connector_ActivityRecorded(object sender, EventArgs<ConnectorActivityEvent> args)
        {
            _SpinLock.Lock();
            try {
                while(_Activities.Count >= MaximumActivities) {
                    _Activities.RemoveFirst();
                }
                _Activities.AddLast(args.Value);
            } finally {
                _SpinLock.Unlock();
            }
        }
    }
}
