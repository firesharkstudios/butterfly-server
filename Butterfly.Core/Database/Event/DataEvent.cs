/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database.Event {
    public enum DataEventType {
        InitialBegin,
        Initial,
        InitialEnd,
        Insert,
        Update, 
        Delete,
    }

    /// <summary>
    /// Represents the initial data or a change in the data.  The <see cref="dataEventType"/> indicates 
    /// the type of change and the <see cref="name"/> indicates the table or view name.<para/>
    /// </summary>
    public class DataEvent {

        public readonly DataEventType dataEventType;

        public DataEvent(DataEventType dataEventType) {
            this.id = Guid.NewGuid().ToString();
            this.dataEventType = dataEventType;
        }

        public string id {
            get;
            protected set;
        }

        public override string ToString() {
            return $"{{id={id},dataEventType={dataEventType}}}";
        }
    }

    public class InitialBeginDataEvent : DataEvent {
        public readonly string name;
        public readonly string[] keyFieldNames;

        public InitialBeginDataEvent(string name, string[] keyFieldNames) : base(DataEventType.InitialBegin) {
            this.name = name;
            this.keyFieldNames = keyFieldNames;
        }

        public override string ToString() {
            return $"{{id={id},dataEventType={dataEventType},name={name},keyFieldNames={string.Join(",", keyFieldNames)}}}";
        }
    }

    public class InitialEndDataEvent : DataEvent {
        public InitialEndDataEvent() : base(DataEventType.InitialEnd) {
        }
    }

    public class KeyValueDataEvent : DataEvent {
        public readonly string name;
        public readonly object keyValue;

        public KeyValueDataEvent(DataEventType dataEventType, string name, object keyValue) : base(dataEventType) {
            this.name = name;
            this.keyValue = keyValue;
        }

        public override string ToString() {
            return $"{{id={id},dataEventType={dataEventType},name={name},keyValue={keyValue}}}";
        }
    }

    public class RecordDataEvent : KeyValueDataEvent {
        public readonly Dict record;

        public RecordDataEvent(DataEventType dataEventType, string name, object keyValue, Dict record) : base(dataEventType, name, keyValue) {
            this.record = record;
        }

        public override string ToString() {
            return $"{{id={id},dataEventType={dataEventType},name={name},keyValue={keyValue},record={record}}}";
        }
    }

}
