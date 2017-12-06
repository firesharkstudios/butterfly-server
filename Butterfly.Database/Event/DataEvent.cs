/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Event {
    public enum DataEventType {
        InitialBegin,
        Initial,
        InitialEnd,
        Insert,
        Update, 
        Delete,
    }

    public class DataEvent {

        public readonly DataEventType dataEventType;
        public readonly string name;

        public DataEvent(DataEventType dataEventType, string name) {
            this.id = Guid.NewGuid().ToString();
            this.dataEventType = dataEventType;
            this.name = name;
        }

        public string id {
            get;
            protected set;
        }

        public override string ToString() {
            return $"{{id={id},dataEventType={dataEventType},name={name}}}";
        }
    }

    public class InitialBeginDataEvent : DataEvent {
        public readonly string[] keyFieldNames;

        public InitialBeginDataEvent(string name, string[] keyFieldNames) : base(DataEventType.InitialBegin, name) {
            this.keyFieldNames = keyFieldNames;
        }

        public override string ToString() {
            return $"{{id={id},dataEventType={dataEventType},name={name},keyFieldNames={string.Join(",", keyFieldNames)}}}";
        }
    }

    public class KeyValueDataEvent : DataEvent {
        public readonly object keyValue;

        public KeyValueDataEvent(DataEventType dataEventType, string name, object keyValue) : base(dataEventType, name) {
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
