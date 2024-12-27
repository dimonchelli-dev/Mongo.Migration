using System;

using Mongo.Migration.Migrations.Document;

using MongoDB.Bson.Serialization;

namespace Mongo.Migration.Services.Interceptors
{
    internal class MigrationInterceptorFactory(
        IDocumentMigrationRunner migrationRunner,
        IDocumentVersionService documentVersionService)
        : IMigrationInterceptorFactory
    {
        public IBsonSerializer Create(Type type)
        {
            var genericBsonClassMapSerializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(type);
            var bsonClassMapSerializer = Activator.CreateInstance(genericBsonClassMapSerializerType, BsonClassMap.LookupClassMap(type));

            var genericType = typeof(MigrationInterceptor<>).MakeGenericType(type);
            var interceptor = Activator.CreateInstance(genericType, bsonClassMapSerializer, migrationRunner, documentVersionService);
            return interceptor as IBsonSerializer ?? throw new InvalidOperationException();
        }
    }
}