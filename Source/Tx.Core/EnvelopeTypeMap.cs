﻿namespace Tx.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;

    public class EnvelopeTypeMap : IPartitionableTypeMap<IEnvelope, string>
    {
        private readonly bool handleTransportObject;

        private static readonly IEqualityComparer<string> comparer = StringComparer.Ordinal;

        protected readonly Dictionary<Type, Func<IEnvelope, object>> transforms = new Dictionary<Type, Func<IEnvelope, object>>();

        public EnvelopeTypeMap()
            : this(true)
        {
        }

        protected EnvelopeTypeMap(bool handleTransportObject)
        {
            this.handleTransportObject = handleTransportObject;
        }

        public IEqualityComparer<string> Comparer
        {
            get { return comparer; }
        }

        public string GetTypeKey(Type outputType)
        {
            string manifestId;

            try
            {
                manifestId = outputType.GetTypeIdentifier();
            }
            catch
            {
                manifestId = string.Empty;
            }

            return manifestId;
        }

        public Func<IEnvelope, object> GetTransform(Type outputType)
        {
            Func<IEnvelope, object> transform;
            this.transforms.TryGetValue(outputType, out transform);

            if (transform != null)
            {
                return transform;
            }

            var deserializerMap = this.BuildDeserializers(outputType);

            transform = e => Transform(e, this.handleTransportObject, deserializerMap);

            this.transforms.Add(outputType, transform);

            return transform;
        }

        public Func<IEnvelope, DateTimeOffset> TimeFunction
        {
            get
            {
                return GetTime;
            }
        }

        public string GetInputKey(IEnvelope envelope)
        {
            return envelope.TypeId;
        }

        protected virtual IDictionary<string, Func<byte[], object>> BuildDeserializers(Type outputType)
        {
            return new Dictionary<string, Func<byte[], object>>();
        }

        private static DateTimeOffset GetTime(IEnvelope envelope)
        {
            var time = envelope.ReceivedTime;

            return time;
        }

        private static object Transform(
            IEnvelope envelope,
            bool handleTransportObject,
            IDictionary<string, Func<byte[], object>> deserializerMap)
        {
            if (envelope.PayloadInstance != null)
            {
                return handleTransportObject ? envelope.PayloadInstance : null;
            }
            if (envelope.Payload == null)
            {
                return null;
            }

            object deserializedObject;

            try
            {
                Func<byte[], object> transform;
                if (deserializerMap.TryGetValue(envelope.Protocol, out transform))
                {
                    deserializedObject = transform(envelope.Payload);
                }
                else
                {
                    deserializedObject = null;
                }
            }
            catch
            {
                deserializedObject = null;
            }

            return deserializedObject;
        }
    }
}