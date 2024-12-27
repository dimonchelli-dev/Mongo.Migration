using System;
using Mongo.Migration.Documents;
using Mongo.Migration.Migrations.Document;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Mongo.Migration.Services.Interceptors
{
    internal class MigrationInterceptor<TDocument>(
        BsonClassMapSerializer<TDocument> serializer,
        IDocumentMigrationRunner migrationRunner,
        IDocumentVersionService documentVersionService)
        : IBsonIdProvider,
          IBsonDocumentSerializer,
          IBsonPolymorphicSerializer,
          IHasDiscriminatorConvention,
          IBsonSerializer<TDocument>
        where TDocument : class, IDocument
    {
        TDocument IBsonSerializer<TDocument>.Deserialize(
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            return DeserializeInternal(context, args);
        }

        void IBsonSerializer<TDocument>.Serialize(
            BsonSerializationContext context,
            BsonSerializationArgs args,
            TDocument value)
        {
            SerializeInternal(context, args, value);
        }

        private TDocument DeserializeInternal(
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            migrationRunner.Run(typeof(TDocument), document);
            var migratedContext =
                BsonDeserializationContext.CreateRoot(new BsonDocumentReader(document));
            return serializer.Deserialize(migratedContext, args);
        }

        private void SerializeInternal(
            BsonSerializationContext context,
            BsonSerializationArgs args,
            TDocument value)
        {
            documentVersionService.DetermineVersion(value);
            serializer.Serialize(context, args, value);
        }

        public void Serialize(
            BsonSerializationContext context,
            BsonSerializationArgs args,
            object value)
        {
            var typedValue = (TDocument)value;
            SerializeInternal(context, args, typedValue);
        }

        public object Deserialize(
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            return DeserializeInternal(context, args);
        }

        public bool GetDocumentId(
            object document,
            out object id,
            out Type idNominalType,
            out IIdGenerator idGenerator)
        {
            return serializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);
        }

        public void SetDocumentId(
            object document,
            object id)
        {
            serializer.SetDocumentId(document, id);
        }

        public bool TryGetMemberSerializationInfo(
            string memberName,
            out BsonSerializationInfo serializationInfo)
        {
            return serializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
        }

        public bool IsDiscriminatorCompatibleWithObjectSerializer  => serializer.IsDiscriminatorCompatibleWithObjectSerializer;

        public IDiscriminatorConvention DiscriminatorConvention  => serializer.DiscriminatorConvention;

        public Type ValueType => serializer.ValueType;
    }
}